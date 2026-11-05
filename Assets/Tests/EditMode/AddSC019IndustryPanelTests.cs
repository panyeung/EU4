using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 7: transactional generator behavior for the industry panel.</summary>
    public sealed class AddSC019IndustryPanelTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
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
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame(33UL);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
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

        [Test]
        public void AddSC019IndustryPanel_CreatesOneRootAndBindings()
        {
            int before = ObjectCount();

            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));

            Transform root = Find(AddSC019IndustryPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(root.GetComponent<IndustryPanel>(), Is.Not.Null);
            Assert.That(Find(AddSC019IndustryPanel.PanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC019IndustryPanel.CandidateTemplateName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC019IndustryPanel.OpenButtonName).gameObject.activeSelf, Is.True);
            Assert.That(ObjectCount(), Is.GreaterThan(before));
            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "the test seam must not save or reload a scene");

            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            Assert.That(AddSC019IndustryPanel.Validate(_scene, out error), Is.True, error);
        }

        [Test]
        public void AddSC019IndustryPanel_ValidateRejectsMissingCandidateLayout()
        {
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            Transform container = Find(AddSC019IndustryPanel.FarmCandidateContainerName);
            Object.DestroyImmediate(container.GetComponent<GridLayoutGroup>());

            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("GridLayoutGroup"));
        }

        [Test]
        public void AddSC019IndustryPanel_ValidateRejectsOffCanvasTemplate()
        {
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            Transform template = Find(AddSC019IndustryPanel.CandidateTemplateName);
            template.GetComponent<RectTransform>().anchoredPosition = new Vector2(-470f, -399f);

            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("template"));
        }

        [Test]
        public void AddSC019IndustryPanel_RepairsExistingCandidateLayoutTransactionally()
        {
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            Transform container = Find(AddSC019IndustryPanel.FarmCandidateContainerName);
            Object.DestroyImmediate(container.GetComponent<GridLayoutGroup>());
            Transform template = Find(AddSC019IndustryPanel.CandidateTemplateName);
            template.GetComponent<RectTransform>().anchoredPosition = new Vector2(-470f, -399f);

            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));

            Assert.That(container.GetComponent<GridLayoutGroup>(), Is.Not.Null,
                "repair must restore the candidate grid layout");
            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.True, error);
        }

        [Test]
        public void AddSC019IndustryPanel_RefusesExistingPartialControl()
        {
            Create(AddSC019IndustryPanel.RootName, _canvas);

            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.ExistingPartialControl));
            Assert.That(Find(AddSC019IndustryPanel.PanelName), Is.Null);
        }

        [Test]
        public void AddSC019IndustryPanel_RollsBackOnValidationFailure()
        {
            AddSC019IndustryPanel.UpdateResult result = AddSC019IndustryPanel.Update(_scene,
                root => root.name = "IndustryRootBroken");

            Assert.That(result, Is.EqualTo(AddSC019IndustryPanel.UpdateResult.ValidationFailed));
            Assert.That(Find(AddSC019IndustryPanel.RootName), Is.Null);
            Assert.That(Find(AddSC019IndustryPanel.PanelName), Is.Null);
        }

        [Test]
        public void AddSC019IndustryPanel_ValidateRejectsWrongSiblingOrder()
        {
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            Transform root = Find(AddSC019IndustryPanel.RootName);
            GameObject map = Create("M2WorldMapRoot", _canvas);
            map.transform.SetAsLastSibling();
            root.SetAsFirstSibling();

            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("above the map"));
        }

        [Test]
        public void AddSC019IndustryPanel_ValidateRejectsDuplicatePanel()
        {
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            GameObject duplicate = Create("IndustryPanelDuplicate", _canvas, typeof(IndustryPanel));

            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("exactly one"));
            Object.DestroyImmediate(duplicate);
        }

        [Test]
        public void AddSC019IndustryPanel_OpenButtonClearsSuccessionHud()
        {
            RectTransform hud = CreateSuccessionHud();

            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));

            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.True, error);
            var open = (RectTransform)Find(AddSC019IndustryPanel.OpenButtonName);
            Rect button = WorldBounds(open);
            Assert.That(button.Overlaps(WorldBounds(hud), true), Is.False,
                "the industry open button must not overlap the succession HUD");
            Rect canvas = WorldBounds((RectTransform)_canvas);
            Assert.That(canvas.Contains(button.min) && canvas.Contains(button.max), Is.True,
                "the industry open button must stay inside the canvas");
        }

        [Test]
        public void AddSC019IndustryPanel_ValidateRejectsOpenButtonOverSuccessionHud()
        {
            RectTransform hud = CreateSuccessionHud();
            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));

            var open = (RectTransform)Find(AddSC019IndustryPanel.OpenButtonName);
            open.anchorMin = open.anchorMax = new Vector2(0f, 1f);
            open.pivot = new Vector2(0f, 1f);
            open.anchoredPosition = new Vector2(20f, -20f);

            Assert.That(AddSC019IndustryPanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("succession HUD"));

            Assert.That(AddSC019IndustryPanel.Update(_scene),
                Is.EqualTo(AddSC019IndustryPanel.UpdateResult.Success));
            Assert.That(AddSC019IndustryPanel.Validate(_scene, out error), Is.True, error);
            Assert.That(WorldBounds(open).Overlaps(WorldBounds(hud), true), Is.False,
                "the transactional repair must move the button clear of the succession HUD");
        }

        private RectTransform CreateSuccessionHud()
        {
            GameObject hud = Create("SuccessionHud", _canvas, typeof(Image));
            var rect = (RectTransform)hud.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(430f, 210f);
            return rect;
        }

        private static Rect WorldBounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
