using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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
        public void IndustryPanel_ShowsEachIndustriesMonthlyRuleDetail()
        {
            IndustryPanel panel = Panel();
            panel.Open();

            // The detail label is authored from the read-only catalog rule, so the exact text
            // (upkeep, separator and output order) is what the player reads. A blank or
            // loosely-matching line would hide a wrong rule behind a passing test.
            Assert.That(panel.FarmDetail,
                Is.EqualTo("每月规则：维护 1 物资（自留存）｜产出 +2 资金、+4 物资、+4 粮食"));
            Assert.That(panel.TradingDetail,
                Is.EqualTo("每月规则：维护 6 资金、2 物资｜产出 +9 资金、+1 情报"));
            Assert.That(panel.FarmDetailVisible, Is.True);
            Assert.That(panel.TradingDetailVisible, Is.True);
        }

        [Test]
        public void IndustryPanel_RefitsReportBandAfterHouseholdPageResizesPanel()
        {
            IndustryPanel panel = Panel();
            panel.Open();

            RectTransform panelRect = (RectTransform)Find(AddSC019IndustryPanel.PanelName);
            RectTransform reportRect = (RectTransform)Find(AddSC019IndustryPanel.ReportScrollName);
            Assert.That(reportRect.rect.height, Is.EqualTo(68f).Within(0.1f));

            // ReferenceGalleryNavigator applies the household card size after the panel's
            // first refresh. The next frame must re-fit the report before it is drawn.
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 560f);
            var lateUpdate = typeof(IndustryPanel).GetMethod("LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null,
                "the panel must observe its post-open household layout change");
            lateUpdate.Invoke(panel, null);

            float reportBottom = reportRect.anchoredPosition.y - reportRect.rect.height * 0.5f;
            Assert.That(reportBottom, Is.GreaterThanOrEqualTo(-panelRect.rect.height * 0.5f + 3.9f),
                "the report band must remain above the household card's bottom margin");
            Assert.That(reportRect.rect.height, Is.LessThan(68f),
                "the report band must use the shorter household card layout on first open");
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
            panel.ClickConfirm();
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
            Assert.That(panel.CanConfirmSelection, Is.True);
            Assert.That(panel.ConfirmInteractable, Is.False,
                "confirmation is unavailable until a candidate is selected");
            Assert.That(panel.FarmCandidateLabel(0), Does.Contain("剩余 4/4"));

            panel.ClickFarmCandidate(0);
            panel.Refresh();
            Assert.That(panel.HasPendingAssignment, Is.True);
            Assert.That(panel.PreviewVisible, Is.True);
            Assert.That(panel.ConfirmVisible, Is.True);
            Assert.That(panel.ConfirmInteractable, Is.True);
            string pendingLabel = panel.FarmCandidateLabel(0);
            Assert.That(pendingLabel, Does.StartWith("▶ "),
                "the candidate list marks the pending choice");
            string pendingName = pendingLabel.Substring(2, pendingLabel.IndexOf("（") - 2);
            Assert.That(panel.PreviewLine, Does.Contain("已选择：" + pendingName + " → "));
            Assert.That(panel.PreviewLine, Does.Contain("待确认"));
            Assert.That(panel.PreviewLine, Does.Contain("每月规则："),
                "the preview exposes the selected post's consequence");
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True,
                "candidate selection is only a preview until confirmed");
            string firstAssignee = panel.PendingPersonId.Value;

            panel.ClickConfirm();
            panel.Refresh();
            Assert.That(panel.FarmAssignee, Does.Contain("3/4 已用"));
            Assert.That(panel.FarmAssignee, Does.Contain("1 可用"));
            Assert.That(panel.FarmCandidateCount, Is.EqualTo(5),
                "the assigned person is no longer a candidate for that post");
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.Value,
                Is.EqualTo(firstAssignee));
            Assert.That(panel.ConfirmVisible, Is.False);
            Assert.That(panel.ConfirmInteractable, Is.False);

            string committedState = JsonUtility.ToJson(_session.CaptureSave());
            panel.ClickConfirm();
            Assert.That(JsonUtility.ToJson(_session.CaptureSave()), Is.EqualTo(committedState),
                "a repeated confirmation with no pending selection is a no-op");

            panel.ClickFarmCandidate(0);
            panel.Refresh();
            string replacementLabel = panel.FarmCandidateLabel(0);
            Assert.That(replacementLabel, Does.StartWith("▶ "));
            string replacementName = replacementLabel.Substring(2, replacementLabel.IndexOf("（") - 2);
            Assert.That(panel.PreviewLine, Does.Contain("已选择：" + replacementName + " → "));
            Assert.That(panel.PreviewLine, Does.Contain("替换现任"),
                "replacement preview identifies the current holder");
            Assert.That(panel.ConfirmInteractable, Is.True);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.Value,
                Is.EqualTo(firstAssignee), "replacement selection must not commit early");
            panel.ClickConfirm();
            panel.Refresh();
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.Value,
                Is.Not.EqualTo(firstAssignee));
            Assert.That(panel.TradingCandidateCount, Is.EqualTo(5),
                "the replaced person's reservation is released");
        }

        [Test]
        public void IndustryPanel_EligibleCandidateTotalDropsAfterAnAppointment()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            Assert.That(panel.EligibleCandidateTotal, Is.EqualTo(6),
                "every eligible family member is counted once across both posts");

            panel.ClickFarmCandidate(0);
            panel.ClickConfirm();
            panel.Refresh();

            Assert.That(panel.EligibleCandidateTotal, Is.EqualTo(5),
                "the appointed person no longer has the work units for either post");
            Assert.That(panel.FarmCandidateCount, Is.EqualTo(5));
            Assert.That(panel.TradingCandidateCount, Is.EqualTo(5),
                "both rows list the same five remaining people");
        }

        [Test]
        public void IndustryPanel_LegacySceneExplainsThatAssignmentControlsAreMissing()
        {
            IndustryPanel panel = Panel();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("previewText").objectReferenceValue = null;
            serialized.FindProperty("confirmButton").objectReferenceValue = null;
            serialized.FindProperty("cancelButton").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            panel.Open();

            Assert.That(panel.IsBound, Is.True,
                "legacy optional controls must not prevent the rest of the panel from binding");
            Assert.That(panel.CanConfirmSelection, Is.False);
            Assert.That(panel.FarmCandidateCount, Is.GreaterThan(0));
            Button candidate = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .First(button => button.name.StartsWith(
                    "IndustryCandidate_" + IndustryCatalog.FarmId + "_"));
            Assert.That(candidate.interactable, Is.False,
                "legacy candidate buttons must not imply a working one-click assignment");
            candidate.onClick.Invoke();
            Assert.That(panel.HasPendingAssignment, Is.False);
            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain(IndustryPanel.ConfirmUnboundNotice));
        }

        [Test]
        public void IndustryPanel_UsesBlockReasonWhenLegacyMessageTextIsUnbound()
        {
            IndustryPanel panel = Panel();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("messageText").objectReferenceValue = null;
            serialized.FindProperty("previewText").objectReferenceValue = null;
            serialized.FindProperty("confirmButton").objectReferenceValue = null;
            serialized.FindProperty("cancelButton").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            panel.Open();
            Assert.That(panel.IsBound, Is.True,
                "the message label is optional when the block-reason label can carry its fallback");
            Assert.That(panel.IsOpen, Is.True);
            Button candidate = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .First(button => button.name.StartsWith(
                    "IndustryCandidate_" + IndustryCatalog.FarmId + "_"));
            candidate.onClick.Invoke();

            Assert.That(panel.IsOpen, Is.True,
                "an absent optional message label must not disable the rest of the panel");
            Assert.That(panel.BlockReasonVisible, Is.True);
            Assert.That(panel.BlockReasonLine, Does.Contain(IndustryPanel.ConfirmUnboundNotice));
        }

        [Test]
        public void IndustryPanel_CancelCandidatePreviewLeavesAssignmentUnchanged()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            IndustrySlotView before = _session.Industry.Slot(IndustryCatalog.FarmId);
            IndustryResources resourcesBefore = _session.Industry.Resources;

            panel.ClickFarmCandidate(0);
            panel.Refresh();
            Assert.That(panel.HasPendingAssignment, Is.True);
            Assert.That(panel.ConfirmVisible, Is.True);
            Assert.That(panel.CancelVisible, Is.True);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId,
                Is.EqualTo(before.AssigneeId));

            panel.ClickCancel();
            panel.Refresh();

            Assert.That(panel.HasPendingAssignment, Is.False);
            Assert.That(panel.PreviewVisible, Is.True,
                "the idle preview hint remains available after cancelling a selection");
            Assert.That(panel.PreviewLine, Does.Contain("选择一名家人后确认任命"));
            Assert.That(panel.ConfirmVisible, Is.False);
            Assert.That(panel.CancelVisible, Is.False);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId,
                Is.EqualTo(before.AssigneeId));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resourcesBefore));
        }

        [Test]
        public void IndustryPanel_PendingIndustryIdTracksSelectionAndClearsOnCancel()
        {
            IndustryPanel panel = Panel();
            panel.Open();

            panel.ClickFarmCandidate(0);
            panel.Refresh();
            PersonId candidate = panel.PendingPersonId;
            Assert.That(candidate.IsEmpty, Is.False,
                "the fixture needs a real candidate before it can select one explicitly");

            panel.ClickCancel();
            panel.Refresh();
            Assert.That(panel.PendingIndustryId, Is.EqualTo(string.Empty),
                "cancelling the preview clears the industry as well as the person");

            panel.SelectPending(IndustryCatalog.FarmId, candidate);
            Assert.That(panel.PendingIndustryId, Is.EqualTo("industry.farm"));
            Assert.That(panel.PendingIndustryId, Is.EqualTo(IndustryCatalog.FarmId),
                "the pending industry is the selected post, not the selected person");

            panel.SelectPending(IndustryCatalog.TradingHouseId, candidate);
            Assert.That(panel.PendingIndustryId, Is.EqualTo("industry.trading-house"),
                "re-selecting another post replaces the pending industry");

            panel.ClickCancel();
            panel.Refresh();
            Assert.That(panel.PendingIndustryId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void IndustryPanel_CancelWithoutPendingAssignmentLeavesStateUnchanged()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            IndustryResources before = _session.Industry.Resources;

            panel.ClickCancel();

            Assert.That(panel.HasPendingAssignment, Is.False);
            Assert.That(panel.CancelVisible, Is.False);
            Assert.That(_session.Industry.Resources, Is.EqualTo(before));
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);
        }

        [Test]
        public void IndustryPanel_CloseCancelsUnconfirmedAssignmentPreview()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            IndustrySlotView before = _session.Industry.Slot(IndustryCatalog.FarmId);
            panel.ClickFarmCandidate(0);
            Assert.That(panel.HasPendingAssignment, Is.True);

            panel.Close();
            panel.Open();

            Assert.That(panel.HasPendingAssignment, Is.False,
                "reopening must not preserve a hidden, unconfirmed assignment");
            Assert.That(panel.ConfirmVisible, Is.False);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId,
                Is.EqualTo(before.AssigneeId));
        }

        [Test]
        public void IndustryPanel_DropsPreviewWhenTheCandidateBecomesUnavailable()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            panel.ClickFarmCandidate(0);
            PersonId selected = panel.PendingPersonId;
            Assert.That(selected.IsEmpty, Is.False);

            IndustryAssignmentResult assignedElsewhere = _session.TryAssignIndustry(
                IndustryCatalog.TradingHouseId, selected);
            Assert.That(assignedElsewhere.Succeeded, Is.True, assignedElsewhere.Message);
            panel.Refresh();

            Assert.That(panel.HasPendingAssignment, Is.False,
                "a candidate already assigned elsewhere must not remain confirmable");
            Assert.That(panel.ConfirmVisible, Is.False);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);
        }

        [Test]
        public void IndustryPanel_UnassignClearsSlot()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            panel.ClickTradingCandidate(0);
            panel.Refresh();
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeId.IsEmpty,
                Is.True, "candidate selection does not mutate the assignment");
            panel.ClickConfirm();
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
        public void IndustryPanel_SettlementMessageStaysAboveTheFarmRow()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            panel.ClickFarmCandidate(0);
            panel.ClickConfirm();
            panel.ClickTradingCandidate(0);
            panel.ClickConfirm();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            panel.Refresh();

            var band = (RectTransform)Find(AddSC019IndustryPanel.MessageName);
            var label = band.GetComponent<Text>();
            var farm = (RectTransform)Find(AddSC019IndustryPanel.FarmRowName);

            Assert.That(panel.MessageVisible, Is.True,
                "the settled month publishes a status message");
            Assert.That(panel.MessageLine, Does.StartWith("农庄"),
                "the message is the month settlement summary");
            Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(band.rect.height),
                "the settlement summary must wrap inside the authored status band");

            float textBottom = band.anchoredPosition.y - label.preferredHeight * 0.5f;
            float farmTop = farm.anchoredPosition.y + farm.rect.height * 0.5f;
            Assert.That(textBottom, Is.GreaterThan(farmTop),
                "the rendered settlement text must stay above the farm row");
        }

        [Test]
        public void IndustryPanel_UsesMonthlyReportPointerWhenSettlementSummaryExceedsBandCapacity()
        {
            IndustryPanel panel = Panel();
            panel.Open();
            panel.ClickFarmCandidate(0);
            panel.ClickConfirm();
            panel.ClickTradingCandidate(0);
            panel.ClickConfirm();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            panel.Refresh();

            string fullSummary = panel.MessageLine;
            var band = (RectTransform)Find(AddSC019IndustryPanel.MessageName);
            Assert.That(panel.StatusBandLineBudget, Is.EqualTo(3));
            Assert.That(panel.StatusLineCount, Is.EqualTo(3));
            Assert.That(panel.StatusPointerActive, Is.False);
            Assert.That(panel.ReportLine, Does.Contain(fullSummary));

            band.sizeDelta = new Vector2(band.sizeDelta.x, 24f);
            panel.Refresh();

            Assert.That(panel.StatusBandLineBudget, Is.EqualTo(1));
            Assert.That(panel.StatusLineCount, Is.EqualTo(3));
            Assert.That(panel.StatusPointerActive, Is.True);
            Assert.That(panel.MessageLine, Is.EqualTo(IndustryPanel.ReportPointerStatus));
            Assert.That(panel.ReportLine, Does.Contain(fullSummary),
                "the detailed monthly report retains the complete settlement text");
        }

        [Test]
        public void IndustryPanel_LongActionMessageIsPreservedInReportWhenStatusBandCannotFitIt()
        {
            const string fragment = "执行人仍在委托任务中，须等结算后再任命产业职务。";
            string longMessage = string.Concat(Enumerable.Repeat(fragment, 8));
            FieldInfo lastMessage = typeof(GameSession).GetField(
                "<LastIndustryMessage>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lastMessage, Is.Not.Null);
            lastMessage.SetValue(_session, longMessage);

            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.StatusPointerActive, Is.True);
            Assert.That(panel.MessageLine, Is.EqualTo(IndustryPanel.ReportPointerStatus));
            Assert.That(panel.ReportLine, Does.Contain(longMessage),
                "a long non-settlement action message must remain fully readable outside the clipped band");
            Assert.That(Find("IndustryReportContent").GetComponent<RectTransform>().rect.height,
                Is.GreaterThan(Find("IndustryReportViewport").GetComponent<RectTransform>().rect.height),
                "long action detail must remain reachable by scrolling");
        }

        [Test]
        public void IndustryPanel_LongRollbackMessagePointerDoesNotClaimSettlementCompleted()
        {
            const string rollbackPrefix = "月结异常，已回滚到本月初始状态：";
            const string fragment = "结算流水写入失败，状态已恢复；";
            string rollbackMessage = rollbackPrefix + string.Concat(Enumerable.Repeat(fragment, 24));
            FieldInfo lastMessage = typeof(GameSession).GetField(
                "<LastIndustryMessage>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lastMessage, Is.Not.Null);
            lastMessage.SetValue(_session, rollbackMessage);

            IndustryPanel panel = Panel();
            panel.Open();

            Assert.That(panel.StatusLineCount, Is.GreaterThan(panel.StatusBandLineBudget),
                "the fixture must overflow the authored status band, not sit at its capacity");
            Assert.That(panel.StatusPointerActive, Is.True);
            Assert.That(panel.MessageLine, Does.Not.Contain("已完成"),
                "a long rollback message must not be replaced by a success claim");
            Assert.That(panel.MessageLine, Is.EqualTo(IndustryPanel.ReportPointerStatus));
            Assert.That(panel.ReportLine, Does.Contain(rollbackMessage),
                "the report must retain the rollback reason in full");

            ScrollRect scroll = Find("IndustryReportScrollRect").GetComponent<ScrollRect>();
            RectTransform viewport = Find("IndustryReportViewport") as RectTransform;
            RectTransform content = Find("IndustryReportContent") as RectTransform;
            Text report = Find(AddSC019IndustryPanel.ReportName).GetComponent<Text>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.content, Is.EqualTo(content));
            Assert.That(scroll.viewport, Is.EqualTo(viewport));
            Assert.That(content.rect.height, Is.GreaterThan(viewport.rect.height),
                "long rollback detail must extend beyond the viewport and remain reachable by scrolling");
            Assert.That(content.rect.height + 0.5f, Is.GreaterThanOrEqualTo(report.preferredHeight),
                "the scroll content must contain the complete report text");
            Canvas.ForceUpdateCanvases();
            float maxScroll = content.rect.height - viewport.rect.height;
            float initialOffset = content.anchoredPosition.y;
            Assert.That(maxScroll, Is.GreaterThan(0f));
            scroll.verticalNormalizedPosition = 0f;
            Assert.That(content.anchoredPosition.y, Is.EqualTo(maxScroll).Within(1f),
                "moving to the bottom must expose the end of the full report content");
            Assert.That(content.anchoredPosition.y, Is.GreaterThan(initialOffset));
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
