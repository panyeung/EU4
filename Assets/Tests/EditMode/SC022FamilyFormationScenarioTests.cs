using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-022 Task 2: the fixed family identity, roster and candidate split.</summary>
    public sealed class SC022FamilyFormationScenarioTests
    {
        [Test]
        public void Scenario_HasStableIdentityAndStartDate()
        {
            SuccessionState state = SC022FamilyFormationScenario.CreateSuccession();

            Assert.That(state.World.Family.Id, Is.EqualTo(SC022FamilyFormationScenario.FamilyId));
            Assert.That(state.World.Family.DisplayName,
                Is.EqualTo(SC022FamilyFormationScenario.FamilyName));
            Assert.That(state.HeadPersonId, Is.EqualTo(SC022FamilyFormationScenario.HeadId));
            Assert.That(state.DesignatedHeirId, Is.EqualTo(SC022FamilyFormationScenario.ChildId));
            Assert.That(SC022FamilyFormationScenario.StartDate, Is.EqualTo(new GameDate(1560, 5, 1)));
            Assert.That(SC022FamilyFormationScenario.IsActive(state), Is.True);
        }

        [Test]
        public void Scenario_RegistersCandidatesWithoutFamilyMembership()
        {
            SuccessionState state = SC022FamilyFormationScenario.CreateSuccession();
            foreach (PersonId candidate in SC022FamilyFormationScenario.AllScenarioCandidates)
            {
                Assert.That(state.World.TryGetPerson(candidate, out _), Is.True,
                    "candidate must be registered for saving");
                Assert.That(state.World.Family.IsMember(candidate), Is.False,
                    "candidate must not start as a family member");
            }
        }

        [Test]
        public void Scenario_ProvidesAtLeastTwoAdultMarriageCandidatesAndChildAndAdultAdoptionCandidates()
        {
            SuccessionState state = SC022FamilyFormationScenario.CreateSuccession();
            int adultMarriage = 0;
            foreach (PersonId candidate in SC022FamilyFormationScenario.MarriageCandidates)
            {
                state.World.TryGetLife(candidate, out SuccessionPersonLife life);
                if (life.Alive && life.AgeAt(SC022FamilyFormationScenario.StartDate) >=
                    state.AdulthoodAge)
                    adultMarriage++;
            }
            Assert.That(adultMarriage, Is.GreaterThanOrEqualTo(2));

            bool hasChild = false;
            bool hasAdult = false;
            foreach (PersonId candidate in SC022FamilyFormationScenario.AdoptionCandidates)
            {
                state.World.TryGetLife(candidate, out SuccessionPersonLife life);
                int age = life.AgeAt(SC022FamilyFormationScenario.StartDate);
                if (age < state.AdulthoodAge) hasChild = true;
                if (age >= state.AdulthoodAge) hasAdult = true;
            }
            Assert.That(hasChild, Is.True);
            Assert.That(hasAdult, Is.True);
        }

        [Test]
        public void Scenario_IsNotActiveForAnotherFamily()
        {
            Assert.That(SC022FamilyFormationScenario.IsActive(SuccessionSample.CreateStandard()),
                Is.False);
            Assert.That(SC022FamilyFormationScenario.IsActive(null), Is.False);
        }

        [Test]
        public void Scenario_NamesAreOriginalAndNeverRawIds()
        {
            Assert.That(SC022FamilyFormationScenario.PersonName(
                SC022FamilyFormationScenario.HeadId), Is.EqualTo(SC022FamilyFormationScenario.HeadName));
            Assert.That(SC022FamilyFormationScenario.HeadName, Does.Not.Contain("person."));
            Assert.That(SC022FamilyFormationScenario.FamilyName, Does.Not.Contain("family."));
        }
    }
}
