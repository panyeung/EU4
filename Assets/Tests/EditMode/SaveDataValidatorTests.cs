using NUnit.Framework;
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
        [TestCase(2)]
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
    }
}
