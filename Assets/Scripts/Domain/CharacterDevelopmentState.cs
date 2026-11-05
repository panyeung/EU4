using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Per-person growth values plus one plan and at most one settled result for every
    /// age year 0..15. Every mutation validates on a copy of the affected facts first,
    /// so a rejected command leaves the whole state unchanged. Keys are stable IDs and
    /// integer age years only; display names and scenario context never enter this type.
    /// </summary>
    public sealed class CharacterDevelopmentState
    {
        private sealed class Entry
        {
            public PersonId PersonId;
            public CharacterGrowthValues Values;
            public readonly SortedDictionary<int, EducationYearPlan> Plans =
                new SortedDictionary<int, EducationYearPlan>();
            public readonly SortedDictionary<int, EducationYearResult> Results =
                new SortedDictionary<int, EducationYearResult>();
        }

        private readonly Dictionary<PersonId, Entry> _entries = new Dictionary<PersonId, Entry>();

        private CharacterDevelopmentState()
        {
        }

        public static CharacterDevelopmentState CreateEmpty() => new CharacterDevelopmentState();

        public int PersonCount => _entries.Count;

        /// <summary>Stable ordinal person ID snapshot.</summary>
        public IReadOnlyList<PersonId> PersonIds
        {
            get
            {
                var ids = new List<PersonId>(_entries.Keys);
                ids.Sort((a, b) => string.CompareOrdinal(a.Value ?? string.Empty, b.Value ?? string.Empty));
                return ids.AsReadOnly();
            }
        }

        public bool Contains(PersonId personId) => !personId.IsEmpty && _entries.ContainsKey(personId);

        public bool TryAddPerson(PersonId personId, CharacterGrowthValues values, out string error)
        {
            if (personId.IsEmpty)
            {
                error = "人物 ID 为空。";
                return false;
            }
            if (_entries.ContainsKey(personId))
            {
                error = "人物成长记录已存在：" + personId.Value + "。";
                return false;
            }

            _entries.Add(personId, new Entry { PersonId = personId, Values = values });
            error = string.Empty;
            return true;
        }

        public bool TryGetValues(PersonId personId, out CharacterGrowthValues values)
        {
            if (!personId.IsEmpty && _entries.TryGetValue(personId, out Entry entry))
            {
                values = entry.Values;
                return true;
            }
            values = default;
            return false;
        }

        public bool TryGetPlan(PersonId personId, int ageYear, out EducationYearPlan plan)
        {
            if (!personId.IsEmpty && _entries.TryGetValue(personId, out Entry entry) &&
                entry.Plans.TryGetValue(ageYear, out plan))
                return true;
            plan = default;
            return false;
        }

        public bool TryGetResult(PersonId personId, int ageYear, out EducationYearResult result)
        {
            if (!personId.IsEmpty && _entries.TryGetValue(personId, out Entry entry) &&
                entry.Results.TryGetValue(ageYear, out result))
                return true;
            result = default;
            return false;
        }

        public bool HasResult(PersonId personId, int ageYear) =>
            TryGetResult(personId, ageYear, out _);

        /// <summary>Stable planned age years for one person, ascending.</summary>
        public IReadOnlyList<int> PlannedAgeYears(PersonId personId)
        {
            if (!personId.IsEmpty && _entries.TryGetValue(personId, out Entry entry))
                return new List<int>(entry.Plans.Keys).AsReadOnly();
            return Array.Empty<int>();
        }

        public IReadOnlyList<int> SettledAgeYears(PersonId personId)
        {
            if (!personId.IsEmpty && _entries.TryGetValue(personId, out Entry entry))
                return new List<int>(entry.Results.Keys).AsReadOnly();
            return Array.Empty<int>();
        }

        /// <summary>
        /// Stores or replaces the pending plan for its age year. A settled year can never
        /// be replanned, and replacing a plan never changes values or results.
        /// </summary>
        public bool TrySetPlan(EducationYearPlan plan, out string error)
        {
            if (!_entries.TryGetValue(plan.Learner, out Entry entry))
            {
                error = "培养对象没有成长记录。";
                return false;
            }
            if (plan.AgeYear < EducationYearPlan.MinimumAgeYear ||
                plan.AgeYear > EducationYearPlan.MaximumAgeYear ||
                !Enum.IsDefined(typeof(EducationDirection), plan.Direction))
            {
                error = "培养计划年度或方向无效。";
                return false;
            }
            if (plan.IsFreeGrowth != plan.Mentor.IsEmpty)
            {
                error = plan.IsFreeGrowth
                    ? "自由成长不能指定导师。"
                    : "定向培养必须指定导师。";
                return false;
            }
            if (entry.Results.ContainsKey(plan.AgeYear))
            {
                error = "该年龄年度已结算，不能替换计划。";
                return false;
            }

            entry.Plans[plan.AgeYear] = plan;
            error = string.Empty;
            return true;
        }

        public bool TryClearPlan(PersonId personId, int ageYear, out string error)
        {
            if (!_entries.TryGetValue(personId, out Entry entry))
            {
                error = "培养对象没有成长记录。";
                return false;
            }
            if (entry.Results.ContainsKey(ageYear))
            {
                error = "该年龄年度已结算，不能撤销计划。";
                return false;
            }
            entry.Plans.Remove(ageYear);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Settles exactly one age year. The stored plan supplies direction and mentor;
        /// an unplanned year uses free growth. When the caller reports that the stored
        /// mentor is no longer eligible, free growth replaces the directed plan instead of
        /// blocking the month transaction. A year can be settled once: a second call
        /// returns false before any value changes.
        /// </summary>
        public bool TrySettleYear(PersonId personId, int ageYear, bool hasAptitude,
            GrowthAttribute childAptitude, MentorSpecialty storedMentorSpecialty,
            bool supportiveEnvironment, bool mentorUnavailable, out EducationYearResult result,
            out string error)
        {
            result = default;
            if (!_entries.TryGetValue(personId, out Entry entry))
            {
                error = "培养对象没有成长记录。";
                return false;
            }
            if (ageYear < EducationYearPlan.MinimumAgeYear || ageYear > EducationYearPlan.MaximumAgeYear)
            {
                error = "培养年度必须在 0..15 岁之间。";
                return false;
            }
            if (entry.Results.ContainsKey(ageYear))
            {
                error = "该年龄年度已经结算，不能重复结算。";
                return false;
            }

            bool hasPlan = entry.Plans.TryGetValue(ageYear, out EducationYearPlan plan);
            bool fallback = !hasPlan || plan.IsFreeGrowth || mentorUnavailable;
            EducationDirection direction = fallback ? EducationDirection.FreeGrowth : plan.Direction;
            PersonId mentor = fallback ? default : plan.Mentor;
            MentorSpecialty specialty = fallback ? MentorSpecialty.None : storedMentorSpecialty;

            if (!CharacterGrowthRules.TryResolve(entry.Values, direction, hasAptitude,
                    childAptitude, specialty, supportiveEnvironment,
                    out EducationYearResult resolved, out string resolveError))
            {
                error = resolveError;
                return false;
            }

            string explanation = resolved.Explanation;
            bool explicitFallback = hasPlan && !plan.IsFreeGrowth && mentorUnavailable;
            if (explicitFallback)
                explanation += "（原导师已失效，按自由成长结算）";

            result = new EducationYearResult(resolved.Direction, mentor, resolved.Before,
                resolved.After, explanation, explicitFallback);
            entry.Values = result.After;
            entry.Results[ageYear] = result;
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Restores one persisted plan verbatim. Used only by validated save loading; a
        /// settled year still accepts its original plan for display purposes.
        /// </summary>
        public bool TryRestorePlan(EducationYearPlan plan, out string error)
        {
            if (!_entries.TryGetValue(plan.Learner, out Entry entry))
            {
                error = "培养对象没有成长记录。";
                return false;
            }
            if (plan.AgeYear < EducationYearPlan.MinimumAgeYear ||
                plan.AgeYear > EducationYearPlan.MaximumAgeYear ||
                !Enum.IsDefined(typeof(EducationDirection), plan.Direction) ||
                plan.IsFreeGrowth != plan.Mentor.IsEmpty)
            {
                error = "培养计划结构无效。";
                return false;
            }
            if (entry.Results.TryGetValue(plan.AgeYear, out EducationYearResult settled) &&
                (settled.Direction != plan.Direction || settled.Mentor != plan.Mentor))
            {
                error = "已结算年度的计划与结果不一致。";
                return false;
            }
            entry.Plans[plan.AgeYear] = plan;
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Restores one persisted settlement and moves values to its after snapshot.
        /// Callers restore years in ascending order; the final entry must match capture.
        /// </summary>
        public bool TryRestoreSettledYear(PersonId personId, int ageYear, EducationYearResult result,
            out string error)
        {
            if (!_entries.TryGetValue(personId, out Entry entry))
            {
                error = "培养对象没有成长记录。";
                return false;
            }
            if (ageYear < EducationYearPlan.MinimumAgeYear || ageYear > EducationYearPlan.MaximumAgeYear)
            {
                error = "培养年度必须在 0..15 岁之间。";
                return false;
            }
            if (entry.Results.ContainsKey(ageYear))
            {
                error = "培养年度结果重复。";
                return false;
            }
            if (!Enum.IsDefined(typeof(EducationDirection), result.Direction) ||
                !CharacterGrowthValues.TryCreate(result.Before.Ninjutsu, result.Before.MartialArts,
                    result.Before.Scholarship, result.Before.Management, result.Before.Social, out _, out _) ||
                !CharacterGrowthValues.TryCreate(result.After.Ninjutsu, result.After.MartialArts,
                    result.After.Scholarship, result.After.Management, result.After.Social, out _, out _) ||
                string.IsNullOrWhiteSpace(result.Explanation) ||
                result.Direction == EducationDirection.FreeGrowth && !result.Mentor.IsEmpty)
            {
                error = "培养年度结果结构无效。";
                return false;
            }

            entry.Values = result.After;
            entry.Results[ageYear] = result;
            error = string.Empty;
            return true;
        }

        public CharacterDevelopmentState Clone()
        {
            var clone = new CharacterDevelopmentState();
            foreach (KeyValuePair<PersonId, Entry> pair in _entries)
            {
                var entry = new Entry { PersonId = pair.Value.PersonId, Values = pair.Value.Values };
                foreach (KeyValuePair<int, EducationYearPlan> plan in pair.Value.Plans)
                    entry.Plans.Add(plan.Key, plan.Value);
                foreach (KeyValuePair<int, EducationYearResult> result in pair.Value.Results)
                    entry.Results.Add(result.Key, result.Value);
                clone._entries.Add(pair.Key, entry);
            }
            return clone;
        }

        /// <summary>Structural validation used by saves and defensive tests.</summary>
        public bool TryValidate(out string error)
        {
            foreach (PersonId personId in PersonIds)
            {
                Entry entry = _entries[personId];
                foreach (KeyValuePair<int, EducationYearPlan> pair in entry.Plans)
                {
                    EducationYearPlan plan = pair.Value;
                    if (pair.Key != plan.AgeYear || plan.Learner != personId ||
                        pair.Key < EducationYearPlan.MinimumAgeYear ||
                        pair.Key > EducationYearPlan.MaximumAgeYear ||
                        !Enum.IsDefined(typeof(EducationDirection), plan.Direction) ||
                        plan.IsFreeGrowth != plan.Mentor.IsEmpty)
                    {
                        error = "人物 " + personId.Value + " 的培养计划结构无效。";
                        return false;
                    }
                    if (entry.Results.TryGetValue(plan.AgeYear, out EducationYearResult settled) &&
                        (settled.Direction != plan.Direction || settled.Mentor != plan.Mentor))
                    {
                        error = "人物 " + personId.Value + " 的已结算计划与结果不一致。";
                        return false;
                    }
                }

                foreach (KeyValuePair<int, EducationYearResult> pair in entry.Results)
                {
                    EducationYearResult result = pair.Value;
                    if (pair.Key < EducationYearPlan.MinimumAgeYear ||
                        pair.Key > EducationYearPlan.MaximumAgeYear ||
                        !Enum.IsDefined(typeof(EducationDirection), result.Direction) ||
                        string.IsNullOrWhiteSpace(result.Explanation) ||
                        result.Direction == EducationDirection.FreeGrowth && !result.Mentor.IsEmpty)
                    {
                        error = "人物 " + personId.Value + " 的培养结果结构无效。";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
