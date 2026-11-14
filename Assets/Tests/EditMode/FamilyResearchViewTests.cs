using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 4: read-only research projection for the panel.</summary>
    public sealed class FamilyResearchViewTests
    {
        private static readonly GameDate Start = SC021FamilyResearchScenario.StartDate;

        private static FamilyResearchState EmptyState() => FamilyResearchState.CreateInitial(Start);

        private static FamilyResearchView Build(FamilyResearchState state, IndustryState industry = null) =>
            FamilyResearchView.Build(state, SC021FamilyResearchScenario.CreateSuccession(),
                industry ?? SC021FamilyResearchScenario.CreateIndustryState(), Start,
                true, string.Empty, null, string.Empty);

        [Test]
        public void EmptyView_ShowsBudgetCostsAndThreeEligibleResearchers()
        {
            FamilyResearchView view = Build(EmptyState());

            Assert.That(view.HasPlan, Is.False);
            Assert.That(view.ProgressLine, Does.Contain("0/2"));
            Assert.That(view.ProgressLine, Does.Contain("每次月结推进 1 点"),
                "the initial view must show the monthly rate once, before a plan starts");
            Assert.That(view.ResourceLine, Does.Contain("资金 4").And.Contain("物资 2"));
            Assert.That(view.CostLine, Does.Contain("1 资金").And.Contain("1 物资"));
            Assert.That(view.CostLine, Does.Not.Contain("推进"),
                "the rate belongs to the progress line, not the monthly cost line");
            Assert.That(view.EligibleResearchers, Has.Count.EqualTo(3));
            Assert.That(view.CanStartPlan, Is.True);
            Assert.That(view.EligibleResearchers[0].DisplayName, Is.EqualTo(SC021FamilyResearchScenario.HeadName));
            Assert.That(view.KnownLine, Does.Contain("尚无"));
            Assert.That(view.TechniqueLine, Does.Contain("可研究技艺"));
            Assert.That(view.TechniqueLine, Does.Not.Contain("已记录"));
        }

        [Test]
        public void PlannedView_ShowsTechniqueResearcherProgressAndCost()
        {
            FamilyResearchState state = EmptyState();
            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _), Is.True);

            FamilyResearchView view = Build(state);

            Assert.That(view.HasPlan, Is.True);
            Assert.That(view.TechniqueName, Is.EqualTo("烟幕调制术"));
            Assert.That(view.ResearcherName, Is.EqualTo(SC021FamilyResearchScenario.HeadName));
            Assert.That(view.ProgressLine, Does.Contain("0/2"));
            Assert.That(view.StatusLine, Does.Contain(SC021FamilyResearchScenario.HeadName));
            Assert.That(view.CostLine, Does.Contain("不授予个人掌握"));
            Assert.That(view.TechniqueLine, Does.Contain("研究中"));
            Assert.That(view.TechniqueLine, Does.Not.Contain("已记录"));
        }

        [Test]
        public void KnownView_ShowsFamilyKnowledgeWithoutActivePlan()
        {
            FamilyResearchState state = EmptyState();
            state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _);
            state.TryAdvance(2, out _);

            FamilyResearchView view = Build(state);

            Assert.That(view.HasPlan, Is.False);
            Assert.That(view.IsKnown, Is.True);
            Assert.That(view.ProgressLine, Does.Contain("已完成"));
            Assert.That(view.KnownLine, Does.Contain("烟幕调制术"));
            Assert.That(view.TechniqueLine, Does.Contain("烟幕调制术"));
            Assert.That(view.CanStartPlan, Is.False);
            Assert.That(view.EligibleResearchers, Is.Empty,
                "a recorded technique must not offer researcher buttons");
            Assert.That(view.CostLine, Does.Contain("已记录为家族知识"));
            Assert.That(view.CostLine, Does.Not.Contain("每月消耗"));
        }

        [Test]
        public void IndustryAssignee_IsExcludedFromResearcherChoices()
        {
            IndustryState industry = SC021FamilyResearchScenario.CreateIndustryState();
            Assert.That(industry.TrySetAssignee(IndustryCatalog.FarmId,
                SC021FamilyResearchScenario.HeadId, out _), Is.True);

            FamilyResearchView view = Build(EmptyState(), industry);

            Assert.That(view.EligibleResearchers, Has.Count.EqualTo(2));
            foreach (FamilyResearchResearcherOption option in view.EligibleResearchers)
                Assert.That(option.PersonId, Is.Not.EqualTo(SC021FamilyResearchScenario.HeadId));
        }

        [Test]
        public void PausedReport_IsShownWithoutChangingProgress()
        {
            FamilyResearchState state = EmptyState();
            state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _);
            FamilyResearchSettlementResult result = FamilyResearchSettlement.Plan(
                state, default, SC021FamilyResearchScenario.CreateSuccession(), Start);

            // Project the same resource shortfall that produced the paused report so the
            // live pause gate and the settled result agree.
            Assert.That(IndustryResources.TryCreate(0, 2, 0, out IndustryResources resources,
                out string error), Is.True, error);
            IndustryState industry = IndustryState.CreateInitial(Start, resources);
            FamilyResearchView view = FamilyResearchView.Build(state,
                SC021FamilyResearchScenario.CreateSuccession(), industry, Start, true, string.Empty,
                result.Report, result.Message);

            Assert.That(result.Paused, Is.True);
            Assert.That(view.IsPaused, Is.True);
            Assert.That(view.ReportLine, Does.Contain("研究暂停"));
            Assert.That(view.ReportLine, Does.Contain("资金不足 1"));
            Assert.That(view.ReportLine, Does.Contain("进度保持 0/2"));
            Assert.That(view.ReportLine, Does.Contain("本月不推进"));
            Assert.That(view.ProgressLine, Does.Contain("0/2"));
            Assert.That(view.ProgressLine, Does.Not.Contain("每次月结推进"),
                "a paused month must not promise monthly advancement");
        }

        [Test]
        public void SavedPlanWithFormerMemberResearcher_IsPausedWithoutSettlement()
        {
            FamilyResearchState state = EmptyState();
            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId, out _), Is.True);

            // A loaded plan can reference a researcher who is no longer a current family
            // member. No month has been settled for this state yet, so the pause must be
            // derived directly from the shared eligibility rule.
            SuccessionState succession = SC021FamilyResearchScenario.CreateSuccession();
            Assert.That(succession.World.TryRemoveMember(SC021FamilyResearchScenario.SpouseId,
                out _), Is.True);

            FamilyResearchView view = FamilyResearchView.Build(state, succession,
                SC021FamilyResearchScenario.CreateIndustryState(), Start, true, string.Empty,
                null, string.Empty);

            Assert.That(view.HasPlan, Is.True);
            Assert.That(view.IsPaused, Is.True);
            Assert.That(view.PauseReason, Does.Contain("不是当前家族成员"));
            Assert.That(view.StatusLine, Does.Not.Contain("研究暂停"),
                "the pause reason is reported once on the report line");
            Assert.That(view.ReportLine, Does.Contain("研究暂停"));
            Assert.That(view.ReportLine, Does.Contain("不是当前家族成员"));
            Assert.That(view.ReportLine, Does.Contain("进度保持 0/2"));
            Assert.That(view.ReportLine, Does.Contain("本月不推进"));
            Assert.That(view.ProgressLine, Does.Contain("0/2"));
            Assert.That(view.ProgressLine, Does.Not.Contain("每次月结推进"),
                "a paused month must not promise monthly advancement");
            Assert.That(view.Resources.Funds, Is.EqualTo(SC021FamilyResearchScenario.OpeningFunds),
                "projecting a paused plan must not spend funds");
            Assert.That(view.Resources.Supplies, Is.EqualTo(SC021FamilyResearchScenario.OpeningSupplies),
                "projecting a paused plan must not spend supplies");
        }
    }
}
