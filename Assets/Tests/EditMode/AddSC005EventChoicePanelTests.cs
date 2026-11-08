using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class AddSC005EventChoicePanelTests
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

            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));

            Transform root = Find(AddSC005EventChoicePanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(Find(AddSC005EventChoicePanel.ModalName).gameObject.activeSelf, Is.False);
            Assert.That(ObjectCount(), Is.GreaterThan(count));
            Assert.That(AddSC005EventChoicePanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must not save or reload a scene.");
        }

        [Test]
        public void UpdaterIsIdempotentAndRollbackRemovesPartialRoot()
        {
            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));
            int completeCount = ObjectCount();
            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(completeCount));

            Scene rollbackScene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = CreateIn(rollbackScene, "Canvas", null, typeof(Canvas));
            GameObject sessionObject = CreateIn(rollbackScene, "GameSession", null, typeof(GameSession));
            GameSession session = sessionObject.GetComponent<GameSession>();
            session.InitNewGame(33UL);
            int rollbackCount = ObjectCount(rollbackScene);
            try
            {
                AddSC005EventChoicePanel.UpdateResult result = AddSC005EventChoicePanel.Update(rollbackScene, root =>
                {
                    EventChoicePanel panel = root.GetComponent<EventChoicePanel>();
                    panel.BindReferences(session, session.EventCatalog, session.EventText,
                        null, null, null, null, null, null);
                });
                Assert.That(result, Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.ValidationFailed));
                Assert.That(Named(rollbackScene, AddSC005EventChoicePanel.RootName).Count(), Is.Zero);
                Assert.That(ObjectCount(rollbackScene), Is.EqualTo(rollbackCount));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(rollbackScene);
            }
        }

        [Test]
        public void UpdaterPreflightRefusesWrongAndDirtyScenes()
        {
            Assert.That(AddSC005EventChoicePanel.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.SceneMismatch));
            Assert.That(AddSC005EventChoicePanel.CheckScenePreflight(AddSC005EventChoicePanel.ScenePath, true),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.SceneDirty));
            Assert.That(AddSC005EventChoicePanel.CheckScenePreflight(AddSC005EventChoicePanel.ScenePath, false),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));
            Assert.That(AddSC005EventChoicePanel.CheckScenePreflight(AddSC005EventChoicePanel.ScenePath, false, true),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.EditorPlaying));
        }

        [Test]
        public void GeneratedStoryCardKeepsEveryRegionInsideTheModal()
        {
            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));
            Canvas.ForceUpdateCanvases();

            RectTransform modal = (RectTransform)Find(AddSC005EventChoicePanel.ModalName);
            foreach (string childName in new[]
            {
                AddSC005EventChoicePanel.TitleName,
                AddSC005EventChoicePanel.BodyName,
                AddSC005EventChoicePanel.OptionsName
            })
            {
                RectTransform child = (RectTransform)Find(childName);
                Rect modalBounds = LocalBounds(modal, modal);
                Rect childBounds = LocalBounds(child, modal);
                Assert.That(modalBounds.Contains(new Vector2(childBounds.xMin, childBounds.yMin)),
                    Is.True, childName + " must stay inside the story card");
                Assert.That(modalBounds.Contains(new Vector2(childBounds.xMax, childBounds.yMax)),
                    Is.True, childName + " must stay inside the story card");
            }

            Rect title = LocalBounds((RectTransform)Find(AddSC005EventChoicePanel.TitleName), null);
            Rect body = LocalBounds((RectTransform)Find(AddSC005EventChoicePanel.BodyName), null);
            Rect options = LocalBounds((RectTransform)Find(AddSC005EventChoicePanel.OptionsName), null);
            Assert.That(title.Overlaps(body, true), Is.False);
            Assert.That(body.Overlaps(options, true), Is.False);
            Assert.That(title.center.y, Is.GreaterThan(body.center.y),
                "the card stacks title above story above choices");
        }

        private static Rect LocalBounds(RectTransform target, RectTransform coordinateRoot)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = coordinateRoot != null
                    ? coordinateRoot.InverseTransformPoint(corner)
                    : corner;
                xMin = Mathf.Min(xMin, local.x);
                xMax = Mathf.Max(xMax, local.x);
                yMin = Mathf.Min(yMin, local.y);
                yMax = Mathf.Max(yMax, local.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        [Test]
        public void FeedbackBannerClearsTheMapActionRowBand()
        {
            // FoundationScene keeps the map confirm/cancel row at canvas y=-322.
            // The legacy banner used exactly that centre-anchored band, so the
            // post-choice relationship feedback was drawn across live controls.
            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));

            RectTransform root = (RectTransform)Find(AddSC005EventChoicePanel.RootName);
            var feedback = (RectTransform)Find(AddSC005EventChoicePanel.FeedbackName);
            Assert.That(root, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);

            RectTransform mapActionRow = CreateRect("MapActionRowProbe", root);
            mapActionRow.anchorMin = mapActionRow.anchorMax = mapActionRow.pivot = new Vector2(0.5f, 0.5f);
            mapActionRow.anchoredPosition = new Vector2(0f, -322f);
            mapActionRow.sizeDelta = new Vector2(1180f, 48f);

            Rect feedbackBounds = LocalRect(root, feedback);
            Rect mapRowBounds = LocalRect(root, mapActionRow);
            Assert.That(feedbackBounds.Overlaps(mapRowBounds), Is.False,
                $"Feedback {feedbackBounds} must not cover the map action row {mapRowBounds}.");
            Assert.That(feedbackBounds.yMin, Is.GreaterThan(mapRowBounds.yMax),
                "Feedback must stay above the map action row instead of across it.");
            Assert.That(feedback.GetComponent<Text>().raycastTarget, Is.False);
        }

        [Test]
        public void UpdaterMovesLegacyFeedbackBannerWithoutRebuildingThePanel()
        {
            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));
            var feedback = (RectTransform)Find(AddSC005EventChoicePanel.FeedbackName);
            Text text = feedback.GetComponent<Text>();
            int completeCount = ObjectCount();

            // Reproduce the layout serialized in FoundationScene by the previous updater.
            feedback.anchorMin = feedback.anchorMax = feedback.pivot = new Vector2(0.5f, 0.5f);
            feedback.anchoredPosition = new Vector2(0f, -322f);
            feedback.sizeDelta = new Vector2(1180f, 42f);
            text.alignment = TextAnchor.MiddleCenter;

            Assert.That(AddSC005EventChoicePanel.Update(_scene),
                Is.EqualTo(AddSC005EventChoicePanel.UpdateResult.Success));

            Assert.That(feedback.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(feedback.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(feedback.pivot, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(feedback.anchoredPosition,
                Is.EqualTo(new Vector2(0f, -EventChoicePanel.FeedbackTopMargin)));
            Assert.That(text.alignment, Is.EqualTo(TextAnchor.UpperCenter));
            Assert.That(ObjectCount(), Is.EqualTo(completeCount),
                "Repairing the banner must not rebuild the generated panel.");
            Assert.That(AddSC005EventChoicePanel.Validate(_scene, out string error), Is.True, error);
        }

        private RectTransform CreateRect(string name, Transform parent) =>
            CreateIn(_scene, name, parent, System.Array.Empty<System.Type>())
                .GetComponent<RectTransform>();

        private static Rect LocalRect(RectTransform coordinateRoot, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.PositiveInfinity;
            float yMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMax = float.NegativeInfinity;
            foreach (Vector3 corner in corners)
            {
                Vector3 local = coordinateRoot.InverseTransformPoint(corner);
                xMin = Mathf.Min(xMin, local.x);
                yMin = Mathf.Min(yMin, local.y);
                xMax = Mathf.Max(xMax, local.x);
                yMax = Mathf.Max(yMax, local.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private GameObject Create(string name, Transform parent, params System.Type[] components) =>
            CreateIn(_scene, name, parent, components);

        private static GameObject CreateIn(Scene scene, string name, Transform parent, params System.Type[] components)
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
