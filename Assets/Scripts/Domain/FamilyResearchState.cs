using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Current progress of the single active family research project.</summary>
    public readonly struct FamilyResearchPlan
    {
        public FamilyResearchPlan(string researchId, PersonId researcherId, int progress)
        {
            ResearchId = researchId ?? string.Empty;
            ResearcherId = researcherId;
            Progress = progress;
        }

        public string ResearchId { get; }
        public PersonId ResearcherId { get; }
        public int Progress { get; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(ResearchId) && ResearcherId.IsEmpty;
    }

    /// <summary>
    /// Mutable SC-021 family research state. It owns exactly one optional active
    /// project, the recorded family technique IDs and the last settled month. It
    /// deliberately owns no person-mastery collection: family knowledge never makes
    /// an individual able to use a technique.
    /// </summary>
    public sealed class FamilyResearchState
    {
        private readonly List<string> _knownTechniqueIds = new List<string>();
        private bool _hasPlan;
        private FamilyResearchPlan _plan;
        private int _lastSettledMonthIndex;

        private FamilyResearchState(int lastSettledMonthIndex)
        {
            _lastSettledMonthIndex = lastSettledMonthIndex;
        }

        /// <summary>New game / migrated save: no plan, no knowledge, prior month settled.</summary>
        public static FamilyResearchState CreateInitial(GameDate date) =>
            new FamilyResearchState(IndustryCalendar.MonthIndex(date) - 1);

        public static FamilyResearchState CreateEmpty() => new FamilyResearchState(0);

        public bool HasPlan => _hasPlan;
        public FamilyResearchPlan Plan => _plan;
        public string ResearchId => _hasPlan ? _plan.ResearchId : string.Empty;
        public PersonId ResearcherId => _hasPlan ? _plan.ResearcherId : default;
        public int Progress => _hasPlan ? _plan.Progress : 0;
        public int LastSettledMonthIndex => _lastSettledMonthIndex;

        /// <summary>Ordinal-sorted, duplicate-free family technique IDs.</summary>
        public IReadOnlyList<string> KnownTechniqueIds => _knownTechniqueIds.AsReadOnly();

        public bool IsKnownTechnique(string techniqueId)
        {
            if (string.IsNullOrWhiteSpace(techniqueId))
                return false;
            for (int i = 0; i < _knownTechniqueIds.Count; i++)
                if (string.Equals(_knownTechniqueIds[i], techniqueId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        /// <summary>
        /// Starts the one active project or replaces its researcher. Replacing the
        /// researcher of the same project preserves progress; switching projects starts
        /// that project at zero. An already-recorded technique cannot be researched again.
        /// </summary>
        public bool TryStartOrReplacePlan(string researchId, PersonId researcherId, out string error)
        {
            if (!FamilyResearchCatalog.TryGet(researchId, out FamilyResearchDefinition definition))
            {
                error = "未知研究项目：" + (researchId ?? "(null)") + "。";
                return false;
            }
            if (researcherId.IsEmpty)
            {
                error = "研究者不能为空。";
                return false;
            }
            if (IsKnownTechnique(definition.TechniqueId))
            {
                error = "「" + definition.TechniqueDisplayName + "」已经记录为家族知识，无需再次研究。";
                return false;
            }

            int progress = _hasPlan &&
                           string.Equals(_plan.ResearchId, definition.Id, StringComparison.Ordinal)
                ? _plan.Progress
                : 0;
            _plan = new FamilyResearchPlan(definition.Id, researcherId, progress);
            _hasPlan = true;
            error = string.Empty;
            return true;
        }

        /// <summary>Adds deterministic progress and completes exactly once when full.</summary>
        public bool TryAdvance(int amount, out string error)
        {
            if (!_hasPlan)
            {
                error = "当前没有进行中的研究项目。";
                return false;
            }
            if (amount <= 0)
            {
                error = "研究进度增量必须为正数。";
                return false;
            }
            if (!FamilyResearchCatalog.TryGet(_plan.ResearchId, out FamilyResearchDefinition definition))
            {
                error = "研究项目 ID 无效：" + _plan.ResearchId + "。";
                return false;
            }
            if (IsKnownTechnique(definition.TechniqueId))
            {
                error = "该技艺已经记录为家族知识。";
                return false;
            }

            int next = Math.Min(definition.RequiredProgress, _plan.Progress + amount);
            _plan = new FamilyResearchPlan(_plan.ResearchId, _plan.ResearcherId, next);
            if (next >= definition.RequiredProgress)
                return TryComplete(definition, out error);

            error = string.Empty;
            return true;
        }

        /// <summary>Records the researched technique and clears the finished project.</summary>
        public bool TryComplete(out string error)
        {
            if (!_hasPlan)
            {
                error = "当前没有进行中的研究项目。";
                return false;
            }
            if (!FamilyResearchCatalog.TryGet(_plan.ResearchId, out FamilyResearchDefinition definition))
            {
                error = "研究项目 ID 无效：" + _plan.ResearchId + "。";
                return false;
            }
            return TryComplete(definition, out error);
        }

        private bool TryComplete(FamilyResearchDefinition definition, out string error)
        {
            if (_plan.Progress < definition.RequiredProgress)
            {
                error = "研究进度尚未达到完成要求。";
                return false;
            }

            if (!IsKnownTechnique(definition.TechniqueId))
            {
                _knownTechniqueIds.Add(definition.TechniqueId);
                _knownTechniqueIds.Sort(StringComparer.Ordinal);
            }

            _hasPlan = false;
            _plan = default;
            error = string.Empty;
            return true;
        }

        /// <summary>Clears a paused/invalid plan without recording knowledge.</summary>
        public bool TryClearPlan(out string error)
        {
            _hasPlan = false;
            _plan = default;
            error = string.Empty;
            return true;
        }

        public bool TrySetLastSettledMonthIndex(int monthIndex, out string error)
        {
            if (monthIndex < 0)
            {
                error = "已结算月份索引不能为负。";
                return false;
            }

            _lastSettledMonthIndex = monthIndex;
            error = string.Empty;
            return true;
        }

        public FamilyResearchState Clone()
        {
            var clone = new FamilyResearchState(_lastSettledMonthIndex)
            {
                _hasPlan = _hasPlan,
                _plan = _plan
            };
            clone._knownTechniqueIds.AddRange(_knownTechniqueIds);
            return clone;
        }

        /// <summary>
        /// Rebuilds a validated state. Stable IDs and progress are checked here; the save
        /// validator owns roster cross-references. A finished project must not remain
        /// active and a known technique must not be researched again.
        /// </summary>
        public static bool TryRestore(string researchId, string researcherId, int progress,
            int lastSettledMonthIndex, string[] knownTechniqueIds, out FamilyResearchState state,
            out string error)
        {
            state = null;
            if (lastSettledMonthIndex < 0)
            {
                error = "存档研究已结算月份不能为负。";
                return false;
            }

            var restored = new FamilyResearchState(lastSettledMonthIndex);
            if (knownTechniqueIds == null)
            {
                error = "存档缺少家族技艺知识列表。";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < knownTechniqueIds.Length; i++)
            {
                string techniqueId = knownTechniqueIds[i];
                if (!FamilyResearchCatalog.IsKnownTechniqueId(techniqueId))
                {
                    error = "存档包含未知家族技艺：" + (techniqueId ?? "(null)") + "。";
                    return false;
                }
                if (!seen.Add(techniqueId))
                {
                    error = "存档家族技艺重复：" + techniqueId + "。";
                    return false;
                }
                restored._knownTechniqueIds.Add(techniqueId);
            }
            restored._knownTechniqueIds.Sort(StringComparer.Ordinal);

            bool hasResearchId = !string.IsNullOrWhiteSpace(researchId);
            bool hasResearcher = !string.IsNullOrWhiteSpace(researcherId);
            if (!hasResearchId)
            {
                if (hasResearcher || progress != 0)
                {
                    error = "存档研究计划缺少项目 ID，但仍有研究者或进度。";
                    return false;
                }
                error = string.Empty;
                state = restored;
                return true;
            }

            if (!FamilyResearchCatalog.TryGet(researchId, out FamilyResearchDefinition definition))
            {
                error = "存档研究项目 ID 未知：" + researchId + "。";
                return false;
            }
            if (!hasResearcher)
            {
                error = "存档研究计划缺少研究者 ID。";
                return false;
            }
            if (!PersonId.TryParse(researcherId, out PersonId researcher))
            {
                error = "存档研究者 ID 无效。";
                return false;
            }
            if (progress < 0 || progress >= definition.RequiredProgress)
            {
                error = "存档研究进度必须在 0.." + (definition.RequiredProgress - 1) + " 之间。";
                return false;
            }
            if (restored.IsKnownTechnique(definition.TechniqueId))
            {
                error = "存档研究计划与已记录家族知识冲突。";
                return false;
            }

            restored._hasPlan = true;
            restored._plan = new FamilyResearchPlan(researchId, researcher, progress);
            error = string.Empty;
            state = restored;
            return true;
        }
    }
}
