using System.Globalization;
using NUnit.Framework;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    public class SaveDataValidatorTests
    {
        private static SaveData ValidData()
        {
            return new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = 1560,
                month = 4,
                day = 2,
                locationId = "owari.atsuta",
                supplyShortageStage = 2,
                sealedLetterStage = 1,
                marketTallyStage = 0,
                relationships = new[]
                {
                    new RelationshipSaveEntry { characterId = "npc.kuroda.iori", value = 10 }
                }
            };
        }

        [Test]
        public void ValidData_Passes()
        {
            SaveOperationResult result = SaveDataValidator.Validate(ValidData());

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.None));
        }

        [Test]
        public void NullData_IsRejected()
        {
            SaveOperationResult result = SaveDataValidator.Validate(null);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(-1)]
        public void WrongVersion_IsUnsupported(int version)
        {
            SaveData data = ValidData();
            data.saveFormatVersion = version;

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
            Assert.That(result.Message, Is.Not.Empty);
        }

        [TestCase(1560, 2, 30)]
        [TestCase(1559, 12, 31)]
        [TestCase(2001, 1, 1)]
        [TestCase(1560, 13, 1)]
        public void InvalidDate_IsRejected(int year, int month, int day)
        {
            SaveData data = ValidData();
            data.year = year;
            data.month = month;
            data.day = day;

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("owari.unknown")]
        public void UnknownLocation_IsRejected(string locationId)
        {
            SaveData data = ValidData();
            data.locationId = locationId;

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void UndefinedSupplyShortageStage_IsRejected(int stage)
        {
            SaveData data = ValidData();
            data.supplyShortageStage = stage;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void UndefinedSealedLetterStage_IsRejected(int stage)
        {
            SaveData data = ValidData();
            data.sealedLetterStage = stage;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void UndefinedMarketTallyStage_IsRejected(int stage)
        {
            SaveData data = ValidData();
            data.marketTallyStage = stage;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [Test]
        public void UnknownCharacter_IsRejected()
        {
            SaveData data = ValidData();
            data.relationships[0].characterId = "npc.unknown";

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
        }

        [Test]
        public void PlayerCharacter_IsRejected()
        {
            SaveData data = ValidData();
            data.relationships[0].characterId = "player.samurai";

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [Test]
        public void DuplicateCharacter_IsRejected()
        {
            SaveData data = ValidData();
            data.relationships = new[]
            {
                new RelationshipSaveEntry { characterId = "npc.kuroda.iori", value = 1 },
                new RelationshipSaveEntry { characterId = "npc.kuroda.iori", value = 2 }
            };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [Test]
        public void NullRelationshipEntry_IsRejected()
        {
            SaveData data = ValidData();
            data.relationships = new RelationshipSaveEntry[] { null };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [TestCase(-101)]
        [TestCase(101)]
        public void OutOfRangeRelationship_IsRejected(int value)
        {
            SaveData data = ValidData();
            data.relationships[0].value = value;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [TestCase(-100)]
        [TestCase(0)]
        [TestCase(100)]
        public void InRangeRelationship_IsAccepted(int value)
        {
            SaveData data = ValidData();
            data.relationships[0].value = value;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
        }

        [Test]
        public void NullRelationships_AreAcceptedAsEmpty()
        {
            SaveData data = ValidData();
            data.relationships = null;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
        }

        [Test]
        public void V1MigrationPreservesExistingSessionFields()
        {
            SaveData legacy = ValidData();
            legacy.saveFormatVersion = 1;

            bool succeeded = SaveDataMigrator.TryMigrate(
                legacy, out SaveData migrated, out SaveOperationResult result);

            Assert.That(succeeded, Is.True, result.Message);
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(migrated.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(migrated.year, Is.EqualTo(legacy.year));
            Assert.That(migrated.month, Is.EqualTo(legacy.month));
            Assert.That(migrated.day, Is.EqualTo(legacy.day));
            Assert.That(migrated.locationId, Is.EqualTo(legacy.locationId));
            Assert.That(migrated.supplyShortageStage, Is.EqualTo(legacy.supplyShortageStage));
            Assert.That(migrated.sealedLetterStage, Is.EqualTo(legacy.sealedLetterStage));
            Assert.That(migrated.marketTallyStage, Is.EqualTo(legacy.marketTallyStage));
            Assert.That(migrated.relationships[0].characterId, Is.EqualTo(legacy.relationships[0].characterId));
            Assert.That(migrated.relationships[0].value, Is.EqualTo(legacy.relationships[0].value));
            Assert.That(RandomStateManager.TryRestore(migrated.randomState, out _), Is.True);
            Assert.That(migrated.randomState.initialSeedHex,
                Is.EqualTo(SaveDataMigrator.V1DefaultSeed.ToString("X16", CultureInfo.InvariantCulture)),
                "v1 migration must keep its documented deterministic default seed");
            Assert.That(migrated.eventRuntime.pendingEventIds, Is.Empty);
            Assert.That(migrated.gameVersion, Is.EqualTo(SaveData.CurrentGameVersion));
            Assert.That(migrated.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(migrated.eventContentVersion, Is.EqualTo(SaveData.CurrentEventContentVersion));
            Assert.That(legacy.saveFormatVersion, Is.EqualTo(1), "Migration must not mutate its input.");
        }

        [Test]
        public void V1MigrationWithoutContentVersionUsesCurrentContentVersion()
        {
            SaveData legacy = ValidData();
            legacy.saveFormatVersion = 1;
            legacy.eventContentVersion = 0;

            bool succeeded = SaveDataMigrator.TryMigrate(
                legacy, out SaveData migrated, out SaveOperationResult result);

            Assert.That(succeeded, Is.True, result.Message);
            Assert.That(SaveData.CurrentEventContentVersion, Is.EqualTo(2));
            Assert.That(migrated.eventContentVersion, Is.EqualTo(SaveData.CurrentEventContentVersion));
        }

        [Test]
        public void PendingEventAlsoMarkedOnce_IsRejected()
        {
            SaveData data = ValidData();
            data.eventRuntime = PendingRuntime("event.duplicate-scope");
            data.eventRuntime.onceEventIds = new[] { "event.duplicate-scope" };

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("一次性"));
        }

        [Test]
        public void PendingEventStillOnCooldown_IsRejected()
        {
            SaveData data = ValidData();
            data.eventRuntime = PendingRuntime("event.cooling");
            data.eventRuntime.cooldowns = new[]
            {
                new EventCooldownSaveEntry
                {
                    eventId = "event.cooling",
                    availableAtMonth = data.year * 12 + data.month + 1
                }
            };

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("冷却"));
        }

        [Test]
        public void PendingEventWithExpiredCooldown_IsAccepted()
        {
            SaveData data = ValidData();
            data.eventRuntime = PendingRuntime("event.cooled-down");
            data.eventRuntime.cooldowns = new[]
            {
                new EventCooldownSaveEntry
                {
                    eventId = "event.cooled-down",
                    availableAtMonth = data.year * 12 + data.month
                }
            };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
        }

        private static EventRuntimeSaveData PendingRuntime(string eventId)
        {
            return new EventRuntimeSaveData
            {
                phase = (int)StableGamePhase.AwaitingEventChoice,
                pendingEventIds = new[] { eventId }
            };
        }

        [Test]
        public void UnknownVersionIsRejected()
        {
            SaveData future = ValidData();
            future.saveFormatVersion = SaveDataValidator.CurrentVersion + 1;

            bool succeeded = SaveDataMigrator.TryMigrate(
                future, out SaveData migrated, out SaveOperationResult result);

            Assert.That(succeeded, Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
            Assert.That(migrated, Is.Null);
        }
    }
}
