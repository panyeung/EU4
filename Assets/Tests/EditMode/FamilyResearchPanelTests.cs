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
    /// <summary>SC-021 Task 4: panel projection, real button callbacks and single refresh.</summary>
    public sealed class FamilyResearchPanelTests
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
            _session.InitResearchDemoScenario();
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private FamilyResearchPanel Panel() =>
            Find(AddSC021FamilyResearch.RootName).GetComponent<FamilyResearchPanel>();

        [Test]
        public void Panel_ShowsBudgetProgressTechniqueAndThreeResearchers()
        {
            FamilyResearchPanel panel = Panel();
            panel.Open();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.TitleLine, Does.Contain("家族研究"));
            Assert.That(panel.ResourceLine, Does.Contain("资金 2").And.Contain("物资 2"));
            Assert.That(panel.TechniqueLine, Does.Contain("烟幕调制术"));
            Assert.That(panel.TechniqueLine, Does.Not.Contain("已记录"));
            Assert.That(panel.ProgressLine, Does.Contain("0/2"));
            Assert.That(panel.ProgressLine, Does.Contain("每次月结推进 1 点"),
                "the initial panel must show the monthly rate once");
            Assert.That(panel.CostLine, Does.Contain("1 资金").And.Contain("1 物资"));
            Assert.That(panel.CostLine, Does.Not.Contain("推进"),
                "the rate belongs to the progress line, not the monthly cost line");
            Assert.That(panel.VisibleResearcherCount, Is.EqualTo(3));
            Assert.That(panel.ResearcherLabel(0), Is.EqualTo(SC021FamilyResearchScenario.HeadName));
        }

        [Test]
        public void Panel_ContainerKeepsOnlyTheInactiveTemplateAfterGeneration()
        {
            Transform container = Find(AddSC021FamilyResearch.ResearcherContainerName);
            Assert.That(container.childCount, Is.EqualTo(1));
            Assert.That(container.GetChild(0).name,
                Is.EqualTo(AddSC021FamilyResearch.ResearcherTemplateName));
            Assert.That(container.GetChild(0).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Panel_ClickingResearcherStartsPlanAndRefreshesExactlyOnce()
        {
            FamilyResearchPanel panel = Panel();
            panel.Open();
            int refreshes = panel.RefreshCount;

            panel.ClickResearcher(0);

            Assert.That(_session.ResearchState.HasPlan, Is.True);
            Assert.That(_session.ResearchState.ResearcherId,
                Is.EqualTo(SC021FamilyResearchScenario.HeadId));
            Assert.That(panel.RefreshCount, Is.EqualTo(refreshes + 1));
            Assert.That(panel.StatusLine, Does.Contain(SC021FamilyResearchScenario.HeadName));
            Assert.That(panel.ProgressLine, Does.Contain("0/2"));
        }

        [Test]
        public void Panel_RejectedPlanShowsMessageAndRefreshesExactlyOnce()
        {
            FamilyResearchPanel panel = Panel();
            panel.Open();
            // Mutate the roster directly so the already-built head button becomes stale
            // without publishing a session refresh first.
            _session.Succession.World.TrySetLife(SC021FamilyResearchScenario.HeadId,
                new SuccessionPersonLife(SC021FamilyResearchScenario.HeadBirthYear, false, false), out _);
            int refreshes = panel.RefreshCount;

            panel.ClickResearcher(0);

            Assert.That(_session.ResearchState.HasPlan, Is.False);
            Assert.That(panel.RefreshCount, Is.EqualTo(refreshes + 1));
            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain("研究者"));
            Assert.That(panel.VisibleResearcherCount, Is.EqualTo(2));
        }

        [Test]
        public void Panel_KnownTechniqueHidesResearcherChoicesAndMonthlyCost()
        {
            CompleteResearch();

            FamilyResearchPanel panel = Panel();
            panel.Open();

            Assert.That(_session.ResearchState.IsKnownTechnique(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True);
            Assert.That(panel.VisibleResearcherCount, Is.Zero,
                "a recorded family technique must not offer new researcher buttons");
            Assert.That(panel.CostLine, Does.Contain("已记录为家族知识"));
            Assert.That(panel.CostLine, Does.Not.Contain("每月消耗"));
            Assert.That(panel.TechniqueLine, Does.Contain("已记录"));
            Assert.That(panel.ProgressLine, Does.Contain("已完成"));
            Assert.That(panel.ReportLine, Does.Contain("研究完成"));
            Assert.That(panel.MessageVisible, Is.False,
                "the completed settlement summary must not be rendered twice");
        }

        [Test]
        public void Panel_DoesNotAutoOpenWhenEnteringTheResearchScenario()
        {
            FamilyResearchPanel panel = Panel();
            Assert.That(panel.IsOpen, Is.False);

            _session.InitStandardDemo();
            Assert.That(panel.IsOpen, Is.False);

            _session.InitResearchDemoScenario();
            Assert.That(panel.IsOpen, Is.False,
                "the SC-021 demo requires the player to open the panel explicitly");

            Find(AddSC021FamilyResearch.OpenButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True);
        }

        [Test]
        public void Panel_OpenAndCloseButtonsDriveThePanel()
        {
            FamilyResearchPanel panel = Panel();

            Find(AddSC021FamilyResearch.OpenButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True);

            Find(AddSC021FamilyResearch.CloseButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void Panel_ActiveMonthShowsOneProgressResult()
        {
            FamilyResearchPanel panel = Panel();
            panel.Open();
            panel.ClickResearcher(0);

            // The plan confirmation must not restate the researcher, monthly rate and cost
            // that the status/progress/cost lines already show.
            Assert.That(panel.StatusLine, Does.Contain(SC021FamilyResearchScenario.HeadName));
            Assert.That(panel.StatusLine, Does.Not.Contain("0/2"));
            Assert.That(panel.ProgressLine, Does.Contain("0/2"));
            Assert.That(panel.ProgressLine, Does.Contain("每次月结推进 1 点"));
            Assert.That(panel.CostLine, Does.Contain("1 资金").And.Contain("1 物资"));
            Assert.That(panel.MessageLine, Does.Not.Contain(SC021FamilyResearchScenario.HeadName));
            Assert.That(panel.MessageLine, Does.Not.Contain("资金"));
            Assert.That(panel.MessageLine, Does.Not.Contain("推进"));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            panel.Refresh();

            // One settled month must yield exactly one progress summary: the status line
            // keeps the researcher, the progress line owns "1/2", and the monthly report
            // owns the settlement result instead of being echoed by the message line.
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(1));
            Assert.That(panel.StatusLine, Does.Contain(SC021FamilyResearchScenario.HeadName));
            Assert.That(panel.StatusLine, Does.Not.Contain("1/2"));
            Assert.That(panel.ProgressLine, Does.Contain("1/2"));
            Assert.That(panel.ReportLine, Does.Contain("0 → 1"));
            Assert.That(panel.MessageVisible, Is.False,
                "the settlement result must not be printed in both the report and message lines");
        }

        [Test]
        public void Panel_ShowsShortagePauseFromTheMonthlyReport()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();
            data.industry.funds = 0;
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);

            FamilyResearchPanel panel = Panel();
            panel.Open();

            // The pause reason and the unchanged progress must appear once, on the
            // report line. The status and progress lines stay neutral, the progress
            // line makes no monthly-advance promise, and the settlement message is
            // not echoed back as a second identical confirmation.
            Assert.That(panel.ReportLine, Does.Contain("研究暂停"));
            Assert.That(panel.ReportLine, Does.Contain("资金不足 1"));
            Assert.That(panel.ReportLine, Does.Contain("进度保持 0/2"));
            Assert.That(panel.ReportLine, Does.Contain("本月不推进"));
            Assert.That(panel.StatusLine, Does.Not.Contain("研究暂停"));
            Assert.That(panel.ProgressLine, Does.Contain("0/2"));
            Assert.That(panel.ProgressLine, Does.Not.Contain("每次月结推进"));
            Assert.That(panel.MessageVisible, Is.False,
                "the shortage report must not be echoed by the message line");
            Assert.That(panel.MessageLine, Is.Empty,
                "a hidden message row must not retain a duplicate report");
            Assert.That(panel.BlockReasonVisible, Is.False);
            Assert.That(panel.BlockReasonLine, Is.Empty);
            Assert.That(CountOccurrences(VisibleText(panel), "研究暂停"), Is.EqualTo(1),
                "exactly one visible line must own the pause reason");
        }

        [Test]
        public void Panel_SavedFormerMemberResearcher_ShowsImmediatePauseWithoutSpending()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId).Succeeded, Is.True);
            Assert.That(_session.Succession.World.TryRemoveMember(
                SC021FamilyResearchScenario.SpouseId, out _), Is.True);

            // Persist and reload the already-invalid plan. Loading clears the previous
            // month report, so the visible pause must come from live eligibility.
            SaveData data = _session.CaptureSave();
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True, _session.LastEventMessage);
            Assert.That(_session.LastResearchReport, Is.Null);

            FamilyResearchPanel panel = Panel();
            panel.Open();

            Assert.That(panel.StatusLine, Does.Contain(SC021FamilyResearchScenario.SpouseName));
            Assert.That(panel.StatusLine, Does.Not.Contain("研究暂停"),
                "the pause reason is reported once on the report line");
            Assert.That(panel.ReportLine, Does.Contain("研究暂停"));
            Assert.That(panel.ReportLine, Does.Contain("不是当前家族成员"));
            Assert.That(panel.ReportLine, Does.Contain("进度保持 0/2"));
            Assert.That(panel.ReportLine, Does.Contain("本月不推进"));
            Assert.That(panel.ProgressLine, Does.Contain("0/2"));
            Assert.That(panel.ProgressLine, Does.Not.Contain("每次月结推进"));
            Assert.That(panel.MessageVisible, Is.False);
            Assert.That(panel.MessageLine, Is.Empty);
            Assert.That(CountOccurrences(VisibleText(panel), "研究暂停"), Is.EqualTo(1));
            Assert.That(_session.ResearchState.HasPlan, Is.True);
            Assert.That(_session.ResearchState.Progress, Is.Zero);
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(SC021FamilyResearchScenario.OpeningFunds),
                "loading a paused plan must not spend funds");
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(SC021FamilyResearchScenario.OpeningSupplies),
                "loading a paused plan must not spend supplies");
        }

        private static string VisibleText(FamilyResearchPanel panel)
        {
            string text = panel.StatusLine + "\n" + panel.ProgressLine + "\n" + panel.ReportLine;
            if (panel.MessageVisible)
                text += "\n" + panel.MessageLine;
            if (panel.BlockReasonVisible)
                text += "\n" + panel.BlockReasonLine;
            return text;
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private GameObject Create(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (System.Type component in components)
                go.AddComponent(component);
            return go;
        }

        private Transform Find(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(transform => transform.name == name);

        private void CompleteResearch()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
        }

        private static void ResolvePending(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            string pending = session.PendingEventId;
            Events.EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (Events.EventOptionData option in definition.options)
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
