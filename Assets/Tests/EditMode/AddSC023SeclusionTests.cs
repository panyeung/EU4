using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-023 title entry: legacy generation and migrated selector compatibility.</summary>
    public sealed class AddSC023SeclusionTests
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
            Text titleLabel = CreateText("M6TitleLabel", _titleRoot, "影之一族");
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
        public void Generator_CreatesAndBindsTheSeclusionTitleEntry()
        {
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
            Assert.That(AddSC023Seclusion.Update(_scene),
                Is.EqualTo(AddSC023Seclusion.UpdateResult.Success));

            Transform entry = Find(AddSC023Seclusion.TitleButtonName);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.parent, Is.EqualTo(_titleRoot));
            Assert.That(entry.GetComponentInChildren<Text>(true).text,
                Is.EqualTo("SC-023 隐居与后见人示范"));
            Assert.That(_titleController.SeclusionScenarioBound, Is.True);
            Assert.That(AddSC023Seclusion.Validate(_scene, out string error), Is.True, error);

            Assert.That(AddSC023Seclusion.Update(_scene),
                Is.EqualTo(AddSC023Seclusion.UpdateResult.Success));
            Assert.That(AddSC023Seclusion.Validate(_scene, out error), Is.True, error);
        }

        [Test]
        public void Updater_AcceptsMigratedSelectorLayoutEndToEnd()
        {
            Assert.That(AddSC020CharacterEducation.Update(_scene),
                Is.EqualTo(AddSC020CharacterEducation.UpdateResult.Success));
            Assert.That(AddSC023Seclusion.Update(_scene),
                Is.EqualTo(AddSC023Seclusion.UpdateResult.Success));

            Transform selector = Create("M6ScenarioSelectRoot", _titleRoot).transform;
            RectTransform selectorRect = (RectTransform)selector;
            selectorRect.anchorMin = Vector2.zero;
            selectorRect.anchorMax = Vector2.one;
            selectorRect.offsetMin = Vector2.zero;
            selectorRect.offsetMax = Vector2.zero;
            Transform entry = Find(AddSC023Seclusion.TitleButtonName);
            entry.SetParent(selector, false);
            ((RectTransform)entry).anchoredPosition = new Vector2(235f, -20f);
            LayoutRebuilder.ForceRebuildLayoutImmediate(selectorRect);

            int count = ObjectCount();
            Assert.That(AddSC023Seclusion.Update(_scene),
                Is.EqualTo(AddSC023Seclusion.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "A migrated rerun must not create a second flat title entry.");
            Assert.That(entry.parent, Is.EqualTo(selector));
            Assert.That(_titleRoot.Find(AddSC023Seclusion.TitleButtonName), Is.Null);
            Assert.That(AddSC023Seclusion.Validate(_scene, out string error), Is.True, error);
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
