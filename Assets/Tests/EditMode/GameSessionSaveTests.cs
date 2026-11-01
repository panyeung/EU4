using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
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
