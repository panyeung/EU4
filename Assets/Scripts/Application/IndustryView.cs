using System;
using System.Collections.Generic;
using System.Globalization;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>Read-only view of one industry slot for the uGUI panel.</summary>
    public readonly struct IndustrySlotView
    {
        public IndustrySlotView(string industryId, string displayName, PersonId assigneeId,
            string assigneeName, int workUnitsRequired, int workUnitsRemaining,
            IndustryLineStatus status, string statusReason)
        {
            IndustryId = industryId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AssigneeId = assigneeId;
            AssigneeName = assigneeName ?? string.Empty;
            WorkUnitsRequired = workUnitsRequired;
            WorkUnitsRemaining = workUnitsRemaining;
            Status = status;
            StatusReason = statusReason ?? string.Empty;
        }

        public string IndustryId { get; }
        public string DisplayName { get; }
        public PersonId AssigneeId { get; }
        public string AssigneeName { get; }
        public int WorkUnitsRequired { get; }
        public int WorkUnitsRemaining { get; }
        public IndustryLineStatus Status { get; }
        public string StatusReason { get; }
    }

    /// <summary>
    /// Immutable snapshot used by the industry panel. It derives the actionable slot
    /// status from the live state so a shortage remains visible after a load, when
    /// the transient month report is no longer available.
    /// </summary>
    public sealed class IndustryView
    {
        public IndustryView(IndustryResources resources, int lastSettledMonthIndex,
            IReadOnlyList<IndustrySlotView> slots, bool canEditAssignments, string blockReason,
            IndustryMonthReport lastReport, string lastChronicleSummary, string lastMessage,
            MarketState market, MarketMonthReport lastMarketReport)
        {
            Resources = resources;
            LastSettledMonthIndex = lastSettledMonthIndex;
            Slots = slots ?? Array.Empty<IndustrySlotView>();
            CanEditAssignments = canEditAssignments;
            BlockReason = blockReason ?? string.Empty;
            LastReport = lastReport;
            LastChronicleSummary = lastChronicleSummary ?? string.Empty;
            LastMessage = lastMessage ?? string.Empty;
            Market = market;
            LastMarketReport = lastMarketReport;
        }

        public IndustryResources Resources { get; }
        public int LastSettledMonthIndex { get; }
        public IReadOnlyList<IndustrySlotView> Slots { get; }
        public bool CanEditAssignments { get; }
        public string BlockReason { get; }
        public IndustryMonthReport LastReport { get; }
        public string LastChronicleSummary { get; }

        /// <summary>Persisted M08B market state; the only source that survives a load.</summary>
        public MarketState Market { get; }

        /// <summary>Transient report of the last committed market month; null after a load.</summary>
        public MarketMonthReport LastMarketReport { get; }

        /// <summary>Last visible action feedback (rejected assignment, cleanup, settlement).</summary>
        public string LastMessage { get; }

        public string ResourceLine =>
            "资金 " + Resources.Funds.ToString(CultureInfo.InvariantCulture) +
            "　粮食 " + FoodInventory.ToString(CultureInfo.InvariantCulture) +
            "（粮价 " + FoodPriceIndexPermille.ToString(CultureInfo.InvariantCulture) + "‰）" +
            "　物资 " + Resources.Supplies.ToString(CultureInfo.InvariantCulture) +
            "（物资价 " + MaterialsPriceIndexPermille.ToString(CultureInfo.InvariantCulture) + "‰）" +
            "　情报 " + Resources.Intelligence.ToString(CultureInfo.InvariantCulture);

        /// <summary>Committed market month summary, or empty when only the persisted state is known.</summary>
        public string MarketSummary => LastMarketReport != null ? LastMarketReport.Summary : string.Empty;

        /// <summary>
        /// Consecutive food-shortage status derived from the persisted market state, so it stays
        /// visible after a load clears <see cref="LastMarketReport"/>. A zero count is shown too,
        /// because "no consecutive shortage" is a real state that must not vanish from the panel.
        /// </summary>
        public string ShortageLine =>
            Market != null
                ? "粮食连续短缺 " +
                  Market.ConsecutiveFoodShortageMonths.ToString(CultureInfo.InvariantCulture) + " 个月。"
                : string.Empty;

        private int FoodInventory => Market != null ? Market.FoodInventory : 0;

        private int FoodPriceIndexPermille => Market != null
            ? Market.FoodPriceIndexPermille
            : MarketState.BaselinePriceIndexPermille;

        private int MaterialsPriceIndexPermille => Market != null
            ? Market.MaterialsPriceIndexPermille
            : MarketState.BaselinePriceIndexPermille;

        public IndustrySlotView Slot(string industryId)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (string.Equals(Slots[i].IndustryId, industryId, StringComparison.Ordinal))
                    return Slots[i];
            return default;
        }

        public static IndustryView Build(IndustryState state, SuccessionState succession,
            GameDate date, IndustryMonthReport lastReport, string lastChronicleSummary,
            bool canEdit, string blockReason, string lastMessage, MarketState market,
            MarketMonthReport lastMarketReport)
        {
            var slots = new List<IndustrySlotView>(IndustryCatalog.All.Count);
            if (state != null)
            {
                // The preview runs the same pure planner the month turn runs, so a slot
                // cannot promise Operated while settlement stalls on material cost.
                IndustryMonthReport predicted = PredictMonth(state, succession, date, market, lastReport);
                foreach (IndustryDefinition definition in IndustryCatalog.All)
                {
                    state.TryGetAssignee(definition.Id, out PersonId assignee);
                    string name = assignee.IsEmpty
                        ? string.Empty
                        : IndustryService.PersonName(succession, assignee);
                    IndustryLineStatus status;
                    string reason;
                    if (assignee.IsEmpty)
                    {
                        status = IndustryLineStatus.Vacant;
                        reason = "空缺：无人负责，本月无产出";
                    }
                    else if (TryGetLine(predicted, definition.Id, out IndustryLineResult predictedLine))
                    {
                        status = predictedLine.Status;
                        reason = predictedLine.Reason;
                    }
                    else if (!IndustryService.CanWork(succession, assignee, date, out string invalidReason))
                    {
                        status = IndustryLineStatus.InvalidAssignment;
                        reason = "负责人失效：" + invalidReason;
                    }
                    else if (!definition.MaintainsFromOutput &&
                             (state.Resources.Funds < definition.UpkeepFunds ||
                              state.Resources.Supplies < definition.UpkeepSupplies))
                    {
                        status = IndustryLineStatus.Stalled;
                        reason = "资源不足：缺 " +
                                 Math.Max(0, definition.UpkeepFunds - state.Resources.Funds)
                                     .ToString(CultureInfo.InvariantCulture) + " 资金、" +
                                 Math.Max(0, definition.UpkeepSupplies - state.Resources.Supplies)
                                     .ToString(CultureInfo.InvariantCulture) + " 物资，本月停产";
                    }
                    else
                    {
                        status = IndustryLineStatus.Operated;
                        reason = "运作中";
                    }

                    int remaining = assignee.IsEmpty
                        ? IndustryService.MonthlyWorkUnits
                        : IndustryService.RemainingWorkUnits(succession, state, assignee, date);
                    slots.Add(new IndustrySlotView(definition.Id, definition.DisplayName, assignee, name,
                        definition.WorkUnitsRequired, remaining, status, reason));
                }
            }

            return new IndustryView(state != null ? state.Resources : default,
                state != null ? state.LastSettledMonthIndex : 0, slots.AsReadOnly(),
                canEdit, blockReason, lastReport, lastChronicleSummary, lastMessage,
                market, lastMarketReport);
        }

        /// <summary>
        /// Plans the month the next month turn closes, so the slot preview and the
        /// settlement cannot disagree on Operated versus Stalled. A month that is
        /// already settled shows its committed report; a ledger the planner would
        /// reject returns null and the caller keeps the read-only fallback presentation.
        /// </summary>
        private static IndustryMonthReport PredictMonth(IndustryState state,
            SuccessionState succession, GameDate date, MarketState market,
            IndustryMonthReport lastReport)
        {
            int currentIndex = IndustryCalendar.MonthIndex(date);
            if (state.LastSettledMonthIndex == currentIndex)
                return lastReport != null && lastReport.Year == date.Year &&
                       lastReport.Month == date.Month
                    ? lastReport
                    : null;
            if (state.LastSettledMonthIndex != currentIndex - 1)
                return null;

            int materialsPriceIndex = market != null
                ? market.MaterialsPriceIndexPermille
                : MarketState.BaselinePriceIndexPermille;
            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, date,
                materialsPriceIndex);
            return result.Succeeded ? result.Report : null;
        }

        private static bool TryGetLine(IndustryMonthReport report, string industryId,
            out IndustryLineResult line)
        {
            if (report != null)
            {
                for (int i = 0; i < report.Lines.Count; i++)
                {
                    if (string.Equals(report.Lines[i].IndustryId, industryId,
                            StringComparison.Ordinal))
                    {
                        line = report.Lines[i];
                        return true;
                    }
                }
            }

            line = default;
            return false;
        }
    }
}
