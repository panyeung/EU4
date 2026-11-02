using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public class GameSessionSaveTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True, "No preview for " + destination);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }

        private void CompleteSupplyShortage()
        {
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.True);
            Assert.That(_session.TryNegotiateSupplyShortage(CharacterId.KurodaIori).Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryReportSupplyShortage().Succeeded, Is.True);
        }

        [Test]
        public void CaptureSave_ReflectsCurrentState()
        {
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.False);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).Succeeded, Is.True);
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 7).Succeeded, Is.True);

            SaveData data = _session.CaptureSave();

            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(data.year, Is.EqualTo(1560));
            Assert.That(data.month, Is.EqualTo(4));
            Assert.That(data.day, Is.EqualTo(2));
            Assert.That(data.locationId, Is.EqualTo(LocationId.Atsuta.Value));
            Assert.That(data.supplyShortageStage, Is.EqualTo((int)SupplyShortageStage.NotAccepted));
            Assert.That(data.sealedLetterStage, Is.EqualTo((int)SealedLetterStage.NotAccepted));
            Assert.That(data.marketTallyStage, Is.EqualTo((int)MarketTallyStage.NotAccepted));
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
        }

        [Test]
        public void CaptureSave_UsesSingleVersionSourceAndActiveCatalogVersion()
        {
            SaveData data = _session.CaptureSave();

            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveData.CurrentGameSchemaVersion),
                "schema version must have one source of truth");
            Assert.That(data.gameVersion, Is.EqualTo(SaveData.CurrentGameVersion));
            Assert.That(data.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(_session.EventCatalog, Is.Not.Null);
            Assert.That(data.eventContentVersion, Is.EqualTo(_session.EventCatalog.contentVersion),
                "capture must record the active catalog content version");
        }

        [Test]
        public void TryApplySave_RejectsEventContentVersionMismatch()
        {
            SaveData data = _session.CaptureSave();
            data.eventContentVersion = _session.EventCatalog.contentVersion + 1;
            GameDate date = _session.WorldClock.CurrentDate;

            SaveOperationResult result = _session.TryApplySave(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void TryApplySave_RejectsPendingEventThatIsAlsoMarkedOnce()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData data = _session.CaptureSave();
            string pendingId = data.eventRuntime.pendingEventIds[0];
            data.eventRuntime.onceEventIds = new[] { pendingId };
            GameDate date = _session.WorldClock.CurrentDate;

            SaveOperationResult result = _session.TryApplySave(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.PendingEventId, Is.EqualTo(pendingId),
                "rejected load must leave the live pending queue unchanged");
        }

        [Test]
        public void TryApplySave_RejectsPendingEventStillOnCooldown()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData data = _session.CaptureSave();
            string pendingId = data.eventRuntime.pendingEventIds[0];
            int currentMonthIndex = data.year * 12 + data.month;
            data.eventRuntime.cooldowns = new[]
            {
                new EventCooldownSaveEntry { eventId = pendingId, availableAtMonth = currentMonthIndex + 1 }
            };
            GameDate date = _session.WorldClock.CurrentDate;

            SaveOperationResult result = _session.TryApplySave(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.PendingEventId, Is.EqualTo(pendingId));
        }

        [Test]
        public void CaptureSave_StoresOnlyKnownNpcs()
        {
            SaveData data = _session.CaptureSave();

            Assert.That(data.relationships, Has.Length.EqualTo(CharacterCatalog.Npcs.Count));
            foreach (RelationshipSaveEntry entry in data.relationships)
                Assert.That(entry.characterId, Is.Not.EqualTo(CharacterId.Player.Value));
        }

        [Test]
        public void TryApplySave_RestoresDateLocationMissionsAndRelationships()
        {
            CompleteSupplyShortage();
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 10).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();

            _session.InitNewGame();
            Assert.That(_session.Merit, Is.EqualTo(0));

            SaveOperationResult applied = _session.TryApplySave(data);

            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 3)));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.RewardClaimed));
            Assert.That(_session.Merit, Is.EqualTo(10));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(10));
        }

        [Test]
        public void TryApplySave_InvalidDataLeavesLiveSessionUnchanged()
        {
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 5).Succeeded, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;

            SaveData invalid = _session.CaptureSave();
            invalid.locationId = "owari.unknown";

            SaveOperationResult applied = _session.TryApplySave(invalid);

            Assert.That(applied.Succeeded, Is.False);
            Assert.That(applied.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
        }

        [Test]
        public void RejectedLoadLeavesLiveSessionUnchanged()
        {
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 5).Succeeded, Is.True);
            SaveData invalid = _session.CaptureSave();
            invalid.randomState.streams[0].state0Hex = "0000000000000000";
            invalid.randomState.streams[0].state1Hex = "0000000000000000";
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            string beforeSeed = _session.CaptureSave().randomState.initialSeedHex;

            SaveOperationResult result = _session.TryApplySave(invalid);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
            Assert.That(_session.CaptureSave().randomState.initialSeedHex, Is.EqualTo(beforeSeed));
        }

        [Test]
        public void RejectedUnknownPendingEventLeavesLiveSessionUnchanged()
        {
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 7).Succeeded, Is.True);
            SaveData invalid = _session.CaptureSave();
            invalid.eventRuntime.phase = (int)StableGamePhase.AwaitingEventChoice;
            invalid.eventRuntime.pendingEventIds = new[] { "event.not-in-current-catalog" };
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            string beforeSeed = _session.CaptureSave().randomState.initialSeedHex;

            SaveOperationResult result = _session.TryApplySave(invalid);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.PendingEventId, Is.Empty);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(7));
            Assert.That(_session.CaptureSave().randomState.initialSeedHex, Is.EqualTo(beforeSeed));
        }

        [Test]
        public void LoadedCompletedMission_CannotPayRewardAgain()
        {
            CompleteSupplyShortage();
            SaveData data = _session.CaptureSave();

            _session.InitNewGame();
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            SupplyShortageActionResult retry = _session.TryReportSupplyShortage();

            Assert.That(retry.Succeeded, Is.False);
            Assert.That(_session.Merit, Is.EqualTo(10));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.RewardClaimed));
        }

        [Test]
        public void TryApplySave_ClearsTransientDialogues()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            Assert.That(started.Succeeded, Is.True, started.Error);
            SaveData data = _session.CaptureSave();

            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            DialogueSelectionResult selection = _session.TrySelectDialogueOption(
                started.DialogueInstanceId, started.CurrentNodeId, new DialogueOptionId("anything"));
            Assert.That(selection.Succeeded, Is.False);
            Assert.That(selection.Error, Is.Not.Empty);
        }

        [Test]
        public void SaveToAndLoadFrom_RoundTripThroughStore()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            try
            {
                CompleteSupplyShortage();
                Assert.That(_session.TryChangeRelationship(CharacterId.HoshinoAya, 4).Succeeded, Is.True);

                Assert.That(_session.SaveTo(store).Succeeded, Is.True);

                _session.InitNewGame();
                SaveOperationResult loaded = _session.LoadFrom(store);

                Assert.That(loaded.Succeeded, Is.True, loaded.Message);
                Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.RewardClaimed));
                Assert.That(_session.Merit, Is.EqualTo(10));
                Assert.That(_session.TryGetRelationship(CharacterId.HoshinoAya).CurrentValue, Is.EqualTo(4));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SaveToAndLoadFrom_RoundTripsEntireEventAndRandomState()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            GameSession restored = null;
            GameSession reloaded = null;
            try
            {
                _session.InitNewGame(0xABCDEF0123456789UL);
                Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
                SaveData pendingSnapshot = _session.CaptureSave();
                Assert.That(pendingSnapshot.eventRuntime.pendingEventIds, Is.Not.Empty);

                Assert.That(_session.SaveTo(store).Succeeded, Is.True);

                restored = GameSession.NewGame();
                restored.InitNewGame(1UL);
                Assert.That(restored.LoadFrom(store).Succeeded, Is.True);

                SaveData restoredPending = restored.CaptureSave();
                Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(_session.WorldClock.CurrentDate));
                Assert.That(restoredPending.eventRuntime.phase,
                    Is.EqualTo((int)StableGamePhase.AwaitingEventChoice));
                CollectionAssert.AreEqual(pendingSnapshot.eventRuntime.pendingEventIds,
                    restoredPending.eventRuntime.pendingEventIds,
                    "the whole pending queue must round-trip in order");
                AssertRandomStateEqual(pendingSnapshot.randomState, restoredPending.randomState,
                    "pending save");

                // Continue on the restored session so the round trip also covers the
                // durable markers produced by resolving the event and taking another month.
                string eventId = restored.PendingEventId;
                const string optionId = SampleEventIds.OfferRestOptionId;
                EventChoiceResult choice = restored.TryChooseEventOption(eventId, optionId,
                    restored.CreateEventResultId(eventId, optionId));
                Assert.That(choice.Succeeded, Is.True, choice.Message);
                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);

                SaveData resolvedSnapshot = restored.CaptureSave();
                Assert.That(resolvedSnapshot.eventRuntime.pendingEventIds, Is.Empty);
                Assert.That(resolvedSnapshot.eventRuntime.history, Has.Length.EqualTo(1));
                Assert.That(resolvedSnapshot.eventRuntime.cooldowns, Has.Length.EqualTo(1));
                Assert.That(resolvedSnapshot.eventRuntime.onceEventIds, Has.Length.EqualTo(1));
                Assert.That(resolvedSnapshot.eventRuntime.committedResultIds, Has.Length.EqualTo(1));
                Assert.That(restored.SaveTo(store).Succeeded, Is.True);

                reloaded = GameSession.NewGame();
                reloaded.InitNewGame(9UL);
                Assert.That(reloaded.LoadFrom(store).Succeeded, Is.True);

                SaveData reloadedSnapshot = reloaded.CaptureSave();
                Assert.That(reloaded.WorldClock.CurrentDate, Is.EqualTo(restored.WorldClock.CurrentDate));
                Assert.That(reloadedSnapshot.eventRuntime.phase, Is.EqualTo((int)StableGamePhase.Management));
                Assert.That(reloadedSnapshot.eventRuntime.pendingEventIds, Is.Empty);
                Assert.That(reloadedSnapshot.eventRuntime.history, Has.Length.EqualTo(1));
                Assert.That(reloadedSnapshot.eventRuntime.history[0].eventId,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.history[0].eventId));
                Assert.That(reloadedSnapshot.eventRuntime.history[0].optionId,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.history[0].optionId));
                Assert.That(reloadedSnapshot.eventRuntime.history[0].resultId,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.history[0].resultId));
                Assert.That(reloadedSnapshot.eventRuntime.history[0].year,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.history[0].year));
                Assert.That(reloadedSnapshot.eventRuntime.history[0].month,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.history[0].month));
                Assert.That(reloadedSnapshot.eventRuntime.history[0].summary,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.history[0].summary));
                Assert.That(reloadedSnapshot.eventRuntime.cooldowns[0].eventId,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.cooldowns[0].eventId));
                Assert.That(reloadedSnapshot.eventRuntime.cooldowns[0].availableAtMonth,
                    Is.EqualTo(resolvedSnapshot.eventRuntime.cooldowns[0].availableAtMonth));
                CollectionAssert.AreEqual(resolvedSnapshot.eventRuntime.onceEventIds,
                    reloadedSnapshot.eventRuntime.onceEventIds);
                CollectionAssert.AreEqual(resolvedSnapshot.eventRuntime.committedResultIds,
                    reloadedSnapshot.eventRuntime.committedResultIds);
                AssertRandomStateEqual(resolvedSnapshot.randomState, reloadedSnapshot.randomState,
                    "resolved save");
            }
            finally
            {
                if (reloaded != null) UnityEngine.Object.DestroyImmediate(reloaded.gameObject);
                if (restored != null) UnityEngine.Object.DestroyImmediate(restored.gameObject);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void AssertRandomStateEqual(RandomStateSaveData expected, RandomStateSaveData actual,
            string context)
        {
            Assert.That(actual, Is.Not.Null, context);
            Assert.That(actual.initialSeedHex, Is.EqualTo(expected.initialSeedHex), context);
            Assert.That(actual.streams, Has.Length.EqualTo(expected.streams.Length), context);
            for (int i = 0; i < expected.streams.Length; i++)
            {
                Assert.That(actual.streams[i].streamId, Is.EqualTo(expected.streams[i].streamId), context);
                Assert.That(actual.streams[i].algorithmVersion,
                    Is.EqualTo(expected.streams[i].algorithmVersion), context);
                Assert.That(actual.streams[i].state0Hex, Is.EqualTo(expected.streams[i].state0Hex), context);
                Assert.That(actual.streams[i].state1Hex, Is.EqualTo(expected.streams[i].state1Hex), context);
            }
        }

        [Test]
        public void LoadFrom_MissingSlot_LeavesSessionUnchanged()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            GameDate date = _session.WorldClock.CurrentDate;

            SaveOperationResult loaded = _session.LoadFrom(store);

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.NotFound));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
        }
    }
}
