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
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class AddSC010SuccessionPanelTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            _canvas = canvas.transform;
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitStandardDemo(33UL);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        [Test]
        public void UpdaterAddsValidatedPanelUnderOneFixedRoot()
        {
            int count = ObjectCount();

            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));

            Transform root = Find(AddSC010SuccessionPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(Find(AddSC010SuccessionPanel.HudName).gameObject.activeSelf, Is.True);
            Assert.That(Find(AddSC010SuccessionPanel.ModalName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC010SuccessionPanel.CandidateTemplateName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC010SuccessionPanel.RegentTemplateName).gameObject.activeSelf, Is.False);
            Assert.That(ObjectCount(), Is.GreaterThan(count));
            Assert.That(AddSC010SuccessionPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must not save or reload a scene.");
        }

        [Test]
        public void UpdaterIsIdempotentAndRollbackRemovesPartialRoot()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            int completeCount = ObjectCount();
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(completeCount));

            Scene rollbackScene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = CreateIn(rollbackScene, "Canvas", null, typeof(Canvas));
            GameObject sessionObject = CreateIn(rollbackScene, "GameSession", null, typeof(GameSession));
            GameSession session = sessionObject.GetComponent<GameSession>();
            session.InitStandardDemo(33UL);
            int rollbackCount = ObjectCount(rollbackScene);
            try
            {
                AddSC010SuccessionPanel.UpdateResult result = AddSC010SuccessionPanel.Update(rollbackScene, root =>
                {
                    SuccessionPanel panel = root.GetComponent<SuccessionPanel>();
                    panel.BindReferences(null, null, null, null, null, null, null, null,
                        null, null, null, null, null, null, null);
                });
                Assert.That(result, Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.ValidationFailed));
                Assert.That(Named(rollbackScene, AddSC010SuccessionPanel.RootName).Count(), Is.Zero);
                Assert.That(ObjectCount(rollbackScene), Is.EqualTo(rollbackCount));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(rollbackScene);
            }
        }

        [Test]
        public void UpdaterPreflightRefusesWrongAndDirtyScenes()
        {
            Assert.That(AddSC010SuccessionPanel.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.SceneMismatch));
            Assert.That(AddSC010SuccessionPanel.CheckScenePreflight(AddSC010SuccessionPanel.ScenePath, true),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.SceneDirty));
            Assert.That(AddSC010SuccessionPanel.CheckScenePreflight(AddSC010SuccessionPanel.ScenePath, false),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            Assert.That(AddSC010SuccessionPanel.CheckScenePreflight(AddSC010SuccessionPanel.ScenePath, false, true),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.EditorPlaying));
        }

        [Test]
        public void PanelRefreshesHeadAndShowsCandidatesAfterRetirement()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();

            panel.Refresh();

            Assert.That(panel.HeadSummary, Does.Contain("明智 晴信"));
            Assert.That(panel.StatusMessage, Does.Contain("家主在位"));
            Assert.That(panel.IsModalVisible, Is.False);

            panel.BeginRetirement();

            Assert.That(panel.IsModalVisible, Is.True);
            Assert.That(panel.VisibleCandidateCount, Is.EqualTo(3));
            Assert.That(panel.StatusMessage, Does.Contain("继承待定"));
            Assert.That(panel.FeedbackMessage, Does.Contain("原指定继承人"));
        }

        [Test]
        public void PanelMinorFlowRequiresRegentThenShowsRegency()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.BeginRetirement();

            panel.SelectCandidate(new PersonId("person.akechi.tokihime"));

            Assert.That(panel.VisibleRegentCount, Is.EqualTo(3));
            Assert.That(panel.ConfirmInteractable, Is.False);

            panel.SelectRegent(new PersonId("person.akechi.chie"));
            Assert.That(panel.ConfirmInteractable, Is.True);

            panel.ConfirmSelection();

            Assert.That(panel.StatusMessage, Does.Contain("摄政中"));
            Assert.That(panel.IsModalVisible, Is.False);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
        }

        [Test]
        public void PanelRegencyShowsRestrictedDecisionAndDisablesRestartActions()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.BeginRetirement();
            panel.SelectCandidate(new PersonId("person.akechi.tokihime"));
            panel.SelectRegent(new PersonId("person.akechi.chie"));
            panel.ConfirmSelection();

            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
            Assert.That(panel.StatusMessage, Does.Contain("受限决策"),
                "The regency HUD must state that decisions are restricted (GDD 4.3/19.2).");
            Assert.That(panel.StatusMessage, Does.Contain("退休并安排继承"),
                "The restricted-decision text must name the action that is actually blocked.");
            Assert.That(panel.StatusMessage, Does.Contain("成年时暂停"),
                "The restricted-decision text must stay truthful about the explicit adult handoff.");
            Assert.That(Find(AddSC010SuccessionPanel.RetireName).GetComponent<Button>().interactable,
                Is.False, "Retirement must not be reachable again during the regency.");
            Assert.That(Find(AddSC010SuccessionPanel.CrisisDemoName).GetComponent<Button>().interactable,
                Is.False, "The crisis demo must not be reachable during the regency.");
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.False,
                "The session must reject a second retirement while the regency is active.");
        }

        [Test]
        public void PanelAdultHandoffSurfacesPauseReasonAndRequiredAction()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.BeginRetirement();
            panel.SelectCandidate(new PersonId("person.akechi.tokihime"));
            panel.SelectRegent(new PersonId("person.akechi.chie"));
            panel.ConfirmSelection();

            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(800), Is.True);
            panel.Refresh();

            Assert.That(panel.IsModalVisible, Is.True,
                "The explicit handoff modal is the visible surface while time is paused.");
            Assert.That(panel.StatusMessage, Does.Contain("时间推进已暂停"),
                "The HUD must state that time is paused.");
            Assert.That(panel.ModalStatusMessage, Does.Contain("完成成年交权"),
                "The modal must name the action the player has to take, not imply an automatic handoff.");
            Assert.That(panel.ConfirmInteractable, Is.True);
        }

        [Test]
        public void PanelAdultConfirmTransfersHead()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.BeginRetirement();
            panel.SelectCandidate(new PersonId("person.akechi.masatsuna"));

            Assert.That(panel.ConfirmInteractable, Is.True);
            panel.ConfirmSelection();

            Assert.That(panel.HeadSummary, Does.Contain("明智 正綱"));
            Assert.That(panel.StatusMessage, Does.Contain("上次继承"));
            Assert.That(panel.StatusMessage, Does.Contain("明智 正綱"));
            Assert.That(panel.StatusMessage, Does.Not.Contain("succession."),
                "the status line must use the human-readable head label, not the raw result ID.");
            Assert.That(panel.IsModalVisible, Is.False);
            Assert.That(panel.RetireInteractable, Is.True,
                "A completed handoff must leave retirement available for a new, distinct cycle.");

            SuccessionResult next = _session.BeginSuccessionRetirement();

            Assert.That(next.Succeeded, Is.True, next.Message);
            Assert.That(_session.Succession.Phase,
                Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice),
                "The visible retirement button starts a new succession cycle instead of repeating the old handoff.");
        }

        [Test]
        public void PanelCrisisDemoShowsDecisionAndDeclineEndsFamily()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.StartCrisisDemo();
            panel.BeginRetirement();

            Assert.That(panel.CrisisActionsVisible, Is.True);
            Assert.That(panel.VisibleCandidateCount, Is.Zero);

            panel.DeclineCrisis();

            Assert.That(panel.StatusMessage, Does.Contain("家族灭亡"));
            Assert.That(panel.CrisisActionsVisible, Is.False);
        }

        [Test]
        public void PanelExtinctionStaysVisibleAsTerminalStatusWithoutSuccessionActions()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.StartCrisisDemo();
            panel.BeginRetirement();
            panel.DeclineCrisis();

            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            Assert.That(panel.IsTerminalExtinctionVisible, Is.True,
                "The extinction result must be a persistent terminal overlay, not only a HUD line.");
            Assert.That(panel.IsModalVisible, Is.True);
            Assert.That(panel.StatusMessage, Does.Contain("家族灭亡"));
            Assert.That(panel.ModalStatusMessage, Does.Contain("无法再推进时间"));
            Assert.That(panel.ModalStatusMessage, Does.Contain("读取较早的存档"),
                "The terminal text must point at the one remaining recovery path.");
            Assert.That(panel.VisibleCandidateCount, Is.Zero);
            Assert.That(panel.VisibleRegentCount, Is.Zero);
            Assert.That(panel.CrisisActionsVisible, Is.False);
            Assert.That(panel.ConfirmVisible, Is.False);
            Assert.That(panel.ConfirmInteractable, Is.False);
            Assert.That(panel.RetireInteractable, Is.False,
                "A new succession must not be startable after the family is extinct.");
            Assert.That(panel.CrisisDemoInteractable, Is.False);
            Assert.That(Find(AddSC010SuccessionPanel.RetireName).GetComponent<Button>().interactable,
                Is.False);
            Assert.That(Find(AddSC010SuccessionPanel.CrisisDemoName).GetComponent<Button>().interactable,
                Is.False);

            panel.Refresh();

            Assert.That(panel.IsTerminalExtinctionVisible, Is.True,
                "Refreshing must not clear the extinction overlay.");
            Assert.That(panel.ConfirmVisible, Is.False);
        }

        [Test]
        public void PanelExtinctionStatusUsesReadableTextWithoutTheRawResultId()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.StartCrisisDemo();
            panel.BeginRetirement();
            panel.DeclineCrisis();

            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            Assert.That(panel.StatusMessage, Does.Contain("家族灭亡"));
            Assert.That(panel.StatusMessage, Does.Contain("无符合资格的继承人"),
                "the decline path can leave living relatives, so the status must describe the missing heir");
            Assert.That(panel.StatusMessage, Does.Not.Contain("血脉"),
                "the status must not claim every family member died");
            Assert.That(panel.StatusMessage, Does.Not.Contain("extinction."),
                "the player-facing status must not render the raw completed-result id");
            Assert.That(panel.StatusMessage, Does.Not.Contain(_session.Succession.CompletedResultId),
                "the save-stable result id must stay internal");
            Assert.That(_session.Succession.CompletedResultId, Does.StartWith("extinction."),
                "presentation must not alter the save-stable extinction result");
        }

        [Test]
        public void StaticPanelButtonsAreWiredExactlyOnceAcrossRebinds()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();

            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(5));
            Assert.That(panel.StaticButtonsWired, Is.True);

            panel.enabled = false;
            panel.enabled = true;
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(5),
                "Disable/enable must not duplicate static listeners.");

            BindPanelFromSerialized(panel);
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(5),
                "A repeated BindReferences call must replace, not stack, static listeners.");
        }

        [Test]
        public void StaticRetireAndConfirmButtonsDriveAdultSuccession()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();

            Find(AddSC010SuccessionPanel.RetireName).GetComponent<Button>().onClick.Invoke();
            Assert.That(_session.Succession.Phase,
                Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(panel.IsModalVisible, Is.True);

            panel.SelectCandidate(new PersonId("person.akechi.masatsuna"));
            Find(AddSC010SuccessionPanel.ConfirmName).GetComponent<Button>().onClick.Invoke();

            Assert.That(panel.HeadSummary, Does.Contain("明智 正綱"));
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
        }

        [Test]
        public void StaticCrisisDemoRetireAcceptAndDeclineButtonsDriveCrisisFlow()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();

            Find(AddSC010SuccessionPanel.CrisisDemoName).GetComponent<Button>().onClick.Invoke();
            Find(AddSC010SuccessionPanel.RetireName).GetComponent<Button>().onClick.Invoke();
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));

            Find(AddSC010SuccessionPanel.AcceptCrisisName).GetComponent<Button>().onClick.Invoke();
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(panel.HeadSummary, Does.Contain("養子 宗次"));

            Assert.That(_session.StartCrisisDemo(), Is.False,
                "The pristine-state guard must refuse to replace completed family history.");
            Assert.That(_session.CanStartCrisisDemo, Is.False);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None),
                "The refused demo must preserve the completed succession state.");
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("養子 宗次"));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));
            Assert.That(_session.Succession.CrisisAdoptionUsed, Is.True);
            panel.Refresh();
            Assert.That(panel.CrisisDemoInteractable, Is.False,
                "The crisis demo button must be disabled after a completed succession.");

            _session.InitStandardDemo(33UL);
            panel.Refresh();
            Assert.That(_session.CanStartCrisisDemo, Is.True,
                "A fresh session must allow the crisis demo again.");

            Find(AddSC010SuccessionPanel.CrisisDemoName).GetComponent<Button>().onClick.Invoke();
            Find(AddSC010SuccessionPanel.RetireName).GetComponent<Button>().onClick.Invoke();
            Find(AddSC010SuccessionPanel.DeclineCrisisName).GetComponent<Button>().onClick.Invoke();

            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            Assert.That(panel.StatusMessage, Does.Contain("家族灭亡"));
        }

        [Test]
        public void StaticConfirmButtonCompletesPausedAdultRegencyHandoff()
        {
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();
            panel.BeginRetirement();
            panel.SelectCandidate(new PersonId("person.akechi.tokihime"));
            panel.SelectRegent(new PersonId("person.akechi.chie"));
            panel.ConfirmSelection();

            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(800), Is.True);
            panel.Refresh();

            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
            Assert.That(_session.Succession.IsRegencyHandoffDue(
                _session.WorldClock.CurrentDate), Is.True);
            Assert.That(panel.IsModalVisible, Is.True);
            Assert.That(panel.ConfirmInteractable, Is.True);
            Text label = Find(AddSC010SuccessionPanel.ConfirmName).GetComponentInChildren<Text>(true);
            Assert.That(label.text, Is.EqualTo("完成成年交权"));

            Find(AddSC010SuccessionPanel.ConfirmName).GetComponent<Button>().onClick.Invoke();

            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.Succession.RegencyCompleted, Is.True);
            Assert.That(panel.IsModalVisible, Is.False);
        }

        [Test]
        public void UpdaterPlacesRootAboveMapAndBelowTitleAndRejectsPriorOverlap()
        {
            Create("M2WorldMapRoot", _canvas);
            Transform title = Create("M6TitleRoot", _canvas).transform;

            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            Transform root = Find(AddSC010SuccessionPanel.RootName);
            Transform map = Find("M2WorldMapRoot");

            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(map.GetSiblingIndex()),
                "SuccessionRoot must render above the map/town HUD.");
            Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()),
                "SuccessionRoot must remain below the full-screen title overlay.");
            Assert.That(AddSC010SuccessionPanel.Validate(_scene, out string error), Is.True, error);

            root.SetAsFirstSibling();
            Assert.That(AddSC010SuccessionPanel.Validate(_scene, out string overlapError), Is.False);
            Assert.That(overlapError, Does.Contain("above the map"));
        }

        [Test]
        public void M2PanelBlocksEarlyRetirementAndShowsFixedHeir()
        {
            _session.InitNewGame(33UL);
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();

            panel.Refresh();

            Assert.That(panel.RetireInteractable, Is.False,
                "M2 retirement stays unavailable until the required first-generation choices exist");
            Assert.That(panel.StatusMessage, Does.Contain("固定继承人：明智 正綱"));
            Assert.That(panel.StatusMessage, Does.Contain("继承前的家议"));
            panel.BeginRetirement();
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 晴信"));
            Assert.That(panel.FeedbackMessage, Does.Contain("继承前的家议"));
        }

        [Test]
        public void M2PanelEnablesRetirementAfterRequiredChoicesAndTransfersTheFixedHeir()
        {
            _session.InitNewGame(33UL);
            Assert.That(AddSC010SuccessionPanel.Update(_scene),
                Is.EqualTo(AddSC010SuccessionPanel.UpdateResult.Success));
            SuccessionPanel panel = Find(AddSC010SuccessionPanel.RootName).GetComponent<SuccessionPanel>();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ChooseM2(M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ChooseM2(SampleEventIds.EventId, SampleEventIds.OfferRestOptionId);

            panel.Refresh();
            Assert.That(panel.RetireInteractable, Is.True);
            Assert.That(panel.StatusMessage, Does.Contain("主线条件已满足"));
            Assert.That(panel.StatusMessage, Does.Contain("固定继承人：明智 正綱"));

            Find(AddSC010SuccessionPanel.RetireName).GetComponent<Button>().onClick.Invoke();

            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));
            Assert.That(panel.StatusMessage, Does.Contain("固定继承人：明智 信正"));
        }

        private void ChooseM2(string eventId, string optionId)
        {
            Assert.That(_session.TryChooseEventOption(eventId, optionId,
                _session.CreateEventResultId(eventId, optionId)).Succeeded, Is.True);
        }

        private static void BindPanelFromSerialized(SuccessionPanel panel)
        {
            var serialized = new UnityEditor.SerializedObject(panel);
            panel.BindReferences(
                serialized.FindProperty("gameSession").objectReferenceValue as GameSession,
                serialized.FindProperty("modalRoot").objectReferenceValue as GameObject,
                serialized.FindProperty("headText").objectReferenceValue as Text,
                serialized.FindProperty("statusText").objectReferenceValue as Text,
                serialized.FindProperty("retireButton").objectReferenceValue as Button,
                serialized.FindProperty("crisisDemoButton").objectReferenceValue as Button,
                serialized.FindProperty("feedbackText").objectReferenceValue as Text,
                serialized.FindProperty("modalStatusText").objectReferenceValue as Text,
                serialized.FindProperty("candidateContent").objectReferenceValue as Transform,
                serialized.FindProperty("candidateButtonPrefab").objectReferenceValue as Button,
                serialized.FindProperty("regentContent").objectReferenceValue as Transform,
                serialized.FindProperty("regentButtonPrefab").objectReferenceValue as Button,
                serialized.FindProperty("confirmButton").objectReferenceValue as Button,
                serialized.FindProperty("acceptCrisisButton").objectReferenceValue as Button,
                serialized.FindProperty("declineCrisisButton").objectReferenceValue as Button);
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

        private int ObjectCount() => ObjectCount(_scene);

        private static int ObjectCount(Scene scene) => scene.GetRootGameObjects()
            .Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
    }
}
