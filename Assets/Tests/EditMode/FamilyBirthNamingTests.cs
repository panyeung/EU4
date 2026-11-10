using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M04B Task 2: stable newborn IDs, sex-specific original names and collisions.</summary>
    public sealed class FamilyBirthNamingTests
    {
        private static readonly PersonId Father = new PersonId("person.test.father");
        private static readonly PersonId Mother = new PersonId("person.test.mother");
        private static readonly GameDate Date = new GameDate(1561, 1, 1);

        [Test]
        public void SameFamilyInputs_ProduceTheSameIdAndName()
        {
            SuccessionState state = BuildFamily();

            FamilyBirthName first = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Female);
            FamilyBirthName second = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Female);

            Assert.That(first.Succeeded, Is.True, first.Error);
            Assert.That(second.Succeeded, Is.True, second.Error);
            Assert.That(first.PersonId, Is.EqualTo(second.PersonId));
            Assert.That(first.DisplayName, Is.EqualTo(second.DisplayName));
            Assert.That(first.PersonId.Value, Does.StartWith("person.birth."));
            Assert.That(first.DisplayName, Is.Not.Empty);
        }

        [Test]
        public void ParentOrder_DoesNotChangeTheIdentity()
        {
            SuccessionState state = BuildFamily();

            FamilyBirthName ab = FamilyBirthNaming.Create(state, Father, Mother, Date,
                PersonGender.Male);
            FamilyBirthName ba = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Male);

            Assert.That(ab.Succeeded, Is.True, ab.Error);
            Assert.That(ba.Succeeded, Is.True, ba.Error);
            Assert.That(ab.PersonId, Is.EqualTo(ba.PersonId));
            Assert.That(ab.DisplayName, Is.EqualTo(ba.DisplayName));
        }

        [Test]
        public void GenderSpecificNames_AreStableAndDistinct()
        {
            SuccessionState state = BuildFamily();

            FamilyBirthName male = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Male);
            FamilyBirthName female = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Female);

            Assert.That(male.Succeeded, Is.True, male.Error);
            Assert.That(female.Succeeded, Is.True, female.Error);
            Assert.That(male.PersonId, Is.EqualTo(female.PersonId),
                "identity stays stable; only the name table is sex-specific");
            Assert.That(male.DisplayName, Is.Not.EqualTo(female.DisplayName));
        }

        [Test]
        public void ExistingDisplayName_GetsAStableSuffix()
        {
            SuccessionState state = BuildFamily();
            FamilyBirthName baseline = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Male);
            Assert.That(baseline.Succeeded, Is.True, baseline.Error);

            Assert.That(state.World.TryAddPerson(new PersonState(
                    new PersonId("person.test.blocker"), baseline.DisplayName,
                    PersonGender.Unspecified),
                new SuccessionPersonLife(1540, true, false), out string error), Is.True, error);

            FamilyBirthName suffixed = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Male);

            Assert.That(suffixed.Succeeded, Is.True, suffixed.Error);
            Assert.That(suffixed.PersonId, Is.EqualTo(baseline.PersonId));
            Assert.That(suffixed.DisplayName, Is.EqualTo(baseline.DisplayName + " 2"));
        }

        [Test]
        public void ExistingId_GetsAStableSuffix()
        {
            SuccessionState state = BuildFamily();
            FamilyBirthName baseline = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Female);
            Assert.That(baseline.Succeeded, Is.True, baseline.Error);
            Assert.That(state.World.TryAddPerson(new PersonState(baseline.PersonId, "占位",
                    PersonGender.Unspecified),
                new SuccessionPersonLife(1540, true, false), out string error), Is.True, error);

            FamilyBirthName suffixed = FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Female);

            Assert.That(suffixed.Succeeded, Is.True, suffixed.Error);
            Assert.That(suffixed.PersonId, Is.Not.EqualTo(baseline.PersonId));
            Assert.That(suffixed.PersonId.Value, Does.StartWith(baseline.PersonId.Value + "."));
        }

        [Test]
        public void InvalidGenderOrParents_ReturnAReason()
        {
            SuccessionState state = BuildFamily();

            Assert.That(FamilyBirthNaming.Create(state, Mother, Father, Date,
                PersonGender.Unspecified).Succeeded, Is.False);
            Assert.That(FamilyBirthNaming.Create(state, Mother, Mother, Date,
                PersonGender.Male).Succeeded, Is.False);
            Assert.That(FamilyBirthNaming.Create(state, Mother,
                new PersonId("person.test.missing"), Date, PersonGender.Male).Succeeded,
                Is.False);
        }

        private static SuccessionState BuildFamily()
        {
            var world = new SuccessionWorld(new FamilyId("family.test.naming"), "测试家");
            Add(world, Father, PersonGender.Male, FamilyMemberRole.Head);
            Add(world, Mother, PersonGender.Female, FamilyMemberRole.Spouse);
            Assert.That(KinshipLink.TryCreate(Father, Mother, KinshipType.Spouse,
                out KinshipLink link, out string error), Is.True, error);
            Assert.That(world.TryAddKinship(link, out error), Is.True, error);
            return new SuccessionState(world) { HeadPersonId = Father };
        }

        private static void Add(SuccessionWorld world, PersonId id, PersonGender gender,
            FamilyMemberRole role)
        {
            Assert.That(world.TryAddPerson(new PersonState(id, id.Value, gender),
                new SuccessionPersonLife(1530, true, false), out string error), Is.True, error);
            Assert.That(world.TryAddMember(id, role, out error), Is.True, error);
        }
    }
}
