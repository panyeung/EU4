using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Adds the Phase 1 title screen and demo summary to FoundationScene in one Undo group.
    /// The title overlay has two pages: a two-action main menu and a scenario selector.
    /// A complete, recognised legacy title hierarchy is rebuilt in place; any partial or
    /// unknown hierarchy is refused without deleting or saving anything. Validation always
    /// runs before saving, and failures roll the whole Undo group back.
    /// </summary>
    public static class AddM6TitleScreen
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string CanvasName = "Canvas";
        public const string RootName = "M6TitleRoot";
        public const string MainMenuRootName = "M6MainMenuRoot";
        public const string ScenarioSelectRootName = "M6ScenarioSelectRoot";
        public const string LabelName = "M6TitleLabel";
        public const string SubtitleName = "M6TitleSubtitle";
        public const string SealName = "M6SealImage";
        public const string NewGameButtonName = "M6NewGameButton";
        public const string ContinueButtonName = "M6ContinueButton";
        public const string StatusTextName = "M6TitleStatusText";
        public const string ScenarioHeadingName = "M6ScenarioSelectHeading";
        public const string MainlineSectionName = "M6ScenarioSectionMainline";
        public const string TopicsSectionName = "M6ScenarioSectionTopics";
        public const string ScenarioBackButtonName = "M6ScenarioBackButton";
        public const string FixedScenarioButtonName = "SC013FixedScenarioButton";
        public const string DemoSummaryTextName = "M6DemoSummaryText";
        public const string EventChoiceRootName = "EventChoiceRoot";

        /// <summary>
        /// The M06 tactical sheet is a hidden canvas-level overlay that must stay the last
        /// Canvas child, so it legitimately sits above the inactive title overlay.
        /// </summary>
        public const string TacticalOverlayName = TacticalMissionPanel.OverlayObjectName;
        public const string StandardSampleButtonName = "SC013StandardSampleButton";
        public const string CrisisDemoButtonName = "SC013CrisisDemoButton";
        public const string EducationDemoButtonName = "SC020EducationDemoButton";
        public const string ResearchDemoButtonName = "SC021ResearchDemoButton";
        public const string FamilyFormationDemoButtonName = "SC022FamilyFormationDemoButton";
        public const string SeclusionDemoButtonName = "SC023SeclusionDemoButton";
        public const string FamilyDoctrineDemoButtonName = "SC024FamilyDoctrineDemoButton";

        public const string BackgroundAssetPath = "Assets/Art/UI/start_screen_background.png";
        public const string SealAssetPath = "Assets/Art/UI/ReferenceArt/seal.png";

        private const string PaperAssetPath = "Assets/Art/UI/paper_grain.png";
        private const string IndigoPatternAssetPath = "Assets/Resources/Generated/indigo_pattern_tile.png";
        private const string IndigoPatternSpriteName = "indigo_pattern_tile_0";

        private const string MapRootName = "M2WorldMapRoot";
        private const string TownRootName = "TownMenuRoot";
        private const string MenuPath = "Tools/Sengoku Chronicle/Add M6 Title Screen";
        private const string UndoName = "Add M6 Title Screen";
        private const float MapWidth = 1216f;
        private const float MapHeight = 268f;
        private const float DesignWidth = 1280f;
        private const float DesignHeight = 720f;
        private const float MainPanelWidth = 452f;

        private static readonly string[] ScenarioButtonNames =
        {
            StandardSampleButtonName, CrisisDemoButtonName, EducationDemoButtonName,
            ResearchDemoButtonName, FamilyFormationDemoButtonName, SeclusionDemoButtonName,
            FamilyDoctrineDemoButtonName
        };

        private static readonly string[] BaseControlNames =
        {
            LabelName, NewGameButtonName, ContinueButtonName, StatusTextName
        };

        // Overlap guard: the demo summary must not cover these M5 controls when they exist.
        private static readonly string[] NearbyControlNames =
        {
            "M5SaveButton", "M5LoadButton", "M5SaveStatusText", "ConfirmButton", "CancelButton"
        };

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, InvalidHierarchyOrBindings, ExistingPanel,
            ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.EditorPlaying : CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("M6 title screen validated; FoundationScene saved.");
            else
                Debug.LogError($"M6 title updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>
        /// Batch entry point: opens FoundationScene, runs the transactional updater,
        /// validates, and saves only after validation; then writes the scene report.
        /// </summary>
        public static void RunBatchIntegration()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);

            FoundationSceneValidator.ValidateActiveScene();
            if (result != UpdateResult.Success)
                throw new InvalidOperationException($"M6 title integration failed: {result}.");

            Debug.Log("M6 title integration completed and FoundationScene was saved.");
        }

        /// <summary>Structural test seam. It never saves the supplied scene.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault-injection seam, executed inside the same Undo transaction as construction.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return Invalid(out error, "Scene is invalid or not loaded.");

            Transform[] canvasAll = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            if (canvasAll.Length != 1)
                return Invalid(out error, "Expected exactly one Canvas root.");
            Transform canvas = canvasAll[0];

            Transform map = canvas.Find(MapRootName);
            Transform town = map != null ? map.Find(TownRootName) : null;
            if (map == null || town == null)
                return Invalid(out error, "Missing M2WorldMapRoot or TownMenuRoot.");
            if (Components<WorldMapController>(scene).Length != 1)
                return Invalid(out error, "Expected exactly one WorldMapController.");

            GameSession[] sessions = Components<GameSession>(scene);
            if (sessions.Length != 1)
                return Invalid(out error, "Expected exactly one GameSession.");
            GameSession session = sessions[0];

            Transform[] roots = Named(scene, RootName);
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            DemoSummaryPanel[] panels = Components<DemoSummaryPanel>(scene);
            Transform[] summaryTexts = Named(scene, DemoSummaryTextName);
            if (roots.Length != 1 || controllers.Length != 1 || panels.Length != 1 || summaryTexts.Length != 1)
                return Invalid(out error, "Expected exactly one M6 title root, controller, panel, and summary text.");

            Transform root = roots[0];
            TitleScreenController controller = controllers[0];
            DemoSummaryPanel panel = panels[0];
            Transform summary = summaryTexts[0];

            if (root.parent != canvas)
                return Invalid(out error, "M6 title root must be a direct child of Canvas.");
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            Transform tacticalOverlay = canvas.Find(TacticalOverlayName);
            if (tacticalOverlay != null && tacticalOverlay.GetSiblingIndex() < root.GetSiblingIndex())
                return Invalid(out error,
                    "The M06 tactical overlay must render above the M6 title root.");
            if (eventRoot != null)
            {
                if (root.GetSiblingIndex() != eventRoot.GetSiblingIndex() - 1)
                    return Invalid(out error,
                        "M6 title root must be immediately below EventChoiceRoot when it exists.");
            }
            else if (root.GetSiblingIndex() != (tacticalOverlay != null
                ? tacticalOverlay.GetSiblingIndex() - 1
                : canvas.childCount - 1))
            {
                return Invalid(out error,
                    "M6 title root must be the last Canvas child beneath the M06 tactical overlay when EventChoiceRoot is absent.");
            }
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform sibling = canvas.GetChild(i);
                if (sibling != root && sibling != eventRoot && sibling != tacticalOverlay &&
                    sibling.GetSiblingIndex() > root.GetSiblingIndex())
                    return Invalid(out error,
                        "Only EventChoiceRoot and the M06 tactical overlay may render above the full-screen M6 title overlay.");
            }
            if (root.gameObject.activeSelf)
                return Invalid(out error, "M6 title root must be saved inactive; Play Mode shows it.");
            if (root.childCount != 2)
                return Invalid(out error, "M6 title root must contain exactly the main-menu and selector pages.");

            RectTransform rootRect = root as RectTransform;
            if (rootRect == null || rootRect.anchorMin != Vector2.zero || rootRect.anchorMax != Vector2.one ||
                rootRect.offsetMin != Vector2.zero || rootRect.offsetMax != Vector2.zero)
                return Invalid(out error, "M6 title root must stretch to the full Canvas.");

            Sprite expectedBackground = LoadBackgroundSprite();
            Image background = root.GetComponent<Image>();
            if (background == null || !background.raycastTarget || background.color.a < 1f ||
                expectedBackground == null || background.sprite != expectedBackground)
                return Invalid(out error,
                    "M6 title background must use the start-screen Sprite as an opaque raycast-blocking image.");

            Transform main = root.Find(MainMenuRootName);
            Transform selector = root.Find(ScenarioSelectRootName);
            if (main == null || selector == null)
                return Invalid(out error, "Missing M6MainMenuRoot or M6ScenarioSelectRoot.");
            if (!main.gameObject.activeSelf || selector.gameObject.activeSelf)
                return Invalid(out error, "M6 title must boot on the main menu with the selector hidden.");
            if (!LeftPanel(main))
                return Invalid(out error, "M6 main menu must be a left-side panel of the full-screen overlay.");
            if (!FullStretch(selector) || selector.GetComponent<Image>() == null)
                return Invalid(out error, "M6 scenario selector must be a full-screen image panel.");

            Transform label = main.Find(LabelName);
            Transform subtitle = main.Find(SubtitleName);
            Transform seal = main.Find(SealName);
            Transform newGame = main.Find(NewGameButtonName);
            Transform continueButton = main.Find(ContinueButtonName);
            Transform status = main.Find(StatusTextName);
            if (label == null || subtitle == null || seal == null || newGame == null ||
                continueButton == null || status == null)
                return Invalid(out error, "Missing a main-menu control.");
            if (main.childCount != 6)
                return Invalid(out error, "M6 main menu must contain only the title, subtitle, seal, two actions, and status.");
            foreach (string scenarioName in ScenarioButtonNames)
            {
                if (main.Find(scenarioName) != null)
                    return Invalid(out error, $"Scenario entry {scenarioName} must not appear on the main menu.");
            }

            if (main.GetComponentsInChildren<Button>(true).Length != 2)
                return Invalid(out error, "M6 main menu must expose only Continue and New Game.");

            if (label.GetComponent<Text>() == null || label.GetComponent<Text>().font == null ||
                subtitle.GetComponent<Text>() == null || subtitle.GetComponent<Text>().font == null)
                return Invalid(out error, "M6 title label or subtitle is invalid.");
            if (!ValidSeal(seal))
                return Invalid(out error, "M6 title seal image is invalid.");
            if (!ValidButton(newGame) || !ValidButton(continueButton))
                return Invalid(out error, "M6 main-menu buttons are unbound or have invalid labels.");
            Text statusText = status.GetComponent<Text>();
            if (statusText == null || statusText.font == null)
                return Invalid(out error, "M6 title status text is invalid.");

            Transform heading = selector.Find(ScenarioHeadingName);
            Transform mainlineSection = selector.Find(MainlineSectionName);
            Transform topicsSection = selector.Find(TopicsSectionName);
            Transform back = selector.Find(ScenarioBackButtonName);
            if (heading == null || mainlineSection == null || topicsSection == null || back == null)
                return Invalid(out error,
                    "M6 scenario selector is missing its heading, section labels, or back button.");
            Text headingText = heading.GetComponent<Text>();
            if (headingText == null || headingText.font == null ||
                mainlineSection.GetComponent<Text>() == null ||
                mainlineSection.GetComponent<Text>().font == null ||
                topicsSection.GetComponent<Text>() == null ||
                topicsSection.GetComponent<Text>().font == null)
                return Invalid(out error, "M6 scenario selector heading or section label is invalid.");
            if (!ValidButton(back))
                return Invalid(out error, "M6 scenario selector back button is invalid.");
            if (selector.childCount != 4 + 1 + ScenarioButtonNames.Length)
                return Invalid(out error,
                    "M6 scenario selector must contain the heading, two section labels, back button, fixed SC-013 entry, and seven scenario entries.");

            Transform fixedScenario = selector.Find(FixedScenarioButtonName);
            if (fixedScenario == null)
                return Invalid(out error, "M6 scenario selector is missing the fixed SC-013 entry.");
            if (!ValidButton(fixedScenario))
                return Invalid(out error, "M6 fixed SC-013 entry is unbound or has an invalid label.");

            Transform[] scenarioButtons = new Transform[ScenarioButtonNames.Length];
            for (int i = 0; i < ScenarioButtonNames.Length; i++)
            {
                scenarioButtons[i] = selector.Find(ScenarioButtonNames[i]);
                if (scenarioButtons[i] == null)
                    return Invalid(out error, $"M6 scenario selector is missing {ScenarioButtonNames[i]}.");
                if (!ValidButton(scenarioButtons[i]))
                    return Invalid(out error, $"M6 scenario entry {ScenarioButtonNames[i]} is invalid.");
            }

            Rect mainArea = Rect.MinMaxRect(-DesignWidth * 0.5f, -DesignHeight * 0.5f,
                -DesignWidth * 0.5f + MainPanelWidth, DesignHeight * 0.5f);
            Vector2 mainCenter = new Vector2(mainArea.x + MainPanelWidth * 0.5f, 0f);
            if (!Fits(label, mainCenter, mainArea) || !Fits(subtitle, mainCenter, mainArea) ||
                !Fits(seal, mainCenter, mainArea) || !Fits(newGame, mainCenter, mainArea) ||
                !Fits(continueButton, mainCenter, mainArea) || !Fits(status, mainCenter, mainArea))
                return Invalid(out error, "A main-menu control falls outside the left menu panel at 1280x720.");

            Rect selectorArea = Rect.MinMaxRect(-DesignWidth * 0.5f, -DesignHeight * 0.5f,
                DesignWidth * 0.5f, DesignHeight * 0.5f);
            if (!Fits(heading, Vector2.zero, selectorArea) || !Fits(back, Vector2.zero, selectorArea) ||
                !Fits(mainlineSection, Vector2.zero, selectorArea) ||
                !Fits(topicsSection, Vector2.zero, selectorArea) ||
                !Fits(fixedScenario, Vector2.zero, selectorArea))
                return Invalid(out error, "A scenario-selector control falls outside the Canvas at 1280x720.");
            foreach (Transform scenarioButton in scenarioButtons)
            {
                if (!Fits(scenarioButton, Vector2.zero, selectorArea))
                    return Invalid(out error, "A scenario-selector entry falls outside the Canvas at 1280x720.");
            }

            if (controller.transform != session.transform)
                return Invalid(out error, "TitleScreenController must live on the GameSession object.");
            WorldMapController mapController = map.GetComponent<WorldMapController>();
            if (!Bound(controller, "gameSession", session) ||
                !Bound(controller, "worldMapController", mapController) ||
                !Bound(controller, "titleRoot", root.gameObject) ||
                !Bound(controller, "mainMenuRoot", main.gameObject) ||
                !Bound(controller, "scenarioSelectionRoot", selector.gameObject) ||
                !Bound(controller, "newGameButton", newGame.GetComponent<Button>()) ||
                !Bound(controller, "continueButton", continueButton.GetComponent<Button>()) ||
                !Bound(controller, "fixedScenarioButton", fixedScenario.GetComponent<Button>()) ||
                !Bound(controller, "scenarioBackButton", back.GetComponent<Button>()) ||
                !Bound(controller, "statusText", statusText))
                return Invalid(out error, "Title screen references are not bound exactly once.");
            if (!Bound(controller, "standardSampleButton", scenarioButtons[0].GetComponent<Button>()) ||
                !Bound(controller, "crisisDemoButton", scenarioButtons[1].GetComponent<Button>()) ||
                !Bound(controller, "educationDemoButton", scenarioButtons[2].GetComponent<Button>()) ||
                !Bound(controller, "researchDemoButton", scenarioButtons[3].GetComponent<Button>()) ||
                !Bound(controller, "familyFormationDemoButton", scenarioButtons[4].GetComponent<Button>()) ||
                !Bound(controller, "seclusionDemoButton", scenarioButtons[5].GetComponent<Button>()) ||
                !Bound(controller, "familyDoctrineDemoButton", scenarioButtons[6].GetComponent<Button>()))
                return Invalid(out error, "Scenario selector entries are not bound exactly once.");

            if (panel.transform != session.transform)
                return Invalid(out error, "DemoSummaryPanel must live on the GameSession object.");
            Text summaryText = summary.GetComponent<Text>();
            if (summaryText == null || summaryText.font == null)
                return Invalid(out error, "M6 demo summary text is invalid.");
            if (!Bound(panel, "gameSession", session) || !Bound(panel, "summaryText", summaryText))
                return Invalid(out error, "Demo summary references are not bound exactly once.");

            if (summary.parent != map)
                return Invalid(out error, "Demo summary text must live under the map root.");
            if (summary.GetSiblingIndex() >= town.GetSiblingIndex())
                return Invalid(out error, "Demo summary text must render beneath the town menu.");
            if (summary.gameObject.activeSelf)
                return Invalid(out error, "Demo summary text must be saved inactive until the demo completes.");

            RectTransform mapRect = map as RectTransform;
            var summaryRect = summary as RectTransform;
            if (mapRect == null || summaryRect == null || mapRect.rect.width < MapWidth - 1f ||
                mapRect.rect.height < MapHeight - 1f || !Contained(summaryRect, mapRect))
                return Invalid(out error, "Demo summary text is outside the map bounds.");

            foreach (string name in NearbyControlNames)
            {
                foreach (Transform sibling in scene.GetRootGameObjects()
                             .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                             .Where(t => t.name == name && t != summary))
                {
                    var siblingRect = sibling as RectTransform;
                    if (siblingRect != null && Overlaps(summaryRect, siblingRect, mapRect))
                        return Invalid(out error, $"Demo summary text overlaps {name}.");
                }
            }

            return true;
        }

        private sealed class LegacyLayout
        {
            public Transform Root;
            public TitleScreenController Controller;
            public DemoSummaryPanel Panel;
            public Text Summary;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded) return UpdateResult.InvalidScene;

            Transform[] canvasAll = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            WorldMapController[] mapControllers = Components<WorldMapController>(scene);
            Transform canvas = canvasAll.Length == 1 ? canvasAll[0] : null;
            Transform map = canvas != null ? canvas.Find(MapRootName) : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            WorldMapController mapController = mapControllers.Length == 1 ? mapControllers[0] : null;
            if (canvas == null || map == null || map.Find(TownRootName) == null ||
                session == null || mapController == null)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            LegacyLayout legacy = RecognizeLegacy(scene, canvas, map, session, mapController, out string legacyError);
            if (legacy != null)
                return Rebuild(scene, save, beforeValidation, legacy);

            if (legacyError == null && !AnyTitleArtifacts(scene))
                return BuildFresh(scene, save, beforeValidation);

            if (legacyError != null)
                Debug.Log($"M6 title updater refused legacy layout: {legacyError}");
            return UpdateResult.ExistingPanel;
        }

        private static bool AnyTitleArtifacts(Scene scene)
        {
            if (Components<TitleScreenController>(scene).Length != 0 ||
                Components<DemoSummaryPanel>(scene).Length != 0)
                return true;
            return Named(scene, RootName).Length != 0 ||
                   Named(scene, MainMenuRootName).Length != 0 ||
                   Named(scene, ScenarioSelectRootName).Length != 0 ||
                   Named(scene, LabelName).Length != 0 ||
                   Named(scene, NewGameButtonName).Length != 0 ||
                   Named(scene, ContinueButtonName).Length != 0 ||
                   Named(scene, StatusTextName).Length != 0 ||
                   Named(scene, DemoSummaryTextName).Length != 0 ||
                   ScenarioButtonNames.Any(name => Named(scene, name).Length != 0) ||
                   Named(scene, FixedScenarioButtonName).Length != 0 ||
                   Named(scene, ScenarioBackButtonName).Length != 0;
        }

        /// <summary>
        /// Recognises only the complete legacy single-page title hierarchy (four base controls
        /// directly under M6TitleRoot plus the optional paired SC-010 entries and SC-020–024
        /// entries). Returns null with a reason for every partial or unknown layout.
        /// </summary>
        private static LegacyLayout RecognizeLegacy(Scene scene, Transform canvas, Transform map,
            GameSession session, WorldMapController mapController, out string error)
        {
            error = null;
            Transform[] roots = Named(scene, RootName);
            if (roots.Length == 0 && Named(scene, MainMenuRootName).Length == 0 &&
                Named(scene, ScenarioSelectRootName).Length == 0)
                return null;
            if (roots.Length != 1)
            {
                error = "expected exactly one M6TitleRoot";
                return null;
            }

            Transform root = roots[0];
            if (root.parent != canvas)
            {
                error = "M6TitleRoot must be a direct child of Canvas";
                return null;
            }
            if (root.GetSiblingIndex() <= map.GetSiblingIndex())
            {
                error = "M6TitleRoot must render above the map";
                return null;
            }
            if (root.gameObject.activeSelf)
            {
                error = "M6TitleRoot must be inactive in the saved scene";
                return null;
            }
            if (root.Find(MainMenuRootName) != null || root.Find(ScenarioSelectRootName) != null)
            {
                error = "the hierarchy already contains a page root";
                return null;
            }
            if (root.GetComponent<Image>() == null)
            {
                error = "M6TitleRoot is missing its background Image";
                return null;
            }

            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            DemoSummaryPanel[] panels = Components<DemoSummaryPanel>(scene);
            Transform[] summaries = Named(scene, DemoSummaryTextName);
            if (controllers.Length != 1 || panels.Length != 1 || summaries.Length != 1)
            {
                error = "expected exactly one controller, summary panel, and summary text";
                return null;
            }
            TitleScreenController controller = controllers[0];
            if (controller.transform != session.transform)
            {
                error = "TitleScreenController must live on the GameSession object";
                return null;
            }

            string[] allowedNames = BaseControlNames.Concat(ScenarioButtonNames).ToArray();
            var directNames = new string[root.childCount];
            for (int i = 0; i < root.childCount; i++)
            {
                directNames[i] = root.GetChild(i).name;
                if (!allowedNames.Contains(directNames[i]) || directNames.Count(n => n == directNames[i]) != 1)
                {
                    error = $"unexpected or duplicate direct child {directNames[i]}";
                    return null;
                }
            }

            bool hasStandard = root.Find(StandardSampleButtonName) != null;
            bool hasCrisis = root.Find(CrisisDemoButtonName) != null;
            if (!hasStandard || !hasCrisis)
            {
                error = "the complete legacy layout requires both SC-010 standard and crisis entries";
                return null;
            }
            foreach (string scenarioName in ScenarioButtonNames.Skip(2))
            {
                if (root.Find(scenarioName) == null)
                {
                    error = $"the complete legacy layout is missing {scenarioName}";
                    return null;
                }
            }
            if (root.childCount != BaseControlNames.Length + ScenarioButtonNames.Length)
            {
                error = "the complete legacy layout must contain exactly four base controls and seven scenario entries";
                return null;
            }

            Transform label = root.Find(LabelName);
            Transform newGame = root.Find(NewGameButtonName);
            Transform continueButton = root.Find(ContinueButtonName);
            Transform status = root.Find(StatusTextName);
            if (label == null || newGame == null || continueButton == null || status == null)
            {
                error = "legacy title hierarchy is missing a base control";
                return null;
            }
            if (label.GetComponent<Text>() == null || label.GetComponent<Text>().font == null ||
                status.GetComponent<Text>() == null || status.GetComponent<Text>().font == null ||
                !ValidButton(newGame) || !ValidButton(continueButton))
            {
                error = "legacy title controls are invalid";
                return null;
            }

            for (int i = 2; i < ScenarioButtonNames.Length; i++)
            {
                Transform scenario = root.Find(ScenarioButtonNames[i]);
                if (scenario != null && !ValidButton(scenario))
                {
                    error = $"legacy scenario entry {ScenarioButtonNames[i]} is invalid";
                    return null;
                }
            }
            if (hasStandard &&
                (!ValidButton(root.Find(StandardSampleButtonName)) || !ValidButton(root.Find(CrisisDemoButtonName))))
            {
                error = "legacy SC-010 entries are invalid";
                return null;
            }

            if (!Bound(controller, "gameSession", session) ||
                !Bound(controller, "worldMapController", mapController) ||
                !Bound(controller, "titleRoot", root.gameObject) ||
                !Bound(controller, "newGameButton", newGame.GetComponent<Button>()) ||
                !Bound(controller, "continueButton", continueButton.GetComponent<Button>()) ||
                !Bound(controller, "statusText", status.GetComponent<Text>()))
            {
                error = "legacy title references are not fully bound";
                return null;
            }
            string[] legacyScenarioFields =
            {
                "standardSampleButton", "crisisDemoButton", "educationDemoButton", "researchDemoButton",
                "familyFormationDemoButton", "seclusionDemoButton", "familyDoctrineDemoButton"
            };
            for (int i = 0; i < ScenarioButtonNames.Length; i++)
            {
                Transform scenario = root.Find(ScenarioButtonNames[i]);
                if (scenario == null)
                    continue;
                if (!Bound(controller, legacyScenarioFields[i], scenario.GetComponent<Button>()))
                {
                    error = $"legacy binding {legacyScenarioFields[i]} is not wired";
                    return null;
                }
            }

            Transform summary = summaries[0];
            DemoSummaryPanel panel = panels[0];
            if (summary.parent != map || summary.GetSiblingIndex() >= map.Find(TownRootName).GetSiblingIndex() ||
                summary.gameObject.activeSelf || summary.GetComponent<Text>() == null ||
                summary.GetComponent<Text>().font == null ||
                panel.transform != session.transform ||
                !Bound(panel, "gameSession", session) ||
                !Bound(panel, "summaryText", summary.GetComponent<Text>()))
            {
                error = "legacy demo summary is incomplete";
                return null;
            }

            return new LegacyLayout
            {
                Root = root,
                Controller = controller,
                Panel = panel,
                Summary = summary.GetComponent<Text>()
            };
        }

        private static UpdateResult Rebuild(Scene scene, bool save, Action<GameObject> beforeValidation,
            LegacyLayout legacy)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            int originalSiblingIndex = legacy.Root != null ? legacy.Root.GetSiblingIndex() : -1;
            try
            {
                BuildInto(scene, legacy, out _);
                beforeValidation?.Invoke(legacy.Root.gameObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error)) throw new InvalidOperationException(error);

                Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                failure = UpdateResult.SaveFailed;
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");

                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                if (legacy.Root != null && originalSiblingIndex >= 0 &&
                    legacy.Root.GetSiblingIndex() != originalSiblingIndex)
                {
                    // Explicit transactional fallback: sibling order lives in the parent's
                    // child list and must be restored even if the parent undo record is not.
                    legacy.Root.SetSiblingIndex(originalSiblingIndex);
                    EditorUtility.SetDirty(legacy.Root);
                }
                Debug.LogWarning($"M6 updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static UpdateResult BuildFresh(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = LoadFont();
                GameObject root = Create(scene, scene.GetRootGameObjects()
                    .Single(go => go.name == CanvasName && go.GetComponent<Canvas>() != null).transform,
                    RootName, typeof(Image));
                ConfigureRoot(root);
                Transform map = root.transform.parent.Find(MapRootName);
                Text summary = CreateText(scene, map, DemoSummaryTextName, font, "", 460f, -110f, 260f, 44f, 12,
                    Color.white, TextAnchor.MiddleCenter);
                summary.gameObject.SetActive(false);
                summary.transform.SetSiblingIndex(map.Find(TownRootName).GetSiblingIndex());

                GameSession session = Components<GameSession>(scene).Single();
                BuildInto(scene, new LegacyLayout { Root = root.transform, Summary = summary }, out _);

                DemoSummaryPanel panel = Undo.AddComponent<DemoSummaryPanel>(session.gameObject);
                Undo.RecordObject(panel, UndoName);
                panel.Bind(session, summary);
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(root);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error)) throw new InvalidOperationException(error);

                Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                failure = UpdateResult.SaveFailed;
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");

                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogWarning($"M6 updater rolled back: {exception.Message} Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        /// <summary>
        /// Builds the two-page title UI beneath the supplied root. When a legacy layout is
        /// supplied its existing children are destroyed inside the current Undo group; the
        /// root, controller, panel, and summary objects are preserved.
        /// </summary>
        private static void BuildInto(Scene scene, LegacyLayout legacy, out TitleScreenController controller)
        {
            Transform root = legacy.Root;
            controller = legacy.Controller;
            Font font = LoadFont();

            for (int i = root.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);

            // Record both before ConfigureRoot so a failed migration restores the legacy
            // background sprite/colour and the original stretch/anchors, not just children.
            Undo.RecordObject(root, UndoName);
            Undo.RecordObject(root.GetComponent<Image>(), UndoName);
            ConfigureRoot(root.gameObject);

            Transform main = CreatePanel(scene, root, MainMenuRootName, SengokuUiTheme.IndigoDeep, 0.92f, false);
            ConfigureLeftPanel(main);
            AddGoldOutline(main);
            Transform selector = CreatePanel(scene, root, ScenarioSelectRootName, SengokuUiTheme.IndigoDeep, 0.97f, true);
            ConfigureFullStretch(selector);
            AddGoldOutline(selector);

            Image seal = CreateImage(scene, main, SealName, LoadSealSprite(), new Vector2(0f, 250f),
                new Vector2(72f, 72f), false);
            seal.preserveAspect = true;

            Text title = CreateText(scene, main, LabelName, font, "影之一族", 0f, 170f, 400f, 80f, 54,
                SengokuUiTheme.ParchmentLight, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            CreateText(scene, main, SubtitleName, font, "守护家名，传承百年", 0f, 105f, 400f, 32f, 20,
                SengokuUiTheme.Gold, TextAnchor.MiddleCenter);

            Button continueBtn = CreateButton(scene, main, ContinueButtonName, font, "继续游戏",
                0f, 0f, 300f, 58f, 22, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);
            Button newGame = CreateButton(scene, main, NewGameButtonName, font, "开始新游戏",
                0f, -72f, 300f, 58f, 22, SengokuUiTheme.Vermilion, SengokuUiTheme.ParchmentLight);
            CreateText(scene, main, StatusTextName, font, "", 0f, -160f, 400f, 48f, 14,
                SengokuUiTheme.Parchment, TextAnchor.MiddleCenter);

            CreateText(scene, selector, ScenarioHeadingName, font, "选择剧本", 0f, 288f, 600f, 56f, 36,
                SengokuUiTheme.ParchmentLight, TextAnchor.MiddleCenter);
            CreateText(scene, selector, MainlineSectionName, font, "主线剧本", -235f, 192f, 380f, 26f, 16,
                SengokuUiTheme.Gold, TextAnchor.MiddleCenter);
            CreateText(scene, selector, TopicsSectionName, font, "专题示范", 235f, 238f, 380f, 26f, 16,
                SengokuUiTheme.Gold, TextAnchor.MiddleCenter);
            Button back = CreateButton(scene, selector, ScenarioBackButtonName, font, "返回",
                0f, -292f, 200f, 48f, 20, SengokuUiTheme.IndigoPanel, SengokuUiTheme.Parchment);

            Button fixedScenario = CreateButton(scene, selector, FixedScenarioButtonName, font,
                "固定三代剧本（SC-013）", -235f, 120f, 380f, 48f, 18,
                SengokuUiTheme.Vermilion, SengokuUiTheme.ParchmentLight);
            Button standard = CreateButton(scene, selector, StandardSampleButtonName, font, "SC-010 标准样例",
                -235f, 50f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);
            Button crisis = CreateButton(scene, selector, CrisisDemoButtonName, font, "SC-010 危机示范",
                -235f, -20f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);

            Button education = CreateButton(scene, selector, EducationDemoButtonName, font, "SC-020 子女培养",
                235f, 175f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);
            Button research = CreateButton(scene, selector, ResearchDemoButtonName, font, "SC-021 家族研究",
                235f, 110f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);
            Button familyFormation = CreateButton(scene, selector, FamilyFormationDemoButtonName, font,
                "SC-022 婚姻与收养", 235f, 45f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);
            Button seclusion = CreateButton(scene, selector, SeclusionDemoButtonName, font,
                "SC-023 隐居与后见人", 235f, -20f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);
            Button familyDoctrine = CreateButton(scene, selector, FamilyDoctrineDemoButtonName, font,
                "SC-024 家训与抱负", 235f, -85f, 380f, 48f, 18, SengokuUiTheme.Parchment, SengokuUiTheme.Ink);

            if (controller == null)
            {
                GameSession session = Components<GameSession>(scene).Single();
                controller = Undo.AddComponent<TitleScreenController>(session.gameObject);
            }
            Undo.RecordObject(controller, UndoName);
            controller.Bind(Components<GameSession>(scene).Single(), null,
                Components<WorldMapController>(scene).Single(), root.gameObject, newGame, continueBtn,
                main.Find(StatusTextName).GetComponent<Text>());
            controller.BindScenarioSelection(main.gameObject, selector.gameObject, fixedScenario, back);
            controller.BindScenarioButtons(standard, crisis);
            controller.BindEducationScenarioButton(education);
            controller.BindResearchScenarioButton(research);
            controller.BindFamilyFormationScenarioButton(familyFormation);
            controller.BindSeclusionScenarioButton(seclusion);
            controller.BindFamilyDoctrineScenarioButton(familyDoctrine);
            EditorUtility.SetDirty(controller);

            // Sibling order lives in the parent RectTransform, so record the parent too.
            Undo.RegisterCompleteObjectUndo(root.parent, UndoName);
            root.SetAsLastSibling();
            Transform eventRoot = root.parent.Find(EventChoiceRootName);
            Transform tacticalOverlay = root.parent.Find(TacticalOverlayName);
            if (eventRoot != null)
                root.SetSiblingIndex(eventRoot.GetSiblingIndex());
            else if (tacticalOverlay != null)
                root.SetSiblingIndex(tacticalOverlay.GetSiblingIndex());
            EditorUtility.SetDirty(root);
            selector.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        private static void AddGoldOutline(Transform panel)
        {
            var outline = Undo.AddComponent<Outline>(panel.gameObject);
            outline.effectColor = SengokuUiTheme.GoldDim;
            outline.effectDistance = new Vector2(1f, 1f);
            outline.useGraphicAlpha = false;
        }

        private static void ConfigureRoot(GameObject root)
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image background = root.GetComponent<Image>();
            background.sprite = LoadBackgroundSprite();
            background.color = Color.white;
            background.raycastTarget = true;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
        }

        private static void ConfigureLeftPanel(Transform panel)
        {
            RectTransform rect = (RectTransform)panel;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(MainPanelWidth, 0f);
        }

        private static void ConfigureFullStretch(Transform panel)
        {
            RectTransform rect = (RectTransform)panel;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool LeftPanel(Transform panel)
        {
            var rect = panel as RectTransform;
            return rect != null && rect.anchorMin == new Vector2(0f, 0f) &&
                rect.anchorMax == new Vector2(0f, 1f) && rect.pivot == new Vector2(0f, 0.5f) &&
                rect.anchoredPosition == Vector2.zero && Mathf.Approximately(rect.sizeDelta.x, MainPanelWidth) &&
                Mathf.Approximately(rect.sizeDelta.y, 0f);
        }

        private static bool FullStretch(Transform panel)
        {
            var rect = panel as RectTransform;
            return rect != null && rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one &&
                rect.offsetMin == Vector2.zero && rect.offsetMax == Vector2.zero;
        }

        private static bool Fits(Transform control, Vector2 parentCenter, Rect area)
        {
            var rect = control as RectTransform;
            if (rect == null || rect.anchorMin != new Vector2(0.5f, 0.5f) ||
                rect.anchorMax != new Vector2(0.5f, 0.5f))
                return false;
            Vector2 center = parentCenter + rect.anchoredPosition;
            Vector2 half = rect.sizeDelta * 0.5f;
            return area.Contains(center - half) && area.Contains(center + half);
        }

        private static bool ValidSeal(Transform seal)
        {
            Image image = seal.GetComponent<Image>();
            return image != null && image.sprite != null && !image.raycastTarget && image.preserveAspect &&
                seal.childCount == 0;
        }

        private static bool ValidButton(Transform control)
        {
            Button button = control.GetComponent<Button>();
            Text[] labels = control.GetComponentsInChildren<Text>(true);
            return button != null && button.targetGraphic != null &&
                button.targetGraphic.gameObject == control.gameObject &&
                labels.Length == 1 && labels[0].font != null && !string.IsNullOrWhiteSpace(labels[0].text) &&
                labels[0].transform.parent == control && control.childCount == 1;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static Sprite LoadBackgroundSprite() =>
            AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundAssetPath);

        private static Sprite LoadSealSprite()
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(SealAssetPath))
                if (asset is Sprite sprite)
                    return sprite;
            return null;
        }

        private static Sprite LoadPaperSprite()
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(PaperAssetPath))
                if (asset is Sprite sprite)
                    return sprite;
            return null;
        }

        private static Sprite LoadIndigoPatternSprite()
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(IndigoPatternAssetPath))
                if (asset is Sprite sprite && sprite.name == IndigoPatternSpriteName)
                    return sprite;
            return null;
        }

        private static Font LoadFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) throw new InvalidOperationException("Missing built-in UI font.");
            return font;
        }

        private static Transform CreatePanel(Scene scene, Transform parent, string name, Color color,
            float alpha, bool raycast)
        {
            GameObject go = Create(scene, parent, name, typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            Image image = go.GetComponent<Image>();
            image.sprite = LoadIndigoPatternSprite();
            image.type = image.sprite != null ? Image.Type.Tiled : Image.Type.Simple;
            image.color = SengokuUiTheme.WithAlpha(color, alpha);
            image.raycastTarget = raycast;
            return go.transform;
        }

        private static Image CreateImage(Scene scene, Transform parent, string name, Sprite sprite,
            Vector2 position, Vector2 size, bool raycast)
        {
            GameObject go = Create(scene, parent, name, typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = raycast;
            return image;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, string value,
            float x, float y, float width, float height, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font, string label,
            float x, float y, float width, float height, int fontSize, Color background, Color foreground)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.sprite = LoadPaperSprite();
            image.type = Image.Type.Simple;
            image.color = background;
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            AddGoldOutline(go.transform);
            Text buttonLabel = CreateText(scene, rect, "Label", font, label, 0f, 0f, width - 8f, height - 2f,
                fontSize, foreground, TextAnchor.MiddleCenter);
            buttonLabel.raycastTarget = false;
            return button;
        }

        private static GameObject Create(Scene scene, Transform parent, string name, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components)
                Undo.AddComponent(go, component);
            return go;
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(c =>
            {
                Vector3 point = parent.InverseTransformPoint(c);
                return bounds.Contains(new Vector2(point.x, point.y));
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform coordinateRoot)
        {
            Rect ToRoot(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Vector3 a = coordinateRoot.InverseTransformPoint(corners[0]);
                Vector3 c = coordinateRoot.InverseTransformPoint(corners[2]);
                return Rect.MinMaxRect(
                    Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y),
                    Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
            }

            return ToRoot(first).Overlaps(ToRoot(second), true);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }

        private static Transform[] Named(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name)
            .ToArray();

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true))
            .ToArray();
    }
}
