using System;
using System.Linq;
using System.Reflection;
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
    /// SC-024 Task 5: pure doctrine projection, Chinese labels, panel bindings and the
    /// single ambition submission path through GameSession.
    /// </summary>
    public sealed class FamilyDoctrinePanelTests
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
            _session.gameObject.AddComponent<TitleScreenController>();
            _session.InitFamilyDoctrineDemoScenario(33UL);
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private FamilyDoctrinePanel Panel() =>
            Find(AddSC024FamilyDoctrine.OpenButtonName).GetComponent<FamilyDoctrinePanel>();

        [Test]
        public void View_ProjectsCreedHeadAndThreeChineseAmbitionChoices()
        {
            SuccessionState succession = SC024FamilyDoctrineScenario.CreateSuccession();
            FamilyDoctrineView view = FamilyDoctrineView.Build(succession,
                FamilyCreedKind.RighteousnessFirst, null, true, string.Empty);

            Assert.That(view.IsScenarioActive, Is.True);
            Assert.That(view.CreedId, Is.EqualTo(nameof(FamilyCreedKind.RighteousnessFirst)));
            Assert.That(view.CreedName, Is.EqualTo("义理至上"));
            Assert.That(view.CouncilName, Is.EqualTo("各房先开口"));
            Assert.That(view.CouncilEffect, Does.Contain("正綱").And.Contain("信正"));
            Assert.That(view.HeadId, Is.EqualTo(M2FixedScenario.HarunobuId));
            Assert.That(view.HeadDisplayName, Is.EqualTo("明智 晴信"));
            Assert.That(view.CanStartAmbition, Is.True);
            Assert.That(view.BlockReason, Is.Empty);
            Assert.That(view.HasCurrentAmbition, Is.False);

            Assert.That(view.Creeds.Select(option => option.Name),
                Is.EquivalentTo(new[] { "守秘为先", "义理至上", "唯才是举" }));
            Assert.That(view.Creeds.Single(option => option.IsCurrent).Kind,
                Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(view.Ambitions.Select(option => option.Name),
                Is.EqualTo(new[] { "发展家业", "传承家技", "培养继承人" }));
            Assert.That(view.Ambitions.Select(option => option.Target),
                Is.EqualTo(new[] { 3, 1, 5 }));
            Assert.That(view.Ambitions.Any(option => option.IsSelected), Is.False);
            Assert.That(view.AmbitionProgressLine, Does.Contain("尚未选定"));
        }

        [Test]
        public void View_MapsAllCreedEffectsAndRejectsUnavailableCommands()
        {
            SuccessionState succession = SC024FamilyDoctrineScenario.CreateSuccession();

            FamilyDoctrineView secrecy = FamilyDoctrineView.Build(succession,
                FamilyCreedKind.SecrecyFirst, null, true, string.Empty);
            Assert.That(secrecy.CreedName, Is.EqualTo("守秘为先"));
            Assert.That(secrecy.CouncilName, Is.EqualTo("小范围议事"));
            Assert.That(secrecy.CouncilEffect, Is.EqualTo("正綱关系 +5，信正关系 -5"));

            FamilyDoctrineView merit = FamilyDoctrineView.Build(succession,
                FamilyCreedKind.MeritFirst, null, true, string.Empty);
            Assert.That(merit.CreedName, Is.EqualTo("唯才是举"));
            Assert.That(merit.CouncilName, Is.EqualTo("按所长分工"));
            Assert.That(merit.CouncilEffect, Is.EqualTo("正綱关系 -5，信正关系 +5"));

            FamilyDoctrineView inactive = FamilyDoctrineView.Build(succession,
                FamilyCreedKind.RighteousnessFirst, null, false, "last");
            Assert.That(inactive.CanStartAmbition, Is.False);
            Assert.That(inactive.BlockReason, Does.Contain("SC-024"));

            FamilyAmbitionRecord record = new FamilyAmbitionRecord(M2FixedScenario.HarunobuId,
                FamilyAmbitionKind.EducateHeir, 2, FamilyDoctrineState.EducateHeirTarget,
                Array.Empty<string>());
            FamilyDoctrineView selected = FamilyDoctrineView.Build(succession,
                FamilyCreedKind.MeritFirst, record, true, "已选定");
            Assert.That(selected.HasCurrentAmbition, Is.True);
            Assert.That(selected.CanStartAmbition, Is.False);
            Assert.That(selected.BlockReason, Does.Contain("已选定"));
            FamilyAmbitionOption current = selected.Ambitions.Single(option => option.IsSelected);
            Assert.That(current.Name, Is.EqualTo("培养继承人"));
            Assert.That(current.Progress, Is.EqualTo(2));
            Assert.That(current.Target, Is.EqualTo(5));
            Assert.That(current.IsComplete, Is.False);
            Assert.That(current.ProgressLine, Does.Contain("2/5"));
            Assert.That(selected.AmbitionProgressLine, Does.Contain("培养继承人"));

            FamilyDoctrineView noHead = FamilyDoctrineView.Build(null,
                FamilyCreedKind.RighteousnessFirst, null, true, string.Empty);
            Assert.That(noHead.CanStartAmbition, Is.False);
            Assert.That(noHead.BlockReason, Does.Contain("家主"));

            FamilyDoctrineView noSession = FamilyDoctrineView.Build((GameSession)null);
            Assert.That(noSession.CanStartAmbition, Is.False);
            Assert.That(noSession.BlockReason, Does.Contain("GameSession"));
        }

        [Test]
        public void Panel_StartsClosedAndShowsChineseLabelsWhenOpened()
        {
            FamilyDoctrinePanel panel = Panel();
            Assert.That(panel.IsBound, Is.True);
            Assert.That(panel.IsOpen, Is.False);

            Find(AddSC024FamilyDoctrine.OpenButtonName).GetComponent<Button>().onClick.Invoke();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.TitleLine, Does.Contain("家训").And.Contain("抱负"));
            Assert.That(panel.CreedLine, Does.Contain("义理至上"));
            Assert.That(panel.CreedLine, Does.Contain(nameof(FamilyCreedKind.RighteousnessFirst)));
            Assert.That(panel.CreedChoicesLine, Does.Contain("守秘为先"));
            Assert.That(panel.CreedChoicesLine, Does.Contain("义理至上"));
            Assert.That(panel.CreedChoicesLine, Does.Contain("唯才是举"));
            Assert.That(panel.EffectLine, Does.Contain("各房先开口"));
            Assert.That(panel.EffectLine, Does.Contain("正綱"));
            Assert.That(panel.HeadLine, Does.Contain("明智 晴信"));
            Assert.That(panel.AmbitionLine, Does.Contain("发展家业"));
            Assert.That(panel.AmbitionLine, Does.Contain("0/3"));
            Assert.That(panel.AmbitionLabel(0), Does.Contain("发展家业"));
            Assert.That(panel.AmbitionLabel(1), Does.Contain("传承家技"));
            Assert.That(panel.AmbitionLabel(2), Does.Contain("培养继承人"));

            Find(AddSC024FamilyDoctrine.CloseButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void Panel_BlocksChangeWhenHeadAlreadyChoseAmbition()
        {
            FamilyDoctrinePanel panel = Panel();
            panel.Open();

            Assert.That(panel.BlockReasonVisible, Is.True);
            Assert.That(panel.BlockReasonLine, Does.Contain("已选定"));
            for (int i = 0; i < panel.AmbitionChoiceCountValue; i++)
                Assert.That(panel.AmbitionInteractable(i), Is.False);

            panel.ClickDevelopHousehold();

            Assert.That(panel.AmbitionCommitCount, Is.Zero);
            Assert.That(_session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.DevelopHousehold));
        }

        [Test]
        public void Panel_SubmitsExactlyOneAmbitionThroughTheSession()
        {
            SaveData data = _session.CaptureSave();
            data.familyDoctrine.ambitions = Array.Empty<FamilyAmbitionSaveEntry>();
            SaveOperationResult applied = _session.TryApplySave(data);
            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(_session.CurrentHeadAmbition, Is.Null);

            FamilyDoctrinePanel panel = Panel();
            panel.Open();
            Assert.That(panel.BlockReasonVisible, Is.False);
            Assert.That(panel.AmbitionInteractable(1), Is.True);

            panel.ClickPassDownTechnique();

            Assert.That(panel.AmbitionCommitCount, Is.EqualTo(1));
            Assert.That(_session.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(_session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.PassDownTechnique));
            Assert.That(panel.AmbitionLine, Does.Contain("传承家技"));
            Assert.That(panel.AmbitionLine, Does.Contain("0/1"));
            Assert.That(panel.BlockReasonLine, Does.Contain("已选定"));
            Assert.That(panel.AmbitionInteractable(0), Is.False);

            panel.ClickDevelopHousehold();
            Assert.That(panel.AmbitionCommitCount, Is.EqualTo(1),
                "a second click after a selected ambition must not submit again");
            Assert.That(_session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.PassDownTechnique));
        }

        [TestCase(0, FamilyAmbitionKind.DevelopHousehold, "发展家业", 3)]
        [TestCase(1, FamilyAmbitionKind.PassDownTechnique, "传承家技", 1)]
        [TestCase(2, FamilyAmbitionKind.EducateHeir, "培养继承人", 5)]
        public void Panel_EachAmbitionButtonSubmitsItsChoiceThroughOnClick(int index,
            FamilyAmbitionKind expectedKind, string expectedLabel, int expectedTarget)
        {
            ClearHeadAmbition();
            FamilyDoctrinePanel panel = Panel();
            panel.Open();
            Assert.That(panel.BlockReasonVisible, Is.False);
            Assert.That(panel.AmbitionButtonBindingCount, Is.EqualTo(3));
            Assert.That(panel.AmbitionButtonsWired, Is.True);
            Assert.That(panel.AmbitionInteractable(index), Is.True);

            Button button = AmbitionButton(index);
            button.onClick.Invoke();

            Assert.That(panel.AmbitionCommitCount, Is.EqualTo(1));
            Assert.That(_session.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(_session.CurrentHeadAmbition.Kind, Is.EqualTo(expectedKind));
            Assert.That(_session.CurrentHeadAmbition.Progress, Is.Zero);
            Assert.That(_session.CurrentHeadAmbition.Target, Is.EqualTo(expectedTarget));
            Assert.That(panel.AmbitionLine, Does.Contain(expectedLabel));
            Assert.That(panel.AmbitionLine, Does.Contain("0/" + expectedTarget));
            Assert.That(panel.BlockReasonLine, Does.Contain("已选定"));
            for (int i = 0; i < panel.AmbitionChoiceCountValue; i++)
                Assert.That(panel.AmbitionInteractable(i), Is.False);

            button.onClick.Invoke();
            Assert.That(panel.AmbitionCommitCount, Is.EqualTo(1),
                "a repeated onClick after the choice is selected must not submit again");
            Assert.That(_session.CurrentHeadAmbition.Kind, Is.EqualTo(expectedKind));
        }

        [Test]
        public void Panel_AmbitionButtonsStayWiredExactlyOnceAcrossRefreshDisableAndRebind()
        {
            FamilyDoctrinePanel panel = Panel();
            panel.Open();
            Assert.That(panel.AmbitionButtonBindingCount, Is.EqualTo(3));
            Assert.That(panel.AmbitionButtonsWired, Is.True);

            panel.Refresh();
            panel.Refresh();
            Assert.That(panel.AmbitionButtonBindingCount, Is.EqualTo(3),
                "refreshing must not add ambition listeners");

            panel.enabled = false;
            InvokeLifecycle(panel, "OnDisable");
            Assert.That(panel.AmbitionButtonBindingCount, Is.Zero,
                "disable must release every ambition listener");
            ClearHeadAmbition();
            AmbitionButton(0).onClick.Invoke();
            Assert.That(panel.AmbitionCommitCount, Is.Zero);
            Assert.That(_session.CurrentHeadAmbition, Is.Null,
                "a disabled panel must not submit through a stale listener");

            panel.enabled = true;
            InvokeLifecycle(panel, "OnEnable");
            Assert.That(panel.AmbitionButtonBindingCount, Is.EqualTo(3),
                "re-enable must restore each ambition listener exactly once");

            var serialized = new UnityEditor.SerializedObject(panel);
            panel.BindReferences(
                serialized.FindProperty("gameSession").objectReferenceValue as GameSession,
                serialized.FindProperty("panelRoot").objectReferenceValue as GameObject,
                serialized.FindProperty("openButton").objectReferenceValue as Button,
                serialized.FindProperty("closeButton").objectReferenceValue as Button,
                serialized.FindProperty("titleText").objectReferenceValue as Text,
                serialized.FindProperty("creedText").objectReferenceValue as Text,
                serialized.FindProperty("creedChoicesText").objectReferenceValue as Text,
                serialized.FindProperty("effectText").objectReferenceValue as Text,
                serialized.FindProperty("headText").objectReferenceValue as Text,
                serialized.FindProperty("ambitionText").objectReferenceValue as Text,
                serialized.FindProperty("blockReasonText").objectReferenceValue as Text,
                serialized.FindProperty("messageText").objectReferenceValue as Text,
                serialized.FindProperty("developHouseholdButton").objectReferenceValue as Button,
                serialized.FindProperty("passDownTechniqueButton").objectReferenceValue as Button,
                serialized.FindProperty("educateHeirButton").objectReferenceValue as Button);
            Assert.That(panel.AmbitionButtonBindingCount, Is.EqualTo(3),
                "a repeated BindReferences call must replace, not stack, ambition listeners");
            Assert.That(panel.AmbitionButtonsWired, Is.True);

            panel.Open();
            for (int i = 0; i < panel.AmbitionChoiceCountValue; i++)
            {
                ClearHeadAmbition();
                AmbitionButton(i).onClick.Invoke();
                Assert.That(_session.CurrentHeadAmbition, Is.Not.Null);
                Assert.That(_session.CurrentHeadAmbition.Kind,
                    Is.EqualTo(ExpectedAmbitionKind(i)));
            }
            Assert.That(panel.AmbitionCommitCount, Is.EqualTo(3),
                "each rebound button must submit its choice exactly once");
        }

        [Test]
        public void Panel_DoesNotAutoOpenWhenEnteringTheScenario()
        {
            FamilyDoctrinePanel panel = Panel();
            Assert.That(panel.IsOpen, Is.False);

            _session.InitStandardDemo();
            Assert.That(panel.IsOpen, Is.False);

            _session.InitFamilyDoctrineDemoScenario();
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void Panel_NewHeadDoesNotInheritPreviousHeadsAmbitionFeedback()
        {
            FamilyDoctrinePanel panel = Panel();
            panel.Open();
            Assert.That(panel.MessageLine, Does.Contain("家主已选定抱负"),
                "the opening head's own selection feedback must be visible first");

            AdvanceToFirstSuccession();

            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(_session.CurrentHeadAmbition, Is.Null,
                "a new head must start without an inherited ambition");

            panel.Open();
            Assert.That(panel.HeadLine, Does.Contain("明智 正綱"));
            Assert.That(panel.AmbitionLine, Does.Contain("尚未选定"));
            for (int i = 0; i < panel.AmbitionChoiceCountValue; i++)
                Assert.That(panel.AmbitionInteractable(i), Is.True);
            Assert.That(panel.MessageVisible, Is.False,
                "the previous head's selection feedback must not be shown under the new head");

            panel.ClickPassDownTechnique();

            Assert.That(panel.AmbitionCommitCount, Is.EqualTo(1));
            Assert.That(_session.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(_session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.PassDownTechnique));
            Assert.That(panel.AmbitionLine, Does.Contain("传承家技"));
            Assert.That(panel.AmbitionLine, Does.Contain("0/1"));
            Assert.That(panel.MessageVisible, Is.True,
                "the new head's own confirmation must be visible");
            Assert.That(panel.MessageLine, Does.Contain("传承家技"));
            for (int i = 0; i < panel.AmbitionChoiceCountValue; i++)
                Assert.That(panel.AmbitionInteractable(i), Is.False);
        }

        [Test]
        public void Panel_FirstOpenAfterSuccessionHidesPreviousHeadsFeedback()
        {
            FamilyDoctrinePanel panel = Panel();
            Assert.That(panel.IsOpen, Is.False);

            AdvanceToFirstSuccession();
            Assert.That(_session.CurrentHeadAmbition, Is.Null);

            panel.Open();
            Assert.That(panel.HeadLine, Does.Contain("明智 正綱"));
            Assert.That(panel.AmbitionLine, Does.Contain("尚未选定"));
            Assert.That(panel.AmbitionInteractable(0), Is.True);
            Assert.That(panel.MessageVisible, Is.False,
                "feedback produced for the previous head must stay hidden on the first open");
        }

        private void AdvanceToFirstSuccession()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId,
                Is.EqualTo(SC024EventIds.RighteousnessCouncilEventId));
            Choose(_session, SC024EventIds.RighteousnessKeepOptionId);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId),
                "the SC-024 route must not require the SC-013 inheritance pledge");
            Choose(_session, SampleEventIds.OfferRestOptionId);

            Assert.That(_session.CanBeginSuccessionRetirement, Is.True,
                _session.SuccessionRetirementBlockReason);
            SuccessionResult result = _session.BeginSuccessionRetirement();
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        private static void Choose(GameSession session, string optionId)
        {
            string pending = session.PendingEventId;
            Assert.That(pending, Is.Not.Empty);
            EventChoiceResult result = session.TryChooseEventOption(pending, optionId,
                session.CreateEventResultId(pending, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        private static readonly string[] AmbitionButtonNames =
        {
            AddSC024FamilyDoctrine.DevelopButtonName,
            AddSC024FamilyDoctrine.TechniqueButtonName,
            AddSC024FamilyDoctrine.EducateButtonName
        };

        private Button AmbitionButton(int index) =>
            Find(AmbitionButtonNames[index]).GetComponent<Button>();

        private void ClearHeadAmbition()
        {
            SaveData data = _session.CaptureSave();
            data.familyDoctrine.ambitions = Array.Empty<FamilyAmbitionSaveEntry>();
            SaveOperationResult applied = _session.TryApplySave(data);
            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(_session.CurrentHeadAmbition, Is.Null);
        }

        private static FamilyAmbitionKind ExpectedAmbitionKind(int index)
        {
            switch (index)
            {
                case 0: return FamilyAmbitionKind.DevelopHousehold;
                case 1: return FamilyAmbitionKind.PassDownTechnique;
                case 2: return FamilyAmbitionKind.EducateHeir;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static void InvokeLifecycle(FamilyDoctrinePanel panel, string methodName)
        {
            // Plain MonoBehaviours do not receive EditMode lifecycle dispatch, so invoke the
            // same callback Play Mode enable/disable runs.
            MethodInfo method = typeof(FamilyDoctrinePanel).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "FamilyDoctrinePanel." + methodName + " is missing");
            method.Invoke(panel, null);
        }

        private GameObject Create(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
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
    }
}
