using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>One eligible adult family member who may lead the research project.</summary>
    public readonly struct FamilyResearchResearcherOption
    {
        public FamilyResearchResearcherOption(PersonId personId, string displayName)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
    }

    /// <summary>Explicit result code for one research plan command; never a silent failure.</summary>
    public enum FamilyResearchPlanOutcome
    {
        Succeeded = 0,
        Blocked = 1,
        UnknownResearch = 2,
        InvalidResearcher = 3,
        AlreadyKnown = 4,
        InvalidState = 5
    }

    public readonly struct FamilyResearchPlanCommandResult
    {
        public FamilyResearchPlanCommandResult(bool succeeded, FamilyResearchPlanOutcome outcome,
            string message, string researchId, PersonId researcherId)
        {
            Succeeded = succeeded;
            Outcome = outcome;
            Message = message ?? string.Empty;
            ResearchId = researchId ?? string.Empty;
            ResearcherId = researcherId;
        }

        public bool Succeeded { get; }
        public FamilyResearchPlanOutcome Outcome { get; }
        public string Message { get; }
        public string ResearchId { get; }
        public PersonId ResearcherId { get; }

        public static FamilyResearchPlanCommandResult Failure(FamilyResearchPlanOutcome outcome,
            string message, string researchId, PersonId researcherId) =>
            new FamilyResearchPlanCommandResult(false, outcome, message, researchId, researcherId);
    }

    /// <summary>
    /// Read-only SC-021 projection for the uGUI panel. All eligibility and progress
    /// rules come from the Domain state/settlement; this type only formats them.
    /// </summary>
    public sealed class FamilyResearchView
    {
        public bool HasPlan { get; private set; }
        public string ResearchId { get; private set; } = string.Empty;
        public string ResearchName { get; private set; } = string.Empty;
        public string TechniqueId { get; private set; } = string.Empty;
        public string TechniqueName { get; private set; } = string.Empty;
        public int Progress { get; private set; }
        public int RequiredProgress { get; private set; }
        public PersonId ResearcherId { get; private set; }
        public string ResearcherName { get; private set; } = string.Empty;
        public bool IsKnown { get; private set; }
        /// <summary>True when an active plan's researcher is currently ineligible.</summary>
        public bool IsPaused { get; private set; }
        public string PauseReason { get; private set; } = string.Empty;
        public bool CanEdit { get; private set; }
        public bool CanStartPlan { get; private set; }
        public string BlockReason { get; private set; } = string.Empty;
        public string LastMessage { get; private set; } = string.Empty;
        public FamilyResearchMonthReport LastReport { get; private set; }
        public IndustryResources Resources { get; private set; }
        public IReadOnlyList<FamilyResearchResearcherOption> EligibleResearchers { get; private set; } =
            Array.Empty<FamilyResearchResearcherOption>();
        public IReadOnlyList<string> KnownTechniqueNames { get; private set; } = Array.Empty<string>();

        public string ResourceLine =>
            "资金 " + Resources.Funds.ToString(CultureInfo.InvariantCulture) +
            "　物资 " + Resources.Supplies.ToString(CultureInfo.InvariantCulture) +
            "　情报 " + Resources.Intelligence.ToString(CultureInfo.InvariantCulture);

        public string TechniqueLine
        {
            get
            {
                if (string.IsNullOrEmpty(TechniqueName))
                    return "尚无已记录的家族技艺。";
                if (IsKnown)
                    return "家族技艺：" + TechniqueName + "（已记录）";
                return HasPlan
                    ? "研究技艺：" + TechniqueName + "（研究中）"
                    : "可研究技艺：" + TechniqueName;
            }
        }

        public string StatusLine
        {
            get
            {
                if (!HasPlan)
                    return IsKnown ? "当前项目已完成。" : "尚未安排研究计划。";
                string line = "研究者：" + ResearcherName + "（" + Progress + "/" +
                              RequiredProgress + "）";
                // A loaded plan can become paused without a new settlement. Surface that
                // immediately so the player sees why the next month will not advance it.
                if (IsPaused)
                    line += "　研究暂停：" + PauseReason;
                return line;
            }
        }

        public string ProgressLine
        {
            get
            {
                if (IsKnown && !HasPlan)
                    return "已完成：" + TechniqueName + " 已记录为家族知识。";
                if (!HasPlan)
                    return "进度：0/" + (RequiredProgress > 0 ? RequiredProgress : 0);
                return "进度：" + Progress.ToString(CultureInfo.InvariantCulture) + "/" +
                       RequiredProgress.ToString(CultureInfo.InvariantCulture);
            }
        }

        public string CostLine =>
            IsKnown
                ? "该技艺已记录为家族知识，无需再次研究，也不会再产生研究月度费用。"
                : RequiredProgress > 0
                    ? "每月消耗 1 资金、1 物资，推进 1 点进度；完成后写入家族知识，不授予个人掌握。"
                    : "学习一项家族技艺需要持续的月度投入。";

        public string KnownLine
        {
            get
            {
                if (KnownTechniqueNames.Count == 0)
                    return "家族知识：尚无记录。";
                var builder = new StringBuilder(64);
                builder.Append("家族知识：");
                for (int i = 0; i < KnownTechniqueNames.Count; i++)
                {
                    if (i > 0)
                        builder.Append('、');
                    builder.Append(KnownTechniqueNames[i]);
                }
                return builder.ToString();
            }
        }

        public string ReportLine
        {
            get
            {
                if (IsPaused)
                    return "研究暂停：" +
                           (string.IsNullOrWhiteSpace(PauseReason)
                               ? "研究者已不符合条件。"
                               : PauseReason) +
                           "资源与进度保持不变。";
                if (LastReport != null && !string.IsNullOrWhiteSpace(LastReport.Summary))
                    return LastReport.Summary;
                if (IsKnown)
                    return "「" + TechniqueName + "」已完成并记录为家族知识。";
                return "本月尚未结算研究。";
            }
        }

        public static FamilyResearchView Build(FamilyResearchState state, SuccessionState succession,
            IndustryState industry, GameDate date, bool canEdit, string blockReason,
            FamilyResearchMonthReport lastReport, string lastMessage)
        {
            var view = new FamilyResearchView
            {
                CanEdit = canEdit,
                BlockReason = blockReason ?? string.Empty,
                LastMessage = lastMessage ?? string.Empty,
                LastReport = lastReport,
                Resources = industry != null ? industry.Resources : default
            };
            if (state == null)
                return view;

            view.KnownTechniqueNames = BuildKnownNames(state);
            if (state.HasPlan &&
                FamilyResearchCatalog.TryGet(state.ResearchId, out FamilyResearchDefinition definition))
            {
                view.HasPlan = true;
                view.ResearchId = definition.Id;
                view.ResearchName = definition.ProjectDisplayName;
                view.TechniqueId = definition.TechniqueId;
                view.TechniqueName = definition.TechniqueDisplayName;
                view.Progress = state.Progress;
                view.RequiredProgress = definition.RequiredProgress;
                view.ResearcherId = state.ResearcherId;
                view.ResearcherName = succession != null && succession.World != null
                    ? IndustryService.PersonName(succession, state.ResearcherId)
                    : state.ResearcherId.Value;
                view.IsKnown = state.IsKnownTechnique(definition.TechniqueId);
                if (!view.IsKnown)
                {
                    view.IsPaused = !FamilyResearchSettlement.IsEligibleResearcher(succession,
                        state.ResearcherId, date, industry, out string pauseReason);
                    view.PauseReason = view.IsPaused
                        ? (string.IsNullOrWhiteSpace(pauseReason)
                            ? "研究者已不符合条件。"
                            : pauseReason)
                        : string.Empty;
                }
            }
            else
            {
                FamilyResearchCatalog.TryGet(FamilyResearchCatalog.SmokeMixingResearchId,
                    out FamilyResearchDefinition smokeMixing);
                view.TechniqueId = smokeMixing.TechniqueId;
                view.TechniqueName = smokeMixing.TechniqueDisplayName;
                view.RequiredProgress = smokeMixing.RequiredProgress;
                view.IsKnown = state.IsKnownTechnique(smokeMixing.TechniqueId);
            }

            view.CanStartPlan = view.CanEdit && !view.IsKnown;
            view.EligibleResearchers = view.CanStartPlan
                ? BuildResearchers(succession, industry, date)
                : Array.Empty<FamilyResearchResearcherOption>();
            return view;
        }

        private static IReadOnlyList<FamilyResearchResearcherOption> BuildResearchers(
            SuccessionState succession, IndustryState industry, GameDate date)
        {
            var options = new List<FamilyResearchResearcherOption>();
            if (succession == null || succession.World == null)
                return options.AsReadOnly();

            foreach (FamilyMembership membership in succession.World.Family.Members)
            {
                if (!FamilyResearchSettlement.IsEligibleResearcher(succession, membership.PersonId,
                        date, industry, out _))
                    continue;
                string name = IndustryService.PersonName(succession, membership.PersonId);
                options.Add(new FamilyResearchResearcherOption(membership.PersonId, name));
            }
            return options.AsReadOnly();
        }

        private static IReadOnlyList<string> BuildKnownNames(FamilyResearchState state)
        {
            var names = new List<string>();
            foreach (string techniqueId in state.KnownTechniqueIds)
                names.Add(FamilyResearchCatalog.TechniqueDisplayName(techniqueId));
            return names.AsReadOnly();
        }
    }
}
