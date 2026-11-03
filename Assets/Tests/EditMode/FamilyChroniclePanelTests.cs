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
            _session.InitNewGame(99UL);

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
