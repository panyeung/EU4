using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class EventEngineTests
    {
        private static EventDefinitionData CreateEvent(string eventId, int priority, int weight = 1)
        {
            return new EventDefinitionData
            {
                eventId = eventId,
                trigger = "Monthly",
                conditions = new EventConditionData[0],
                weight = weight,
                textId = "text." + eventId,
                options = new[]
                {
                    new EventOptionData
                    {
                        optionId = "option." + eventId,
                        textId = "option-text." + eventId,
                        requirements = new EventRequirementData[0],
                        effects = new[]
                        {
                            new EventEffectData
                            {
                                type = "RelationshipDelta",
                                targetCharacterId = CharacterId.KurodaIori.Value,
                                delta = 5
                            }
                        }
                    }
                },
                cooldownMonths = 2,
                onceScope = "Never",
                chainId = string.Empty,
                priority = priority
            };
        }

        private static EventEngine CreateEngine(params EventDefinitionData[] definitions)
        {
            var texts = new System.Collections.Generic.List<EventTextEntry>();
            foreach (EventDefinitionData definition in definitions)
            {
                texts.Add(new EventTextEntry
                {
                    textId = definition.textId,
                    title = definition.eventId,
                    body = "A visitor arrives at the family compound."
                });
                foreach (EventOptionData option in definition.options)
                    texts.Add(new EventTextEntry
                    {
                        textId = option.textId,
                        title = option.optionId,
                        body = "Offer a quiet place to rest."
                    });
            }
            var catalog = new EventCatalogData { contentVersion = 1, events = definitions, chains = new EventChainData[0] };
            var textCatalog = new EventTextCatalog { contentVersion = 1, entries = texts.ToArray() };
            return new EventEngine(catalog, textCatalog);
        }

        [Test]
        public void PriorityAndIdOrderingIsStable()
        {
            EventEngine engine = CreateEngine(
                CreateEvent("event.zeta", 20),
                CreateEvent("event.omega", 10),
                CreateEvent("event.alpha", 20));

            var eligible = engine.GetEligibleEventIds(new GameDate(1560, 5, 1), new CharacterRelationships(), new EventRuntimeState());

            CollectionAssert.AreEqual(new[] { "event.alpha", "event.zeta", "event.omega" }, eligible);
        }

        [Test]
        public void WeightsAreRepeatableForSameSeed()
        {
            EventEngine firstEngine = CreateEngine(CreateEvent("event.one", 10, 1), CreateEvent("event.two", 10, 3));
            EventEngine secondEngine = CreateEngine(CreateEvent("event.one", 10, 1), CreateEvent("event.two", 10, 3));
            var firstRandom = RandomStateManager.Create(707UL);
            var secondRandom = RandomStateManager.Create(707UL);

            EventScheduleResult first = firstEngine.ScheduleMonthly(new GameDate(1560, 5, 1),
                new CharacterRelationships(), firstRandom.GetStream(RandomStreamKind.Events), new EventRuntimeState());
            EventScheduleResult second = secondEngine.ScheduleMonthly(new GameDate(1560, 5, 1),
                new CharacterRelationships(), secondRandom.GetStream(RandomStreamKind.Events), new EventRuntimeState());

            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(first.SelectedEventId, Is.EqualTo(second.SelectedEventId));
        }

        [Test]
        public void NoCandidatesDoNotConsumeEventStream()
        {
            EventDefinitionData conditional = CreateEvent("event.relationship-gated", 1);
            conditional.conditions = new[]
            {
                new EventConditionData
                {
                    type = "RelationshipAtLeast",
                    targetCharacterId = CharacterId.KurodaIori.Value,
                    threshold = 50
                }
            };
            EventEngine engine = CreateEngine(conditional);
            var manager = RandomStateManager.Create(91UL);
            RandomStateSaveData before = manager.CaptureState();

            EventScheduleResult scheduled = engine.ScheduleMonthly(new GameDate(1560, 5, 1),
                new CharacterRelationships(), manager.GetStream(RandomStreamKind.Events), new EventRuntimeState());

            RandomStateSaveData after = manager.CaptureState();
            Assert.That(scheduled.Succeeded, Is.True, scheduled.Message);
            Assert.That(scheduled.SelectedEventId, Is.Empty);
            int eventStreamIndex = (int)RandomStreamKind.Events - 1;
            Assert.That(after.streams[eventStreamIndex].state0Hex,
                Is.EqualTo(before.streams[eventStreamIndex].state0Hex));
            Assert.That(after.streams[eventStreamIndex].state1Hex,
                Is.EqualTo(before.streams[eventStreamIndex].state1Hex));
        }

        [Test]
        public void ExpiredConditionsSkipWithoutMutation()
        {
            EventDefinitionData conditional = CreateEvent("event.relationship-gated", 1);
            conditional.conditions = new[]
            {
                new EventConditionData
                {
                    type = "RelationshipAtLeast",
                    targetCharacterId = CharacterId.KurodaIori.Value,
                    threshold = 5
                }
            };
            EventEngine engine = CreateEngine(conditional);
            var relationships = new CharacterRelationships();
            relationships.TryChange(CharacterId.KurodaIori, 5);
            var random = RandomStateManager.Create(81UL);
            var state = new EventRuntimeState();
            EventScheduleResult scheduled = engine.ScheduleMonthly(new GameDate(1560, 5, 1), relationships,
                random.GetStream(RandomStreamKind.Events), state);
            Assert.That(scheduled.SelectedEventId, Is.EqualTo("event.relationship-gated"));
            relationships.TryChange(CharacterId.KurodaIori, -5);
            EventRuntimeSaveData before = state.Capture();

            EventChoiceResult result = engine.TryChooseEventOption(scheduled.SelectedEventId,
                "option.event.relationship-gated", "result.expired.1", new GameDate(1560, 5, 1), relationships, state);

            Assert.That(result.Succeeded, Is.False);
            EventRuntimeSaveData after = state.Capture();
            Assert.That(after.phase, Is.EqualTo(before.phase));
            CollectionAssert.AreEqual(after.pendingEventIds, before.pendingEventIds);
            Assert.That(after.history, Is.Empty);
            Assert.That(after.committedResultIds, Is.Empty);
            Assert.That(relationships.TryGet(CharacterId.KurodaIori).CurrentValue, Is.Zero);
        }

        [Test]
        public void PendingEventBlocksAnotherMonth()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitNewGame(55UL);
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                GameDate pendingDate = session.WorldClock.CurrentDate;
                Assert.That(session.PendingEventId, Is.Not.Empty);

                Assert.That(session.TryAdvanceToNextMonth(), Is.False);
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(pendingDate));
            }
            finally
            {
                if (session != null) UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void InvalidAndRepeatedChoiceDoNotMutate()
        {
            EventDefinitionData definition = CreateEvent("event.single", 1);
            EventEngine engine = CreateEngine(definition);
            var relationships = new CharacterRelationships();
            var state = new EventRuntimeState();
            EventScheduleResult scheduled = engine.ScheduleMonthly(new GameDate(1560, 5, 1), relationships,
                RandomStateManager.Create(5UL).GetStream(RandomStreamKind.Events), state);
            EventRuntimeSaveData before = state.Capture();

            EventChoiceResult invalid = engine.TryChooseEventOption(scheduled.SelectedEventId, "option.invalid",
                "result.invalid.1", new GameDate(1560, 5, 1), relationships, state);

            Assert.That(invalid.Succeeded, Is.False);
            CollectionAssert.AreEqual(state.Capture().pendingEventIds, before.pendingEventIds);
            Assert.That(relationships.TryGet(CharacterId.KurodaIori).CurrentValue, Is.Zero);

            string resultId = "result.single.1";
            EventChoiceResult committed = engine.TryChooseEventOption(scheduled.SelectedEventId,
                "option.event.single", resultId, new GameDate(1560, 5, 1), relationships, state);
            Assert.That(committed.Succeeded, Is.True, committed.Message);
            EventRuntimeSaveData afterCommit = state.Capture();

            EventChoiceResult repeated = engine.TryChooseEventOption(scheduled.SelectedEventId,
                "option.event.single", resultId, new GameDate(1560, 5, 1), relationships, state);

            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(relationships.TryGet(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
            CollectionAssert.AreEqual(state.Capture().committedResultIds, afterCommit.committedResultIds);
            Assert.That(state.Capture().history, Has.Length.EqualTo(1));
        }

        [Test]
        public void ChoiceEffectAndHistoryCommitOnce()
        {
            EventEngine engine = CreateEngine(CreateEvent("event.commit", 1));
            var relationships = new CharacterRelationships();
            var state = new EventRuntimeState();
            EventScheduleResult scheduled = engine.ScheduleMonthly(new GameDate(1560, 5, 1), relationships,
                RandomStateManager.Create(2UL).GetStream(RandomStreamKind.Events), state);

            EventChoiceResult result = engine.TryChooseEventOption(scheduled.SelectedEventId,
                "option.event.commit", "result.commit.1", new GameDate(1560, 5, 1), relationships, state);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(relationships.TryGet(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
            EventRuntimeSaveData saved = state.Capture();
            Assert.That(saved.history, Has.Length.EqualTo(1));
            Assert.That(saved.history[0].resultId, Is.EqualTo("result.commit.1"));
            CollectionAssert.Contains(saved.committedResultIds, "result.commit.1");
            Assert.That(saved.pendingEventIds, Is.Empty);
        }

        [Test]
        public void EffectOverflowLeavesRelationshipHistoryAndQueueUnchanged()
        {
            EventEngine engine = CreateEngine(CreateEvent("event.overflow", 1));
            var relationships = new CharacterRelationships();
            relationships.TryChange(CharacterId.KurodaIori, 98);
            var state = new EventRuntimeState();
            EventScheduleResult scheduled = engine.ScheduleMonthly(new GameDate(1560, 5, 1), relationships,
                RandomStateManager.Create(3UL).GetStream(RandomStreamKind.Events), state);
            EventRuntimeSaveData before = state.Capture();

            EventChoiceResult result = engine.TryChooseEventOption(scheduled.SelectedEventId,
                "option.event.overflow", "result.overflow.1", new GameDate(1560, 5, 1), relationships, state);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(relationships.TryGet(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(98));
            CollectionAssert.AreEqual(state.Capture().pendingEventIds, before.pendingEventIds);
            Assert.That(state.Capture().history, Is.Empty);
            Assert.That(state.Capture().committedResultIds, Is.Empty);
        }

        [Test]
        public void RestoreRejectsPendingEventAlsoMarkedOnce()
        {
            EventEngine engine = CreateEngine(CreateEvent("event.alpha", 1), CreateEvent("event.beta", 1));
            var save = new EventRuntimeSaveData
            {
                phase = (int)StableGamePhase.AwaitingEventChoice,
                pendingEventIds = new[] { "event.alpha" },
                onceEventIds = new[] { "event.alpha" }
            };

            bool restored = engine.TryRestoreState(save, new GameDate(1560, 5, 1), out _, out string error);

            Assert.That(restored, Is.False);
            Assert.That(error, Does.Contain("一次性"));
        }

        [Test]
        public void RestoreRejectsPendingEventStillOnCooldown()
        {
            EventEngine engine = CreateEngine(CreateEvent("event.alpha", 1));
            var save = new EventRuntimeSaveData
            {
                phase = (int)StableGamePhase.AwaitingEventChoice,
                pendingEventIds = new[] { "event.alpha" },
                cooldowns = new[]
                {
                    new EventCooldownSaveEntry
                    {
                        eventId = "event.alpha",
                        availableAtMonth = 1560 * 12 + 5 + 2
                    }
                }
            };

            bool restored = engine.TryRestoreState(save, new GameDate(1560, 5, 1), out _, out string error);

            Assert.That(restored, Is.False);
            Assert.That(error, Does.Contain("冷却"));
            Assert.That(EventRuntimeState.TryRestore(save, out _, out _), Is.True,
                "without a save date the date-dependent cooldown check is skipped");
        }

        [Test]
        public void ScheduleMonthlyRejectsSecondEventWhilePending()
        {
            EventEngine engine = CreateEngine(CreateEvent("event.alpha", 10), CreateEvent("event.beta", 5));
            var random = RandomStateManager.Create(404UL);
            var state = new EventRuntimeState();

            EventScheduleResult first = engine.ScheduleMonthly(new GameDate(1560, 5, 1),
                new CharacterRelationships(), random.GetStream(RandomStreamKind.Events), state);
            Assert.That(first.Succeeded, Is.True, first.Message);
            EventRuntimeSaveData before = state.Capture();
            RandomStateSaveData randomBefore = random.CaptureState();

            EventScheduleResult second = engine.ScheduleMonthly(new GameDate(1560, 6, 1),
                new CharacterRelationships(), random.GetStream(RandomStreamKind.Events), state);

            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Message, Does.Contain("pending"));
            CollectionAssert.AreEqual(before.pendingEventIds, state.Capture().pendingEventIds);
            AssertRandomStreamsEqual(randomBefore, random.CaptureState());
        }

        [Test]
        public void MonthTurnQueuesAtMostOneEventAndBlocksUntilResolved()
        {
            // GDD 7.2 defines one weighted monthly selection; the demo's monthly path
            // therefore enqueues exactly one event and blocks further turns.
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitNewGame(2026UL);
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.CaptureSave().eventRuntime.pendingEventIds, Has.Length.EqualTo(1));
                Assert.That(session.TryAdvanceToNextMonth(), Is.False);

                string eventId = session.PendingEventId;
                EventChoiceResult choice = session.TryChooseEventOption(eventId,
                    SampleEventIds.OfferRestOptionId, session.CreateEventResultId(eventId,
                        SampleEventIds.OfferRestOptionId));
                Assert.That(choice.Succeeded, Is.True, choice.Message);
                Assert.That(session.PendingEventId, Is.Empty);

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.CaptureSave().eventRuntime.pendingEventIds, Is.Empty,
                    "the once-per-game sample event must not be scheduled twice");
                Assert.That(session.CaptureSave().eventRuntime.history, Has.Length.EqualTo(1));
            }
            finally
            {
                if (session != null) UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void RestoredMultiEventQueueCommitsHeadInOrder()
        {
            // The queue storage supports ordered multiple entries for future triggers
            // (event chains); this path is not reachable from the monthly demo.
            EventEngine engine = CreateEngine(CreateEvent("event.alpha", 1), CreateEvent("event.beta", 1));
            var save = new EventRuntimeSaveData
            {
                phase = (int)StableGamePhase.AwaitingEventChoice,
                pendingEventIds = new[] { "event.beta", "event.alpha" }
            };

            Assert.That(engine.TryRestoreState(save, new GameDate(1560, 5, 1),
                out EventRuntimeState state, out string error), Is.True, error);
            CollectionAssert.AreEqual(new[] { "event.beta", "event.alpha" }, state.PendingEventIds);

            Assert.That(engine.TryChooseEventOption("event.beta", "option.event.beta", "result.beta.1",
                new GameDate(1560, 5, 1), new CharacterRelationships(), state).Succeeded, Is.True);
            CollectionAssert.AreEqual(new[] { "event.alpha" }, state.PendingEventIds);
            Assert.That(state.Phase, Is.EqualTo(StableGamePhase.AwaitingEventChoice));

            Assert.That(engine.TryChooseEventOption("event.alpha", "option.event.alpha", "result.alpha.1",
                new GameDate(1560, 5, 1), new CharacterRelationships(), state).Succeeded, Is.True);
            Assert.That(state.Phase, Is.EqualTo(StableGamePhase.Management));
            Assert.That(state.PendingEventIds, Is.Empty);
        }

        private static void AssertRandomStreamsEqual(RandomStateSaveData expected, RandomStateSaveData actual)
        {
            Assert.That(actual.streams, Has.Length.EqualTo(expected.streams.Length));
            for (int i = 0; i < expected.streams.Length; i++)
            {
                Assert.That(actual.streams[i].streamId, Is.EqualTo(expected.streams[i].streamId));
                Assert.That(actual.streams[i].state0Hex, Is.EqualTo(expected.streams[i].state0Hex));
                Assert.That(actual.streams[i].state1Hex, Is.EqualTo(expected.streams[i].state1Hex));
            }
        }

        [Test]
        public void ReloadResumesSamePendingQueue()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            GameSession original = GameSession.NewGame();
            GameSession restored = null;
            try
            {
                original.InitNewGame(555UL);
                Assert.That(original.TryAdvanceToNextMonth(), Is.True, original.LastEventMessage);
                string pendingId = original.PendingEventId;
                RandomStateSaveData beforeRandom = original.CaptureSave().randomState;
                Assert.That(original.SaveTo(store).Succeeded, Is.True);

                restored = GameSession.NewGame();
                restored.InitNewGame(1UL);
                Assert.That(restored.LoadFrom(store).Succeeded, Is.True);

                Assert.That(restored.PendingEventId, Is.EqualTo(pendingId));
                Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(original.WorldClock.CurrentDate));
                Assert.That(restored.CaptureSave().randomState.streams[1].state0Hex,
                    Is.EqualTo(beforeRandom.streams[1].state0Hex));
                Assert.That(restored.TryAdvanceToNextMonth(), Is.False);
            }
            finally
            {
                if (restored != null) UnityEngine.Object.DestroyImmediate(restored.gameObject);
                if (original != null) UnityEngine.Object.DestroyImmediate(original.gameObject);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
