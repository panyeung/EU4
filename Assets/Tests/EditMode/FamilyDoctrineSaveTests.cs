using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-024 Task 4: v9 doctrine/route persistence, v8 default migration, strict
    /// validation and atomic load failure. Historical months and event outcomes are
    /// never replayed by a migration or a round trip.
    /// </summary>
    public sealed class FamilyDoctrineSaveTests
    {
        private readonly List<GameSession> _sessions = new List<GameSession>();
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "SengokuChronicleTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameSession session in _sessions)
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            _sessions.Clear();

            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private GameSession NewSession()
        {
            GameSession session = GameSession.NewGame();
            _sessions.Add(session);
            return session;
        }

        [Test]
        public void V8Migration_AddsDefaultDoctrineAndPreservesExistingBlocks()
        {
            GameSession session = NewSession();
            session.InitResearchDemoScenario();
            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId,
                SC021FamilyResearchScenario.SpouseId).Succeeded, Is.True, session.LastIndustryMessage);
            Assert.That(session.TryChangeFamilyRelationship(SC021FamilyResearchScenario.HeirId, 12)
                .Succeeded, Is.True);
            Assert.That(session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True, session.LastResearchMessage);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.ResearchState.Progress, Is.EqualTo(1));

            SaveData legacy = session.CaptureSave();
            string industry = JsonUtility.ToJson(legacy.industry);
            string development = JsonUtility.ToJson(legacy.development);
            string research = JsonUtility.ToJson(legacy.research);
            string succession = JsonUtility.ToJson(legacy.succession);
            string relationships = JsonUtility.ToJson(legacy.familyRelationships);
            string chronicle = JsonUtility.ToJson(legacy.chronicle);
            string eventRuntime = JsonUtility.ToJson(legacy.eventRuntime);
            int chronicleCount = legacy.chronicle.Length;

            legacy.saveFormatVersion = 8;
            legacy.rulesVersion = 4;
            legacy.eventContentVersion = 2;
            legacy.familyDoctrine = null;
            legacy.activeEventScenarioId = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(current.eventContentVersion, Is.EqualTo(2),
                "migration preserves the legacy additive content version");
            Assert.That(current.familyDoctrine, Is.Not.Null);
            Assert.That(current.familyDoctrine.creedId, Is.EqualTo("RighteousnessFirst"));
            Assert.That(current.familyDoctrine.ambitions, Is.Empty);
            Assert.That(current.activeEventScenarioId, Is.Empty,
                "a v8 save must not invent an SC-024 route");

            Assert.That(JsonUtility.ToJson(current.industry), Is.EqualTo(industry));
            Assert.That(JsonUtility.ToJson(current.development), Is.EqualTo(development));
            Assert.That(JsonUtility.ToJson(current.research), Is.EqualTo(research));
            Assert.That(JsonUtility.ToJson(current.succession), Is.EqualTo(succession));
            Assert.That(JsonUtility.ToJson(current.familyRelationships), Is.EqualTo(relationships));
            Assert.That(JsonUtility.ToJson(current.chronicle), Is.EqualTo(chronicle));
            Assert.That(current.chronicle, Has.Length.EqualTo(chronicleCount),
                "migration must not append a chronicle record");
            Assert.That(JsonUtility.ToJson(current.eventRuntime), Is.EqualTo(eventRuntime));
        }

        [Test]
        public void V9RoundTrip_RestoresCreedMultipleHeadsBaselineAndSc024Route()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, session.LastIndustryMessage);

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Choose(session, SC024EventIds.RighteousnessReformMeritOptionId);
            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.MeritFirst));

            Assert.That(session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                M2FixedScenario.ChieId).Succeeded, Is.True, session.LastResearchMessage);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Choose(session, SC024EventIds.MeritKeepOptionId);
            Assert.That(session.ResearchState.Progress, Is.EqualTo(1));

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.ResearchState.KnownTechniqueIds,
                Is.EqualTo(new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }));
            Assert.That(session.CurrentHeadAmbition.IsComplete, Is.True,
                "the default household ambition completes after three qualifying months");

            SuccessionResult handoff = SuccessionService.BeginRetirement(session.Succession,
                session.WorldClock.CurrentDate);
            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(session.Succession.HeadPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(session.TryStartCurrentHeadAmbition(FamilyAmbitionKind.PassDownTechnique),
                Is.True, session.LastFamilyDoctrineMessage);

            SaveData data = session.CaptureSave();
            Assert.That(data.activeEventScenarioId, Is.EqualTo(SC024FamilyDoctrineScenario.ScenarioId));
            Assert.That(data.familyDoctrine.creedId, Is.EqualTo("MeritFirst"));
            Assert.That(data.familyDoctrine.ambitions, Has.Length.EqualTo(2));
            FamilyAmbitionSaveEntry oldHead = data.familyDoctrine.ambitions[0];
            FamilyAmbitionSaveEntry newHead = data.familyDoctrine.ambitions[1];
            Assert.That(oldHead.headPersonId, Is.EqualTo(M2FixedScenario.HarunobuId.Value));
            Assert.That(oldHead.ambitionId, Is.EqualTo(nameof(FamilyAmbitionKind.DevelopHousehold)));
            Assert.That(oldHead.progress, Is.EqualTo(FamilyDoctrineState.DevelopHouseholdTarget));
            Assert.That(oldHead.isComplete, Is.True);
            Assert.That(newHead.headPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId.Value));
            Assert.That(newHead.ambitionId, Is.EqualTo(nameof(FamilyAmbitionKind.PassDownTechnique)));
            Assert.That(newHead.progress, Is.Zero);
            Assert.That(newHead.isComplete, Is.False);
            Assert.That(newHead.knownTechniqueIds,
                Is.EqualTo(new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }),
                "the selection-time technique snapshot must be stable across a save");

            string json = JsonUtility.ToJson(data, true);
            Assert.That(SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(json), json).Succeeded, Is.True);
            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            Assert.That(SaveDataValidator.Validate(parsed).Succeeded, Is.True);

            GameSession restored = NewSession();
            restored.InitNewGame();
            Assert.That(restored.TryApplySave(parsed).Succeeded, Is.True);
            Assert.That(restored.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.MeritFirst));
            Assert.That(restored.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(restored.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.PassDownTechnique));
            Assert.That(restored.CurrentHeadAmbition.KnownTechniqueIds,
                Is.EqualTo(new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }));
            Assert.That(restored.TryGetFamilyAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord retained), Is.True);
            Assert.That(retained.Kind, Is.EqualTo(FamilyAmbitionKind.DevelopHousehold));
            Assert.That(retained.Progress, Is.EqualTo(FamilyDoctrineState.DevelopHouseholdTarget));
            Assert.That(restored.CaptureSave().activeEventScenarioId,
                Is.EqualTo(SC024FamilyDoctrineScenario.ScenarioId));

            Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
            ResolvePending(restored);
            Assert.That(restored.PendingEventId, Is.Empty,
                "the SC-024 route must not fall back to SC-013 events");
            Assert.That(restored.CaptureSave().activeEventScenarioId,
                Is.EqualTo(SC024FamilyDoctrineScenario.ScenarioId));
        }

        [Test]
        public void Sc024RoundTrip_KeepsRouteForSubsequentRouteAppropriateScheduling()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Choose(session, SC024EventIds.RighteousnessReformMeritOptionId);
            SaveData data = session.CaptureSave();
            Assert.That(data.activeEventScenarioId, Is.EqualTo(SC024FamilyDoctrineScenario.ScenarioId));

            GameSession restored = NewSession();
            restored.InitNewGame();
            Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
            Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
            Assert.That(restored.PendingEventId, Is.EqualTo(SC024EventIds.MeritCouncilEventId),
                "the restored sc024 route must keep scheduling SC-024 events");
        }

        [Test]
        public void NormalSc013Save_RoundTripsWithoutSc024RouteOrDoctrineLeak()
        {
            GameSession normal = NewSession();
            normal.InitNewGame();
            SaveData data = normal.CaptureSave();
            Assert.That(data.activeEventScenarioId, Is.Empty);
            Assert.That(data.familyDoctrine.creedId, Is.EqualTo("RighteousnessFirst"));
            Assert.That(data.familyDoctrine.ambitions, Is.Empty);

            GameSession restored = NewSession();
            restored.InitNewGame();
            Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
            Assert.That(restored.CurrentHeadAmbition, Is.Null);
            Assert.That(restored.CaptureSave().activeEventScenarioId, Is.Empty);
            Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
            Assert.That(restored.PendingEventId, Is.EqualTo(SC013EventIds.PledgeEventId));
        }

        [Test]
        public void Capture_OrdersHeadRecordsByStableId()
        {
            var state = new FamilyDoctrineState();
            Assert.That(state.TryStartAmbition(M2FixedScenario.TokihimeId,
                FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _), Is.True);
            Assert.That(state.TryStartAmbition(M2FixedScenario.HarunobuId,
                FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _), Is.True);

            FamilyDoctrineSaveData captured = FamilyDoctrineSaveData.Capture(state);
            Assert.That(captured.ambitions, Has.Length.EqualTo(2));
            Assert.That(captured.ambitions[0].headPersonId,
                Is.EqualTo(M2FixedScenario.HarunobuId.Value));
            Assert.That(captured.ambitions[1].headPersonId,
                Is.EqualTo(M2FixedScenario.TokihimeId.Value));
            Assert.That(string.CompareOrdinal(captured.ambitions[0].headPersonId,
                captured.ambitions[1].headPersonId), Is.LessThan(0));
        }

        [Test]
        public void Validation_RejectsInvalidDoctrineRows()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            string head = session.Succession.HeadPersonId.Value;

            SaveData data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                Entry(head, FamilyAmbitionKind.DevelopHousehold, 0)
            };
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

            data = session.CaptureSave();
            data.familyDoctrine.creedId = "NotACreed";
            AssertInvalid(data, "家训");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                EntryWithAmbitionId(head, FamilyAmbitionKind.DevelopHousehold, 0,
                    "NotAnAmbition")
            };
            AssertInvalid(data, "抱负");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[] { Entry("person.unknown",
                FamilyAmbitionKind.DevelopHousehold, 0) };
            AssertInvalid(data, "未知人物");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[] { Entry("",
                FamilyAmbitionKind.DevelopHousehold, 0) };
            AssertInvalid(data, "无效");

            data = session.CaptureSave();
            FamilyAmbitionSaveEntry duplicate = Entry(head,
                FamilyAmbitionKind.DevelopHousehold, 0);
            data.familyDoctrine.ambitions = new[] { duplicate, duplicate };
            AssertInvalid(data, "重复");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                Entry(head, FamilyAmbitionKind.DevelopHousehold, -1)
            };
            AssertInvalid(data, "进度");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                Entry(head, FamilyAmbitionKind.DevelopHousehold,
                    FamilyDoctrineState.DevelopHouseholdTarget + 1)
            };
            AssertInvalid(data, "进度");

            data = session.CaptureSave();
            FamilyAmbitionSaveEntry wrongTarget = Entry(head,
                FamilyAmbitionKind.DevelopHousehold, 0);
            wrongTarget.target = 999;
            data.familyDoctrine.ambitions = new[] { wrongTarget };
            AssertInvalid(data, "目标");

            data = session.CaptureSave();
            FamilyAmbitionSaveEntry wrongCompletion = Entry(head,
                FamilyAmbitionKind.DevelopHousehold, 0);
            wrongCompletion.isComplete = true;
            data.familyDoctrine.ambitions = new[] { wrongCompletion };
            AssertInvalid(data, "完成状态");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                EntryWithTechniques(head, FamilyAmbitionKind.DevelopHousehold, 0, null)
            };
            AssertInvalid(data, "knownTechniqueIds");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                EntryWithTechniques(head, FamilyAmbitionKind.PassDownTechnique, 0,
                    "technique.unknown")
            };
            AssertInvalid(data, "技艺");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                EntryWithTechniques(head, FamilyAmbitionKind.PassDownTechnique, 0,
                    FamilyResearchCatalog.SmokeMixingTechniqueId,
                    FamilyResearchCatalog.SmokeMixingTechniqueId)
            };
            AssertInvalid(data, "重复");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new[]
            {
                EntryWithTechniques(head, FamilyAmbitionKind.PassDownTechnique, 0,
                    FamilyResearchCatalog.SmokeMixingTechniqueId)
            };
            AssertInvalid(data, "研究知识",
                "a technique baseline that the saved research knowledge does not contain is impossible");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = new FamilyAmbitionSaveEntry[] { null };
            AssertInvalid(data, "为空");
        }

        [Test]
        public void DeterministicOrdering_IsEnforcedForHeadRows()
        {
            GameSession session = NewSession();
            session.InitNewGame();
            SaveData data = session.CaptureSave();
            string first = data.succession.persons[0].personId;
            string second = data.succession.persons[1].personId;
            if (string.CompareOrdinal(first, second) > 0)
            {
                string swap = first;
                first = second;
                second = swap;
            }

            data.familyDoctrine.ambitions = new[]
            {
                Entry(second, FamilyAmbitionKind.DevelopHousehold, 0),
                Entry(first, FamilyAmbitionKind.DevelopHousehold, 0)
            };
            AssertInvalid(data, "顺序");

            data.familyDoctrine.ambitions = new[]
            {
                Entry(first, FamilyAmbitionKind.DevelopHousehold, 0),
                Entry(second, FamilyAmbitionKind.DevelopHousehold, 0)
            };
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
        }

        [Test]
        public void Validation_RejectsUnknownRouteAndMissingV9Blocks()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();

            SaveData data = session.CaptureSave();
            data.activeEventScenarioId = "sc999";
            AssertInvalid(data, "路线");

            data = session.CaptureSave();
            data.activeEventScenarioId = null;
            AssertInvalid(data, "路线");

            data = session.CaptureSave();
            data.familyDoctrine = null;
            AssertInvalid(data, "familyDoctrine");

            data = session.CaptureSave();
            data.familyDoctrine.ambitions = null;
            AssertInvalid(data, "ambitions");

            SaveData valid = session.CaptureSave();
            string json = JsonUtility.ToJson(valid);
            SaveDataPresenceProbe missingDoctrine =
                JsonUtility.FromJson<SaveDataPresenceProbe>(json);
            missingDoctrine.familyDoctrine = null;
            SaveOperationResult doctrinePresence =
                SaveDataValidator.ValidatePresence(missingDoctrine, json);
            Assert.That(doctrinePresence.Succeeded, Is.False);
            Assert.That(doctrinePresence.Message, Does.Contain("familyDoctrine"));

            int routeIndex = json.LastIndexOf(",\"activeEventScenarioId\"",
                StringComparison.Ordinal);
            Assert.That(routeIndex, Is.GreaterThan(0));
            string withoutRoute = json.Substring(0, routeIndex) + "}";
            SaveOperationResult routePresence = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(withoutRoute), withoutRoute);
            Assert.That(routePresence.Succeeded, Is.False);
            Assert.That(routePresence.Message, Does.Contain("activeEventScenarioId"));
        }

        [Test]
        public void FailedLoad_LeavesTheExistingSessionUnchanged()
        {
            GameSession source = NewSession();
            source.InitFamilyDoctrineDemoScenario();
            Assert.That(source.TryAdvanceToNextMonth(), Is.True, source.LastEventMessage);
            Choose(source, SC024EventIds.RighteousnessReformMeritOptionId);

            GameSession target = NewSession();
            target.InitFamilyDoctrineDemoScenario();
            string before = JsonUtility.ToJson(target.CaptureSave());

            SaveData unknownCreed = source.CaptureSave();
            unknownCreed.familyDoctrine.creedId = "NotACreed";
            Assert.That(target.TryApplySave(unknownCreed).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(target.CaptureSave()), Is.EqualTo(before),
                "a rejected doctrine save must not alter the live session");

            SaveData wrongRoute = source.CaptureSave();
            wrongRoute.activeEventScenarioId = string.Empty;
            Assert.That(target.TryApplySave(wrongRoute).Succeeded, Is.False,
                "SC-024 history must not be accepted without the sc024 route");
            Assert.That(JsonUtility.ToJson(target.CaptureSave()), Is.EqualTo(before),
                "a rejected event route must not alter the live session");
        }

        [Test]
        public void SaveSlotStore_RawV8JsonWithoutV9Fields_MigratesToDefaults()
        {
            GameSession session = NewSession();
            session.InitNewGame();
            string v8Json = V8Json(session, includeV9Fields: false);
            Assert.That(v8Json, Does.Not.Contain("\"familyDoctrine\""));
            Assert.That(v8Json, Does.Not.Contain("\"activeEventScenarioId\""));

            Directory.CreateDirectory(_directory);
            var store = new SaveSlotStore(_directory);
            File.WriteAllText(store.FilePath, v8Json);

            SaveLoadResult loaded = store.Load();

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(loaded.Data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(loaded.Data.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(loaded.Data.familyDoctrine.creedId, Is.EqualTo("RighteousnessFirst"));
            Assert.That(loaded.Data.familyDoctrine.ambitions, Is.Empty);
            Assert.That(loaded.Data.activeEventScenarioId, Is.Empty);
            Assert.That(File.ReadAllText(store.FilePath), Is.EqualTo(v8Json),
                "loading a legacy slot must not rewrite the source file");
        }

        [Test]
        public void SaveSlotStore_RawV8JsonWithV9OnlyRoots_IsRejectedAndLeavesSessionUnchanged()
        {
            GameSession session = NewSession();
            session.InitNewGame();
            Directory.CreateDirectory(_directory);
            var store = new SaveSlotStore(_directory);

            File.WriteAllText(store.FilePath, V8Json(session, includeV9Fields: true));
            SaveLoadResult withDoctrine = store.Load();
            Assert.That(withDoctrine.Succeeded, Is.False);
            Assert.That(withDoctrine.Message, Does.Contain("familyDoctrine"));

            string routeOnly = InsertRootProperty(V8Json(session, includeV9Fields: false),
                "\"activeEventScenarioId\":\"sc024\"");
            File.WriteAllText(store.FilePath, routeOnly);
            SaveLoadResult withRoute = store.Load();
            Assert.That(withRoute.Succeeded, Is.False);
            Assert.That(withRoute.Message, Does.Contain("activeEventScenarioId"));

            GameSession target = NewSession();
            target.InitNewGame();
            string before = JsonUtility.ToJson(target.CaptureSave());
            File.WriteAllText(store.FilePath, V8Json(session, includeV9Fields: true));

            SaveOperationResult load = target.LoadFrom(store);

            Assert.That(load.Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(target.CaptureSave()), Is.EqualTo(before),
                "a rejected legacy payload must not alter the current session");
        }

        [Test]
        public void InMemoryV8_WithV9Blocks_StillUsesTheLegacyMigrationApi()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            SaveData legacy = session.CaptureSave();
            Assert.That(legacy.familyDoctrine, Is.Not.Null);
            Assert.That(legacy.activeEventScenarioId,
                Is.EqualTo(SC024FamilyDoctrineScenario.ScenarioId));
            legacy.saveFormatVersion = 8;
            legacy.rulesVersion = 4;
            legacy.eventContentVersion = 2;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.familyDoctrine, Is.Not.Null);
            Assert.That(current.familyDoctrine.creedId, Is.EqualTo("RighteousnessFirst"));
            Assert.That(current.familyDoctrine.ambitions, Is.Empty);
            Assert.That(current.activeEventScenarioId, Is.Empty,
                "the null-raw in-memory API keeps the documented legacy default");
        }

        private static string V8Json(GameSession session, bool includeV9Fields)
        {
            SaveData data = session.CaptureSave();
            data.saveFormatVersion = 8;
            data.rulesVersion = 4;
            data.eventContentVersion = 2;
            string json = JsonUtility.ToJson(data);
            if (includeV9Fields)
                return json;

            return RemoveRootProperty(RemoveRootProperty(RemoveRootProperty(json,
                "familyDoctrine"), "activeEventScenarioId"), "dailyActions");
        }

        private static string InsertRootProperty(string json, string property)
        {
            int end = json.LastIndexOf('}');
            Assert.That(end, Is.GreaterThan(0));
            return json.Substring(0, end) + "," + property + "}";
        }

        private static string RemoveRootProperty(string json, string propertyName)
        {
            int depth = 0;
            int index = 0;
            while (index < json.Length)
            {
                char current = json[index];
                if (current == '"')
                {
                    int keyStart = index;
                    int contentStart = ++index;
                    bool escaped = false;
                    while (index < json.Length)
                    {
                        char character = json[index];
                        if (escaped)
                            escaped = false;
                        else if (character == '\\')
                            escaped = true;
                        else if (character == '"')
                            break;
                        index++;
                    }
                    if (index >= json.Length)
                        break;

                    int contentLength = index - contentStart;
                    index++;
                    if (depth == 1 && contentLength == propertyName.Length &&
                        string.CompareOrdinal(json, contentStart, propertyName, 0,
                            contentLength) == 0)
                    {
                        int lookahead = index;
                        while (lookahead < json.Length && char.IsWhiteSpace(json[lookahead]))
                            lookahead++;
                        if (lookahead < json.Length && json[lookahead] == ':')
                        {
                            int valueStart = lookahead + 1;
                            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                                valueStart++;
                            int valueEnd = SkipJsonValue(json, valueStart);
                            int removeStart = keyStart;
                            int before = keyStart - 1;
                            while (before >= 0 && char.IsWhiteSpace(json[before]))
                                before--;
                            if (before >= 0 && json[before] == ',')
                            {
                                removeStart = before;
                            }
                            else
                            {
                                int after = valueEnd;
                                while (after < json.Length && char.IsWhiteSpace(json[after]))
                                    after++;
                                if (after < json.Length && json[after] == ',')
                                    valueEnd = after + 1;
                            }

                            return json.Substring(0, removeStart) + json.Substring(valueEnd);
                        }
                    }
                    continue;
                }

                if (current == '{' || current == '[')
                    depth++;
                else if (current == '}' || current == ']')
                    depth--;
                index++;
            }

            throw new InvalidOperationException("Root property was not found: " + propertyName);
        }

        private static int SkipJsonValue(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length)
                return index;

            if (json[index] == '"')
            {
                index++;
                bool escaped = false;
                while (index < json.Length)
                {
                    char character = json[index];
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (character == '"')
                        return index + 1;
                    index++;
                }
                return index;
            }

            if (json[index] == '{' || json[index] == '[')
            {
                int depth = 0;
                while (index < json.Length)
                {
                    char character = json[index];
                    if (character == '"')
                    {
                        index = SkipJsonString(json, index);
                        continue;
                    }
                    if (character == '{' || character == '[')
                        depth++;
                    else if (character == '}' || character == ']')
                    {
                        depth--;
                        if (depth == 0)
                            return index + 1;
                    }
                    index++;
                }
                return index;
            }

            while (index < json.Length && json[index] != ',' && json[index] != '}' &&
                   json[index] != ']')
                index++;
            return index;
        }

        private static int SkipJsonString(string json, int index)
        {
            index++;
            bool escaped = false;
            while (index < json.Length)
            {
                char character = json[index];
                if (escaped)
                    escaped = false;
                else if (character == '\\')
                    escaped = true;
                else if (character == '"')
                    return index + 1;
                index++;
            }
            return index;
        }

        private static FamilyAmbitionSaveEntry Entry(string headPersonId,
            FamilyAmbitionKind kind, int progress) =>
            CreateEntry(headPersonId, kind, progress, kind.ToString(), Array.Empty<string>());

        private static FamilyAmbitionSaveEntry EntryWithAmbitionId(string headPersonId,
            FamilyAmbitionKind kind, int progress, string ambitionId) =>
            CreateEntry(headPersonId, kind, progress, ambitionId, Array.Empty<string>());

        private static FamilyAmbitionSaveEntry EntryWithTechniques(string headPersonId,
            FamilyAmbitionKind kind, int progress, params string[] knownTechniqueIds) =>
            CreateEntry(headPersonId, kind, progress, kind.ToString(), knownTechniqueIds);

        private static FamilyAmbitionSaveEntry CreateEntry(string headPersonId,
            FamilyAmbitionKind kind, int progress, string ambitionId,
            string[] knownTechniqueIds)
        {
            int target = kind == FamilyAmbitionKind.DevelopHousehold
                ? FamilyDoctrineState.DevelopHouseholdTarget
                : kind == FamilyAmbitionKind.PassDownTechnique
                    ? FamilyDoctrineState.PassDownTechniqueTarget
                    : FamilyDoctrineState.EducateHeirTarget;
            return new FamilyAmbitionSaveEntry
            {
                headPersonId = headPersonId,
                ambitionId = ambitionId,
                progress = progress,
                target = target,
                isComplete = progress >= target,
                knownTechniqueIds = knownTechniqueIds
            };
        }

        private static void AssertInvalid(SaveData data, string expectedFragment,
            string because = null)
        {
            SaveOperationResult result = SaveDataValidator.Validate(data);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain(expectedFragment), because);
        }

        private static void Choose(GameSession session, string optionId)
        {
            string pending = session.PendingEventId;
            Assert.That(pending, Is.Not.Empty);
            EventChoiceResult result = session.TryChooseEventOption(pending, optionId,
                session.CreateEventResultId(pending, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
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
