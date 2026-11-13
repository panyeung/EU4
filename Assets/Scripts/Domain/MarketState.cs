using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// One settled month's composite market index: the fixed 50/50 food and materials
    /// basket, used for the approved year-over-year extreme-inflation check.
    /// </summary>
    public readonly struct MarketIndexPoint : IEquatable<MarketIndexPoint>
    {
        public MarketIndexPoint(int monthIndex, int compositeIndexPermille)
        {
            MonthIndex = monthIndex;
            CompositeIndexPermille = compositeIndexPermille;
        }

        public int MonthIndex { get; }
        public int CompositeIndexPermille { get; }

        public bool Equals(MarketIndexPoint other) =>
            MonthIndex == other.MonthIndex &&
            CompositeIndexPermille == other.CompositeIndexPermille;

        public override bool Equals(object obj) => obj is MarketIndexPoint other && Equals(other);

        public override int GetHashCode() => unchecked((MonthIndex * 397) ^ CompositeIndexPermille);

        public static bool operator ==(MarketIndexPoint a, MarketIndexPoint b) => a.Equals(b);
        public static bool operator !=(MarketIndexPoint a, MarketIndexPoint b) => !a.Equals(b);
    }

    /// <summary>
    /// Immutable M08B market state. Food has its own inventory; production materials keep
    /// using <see cref="IndustryResources.Supplies"/> and are deliberately not duplicated.
    /// Prices are game indices in permille of a 1-fund base price, never real-world CPI.
    /// </summary>
    public sealed class MarketState
    {
        public const string FoodGoodId = "market.food";
        public const string MaterialsGoodId = "market.materials";

        public const int BaselinePriceIndexPermille = 1000;
        public const int MinimumPriceIndexPermille = 100;
        public const int MaximumPriceIndexPermille = 100000;

        public const int MinimumFoodInventory = 0;
        public const int MaximumFoodInventory = 999999;

        /// <summary>Current settled month plus the twelve months needed for a year-over-year check.</summary>
        public const int CompositeHistoryCapacity = 13;

        private readonly MarketIndexPoint[] _recentCompositeIndices;
        private readonly IReadOnlyList<MarketIndexPoint> _recentCompositeIndicesView;

        private MarketState(int foodInventory, int foodPriceIndexPermille,
            int materialsPriceIndexPermille, int lastSettledMonthIndex,
            int consecutiveFoodShortageMonths, MarketIndexPoint[] recentCompositeIndices)
        {
            FoodInventory = foodInventory;
            FoodPriceIndexPermille = foodPriceIndexPermille;
            MaterialsPriceIndexPermille = materialsPriceIndexPermille;
            LastSettledMonthIndex = lastSettledMonthIndex;
            ConsecutiveFoodShortageMonths = consecutiveFoodShortageMonths;
            _recentCompositeIndices = recentCompositeIndices ?? Array.Empty<MarketIndexPoint>();
            _recentCompositeIndicesView = Array.AsReadOnly(_recentCompositeIndices);
        }

        public int FoodInventory { get; }
        public int FoodPriceIndexPermille { get; }
        public int MaterialsPriceIndexPermille { get; }
        public int LastSettledMonthIndex { get; }

        /// <summary>Consecutive months whose household food demand was not fully met.</summary>
        public int ConsecutiveFoodShortageMonths { get; }

        /// <summary>
        /// Settled composite indices in ascending month order, oldest first. Capacity is
        /// <see cref="CompositeHistoryCapacity"/> so the current index and the one from
        /// twelve months earlier can coexist without another stored series.
        /// </summary>
        public IReadOnlyList<MarketIndexPoint> RecentCompositeIndices => _recentCompositeIndicesView;

        /// <summary>Fixed 50/50 food and materials basket, half-up arithmetic mean.</summary>
        public int CompositePriceIndexPermille =>
            MarketSettlement.RoundHalfUp((long)FoodPriceIndexPermille + MaterialsPriceIndexPermille, 2);

        public static bool TryCreate(int foodInventory, int foodPriceIndexPermille,
            int materialsPriceIndexPermille, int lastSettledMonthIndex,
            int consecutiveFoodShortageMonths, IReadOnlyList<MarketIndexPoint> recentCompositeIndices,
            out MarketState state, out string error)
        {
            state = null;

            if (foodInventory < MinimumFoodInventory || foodInventory > MaximumFoodInventory)
            {
                error = "粮食库存超出允许范围 0..999999。";
                return false;
            }
            if (!IsValidPriceIndex(foodPriceIndexPermille))
            {
                error = "粮食价格指数超出允许范围 100..100000‰。";
                return false;
            }
            if (!IsValidPriceIndex(materialsPriceIndexPermille))
            {
                error = "生产物资价格指数超出允许范围 100..100000‰。";
                return false;
            }
            if (lastSettledMonthIndex < 0)
            {
                error = "市场已结算月份索引不能为负。";
                return false;
            }
            if (consecutiveFoodShortageMonths < 0)
            {
                error = "粮食连续短缺月数不能为负。";
                return false;
            }

            int historyCount = recentCompositeIndices?.Count ?? 0;
            if (historyCount > CompositeHistoryCapacity)
            {
                error = "综合价格指数历史超过 " + CompositeHistoryCapacity + " 个月。";
                return false;
            }

            var copy = new MarketIndexPoint[historyCount];
            int previousMonthIndex = int.MinValue;
            for (int i = 0; i < historyCount; i++)
            {
                MarketIndexPoint point = recentCompositeIndices[i];
                if (point.MonthIndex <= previousMonthIndex)
                {
                    error = "综合价格指数历史必须按月份严格递增。";
                    return false;
                }
                if (!IsValidPriceIndex(point.CompositeIndexPermille))
                {
                    error = "综合价格指数超出允许范围 100..100000‰。";
                    return false;
                }

                previousMonthIndex = point.MonthIndex;
                copy[i] = point;
            }

            if (historyCount > 0 && copy[historyCount - 1].MonthIndex != lastSettledMonthIndex)
            {
                error = "最近综合价格指数必须属于已结算月份。";
                return false;
            }

            state = new MarketState(foodInventory, foodPriceIndexPermille,
                materialsPriceIndexPermille, lastSettledMonthIndex,
                consecutiveFoodShortageMonths, copy);
            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Opening market: caller-supplied food stock, both indices at the 1000‰ base and no
        /// settled month or recorded shortage history yet.
        /// </summary>
        public static MarketState CreateInitial(GameDate date, int foodInventory)
        {
            if (!TryCreate(foodInventory, BaselinePriceIndexPermille, BaselinePriceIndexPermille,
                    IndustryCalendar.MonthIndex(date) - 1, 0, null,
                    out MarketState state, out string error))
                throw new ArgumentOutOfRangeException(nameof(foodInventory), error);
            return state;
        }

        public MarketState Clone() =>
            new MarketState(FoodInventory, FoodPriceIndexPermille, MaterialsPriceIndexPermille,
                LastSettledMonthIndex, ConsecutiveFoodShortageMonths,
                (MarketIndexPoint[])_recentCompositeIndices.Clone());

        /// <summary>True when the settled composite index for that exact month is recorded.</summary>
        public bool TryGetCompositeIndexForMonth(int monthIndex, out int compositeIndexPermille)
        {
            for (int i = _recentCompositeIndices.Length - 1; i >= 0; i--)
            {
                if (_recentCompositeIndices[i].MonthIndex == monthIndex)
                {
                    compositeIndexPermille = _recentCompositeIndices[i].CompositeIndexPermille;
                    return true;
                }
            }

            compositeIndexPermille = 0;
            return false;
        }

        public static bool IsValidPriceIndex(int priceIndexPermille) =>
            priceIndexPermille >= MinimumPriceIndexPermille &&
            priceIndexPermille <= MaximumPriceIndexPermille;
    }
}
