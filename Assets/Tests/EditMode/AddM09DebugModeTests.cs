using System;
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
    /// M09 transactional scene-updater coverage: preflight guards, idempotent integration,
    /// inactive-by-default dev entry outside the title overlay, binding validation, the
    /// runtime-built window, and Undo rollback without reloading or saving any scene.
    /// </summary>
    public sealed class AddM09DebugModeTests
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
            GameObject canvas = Create("Canvas", null, typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler));
            UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.enabled = false;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.localScale = Vector3.one;
            GameObject map = Create("M2WorldMapRoot", canvas.transform,
                typeof(WorldMapController));
            map.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            GameObject town = Create("TownMenuRoot", map.transform);
            town.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            town.SetActive(false);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
            GameSession session = Create("GameSession", null, typeof(GameSession))
                .GetComponent<GameSession>();
            session.InitNewGame();
            // The Debug Mode entry lives inside the real M6 title page, so every test runs
            // against the screen the M6 updater actually produces.
            Assert.That(AddM6TitleScreen.Update(_scene),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success),
                "fixture must start from a valid M6 title screen.");
            Create("EventChoiceRoot", canvas.transform);
            Create("M6TacticalMissionOverlay", canvas.transform);
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
        public void Update_CreatesAnInactiveRootAndEntryUnderTheTitleRoot()
        {
            Transform canvas = Single("Canvas");
            Transform titleRoot = Single("M6TitleRoot");
            Transform tactical = Single("M6TacticalMissionOverlay");
            int canvasChildren = canvas.childCount;
            Assert.That(tactical.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "fixture precondition: the M06 tactical overlay is the last Canvas child.");

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Assert.That(AddM09DebugMode.Validate(_scene, out string error), Is.True, error);

            Transform root = Single(AddM09DebugMode.RootName);
            Transform entry = Single(AddM09DebugMode.EntryName);
            Assert.That(root.parent.name, Is.EqualTo("Canvas"));
            Assert.That(root.gameObject.activeSelf, Is.False,
                "the window root stays inactive until the debug entry opens it.");
            Assert.That(canvas.childCount, Is.EqualTo(canvasChildren + 1),
                "only the window root is added at Canvas level.");
            Assert.That(entry.parent, Is.SameAs(titleRoot),
                "the saved entry belongs to the title page, not the Canvas modal order.");
            Assert.That(entry.GetSiblingIndex(), Is.EqualTo(titleRoot.childCount - 1),
                "the entry is the last child of the title root.");
            Assert.That(entry.gameObject.activeSelf, Is.False,
                "the entry stays inactive in the scene; only guarded runtime code shows it.");
            Assert.That(tactical.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "the M06 tactical overlay must remain the last Canvas child.");
            Assert.That(root.GetSiblingIndex(), Is.LessThan(titleRoot.GetSiblingIndex()),
                "the window root renders below the title overlay.");
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(Single("M2WorldMapRoot").GetSiblingIndex()));
            Assert.That(root.GetComponent<DebugModePanel>(), Is.Null,
                "no guarded component may be serialized; release players do not compile it.");
            Assert.That(AddM09DebugMode.DescribeForReport(_scene),
                Is.EqualTo("Debug Mode root, entry, and bindings."));
            Assert.That(Reference("debugModeRoot", Controller()),
                Is.SameAs(root.gameObject),
                "the controller owns the root so the guarded code can attach the window.");

            // Release safety: the saved objects use only built-in components, so a release
            // player loads them without a missing script, and the inactive entry has no
            // persistent click handler that could fire before the guarded code binds it.
            var savedComponents = entry.GetComponents<Component>()
                .Select(component => component.GetType().Name).ToArray();
            Assert.That(savedComponents, Does.Contain("Image"));
            Assert.That(savedComponents, Does.Contain("Button"));
            Assert.That(entry.GetComponent<Button>().onClick.GetPersistentEventCount(),
                Is.Zero);
            Assert.That(root.GetComponents<Component>()
                    .Select(component => component.GetType().Name).ToArray(),
                Is.EquivalentTo(new[] { "RectTransform", "CanvasRenderer", "Image" }),
                "the saved root uses only release-safe built-in components.");
            Assert.That(root.GetComponent<Image>().color,
                Is.EqualTo(SengokuUiTheme.IndigoDeep),
                "the updater must use the shared theme instead of a hard-coded colour.");
            Assert.That(entry.GetComponentInChildren<Text>(true).color,
                Is.EqualTo(SengokuUiTheme.ParchmentLight));
            Assert.That(((RectTransform)root).sizeDelta,
                Is.EqualTo(DebugModePanel.WindowSize),
                "the saved root and the runtime layout share one size authority.");

            var entryRect = (RectTransform)entry;
            Assert.That(entryRect.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(entryRect.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(entryRect.pivot, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(entryRect.anchoredPosition,
                Is.EqualTo(new Vector2(-16f, 8f)),
                "the saved entry position must be explicit, not inherited from SetParent.");
            Assert.That(entryRect.sizeDelta, Is.EqualTo(new Vector2(200f, 32f)));
        }

        [Test]
        public void Update_BindsTheWindowRootAndTheTitleController()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));

            Transform root = Single(AddM09DebugMode.RootName);
            TitleScreenController controller = Controller();
            GameSession session = Session();
            Button entry = Single(AddM09DebugMode.EntryName).GetComponent<Button>();

            Assert.That(Reference("gameSession", controller), Is.SameAs(session));
            Assert.That(Reference("debugModeButton", controller), Is.SameAs(entry));
            Assert.That(Reference("debugModeRoot", controller), Is.SameAs(root.gameObject));
        }

        [Test]
        public void Update_SecondRunIsIdempotent()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Transform root = Single(AddM09DebugMode.RootName);
            Transform entry = Single(AddM09DebugMode.EntryName);
            int children = root.parent.childCount;

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));

            Assert.That(FindAll(AddM09DebugMode.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM09DebugMode.EntryName).Length, Is.EqualTo(1));
            Assert.That(Single(AddM09DebugMode.RootName), Is.SameAs(root));
            Assert.That(Single(AddM09DebugMode.EntryName), Is.SameAs(entry));
            Assert.That(root.parent.childCount, Is.EqualTo(children));
        }

        [Test]
        public void SceneRoot_IsTurnedIntoTheWindowAtRuntimeAndCloseKeepsTheSession()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Transform root = Single(AddM09DebugMode.RootName);
            Assert.That(root.GetComponent<DebugModePanel>(), Is.Null);
            // A preview scene never runs MonoBehaviour lifecycle, so the start page has to be
            // shown explicitly; the real Start() does this before the entry can be pressed.
            TitleScreenController controller = Controller();
            controller.ShowTitle();
            Assert.That(controller.StartDebugSession(), Is.True);

            DebugModePanel panel = root.GetComponent<DebugModePanel>();
            Assert.That(panel, Is.Not.Null,
                "the guarded controller attaches the window component on first use.");
            Assert.That(Single(AddM09DebugMode.EntryName).parent.name, Is.EqualTo("Canvas"),
                "the guarded runtime re-parents the saved title entry so it stays reachable " +
                "while the title is hidden.");
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(root.Find("DebugClose"), Is.Not.Null);
            Assert.That(root.Find("DebugSessionContent"), Is.Not.Null);
            Assert.That(panel.Session, Is.Not.Null);
            Assert.That(panel.Session.IsActive, Is.True);

            panel.Close();

            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.Session.IsActive, Is.True,
                "closing only hides the panel; the debug session keeps its state.");

            panel.ExitDebugSession();
            Assert.That(panel.Session, Is.Null);
            Assert.That(root.GetComponent<DebugModePanel>(), Is.Not.Null,
                "the runtime component stays on the root for the next session.");
        }


        [Test]
        public void Update_RepairsTheCanvasLevelEntryInPlaceAndPreservesM06Order()
        {
            // Reproduce the previously integrated shape: the entry was the last Canvas child,
            // which pushed the M06 tactical overlay out of its required final position.
            Transform canvas = Single("Canvas");
            Transform titleRoot = Single("M6TitleRoot");
            Transform tactical = Single("M6TacticalMissionOverlay");
            Button entry = BuildPreviousShape(out GameObject root, out Transform titleRootBuilt);
            Assert.That(titleRootBuilt, Is.SameAs(titleRoot));
            Assert.That(tactical.GetSiblingIndex(), Is.LessThan(canvas.childCount - 1),
                "fixture precondition: the old entry shape displaces the M06 overlay.");

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));

            Assert.That(Single(AddM09DebugMode.EntryName), Is.SameAs(entry.transform),
                "the repair must move the existing entry instead of recreating it.");
            Assert.That(entry.transform.parent, Is.SameAs(titleRoot));
            Assert.That(entry.transform.GetSiblingIndex(),
                Is.EqualTo(titleRoot.childCount - 1));
            Assert.That(entry.gameObject.activeSelf, Is.False);
            Assert.That(tactical.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "the M06 tactical overlay is the final Canvas child again.");
            Assert.That(Single(AddM09DebugMode.RootName), Is.SameAs(root.transform));
            Assert.That(AddM09DebugMode.Validate(_scene, out string error), Is.True, error);

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success),
                "a repaired scene is idempotent.");
            Assert.That(Single(AddM09DebugMode.EntryName), Is.SameAs(entry.transform));
        }

        [Test]
        public void Repair_RollsBackWhenAnOriginalComponentSetChanges()
        {
            Button entry = BuildPreviousShape(out _, out _);

            AddM09DebugMode.UpdateResult result = AddM09DebugMode.Update(_scene, _ =>
                UnityEditor.Undo.AddComponent<UnityEngine.UI.LayoutElement>(entry.gameObject));

            Assert.That(result, Is.EqualTo(AddM09DebugMode.UpdateResult.OriginalObjectsChanged),
                "the narrow repair guard must still reject a changed component set.");
            Assert.That(entry.transform.parent.name, Is.EqualTo("Canvas"),
                "rollback restores the original Canvas parent.");
            Assert.That(entry.GetComponent<UnityEngine.UI.LayoutElement>(), Is.Null,
                "rollback removes the injected component.");
        }

        private Button BuildPreviousShape(out GameObject root, out Transform titleRoot)
        {
            Transform canvas = Single("Canvas");
            titleRoot = Single("M6TitleRoot");
            root = Create(AddM09DebugMode.RootName, canvas, typeof(Image));
            ((RectTransform)root.transform).sizeDelta = DebugModePanel.WindowSize;
            // The prior updater already placed the window below the title overlay; only the
            // entry sat at Canvas level and disturbed the M06 modal order.
            root.transform.SetSiblingIndex(titleRoot.GetSiblingIndex());
            root.SetActive(false);
            GameObject entryObject = Create(AddM09DebugMode.EntryName, canvas, typeof(Image),
                typeof(Button));
            Button entry = entryObject.GetComponent<Button>();
            Text label = CreateIn(_scene, "Label", entryObject.transform, typeof(Text))
                .GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "Debug 综合测试";
            entry.transform.SetAsLastSibling();
            entryObject.SetActive(false);
            SetReference("gameSession", Controller(), Session());
            SetReference("debugModeButton", Controller(), entry);
            SetReference("debugModeRoot", Controller(), root);
            SetReference("titleRoot", Controller(), titleRoot.gameObject);
            return entry;
        }

        private static void SetReference(string field, UnityEngine.Object target,
            UnityEngine.Object value)
        {
            var serialized = new UnityEditor.SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void Validate_RejectsASerializedWindowComponent()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Single(AddM09DebugMode.RootName).gameObject.AddComponent<DebugModePanel>();

            Assert.That(AddM09DebugMode.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("must not serialize a DebugModePanel"));
        }

        [Test]
        public void Update_FaultInjectionRollsBackTheWholeUndoGroup()
        {
            TitleScreenController controller = Controller();
            UnityEngine.Object buttonBefore = Reference("debugModeButton", controller);
            UnityEngine.Object rootBefore = Reference("debugModeRoot", controller);
            UnityEngine.Object sessionBefore = Reference("gameSession", controller);
            UnityEngine.Object titleBefore = Reference("titleRoot", controller);
            Assert.That(sessionBefore, Is.Not.Null,
                "fixture precondition: the M6 screen already bound the GameSession.");

            AddM09DebugMode.UpdateResult result = AddM09DebugMode.Update(_scene, root =>
            {
                Assert.That(root.transform.parent.name, Is.EqualTo("Canvas"));
                root.name = "BrokenDebugRoot";
            });

            Assert.That(result, Is.EqualTo(AddM09DebugMode.UpdateResult.ValidationFailed));
            Assert.That(FindAll(AddM09DebugMode.RootName), Is.Empty);
            Assert.That(FindAll(AddM09DebugMode.EntryName), Is.Empty);
            Assert.That(FindAll("BrokenDebugRoot"), Is.Empty);
            Assert.That(_scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DebugModePanel>(true)).ToArray(),
                Is.Empty);
            Assert.That(Reference("debugModeButton", controller), Is.SameAs(buttonBefore),
                "rollback must restore the controller's exact pre-existing references.");
            Assert.That(Reference("debugModeRoot", controller), Is.SameAs(rootBefore));
            Assert.That(Reference("gameSession", controller), Is.SameAs(sessionBefore),
                "the M6-bound GameSession reference survives the rollback unchanged.");
            Assert.That(Reference("titleRoot", controller), Is.SameAs(titleBefore));
        }

        [Test]
        public void Update_RejectsAPartialIntegrationInsteadOfDuplicatingIt()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Create(AddM09DebugMode.RootName, Single("Canvas"));

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.ExistingPartialControl));
            Assert.That(FindAll(AddM09DebugMode.RootName).Length, Is.EqualTo(2));
        }


        [Test]
        public void Transaction_CrossChecksTheM6TitleScreenRules()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Assert.That(AddM09DebugMode.ValidateTitleScreenOrdering(_scene, out string clean),
                Is.True, clean);

            // Break an M6 rule while keeping the M09 entry in its own valid last position.
            Transform titleRoot = Single("M6TitleRoot");
            Create("UnexpectedTitleChild", titleRoot);
            Single(AddM09DebugMode.EntryName).SetAsLastSibling();
            Assert.That(AddM09DebugMode.Validate(_scene, out _), Is.True,
                "the M09 shape alone still looks valid.");
            Assert.That(AddM09DebugMode.ValidateTitleScreenOrdering(_scene, out string error),
                Is.False);
            Assert.That(error, Does.Contain("M6 title root"));
        }

        [Test]
        public void Update_RollsBackWhenTheM6TitleScreenIsAlreadyInvalid()
        {
            Create("UnexpectedTitleChild", Single("M6TitleRoot"));
            TitleScreenController controller = Controller();
            UnityEngine.Object buttonBefore = Reference("debugModeButton", controller);
            UnityEngine.Object rootBefore = Reference("debugModeRoot", controller);

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.ValidationFailed));
            Assert.That(FindAll(AddM09DebugMode.RootName), Is.Empty,
                "the transaction must not persist a scene that fails the M6 rules.");
            Assert.That(FindAll(AddM09DebugMode.EntryName), Is.Empty);
            Assert.That(Reference("debugModeButton", controller), Is.SameAs(buttonBefore));
            Assert.That(Reference("debugModeRoot", controller), Is.SameAs(rootBefore));
        }

        [Test]
        public void Update_KeepsTheM6TitleScreenValidAfterIntegration()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));

            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error,
                "the scene validator's M6 rules must still pass with the debug entry present.");
            Transform titleRoot = Single("M6TitleRoot");
            Assert.That(titleRoot.childCount, Is.EqualTo(3));
            Assert.That(titleRoot.GetChild(0).name,
                Is.EqualTo(AddM6TitleScreen.MainMenuRootName));
            Assert.That(titleRoot.GetChild(1).name,
                Is.EqualTo(AddM6TitleScreen.ScenarioSelectRootName));
            Assert.That(titleRoot.GetChild(2).name, Is.EqualTo(AddM09DebugMode.EntryName));
        }

        [Test]
        public void Validate_RejectsAnEntryOutsideTheTitleRootOrAnActiveSavedEntry()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Transform entry = Single(AddM09DebugMode.EntryName);
            Transform canvas = Single("Canvas");
            Transform titleRoot = Single("M6TitleRoot");

            entry.SetParent(canvas, false);
            Assert.That(AddM09DebugMode.Validate(_scene, out string outsideTitle), Is.False);
            Assert.That(outsideTitle, Does.Contain("child of M6TitleRoot"));

            entry.SetParent(titleRoot, false);
            entry.gameObject.SetActive(true);
            Assert.That(AddM09DebugMode.Validate(_scene, out string activeEntry), Is.False);
            Assert.That(activeEntry, Does.Contain("saved inactive"));
        }

        [Test]
        public void Validate_ReportsTheMissingRootBeforeIntegration()
        {
            Assert.That(AddM09DebugMode.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain(AddM09DebugMode.RootName),
                "the pre-integration failure must name the missing window for the validator report.");
        }

        [Test]
        public void Update_RejectsMissingGameSessionOrController()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                CreateIn(scene, "Canvas", null, typeof(Canvas))
                    .GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
                Assert.That(AddM09DebugMode.Update(scene),
                    Is.EqualTo(AddM09DebugMode.UpdateResult.MissingOrDuplicateObjects));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Update_RequiresExactlyOneTitleRoot()
        {
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Single("M6TitleRoot").name = "M6TitleRootRenamed";

            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.MissingOrDuplicateObjects));
        }

        [Test]
        public void Preflight_RejectsWrongSceneDirtyAndPlaying()
        {
            Assert.That(AddM09DebugMode.CheckScenePreflight("Assets/Other.unity", false),
                Is.EqualTo(AddM09DebugMode.UpdateResult.SceneMismatch));
            Assert.That(AddM09DebugMode.CheckScenePreflight(AddM09DebugMode.ScenePath, true),
                Is.EqualTo(AddM09DebugMode.UpdateResult.SceneDirty));
            Assert.That(AddM09DebugMode.CheckScenePreflight(AddM09DebugMode.ScenePath, false, true),
                Is.EqualTo(AddM09DebugMode.UpdateResult.EditorPlaying));
            Assert.That(AddM09DebugMode.CheckScenePreflight(AddM09DebugMode.ScenePath, false),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
        }

        private TitleScreenController Controller() => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<TitleScreenController>(true))
            .Single();

        private GameSession Session() => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<GameSession>(true))
            .Single();

        private static UnityEngine.Object Reference(string field, UnityEngine.Object target) =>
            new UnityEditor.SerializedObject(target).FindProperty(field)?.objectReferenceValue;

        private Transform Single(string name)
        {
            Transform[] matches = FindAll(name);
            Assert.That(matches.Length, Is.EqualTo(1), "expected one " + name);
            return matches[0];
        }

        private Transform[] FindAll(string name) => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name == name).ToArray();

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
    }
}
