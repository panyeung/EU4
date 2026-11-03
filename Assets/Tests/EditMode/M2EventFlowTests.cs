using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class M2EventFlowTests
    {
        [Test]
        public void InheritancePledge_OffersBothChoicesAndRecordsExactDeltaOnce()
        {
            GameSession keep = GameSession.NewGame();
            try
            {
                Assert.That(keep.TryAdvanceToNextMonth(), Is.True, keep.LastEventMessage);
                Assert.That(keep.PendingEventId, Is.EqualTo(M2FixedScenario.PledgeEventId));
                Assert.That(keep.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);
                Assert.That(keep.TryGetEventOptionAvailability(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.KeepPromiseOptionId, out bool keepEnabled, out string keepReason),
                    Is.True, keepReason);
                Assert.That(keepEnabled, Is.True, keepReason);
                Assert.That(keep.TryGetEventOptionAvailability(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.BreakPromiseOptionId, out bool breakEnabled, out string breakReason),
                    Is.True, breakReason);
                Assert.That(breakEnabled, Is.True, breakReason);

                Assert.That(keep.TryChooseEventOption(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.KeepPromiseOptionId,
                    keep.CreateEventResultId(M2FixedScenario.PledgeEventId,
                        M2FixedScenario.KeepPromiseOptionId)).Succeeded, Is.True);
                Assert.That(keep.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
                Assert.That(keep.CaptureSave().eventRuntime.history, Has.Length.EqualTo(1));
                AssertRelationshipChronicleCount(keep, 1, 0, 5);

                Assert.That(keep.TryChooseEventOption(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.KeepPromiseOptionId,
                    keep.CreateEventResultId(M2FixedScenario.PledgeEventId,
                        M2FixedScenario.KeepPromiseOptionId)).Succeeded, Is.False,
                    "the choice must not be recorded twice");
                Assert.That(keep.CaptureSave().eventRuntime.history, Has.Length.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(keep.gameObject);
            }

            GameSession broken = GameSession.NewGame();
            try
            {
                Assert.That(broken.TryAdvanceToNextMonth(), Is.True, broken.LastEventMessage);
                Assert.That(broken.TryChooseEventOption(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.BreakPromiseOptionId,
                    broken.CreateEventResultId(M2FixedScenario.PledgeEventId,
                        M2FixedScenario.BreakPromiseOptionId)).Succeeded, Is.True);
                Assert.That(broken.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(-5));
                AssertRelationshipChronicleCount(broken, 1, 0, -5);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(broken.gameObject);
            }
        }

        [Test]
        public void FixedScript_ResolvesPledgeRainyArrivalLegacyThenTwoHandoffs()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(M2FixedScenario.PledgeEventId));
                Choose(session, M2FixedScenario.PledgeEventId, M2FixedScenario.BreakPromiseOptionId);

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(SampleEventIds.EventId),
                    "the accepted rainy-arrival root follows the campaign-specific pledge");
                Choose(session, SampleEventIds.EventId, SampleEventIds.OfferRestOptionId);
                Assert.That(session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);

                SuccessionResult first = session.BeginSuccessionRetirement();
                Assert.That(first.Succeeded, Is.True, first.Message);
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
                Assert.That(session.PendingEventId, Is.Empty,
                    "a lawful handoff must not silently drop a queued event");

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
                Choose(session, SC011EventIds.LegacyHonoredEventId, SC011EventIds.LegacyHonoredRenewOptionId);
                Assert.That(session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));

                SuccessionResult second = session.BeginSuccessionRetirement();
                Assert.That(second.Succeeded, Is.True, second.Message);
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 信正"));
                Assert.That(session.Succession.SuccessionCount, Is.EqualTo(2));
                Assert.That(session.PendingEventId, Is.Empty);

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(M2FixedScenario.FinaleEventId));
                Choose(session, M2FixedScenario.FinaleEventId, M2FixedScenario.SeekAidOptionId);

                Assert.That(session.PendingEventId, Is.Empty);
                Assert.That(session.M2Ending.IsAvailable, Is.True);
                Assert.That(session.M2Ending.OptionId, Is.EqualTo(M2FixedScenario.SeekAidOptionId));
                Assert.That(session.FixedHeirSummary, Does.Contain("终局已完成"));
                Assert.That(session.CaptureSave().eventRuntime.history, Has.Length.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Finale_RequiresTwoHandoffsAndGatesAidAtFive()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                CompleteFirstGeneration(session);
                Assert.That(session.Succession.SuccessionCount, Is.EqualTo(1));
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId),
                    "the finale is not offered before the second handoff");
                Choose(session, SC011EventIds.LegacyHonoredEventId, SC011EventIds.LegacyHonoredRenewOptionId);

                Assert.That(session.BeginSuccessionRetirement().Succeeded, Is.True);
                Assert.That(session.Succession.SuccessionCount, Is.EqualTo(2));
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(M2FixedScenario.FinaleEventId));

                int beforeGateProbe = session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue;
                Assert.That(beforeGateProbe, Is.GreaterThanOrEqualTo(5),
                    "the fixed script must reach the finale with at least five trust");
                Assert.That(session.TryChangeRelationship(CharacterId.KurodaIori,
                    4 - beforeGateProbe).Succeeded, Is.True);
                Assert.That(session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(4));
                Assert.That(session.TryGetEventOptionAvailability(M2FixedScenario.FinaleEventId,
                    M2FixedScenario.SeekAidOptionId, out bool atFour, out string reason), Is.True, reason);
                Assert.That(atFour, Is.False);
                Assert.That(reason, Does.Contain("信任"));

                Assert.That(session.TryChangeRelationship(CharacterId.KurodaIori, 1).Succeeded, Is.True);
                Assert.That(session.TryGetEventOptionAvailability(M2FixedScenario.FinaleEventId,
                    M2FixedScenario.SeekAidOptionId, out bool atFive, out reason), Is.True, reason);
                Assert.That(atFive, Is.True, reason);
                Assert.That(session.TryGetEventOptionAvailability(M2FixedScenario.FinaleEventId,
                    M2FixedScenario.StandAloneOptionId, out bool standAlone, out reason), Is.True, reason);
                Assert.That(standAlone, Is.True, reason);

                SaveData checkpoint = session.CaptureSave();
                Choose(session, M2FixedScenario.FinaleEventId, M2FixedScenario.SeekAidOptionId);
                M2EndingView seekAid = session.M2Ending;
                Assert.That(seekAid.IsAvailable, Is.True);
                Assert.That(seekAid.Title, Does.Contain("旧交来援"));
                Assert.That(seekAid.Summary, Does.Contain("应邀"));

                Assert.That(session.TryApplySave(checkpoint).Succeeded, Is.True);
                Choose(session, M2FixedScenario.FinaleEventId, M2FixedScenario.StandAloneOptionId);
                M2EndingView standAloneEnding = session.M2Ending;
                Assert.That(standAloneEnding.IsAvailable, Is.True);
                Assert.That(standAloneEnding.Title, Does.Contain("独自承担"));
                Assert.That(standAloneEnding.Summary, Does.Not.EqualTo(seekAid.Summary));
                Assert.That(session.AdvanceOneDay(), Is.True,
                    "play must continue after the ending is committed");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static void CompleteFirstGeneration(GameSession session)
        {
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Choose(session, M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Choose(session, SampleEventIds.EventId, SampleEventIds.OfferRestOptionId);
            Assert.That(session.BeginSuccessionRetirement().Succeeded, Is.True);
        }

        private static void Choose(GameSession session, string eventId, string optionId)
        {
            EventChoiceResult result = session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        private static void AssertRelationshipChronicleCount(GameSession session, int expected,
            int from, int to)
        {
            int relationshipEntries = 0;
            int eventEntries = 0;
            foreach (FamilyChronicleEntry entry in session.ChronicleEntries)
            {
                if (entry.Category == ChronicleCategory.Event && entry.SubjectId == M2FixedScenario.PledgeEventId)
                    eventEntries++;
                if (entry.Category == ChronicleCategory.Relationship &&
                    entry.SubjectId == CharacterId.KurodaIori.Value)
                {
                    relationshipEntries++;
                    Assert.That(entry.FromValue, Is.EqualTo(from));
                    Assert.That(entry.ToValue, Is.EqualTo(to));
                }
            }
            Assert.That(relationshipEntries, Is.EqualTo(expected));
            Assert.That(eventEntries, Is.EqualTo(1));
        }
    }
}
