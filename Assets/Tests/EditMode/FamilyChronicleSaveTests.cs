using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-012 Task 2: v4 chronicle persistence, strict presence validation and the
    /// lossless v3 event-history migration. Tests only use plain data and the file
    /// slot; no scene or Editor state is touched.
    /// </summary>
    public sealed class FamilyChronicleSaveTests
    {
        private string _directory;
        private SaveSlotStore _store;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SengokuChronicleTests", "sc012-" +
                System.Guid.NewGuid().ToString("N"));
            _store = new SaveSlotStore(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        private static SaveData ValidData()
        {
            return new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = 1560,
                month = 4,
                day = 2,
                locationId = "owari.atsuta",
                supplyShortageStage = 1,
                sealedLetterStage = 1,
                marketTallyStage = 0,
                relationships = new[]
                {
                    new RelationshipSaveEntry { characterId = "npc.kuroda.iori", value = 5 }
                }
            };
        }

        private static ChronicleSaveEntry Entry(int sequence, ChronicleCategory category, int year = 1560,
            int month = 4, string entryId = null)
        {
            string subjectId = category == ChronicleCategory.Relationship
                ? "npc.kuroda.iori"
                : category == ChronicleCategory.Mission
                    ? FamilyChronicleEntry.SupplyShortageMissionId
                    : "subject." + sequence;
            return new ChronicleSaveEntry
            {
                entryId = entryId ?? FamilyChronicleEntry.BuildEntryId(sequence),
                sequence = sequence,
                year = year,
                month = month,
                category = (int)category,
                subjectId = subjectId,
                sourceId = "source." + sequence,
                title = "记录" + sequence,
                summary = "摘要" + sequence,
                fromValue = 0,
                toValue = category == ChronicleCategory.Relationship ? 5 : 0,
                fromStage = 0,
                toStage = category == ChronicleCategory.Mission ? 1 : 0,
                fromPhase = 0,
                toPhase = category == ChronicleCategory.Succession ? 1 : 0
            };
        }

        [Test]
        public void ChronicleRoundTripsThroughTheVersionFourSlot()
        {
            SaveData data = ValidData();
            data.chronicle = new[]
            {
                Entry(1, ChronicleCategory.Event),
                Entry(2, ChronicleCategory.Relationship, month: 5),
                Entry(3, ChronicleCategory.Mission),
                Entry(4, ChronicleCategory.Succession)
            };

            Assert.That(_store.Save(data).Succeeded, Is.True);
            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.chronicle, Has.Length.EqualTo(4));
            for (int i = 0; i < data.chronicle.Length; i++)
            {
                Assert.That(loaded.Data.chronicle[i].entryId, Is.EqualTo(data.chronicle[i].entryId));
                Assert.That(loaded.Data.chronicle[i].sequence, Is.EqualTo(i + 1));
                Assert.That(loaded.Data.chronicle[i].category, Is.EqualTo(data.chronicle[i].category));
                Assert.That(loaded.Data.chronicle[i].year, Is.EqualTo(data.chronicle[i].year));
                Assert.That(loaded.Data.chronicle[i].month, Is.EqualTo(data.chronicle[i].month));
                Assert.That(loaded.Data.chronicle[i].title, Is.EqualTo(data.chronicle[i].title));
                Assert.That(loaded.Data.chronicle[i].summary, Is.EqualTo(data.chronicle[i].summary));
            }
        }

        [Test]
        public void VersionThreeEventHistoryMigratesInOriginalOrder()
        {
            Directory.CreateDirectory(_directory);
            string legacyJson = V3Json(includeHistory: true, includeChronicle: false);
            File.WriteAllText(_store.FilePath, legacyJson);

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.chronicle, Has.Length.EqualTo(2));
            Assert.That(loaded.Data.chronicle[0].sequence, Is.EqualTo(1));
            Assert.That(loaded.Data.chronicle[0].category, Is.EqualTo((int)ChronicleCategory.Event));
            Assert.That(loaded.Data.chronicle[0].subjectId, Is.EqualTo("event.earlier"));
            Assert.That(loaded.Data.chronicle[0].sourceId, Is.EqualTo("option.earlier"));
            Assert.That(loaded.Data.chronicle[0].year, Is.EqualTo(1560));
            Assert.That(loaded.Data.chronicle[0].month, Is.EqualTo(3));
            Assert.That(loaded.Data.chronicle[0].summary, Is.EqualTo("较早的决定。"));
            Assert.That(loaded.Data.chronicle[1].subjectId, Is.EqualTo("event.later"));
            Assert.That(loaded.Data.chronicle[1].month, Is.EqualTo(4));
            Assert.That(loaded.Data.chronicle[1].summary, Is.EqualTo("较晚的决定。"));
            Assert.That(loaded.Data.eventRuntime.history, Has.Length.EqualTo(2),
                "the original event runtime history must be preserved, not moved away");
            Assert.That(File.ReadAllText(_store.FilePath), Is.EqualTo(legacyJson),
                "migration must not rewrite the source file");
        }

        [Test]
        public void VersionThreeWithoutHistoryMigratesToEmptyChronicle()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V3Json(includeHistory: false, includeChronicle: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.chronicle, Is.Empty,
                "migration must not invent history that the v3 save never contained");
            Assert.That(loaded.Data.relationships[0].value, Is.EqualTo(5));
            Assert.That(loaded.Data.supplyShortageStage, Is.EqualTo(1));
        }

        [Test]
        public void VersionTwoMigratesToAnEmptyChronicle()
        {
            SaveData legacy = ValidData();
            legacy.saveFormatVersion = 2;
            legacy.succession = null;
            legacy.chronicle = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, out SaveData migrated, out SaveOperationResult result),
                Is.True, result.Message);
            Assert.That(migrated.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(migrated.chronicle, Is.Empty);
        }

        [Test]
        public void ProgrammaticVersionTwoWithNonNullChronicleIsRejected()
        {
            SaveData legacy = ValidData();
            legacy.saveFormatVersion = 2;
            legacy.succession = null;
            legacy.chronicle = new ChronicleSaveEntry[0];

            Assert.That(SaveDataMigrator.TryMigrate(legacy, out SaveData migrated,
                out SaveOperationResult result), Is.False);
            Assert.That(migrated, Is.Null);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("chronicle"));
        }

        [Test]
        public void VersionFourWithoutChronicleRootPropertyIsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V3Json(includeHistory: false, includeChronicle: false,
                version: SaveDataValidator.CurrentVersion));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("chronicle"));
        }

        [Test]
        public void DuplicateChronicleIdIsRejected()
        {
            SaveData data = ValidData();
            ChronicleSaveEntry first = Entry(1, ChronicleCategory.Event);
            ChronicleSaveEntry second = Entry(2, ChronicleCategory.Event, entryId: first.entryId);
            data.chronicle = new[] { first, second };

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("重复"),
                "a duplicate ID must keep its specific error even when it also mismatches its sequence");
        }

        [Test]
        public void UniqueChronicleIdThatDoesNotMatchItsSequenceIsRejected()
        {
            SaveData data = ValidData();
            data.chronicle = new[]
            {
                Entry(1, ChronicleCategory.Event, entryId: FamilyChronicleEntry.BuildEntryId(2))
            };

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("不一致"));
        }

        [Test]
        public void DuplicateOrOutOfOrderSequenceIsRejected()
        {
            SaveData data = ValidData();
            data.chronicle = new[]
            {
                Entry(1, ChronicleCategory.Event),
                Entry(2, ChronicleCategory.Event, entryId: FamilyChronicleEntry.BuildEntryId(9))
            };
            data.chronicle[1].sequence = 1;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void UndefinedChronicleCategoryIsRejected(int category)
        {
            SaveData data = ValidData();
            ChronicleSaveEntry entry = Entry(1, ChronicleCategory.Event);
            entry.category = category;
            data.chronicle = new[] { entry };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [TestCase(1560, 0)]
        [TestCase(1560, 13)]
        [TestCase(1559, 4)]
        public void InvalidChronicleDateIsRejected(int year, int month)
        {
            SaveData data = ValidData();
            data.chronicle = new[] { Entry(1, ChronicleCategory.Event, year: year, month: month) };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [Test]
        public void RelationshipEntryWithEqualOrOutOfRangeValuesIsRejected()
        {
            SaveData equal = ValidData();
            ChronicleSaveEntry same = Entry(1, ChronicleCategory.Relationship);
            same.toValue = same.fromValue;
            equal.chronicle = new[] { same };
            Assert.That(SaveDataValidator.Validate(equal).Succeeded, Is.False);

            SaveData outOfRange = ValidData();
            ChronicleSaveEntry huge = Entry(1, ChronicleCategory.Relationship);
            huge.toValue = 101;
            outOfRange.chronicle = new[] { huge };
            Assert.That(SaveDataValidator.Validate(outOfRange).Succeeded, Is.False);
        }

        [Test]
        public void MissionEntryWithEqualOrUndefinedStagesIsRejected()
        {
            SaveData equal = ValidData();
            ChronicleSaveEntry same = Entry(1, ChronicleCategory.Mission);
            same.fromStage = same.toStage = 2;
            equal.chronicle = new[] { same };
            Assert.That(SaveDataValidator.Validate(equal).Succeeded, Is.False);

            SaveData undefined = ValidData();
            ChronicleSaveEntry invalid = Entry(1, ChronicleCategory.Mission);
            invalid.toStage = 9;
            undefined.chronicle = new[] { invalid };
            Assert.That(SaveDataValidator.Validate(undefined).Succeeded, Is.False);
        }

        [Test]
        public void MissionEntryOutsideItsOwnMissionRangeOrWithUnknownIdIsRejected()
        {
            SaveData sealedImpossible = ValidData();
            ChronicleSaveEntry sealedEntry = Entry(1, ChronicleCategory.Mission);
            sealedEntry.subjectId = FamilyChronicleEntry.SealedLetterMissionId;
            sealedEntry.fromStage = 2;
            sealedEntry.toStage = 3;
            sealedImpossible.chronicle = new[] { sealedEntry };
            Assert.That(SaveDataValidator.Validate(sealedImpossible).Succeeded, Is.False,
                "sealed-letter only defines stages 0..2");

            SaveData marketImpossible = ValidData();
            ChronicleSaveEntry marketEntry = Entry(1, ChronicleCategory.Mission);
            marketEntry.subjectId = FamilyChronicleEntry.MarketTallyMissionId;
            marketEntry.fromStage = 3;
            marketEntry.toStage = 4;
            marketImpossible.chronicle = new[] { marketEntry };
            Assert.That(SaveDataValidator.Validate(marketImpossible).Succeeded, Is.False,
                "market-tally only defines stages 0..3");

            SaveData unknownMission = ValidData();
            ChronicleSaveEntry unknownEntry = Entry(1, ChronicleCategory.Mission);
            unknownEntry.subjectId = "mission.unknown";
            unknownMission.chronicle = new[] { unknownEntry };
            Assert.That(SaveDataValidator.Validate(unknownMission).Succeeded, Is.False,
                "an unknown mission ID must not borrow another mission's stage range");
        }

        [Test]
        public void SealedLetterAndMarketTallyMissionRecordsAreAccepted()
        {
            SaveData data = ValidData();
            ChronicleSaveEntry sealedLetter = Entry(1, ChronicleCategory.Mission);
            sealedLetter.subjectId = FamilyChronicleEntry.SealedLetterMissionId;
            sealedLetter.fromStage = 1;
            sealedLetter.toStage = 2;
            ChronicleSaveEntry marketTally = Entry(2, ChronicleCategory.Mission);
            marketTally.subjectId = FamilyChronicleEntry.MarketTallyMissionId;
            marketTally.fromStage = 0;
            marketTally.toStage = 1;
            data.chronicle = new[] { sealedLetter, marketTally };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
        }

        [Test]
        public void SuccessionEntryWithUndefinedPhaseIsRejected()
        {
            SaveData data = ValidData();
            ChronicleSaveEntry entry = Entry(1, ChronicleCategory.Succession);
            entry.fromPhase = 0;
            entry.toPhase = 42;
            data.chronicle = new[] { entry };

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.False);
        }

        [Test]
        public void SamePhaseSuccessionHeadHandoffRoundTripsThroughTheVersionFourSlot()
        {
            SaveData data = ValidData();
            ChronicleSaveEntry entry = Entry(1, ChronicleCategory.Succession);
            entry.fromPhase = (int)SuccessionPhase.None;
            entry.toPhase = (int)SuccessionPhase.None;
            entry.fromHeadId = "person.akechi.harunobu";
            entry.toHeadId = "person.akechi.kageyu";
            data.chronicle = new[] { entry };

            Assert.That(_store.Save(data).Succeeded, Is.True);
            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.chronicle[0].fromPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(loaded.Data.chronicle[0].toPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(loaded.Data.chronicle[0].fromHeadId,
                Is.EqualTo("person.akechi.harunobu"));
            Assert.That(loaded.Data.chronicle[0].toHeadId,
                Is.EqualTo("person.akechi.kageyu"));
        }

        [Test]
        public void SamePhaseSuccessionWithoutARealHeadChangeIsRejected()
        {
            SaveData missingHeads = ValidData();
            ChronicleSaveEntry missing = Entry(1, ChronicleCategory.Succession);
            missing.fromPhase = (int)SuccessionPhase.None;
            missing.toPhase = (int)SuccessionPhase.None;
            missingHeads.chronicle = new[] { missing };
            Assert.That(SaveDataValidator.Validate(missingHeads).Succeeded, Is.False,
                "equal phases need a recorded head change");

            SaveData equalHeads = ValidData();
            ChronicleSaveEntry equal = Entry(1, ChronicleCategory.Succession);
            equal.fromPhase = (int)SuccessionPhase.None;
            equal.toPhase = (int)SuccessionPhase.None;
            equal.fromHeadId = equal.toHeadId = "person.akechi.harunobu";
            equalHeads.chronicle = new[] { equal };
            Assert.That(SaveDataValidator.Validate(equalHeads).Succeeded, Is.False,
                "an identical head pair must not make a same-phase record look valid");

            SaveData otherPhase = ValidData();
            ChronicleSaveEntry pending = Entry(1, ChronicleCategory.Succession);
            pending.fromPhase = (int)SuccessionPhase.AwaitingSuccessionChoice;
            pending.toPhase = (int)SuccessionPhase.AwaitingSuccessionChoice;
            pending.fromHeadId = "person.akechi.harunobu";
            pending.toHeadId = "person.akechi.kageyu";
            otherPhase.chronicle = new[] { pending };
            Assert.That(SaveDataValidator.Validate(otherPhase).Succeeded, Is.False,
                "only the adult None-to-None handoff may keep the phase unchanged");
        }

        [Test]
        public void RejectedChronicleLoadLeavesTheLiveSessionUnchanged()
        {
            _session = GameSession.NewGame();
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 7).Succeeded, Is.True);
            int entriesBefore = _session.ChronicleEntries.Count;
            int valueBefore = _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue;

            SaveData corrupted = _session.CaptureSave();
            corrupted.chronicle[0].sequence = 9;

            SaveOperationResult result = _session.TryApplySave(corrupted);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(entriesBefore));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.EqualTo(valueBefore));
        }

        [Test]
        public void MismatchedUniqueChronicleIdLoadIsRejectedAtomically()
        {
            _session = GameSession.NewGame();
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 7).Succeeded, Is.True);
            int entriesBefore = _session.ChronicleEntries.Count;
            string entryIdBefore = _session.ChronicleEntries[0].EntryId;

            SaveData corrupted = _session.CaptureSave();
            corrupted.chronicle[0].entryId = FamilyChronicleEntry.BuildEntryId(2);

            SaveOperationResult result = _session.TryApplySave(corrupted);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(entriesBefore));
            Assert.That(_session.ChronicleEntries[0].EntryId, Is.EqualTo(entryIdBefore));
        }

        [Test]
        public void VersionThreeJsonWithChronicleFieldIsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V3Json(includeHistory: false, includeChronicle: true));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("chronicle"));
        }

        [Test]
        public void VersionTwoJsonWithChronicleFieldIsRejected()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, V3Json(includeHistory: false, includeChronicle: true,
                version: 2, includeSuccession: false));

            SaveLoadResult loaded = _store.Load();

            Assert.That(loaded.Succeeded, Is.False);
            Assert.That(loaded.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(loaded.Message, Does.Contain("chronicle"));
        }

        [Test]
        public void CapturedSessionChronicleSurvivesLoadWithoutDuplicates()
        {
            _session = GameSession.NewGame();
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 3).Succeeded, Is.True);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            SaveData saved = _session.CaptureSave();

            Assert.That(_session.SaveTo(_store).Succeeded, Is.True);
            int before = _session.ChronicleEntries.Count;
            Assert.That(_session.LoadFrom(_store).Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(before),
                "loading must restore exactly the saved records and append none");
            Assert.That(_session.ChronicleEntries[0].EntryId, Is.EqualTo(saved.chronicle[0].entryId));
            Assert.That(_session.ChronicleEntries[1].EntryId, Is.EqualTo(saved.chronicle[1].entryId));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(3));
        }

        private static string V3Json(bool includeHistory, bool includeChronicle, int version = 3,
            bool includeSuccession = true)
        {
            var parts = new List<string>
            {
                "\"saveFormatVersion\":" + version,
                "\"gameVersion\":\"0.1.0\"",
                "\"rulesVersion\":1",
                "\"eventContentVersion\":2",
                "\"year\":1560",
                "\"month\":4",
                "\"day\":2",
                "\"locationId\":\"owari.atsuta\"",
                "\"supplyShortageStage\":1",
                "\"sealedLetterStage\":1",
                "\"marketTallyStage\":0",
                "\"relationships\":[{\"characterId\":\"npc.kuroda.iori\",\"value\":5}]",
                "\"randomState\":" + JsonUtility.ToJson(RandomStateManager.Create(7UL).CaptureState())
            };

            var runtime = new EventRuntimeSaveData();
            if (includeHistory)
            {
                runtime.history = new[]
                {
                    new EventHistorySaveEntry
                    {
                        eventId = "event.earlier",
                        optionId = "option.earlier",
                        resultId = "result.earlier",
                        year = 1560,
                        month = 3,
                        summary = "较早的决定。"
                    },
                    new EventHistorySaveEntry
                    {
                        eventId = "event.later",
                        optionId = "option.later",
                        resultId = "result.later",
                        year = 1560,
                        month = 4,
                        summary = "较晚的决定。"
                    }
                };
                runtime.committedResultIds = new[] { "result.earlier", "result.later" };
            }

            parts.Add("\"eventRuntime\":" + JsonUtility.ToJson(runtime));
            if (includeSuccession)
                parts.Add("\"succession\":" + JsonUtility.ToJson(SuccessionSaveData.CreateDefault()));
            if (includeChronicle)
                parts.Add("\"chronicle\":[]");
            return "{" + string.Join(",", parts) + "}";
        }
    }
}
