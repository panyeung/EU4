using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class AddM6TitleScreenTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
        private Transform _map;
        private Transform _town;
        private GameObject _sessionObject;
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

            GameObject map = Create("M2WorldMapRoot", _canvas, typeof(WorldMapController));
            map.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _map = map.transform;

            GameObject town = Create("TownMenuRoot", _map);
            town.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _town = town.transform;
            town.SetActive(false);

            _sessionObject = Create("GameSession", null, typeof(GameSession));
            _session = _sessionObject.GetComponent<GameSession>();
            _session.InitNewGame();
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
        public void Updater_AddsAndBindsTitleAndSummary()
        {
            int count = ObjectCount();

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Transform root = Find(AddM6TitleScreen.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(_map.GetSiblingIndex()));
            Assert.That(root.childCount, Is.EqualTo(4));
            Assert.That(root.gameObject.activeSelf, Is.False, "Title must boot hidden and be shown by Play Mode.");
            Assert.That(ObjectCount(), Is.EqualTo(count + 8));

            Image background = root.GetComponent<Image>();
            Assert.That(background, Is.Not.Null);
            Assert.That(background.raycastTarget, Is.True);
            Assert.That(background.color.a, Is.EqualTo(1f));
            RectTransform rootRect = (RectTransform)root;
            Assert.That(rootRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rootRect.anchorMax, Is.EqualTo(Vector2.one));

            TitleScreenController controller = _sessionObject.GetComponent<TitleScreenController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(GetReference(controller, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(controller, "worldMapController"), Is.EqualTo(_map.GetComponent<WorldMapController>()));
            Assert.That(GetReference(controller, "titleRoot"), Is.EqualTo(root.gameObject));
            Assert.That(GetReference(controller, "newGameButton"), Is.EqualTo(Find(AddM6TitleScreen.NewGameButtonName).GetComponent<Button>()));
            Assert.That(GetReference(controller, "continueButton"), Is.EqualTo(Find(AddM6TitleScreen.ContinueButtonName).GetComponent<Button>()));
            Assert.That(GetReference(controller, "statusText"), Is.EqualTo(Find(AddM6TitleScreen.StatusTextName).GetComponent<Text>()));

            Transform summary = Find(AddM6TitleScreen.DemoSummaryTextName);
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.parent, Is.EqualTo(_map));
            Assert.That(summary.GetSiblingIndex(), Is.LessThan(_town.GetSiblingIndex()));
            Assert.That(summary.gameObject.activeSelf, Is.False);

            DemoSummaryPanel panel = _sessionObject.GetComponent<DemoSummaryPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(panel, "summaryText"), Is.EqualTo(summary.GetComponent<Text>()));

            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must never save its scene.");
        }

        [Test]
        public void Updater_IsIdempotentWhenAlreadyValid()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            int builtCount = ObjectCount();
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount));
        }

        [Test]
        public void Updater_RollsBackWhenValidationFails()
        {
            int count = ObjectCount();

            AddM6TitleScreen.UpdateResult result = AddM6TitleScreen.Update(_scene, _ =>
                SetReference(_sessionObject.GetComponent<TitleScreenController>(), "titleRoot", null));

            Assert.That(result, Is.EqualTo(AddM6TitleScreen.UpdateResult.ValidationFailed));
            Assert.That(Find(AddM6TitleScreen.RootName), Is.Null);
            Assert.That(Find(AddM6TitleScreen.DemoSummaryTextName), Is.Null);
            Assert.That(_sessionObject.GetComponent<TitleScreenController>(), Is.Null);
            Assert.That(_sessionObject.GetComponent<DemoSummaryPanel>(), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Updater_RefusesPartialExistingPanel()
        {
            Create(AddM6TitleScreen.RootName, _canvas);

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.ExistingPanel));
        }

        [Test]
        public void Validator_RejectsSummaryOverlappingSaveControls()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            GameObject blocker = Create("M5SaveButton", _map, typeof(Image), typeof(Button));
            RectTransform rect = blocker.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(460f, -110f);
            rect.sizeDelta = new Vector2(150f, 30f);

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
            Object.DestroyImmediate(blocker);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsActiveTitleRoot()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Find(AddM6TitleScreen.RootName).gameObject.SetActive(true);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            Assert.That(AddM6TitleScreen.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.SceneMismatch));
            Assert.That(AddM6TitleScreen.CheckScenePreflight(AddM6TitleScreen.ScenePath, true),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.SceneDirty));
            Assert.That(AddM6TitleScreen.CheckScenePreflight(AddM6TitleScreen.ScenePath, false),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
        }

        private Transform Find(string name) => _scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name)
            .SingleOrDefault();

        private int ObjectCount() =>
            _scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

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

        private static Object GetReference(Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
