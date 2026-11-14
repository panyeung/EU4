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
    /// Structural tests for the transactional M06 page generator. Every fixture is an
    /// isolated preview scene, so the user's FoundationScene is never saved, replaced
    /// or reloaded.
    /// </summary>
    public sealed class AddM06TacticalMissionPanelTests
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
            // FoundationScene holds M2WorldMapRoot as a fixed 1216x268 rect anchored at
            // (0.5, 0.19722222) of the canvas (rectTransform fileID 1545502404), and
            // TownMenuRoot stretches inside it (fileID 1447859597), so the town menu is the
            // bottom 268-unit band, not the whole canvas. The fixture mirrors both rects so a
            // layout that only fits a full-screen parent cannot pass here.
            var mapRect = map.GetComponent<RectTransform>();
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.19722222f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.sizeDelta = new Vector2(1216f, 268f);
            town = Create("TownMenuRoot", map.transform).transform;
            var townRect = (RectTransform)town;
            townRect.anchorMin = Vector2.zero;
            townRect.anchorMax = Vector2.one;
            townRect.pivot = new Vector2(0.5f, 0.5f);
            townRect.offsetMin = Vector2.zero;
            townRect.offsetMax = Vector2.zero;
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
            Assert.That(AddM06TacticalMissionPanel.CheckScenePreflight(
                    "Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.SceneMismatch));
            Assert.That(AddM06TacticalMissionPanel.CheckScenePreflight(
                    AddM06TacticalMissionPanel.ScenePath, true),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.SceneDirty));
            Assert.That(AddM06TacticalMissionPanel.CheckScenePreflight(
                    AddM06TacticalMissionPanel.ScenePath, false),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
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
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Updater_BuildsBindsAndUndoesTheWholePage()
        {
            int count = ObjectCount();
            Assert.That(new SerializedObject(controller)
                    .FindProperty("tacticalMissionPanel").objectReferenceEntityIdValue,
                Is.EqualTo(EntityId.None));

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));

            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(town));
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            Assert.That(panel, Is.Not.Null);
            Transform page = Page();
            Transform open = root.Find(AddM06TacticalMissionPanel.OpenButtonName);
            Assert.That(page, Is.Not.Null);
            Assert.That(open, Is.Not.Null);
            Assert.That(page.gameObject.activeSelf, Is.False, "The page must start hidden.");
            Assert.That(open.gameObject.activeSelf, Is.True);

            Transform grid = page.Find(AddM06TacticalMissionPanel.GridName);
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.GetComponent<GridLayoutGroup>().constraintCount,
                Is.EqualTo(TacticalMissionScenario.Width));
            Assert.That(grid.Cast<Transform>().Count(),
                Is.EqualTo(TacticalMissionScenario.Width * TacticalMissionScenario.Height));
            Assert.That(page.GetComponentsInChildren<Text>(true).Length,
                Is.GreaterThanOrEqualTo(13 + 48));

            Assert.That(GetReference(controller, "tacticalMissionPanel"), Is.EqualTo(panel));
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(session));
            Assert.That(GetReference(panel, "panelRoot"), Is.EqualTo(page.gameObject));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);
            Assert.That(scene.path, Is.Empty, "The public updater must never save the test scene.");

            int builtCount = ObjectCount();
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount), "A repeat update must be a no-op.");

            Undo.PerformUndo();
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "One Undo must remove the entire generated hierarchy.");
            Assert.That(GetReference(controller, "tacticalMissionPanel"), Is.Null);
        }

        [TestCase("reference")]
        [TestCase("exception")]
        [TestCase("geometry")]
        [TestCase("cell-font")]
        [TestCase("layout-skipped")]
        [TestCase("layout-shifted")]
        public void Updater_RollsBackOnValidationFailure(string fault)
        {
            int count = ObjectCount();
            bool wasDirty = scene.isDirty;
            var result = AddM06TacticalMissionPanel.Update(scene, root =>
            {
                if (fault == "exception")
                    throw new System.InvalidOperationException("Injected construction failure");
                if (fault == "geometry")
                {
                    root.GetComponent<RectTransform>().anchoredPosition = new Vector2(-5000f, 0f);
                    return;
                }
                if (fault == "cell-font")
                {
                    // Cell-level fault: the grid layout cannot repair a broken label font.
                    Cell(Page(), 7, 5).GetComponentInChildren<Text>(true).font = null;
                    return;
                }
                if (fault == "layout-skipped")
                {
                    // Overwrite the driven rects so the page looks like the layout pass
                    // never ran on the hidden page.
                    foreach (Transform cell in Grid(Page()).Cast<Transform>())
                    {
                        var rect = (RectTransform)cell;
                        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = Vector2.zero;
                    }
                    return;
                }
                if (fault == "layout-shifted")
                {
                    // Slot spacing stays intact, so only the "fills its grid" check can
                    // catch a block of cells that sits off its grid center.
                    foreach (Transform cell in Grid(Page()).Cast<Transform>())
                        ((RectTransform)cell).anchoredPosition += new Vector2(9f, 9f);
                    return;
                }
                SetReference(root.GetComponent<TacticalMissionPanel>(), "titleText", null);
            });

            Assert.That(result, Is.EqualTo(fault == "exception"
                ? AddM06TacticalMissionPanel.UpdateResult.Failed
                : AddM06TacticalMissionPanel.UpdateResult.ValidationFailed));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
            Assert.That(GetReference(controller, "tacticalMissionPanel"), Is.Null);
            Assert.That(GetReference(controller, "gameSession"), Is.EqualTo(session));
            Assert.That(GetReference(controller, "townMenuRoot"), Is.EqualTo(town.gameObject));
            TestContext.WriteLine($"Rollback dirty state ({fault}): before={wasDirty}; after={scene.isDirty}");
            Assert.That(scene.path, Is.Empty,
                "Rollback must not save an unvalidated result to clear the dirty flag.");

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success),
                "The rolled-back scene must accept a clean retry.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Updater_RefusesExistingTacticalObjects(bool componentOnly)
        {
            GameObject existing = Create(componentOnly
                ? "OtherTacticalHost"
                : AddM06TacticalMissionPanel.RootName, town);
            if (componentOnly)
                existing.AddComponent<TacticalMissionPanel>();
            int count = ObjectCount();
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.ExistingTacticalPanel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(existing, Is.Not.Null);
            Assert.That(GetReference(controller, "tacticalMissionPanel"), Is.Null);
        }

        [Test]
        public void Updater_RefusesUnboundSessionAndInsufficientSpace()
        {
            int count = ObjectCount();
            SetReference(controller, "gameSession", null);
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.InvalidHierarchyOrBindings));
            SetReference(controller, "gameSession", session);
            // The town root stretches inside the map band, so the band's own rect is what a
            // real smaller window narrows; TownMenuRoot.sizeDelta alone cannot change it.
            ((RectTransform)town.parent).sizeDelta = new Vector2(300f, 200f);
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.InsufficientSpace));
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
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndExternalReferences()
        {
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.False);
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Transform page = Page();
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();

            SetReference(panel, "titleText",
                Create("ExternalText", null, typeof(Text)).GetComponent<Text>());
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.False,
                "An external title reference must be rejected.");
            SetReference(panel, "titleText",
                page.Find(AddM06TacticalMissionPanel.TitleTextName).GetComponent<Text>());
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);

            Transform template = page
                .Find(AddM06TacticalMissionPanel.CandidateScrollName)
                .Find(AddM06TacticalMissionPanel.CandidateViewportName)
                .Find(AddM06TacticalMissionPanel.CandidateContentName)
                .Find(AddM06TacticalMissionPanel.CandidateTemplateName);
            template.gameObject.SetActive(true);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.False,
                "An active candidate template must be rejected.");
            template.gameObject.SetActive(false);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);

            Transform firstCell = page.Find(AddM06TacticalMissionPanel.GridName)
                .Find(AddM06TacticalMissionPanel.CellNamePrefix + "0_0");
            firstCell.gameObject.SetActive(false);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string inactive),
                Is.False,
                "A hidden cell would reflow the other 47 cells out of their fixed slots.");
            Assert.That(inactive, Does.Contain("stay active"));
            firstCell.gameObject.SetActive(true);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);

            Create(AddM06TacticalMissionPanel.RootName, town);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.False,
                "A duplicate root must be rejected.");
        }

        [Test]
        public void Updater_LeavesTheM5EntryAndTownControlsUntouched()
        {
            GameObject title = Create("TownMenuTitleText", town, typeof(Text));
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.sizeDelta = new Vector2(500f, 32f);
            titleRect.anchoredPosition = new Vector2(0f, 104f);
            GameObject back = Create("ReturnToMapButton", town, typeof(Image), typeof(Button));
            RectTransform backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(260f, 60f);
            backRect.anchoredPosition = new Vector2(300f, 42f);

            M5Fixture m5 = CreateM5CommissionEntry();
            var m5OpenRect = (RectTransform)m5.open.transform;
            Vector2 m5OpenPosition = m5OpenRect.anchoredPosition;
            Vector2 m5OpenSize = m5OpenRect.sizeDelta;
            bool m5Enabled = m5.panel.enabled;
            bool m5PageActive = m5.page.activeSelf;
            int count = ObjectCount();

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);

            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            var openRect = (RectTransform)root.Find(AddM06TacticalMissionPanel.OpenButtonName);
            Assert.That(Overlap(openRect, m5OpenRect), Is.False,
                "the new entry must not cover the M5 commission entry");
            Assert.That(Overlap(openRect, backRect), Is.False,
                "the new entry must not cover the return control");
            Assert.That(Overlap(openRect, titleRect), Is.False,
                "the new entry must not cover the town title");
            Assert.That(openRect.anchoredPosition, Is.Not.EqualTo(m5OpenPosition));

            Assert.That(GetReference(controller, "shadowEscortCommissionPanel"),
                Is.SameAs(m5.panel),
                "the existing M05 binding must keep its object reference.");
            Assert.That(m5.root.parent, Is.EqualTo(town));
            Assert.That(m5.panel.transform, Is.EqualTo(m5.root));
            Assert.That(m5.open.transform.parent, Is.EqualTo(m5.root));
            Assert.That(m5OpenRect.anchoredPosition, Is.EqualTo(m5OpenPosition));
            Assert.That(m5OpenRect.sizeDelta, Is.EqualTo(m5OpenSize));
            Assert.That(m5.open.gameObject.activeSelf, Is.True);
            Assert.That(m5.page.activeSelf, Is.EqualTo(m5PageActive));
            Assert.That(m5.panel.enabled, Is.EqualTo(m5Enabled));
            Assert.That(m5.panel.IsOpen, Is.False);

            Assert.That(title.transform.parent, Is.EqualTo(town));
            Assert.That(back.transform.parent, Is.EqualTo(town));
            Assert.That(titleRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 104f)));
            Assert.That(backRect.anchoredPosition, Is.EqualTo(new Vector2(300f, 42f)));
            Assert.That(GetReference(controller, "supplyShortageMissionPanel"), Is.Null,
                "The M04 mission binding must stay unset.");
            Assert.That(ObjectCount(), Is.GreaterThan(count));
        }

        [Test]
        public void Validator_RejectsAnEntryThatCoversTheM5CommissionButton()
        {
            M5Fixture m5 = CreateM5CommissionEntry();
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));

            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            var openRect = (RectTransform)root.Find(AddM06TacticalMissionPanel.OpenButtonName);
            Vector2 placed = openRect.anchoredPosition;
            openRect.anchoredPosition = ((RectTransform)m5.open.transform).anchoredPosition;

            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error), Is.False,
                "Two town entries on the same rect must be rejected.");
            Assert.That(error, Does.Contain("M5CommissionOpenButton"));

            openRect.anchoredPosition = placed;
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);
        }

        [Test]
        public void Validator_ReadsTheBuiltPageWithoutTouchingTheScene()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));

            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Transform page = Page();
            Transform grid = page.Find(AddM06TacticalMissionPanel.GridName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            bool dirty = scene.isDirty;
            bool townActive = town.gameObject.activeSelf;
            bool rootActive = root.gameObject.activeSelf;
            bool pageActive = page.gameObject.activeSelf;
            bool gridActive = grid.gameObject.activeSelf;
            bool cellActive = grid.GetChild(0).gameObject.activeSelf;
            bool panelEnabled = panel.enabled;

            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);

            Assert.That(scene.isDirty, Is.EqualTo(dirty),
                "The validator must not dirty the user's scene.");
            Assert.That(town.gameObject.activeSelf, Is.EqualTo(townActive),
                "The validator must not activate TownMenuRoot.");
            Assert.That(root.gameObject.activeSelf, Is.EqualTo(rootActive));
            Assert.That(page.gameObject.activeSelf, Is.EqualTo(pageActive));
            Assert.That(grid.gameObject.activeSelf, Is.EqualTo(gridActive));
            Assert.That(grid.GetChild(0).gameObject.activeSelf, Is.EqualTo(cellActive));
            Assert.That(panel.enabled, Is.EqualTo(panelEnabled),
                "The validator must not enable or disable the panel.");
        }

        [Test]
        public void GeneratedPage_ShowsTheGridAndFitsReferenceAndNarrowCanvas()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            RectTransform page = (RectTransform)Page();
            Transform pageTransform = Page();
            var canvasRect = (RectTransform)All().Single(t => t.name == "Canvas");

            Assert.That(panel.OverlayVisible, Is.False, "the overlay must start hidden");
            panel.Open();
            Assert.That(panel.OverlayVisible, Is.True,
                "opening the page must raise its opaque overlay above the town band");
            Assert.That(page.localScale.x, Is.EqualTo(1f).Within(0.0001f),
                "the 1280x720 reference canvas must keep the authored layout.");
            Assert.That(Contained(page, canvasRect), Is.True);
            // The sheet is a modal page, not a second strip inside the 268-unit town band.
            Assert.That(WorldSize(page).y, Is.GreaterThan(((RectTransform)town).rect.height),
                "the overlay page must be taller than the town band it covers");
            Assert.That(panel.CellCount, Is.EqualTo(48));
            Assert.That(panel.CellLabel(3, 1), Is.EqualTo("壁"));
            Assert.That(panel.CellLabel(6, 4), Is.EqualTo("报"));
            Assert.That(panel.IsCandidateListVisible, Is.True);
            Assert.That(panel.CandidateCount, Is.GreaterThanOrEqualTo(2));

            Button north = pageTransform.Find(AddM06TacticalMissionPanel.NorthButtonName)
                .GetComponent<Button>();
            Button withdraw = pageTransform.Find(AddM06TacticalMissionPanel.WithdrawButtonName)
                .GetComponent<Button>();
            Assert.That(WorldSize((RectTransform)north.transform).x,
                Is.EqualTo(92f).Within(0.1f));
            Assert.That(WorldSize((RectTransform)north.transform).y,
                Is.EqualTo(30f).Within(0.1f));
            Assert.That(Contained((RectTransform)north.transform, page), Is.True);
            Assert.That(Contained((RectTransform)withdraw.transform, page), Is.True);

            // A narrower window narrows the canvas rect, so the page must scale down, not clip.
            canvasRect.sizeDelta = new Vector2(1000f, 720f);
            panel.Refresh();
            float fit = page.localScale.x;
            Assert.That(fit, Is.LessThan(1f));
            Assert.That(fit, Is.GreaterThan(0.8f));
            Assert.That(Contained(page, canvasRect), Is.True);
            // The page now fits a parent edge to edge, so this is an exact-fit bound and the
            // tolerance only absorbs float rounding through the transform corners.
            Assert.That(WorldSize(page).x, Is.LessThanOrEqualTo(1000.01f));

            TestContext.WriteLine($"Narrow canvas: page fit={fit:F3}, " +
                                  $"grid={WorldSize((RectTransform)pageTransform.Find(AddM06TacticalMissionPanel.GridName)).x:F1} " +
                                  $"x {WorldSize((RectTransform)pageTransform.Find(AddM06TacticalMissionPanel.GridName)).y:F1}");
            Assert.That(WorldSize((RectTransform)north.transform).y,
                Is.GreaterThanOrEqualTo(23f),
                "a 1000-unit canvas keeps the command buttons above 23 px.");
        }

        [TestCase(1280f, 720f, 1f)]
        [TestCase(1600f, 900f, 1.25f)]
        [TestCase(1920f, 1080f, 1.5f)]
        public void GeneratedPage_CentersInsideTheRealCanvasAtEveryTargetSize(
            float width, float height, float expectedScale)
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            var page = (RectTransform)Page();
            var canvasRect = (RectTransform)All().Single(t => t.name == "Canvas");
            CanvasScaler scaler = canvasRect.GetComponent<CanvasScaler>();

            // The CanvasScaler is ScaleWithScreenSize at a 1280x720 reference with a 0.5
            // width/height match. At 16:9 the derived canvas rect is the reference rect for
            // every target, while the rendered scale factor is 1.0, 1.25 and 1.5.
            float canvasScale = Mathf.Pow(2f, 0.5f * Mathf.Log(width / 1280f, 2f) +
                                          0.5f * Mathf.Log(height / 720f, 2f));
            Assert.That(canvasScale, Is.EqualTo(expectedScale).Within(0.001f));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.localScale = Vector3.one * canvasScale;

            panel.Open();
            Assert.That(page.localScale.x, Is.EqualTo(1f).Within(0.0001f),
                "the sheet must keep the authored size at every 16:9 target");
            Assert.That(Contained(page, canvasRect), Is.True);
            Vector3 pageCenter = page.TransformPoint(page.rect.center);
            Vector3 canvasCenter = canvasRect.TransformPoint(canvasRect.rect.center);
            Assert.That((pageCenter - canvasCenter).magnitude, Is.LessThan(0.01f),
                "the sheet must be centered on the canvas, not offset into the town band");
            Assert.That(WorldSize(page).x, Is.EqualTo(1120f * canvasScale).Within(0.5f));
            Assert.That(WorldSize(page).y, Is.EqualTo(620f * canvasScale).Within(0.5f));
            Assert.That(WorldSize(page).x, Is.LessThan(WorldSize(canvasRect).x));
            Assert.That(WorldSize(page).y, Is.LessThan(WorldSize(canvasRect).y));
        }

        [TestCase(1024f, 768f)]
        [TestCase(1920f, 1080f)]
        [TestCase(3840f, 1080f)]
        public void GeneratedPage_StaysContainedAtEveryRealCanvasAspect(
            float screenWidth, float screenHeight)
        {
            RectTransform canvasRect = ApplyScreenSize(screenWidth, screenHeight);
            Vector2 canvasSize = canvasRect.sizeDelta;

            // 16:9 resolves to the authored 1280x720 rect. 4:3 is narrower than the sheet
            // (about 1108 units at 1024x768) and 32:9 is shorter (about 509 units at
            // 3840x1080). The old gate compared the authored 1120x620 sizeDelta with this
            // rect at face value and rejected both valid layouts; the fit-aware gate must
            // accept them.
            bool referenceAspect = Mathf.Abs(screenWidth / screenHeight - 16f / 9f) < 0.001f;
            if (referenceAspect)
            {
                Assert.That((canvasSize - new Vector2(1280f, 720f)).magnitude,
                    Is.LessThan(0.01f), "16:9 must resolve to the reference canvas rect");
            }
            else
            {
                Assert.That(canvasSize.x < 1120f || canvasSize.y < 620f, Is.True,
                    "a non-16:9 window must derive a canvas rect smaller than the sheet");
            }

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);

            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            var page = (RectTransform)Page();
            panel.Open();

            float expectedFit = Mathf.Clamp(
                Mathf.Min(canvasSize.x / page.rect.width, canvasSize.y / page.rect.height),
                TacticalMissionPanel.MinimumPageScale, 1f);
            Assert.That(page.localScale.x, Is.EqualTo(expectedFit).Within(0.0005f),
                "the sheet must render at the fit for the derived canvas rect");
            Assert.That(Contained(page, canvasRect), Is.True,
                "the fitted sheet must stay inside the derived canvas rect");
            Assert.That(page.rect.width * page.localScale.x,
                Is.LessThanOrEqualTo(canvasSize.x + 0.5f));
            Assert.That(page.rect.height * page.localScale.y,
                Is.LessThanOrEqualTo(canvasSize.y + 0.5f));
            Assert.That(WorldSize(page).x,
                Is.LessThanOrEqualTo(WorldSize(canvasRect).x + 0.5f));
            Assert.That(WorldSize(page).y,
                Is.LessThanOrEqualTo(WorldSize(canvasRect).y + 0.5f));

            Transform grid = page.Find(AddM06TacticalMissionPanel.GridName);
            Assert.That(Contained((RectTransform)grid, page), Is.True);
            Assert.That(Contained(
                    (RectTransform)page.Find(AddM06TacticalMissionPanel.NorthButtonName), page),
                Is.True);
            Assert.That(panel.OverlayVisible, Is.True);
            TestContext.WriteLine($"{screenWidth}x{screenHeight}: canvas=" +
                                  $"{canvasSize.x:F2}x{canvasSize.y:F2}, fit=" +
                                  $"{page.localScale.x:F4}");
        }

        [Test]
        public void Overlay_MustBeTheLastCanvasChildAndIsRaisedOnOpen()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            var canvasRect = (RectTransform)All().Single(t => t.name == "Canvas");
            Transform overlay = Overlay();
            Assert.That(overlay.parent, Is.EqualTo(canvasRect));
            Assert.That(overlay.GetSiblingIndex(), Is.EqualTo(canvasRect.childCount - 1),
                "the generated modal must be the last canvas child");
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);

            overlay.SetAsFirstSibling();
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error), Is.False,
                "a modal buried under the map view must be rejected");
            Assert.That(error, Does.Contain("last Canvas child"));
            Assert.That(overlay.GetSiblingIndex(), Is.Zero);

            // Opening re-raises the canvas overlay. The town-rooted panel transform cannot
            // change this order, which is why the controller no longer reorders the town.
            town.gameObject.SetActive(true);
            TacticalMissionPanel panel = town
                .Find(AddM06TacticalMissionPanel.RootName).GetComponent<TacticalMissionPanel>();
            controller.OpenTownMenu();
            controller.ToggleTacticalMissionPanel();
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(overlay.GetSiblingIndex(), Is.EqualTo(canvasRect.childCount - 1),
                "opening must raise the canvas overlay above the map view");
            // Back to the saved closed state; the raised slot must survive the close.
            panel.Close();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(overlay.GetSiblingIndex(), Is.EqualTo(canvasRect.childCount - 1),
                "closing must leave the overlay's top canvas slot intact");
        }

        [Test]
        public void GeneratedPage_RefitsWhenTheCanvasChangesWhileOpen()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            var page = (RectTransform)Page();
            var canvasRect = (RectTransform)All().Single(t => t.name == "Canvas");
            panel.Open();
            Assert.That(page.localScale.x, Is.EqualTo(1f).Within(0.0001f));
            string before = JsonUtility.ToJson(session.CaptureSave());

            canvasRect.sizeDelta = new Vector2(1000f, 720f);
            // The frame loop calls this while the page is open; EditMode has no frames, so
            // the test calls the same method LateUpdate uses.
            panel.RefitIfParentResized();

            float fit = page.localScale.x;
            Assert.That(fit, Is.LessThan(1f));
            Assert.That(fit, Is.EqualTo(1000f / 1120f).Within(0.001f));
            Assert.That(Contained(page, canvasRect), Is.True,
                "the open sheet must re-fit the resized canvas instead of keeping the old scale");
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.OverlayVisible, Is.True);
            Assert.That(JsonUtility.ToJson(session.CaptureSave()), Is.EqualTo(before),
                "a resize must not mutate campaign state");

            panel.RefitIfParentResized();
            Assert.That(page.localScale.x, Is.EqualTo(fit).Within(0.00001f),
                "an unchanged parent must not re-scale the sheet");

            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            panel.RefitIfParentResized();
            Assert.That(page.localScale.x, Is.EqualTo(1f).Within(0.0001f),
                "returning to the reference canvas restores the authored scale");
        }

        [Test]
        public void GeneratedPage_OffersAReachableCloseThenReturnPath()
        {
            GameObject back = Create("ReturnToMapButton", town, typeof(Image), typeof(Button));
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(260f, 60f);
            backRect.anchoredPosition = new Vector2(300f, 42f);
            Button returnButton = back.GetComponent<Button>();

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);
            town.gameObject.SetActive(true);
            TacticalMissionPanel panel = town.Find(AddM06TacticalMissionPanel.RootName)
                .GetComponent<TacticalMissionPanel>();
            Transform page = Page();
            Button close = page.Find(AddM06TacticalMissionPanel.CloseButtonName)
                .GetComponent<Button>();
            Text closeLabel = close.GetComponentInChildren<Text>(true);
            // The scrim blocks the town band, so the modal's own control is the way out. It
            // must say so, and it must not need a game-state change to get back.
            Assert.That(closeLabel.text, Does.Contain("返回"),
                "the close control must advertise the return to town, not a bare close");

            controller.OpenTownMenu();
            controller.ToggleTacticalMissionPanel();
            Assert.That(panel.OverlayVisible, Is.True);
            string before = JsonUtility.ToJson(session.CaptureSave());

            close.onClick.Invoke();
            Assert.That(panel.IsOpen, Is.False, "the close control closes the modal");
            Assert.That(panel.OverlayVisible, Is.False);
            Assert.That(page.gameObject.activeSelf, Is.False);
            Assert.That(town.gameObject.activeSelf, Is.True,
                "closing the modal returns to the town band, not to the map");
            Assert.That(returnButton.gameObject.activeInHierarchy, Is.True,
                "the town return control must be reachable once the scrim is gone");

            controller.ReturnToMap();
            Assert.That(controller.InTownMenu, Is.False);
            Assert.That(town.gameObject.activeSelf, Is.False);
            Assert.That(JsonUtility.ToJson(session.CaptureSave()), Is.EqualTo(before),
                "close-then-return must not change campaign state");
        }

        [Test]
        public void GeneratedPage_ScopesTheOverlayToTheTownLifecycle()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            Assert.That(panel.OverlayVisible, Is.False, "the overlay must start hidden");
            Assert.That(GetReference(controller, "tacticalMissionPanel"), Is.SameAs(panel),
                "the menu controller owns the close path, so it must see this panel");

            // Entering the town is the production lifecycle that owns the modal.
            controller.OpenTownMenu();
            Assert.That(panel.OverlayVisible, Is.False, "entering town keeps the page closed");
            controller.ToggleTacticalMissionPanel();
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.OverlayVisible, Is.True,
                "opening the page shows its scrim and sheet together");

            // Leaving the town menu closes the page through the controller, which is what
            // guarantees the click-blocking scrim cannot outlive the town band.
            controller.ReturnToMap();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.OverlayVisible, Is.False,
                "closing the town must not leave the scrim over the map view");
            Assert.That(Page().gameObject.activeSelf, Is.False);
            Assert.That(town.gameObject.activeSelf, Is.False);

            // Returning to town keeps the page closed until the player asks for it again.
            controller.OpenTownMenu();
            Assert.That(panel.OverlayVisible, Is.False);
            controller.ToggleTacticalMissionPanel();
            Assert.That(panel.OverlayVisible, Is.True);
            controller.ToggleTacticalMissionPanel();
            Assert.That(panel.OverlayVisible, Is.False);
            Assert.That(panel.IsOpen, Is.False);
        }

        [Test]
        public void GeneratedGrid_CellsReceiveDistinctLayoutSlots()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));

            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Transform page = Page();
            Assert.That(page, Is.Not.Null);
            Assert.That(page.gameObject.activeSelf, Is.False,
                "The layout pass must leave the generated page hidden.");
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(town.gameObject.activeSelf, Is.False,
                "The layout pass must restore the town root's own active state.");

            // Read the saved rects without activating anything: the generator's own layout
            // pass has to be the one that placed the cells.
            AssertGridCellsAreLaidOutDistinctly(page);
            Assert.That(page.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void GeneratedPage_StartBeginsTheAuthoredOperation()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            panel.Open();

            int selected = 0;
            for (int i = 0; i < panel.CandidateCount && selected < 2; i++)
            {
                if (!panel.CandidateInteractable(i))
                    continue;
                panel.ClickCandidate(i);
                selected++;
            }
            Assert.That(panel.StartInteractable, Is.True);
            panel.ClickStart();

            Assert.That(session.IsTacticalMissionActive, Is.True);
            Assert.That(panel.IsCandidateListVisible, Is.False);
            Assert.That(panel.MoveInteractable, Is.True);
            Assert.That(panel.TeamLine, Does.Contain("潜入队"));

            panel.ClickNorth();
            panel.ClickMove();
            Assert.That(panel.StatusLine, Does.Contain("回合 1"));
        }

        [Test]
        public void Updater_SucceedsWithTheExactFoundationScenePosition()
        {
            // Reference Game View: the pinned 1280x720 canvas derives a -218 town offset.
            ConfigureRealSceneRig(new Vector2(1280f, 720f));

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);
            AssertGridCellsAreLaidOutDistinctly(TownPage());
        }

        [Test]
        public void Updater_SucceedsWithTheDerivedTallCanvasOffset()
        {
            // A taller canvas than the reference derives a -327 town offset. The canvas is
            // pinned by the fixture, so this is the rig's own number, not a Game View size.
            ConfigureRealSceneRig(new Vector2(1280f, 1080f));

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error),
                Is.True, error);
            AssertGridCellsAreLaidOutDistinctly(TownPage());
        }

        private Transform Page() => All()
            .Single(t => t.name == AddM06TacticalMissionPanel.PanelName);

        private Transform Overlay() => All()
            .Single(t => t.name == AddM06TacticalMissionPanel.OverlayName);

        /// <summary>
        /// Derives the canvas rect the ScaleWithScreenSize scaler produces for a screen size
        /// and applies it to the fixture, so the aspect tests exercise real 4:3 and ultrawide
        /// rects instead of pinning every aspect to the 1280x720 reference.
        /// </summary>
        private RectTransform ApplyScreenSize(float width, float height)
        {
            var canvasRect = (RectTransform)All().Single(t => t.name == "Canvas");
            CanvasScaler scaler = canvasRect.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Vector2 reference = scaler.referenceResolution;
            float scale = Mathf.Pow(2f,
                0.5f * Mathf.Log(width / reference.x, 2f) +
                0.5f * Mathf.Log(height / reference.y, 2f));
            var rectSize = new Vector2(width / scale, height / scale);
            canvasRect.sizeDelta = rectSize;
            canvasRect.localScale = new Vector3(scale, scale, 1f);
            Assert.That((canvasRect.rect.size - rectSize).magnitude, Is.LessThan(0.01f));
            return canvasRect;
        }

        private Transform TownPage() => Page();

        private RectTransform ConfigureRealSceneRig(Vector2 canvasSize)
        {
            // FoundationScene: Screen Space Overlay canvas, ScaleWithScreenSize 1280x720,
            // MatchWidthOrHeight 0.5, M2WorldMapRoot anchored at (0.5, 0.19722222) with a
            // 1216x268 size, and a stretching TownMenuRoot under it. A preview scene never
            // receives a Game View, so the canvas rect is pinned here instead of inheriting
            // whatever window size happens to be open.
            GameObject canvasObject = All().Single(t => t.name == "Canvas").gameObject;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
            Assert.That(scaler.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.0001f));

            RectTransform canvasRect = (RectTransform)canvasObject.transform;
            canvasObject.transform.position = Vector3.zero;
            canvasRect.anchorMin = canvasRect.anchorMax = canvasRect.pivot =
                new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = canvasSize;
            Assert.That(canvasRect.rect.size, Is.EqualTo(canvasSize));

            RectTransform mapRect =
                (RectTransform)All().Single(t => t.name == "M2WorldMapRoot");
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.19722222f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.sizeDelta = new Vector2(1216f, 268f);
            mapRect.anchoredPosition = Vector2.zero;
            Assert.That(mapRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.19722222f)));
            Assert.That(mapRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.19722222f)));
            Assert.That(mapRect.sizeDelta, Is.EqualTo(new Vector2(1216f, 268f)));
            Assert.That(mapRect.anchoredPosition, Is.EqualTo(Vector2.zero));

            RectTransform townRect = (RectTransform)town;
            townRect.anchorMin = Vector2.zero;
            townRect.anchorMax = Vector2.one;
            townRect.pivot = new Vector2(0.5f, 0.5f);
            townRect.offsetMin = Vector2.zero;
            townRect.offsetMax = Vector2.zero;

            // Assert the rig's derived world offset instead of assuming a measured value.
            Vector2 derivedOffset = new Vector2(0f,
                (mapRect.anchorMin.y - mapRect.pivot.y) * canvasSize.y +
                mapRect.anchoredPosition.y);
            Assert.That(townRect.position.x, Is.EqualTo(derivedOffset.x).Within(0.01f));
            Assert.That(townRect.position.y, Is.EqualTo(derivedOffset.y).Within(0.01f));
            return mapRect;
        }

        /// <summary>
        /// Reads the saved cell rects without activating the page. Distinct, slot-ordered
        /// positions prove the generator's own layout pass ran; centered anchors mean it
        /// was skipped.
        /// </summary>
        private static void AssertGridCellsAreLaidOutDistinctly(Transform page)
        {
            Assert.That(page, Is.Not.Null);
            Transform gridTransform = page.Find(AddM06TacticalMissionPanel.GridName);
            Assert.That(gridTransform, Is.Not.Null);
            GridLayoutGroup layout = gridTransform.GetComponent<GridLayoutGroup>();
            Assert.That(layout, Is.Not.Null);
            Vector2 step = layout.cellSize + layout.spacing;
            var positions = new Vector2[
                TacticalMissionScenario.Width * TacticalMissionScenario.Height];
            Vector2 origin = Vector2.zero;
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    string name = AddM06TacticalMissionPanel.CellNamePrefix + x + "_" + y;
                    var cell = (RectTransform)gridTransform.Find(name);
                    Assert.That(cell, Is.Not.Null, name);
                    Assert.That(cell.anchorMin, Is.EqualTo(Vector2.up),
                        name + " must be anchored by the GridLayoutGroup.");
                    Assert.That(cell.anchorMax, Is.EqualTo(Vector2.up), name);
                    Assert.That(cell.sizeDelta, Is.EqualTo(layout.cellSize), name);
                    if (x == 0 && y == 0)
                        origin = cell.anchoredPosition;
                    Vector2 expected = origin + new Vector2(step.x * x, -step.y * y);
                    Assert.That((cell.anchoredPosition - expected).magnitude, Is.LessThan(0.01f),
                        name + " must sit at its grid slot.");
                    Assert.That(Contained(cell, (RectTransform)page), Is.True,
                        name + " must stay inside the page.");
                    positions[y * TacticalMissionScenario.Width + x] = cell.anchoredPosition;
                }
            }
            int distinct = positions
                .Select(p => new Vector2(Mathf.Round(p.x * 100f), Mathf.Round(p.y * 100f)))
                .Distinct()
                .Count();
            Assert.That(distinct, Is.EqualTo(positions.Length),
                "all 48 cells must occupy distinct grid slots.");
        }

        [Test]
        public void GeneratedPage_PutsEveryLabelOnAContrastingSurface()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Transform page = Page();
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            panel.Open();

            Image pageImage = page.GetComponent<Image>();
            var bandRect = (RectTransform)page.Find(AddM06TacticalMissionPanel.HeaderBandName);
            Image band = bandRect.GetComponent<Image>();
            Assert.That(RelativeLuminance(pageImage.color), Is.GreaterThan(0.5f),
                "the page must stay a light parchment sheet, not the broad navy slab");
            Assert.That(RelativeLuminance(band.color), Is.LessThan(0.1f),
                "the header band must stay a dark indigo strip");

            // Pale type on the sheet is only readable while it stays on the band, so the
            // band carries the title and the two header buttons. The glyph legend moved to
            // the parchment as Ink for the same reason.
            foreach (string name in new[]
                     {
                         AddM06TacticalMissionPanel.TitleTextName,
                         AddM06TacticalMissionPanel.StartButtonName,
                         AddM06TacticalMissionPanel.CloseButtonName
                     })
            {
                var item = (RectTransform)page.Find(name);
                Assert.That(item, Is.Not.Null, name);
                Assert.That(Contained(item, bandRect), Is.True,
                    name + " must stay inside the header band; off the band its pale type " +
                    "drops to about 1.9:1 against the sheet");
            }

            var legend = (RectTransform)page.Find(AddM06TacticalMissionPanel.LegendTextName);
            Assert.That(legend, Is.Not.Null);
            Assert.That(Contained(legend, (RectTransform)page), Is.True);
            Assert.That(Overlap(legend, bandRect), Is.False,
                "the legend belongs on the parchment sheet, not on the dark band");
            Assert.That(page.Find(AddM06TacticalMissionPanel.LegendTextName)
                    .GetComponent<Text>().color,
                Is.EqualTo(SengokuUiTheme.Ink),
                "the legend must use the dark ink that reads on paper");

            var failures = new System.Collections.Generic.List<string>();
            foreach (Text text in page.GetComponentsInChildren<Text>(true))
            {
                Color backdrop = Backdrop(text, page, bandRect, band.color);
                Assert.That(backdrop.a, Is.GreaterThan(0.9f),
                    text.name + " must sit on an opaque surface");
                float ratio = ContrastRatio(text.color, backdrop);
                if (ratio < 4.5f)
                    failures.Add(text.name + ": " + ratio.ToString("F2") + ":1 (" +
                                 text.color + " on " + backdrop + ")");
                Assert.That(MinimumFontSize(text), Is.LessThanOrEqualTo(text.fontSize),
                    text.name + " is below the readable size for this page");
            }
            Assert.That(failures, Is.Empty,
                "every label must reach 4.5:1 against the surface it is painted on");
        }

        [Test]
        public void GeneratedPage_KeepsBodyRowsInsideThePanelWithoutCollisions()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Transform page = TownPage();
            string[] rows =
            {
                AddM06TacticalMissionPanel.ObjectiveTextName,
                AddM06TacticalMissionPanel.StateTextName,
                AddM06TacticalMissionPanel.DateTextName,
                AddM06TacticalMissionPanel.ResourceTextName,
                AddM06TacticalMissionPanel.TeamTextName,
                AddM06TacticalMissionPanel.StatusTextName,
                AddM06TacticalMissionPanel.GuardTextName,
                AddM06TacticalMissionPanel.ResultTextName,
                AddM06TacticalMissionPanel.EventTextName,
                AddM06TacticalMissionPanel.ChronicleTextName,
                AddM06TacticalMissionPanel.MessageTextName
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = (RectTransform)page.Find(rows[i]);
                Assert.That(row, Is.Not.Null, rows[i]);
                Assert.That(Contained(row, (RectTransform)page), Is.True,
                    rows[i] + " must stay inside the page");
                Assert.That(row.rect.height, Is.GreaterThanOrEqualTo(
                        row.GetComponent<Text>().fontSize + 2f),
                    rows[i] + " must host one uncut line of its own text");
                for (int j = i + 1; j < rows.Length; j++)
                {
                    Assert.That(Overlap(row, (RectTransform)page.Find(rows[j])), Is.False,
                        rows[i] + " and " + rows[j] + " must not collide");
                }
            }

            // The briefing hint and the operation party share one row because the panel
            // never shows both; assert that pairing instead of pretending they are apart.
            Assert.That(((RectTransform)page.Find(AddM06TacticalMissionPanel.SelectionTextName))
                .anchoredPosition, Is.EqualTo(
                    ((RectTransform)page.Find(AddM06TacticalMissionPanel.TeamTextName))
                    .anchoredPosition));
        }

        [Test]
        public void GeneratedGrid_KeepsItsLabelReadableOnEveryCellFill()
        {
            int kinds = System.Enum.GetValues(typeof(TacticalTileKind)).Length;
            int highlights = System.Enum.GetValues(typeof(TacticalMissionPanel.CellHighlight)).Length;
            for (int kind = 0; kind < kinds; kind++)
            {
                for (int highlight = 0; highlight < highlights; highlight++)
                {
                    for (int sight = 0; sight < 2; sight++)
                    {
                        Color fill = TacticalMissionPanel.CellFill((TacticalTileKind)kind,
                            (TacticalMissionPanel.CellHighlight)highlight, sight == 1);
                        float ratio = ContrastRatio(SengokuUiTheme.ParchmentLight, fill);
                        Assert.That(ratio, Is.GreaterThanOrEqualTo(4.5f),
                            $"cell {kind}/{highlight}/sight={sight} measures " +
                            ratio.ToString("F2") + ":1 under its pale label");
                    }
                }
            }

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Transform page = Page();
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            panel.Open();
            AssertRenderedCellLabels(page, panel);

            int selected = 0;
            for (int i = 0; i < panel.CandidateCount && selected < 2; i++)
            {
                if (!panel.CandidateInteractable(i))
                    continue;
                panel.ClickCandidate(i);
                selected++;
            }
            panel.ClickStart();
            Assert.That(session.IsTacticalMissionActive, Is.True);
            AssertRenderedCellLabels(page, panel);
        }

        private static void AssertRenderedCellLabels(Transform page, TacticalMissionPanel panel)
        {
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    Transform cell = page.Find(AddM06TacticalMissionPanel.GridName)
                        .Find(AddM06TacticalMissionPanel.CellNamePrefix + x + "_" + y);
                    Text label = cell.GetComponentInChildren<Text>(true);
                    float ratio = ContrastRatio(label.color, panel.CellColor(x, y));
                    Assert.That(ratio, Is.GreaterThanOrEqualTo(4.5f),
                        $"cell {x},{y} renders {ratio:F2}:1");
                }
            }
        }

        private static int MinimumFontSize(Text text) =>
            text.name == AddM06TacticalMissionPanel.TitleTextName ? 20 : 13;

        /// <summary>
        /// The live runtime serif face. The theme caches the dynamic OS font in a static, and
        /// a previous EditMode session can destroy that font while the cached "resolved" flag
        /// survives, which hands out a destroyed handle whose Font.name access throws
        /// MissingReferenceException. Re-resolving through the same theme policy once gives
        /// this test a live face with the exact family list and fallback the player sees;
        /// the page Text then holds it for the duration of the measurement.
        /// </summary>
        private static Font RuntimeSerif()
        {
            Font font = SengokuUiTheme.Serif;
            if (font != null)
                return font;
            typeof(SengokuUiTheme)
                .GetField("_serifResolved",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(null, false);
            return SengokuUiTheme.Serif;
        }

        private static Color Backdrop(Text text, Transform page, RectTransform bandRect,
            Color bandColor)
        {
            if (Contained((RectTransform)text.transform, bandRect))
                return bandColor;
            for (Transform node = text.transform.parent; node != null; node = node.parent)
            {
                Image image = node.GetComponent<Image>();
                if (image != null && image.color.a >= 0.9f)
                    return image.color;
                if (node == page)
                    break;
            }
            return Color.clear;
        }

        private static float ContrastRatio(Color first, Color second)
        {
            float high = Mathf.Max(RelativeLuminance(first), RelativeLuminance(second));
            float low = Mathf.Min(RelativeLuminance(first), RelativeLuminance(second));
            return (high + 0.05f) / (low + 0.05f);
        }

        private static float RelativeLuminance(Color color) =>
            0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);

        private static float Linear(float channel) =>
            channel <= 0.03928f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);

        [Test]
        public void Updater_RebuildsAnOutdatedGeneratedPageInOneUndoGroup()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Transform page = TownPage();

            // Recreate the pre-restyle page: navy sheet, no band, 11 px labels, old grid.
            page.GetComponent<Image>().color = SengokuUiTheme.IndigoDeep;
            Object.DestroyImmediate(page.Find(AddM06TacticalMissionPanel.HeaderBandName).gameObject);
            Object.DestroyImmediate(page.Find(AddM06TacticalMissionPanel.HeaderRuleName).gameObject);
            Object.DestroyImmediate(page.Find(AddM06TacticalMissionPanel.LegendTextName).gameObject);
            foreach (Text text in page.GetComponentsInChildren<Text>(true))
                text.fontSize = 11;
            var grid = (RectTransform)page.Find(AddM06TacticalMissionPanel.GridName);
            grid.anchoredPosition = new Vector2(-337f, 14f);
            grid.sizeDelta = new Vector2(446f, 202f);

            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string staleError), Is.False,
                "the scene gate must reject the pre-restyle page");
            TestContext.WriteLine("Outdated page rejected: " + staleError);

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success),
                "the menu updater must rebuild an outdated page it generated");
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string rebuilt), Is.True, rebuilt);
            Assert.That(Named(AddM06TacticalMissionPanel.RootName).Length, Is.EqualTo(1));
            Assert.That(Named(AddM06TacticalMissionPanel.OverlayName).Length, Is.EqualTo(1),
                "a rebuild must replace the overlay instead of stacking a second one");
            Assert.That(TownPage().Find(AddM06TacticalMissionPanel.HeaderBandName), Is.Not.Null);
            Assert.That(GetReference(controller, "tacticalMissionPanel"),
                Is.SameAs(town.Find(AddM06TacticalMissionPanel.RootName)
                    .GetComponent<TacticalMissionPanel>()));

            Undo.PerformUndo();
            Assert.That(Named(AddM06TacticalMissionPanel.RootName).Length, Is.EqualTo(1),
                "one Undo must restore the replaced page, not delete both.");
            Assert.That(TownPage().Find(AddM06TacticalMissionPanel.HeaderBandName), Is.Null,
                "the restored page is the outdated shape that was replaced.");
        }

        [Test]
        public void Updater_RepairsAStaleControllerBindingWithNoSceneObject()
        {
            GameObject host = Create("StaleTacticalHost", town, typeof(TacticalMissionPanel));
            SetReference(controller, "tacticalMissionPanel",
                host.GetComponent<TacticalMissionPanel>());
            Object.DestroyImmediate(host);
            Assert.That(GetReference(controller, "tacticalMissionPanel"), Is.Null,
                "a deleted root leaves the binding empty in the scene");
            SerializedProperty binding =
                new SerializedObject(controller).FindProperty("tacticalMissionPanel");
            TestContext.WriteLine($"stale binding: value={binding.objectReferenceValue}; " +
                                  $"entityId={binding.objectReferenceEntityIdValue}");

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success),
                "a deleted root must not keep the updater answering ExistingTacticalPanel");
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(GetReference(controller, "tacticalMissionPanel"),
                Is.SameAs(root.GetComponent<TacticalMissionPanel>()));
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error), Is.True, error);
        }

        [Test]
        public void Validator_RequiresEveryHeaderLabelInsideTheBand()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Transform page = TownPage();
            var title = (RectTransform)page.Find(AddM06TacticalMissionPanel.TitleTextName);
            Vector2 placed = title.anchoredPosition;

            title.anchoredPosition = new Vector2(0f, -60f);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string error), Is.False,
                "a title outside the header band would render pale on pale");
            Assert.That(error, Does.Contain("header band"));

            title.anchoredPosition = placed;
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out _), Is.True);
        }

        [Test]
        public void GeneratedPage_ObjectiveFitsItsTwoLinesWithoutTruncation()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            panel.Open();

            Transform page = Page();
            var objective = page.Find(AddM06TacticalMissionPanel.ObjectiveTextName)
                .GetComponent<Text>();
            // The saved scene must keep the serializable built-in font, and the panel only
            // swaps in the CJK serif while playing. EditMode never enters play mode, so this
            // test applies the same runtime face before measuring; otherwise it would measure
            // legacy metrics for type the player never sees.
            Font runtimeFont = RuntimeSerif();
            Assert.That(runtimeFont == null, Is.False,
                "the runtime serif must be a live font handle, not a destroyed one");
            string face = runtimeFont.name;
            objective.font = runtimeFont;
            Assert.That(objective.font, Is.SameAs(runtimeFont));
            Assert.That(objective.text, Does.Contain("\n"),
                "the authored briefing is a multi-line block");
            Assert.That(objective.preferredHeight, Is.LessThanOrEqualTo(
                    objective.rectTransform.rect.height + 0.5f),
                $"the briefing needs {objective.preferredHeight:F1} of " +
                $"{objective.rectTransform.rect.height:F1} units in {face}, " +
                "so it would be truncated");
            Assert.That(objective.preferredWidth, Is.LessThanOrEqualTo(
                    objective.rectTransform.rect.width + 0.5f),
                "a wrapped line wider than the rect would clip sideways");
        }

        [Test]
        public void GeneratedPage_CandidateRowButtonDrivesTheSelection()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            panel.Open();
            int before = panel.CandidateCount;
            Assert.That(before, Is.GreaterThanOrEqualTo(2));

            GameObject row = CandidateRow(Page(), 0);
            string rowName = row.name;
            row.GetComponent<Button>().onClick.Invoke();

            Assert.That(panel.SelectedCount, Is.EqualTo(1),
                "a real row click must reach the panel, not just the ClickCandidate seam");
            Assert.That(panel.CandidateCount, Is.EqualTo(before),
                "the click rebuilds the list instead of appending rows");

            GameObject next = CandidateRow(Page(), 0);
            Assert.That(next.name, Is.EqualTo(rowName));
            Assert.That(next, Is.Not.SameAs(row),
                "the clicked row retires and a fresh row replaces it");
            Assert.That(row == null || !row.activeSelf, Is.True,
                "the retired row must be inactive before it is destroyed");
            next.GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.SelectedCount, Is.EqualTo(0));
        }

        [Test]
        public void GeneratedGrid_ComposesTheSightGlyphOnEveryVisibleCell()
        {
            // The contract is composed, not substituted: terrain or occupant first, then the
            // sight cue. A wall, the dispatch and an operative all keep their own glyph and
            // still read as sighted, which colour alone could not carry.
            string[] occupants = { "", "报", "忍一", "忍二", "忍报", "守一", "守二", "守" };
            foreach (TacticalTileKind kind in System.Enum.GetValues(typeof(TacticalTileKind)))
            {
                foreach (string occupant in occupants)
                {
                    string terrain = TerrainGlyph(kind);
                    string clear = occupant.Length > 0 ? occupant : terrain;
                    Assert.That(TacticalMissionPanel.CellMarker(kind, occupant, false),
                        Is.EqualTo(clear), "an unseen cell keeps its plain glyph.");
                    Assert.That(TacticalMissionPanel.CellMarker(kind, occupant, true),
                        Is.EqualTo(clear + TacticalMissionPanel.SightGlyph),
                        $"a sighted {kind} holding '{occupant}' must append the cue.");
                }
            }
            Assert.That(TacticalMissionPanel.CellMarker(TacticalTileKind.Wall, "", true),
                Is.EqualTo("壁目"), "a sighted wall keeps its terrain glyph and shows sight.");
            Assert.That(TacticalMissionPanel.CellMarker(TacticalTileKind.Floor, "报", true),
                Is.EqualTo("报目"), "a sighted dispatch keeps its occupant glyph.");
            Assert.That(TacticalMissionPanel.CellMarker(TacticalTileKind.Floor, "忍一", true),
                Is.EqualTo("忍一目"), "a sighted operative keeps its occupant glyph.");

            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            Transform page = Page();
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            panel.Open();

            string legend = page.Find(AddM06TacticalMissionPanel.LegendTextName)
                .GetComponent<Text>().text;
            foreach (string glyph in new[] { "壁", "掩", "撤", "报", "忍", "守",
                         TacticalMissionPanel.SightGlyph })
                Assert.That(legend, Does.Contain(glyph), "the legend must explain " + glyph);

            int selected = 0;
            for (int i = 0; i < panel.CandidateCount && selected < 2; i++)
            {
                if (!panel.CandidateInteractable(i))
                    continue;
                panel.ClickCandidate(i);
                selected++;
            }
            panel.ClickStart();
            Assert.That(session.IsTacticalMissionActive, Is.True);

            int sighted = 0;
            int composed = 0;
            int cueMissing = 0;
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    if (!panel.CellInGuardSight(x, y))
                        continue;
                    sighted++;
                    string label = panel.CellLabel(x, y);
                    if (!label.EndsWith(TacticalMissionPanel.SightGlyph,
                            System.StringComparison.Ordinal))
                        cueMissing++;
                    else if (label.Length > TacticalMissionPanel.SightGlyph.Length)
                        composed++;
                }
            }
            Assert.That(sighted, Is.GreaterThan(0), "the operation must expose guard sight");
            Assert.That(cueMissing, Is.Zero,
                "every cell in guard sight must carry the sight glyph in its label");
            Assert.That(composed, Is.GreaterThan(0),
                "at least one cell in guard sight must keep its own glyph in front of the cue");

            // Terrain and occupant meaning survives the cue on real cells.
            Assert.That(panel.CellLabel(6, 4), Does.StartWith("报"));
            Assert.That(panel.CellLabel(3, 1), Does.StartWith("壁"));
            TestContext.WriteLine($"guard sight: {sighted} cells, {composed} composed, " +
                                  $"{cueMissing} missing the cue");
        }

        private static string TerrainGlyph(TacticalTileKind kind) =>
            kind == TacticalTileKind.Wall ? "壁"
            : kind == TacticalTileKind.Cover ? "掩"
            : kind == TacticalTileKind.Extraction ? "撤"
            : string.Empty;

        [Test]
        public void GeneratedPage_KeepsTheAuthoredTypeAtTheReferenceCanvas()
        {
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            town.gameObject.SetActive(true);
            Transform root = town.Find(AddM06TacticalMissionPanel.RootName);
            TacticalMissionPanel panel = root.GetComponent<TacticalMissionPanel>();
            var page = (RectTransform)Page();
            var canvasRect = (RectTransform)All().Single(t => t.name == "Canvas");

            // The 1280x720 canvas is the authored size, and the page must not shrink there.
            panel.Open();
            Assert.That(page.localScale.x, Is.EqualTo(1f).Within(0.0001f),
                "the reference canvas must not shrink the authored type");
            Text body = page.Find(AddM06TacticalMissionPanel.DateTextName).GetComponent<Text>();
            Assert.That(body.fontSize, Is.GreaterThanOrEqualTo(14));
            Assert.That(body.fontSize * page.localScale.x, Is.GreaterThanOrEqualTo(14f - 0.001f));

            canvasRect.sizeDelta = new Vector2(300f, 200f);
            panel.Refresh();
            Assert.That(page.localScale.x,
                Is.GreaterThanOrEqualTo(TacticalMissionPanel.MinimumPageScale),
                "a pathological parent may shrink the page but never below its floor");
        }

        private Transform[] Named(string name) => All().Where(t => t.name == name).ToArray();

        private static GameObject CandidateRow(Transform page, int index)
        {
            Transform content = page
                .Find(AddM06TacticalMissionPanel.CandidateScrollName)
                .Find(AddM06TacticalMissionPanel.CandidateViewportName)
                .Find(AddM06TacticalMissionPanel.CandidateContentName);
            GameObject row = null;
            int seen = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;
                if (seen == index)
                    row = child.gameObject;
                seen++;
            }
            Assert.That(row, Is.Not.Null, "candidate row " + index + " must exist");
            return row;
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

        private static Transform Cell(Transform page, int x, int y) => page
            .Find(AddM06TacticalMissionPanel.GridName)
            .Find(AddM06TacticalMissionPanel.CellNamePrefix + x + "_" + y);

        private static Transform Grid(Transform page) =>
            page.Find(AddM06TacticalMissionPanel.GridName);

        /// <summary>
        /// Builds the saved M05 commission root, page, entry button and component with the
        /// rects FoundationScene holds, so preservation is asserted by object reference.
        /// </summary>
        private M5Fixture CreateM5CommissionEntry()
        {
            GameObject root = Create(ShadowEscortCommissionPanel.RootObjectName, town,
                typeof(ShadowEscortCommissionPanel));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            GameObject page = Create(ShadowEscortCommissionPanel.PageObjectName, root.transform,
                typeof(Image));
            var pageRect = (RectTransform)page.transform;
            pageRect.anchorMin = pageRect.anchorMax = pageRect.pivot = new Vector2(0.5f, 0.5f);
            pageRect.anchoredPosition = Vector2.zero;
            pageRect.sizeDelta = new Vector2(1160f, 260f);

            GameObject closeObject = Create("M5CommissionCloseButton", page.transform,
                typeof(Image), typeof(Button));
            var closeRect = (RectTransform)closeObject.transform;
            closeRect.anchoredPosition = new Vector2(505f, 106f);
            closeRect.sizeDelta = new Vector2(140f, 36f);
            Button close = closeObject.GetComponent<Button>();
            page.SetActive(false);

            GameObject openObject = Create("M5CommissionOpenButton", root.transform,
                typeof(Image), typeof(Button));
            var openRect = (RectTransform)openObject.transform;
            openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(1f, 1f);
            openRect.sizeDelta = new Vector2(220f, 40f);
            openRect.anchoredPosition = new Vector2(-24f, -16f);
            Button open = openObject.GetComponent<Button>();

            ShadowEscortCommissionPanel panel = root.GetComponent<ShadowEscortCommissionPanel>();
            panel.BindReferences(session, page, open, close, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null);
            SetReference(controller, "shadowEscortCommissionPanel", panel);
            return new M5Fixture
            {
                root = root.transform,
                panel = panel,
                page = page,
                open = open,
                close = close
            };
        }

        private static bool Overlap(RectTransform first, RectTransform second)
        {
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            float minX = Mathf.Max(firstCorners.Min(c => c.x), secondCorners.Min(c => c.x));
            float maxX = Mathf.Min(firstCorners.Max(c => c.x), secondCorners.Max(c => c.x));
            float minY = Mathf.Max(firstCorners.Min(c => c.y), secondCorners.Min(c => c.y));
            float maxY = Mathf.Min(firstCorners.Max(c => c.y), secondCorners.Max(c => c.y));
            return maxX - minX > 0.5f && maxY - minY > 0.5f;
        }

        private sealed class M5Fixture
        {
            public Transform root;
            public ShadowEscortCommissionPanel panel;
            public GameObject page;
            public Button open;
            public Button close;
        }

        private int ObjectCount() => All().Length;

        private void AssertNoPanel()
        {
            Assert.That(All().Any(t =>
                t.name == AddM06TacticalMissionPanel.RootName ||
                t.name == AddM06TacticalMissionPanel.OverlayName ||
                t.GetComponent<TacticalMissionPanel>() != null), Is.False);
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
