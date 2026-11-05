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
    /// <summary>SC-019 Task 7: the uGUI panel exposes resources, status, candidates and report.</summary>
    public sealed class IndustryPanelTests
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
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private IndustryPanel Panel() =>
            Find(AddSC019IndustryPanel.RootName).GetComponent<IndustryPanel>();

        private GameObject Create(string name, Transform parent, params System.Type[] types)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (System.Type type in types)
                go.AddComponent(type);
            return go;
        }

        private Transform Find(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .First(transform => transform.name == name);

        [Test]
        public void IndustryPanel_ShowsResourcesSlotsStatusesAndReport()
        {
            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.ResourceLine, Does.Contain("资金 30"));
            Assert.That(panel.ResourceLine, Does.Contain("物资 12"));
            Assert.That(panel.ResourceLine, Does.Contain("情报 0"));
            Assert.That(panel.FarmStatus, Does.Contain("空缺"));
            Assert.That(panel.TradingStatus, Does.Contain("空缺"));
            Assert.That(panel.FarmAssignee, Is.EqualTo("负责人：空缺"));
            Assert.That(panel.ReportLine, Is.EqualTo("本月尚未结算。"));
            Assert.That(panel.MessageVisible, Is.False);
            Assert.That(panel.FarmCandidateCount, Is.EqualTo(6));
        }

        [Test]
        public void IndustryPanel_CandidatesUseDistinctInPanelGridPositionsForBothIndustries()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            panel.RebuildCandidateLayout();

            Assert.That(panel.FarmCandidateCount, Is.EqualTo(6));
            Assert.That(panel.TradingCandidateCount, Is.EqualTo(6));

            AssertDistinctInside(panel, true);
            AssertDistinctInside(panel, false);

            panel.ClickTradingCandidate(2);
            panel.Refresh();
            Assert.That(panel.TradingCandidateCount, Is.EqualTo(5),
                "the assigned trading candidate must leave the selectable list");
            panel.RebuildCandidateLayout();
            AssertDistinctInside(panel, false);
        }

        [Test]
        public void IndustryPanel_CandidateAreasAreInsidePanelBounds()
        {
            IndustryPanel panel = Panel();
            panel.Open();

            RectTransform panelRect = (RectTransform)Find(AddSC019IndustryPanel.PanelName);
            Assert.That(panel.FarmCandidateArea, Is.Not.Null);
            Assert.That(panel.TradingCandidateArea, Is.Not.Null);
            Assert.That(IsInside(panelRect, panel.FarmCandidateArea), Is.True);
            Assert.That(IsInside(panelRect, panel.TradingCandidateArea), Is.True);
        }

        private static void AssertDistinctInside(IndustryPanel panel, bool farm)
        {
            int count = farm ? panel.FarmCandidateCount : panel.TradingCandidateCount;
            RectTransform area = farm ? panel.FarmCandidateArea : panel.TradingCandidateArea;
            var positions = new System.Collections.Generic.List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                Vector2 position = farm
                    ? panel.FarmCandidatePosition(i)
                    : panel.TradingCandidatePosition(i);
                Assert.That(area.rect.Contains(position), Is.True,
                    "candidate " + i + " must be placed inside its container");
                positions.Add(position);
            }

            for (int i = 0; i < positions.Count; i++)
                for (int j = i + 1; j < positions.Count; j++)
                    Assert.That(positions[i], Is.Not.EqualTo(positions[j]),
                        "candidate buttons must not overlap on the same position");

            for (int i = 0; i < positions.Count; i++)
                for (int j = i + 1; j < positions.Count; j++)
                {
                    var first = new Rect(positions[i] - new Vector2(98f, 14f), new Vector2(196f, 28f));
                    var second = new Rect(positions[j] - new Vector2(98f, 14f), new Vector2(196f, 28f));
                    Assert.That(first.Overlaps(second), Is.False,
                        "candidate rectangles must not overlap");
                }
        }

        private static bool IsInside(RectTransform parent, RectTransform child)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = parent.InverseTransformPoint(corner);
                if (!parent.rect.Contains(new Vector2(local.x, local.y)))
                    return false;
            }
            return true;
        }

        [Test]
        public void IndustryPanel_ShowsBlockReasonWhenPendingEvent()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.BlockReasonVisible, Is.True);
            Assert.That(panel.BlockReasonLine, Does.Contain("事件"));
            Assert.That(panel.FarmCandidateCount, Is.Zero,
                "appointments are locked while an event is pending");
        }

        [Test]
        public void IndustryPanel_CandidateButtonsAssignAndReplace()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            Assert.That(panel.FarmCandidateLabel(0), Does.Contain("剩余 4/4"));

            panel.ClickFarmCandidate(0);
            panel.Refresh();
            Assert.That(panel.FarmAssignee, Does.Contain("3/4 已用"));
            Assert.That(panel.FarmAssignee, Does.Contain("1 可用"));
            Assert.That(panel.FarmCandidateCount, Is.EqualTo(5),
                "the assigned person is no longer a candidate for that post");

            panel.ClickFarmCandidate(0);
            panel.Refresh();
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeName, Is.Not.Empty);
            Assert.That(panel.TradingCandidateCount, Is.EqualTo(5),
                "the replaced person's reservation is released");
        }

        [Test]
        public void IndustryPanel_UnassignClearsSlot()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            panel.ClickTradingCandidate(0);
            panel.Refresh();
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeId.IsEmpty, Is.False);

            panel.ClickTradingUnassign();
            panel.Refresh();

            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeId.IsEmpty, Is.True);
            Assert.That(panel.TradingAssignee, Is.EqualTo("负责人：空缺"));
        }

        [Test]
        public void IndustryPanel_ShowsActionableReasonForRejectedAssignment()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True);
            IndustryAssignmentResult rejected = _session.TryAssignIndustry(
                IndustryCatalog.TradingHouseId, M2FixedScenario.HarunobuId);
            Assert.That(rejected.Succeeded, Is.False);

            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain("时间不足"));
            Assert.That(panel.MessageLine, Does.Contain("3/4"));
        }

        [Test]
        public void IndustryPanel_ShowsBlockReasonAndMessageWhenAppointmentsLocked()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            IndustryAssignmentResult blocked = _session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.ChieId);
            Assert.That(blocked.Succeeded, Is.False);

            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.BlockReasonVisible, Is.True);
            Assert.That(panel.BlockReasonLine, Does.Contain("事件"));
            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain("事件"));
        }

        [Test]
        public void IndustryPanel_ShowsInvalidAssignmentCleanupAfterCrisisRosterReplacement()
        {
            _session.InitStandardDemo();
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.TradingHouseId,
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.StartCrisisDemo(), Is.True);

            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);
            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain("已移除失效产业任命"));
            Assert.That(panel.FarmAssignee, Is.EqualTo("负责人：空缺"));
        }

        [Test]
        public void IndustryPanel_ReportFallsBackToLastChronicleAfterLoad()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            SaveData saved = _session.CaptureSave();
            Assert.That(_session.TryApplySave(saved).Succeeded, Is.True);
            Assert.That(_session.LastIndustryReport, Is.Null);

            IndustryPanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.ReportLine, Does.Contain("月末"));
            Assert.That(panel.ReportLine, Does.Contain("32"));
        }
    }
}
