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
