using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SengokuChronicle.Domain
{
    public enum IndustryLineStatus { Operated, Vacant, Stalled, InvalidAssignment }

    /// <summary>One industry line of a pure monthly settlement plan.</summary>
    public readonly struct IndustryLineResult
    {
        public IndustryLineResult(string industryId, string industryName, PersonId assigneeId,
            string assigneeName, IndustryLineStatus status, string reason, int upkeepFunds,
            int upkeepSupplies, int grossOutputFunds, int grossOutputSupplies,
            int grossOutputIntelligence, int netOutputFunds, int netOutputSupplies,
            int netOutputIntelligence, int appliedFundsDelta, int appliedSuppliesDelta,
            int appliedIntelligenceDelta, bool maintenanceRetainedFromOutput,
            int grossOutputFood = 0, int materialsCostFunds = 0)
        {
            IndustryId = industryId ?? string.Empty;
            IndustryName = industryName ?? string.Empty;
            AssigneeId = assigneeId;
            AssigneeName = assigneeName ?? string.Empty;
            Status = status;
            Reason = reason ?? string.Empty;
            UpkeepFunds = upkeepFunds;
            UpkeepSupplies = upkeepSupplies;
            GrossOutputFunds = grossOutputFunds;
            GrossOutputSupplies = grossOutputSupplies;
            GrossOutputIntelligence = grossOutputIntelligence;
            NetOutputFunds = netOutputFunds;
            NetOutputSupplies = netOutputSupplies;
            NetOutputIntelligence = netOutputIntelligence;
            AppliedFundsDelta = appliedFundsDelta;
            AppliedSuppliesDelta = appliedSuppliesDelta;
            AppliedIntelligenceDelta = appliedIntelligenceDelta;
            MaintenanceRetainedFromOutput = maintenanceRetainedFromOutput;
            GrossOutputFood = grossOutputFood;
            MaterialsCostFunds = materialsCostFunds;
        }

        public string IndustryId { get; }
        public string IndustryName { get; }
        public PersonId AssigneeId { get; }
        public string AssigneeName { get; }
        public IndustryLineStatus Status { get; }
        public string Reason { get; }
        public int UpkeepFunds { get; }
        public int UpkeepSupplies { get; }
        public int GrossOutputFunds { get; }
        public int GrossOutputSupplies { get; }
        public int GrossOutputIntelligence { get; }
        public int NetOutputFunds { get; }
        public int NetOutputSupplies { get; }
        public int NetOutputIntelligence { get; }
        public int AppliedFundsDelta { get; }
        public int AppliedSuppliesDelta { get; }
        public int AppliedIntelligenceDelta { get; }
        public bool MaintenanceRetainedFromOutput { get; }
        /// <summary>Approved M08B food harvest; always zero for a line that did not operate.</summary>
        public int GrossOutputFood { get; }
        /// <summary>Added funds cost of this line's actual material input at the month's index.</summary>
        public int MaterialsCostFunds { get; }
    }

    /// <summary>Immutable result of one month's industry settlement.</summary>
    public sealed class IndustryMonthReport
    {
        public IndustryMonthReport(int year, int month, IndustryResources before, IndustryResources after,
            IReadOnlyList<IndustryLineResult> lines, IReadOnlyList<IndustryVacancy> vacated,
            bool hasShortage, bool anyStaffed, bool changed, string summary)
        {
            Year = year;
            Month = month;
            Before = before;
            After = after;
            Lines = lines ?? Array.Empty<IndustryLineResult>();
            Vacated = vacated ?? Array.Empty<IndustryVacancy>();
            HasShortage = hasShortage;
            AnyStaffed = anyStaffed;
            Changed = changed;
            Summary = summary ?? string.Empty;
        }

        public int Year { get; }
        public int Month { get; }
        public IndustryResources Before { get; }
        public IndustryResources After { get; }
        public IReadOnlyList<IndustryLineResult> Lines { get; }
        public IReadOnlyList<IndustryVacancy> Vacated { get; }
        public bool HasShortage { get; }
        public bool AnyStaffed { get; }
        public bool Changed { get; }
        public string Summary { get; }
    }

    public enum IndustrySettlementOutcome { Succeeded, AlreadySettled, SettlementGap, InvalidState }

    public readonly struct IndustrySettlementResult
    {
        public IndustrySettlementResult(IndustrySettlementOutcome outcome, bool succeeded, string message,
            IndustryState nextState, IndustryMonthReport report)
        {
            Outcome = outcome;
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            NextState = nextState;
            Report = report;
        }

        public IndustrySettlementOutcome Outcome { get; }
        public bool Succeeded { get; }
        public string Message { get; }
        public IndustryState NextState { get; }
        public IndustryMonthReport Report { get; }
    }

    /// <summary>
    /// Pure, deterministic monthly settlement. It never mutates its input, never
    /// consumes the random stream and applies farm before trading so the farm's
    /// same-month net output can pay the trading house's maintenance check.
    /// </summary>
    public static class IndustrySettlement
    {
        /// <summary>
        /// Three-argument compatibility entry: current callers keep the 1000‰ base material
        /// index, while M08B month-turn code passes the previous month's settled index.
        /// </summary>
        public static IndustrySettlementResult Plan(IndustryState state, SuccessionState succession,
            GameDate closedMonth) =>
            Plan(state, succession, closedMonth, MarketState.BaselinePriceIndexPermille);

        public static IndustrySettlementResult Plan(IndustryState state, SuccessionState succession,
            GameDate closedMonth, int materialsPriceIndexPermille)
        {
            if (state == null || succession == null || succession.World == null)
                return Rejected(IndustrySettlementOutcome.InvalidState, state, "产业或继承状态为空。");
            if (!MarketState.IsValidPriceIndex(materialsPriceIndexPermille))
                return Rejected(IndustrySettlementOutcome.InvalidState, state,
                    "生产物资价格指数超出允许范围 100..100000‰。");

            int currentIndex = IndustryCalendar.MonthIndex(closedMonth);
            if (state.LastSettledMonthIndex == currentIndex)
                return Rejected(IndustrySettlementOutcome.AlreadySettled, state,
                    "本月产业已经结算，不会重复扣费或产出。");
            if (state.LastSettledMonthIndex > currentIndex)
                return Rejected(IndustrySettlementOutcome.InvalidState, state,
                    "已结算月份晚于当前月份，产业状态无效。");
            if (state.LastSettledMonthIndex < currentIndex - 1)
                return Rejected(IndustrySettlementOutcome.SettlementGap, state,
                    "存在未结算月份，不能静默补算多个月。");

            IndustryResources before = state.Resources;
            IndustryState next = state.Clone();
            var vacated = IndustryService.ReconcileAssignments(next, succession, closedMonth);

            int funds = before.Funds;
            int supplies = before.Supplies;
            int intelligence = before.Intelligence;
            bool anyStaffed = false;
            bool hasShortage = false;
            var lines = new List<IndustryLineResult>(IndustryCatalog.All.Count);

            foreach (IndustryDefinition definition in IndustryCatalog.All)
            {
                state.TryGetAssignee(definition.Id, out PersonId originalAssignee);
                if (!originalAssignee.IsEmpty)
                    anyStaffed = true;

                if (originalAssignee.IsEmpty)
                {
                    lines.Add(VacantLine(definition));
                    continue;
                }

                string assigneeName = IndustryService.PersonName(succession, originalAssignee);
                if (TryFindVacancy(vacated, definition.Id, out IndustryVacancy vacancy))
                {
                    lines.Add(new IndustryLineResult(definition.Id, definition.DisplayName,
                        originalAssignee, assigneeName, IndustryLineStatus.InvalidAssignment,
                        vacancy.Reason, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false));
                    continue;
                }

                if (definition.MaintainsFromOutput)
                {
                    int retained = Math.Min(definition.UpkeepSupplies, definition.OutputSupplies);
                    int materialsCost = MaterialsCostFunds(retained, materialsPriceIndexPermille);
                    long payableFunds = (long)funds + definition.OutputFunds - definition.UpkeepFunds;
                    if (materialsCost > payableFunds)
                    {
                        hasShortage = true;
                        lines.Add(StalledLine(definition, originalAssignee, assigneeName,
                            (int)(materialsCost - payableFunds), 0, materialsCost));
                        continue;
                    }

                    int netFunds = definition.OutputFunds - definition.UpkeepFunds - materialsCost;
                    int netSupplies = definition.OutputSupplies - retained;
                    int netIntelligence = definition.OutputIntelligence;
                    int beforeFunds = funds;
                    int beforeSupplies = supplies;
                    int beforeIntelligence = intelligence;
                    funds = Clamp(funds + (long)netFunds);
                    supplies = Clamp(supplies + (long)netSupplies);
                    intelligence = Clamp(intelligence + (long)netIntelligence);
                    lines.Add(new IndustryLineResult(definition.Id, definition.DisplayName,
                        originalAssignee, assigneeName, IndustryLineStatus.Operated,
                        "运作中", definition.UpkeepFunds, retained,
                        definition.OutputFunds, definition.OutputSupplies,
                        definition.OutputIntelligence, netFunds, netSupplies, netIntelligence,
                        funds - beforeFunds, supplies - beforeSupplies, intelligence - beforeIntelligence,
                        true, definition.OutputFood, materialsCost));
                    continue;
                }

                int lineMaterialsCost = MaterialsCostFunds(definition.UpkeepSupplies,
                    materialsPriceIndexPermille);
                int requiredFunds = definition.UpkeepFunds + lineMaterialsCost;
                int missingFunds = Math.Max(0, requiredFunds - funds);
                int missingSupplies = Math.Max(0, definition.UpkeepSupplies - supplies);
                if (missingFunds > 0 || missingSupplies > 0)
                {
                    hasShortage = true;
                    lines.Add(StalledLine(definition, originalAssignee, assigneeName,
                        missingFunds, missingSupplies, lineMaterialsCost));
                    continue;
                }

                int lineFunds = funds;
                int lineSupplies = supplies;
                int lineIntelligence = intelligence;
                funds -= requiredFunds;
                supplies -= definition.UpkeepSupplies;
                funds = Clamp(funds + (long)definition.OutputFunds);
                supplies = Clamp(supplies + (long)definition.OutputSupplies);
                intelligence = Clamp(intelligence + (long)definition.OutputIntelligence);
                lines.Add(new IndustryLineResult(definition.Id, definition.DisplayName,
                    originalAssignee, assigneeName, IndustryLineStatus.Operated,
                    "运作中", definition.UpkeepFunds, definition.UpkeepSupplies,
                    definition.OutputFunds, definition.OutputSupplies,
                    definition.OutputIntelligence,
                    definition.OutputFunds - requiredFunds,
                    definition.OutputSupplies - definition.UpkeepSupplies,
                    definition.OutputIntelligence,
                    funds - lineFunds, supplies - lineSupplies, intelligence - lineIntelligence,
                    false, definition.OutputFood, lineMaterialsCost));
            }

            IndustryResources.TryCreate(funds, supplies, intelligence,
                out IndustryResources after, out _);
            next.TrySetResources(after, out _);
            next.TrySetLastSettledMonthIndex(currentIndex, out _);

            bool changed = after != before;
            string summary = !anyStaffed && vacated.Count == 0
                ? "本月无产业运作。"
                : BuildSummary(closedMonth, before, after, lines);
            var report = new IndustryMonthReport(closedMonth.Year, closedMonth.Month, before, after,
                lines.AsReadOnly(), vacated, hasShortage, anyStaffed, changed, summary);
            return new IndustrySettlementResult(IndustrySettlementOutcome.Succeeded, true,
                "产业月结完成。", next, report);
        }

        private static IndustryLineResult VacantLine(IndustryDefinition definition) =>
            new IndustryLineResult(definition.Id, definition.DisplayName, default, string.Empty,
                IndustryLineStatus.Vacant, "空缺：无人负责，本月无产出",
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false);

        private static IndustryLineResult StalledLine(IndustryDefinition definition, PersonId assignee,
            string assigneeName, int missingFunds, int missingSupplies, int materialsCost)
        {
            string reason = "资源不足：缺 " + Number(missingFunds) + " 资金、" +
                            Number(missingSupplies) + " 物资";
            if (materialsCost > 0)
                reason += "（其中材料成本 " + Number(materialsCost) + " 资金）";
            reason += "，本月停产";
            return new IndustryLineResult(definition.Id, definition.DisplayName, assignee, assigneeName,
                IndustryLineStatus.Stalled, reason, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false);
        }

        /// <summary>Approved M08B input cost: ceil(units * base price 1 * index / 1000).</summary>
        private static int MaterialsCostFunds(int units, int materialsPriceIndexPermille)
        {
            if (units <= 0)
                return 0;
            return (int)(((long)units * materialsPriceIndexPermille +
                          MarketState.BaselinePriceIndexPermille - 1) /
                         MarketState.BaselinePriceIndexPermille);
        }

        private static bool TryFindVacancy(IReadOnlyList<IndustryVacancy> vacated, string industryId,
            out IndustryVacancy vacancy)
        {
            for (int i = 0; i < vacated.Count; i++)
            {
                if (string.Equals(vacated[i].IndustryId, industryId, StringComparison.Ordinal))
                {
                    vacancy = vacated[i];
                    return true;
                }
            }
            vacancy = default;
            return false;
        }

        private static int Clamp(long value) =>
            value < IndustryResources.Minimum ? IndustryResources.Minimum
                : value > IndustryResources.Maximum ? IndustryResources.Maximum
                : (int)value;

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string BuildSummary(GameDate month, IndustryResources before,
            IndustryResources after, IReadOnlyList<IndustryLineResult> lines)
        {
            var builder = new StringBuilder(256);
            for (int i = 0; i < lines.Count; i++)
            {
                IndustryLineResult line = lines[i];
                if (i > 0)
                    builder.Append('；');
                switch (line.Status)
                {
                    case IndustryLineStatus.Operated:
                        builder.Append(line.IndustryName).Append('（').Append(line.AssigneeName)
                            .Append("）：");
                        if (line.MaintenanceRetainedFromOutput)
                        {
                            builder.Append("收成 +").Append(Number(line.GrossOutputFunds))
                                .Append("资金/+").Append(Number(line.GrossOutputSupplies))
                                .Append("物资，保留 ").Append(Number(line.UpkeepSupplies))
                                .Append("物资维护（材料成本 ").Append(Number(line.MaterialsCostFunds))
                                .Append("资金），产粮 +").Append(Number(line.GrossOutputFood))
                                .Append("，净 +").Append(Number(line.NetOutputFunds))
                                .Append("资金/+").Append(Number(line.NetOutputSupplies))
                                .Append("物资");
                            AppendCapNote(builder, line.NetOutputFunds, line.AppliedFundsDelta, "资金");
                            AppendCapNote(builder, line.NetOutputSupplies, line.AppliedSuppliesDelta, "物资");
                        }
                        else
                        {
                            builder.Append("维护 ").Append(Number(line.UpkeepFunds)).Append("资金/")
                                .Append(Number(line.UpkeepSupplies)).Append("物资（材料成本 ")
                                .Append(Number(line.MaterialsCostFunds)).Append("资金），产出 +")
                                .Append(Number(line.GrossOutputFunds)).Append("资金/+")
                                .Append(Number(line.GrossOutputIntelligence)).Append("情报");
                            AppendCapNote(builder, line.NetOutputFunds, line.AppliedFundsDelta, "资金");
                            AppendCapNote(builder, line.NetOutputIntelligence,
                                line.AppliedIntelligenceDelta, "情报");
                        }
                        break;
                    case IndustryLineStatus.Vacant:
                        builder.Append(line.IndustryName).Append("：空缺，本月无产出");
                        break;
                    case IndustryLineStatus.Stalled:
                        builder.Append(line.IndustryName).Append('（').Append(line.AssigneeName)
                            .Append("）：").Append(line.Reason);
                        break;
                    case IndustryLineStatus.InvalidAssignment:
                        builder.Append(line.IndustryName).Append('（').Append(line.AssigneeName)
                            .Append("）：").Append(line.Reason).Append("，本月无产出");
                        break;
                }
            }

            builder.Append("。月末 资金 ").Append(Number(before.Funds)).Append('→')
                .Append(Number(after.Funds))
                .Append("，物资 ").Append(Number(before.Supplies)).Append('→')
                .Append(Number(after.Supplies))
                .Append("，情报 ").Append(Number(before.Intelligence)).Append('→')
                .Append(Number(after.Intelligence)).Append('。');
            return builder.ToString();
        }

        private static void AppendCapNote(StringBuilder builder, int net, int applied, string label)
        {
            if (net == applied)
                return;
            builder.Append("（净产 ").Append(Number(net)).Append('，').Append(label)
                .Append("实际 +").Append(Number(applied)).Append("，已达上限）");
        }

        private static IndustrySettlementResult Rejected(IndustrySettlementOutcome outcome,
            IndustryState state, string message) =>
            new IndustrySettlementResult(outcome, false, message,
                state != null ? state.Clone() : null, null);
    }
}
