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
    public sealed class AddSC013TitleScenarioButtonsTests
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;

        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private GameSession _session;
        private TitleScreenController _controller;
        private Transform _root;
        private Text _status;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas), typeof(CanvasScaler));
            CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            // The synthetic fixture is frozen at reference resolution; no screen scaling is involved.
            canvasScaler.enabled = false;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame(33UL);
            _controller = _session.gameObject.AddComponent<TitleScreenController>();

            // Mirror the serialized FoundationScene: the title root stretches to the full
            // 1280x720 Canvas and every base control is center-anchored. The default
            // 100x100 RectTransform would make the updater's bounds check fail.
            GameObject rootObject = Create("M6TitleRoot", canvas.transform, typeof(Image));
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            _root = rootObject.transform;

            Text titleLabel = CreateText("M6TitleLabel", _root, "战国立志传");
            SetCenteredRect(titleLabel.rectTransform, new Vector2(0f, 180f), new Vector2(900f, 70f));
            Button newGame = CreateButton("M6NewGameButton", _root, "新游戏");
            SetCenteredRect((RectTransform)newGame.transform, new Vector2(0f, 30f), new Vector2(260f, 56f));
            Button cont = CreateButton("M6ContinueButton", _root, "继续");
            SetCenteredRect((RectTransform)cont.transform, new Vector2(0f, -46f), new Vector2(260f, 56f));
            // The base generator saves the status text at y=-120; the SC-013 updater moves it to -260.
            _status = CreateText("M6TitleStatusText", _root, string.Empty);
            SetCenteredRect(_status.rectTransform, new Vector2(0f, -120f), new Vector2(700f, 30f));

            _controller.Bind(_session, null, null, rootObject, newGame, cont, _status);
            PrepareCanvasGeometry(_scene, canvasRect, canvasScaler, rootRect);
            rootObject.SetActive(false);
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
        public void UpdaterAddsTwoValidatedEntriesAndRebindsController()
        {
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));

            Assert.That(_root.childCount, Is.EqualTo(6));
            Transform standard = Find(AddSC013TitleScenarioButtons.StandardButtonName);
            Transform crisis = Find(AddSC013TitleScenarioButtons.CrisisButtonName);
            Assert.That(standard, Is.Not.Null);
            Assert.That(crisis, Is.Not.Null);
            Assert.That(standard.GetComponentInChildren<Text>(true).text, Is.EqualTo("SC-010 标准样例"));
            Assert.That(crisis.GetComponentInChildren<Text>(true).text, Is.EqualTo("SC-010 危机示范"));
            Assert.That(_status.rectTransform.anchoredPosition.y, Is.EqualTo(-260f));
            Assert.That(_controller.ScenarioEntriesBound, Is.True);
            Assert.That(AddSC013TitleScenarioButtons.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must not save or reload a scene.");
        }

        [Test]
        public void UpdaterIsIdempotentAndPreflightRefusesWrongScenes()
        {
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));
            int count = ObjectCount();
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count));

            Assert.That(AddSC013TitleScenarioButtons.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.SceneMismatch));
            Assert.That(AddSC013TitleScenarioButtons.CheckScenePreflight(
                    AddSC013TitleScenarioButtons.ScenePath, true),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.SceneDirty));
            Assert.That(AddSC013TitleScenarioButtons.CheckScenePreflight(
                    AddSC013TitleScenarioButtons.ScenePath, false, true),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.EditorPlaying));
        }

        [Test]
        public void ScenarioButtonsDriveIndependentSessionInitialization()
        {
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success));

            Find(AddSC013TitleScenarioButtons.StandardButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
            Assert.That(_session.IsM2ScenarioActive, Is.False);

            Find(AddSC013TitleScenarioButtons.CrisisButtonName).GetComponent<Button>().onClick.Invoke();
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(4),
                "the crisis entry must replace the standard roster with the fixed crisis roster");
            Assert.That(_session.Succession.World.TryGetPerson(
                new PersonId(M2FixedScenario.HarunobuIdValue), out _), Is.False);
        }

        private Text CreateText(string name, Transform parent, string value)
        {
            GameObject go = Create(name, parent, typeof(Text));
            Text text = go.GetComponent<Text>();
            text.text = value;
            return text;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PrepareCanvasGeometry(Scene scene, RectTransform canvasRect, CanvasScaler scaler,
            RectTransform titleRoot)
        {
            Assert.That(canvasRect, Is.Not.Null);
            Assert.That(scaler, Is.Not.Null);
            Assert.That(titleRoot, Is.Not.Null);
            Assert.That(canvasRect.gameObject.scene.handle, Is.EqualTo(scene.handle),
                "Canvas must belong to the preview scene");
            Assert.That(titleRoot.gameObject.scene.handle, Is.EqualTo(scene.handle),
                "M6TitleRoot must belong to the preview scene");

            // A root Canvas in a Preview Scene can still carry Unity-driven zero scale/size before its
            // first layout pass. Freeze the fixture at reference resolution and rebuild only its tree so
            // the stretched title root has measurable 1280x720 bounds.
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = scaler.referenceResolution;
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);

            Vector2 titleSize = MeasureInCanvas(canvasRect, titleRoot);
            Assert.That(titleSize.x, Is.EqualTo(ReferenceWidth).Within(0.5f),
                $"M6TitleRoot must span the full canvas width: {DescribeGeometry(canvasRect, titleRoot)}");
            Assert.That(titleSize.y, Is.EqualTo(ReferenceHeight).Within(0.5f),
                $"M6TitleRoot must span the full canvas height: {DescribeGeometry(canvasRect, titleRoot)}");
        }

        private static Vector2 MeasureInCanvas(RectTransform canvas, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 min = canvas.InverseTransformPoint(corners[0]);
            Vector3 max = canvas.InverseTransformPoint(corners[2]);
            return new Vector2(max.x - min.x, max.y - min.y);
        }

        private static string DescribeGeometry(RectTransform canvas, RectTransform target) =>
            $"canvasRect={canvas.rect}, canvasSizeDelta={canvas.sizeDelta}, " +
            $"canvasScale={canvas.localScale}, canvasLossyScale={canvas.lossyScale}, " +
            $"titleRect={target.rect}, titleSizeDelta={target.sizeDelta}, " +
            $"titleAnchors={target.anchorMin}..{target.anchorMax}, titleScale={target.localScale}";

        private Button CreateButton(string name, Transform parent, string label)
        {
            GameObject go = Create(name, parent, typeof(Image), typeof(Button));
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            CreateText("Label", go.transform, label);
            return button;
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

        private int ObjectCount() => _scene.GetRootGameObjects()
            .Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
    }
}
