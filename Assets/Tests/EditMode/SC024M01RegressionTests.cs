using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M01/SC-024 regression tests: route-specific retirement, the approved
    /// SC-024-only fourth-generation child, its education target, the save round trip,
    /// the shared third-generation finale, and the rule that SC-024 family data must not
    /// change ordinary SC-013/no-route behavior.
    /// They were executed in the open Unity Editor for the M01 finale routing repair and
    /// again for the M01/SC-024 route-boundary repair.
    /// </summary>
    public sealed class SC024M01RegressionTests
    {
        private const int ChildStartAge = 10;
        private const string ChildIdValue = "person.sc024.nobumasa-heir";
        private const string ChildName = "明智 景继";

        private static readonly PersonId ChildId = new PersonId(ChildIdValue);
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        private readonly List<GameSession> _sessions = new List<GameSession>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameSession session in _sessions)
                if (session != null)
                    Object.DestroyImmediate(session.gameObject);
            _sessions.Clear();
        }

        private GameSession NewSession()
        {
            GameSession session = GameSession.NewGame();
            _sessions.Add(session);
            return session;
        }

        private GameSession NewSc024Session()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario(173UL);
            return session;
        }

        [Test]
        public void Sc024_RetirementIsBlockedUntilItsOwnCouncilCommits()
        {
            GameSession session = NewSc024Session();

            Assert.That(session.CanBeginSuccessionRetirement, Is.False);
            Assert.That(session.SuccessionRetirementBlockReason, Does.Contain("家训议事"));
            Assert.That(session.SuccessionRetirementBlockReason, Does.Not.Contain("继承前的家议"));
            Assert.That(session.SuccessionRetirementBlockReason, Does.Not.Contain("雨夜来客"));
        }

        [Test]
        public void Sc024_FirstRetirementIsAllowedAfterItsOwnFamilyCouncilWithoutSc013Gates()
        {
            GameSession session = NewSc024Session();

            Advance(session);
            Assert.That(session.PendingEventId,
                Is.EqualTo(SC024EventIds.RighteousnessCouncilEventId));
            Choose(session, SC024EventIds.RighteousnessKeepOptionId);

            Assert.That(session.PendingEventId, Is.Empty);
            Assert.That(session.SuccessionRetirementBlockReason,
                Does.Not.Contain("继承前的家议"),
                "the SC-024 route must not ask for the SC-013 inheritance pledge");
            Assert.That(session.SuccessionRetirementBlockReason,
                Does.Not.Contain("雨夜来客"),
                "the SC-024 route must not wait for the SC-013-only rainy-arrival gate");
            Assert.That(session.CanBeginSuccessionRetirement, Is.True,
                session.SuccessionRetirementBlockReason);

            SuccessionResult result = session.BeginSuccessionRetirement();

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.MasatsunaId));
        }

        [Test]
        public void Sc013_RetirementStillRequiresItsExistingPledgeAndRainyArrivalGates()
        {
            GameSession session = NewSession();
            session.InitNewGame();

            Advance(session);
            Assert.That(session.PendingEventId, Is.EqualTo(SC013EventIds.PledgeEventId));
            Choose(session, SC013EventIds.KeepPromiseOptionId);

            Assert.That(session.CanBeginSuccessionRetirement, Is.False,
                "SC-013 must still require the rainy-arrival choice after the pledge");
            Assert.That(session.SuccessionRetirementBlockReason, Does.Contain("雨夜来客"));

            Advance(session);
            Assert.That(session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            Choose(session, SampleEventIds.OfferRestOptionId);

            Assert.That(session.CanBeginSuccessionRetirement, Is.True,
                session.SuccessionRetirementBlockReason);
        }

        [Test]
        public void Sc024_BothExistingSuccessionsStartWithoutTheSc013RainyOrLegacyGates()
        {
            GameSession session = NewSc024Session();

            Advance(session);
            Choose(session, SC024EventIds.RighteousnessKeepOptionId);

            SuccessionResult first = session.BeginSuccessionRetirement();
            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.MasatsunaId));

            // No SC-011 rainy-arrival legacy choice has been made, so this can only
            // pass when the SC-024 route is independent of the SC-013 chain.
            Assert.That(session.CanBeginSuccessionRetirement, Is.True,
                session.SuccessionRetirementBlockReason);
            SuccessionResult second = session.BeginSuccessionRetirement();

            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(session.Succession.SuccessionCount, Is.EqualTo(2));
            Assert.That(session.Succession.DesignatedHeirId,
                Is.EqualTo(SC024FamilyDoctrineScenario.HeirId));
        }

        [Test]
        public void Sc024_AddsOnlyItsOwnFourthGenerationChild()
        {
            SuccessionState standard = M2FixedScenario.CreateSuccession();
            Assert.That(standard.World.PersonCount, Is.EqualTo(6),
                "the ordinary SC-013 roster must remain the approved six people");
            Assert.That(standard.World.TryGetPerson(ChildId, out _), Is.False);

            SuccessionState sc024 = SC024FamilyDoctrineScenario.CreateSuccession();
            Assert.That(sc024.World.PersonCount, Is.EqualTo(7));
            Assert.That(sc024.World.Family.MemberCount, Is.EqualTo(7));
            Assert.That(sc024.World.TryGetPerson(ChildId, out PersonState child), Is.True);
            Assert.That(child.DisplayName, Is.EqualTo(ChildName));
            Assert.That(sc024.World.TryGetLife(ChildId, out SuccessionPersonLife life), Is.True);
            Assert.That(life.BirthYear, Is.EqualTo(Start.Year - ChildStartAge));
            Assert.That(life.AgeAt(Start), Is.EqualTo(ChildStartAge));
            Assert.That(sc024.World.Family.TryGetRole(ChildId, out FamilyMemberRole role),
                Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.DirectKin));
            Assert.That(sc024.World.Kinship.Contains(new KinshipLink(
                M2FixedScenario.NobumasaId, ChildId, KinshipType.ParentChild)), Is.True,
                "the fourth-generation child must be linked from third-generation Nobumasa");
            Assert.That(sc024.DesignatedHeirId, Is.EqualTo(M2FixedScenario.MasatsunaId),
                "the child must not displace the first opening designation");
        }

        [Test]
        public void Sc024_FamilyDataKeepsSc013RulesWhenTheRouteIsNotSc024()
        {
            // The state deliberately carries the SC-024-only seventh person. The route,
            // not the presence of that data, decides the fixed line and the roster.
            SuccessionState state = SC024FamilyDoctrineScenario.CreateSuccession();
            Assert.That(state.World.PersonCount, Is.EqualTo(7));
            Assert.That(SC024FamilyDoctrineScenario.HasHeir(state), Is.True);

            Assert.That(M2FixedScenario.ExpectedHeir(state, EventScenarioIds.Sc013),
                Is.EqualTo(M2FixedScenario.MasatsunaId));

            state.HeadPersonId = M2FixedScenario.MasatsunaId;
            Assert.That(M2FixedScenario.ExpectedHeir(state, EventScenarioIds.Sc013),
                Is.EqualTo(M2FixedScenario.NobumasaId));

            // On the ordinary route the third generation has no fixed heir, even though
            // the SC-024 child exists in the world data.
            state.HeadPersonId = M2FixedScenario.NobumasaId;
            M2FixedScenario.SyncDesignation(state, EventScenarioIds.Sc013);
            Assert.That(M2FixedScenario.ExpectedHeir(state, EventScenarioIds.Sc013),
                Is.EqualTo(default(PersonId)));
            Assert.That(M2FixedScenario.ExpectedHeir(state), Is.EqualTo(default(PersonId)));
            Assert.That(state.DesignatedHeirId.IsEmpty, Is.True);

            IReadOnlyList<M2RosterRow> sc013Rows = M2FixedScenario.BuildRoster(state, Start,
                state.DesignatedHeirId, EventScenarioIds.Sc013);
            Assert.That(sc013Rows.Count, Is.EqualTo(6),
                "ordinary SC-013 keeps the approved six-row roster");
            Assert.That(sc013Rows.Any(row => row.PersonId == SC024FamilyDoctrineScenario.HeirId),
                Is.False);
            Assert.That(sc013Rows.Count(row => row.GenerationLabel == "第四代"), Is.Zero);

            IReadOnlyList<M2RosterRow> noRouteRows = M2FixedScenario.BuildRoster(state, Start,
                state.DesignatedHeirId);
            Assert.That(noRouteRows.Count, Is.EqualTo(6),
                "no route also keeps the approved six-row roster");

            // The same family data on the sc024 route still designates and shows the child.
            Assert.That(M2FixedScenario.ExpectedHeir(state, SC024FamilyDoctrineScenario.ScenarioId),
                Is.EqualTo(SC024FamilyDoctrineScenario.HeirId));
            M2FixedScenario.SyncDesignation(state, SC024FamilyDoctrineScenario.ScenarioId);
            Assert.That(state.DesignatedHeirId, Is.EqualTo(SC024FamilyDoctrineScenario.HeirId));

            IReadOnlyList<M2RosterRow> sc024Rows = M2FixedScenario.BuildRoster(state, Start,
                state.DesignatedHeirId, SC024FamilyDoctrineScenario.ScenarioId);
            Assert.That(sc024Rows.Count, Is.EqualTo(7));
            M2RosterRow childRow = sc024Rows.Last();
            Assert.That(childRow.PersonId, Is.EqualTo(SC024FamilyDoctrineScenario.HeirId));
            Assert.That(childRow.GenerationLabel,
                Is.EqualTo(SC024FamilyDoctrineScenario.HeirGenerationLabel));
            Assert.That(childRow.IsDesignatedHeir, Is.True);
        }

        [Test]
        public void Sc024_ChildBecomesDesignatedHeirOnlyAfterTheTwoExistingSuccessions()
        {
            GameSession initial = NewSc024Session();
            Assert.That(initial.Succession.World.PersonCount, Is.EqualTo(7));
            Assert.That(initial.Succession.DesignatedHeirId,
                Is.EqualTo(M2FixedScenario.MasatsunaId),
                "the opening designation is still Masatsuna, not the fourth-generation child");

            GameSession session = DriveToThirdGeneration();

            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(session.Succession.SuccessionCount, Is.EqualTo(2));
            Assert.That(session.Succession.DesignatedHeirId, Is.EqualTo(ChildId),
                "only the third-generation head may designate the fourth-generation child");
            Assert.That(session.Succession.World.TryGetPerson(ChildId, out PersonState child),
                Is.True);
            Assert.That(child.DisplayName, Is.EqualTo(ChildName));

            M2RosterRow row = session.FamilyRoster.Single(item => item.PersonId == ChildId);
            Assert.That(row.DisplayName, Is.EqualTo(ChildName));
            Assert.That(row.Age, Is.EqualTo(ChildStartAge));
            Assert.That(row.GenerationLabel, Is.EqualTo("第四代"));
            Assert.That(row.Kinship, Does.Contain("父亲：明智 信正"));
            Assert.That(row.IsDesignatedHeir, Is.True);
            Assert.That(row.IsDesignatedHeirValid, Is.True);
            Assert.That(session.FixedHeirSummary, Does.Contain(ChildName));
            Assert.That(session.FixedHeirSummary, Does.Contain(ChildStartAge + "岁"));
        }

        [Test]
        public void Sc024_ChildIsAnEligibleEducationTargetAfterTheThirdSuccession()
        {
            GameSession session = DriveToThirdGeneration();

            Assert.That(session.TryStartCurrentHeadAmbition(FamilyAmbitionKind.EducateHeir),
                Is.True, session.LastFamilyDoctrineMessage);

            EducationView education = session.Education;
            Assert.That(education.HasLearner, Is.True);
            Assert.That(education.LearnerId, Is.EqualTo(ChildId));
            Assert.That(education.LearnerName, Is.EqualTo(ChildName));
            Assert.That(education.Age, Is.EqualTo(ChildStartAge));
            Assert.That(education.IsEligible, Is.True);
            Assert.That(education.CanEdit, Is.True, education.BlockReason);

            EducationPlanCommandResult planned = session.TrySetEducationPlan(ChildId,
                EducationDirection.Scholarship, M2FixedScenario.NobumasaId);

            Assert.That(planned.Succeeded, Is.True, session.LastEducationMessage);
            Assert.That(session.Development.TryGetPlan(ChildId, ChildStartAge,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(plan.Mentor, Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.EducateHeir));
        }

        [Test]
        public void Sc024_SaveRestoreKeepsChildKinshipDesignationDoctrineAndEducation()
        {
            GameSession session = DriveToThirdGeneration();
            Assert.That(session.TryStartCurrentHeadAmbition(FamilyAmbitionKind.EducateHeir),
                Is.True, session.LastFamilyDoctrineMessage);
            Assert.That(session.TrySetEducationPlan(ChildId, EducationDirection.Scholarship,
                M2FixedScenario.NobumasaId).Succeeded, Is.True, session.LastEducationMessage);

            SaveData data = session.CaptureSave();
            GameSession restored = NewSession();
            restored.InitNewGame();
            SaveOperationResult applied = restored.TryApplySave(data);

            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(restored.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(restored.Succession.SuccessionCount, Is.EqualTo(2));
            Assert.That(restored.Succession.DesignatedHeirId, Is.EqualTo(ChildId));
            Assert.That(restored.Succession.World.TryGetPerson(ChildId,
                out PersonState child), Is.True);
            Assert.That(child.DisplayName, Is.EqualTo(ChildName));
            Assert.That(restored.Succession.World.TryGetLife(ChildId,
                out SuccessionPersonLife life), Is.True);
            Assert.That(life.AgeAt(Start), Is.EqualTo(ChildStartAge));
            Assert.That(restored.Succession.World.Family.TryGetRole(ChildId,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.DirectKin));
            Assert.That(restored.Succession.World.Kinship.Contains(new KinshipLink(
                M2FixedScenario.NobumasaId, ChildId, KinshipType.ParentChild)), Is.True);

            Assert.That(restored.CurrentFamilyCreed,
                Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(restored.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(restored.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.EducateHeir));

            Assert.That(restored.Education.HasLearner, Is.True);
            Assert.That(restored.Education.LearnerId, Is.EqualTo(ChildId));
            Assert.That(restored.Education.Age, Is.EqualTo(ChildStartAge));
            Assert.That(restored.Education.IsEligible, Is.True);
            Assert.That(restored.Education.CanEdit, Is.True, restored.Education.BlockReason);
            Assert.That(restored.Development.TryGetPlan(ChildId, ChildStartAge,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(plan.Mentor, Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(restored.FamilyRoster.Single(item => item.PersonId == ChildId)
                .IsDesignatedHeir, Is.True);
        }

        [Test]
        public void Sc024_FinaleIsOfferedAfterTheTwoObservedSuccessions()
        {
            GameSession session = DriveToThirdGeneration();

            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(session.Succession.SuccessionCount, Is.EqualTo(2));
            Assert.That(session.PendingEventId, Is.Empty);

            Advance(session);

            Assert.That(session.PendingEventId, Is.EqualTo(SC013EventIds.FinaleEventId),
                "the third-generation finale must be offered on the SC-024 route");
            Choose(session, SC013EventIds.StandAloneOptionId);

            Assert.That(session.PendingEventId, Is.Empty);
            Assert.That(session.IsM2FinaleCommitted, Is.True);
            Assert.That(session.M2Ending.IsAvailable, Is.True);
            Assert.That(session.M2Ending.OptionId, Is.EqualTo(SC013EventIds.StandAloneOptionId));
        }

        [Test]
        public void Sc024_FinaleSharingDoesNotLeakToOtherScenarios()
        {
            GameSession session = NewSc024Session();
            var engine = new EventEngine(session.EventCatalog, session.EventText);
            Assert.That(engine.IsReady, Is.True, engine.ValidationMessage);

            Assert.That(EligibleEventIds(engine, session, SC024FamilyDoctrineScenario.ScenarioId),
                Does.Contain(SC013EventIds.FinaleEventId));
            Assert.That(EligibleEventIds(engine, session, EventScenarioIds.Sc013),
                Does.Contain(SC013EventIds.FinaleEventId));
            Assert.That(EligibleEventIds(engine, session, string.Empty),
                Does.Not.Contain(SC013EventIds.FinaleEventId));
            Assert.That(EligibleEventIds(engine, session, "sc020"),
                Does.Not.Contain(SC013EventIds.FinaleEventId));
        }

        private static IReadOnlyList<string> EligibleEventIds(EventEngine engine, GameSession session,
            string scenarioId)
        {
            var context = new EventRuleContext(2, 2, scenarioId, M2FixedScenario.NobumasaId,
                session.Succession.World.Family.Members.Select(member => member.PersonId).ToArray());
            return engine.GetEligibleEventIds(new GameDate(1560, 6, 1), new CharacterRelationships(),
                new EventRuntimeState(), context);
        }

        private GameSession DriveToThirdGeneration()
        {
            GameSession session = NewSc024Session();
            Advance(session);
            Assert.That(session.PendingEventId,
                Is.EqualTo(SC024EventIds.RighteousnessCouncilEventId));
            Choose(session, SC024EventIds.RighteousnessKeepOptionId);

            // Keep the neutral rainy-arrival choice so the later neutral legacy event
            // remains available for content coverage; SC-024 no longer gates on it.
            Advance(session);
            Assert.That(session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            Choose(session, SampleEventIds.OfferRestOptionId);

            AdvanceUntilRetirable(session);
            SuccessionResult first = session.BeginSuccessionRetirement();
            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(session.Succession.DesignatedHeirId,
                Is.EqualTo(M2FixedScenario.NobumasaId),
                "the designated line must still be Harunobu -> Masatsuna -> Nobumasa");

            AdvanceUntilRetirable(session);
            SuccessionResult second = session.BeginSuccessionRetirement();
            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(session.Succession.HeadPersonId,
                Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(session.Succession.DesignatedHeirId, Is.EqualTo(ChildId));
            return session;
        }

        private static void AdvanceUntilRetirable(GameSession session)
        {
            for (int month = 0; month < 24; month++)
            {
                if (session.CanBeginSuccessionRetirement)
                    return;
                if (!string.IsNullOrEmpty(session.PendingEventId))
                {
                    ResolvePending(session);
                    continue;
                }
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            }
            Assert.Fail("succession retirement did not become available: " +
                        session.SuccessionRetirementBlockReason);
        }

        private static void Advance(GameSession session) =>
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);

        private static void Choose(GameSession session, string optionId)
        {
            string pending = session.PendingEventId;
            Assert.That(pending, Is.Not.Empty);
            EventChoiceResult result = session.TryChooseEventOption(pending, optionId,
                session.CreateEventResultId(pending, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        private static void ResolvePending(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            string pending = session.PendingEventId;
            EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (EventOptionData option in definition.options)
            {
                if (!session.TryGetEventOptionAvailability(pending, option.optionId,
                        out bool enabled, out _) || !enabled)
                    continue;
                Assert.That(session.TryChooseEventOption(pending, option.optionId,
                    session.CreateEventResultId(pending, option.optionId)).Succeeded, Is.True);
                return;
            }
            Assert.Fail("no enabled option for " + pending);
        }
    }
}
