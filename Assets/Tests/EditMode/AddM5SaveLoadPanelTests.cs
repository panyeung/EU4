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
    public sealed class AddM5SaveLoadPanelTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _map;
        private Transform _town;
        private WorldMapController _controller;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);

            GameObject map = Create("M2WorldMapRoot", canvas.transform, typeof(WorldMapController));
            map.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _map = map.transform;
            _controller = map.GetComponent<WorldMapController>();

            CreateMapControl("TitleText", new Vector2(0f, 110f), new Vector2(600f, 36f), typeof(Text));
            CreateMapControl("LocationMarkerText", new Vector2(0f, 76f), new Vector2(540f, 28f), typeof(Text));
            CreateMapControl("CurrentDateText", new Vector2(0f, 48f), new Vector2(500f, 24f), typeof(Text));
            CreateMapControl("FeedbackText", new Vector2(0f, 22f), new Vector2(1120f, 22f), typeof(Text));
            CreateMapControl("DestinationNameText", new Vector2(-380f, -1f), new Vector2(340f, 24f), typeof(Text));
            CreateMapControl("TravelTimeText", new Vector2(0f, -1f), new Vector2(240f, 24f), typeof(Text));
            CreateMapControl("ArrivalDateText", new Vector2(380f, -1f), new Vector2(340f, 24f), typeof(Text));
            CreateMapControl("DestButton_0", new Vector2(-350f, -44f), new Vector2(220f, 60f), typeof(Image), typeof(Button));
            CreateMapControl("DestButton_1", new Vector2(0f, -44f), new Vector2(220f, 60f), typeof(Image), typeof(Button));
            CreateMapControl("DestButton_2", new Vector2(350f, -44f), new Vector2(220f, 60f), typeof(Image), typeof(Button));
            CreateMapControl("ConfirmButton", new Vector2(-145f, -104f), new Vector2(260f, 48f), typeof(Image), typeof(Button));
            CreateMapControl("CancelButton", new Vector2(145f, -104f), new Vector2(260f, 48f), typeof(Image), typeof(Button));

            GameObject town = Create("TownMenuRoot", map.transform);
            ((RectTransform)town.transform).sizeDelta = new Vector2(1216f, 268f);
            _town = town.transform;
            town.SetActive(false);

            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame();
            SetReference(_controller, "gameSession", _session);
            SetReference(_controller, "townMenuRoot", town);
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
        public void Updater_AddsAndBindsControlsExactlyOnce()
        {
            int count = ObjectCount();

            Assert.That(AddM5SaveLoadPanel.Update(_scene), Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.Success));

            Transform root = _map.Find(AddM5SaveLoadPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_map));
            Assert.That(root.childCount, Is.EqualTo(3));
            Assert.That(ObjectCount(), Is.EqualTo(count + 6));
            Assert.That(root.GetSiblingIndex(), Is.LessThan(_town.GetSiblingIndex()));

            SaveLoadPanel panel = root.GetComponent<SaveLoadPanel>();
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(panel, "worldMapController"), Is.EqualTo(_controller));
            Assert.That(GetReference(panel, "saveButton"), Is.EqualTo(root.Find(AddM5SaveLoadPanel.SaveButtonName).GetComponent<Button>()));
            Assert.That(GetReference(panel, "loadButton"), Is.EqualTo(root.Find(AddM5SaveLoadPanel.LoadButtonName).GetComponent<Button>()));
            Assert.That(GetReference(panel, "statusText"), Is.EqualTo(root.Find(AddM5SaveLoadPanel.StatusTextName).GetComponent<Text>()));

            foreach (string controlName in new[] { "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText", "ConfirmButton", "CancelButton" })
                Assert.That(Overlaps((RectTransform)root, (RectTransform)_map.Find(controlName)), Is.False,
                    $"Save/load root covers {controlName}.");

            Assert.That(AddM5SaveLoadPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must never save its scene.");

            int builtCount = ObjectCount();
            Assert.That(AddM5SaveLoadPanel.Update(_scene), Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount));
        }

        [Test]
        public void Updater_RollsBackWhenValidationFails()
        {
            int count = ObjectCount();

            var result = AddM5SaveLoadPanel.Update(_scene, generated =>
            {
                SetReference(generated.GetComponent<SaveLoadPanel>(), "saveButton", null);
            });

            Assert.That(result, Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.ValidationFailed));
            Assert.That(_map.Find(AddM5SaveLoadPanel.RootName), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddM5SaveLoadPanel.Validate(_scene, out _), Is.False);
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndExternalControls()
        {
            Assert.That(AddM5SaveLoadPanel.Validate(_scene, out _), Is.False);
            Assert.That(AddM5SaveLoadPanel.Update(_scene), Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.Success));
            Assert.That(AddM5SaveLoadPanel.Validate(_scene, out _), Is.True);

            Transform root = _map.Find(AddM5SaveLoadPanel.RootName);
            GameObject duplicate = Create(AddM5SaveLoadPanel.SaveButtonName, root, typeof(Image), typeof(Button));
            Assert.That(AddM5SaveLoadPanel.Validate(_scene, out _), Is.False);
            Object.DestroyImmediate(duplicate);

            var panel = root.GetComponent<SaveLoadPanel>();
            SetReference(panel, "statusText", Create("ExternalStatus", null, typeof(Text)).GetComponent<Text>());
            Assert.That(AddM5SaveLoadPanel.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            Assert.That(AddM5SaveLoadPanel.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.SceneMismatch));
            Assert.That(AddM5SaveLoadPanel.CheckScenePreflight(AddM5SaveLoadPanel.ScenePath, true),
                Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.SceneDirty));
            Assert.That(AddM5SaveLoadPanel.CheckScenePreflight(AddM5SaveLoadPanel.ScenePath, false),
                Is.EqualTo(AddM5SaveLoadPanel.UpdateResult.Success));
        }

        private int ObjectCount() =>
            _scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        private void CreateMapControl(string name, Vector2 position, Vector2 size, params System.Type[] components)
        {
            GameObject control = Create(name, _map, components);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static bool Overlaps(RectTransform first, RectTransform second)
        {
            RectTransform parent = first.parent as RectTransform;
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            Vector3 firstMin = parent.InverseTransformPoint(firstCorners[0]);
            Vector3 firstMax = parent.InverseTransformPoint(firstCorners[2]);
            Vector3 secondMin = parent.InverseTransformPoint(secondCorners[0]);
            Vector3 secondMax = parent.InverseTransformPoint(secondCorners[2]);
            Rect firstRect = Rect.MinMaxRect(firstMin.x, firstMin.y, firstMax.x, firstMax.y);
            Rect secondRect = Rect.MinMaxRect(secondMin.x, secondMin.y, secondMax.x, secondMax.y);
            return firstRect.Overlaps(secondRect, true);
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
