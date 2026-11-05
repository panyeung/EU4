using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 1: deterministic monthly cost, pause and completion rules.</summary>
    public sealed class FamilyResearchSettlementTests
    {
        private static readonly GameDate Start = new GameDate(1560, 1, 1);

        private static FamilyResearchState StateWithPlan(PersonId researcher)
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);
            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                researcher, out string error), Is.True, error);
            return state;
        }

        private static IndustryResources Resources(int funds, int supplies)
        {
            Assert.That(IndustryResources.TryCreate(funds, supplies, 0,
                out IndustryResources resources, out string error), Is.True, error);
            return resources;
        }

        private static SuccessionState Succession() => SC021FamilyResearchScenario.CreateSuccession();

        [Test]
        public void SuccessfulMonth_SpendsExactlyOneFundAndSupplyAndAddsOneProgress()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);

            FamilyResearchSettlementResult result = FamilyResearchSettlement.Plan(
                state, Resources(2, 2), succession, Start);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Outcome, Is.EqualTo(FamilyResearchOutcome.Succeeded));
            Assert.That(result.BeforeProgress, Is.Zero);
            Assert.That(result.AfterProgress, Is.EqualTo(1));
            Assert.That(result.NextResources.Funds, Is.EqualTo(1));
            Assert.That(result.NextResources.Supplies, Is.EqualTo(1));
            Assert.That(result.NextState.HasPlan, Is.True);
            Assert.That(result.NextState.Progress, Is.EqualTo(1));
            Assert.That(result.Report.Completed, Is.False);
            Assert.That(result.Report.Changed, Is.True);
        }

        [Test]
        public void SecondSuccessfulMonth_CompletesAndRecordsKnowledgeOnce()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);
            FamilyResearchSettlementResult first = FamilyResearchSettlement.Plan(
                state, Resources(2, 2), succession, Start);

            FamilyResearchSettlementResult second = FamilyResearchSettlement.Plan(
                first.NextState, first.NextResources, succession, new GameDate(1560, 2, 1));

            Assert.That(second.Completed, Is.True);
            Assert.That(second.Report.Completed, Is.True);
            Assert.That(second.NextState.HasPlan, Is.False);
            Assert.That(second.NextState.KnownTechniqueIds, Has.Count.EqualTo(1));
            Assert.That(second.NextState.KnownTechniqueIds[0],
                Is.EqualTo(FamilyResearchCatalog.SmokeMixingTechniqueId));
            Assert.That(second.NextResources.Funds, Is.Zero);
            Assert.That(second.NextResources.Supplies, Is.Zero);
            Assert.That(second.Report.BeforeProgress, Is.EqualTo(1));
            Assert.That(second.Report.AfterProgress, Is.EqualTo(2));
        }

        [Test]
        public void NoPlan_StillAdvancesTheSettledMonthWithoutCost()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);

            FamilyResearchSettlementResult result = FamilyResearchSettlement.Plan(
                state, Resources(5, 5), Succession(), Start);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Outcome, Is.EqualTo(FamilyResearchOutcome.NoPlan));
            Assert.That(result.NextResources, Is.EqualTo(Resources(5, 5)));
            Assert.That(result.NextState.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(Start)));
        }

        [Test]
        public void FundsShortage_PausesWithoutSpendingOrProgress()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);

            FamilyResearchSettlementResult result = FamilyResearchSettlement.Plan(
                state, Resources(0, 2), succession, Start);

            Assert.That(result.Outcome, Is.EqualTo(FamilyResearchOutcome.ResourceShortage));
            Assert.That(result.Paused, Is.True);
            Assert.That(result.NextResources, Is.EqualTo(Resources(0, 2)));
            Assert.That(result.NextState.Progress, Is.Zero);
            Assert.That(result.Report.Summary, Does.Contain("资金不足 1"));
            Assert.That(result.Report.Summary, Does.Not.Contain("物资不足"));
        }

        [Test]
        public void SuppliesShortage_PausesWithoutSpendingOrProgress()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);

            FamilyResearchSettlementResult result = FamilyResearchSettlement.Plan(
                state, Resources(2, 0), succession, Start);

            Assert.That(result.Outcome, Is.EqualTo(FamilyResearchOutcome.ResourceShortage));
            Assert.That(result.NextResources, Is.EqualTo(Resources(2, 0)));
            Assert.That(result.NextState.Progress, Is.Zero);
            Assert.That(result.Report.Summary, Does.Contain("物资不足 1"));
            Assert.That(result.Report.Summary, Does.Not.Contain("资金不足"));
        }

        [Test]
        public void DeadRetiredMinorAndNonMemberResearchers_PauseWithoutProgress()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);
            succession.World.TrySetLife(SC021FamilyResearchScenario.HeadId,
                new SuccessionPersonLife(SC021FamilyResearchScenario.HeadBirthYear, false, false), out _);

            FamilyResearchSettlementResult dead = FamilyResearchSettlement.Plan(
                state, Resources(2, 2), succession, Start);
            Assert.That(dead.Outcome, Is.EqualTo(FamilyResearchOutcome.InvalidResearcher));
            Assert.That(dead.NextResources, Is.EqualTo(Resources(2, 2)));
            Assert.That(dead.NextState.Progress, Is.Zero);

            SuccessionState retired = Succession();
            FamilyResearchState retiredState = StateWithPlan(SC021FamilyResearchScenario.SpouseId);
            retired.World.TrySetLife(SC021FamilyResearchScenario.SpouseId,
                new SuccessionPersonLife(SC021FamilyResearchScenario.SpouseBirthYear, true, true), out _);
            Assert.That(FamilyResearchSettlement.Plan(retiredState, Resources(2, 2), retired, Start)
                .Outcome, Is.EqualTo(FamilyResearchOutcome.InvalidResearcher));

            SuccessionState minor = Succession();
            FamilyResearchState minorState = StateWithPlan(SC021FamilyResearchScenario.HeirId);
            minor.World.TrySetLife(SC021FamilyResearchScenario.HeirId,
                new SuccessionPersonLife(1555, true, false), out _);
            Assert.That(FamilyResearchSettlement.Plan(minorState, Resources(2, 2), minor, Start)
                .Outcome, Is.EqualTo(FamilyResearchOutcome.InvalidResearcher));

            SuccessionState nonMember = Succession();
            FamilyResearchState nonMemberState = StateWithPlan(new PersonId("person.not-in-family"));
            Assert.That(FamilyResearchSettlement.Plan(nonMemberState, Resources(2, 2), nonMember, Start)
                .Outcome, Is.EqualTo(FamilyResearchOutcome.InvalidResearcher));
        }

        [Test]
        public void IndustryAssignee_PausesWithoutSpendingOrProgress()
        {
            SuccessionState succession = Succession();
            IndustryState industry = SC021FamilyResearchScenario.CreateIndustryState();
            Assert.That(industry.TrySetAssignee(IndustryCatalog.FarmId,
                SC021FamilyResearchScenario.HeadId, out _), Is.True);
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);

            FamilyResearchSettlementResult result = FamilyResearchSettlement.Plan(
                state, Resources(2, 2), succession, Start, industry);

            Assert.That(result.Outcome, Is.EqualTo(FamilyResearchOutcome.InvalidResearcher));
            Assert.That(result.Report.Summary, Does.Contain("产业"));
            Assert.That(result.NextResources, Is.EqualTo(Resources(2, 2)));
            Assert.That(result.NextState.Progress, Is.Zero);
        }

        [Test]
        public void AlreadySettledGapAndFutureMonths_AreRejected()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);
            state.TrySetLastSettledMonthIndex(IndustryCalendar.MonthIndex(Start), out _);
            Assert.That(FamilyResearchSettlement.Plan(state, Resources(2, 2), succession, Start)
                .Outcome, Is.EqualTo(FamilyResearchOutcome.AlreadySettled));

            FamilyResearchState gap = StateWithPlan(SC021FamilyResearchScenario.HeadId);
            gap.TrySetLastSettledMonthIndex(IndustryCalendar.MonthIndex(Start) - 2, out _);
            Assert.That(FamilyResearchSettlement.Plan(gap, Resources(2, 2), succession, Start)
                .Outcome, Is.EqualTo(FamilyResearchOutcome.SettlementGap));

            FamilyResearchState future = StateWithPlan(SC021FamilyResearchScenario.HeadId);
            future.TrySetLastSettledMonthIndex(IndustryCalendar.MonthIndex(Start) + 1, out _);
            Assert.That(FamilyResearchSettlement.Plan(future, Resources(2, 2), succession, Start)
                .Outcome, Is.EqualTo(FamilyResearchOutcome.InvalidState));
        }

        [Test]
        public void Plan_DoesNotMutateInputsAndIsDeterministic()
        {
            SuccessionState succession = Succession();
            FamilyResearchState state = StateWithPlan(SC021FamilyResearchScenario.HeadId);
            IndustryResources resources = Resources(2, 2);

            FamilyResearchSettlementResult first = FamilyResearchSettlement.Plan(
                state, resources, succession, Start);
            FamilyResearchSettlementResult second = FamilyResearchSettlement.Plan(
                state.Clone(), resources, Succession(), Start);

            Assert.That(state.Progress, Is.Zero);
            Assert.That(state.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(Start) - 1));
            Assert.That(second.NextResources, Is.EqualTo(first.NextResources));
            Assert.That(second.NextState.Progress, Is.EqualTo(first.NextState.Progress));
        }
    }
}
