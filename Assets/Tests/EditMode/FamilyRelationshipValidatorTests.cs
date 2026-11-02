using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class FamilyRelationshipValidatorTests
    {
        private static PersonId Person(string raw) => new PersonId(raw);
        private static FamilyId Family(string raw) => new FamilyId(raw);

        private static void BuildCleanModel(
            out List<PersonState> persons,
            out List<FamilyState> families,
            out KinshipGraph kinship,
            out InterpersonalState relationships)
        {
            persons = new List<PersonState>
            {
                new PersonState(Person("p.head"), "当主"),
                new PersonState(Person("p.child"), "子"),
                new PersonState(Person("p.vassal"), "家臣")
            };
            families = new List<FamilyState>();
            var family = new FamilyState(Family("f.main"), "本家");
            string error;
            Assert.That(family.TryAddMember(Person("p.head"), FamilyMemberRole.Head, out error), Is.True, error);
            Assert.That(family.TryAddMember(Person("p.child"), FamilyMemberRole.DirectKin, out error), Is.True, error);
            Assert.That(family.TryAddMember(Person("p.vassal"), FamilyMemberRole.Retainer, out error), Is.True, error);
            families.Add(family);

            kinship = new KinshipGraph();
            Assert.That(kinship.TryAdd(new KinshipLink(Person("p.head"), Person("p.child"), KinshipType.ParentChild), out error), Is.True, error);
            Assert.That(kinship.TryAdd(new KinshipLink(Person("p.head"), Person("p.child"), KinshipType.Adoption), out error), Is.True, error);

            relationships = new InterpersonalState();
            Assert.That(relationships.TryChange(Person("p.child"), Person("p.head"), InterpersonalAxis.Obligation, 50).Succeeded, Is.True);
        }

        [Test]
        public void Validate_CleanModelProducesNoDiagnostics()
        {
            BuildCleanModel(out List<PersonState> persons, out List<FamilyState> families, out KinshipGraph kinship, out InterpersonalState relationships);
            IReadOnlyList<FamilyRelationshipDiagnostic> diagnostics =
                FamilyRelationshipValidator.Validate(persons, families, kinship, relationships);
            Assert.That(diagnostics, Is.Empty, string.Join(" | ", System.Linq.Enumerable.Select(diagnostics, d => d.ToString())));
        }

        [Test]
        public void Validate_ReportsNullCollectionsWithoutThrowing()
        {
            IReadOnlyList<FamilyRelationshipDiagnostic> diagnostics =
                FamilyRelationshipValidator.Validate(null, null, null, null);
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("null_people"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("null_families"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("null_kinship"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("null_relationships"));
        }

        [Test]
        public void Validate_ReportsEmptyDuplicateAndUnnamedPersonIds()
        {
            BuildCleanModel(out List<PersonState> persons, out List<FamilyState> families, out KinshipGraph kinship, out InterpersonalState relationships);
            persons.Add(new PersonState(default, "空ID"));
            persons.Add(new PersonState(Person("p.head"), "重复ID"));
            persons.Add(new PersonState(Person("p.nameless"), "   "));
            persons.Add(null);

            IReadOnlyList<FamilyRelationshipDiagnostic> diagnostics =
                FamilyRelationshipValidator.Validate(persons, families, kinship, relationships);
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("person_empty_id"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("person_duplicate_id"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("person_invalid_name"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("null_person"));
        }

        [Test]
        public void Validate_ReportsFamilyIdAndMembershipProblems()
        {
            BuildCleanModel(out List<PersonState> persons, out List<FamilyState> families, out KinshipGraph kinship, out InterpersonalState relationships);

            var duplicateFamily = new FamilyState(Family("f.main"), "重复ID本家");
            var ghostFamily = new FamilyState(Family("f.ghost"), "幽灵家");
            string error;
            Assert.That(ghostFamily.TryAddMember(Person("p.nobody"), FamilyMemberRole.DirectKin, out error), Is.True, error);
            var twoHeadFamily = new FamilyState(Family("f.split"), "二当主家");
            Assert.That(twoHeadFamily.TryAddMember(Person("p.head"), FamilyMemberRole.Head, out error), Is.True, error);
            Assert.That(twoHeadFamily.TryAddMember(Person("p.child"), FamilyMemberRole.Head, out error), Is.True, error);
            var unnamedFamily = new FamilyState(Family("f.noname"), " ");
            Assert.That(unnamedFamily.TryAddMember(Person("p.vassal"), FamilyMemberRole.Guest, out error), Is.True, error);

            families.Add(duplicateFamily);
            families.Add(ghostFamily);
            families.Add(twoHeadFamily);
            families.Add(unnamedFamily);

            IReadOnlyList<FamilyRelationshipDiagnostic> diagnostics =
                FamilyRelationshipValidator.Validate(persons, families, kinship, relationships);
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("family_duplicate_id"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("family_unknown_member"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("family_multiple_heads"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("family_invalid_name"));
        }

        [Test]
        public void Validate_ReportsKinshipAndRelationshipEndpointProblems()
        {
            BuildCleanModel(out List<PersonState> persons, out List<FamilyState> families, out KinshipGraph kinship, out InterpersonalState relationships);

            // Registered persons keep the graph valid; unregistered ghost endpoints are the defect under test.
            string error;
            Assert.That(kinship.TryAdd(new KinshipLink(Person("p.head"), Person("p.ghost"), KinshipType.Sibling), out error), Is.True, error);
            Assert.That(relationships.TryChange(Person("p.ghost"), Person("p.head"), InterpersonalAxis.Trust, 10).Succeeded, Is.True);

            IReadOnlyList<FamilyRelationshipDiagnostic> diagnostics =
                FamilyRelationshipValidator.Validate(persons, families, kinship, relationships);
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("kinship_unknown_person"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("relationship_unknown_person"));
            Assert.That(System.Linq.Enumerable.Any(diagnostics, d => d.Subject.Contains("p.ghost")), Is.True);
        }

        [Test]
        public void Validate_MutableChangeNeverInvalidatesStructuralFacts()
        {
            BuildCleanModel(out List<PersonState> persons, out List<FamilyState> families, out KinshipGraph kinship, out InterpersonalState relationships);

            relationships.TryChange(Person("p.child"), Person("p.head"), InterpersonalAxis.Affection, -100);
            relationships.TryRecordBetrayal(Person("p.child"), Person("p.head"), out string error);

            IReadOnlyList<FamilyRelationshipDiagnostic> before =
                FamilyRelationshipValidator.Validate(persons, families, kinship, relationships);
            Assert.That(System.Linq.Enumerable.Any(before, d => d.Code.StartsWith("kinship")), Is.False,
                "Mutable sentiment must not corrupt kinship validation");
            Assert.That(kinship.Count, Is.EqualTo(2));
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void Diagnostics_CollectionIsReadOnlyAndSelfDescribing()
        {
            IReadOnlyList<FamilyRelationshipDiagnostic> diagnostics =
                FamilyRelationshipValidator.Validate(null, new List<FamilyState>(), new KinshipGraph(), new InterpersonalState());
            Assert.That(diagnostics, Is.Not.Empty);
            IList<FamilyRelationshipDiagnostic> list = diagnostics as IList<FamilyRelationshipDiagnostic>;
            Assert.That(list, Is.Not.Null);
            Assert.That(list.IsReadOnly, Is.True);
            Assert.Throws<System.NotSupportedException>(() => list[0] = null);
            Assert.That(diagnostics[0].ToString(), Does.StartWith(diagnostics[0].Code));
        }
    }
}
