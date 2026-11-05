using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class M2ScenarioSaveTests
    {
        [Test]
        public void Load_RestoresEachStableM2DecisionPointWithoutDuplicateEffects()
        {
            AssertCheckpoint(DriveToPledge, session => Choose(session,
                M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId));
            AssertCheckpoint(DriveToRainyArrival, session => Choose(session,
                SampleEventIds.EventId, SampleEventIds.OfferRestOptionId));
            AssertCheckpoint(DriveToFirstHandoff,
                session => Assert.That(session.BeginSuccessionRetirement().Succeeded, Is.True));
            AssertCheckpoint(DriveToLegacyFollowUp, session => Choose(session,
                SC011EventIds.LegacyHonoredEventId, SC011EventIds.LegacyHonoredRenewOptionId));
            AssertCheckpoint(DriveToFinale, session => Choose(session,
                M2FixedScenario.FinaleEventId, M2FixedScenario.StandAloneOptionId));
            AssertCheckpoint(DriveToPostFinale, session => Assert.That(session.AdvanceOneDay(), Is.True));
        }

        [Test]
        public void Load_RejectsSc013PendingEventUnderNonM2Succession()
        {
            GameSession source = GameSession.NewGame();
            GameSession standard = GameSession.NewGame();
            GameSession crisis = GameSession.NewGame();
            try
            {
                Assert.That(source.TryAdvanceToNextMonth(), Is.True, source.LastEventMessage);
                Assert.That(source.PendingEventId, Is.EqualTo(M2FixedScenario.PledgeEventId));
                SaveData mismatched = source.CaptureSave();

                // Pair the SC-013 pending event with a non-M2 succession snapshot.
                // The load context is derived from the saved succession, so this must
                // be rejected by the scenario gate regardless of the target session.
                standard.InitStandardDemo();
                mismatched.succession = standard.CaptureSave().succession;
                // v7 requires growth rows to match the replaced roster exactly. Keep the
                // fixture valid so the load still reaches the scenario/event gate.
                mismatched.development =
                    CharacterDevelopmentSaveData.CreateEmpty(mismatched.succession);

                SaveOperationResult standardResult = standard.TryApplySave(mismatched);
                Assert.That(standardResult.Succeeded, Is.False,
                    "an SC-013 pending event under a non-M2 succession must be rejected");
                Assert.That(standardResult.Message, Does.Contain(M2FixedScenario.PledgeEventId));
                Assert.That(standard.IsM2ScenarioActive, Is.False);
                Assert.That(standard.PendingEventId, Is.Empty,
                    "a rejected load must leave the target session untouched");

                crisis.StartCrisisDemoScenario();
                SaveOperationResult crisisResult = crisis.TryApplySave(mismatched);
                Assert.That(crisisResult.Succeeded, Is.False,
                    "an SC-013 pending event under a non-M2 succession must be rejected");
                Assert.That(crisis.IsM2ScenarioActive, Is.False);
                Assert.That(crisis.PendingEventId, Is.Empty,
                    "a rejected load must leave the target session untouched");
            }
            finally
            {
                Object.DestroyImmediate(source.gameObject);
                Object.DestroyImmediate(standard.gameObject);
                Object.DestroyImmediate(crisis.gameObject);
            }
        }

        private static void AssertCheckpoint(System.Action<GameSession> drive,
            System.Action<GameSession> mutate)
        {
            GameSession session = GameSession.NewGame();
            try
            {
                drive(session);
                SaveData expected = session.CaptureSave();
                mutate(session);
                Assert.That(session.TryApplySave(expected).Succeeded, Is.True);

                SaveData actual = session.CaptureSave();
                Assert.That(JsonUtility.ToJson(actual), Is.EqualTo(JsonUtility.ToJson(expected)),
                    "loading a stable M2 checkpoint must restore every persisted field without replay");
                Assert.That(actual.succession.familyId, Is.EqualTo(M2FixedScenario.FamilyIdValue));
                Assert.That(actual.succession.persons.Length, Is.EqualTo(6));
                Assert.That(actual.succession.members.Length, Is.EqualTo(6));
                Assert.That(actual.succession.kinship.Length, Is.GreaterThanOrEqualTo(5));
                Assert.That(actual.chronicle.Length, Is.EqualTo(expected.chronicle.Length));
                Assert.That(actual.eventRuntime.history.Length,
                    Is.EqualTo(expected.eventRuntime.history.Length));
                Assert.That(session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                    Is.EqualTo(RelationshipValue(expected)));
            }
            finally
            {
                Object.DestroyImmediate(session.gameObject);
            }
        }

        private static int RelationshipValue(SaveData data)
        {
            if (data.relationships == null)
                return 0;
            foreach (RelationshipSaveEntry entry in data.relationships)
                if (string.Equals(entry.characterId, CharacterId.KurodaIori.Value,
                        System.StringComparison.Ordinal))
                    return entry.value;
            return 0;
        }

        private static void DriveToPledge(GameSession session)
        {
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(M2FixedScenario.PledgeEventId));
        }

        private static void DriveToRainyArrival(GameSession session)
        {
            DriveToPledge(session);
            Choose(session, M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
        }

        private static void DriveToFirstHandoff(GameSession session)
        {
            DriveToRainyArrival(session);
            Choose(session, SampleEventIds.EventId, SampleEventIds.OfferRestOptionId);
            Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 晴信"));
            Assert.That(session.CanBeginSuccessionRetirement, Is.True);
        }

        private static void DriveToLegacyFollowUp(GameSession session)
        {
            DriveToFirstHandoff(session);
            Assert.That(session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
        }

        private static void DriveToFinale(GameSession session)
        {
            DriveToLegacyFollowUp(session);
            Choose(session, SC011EventIds.LegacyHonoredEventId,
                SC011EventIds.LegacyHonoredRenewOptionId);
            Assert.That(session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(M2FixedScenario.FinaleEventId));
        }

        private static void DriveToPostFinale(GameSession session)
        {
            DriveToFinale(session);
            Choose(session, M2FixedScenario.FinaleEventId, M2FixedScenario.SeekAidOptionId);
            Assert.That(session.M2Ending.IsAvailable, Is.True);
        }

        private static void Choose(GameSession session, string eventId, string optionId)
        {
            EventChoiceResult result = session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
        }
    }
}
