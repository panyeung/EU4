using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class PersonFamilyIdentityTests
    {
        [TestCase("person.first")]
        [TestCase("npc.kuroda.iori")]
        [TestCase("p-7")]
        public void PersonId_AcceptsAnyNonBlankTextAsDynamicStableId(string raw)
        {
            Assert.That(PersonId.TryParse(raw, out PersonId id), Is.True);
            Assert.That(id.Value, Is.EqualTo(raw));
            Assert.That(id.IsEmpty, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        public void PersonId_RejectsEmptyOrWhitespace(string raw)
        {
            Assert.That(PersonId.TryParse(raw, out PersonId id), Is.False);
            Assert.That(id.IsEmpty, Is.True);
            Assert.That(default(PersonId).IsEmpty, Is.True);
        }

        [Test]
        public void PersonId_ComparesOrdinallyAndServesAsDictionaryKey()
        {
            var upper = new PersonId("Person.A");
            var lower = new PersonId("person.a");
            Assert.That(upper, Is.Not.EqualTo(lower));
            Assert.That(upper == lower, Is.False);
            Assert.That(new PersonId("x") == new PersonId("x"), Is.True);

            var map = new Dictionary<PersonId, int> { { new PersonId("x"), 1 } };
            Assert.That(map[new PersonId("x")], Is.EqualTo(1));
            Assert.That(map.ContainsKey(new PersonId("X")), Is.False);
            Assert.That(new PersonId("x").ToString(), Is.EqualTo("x"));
            Assert.That(default(PersonId).ToString(), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void FamilyId_RejectsEmptyOrWhitespace(string raw)
        {
            Assert.That(FamilyId.TryParse(raw, out FamilyId id), Is.False);
            Assert.That(id.IsEmpty, Is.True);
        }

        [Test]
        public void FamilyId_AcceptsNonBlankTextAndComparesOrdinally()
        {
            Assert.That(FamilyId.TryParse("family.kuroda", out FamilyId parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(new FamilyId("family.kuroda")));
            Assert.That(new FamilyId("family.a"), Is.Not.EqualTo(new FamilyId("family.A")));
        }

        [Test]
        public void PersonState_RenameKeepsPermanentIdAndRejectsBlankName()
        {
            var person = new PersonState(new PersonId("p.head"), "初代");
            Assert.That(person.TryRename("改名后的名字", out string error), Is.True, error);
            Assert.That(person.Id, Is.EqualTo(new PersonId("p.head")), "Permanent ID must survive rename (GDD 3.1)");
            Assert.That(person.DisplayName, Is.EqualTo("改名后的名字"));

            Assert.That(person.TryRename("  ", out error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(person.DisplayName, Is.EqualTo("改名后的名字"), "Failed rename leaves state untouched");
            Assert.That(person.Id, Is.EqualTo(new PersonId("p.head")));
        }

        [Test]
        public void FamilyState_MembershipOperationsRejectInvalidInputWithoutMutation()
        {
            var family = new FamilyState(new FamilyId("f.kuroda"), "黒田家");
            var person = new PersonId("p.a");

            Assert.That(family.TryAddMember(person, FamilyMemberRole.DirectKin, out string error), Is.True, error);
            Assert.That(family.TryAddMember(person, FamilyMemberRole.Head, out error), Is.False, "Duplicate membership must be rejected");
            Assert.That(family.MemberCount, Is.EqualTo(1));
            Assert.That(family.TryGetRole(person, out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.DirectKin));

            Assert.That(family.TryAddMember(default, FamilyMemberRole.Head, out error), Is.False);
            Assert.That(family.MemberCount, Is.EqualTo(1), "Failed add leaves roster untouched");
            Assert.That(family.TryAddMember(new PersonId("p.badrole"), (FamilyMemberRole)99, out error), Is.False);
            Assert.That(family.TrySetRole(new PersonId("p.nobody"), FamilyMemberRole.Head, out error), Is.False, "Unknown member cannot receive a role");

            Assert.That(family.TrySetRole(person, FamilyMemberRole.Head, out error), Is.True, error);
            Assert.That(family.TryGetRole(person, out role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Head));
            Assert.That(family.TryRemoveMember(person, out error), Is.True, error);
            Assert.That(family.TryRemoveMember(person, out error), Is.False);
            Assert.That(family.IsMember(person), Is.False);
        }

        [Test]
        public void FamilyState_PersonMovingFamiliesKeepsSamePermanentPersonId()
        {
            var person = new PersonState(new PersonId("p.spouse"), "配偶");
            var birth = new FamilyState(new FamilyId("f.birth"), "実家");
            var married = new FamilyState(new FamilyId("f.married"), "婚家");

            Assert.That(birth.TryAddMember(person.Id, FamilyMemberRole.DirectKin, out string error), Is.True, error);
            Assert.That(birth.TryRemoveMember(person.Id, out error), Is.True, error);
            Assert.That(married.TryAddMember(person.Id, FamilyMemberRole.Spouse, out error), Is.True, error);

            Assert.That(person.Id, Is.EqualTo(new PersonId("p.spouse")), "Marriage must not change the person ID (GDD 3.1)");
            Assert.That(married.IsMember(new PersonId("p.spouse")), Is.True);
            Assert.That(birth.IsMember(new PersonId("p.spouse")), Is.False);
            Assert.That(married.Id, Is.EqualTo(new FamilyId("f.married")));
            Assert.That(married.TryRename("新姓", out error), Is.True, error);
            Assert.That(married.Id, Is.EqualTo(new FamilyId("f.married")), "Family ID must survive renaming");
        }

        [Test]
        public void FamilyState_MembersSnapshotIsSortedAndDetached()
        {
            var family = new FamilyState(new FamilyId("f.one"), "一家");
            Assert.That(family.TryAddMember(new PersonId("p.b"), FamilyMemberRole.Disciple, out string error), Is.True, error);
            Assert.That(family.TryAddMember(new PersonId("p.a"), FamilyMemberRole.Retainer, out error), Is.True, error);

            IReadOnlyList<FamilyMembership> first = family.Members;
            Assert.That(first.Count, Is.EqualTo(2));
            Assert.That(first[0].PersonId, Is.EqualTo(new PersonId("p.a")));
            Assert.That(first[1].PersonId, Is.EqualTo(new PersonId("p.b")));

            Assert.That(family.TryAddMember(new PersonId("p.c"), FamilyMemberRole.Guest, out error), Is.True, error);
            Assert.That(first.Count, Is.EqualTo(2), "Snapshot must not change after mutation");
        }
    }
}
