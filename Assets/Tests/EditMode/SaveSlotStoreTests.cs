using System;
using System.IO;
using NUnit.Framework;
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
        public void Exists_ReflectsSlotPresence()
        {
            Assert.That(_store.Exists(), Is.False);
            Assert.That(_store.Save(ValidData()).Succeeded, Is.True);
            Assert.That(_store.Exists(), Is.True);
        }
    }
}
