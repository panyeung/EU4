using System;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 3: migration-chain coverage for the v8 research block.</summary>
    public sealed class SaveDataMigratorTests
    {
        [Test]
        public void V7ToV8Migration_InitializesEmptyResearchAtThePriorMonth()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario();
                SaveData legacy = session.CaptureSave();
                GameDate date = session.WorldClock.CurrentDate;
                legacy.saveFormatVersion = 7;
                legacy.rulesVersion = 3;
                legacy.research = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
                Assert.That(current.research.researchId, Is.Empty);
                Assert.That(current.research.knownTechniqueIds, Is.Empty);
                Assert.That(current.research.lastSettledMonthIndex,
                    Is.EqualTo(IndustryCalendar.MonthIndex(date) - 1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V7ToV8Migration_AcceptsLegitimateDevelopmentJsonAndPreservesIt()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                // SC-020's fixed scenario gives the v7 development block a real,
                // non-default payload so this test would fail if the generic legacy
                // "development" rejection were applied to v7 again.
                session.InitEducationDemoScenario();
                SaveData captured = session.CaptureSave();
                captured.saveFormatVersion = 7;
                captured.rulesVersion = 3;
                string json = LegitimateV7Json(captured);

                Assert.That(json, Does.Not.Contain("\"research\":"));
                Assert.That(json, Does.Contain("\"development\":"));
                Assert.That(json, Does.Contain("\"industry\":"));

                SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                    JsonUtility.FromJson<SaveDataPresenceProbe>(json), json);
                Assert.That(presence.Succeeded, Is.True, presence.Message);

                // Load the file the way SaveSlotStore does: JsonUtility fills the probe
                // and parsed data, while the raw JSON remains the presence authority.
                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                Assert.That(SaveDataMigrator.TryMigrate(parsed, json, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
                Assert.That(current.research, Is.Not.Null);
                Assert.That(current.research.researchId, Is.Empty);
                Assert.That(current.research.researcherPersonId, Is.Empty);
                Assert.That(current.research.progress, Is.Zero);
                Assert.That(current.research.knownTechniqueIds, Is.Empty);
                Assert.That(current.research.lastSettledMonthIndex,
                    Is.EqualTo(current.industry.lastSettledMonthIndex),
                    "migration initializes the prior research month without replaying one");

                Assert.That(JsonUtility.ToJson(current.development),
                    Is.EqualTo(JsonUtility.ToJson(parsed.development)),
                    "the existing v7 growth rows must survive migration unchanged");
                CharacterDevelopmentPersonSaveEntry child = null;
                foreach (CharacterDevelopmentPersonSaveEntry person in current.development.persons)
                    if (person.personId == SC020EducationScenario.ChildIdValue)
                        child = person;
                Assert.That(child, Is.Not.Null);
                Assert.That(child.ninjutsu, Is.EqualTo(SC020EducationScenario.ChildNinjutsu));
                Assert.That(child.scholarship, Is.EqualTo(SC020EducationScenario.ChildScholarship));
                Assert.That(child.plans, Is.Empty);
                Assert.That(child.results, Is.Empty);

                Assert.That(JsonUtility.ToJson(current.industry),
                    Is.EqualTo(JsonUtility.ToJson(parsed.industry)));
                Assert.That(JsonUtility.ToJson(current.familyRelationships),
                    Is.EqualTo(JsonUtility.ToJson(parsed.familyRelationships)));
                Assert.That(JsonUtility.ToJson(current.chronicle),
                    Is.EqualTo(JsonUtility.ToJson(parsed.chronicle)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void CurrentVersionMigration_PreservesGendersAndPregnancies()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                GameDate date = session.WorldClock.CurrentDate;
                Assert.That(FamilyPregnancyRecord.TryCreate(
                    SC022FamilyFormationScenario.SpouseId,
                    SC022FamilyFormationScenario.HeadId, date,
                    FamilyPregnancyRecord.AddMonths(date,
                        FamilyPregnancyRecord.GestationMonths),
                    out FamilyPregnancyRecord record, out string error), Is.True, error);
                Assert.That(session.Succession.World.Pregnancies.TryAdd(record, out error),
                    Is.True, error);

                SaveData current = session.CaptureSave();
                Assert.That(SaveDataMigrator.TryMigrate(current, null, out SaveData migrated,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(migrated.succession.pregnancies, Has.Length.EqualTo(1));
                Assert.That(migrated.succession.pregnancies[0].gestatingPersonId,
                    Is.EqualTo(SC022FamilyFormationScenario.SpouseId.Value));
                Assert.That(GenderOf(migrated, SC022FamilyFormationScenario.SpouseId),
                    Is.EqualTo(PersonGender.Female));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V12ToV13Migration_ResolvesOnlyAuthoredGendersAndAddsNoPregnancies()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                var unknownId = new PersonId("person.sc022.custom.unknown");
                Assert.That(session.Succession.World.TryAddPerson(
                    new PersonState(unknownId, "无名氏"),
                    new SuccessionPersonLife(1540, true, false), out string addError),
                    Is.True, addError);
                Assert.That(session.Development.TryAddPerson(unknownId,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0),
                    out string developmentError), Is.True, developmentError);

                SaveData legacy = session.CaptureSave();
                int persons = legacy.succession.persons.Length;
                int kinship = legacy.succession.kinship.Length;
                legacy.saveFormatVersion = 12;
                legacy.rulesVersion = SaveData.CurrentRulesVersion;
                foreach (SuccessionPersonSaveEntry person in legacy.succession.persons)
                    person.gender = (int)PersonGender.Unspecified;
                legacy.succession.pregnancies = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.persons, Has.Length.EqualTo(persons));
                Assert.That(current.succession.kinship, Has.Length.EqualTo(kinship),
                    "v12 migration must preserve every kinship fact");
                Assert.That(current.succession.pregnancies, Is.Empty);

                Assert.That(GenderOf(current, SC022FamilyFormationScenario.HeadId),
                    Is.EqualTo(PersonGender.Male));
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.SpouseId),
                    Is.EqualTo(PersonGender.Female));
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.MarriageCandidateOneId),
                    Is.EqualTo(PersonGender.Female));
                Assert.That(GenderOf(current, unknownId),
                    Is.EqualTo(PersonGender.Unspecified),
                    "unknown IDs must never be inferred from names or kinship");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V12ToV13Migration_InitializesEmptyPregnanciesEvenWhenBlockMaterializes()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 12;
                legacy.rulesVersion = SaveData.CurrentRulesVersion;
                foreach (SuccessionPersonSaveEntry person in legacy.succession.persons)
                    person.gender = (int)PersonGender.Unspecified;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.pregnancies, Is.Empty);
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.HeadId),
                    Is.EqualTo(PersonGender.Male));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [TestCase(9)]
        [TestCase(6)]
        public void SupportedPreV13Versions_ResolveCatalogGendersAndKeepUnknownUnspecified(
            int version)
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                var unknownId = new PersonId("person.sc022.legacy.unknown");
                Assert.That(session.Succession.World.TryAddPerson(
                    new PersonState(unknownId, "旧档无名"),
                    new SuccessionPersonLife(1540, true, false), out string addError),
                    Is.True, addError);
                Assert.That(session.Development.TryAddPerson(unknownId,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0),
                    out string developmentError), Is.True, developmentError);

                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = version;
                legacy.rulesVersion = version >= 8
                    ? SaveData.CurrentRulesVersion
                    : 2;
                foreach (SuccessionPersonSaveEntry person in legacy.succession.persons)
                    person.gender = (int)PersonGender.Unspecified;
                legacy.succession.pregnancies = null;
                if (version == 6)
                {
                    legacy.development = null;
                    legacy.research = null;
                    legacy.familyDoctrine = null;
                }

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.pregnancies, Is.Empty);
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.HeadId),
                    Is.EqualTo(PersonGender.Male), "authored ID resolves from catalog");
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.SpouseId),
                    Is.EqualTo(PersonGender.Female));
                Assert.That(GenderOf(current, unknownId),
                    Is.EqualTo(PersonGender.Unspecified),
                    "unknown IDs must not be inferred");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static PersonGender GenderOf(SaveData data, PersonId personId)
        {
            SuccessionPersonSaveEntry entry = Array.Find(data.succession.persons,
                person => person.personId == personId.Value);
            Assert.That(entry, Is.Not.Null, personId.Value);
            return (PersonGender)entry.gender;
        }

        [Test]
        public void V11ToV12Migration_AddsOnlyEmptyMemories()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.AdvanceOneDay();
                SaveData legacy = session.CaptureSave();
                string dailyActionsJson = JsonUtility.ToJson(legacy.dailyActions);
                legacy.saveFormatVersion = 11;
                legacy.rulesVersion = SaveData.CurrentRulesVersion;
                foreach (NpcAutonomyPersonSaveEntry entry in legacy.npcAutonomy.persons)
                    entry.memories = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.npcAutonomy.persons, Has.Length.EqualTo(2));
                foreach (NpcAutonomyPersonSaveEntry entry in current.npcAutonomy.persons)
                    Assert.That(entry.memories, Is.Empty);
                Assert.That(JsonUtility.ToJson(current.dailyActions),
                    Is.EqualTo(dailyActionsJson),
                    "v11 migration must not replay or alter daily actions");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V6ToV8Migration_AddsResearchAndPreservesTheRest()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario();
                SaveData legacy = session.CaptureSave();
                int persons = legacy.succession.persons.Length;
                legacy.saveFormatVersion = 6;
                legacy.rulesVersion = 2;
                legacy.development = null;
                legacy.research = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.development.persons.Length, Is.EqualTo(persons));
                Assert.That(current.research, Is.Not.Null);
                Assert.That(current.research.knownTechniqueIds, Is.Empty);
                Assert.That(current.research.lastSettledMonthIndex,
                    Is.EqualTo(current.industry.lastSettledMonthIndex));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V7Migration_RejectsAConflictingResearchPayload()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario();
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 7;
                legacy.rulesVersion = 3;
                string json = UnityEngine.JsonUtility.ToJson(legacy, true);

                Assert.That(SaveDataMigrator.TryMigrate(legacy, json, out SaveData current,
                    out SaveOperationResult result), Is.False);
                Assert.That(result.Message, Does.Contain("research"));
                Assert.That(current, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        /// <summary>
        /// Builds the exact v7 shape a real file has: every block v7 owned, with the
        /// development payload intact and no research root property at all.
        /// </summary>
        private static string LegitimateV7Json(SaveData data)
        {
            // Serialize the whole object so Unity emits the real array/object shape,
            // then drop the trailing research property that did not exist in v7.
            string json = JsonUtility.ToJson(data, true);
            int research = json.LastIndexOf("\"research\":", StringComparison.Ordinal);
            Assert.That(research, Is.GreaterThan(0));
            string prefix = json.Substring(0, research).TrimEnd();
            Assert.That(prefix.EndsWith(","), Is.True);
            return prefix.Substring(0, prefix.Length - 1) + "\n}";
        }
    }
}
