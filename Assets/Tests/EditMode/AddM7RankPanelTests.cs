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
    public sealed class AddM7RankPanelTests
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

            CreateMapControl("TitleText", new Vector2(0f, 110f), new Vector2(600f, 36f), typeof(Text));

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
        public void Updater_AddsAndBindsRankTextAndPanel()
        {
            int count = ObjectCount();

            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count + 1));

            Transform rankText = Find(AddM7RankPanel.RankTextName);
            Assert.That(rankText, Is.Not.Null);
            Assert.That(rankText.parent, Is.EqualTo(_map));
            Assert.That(rankText.GetSiblingIndex(), Is.LessThan(_town.GetSiblingIndex()));

            RankPanel panel = _sessionObject.GetComponent<RankPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(panel, "rankText"), Is.EqualTo(rankText.GetComponent<Text>()));

            Assert.That(rankText.GetComponent<Text>().text, Does.Contain("足轻"));
            Assert.That(AddM7RankPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must never save its scene.");
        }

        [Test]
        public void Updater_IsIdempotentWhenAlreadyValid()
        {
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));

            int builtCount = ObjectCount();
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount));
        }

        [Test]
        public void Updater_RollsBackWhenValidationFails()
        {
            int count = ObjectCount();

            AddM7RankPanel.UpdateResult result = AddM7RankPanel.Update(_scene, _ =>
                SetReference(_sessionObject.GetComponent<RankPanel>(), "rankText", null));

            Assert.That(result, Is.EqualTo(AddM7RankPanel.UpdateResult.ValidationFailed));
            Assert.That(Find(AddM7RankPanel.RankTextName), Is.Null);
            Assert.That(_sessionObject.GetComponent<RankPanel>(), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Updater_RefusesPartialExistingPanel()
        {
            Create(AddM7RankPanel.RankTextName, _map);

            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.ExistingPanel));
        }

        [Test]
        public void Validator_RejectsRankTextOverlappingMapControls()
        {
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));

            Transform rankText = Find(AddM7RankPanel.RankTextName);
            rankText.position = _map.Find("TitleText").position;

            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            Assert.That(AddM7RankPanel.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM7RankPanel.UpdateResult.SceneMismatch));
            Assert.That(AddM7RankPanel.CheckScenePreflight(AddM7RankPanel.ScenePath, true),
                Is.EqualTo(AddM7RankPanel.UpdateResult.SceneDirty));
            Assert.That(AddM7RankPanel.CheckScenePreflight(AddM7RankPanel.ScenePath, false),
                Is.EqualTo(AddM7RankPanel.UpdateResult.Success));
        }

        private void CreateMapControl(string name, Vector2 position, Vector2 size, params System.Type[] components)
        {
            GameObject control = Create(name, _map, components);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
