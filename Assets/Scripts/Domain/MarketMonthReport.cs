using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// One good's settled result inside a market month. Food carries the household bill and
    /// its unmet demand; materials report the industry input draw whose funds cost is already
    /// charged by <see cref="IndustrySettlement"/>, so its <see cref="FundsCharged"/> is zero.
    /// </summary>
    public readonly struct MarketGoodResult
    {
        public MarketGoodResult(string goodId, int demand, int availableSupply, int settledQuantity,
            int unmetDemand, int fundsCharged, int priceIndexBeforePermille,
            int priceIndexAfterPermille, int pressurePermille, int movementPermille)
        {
            GoodId = goodId ?? string.Empty;
            Demand = demand;
            AvailableSupply = availableSupply;
            SettledQuantity = settledQuantity;
            UnmetDemand = unmetDemand;
            FundsCharged = fundsCharged;
            PriceIndexBeforePermille = priceIndexBeforePermille;
            PriceIndexAfterPermille = priceIndexAfterPermille;
            PressurePermille = pressurePermille;
            MovementPermille = movementPermille;
        }

        public string GoodId { get; }
        public int Demand { get; }
        public int AvailableSupply { get; }
        public int SettledQuantity { get; }
        public int UnmetDemand { get; }
        public int FundsCharged { get; }
        public int PriceIndexBeforePermille { get; }
        public int PriceIndexAfterPermille { get; }
        /// <summary>Signed (demand - supply) pressure in permille of the larger side.</summary>
        public int PressurePermille { get; }
        /// <summary>Signed index movement applied this month, already clamped to ±100‰.</summary>
        public int MovementPermille { get; }
    }

    /// <summary>Immutable result of one month's market settlement.</summary>
    public sealed class MarketMonthReport
    {
        public MarketMonthReport(int year, int month, MarketState before, MarketState after,
            IndustryResources resourcesBefore, IndustryResources resourcesAfter, int householdMembers,
            int foodProduced, int materialsProduced, int materialsConsumed,
            IReadOnlyList<MarketGoodResult> lines, bool changed, string summary)
        {
            Year = year;
            Month = month;
            Before = before;
            After = after;
            ResourcesBefore = resourcesBefore;
            ResourcesAfter = resourcesAfter;
            HouseholdMembers = householdMembers;
            FoodProduced = foodProduced;
            MaterialsProduced = materialsProduced;
            MaterialsConsumed = materialsConsumed;
            Lines = lines ?? Array.Empty<MarketGoodResult>();
            Changed = changed;
            Summary = summary ?? string.Empty;
        }

        public int Year { get; }
        public int Month { get; }
        public MarketState Before { get; }
        public MarketState After { get; }
        public IndustryResources ResourcesBefore { get; }
        public IndustryResources ResourcesAfter { get; }

        /// <summary>Living family members counted from the month-start snapshot.</summary>
        public int HouseholdMembers { get; }
        public int FoodProduced { get; }
        public int MaterialsProduced { get; }
        public int MaterialsConsumed { get; }

        /// <summary>Stable good order: market.food first, then market.materials.</summary>
        public IReadOnlyList<MarketGoodResult> Lines { get; }
        public bool Changed { get; }
        public string Summary { get; }

        public MarketGoodResult Food => LineAt(0);
        public MarketGoodResult Materials => LineAt(1);

        public int FoodDemand => Food.Demand;
        public int FoodPurchased => Food.SettledQuantity;
        public int FoodUnmet => Food.UnmetDemand;
        public int FoodBillFunds => Food.FundsCharged;
        public int FoodAvailable => Food.AvailableSupply;
        public int FoodInventoryBefore => Before.FoodInventory;
        public int FoodInventoryAfter => After.FoodInventory;
        public int FoodPriceIndexBefore => Before.FoodPriceIndexPermille;
        public int FoodPriceIndexAfter => After.FoodPriceIndexPermille;
        public int MaterialsInventoryBefore => ResourcesBefore.Supplies;
        public int MaterialsInventoryAfter => ResourcesAfter.Supplies;
        public int MaterialsPriceIndexBefore => Before.MaterialsPriceIndexPermille;
        public int MaterialsPriceIndexAfter => After.MaterialsPriceIndexPermille;
        public int ConsecutiveFoodShortageMonths => After.ConsecutiveFoodShortageMonths;

        public bool HasShortage => FoodUnmet > 0;

        private MarketGoodResult LineAt(int index) =>
            index >= 0 && index < Lines.Count ? Lines[index] : default;
    }
}
