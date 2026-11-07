using System;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M03 transactional scene-generator coverage, kept separate from the panel
    /// projection tests: preflight guards, idempotent integration, binding validation and
    /// Undo rollback without reloading or saving the supplied scene.
    /// </summary>
    public sealed class AddM03NpcAiInspectorTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            GameSession session = Create("GameSession", null, typeof(GameSession))
                .GetComponent<GameSession>();
            session.InitNewGame();
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
        public void Update_CreatesOneValidRootAndSecondRunIsIdempotent()
        {
            Assert.That(AddM03NpcAiInspector.Update(_scene),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.Success));
            Assert.That(AddM03NpcAiInspector.Validate(_scene, out string error), Is.True, error);
            Assert.That(FindAll(AddM03NpcAiInspector.RootName).Length, Is.EqualTo(1));

            Assert.That(AddM03NpcAiInspector.Update(_scene),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.Success));
            Assert.That(FindAll(AddM03NpcAiInspector.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM03NpcAiInspector.OpenButtonName).Length, Is.EqualTo(1));
        }

        [Test]
        public void Update_FaultInjectionRollsBackTheWholeUndoGroup()
        {
            AddM03NpcAiInspector.UpdateResult result = AddM03NpcAiInspector.Update(_scene,
                root =>
                {
                    Transform open = root.transform.Find(AddM03NpcAiInspector.OpenButtonName);
                    Assert.That(open, Is.Not.Null);
                    open.name = "BrokenM03OpenButton";
                });

            Assert.That(result, Is.EqualTo(AddM03NpcAiInspector.UpdateResult.ValidationFailed));
            Assert.That(FindAll(AddM03NpcAiInspector.RootName), Is.Empty);
            Assert.That(FindAll(AddM03NpcAiInspector.PanelName), Is.Empty);
            Assert.That(_scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<NpcAiInspectorPanel>(true))
                    .ToArray(),
                Is.Empty);
        }

        [Test]
        public void Update_RejectsMissingGameSession()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject canvas = CreateIn(scene, "Canvas", null, typeof(Canvas));
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);

                Assert.That(AddM03NpcAiInspector.Update(scene),
                    Is.EqualTo(AddM03NpcAiInspector.UpdateResult.MissingOrDuplicateObjects));
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Preflight_RejectsWrongSceneDirtyAndPlaying()
        {
            Assert.That(AddM03NpcAiInspector.CheckScenePreflight("Assets/Other.unity", false),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.SceneMismatch));
            Assert.That(AddM03NpcAiInspector.CheckScenePreflight(
                AddM03NpcAiInspector.ScenePath, true),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.SceneDirty));
            Assert.That(AddM03NpcAiInspector.CheckScenePreflight(
                AddM03NpcAiInspector.ScenePath, false, true),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.EditorPlaying));
            Assert.That(AddM03NpcAiInspector.CheckScenePreflight(
                AddM03NpcAiInspector.ScenePath, false),
                Is.EqualTo(AddM03NpcAiInspector.UpdateResult.Success));
        }

        private GameObject Create(string name, Transform parent, params Type[] components) =>
            CreateIn(_scene, name, parent, components);

        private static GameObject CreateIn(Scene scene, string name, Transform parent,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type component in components)
                go.AddComponent(component);
            return go;
        }

        private Transform[] FindAll(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name).ToArray();
    }
}
