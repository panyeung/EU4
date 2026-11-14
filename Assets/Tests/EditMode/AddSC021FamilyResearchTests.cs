using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 4: transactional scene generation, validation and title binding.</summary>
    public sealed class AddSC021FamilyResearchTests
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;

        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
        private GameSession _session;
        private TitleScreenController _titleController;
        private Transform _titleRoot;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas), typeof(CanvasScaler));
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.enabled = false;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            _canvas = canvas.transform;

            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitResearchDemoScenario(33UL);
            _titleController = _session.gameObject.AddComponent<TitleScreenController>();

            GameObject titleRoot = Create("M6TitleRoot", _canvas, typeof(Image));
            RectTransform titleRect = titleRoot.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            _titleRoot = titleRoot.transform;
            Text titleLabel = CreateText("M6TitleLabel", _titleRoot, "影之一族");
            SetCenteredRect(titleLabel.rectTransform, new Vector2(0f, 180f), new Vector2(900f, 70f));
            Button newGame = CreateButton("M6NewGameButton", _titleRoot, "新游戏");
            SetCenteredRect((RectTransform)newGame.transform, new Vector2(0f, 30f), new Vector2(260f, 56f));
            Button cont = CreateButton("M6ContinueButton", _titleRoot, "继续");
            SetCenteredRect((RectTransform)cont.transform, new Vector2(0f, -46f), new Vector2(260f, 56f));
            Text status = CreateText("M6TitleStatusText", _titleRoot, string.Empty);
            SetCenteredRect(status.rectTransform, new Vector2(0f, -330f), new Vector2(700f, 30f));
            _titleController.Bind(_session, null, null, titleRoot, newGame, cont, status);
            PrepareCanvasGeometry(canvasRect, scaler, titleRect);
            titleRoot.SetActive(false);
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
        public void Generator_CreatesOneRootPanelAndTitleEntry()
        {
            int before = ObjectCount();

            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.GreaterThan(before));

            Transform root = Find(AddSC021FamilyResearch.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.GetComponent<FamilyResearchPanel>(), Is.Not.Null);
            Assert.That(Find(AddSC021FamilyResearch.PanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC021FamilyResearch.ResearcherTemplateName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddSC021FamilyResearch.OpenButtonName).gameObject.activeSelf, Is.True);
            Assert.That(Find(AddSC021FamilyResearch.CostName).GetComponent<Text>().text,
                Is.EqualTo("每月消耗 1 资金、1 物资；完成后写入家族知识，不授予个人掌握。"),
                "the generated cost placeholder must match the current cost line");

            Transform titleButton = Find(AddSC021FamilyResearch.TitleButtonName);
            Assert.That(titleButton, Is.Not.Null);
            Assert.That(titleButton.parent, Is.EqualTo(_titleRoot));
            Assert.That(titleButton.GetComponentInChildren<Text>(true).text,
                Is.EqualTo("SC-021 家族研究示范"));
            Assert.That(_titleController.ResearchScenarioBound, Is.True);

            Assert.That(AddSC021FamilyResearch.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "the test seam must not save or reload a scene");

            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));
            Assert.That(AddSC021FamilyResearch.Validate(_scene, out error), Is.True, error);
        }

        [Test]
        public void Generator_PreflightRefusesWrongDirtyAndPlayingScene()
        {
            Assert.That(AddSC021FamilyResearch.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.SceneMismatch));
            Assert.That(AddSC021FamilyResearch.CheckScenePreflight(
                    AddSC021FamilyResearch.ScenePath, true),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.SceneDirty));
            Assert.That(AddSC021FamilyResearch.CheckScenePreflight(
                    AddSC021FamilyResearch.ScenePath, false, true),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.EditorPlaying));
        }

        [Test]
        public void Generator_RefusesExistingPartialControl()
        {
            Create(AddSC021FamilyResearch.RootName, _canvas);

            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.ExistingPartialControl));
            Assert.That(Find(AddSC021FamilyResearch.PanelName), Is.Null);
        }

        [Test]
        public void Generator_RollsBackOnValidationFailure()
        {
            AddSC021FamilyResearch.UpdateResult result = AddSC021FamilyResearch.Update(_scene,
                root => root.name = "SC021ResearchRootBroken");

            Assert.That(result, Is.EqualTo(AddSC021FamilyResearch.UpdateResult.ValidationFailed));
            Assert.That(Find(AddSC021FamilyResearch.RootName), Is.Null);
            Assert.That(Find(AddSC021FamilyResearch.PanelName), Is.Null);
            Assert.That(Find(AddSC021FamilyResearch.TitleButtonName), Is.Null,
                "the title entry must live in the same Undo transaction");
            Assert.That(_titleController.ResearchScenarioBound, Is.False);
        }

        [Test]
        public void Validator_RejectsMissingGridAndOffCanvasTemplate()
        {
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));

            Transform container = Find(AddSC021FamilyResearch.ResearcherContainerName);
            Object.DestroyImmediate(container.GetComponent<GridLayoutGroup>());
            Assert.That(AddSC021FamilyResearch.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("GridLayoutGroup"));

            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));
            Transform template = Find(AddSC021FamilyResearch.ResearcherTemplateName);
            template.GetComponent<RectTransform>().anchoredPosition = new Vector2(-900f, -900f);
            Assert.That(AddSC021FamilyResearch.Validate(_scene, out error), Is.False);
            Assert.That(error, Does.Contain("template"));
        }

        [Test]
        public void Validator_RejectsOpenButtonOverlappingTheSuccessionHud()
        {
            RectTransform hud = Create("SuccessionHud", _canvas, typeof(Image)).GetComponent<RectTransform>();
            hud.anchorMin = hud.anchorMax = new Vector2(0f, 1f);
            hud.pivot = new Vector2(0f, 1f);
            hud.anchoredPosition = new Vector2(20f, -20f);
            hud.sizeDelta = new Vector2(430f, 210f);
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));

            var open = (RectTransform)Find(AddSC021FamilyResearch.OpenButtonName);
            open.anchorMin = open.anchorMax = new Vector2(0f, 1f);
            open.pivot = new Vector2(0f, 1f);
            open.anchoredPosition = new Vector2(20f, -20f);
            Assert.That(AddSC021FamilyResearch.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("HUD").Or.Contain("overlaps"));
        }

        [Test]
        public void Validator_RejectsOpenButtonOverlappingTheSc020OpenButton()
        {
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));

            RectTransform sc020Open = (RectTransform)CreateButton(
                AddSC020CharacterEducation.OpenButtonName, _canvas, "子女培养").transform;
            var researchOpen = (RectTransform)Find(AddSC021FamilyResearch.OpenButtonName);
            sc020Open.anchorMin = researchOpen.anchorMin;
            sc020Open.anchorMax = researchOpen.anchorMax;
            sc020Open.pivot = researchOpen.pivot;
            sc020Open.anchoredPosition = researchOpen.anchoredPosition;
            sc020Open.sizeDelta = researchOpen.sizeDelta;

            Assert.That(AddSC021FamilyResearch.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("overlaps"));
        }

        [Test]
        public void TitleEntry_StartsTheFixedResearchScenario()
        {
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));

            Find(AddSC021FamilyResearch.TitleButtonName).GetComponent<Button>().onClick.Invoke();

            Assert.That(SC021FamilyResearchScenario.IsActive(_session.Succession), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 1, 1)));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(4),
                "opening funds cover two research months plus the M08B food bill");
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(2));
            Assert.That(_session.ResearchState.HasPlan, Is.False);
            Assert.That(_titleController.IsTitleVisible, Is.False);
        }

        [Test]
        public void Sc013AndSc020Validators_AcceptTheAdditionalResearchEntry()
        {
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));

            Assert.That(_titleRoot.childCount, Is.EqualTo(8));
            Assert.That(AddSC013TitleScenarioButtons.Validate(_scene, out string error), Is.True, error);
            Assert.That(AddSC020CharacterEducation.Validate(_scene, out error), Is.True, error);
            Assert.That(AddSC021FamilyResearch.Validate(_scene, out error), Is.True, error);
        }

        [Test]
        public void Updater_AcceptsMigratedSelectorLayoutEndToEnd()
        {
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));

            Transform selector = Create("M6ScenarioSelectRoot", _titleRoot).transform;
            RectTransform selectorRect = (RectTransform)selector;
            selectorRect.anchorMin = Vector2.zero;
            selectorRect.anchorMax = Vector2.one;
            selectorRect.offsetMin = Vector2.zero;
            selectorRect.offsetMax = Vector2.zero;
            Transform entry = Find(AddSC021FamilyResearch.TitleButtonName);
            entry.SetParent(selector, false);
            ((RectTransform)entry).anchoredPosition = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(selectorRect);

            int count = ObjectCount();
            Assert.That(AddSC021FamilyResearch.Update(_scene),
                Is.EqualTo(AddSC021FamilyResearch.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "A migrated rerun must not create a second flat title entry.");
            Assert.That(entry.parent, Is.EqualTo(selector));
            Assert.That(_titleRoot.Find(AddSC021FamilyResearch.TitleButtonName), Is.Null);
            Assert.That(AddSC021FamilyResearch.Validate(_scene, out string error), Is.True, error);
        }

        private Text CreateText(string name, Transform parent, string value)
        {
            GameObject go = Create(name, parent, typeof(Text));
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label)
        {
            GameObject go = Create(name, parent, typeof(Image), typeof(Button));
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            CreateText("Label", go.transform, label);
            return button;
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
                .FirstOrDefault(transform => transform.name == name);

        private int ObjectCount() =>
            _scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PrepareCanvasGeometry(RectTransform canvasRect, CanvasScaler scaler,
            RectTransform titleRoot)
        {
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = scaler.referenceResolution;
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            var corners = new Vector3[4];
            titleRoot.GetWorldCorners(corners);
            Vector3 min = canvasRect.InverseTransformPoint(corners[0]);
            Vector3 max = canvasRect.InverseTransformPoint(corners[2]);
            Assert.That(max.x - min.x, Is.EqualTo(ReferenceWidth).Within(0.5f));
            Assert.That(max.y - min.y, Is.EqualTo(ReferenceHeight).Within(0.5f));
        }
    }
}
