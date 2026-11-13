using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SengokuChronicle.Domain
{
    public enum MarketSettlementOutcome { Succeeded, AlreadySettled, SettlementGap, InvalidState }

    public readonly struct MarketSettlementResult
    {
        public MarketSettlementResult(MarketSettlementOutcome outcome, bool succeeded, string message,
            MarketState nextMarket, IndustryResources nextResources, MarketMonthReport report)
        {
            Outcome = outcome;
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            NextMarket = nextMarket;
            NextResources = nextResources;
            Report = report;
        }

        public MarketSettlementOutcome Outcome { get; }
        public bool Succeeded { get; }
        public string Message { get; }
        public MarketState NextMarket { get; }
        public IndustryResources NextResources { get; }
        public MarketMonthReport Report { get; }
    }

    /// <summary>
    /// Pure, deterministic M08B market settlement. Household food is bought at the previous
    /// month's index with no debt and no negative balance; materials are supplied by industry
    /// output and consumed by industry upkeep, and their funds cost is charged separately by
    /// <see cref="IndustrySettlement"/>. It never mutates its inputs or consumes randomness.
    /// </summary>
    public static class MarketSettlement
    {
        public const int PersonsPerFoodUnit = 5;
        public const int MaximumMonthlyMovementPermille = 100;

        public static MarketSettlementResult Plan(MarketState market, IndustryResources resources,
            SuccessionState succession, IndustryMonthReport industryReport, GameDate closedMonth)
        {
            if (market == null || succession == null || succession.World == null)
                return Rejected(MarketSettlementOutcome.InvalidState, market, resources,
                    "市场或继承状态为空。");

            int currentIndex = IndustryCalendar.MonthIndex(closedMonth);
            if (market.LastSettledMonthIndex == currentIndex)
                return Rejected(MarketSettlementOutcome.AlreadySettled, market, resources,
                    "本月市场已经结算，不会重复交易或更新价格。");
            if (market.LastSettledMonthIndex > currentIndex)
                return Rejected(MarketSettlementOutcome.InvalidState, market, resources,
                    "已结算月份晚于当前月份，市场状态无效。");
            if (market.LastSettledMonthIndex < currentIndex - 1)
                return Rejected(MarketSettlementOutcome.SettlementGap, market, resources,
                    "存在未结算月份，不能静默补算多个月。");

            int householdMembers = CountLivingFamilyMembers(succession);
            int foodDemand = FoodDemandForHousehold(householdMembers);

            int foodProduced = 0;
            int materialsProduced = 0;
            int materialsConsumed = 0;
            if (industryReport != null)
            {
                for (int i = 0; i < industryReport.Lines.Count; i++)
                {
                    IndustryLineResult line = industryReport.Lines[i];
                    if (line.Status != IndustryLineStatus.Operated)
                        continue;
                    foodProduced += line.GrossOutputFood;
                    materialsProduced += line.GrossOutputSupplies;
                    materialsConsumed += line.UpkeepSupplies;
                }
            }

            int foodAvailable = ClampInventory((long)market.FoodInventory + foodProduced);
            int affordableFood = AffordableUnits(resources.Funds, market.FoodPriceIndexPermille);
            int foodPurchased = Math.Min(foodDemand, Math.Min(foodAvailable, affordableFood));
            int foodBill = FoodBill(foodPurchased, market.FoodPriceIndexPermille);
            int foodUnmet = foodDemand - foodPurchased;

            int materialsInventoryBefore = industryReport != null
                ? industryReport.Before.Supplies
                : resources.Supplies;
            int materialsAvailable = ClampResources((long)materialsInventoryBefore + materialsProduced);
            int materialsSettled = Math.Min(materialsConsumed, materialsAvailable);
            int materialsUnmet = materialsConsumed - materialsSettled;

            int foodPressure = PressurePermille(foodDemand, foodAvailable);
            int materialsPressure = PressurePermille(materialsConsumed, materialsAvailable);
            int foodMovement = MovementPermille(foodPressure);
            int materialsMovement = MovementPermille(materialsPressure);
            int nextFoodPriceIndex = NextPriceIndex(market.FoodPriceIndexPermille, foodMovement);
            int nextMaterialsPriceIndex =
                NextPriceIndex(market.MaterialsPriceIndexPermille, materialsMovement);

            int nextFoodInventory = foodAvailable - foodPurchased;
            int nextShortageMonths = foodUnmet > 0
                ? market.ConsecutiveFoodShortageMonths == int.MaxValue
                    ? int.MaxValue
                    : market.ConsecutiveFoodShortageMonths + 1
                : 0;

            var history = new List<MarketIndexPoint>(MarketState.CompositeHistoryCapacity);
            int historyStart = Math.Max(0,
                market.RecentCompositeIndices.Count - (MarketState.CompositeHistoryCapacity - 1));
            for (int i = historyStart; i < market.RecentCompositeIndices.Count; i++)
                history.Add(market.RecentCompositeIndices[i]);
            int nextCompositeIndex = RoundHalfUp((long)nextFoodPriceIndex + nextMaterialsPriceIndex, 2);
            history.Add(new MarketIndexPoint(currentIndex, nextCompositeIndex));

            if (!MarketState.TryCreate(nextFoodInventory, nextFoodPriceIndex, nextMaterialsPriceIndex,
                    currentIndex, nextShortageMonths, history,
                    out MarketState nextMarket, out string marketError))
                return Rejected(MarketSettlementOutcome.InvalidState, market, resources, marketError);

            if (!IndustryResources.TryCreate(resources.Funds - foodBill, resources.Supplies,
                    resources.Intelligence, out IndustryResources nextResources, out string resourceError))
                return Rejected(MarketSettlementOutcome.InvalidState, market, resources, resourceError);

            var lines = new List<MarketGoodResult>(2)
            {
                new MarketGoodResult(MarketState.FoodGoodId, foodDemand, foodAvailable, foodPurchased,
                    foodUnmet, foodBill, market.FoodPriceIndexPermille, nextFoodPriceIndex,
                    foodPressure, foodMovement),
                new MarketGoodResult(MarketState.MaterialsGoodId, materialsConsumed, materialsAvailable,
                    materialsSettled, materialsUnmet, 0, market.MaterialsPriceIndexPermille,
                    nextMaterialsPriceIndex, materialsPressure, materialsMovement)
            };

            bool changed = nextMarket.FoodInventory != market.FoodInventory ||
                           nextMarket.FoodPriceIndexPermille != market.FoodPriceIndexPermille ||
                           nextMarket.MaterialsPriceIndexPermille != market.MaterialsPriceIndexPermille ||
                           nextMarket.ConsecutiveFoodShortageMonths != market.ConsecutiveFoodShortageMonths ||
                           nextResources != resources;
            string summary = BuildSummary(householdMembers, foodDemand, foodPurchased,
                foodUnmet, foodBill, market.FoodInventory, nextFoodInventory,
                market.FoodPriceIndexPermille, nextFoodPriceIndex, materialsInventoryBefore,
                nextResources.Supplies, materialsConsumed, market.MaterialsPriceIndexPermille,
                nextMaterialsPriceIndex);

            var report = new MarketMonthReport(closedMonth.Year, closedMonth.Month, market, nextMarket,
                resources, nextResources, householdMembers, foodProduced, materialsProduced,
                materialsConsumed, lines.AsReadOnly(), changed, summary);
            return new MarketSettlementResult(MarketSettlementOutcome.Succeeded, true,
                "市场月结完成。", nextMarket, nextResources, report);
        }

        /// <summary>Household food demand: one unit per started group of five living members.</summary>
        public static int FoodDemandForHousehold(int livingMembers)
        {
            if (livingMembers <= 0)
                return 0;
            long demand = ((long)livingMembers + PersonsPerFoodUnit - 1) / PersonsPerFoodUnit;
            return demand > int.MaxValue ? int.MaxValue : (int)demand;
        }

        /// <summary>Living family members from the supplied month-start succession snapshot.</summary>
        public static int CountLivingFamilyMembers(SuccessionState succession)
        {
            if (succession == null || succession.World == null)
                return 0;

            int count = 0;
            IReadOnlyList<FamilyMembership> members = succession.World.Family.Members;
            for (int i = 0; i < members.Count; i++)
            {
                if (succession.World.IsLiving(members[i].PersonId))
                    count++;
            }

            return count;
        }

        /// <summary>half-up rounding for a signed numerator and a positive denominator.</summary>
        internal static int RoundHalfUp(long numerator, long denominator)
        {
            if (denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(denominator));

            if (numerator >= 0)
                return (int)((numerator + denominator / 2) / denominator);
            return (int)(-((-numerator + denominator / 2) / denominator));
        }

        /// <summary>Signed imbalance in permille of the larger side; zero when both are zero.</summary>
        internal static int PressurePermille(int demand, int supply)
        {
            long scale = Math.Max(Math.Max(demand, supply), 1);
            return RoundHalfUp(((long)demand - supply) * 1000L, scale);
        }

        internal static int MovementPermille(int pressurePermille)
        {
            int movement = pressurePermille / 4;
            if (movement > MaximumMonthlyMovementPermille)
                return MaximumMonthlyMovementPermille;
            if (movement < -MaximumMonthlyMovementPermille)
                return -MaximumMonthlyMovementPermille;
            return movement;
        }

        internal static int NextPriceIndex(int currentPriceIndexPermille, int movementPermille)
        {
            int next = RoundHalfUp((long)currentPriceIndexPermille * (1000 + movementPermille), 1000);
            if (next < MarketState.MinimumPriceIndexPermille)
                return MarketState.MinimumPriceIndexPermille;
            if (next > MarketState.MaximumPriceIndexPermille)
                return MarketState.MaximumPriceIndexPermille;
            return next;
        }

        internal static int FoodBill(int units, int priceIndexPermille) =>
            RoundHalfUp((long)units * priceIndexPermille, MarketState.BaselinePriceIndexPermille);

        /// <summary>
        /// Largest whole-unit quantity whose half-up bill does not exceed available funds.
        /// Closed form: bill(q) = floor((q * price + 500) / 1000) ≤ funds, so the exact
        /// bound is q ≤ (1000 * (funds + 1) - 501) / price. No loop and no int overflow.
        /// </summary>
        internal static int AffordableUnits(int funds, int priceIndexPermille)
        {
            if (funds <= 0 || priceIndexPermille <= 0)
                return 0;

            long numerator = (long)MarketState.BaselinePriceIndexPermille * (funds + 1L) -
                             (MarketState.BaselinePriceIndexPermille / 2) - 1;
            long quantity = numerator / priceIndexPermille;
            if (quantity <= 0)
                return 0;
            return quantity > int.MaxValue ? int.MaxValue : (int)quantity;
        }

        private static int ClampInventory(long value) =>
            value < MarketState.MinimumFoodInventory ? MarketState.MinimumFoodInventory
                : value > MarketState.MaximumFoodInventory ? MarketState.MaximumFoodInventory
                : (int)value;

        private static int ClampResources(long value) =>
            value < IndustryResources.Minimum ? IndustryResources.Minimum
                : value > IndustryResources.Maximum ? IndustryResources.Maximum
                : (int)value;

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string BuildSummary(int householdMembers, int foodDemand,
            int foodPurchased, int foodUnmet, int foodBill, int foodBefore, int foodAfter,
            int foodIndexBefore, int foodIndexAfter, int materialsBefore, int materialsAfter,
            int materialsConsumed, int materialsIndexBefore, int materialsIndexAfter)
        {
            var builder = new StringBuilder(192);
            builder.Append("人口 ").Append(Number(householdMembers)).Append(" 人，需粮 ")
                .Append(Number(foodDemand)).Append("，购入 ").Append(Number(foodPurchased))
                .Append("（").Append(Number(foodBill)).Append(" 资金）。");
            if (foodUnmet > 0)
                builder.Append("缺粮 ").Append(Number(foodUnmet)).Append("。");
            builder.Append("粮食 ").Append(Number(foodBefore)).Append('→').Append(Number(foodAfter))
                .Append("，粮价 ").Append(Number(foodIndexBefore)).Append('→')
                .Append(Number(foodIndexAfter)).Append("‰；物资 ").Append(Number(materialsBefore))
                .Append('→').Append(Number(materialsAfter)).Append("，产业投入 ")
                .Append(Number(materialsConsumed)).Append("，物资价 ")
                .Append(Number(materialsIndexBefore)).Append('→')
                .Append(Number(materialsIndexAfter)).Append("‰。");
            return builder.ToString();
        }

        private static MarketSettlementResult Rejected(MarketSettlementOutcome outcome,
            MarketState market, IndustryResources resources, string message) =>
            new MarketSettlementResult(outcome, false, message,
                market != null ? market.Clone() : null, resources, null);
    }
}
