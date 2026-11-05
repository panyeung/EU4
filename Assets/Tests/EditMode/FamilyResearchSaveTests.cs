using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 3: v8 round trip, validation and v7-to-v8 migration.</summary>
    public sealed class FamilyResearchSaveTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitResearchDemoScenario();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void V8RoundTrip_PreservesPlanResearcherProgressAndKnownKnowledge()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            SaveData data = _session.CaptureSave();

            Assert.That(data.saveFormatVersion, Is.EqualTo(8));
            Assert.That(data.rulesVersion, Is.EqualTo(4));
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
            Assert.That(data.research.researchId,
                Is.EqualTo(FamilyResearchCatalog.SmokeMixingResearchId));
            Assert.That(data.research.researcherPersonId,
                Is.EqualTo(SC021FamilyResearchScenario.HeadId.Value));
            Assert.That(data.research.progress, Is.EqualTo(1));
            Assert.That(data.research.knownTechniqueIds, Is.Empty);
            Assert.That(JsonUtility.ToJson(data, true), Does.Not.Contain("mastery"));

            string json = JsonUtility.ToJson(data, true);
            Assert.That(SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json).Succeeded, Is.True);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.ResearchState.HasPlan, Is.True);
                Assert.That(restored.ResearchState.Progress, Is.EqualTo(1));
                Assert.That(restored.ResearchState.ResearcherId,
                    Is.EqualTo(SC021FamilyResearchScenario.HeadId));

                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
                ResolvePending(restored);
                Assert.That(restored.ResearchState.KnownTechniqueIds, Has.Count.EqualTo(1));
                Assert.That(restored.ResearchState.KnownTechniqueIds[0],
                    Is.EqualTo(FamilyResearchCatalog.SmokeMixingTechniqueId));
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void CompletedKnowledge_RoundTripsWithoutReplayingResearch()
        {
            CompleteResearch();
            int chronicle = _session.ChronicleEntries.Count(entry =>
                entry.Category == ChronicleCategory.Research);
            SaveData data = _session.CaptureSave();
            Assert.That(data.research.knownTechniqueIds,
                Is.EqualTo(new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }));
            Assert.That(data.research.researchId, Is.Empty);
            Assert.That(data.research.progress, Is.Zero);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.ResearchState.IsKnownTechnique(
                    FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True);
                Assert.That(restored.ResearchState.HasPlan, Is.False);
                Assert.That(restored.ChronicleEntries.Count(entry =>
                    entry.Category == ChronicleCategory.Research), Is.EqualTo(chronicle));

                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
                ResolvePending(restored);
                Assert.That(restored.ResearchState.KnownTechniqueIds, Has.Count.EqualTo(1));
                Assert.That(restored.ChronicleEntries.Count(entry =>
                    entry.Category == ChronicleCategory.Research), Is.EqualTo(chronicle));
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void PausedPlanWithFormerFamilyResearcher_RoundTripsAndStaysPaused()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();
            // The researcher left the family after the plan started. Saving must keep
            // the paused plan so the player can inspect or replace it later; only an
            // unregistered person ID is invalid.
            data.succession.members = data.succession.members
                .Where(member => member.personId != SC021FamilyResearchScenario.SpouseId.Value)
                .ToArray();
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True,
                "a paused plan whose researcher left the family must still be saveable");

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.ResearchState.HasPlan, Is.True);
                Assert.That(restored.ResearchState.ResearcherId,
                    Is.EqualTo(SC021FamilyResearchScenario.SpouseId));
                Assert.That(restored.ResearchState.Progress, Is.Zero);

                IndustryResources resources = restored.Industry.Resources;
                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
                ResolvePending(restored);

                Assert.That(restored.Industry.Resources, Is.EqualTo(resources),
                    "a paused plan must not charge either resource");
                Assert.That(restored.ResearchState.Progress, Is.Zero,
                    "a paused plan must not progress");
                Assert.That(restored.ResearchState.KnownTechniqueIds, Is.Empty);
                Assert.That(restored.LastResearchReport.Paused, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void V7Migration_AddsEmptyResearchAndPreservesExistingBlocks()
        {
            _session.TryChangeFamilyRelationship(SC021FamilyResearchScenario.SpouseId, 12);
            _session.TryAssignIndustry(IndustryCatalog.FarmId, SC021FamilyResearchScenario.HeadId);
            _session.TryAdvanceToNextMonth();
            ResolvePending(_session);
            SaveData legacy = _session.CaptureSave();
            int familyRelationships = legacy.familyRelationships.Length;
            int chronicle = legacy.chronicle.Length;
            int persons = legacy.development.persons.Length;
            int industryFunds = legacy.industry.funds;

            legacy.saveFormatVersion = 7;
            legacy.rulesVersion = 3;
            legacy.research = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion, Is.EqualTo(8));
            Assert.That(current.rulesVersion, Is.EqualTo(4));
            Assert.That(current.research, Is.Not.Null);
            Assert.That(current.research.researchId, Is.Empty);
            Assert.That(current.research.knownTechniqueIds, Is.Empty);
            Assert.That(current.research.progress, Is.Zero);
            Assert.That(current.research.lastSettledMonthIndex,
                Is.EqualTo(current.industry.lastSettledMonthIndex),
                "migration initializes the prior research month without replaying one");
            Assert.That(current.familyRelationships.Length, Is.EqualTo(familyRelationships));
            Assert.That(current.chronicle.Length, Is.EqualTo(chronicle));
            Assert.That(current.development.persons.Length, Is.EqualTo(persons));
            Assert.That(current.industry.funds, Is.EqualTo(industryFunds));
        }

        [Test]
        public void V7PayloadWithResearch_IsRejected()
        {
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 7;
            legacy.rulesVersion = 3;
            string json = JsonUtility.ToJson(legacy, true);

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json);
            Assert.That(presence.Succeeded, Is.False);
            Assert.That(presence.Message, Does.Contain("research"));

            Assert.That(SaveDataMigrator.TryMigrate(legacy, json, out SaveData current,
                out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("research"));
            Assert.That(current, Is.Null);
        }

        [Test]
        public void V8Presence_RequiresNonNullResearch()
        {
            const string absent =
                "{\"saveFormatVersion\":8,\"gameVersion\":\"0.1.0\",\"rulesVersion\":4," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[],\"familyRelationships\":[],\"industry\":{}," +
                "\"development\":{}}";
            SaveOperationResult missing = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(absent), absent);
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.Message, Does.Contain("research"));

            const string nullValue =
                "{\"saveFormatVersion\":8,\"gameVersion\":\"0.1.0\",\"rulesVersion\":4," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[],\"familyRelationships\":[],\"industry\":{}," +
                "\"development\":{},\"research\":null}";
            SaveOperationResult explicitNull = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(nullValue), nullValue);
            Assert.That(explicitNull.Succeeded, Is.False);
            Assert.That(explicitNull.Message, Does.Contain("research"));
        }

        [Test]
        public void Validation_RejectsUnknownDuplicateNegativeAndOverCapProgress()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);

            SaveData data = _session.CaptureSave();
            data.research.knownTechniqueIds = new[] { "technique.unknown" };
            AssertInvalid(data, "技艺");

            data = _session.CaptureSave();
            data.research.knownTechniqueIds = new[]
            {
                FamilyResearchCatalog.SmokeMixingTechniqueId,
                FamilyResearchCatalog.SmokeMixingTechniqueId
            };
            AssertInvalid(data, "重复");

            data = _session.CaptureSave();
            data.research.progress = -1;
            AssertInvalid(data, "进度");

            data = _session.CaptureSave();
            data.research.progress = 2;
            AssertInvalid(data, "0..1");

            data = _session.CaptureSave();
            data.research.knownTechniqueIds = new[] { FamilyResearchCatalog.SmokeMixingTechniqueId };
            AssertInvalid(data, "冲突");

            data = _session.CaptureSave();
            data.research.researcherPersonId = "person.unknown";
            AssertInvalid(data, "研究者");

            data = _session.CaptureSave();
            data.research = null;
            AssertInvalid(data, "research");
        }

        [Test]
        public void Validation_RejectsMissingKnownTechniqueList()
        {
            SaveData data = _session.CaptureSave();
            data.research.knownTechniqueIds = null;
            AssertInvalid(data, "knownTechniqueIds");
        }

        private void CompleteResearch()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
        }

        private static void AssertInvalid(SaveData data, string expectedFragment)
        {
            SaveOperationResult result = SaveDataValidator.Validate(data);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain(expectedFragment));
        }

        private static void ResolvePending(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            string pending = session.PendingEventId;
            EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (EventOptionData option in definition.options)
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
