using System;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M03 read-only Inspector coverage: both pilots render, all three candidates show a
    /// score or exclusion, logs expose trigger/outcome, UI interactions do not mutate the
    /// session, and the transactional generator is idempotent and rolls back on failure.
    /// </summary>
    public sealed class NpcAiInspectorPanelTests
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
            Assert.That(AddM03NpcAiInspector.Update(_scene),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private NpcAiInspectorPanel Panel() =>
            Find(AddM03NpcAiInspector.RootName).GetComponent<NpcAiInspectorPanel>();

        [Test]
        public void Generator_ProducesValidIdempotentSceneWithOneRoot()
        {
            Assert.That(AddM03NpcAiInspector.Validate(_scene, out string error), Is.True, error);
            Assert.That(AddM03NpcAiInspector.Update(_scene),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.Success));
            Assert.That(FindAll(AddM03NpcAiInspector.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM03NpcAiInspector.OpenButtonName).Length, Is.EqualTo(1));

            NpcAiInspectorPanel panel = Panel();
            Assert.That(panel.IsBound, Is.True);
            Assert.That(panel.IsOpen, Is.False,
                "the Inspector must start hidden behind the HUD button");
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(4));
        }

        [Test]
        public void Generator_FaultInjectionRollsBackWithoutPartialControls()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject canvas = CreateIn(scene, "Canvas", null, typeof(Canvas));
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
                GameSession session = CreateIn(scene, "GameSession", null, typeof(GameSession))
                    .GetComponent<GameSession>();
                session.InitNewGame();

                AddM03NpcAiInspector.UpdateResult result = AddM03NpcAiInspector.Update(scene,
                    root =>
                    {
                        Transform open = root.transform.Find(
                            AddM03NpcAiInspector.OpenButtonName);
                        Assert.That(open, Is.Not.Null);
                        open.name = "BrokenM03OpenButton";
                    });

                Assert.That(result,
                    Is.EqualTo(AddM03NpcAiInspector.UpdateResult.ValidationFailed));
                Assert.That(FindAllIn(scene, AddM03NpcAiInspector.RootName), Is.Empty);
                Assert.That(FindAllIn(scene, AddM03NpcAiInspector.PanelName), Is.Empty);
                Assert.That(scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<NpcAiInspectorPanel>(true))
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
        public void Panel_RendersBothPilotsNeedsCandidatesAndState()
        {
            NpcAiInspectorPanel panel = Panel();
            panel.Open();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.SelectedPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(panel.NeedsLine, Does.Contain("休息 40"));
            Assert.That(panel.NeedsLine, Does.Contain("学艺 75"));
            Assert.That(panel.NeedsLine, Does.Contain("家务 45"));
            Assert.That(panel.NeedsLine, Does.Contain("偏好"));
            Assert.That(panel.StatusLine, Does.Contain("执行中"));
            Assert.That(panel.StatusLine, Does.Contain("训练"));
            Assert.That(panel.CandidatesLine, Does.Contain("休息"));
            Assert.That(panel.CandidatesLine, Does.Contain("训练"));
            Assert.That(panel.CandidatesLine, Does.Contain("工作"));
            Assert.That(panel.CandidatesLine, Does.Contain("分数"));
            Assert.That(panel.CandidatesLine, Does.Contain("已选中"));
            Assert.That(panel.CandidatesLine, Does.Contain("当前诊断"));
            Assert.That(panel.CandidatesLine, Does.Contain("排除"),
                "the active pilot's current diagnostics explain why it is not replanned");

            Assert.That(panel.SelectPilot(M2FixedScenario.NobumasaId), Is.True);
            Assert.That(panel.NeedsLine, Does.Contain("休息 45"));
            Assert.That(panel.NeedsLine, Does.Contain("家务 75"));
            Assert.That(panel.StatusLine, Does.Contain("工作"));
            Assert.That(panel.CandidatesLine, Does.Contain("已选中"));
        }

        [Test]
        public void Panel_RefreshesNeedsAndCandidateScoresWhenAnActiveDayGrowsNeeds()
        {
            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.NeedsLine, Does.Contain("休息 40"));
            Assert.That(panel.CandidatesLine, Does.Contain("分数 150"));

            Assert.That(_session.AdvanceOneDay(), Is.True);

            Assert.That(panel.NeedsLine, Does.Contain("休息 44"),
                "the open Inspector must refresh from SessionStateChanged after need growth");
            Assert.That(panel.NeedsLine, Does.Contain("学艺 78"));
            Assert.That(panel.CandidatesLine, Does.Contain("分数 156"),
                "an active pilot's diagnostic candidate scores follow the grown needs");
            Assert.That(panel.CandidatesLine, Does.Contain("进行中"),
                "the active pilot still shows why it cannot be replanned");
            Assert.That(panel.StatusLine, Does.Contain("执行中"));
        }

        [Test]
        public void Panel_RendersExclusionReasonForAnUnknownLocationPilot()
        {
            SaveData saved = _session.CaptureSave();
            DailyActionPersonSaveEntry row = saved.dailyActions.persons.First(entry =>
                entry.personId == M2FixedScenario.MasatsunaId.Value);
            row.hasLocation = false;
            row.locationId = string.Empty;
            row.hasAction = false;
            row.actionKind = 0;
            row.actionDurationDays = 0;
            row.actionElapsedDays = 0;
            row.actionStartYear = 0;
            row.actionStartMonth = 0;
            row.actionStartDay = 0;
            row.travelOriginId = string.Empty;
            row.travelDestinationId = string.Empty;
            row.actionReason = string.Empty;
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);
            Assert.That(_session.TryApplySave(saved).Succeeded, Is.True,
                _session.LastEventMessage);

            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.SelectedPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(panel.CandidatesLine, Does.Contain("排除"));
            Assert.That(panel.CandidatesLine, Does.Contain("地点未知"));
            Assert.That(panel.CandidatesLine, Does.Contain("休息"));
            Assert.That(panel.CandidatesLine, Does.Contain("训练"));
            Assert.That(panel.CandidatesLine, Does.Contain("工作"));
        }

        [Test]
        public void Panel_ShowsRecentDecisionOutcomeAndTrigger()
        {
            Assert.That(_session.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "面板测试中断。"), Is.True);

            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.LogLine, Does.Contain("中断"));
            Assert.That(panel.LogLine, Does.Contain("接受"));
            Assert.That(panel.LogLine, Does.Contain("行动中断"));
        }

        [Test]
        public void Panel_ReadOnlyControlsDoNotMutateSessionState()
        {
            SaveData before = _session.CaptureSave();
            string dailyBefore = JsonUtility.ToJson(before.dailyActions);
            string needsBefore = JsonUtility.ToJson(before.npcAutonomy);

            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.SelectPilot(M2FixedScenario.NobumasaId), Is.True);
            Assert.That(panel.SelectPilot(M2FixedScenario.MasatsunaId), Is.True);
            panel.SelectFirstPilot();
            panel.Refresh();
            panel.Close();

            SaveData after = _session.CaptureSave();
            Assert.That(JsonUtility.ToJson(after.dailyActions), Is.EqualTo(dailyBefore));
            Assert.That(JsonUtility.ToJson(after.npcAutonomy), Is.EqualTo(needsBefore));
            Assert.That(after.year, Is.EqualTo(before.year));
            Assert.That(after.month, Is.EqualTo(before.month));
            Assert.That(after.day, Is.EqualTo(before.day));
        }

        [Test]
        public void Panel_KeepsSelectionExplanationAndShowsCurrentDiagnosticsOnActiveDay()
        {
            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.CandidatesLine, Does.Contain("最近目标评估"));
            Assert.That(panel.CandidatesLine, Does.Contain("已选中"));
            Assert.That(_session.TryGetNpcAutonomy(M2FixedScenario.MasatsunaId,
                out NpcAutonomySnapshot before), Is.True);
            Assert.That(before.LatestEvaluation.HasSelection, Is.True);

            Assert.That(_session.AdvanceOneDay(), Is.True);

            Assert.That(panel.CandidatesLine, Does.Contain("最近目标评估"),
                "daily need growth must not erase the last goal-selection explanation");
            Assert.That(panel.CandidatesLine, Does.Contain("当前诊断"));
            Assert.That(panel.CandidatesLine, Does.Contain("分数 156"),
                "current diagnostics follow the grown needs");
            Assert.That(panel.CandidatesLine, Does.Contain("进行中"),
                "current diagnostics explain why an active pilot is not replanned");

            Assert.That(_session.TryGetNpcAutonomy(M2FixedScenario.MasatsunaId,
                out NpcAutonomySnapshot after), Is.True);
            Assert.That(after.LatestEvaluation.HasSelection, Is.True,
                "LatestEvaluation keeps the selection that started the active action");
            Assert.That(after.CurrentEvaluation.HasSelection, Is.False,
                "CurrentEvaluation is the pure active-day diagnostic projection");
            Assert.That(after.CurrentEvaluation.Needs, Is.EqualTo(after.Needs));
        }

        [Test]
        public void Panel_ShowsCurrentExecutionAndLastCompletedOutcome()
        {
            Assert.That(_session.AdvanceOneDay(), Is.True);
            Assert.That(_session.AdvanceOneDay(), Is.True);

            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.StatusLine, Does.Contain("状态：执行中"));
            Assert.That(panel.StatusLine, Does.Contain("最近结果：已完成 训练"));
            Assert.That(panel.StatusLine, Does.Contain("当前行动：训练"));
        }

        [Test]
        public void Panel_ShowsLastInterruptedOutcomeAfterImmediateReplan()
        {
            Assert.That(_session.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "面板结果测试。"), Is.True);

            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.StatusLine, Does.Contain("状态：执行中"),
                "the immediate replan owns the current execution state");
            Assert.That(panel.StatusLine, Does.Contain("最近结果：被中断 训练"),
                "the interrupted outcome remains visible after the replan");
        }

        [Test]
        public void Panel_ShowsEmptyMemoryStateForFreshPilot()
        {
            NpcAiInspectorPanel panel = Panel();
            panel.Open();

            Assert.That(panel.StatusLine, Does.Contain("记忆：暂无"));
            Assert.That(panel.CandidatesLine, Does.Contain("基础分数"));
            Assert.That(panel.CandidatesLine, Does.Contain("记忆修正 +0"));
            Assert.That(panel.CandidatesLine, Does.Contain("最终分数"));
            Assert.That(panel.GetComponentsInChildren<InputField>(true), Is.Empty,
                "the read-only Inspector must not gain editable memory controls");
        }

        [Test]
        public void Panel_ShowsCompletedMemoryAndScoreBreakdown()
        {
            Assert.That(_session.AdvanceOneDay(), Is.True);
            Assert.That(_session.AdvanceOneDay(), Is.True);

            NpcAiInspectorPanel panel = Panel();
            panel.Open();

            Assert.That(panel.StatusLine, Does.Contain("记忆"));
            Assert.That(panel.StatusLine, Does.Contain("完成记忆 训练"));
            Assert.That(panel.StatusLine, Does.Contain("1560-04-03"));
            Assert.That(panel.StatusLine, Does.Contain("当前修正 +12"));
            Assert.That(panel.StatusLine, Does.Contain("生效"));
            Assert.That(panel.CandidatesLine, Does.Contain("基础分数"));
            Assert.That(panel.CandidatesLine, Does.Contain("记忆修正 +12"));
            Assert.That(panel.CandidatesLine, Does.Contain("最终分数"));
            Assert.That(panel.CandidatesLine, Does.Contain("完成记忆"));
        }

        [Test]
        public void Panel_ShowsInterruptedMemoryReasonAndExpiredAdjustment()
        {
            Assert.That(_session.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "面板记忆中断原因。"), Is.True);
            NpcAiInspectorPanel panel = Panel();
            panel.Open();
            Assert.That(panel.StatusLine, Does.Contain("中断记忆 训练"));
            Assert.That(panel.StatusLine, Does.Contain("当前修正 -12"));
            Assert.That(panel.StatusLine, Does.Contain("面板记忆中断原因"));

            SaveData saved = _session.CaptureSave();
            NpcAutonomyPersonSaveEntry entry = saved.npcAutonomy.persons.First(row =>
                row.personId == M2FixedScenario.MasatsunaId.Value);
            entry.memories = new[]
            {
                new NpcActionMemorySaveEntry
                {
                    personId = M2FixedScenario.MasatsunaId.Value,
                    kind = (int)NpcActionMemoryKind.Completed,
                    action = (int)DailyActionKind.Rest,
                    year = 1560,
                    month = 3,
                    day = 1,
                    reason = string.Empty
                }
            };
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True,
                SaveDataValidator.Validate(saved).Message);
            Assert.That(_session.TryApplySave(saved).Succeeded, Is.True,
                _session.LastEventMessage);

            panel.Refresh();
            Assert.That(panel.StatusLine, Does.Contain("已过期"));
            Assert.That(panel.StatusLine, Does.Contain("当前修正 +0"));
        }

        [Test]
        public void Generator_ProducesScrollableDiagnosticsForCandidatesAndLog()
        {
            Transform scrollTransform = Find(AddM03NpcAiInspector.ScrollName);
            Transform viewport = Find(AddM03NpcAiInspector.ViewportName);
            Transform content = Find(AddM03NpcAiInspector.ContentName);
            Transform candidates = Find(AddM03NpcAiInspector.CandidatesName);
            Transform log = Find(AddM03NpcAiInspector.LogName);
            Assert.That(scrollTransform, Is.Not.Null);
            Assert.That(viewport, Is.Not.Null);
            Assert.That(content, Is.Not.Null);
            Assert.That(candidates, Is.Not.Null);
            Assert.That(log, Is.Not.Null);

            ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.viewport, Is.EqualTo((RectTransform)viewport));
            Assert.That(scroll.content, Is.EqualTo((RectTransform)content));
            Assert.That(viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(content.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            Assert.That(fitter, Is.Not.Null);
            Assert.That(fitter.verticalFit,
                Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
            Assert.That(candidates.IsChildOf(content), Is.True);
            Assert.That(log.IsChildOf(content), Is.True);
            Assert.That(AddM03NpcAiInspector.Validate(_scene, out string error), Is.True, error);
        }

        [Test]
        public void Generator_ReplacesAnExistingGeneratedHierarchyWithoutDuplicates()
        {
            Assert.That(FindAll(AddM03NpcAiInspector.ScrollName).Length, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(
                Find(AddM03NpcAiInspector.ScrollName).gameObject);

            Assert.That(AddM03NpcAiInspector.Update(_scene),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.Success));
            Assert.That(FindAll(AddM03NpcAiInspector.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM03NpcAiInspector.ScrollName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM03NpcAiInspector.CandidatesName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM03NpcAiInspector.LogName).Length, Is.EqualTo(1));
            Assert.That(AddM03NpcAiInspector.Validate(_scene, out string error), Is.True, error);
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
