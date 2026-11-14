using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-012 Task 4: the read-only chronicle panel shows the ledger in order,
    /// renders an explicit empty state, refreshes without duplicating rows, and
    /// never advances game time when opened or closed.
    /// </summary>
    public sealed class FamilyChroniclePanelTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitStandardDemo(99UL);

            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private FamilyChroniclePanel Panel() =>
            Find(AddSC012FamilyChroniclePanel.RootName).GetComponent<FamilyChroniclePanel>();

        private void CreateEventRecord()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            string eventId = _session.PendingEventId;
            Assert.That(_session.TryChooseEventOption(eventId, SampleEventIds.OfferRestOptionId,
                _session.CreateEventResultId(eventId, SampleEventIds.OfferRestOptionId)).Succeeded, Is.True);
        }

        private void ResolvePending()
        {
            if (string.IsNullOrEmpty(_session.PendingEventId))
                return;
            string pending = _session.PendingEventId;
            SengokuChronicle.Events.EventDefinitionData definition = _session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (SengokuChronicle.Events.EventOptionData option in definition.options)
            {
                if (!_session.TryGetEventOptionAvailability(pending, option.optionId,
                        out bool enabled, out _) || !enabled)
                    continue;
                Assert.That(_session.TryChooseEventOption(pending, option.optionId,
                    _session.CreateEventResultId(pending, option.optionId)).Succeeded, Is.True);
                return;
            }
            Assert.Fail("no enabled option for " + pending);
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True, "missing travel route to " + destination.Value);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }

        [Test]
        public void EmptyHistoryShowsAnExplicitPlaceholder()
        {
            FamilyChroniclePanel panel = Panel();

            panel.Open();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.VisibleRowCount, Is.Zero);
            Assert.That(panel.EmptyStateVisible, Is.True);
            Assert.That(panel.EmptyStateText, Does.Contain("尚无"));
        }

        [Test]
        public void RowsShowDateCategoryTitleSummaryAndRelationshipValues()
        {
            CreateEventRecord();
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 4).Succeeded, Is.True);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(5));
            Assert.That(panel.EmptyStateVisible, Is.False);

            Assert.That(panel.RowText(0), Does.Contain("1560年5月"));
            Assert.That(panel.RowText(0), Does.Contain("事件"));
            Assert.That(panel.RowText(0), Does.Contain("雨夜来客"));

            Assert.That(panel.RowText(1), Does.Contain("关系"));
            Assert.That(panel.RowText(1), Does.Contain("0 → 5"));

            Assert.That(panel.RowText(2), Does.Contain("关系"));
            Assert.That(panel.RowText(2), Does.Contain("5 → 9"));

            Assert.That(panel.RowText(3), Does.Contain("任务"));
            Assert.That(panel.RowText(3), Does.Contain("阶段 未接受 → 已接受"));

            Assert.That(panel.RowText(4), Does.Contain("继承"));
        }

        [Test]
        public void ResearchCompletion_RendersReadableTechniqueResearcherProgressAndMonth()
        {
            _session.InitResearchDemoScenario(99UL);
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending();

            FamilyChroniclePanel panel = Panel();
            panel.Open();
            int index = -1;
            for (int i = 0; i < _session.ChronicleEntries.Count; i++)
                if (_session.ChronicleEntries[i].Category == ChronicleCategory.Research)
                    index = i;
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            string row = panel.RowText(index);

            Assert.That(row, Does.Contain("研究"));
            Assert.That(row, Does.Contain("烟幕调制术"));
            Assert.That(row, Does.Contain("影山 宗次"));
            Assert.That(row, Does.Contain("进度 1 → 2"));
            Assert.That(row, Does.Contain("1560年2月"));
            Assert.That(row, Does.Not.Contain("research."));
            Assert.That(row, Does.Not.Contain("technique."));
            Assert.That(row, Does.Not.Contain("person."));
        }

        [Test]
        public void FamilyFormation_RendersReadableMarriageAndAdoptionRows()
        {
            _session.InitFamilyFormationDemoScenario(99UL);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Succeeded, Is.True);
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId).Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();
            int marriageIndex = -1;
            int adoptionIndex = -1;
            for (int i = 0; i < _session.ChronicleEntries.Count; i++)
            {
                FamilyChronicleEntry entry = _session.ChronicleEntries[i];
                if (entry.SourceId == FamilyChronicleEntry.MarriageSourceId)
                    marriageIndex = i;
                if (entry.SourceId == FamilyChronicleEntry.AdoptionSourceId)
                    adoptionIndex = i;
            }
            Assert.That(marriageIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(adoptionIndex, Is.GreaterThanOrEqualTo(0));

            string marriageRow = panel.RowText(marriageIndex);
            Assert.That(marriageRow, Does.Contain("家族关系"));
            Assert.That(marriageRow, Does.Contain("婚姻登记"));
            Assert.That(marriageRow, Does.Contain(SC022FamilyFormationScenario.HeadName));
            Assert.That(marriageRow, Does.Contain(
                SC022FamilyFormationScenario.MarriageCandidateOneName));
            Assert.That(marriageRow, Does.Not.Contain("person."));

            string adoptionRow = panel.RowText(adoptionIndex);
            Assert.That(adoptionRow, Does.Contain("家族关系"));
            Assert.That(adoptionRow, Does.Contain("普通收养"));
            Assert.That(adoptionRow, Does.Contain(
                SC022FamilyFormationScenario.ChildAdoptionCandidateName));
            Assert.That(adoptionRow, Does.Not.Contain("person."));
        }

        [Test]
        public void LongMessageRowsGrowToFitTheirWrappedText()
        {
            SaveData data = _session.CaptureSave();
            data.chronicle = new[]
            {
                new ChronicleSaveEntry
                {
                    entryId = FamilyChronicleEntry.BuildEntryId(1),
                    sequence = 1,
                    year = 1560,
                    month = 4,
                    category = (int)ChronicleCategory.Event,
                    subjectId = "event.short",
                    sourceId = "option.short",
                    title = "短记录",
                    summary = "简短摘要"
                },
                new ChronicleSaveEntry
                {
                    entryId = FamilyChronicleEntry.BuildEntryId(2),
                    sequence = 2,
                    year = 1560,
                    month = 4,
                    category = (int)ChronicleCategory.Event,
                    subjectId = "event.long",
                    sourceId = "option.long",
                    title = "长记录",
                    summary = string.Concat(Enumerable.Repeat("long message ", 80))
                }
            };
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();

            Transform shortRow = Find("FamilyChronicleRow_1");
            Transform longRow = Find("FamilyChronicleRow_2");
            Assert.That(shortRow, Is.Not.Null);
            Assert.That(longRow, Is.Not.Null);
            LayoutElement shortLayout = shortRow.GetComponent<LayoutElement>();
            LayoutElement longLayout = longRow.GetComponent<LayoutElement>();
            Assert.That(shortLayout, Is.Not.Null);
            Assert.That(longLayout, Is.Not.Null);

            Assert.That(shortLayout.preferredHeight, Is.GreaterThanOrEqualTo(44f));
            Assert.That(longLayout.preferredHeight, Is.GreaterThan(shortLayout.preferredHeight));
            Assert.That(longLayout.preferredHeight, Is.GreaterThan(86f),
                "a long message must exceed the old fixed row height instead of clipping");
        }

        [Test]
        public void OpenPanelRefreshesWhenTheSessionChanges()
        {
            FamilyChroniclePanel panel = Panel();
            panel.Open();
            Assert.That(panel.VisibleRowCount, Is.Zero);

            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 4).Succeeded, Is.True);

            Assert.That(panel.VisibleRowCount, Is.EqualTo(1),
                "an open panel must refresh on SessionStateChanged without a manual Refresh call");
            Assert.That(panel.RowText(0), Does.Contain("0 → 4"));
        }

        [Test]
        public void RowsShowReadableSealedLetterAndSuccessionPhaseNames()
        {
            Assert.That(_session.TryAcceptSealedLetterMission().Succeeded, Is.True);
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryDeliverSealedLetter().Succeeded, Is.True);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(3));
            Assert.That(panel.RowText(0), Does.Contain("阶段 未接受 → 已接受"));
            Assert.That(panel.RowText(1), Does.Contain("阶段 已接受 → 已领取报酬"));
            Assert.That(panel.RowText(2), Does.Contain("继承阶段 无待决 → 待选继承人"));
        }

        [Test]
        public void MarketTallyRowsShowReadableStageNames()
        {
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryAcceptMarketTallyMission().Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryInvestigateMarketTally().Succeeded, Is.True);
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryReportMarketTally().Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(3));
            Assert.That(panel.RowText(0), Does.Contain("阶段 未接受 → 已接受"));
            Assert.That(panel.RowText(1), Does.Contain("阶段 已接受 → 已调查"));
            Assert.That(panel.RowText(2), Does.Contain("阶段 已调查 → 已领取报酬"));
        }

        [Test]
        public void TacticalMissionRowsShowReadableStageNamesIncludingInProgressToFailed()
        {
            SaveData data = _session.CaptureSave();
            data.chronicle = new[]
            {
                TacticalChronicleEntry(1, TacticalMissionStage.NotStarted,
                    TacticalMissionStage.InProgress),
                TacticalChronicleEntry(2, TacticalMissionStage.InProgress,
                    TacticalMissionStage.Succeeded),
                TacticalChronicleEntry(3, TacticalMissionStage.InProgress,
                    TacticalMissionStage.Failed),
                TacticalChronicleEntry(4, TacticalMissionStage.InProgress,
                    TacticalMissionStage.Withdrawn)
            };
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(4));
            Assert.That(panel.RowText(0), Does.Contain("阶段 未开始 → 潜入进行中"));
            Assert.That(panel.RowText(1), Does.Contain("阶段 潜入进行中 → 成功，已结算"));
            Assert.That(panel.RowText(2), Does.Contain("阶段 潜入进行中 → 失败，已结算"),
                "the observed M06 InProgress -> Failed record must not render as 未知阶段");
            Assert.That(panel.RowText(3), Does.Contain("阶段 潜入进行中 → 撤退，已结算"));
            for (int i = 0; i < panel.VisibleRowCount; i++)
                Assert.That(panel.RowText(i), Does.Not.Contain("未知阶段"),
                    "known tactical stages must never fall through to the unknown-stage fallback");
        }

        private static ChronicleSaveEntry TacticalChronicleEntry(int sequence,
            TacticalMissionStage fromStage, TacticalMissionStage toStage)
        {
            return new ChronicleSaveEntry
            {
                entryId = FamilyChronicleEntry.BuildEntryId(sequence),
                sequence = sequence,
                year = 1560,
                month = 4,
                category = (int)ChronicleCategory.Mission,
                subjectId = FamilyChronicleEntry.TacticalMissionId,
                sourceId = "settle",
                title = "潜入密令",
                summary = "潜入密令阶段变化",
                fromStage = (int)fromStage,
                toStage = (int)toStage
            };
        }

        [Test]
        public void ShadowEscortRowsRenderEveryLegalCommissionStageFromAnAppliedSave()
        {
            SaveData data = _session.CaptureSave();
            data.chronicle = new[]
            {
                ShadowEscortChronicleEntry(1, ShadowEscortStage.NotAccepted,
                    ShadowEscortStage.Accepted, "accept"),
                ShadowEscortChronicleEntry(2, ShadowEscortStage.Accepted,
                    ShadowEscortStage.InProgress, "assign"),
                ShadowEscortChronicleEntry(3, ShadowEscortStage.InProgress,
                    ShadowEscortStage.Succeeded, "settle"),
                ShadowEscortChronicleEntry(4, ShadowEscortStage.InProgress,
                    ShadowEscortStage.PartialSuccess, "settle"),
                ShadowEscortChronicleEntry(5, ShadowEscortStage.InProgress,
                    ShadowEscortStage.Failed, "settle"),
                ShadowEscortChronicleEntry(6, ShadowEscortStage.InProgress,
                    ShadowEscortStage.Abandoned, "abandon")
            };
            // Applying through the normal validator keeps this honest: every pair below
            // must first be a legal commission record, then be readable in the panel.
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            FamilyChroniclePanel panel = Panel();
            panel.Open();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(6));
            Assert.That(panel.RowText(0), Does.Contain("阶段 待接取 → 已接取，等待指派执行人"));
            Assert.That(panel.RowText(1), Does.Contain("阶段 已接取，等待指派执行人 → 执行中"));
            Assert.That(panel.RowText(2), Does.Contain("阶段 执行中 → 成功，已结算"));
            Assert.That(panel.RowText(3), Does.Contain("阶段 执行中 → 部分成功，已结算"));
            Assert.That(panel.RowText(4), Does.Contain("阶段 执行中 → 失败，已结算"),
                "the InProgress -> terminal settlement pattern must stay readable");
            Assert.That(panel.RowText(5), Does.Contain("阶段 执行中 → 已中止（执行人不可用，无结算结果）"));
            Assert.That(panel.RowText(5), Does.Not.Contain("已结算"),
                "an abandoned commission has no shared result, so it must not claim a settlement");
            for (int i = 0; i < panel.VisibleRowCount; i++)
                Assert.That(panel.RowText(i), Does.Not.Contain("未知阶段"),
                    "known commission stages must never fall through to the unknown-stage fallback");
        }

        private static ChronicleSaveEntry ShadowEscortChronicleEntry(int sequence,
            ShadowEscortStage fromStage, ShadowEscortStage toStage, string actionId)
        {
            return new ChronicleSaveEntry
            {
                entryId = FamilyChronicleEntry.BuildEntryId(sequence),
                sequence = sequence,
                year = 1560,
                month = 4,
                category = (int)ChronicleCategory.Mission,
                subjectId = FamilyChronicleEntry.ShadowEscortCommissionId,
                sourceId = actionId,
                title = "护送委托",
                summary = "护送委托阶段变化",
                fromStage = (int)fromStage,
                toStage = (int)toStage
            };
        }

        [Test]
        public void RefreshDoesNotDuplicateRowsOrTemplates()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 6).Succeeded, Is.True);
            FamilyChroniclePanel panel = Panel();
            panel.Open();

            panel.Refresh();
            panel.Refresh();
            panel.Refresh();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(1));
            Transform content = Find(AddSC012FamilyChroniclePanel.ContentName);
            Assert.That(content.childCount, Is.EqualTo(2),
                "one inactive template plus exactly one live row");
            Assert.That(content.Cast<Transform>().Count(child => child.gameObject.activeSelf),
                Is.EqualTo(1));
        }

        [Test]
        public void OpeningAndClosingNeverAdvancesTimeOrChangesState()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 2).Succeeded, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            int merit = _session.Merit;
            LocationId location = _session.CurrentLocation;
            int records = _session.ChronicleEntries.Count;
            FamilyChroniclePanel panel = Panel();

            panel.Open();
            panel.Close();
            panel.Open();

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Merit, Is.EqualTo(merit));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(records));
            Assert.That(panel.IsOpen, Is.True);
        }

        [Test]
        public void OpenScrollsToTheLatestRecord()
        {
            for (int i = 0; i < 12; i++)
                Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 1).Succeeded, Is.True);
            FamilyChroniclePanel panel = Panel();

            panel.Open();

            Assert.That(panel.VisibleRowCount, Is.EqualTo(12));
            Assert.That(panel.ScrollRect.verticalNormalizedPosition, Is.EqualTo(0f)
                .Within(0.0001f), "0 is the bottom of the scroll view, where the newest record lives");
        }

        [Test]
        public void StaticButtonsAreWiredExactlyOnceAcrossRebinds()
        {
            FamilyChroniclePanel panel = Panel();

            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(2));

            panel.enabled = false;
            panel.enabled = true;
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(2),
                "disable/enable must not duplicate static listeners");

            var serialized = new UnityEditor.SerializedObject(panel);
            panel.BindReferences(
                serialized.FindProperty("gameSession").objectReferenceValue as GameSession,
                serialized.FindProperty("panelRoot").objectReferenceValue as GameObject,
                serialized.FindProperty("openButton").objectReferenceValue as Button,
                serialized.FindProperty("closeButton").objectReferenceValue as Button,
                serialized.FindProperty("scrollRect").objectReferenceValue as ScrollRect,
                serialized.FindProperty("content").objectReferenceValue as Transform,
                serialized.FindProperty("rowTemplate").objectReferenceValue as GameObject,
                serialized.FindProperty("emptyText").objectReferenceValue as Text,
                serialized.FindProperty("titleText").objectReferenceValue as Text);

            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(2),
                "a repeated BindReferences call must replace, not stack, static listeners");
        }

        [Test]
        public void OpenAndCloseButtonsDriveThePanel()
        {
            FamilyChroniclePanel panel = Panel();

            Find(AddSC012FamilyChroniclePanel.OpenButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True);

            Find(AddSC012FamilyChroniclePanel.CloseButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False);
        }

        private GameObject Create(string name, Transform parent, params System.Type[] components) =>
            CreateIn(_scene, name, parent, components);

        private static GameObject CreateIn(Scene scene, string name, Transform parent,
            params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null) go.transform.SetParent(parent, false);
            foreach (System.Type component in components) go.AddComponent(component);
            return go;
        }

        private Transform Find(string name) => Named(_scene, name).SingleOrDefault();

        private static System.Collections.Generic.IEnumerable<Transform> Named(Scene scene, string name) =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name);
    }
}
