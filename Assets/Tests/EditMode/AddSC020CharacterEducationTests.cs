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
    /// <summary>SC-020 Task 4: transactional scene generation, validation and title binding.</summary>
    public sealed class AddSC020CharacterEducationTests
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
            _session.InitNewGame(33UL);
            _titleController = _session.gameObject.AddComponent<TitleScreenController>();

            GameObject titleRoot = Create("M6TitleRoot", _canvas, typeof(Image));
            RectTransform titleRect = titleRoot.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            _titleRoot = titleRoot.transform;
            Text titleLabel = CreateText("M6TitleLabel", _titleRoot, "战国立志传");
            SetCenteredRect(titleLabel.rectTransform, new Vector2(0f, 180f), new Vector2(900f, 70f));
            Button newGame = CreateButton("M6NewGameButton", _titleRoot, "新游戏");
            SetCenteredRect((RectTransform)newGame.transform, new Vector2(0f, 30f), new Vector2(260f, 56f));
            Button cont = CreateButton("M6ContinueButton", _titleRoot, "继续");
            SetCenteredRect((RectTransform)cont.transform, new Vector2(0f, -46f), new Vector2(260f, 56f));
            Text status = CreateText("M6TitleStatusText", _titleRoot, string.Empty);
            SetCenteredRect(status.rectTransform, new Vector2(0f, -120f), new Vector2(700f, 30f));
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

            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.GreaterThan(before));

            Transform root = Find(AddSC020CharacterEducation.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.GetComponent<CharacterEducationPanel>(), Is.Not.Null);
            Assert.That(Find(AddSC020CharacterEducation.PanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC020CharacterEducation.DirectionTemplateName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddSC020CharacterEducation.MentorTemplateName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddSC020CharacterEducation.OpenButtonName).gameObject.activeSelf, Is.True);

            Transform titleButton = Find(AddSC020CharacterEducation.TitleButtonName);
            Assert.That(titleButton, Is.Not.Null);
            Assert.That(titleButton.parent, Is.EqualTo(_titleRoot));
            Assert.That(titleButton.GetComponentInChildren<Text>(true).text,
                Is.EqualTo("SC-020 子女培养示范"));
            Assert.That(_titleController.EducationScenarioBound, Is.True);
            Assert.That(Find("M6TitleStatusText").GetComponent<RectTransform>().anchoredPosition.y,
                Is.EqualTo(-330f));

            Assert.That(AddSC020CharacterEducation.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "the test seam must not save or reload a scene");

            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
            Assert.That(AddSC020CharacterEducation.Validate(_scene, out error), Is.True, error);
        }

        [Test]
        public void Generator_PreflightRefusesWrongDirtyAndPlayingScene()
        {
            Assert.That(AddSC020CharacterEducation.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.SceneMismatch));
            Assert.That(AddSC020CharacterEducation.CheckScenePreflight(
                    AddSC020CharacterEducation.ScenePath, true),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.SceneDirty));
            Assert.That(AddSC020CharacterEducation.CheckScenePreflight(
                    AddSC020CharacterEducation.ScenePath, false, true),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.EditorPlaying));
        }

        [Test]
        public void Generator_RefusesExistingPartialControl()
        {
            Create(AddSC020CharacterEducation.RootName, _canvas);

            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.ExistingPartialControl));
            Assert.That(Find(AddSC020CharacterEducation.PanelName), Is.Null);
        }

        [Test]
        public void Generator_RollsBackOnValidationFailure()
        {
            AddSC020CharacterEducation.UpdateResult result = AddSC020CharacterEducation.Update(_scene,
                root => root.name = "SC020EducationRootBroken");

            Assert.That(result, Is.EqualTo(AddSC020CharacterEducation.UpdateResult.ValidationFailed));
            Assert.That(Find(AddSC020CharacterEducation.RootName), Is.Null);
            Assert.That(Find(AddSC020CharacterEducation.PanelName), Is.Null);
            Assert.That(Find(AddSC020CharacterEducation.TitleButtonName), Is.Null,
                "the title entry must live in the same Undo transaction");
            Assert.That(_titleController.EducationScenarioBound, Is.False);
        }

        [Test]
        public void Validator_RejectsMissingGridAndOffCanvasTemplate()
        {
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));

            Transform directions = Find(AddSC020CharacterEducation.DirectionContainerName);
            Object.DestroyImmediate(directions.GetComponent<GridLayoutGroup>());
            Assert.That(AddSC020CharacterEducation.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("GridLayoutGroup"));

            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
            Transform template = Find(AddSC020CharacterEducation.DirectionTemplateName);
            template.GetComponent<RectTransform>().anchoredPosition = new Vector2(-900f, -900f);
            Assert.That(AddSC020CharacterEducation.Validate(_scene, out error), Is.False);
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
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));

            var open = (RectTransform)Find(AddSC020CharacterEducation.OpenButtonName);
            open.anchorMin = open.anchorMax = new Vector2(0f, 1f);
            open.pivot = new Vector2(0f, 1f);
            open.anchoredPosition = new Vector2(20f, -20f);
            Assert.That(AddSC020CharacterEducation.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("HUD").Or.Contain("overlaps"));
        }

        [Test]
        public void TitleEntry_StartsTheFixedEducationScenario()
        {
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));

            Find(AddSC020CharacterEducation.TitleButtonName).GetComponent<Button>().onClick.Invoke();

            Assert.That(SC020EducationScenario.IsActive(_session.Succession), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 1, 1)));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(_titleController.IsTitleVisible, Is.False);
        }

        [Test]
        public void Sc013Validator_AcceptsTheAdditionalEducationEntry()
        {
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));

            Assert.That(_titleRoot.childCount, Is.EqualTo(7));
            Assert.That(AddSC013TitleScenarioButtons.Validate(_scene, out string error), Is.True, error);
            Assert.That(AddSC020CharacterEducation.Validate(_scene, out error), Is.True, error);
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
