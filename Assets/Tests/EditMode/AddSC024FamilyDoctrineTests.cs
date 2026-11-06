using System;
using System.Collections.Generic;
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
    /// <summary>
    /// SC-024 Task 5: transactional scene generation, duplicate/rollback safety, Canvas
    /// containment, HUD separation, and title layout beside the SC-023 entry.
    /// </summary>
    public sealed class AddSC024FamilyDoctrineTests
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;

        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
        private RectTransform _canvasRect;
        private GameSession _session;
        private TitleScreenController _controller;
        private Transform _titleRoot;
        private Button _seclusionButton;

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
            _canvasRect = canvas.GetComponent<RectTransform>();
            _canvasRect.anchorMin = _canvasRect.anchorMax = Vector2.zero;
            _canvasRect.pivot = new Vector2(0.5f, 0.5f);
            _canvasRect.anchoredPosition = Vector2.zero;
            _canvasRect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            _canvas = canvas.transform;

            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitFamilyDoctrineDemoScenario(33UL);
            _controller = _session.gameObject.AddComponent<TitleScreenController>();

            GameObject titleRoot = Create("M6TitleRoot", _canvas, typeof(Image));
            RectTransform titleRect = titleRoot.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            _titleRoot = titleRoot.transform;
            Text label = CreateText("M6TitleLabel", _titleRoot, "影之一族");
            SetCenteredRect(label.rectTransform, new Vector2(0f, 180f), new Vector2(900f, 70f));
            Button newGame = CreateButton("M6NewGameButton", _titleRoot, "新游戏",
                new Vector2(0f, 30f), new Vector2(260f, 56f));
            Button cont = CreateButton("M6ContinueButton", _titleRoot, "继续",
                new Vector2(0f, -46f), new Vector2(260f, 56f));
            Text status = CreateText("M6TitleStatusText", _titleRoot, string.Empty);
            SetCenteredRect(status.rectTransform, new Vector2(0f, -330f), new Vector2(700f, 30f));
            _seclusionButton = CreateButton("SC023SeclusionDemoButton", _titleRoot,
                "SC-023 隐居与后见人示范", new Vector2(495f, -325f), new Vector2(250f, 40f));
            _controller.Bind(_session, null, null, titleRoot, newGame, cont, status);
            _controller.BindSeclusionScenarioButton(_seclusionButton);
            PrepareCanvasGeometry();
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
        public void Generator_CreatesInactiveRootOpenButtonAndValidatedTitleEntry()
        {
            int before = ObjectCount();

            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.GreaterThan(before));

            Transform root = Find(AddSC024FamilyDoctrine.RootName);
            Transform open = Find(AddSC024FamilyDoctrine.OpenButtonName);
            Assert.That(root, Is.Not.Null);
            Assert.That(open, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(open.parent, Is.EqualTo(_canvas));
            Assert.That(root.gameObject.activeSelf, Is.False,
                "the fixed panel root must be saved inactive");
            Assert.That(open.gameObject.activeSelf, Is.True);
            Assert.That(FindCount(AddSC024FamilyDoctrine.RootName), Is.EqualTo(1));
            Assert.That(FindCount(AddSC024FamilyDoctrine.OpenButtonName), Is.EqualTo(1));
            Assert.That(FindCount(AddSC024FamilyDoctrine.TitleButtonName), Is.EqualTo(1));

            FamilyDoctrinePanel panel = open.GetComponent<FamilyDoctrinePanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.transform, Is.EqualTo(open));
            Assert.That(panel.IsBound, Is.True);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(Find(AddSC024FamilyDoctrine.BlockReasonName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC024FamilyDoctrine.MessageName).gameObject.activeSelf, Is.False);

            Transform titleButton = Find(AddSC024FamilyDoctrine.TitleButtonName);
            Assert.That(titleButton.parent, Is.EqualTo(_titleRoot));
            Assert.That(titleButton.gameObject.activeSelf, Is.True);
            Assert.That(titleButton.GetComponentInChildren<Text>(true).text,
                Does.Contain("家训").And.Contain("抱负"));
            Assert.That(_controller.FamilyDoctrineScenarioBound, Is.True);
            Assert.That(_seclusionButton, Is.Not.Null, "the SC-023 entry must be preserved");

            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "the test seam must not save or reload a scene");

            int count = ObjectCount();
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out error), Is.True, error);
        }

        [Test]
        public void Generator_PreflightRefusesWrongDirtyAndPlayingScene()
        {
            Assert.That(AddSC024FamilyDoctrine.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.SceneMismatch));
            Assert.That(AddSC024FamilyDoctrine.CheckScenePreflight(
                    AddSC024FamilyDoctrine.ScenePath, true),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.SceneDirty));
            Assert.That(AddSC024FamilyDoctrine.CheckScenePreflight(
                    AddSC024FamilyDoctrine.ScenePath, false, true),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.EditorPlaying));
        }

        [Test]
        public void Generator_RefusesExistingPartialControlAndDuplicateRoot()
        {
            Create(AddSC024FamilyDoctrine.RootName, _canvas);

            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.ExistingPartialControl));
            Assert.That(Find(AddSC024FamilyDoctrine.OpenButtonName), Is.Null);
            Assert.That(_controller.FamilyDoctrineScenarioBound, Is.False);

            UnityEngine.Object.DestroyImmediate(Find(AddSC024FamilyDoctrine.RootName).gameObject);
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));

            Create(AddSC024FamilyDoctrine.RootName, _canvas);
            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("exactly one"));
        }

        [Test]
        public void Generator_RollsBackOnValidationFailureAndPreservesOriginalObjects()
        {
            int before = ObjectCount();
            Transform[] originals = AllTransforms();

            AddSC024FamilyDoctrine.UpdateResult result = AddSC024FamilyDoctrine.Update(_scene,
                root => root.name = AddSC024FamilyDoctrine.RootName + "Broken");

            Assert.That(result, Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.ValidationFailed));
            Assert.That(Find(AddSC024FamilyDoctrine.RootName), Is.Null);
            Assert.That(Find(AddSC024FamilyDoctrine.OpenButtonName), Is.Null);
            Assert.That(Find(AddSC024FamilyDoctrine.TitleButtonName), Is.Null,
                "the title entry must live in the same Undo transaction");
            Assert.That(_controller.FamilyDoctrineScenarioBound, Is.False);
            Assert.That(ObjectCount(), Is.EqualTo(before));
            Transform[] after = AllTransforms();
            foreach (Transform original in originals)
            {
                Assert.That(original, Is.Not.Null, "an original object was removed");
                Assert.That(after, Does.Contain(original),
                    "an original object was replaced by a new instance");
            }
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Validator_RejectsOpenButtonOverlappingAnotherHudEntry()
        {
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));
            RectTransform open = (RectTransform)Find(AddSC024FamilyDoctrine.OpenButtonName);
            Button chronicle = CreateButton("FamilyChronicleOpenButton", _canvas, "家族编年史",
                Vector2.zero, new Vector2(200f, 44f));
            RectTransform chronicleRect = (RectTransform)chronicle.transform;
            chronicleRect.anchorMin = chronicleRect.anchorMax = new Vector2(0f, 1f);
            chronicleRect.pivot = new Vector2(0f, 1f);
            chronicleRect.anchoredPosition = open.anchoredPosition;
            chronicleRect.sizeDelta = open.sizeDelta;

            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("overlaps"));
        }

        [Test]
        public void Validator_RejectsTitleEntryOverlappingSc023()
        {
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));
            RectTransform titleButton =
                (RectTransform)Find(AddSC024FamilyDoctrine.TitleButtonName);
            RectTransform seclusion = (RectTransform)_seclusionButton.transform;
            titleButton.anchorMin = seclusion.anchorMin;
            titleButton.anchorMax = seclusion.anchorMax;
            titleButton.pivot = seclusion.pivot;
            titleButton.anchoredPosition = seclusion.anchoredPosition;
            titleButton.sizeDelta = seclusion.sizeDelta;

            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("overlaps"));
            Assert.That(error, Does.Contain("SC-023"));
        }

        [Test]
        public void Validator_RejectsControlOutsidePanelBounds()
        {
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));
            RectTransform close = (RectTransform)Find(AddSC024FamilyDoctrine.CloseButtonName);
            close.anchoredPosition = new Vector2(900f, -900f);

            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("outside"));
        }

        [Test]
        public void TitleLayout_AcceptsSc010AndSc020ToSc024EntriesBesideSc023()
        {
            CreateButton("SC020EducationDemoButton", _titleRoot, "SC-020 子女培养示范",
                new Vector2(0f, -274f), new Vector2(320f, 48f));
            CreateButton("SC021ResearchDemoButton", _titleRoot, "SC-021 家族研究示范",
                new Vector2(340f, -274f), new Vector2(300f, 48f));
            CreateButton("SC022FamilyFormationDemoButton", _titleRoot, "SC-022 婚姻与收养示范",
                new Vector2(-340f, -274f), new Vector2(300f, 48f));

            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));

            Assert.That(AddSC013TitleScenarioButtons.Validate(_scene, out string scError),
                Is.True, scError);
            Assert.That(AddSC024FamilyDoctrine.Validate(_scene, out string sc024Error),
                Is.True, sc024Error);
            Assert.That(_titleRoot.childCount, Is.EqualTo(11));
            Assert.That(Find(AddSC013TitleScenarioButtons.StandardButtonName), Is.Not.Null);
            Assert.That(Find(AddSC013TitleScenarioButtons.CrisisButtonName), Is.Not.Null);
            Assert.That(Find(AddSC024FamilyDoctrine.TitleButtonName), Is.Not.Null);
            Assert.That(_seclusionButton, Is.Not.Null);
        }

        [Test]
        public void TitleEntry_StartsTheFixedSc024Scenario()
        {
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));

            Find(AddSC024FamilyDoctrine.TitleButtonName).GetComponent<Button>().onClick.Invoke();

            Assert.That(SC024FamilyDoctrineScenario.IsActive(_session.Succession), Is.True);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(SC024FamilyDoctrineScenario.StartDate));
            Assert.That(_session.CurrentFamilyCreed,
                Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(_session.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(_session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.DevelopHousehold));
            Assert.That(_controller.IsTitleVisible, Is.False);
        }

        private Text CreateText(string name, Transform parent, string value)
        {
            GameObject go = Create(name, parent, typeof(Text));
            Text text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 position,
            Vector2 size)
        {
            GameObject go = Create(name, parent, typeof(Image), typeof(Button));
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            CreateText("Label", go.transform, label);
            SetCenteredRect((RectTransform)go.transform, position, size);
            return button;
        }

        private GameObject Create(string name, Transform parent, params Type[] types)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type type in types)
                go.AddComponent(type);
            return go;
        }

        private Transform Find(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);

        private int FindCount(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Count(transform => transform.name == name);

        private Transform[] AllTransforms() =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

        private int ObjectCount() => AllTransforms().Length;

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void PrepareCanvasGeometry()
        {
            _canvasRect.localScale = Vector3.one;
            _canvasRect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_canvasRect);
            var corners = new Vector3[4];
            _titleRoot.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector3 min = _canvasRect.InverseTransformPoint(corners[0]);
            Vector3 max = _canvasRect.InverseTransformPoint(corners[2]);
            Assert.That(max.x - min.x, Is.EqualTo(ReferenceWidth).Within(0.5f));
            Assert.That(max.y - min.y, Is.EqualTo(ReferenceHeight).Within(0.5f));
        }
    }
}
