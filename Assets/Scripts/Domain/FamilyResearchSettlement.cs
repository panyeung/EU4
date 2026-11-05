using System;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    public enum FamilyResearchOutcome
    {
        Succeeded = 0,
        NoPlan = 1,
        AlreadyKnown = 2,
        InvalidResearcher = 3,
        ResourceShortage = 4,
        AlreadySettled = 5,
        SettlementGap = 6,
        InvalidState = 7
    }

    /// <summary>Immutable, player-readable report of one month's research settlement.</summary>
    public sealed class FamilyResearchMonthReport
    {
        public FamilyResearchMonthReport(int year, int month, IndustryResources beforeResources,
            IndustryResources afterResources, int beforeProgress, int afterProgress,
            int requiredProgress, bool completed, bool paused, bool changed, string researchName,
            string techniqueName, string researcherName, string status, string summary)
        {
            Year = year;
            Month = month;
            BeforeResources = beforeResources;
            AfterResources = afterResources;
            BeforeProgress = beforeProgress;
            AfterProgress = afterProgress;
            RequiredProgress = requiredProgress;
            Completed = completed;
            Paused = paused;
            Changed = changed;
            ResearchName = researchName ?? string.Empty;
            TechniqueName = techniqueName ?? string.Empty;
            ResearcherName = researcherName ?? string.Empty;
            Status = status ?? string.Empty;
            Summary = summary ?? string.Empty;
        }

        public int Year { get; }
        public int Month { get; }
        public IndustryResources BeforeResources { get; }
        public IndustryResources AfterResources { get; }
        public int BeforeProgress { get; }
        public int AfterProgress { get; }
        public int RequiredProgress { get; }
        public bool Completed { get; }
        public bool Paused { get; }
        public bool Changed { get; }
        public string ResearchName { get; }
        public string TechniqueName { get; }
        public string ResearcherName { get; }
        public string Status { get; }
        public string Summary { get; }
    }

    /// <summary>Immutable result of one pure research settlement.</summary>
    public readonly struct FamilyResearchSettlementResult
    {
        public FamilyResearchSettlementResult(FamilyResearchOutcome outcome, bool succeeded,
            string message, FamilyResearchState nextState, IndustryResources nextResources,
            FamilyResearchMonthReport report, string researchId, string techniqueId,
            PersonId researcherId, int beforeProgress, int afterProgress, bool completed, bool paused)
        {
            Outcome = outcome;
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            NextState = nextState;
            NextResources = nextResources;
            Report = report;
            ResearchId = researchId ?? string.Empty;
            TechniqueId = techniqueId ?? string.Empty;
            ResearcherId = researcherId;
            BeforeProgress = beforeProgress;
            AfterProgress = afterProgress;
            Completed = completed;
            Paused = paused;
        }

        public FamilyResearchOutcome Outcome { get; }
        public bool Succeeded { get; }
        public string Message { get; }
        public FamilyResearchState NextState { get; }
        public IndustryResources NextResources { get; }
        public FamilyResearchMonthReport Report { get; }
        public string ResearchId { get; }
        public string TechniqueId { get; }
        public PersonId ResearcherId { get; }
        public int BeforeProgress { get; }
        public int AfterProgress { get; }
        public bool Completed { get; }
        public bool Paused { get; }
    }

    /// <summary>
    /// Pure, deterministic SC-021 monthly research settlement. It never mutates its
    /// inputs and never consumes a random stream. A month always advances the research
    /// settled index; only a valid researcher with enough funds and supplies spends
    /// exactly the authored cost and adds the authored progress.
    /// </summary>
    public static class FamilyResearchSettlement
    {
        /// <summary>
        /// One shared researcher rule: a living, current-family, non-retired adult who
        /// does not already occupy an industry post.
        /// </summary>
        public static bool IsEligibleResearcher(SuccessionState succession, PersonId personId,
            GameDate date, IndustryState industry, out string reason)
        {
            if (!IndustryService.CanWork(succession, personId, date, out reason))
                return false;

            if (industry != null)
            {
                foreach (IndustryAssignment assignment in industry.Assignments)
                {
                    if (assignment.PersonId.IsEmpty || assignment.PersonId != personId)
                        continue;
                    reason = "已担任产业「" + IndustryService.IndustryName(assignment.IndustryId) +
                             "」负责人，不能同时进行家族研究。";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public static FamilyResearchSettlementResult Plan(FamilyResearchState state,
            IndustryResources resources, SuccessionState succession, GameDate closedMonth) =>
            Plan(state, resources, succession, closedMonth, null);

        public static FamilyResearchSettlementResult Plan(FamilyResearchState state,
            IndustryResources resources, SuccessionState succession, GameDate closedMonth,
            IndustryState industry)
        {
            if (state == null || succession == null || succession.World == null)
                return Rejected(FamilyResearchOutcome.InvalidState, state, resources, default, 0,
                    "研究或继承状态为空。");

            int currentIndex = IndustryCalendar.MonthIndex(closedMonth);
            if (state.LastSettledMonthIndex == currentIndex)
                return Rejected(FamilyResearchOutcome.AlreadySettled, state, resources, default, 0,
                    "本月研究已经结算，不会重复扣费或推进。");
            if (state.LastSettledMonthIndex > currentIndex)
                return Rejected(FamilyResearchOutcome.InvalidState, state, resources, default, 0,
                    "已结算月份晚于当前月份，研究状态无效。");
            if (state.LastSettledMonthIndex < currentIndex - 1)
                return Rejected(FamilyResearchOutcome.SettlementGap, state, resources, default, 0,
                    "存在未结算月份，不能静默补算多个月研究。");

            FamilyResearchState next = state.Clone();
            next.TrySetLastSettledMonthIndex(currentIndex, out _);

            if (!state.HasPlan)
            {
                var idleReport = new FamilyResearchMonthReport(closedMonth.Year, closedMonth.Month,
                    resources, resources, 0, 0, 0, false, false, false, string.Empty,
                    string.Empty, string.Empty, "无研究计划", "本月无研究计划。");
                return new FamilyResearchSettlementResult(FamilyResearchOutcome.NoPlan, true,
                    idleReport.Summary, next, resources, idleReport, string.Empty, string.Empty,
                    default, 0, 0, false, false);
            }

            FamilyResearchPlan plan = state.Plan;
            if (!FamilyResearchCatalog.TryGet(plan.ResearchId, out FamilyResearchDefinition definition))
                return Rejected(FamilyResearchOutcome.InvalidState, state, resources, plan,
                    plan.Progress, "研究项目 ID 无效：" + plan.ResearchId + "。");

            string researcherName = IndustryService.PersonName(succession, plan.ResearcherId);
            if (next.IsKnownTechnique(definition.TechniqueId))
            {
                next.TryClearPlan(out _);
                var knownReport = new FamilyResearchMonthReport(closedMonth.Year, closedMonth.Month,
                    resources, resources, plan.Progress, plan.Progress, definition.RequiredProgress,
                    false, true, false, definition.ProjectDisplayName,
                    definition.TechniqueDisplayName, researcherName, "已记录",
                    "「" + definition.TechniqueDisplayName +
                    "」已记录为家族知识，研究计划已清除。资源保持不变。");
                return new FamilyResearchSettlementResult(FamilyResearchOutcome.AlreadyKnown, true,
                    knownReport.Summary, next, resources, knownReport, plan.ResearchId,
                    definition.TechniqueId, plan.ResearcherId, plan.Progress, plan.Progress, false, true);
            }

            if (!IsEligibleResearcher(succession, plan.ResearcherId, closedMonth, industry,
                    out string reason))
            {
                var pausedReport = new FamilyResearchMonthReport(closedMonth.Year, closedMonth.Month,
                    resources, resources, plan.Progress, plan.Progress, definition.RequiredProgress,
                    false, true, false, definition.ProjectDisplayName,
                    definition.TechniqueDisplayName, researcherName, "研究者失效",
                    "研究暂停：研究者" + researcherName + "无法继续（" + reason +
                    "）。资源与进度保持不变。");
                return new FamilyResearchSettlementResult(FamilyResearchOutcome.InvalidResearcher,
                    true, pausedReport.Summary, next, resources, pausedReport, plan.ResearchId,
                    definition.TechniqueId, plan.ResearcherId, plan.Progress, plan.Progress, false, true);
            }

            int missingFunds = Math.Max(0, definition.MonthlyFundsCost - resources.Funds);
            int missingSupplies = Math.Max(0, definition.MonthlySuppliesCost - resources.Supplies);
            if (missingFunds > 0 || missingSupplies > 0)
            {
                string shortage = missingFunds > 0 && missingSupplies > 0
                    ? "资金不足 " + Number(missingFunds) + "、物资不足 " + Number(missingSupplies)
                    : missingFunds > 0
                        ? "资金不足 " + Number(missingFunds)
                        : "物资不足 " + Number(missingSupplies);
                var shortageReport = new FamilyResearchMonthReport(closedMonth.Year, closedMonth.Month,
                    resources, resources, plan.Progress, plan.Progress, definition.RequiredProgress,
                    false, true, false, definition.ProjectDisplayName,
                    definition.TechniqueDisplayName, researcherName, "资源不足",
                    "研究暂停：" + shortage + "。资源与进度保持不变。");
                return new FamilyResearchSettlementResult(FamilyResearchOutcome.ResourceShortage,
                    true, shortageReport.Summary, next, resources, shortageReport, plan.ResearchId,
                    definition.TechniqueId, plan.ResearcherId, plan.Progress, plan.Progress, false, true);
            }

            IndustryResources.TryCreate(resources.Funds - definition.MonthlyFundsCost,
                resources.Supplies - definition.MonthlySuppliesCost, resources.Intelligence,
                out IndustryResources afterResources, out _);

            int beforeProgress = plan.Progress;
            if (!next.TryAdvance(definition.MonthlyProgress, out string advanceError))
                return Rejected(FamilyResearchOutcome.InvalidState, state, resources, plan,
                    beforeProgress, "研究推进失败：" + advanceError);
            bool completed = !next.HasPlan && next.IsKnownTechnique(definition.TechniqueId);
            int afterProgress = completed ? definition.RequiredProgress : next.Progress;

            string status = completed ? "已完成" : "研究中";
            string summary = completed
                ? "「" + definition.TechniqueDisplayName + "」研究完成：研究者" + researcherName +
                  "，进度 " + Number(beforeProgress) + " → " + Number(afterProgress) +
                  "，已记录为家族知识。"
                : "「" + definition.ProjectDisplayName + "」推进：研究者" + researcherName +
                  "，进度 " + Number(beforeProgress) + " → " + Number(afterProgress) + "。";

            var report = new FamilyResearchMonthReport(closedMonth.Year, closedMonth.Month,
                resources, afterResources, beforeProgress, afterProgress,
                definition.RequiredProgress, completed, false, true, definition.ProjectDisplayName,
                definition.TechniqueDisplayName, researcherName, status, summary);
            return new FamilyResearchSettlementResult(FamilyResearchOutcome.Succeeded, true, summary,
                next, afterResources, report, plan.ResearchId, definition.TechniqueId,
                plan.ResearcherId, beforeProgress, afterProgress, completed, false);
        }

        private static FamilyResearchSettlementResult Rejected(FamilyResearchOutcome outcome,
            FamilyResearchState state, IndustryResources resources, FamilyResearchPlan plan,
            int progress, string message)
        {
            string researchId = plan.IsEmpty ? string.Empty : plan.ResearchId;
            string techniqueId = string.Empty;
            if (FamilyResearchCatalog.TryGet(researchId, out FamilyResearchDefinition definition))
                techniqueId = definition.TechniqueId;
            return new FamilyResearchSettlementResult(outcome, false, message,
                state != null ? state.Clone() : null, resources, null, researchId, techniqueId,
                plan.ResearcherId, progress, progress, false, false);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
