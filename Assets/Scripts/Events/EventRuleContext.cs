namespace SengokuChronicle.Events
{
    /// <summary>
    /// Small, immutable snapshot of campaign facts that event eligibility may read.
    /// <para>
    /// The two succession counts are offer-time gates only. <c>GameSession</c> supplies a
    /// context to both scheduling and commit, but <see cref="EventEngine"/> evaluates the
    /// succession conditions only during eligibility/scheduling and ignores them at commit.
    /// That keeps a stable saved decision resolvable after a later head change; history
    /// conditions are still re-checked at commit.
    /// </para>
    /// </summary>
    public readonly struct EventRuleContext
    {
        public EventRuleContext(int successionCount, int settledSuccessionCount)
            : this(successionCount, settledSuccessionCount, string.Empty)
        {
        }

        public EventRuleContext(int successionCount, int settledSuccessionCount, string scenarioId)
        {
            SuccessionCount = successionCount;
            SettledSuccessionCount = settledSuccessionCount;
            ScenarioId = scenarioId ?? string.Empty;
        }

        /// <summary>Raw SC-010 count; increments on every handoff, including a regency start.</summary>
        public int SuccessionCount { get; }

        /// <summary>
        /// Active scenario ID. Scenario-tagged catalog rows are eligible only when this
        /// matches their <c>scenario</c> field; empty keeps every scenario-neutral row.
        /// </summary>
        public string ScenarioId { get; }

        /// <summary>
        /// The raw count only while <c>SuccessionPhase.None</c>; zero during a pending
        /// succession, a regency, or extinction.
        /// </summary>
        public int SettledSuccessionCount { get; }

        /// <summary>Zero counts: the generation-one state.</summary>
        public static EventRuleContext Default => default;
    }
}
