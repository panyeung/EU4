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
    public sealed class AddM4bMissionBoardTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
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
            _town = Create("TownMenuRoot", map.transform).transform;
            ((RectTransform)_town).sizeDelta = new Vector2(1216f, 268f);
            _town.gameObject.SetActive(false);
            CreateTownControl("TownMenuTitleText", null, new Vector2(0f, 104f), new Vector2(500f, 32f), typeof(Text));
            CreateTownControl("OpenNpcRosterButton", null, new Vector2(-160f, -95f), new Vector2(240f, 46f), typeof(Image), typeof(Button));
            CreateTownControl("ReturnToMapButton", null, new Vector2(160f, -95f), new Vector2(260f, 52f), typeof(Image), typeof(Button));
            _controller = map.GetComponent<WorldMapController>();
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame();
            SetReference(_controller, "gameSession", _session);
            SetReference(_controller, "townMenuRoot", _town.gameObject);
            Assert.That(AddM4SupplyShortageMissionPanel.Update(_scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        [Test]
        public void Updater_AddsAndBindsBothM4bCardsExactlyOnce()
        {
            int count = ObjectCount();
            Assert.That(AddM4bMissionBoard.Update(_scene), Is.EqualTo(AddM4bMissionBoard.UpdateResult.Success));
            Transform root = _town.Find(AddM4SupplyShortageMissionPanel.RootName);
            SupplyShortageMissionPanel panel = root.GetComponent<SupplyShortageMissionPanel>();
            Assert.That(((RectTransform)root).sizeDelta.x, Is.EqualTo(280f));
            Assert.That(root.childCount, Is.EqualTo(17));
            Assert.That(ObjectCount(), Is.EqualTo(count + 12));
            Assert.That(Overlaps((RectTransform)root.Find("SealedLetterObjectiveText"), (RectTransform)root.Find("SealedLetterFeedbackText")), Is.False,
                "Sealed letter objective and feedback need separate readable slots.");
            Assert.That(Overlaps((RectTransform)root.Find("MarketTallyObjectiveText"), (RectTransform)root.Find("MarketTallyFeedbackText")), Is.False,
                "Market tally objective and feedback need separate readable slots.");
            Assert.That(((RectTransform)root.Find("SealedLetterFeedbackText")).sizeDelta.y, Is.GreaterThanOrEqualTo(22f));
            Assert.That(((RectTransform)root.Find("MarketTallyFeedbackText")).sizeDelta.y, Is.GreaterThanOrEqualTo(22f));
            Assert.That(Overlaps((RectTransform)root.Find("MarketTallyActionButton"), (RectTransform)root.Find("MissionBoardToggleButton")), Is.False,
                "Mission actions and page navigation must have independent click targets.");
            foreach (string controlName in new[] { "TownMenuTitleText", "OpenNpcRosterButton", "ReturnToMapButton" })
                Assert.That(Overlaps((RectTransform)root, (RectTransform)_town.Find(controlName)), Is.False, $"Mission panel covers {controlName}.");
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(_scene, out string m4aError), Is.True, m4aError);
            Assert.That(AddM4bMissionBoard.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must never save its scene.");
            int builtCount = ObjectCount();
            Assert.That(AddM4bMissionBoard.Update(_scene), Is.EqualTo(AddM4bMissionBoard.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount));
            Assert.That(GetReference(panel, "sealedLetterActionButton"), Is.EqualTo(root.Find("SealedLetterActionButton").GetComponent<Button>()));
            Assert.That(GetReference(panel, "marketTallyActionButton"), Is.EqualTo(root.Find("MarketTallyActionButton").GetComponent<Button>()));
        }

        [Test]
        public void Updater_RollsBackAllBoardObjectsAndGeometryWhenValidationFails()
        {
            Transform root = _town.Find(AddM4SupplyShortageMissionPanel.RootName);
            int count = ObjectCount();
            RectTransform m4aAccept = (RectTransform)root.Find("AcceptButton");
            Vector2 oldPosition = m4aAccept.anchoredPosition;
            Vector2 oldSize = m4aAccept.sizeDelta;
            var result = AddM4bMissionBoard.Update(_scene, generated =>
            {
                Text objective = generated.GetComponentsInChildren<Text>(true).Single(t => t.name == "SealedLetterObjectiveText");
                SetReference(generated.GetComponent<SupplyShortageMissionPanel>(), "sealedLetterObjectiveText", null);
                objective.text = "invalid";
            });
            Assert.That(result, Is.EqualTo(AddM4bMissionBoard.UpdateResult.ValidationFailed));
            Assert.That(root.childCount, Is.EqualTo(8));
            Assert.That(((RectTransform)root).sizeDelta.x, Is.EqualTo(280f));
            Assert.That(m4aAccept.anchoredPosition, Is.EqualTo(oldPosition));
            Assert.That(m4aAccept.sizeDelta, Is.EqualTo(oldSize));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddM4bMissionBoard.Validate(_scene, out _), Is.False);
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndExternalM4bControls()
        {
            Assert.That(AddM4bMissionBoard.Validate(_scene, out _), Is.False);
            Assert.That(AddM4bMissionBoard.Update(_scene), Is.EqualTo(AddM4bMissionBoard.UpdateResult.Success));
            Transform root = _town.Find(AddM4SupplyShortageMissionPanel.RootName);
            GameObject duplicate = Create("SealedLetterActionButton", root, typeof(Image), typeof(Button));
            Assert.That(AddM4bMissionBoard.Validate(_scene, out _), Is.False);
            Object.DestroyImmediate(duplicate);
            var panel = root.GetComponent<SupplyShortageMissionPanel>();
            SetReference(panel, "marketTallyFeedbackText", Create("External", null, typeof(Text)).GetComponent<Text>());
            Assert.That(AddM4bMissionBoard.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            Assert.That(AddM4bMissionBoard.CheckScenePreflight("Assets/Scenes/Other.unity", false), Is.EqualTo(AddM4bMissionBoard.UpdateResult.SceneMismatch));
            Assert.That(AddM4bMissionBoard.CheckScenePreflight(AddM4bMissionBoard.ScenePath, true), Is.EqualTo(AddM4bMissionBoard.UpdateResult.SceneDirty));
            Assert.That(AddM4bMissionBoard.CheckScenePreflight(AddM4bMissionBoard.ScenePath, false), Is.EqualTo(AddM4bMissionBoard.UpdateResult.Success));
        }

        private int ObjectCount() => _scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        private void CreateTownControl(string name, Transform parent, Vector2 position, Vector2 size, params System.Type[] components)
        {
            GameObject control = Create(name, parent ?? _town, components);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static bool Overlaps(RectTransform first, RectTransform second)
        {
            RectTransform parent = first.parent as RectTransform;
            var firstCorners = new Vector3[4]; var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners); second.GetWorldCorners(secondCorners);
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
            if (parent != null) go.transform.SetParent(parent, false);
            foreach (System.Type component in components) go.AddComponent(component);
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
