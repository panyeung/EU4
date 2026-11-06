using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

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
    }
}
