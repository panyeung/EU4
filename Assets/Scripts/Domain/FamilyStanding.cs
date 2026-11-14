using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>One requested relation change, keyed by stable employer/faction ID.</summary>
    public readonly struct MissionRelationDelta
    {
        public MissionRelationDelta(string relationId, int delta)
        {
            RelationId = relationId ?? string.Empty;
            Delta = delta;
        }

        public string RelationId { get; }
        public int Delta { get; }
    }

    /// <summary>
    /// M07 family standing: family fame, infiltration concealment and employer/faction
    /// relations keyed by stable IDs. Fame and concealment are independent values in
    /// 0..100; relations are in -100..100. Every change is clamped, and the caller
    /// receives the actual before/after values so a clamped settlement never claims a
    /// nominal change it did not apply.
    /// </summary>
    public sealed class FamilyStandingState
    {
        public const int Minimum = 0;
        public const int Maximum = 100;
        public const int RelationMinimum = -100;
        public const int RelationMaximum = 100;
        public const int InitialFame = 0;
        public const int InitialConcealment = 50;

        private readonly Dictionary<string, int> _relations =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public int Fame { get; private set; }
        public int Concealment { get; private set; }

        public static FamilyStandingState CreateInitial() =>
            new FamilyStandingState { Fame = InitialFame, Concealment = InitialConcealment };

        public static bool TryCreate(int fame, int concealment,
            IReadOnlyList<KeyValuePair<string, int>> relations,
            out FamilyStandingState state, out string error)
        {
            state = null;
            error = string.Empty;
            if (fame < Minimum || fame > Maximum)
            {
                error = "家族名望必须在 " + Minimum + ".." + Maximum + "。";
                return false;
            }
            if (concealment < Minimum || concealment > Maximum)
            {
                error = "潜入隐蔽度必须在 " + Minimum + ".." + Maximum + "。";
                return false;
            }

            var created = new FamilyStandingState { Fame = fame, Concealment = concealment };
            if (relations != null)
            {
                for (int i = 0; i < relations.Count; i++)
                {
                    string relationId = relations[i].Key;
                    int value = relations[i].Value;
                    if (string.IsNullOrWhiteSpace(relationId))
                    {
                        error = "关系稳定 ID 为空。";
                        return false;
                    }
                    if (value < RelationMinimum || value > RelationMaximum)
                    {
                        error = "关系值超出 " + RelationMinimum + ".." + RelationMaximum + "。";
                        return false;
                    }
                    if (created._relations.ContainsKey(relationId))
                    {
                        error = "关系稳定 ID 重复：" + relationId + "。";
                        return false;
                    }
                    created._relations.Add(relationId, value);
                }
            }

            state = created;
            return true;
        }

        /// <summary>Missing relation records read as 0, as approved for M07.</summary>
        public int GetRelation(string relationId)
        {
            if (string.IsNullOrEmpty(relationId))
                return 0;
            return _relations.TryGetValue(relationId, out int value) ? value : 0;
        }

        public bool HasRelation(string relationId) =>
            !string.IsNullOrEmpty(relationId) && _relations.ContainsKey(relationId);

        /// <summary>
        /// Applies the approved deltas to a copy-free live state and reports the actual
        /// applied before/after values. Invalid input is rejected before any mutation.
        /// </summary>
        public bool TryApplyDeltas(int fameDelta, int concealmentDelta,
            IReadOnlyList<MissionRelationDelta> relationDeltas,
            out MissionStandingResult result, out string error)
        {
            result = null;
            error = string.Empty;
            if (relationDeltas == null)
                relationDeltas = new MissionRelationDelta[0];

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var changes = new List<MissionStandingChange>();
            for (int i = 0; i < relationDeltas.Count; i++)
            {
                string relationId = relationDeltas[i].RelationId;
                if (string.IsNullOrWhiteSpace(relationId))
                {
                    error = "关系变化缺少稳定 ID。";
                    return false;
                }
                if (!seen.Add(relationId))
                {
                    error = "同一次结算重复修改关系：" + relationId + "。";
                    return false;
                }
                int before = GetRelation(relationId);
                int after = Clamp(before, relationDeltas[i].Delta, RelationMinimum, RelationMaximum);
                changes.Add(new MissionStandingChange(relationId, before, after));
            }

            int fameBefore = Fame;
            int concealmentBefore = Concealment;
            int fameAfter = Clamp(fameBefore, fameDelta, Minimum, Maximum);
            int concealmentAfter = Clamp(concealmentBefore, concealmentDelta, Minimum, Maximum);

            Fame = fameAfter;
            Concealment = concealmentAfter;
            for (int i = 0; i < changes.Count; i++)
            {
                MissionStandingChange change = changes[i];
                if (change.Before == change.After)
                {
                    if (_relations.ContainsKey(change.RelationId))
                        _relations[change.RelationId] = change.After;
                    continue;
                }
                _relations[change.RelationId] = change.After;
            }

            result = new MissionStandingResult(fameBefore, fameAfter, concealmentBefore,
                concealmentAfter, changes.AsReadOnly());
            return true;
        }

        /// <summary>Relations in stable ordinal ID order for deterministic saves and tests.</summary>
        public IReadOnlyList<KeyValuePair<string, int>> RelationsSorted()
        {
            var keys = new List<string>(_relations.Keys);
            keys.Sort(StringComparer.Ordinal);
            var rows = new List<KeyValuePair<string, int>>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
                rows.Add(new KeyValuePair<string, int>(keys[i], _relations[keys[i]]));
            return rows.AsReadOnly();
        }

        public FamilyStandingState Clone()
        {
            var clone = new FamilyStandingState { Fame = Fame, Concealment = Concealment };
            foreach (KeyValuePair<string, int> row in _relations)
                clone._relations[row.Key] = row.Value;
            return clone;
        }

        /// <summary>Restores a validated snapshot in place, used by transaction rollback.</summary>
        public void CopyFrom(FamilyStandingState source)
        {
            if (source == null)
                return;
            Fame = source.Fame;
            Concealment = source.Concealment;
            _relations.Clear();
            foreach (KeyValuePair<string, int> row in source._relations)
                _relations[row.Key] = row.Value;
        }

        private static int Clamp(int before, int delta, int minimum, int maximum)
        {
            long value = (long)before + delta;
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return (int)value;
        }
    }

    /// <summary>
    /// Session-safe injury ledger. An injured person cannot join physical or infiltration
    /// missions, keeps ordinary administrative work, and recovers at the next month
    /// settlement regardless of that settlement's success or failure.
    /// </summary>
    public sealed class MissionInjuryState
    {
        private readonly List<PersonId> _injured = new List<PersonId>();

        public IReadOnlyList<PersonId> Injured => _injured.AsReadOnly();

        public bool IsInjured(PersonId personId) => _injured.Contains(personId);

        public bool TrySetInjured(PersonId personId, bool injured, out string error)
        {
            error = string.Empty;
            if (personId.IsEmpty)
            {
                error = "伤员记录缺少人物 ID。";
                return false;
            }
            bool current = _injured.Contains(personId);
            if (current == injured)
                return true;
            if (injured)
            {
                _injured.Add(personId);
                Sort();
                return true;
            }
            _injured.Remove(personId);
            return true;
        }

        /// <summary>Clears every injury at one month boundary; returns how many recovered.</summary>
        public int RecoverAll()
        {
            int count = _injured.Count;
            _injured.Clear();
            return count;
        }

        public static bool TryCreate(IReadOnlyList<PersonId> injured,
            out MissionInjuryState state, out string error)
        {
            state = null;
            error = string.Empty;
            var created = new MissionInjuryState();
            if (injured != null)
            {
                var seen = new HashSet<PersonId>();
                for (int i = 0; i < injured.Count; i++)
                {
                    PersonId personId = injured[i];
                    if (personId.IsEmpty)
                    {
                        error = "存档伤员记录包含空人物 ID。";
                        return false;
                    }
                    if (!seen.Add(personId))
                    {
                        error = "存档伤员记录重复。";
                        return false;
                    }
                    created._injured.Add(personId);
                }
            }
            created.Sort();
            state = created;
            return true;
        }

        public MissionInjuryState Clone()
        {
            var clone = new MissionInjuryState();
            clone._injured.AddRange(_injured);
            return clone;
        }

        public void CopyFrom(MissionInjuryState source)
        {
            if (source == null)
                return;
            _injured.Clear();
            _injured.AddRange(source._injured);
        }

        private void Sort() =>
            _injured.Sort((a, b) => string.CompareOrdinal(a.Value ?? string.Empty,
                b.Value ?? string.Empty));
    }
}
