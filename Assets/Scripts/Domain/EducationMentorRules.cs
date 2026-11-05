using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Shared education-mentor eligibility. The active rule is unchanged: a mentor must
    /// be a living, non-retired adult family member and must not be the learner. SC-023
    /// alone also admits a retired direct kin former head as a mentor, and it does so only
    /// for its own fixed family identity and only when the persisted succession chronicle
    /// proves that person actually left the head role in a real handoff.
    /// </summary>
    public static class EducationMentorRules
    {
        /// <summary>
        /// Stable SC-023 family identity. It lives in the domain so application routing and
        /// save validation compare the same single value instead of repeating a literal.
        /// </summary>
        public const string SC023SeclusionFamilyIdValue = "family.tachibana.sc023";

        public static bool IsSeclusionScenario(string familyId) =>
            string.Equals(familyId ?? string.Empty, SC023SeclusionFamilyIdValue,
                StringComparison.Ordinal);

        /// <summary>
        /// One rule used by the mentor list, preview, plan submission, save validation and
        /// annual settlement. Retirement is accepted only inside the SC-023 family, only
        /// for a <see cref="FamilyMemberRole.DirectKin"/> former head, and only when
        /// <paramref name="hasHeldHeadRole"/> is proven by succession history. A retired
        /// non-head direct kin is therefore never treated as a former head.
        /// </summary>
        public static bool IsEligible(string familyId, PersonId mentorId, PersonId learnerId,
            bool isMember, bool alive, bool retired, int age, int adulthoodAge,
            FamilyMemberRole role, bool hasHeldHeadRole)
        {
            if (mentorId.IsEmpty || mentorId == learnerId || !isMember || !alive ||
                age < adulthoodAge)
                return false;
            if (!retired)
                return true;
            return IsSeclusionScenario(familyId) && role == FamilyMemberRole.DirectKin &&
                   hasHeldHeadRole;
        }

        /// <summary>True when a stored plan's mentor may be kept even after eligibility loss.</summary>
        public static bool AllowsRetainedFallback(string familyId) => IsSeclusionScenario(familyId);
    }

    /// <summary>
    /// Derives stable succession facts from the append-only chronicle that is already
    /// part of every current save. No new save field or schema version is required.
    /// </summary>
    public static class SuccessionHistory
    {
        /// <summary>
        /// The exact persisted handoff shape that proves a head role: both stable IDs are
        /// present and they differ. Shared by the runtime ledger scan and save DTO scan.
        /// </summary>
        public static bool IsRealHeadHandoff(string fromHeadId, string toHeadId) =>
            !string.IsNullOrWhiteSpace(fromHeadId) &&
            !string.IsNullOrWhiteSpace(toHeadId) &&
            !string.Equals(fromHeadId, toHeadId, StringComparison.Ordinal);

        /// <summary>
        /// True when the chronicle contains a real head handoff whose departing head is
        /// <paramref name="personId"/>. This is the proof that a retired direct kin actually
        /// held the head role; mere retirement is not sufficient.
        /// </summary>
        public static bool HasHeldHeadRole(IReadOnlyList<FamilyChronicleEntry> entries,
            PersonId personId)
        {
            if (entries == null || personId.IsEmpty)
                return false;
            for (int i = 0; i < entries.Count; i++)
            {
                FamilyChronicleEntry entry = entries[i];
                if (entry != null && entry.Category == ChronicleCategory.Succession &&
                    IsRealHeadHandoff(entry.FromHeadId, entry.ToHeadId) &&
                    string.Equals(entry.FromHeadId, personId.Value, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}

