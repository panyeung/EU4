using System;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Events;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M02 focused presentation and scene-integration coverage: stable person ordering,
    /// location/in-transit and progress rendering, positive-duration commands, travel
    /// through WorldMapRules, interruption/replanning feedback, the one-day control's
    /// monthly boundary, and the transactional generator's validation/rollback.
    /// No Play Mode behavior is asserted here.
    /// </summary>
    public sealed class M02DailyActionPanelTests
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
            _session.InitNewGame();
            Create("WorldClockPanel", null, typeof(WorldClockPanel));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private DailyActionPanel Panel() =>
            Find(AddM02DailyActionPanel.RootName).GetComponent<DailyActionPanel>();

        [Test]
        public void Generator_ProducesValidIdempotentSceneAndPreservesTheClockBindings()
        {
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(FindAll(AddM02DailyActionPanel.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM02DailyActionPanel.OpenButtonName).Length, Is.EqualTo(1));

            DailyActionPanel panel = Panel();
            Assert.That(panel.IsBound, Is.True);
            Assert.That(panel.IsOpen, Is.False,
                "the panel must start hidden and be opened by the HUD button");
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(9));
        }

        [Test]
        public void Generator_FaultInjectionRollsBackWithoutLeavingPartialControls()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject canvas = CreateIn(scene, "Canvas", null, typeof(Canvas));
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
                GameSession session = CreateIn(scene, "GameSession", null, typeof(GameSession))
                    .GetComponent<GameSession>();
                session.InitNewGame();

                AddM02DailyActionPanel.UpdateResult result = AddM02DailyActionPanel.Update(scene,
                    root =>
                    {
                        Transform open = root.transform.Find(
                            AddM02DailyActionPanel.OpenButtonName);
                        Assert.That(open, Is.Not.Null);
                        open.name = "BrokenM02OpenButton";
                    });

                Assert.That(result,
                    Is.EqualTo(AddM02DailyActionPanel.UpdateResult.ValidationFailed));
                Assert.That(FindAllIn(scene, AddM02DailyActionPanel.RootName), Is.Empty);
                Assert.That(FindAllIn(scene, AddM02DailyActionPanel.PanelName), Is.Empty);
                Assert.That(scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<DailyActionPanel>(true))
                        .ToArray(),
                    Is.Empty);
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Panel_ListsRegisteredPeopleInStableOrderWithLocationAndActionProgress()
        {
            DailyActionPanel panel = Panel();
            panel.Open();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.RowCount, Is.EqualTo(_session.DailyActions.Count));
            Assert.That(panel.TitleLine, Does.Contain("每日行动"));

            string previous = string.Empty;
            for (int i = 0; i < panel.RowCount; i++)
            {
                string personId = panel.RowPersonId(i).Value;
                Assert.That(string.CompareOrdinal(previous, personId), Is.LessThan(0),
                    "rows must follow ordinal person-ID order");
                previous = personId;
                Assert.That(panel.RowText(i), Does.Contain("清洲"));
                if (NpcAutonomyRules.IsPilot(panel.RowPersonId(i)))
                    Assert.That(panel.RowText(i), Does.Not.Contain("空闲"),
                        "M03 pilots start with an autonomous action");
                else
                    Assert.That(panel.RowText(i), Does.Contain("空闲"));
            }

            panel.SelectPerson(M2FixedScenario.HarunobuId);
            panel.SetDuration("2");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);

            Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                out DailyActionTask task), Is.True);
            Assert.That(task.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("休息 0/2"));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("剩 2"));
            Assert.That(panel.SelectedLine, Does.Contain("休息 进度 0/2"));
            Assert.That(panel.FeedbackVisible, Is.True);
            Assert.That(panel.FeedbackLine, Does.Contain("已安排"));
        }

        [Test]
        public void Panel_RejectsNonPositiveOrNonNumericDurationWithoutPlanning()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);

            panel.SetDuration("0");
            Assert.That(panel.ClickTraining(), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("大于 0"));
            Assert.That(_session.HasActiveDailyAction(M2FixedScenario.HarunobuId), Is.False);

            panel.SetDuration("abc");
            Assert.That(panel.ClickWork(), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("大于 0"));
            Assert.That(_session.HasActiveDailyAction(M2FixedScenario.HarunobuId), Is.False);
        }

        [Test]
        public void Panel_TravelUsesWorldMapRulesAndShowsInTransitState()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);

            Assert.That(panel.TravelInteractable(LocationId.Kiyosu), Is.False,
                "the current location is not a travel destination");
            Assert.That(panel.TravelInteractable(LocationId.Atsuta), Is.True);
            Assert.That(panel.TravelInteractable(LocationId.Nagoya), Is.True);

            Assert.That(panel.ClickTravel(LocationId.Atsuta), Is.True, panel.FeedbackLine);
            Assert.That(_session.IsPersonInTransit(M2FixedScenario.HarunobuId), Is.True);
            Assert.That(_session.TryGetPersonLocation(M2FixedScenario.HarunobuId, out _), Is.False);
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("旅途中"));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("清洲→热田"));
            Assert.That(panel.SelectedLine, Does.Contain("旅途中"));
            Assert.That(panel.TravelInteractable(LocationId.Atsuta), Is.False);
            Assert.That(panel.TravelInteractable(LocationId.Nagoya), Is.False);

            Assert.That(_session.AdvanceOneDay(), Is.True);
            panel.Refresh();
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("热田"));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("空闲"));
            Assert.That(panel.TravelInteractable(LocationId.Nagoya), Is.False,
                "Atsuta and Nagoya have no direct route");
            Assert.That(panel.ClickTravel(LocationId.Nagoya), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("没有直达路线"));
        }

        [Test]
        public void Panel_InterruptAndReplanShowExplicitOutcomeFeedback()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);

            panel.SetDuration("3");
            Assert.That(panel.ClickWork(), Is.True, panel.FeedbackLine);
            Assert.That(panel.FeedbackLine, Does.Contain("已安排"));

            Assert.That(panel.ClickInterrupt(), Is.True, panel.FeedbackLine);
            Assert.That(panel.InterruptCount, Is.EqualTo(1));
            Assert.That(panel.FeedbackLine, Does.Contain("已中断"));
            Assert.That(_session.HasActiveDailyAction(M2FixedScenario.HarunobuId), Is.False);
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("空闲"));

            panel.SetDuration("2");
            Assert.That(panel.ClickTraining(), Is.True, panel.FeedbackLine);
            panel.SetDuration("1");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);
            Assert.That(panel.ReplanCount, Is.EqualTo(1));
            Assert.That(panel.FeedbackLine, Does.Contain("已中断"));
            Assert.That(panel.FeedbackLine, Does.Contain("已安排"));
            Assert.That(panel.FeedbackLine, Does.Contain("重新规划"));
            Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                out DailyActionTask task), Is.True);
            Assert.That(task.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(task.RemainingDays, Is.EqualTo(1));

            panel.SetDuration("0");
            Assert.That(panel.ClickWork(), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("大于 0"));
            Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                out DailyActionTask preserved), Is.True);
            Assert.That(preserved.Kind, Is.EqualTo(DailyActionKind.Rest),
                "a rejected replan must keep the original action");
        }

        [Test]
        public void ClockPanel_AdvanceUsesDailyActionDayAndMonthSettlementExactlyOnce()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(28), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 29)));

            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);
            panel.SetDuration("4");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);

            var clockObject = new GameObject("WorldClockPanel_M02Test");
            try
            {
                WorldClockPanel clock = clockObject.AddComponent<WorldClockPanel>();
                clock.SetDependencies(_session, null, null);
                clock.BindDailyActionPanel(panel);

                Assert.That(clock.TryAdvanceOneDay(), Is.True, clock.LastAdvanceMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 30)));
                Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                    out DailyActionTask task), Is.True);
                Assert.That(task.RemainingDays, Is.EqualTo(3));
                Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("剩 3"));

                Assert.That(clock.TryAdvanceOneDay(), Is.True, clock.LastAdvanceMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
                Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                    out task), Is.True);
                Assert.That(task.RemainingDays, Is.EqualTo(2),
                    "the month boundary must consume exactly one action day");
                Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("剩 2"));
                Assert.That(_session.Industry.LastSettledMonthIndex,
                    Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 5, 1)) - 1));
                Assert.That(_session.LastIndustryReport, Is.Not.Null);
                Assert.That(_session.LastIndustryReport.Month, Is.EqualTo(4));
                int fundsAfterSettlement = _session.Industry.Resources.Funds;

                if (!string.IsNullOrEmpty(_session.PendingEventId))
                {
                    string pending = _session.PendingEventId;
                    EventChoiceResult choice = _session.TryChooseEventOption(pending,
                        M2FixedScenario.KeepPromiseOptionId,
                        _session.CreateEventResultId(pending,
                            M2FixedScenario.KeepPromiseOptionId));
                    Assert.That(choice.Succeeded, Is.True, choice.Message);
                }

                Assert.That(clock.TryAdvanceOneDay(), Is.True, clock.LastAdvanceMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 2)));
                Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                    out task), Is.True);
                Assert.That(task.RemainingDays, Is.EqualTo(1));
                Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(fundsAfterSettlement),
                    "a day inside the new month must not repeat the previous settlement");
                Assert.That(_session.LastIndustryReport.Month, Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clockObject);
            }
        }

        private GameObject Create(string name, Transform parent, params Type[] components) =>
            CreateIn(_scene, name, parent, components);

        private static GameObject CreateIn(Scene scene, string name, Transform parent,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type component in components)
                go.AddComponent(component);
            return go;
        }

        private Transform Find(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(transform => transform.name == name);

        private Transform[] FindAll(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name).ToArray();

        private static Transform[] FindAllIn(Scene scene, string name) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name).ToArray();
    }
}
