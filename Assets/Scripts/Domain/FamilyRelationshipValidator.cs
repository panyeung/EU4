using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>One SC-003 validation finding: stable IDs, family membership, kinship and relationship endpoint/type checks.</summary>
    public sealed class FamilyRelationshipDiagnostic
    {
        public FamilyRelationshipDiagnostic(string code, string message, string subject)
        {
            Code = code;
            Message = message;
            Subject = subject ?? string.Empty;
        }

        public string Code { get; }
        public string Message { get; }
        /// <summary>Person ID, Family ID, or "from|to" pair the finding refers to.</summary>
        public string Subject { get; }
        public override string ToString() => Code + ": " + Message + " [" + Subject + "]";
    }

    /// <summary>
    /// Cross-checks an untrusted SC-003 snapshot without throwing (same posture as CharacterDefinitionValidator):
    /// duplicate/empty IDs, blank names, unknown relationship endpoints, malformed or repeated structural facts.
    /// Head uniqueness (GDD 3.1 当代家主) is checked; counts of parents, spouse policy and succession rules
    /// are deliberately out of SC-003 scope.
    /// </summary>
    public static class FamilyRelationshipValidator
    {
        public static IReadOnlyList<FamilyRelationshipDiagnostic> Validate(
            IEnumerable<PersonState> persons,
            IEnumerable<FamilyState> families,
            KinshipGraph kinship,
            InterpersonalState relationships)
        {
            var diagnostics = new List<FamilyRelationshipDiagnostic>();
            var knownPersons = new HashSet<PersonId>();

            ValidatePersons(persons, diagnostics, knownPersons);
            ValidateFamilies(families, knownPersons, diagnostics);
            ValidateKinship(kinship, knownPersons, diagnostics);
            ValidateRelationships(relationships, knownPersons, diagnostics);
            return diagnostics.AsReadOnly();
        }

        private static void ValidatePersons(IEnumerable<PersonState> persons, List<FamilyRelationshipDiagnostic> diagnostics, HashSet<PersonId> knownPersons)
        {
            if (persons == null)
            {
                diagnostics.Add(new FamilyRelationshipDiagnostic("null_people", "Person collection is null.", string.Empty));
                return;
            }
            var seen = new HashSet<PersonId>();
            foreach (PersonState person in persons)
            {
                if (person == null)
                {
                    diagnostics.Add(new FamilyRelationshipDiagnostic("null_person", "Person entry is null.", string.Empty));
                    continue;
                }
                PersonId id = person.Id;
                if (id.IsEmpty)
                {
                    diagnostics.Add(new FamilyRelationshipDiagnostic("person_empty_id", "Person ID is empty.", id.ToString()));
                    continue;
                }
                if (!seen.Add(id))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("person_duplicate_id", "Person ID is duplicated.", id.ToString()));
                else
                    knownPersons.Add(id);
                if (string.IsNullOrWhiteSpace(person.DisplayName))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("person_invalid_name", "Display name is empty.", id.ToString()));
            }
        }

        private static void ValidateFamilies(IEnumerable<FamilyState> families, HashSet<PersonId> knownPersons, List<FamilyRelationshipDiagnostic> diagnostics)
        {
            if (families == null)
            {
                diagnostics.Add(new FamilyRelationshipDiagnostic("null_families", "Family collection is null.", string.Empty));
                return;
            }
            var seen = new HashSet<FamilyId>();
            foreach (FamilyState family in families)
            {
                if (family == null)
                {
                    diagnostics.Add(new FamilyRelationshipDiagnostic("null_family", "Family entry is null.", string.Empty));
                    continue;
                }
                FamilyId familyId = family.Id;
                if (familyId.IsEmpty)
                {
                    diagnostics.Add(new FamilyRelationshipDiagnostic("family_empty_id", "Family ID is empty.", familyId.ToString()));
                    continue;
                }
                if (!seen.Add(familyId))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("family_duplicate_id", "Family ID is duplicated.", familyId.ToString()));
                if (string.IsNullOrWhiteSpace(family.DisplayName))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("family_invalid_name", "Family display name is empty.", familyId.ToString()));

                int headCount = 0;
                var seenMembers = new HashSet<PersonId>();
                foreach (FamilyMembership membership in family.Members)
                {
                    string subject = familyId + "|" + membership.PersonId;
                    if (membership.PersonId.IsEmpty)
                        diagnostics.Add(new FamilyRelationshipDiagnostic("family_member_empty_id", "Membership person ID is empty.", subject));
                    else if (!seenMembers.Add(membership.PersonId))
                        diagnostics.Add(new FamilyRelationshipDiagnostic("family_duplicate_member", "Person has multiple memberships in this family.", subject));
                    if (!membership.PersonId.IsEmpty && !knownPersons.Contains(membership.PersonId))
                        diagnostics.Add(new FamilyRelationshipDiagnostic("family_unknown_member", "Family member ID is not a registered person.", subject));
                    if (membership.Role == FamilyMemberRole.Head)
                        headCount++;
                }
                if (headCount > 1)
                    diagnostics.Add(new FamilyRelationshipDiagnostic("family_multiple_heads", "Family has more than one Head member.", familyId.ToString()));
            }
        }

        private static void ValidateKinship(KinshipGraph kinship, HashSet<PersonId> knownPersons, List<FamilyRelationshipDiagnostic> diagnostics)
        {
            if (kinship == null)
            {
                diagnostics.Add(new FamilyRelationshipDiagnostic("null_kinship", "Kinship graph is null.", string.Empty));
                return;
            }
            var seen = new HashSet<KinshipLink>();
            foreach (KinshipLink link in kinship.Links)
            {
                string subject = link.From + "|" + link.To;
                if (link.From.IsEmpty || link.To.IsEmpty || link.From == link.To)
                    diagnostics.Add(new FamilyRelationshipDiagnostic("kinship_invalid_endpoints", "Kinship link must join two different non-empty person IDs.", subject));
                if (!Enum.IsDefined(typeof(KinshipType), link.Type))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("kinship_invalid_type", "Kinship type is undefined.", subject));
                if (!link.From.IsEmpty && !knownPersons.Contains(link.From))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("kinship_unknown_person", "Kinship start person is not a registered person.", subject));
                if (!link.To.IsEmpty && !knownPersons.Contains(link.To))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("kinship_unknown_person", "Kinship end person is not a registered person.", subject));
                if (!seen.Add(link))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("kinship_duplicate_link", "Kinship link is recorded twice.", subject));
            }
        }

        private static void ValidateRelationships(InterpersonalState relationships, HashSet<PersonId> knownPersons, List<FamilyRelationshipDiagnostic> diagnostics)
        {
            if (relationships == null)
            {
                diagnostics.Add(new FamilyRelationshipDiagnostic("null_relationships", "Interpersonal state is null.", string.Empty));
                return;
            }
            foreach (InterpersonalRecord record in relationships.Records)
            {
                string subject = record.From + "|" + record.To;
                if (record.From.IsEmpty || record.To.IsEmpty || record.From == record.To)
                    diagnostics.Add(new FamilyRelationshipDiagnostic("relationship_invalid_endpoints", "Interpersonal record must join two different non-empty person IDs.", subject));
                if (!record.From.IsEmpty && !knownPersons.Contains(record.From))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("relationship_unknown_person", "Interpersonal source person is not a registered person.", subject));
                if (!record.To.IsEmpty && !knownPersons.Contains(record.To))
                    diagnostics.Add(new FamilyRelationshipDiagnostic("relationship_unknown_person", "Interpersonal target person is not a registered person.", subject));
            }
        }
    }
}
