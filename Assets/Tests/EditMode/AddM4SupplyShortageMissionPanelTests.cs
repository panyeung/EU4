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
    public sealed class AddM4SupplyShortageMissionPanelTests
    {
        private Scene scene;
        private Scene previousScene;
        private bool previousDirty;
        private Transform town;
        private WorldMapController controller;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            previousDirty = previousScene.isDirty;
            scene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            GameObject map = Create("M2WorldMapRoot", canvas.transform, typeof(WorldMapController));
            map.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            town = Create("TownMenuRoot", map.transform).transform;
            ((RectTransform)town).sizeDelta = new Vector2(1216f, 268f);
            town.gameObject.SetActive(false);
            controller = map.GetComponent<WorldMapController>();
            session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            session.InitNewGame();
            SetReference(controller, "gameSession", session);
            SetReference(controller, "townMenuRoot", town.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
            Assert.That(previousScene.isDirty, Is.EqualTo(previousDirty), "Tests must preserve the user's active scene.");
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            int count = ObjectCount();
            Assert.That(AddM4SupplyShortageMissionPanel.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.SceneMismatch));
            Assert.That(AddM4SupplyShortageMissionPanel.CheckScenePreflight(AddM4SupplyShortageMissionPanel.ScenePath, true),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.SceneDirty));
            Assert.That(AddM4SupplyShortageMissionPanel.CheckScenePreflight(AddM4SupplyShortageMissionPanel.ScenePath, false),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [TestCase("Canvas", false)]
        [TestCase("Canvas", true)]
        [TestCase("M2WorldMapRoot", false)]
        [TestCase("M2WorldMapRoot", true)]
        [TestCase("TownMenuRoot", false)]
        [TestCase("TownMenuRoot", true)]
        public void Updater_RefusesMissingOrDuplicateRoots(string name, bool duplicate)
        {
            Transform target = All().Single(t => t.name == name);
            if (duplicate) Create(name, null);
            else Object.DestroyImmediate(target.gameObject);
            int count = ObjectCount();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Updater_BuildsAndBindsMissionPanelUnderTownRoot()
        {
            int count = ObjectCount();
            Assert.That(new SerializedObject(controller).FindProperty("supplyShortageMissionPanel").objectReferenceEntityIdValue,
                Is.EqualTo(EntityId.None), "An unassigned mission reference must use the EntityId.None sentinel.");
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Transform root = town.Find(AddM4SupplyShortageMissionPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.childCount, Is.EqualTo(8));
            Assert.That(root.GetComponentsInChildren<Text>(true).Length, Is.EqualTo(8));
            Assert.That(root.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(4));
            Assert.That(ObjectCount(), Is.EqualTo(count + 13));
            Assert.That(root.gameObject.activeSelf, Is.False);
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.EqualTo(root.GetComponent<SupplyShortageMissionPanel>()));
            Assert.That(new SerializedObject(controller).FindProperty("supplyShortageMissionPanel").objectReferenceEntityIdValue,
                Is.Not.EqualTo(EntityId.None), "The bound mission reference must have an entity ID.");
            Assert.That(GetReference(root.GetComponent<SupplyShortageMissionPanel>(), "gameSession"), Is.EqualTo(session));
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out string error), Is.True, error);
            Assert.That(scene.path, Is.Empty, "The public updater must never save the test scene.");
            int builtCount = ObjectCount();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount));
            Undo.PerformUndo();
            Assert.That(ObjectCount(), Is.EqualTo(count), "One Undo must remove the entire generated hierarchy.");
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.Null);
        }

        [TestCase("reference")]
        [TestCase("exception")]
        [TestCase("geometry")]
        public void Updater_RollsBackOnValidationFailure(string fault)
        {
            int count = ObjectCount();
            bool wasDirty = scene.isDirty;
            var result = AddM4SupplyShortageMissionPanel.Update(scene, root =>
            {
                if (fault == "exception") throw new System.InvalidOperationException("Injected construction failure");
                if (fault == "geometry") root.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5000f, 0f);
                else SetReference(root.GetComponent<SupplyShortageMissionPanel>(), "stageText", null);
            });
            Assert.That(result, Is.EqualTo(fault == "exception"
                ? AddM4SupplyShortageMissionPanel.UpdateResult.Failed
                : AddM4SupplyShortageMissionPanel.UpdateResult.ValidationFailed));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.Null);
            Assert.That(GetReference(controller, "gameSession"), Is.EqualTo(session));
            Assert.That(GetReference(controller, "townMenuRoot"), Is.EqualTo(town.gameObject));
            TestContext.WriteLine($"Rollback dirty state ({fault}): before={wasDirty}; after={scene.isDirty}");
            // Undo restores objects/references but Unity 6000.6 may leave the scene dirty.
            Assert.That(scene.path, Is.Empty, "Rollback must not save an unvalidated result to clear the dirty flag.");
        }

        [TestCase("GameSession")]
        [TestCase("WorldMapController")]
        public void Updater_RefusesDuplicateComponents(string component)
        {
            Create("Duplicate", null, component == "GameSession" ? typeof(GameSession) : typeof(WorldMapController));
            int count = ObjectCount();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Updater_RefusesUnboundSessionAndInsufficientSpace()
        {
            int count = ObjectCount();
            SetReference(controller, "gameSession", null);
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.InvalidHierarchyOrBindings));
            SetReference(controller, "gameSession", session);
            ((RectTransform)town).sizeDelta = new Vector2(300f, 200f);
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.InsufficientSpace));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Updater_RefusesExistingMissionObjects(bool componentOnly)
        {
            GameObject existing = Create(componentOnly ? "OtherMissionHost" : AddM4SupplyShortageMissionPanel.RootName, town);
            if (componentOnly) existing.AddComponent<SupplyShortageMissionPanel>();
            int count = ObjectCount();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene),
                Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.ExistingMissionPanel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(existing, Is.Not.Null, "Existing objects must not be replaced.");
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.Null);
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndExternalReferences()
        {
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out _), Is.False);
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Transform root = town.Find(AddM4SupplyShortageMissionPanel.RootName);
            SupplyShortageMissionPanel panel = root.GetComponent<SupplyShortageMissionPanel>();
            SetReference(panel, "stageText", Create("ExternalText", null, typeof(Text)).GetComponent<Text>());
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out _), Is.False);
            SetReference(panel, "stageText", root.Find("StageText").GetComponent<Text>());
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out _), Is.True);
            Create(AddM4SupplyShortageMissionPanel.RootName, town);
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out _), Is.False);
        }

        [Test]
        public void Updater_UsesUniqueMissionFeedbackNameWithoutChangingM2Feedback()
        {
            GameObject mapFeedback = Create("FeedbackText", controller.transform, typeof(Text));
            CreateM2ResolverNames();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Assert.That(WorldMapLayout.ResolveRequiredChildren(controller.transform, out _, out string error), Is.True, error);
            Assert.That(controller.GetComponentsInChildren<Transform>(true).Count(t => t.name == "FeedbackText"), Is.EqualTo(1));
            Assert.That(mapFeedback.name, Is.EqualTo("FeedbackText"));
            Transform root = town.Find(AddM4SupplyShortageMissionPanel.RootName);
            Assert.That(root.Find("MissionFeedbackText"), Is.Not.Null);
            Assert.That(GetReference(root.GetComponent<SupplyShortageMissionPanel>(), "feedbackText"),
                Is.EqualTo(root.Find("MissionFeedbackText").GetComponent<Text>()));
        }

        [Test]
        public void Updater_MigratesLegacyFeedbackNameOnceWithUndoAndWithoutSaving()
        {
            Text feedback = CreateLegacyPanel();
            int count = ObjectCount();
            SupplyShortageMissionPanel panel = feedback.GetComponentInParent<SupplyShortageMissionPanel>(true);
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out _), Is.False, "Legacy names require migration.");
            Assert.That(WorldMapLayout.ResolveRequiredChildren(controller.transform, out _, out _), Is.False, "Legacy feedback duplicates M2's required name.");
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Assert.That(feedback.name, Is.EqualTo("MissionFeedbackText"));
            Assert.That(GetReference(panel, "feedbackText"), Is.EqualTo(feedback));
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.EqualTo(panel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddM4SupplyShortageMissionPanel.Validate(scene, out string error), Is.True, error);
            Assert.That(WorldMapLayout.ResolveRequiredChildren(controller.transform, out _, out string layoutError), Is.True, layoutError);
            Assert.That(controller.GetComponentsInChildren<Transform>(true).Count(t => t.name == "FeedbackText"), Is.EqualTo(1));
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count), "A repeat update must be a no-op.");
            Assert.That(scene.path, Is.Empty, "The migration test seam must never save.");
            Undo.PerformUndo();
            Assert.That(feedback.name, Is.EqualTo("FeedbackText"), "One Undo must restore the legacy name.");
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(GetReference(panel, "feedbackText"), Is.EqualTo(feedback));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Updater_MigrationRollsBackRenameOnFailure(bool throwException)
        {
            Text feedback = CreateLegacyPanel();
            int count = ObjectCount();
            var result = AddM4SupplyShortageMissionPanel.Update(scene, root =>
            {
                if (throwException) throw new System.InvalidOperationException("Injected migration failure");
                root.transform.Find("MissionFeedbackText").gameObject.name = "InvalidFeedbackName";
            });
            Assert.That(result, Is.EqualTo(throwException ? AddM4SupplyShortageMissionPanel.UpdateResult.Failed
                : AddM4SupplyShortageMissionPanel.UpdateResult.ValidationFailed));
            Assert.That(feedback.name, Is.EqualTo("FeedbackText"));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(GetReference(feedback.GetComponentInParent<SupplyShortageMissionPanel>(true), "feedbackText"), Is.EqualTo(feedback));
            Assert.That(scene.path, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Updater_RefusesInvalidOrAmbiguousLegacyPanel(bool duplicate)
        {
            Text feedback = CreateLegacyPanel();
            SupplyShortageMissionPanel panel = feedback.GetComponentInParent<SupplyShortageMissionPanel>(true);
            if (duplicate) Create("FeedbackText", panel.transform, typeof(Text));
            else SetReference(panel, "feedbackText", null);
            int count = ObjectCount();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.ExistingMissionPanel));
            Assert.That(feedback.name, Is.EqualTo("FeedbackText"));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(scene.path, Is.Empty);
        }

        private Text CreateLegacyPanel()
        {
            CreateM2ResolverNames();
            Assert.That(AddM4SupplyShortageMissionPanel.Update(scene), Is.EqualTo(AddM4SupplyShortageMissionPanel.UpdateResult.Success));
            Text feedback = town.Find(AddM4SupplyShortageMissionPanel.RootName).Find("MissionFeedbackText").GetComponent<Text>();
            feedback.gameObject.name = "FeedbackText";
            return feedback;
        }

        private void CreateM2ResolverNames()
        {
            foreach (string name in WorldMapLayout.RequiredChildNames)
                if (!controller.GetComponentsInChildren<Transform>(true).Any(t => t.name == name))
                    Create(name, controller.transform);
        }

        private GameObject Create(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null) go.transform.SetParent(parent, false);
            foreach (System.Type type in components) go.AddComponent(type);
            return go;
        }

        private Transform[] All() => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        private int ObjectCount() => All().Length;
        private void AssertNoPanel()
        {
            Assert.That(All().Any(t => t.name == AddM4SupplyShortageMissionPanel.RootName || t.GetComponent<SupplyShortageMissionPanel>() != null), Is.False);
        }
        private static Object GetReference(Object target, string field) => new SerializedObject(target).FindProperty(field).objectReferenceValue;
        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
