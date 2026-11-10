using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M04B Task 1: immutable person gender, validation and authored catalog.</summary>
    public sealed class PersonGenderTests
    {
        [Test]
        public void DefaultConstruction_IsUnspecified()
        {
            var person = new PersonState(new PersonId("person.test.default"), "无名");

            Assert.That(person.Gender, Is.EqualTo(PersonGender.Unspecified));
            Assert.That(person.DisplayName, Is.EqualTo("无名"));
        }

        [Test]
        public void ExplicitGender_SurvivesCaptureAndRestore()
        {
            SuccessionState state = BuildPair();
            var data = SuccessionSaveData.Capture(state);
            var date = new GameDate(1560, 4, 1);

            Assert.That(SuccessionSaveData.TryRestore(data, date,
                out SuccessionState restored, out string error), Is.True, error);
            Assert.That(restored.World.TryGetPerson(MaleId, out PersonState male), Is.True);
            Assert.That(restored.World.TryGetPerson(FemaleId, out PersonState female), Is.True);
            Assert.That(male.Gender, Is.EqualTo(PersonGender.Male));
            Assert.That(female.Gender, Is.EqualTo(PersonGender.Female));
        }

        [Test]
        public void UndefinedGender_CannotEnterTheWorld()
        {
            var world = new SuccessionWorld(new FamilyId("family.test.gender"), "测试家");
            bool added = world.TryAddPerson(
                new PersonState(new PersonId("person.test.bad"), "异常",
                    (PersonGender)99),
                new SuccessionPersonLife(1540, true, false), out string error);

            Assert.That(added, Is.False);
            Assert.That(error, Does.Contain("gender"));
            Assert.That(world.PersonCount, Is.Zero);
        }

        [Test]
        public void AuthoredCatalog_ResolvesOnlyExplicitScenarioIds()
        {
            Assert.That(PersonGenderCatalog.TryGet(M2FixedScenario.ChieId,
                out PersonGender chie), Is.True);
            Assert.That(chie, Is.EqualTo(PersonGender.Female));
            Assert.That(PersonGenderCatalog.TryGet(
                new PersonId("person.unknown.authored"), out _), Is.False);
            Assert.That(PersonGenderCatalog.GenderFor(
                new PersonId("person.unknown.authored")),
                Is.EqualTo(PersonGender.Unspecified));
        }

        [Test]
        public void FixedScenarios_SetAuthoredGendersAtConstruction()
        {
            AssertScenarioGenders(M2FixedScenario.CreateSuccession(),
                (M2FixedScenario.HarunobuId, PersonGender.Male),
                (M2FixedScenario.ChieId, PersonGender.Female),
                (M2FixedScenario.MasatsunaId, PersonGender.Male),
                (M2FixedScenario.NobumasaId, PersonGender.Male),
                (M2FixedScenario.TokihimeId, PersonGender.Female),
                (M2FixedScenario.SakonId, PersonGender.Male));

            AssertScenarioGenders(SuccessionSample.CreateStandard(),
                (SuccessionSample.StandardHeadId, PersonGender.Male),
                (new PersonId("person.akechi.chie"), PersonGender.Female));

            AssertScenarioGenders(SC020EducationScenario.CreateSuccession(),
                (SC020EducationScenario.HeadId, PersonGender.Male),
                (SC020EducationScenario.SpouseId, PersonGender.Female),
                (SC020EducationScenario.ChildId, PersonGender.Female));

            AssertScenarioGenders(SC021FamilyResearchScenario.CreateSuccession(),
                (SC021FamilyResearchScenario.HeadId, PersonGender.Male),
                (SC021FamilyResearchScenario.SpouseId, PersonGender.Female),
                (SC021FamilyResearchScenario.HeirId, PersonGender.Male));

            AssertScenarioGenders(SC022FamilyFormationScenario.CreateSuccession(),
                (SC022FamilyFormationScenario.HeadId, PersonGender.Male),
                (SC022FamilyFormationScenario.SpouseId, PersonGender.Female),
                (SC022FamilyFormationScenario.MarriageCandidateOneId, PersonGender.Female),
                (SC022FamilyFormationScenario.AdultAdoptionCandidateId, PersonGender.Male));

            AssertScenarioGenders(SC023SeclusionScenario.CreateSuccession(),
                (SC023SeclusionScenario.ElderId, PersonGender.Male),
                (SC023SeclusionScenario.ChildId, PersonGender.Female));

            AssertScenarioGenders(SC024FamilyDoctrineScenario.CreateSuccession(),
                (SC024FamilyDoctrineScenario.HeirId, PersonGender.Male));
        }

        [Test]
        public void CrisisAdoption_CreatesAnExplicitMalePerson()
        {
            SuccessionState state = SuccessionSample.CreateCrisis();
            var date = new GameDate(1560, 4, 1);
            Assert.That(SuccessionService.BeginRetirement(state, date).Succeeded, Is.True);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));

            SuccessionResult result = SuccessionService.AcceptCrisisAdoption(state, date);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(state.World.TryGetPerson(result.Subject, out PersonState adopted), Is.True);
            Assert.That(adopted.Gender, Is.EqualTo(PersonGender.Male));
        }

        private static readonly PersonId MaleId = new PersonId("person.test.male");
        private static readonly PersonId FemaleId = new PersonId("person.test.female");

        private static SuccessionState BuildPair()
        {
            var world = new SuccessionWorld(new FamilyId("family.test.pair"), "测试家");
            Add(world, MaleId, PersonGender.Male, FamilyMemberRole.Head);
            Add(world, FemaleId, PersonGender.Female, FamilyMemberRole.Spouse);
            Link(world, MaleId, FemaleId, KinshipType.Spouse);
            return new SuccessionState(world) { HeadPersonId = MaleId };
        }

        private static void Add(SuccessionWorld world, PersonId id, PersonGender gender,
            FamilyMemberRole role)
        {
            Assert.That(world.TryAddPerson(new PersonState(id, id.Value, gender),
                new SuccessionPersonLife(1530, true, false), out string error), Is.True, error);
            Assert.That(world.TryAddMember(id, role, out error), Is.True, error);
        }

        private static void Link(SuccessionWorld world, PersonId from, PersonId to,
            KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link,
                out string error), Is.True, error);
            Assert.That(world.TryAddKinship(link, out error), Is.True, error);
        }

        private static void AssertScenarioGenders(SuccessionState state,
            params (PersonId Id, PersonGender Gender)[] expected)
        {
            foreach ((PersonId id, PersonGender gender) in expected)
            {
                Assert.That(state.World.TryGetPerson(id, out PersonState person), Is.True,
                    "missing " + id.Value);
                Assert.That(person.Gender, Is.EqualTo(gender), id.Value);
            }
        }
    }
}
