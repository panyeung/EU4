using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M08B Task 1: pure, deterministic market rules. Household food demand is settled at the
    /// previous month's index, prices update from this month's demand and supply in stable
    /// good order, and no input state is mutated.
    /// </summary>
    public sealed class MarketSettlementTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);
        private static readonly int StartMonthIndex = IndustryCalendar.MonthIndex(Start);
        private static readonly PersonId Harunobu = M2FixedScenario.HarunobuId;
        private static readonly PersonId Chie = M2FixedScenario.ChieId;

        [Test]
        public void Plan_BalancedDemandAndSupply_KeepsPricesAndMoney()
        {
            MarketSettlementResult result = Settle(
                Market(2), 10, 12, Household(6),
                0, 12, 0);

            Assert.That(result.Report.HouseholdMembers, Is.EqualTo(6));
            Assert.That(result.Report.FoodDemand, Is.EqualTo(2));
            Assert.That(result.Report.FoodPurchased, Is.EqualTo(2));
            Assert.That(result.Report.FoodUnmet, Is.Zero);
            Assert.That(result.Report.FoodBillFunds, Is.EqualTo(2));
            Assert.That(result.Report.FoodInventoryAfter, Is.Zero);
            Assert.That(result.NextResources.Funds, Is.EqualTo(8));
            Assert.That(result.NextResources.Supplies, Is.Zero,
                "the balanced materials month consumed its whole stock");
            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(1000));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(1000));
            Assert.That(result.Report.HasShortage, Is.False);
            Assert.That(result.Report.ConsecutiveFoodShortageMonths, Is.Zero);
            Assert.That(result.Report.Summary, Does.Contain("需粮 2"));
        }

        [Test]
        public void Plan_ZeroFoodSupply_RecordsShortageAndRaisesPriceToMonthlyCap()
        {
            MarketSettlementResult result = Settle(
                Market(0), 100, 12, Household(6),
                0, 12, 0);

            Assert.That(result.Report.FoodAvailable, Is.Zero);
            Assert.That(result.Report.FoodPurchased, Is.Zero);
            Assert.That(result.Report.FoodUnmet, Is.EqualTo(2));
            Assert.That(result.Report.FoodBillFunds, Is.Zero);
            Assert.That(result.NextMarket.FoodInventory, Is.Zero);
            Assert.That(result.NextResources.Funds, Is.EqualTo(100));
            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(1100),
                "100% shortage moves +250‰ pressure but the monthly cap is +100‰");
            Assert.That(result.NextMarket.ConsecutiveFoodShortageMonths, Is.EqualTo(1));
            Assert.That(result.Report.HasShortage, Is.True);
        }

        [Test]
        public void Plan_ZeroDemand_ChargesNothingAndLowersPrice()
        {
            MarketSettlementResult result = Settle(
                Market(10, shortageMonths: 2), 5, 12, Household(0),
                0, 12, 0);

            Assert.That(result.Report.HouseholdMembers, Is.Zero);
            Assert.That(result.Report.FoodDemand, Is.Zero);
            Assert.That(result.Report.FoodPurchased, Is.Zero);
            Assert.That(result.Report.FoodBillFunds, Is.Zero);
            Assert.That(result.NextResources.Funds, Is.EqualTo(5));
            Assert.That(result.NextMarket.FoodInventory, Is.EqualTo(10));
            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(900));
            Assert.That(result.NextMarket.ConsecutiveFoodShortageMonths, Is.Zero,
                "a month with no unmet demand resets the streak");
        }

        [Test]
        public void Plan_Shortage_PurchasesAvailableAndStartsStreak()
        {
            MarketSettlementResult result = Settle(
                Market(1), 100, 12, Household(6),
                0, 12, 0);

            Assert.That(result.Report.FoodPurchased, Is.EqualTo(1));
            Assert.That(result.Report.FoodUnmet, Is.EqualTo(1));
            Assert.That(result.Report.FoodBillFunds, Is.EqualTo(1));
            Assert.That(result.NextMarket.FoodInventory, Is.Zero);
            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(1100));
            Assert.That(result.NextMarket.ConsecutiveFoodShortageMonths, Is.EqualTo(1));
        }

        [Test]
        public void Plan_Surplus_PurchasesDemandAndLowersPrice()
        {
            MarketSettlementResult result = Settle(
                Market(20), 100, 12, Household(6),
                0, 12, 0);

            Assert.That(result.Report.FoodPurchased, Is.EqualTo(2));
            Assert.That(result.NextMarket.FoodInventory, Is.EqualTo(18));
            Assert.That(result.Report.Food.PressurePermille, Is.EqualTo(-900));
            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(900),
                "−225‰ pressure is clamped to the −100‰ monthly cap");
        }

        [Test]
        public void Plan_FullMonthResetsShortageStreak()
        {
            MarketSettlementResult result = Settle(
                Market(10, shortageMonths: 2), 100, 12, Household(6),
                0, 12, 0);

            Assert.That(result.NextMarket.ConsecutiveFoodShortageMonths, Is.Zero);
        }

        [Test]
        public void Plan_PriceIndexUpperBound_IsClamped()
        {
            MarketSettlementResult result = Settle(
                Market(0, foodIndex: MarketState.MaximumPriceIndexPermille), 999999, 12,
                Household(6), 0, 12, 0);

            Assert.That(result.NextMarket.FoodPriceIndexPermille,
                Is.EqualTo(MarketState.MaximumPriceIndexPermille));
            Assert.That(result.NextMarket.FoodPriceIndexPermille,
                Is.LessThanOrEqualTo(MarketState.MaximumPriceIndexPermille));
        }

        [Test]
        public void Plan_PriceIndexLowerBound_IsClamped()
        {
            MarketSettlementResult result = Settle(
                Market(100, foodIndex: MarketState.MinimumPriceIndexPermille), 999999, 12,
                Household(6), 0, 12, 0);

            Assert.That(result.NextMarket.FoodPriceIndexPermille,
                Is.EqualTo(MarketState.MinimumPriceIndexPermille));
        }

        [Test]
        public void Plan_MonthlyMovementIsCappedInBothDirections()
        {
            MarketSettlementResult deficit = Settle(
                Market(0), 999999, 12, Household(6), 0, 12, 0);
            MarketSettlementResult surplus = Settle(
                Market(100), 999999, 12, Household(6), 0, 12, 0);

            Assert.That(deficit.Report.Food.MovementPermille, Is.EqualTo(100));
            Assert.That(surplus.Report.Food.MovementPermille, Is.EqualTo(-100));
        }

        [Test]
        public void Plan_UsesHalfUpRoundingForPressureAndIndex()
        {
            // Materials: 10 consumed vs 11 available -> −91‰ pressure -> −22‰ movement -> 978‰.
            MarketSettlementResult result = Settle(
                Market(10), 100, 11, Household(6),
                0, 10, 0);

            Assert.That(result.Report.Materials.PressurePermille, Is.EqualTo(-91));
            Assert.That(result.Report.Materials.MovementPermille, Is.EqualTo(-22));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(978));
            Assert.That(result.Report.Materials.GoodId, Is.EqualTo(MarketState.MaterialsGoodId));
        }

        [Test]
        public void Plan_BillUsesHalfUpWholeFunds()
        {
            // 2 units at 1250‰ = 2.5 funds, half-up to 3.
            MarketSettlementResult result = Settle(
                Market(5, foodIndex: 1250), 3, 12, Household(6),
                0, 12, 0);

            Assert.That(result.Report.FoodPurchased, Is.EqualTo(2));
            Assert.That(result.Report.FoodBillFunds, Is.EqualTo(3));
            Assert.That(result.NextResources.Funds, Is.Zero);
        }

        [Test]
        public void Plan_FundsLimitPurchasesWithoutDebt()
        {
            // One affordable unit at 1250‰ costs 1; the second would cost 3 total.
            MarketSettlementResult result = Settle(
                Market(5, foodIndex: 1250), 2, 12, Household(6),
                0, 12, 0);

            Assert.That(result.Report.FoodPurchased, Is.EqualTo(1));
            Assert.That(result.Report.FoodUnmet, Is.EqualTo(1));
            Assert.That(result.Report.FoodBillFunds, Is.EqualTo(1));
            Assert.That(result.NextResources.Funds, Is.EqualTo(1));
            Assert.That(result.NextResources.Funds, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.NextMarket.FoodInventory, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Plan_ZeroFundsBuysNothingAndStaysAtZero()
        {
            MarketSettlementResult result = Settle(
                Market(5), 0, 12, Household(6), 0, 12, 0);

            Assert.That(result.Report.FoodPurchased, Is.Zero);
            Assert.That(result.Report.FoodUnmet, Is.EqualTo(2));
            Assert.That(result.NextResources.Funds, Is.Zero);
            Assert.That(result.Report.FoodBillFunds, Is.Zero);
        }

        [Test]
        public void FoodDemand_UsesCeilingOfLivingMembersPerFive()
        {
            Assert.That(MarketSettlement.FoodDemandForHousehold(0), Is.Zero);
            Assert.That(MarketSettlement.FoodDemandForHousehold(1), Is.EqualTo(1));
            Assert.That(MarketSettlement.FoodDemandForHousehold(5), Is.EqualTo(1));
            Assert.That(MarketSettlement.FoodDemandForHousehold(6), Is.EqualTo(2));

            Assert.That(MarketSettlement.CountLivingFamilyMembers(Household(0)), Is.Zero);
            Assert.That(MarketSettlement.CountLivingFamilyMembers(Household(1)), Is.EqualTo(1));
            Assert.That(MarketSettlement.CountLivingFamilyMembers(Household(5)), Is.EqualTo(5));
            Assert.That(MarketSettlement.CountLivingFamilyMembers(Household(6)), Is.EqualTo(6));
            Assert.That(MarketSettlement.CountLivingFamilyMembers(Household(5, 3)),
                Is.EqualTo(5), "dead members are not counted");
        }

        [Test]
        public void Plan_CountsOnlyLivingMembersForDemand()
        {
            MarketSettlementResult result = Settle(
                Market(10), 100, 12, Household(5, 4),
                0, 12, 0);

            Assert.That(result.Report.HouseholdMembers, Is.EqualTo(5));
            Assert.That(result.Report.FoodDemand, Is.EqualTo(1));
        }

        [Test]
        public void Plan_ExtremeFundsAndLowestPriceDoNotOverflow()
        {
            Assert.That(MarketSettlement.AffordableUnits(int.MaxValue, 100),
                Is.EqualTo(int.MaxValue));
            Assert.That(MarketSettlement.FoodDemandForHousehold(int.MaxValue),
                Is.EqualTo(429496730));
            Assert.That(MarketSettlement.FoodDemandForHousehold(int.MaxValue),
                Is.GreaterThan(0));
        }

        [Test]
        public void Plan_DoesNotMutateInputs()
        {
            MarketState market = Market(10, foodIndex: 1100, materialsIndex: 1200,
                shortageMonths: 3);
            IndustryResources.TryCreate(50, 12, 4, out IndustryResources resources, out string error);
            Assert.That(error, Is.Empty);

            IndustryMonthReport industryReport = IndustryReport(resources, 0, 0, 0);
            MarketSettlementResult result = MarketSettlement.Plan(market, industryReport.After,
                Household(6), industryReport, Start);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.NextMarket, Is.Not.SameAs(market));
            Assert.That(market.FoodInventory, Is.EqualTo(10));
            Assert.That(market.FoodPriceIndexPermille, Is.EqualTo(1100));
            Assert.That(market.MaterialsPriceIndexPermille, Is.EqualTo(1200));
            Assert.That(market.LastSettledMonthIndex, Is.EqualTo(StartMonthIndex - 1));
            Assert.That(market.ConsecutiveFoodShortageMonths, Is.EqualTo(3));
            Assert.That(market.RecentCompositeIndices.Count, Is.Zero);
            Assert.That(resources.Funds, Is.EqualTo(50));
            Assert.That(resources.Supplies, Is.EqualTo(12));
            Assert.That(resources.Intelligence, Is.EqualTo(4));
        }

        [Test]
        public void Plan_SettlesGoodsInStableOrder()
        {
            MarketSettlementResult result = Settle(
                Market(10), 100, 12, Household(6), 4, 3, 4);

            Assert.That(result.Report.Lines.Count, Is.EqualTo(2));
            Assert.That(result.Report.Lines[0].GoodId, Is.EqualTo(MarketState.FoodGoodId));
            Assert.That(result.Report.Lines[0].GoodId, Is.EqualTo("market.food"));
            Assert.That(result.Report.Lines[1].GoodId, Is.EqualTo(MarketState.MaterialsGoodId));
            Assert.That(result.Report.Lines[1].GoodId, Is.EqualTo("market.materials"));
        }

        [Test]
        public void Plan_MaterialsUseIndustryOutputAndMissingInputOnly()
        {
            MarketSettlementResult result = Settle(
                Market(10), 100, 12, Household(6), 4, 3, 4);

            Assert.That(result.Report.MaterialsProduced, Is.EqualTo(4));
            Assert.That(result.Report.MaterialsConsumed, Is.EqualTo(3));
            Assert.That(result.Report.Materials.Demand, Is.EqualTo(3));
            Assert.That(result.Report.Materials.AvailableSupply, Is.EqualTo(16));
            Assert.That(result.Report.Materials.SettledQuantity, Is.EqualTo(3));
            Assert.That(result.Report.Materials.FundsCharged, Is.Zero,
                "industry material cost is charged by IndustrySettlement, not twice here");
            Assert.That(result.Report.Materials.PressurePermille, Is.EqualTo(-813));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(900));
            Assert.That(result.NextResources.Supplies, Is.EqualTo(13),
                "the materials inventory is never duplicated or changed here");
        }

        [Test]
        public void Plan_RecordsCompositeIndexHistory()
        {
            MarketSettlementResult result = Settle(
                Market(2, foodIndex: 1101, materialsIndex: 1000), 100, 12, Household(6),
                0, 12, 0);

            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(1101));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(1000));
            Assert.That(result.NextMarket.CompositePriceIndexPermille, Is.EqualTo(1051),
                "the 50/50 basket is half-up: 2101 / 2 = 1050.5 -> 1051");
            Assert.That(result.NextMarket.RecentCompositeIndices.Count, Is.EqualTo(1));
            Assert.That(result.NextMarket.RecentCompositeIndices[0].MonthIndex,
                Is.EqualTo(StartMonthIndex));
            Assert.That(result.NextMarket.TryGetCompositeIndexForMonth(StartMonthIndex,
                out int composite), Is.True);
            Assert.That(composite, Is.EqualTo(1051));
        }

        [Test]
        public void Plan_CompositeHistoryKeepsCurrentAndTwelvePriorMonths()
        {
            var points = new List<MarketIndexPoint>();
            for (int i = 0; i < MarketState.CompositeHistoryCapacity; i++)
            {
                points.Add(new MarketIndexPoint(StartMonthIndex - 13 + i, 1000));
            }

            Assert.That(MarketState.TryCreate(10, 1000, 1000, StartMonthIndex - 1, 0, points,
                out MarketState market, out string error), Is.True, error);

            MarketSettlementResult result = Settle(market, 100, 12, Household(6),
                0, 12, 0);

            Assert.That(result.NextMarket.RecentCompositeIndices.Count,
                Is.EqualTo(MarketState.CompositeHistoryCapacity));
            Assert.That(result.NextMarket.RecentCompositeIndices[0].MonthIndex,
                Is.EqualTo(StartMonthIndex - 12));
            Assert.That(result.NextMarket.RecentCompositeIndices[
                MarketState.CompositeHistoryCapacity - 1].MonthIndex,
                Is.EqualTo(StartMonthIndex));
        }

        [Test]
        public void Plan_AlreadySettledMonth_IsRejected()
        {
            Assert.That(MarketState.TryCreate(10, 1000, 1000, StartMonthIndex, 0, null,
                out MarketState settled, out string error), Is.True, error);

            IndustryMonthReport industryReport = IndustryReport(Resources(100, 12), 0, 0, 0);
            MarketSettlementResult result = MarketSettlement.Plan(settled,
                industryReport.After, Household(6), industryReport, Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(MarketSettlementOutcome.AlreadySettled));
            Assert.That(result.NextMarket.FoodInventory, Is.EqualTo(10));
        }

        [Test]
        public void Plan_SettlementGap_IsRejected()
        {
            Assert.That(MarketState.TryCreate(10, 1000, 1000, StartMonthIndex - 2, 0, null,
                out MarketState gap, out string error), Is.True, error);

            IndustryMonthReport industryReport = IndustryReport(Resources(100, 12), 0, 0, 0);
            MarketSettlementResult result = MarketSettlement.Plan(gap, industryReport.After,
                Household(6), industryReport, Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(MarketSettlementOutcome.SettlementGap));
        }

        [Test]
        public void MarketState_RejectsInvalidPricesHistoryAndInventory()
        {
            Assert.That(MarketState.TryCreate(0, 99, 1000, StartMonthIndex - 1, 0, null,
                out _, out string error), Is.False);
            Assert.That(error, Does.Contain("粮食"));
            Assert.That(MarketState.TryCreate(-1, 1000, 1000, StartMonthIndex - 1, 0, null,
                out _, out _), Is.False);
            Assert.That(MarketState.TryCreate(1000000, 1000, 1000, StartMonthIndex - 1, 0, null,
                out _, out _), Is.False);
            Assert.That(MarketState.TryCreate(0, 1000, 100001, StartMonthIndex - 1, 0, null,
                out _, out _), Is.False);
            Assert.That(MarketState.TryCreate(0, 1000, 1000, StartMonthIndex - 1, -1, null,
                out _, out _), Is.False);
            Assert.That(MarketState.TryCreate(0, 1000, 1000, StartMonthIndex - 1, 0,
                new[] { new MarketIndexPoint(StartMonthIndex, 1000) }, out _, out _), Is.False,
                "history must agree with the last settled month");
            Assert.That(MarketState.TryCreate(0, 1000, 1000, StartMonthIndex - 1, 0,
                new[]
                {
                    new MarketIndexPoint(StartMonthIndex - 2, 1000),
                    new MarketIndexPoint(StartMonthIndex - 2, 1000)
                }, out _, out _), Is.False, "history months must strictly increase");
        }

        [Test]
        public void Plan_ZeroMaterialStockWithAppointedIndustryRaisesPriceAndReportsUnmetInput()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryResources.TryCreate(100, 0, 0, out IndustryResources opening, out string error);
            Assert.That(error, Is.Empty);
            state.TrySetResources(opening, out _);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, Start);
            Assert.That(industry.Succeeded, Is.True, industry.Message);
            Assert.That(industry.Report.Lines[1].Status, Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(industry.Report.Lines[1].RequiredSupplies, Is.EqualTo(2));

            MarketSettlementResult result = MarketSettlement.Plan(Market(10),
                industry.NextState.Resources, succession, industry.Report, Start);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Report.MaterialsRequired, Is.EqualTo(2));
            Assert.That(result.Report.Materials.AvailableSupply, Is.Zero);
            Assert.That(result.Report.Materials.SettledQuantity, Is.Zero);
            Assert.That(result.Report.Materials.UnmetDemand, Is.EqualTo(2),
                "a stalled appointment leaves required input unmet");
            Assert.That(result.Report.Materials.PressurePermille, Is.EqualTo(1000));
            Assert.That(result.Report.Materials.MovementPermille, Is.EqualTo(100));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(1100),
                "material scarcity must raise the price");
            Assert.That(result.Report.MaterialsConsumed, Is.Zero,
                "required input stays distinct from actual consumption");
        }

        [Test]
        public void Plan_InsufficientMaterialStockRaisesPriceAndReportsUnmetInput()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryResources.TryCreate(100, 1, 0, out IndustryResources opening, out string error);
            Assert.That(error, Is.Empty);
            state.TrySetResources(opening, out _);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);

            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, Start);
            Assert.That(industry.Report.Lines[1].Status, Is.EqualTo(IndustryLineStatus.Stalled),
                "one unit cannot cover the trading house's two-unit input");

            MarketSettlementResult result = MarketSettlement.Plan(Market(10),
                industry.NextState.Resources, succession, industry.Report, Start);

            Assert.That(result.Report.Materials.AvailableSupply, Is.EqualTo(1));
            Assert.That(result.Report.Materials.Demand, Is.EqualTo(2));
            Assert.That(result.Report.Materials.UnmetDemand, Is.EqualTo(2));
            Assert.That(result.Report.Materials.PressurePermille, Is.EqualTo(500));
            Assert.That(result.Report.Materials.MovementPermille, Is.EqualTo(100));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(1100));
        }

        [Test]
        public void Plan_OperatingFarmKeepsOwnOutputMaintenanceWithoutDoubleCounting()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryResources.TryCreate(100, 0, 0, out IndustryResources opening, out string error);
            Assert.That(error, Is.Empty);
            state.TrySetResources(opening, out _);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, Start);
            Assert.That(industry.Report.Lines[0].Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(industry.Report.Lines[0].RequiredSupplies, Is.EqualTo(1));
            Assert.That(industry.Report.Lines[0].UpkeepSupplies, Is.EqualTo(1));
            Assert.That(industry.Report.Lines[0].GrossOutputSupplies, Is.EqualTo(4));

            MarketSettlementResult result = MarketSettlement.Plan(Market(10),
                industry.NextState.Resources, succession, industry.Report, Start);

            Assert.That(result.Report.Materials.Demand, Is.EqualTo(1),
                "the farm's retained unit is its only requirement");
            Assert.That(result.Report.Materials.AvailableSupply, Is.EqualTo(4));
            Assert.That(result.Report.Materials.SettledQuantity, Is.EqualTo(1));
            Assert.That(result.Report.Materials.UnmetDemand, Is.Zero);
            Assert.That(result.Report.Materials.PressurePermille, Is.EqualTo(-750));
        }

        [Test]
        public void Plan_BalancedMonthStillReportsChangedBecauseTheMonthAdvanced()
        {
            MarketSettlementResult result = Settle(Market(0), 0, 0, Household(0), 0, 0, 0);

            Assert.That(result.Report.FoodInventoryAfter, Is.EqualTo(result.Report.FoodInventoryBefore));
            Assert.That(result.NextMarket.FoodPriceIndexPermille, Is.EqualTo(1000));
            Assert.That(result.NextMarket.MaterialsPriceIndexPermille, Is.EqualTo(1000));
            Assert.That(result.NextResources, Is.EqualTo(result.Report.ResourcesBefore));
            Assert.That(result.Report.Changed, Is.True,
                "the settled month and its composite history still advanced");
            Assert.That(result.NextMarket.LastSettledMonthIndex, Is.EqualTo(StartMonthIndex));
            Assert.That(result.NextMarket.RecentCompositeIndices.Count, Is.EqualTo(1));
        }

        [Test]
        public void Plan_ResourcesMismatchWithIndustryReport_IsRejected()
        {
            IndustryMonthReport industryReport = IndustryReport(Resources(100, 12), 0, 0, 0);

            MarketSettlementResult result = MarketSettlement.Plan(Market(10),
                Resources(100, 11), Household(6), industryReport, Start);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(MarketSettlementOutcome.InvalidState));
            Assert.That(result.Message, Does.Contain("不一致"));
            Assert.That(result.NextResources.Supplies, Is.EqualTo(11),
                "the rejected plan never substitutes one snapshot for another");
        }

        [Test]
        public void Plan_FarmFoodOutputFeedsTheHouseholdInTheSameMonth()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, Start);
            Assert.That(industry.Succeeded, Is.True, industry.Message);
            Assert.That(industry.Report.Lines[0].GrossOutputFood, Is.EqualTo(4));

            MarketSettlementResult result = MarketSettlement.Plan(Market(0),
                industry.NextState.Resources, succession, industry.Report, Start);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Report.FoodProduced, Is.EqualTo(4));
            Assert.That(result.Report.FoodAvailable, Is.EqualTo(4));
            Assert.That(result.Report.FoodDemand, Is.EqualTo(2));
            Assert.That(result.Report.FoodPurchased, Is.EqualTo(2));
            Assert.That(result.NextMarket.FoodInventory, Is.EqualTo(2));
            Assert.That(result.NextResources.Supplies, Is.EqualTo(industry.NextState.Resources.Supplies),
                "farm material output and upkeep stay in IndustryResources.Supplies");
        }

        [Test]
        public void Plan_StalledFarmFeedsNoFood()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            IndustryState state = IndustryState.CreateInitial(Start, ZeroResources());
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, Start, 5000);
            Assert.That(industry.Report.Lines[0].Status, Is.EqualTo(IndustryLineStatus.Stalled));

            MarketSettlementResult result = MarketSettlement.Plan(Market(0),
                industry.NextState.Resources, succession, industry.Report, Start);

            Assert.That(result.Report.FoodProduced, Is.Zero);
            Assert.That(result.Report.FoodPurchased, Is.Zero);
            Assert.That(result.Report.FoodUnmet, Is.EqualTo(2));
        }

        private static MarketSettlementResult Settle(MarketState market, int funds, int supplies,
            SuccessionState succession, int materialsProduced, int materialsConsumed, int foodProduced)
        {
            IndustryMonthReport industryReport = IndustryReport(Resources(funds, supplies),
                materialsProduced, materialsConsumed, foodProduced);
            MarketSettlementResult result = MarketSettlement.Plan(market, industryReport.After,
                succession, industryReport, Start);
            Assert.That(result.Succeeded, Is.True, result.Message);
            return result;
        }

        private static MarketState Market(int foodInventory, int foodIndex = 1000,
            int materialsIndex = 1000, int shortageMonths = 0)
        {
            Assert.That(MarketState.TryCreate(foodInventory, foodIndex, materialsIndex,
                StartMonthIndex - 1, shortageMonths, null,
                out MarketState market, out string error), Is.True, error);
            return market;
        }

        private static IndustryResources Resources(int funds, int supplies)
        {
            Assert.That(IndustryResources.TryCreate(funds, supplies, 0,
                out IndustryResources resources, out string error), Is.True, error);
            return resources;
        }

        private static IndustryResources ZeroResources()
        {
            Assert.That(IndustryResources.TryCreate(0, 0, 0,
                out IndustryResources resources, out string error), Is.True, error);
            return resources;
        }

        private static SuccessionState Household(int living, int dead = 0)
        {
            var world = new SuccessionWorld(new FamilyId("family.m08b.market-test"), "市场测试家");
            int total = living + dead;
            for (int i = 0; i < total; i++)
            {
                bool alive = i < living;
                var personId = new PersonId("person.m08b.market-test." +
                    i.ToString("00", CultureInfo.InvariantCulture));
                Assert.That(world.TryAddPerson(new PersonState(personId, "测试" + i,
                        i % 2 == 0 ? PersonGender.Male : PersonGender.Female),
                    new SuccessionPersonLife(1500 + i, alive, false), out string error),
                    Is.True, error);
                Assert.That(world.TryAddMember(personId,
                    i == 0 ? FamilyMemberRole.Head : FamilyMemberRole.DirectKin, out error),
                    Is.True, error);
            }

            var state = new SuccessionState(world);
            if (living > 0)
                state.HeadPersonId = new PersonId("person.m08b.market-test.00");
            return state;
        }

        private static IndustryMonthReport IndustryReport(IndustryResources before,
            int materialsProduced, int materialsConsumed, int foodProduced)
        {
            Assert.That(materialsConsumed, Is.LessThanOrEqualTo(before.Supplies),
                "a synthetic industry month cannot draw more material stock than it started with");
            Assert.That(IndustryResources.TryCreate(before.Funds,
                before.Supplies - materialsConsumed + materialsProduced, before.Intelligence,
                out IndustryResources after, out string error), Is.True, error);

            var lines = new List<IndustryLineResult>(1);
            if (materialsProduced != 0 || materialsConsumed != 0 || foodProduced != 0)
            {
                lines.Add(new IndustryLineResult("industry.test", "测试产业", default, string.Empty,
                    IndustryLineStatus.Operated, "运作中", 0, materialsConsumed, 0,
                    materialsProduced, 0, 0, materialsProduced - materialsConsumed, 0,
                    0, 0, 0, false, foodProduced, 0, materialsConsumed));
            }

            return new IndustryMonthReport(Start.Year, Start.Month, before, after,
                lines.AsReadOnly(), Array.Empty<IndustryVacancy>(), false, lines.Count > 0,
                true, "测试产业月结。");
        }
    }
}
