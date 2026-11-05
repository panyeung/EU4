using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-020 Task 3: v7 round trip, v6 migration and strict development validation.</summary>
    public sealed class CharacterGrowthSaveTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void V7RoundTrip_PreservesValuesAndPendingPlan()
        {
            _session.InitEducationDemoScenario();
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();

            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(data.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

            string json = JsonUtility.ToJson(data, true);
            Assert.That(SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json).Succeeded, Is.True);

            Assert.That(SaveDataMigrator.TryMigrate(data, json, out SaveData migrated,
                out SaveOperationResult migration), Is.True, migration.Message);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(migrated).Succeeded, Is.True);
                Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 1, 1)));
                Assert.That(restored.Development.TryGetValues(SC020EducationScenario.ChildId,
                    out CharacterGrowthValues values), Is.True);
                Assert.That(values.Ninjutsu, Is.EqualTo(15));
                Assert.That(values.Scholarship, Is.EqualTo(20));
                Assert.That(restored.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                    out EducationYearPlan plan), Is.True);
                Assert.That(plan.Direction, Is.EqualTo(EducationDirection.Scholarship));
                Assert.That(plan.Mentor, Is.EqualTo(SC020EducationScenario.SpouseId));
                Assert.That(restored.ChronicleEntries.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void V6Migration_PreservesSc018AndSc019DataAndAddsEmptyGrowth()
        {
            _session.TryChangeFamilyRelationship(M2FixedScenario.ChieId, 12);
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData legacy = _session.CaptureSave();
            int familyRelationships = legacy.familyRelationships.Length;
            int chronicle = legacy.chronicle.Length;
            int industryFunds = legacy.industry.funds;
            int persons = legacy.succession.persons.Length;

            legacy.saveFormatVersion = 6;
            legacy.rulesVersion = 2;
            legacy.development = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(current.familyRelationships.Length, Is.EqualTo(familyRelationships));
            Assert.That(current.familyRelationships[0].value, Is.EqualTo(12));
            Assert.That(current.chronicle.Length, Is.EqualTo(chronicle));
            Assert.That(current.industry.funds, Is.EqualTo(industryFunds));
            Assert.That(current.industry.slots[0].assignedPersonId,
                Is.EqualTo(M2FixedScenario.HarunobuIdValue));
            Assert.That(current.development.persons.Length, Is.EqualTo(persons));
            foreach (CharacterDevelopmentPersonSaveEntry person in current.development.persons)
            {
                Assert.That(person.ninjutsu, Is.Zero);
                Assert.That(person.martialArts, Is.Zero);
                Assert.That(person.scholarship, Is.Zero);
                Assert.That(person.management, Is.Zero);
                Assert.That(person.social, Is.Zero);
                Assert.That(person.plans, Is.Empty);
                Assert.That(person.results, Is.Empty);
            }
            Assert.That(current.chronicle.All(entry => entry.category != (int)ChronicleCategory.Education),
                Is.True, "migration must not fabricate education history");
        }

        [Test]
        public void V6PayloadWithDevelopment_IsRejected()
        {
            _session.InitEducationDemoScenario();
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 6;
            legacy.rulesVersion = 2;
            string json = JsonUtility.ToJson(legacy, true);

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json);
            Assert.That(presence.Succeeded, Is.False);
            Assert.That(presence.Message, Does.Contain("development"));

            Assert.That(SaveDataMigrator.TryMigrate(legacy, json, out SaveData current,
                out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("development"));
            Assert.That(current, Is.Null);
        }

        [Test]
        public void V7Presence_RequiresNonNullDevelopment()
        {
            const string missing =
                "{\"saveFormatVersion\":7,\"gameVersion\":\"0.1.0\",\"rulesVersion\":3," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[],\"familyRelationships\":[],\"industry\":{}}";
            SaveOperationResult absent = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(missing), missing);
            Assert.That(absent.Succeeded, Is.False);
            Assert.That(absent.Message, Does.Contain("development"));

            const string nullValue =
                "{\"saveFormatVersion\":7,\"gameVersion\":\"0.1.0\",\"rulesVersion\":3," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[],\"familyRelationships\":[],\"industry\":{}," +
                "\"development\":null}";
            SaveOperationResult explicitNull = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(nullValue), nullValue);
            Assert.That(explicitNull.Succeeded, Is.False);
            Assert.That(explicitNull.Message, Does.Contain("development"));
        }

        [Test]
        public void DevelopmentValidation_RejectsMissingDuplicateAndOutOfRangeRows()
        {
            SaveData data = DemoCapture();
            data.development.persons = data.development.persons.Take(2).ToArray();
            AssertInvalid(data, "成长记录");

            data = DemoCapture();
            data.development.persons[1].personId = data.development.persons[0].personId;
            AssertInvalid(data, "重复");

            data = DemoCapture();
            data.development.persons[0].ninjutsu = 101;
            AssertInvalid(data, "0..100");

            data = DemoCapture();
            data.development = null;
            AssertInvalid(data, "development");
        }

        [Test]
        public void DevelopmentValidation_RejectsBadPlanDirectionMentorAndYear()
        {
            SaveData data = DemoCapture();
            data.development.persons[0].plans = new[]
            {
                new EducationPlanSaveEntry { ageYear = 15, direction = 99, mentorPersonId = string.Empty }
            };
            AssertInvalid(data, "方向");

            data = DemoCapture();
            data.development.persons[0].plans = new[]
            {
                new EducationPlanSaveEntry
                {
                    ageYear = 15, direction = (int)EducationDirection.Scholarship,
                    mentorPersonId = "person.unknown"
                }
            };
            AssertInvalid(data, "导师");

            data = DemoCapture();
            data.development.persons[0].plans = new[]
            {
                new EducationPlanSaveEntry
                {
                    ageYear = 16, direction = (int)EducationDirection.FreeGrowth,
                    mentorPersonId = string.Empty
                }
            };
            AssertInvalid(data, "0..15");
        }

        [Test]
        public void SettledSave_ReloadsWithoutReplayingTheYear()
        {
            _session.InitEducationDemoScenario();
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            for (int i = 0; i < 11; i++)
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
                ResolvePending();
            }
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending();
            int educationBefore = EducationEntries(_session);
            Assert.That(educationBefore, Is.EqualTo(1));
            SaveData data = _session.CaptureSave();

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1561, 1, 1)));
                Assert.That(EducationEntries(restored), Is.EqualTo(1));
                Assert.That(restored.Development.TryGetValues(SC020EducationScenario.ChildId,
                    out CharacterGrowthValues values), Is.True);
                Assert.That(values.Scholarship, Is.EqualTo(29));

                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
                ResolvePending(restored);
                Assert.That(EducationEntries(restored), Is.EqualTo(1),
                    "a later month end must not replay the settled age year");
                Assert.That(restored.Development.TryGetValues(SC020EducationScenario.ChildId,
                    out values), Is.True);
                Assert.That(values.Scholarship, Is.EqualTo(29));
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        private SaveData DemoCapture()
        {
            _session.InitEducationDemoScenario();
            return _session.CaptureSave();
        }

        private static void AssertInvalid(SaveData data, string expectedFragment)
        {
            SaveOperationResult result = SaveDataValidator.Validate(data);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain(expectedFragment));
        }

        private static int EducationEntries(GameSession session) =>
            session.ChronicleEntries.Count(entry => entry.Category == ChronicleCategory.Education);

        private void ResolvePending() => ResolvePending(_session);

        private static void ResolvePending(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            string pending = session.PendingEventId;
            SengokuChronicle.Events.EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (SengokuChronicle.Events.EventOptionData option in definition.options)
            {
                if (!session.TryGetEventOptionAvailability(pending, option.optionId,
                        out bool enabled, out _) || !enabled)
                    continue;
                Assert.That(session.TryChooseEventOption(pending, option.optionId,
                    session.CreateEventResultId(pending, option.optionId)).Succeeded, Is.True);
                return;
            }
            Assert.Fail("no enabled option for " + pending);
        }
    }
}
