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
        private Transform _hud;
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
            RectTransform mapRect = map.GetComponent<RectTransform>();
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.19722222f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.anchoredPosition = Vector2.zero;
            mapRect.sizeDelta = new Vector2(1216f, 268f);
            _map = map.transform;

            CreateMapControl("TitleText", new Vector2(0f, 110f), new Vector2(600f, 36f), typeof(Text));

            GameObject town = Create("TownMenuRoot", _map);
            town.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _town = town.transform;
            town.SetActive(false);

            BuildSuccessionHud();

            // Representative left-side feature buttons. FamilyDoctrineOpenButton is
            // the control that used to cover the old map-region rank text.
            CreateTopLeftControl("IndustryOpenButton", new Vector2(20f, -250f), new Vector2(200f, 44f));
            CreateTopLeftControl("FamilyDoctrineOpenButton", new Vector2(20f, -458f), new Vector2(220f, 44f));

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
            Assert.That(rankText.parent, Is.EqualTo(_hud));
            Assert.That(rankText.gameObject.activeSelf, Is.True);

            var rect = rankText as RectTransform;
            Assert.That(rect.rect.width, Is.EqualTo(390f).Within(0.5f));
            Assert.That(rect.rect.height, Is.EqualTo(20f).Within(0.5f));
            Text text = rankText.GetComponent<Text>();
            Assert.That(text.fontSize, Is.EqualTo(15));
            Assert.That(text.alignment, Is.EqualTo(TextAnchor.MiddleLeft));

            RankPanel panel = _sessionObject.GetComponent<RankPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(panel, "rankText"), Is.EqualTo(text));

            Assert.That(text.text, Does.Contain("足轻"));
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
        public void Updater_MigratesLegacyMapRankTextIntoSuccessionHud()
        {
            Text legacy = CreateLegacyRankText();
            int count = ObjectCount();

            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False,
                "The legacy map placement must not validate.");
            Assert.That(WorldRect((RectTransform)legacy.transform)
                    .Overlaps(WorldRect((RectTransform)_canvas.Find("FamilyDoctrineOpenButton")), true),
                Is.True, "The legacy rect must reproduce the reported feature-button overlap.");

            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count));

            Transform rankText = Find(AddM7RankPanel.RankTextName);
            Assert.That(rankText.parent, Is.EqualTo(_hud));
            Assert.That(rankText.GetComponent<Text>(), Is.SameAs(legacy));
            var rect = rankText as RectTransform;
            Assert.That(rect.rect.width, Is.EqualTo(390f).Within(0.5f));
            Assert.That(rect.rect.height, Is.EqualTo(20f).Within(0.5f));
            Assert.That(legacy.fontSize, Is.EqualTo(15));
            Assert.That(legacy.alignment, Is.EqualTo(TextAnchor.MiddleLeft));

            RankPanel panel = _sessionObject.GetComponent<RankPanel>();
            Assert.That(GetReference(panel, "rankText"), Is.SameAs(legacy));
            Assert.That(AddM7RankPanel.Validate(_scene, out string error), Is.True, error);
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
        public void Validator_RejectsRankTextOverlappingSuccessionControls()
        {
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));

            Transform rankText = Find(AddM7RankPanel.RankTextName);
            var rect = rankText as RectTransform;
            rect.anchoredPosition = new Vector2(0f, 80f); // SuccessionHeadText slot.

            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsLegacyMapPlacement()
        {
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));

            Transform rankText = Find(AddM7RankPanel.RankTextName);
            rankText.SetParent(_map, false);
            var rect = rankText as RectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-450f, 100f);
            rect.sizeDelta = new Vector2(240f, 40f);
            Text text = rankText.GetComponent<Text>();
            text.fontSize = 11;
            text.alignment = TextAnchor.MiddleCenter;

            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsUnreadableRankText()
        {
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));

            Transform rankText = Find(AddM7RankPanel.RankTextName);
            var rect = rankText as RectTransform;
            Text text = rankText.GetComponent<Text>();

            text.fontSize = 10;
            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);

            text.fontSize = 15;
            rect.sizeDelta = new Vector2(200f, 20f);
            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);

            rect.sizeDelta = new Vector2(390f, 12f);
            Assert.That(AddM7RankPanel.Validate(_scene, out _), Is.False);

            rect.sizeDelta = new Vector2(390f, 20f);
            Assert.That(AddM7RankPanel.Validate(_scene, out string error), Is.True, error);
        }

        [Test]
        public void Validator_AcceptsCommonAspectRatios()
        {
            Assert.That(AddM7RankPanel.Update(_scene), Is.EqualTo(AddM7RankPanel.UpdateResult.Success));

            Vector2[] sizes =
            {
                new Vector2(1280f, 720f),  // 16:9
                new Vector2(1280f, 800f),  // 16:10
                new Vector2(1024f, 768f),  // 4:3
                new Vector2(2560f, 1080f), // 21:9
                new Vector2(3440f, 1440f)  // 21.5:9
            };

            var canvasRect = _canvas as RectTransform;
            foreach (Vector2 size in sizes)
            {
                canvasRect.sizeDelta = size;
                Canvas.ForceUpdateCanvases();
                Assert.That(AddM7RankPanel.Validate(_scene, out string error), Is.True,
                    $"{size.x}x{size.y}: {error}");
            }
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

        private void BuildSuccessionHud()
        {
            GameObject root = Create("SuccessionRoot", _canvas);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject hud = Create("SuccessionHud", root.transform, typeof(Image));
            RectTransform hudRect = hud.GetComponent<RectTransform>();
            hudRect.anchorMin = hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(20f, -20f);
            hudRect.sizeDelta = new Vector2(430f, 210f);
            _hud = hud.transform;

            CreateHudControl("SuccessionHeadText", new Vector2(0f, 80f), new Vector2(390f, 30f));
            CreateHudControl("SuccessionStatusText", new Vector2(0f, 35f), new Vector2(390f, 50f));
            CreateHudControl("SuccessionRetireButton", new Vector2(-100f, -25f), new Vector2(190f, 36f));
            CreateHudControl("SuccessionCrisisDemoButton", new Vector2(105f, -25f), new Vector2(200f, 36f));
            CreateHudControl("SuccessionFeedbackText", new Vector2(0f, -80f), new Vector2(390f, 30f));
        }

        private Text CreateLegacyRankText()
        {
            GameObject go = Create(AddM7RankPanel.RankTextName, _map, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-450f, 100f);
            rect.sizeDelta = new Vector2(240f, 40f);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 11;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "功名：0 · 足轻（下一阶 番头 5）";

            RankPanel panel = _sessionObject.AddComponent<RankPanel>();
            panel.Bind(_session, text);
            return text;
        }

        private void CreateMapControl(string name, Vector2 position, Vector2 size, params System.Type[] components)
        {
            GameObject control = Create(name, _map, components);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void CreateHudControl(string name, Vector2 position, Vector2 size)
        {
            GameObject control = Create(name, _hud);
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private void CreateTopLeftControl(string name, Vector2 position, Vector2 size)
        {
            GameObject control = Create(name, _canvas, typeof(Image));
            RectTransform rect = control.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
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

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                Mathf.Min(corners[0].x, corners[2].x), Mathf.Min(corners[0].y, corners[2].y),
                Mathf.Max(corners[0].x, corners[2].x), Mathf.Max(corners[0].y, corners[2].y));
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
