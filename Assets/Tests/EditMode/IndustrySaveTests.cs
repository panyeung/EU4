using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 5: v6 round trip, v5 migration and non-duplicating loads.</summary>
    public sealed class IndustrySaveTests
    {
        private GameSession _session;
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _directory = Path.Combine(Path.GetTempPath(), "sc019-save-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void V6RoundTrip_PreservesResourcesAssignmentsAndLastSettled()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            _session.TryAssignIndustry(IndustryCatalog.TradingHouseId, M2FixedScenario.ChieId);
            SaveData data = _session.CaptureSave();

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
            Assert.That(SaveDataMigrator.TryMigrate(data, null, out SaveData migrated,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(migrated.industry.funds, Is.EqualTo(30));
            Assert.That(migrated.industry.supplies, Is.EqualTo(12));
            Assert.That(migrated.industry.lastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 3, 1))));
            Assert.That(migrated.industry.slots[0].assignedPersonId,
                Is.EqualTo(M2FixedScenario.HarunobuIdValue));
            Assert.That(migrated.industry.slots[1].assignedPersonId,
                Is.EqualTo(M2FixedScenario.ChieIdValue));
        }

        [Test]
        public void V5Migration_PreservesSC018FamilyRelationshipsAndChronicle()
        {
            _session.TryChangeFamilyRelationship(M2FixedScenario.ChieId, 12);
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            _session.TryAdvanceToNextMonth();
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 5;
            legacy.rulesVersion = 1;
            legacy.industry = null;
            int familyRelationships = legacy.familyRelationships.Length;
            int chronicle = legacy.chronicle.Length;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(current.familyRelationships.Length, Is.EqualTo(familyRelationships));
            Assert.That(current.familyRelationships[0].value, Is.EqualTo(12));
            Assert.That(current.chronicle.Length, Is.EqualTo(chronicle));
            Assert.That(current.industry.funds, Is.EqualTo(30));
            Assert.That(current.industry.lastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 5, 1)) - 1),
                "migration initializes the industry at the legacy date without replaying production");
        }

        [Test]
        public void V5PayloadWithIndustry_IsRejected()
        {
            SaveData data = _session.CaptureSave();
            data.saveFormatVersion = 5;
            data.rulesVersion = 1;
            data.development = null;
            string json = JsonUtility.ToJson(data, true);

            Assert.That(SaveDataValidator.ValidatePresence(JsonUtility.FromJson<SaveDataPresenceProbe>(json), json)
                .Succeeded, Is.False);
            Assert.That(SaveDataMigrator.TryMigrate(data, json, out _, out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("industry"));
        }

        [Test]
        public void V4PayloadWithIndustry_IsRejected()
        {
            SaveData data = _session.CaptureSave();
            data.saveFormatVersion = 4;
            data.rulesVersion = 1;
            data.familyRelationships = new FamilyRelationshipSaveEntry[0];
            data.development = null;
            string json = JsonUtility.ToJson(data, true);

            Assert.That(SaveDataMigrator.TryMigrate(data, json, out _, out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("industry"));
        }

        [Test]
        public void V6Presence_RequiresIndustry()
        {
            const string json =
                "{\"saveFormatVersion\":6,\"gameVersion\":\"0.1.0\",\"rulesVersion\":2," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[],\"familyRelationships\":[]}";

            SaveOperationResult result = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("industry"));
        }

        [Test]
        public void V6NullIndustry_IsRejectedWithoutDefaulting()
        {
            const string json =
                "{\"saveFormatVersion\":6,\"gameVersion\":\"0.1.0\",\"rulesVersion\":2," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[],\"familyRelationships\":[]," +
                "\"industry\":null}";

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json);
            Assert.That(presence.Succeeded, Is.False);
            Assert.That(presence.Message, Does.Contain("industry"));

            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            Assert.That(SaveDataMigrator.TryMigrate(parsed, json, out SaveData migrated,
                out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("industry"));
            Assert.That(migrated, Is.Null);
        }

        [Test]
        public void V6ObjectWithNullIndustry_IsRejectedWithoutDefaulting()
        {
            SaveData data = _session.CaptureSave();
            data.industry = null;

            Assert.That(SaveDataMigrator.TryMigrate(data, null, out SaveData current,
                out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("industry"));
            Assert.That(current, Is.Null);
        }

        [Test]
        public void Load_DoesNotAppendOrSettle()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            var store = new SaveSlotStore(_directory);
            Assert.That(_session.SaveTo(store).Succeeded, Is.True);
            int chronicle = _session.ChronicleEntries.Count;

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.LoadFrom(store).Succeeded, Is.True);
                Assert.That(restored.ChronicleEntries.Count, Is.EqualTo(chronicle));
                Assert.That(restored.Industry.Resources.Funds, Is.EqualTo(30));
                Assert.That(restored.Industry.Slot(IndustryCatalog.FarmId).AssigneeName,
                    Is.EqualTo("明智 晴信"));
                Assert.That(restored.LastIndustryReport, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void LoadThenEndMonth_DoesNotDuplicateSettlement()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.TryChooseEventOption(M2FixedScenario.PledgeEventId,
                M2FixedScenario.BreakPromiseOptionId,
                _session.CreateEventResultId(M2FixedScenario.PledgeEventId,
                    M2FixedScenario.BreakPromiseOptionId)).Succeeded, Is.True);
            var store = new SaveSlotStore(_directory);
            Assert.That(_session.SaveTo(store).Succeeded, Is.True);
            int monthlyBefore = CountMonthly(_session);
            int settledBefore = _session.Industry.LastSettledMonthIndex;

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.LoadFrom(store).Succeeded, Is.True);
                Assert.That(restored.Industry.LastSettledMonthIndex, Is.EqualTo(settledBefore));
                Assert.That(CountMonthly(restored), Is.EqualTo(monthlyBefore));

                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
                Assert.That(restored.Industry.LastSettledMonthIndex, Is.EqualTo(settledBefore + 1));
                Assert.That(CountMonthly(restored), Is.EqualTo(monthlyBefore + 1));
                Assert.That(restored.Industry.Resources.Funds, Is.EqualTo(32));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void V5Migration_InitializesIndustriesWithoutRetroactiveProduction()
        {
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 5;
            legacy.rulesVersion = 1;
            legacy.industry = null;
            int chronicle = legacy.chronicle.Length;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(current.industry.funds, Is.EqualTo(30));
            Assert.That(current.industry.supplies, Is.EqualTo(12));
            Assert.That(current.industry.intelligence, Is.Zero);
            Assert.That(current.industry.slots[0].assignedPersonId, Is.Empty);
            Assert.That(current.industry.slots[1].assignedPersonId, Is.Empty);
            Assert.That(current.chronicle.Length, Is.EqualTo(chronicle));
        }

        private static int CountMonthly(GameSession session)
        {
            int count = 0;
            foreach (FamilyChronicleEntry entry in session.ChronicleEntries)
                if (entry.Category == ChronicleCategory.Industry && entry.SubjectId == "industry.monthly")
                    count++;
            return count;
        }
    }
}
