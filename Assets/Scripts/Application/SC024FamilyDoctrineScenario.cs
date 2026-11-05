using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Fixed, deterministic SC-024 family-creed demonstration. It deliberately reuses
    /// the approved six-person SC-013 roster, birth years, stable person IDs and start
    /// date, but owns the independent event scenario gate <c>sc024</c> so the creed
    /// council rows are eligible only through this explicit demonstration entry.
    /// </summary>
    public static class SC024FamilyDoctrineScenario
    {
        public const string ScenarioId = "sc024";

        /// <summary>The approved SC-013 roster reused without copying people or kinship.</summary>
        public static SuccessionState CreateSuccession() => M2FixedScenario.CreateSuccession();

        /// <summary>Same opening date as the fixed M2/SC-013 new game.</summary>
        public static GameDate StartDate => WorldClock.NewGameDate;

        /// <summary>True only for the reused fixed Akechi family identity.</summary>
        public static bool IsActive(SuccessionState state) => M2FixedScenario.IsActive(state);
    }
}
