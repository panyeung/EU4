using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public class SaveSlotStoreTests
    {
        private string _directory;
        private SaveSlotStore _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "SengokuChronicleTests",
                Guid.NewGuid().ToString("N"));
            _store = new SaveSlotStore(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private static SaveData ValidData(int day = 2)
        {
            return new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = 1560,
                month = 4,
                day = day,
                locationId = "owari.atsuta",
                supplyShortageStage = 2,
                sealedLetterStage = 1,
                marketTallyStage = 0,
                relationships = new[]
                {
                    new RelationshipSaveEntry { characterId = "npc.kuroda.iori", value = 10 },
                    new RelationshipSaveEntry { characterId = "npc.hoshino.aya", value = -3 }
                }
            };
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            Assert.That(_store.Save(ValidData()).Succeeded, Is.True);

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.year, Is.EqualTo(1560));
            Assert.That(loaded.Data.month, Is.EqualTo(4));
            Assert.That(loaded.Data.day, Is.EqualTo(2));
            Assert.That(loaded.Data.locationId, Is.EqualTo("owari.atsuta"));
            Assert.That(loaded.Data.supplyShortageStage, Is.EqualTo(2));
            Assert.That(loaded.Data.sealedLetterStage, Is.EqualTo(1));
            Assert.That(loaded.Data.marketTallyStage, Is.EqualTo(0));
            Assert.That(loaded.Data.relationships, Has.Length.EqualTo(2));
            Assert.That(loaded.Data.relationships[0].characterId, Is.EqualTo("npc.kuroda.iori"));
            Assert.That(loaded.Data.relationships[0].value, Is.EqualTo(10));
            Assert.That(loaded.Data.relationships[1].value, Is.EqualTo(-3));
        }

        [Test]
        public void Save_CreatesDirectoryAndFile()
        {
            Assert.That(Directory.Exists(_directory), Is.False);

            Assert.That(_store.Save(ValidData()).Succeeded, Is.True);

            Assert.That(File.Exists(_store.FilePath), Is.True);
            Assert.That(File.Exists(_store.FilePath + ".tmp"), Is.False);
        }

        [Test]
        public void Load_MissingFile_ReturnsNotFound()
        {
            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.NotFound));
            Assert.That(loaded.Message, Is.Not.Empty);
        }

        [Test]
        public void Load_CorruptJson_ReturnsInvalidJson()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, "{ this is not json");

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidJson));
        }

        [Test]
        public void Load_EmptyFile_ReturnsInvalidJson()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, "   ");

            Assert.That(_store.Load().Error, Is.EqualTo(SaveErrorCode.InvalidJson));
        }

        [Test]
        public void Load_UnsupportedVersion_ReturnsUnsupportedVersion()
        {
            Directory.CreateDirectory(_directory);
            SaveData data = ValidData();
            data.saveFormatVersion = SaveDataValidator.CurrentVersion + 1;
            File.WriteAllText(_store.FilePath, JsonUtility.ToJson(data));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
        }

        [Test]
        public void Load_InvalidData_ReturnsInvalidData()
        {
            Directory.CreateDirectory(_directory);
            SaveData data = ValidData();
            data.locationId = "owari.unknown";
            File.WriteAllText(_store.FilePath, JsonUtility.ToJson(data));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
        }

        [Test]
        public void Save_InvalidData_IsRejectedAndWritesNothing()
        {
            SaveData data = ValidData();
            data.saveFormatVersion = 99;

            SaveOperationResult result = _store.Save(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
            Assert.That(File.Exists(_store.FilePath), Is.False);
        }

        [Test]
        public void Save_OverwritesPreviousSlot()
        {
            Assert.That(_store.Save(ValidData(2)).Succeeded, Is.True);
            Assert.That(_store.Save(ValidData(9)).Succeeded, Is.True);

            Assert.That(_store.Load().Data.day, Is.EqualTo(9));
        }

        [Test]
        public void Save_KeepsPreviousSlotWhenValidationFails()
        {
            Assert.That(_store.Save(ValidData(2)).Succeeded, Is.True);
            SaveData invalid = ValidData(9);
            invalid.year = 1500;

            Assert.That(_store.Save(invalid).Succeeded, Is.False);
            Assert.That(_store.Load().Data.day, Is.EqualTo(2));
        }

        [Test]
        public void PendingEventAndRandomStateRoundTrip()
        {
            RandomStateManager random = RandomStateManager.Create(0x1020304050607080UL);
            random.GetStream(RandomStreamKind.Events).NextUInt64();
            SaveData data = ValidData();
            data.randomState = random.CaptureState();
            data.eventRuntime = new EventRuntimeSaveData
            {
                phase = (int)StableGamePhase.AwaitingEventChoice,
                pendingEventIds = new[] { "event.sample.guest" },
                cooldowns = new[] { new EventCooldownSaveEntry { eventId = "event.sample.guest", availableAtMonth = 9 } },
                onceEventIds = new[] { "event.sample.first-meeting" },
                history = new[]
                {
                    new EventHistorySaveEntry
                    {
                        eventId = "event.sample.earlier",
                        optionId = "option.sample.accept",
                        resultId = "result.sample.earlier.1",
                        year = 1560,
                        month = 4,
                        summary = "A previous decision was recorded."
                    }
                },
                committedResultIds = new[] { "result.sample.earlier.1" }
            };

            Assert.That(_store.Save(data).Succeeded, Is.True);
            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.eventRuntime.phase, Is.EqualTo((int)StableGamePhase.AwaitingEventChoice));
            Assert.That(loaded.Data.eventRuntime.pendingEventIds, Is.EqualTo(data.eventRuntime.pendingEventIds));
            Assert.That(loaded.Data.eventRuntime.cooldowns[0].availableAtMonth, Is.EqualTo(9));
            Assert.That(loaded.Data.eventRuntime.onceEventIds, Is.EqualTo(data.eventRuntime.onceEventIds));
            Assert.That(loaded.Data.eventRuntime.history[0].resultId, Is.EqualTo("result.sample.earlier.1"));
            Assert.That(loaded.Data.eventRuntime.committedResultIds, Is.EqualTo(data.eventRuntime.committedResultIds));
            Assert.That(loaded.Data.randomState.initialSeedHex, Is.EqualTo(data.randomState.initialSeedHex));
            Assert.That(loaded.Data.randomState.streams, Has.Length.EqualTo(data.randomState.streams.Length));
            for (int i = 0; i < data.randomState.streams.Length; i++)
            {
                Assert.That(loaded.Data.randomState.streams[i].streamId,
                    Is.EqualTo(data.randomState.streams[i].streamId));
                Assert.That(loaded.Data.randomState.streams[i].algorithmVersion,
                    Is.EqualTo(data.randomState.streams[i].algorithmVersion));
                Assert.That(loaded.Data.randomState.streams[i].state0Hex,
                    Is.EqualTo(data.randomState.streams[i].state0Hex));
                Assert.That(loaded.Data.randomState.streams[i].state1Hex,
                    Is.EqualTo(data.randomState.streams[i].state1Hex));
            }
        }

        [Test]
        public void LoadMigratesV1WithoutRewritingSourceFile()
        {
            Directory.CreateDirectory(_directory);
            const string legacyJson =
                "{\"saveFormatVersion\":1,\"year\":1560,\"month\":4,\"day\":2," +
                "\"locationId\":\"owari.atsuta\",\"supplyShortageStage\":2," +
                "\"sealedLetterStage\":1,\"marketTallyStage\":0," +
                "\"relationships\":[{\"characterId\":\"npc.kuroda.iori\",\"value\":10}]}";
            File.WriteAllText(_store.FilePath, legacyJson);

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.year, Is.EqualTo(1560));
            Assert.That(loaded.Data.relationships[0].value, Is.EqualTo(10));
            Assert.That(RandomStateManager.TryRestore(loaded.Data.randomState, out _), Is.True);
            Assert.That(loaded.Data.eventRuntime.pendingEventIds, Is.Empty);
            Assert.That(loaded.Data.eventContentVersion, Is.EqualTo(SaveData.CurrentEventContentVersion),
                "a v1 payload without content metadata migrates to the current content version");
            Assert.That(File.ReadAllText(_store.FilePath), Is.EqualTo(legacyJson));
        }

        [Test]
        public void InvalidTempDoesNotReplaceOriginal()
        {
            Assert.That(_store.Save(ValidData(2)).Succeeded, Is.True);
            var invalidTempStore = new SaveSlotStore(_directory, _ => "{ invalid json");

            SaveOperationResult result = invalidTempStore.Save(ValidData(9));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.Not.EqualTo(SaveErrorCode.None));
            Assert.That(_store.Load().Succeeded, Is.True);
            Assert.That(_store.Load().Data.day, Is.EqualTo(2));
            Assert.That(File.Exists(_store.FilePath + ".tmp"), Is.False);
        }

        [Test]
        public void PreviousSaveRemainsRecoverable()
        {
            Assert.That(_store.Save(ValidData(2)).Succeeded, Is.True);
            Assert.That(_store.Save(ValidData(9)).Succeeded, Is.True);

            SaveLoadResult backup = _store.LoadBackup();

            Assert.That(backup.Succeeded, Is.True, backup.Message);
            Assert.That(backup.Data.day, Is.EqualTo(2));
        }

        [Test]
        public void Load_V2MissingRandomState_IsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V2Json(includeRandomState: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("randomState"));
        }

        [Test]
        public void Load_V2MissingEventRuntime_IsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V2Json(includeEventRuntime: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("eventRuntime"));
        }

        [Test]
        public void Load_V2MissingSchemaVersion_IsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V2Json(includeSchemaVersion: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
        }

        [Test]
        public void Load_V2MissingVersionMetadata_IsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V2Json(includeGameVersion: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("版本元数据"));
        }

        [Test]
        public void Load_V2MissingEventContentVersion_IsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V2Json(includeEventContentVersion: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("版本元数据"));
        }

        [Test]
        public void Load_V2ContentVersionOneMigratesWithoutRewritingSource()
        {
            Directory.CreateDirectory(_directory);
            string legacyJson = V2Json();
            File.WriteAllText(_store.FilePath, legacyJson);

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.eventContentVersion, Is.EqualTo(1),
                "the v2 content version is preserved for the explicit 1 -> 2 additive load window");
            Assert.That(File.ReadAllText(_store.FilePath), Is.EqualTo(legacyJson));
        }

        [Test]
        public void Save_SerializedTempMissingRuntimeStateIsRejected()
        {
            var missingStateStore = new SaveSlotStore(_directory,
                _ => V2Json(includeRandomState: false));

            SaveOperationResult result = missingStateStore.Save(ValidData());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(File.Exists(_store.FilePath), Is.False);
        }

        [Test]
        public void Save_RecoversFromCorruptActiveSlotAndPreservesSource()
        {
            Directory.CreateDirectory(_directory);
            const string corrupt = "{ this is not json";
            File.WriteAllText(_store.FilePath, corrupt);

            SaveOperationResult result = _store.Save(ValidData(9));

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(_store.LastRecoveryFilePath, Is.Not.Empty);
            Assert.That(File.Exists(_store.LastRecoveryFilePath), Is.True);
            Assert.That(File.ReadAllText(_store.LastRecoveryFilePath), Is.EqualTo(corrupt),
                "the corrupt source must be preserved byte-for-byte");
            Assert.That(_store.LastRecoveryFilePath, Is.Not.EqualTo(_store.BackupFilePath));
            Assert.That(_store.Load().Data.day, Is.EqualTo(9), "a valid save must be possible again");
        }

        [Test]
        public void Save_RecoversFromInvalidButParseableActiveSlot()
        {
            Directory.CreateDirectory(_directory);
            SaveData corruptData = ValidData(2);
            corruptData.locationId = "owari.unknown";
            string corruptJson = JsonUtility.ToJson(corruptData);
            File.WriteAllText(_store.FilePath, corruptJson);

            SaveOperationResult result = _store.Save(ValidData(9));

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(File.Exists(_store.LastRecoveryFilePath), Is.True);
            Assert.That(File.ReadAllText(_store.LastRecoveryFilePath), Is.EqualTo(corruptJson));
            Assert.That(_store.Load().Data.day, Is.EqualTo(9));
        }

        private static string V2Json(bool includeSchemaVersion = true, bool includeGameVersion = true,
            bool includeEventContentVersion = true, bool includeRandomState = true, bool includeEventRuntime = true)
        {
            var parts = new List<string>();
            if (includeSchemaVersion) parts.Add("\"saveFormatVersion\":2");
            if (includeGameVersion) parts.Add("\"gameVersion\":\"0.1.0\"");
            parts.Add("\"rulesVersion\":1");
            if (includeEventContentVersion) parts.Add("\"eventContentVersion\":1");
            parts.Add("\"year\":1560");
            parts.Add("\"month\":4");
            parts.Add("\"day\":2");
            parts.Add("\"locationId\":\"owari.atsuta\"");
            parts.Add("\"supplyShortageStage\":2");
            parts.Add("\"sealedLetterStage\":1");
            parts.Add("\"marketTallyStage\":0");
            parts.Add("\"relationships\":[]");
            if (includeRandomState)
                parts.Add("\"randomState\":" + JsonUtility.ToJson(RandomStateManager.Create(7UL).CaptureState()));
            if (includeEventRuntime)
                parts.Add("\"eventRuntime\":" + JsonUtility.ToJson(new EventRuntimeSaveData()));
            return "{" + string.Join(",", parts) + "}";
        }

        [Test]
        public void Exists_ReflectsSlotPresence()
        {
            Assert.That(_store.Exists(), Is.False);
            Assert.That(_store.Save(ValidData()).Succeeded, Is.True);
            Assert.That(_store.Exists(), Is.True);
        }
    }
}
