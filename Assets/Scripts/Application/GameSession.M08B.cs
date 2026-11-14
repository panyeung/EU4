using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// M08B market integration. The month transaction settles the market with the same
    /// atomic commit and rollback as every other monthly ledger; this partial only owns
    /// the read-only accessors, the opening food stock and the initial-state helper.
    /// </summary>
    public sealed partial class GameSession
    {
        private MarketState _marketState;
        private MarketMonthReport _lastMarketReport;

        /// <summary>Read-only settled market state; opening values before the first month.</summary>
        public MarketState Market => EnsureMarket();

        /// <summary>Report of the most recently committed month; cleared by a load.</summary>
        public MarketMonthReport LastMarketReport
        {
            get => _lastMarketReport;
            private set => _lastMarketReport = value;
        }

        /// <summary>Opening food covers exactly one month of the living family's demand.</summary>
        private static MarketState CreateInitialMarket(GameDate date, SuccessionState succession)
        {
            int demand = MarketSettlement.FoodDemandForHousehold(
                MarketSettlement.CountLivingFamilyMembers(succession));
            return MarketState.CreateInitial(date, demand);
        }

        private MarketState EnsureMarket()
        {
            if (_marketState == null)
                _marketState = CreateInitialMarket(CurrentGameDate, _succession);
            return _marketState;
        }
    }
}
