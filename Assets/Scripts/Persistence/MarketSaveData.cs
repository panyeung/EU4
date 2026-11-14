using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>One settled composite market index, keyed by the settled month.</summary>
    [Serializable]
    public sealed class MarketIndexPointSaveEntry
    {
        public int monthIndex;
        public int compositeIndexPermille;
    }

    /// <summary>
    /// Versioned M08B market block (v17). It stores the food stock, both game price
    /// indices, the last settled month, the consecutive shortage count and the settled
    /// composite-index history. The production-materials stock is not duplicated here;
    /// it stays in <see cref="IndustryStateSaveData.supplies"/>.
    /// </summary>
    [Serializable]
    public sealed class MarketSaveData
    {
        public int foodInventory;
        public int foodPriceIndexPermille;
        public int materialsPriceIndexPermille;
        public int lastSettledMonthIndex;
        public int consecutiveFoodShortageMonths;
        public MarketIndexPointSaveEntry[] compositeHistory;

        /// <summary>Opening block: no settled month yet and the base 1000‰ prices.</summary>
        public static MarketSaveData CreateInitial(GameDate date) =>
            Capture(MarketState.CreateInitial(date, 0));

        public static MarketSaveData Capture(MarketState state)
        {
            if (state == null)
                return null;

            var history = new MarketIndexPointSaveEntry[state.RecentCompositeIndices.Count];
            for (int i = 0; i < history.Length; i++)
            {
                MarketIndexPoint point = state.RecentCompositeIndices[i];
                history[i] = new MarketIndexPointSaveEntry
                {
                    monthIndex = point.MonthIndex,
                    compositeIndexPermille = point.CompositeIndexPermille
                };
            }

            return new MarketSaveData
            {
                foodInventory = state.FoodInventory,
                foodPriceIndexPermille = state.FoodPriceIndexPermille,
                materialsPriceIndexPermille = state.MaterialsPriceIndexPermille,
                lastSettledMonthIndex = state.LastSettledMonthIndex,
                consecutiveFoodShortageMonths = state.ConsecutiveFoodShortageMonths,
                compositeHistory = history
            };
        }

        /// <summary>
        /// v16-to-v17 conversion: carve at most one month of household food demand out of
        /// the existing production-material stock. The total stock never grows and no
        /// famine history, price movement or elapsed month is invented.
        /// </summary>
        public static MarketSaveData CreateMigrated(SaveData legacy)
        {
            if (legacy == null || legacy.industry == null)
                return null;

            // The saved date must be valid before any market month can be derived. Final
            // validation reports the date error, so an unparseable date yields no block.
            if (!GameDate.TryParse(legacy.year, legacy.month, legacy.day, out GameDate date))
                return null;

            int livingMembers = CountLivingFamilyMembers(legacy.succession);
            int food = Math.Min(legacy.industry.supplies,
                MarketSettlement.FoodDemandForHousehold(livingMembers));
            legacy.industry.supplies -= food;

            return new MarketSaveData
            {
                foodInventory = food,
                foodPriceIndexPermille = MarketState.BaselinePriceIndexPermille,
                materialsPriceIndexPermille = MarketState.BaselinePriceIndexPermille,
                lastSettledMonthIndex = IndustryCalendar.MonthIndex(date) - 1,
                consecutiveFoodShortageMonths = 0,
                compositeHistory = new MarketIndexPointSaveEntry[0]
            };
        }

        /// <summary>Living members of the saved family roster, the v16 demand basis.</summary>
        public static int CountLivingFamilyMembers(SuccessionSaveData succession)
        {
            if (succession == null || succession.persons == null || succession.members == null)
                return 0;

            int count = 0;
            foreach (SuccessionMemberSaveEntry member in succession.members)
            {
                if (member == null)
                    continue;
                foreach (SuccessionPersonSaveEntry person in succession.persons)
                {
                    if (person == null || person.personId != member.personId)
                        continue;
                    if (person.alive)
                        count++;
                    break;
                }
            }
            return count;
        }

        public MarketSaveData Clone()
        {
            var clone = new MarketSaveData
            {
                foodInventory = foodInventory,
                foodPriceIndexPermille = foodPriceIndexPermille,
                materialsPriceIndexPermille = materialsPriceIndexPermille,
                lastSettledMonthIndex = lastSettledMonthIndex,
                consecutiveFoodShortageMonths = consecutiveFoodShortageMonths,
                // A malformed null stays null so validation and restore reject it instead
                // of silently repairing the block with an empty history.
                compositeHistory = null
            };
            if (compositeHistory == null)
                return clone;

            clone.compositeHistory = new MarketIndexPointSaveEntry[compositeHistory.Length];
            for (int i = 0; i < compositeHistory.Length; i++)
            {
                MarketIndexPointSaveEntry entry = compositeHistory[i];
                clone.compositeHistory[i] = entry == null ? null : new MarketIndexPointSaveEntry
                {
                    monthIndex = entry.monthIndex,
                    compositeIndexPermille = entry.compositeIndexPermille
                };
            }
            return clone;
        }

        /// <summary>Validates the stored block against the saved date before it is applied.</summary>
        public SaveOperationResult ValidateForSave(GameDate date)
        {
            if (foodInventory < MarketState.MinimumFoodInventory ||
                foodInventory > MarketState.MaximumFoodInventory)
                return Invalid("存档粮食库存超出允许范围。");
            if (!MarketState.IsValidPriceIndex(foodPriceIndexPermille))
                return Invalid("存档粮食价格指数超出允许范围 100..100000‰。");
            if (!MarketState.IsValidPriceIndex(materialsPriceIndexPermille))
                return Invalid("存档生产物资价格指数超出允许范围 100..100000‰。");
            if (consecutiveFoodShortageMonths < 0)
                return Invalid("存档粮食连续短缺月数不能为负。");
            if (lastSettledMonthIndex != IndustryCalendar.MonthIndex(date) - 1)
                return Invalid("存档市场已结算月份与存档日期不一致。");
            if (compositeHistory == null)
                return Invalid("存档缺少市场综合价格指数历史（market.compositeHistory）。");
            if (compositeHistory.Length > MarketState.CompositeHistoryCapacity)
                return Invalid("存档市场综合价格指数历史超过 " +
                               MarketState.CompositeHistoryCapacity + " 个月。");

            int previousMonthIndex = int.MinValue;
            for (int i = 0; i < compositeHistory.Length; i++)
            {
                MarketIndexPointSaveEntry entry = compositeHistory[i];
                if (entry == null)
                    return Invalid("存档市场综合价格指数历史包含空记录。");
                if (entry.monthIndex <= previousMonthIndex)
                    return Invalid("存档市场综合价格指数历史必须按月份严格递增。");
                if (!MarketState.IsValidPriceIndex(entry.compositeIndexPermille))
                    return Invalid("存档市场综合价格指数超出允许范围 100..100000‰。");
                previousMonthIndex = entry.monthIndex;
            }
            if (compositeHistory.Length > 0 &&
                compositeHistory[compositeHistory.Length - 1].monthIndex != lastSettledMonthIndex)
                return Invalid("存档最近综合价格指数必须属于已结算月份。");

            return SaveOperationResult.Success();
        }

        /// <summary>Builds the immutable domain state; callers validate first.</summary>
        public bool TryRestore(out MarketState state, out string error)
        {
            state = null;
            if (compositeHistory == null)
            {
                error = "存档缺少市场综合价格指数历史（market.compositeHistory）。";
                return false;
            }

            var history = new MarketIndexPoint[compositeHistory.Length];
            for (int i = 0; i < history.Length; i++)
            {
                MarketIndexPointSaveEntry entry = compositeHistory[i];
                if (entry == null)
                {
                    error = "存档市场综合价格指数历史包含空记录。";
                    return false;
                }
                history[i] = new MarketIndexPoint(entry.monthIndex, entry.compositeIndexPermille);
            }

            return MarketState.TryCreate(foodInventory, foodPriceIndexPermille,
                materialsPriceIndexPermille, lastSettledMonthIndex,
                consecutiveFoodShortageMonths, history, out state, out error);
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }
}
