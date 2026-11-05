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
            IndustryMonthReport lastReport, string lastChronicleSummary, string lastMessage)
        {
            Resources = resources;
            LastSettledMonthIndex = lastSettledMonthIndex;
            Slots = slots ?? Array.Empty<IndustrySlotView>();
            CanEditAssignments = canEditAssignments;
            BlockReason = blockReason ?? string.Empty;
            LastReport = lastReport;
            LastChronicleSummary = lastChronicleSummary ?? string.Empty;
            LastMessage = lastMessage ?? string.Empty;
        }

        public IndustryResources Resources { get; }
        public int LastSettledMonthIndex { get; }
        public IReadOnlyList<IndustrySlotView> Slots { get; }
        public bool CanEditAssignments { get; }
        public string BlockReason { get; }
        public IndustryMonthReport LastReport { get; }
        public string LastChronicleSummary { get; }

        /// <summary>Last visible action feedback (rejected assignment, cleanup, settlement).</summary>
        public string LastMessage { get; }

        public string ResourceLine =>
            "资金 " + Resources.Funds.ToString(CultureInfo.InvariantCulture) +
            "　物资 " + Resources.Supplies.ToString(CultureInfo.InvariantCulture) +
            "　情报 " + Resources.Intelligence.ToString(CultureInfo.InvariantCulture);

        public IndustrySlotView Slot(string industryId)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (string.Equals(Slots[i].IndustryId, industryId, StringComparison.Ordinal))
                    return Slots[i];
            return default;
        }

        public static IndustryView Build(IndustryState state, SuccessionState succession,
            GameDate date, IndustryMonthReport lastReport, string lastChronicleSummary,
            bool canEdit, string blockReason, string lastMessage)
        {
            var slots = new List<IndustrySlotView>(IndustryCatalog.All.Count);
            if (state != null)
            {
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
                canEdit, blockReason, lastReport, lastChronicleSummary, lastMessage);
        }
    }
}
