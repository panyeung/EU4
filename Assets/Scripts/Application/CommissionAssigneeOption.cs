using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Read-only view of one living current-family member as a possible escort
    /// assignee. <see cref="Reason"/> is a short label for a list row and is
    /// produced by the same GameSession check that gates
    /// <see cref="GameSession.TryAssignCommission"/>; the UI never re-derives
    /// eligibility itself.
    /// </summary>
    public readonly struct CommissionAssigneeOption
    {
        public CommissionAssigneeOption(PersonId personId, string displayName, bool eligible,
            string reason)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Eligible = eligible;
            Reason = reason ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }

        /// <summary>True only while the person can start the escort immediately.</summary>
        public bool Eligible { get; }

        /// <summary>Short status such as "可指派", "有进行中的每日行动" or "负责产业".</summary>
        public string Reason { get; }
    }
}
