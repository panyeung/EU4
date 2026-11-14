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
            Assert.That(panel.ReportLine, Does.StartWith("本月尚未结算。"));
            Assert.That(panel.ReportLine, Does.Contain("粮食连续短缺 0 个月"),
                "a zero consecutive shortage is a real state and stays visible");
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
        public void IndustryPanel_ShowsMarketInventoryPricesAndHouseholdShortage()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.TradingHouseId,
                M2FixedScenario.ChieId).Succeeded, Is.True, _session.LastIndustryMessage);
            SaveData shortStock = _session.CaptureSave();
            shortStock.market.foodInventory = 1;
            Assert.That(_session.TryApplySave(shortStock).Succeeded, Is.True, _session.LastEventMessage);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            MarketMonthReport report = _session.LastMarketReport;
            Assert.That(report, Is.Not.Null, "a settled month publishes its market report");
            Assert.That(report.FoodDemand, Is.EqualTo(2));
            Assert.That(report.FoodPurchased, Is.EqualTo(1),
                "one stocked unit is affordable and the second is a shortage");
            Assert.That(report.FoodUnmet, Is.EqualTo(1));
            Assert.That(report.Materials.Demand, Is.GreaterThan(0),
                "the operating trading house still needs material input");

            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.ResourceLine,
                Does.Contain("资金 " + _session.Industry.Resources.Funds),
                "the pre-settlement SC-019 resource values stay visible");
            Assert.That(panel.ResourceLine,
                Does.Contain("粮食 " + _session.Market.FoodInventory));
            Assert.That(panel.ResourceLine,
                Does.Contain("粮价 " + _session.Market.FoodPriceIndexPermille + "‰"));
            Assert.That(panel.ResourceLine,
                Does.Contain("物资 " + _session.Industry.Resources.Supplies));
            Assert.That(panel.ResourceLine,
                Does.Contain("物资价 " + _session.Market.MaterialsPriceIndexPermille + "‰"));
            Assert.That(panel.ResourceLine,
                Does.Contain("情报 " + _session.Industry.Resources.Intelligence));

            Assert.That(panel.ReportLine, Does.Contain("需粮 2"));
            Assert.That(panel.ReportLine, Does.Contain("购入 1"));
            Assert.That(panel.ReportLine, Does.Contain("缺粮 1"));
            Assert.That(panel.ReportLine, Does.Contain("材料成本 2资金"));
            Assert.That(panel.ReportLine, Does.Contain("粮食连续短缺 1 个月"),
                "the shortage status comes from the settled market state");
        }

        [Test]
        public void IndustryPanel_ShowsZeroShortageStatusAfterLoad()
        {
            SaveData settled = _session.CaptureSave();
            settled.market.foodInventory = 4;
            settled.market.consecutiveFoodShortageMonths = 0;
            Assert.That(_session.TryApplySave(settled).Succeeded, Is.True, _session.LastEventMessage);
            Assert.That(_session.LastMarketReport, Is.Null);
            Assert.That(_session.Market.ConsecutiveFoodShortageMonths, Is.Zero);

            IndustryPanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.ReportLine, Does.Contain("粮食连续短缺 0 个月"),
                "a persisted zero count must not disappear from the panel");
            Assert.That(panel.ReportLine, Does.Not.Contain("缺粮"),
                "the current-month unmet quantity is a separate value from the shortage duration");
        }

        [Test]
        public void IndustryPanel_ShowsPersistedMarketStateAfterLoad()
        {
            SaveData stressed = _session.CaptureSave();
            stressed.market.foodInventory = 0;
            stressed.market.foodPriceIndexPermille = 1234;
            stressed.market.materialsPriceIndexPermille = 789;
            stressed.market.consecutiveFoodShortageMonths = 2;
            Assert.That(_session.TryApplySave(stressed).Succeeded, Is.True, _session.LastEventMessage);
            Assert.That(_session.LastMarketReport, Is.Null);
            Assert.That(_session.LastIndustryReport, Is.Null);

            IndustryPanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.ResourceLine, Does.Contain("粮食 0"));
            Assert.That(panel.ResourceLine, Does.Contain("粮价 1234‰"));
            Assert.That(panel.ResourceLine, Does.Contain("物资价 789‰"));
            Assert.That(panel.ResourceLine,
                Does.Contain("物资 " + _session.Industry.Resources.Supplies));
            Assert.That(panel.ReportLine, Does.Contain("粮食连续短缺 2 个月"),
                "shortage status is derived from the persisted market state, not a report");
        }

        [Test]
        public void IndustryPanel_PredictsStalledForMaterialCostShortfall()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.TradingHouseId,
                M2FixedScenario.ChieId).Succeeded, Is.True, _session.LastIndustryMessage);

            // Funds 8 cover the trading house's fixed upkeep 6 but not the M08B
            // material cost ceil(2 * 1500 / 1000) = 3, so settlement stalls by 1 fund.
            SaveData stressed = _session.CaptureSave();
            stressed.industry.funds = 8;
            stressed.industry.supplies = 12;
            stressed.market.materialsPriceIndexPermille = 1500;
            Assert.That(_session.TryApplySave(stressed).Succeeded, Is.True, _session.LastEventMessage);

            IndustryPanel panel = Panel();
            panel.Open();
            panel.Refresh();

            Assert.That(panel.TradingStatus, Does.Contain("资源不足"));
            Assert.That(panel.TradingStatus, Does.Contain("缺 1 资金"));
            Assert.That(panel.TradingStatus, Does.Contain("材料成本 3 资金"));
            Assert.That(panel.TradingStatus, Does.Not.Contain("运作中"),
                "the preview cannot promise production the settlement will refuse");

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            IndustryMonthReport report = _session.LastIndustryReport;
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Lines.First(line => line.IndustryId == IndustryCatalog.TradingHouseId).Status,
                Is.EqualTo(IndustryLineStatus.Stalled),
                "the settled line must match the previewed status");
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
            Assert.That(panel.ReportLine, Does.Contain("31"));
        }
    }
}
