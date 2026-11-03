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
    public sealed class AddSC013FamilyRosterPanelTests
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
        public void Updater_CreatesBoundScrollableGraphLayers()
        {
            int count = ObjectCount();

            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));

            Transform root = Find(AddSC013FamilyRosterPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(root.GetComponent<FamilyRosterPanel>(), Is.Not.Null);
            Assert.That(root.GetComponent<M2EndingPanel>(), Is.Not.Null);
            Assert.That(Find(AddSC013FamilyRosterPanel.PanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC013FamilyRosterPanel.EndingPanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC013FamilyRosterPanel.NodeTemplateName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddSC013FamilyRosterPanel.EdgeTemplateName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddSC013FamilyRosterPanel.SectionTemplateName).gameObject.activeSelf,
                Is.False);

            Transform scroll = Find(AddSC013FamilyRosterPanel.ScrollName);
            Transform content = Find(AddSC013FamilyRosterPanel.ContentName);
            Transform edgeLayer = Find(AddSC013FamilyRosterPanel.EdgeLayerName);
            Transform nodeLayer = Find(AddSC013FamilyRosterPanel.NodeLayerName);
            Assert.That(content.parent, Is.EqualTo(scroll));
            Assert.That(edgeLayer.parent, Is.EqualTo(content));
            Assert.That(nodeLayer.parent, Is.EqualTo(content));
            Assert.That(Find(AddSC013FamilyRosterPanel.NodeTemplateName).parent,
                Is.EqualTo(nodeLayer));
            Assert.That(Find(AddSC013FamilyRosterPanel.EdgeTemplateName).parent,
                Is.EqualTo(edgeLayer));

            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            Assert.That(scrollRect, Is.Not.Null);
            Assert.That(scrollRect.content, Is.EqualTo((RectTransform)content));
            Assert.That(scrollRect.viewport, Is.EqualTo((RectTransform)scroll));
            Assert.That(scrollRect.vertical, Is.True);
            Assert.That(scrollRect.horizontal, Is.True);
            Assert.That(scroll.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(content.GetComponent<VerticalLayoutGroup>(), Is.Null,
                "the graph uses deterministic manual positions, not a vertical row list");

            FamilyRosterPanel roster = root.GetComponent<FamilyRosterPanel>();
            Assert.That(roster.IsBound, Is.True);
            Assert.That(AddSC013FamilyRosterPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(ObjectCount(), Is.GreaterThan(count));
            Assert.That(_scene.path, Is.Empty, "The test seam must not save or reload a scene.");
        }

        [Test]
        public void Updater_IsIdempotentAndRollsBackInvalidHierarchy()
        {
            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));
            int completeCount = ObjectCount();
            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(completeCount));

            Scene rollbackScene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = CreateIn(rollbackScene, "Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            GameObject sessionObject = CreateIn(rollbackScene, "GameSession", null,
                typeof(GameSession));
            GameSession session = sessionObject.GetComponent<GameSession>();
            session.InitNewGame(33UL);
            int rollbackCount = ObjectCount(rollbackScene);
            try
            {
                AddSC013FamilyRosterPanel.UpdateResult result =
                    AddSC013FamilyRosterPanel.Update(rollbackScene, root =>
                    {
                        FamilyRosterPanel panel = root.GetComponent<FamilyRosterPanel>();
                        panel.BindReferences(null, null, null, null, null, null, null, null,
                            null, null, null, null, null, null);
                    });
                Assert.That(result,
                    Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.ValidationFailed));
                Assert.That(Named(rollbackScene, AddSC013FamilyRosterPanel.RootName).Count(),
                    Is.Zero);
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
            Assert.That(AddSC013FamilyRosterPanel.CheckScenePreflight(
                    "Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.SceneMismatch));
            Assert.That(AddSC013FamilyRosterPanel.CheckScenePreflight(
                    AddSC013FamilyRosterPanel.ScenePath, true),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.SceneDirty));
            Assert.That(AddSC013FamilyRosterPanel.CheckScenePreflight(
                    AddSC013FamilyRosterPanel.ScenePath, false),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));
            Assert.That(AddSC013FamilyRosterPanel.CheckScenePreflight(
                    AddSC013FamilyRosterPanel.ScenePath, false, true),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.EditorPlaying));
        }

        [Test]
        public void RosterPanelRendersGraphAndKeepsEndingHidden()
        {
            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));
            FamilyRosterPanel roster = Find(AddSC013FamilyRosterPanel.RootName)
                .GetComponent<FamilyRosterPanel>();
            M2EndingPanel ending = Find(AddSC013FamilyRosterPanel.RootName)
                .GetComponent<M2EndingPanel>();

            roster.Open();
            ending.Refresh();

            Assert.That(roster.VisibleNodeCount, Is.EqualTo(6));
            Assert.That(roster.VisibleRelationCount, Is.EqualTo(5));
            Assert.That(roster.KinNodeCount, Is.EqualTo(5));
            Assert.That(roster.HouseholdNodeCount, Is.EqualTo(1));
            Assert.That(roster.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("明智 晴信"));
            Assert.That(roster.NodeTextFor(M2FixedScenario.MasatsunaId),
                Does.Contain("指定继承人"));
            Assert.That(roster.NodeTextFor(M2FixedScenario.SakonId), Does.Contain("佐近"));
            Assert.That(roster.HasSectionContaining("家臣"), Is.True);
            Assert.That(roster.HeirSummary, Does.Contain("明智 正綱"));
            Assert.That(ending.IsVisible, Is.False);
        }

        [Test]
        public void RosterScrollAreaDoesNotOverlapControls()
        {
            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));

            RectTransform title = (RectTransform)Find(AddSC013FamilyRosterPanel.TitleName);
            RectTransform legend = (RectTransform)Find(AddSC013FamilyRosterPanel.LegendName);
            RectTransform scroll = (RectTransform)Find(AddSC013FamilyRosterPanel.ScrollName);
            RectTransform heir = (RectTransform)Find(AddSC013FamilyRosterPanel.HeirName);
            RectTransform close = (RectTransform)Find(AddSC013FamilyRosterPanel.CloseButtonName);

            Assert.That(Bottom(title), Is.GreaterThan(Top(legend)));
            Assert.That(Bottom(legend), Is.GreaterThan(Top(scroll)));
            Assert.That(Bottom(scroll), Is.GreaterThan(Top(heir)));
            Assert.That(Bottom(heir), Is.GreaterThan(Top(close)));
            Assert.That(scroll.sizeDelta.y, Is.GreaterThan(0f));
        }

        [Test]
        public void UpdaterUpgradesLegacyRowHierarchyInsideOneTransaction()
        {
            CreateLegacyHierarchy();

            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));

            Assert.That(Named(_scene, AddSC013FamilyRosterPanel.RootName).Count(), Is.EqualTo(1));
            Assert.That(Find("FamilyRosterRowTemplate"), Is.Null);
            Assert.That(Find(AddSC013FamilyRosterPanel.ScrollName), Is.Not.Null);
            Assert.That(Find(AddSC013FamilyRosterPanel.NodeLayerName), Is.Not.Null);
            Assert.That(AddSC013FamilyRosterPanel.Validate(_scene, out string error), Is.True, error);
        }

        [Test]
        public void UpdaterRefusesLegacyRootWithUnrecognizedChild()
        {
            CreateLegacyHierarchy();
            Transform content = Find(AddSC013FamilyRosterPanel.ContentName);
            GameObject note = Create("UserFamilyNote", content);
            int count = ObjectCount();

            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.ExistingPartialControl));

            Assert.That(Find("UserFamilyNote"), Is.EqualTo(note.transform));
            Assert.That(note.transform.parent, Is.EqualTo(content));
            Assert.That(Find("FamilyRosterRowTemplate"), Is.Not.Null,
                "the refused legacy row template must remain untouched");
            Assert.That(Find(AddSC013FamilyRosterPanel.ScrollName), Is.Null);
            Assert.That(Find(AddSC013FamilyRosterPanel.NodeLayerName), Is.Null);
            Assert.That(Named(_scene, AddSC013FamilyRosterPanel.RootName).Count(), Is.EqualTo(1));
            Assert.That(ObjectCount(), Is.EqualTo(count));
        }

        [Test]
        public void UpdaterRefusesLegacyRootWithUnrecognizedComponent()
        {
            CreateLegacyHierarchy();
            Transform title = Find(AddSC013FamilyRosterPanel.TitleName);
            Outline outline = title.gameObject.AddComponent<Outline>();
            int count = ObjectCount();

            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.ExistingPartialControl));

            Assert.That(title.GetComponent<Outline>(), Is.SameAs(outline));
            Assert.That(Find("FamilyRosterRowTemplate"), Is.Not.Null);
            Assert.That(Find(AddSC013FamilyRosterPanel.ScrollName), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count));
        }

        [Test]
        public void Validate_RejectsMissingOrDuplicateGraphBindings()
        {
            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));

            Transform nodeLayer = Find(AddSC013FamilyRosterPanel.NodeLayerName);
            GameObject duplicate = CreateIn(_scene, AddSC013FamilyRosterPanel.NodeTemplateName,
                nodeLayer, typeof(Image));
            Assert.That(AddSC013FamilyRosterPanel.Validate(_scene, out string duplicateError),
                Is.False);
            Assert.That(duplicateError, Does.Contain("incomplete or ambiguous"));
            Object.DestroyImmediate(duplicate);

            Transform edgeLayer = Find(AddSC013FamilyRosterPanel.EdgeLayerName);
            Object.DestroyImmediate(edgeLayer.gameObject);
            Assert.That(AddSC013FamilyRosterPanel.Validate(_scene, out string missingError),
                Is.False);
            Assert.That(missingError, Does.Contain("incomplete or ambiguous"));
        }

        [Test]
        public void UpdaterPlacesRootAboveMapAndBelowTitleAndEventOverlays()
        {
            Create("M2WorldMapRoot", _canvas);
            Transform title = Create("M6TitleRoot", _canvas).transform;
            Transform events = Create("EventChoiceRoot", _canvas).transform;

            Assert.That(AddSC013FamilyRosterPanel.Update(_scene),
                Is.EqualTo(AddSC013FamilyRosterPanel.UpdateResult.Success));
            Transform root = Find(AddSC013FamilyRosterPanel.RootName);
            Transform map = Find("M2WorldMapRoot");

            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(map.GetSiblingIndex()));
            Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()));
            Assert.That(root.GetSiblingIndex(), Is.LessThan(events.GetSiblingIndex()));
            Assert.That(AddSC013FamilyRosterPanel.Validate(_scene, out string error), Is.True, error);
        }

        private void CreateLegacyHierarchy()
        {
            GameObject root = Create("FamilyRosterRoot", _canvas);
            root.AddComponent<FamilyRosterPanel>();
            root.AddComponent<M2EndingPanel>();
            GameObject open = Create("FamilyRosterOpenButton", root.transform, typeof(Image),
                typeof(Button), typeof(LayoutElement));
            Create("Label", open.transform, typeof(Text));
            GameObject panel = Create("FamilyRosterPanel", root.transform, typeof(Image));
            Create("FamilyRosterTitleText", panel.transform, typeof(Text));
            GameObject content = Create("FamilyRosterContent", panel.transform,
                typeof(VerticalLayoutGroup));
            Create("FamilyRosterRowTemplate", content.transform, typeof(Text),
                typeof(LayoutElement));
            Create("FamilyRosterHeirText", panel.transform, typeof(Text));
            GameObject close = Create("FamilyRosterCloseButton", panel.transform, typeof(Image),
                typeof(Button), typeof(LayoutElement));
            Create("Label", close.transform, typeof(Text));
            GameObject ending = Create("M2EndingPanel", root.transform, typeof(Image));
            Create("M2EndingTitleText", ending.transform, typeof(Text));
            Create("M2EndingBodyText", ending.transform, typeof(Text));
            GameObject endingClose = Create("M2EndingCloseButton", ending.transform,
                typeof(Image), typeof(Button), typeof(LayoutElement));
            Create("Label", endingClose.transform, typeof(Text));
        }

        private static float Top(RectTransform rect) =>
            rect.anchoredPosition.y + rect.sizeDelta.y / 2f;

        private static float Bottom(RectTransform rect) =>
            rect.anchoredPosition.y - rect.sizeDelta.y / 2f;

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
