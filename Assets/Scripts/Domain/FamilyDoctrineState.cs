using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// The three family creeds available to the SC-024 slice. Values are stable IDs and
    /// must never be renumbered because saves and event conditions compare them directly.
    /// </summary>
    public enum FamilyCreedKind
    {
        SecrecyFirst = 0,
        RighteousnessFirst = 1,
        MeritFirst = 2
    }

    /// <summary>The three personal ambitions a head may choose for one lifetime.</summary>
    public enum FamilyAmbitionKind
    {
        DevelopHousehold = 0,
        PassDownTechnique = 1,
        EducateHeir = 2
    }

    /// <summary>
    /// Immutable progress of one head's ambition. The known-technique snapshot is taken
    /// when the ambition is selected so a technique the family already owned can never
    /// count as a new research result.
    /// </summary>
    public sealed class FamilyAmbitionRecord
    {
        private readonly IReadOnlyList<string> _knownTechniqueIds;

        internal FamilyAmbitionRecord(PersonId headId, FamilyAmbitionKind kind, int progress, int target,
            IReadOnlyList<string> knownTechniqueIds)
        {
            HeadId = headId;
            Kind = kind;
            Progress = progress;
            Target = target;
            _knownTechniqueIds = knownTechniqueIds ?? Array.Empty<string>();
        }

        public PersonId HeadId { get; }
        public FamilyAmbitionKind Kind { get; }
        public int Progress { get; }
        public int Target { get; }
        public bool IsComplete => Progress >= Target;

        /// <summary>Ordinal-sorted, duplicate-free technique IDs known when the ambition began.</summary>
        public IReadOnlyList<string> KnownTechniqueIds => _knownTechniqueIds;

        internal FamilyAmbitionRecord WithProgress(int progress) =>
            new FamilyAmbitionRecord(HeadId, Kind, progress, Target, _knownTechniqueIds);
    }

    /// <summary>
    /// SC-024 family doctrine domain state. It owns exactly one persistent family creed
    /// and one ambition record per stable head person ID. Old heads are retained verbatim
    /// when a new head selects an ambition; nothing is transferred across succession.
    ///
    /// The progress methods only consume results that another system has already produced
    /// (industry month report, research completions, education year result). They never run
    /// a settlement and never mutate the supplied report/result. Every accepted call is
    /// deterministic, and a completed ambition or an already-applied event is a no-op.
    /// </summary>
    public sealed class FamilyDoctrineState
    {
        public const int DevelopHouseholdTarget = 3;
        public const int PassDownTechniqueTarget = 1;
        public const int EducateHeirTarget = 5;

        private sealed class AmbitionEntry
        {
            public FamilyAmbitionRecord Record;
            public readonly HashSet<int> AppliedIndustryMonths = new HashSet<int>();
            public readonly HashSet<string> AppliedTechniqueIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<EducationYearKey> AppliedEducationYears = new HashSet<EducationYearKey>();
        }

        private readonly struct EducationYearKey : IEquatable<EducationYearKey>
        {
            public EducationYearKey(PersonId heirId, int ageYear)
            {
                HeirId = heirId;
                AgeYear = ageYear;
            }

            public PersonId HeirId { get; }
            public int AgeYear { get; }

            public bool Equals(EducationYearKey other) =>
                HeirId == other.HeirId && AgeYear == other.AgeYear;

            public override bool Equals(object obj) => obj is EducationYearKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (HeirId.GetHashCode() * 397) ^ AgeYear;
                }
            }
        }

        private readonly Dictionary<PersonId, AmbitionEntry> _ambitions =
            new Dictionary<PersonId, AmbitionEntry>();

        public FamilyDoctrineState()
        {
            Creed = FamilyCreedKind.RighteousnessFirst;
        }

        /// <summary>Persistent family creed; survives head succession.</summary>
        public FamilyCreedKind Creed { get; private set; }

        public int AmbitionCount => _ambitions.Count;

        /// <summary>Stable ordinal head IDs with an ambition record, ascending.</summary>
        public IReadOnlyList<PersonId> HeadIds
        {
            get
            {
                var ids = new List<PersonId>(_ambitions.Keys);
                ids.Sort((a, b) => string.CompareOrdinal(a.Value ?? string.Empty, b.Value ?? string.Empty));
                return ids.AsReadOnly();
            }
        }

        public bool TrySetCreed(FamilyCreedKind creed, out string error)
        {
            if (!Enum.IsDefined(typeof(FamilyCreedKind), creed))
            {
                error = "未知家训 ID。";
                return false;
            }

            Creed = creed;
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Selects this head's single ambition. The snapshot is sorted ordinally and must
        /// contain distinct, non-empty stable technique IDs. A head that already has a
        /// record is rejected: old ambitions are never replaced or inherited.
        /// </summary>
        public bool TryStartAmbition(PersonId headId, FamilyAmbitionKind kind,
            IReadOnlyCollection<string> knownTechniqueIds, out string error)
        {
            if (headId.IsEmpty)
            {
                error = "家主人物 ID 不能为空。";
                return false;
            }
            if (!Enum.IsDefined(typeof(FamilyAmbitionKind), kind))
            {
                error = "未知抱负类型。";
                return false;
            }
            if (_ambitions.ContainsKey(headId))
            {
                error = "该家主已经选定抱负：" + headId.Value + "。";
                return false;
            }
            if (!TrySnapshotTechniques(knownTechniqueIds, out string[] snapshot, out error))
                return false;

            _ambitions.Add(headId, new AmbitionEntry
            {
                Record = new FamilyAmbitionRecord(headId, kind, 0, TargetFor(kind), Array.AsReadOnly(snapshot))
            });
            error = string.Empty;
            return true;
        }

        public bool TryGetAmbition(PersonId headId, out FamilyAmbitionRecord record)
        {
            if (!headId.IsEmpty && _ambitions.TryGetValue(headId, out AmbitionEntry entry))
            {
                record = entry.Record;
                return true;
            }

            record = null;
            return false;
        }

        /// <summary>
        /// Advances 发展家业 for one settled month. A report qualifies when at least one
        /// Operated line has a positive sum across funds, supplies and intelligence. The
        /// same month index is applied at most once and a completed ambition ignores input.
        /// </summary>
        public bool TryAdvanceHouseholdAmbition(PersonId headId, IndustryMonthReport report,
            out string error)
        {
            if (!TryGetMatchingEntry(headId, FamilyAmbitionKind.DevelopHousehold, out AmbitionEntry entry,
                    out error))
                return false;
            if (entry.Record.IsComplete)
            {
                error = string.Empty;
                return true;
            }
            if (report == null)
            {
                error = "产业月结报告不能为空。";
                return false;
            }
            if (report.Year <= 0 || report.Month < 1 || report.Month > 12)
            {
                error = "产业月结报告的年份或月份无效。";
                return false;
            }

            int monthIndex = IndustryCalendar.MonthIndex(new GameDate(report.Year, report.Month, 1));
            if (entry.AppliedIndustryMonths.Contains(monthIndex))
            {
                error = string.Empty;
                return true;
            }

            // A month is evaluated once even when it does not qualify; reusing the same
            // month later must never turn a settled report into progress.
            entry.AppliedIndustryMonths.Add(monthIndex);
            if (!QualifiesHouseholdMonth(report))
            {
                error = string.Empty;
                return true;
            }

            entry.Record = entry.Record.WithProgress(
                Math.Min(entry.Record.Target, entry.Record.Progress + 1));
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Advances 传承家技 for newly completed techniques. Only an ID absent from the
        /// selection-time snapshot counts, each distinct ID counts once, and the target is
        /// one technique. The method does not settle research.
        /// </summary>
        public bool TryAdvanceTechniqueAmbition(PersonId headId,
            IReadOnlyCollection<string> newlyCompletedTechniqueIds, out string error)
        {
            if (!TryGetMatchingEntry(headId, FamilyAmbitionKind.PassDownTechnique, out AmbitionEntry entry,
                    out error))
                return false;
            if (entry.Record.IsComplete)
            {
                error = string.Empty;
                return true;
            }
            if (newlyCompletedTechniqueIds == null)
            {
                error = "新完成技艺列表不能为空。";
                return false;
            }

            var candidates = new List<string>(newlyCompletedTechniqueIds.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string techniqueId in newlyCompletedTechniqueIds)
            {
                if (string.IsNullOrWhiteSpace(techniqueId))
                {
                    error = "新完成技艺 ID 不能为空。";
                    return false;
                }
                if (!seen.Add(techniqueId))
                    continue;
                candidates.Add(techniqueId);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                string techniqueId = candidates[i];
                if (entry.Record.IsComplete)
                    break;
                if (ContainsTechnique(entry.Record.KnownTechniqueIds, techniqueId))
                    continue;
                if (!entry.AppliedTechniqueIds.Add(techniqueId))
                    continue;

                entry.Record = entry.Record.WithProgress(
                    Math.Min(entry.Record.Target, entry.Record.Progress + 1));
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Advances 培养继承人 from one settled age year of the currently designated heir.
        /// Only positive per-ability changes across the five abilities count. The same
        /// (heir, age year) pair is applied at most once, so replaying a result cannot
        /// double progress. The method does not settle education itself.
        /// </summary>
        public bool TryAdvanceHeirEducationAmbition(PersonId headId, PersonId designatedHeirId,
            int ageYear, EducationYearResult result, out string error)
        {
            if (!TryGetMatchingEntry(headId, FamilyAmbitionKind.EducateHeir, out AmbitionEntry entry,
                    out error))
                return false;
            if (entry.Record.IsComplete)
            {
                error = string.Empty;
                return true;
            }
            if (designatedHeirId.IsEmpty)
            {
                error = "指定继承人 ID 不能为空。";
                return false;
            }
            if (ageYear < EducationYearPlan.MinimumAgeYear || ageYear > EducationYearPlan.MaximumAgeYear)
            {
                error = "教育年度必须在 0..15 岁之间。";
                return false;
            }
            if (!Enum.IsDefined(typeof(EducationDirection), result.Direction) ||
                string.IsNullOrWhiteSpace(result.Explanation))
            {
                error = "教育年度结果无效。";
                return false;
            }

            var key = new EducationYearKey(designatedHeirId, ageYear);
            if (entry.AppliedEducationYears.Contains(key))
            {
                error = string.Empty;
                return true;
            }

            // An age year is consumed once even when it does not qualify; a later replay
            // of that same (heir, age year) pair can never count.
            entry.AppliedEducationYears.Add(key);

            // A free-growth fallback is not a directed success and must never advance the
            // heir ambition, even when clamping produces apparent positive deltas.
            if (result.UsedFreeGrowthFallback)
            {
                error = string.Empty;
                return true;
            }

            int gain = PositiveGrowthSum(result);
            if (gain <= 0)
            {
                error = string.Empty;
                return true;
            }

            entry.Record = entry.Record.WithProgress(
                Math.Min(entry.Record.Target, entry.Record.Progress + gain));
            error = string.Empty;
            return true;
        }

        /// <summary>Deep copy: each head record, snapshot and applied-event set is independent.</summary>
        public FamilyDoctrineState Clone()
        {
            var clone = new FamilyDoctrineState { Creed = Creed };
            foreach (KeyValuePair<PersonId, AmbitionEntry> pair in _ambitions)
            {
                AmbitionEntry source = pair.Value;
                var snapshot = new List<string>(source.Record.KnownTechniqueIds.Count);
                for (int i = 0; i < source.Record.KnownTechniqueIds.Count; i++)
                    snapshot.Add(source.Record.KnownTechniqueIds[i]);

                var copy = new AmbitionEntry
                {
                    Record = new FamilyAmbitionRecord(source.Record.HeadId, source.Record.Kind,
                        source.Record.Progress, source.Record.Target, snapshot.AsReadOnly())
                };
                copy.AppliedIndustryMonths.UnionWith(source.AppliedIndustryMonths);
                copy.AppliedTechniqueIds.UnionWith(source.AppliedTechniqueIds);
                copy.AppliedEducationYears.UnionWith(source.AppliedEducationYears);
                clone._ambitions.Add(pair.Key, copy);
            }
            return clone;
        }

        private static int TargetFor(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold: return DevelopHouseholdTarget;
                case FamilyAmbitionKind.PassDownTechnique: return PassDownTechniqueTarget;
                case FamilyAmbitionKind.EducateHeir: return EducateHeirTarget;
                default: return 0;
            }
        }

        private bool TryGetMatchingEntry(PersonId headId, FamilyAmbitionKind kind,
            out AmbitionEntry entry, out string error)
        {
            if (headId.IsEmpty)
            {
                entry = null;
                error = "家主人物 ID 不能为空。";
                return false;
            }
            if (!_ambitions.TryGetValue(headId, out entry))
            {
                error = "该家主尚未选定抱负。";
                return false;
            }
            if (entry.Record.Kind != kind)
            {
                error = "当前抱负类型不匹配。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool TrySnapshotTechniques(IReadOnlyCollection<string> knownTechniqueIds,
            out string[] snapshot, out string error)
        {
            snapshot = Array.Empty<string>();
            if (knownTechniqueIds == null)
            {
                error = string.Empty;
                return true;
            }

            var ids = new List<string>(knownTechniqueIds.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string techniqueId in knownTechniqueIds)
            {
                if (string.IsNullOrWhiteSpace(techniqueId))
                {
                    error = "已知技艺 ID 不能为空。";
                    return false;
                }
                if (!seen.Add(techniqueId))
                    continue;
                ids.Add(techniqueId);
            }

            ids.Sort(StringComparer.Ordinal);
            snapshot = ids.ToArray();
            error = string.Empty;
            return true;
        }

        private static bool ContainsTechnique(IReadOnlyList<string> techniqueIds, string techniqueId)
        {
            for (int i = 0; i < techniqueIds.Count; i++)
                if (string.Equals(techniqueIds[i], techniqueId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        /// <summary>
        /// Approved SC-024 household rule: a month qualifies only when the report has no
        /// shortage, at least one line actually operated, and the combined net across the
        /// three resource types for all Operated lines is positive. Non-operated lines are
        /// never counted, and one positive Operated line cannot mask negative Operated lines.
        /// </summary>
        private static bool QualifiesHouseholdMonth(IndustryMonthReport report)
        {
            if (report.HasShortage)
                return false;

            bool anyOperated = false;
            long aggregateNet = 0;
            for (int i = 0; i < report.Lines.Count; i++)
            {
                IndustryLineResult line = report.Lines[i];
                if (line.Status != IndustryLineStatus.Operated)
                    continue;

                anyOperated = true;
                aggregateNet += (long)line.NetOutputFunds + line.NetOutputSupplies +
                                line.NetOutputIntelligence;
            }

            return anyOperated && aggregateNet > 0;
        }

        private static int PositiveGrowthSum(EducationYearResult result)
        {
            int sum = 0;
            sum += Math.Max(0, result.After.Ninjutsu - result.Before.Ninjutsu);
            sum += Math.Max(0, result.After.MartialArts - result.Before.MartialArts);
            sum += Math.Max(0, result.After.Scholarship - result.Before.Scholarship);
            sum += Math.Max(0, result.After.Management - result.Before.Management);
            sum += Math.Max(0, result.After.Social - result.Before.Social);
            return sum;
        }
    }
}
