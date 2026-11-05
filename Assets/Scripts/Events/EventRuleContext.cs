using SengokuChronicle.Domain;

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
            : this(successionCount, settledSuccessionCount, string.Empty, default, null)
        {
        }

        public EventRuleContext(int successionCount, int settledSuccessionCount, string scenarioId)
            : this(successionCount, settledSuccessionCount, scenarioId, default, null)
        {
        }

        public EventRuleContext(int successionCount, int settledSuccessionCount, string scenarioId,
            PersonId headPersonId, PersonId[] familyMemberIds)
        {
            SuccessionCount = successionCount;
            SettledSuccessionCount = settledSuccessionCount;
            ScenarioId = scenarioId ?? string.Empty;
            HeadPersonId = headPersonId;
            FamilyMemberIds = familyMemberIds ?? new PersonId[0];
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

        /// <summary>Current head used as the relationship source for family effects.</summary>
        public PersonId HeadPersonId { get; }

        /// <summary>All family members eligible as family-effect targets; empty means unavailable.</summary>
        public PersonId[] FamilyMemberIds { get; }

        /// <summary>Zero counts: the generation-one state.</summary>
        public static EventRuleContext Default => default;
    }
}
