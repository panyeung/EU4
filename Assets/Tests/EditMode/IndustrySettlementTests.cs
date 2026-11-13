using System.Globalization;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 2: deterministic, pure monthly settlement.</summary>
    public sealed class IndustrySettlementTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);
        private static readonly PersonId Harunobu = M2FixedScenario.HarunobuId;
        private static readonly PersonId Chie = M2FixedScenario.ChieId;

        private static SuccessionState Succession() => M2FixedScenario.CreateSuccession();

        private static IndustryState State(int funds, int supplies, int intelligence)
        {
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(IndustryResources.TryCreate(funds, supplies, intelligence,
                out IndustryResources resources, out string error), Is.True, error);
            state.TrySetResources(resources, out _);
            return state;
        }

        private static IndustrySettlementResult Plan(IndustryState state, SuccessionState succession)
        {
            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);
            Assert.That(result.Succeeded, Is.True, result.Message);
            return result;
        }

        [Test]
        public void Settle_BothStaffed_ProducesExpectedNetAndReport()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Outcome, Is.EqualTo(IndustrySettlementOutcome.Succeeded));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(32));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(13));
            Assert.That(result.NextState.Resources.Intelligence, Is.EqualTo(1));
            Assert.That(result.NextState.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(Start)));
            Assert.That(result.Report.AnyStaffed, Is.True);
            Assert.That(result.Report.Changed, Is.True);
            Assert.That(result.Report.HasShortage, Is.False);
            Assert.That(result.Report.Lines.Count, Is.EqualTo(2));
            Assert.That(result.Report.Lines[0].IndustryId, Is.EqualTo(IndustryCatalog.FarmId));
            Assert.That(result.Report.Lines[0].NetOutputFunds, Is.EqualTo(1));
            Assert.That(result.Report.Lines[0].GrossOutputFood, Is.EqualTo(4));
            Assert.That(result.Report.Lines[0].RequiredSupplies, Is.EqualTo(1),
                "the farm still requires its one self-retained input unit");
            Assert.That(result.Report.Lines[0].NetOutputSupplies, Is.EqualTo(3));
            Assert.That(result.Report.Lines[1].IndustryId, Is.EqualTo(IndustryCatalog.TradingHouseId));
            Assert.That(result.Report.Lines[1].NetOutputFunds, Is.EqualTo(1));
            Assert.That(result.Report.Lines[1].MaterialsCostFunds, Is.EqualTo(2));
            Assert.That(result.Report.Lines[1].RequiredSupplies, Is.EqualTo(2));
            Assert.That(result.Report.Lines[1].NetOutputSupplies, Is.EqualTo(-2));
            Assert.That(result.Report.Lines[1].NetOutputIntelligence, Is.EqualTo(1));
            Assert.That(result.Report.Summary, Does.Contain("30"));
            Assert.That(result.Report.Summary, Does.Contain("32"));
            Assert.That(result.Report.Summary, Does.Contain("13"));
        }

        [Test]
        public void Settle_FarmRetainsOneSupplyFromGrossHarvest()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = Plan(state, succession);
            IndustryLineResult farm = result.Report.Lines[0];

            Assert.That(farm.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(farm.GrossOutputSupplies, Is.EqualTo(4));
            Assert.That(farm.UpkeepSupplies, Is.EqualTo(1));
            Assert.That(farm.MaintenanceRetainedFromOutput, Is.True);
            Assert.That(farm.NetOutputSupplies, Is.EqualTo(3));
            Assert.That(farm.MaterialsCostFunds, Is.EqualTo(1),
                "the self-retained supply is priced at the 1000‰ base index");
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(15));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(31));
        }

        [Test]
        public void Settle_FarmWorksFromZeroStartingResources()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(0, 0, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(1),
                "the +2 harvest covers the 1-fund material cost and leaves 1");
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(3));
            Assert.That(result.NextState.Resources.Intelligence, Is.Zero);
            Assert.That(result.Report.HasShortage, Is.False);
            Assert.That(result.Report.Lines[0].GrossOutputFood, Is.EqualTo(4));
        }

        [Test]
        public void Settle_TradingChecksMaintenanceBeforeOutput()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(5, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = Plan(state, succession);
            IndustryLineResult trading = result.Report.Lines[1];

            Assert.That(trading.Status, Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(trading.Reason, Does.Contain("3").And.Contain("资金"));
            Assert.That(trading.Reason, Does.Contain("材料成本 2"));
            Assert.That(trading.RequiredSupplies, Is.EqualTo(2),
                "a validly appointed stalled line still demands its input");
            Assert.That(trading.UpkeepSupplies, Is.Zero, "actual draw stays distinct from requirement");
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(5));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(12));
            Assert.That(result.NextState.Resources.Intelligence, Is.Zero);
            Assert.That(result.Report.HasShortage, Is.True);
            Assert.That(result.NextState.TryGetAssignee(IndustryCatalog.TradingHouseId, out PersonId assigned), Is.True);
            Assert.That(assigned, Is.EqualTo(Chie), "a stalled industry keeps its appointment");
        }

        [Test]
        public void Settle_Shortage_StallsWithoutNegativeResources()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(100, 1, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Report.Lines[1].Status, Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(100));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(1));
            Assert.That(result.NextState.Resources.Funds, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.NextState.Resources.Supplies, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Settle_VacantIndustry_NoUpkeepNoOutput()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Report.AnyStaffed, Is.False);
            Assert.That(result.Report.Changed, Is.False);
            Assert.That(result.Report.HasShortage, Is.False);
            Assert.That(result.Report.Lines[0].Status, Is.EqualTo(IndustryLineStatus.Vacant));
            Assert.That(result.Report.Lines[1].Status, Is.EqualTo(IndustryLineStatus.Vacant));
            Assert.That(result.NextState.Resources, Is.EqualTo(state.Resources));
            Assert.That(result.Report.Summary, Is.EqualTo("本月无产业运作。"));
        }

        [Test]
        public void Settle_FarmCreditsBeforeTrading()
        {
            SuccessionState succession = Succession();
            // Trading alone needs 8 funds (6 upkeep + 2 material cost); the farm's same-month
            // net +1 credit makes that affordable from an opening balance of 7.
            IndustryState state = State(7, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Report.Lines[1].Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(9));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(13));
            Assert.That(result.NextState.Resources.Intelligence, Is.EqualTo(1));
        }

        [Test]
        public void Settle_InvalidAssignee_VacatesAndProducesNothing()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            succession.World.TrySetLife(Harunobu,
                new SuccessionPersonLife(M2FixedScenario.HarunobuBirthYear, true, true), out _);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Report.Vacated.Count, Is.EqualTo(1));
            Assert.That(result.Report.Lines[0].Status, Is.EqualTo(IndustryLineStatus.InvalidAssignment));
            Assert.That(result.Report.Lines[0].AssigneeName, Is.EqualTo("明智 晴信"));
            Assert.That(result.Report.Lines[0].Reason, Does.Contain("退休"));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(30));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(12));
            Assert.That(result.NextState.TryGetAssignee(IndustryCatalog.FarmId, out PersonId assignee), Is.True);
            Assert.That(assignee.IsEmpty, Is.True);
        }

        [Test]
        public void Settle_IsDeterministicAndDoesNotConsumeRandom()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult first = Plan(state, succession);
            IndustrySettlementResult second = Plan(state.Clone(), Succession());

            Assert.That(second.NextState.Resources, Is.EqualTo(first.NextState.Resources));
            Assert.That(second.Report.Summary, Is.EqualTo(first.Report.Summary));
            Assert.That(state.Resources.Funds, Is.EqualTo(30), "Plan must not mutate its input");
        }

        [Test]
        public void Settle_AlreadySettled_IsNoOp()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            state.TrySetLastSettledMonthIndex(IndustryCalendar.MonthIndex(Start), out _);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(IndustrySettlementOutcome.AlreadySettled));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(30));
        }

        [Test]
        public void Settle_Gap_IsRejected()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            state.TrySetLastSettledMonthIndex(IndustryCalendar.MonthIndex(Start) - 2, out _);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(IndustrySettlementOutcome.SettlementGap));
        }

        [Test]
        public void Settle_FutureLastSettled_IsInvalidState()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            state.TrySetLastSettledMonthIndex(IndustryCalendar.MonthIndex(Start) + 1, out _);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(IndustrySettlementOutcome.InvalidState));
        }

        [Test]
        public void Settle_FarmAtCap_NetBeforeClampKeepsFullWarehouse()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 999999, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = Plan(state, succession);
            IndustryLineResult farm = result.Report.Lines[0];

            Assert.That(farm.NetOutputSupplies, Is.EqualTo(3));
            Assert.That(farm.AppliedSuppliesDelta, Is.Zero);
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(999999),
                "gross-then-upkeep must never subtract one from a full warehouse");
            Assert.That(result.Report.Summary, Does.Contain("已达上限"));
        }

        [Test]
        public void Settle_ClampsAtResourceCap()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(999998, 12, 999999);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(999999));
            Assert.That(result.NextState.Resources.Intelligence, Is.EqualTo(999999));
            Assert.That(result.Report.Lines[0].AppliedFundsDelta, Is.EqualTo(1));
            Assert.That(result.Report.Lines[1].NetOutputFunds, Is.EqualTo(1));
            Assert.That(result.Report.Lines[1].AppliedFundsDelta, Is.Zero,
                "the trading house pays upkeep then refills to the cap, so its actual net change is zero");
        }

        [Test]
        public void Settle_SummaryUsesInvariantNumbers()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Report.Summary, Does.Contain(
                (31 - 3).ToString(CultureInfo.InvariantCulture)).Or.Contain("31"));
            Assert.That(result.Report.Summary, Does.Not.Contain("NaN"));
        }

        [Test]
        public void Settle_MaterialCostUsesMonthIndex()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(9, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = PlanWithIndex(state, succession, 1500);
            IndustryLineResult trading = result.Report.Lines[1];

            Assert.That(trading.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(trading.MaterialsCostFunds, Is.EqualTo(3),
                "ceil(2 units * 1500 / 1000)");
            Assert.That(trading.NetOutputFunds, Is.Zero);
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(9));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(10));
            Assert.That(result.NextState.Resources.Intelligence, Is.EqualTo(1));
        }

        [Test]
        public void Settle_MaterialCostShortage_StallsAtomically()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(8, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult result = PlanWithIndex(state, succession, 1500);
            IndustryLineResult trading = result.Report.Lines[1];

            Assert.That(trading.Status, Is.EqualTo(IndustryLineStatus.Stalled),
                "6 upkeep + 3 material cost needs 9 funds");
            Assert.That(trading.Reason, Does.Contain("1").And.Contain("材料成本 3"));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(8));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(12));
            Assert.That(result.NextState.Resources.Intelligence, Is.Zero);
            Assert.That(result.Report.HasShortage, Is.True);
        }

        [Test]
        public void Settle_HighMaterialIndex_StallsFarmWithoutFoodOrPartialCharge()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(0, 12, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = PlanWithIndex(state, succession, 5000);
            IndustryLineResult farm = result.Report.Lines[0];

            Assert.That(farm.Status, Is.EqualTo(IndustryLineStatus.Stalled),
                "ceil(1 * 5000 / 1000) = 5 funds exceeds the 2-fund harvest");
            Assert.That(farm.GrossOutputFood, Is.Zero);
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(0));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(12));
            Assert.That(result.Report.HasShortage, Is.True);
        }

        [Test]
        public void Settle_FarmMaterialCostAtNonBaseIndexStillProducesFood()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(0, 0, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = PlanWithIndex(state, succession, 2000);
            IndustryLineResult farm = result.Report.Lines[0];

            Assert.That(farm.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(farm.MaterialsCostFunds, Is.EqualTo(2));
            Assert.That(farm.NetOutputFunds, Is.Zero);
            Assert.That(farm.GrossOutputFood, Is.EqualTo(4));
            Assert.That(result.NextState.Resources.Funds, Is.Zero);
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(3));
        }

        [Test]
        public void Settle_VacantFarmProducesNoFood()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);

            IndustrySettlementResult result = Plan(state, succession);

            Assert.That(result.Report.Lines[0].Status, Is.EqualTo(IndustryLineStatus.Vacant));
            Assert.That(result.Report.Lines[0].GrossOutputFood, Is.Zero);
            Assert.That(result.Report.Lines[0].RequiredSupplies, Is.Zero);
        }

        [Test]
        public void Settle_InvalidMaterialIndex_IsInvalidState()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12, 0);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start, 99);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(IndustrySettlementOutcome.InvalidState));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(30));
        }

        private static IndustrySettlementResult PlanWithIndex(IndustryState state,
            SuccessionState succession, int materialsPriceIndexPermille)
        {
            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start,
                materialsPriceIndexPermille);
            Assert.That(result.Succeeded, Is.True, result.Message);
            return result;
        }
    }
}
