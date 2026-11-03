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
    /// <summary>
    /// SC-012 Task 4: the transactional FoundationScene updater creates one fixed
    /// chronicle root, is idempotent, and rolls the whole Undo group back when
    /// validation fails.
    /// </summary>
    public sealed class AddSC012FamilyChroniclePanelTests
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

        [Test]
        public void UpdaterAddsValidatedPanelUnderOneFixedRoot()
        {
            int count = ObjectCount();

            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));

            Transform root = Find(AddSC012FamilyChroniclePanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(root.GetComponent<FamilyChroniclePanel>(), Is.Not.Null);
            Assert.That(Find(AddSC012FamilyChroniclePanel.OpenButtonName).gameObject.activeSelf, Is.True);
            Assert.That(Find(AddSC012FamilyChroniclePanel.PanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC012FamilyChroniclePanel.RowTemplateName).gameObject.activeSelf, Is.False);
            Assert.That(ObjectCount(), Is.GreaterThan(count));
            Assert.That(AddSC012FamilyChroniclePanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must not save or reload a scene.");
        }

        [Test]
        public void UpdaterIsIdempotentAndRollbackRemovesPartialRoot()
        {
            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));
            int completeCount = ObjectCount();
            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(completeCount),
                "a second run must not add a second root or duplicate UI");

            Scene rollbackScene = EditorSceneManager.NewPreviewScene();
            CreateIn(rollbackScene, "Canvas", null, typeof(Canvas));
            GameObject sessionObject = CreateIn(rollbackScene, "GameSession", null, typeof(GameSession));
            sessionObject.GetComponent<GameSession>().InitNewGame(33UL);
            int rollbackCount = ObjectCount(rollbackScene);
            try
            {
                AddSC012FamilyChroniclePanel.UpdateResult result =
                    AddSC012FamilyChroniclePanel.Update(rollbackScene, root =>
                    {
                        root.GetComponent<FamilyChroniclePanel>().BindReferences(null, null, null, null,
                            null, null, null, null, null);
                    });

                Assert.That(result, Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.ValidationFailed));
                Assert.That(Named(rollbackScene, AddSC012FamilyChroniclePanel.RootName).Count(), Is.Zero);
                Assert.That(ObjectCount(rollbackScene), Is.EqualTo(rollbackCount));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(rollbackScene);
            }
        }

        [Test]
        public void UpdaterPreflightRefusesWrongDirtyAndPlayingScenes()
        {
            Assert.That(AddSC012FamilyChroniclePanel.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.SceneMismatch));
            Assert.That(AddSC012FamilyChroniclePanel.CheckScenePreflight(
                AddSC012FamilyChroniclePanel.ScenePath, true),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.SceneDirty));
            Assert.That(AddSC012FamilyChroniclePanel.CheckScenePreflight(
                AddSC012FamilyChroniclePanel.ScenePath, false),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));
            Assert.That(AddSC012FamilyChroniclePanel.CheckScenePreflight(
                AddSC012FamilyChroniclePanel.ScenePath, false, true),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.EditorPlaying));
        }

        [Test]
        public void UpdaterPlacesRootAboveMapAndBelowTitleAndRejectsPriorOverlap()
        {
            Create("M2WorldMapRoot", _canvas);
            Transform title = Create("M6TitleRoot", _canvas).transform;

            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));

            Transform root = Find(AddSC012FamilyChroniclePanel.RootName);
            Transform map = Find("M2WorldMapRoot");
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(map.GetSiblingIndex()),
                "FamilyChronicleRoot must render above the map/town HUD.");
            Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()),
                "FamilyChronicleRoot must remain below the full-screen title overlay.");
            Assert.That(AddSC012FamilyChroniclePanel.Validate(_scene, out string error), Is.True, error);

            root.SetAsFirstSibling();
            Assert.That(AddSC012FamilyChroniclePanel.Validate(_scene, out string overlapError), Is.False);
            Assert.That(overlapError, Does.Contain("above the map"));
        }

        [Test]
        public void ValidatorRejectsBrokenScrollContentRelation()
        {
            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));

            Transform content = Find(AddSC012FamilyChroniclePanel.ContentName);
            content.SetParent(_canvas, false);

            Assert.That(AddSC012FamilyChroniclePanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void ValidateRejectsDuplicateRoot()
        {
            Assert.That(AddSC012FamilyChroniclePanel.Update(_scene),
                Is.EqualTo(AddSC012FamilyChroniclePanel.UpdateResult.Success));
            Create(AddSC012FamilyChroniclePanel.RootName, _canvas);

            Assert.That(AddSC012FamilyChroniclePanel.Validate(_scene, out string error), Is.False);
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

        private int ObjectCount() => ObjectCount(_scene);

        private static int ObjectCount(Scene scene) => scene.GetRootGameObjects()
            .Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
    }
}
