using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-022 Task 3: panel open/close, filtering, confirmation and readable rejection.</summary>
    public sealed class FamilyFormationPanelTests
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
            _session.InitFamilyFormationDemoScenario();
            Assert.That(AddSC022FamilyFormation.Update(_scene),
                Is.EqualTo(AddSC022FamilyFormation.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private FamilyFormationPanel Panel() =>
            Find(AddSC022FamilyFormation.RootName).GetComponent<FamilyFormationPanel>();

        [Test]
        public void Panel_StartsClosedAndOpenCloseButtonsDriveIt()
        {
            FamilyFormationPanel panel = Panel();
            Assert.That(panel.IsOpen, Is.False);

            Find(AddSC022FamilyFormation.OpenButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.TitleLine, Does.Contain("婚姻"));
            Assert.That(panel.TitleLine, Does.Contain("收养"));

            Find(AddSC022FamilyFormation.CloseButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void Panel_DoesNotAutoOpenWhenEnteringTheScenario()
        {
            FamilyFormationPanel panel = Panel();
            Assert.That(panel.IsOpen, Is.False);

            _session.InitStandardDemo();
            Assert.That(panel.IsOpen, Is.False);

            _session.InitFamilyFormationDemoScenario();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void Panel_ShowsAdultMembersAndFiltersMarriageCandidatesByDirectLine()
        {
            FamilyFormationPanel panel = Panel();
            panel.Open();

            Assert.That(panel.MarriageMemberCount, Is.EqualTo(3));
            Assert.That(panel.AdoptionMemberCount, Is.EqualTo(3));
            int headIndex = IndexOf(panel.MarriageMemberLabel, 3,
                SC022FamilyFormationScenario.HeadName);
            panel.ClickMarriageMember(headIndex);

            Assert.That(panel.MarriageMemberInfoLine,
                Does.Contain(SC022FamilyFormationScenario.HeadName));
            Assert.That(panel.MarriageMemberInfoLine,
                Does.Contain(SC022FamilyFormationScenario.SpouseName));
            Assert.That(panel.MarriageCandidateCount, Is.EqualTo(3));
            Assert.That(Labels(panel.MarriageCandidateLabel, 3)
                .Any(label => label.Contains(SC022FamilyFormationScenario.MarriageCandidateOneName)),
                Is.True);
            Assert.That(Labels(panel.MarriageCandidateLabel, 3)
                .Any(label => label.Contains(SC022FamilyFormationScenario.MarriageCandidateTwoName)),
                Is.True);
            Assert.That(Labels(panel.MarriageCandidateLabel, 3)
                .Any(label => label.Contains(SC022FamilyFormationScenario.SiblingCandidateName)),
                Is.True);
            Assert.That(Labels(panel.MarriageCandidateLabel, 3)
                .Any(label => label.Contains(SC022FamilyFormationScenario.BloodAncestorName)),
                Is.False);
            Assert.That(Labels(panel.MarriageCandidateLabel, 3)
                .Any(label => label.Contains(SC022FamilyFormationScenario.AdoptedChildCandidateName)),
                Is.False);
        }

        [Test]
        public void Panel_MarriageConfirmationCommitsExactlyOnceAndRefreshes()
        {
            FamilyFormationPanel panel = Panel();
            panel.Open();
            int headIndex = IndexOf(panel.MarriageMemberLabel, 3,
                SC022FamilyFormationScenario.HeadName);
            panel.ClickMarriageMember(headIndex);
            int candidateIndex = IndexOf(panel.MarriageCandidateLabel, 3,
                SC022FamilyFormationScenario.MarriageCandidateOneName);
            panel.ClickMarriageCandidate(candidateIndex);
            Assert.That(panel.MarriageConfirmInteractable, Is.True);
            int refreshes = panel.RefreshCount;

            panel.ConfirmMarriage();

            Assert.That(panel.MarriageCommitCount, Is.EqualTo(1));
            Assert.That(panel.RefreshCount, Is.GreaterThan(refreshes));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.MarriageCandidateOneId, out FamilyMemberRole role),
                Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(panel.MarriageCandidateCount, Is.EqualTo(2));
            Assert.That(panel.MarriageMessageVisible, Is.True);
            Assert.That(panel.MarriageMessageLine, Does.Contain("已登记婚姻"));

            panel.ConfirmMarriage();
            Assert.That(panel.MarriageCommitCount, Is.EqualTo(1),
                "a second click after success must not submit another command");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
        }

        [Test]
        public void Panel_AdoptionConfirmationAddsAdoptedChildAndKeepsSuccession()
        {
            SuccessionPhase phase = _session.Succession.Phase;
            PersonId head = _session.Succession.HeadPersonId;
            FamilyFormationPanel panel = Panel();
            panel.Open();
            int headIndex = IndexOf(panel.AdoptionMemberLabel, 3,
                SC022FamilyFormationScenario.HeadName);
            panel.ClickAdoptionMember(headIndex);

            Assert.That(panel.AdoptionCandidateCount, Is.EqualTo(5));
            Assert.That(Labels(panel.AdoptionCandidateLabel, 5)
                .Any(label => label.Contains(SC022FamilyFormationScenario.ChildAdoptionCandidateName)),
                Is.True);
            Assert.That(Labels(panel.AdoptionCandidateLabel, 5)
                .Any(label => label.Contains(SC022FamilyFormationScenario.AdultAdoptionCandidateName)),
                Is.True);

            int candidateIndex = IndexOf(panel.AdoptionCandidateLabel, 5,
                SC022FamilyFormationScenario.ChildAdoptionCandidateName);
            panel.ClickAdoptionCandidate(candidateIndex);
            Assert.That(panel.AdoptionConfirmInteractable, Is.True);

            panel.ConfirmAdoption();

            Assert.That(panel.AdoptionCommitCount, Is.EqualTo(1));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.ChildAdoptionCandidateId,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.AdoptedChild));
            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(panel.AdoptionMessageVisible, Is.True);
            Assert.That(panel.AdoptionMessageLine, Does.Contain("已办理收养"));
        }

        [Test]
        public void Panel_MarriageSceneButtonSubmitsExactlyOneCommandPerClick()
        {
            FamilyFormationPanel panel = Panel();
            SceneButton(AddSC022FamilyFormation.OpenButtonName).onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True);

            SceneButton(FamilyFormationPanel.MarriageMemberPrefix +
                        SC022FamilyFormationScenario.HeadIdValue).onClick.Invoke();
            Assert.That(panel.SelectedMarriageMember,
                Is.EqualTo(SC022FamilyFormationScenario.HeadId));
            SceneButton(FamilyFormationPanel.MarriageCandidatePrefix +
                        SC022FamilyFormationScenario.MarriageCandidateOneIdValue)
                .onClick.Invoke();
            Assert.That(panel.SelectedMarriageCandidate,
                Is.EqualTo(SC022FamilyFormationScenario.MarriageCandidateOneId));

            Button confirm = SceneButton(AddSC022FamilyFormation.MarriageConfirmButtonName);
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();

            Assert.That(panel.MarriageCommitCount, Is.EqualTo(1));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.MarriageCandidateOneId, out FamilyMemberRole role),
                Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(panel.MarriageMessageVisible, Is.True);
            Assert.That(panel.MarriageMessageLine, Does.Contain("已登记婚姻"));

            confirm.onClick.Invoke();
            Assert.That(panel.MarriageCommitCount, Is.EqualTo(1),
                "the scene button must not replay a confirmed selection");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));

            // A disable/enable cycle must release the click handler, then bind it exactly once.
            SceneButton(FamilyFormationPanel.MarriageCandidatePrefix +
                        SC022FamilyFormationScenario.MarriageCandidateTwoIdValue)
                .onClick.Invoke();
            Assert.That(panel.SelectedMarriageCandidate,
                Is.EqualTo(SC022FamilyFormationScenario.MarriageCandidateTwoId));
            panel.enabled = false;
            InvokeLifecycle(panel, "OnDisable");
            confirm.onClick.Invoke();
            Assert.That(panel.MarriageCommitCount, Is.EqualTo(1),
                "a disabled panel must not submit through a stale scene-button listener");

            panel.enabled = true;
            InvokeLifecycle(panel, "OnEnable");
            panel.Open();
            Assert.That(panel.SelectedMarriageCandidate,
                Is.EqualTo(SC022FamilyFormationScenario.MarriageCandidateTwoId));
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();

            Assert.That(panel.MarriageCommitCount, Is.EqualTo(2));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.MarriageCandidateTwoId, out role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(2));
        }

        [Test]
        public void Panel_AdoptionSceneButtonSubmitsExactlyOneCommandAndKeepsSuccession()
        {
            SuccessionPhase phase = _session.Succession.Phase;
            PersonId head = _session.Succession.HeadPersonId;
            FamilyFormationPanel panel = Panel();
            SceneButton(AddSC022FamilyFormation.OpenButtonName).onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True);

            SceneButton(FamilyFormationPanel.AdoptionMemberPrefix +
                        SC022FamilyFormationScenario.HeadIdValue).onClick.Invoke();
            Assert.That(panel.SelectedAdoptionMember,
                Is.EqualTo(SC022FamilyFormationScenario.HeadId));
            SceneButton(FamilyFormationPanel.AdoptionCandidatePrefix +
                        SC022FamilyFormationScenario.ChildAdoptionCandidateIdValue)
                .onClick.Invoke();
            Assert.That(panel.SelectedAdoptionCandidate,
                Is.EqualTo(SC022FamilyFormationScenario.ChildAdoptionCandidateId));

            Button confirm = SceneButton(AddSC022FamilyFormation.AdoptionConfirmButtonName);
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();

            Assert.That(panel.AdoptionCommitCount, Is.EqualTo(1));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.ChildAdoptionCandidateId,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.AdoptedChild));
            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(panel.AdoptionMessageVisible, Is.True);
            Assert.That(panel.AdoptionMessageLine, Does.Contain("已办理收养"));

            confirm.onClick.Invoke();
            Assert.That(panel.AdoptionCommitCount, Is.EqualTo(1),
                "the scene button must not replay a confirmed selection");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));

            // A disable/enable cycle must release the click handler, then bind it exactly once.
            SceneButton(FamilyFormationPanel.AdoptionCandidatePrefix +
                        SC022FamilyFormationScenario.AdultAdoptionCandidateIdValue)
                .onClick.Invoke();
            Assert.That(panel.SelectedAdoptionCandidate,
                Is.EqualTo(SC022FamilyFormationScenario.AdultAdoptionCandidateId));
            panel.enabled = false;
            InvokeLifecycle(panel, "OnDisable");
            confirm.onClick.Invoke();
            Assert.That(panel.AdoptionCommitCount, Is.EqualTo(1),
                "a disabled panel must not submit through a stale scene-button listener");

            panel.enabled = true;
            InvokeLifecycle(panel, "OnEnable");
            panel.Open();
            Assert.That(panel.SelectedAdoptionCandidate,
                Is.EqualTo(SC022FamilyFormationScenario.AdultAdoptionCandidateId));
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();

            Assert.That(panel.AdoptionCommitCount, Is.EqualTo(2));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.AdultAdoptionCandidateId, out role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.AdoptedChild));
            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(2));
        }

        [Test]
        public void Panel_ConfirmSceneButtonsWithoutASelectionDoNotSubmit()
        {
            FamilyFormationPanel panel = Panel();
            SceneButton(AddSC022FamilyFormation.OpenButtonName).onClick.Invoke();

            // Both buttons are non-interactable here; a programmatic click must still no-op.
            SceneButton(AddSC022FamilyFormation.MarriageConfirmButtonName).onClick.Invoke();
            SceneButton(AddSC022FamilyFormation.AdoptionConfirmButtonName).onClick.Invoke();

            Assert.That(panel.MarriageCommitCount, Is.Zero);
            Assert.That(panel.AdoptionCommitCount, Is.Zero);
            Assert.That(_session.ChronicleEntries, Is.Empty);
        }

        [Test]
        public void Panel_ClosingWithoutConfirmationDoesNotSubmit()
        {
            FamilyFormationPanel panel = Panel();
            panel.Open();
            int headIndex = IndexOf(panel.MarriageMemberLabel, 3,
                SC022FamilyFormationScenario.HeadName);
            panel.ClickMarriageMember(headIndex);
            int candidateIndex = IndexOf(panel.MarriageCandidateLabel, 3,
                SC022FamilyFormationScenario.MarriageCandidateOneName);
            panel.ClickMarriageCandidate(candidateIndex);

            panel.Close();

            Assert.That(panel.MarriageCommitCount, Is.Zero);
            Assert.That(_session.ChronicleEntries, Is.Empty);
            Assert.That(_session.Succession.World.Family.IsMember(
                SC022FamilyFormationScenario.MarriageCandidateOneId), Is.False);
        }

        [Test]
        public void Panel_RejectedConfirmationShowsSpecificReasonAndLeavesStateUnchanged()
        {
            FamilyFormationPanel panel = Panel();
            panel.Open();
            int headIndex = IndexOf(panel.MarriageMemberLabel, 3,
                SC022FamilyFormationScenario.HeadName);
            panel.ClickMarriageMember(headIndex);
            int candidateIndex = IndexOf(panel.MarriageCandidateLabel, 3,
                SC022FamilyFormationScenario.MarriageCandidateOneName);
            panel.ClickMarriageCandidate(candidateIndex);

            // Invalidate the already-built candidate button without publishing a refresh.
            Assert.That(_session.Succession.World.TryRemovePerson(
                SC022FamilyFormationScenario.MarriageCandidateOneId), Is.True);
            int refreshes = panel.RefreshCount;

            panel.ConfirmMarriage();

            Assert.That(panel.MarriageCommitCount, Is.EqualTo(1));
            Assert.That(panel.RefreshCount, Is.GreaterThan(refreshes));
            Assert.That(panel.MarriageMessageVisible, Is.True);
            Assert.That(panel.MarriageMessageLine, Does.Contain("已登记人物"));
            Assert.That(_session.ChronicleEntries, Is.Empty);
            Assert.That(_session.Succession.World.Family.IsMember(
                SC022FamilyFormationScenario.MarriageCandidateOneId), Is.False);
        }

        [Test]
        public void Panel_ShowsBlockReasonAndDisablesConfirmationDuringAPendingEvent()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Not.Empty);

            FamilyFormationPanel panel = Panel();
            panel.Open();

            Assert.That(panel.BlockReasonVisible, Is.True);
            Assert.That(panel.BlockReasonLine, Does.Contain("事件"));
            int headIndex = IndexOf(panel.MarriageMemberLabel, 3,
                SC022FamilyFormationScenario.HeadName);
            panel.ClickMarriageMember(headIndex);
            int candidateIndex = IndexOf(panel.MarriageCandidateLabel, 3,
                SC022FamilyFormationScenario.MarriageCandidateOneName);
            panel.ClickMarriageCandidate(candidateIndex);
            Assert.That(panel.MarriageConfirmInteractable, Is.False);
        }

        private string[] Labels(System.Func<int, string> label, int count)
        {
            var labels = new string[count];
            for (int i = 0; i < count; i++)
                labels[i] = label(i);
            return labels;
        }

        private static int IndexOf(System.Func<int, string> label, int count, string text)
        {
            for (int i = 0; i < count; i++)
                if (label(i).Contains(text))
                    return i;
            Assert.Fail("label not found: " + text);
            return -1;
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

        private Button SceneButton(string name)
        {
            Transform transform = Find(name);
            Assert.That(transform, Is.Not.Null, name + " must exist in the scene");
            Button button = transform.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, name + " must expose a Button");
            return button;
        }

        private static void InvokeLifecycle(FamilyFormationPanel panel, string methodName)
        {
            System.Reflection.MethodInfo method = typeof(FamilyFormationPanel).GetMethod(
                methodName, System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing lifecycle method: " + methodName);
            method.Invoke(panel, null);
        }
    }
}
