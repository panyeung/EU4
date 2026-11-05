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
    /// <summary>SC-020 Task 4: education panel values, exact previews and plan-only submission.</summary>
    public sealed class CharacterEducationPanelTests
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
            _session.InitEducationDemoScenario();
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private CharacterEducationPanel Panel() =>
            Find(AddSC020CharacterEducation.RootName).GetComponent<CharacterEducationPanel>();

        [Test]
        public void Panel_ShowsChildAgeValuesAndCurrentPlanState()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.OverviewLine, Does.Contain("青叶 千景"));
            Assert.That(panel.OverviewLine, Does.Contain("15 岁"));
            Assert.That(panel.OverviewLine, Does.Contain("忍术 15"));
            Assert.That(panel.OverviewLine, Does.Contain("学问 20"));
            Assert.That(panel.DirectionCount, Is.EqualTo(6));
            Assert.That(panel.MentorCount, Is.Zero, "free growth starts without a mentor list");
            Assert.That(panel.ConfirmInteractable, Is.True);
        }

        [Test]
        public void Panel_PreviewMatchesPlusSevenAndPlusNine()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();
            panel.SelectDirection(EducationDirection.Scholarship);

            Assert.That(panel.MentorCount, Is.EqualTo(2));
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.HeadId));
            Assert.That(panel.PreviewLine, Does.Contain("20 → 27"));

            panel.ClickMentor(1);
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.SpouseId));
            Assert.That(panel.PreviewLine, Does.Contain("20 → 29"));
            Assert.That(panel.PreviewLine, Does.Contain("共 +9"));
        }

        [Test]
        public void Panel_UpdateKeepsContainersFreeOfSerializedRuntimeClones()
        {
            Transform directions = Find(AddSC020CharacterEducation.DirectionContainerName);
            Transform mentors = Find(AddSC020CharacterEducation.MentorContainerName);

            Assert.That(directions.childCount, Is.EqualTo(1),
                "generation must serialize only the inactive direction template");
            Assert.That(directions.GetChild(0).name,
                Is.EqualTo(AddSC020CharacterEducation.DirectionTemplateName));
            Assert.That(directions.GetChild(0).gameObject.activeSelf, Is.False);
            Assert.That(mentors.childCount, Is.EqualTo(1),
                "generation must serialize only the inactive mentor template");
            Assert.That(mentors.GetChild(0).name,
                Is.EqualTo(AddSC020CharacterEducation.MentorTemplateName));
            Assert.That(mentors.GetChild(0).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void Panel_ClickingDirectionButtonUpdatesSelectionPreviewAndMentorChoices()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();
            Assert.That(panel.SelectedDirection, Is.EqualTo(EducationDirection.FreeGrowth));
            Assert.That(panel.MentorCount, Is.Zero);

            int scholarship = IndexOfDirection(panel, EducationDirection.Scholarship);
            Assert.That(scholarship, Is.GreaterThanOrEqualTo(0));
            panel.ClickDirection(scholarship);

            Assert.That(panel.SelectedDirection, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(panel.DirectionCount, Is.EqualTo(6), "rebuild must not duplicate buttons");
            Assert.That(panel.DirectionLabel(scholarship), Does.StartWith("▶"));
            Assert.That(panel.MentorCount, Is.EqualTo(2), "choosing a direction must offer mentors");
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.HeadId));
            Assert.That(panel.PreviewLine, Does.Contain("学问"));
            Assert.That(panel.PreviewLine, Does.Contain("20 → 27"));
            Assert.That(panel.ConfirmInteractable, Is.True);

            panel.ClickMentor(1);
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.SpouseId));
            Assert.That(panel.PreviewLine, Does.Contain("20 → 29"));
            Assert.That(panel.PreviewLine, Does.Contain("共 +9"));
        }

        [Test]
        public void Panel_OpenRemovesSerializedDeadDirectionClones()
        {
            CharacterEducationPanel panel = Panel();
            Transform directions = Find(AddSC020CharacterEducation.DirectionContainerName);
            Transform template = Find(AddSC020CharacterEducation.DirectionTemplateName);
            for (int i = 0; i < 6; i++)
            {
                GameObject stale = Object.Instantiate(template.gameObject, directions);
                stale.name = "SC020Direction_Stale" + i;
                stale.SetActive(true);
                stale.GetComponent<Button>().interactable = false;
            }

            panel.Open();

            Assert.That(panel.DirectionCount, Is.EqualTo(6));
            Assert.That(CountGeneratedButtons(directions, "SC020Direction_"), Is.EqualTo(6),
                "dead serialized clones must be purged before the live rebuild");
            Assert.That(directions.childCount, Is.EqualTo(7),
                "six live buttons plus the inactive template");
            for (int i = 0; i < directions.childCount; i++)
            {
                Transform child = directions.GetChild(i);
                if (!child.name.StartsWith("SC020Direction_", System.StringComparison.Ordinal))
                    continue;
                Button button = child.GetComponent<Button>();
                Assert.That(button, Is.Not.Null);
                Assert.That(button.interactable, Is.True);
                Assert.That(button.targetGraphic, Is.Not.Null);
                Assert.That(button.targetGraphic.raycastTarget, Is.True);
            }

            int scholarship = IndexOfDirection(panel, EducationDirection.Scholarship);
            panel.ClickDirection(scholarship);
            Assert.That(panel.SelectedDirection, Is.EqualTo(EducationDirection.Scholarship),
                "the click must reach the live button, not a dead serialized clone");
            Assert.That(panel.PreviewLine, Does.Contain("20 → 27"));
        }

        [Test]
        public void Panel_ConfirmStoresPlanWithoutImmediateGrowthOrChronicle()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();
            panel.SelectDirection(EducationDirection.Scholarship);
            panel.ClickMentor(1);
            panel.ClickConfirm();

            Assert.That(_session.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(plan.Mentor, Is.EqualTo(SC020EducationScenario.SpouseId));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(_session.ChronicleEntries.Count, Is.Zero);
            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain("已提交"));
            Assert.That(panel.OverviewLine, Does.Contain("本年计划：学问"));
        }

        [Test]
        public void Panel_CloseAndReopenKeepsThePendingPlan()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();
            panel.SelectDirection(EducationDirection.Scholarship);
            panel.ClickMentor(1);
            panel.ClickConfirm();
            panel.Close();
            Assert.That(panel.IsOpen, Is.False);

            panel.Open();
            Assert.That(panel.SelectedDirection, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.SpouseId));
            Assert.That(panel.OverviewLine, Does.Contain("本年计划：学问"));
        }

        [Test]
        public void Panel_ConfirmButtonClickSubmitsPlanShowsFeedbackAndKeepsAbilities()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();
            panel.ClickDirection(IndexOfDirection(panel, EducationDirection.Scholarship));
            int spouse = IndexOfMentor(panel, "青叶 小夜");
            Assert.That(spouse, Is.GreaterThanOrEqualTo(0), "青叶 小夜 must be offered as a mentor");
            panel.ClickMentor(spouse);
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.SpouseId));

            Button confirm = ConfirmButton();
            Assert.That(confirm, Is.Not.Null, "the panel must expose the confirm button");
            Assert.That(confirm.interactable, Is.True);
            confirm.onClick.Invoke();

            Assert.That(_session.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(plan.Mentor, Is.EqualTo(SC020EducationScenario.SpouseId));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20),
                "submitting the plan must not grant the annual reward before settlement");
            Assert.That(_session.ChronicleEntries.Count, Is.Zero);

            Assert.That(panel.MessageVisible, Is.True);
            Assert.That(panel.MessageLine, Does.Contain("已提交"));
            Assert.That(panel.MessageLine, Does.Contain("学问"));
            Assert.That(panel.MessageLine, Does.Contain("青叶 小夜"));
            Assert.That(panel.OverviewLine, Does.Contain("本年计划：学问"));
            Assert.That(panel.OverviewLine, Does.Contain("青叶 小夜"));
            Assert.That(panel.OverviewLine, Does.Not.Contain("尚未提交"));
            Assert.That(panel.ConfirmInteractable, Is.True,
                "a pending plan stays editable until the year settles");
        }

        [Test]
        public void Panel_ConfirmButtonClickUpdatesPendingPlanAndSurvivesReopen()
        {
            CharacterEducationPanel panel = Panel();
            panel.Open();
            panel.ClickDirection(IndexOfDirection(panel, EducationDirection.Scholarship));
            panel.ClickMentor(IndexOfMentor(panel, "青叶 小夜"));
            ConfirmButton().onClick.Invoke();

            panel.ClickDirection(IndexOfDirection(panel, EducationDirection.MartialArts));
            panel.ClickMentor(IndexOfMentor(panel, "青叶 宗一郎"));
            ConfirmButton().onClick.Invoke();

            Assert.That(_session.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(EducationDirection.MartialArts));
            Assert.That(plan.Mentor, Is.EqualTo(SC020EducationScenario.HeadId));
            Assert.That(_session.Development.PlannedAgeYears(SC020EducationScenario.ChildId).Count,
                Is.EqualTo(1), "re-submitting must replace the single pending plan");
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(values.MartialArts, Is.EqualTo(12));
            Assert.That(_session.ChronicleEntries.Count, Is.Zero);

            panel.Close();
            panel.Open();

            Assert.That(panel.SelectedDirection, Is.EqualTo(EducationDirection.MartialArts));
            Assert.That(panel.SelectedMentor, Is.EqualTo(SC020EducationScenario.HeadId));
            Assert.That(panel.OverviewLine, Does.Contain("本年计划：武艺"));
            Assert.That(panel.OverviewLine, Does.Contain("青叶 宗一郎"));
        }

        [Test]
        public void Panel_AtSixteenDisablesEducationAndMentorActions()
        {
            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(366), Is.True);
            CharacterEducationPanel panel = Panel();
            panel.Open();

            Assert.That(panel.OverviewLine, Does.Contain("已成年"));
            Assert.That(panel.ConfirmInteractable, Is.False);
            Assert.That(panel.MentorCount, Is.Zero);
            Assert.That(panel.PreviewLine, Does.Contain("培养已结束").Or.Contain("未成年"));
        }

        [Test]
        public void Panel_WithoutBindingsRefreshesSafely()
        {
            GameObject loose = Create("LooseEducationPanel", null,
                typeof(CharacterEducationPanel));
            CharacterEducationPanel panel = loose.GetComponent<CharacterEducationPanel>();

            Assert.That(panel.IsBound, Is.False);
            Assert.DoesNotThrow(() => panel.Refresh());
            Assert.DoesNotThrow(() => panel.Open());
            Assert.DoesNotThrow(() => panel.ClickConfirm());
            Assert.That(panel.IsOpen, Is.False);
        }

        private Button ConfirmButton() =>
            Find(AddSC020CharacterEducation.ConfirmButtonName).GetComponent<Button>();

        private static int IndexOfMentor(CharacterEducationPanel panel, string name)
        {
            for (int i = 0; i < panel.MentorCount; i++)
                if (panel.MentorLabel(i).Contains(name))
                    return i;
            return -1;
        }

        private static int IndexOfDirection(CharacterEducationPanel panel, EducationDirection direction)
        {
            string label = CharacterGrowthRules.DirectionName(direction);
            for (int i = 0; i < panel.DirectionCount; i++)
                if (panel.DirectionLabel(i).Contains(label))
                    return i;
            return -1;
        }

        private static int CountGeneratedButtons(Transform container, string prefix)
        {
            int count = 0;
            for (int i = 0; i < container.childCount; i++)
                if (container.GetChild(i).name.StartsWith(prefix, System.StringComparison.Ordinal))
                    count++;
            return count;
        }

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
    }
}
