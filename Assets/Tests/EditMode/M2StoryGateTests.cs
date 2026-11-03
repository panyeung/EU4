using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class M2StoryGateTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        [Test]
        public void CannotRetireBeforeRequiredChoicesOrWithPendingEvent()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();

            Assert.That(M2FixedScenario.CanBeginRetirement(state, Runtime(), out string pledgeReason), Is.False);
            Assert.That(pledgeReason, Does.Contain("继承前的家议"));

            EventRuntimeState afterPledge = Runtime(
                (M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId));
            Assert.That(M2FixedScenario.CanBeginRetirement(state, afterPledge, out string rainyReason), Is.False);
            Assert.That(rainyReason, Does.Contain("雨夜来客"));

            EventRuntimeState afterRainy = Runtime(
                (M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId),
                (SampleEventIds.EventId, SampleEventIds.OfferRestOptionId));
            Assert.That(M2FixedScenario.CanBeginRetirement(state, afterRainy, out string allowedReason), Is.True,
                allowedReason);
            Assert.That(allowedReason, Is.Empty);

            EventRuntimeState pending = Runtime(true,
                new[]
                {
                    (M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId),
                    (SampleEventIds.EventId, SampleEventIds.OfferRestOptionId)
                });
            Assert.That(M2FixedScenario.CanBeginRetirement(state, pending, out string pendingReason), Is.False);
            Assert.That(pendingReason, Does.Contain("事件"));
        }

        [Test]
        public void AllowsOnlyTheNextFixedHandoff()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                CompletePledgeAndRainyArrival(session);

                Assert.That(Ids(session), Is.EquivalentTo(new[] { "person.sc013.masatsuna" }));
                Assert.That(session.SelectSuccessionSuccessor(
                    new PersonId("person.sc013.tokihime")).Succeeded, Is.False,
                    "the collateral candidate must never skip the intended generation");

                SuccessionResult first = session.BeginSuccessionRetirement();
                Assert.That(first.Succeeded, Is.True, first.Message);
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
                Assert.That(session.Succession.DesignatedHeirId,
                    Is.EqualTo(new PersonId("person.sc013.nobumasa")));

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Is.EqualTo(SC011EventIds.LegacyHonoredEventId));
                Assert.That(session.TryChooseEventOption(SC011EventIds.LegacyHonoredEventId,
                    SC011EventIds.LegacyHonoredRenewOptionId,
                    session.CreateEventResultId(SC011EventIds.LegacyHonoredEventId,
                        SC011EventIds.LegacyHonoredRenewOptionId)).Succeeded, Is.True);

                Assert.That(Ids(session), Is.EquivalentTo(new[] { "person.sc013.nobumasa" }));
                SuccessionResult second = session.BeginSuccessionRetirement();
                Assert.That(second.Succeeded, Is.True, second.Message);
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 信正"));
                Assert.That(session.Succession.SuccessionCount, Is.EqualTo(2));
                Assert.That(session.FixedHeirSummary, Does.Contain("最后一代"));
                Assert.That(session.FixedHeirSummary, Does.Not.Contain("终局已完成"),
                    "the finale has not been committed yet");

                SuccessionResult beforeFinale = session.BeginSuccessionRetirement();
                Assert.That(beforeFinale.Succeeded, Is.False);
                Assert.That(beforeFinale.Message, Does.Contain("终局"));
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 信正"));
                Assert.That(session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void RejectsNonFixedHeirWhileASuccessionDecisionIsActive()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                // Restore an M2 save whose succession phase is genuinely awaiting a
                // choice, so the guard is exercised instead of the generic
                // "no pending decision" rejection.
                SaveData save = session.CaptureSave();
                save.succession.phase = (int)SuccessionPhase.AwaitingSuccessionChoice;
                save.succession.pendingSuccessorId = string.Empty;
                save.succession.pendingRegentId = string.Empty;
                Assert.That(session.TryApplySave(save).Succeeded, Is.True);
                Assert.That(session.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));

                SuccessionResult result = session.SelectSuccessionSuccessor(
                    new PersonId("person.sc013.tokihime"));

                Assert.That(result.Succeeded, Is.False,
                    "only the fixed next heir may be selected while the decision is active");
                Assert.That(result.Message, Does.Contain("固定继承人"));
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 晴信"));
                Assert.That(session.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void InvalidFixedHeirBlocksHandoffWithoutFallingBackToCollateral()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();
            Assert.That(state.World.TrySetLife(M2FixedScenario.MasatsunaId,
                new SuccessionPersonLife(M2FixedScenario.MasatsunaBirthYear, false, false),
                out string error), Is.True, error);
            EventRuntimeState runtime = Runtime(
                (M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId),
                (SampleEventIds.EventId, SampleEventIds.OfferRestOptionId));

            Assert.That(M2FixedScenario.CanBeginRetirement(state, runtime, out string reason), Is.False);
            Assert.That(reason, Does.Contain("固定继承人"));

            GameSession session = GameSession.NewGame();
            try
            {
                CompletePledgeAndRainyArrival(session);
                Assert.That(session.Succession.World.TrySetLife(M2FixedScenario.MasatsunaId,
                    new SuccessionPersonLife(M2FixedScenario.MasatsunaBirthYear, false, false),
                    out error), Is.True, error);

                SuccessionResult result = session.BeginSuccessionRetirement();

                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Message, Does.Contain("固定继承人"));
                Assert.That(session.SuccessionHeadName, Is.EqualTo("明智 晴信"));
                Assert.That(session.Succession.Phase, Is.EqualTo(SuccessionPhase.None),
                    "an invalid fixed heir must not silently enter a collateral or crisis path");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static void CompletePledgeAndRainyArrival(GameSession session)
        {
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(M2FixedScenario.PledgeEventId));
            Assert.That(session.TryChooseEventOption(M2FixedScenario.PledgeEventId,
                M2FixedScenario.KeepPromiseOptionId,
                session.CreateEventResultId(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.KeepPromiseOptionId)).Succeeded, Is.True);

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            Assert.That(session.TryChooseEventOption(SampleEventIds.EventId,
                SampleEventIds.OfferRestOptionId,
                session.CreateEventResultId(SampleEventIds.EventId,
                    SampleEventIds.OfferRestOptionId)).Succeeded, Is.True);
        }

        private static string[] Ids(GameSession session)
        {
            var ids = new string[session.SuccessionCandidates.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = session.SuccessionCandidates[i].PersonId.Value;
            return ids;
        }

        private static EventRuntimeState Runtime(params (string eventId, string optionId)[] entries) =>
            Runtime(false, entries);

        private static EventRuntimeState Runtime(bool pending, (string eventId, string optionId)[] entries)
        {
            var history = new EventHistorySaveEntry[entries.Length];
            var committed = new string[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                string resultId = "test.result." + i;
                history[i] = new EventHistorySaveEntry
                {
                    eventId = entries[i].eventId,
                    optionId = entries[i].optionId,
                    resultId = resultId,
                    year = Start.Year,
                    month = Start.Month,
                    summary = string.Empty
                };
                committed[i] = resultId;
            }

            var data = new EventRuntimeSaveData
            {
                phase = pending ? (int)StableGamePhase.AwaitingEventChoice
                    : (int)StableGamePhase.Management,
                pendingEventIds = pending ? new[] { M2FixedScenario.FinaleEventId } : new string[0],
                cooldowns = new EventCooldownSaveEntry[0],
                onceEventIds = new string[0],
                history = history,
                committedResultIds = committed
            };
            Assert.That(EventRuntimeState.TryRestore(data, out EventRuntimeState state, out string error),
                Is.True, error);
            return state;
        }
    }
}
