using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Structural tests for the transactional M05 commission-page generator. Every
    /// fixture is an isolated preview scene, so the user's FoundationScene is never
    /// saved, replaced or reloaded.
    /// </summary>
    public sealed class AddM5ShadowEscortCommissionPanelTests
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
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
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
            if (scene.IsValid())
                EditorSceneManager.ClosePreviewScene(scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
            Assert.That(previousScene.isDirty, Is.EqualTo(previousDirty),
                "Tests must preserve the user's active scene.");
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            int count = ObjectCount();
            Assert.That(AddM5ShadowEscortCommissionPanel.CheckScenePreflight(
                    "Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.SceneMismatch));
            Assert.That(AddM5ShadowEscortCommissionPanel.CheckScenePreflight(
                    AddM5ShadowEscortCommissionPanel.ScenePath, true),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.SceneDirty));
            Assert.That(AddM5ShadowEscortCommissionPanel.CheckScenePreflight(
                    AddM5ShadowEscortCommissionPanel.ScenePath, false),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));
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
            if (duplicate)
                Create(name, null);
            else
                Object.DestroyImmediate(target.gameObject);
            int count = ObjectCount();
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Updater_BuildsBindsAndUndoesTheWholePage()
        {
            int count = ObjectCount();
            Assert.That(new SerializedObject(controller)
                    .FindProperty("shadowEscortCommissionPanel").objectReferenceEntityIdValue,
                Is.EqualTo(EntityId.None));

            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));

            Transform root = town.Find(AddM5ShadowEscortCommissionPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(town));
            ShadowEscortCommissionPanel panel = root.GetComponent<ShadowEscortCommissionPanel>();
            Assert.That(panel, Is.Not.Null);
            Transform page = root.Find(AddM5ShadowEscortCommissionPanel.PanelName);
            Transform open = root.Find(AddM5ShadowEscortCommissionPanel.OpenButtonName);
            Assert.That(page, Is.Not.Null);
            Assert.That(open, Is.Not.Null);
            Assert.That(page.gameObject.activeSelf, Is.False, "The page must start hidden.");
            Assert.That(open.gameObject.activeSelf, Is.True);
            Assert.That(root.GetComponentsInChildren<Text>(true).Length, Is.EqualTo(18));
            Assert.That(root.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(4),
                "three controls plus the inactive candidate template");
            Assert.That(ObjectCount(), Is.EqualTo(count + 27));
            Assert.That(GetReference(controller, "shadowEscortCommissionPanel"), Is.EqualTo(panel));
            Assert.That(new SerializedObject(controller)
                    .FindProperty("shadowEscortCommissionPanel").objectReferenceEntityIdValue,
                Is.Not.EqualTo(EntityId.None));
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(session));
            Assert.That(GetReference(panel, "panelRoot"), Is.EqualTo(page.gameObject));
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out string error),
                Is.True, error);
            Assert.That(scene.path, Is.Empty, "The public updater must never save the test scene.");

            int builtCount = ObjectCount();
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount), "A repeat update must be a no-op.");

            Undo.PerformUndo();
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "One Undo must remove the entire generated hierarchy.");
            Assert.That(GetReference(controller, "shadowEscortCommissionPanel"), Is.Null);
        }

        [TestCase("reference")]
        [TestCase("exception")]
        [TestCase("geometry")]
        public void Updater_RollsBackOnValidationFailure(string fault)
        {
            int count = ObjectCount();
            bool wasDirty = scene.isDirty;
            var result = AddM5ShadowEscortCommissionPanel.Update(scene, root =>
            {
                if (fault == "exception")
                    throw new System.InvalidOperationException("Injected construction failure");
                if (fault == "geometry")
                    root.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5000f, 0f);
                else
                    SetReference(root.GetComponent<ShadowEscortCommissionPanel>(), "titleText", null);
            });

            Assert.That(result, Is.EqualTo(fault == "exception"
                ? AddM5ShadowEscortCommissionPanel.UpdateResult.Failed
                : AddM5ShadowEscortCommissionPanel.UpdateResult.ValidationFailed));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
            Assert.That(GetReference(controller, "shadowEscortCommissionPanel"), Is.Null);
            Assert.That(GetReference(controller, "gameSession"), Is.EqualTo(session));
            Assert.That(GetReference(controller, "townMenuRoot"), Is.EqualTo(town.gameObject));
            TestContext.WriteLine($"Rollback dirty state ({fault}): before={wasDirty}; after={scene.isDirty}");
            Assert.That(scene.path, Is.Empty,
                "Rollback must not save an unvalidated result to clear the dirty flag.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Updater_RefusesExistingCommissionObjects(bool componentOnly)
        {
            GameObject existing = Create(componentOnly
                ? "OtherCommissionHost"
                : AddM5ShadowEscortCommissionPanel.RootName, town);
            if (componentOnly)
                existing.AddComponent<ShadowEscortCommissionPanel>();
            int count = ObjectCount();
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.ExistingCommissionPanel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(existing, Is.Not.Null);
            Assert.That(GetReference(controller, "shadowEscortCommissionPanel"), Is.Null);
        }

        [Test]
        public void Updater_RefusesUnboundSessionAndInsufficientSpace()
        {
            int count = ObjectCount();
            SetReference(controller, "gameSession", null);
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.InvalidHierarchyOrBindings));
            SetReference(controller, "gameSession", session);
            ((RectTransform)town).sizeDelta = new Vector2(300f, 200f);
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.InsufficientSpace));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [TestCase("GameSession")]
        [TestCase("WorldMapController")]
        public void Updater_RefusesDuplicateComponents(string component)
        {
            Create("Duplicate", null,
                component == "GameSession" ? typeof(GameSession) : typeof(WorldMapController));
            int count = ObjectCount();
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndExternalReferences()
        {
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out _), Is.False);
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));
            Transform root = town.Find(AddM5ShadowEscortCommissionPanel.RootName);
            Transform page = root.Find(AddM5ShadowEscortCommissionPanel.PanelName);
            ShadowEscortCommissionPanel panel = root.GetComponent<ShadowEscortCommissionPanel>();

            SetReference(panel, "titleText",
                Create("ExternalText", null, typeof(Text)).GetComponent<Text>());
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out _), Is.False,
                "An external title reference must be rejected.");
            SetReference(panel, "titleText",
                page.Find(AddM5ShadowEscortCommissionPanel.TitleTextName).GetComponent<Text>());
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out _), Is.True);

            Transform template = page
                .Find(AddM5ShadowEscortCommissionPanel.CandidateScrollName)
                .Find(AddM5ShadowEscortCommissionPanel.CandidateViewportName)
                .Find(AddM5ShadowEscortCommissionPanel.CandidateContentName)
                .Find(AddM5ShadowEscortCommissionPanel.CandidateTemplateName);
            template.gameObject.SetActive(true);
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out _), Is.False,
                "An active candidate template must be rejected.");
            template.gameObject.SetActive(false);
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out _), Is.True);

            Create(AddM5ShadowEscortCommissionPanel.RootName, town);
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out _), Is.False,
                "A duplicate root must be rejected.");
        }

        [Test]
        public void Updater_LeavesExistingTownControlsAndSupplyBindingUntouched()
        {
            GameObject title = Create("TownMenuTitleText", town, typeof(Text));
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 32f);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 60f);
            GameObject back = Create("ReturnToMapButton", town, typeof(Button), typeof(Image));
            back.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 52f);
            back.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);
            int count = ObjectCount();

            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));
            Assert.That(AddM5ShadowEscortCommissionPanel.Validate(scene, out string error),
                Is.True, error);
            Assert.That(title.transform.parent, Is.EqualTo(town));
            Assert.That(back.transform.parent, Is.EqualTo(town));
            Assert.That(title.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(new Vector2(0f, 60f)));
            Assert.That(back.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(new Vector2(0f, -80f)));
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.Null,
                "The existing mission binding must stay untouched.");
            Assert.That(ObjectCount(), Is.GreaterThan(count));
        }

        [Test]
        public void GeneratedPage_FitsReferenceAndNarrowTown_WithAccessibleControls()
        {
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));
            Transform root = town.Find(AddM5ShadowEscortCommissionPanel.RootName);
            ShadowEscortCommissionPanel panel = root.GetComponent<ShadowEscortCommissionPanel>();
            RectTransform page = (RectTransform)root.Find(AddM5ShadowEscortCommissionPanel.PanelName);
            Transform pageTransform = root.Find(AddM5ShadowEscortCommissionPanel.PanelName);
            Button accept = pageTransform
                .Find(AddM5ShadowEscortCommissionPanel.AcceptButtonName).GetComponent<Button>();
            Button close = pageTransform
                .Find(AddM5ShadowEscortCommissionPanel.CloseButtonName).GetComponent<Button>();
            Text title = pageTransform
                .Find(AddM5ShadowEscortCommissionPanel.TitleTextName).GetComponent<Text>();
            Text body = pageTransform
                .Find(AddM5ShadowEscortCommissionPanel.OfferTextName).GetComponent<Text>();
            Text chronicle = pageTransform
                .Find(AddM5ShadowEscortCommissionPanel.ChronicleTextName).GetComponent<Text>();

            panel.Refresh();
            Assert.That(page.localScale.x, Is.EqualTo(1f).Within(0.0001f),
                "the 1280x720 reference town must keep the authored layout.");
            Assert.That(Contained(page, (RectTransform)town), Is.True);
            Assert.That(WorldSize((RectTransform)accept.transform).x, Is.EqualTo(240f).Within(0.1f));
            Assert.That(WorldSize((RectTransform)accept.transform).y, Is.EqualTo(40f).Within(0.1f));
            Assert.That(WorldSize((RectTransform)close.transform).x, Is.EqualTo(140f).Within(0.1f));
            Assert.That(WorldSize((RectTransform)close.transform).y, Is.EqualTo(36f).Within(0.1f));
            Assert.That(Contained((RectTransform)accept.transform, page), Is.True);
            Assert.That(Contained((RectTransform)close.transform, page), Is.True);
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(20));
            Assert.That(body.fontSize, Is.GreaterThanOrEqualTo(11));
            Assert.That(chronicle.fontSize, Is.GreaterThanOrEqualTo(11));

            // A 960x720 window narrows the town root; the page must scale down, not clip.
            ((RectTransform)town).sizeDelta = new Vector2(1000f, 268f);
            panel.Refresh();
            float fit = page.localScale.x;
            Assert.That(fit, Is.LessThan(1f));
            Assert.That(fit, Is.GreaterThan(0.8f));
            Assert.That(Contained(page, (RectTransform)town), Is.True);
            Assert.That(WorldSize(page).x, Is.LessThanOrEqualTo(1000f));
            Assert.That(WorldSize((RectTransform)accept.transform).x, Is.GreaterThanOrEqualTo(190f));
            Assert.That(WorldSize((RectTransform)close.transform).y, Is.GreaterThanOrEqualTo(28f));

            // ScaleWithScreenSize at a 960x720 screen and 1280x720 reference.
            float screenScale = Mathf.Sqrt((960f / 1280f) * (720f / 720f));
            Assert.That(screenScale, Is.EqualTo(0.8660254f).Within(0.0001f));
            float bodyPixels = body.fontSize * fit * screenScale;
            float closePixels = WorldSize((RectTransform)close.transform).y * screenScale;
            TestContext.WriteLine($"960x720: page fit={fit:F3}, screen scale={screenScale:F3}, " +
                                  $"body≈{bodyPixels:F1}px, close height≈{closePixels:F1}px");
            // 960x720 is below the 12px reading target; document the accepted floor
            // rather than claiming visual verification.
            Assert.That(bodyPixels, Is.GreaterThanOrEqualTo(8f));
            Assert.That(closePixels, Is.GreaterThanOrEqualTo(24f));
        }

        [Test]
        public void GeneratedPage_ShowsOneCandidateRowAndScrollsTheRest()
        {
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM5ShadowEscortCommissionPanel.RootName);
            ShadowEscortCommissionPanel panel = root.GetComponent<ShadowEscortCommissionPanel>();
            Transform page = root.Find(AddM5ShadowEscortCommissionPanel.PanelName);

            panel.Open();
            panel.Accept();
            Assert.That(session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(panel.CandidateCount, Is.GreaterThan(2));

            Transform scrollTransform =
                page.Find(AddM5ShadowEscortCommissionPanel.CandidateScrollName);
            RectTransform viewport = (RectTransform)scrollTransform
                .Find(AddM5ShadowEscortCommissionPanel.CandidateViewportName);
            RectTransform content = (RectTransform)viewport
                .Find(AddM5ShadowEscortCommissionPanel.CandidateContentName);
            RectTransform template = (RectTransform)content
                .Find(AddM5ShadowEscortCommissionPanel.CandidateTemplateName);
            ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(viewport.rect.height, Is.GreaterThanOrEqualTo(template.rect.height),
                "at least one complete assignee row must fit the viewport.");
            Assert.That(content.rect.height, Is.GreaterThan(viewport.rect.height),
                "the fixed roster must exercise the candidate scroll.");
            Assert.That(panel.CandidateLabel(0), Is.Not.Empty);
        }

        [Test]
        public void GeneratedPage_RefusesATownNarrowerThanTheAuthoredPage()
        {
            ((RectTransform)town).sizeDelta = new Vector2(960f, 268f);
            int count = ObjectCount();
            Assert.That(AddM5ShadowEscortCommissionPanel.Update(scene),
                Is.EqualTo(AddM5ShadowEscortCommissionPanel.UpdateResult.InsufficientSpace),
                "a 960-unit town cannot host the 1160-unit authored page.");
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        private static Vector2 WorldSize(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Vector2(Mathf.Abs(corners[2].x - corners[0].x),
                Mathf.Abs(corners[2].y - corners[0].y));
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(c =>
            {
                Vector3 point = parent.InverseTransformPoint(c);
                return point.x >= bounds.xMin - 0.1f && point.x <= bounds.xMax + 0.1f &&
                       point.y >= bounds.yMin - 0.1f && point.y <= bounds.yMax + 0.1f;
            });
        }

        private GameObject Create(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (System.Type type in components)
                go.AddComponent(type);
            return go;
        }

        private Transform[] All() => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();

        private int ObjectCount() => All().Length;

        private void AssertNoPanel()
        {
            Assert.That(All().Any(t =>
                t.name == AddM5ShadowEscortCommissionPanel.RootName ||
                t.GetComponent<ShadowEscortCommissionPanel>() != null), Is.False);
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
