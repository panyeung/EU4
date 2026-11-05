using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 5: the fixed demo identity, budget and separate routing.</summary>
    public sealed class SC021FamilyResearchScenarioTests
    {
        [Test]
        public void Scenario_HasThreeLivingAdultsAndExactlyTwoMonthsOfResearchBudget()
        {
            SuccessionState succession = SC021FamilyResearchScenario.CreateSuccession();
            Assert.That(SC021FamilyResearchScenario.IsActive(succession), Is.True);
            Assert.That(succession.World.Family.Members.Count, Is.EqualTo(3));

            GameDate date = SC021FamilyResearchScenario.StartDate;
            foreach (FamilyMembership membership in succession.World.Family.Members)
            {
                Assert.That(succession.World.TryGetLife(membership.PersonId,
                    out SuccessionPersonLife life), Is.True);
                Assert.That(life.Alive, Is.True);
                Assert.That(life.Retired, Is.False);
                Assert.That(life.IsAdultAt(date, succession.AdulthoodAge), Is.True);
            }

            IndustryState industry = SC021FamilyResearchScenario.CreateIndustryState();
            Assert.That(industry.Resources.Funds, Is.EqualTo(2));
            Assert.That(industry.Resources.Supplies, Is.EqualTo(2));
            Assert.That(industry.Resources.Intelligence, Is.Zero);
            Assert.That(industry.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(date) - 1));
            Assert.That(industry.Assignments.All(assignment => assignment.PersonId.IsEmpty), Is.True);
        }

        [Test]
        public void SessionInitialization_UsesTheSeparateFixedScenario()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario(77UL);

                Assert.That(SC021FamilyResearchScenario.IsActive(session.Succession), Is.True);
                Assert.That(session.WorldClock.CurrentDate,
                    Is.EqualTo(SC021FamilyResearchScenario.StartDate));
                Assert.That(session.Industry.Resources.Funds, Is.EqualTo(2));
                Assert.That(session.Industry.Resources.Supplies, Is.EqualTo(2));
                Assert.That(session.ResearchState.HasPlan, Is.False);
                Assert.That(session.Research.EligibleResearchers, Has.Count.EqualTo(3));
                Assert.That(session.ChronicleEntries, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void ExistingScenarioFactories_RemainSeparate()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitNewGame();
                Assert.That(M2FixedScenario.IsActive(session.Succession), Is.True);
                Assert.That(SC021FamilyResearchScenario.IsActive(session.Succession), Is.False);

                session.InitStandardDemo();
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
                Assert.That(SC021FamilyResearchScenario.IsActive(session.Succession), Is.False);

                session.InitEducationDemoScenario();
                Assert.That(SC020EducationScenario.IsActive(session.Succession), Is.True);
                Assert.That(SC021FamilyResearchScenario.IsActive(session.Succession), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(session.gameObject);
            }
        }
    }
}
