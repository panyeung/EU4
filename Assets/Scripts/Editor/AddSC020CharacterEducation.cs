using System;
using System.Collections.Generic;
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
    /// Transactional FoundationScene updater for the SC-020 child-education panel and
    /// title entry. One Undo group, preflight, validate before save, Undo rollback on
    /// exception. It never reloads a scene and never calls DestroyImmediate on scene data.
    /// </summary>
    public static class AddSC020CharacterEducation
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "SC020EducationRoot";
        public const string OpenButtonName = "SC020EducationOpenButton";
        public const string PanelName = "SC020EducationPanel";
        public const string TitleName = "SC020EducationTitleText";
        public const string OverviewName = "SC020EducationOverviewText";
        public const string DirectionContainerName = "SC020DirectionContainer";
        public const string DirectionTemplateName = "SC020DirectionButtonTemplate";
        public const string MentorContainerName = "SC020MentorContainer";
        public const string MentorTemplateName = "SC020MentorButtonTemplate";
        public const string PreviewName = "SC020EducationPreviewText";
        public const string MessageName = "SC020EducationMessageText";
        public const string ConfirmButtonName = "SC020EducationConfirmButton";
        public const string CloseButtonName = "SC020EducationCloseButton";
        public const string TitleButtonName = "SC020EducationDemoButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-020 Character Education";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventChoiceRootName = "EventChoiceRoot";
        private const string TitleStatusName = "M6TitleStatusText";
        private const string NewGameButtonName = "M6NewGameButton";
        private const string ContinueButtonName = "M6ContinueButton";
        private const string StandardButtonName = "SC013StandardSampleButton";
        private const string CrisisButtonName = "SC013CrisisDemoButton";
        private const string ChronicleOpenButtonName = "FamilyChronicleOpenButton";
        private const string RosterOpenButtonName = "FamilyRosterOpenButton";
        private const string IndustryOpenButtonName = "IndustryOpenButton";
        private const string SuccessionHudName = "SuccessionHud";
        private const string UndoName = "Add SC-020 Character Education";

        // Below the SC-010 HUD (bottom -230) and the SC-019 industry entry (bottom -294).
        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -302f);
        private static readonly Vector2 TitleButtonPosition = new Vector2(0f, -274f);

        private const int DirectionColumns = 3;
        private const float CellWidth = 220f;
        private const float CellHeight = 34f;
        private const float CellSpacing = 8f;
        private const float ContainerWidth = 700f;
        private const float ContainerHeight = 86f;

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, InvalidHierarchyOrBindings, ExistingPartialControl,
            ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty,
                EditorApplication.isPlayingOrWillChangePlaymode);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("SC-020 character education panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-020 character education updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty) =>
            CheckScenePreflight(path, dirty, false);

        public static UpdateResult CheckScenePreflight(string path, bool dirty, bool editorPlaying)
        {
            if (editorPlaying) return UpdateResult.EditorPlaying;
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam. It never saves, opens or reloads the supplied scene.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault-injection seam inside the same Undo transaction as construction.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return Invalid(out error, "Scene is invalid or not loaded.");

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            CharacterEducationPanel[] panels = Components<CharacterEducationPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 || roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, CharacterEducationPanel and education root.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform overview = Require(scene, OverviewName);
            Transform directions = Require(scene, DirectionContainerName);
            Transform directionTemplate = Require(scene, DirectionTemplateName);
            Transform mentors = Require(scene, MentorContainerName);
            Transform mentorTemplate = Require(scene, MentorTemplateName);
            Transform preview = Require(scene, PreviewName);
            Transform message = Require(scene, MessageName);
            Transform confirm = Require(scene, ConfirmButtonName);
            Transform close = Require(scene, CloseButtonName);
            if (open == null || panel == null || title == null || overview == null ||
                directions == null || directionTemplate == null || mentors == null ||
                mentorTemplate == null || preview == null || message == null || confirm == null ||
                close == null)
                return Invalid(out error, "SC-020 education hierarchy is incomplete or ambiguous.");

            if (root.parent != canvas || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "SC020EducationRoot must be active, be a direct Canvas child, and own the panel component.");

            Transform mapRoot = canvas.Find(MapRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC020EducationRoot must render above the map/town HUD so its controls receive raycasts.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "SC020EducationRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "SC020EducationRoot must render below the event-choice overlay.");

            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error, "The education open button must be an active direct child of its root.");
            if (panel.parent != root || panel.gameObject.activeSelf)
                return Invalid(out error, "SC020EducationPanel must be a hidden direct child of the root.");
            if (title.parent != panel || overview.parent != panel || directions.parent != panel ||
                mentors.parent != panel || preview.parent != panel || message.parent != panel ||
                confirm.parent != panel || close.parent != panel)
                return Invalid(out error,
                    "All SC-020 education controls must be direct children of the panel.");

            if (!ValidateContainer(directions, directionTemplate, out error) ||
                !ValidateContainer(mentors, mentorTemplate, out error))
                return false;

            Text titleText = title.GetComponent<Text>();
            Text overviewText = overview.GetComponent<Text>();
            Text previewText = preview.GetComponent<Text>();
            Text messageText = message.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button confirmButton = confirm.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            if (titleText == null || overviewText == null || previewText == null || messageText == null ||
                titleText.font == null || overviewText.font == null || previewText.font == null ||
                messageText.font == null || openButton == null || confirmButton == null ||
                closeButton == null || !HasButtonVisual(openButton) || !HasButtonVisual(confirmButton) ||
                !HasButtonVisual(closeButton) || !HasRaycastTarget(openButton) ||
                !HasRaycastTarget(confirmButton) || !HasRaycastTarget(closeButton) ||
                panel.GetComponent<Image>() == null || !panel.GetComponent<Image>().raycastTarget)
                return Invalid(out error, "SC-020 text, buttons, background or raycast components are invalid.");
            if (message.gameObject.activeSelf)
                return Invalid(out error, "The education message line must start hidden.");
            if (!titleText.text.Contains("培养"))
                return Invalid(out error, "The education panel title must contain 培养.");

            CharacterEducationPanel educationPanel = panels[0];
            if (!Bound(educationPanel, "gameSession", sessions[0]) ||
                !Bound(educationPanel, "panelRoot", panel.gameObject) ||
                !Bound(educationPanel, "openButton", openButton) ||
                !Bound(educationPanel, "closeButton", closeButton) ||
                !Bound(educationPanel, "overviewText", overviewText) ||
                !Bound(educationPanel, "directionContainer", directions) ||
                !Bound(educationPanel, "directionTemplate", directionTemplate.GetComponent<Button>()) ||
                !Bound(educationPanel, "mentorContainer", mentors) ||
                !Bound(educationPanel, "mentorTemplate", mentorTemplate.GetComponent<Button>()) ||
                !Bound(educationPanel, "previewText", previewText) ||
                !Bound(educationPanel, "confirmButton", confirmButton) ||
                !Bound(educationPanel, "messageText", messageText))
                return Invalid(out error, "CharacterEducationPanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform panelRect = panel as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform closeRect = close as RectTransform;
            if (rootRect == null || panelRect == null || openRect == null || closeRect == null ||
                !Contained(panelRect, rootRect) || !Contained(openRect, rootRect) ||
                !Contained((RectTransform)title, panelRect) ||
                !Contained((RectTransform)overview, panelRect) ||
                !Contained((RectTransform)directions, panelRect) ||
                !Contained((RectTransform)mentors, panelRect) ||
                !Contained((RectTransform)preview, panelRect) ||
                !Contained((RectTransform)message, panelRect) ||
                !Contained(confirm as RectTransform, panelRect) ||
                !Contained(closeRect, panelRect) ||
                Overlaps((RectTransform)title, (RectTransform)overview, panelRect) ||
                Overlaps((RectTransform)overview, (RectTransform)directions, panelRect) ||
                Overlaps((RectTransform)directions, (RectTransform)mentors, panelRect) ||
                Overlaps((RectTransform)mentors, (RectTransform)preview, panelRect) ||
                Overlaps((RectTransform)preview, (RectTransform)message, panelRect) ||
                Overlaps((RectTransform)message, (RectTransform)confirm, panelRect) ||
                Overlaps((RectTransform)confirm, closeRect, panelRect))
                return Invalid(out error, "SC-020 education controls overlap or lie outside their bounds.");

            if (Overlaps(openRect, SuccessionHud(scene), rootRect) ||
                OverlapsNamed(scene, openRect, IndustryOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, ChronicleOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, RosterOpenButtonName, rootRect))
                return Invalid(out error,
                    "The education open button overlaps the succession HUD or another HUD entry.");

            return ValidateTitleEntry(scene, canvas, sessions[0]);
        }

        private static bool ValidateContainer(Transform container, Transform template, out string error)
        {
            RectTransform containerRect = container as RectTransform;
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (containerRect == null || grid == null)
            {
                error = "SC-020 container " + container.name + " must use a GridLayoutGroup.";
                return false;
            }
            if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                grid.constraintCount != DirectionColumns ||
                grid.cellSize.x + 0.5f < CellWidth || grid.cellSize.y + 0.5f < CellHeight ||
                grid.spacing.x < 0f || grid.spacing.y < 0f)
            {
                error = "SC-020 container " + container.name + " has an unusable grid layout.";
                return false;
            }
            int rows = 2;
            float requiredWidth = grid.padding.left + grid.padding.right +
                                  DirectionColumns * grid.cellSize.x +
                                  (DirectionColumns - 1) * grid.spacing.x;
            float requiredHeight = grid.padding.top + grid.padding.bottom +
                                   rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
            if (containerRect.rect.width + 0.5f < requiredWidth ||
                containerRect.rect.height + 0.5f < requiredHeight)
            {
                error = "SC-020 container " + container.name + " is too small for six direction rows.";
                return false;
            }
            if (template == null || template.gameObject.activeSelf ||
                template.parent != container)
            {
                error = "The SC-020 button template must exist inactive under its container.";
                return false;
            }
            RectTransform templateRect = template as RectTransform;
            if (templateRect == null || !Contained(templateRect, containerRect))
            {
                error = "The SC-020 button template must lie inside its container.";
                return false;
            }
            Button button = template.GetComponent<Button>();
            Image image = template.GetComponent<Image>();
            if (button == null || image == null || button.targetGraphic != image ||
                !HasRaycastTarget(button) || template.GetComponentInChildren<Text>(true) == null)
            {
                error = "The SC-020 button template graphic or label is invalid.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool ValidateTitleEntry(Scene scene, Transform canvas, GameSession session)
        {
            Transform titleRoot = canvas.Find(TitleRootName);
            if (titleRoot == null)
                return true;
            Transform button = Require(scene, TitleButtonName);
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            if (button == null || controllers.Length != 1)
                return Invalid(out _, "The SC-020 title entry or TitleScreenController is missing.");
            if (button.parent != titleRoot || !button.gameObject.activeSelf)
                return Invalid(out _, "The SC-020 title entry must be active under M6TitleRoot.");
            if (!HasButtonVisual(button.GetComponent<Button>()) ||
                !HasRaycastTarget(button.GetComponent<Button>()))
                return Invalid(out _, "The SC-020 title entry button is invalid.");
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null || !label.text.Contains("培养"))
                return Invalid(out _, "The SC-020 title entry label must contain 培养.");
            if (!Bound(controllers[0], "educationDemoButton", button.GetComponent<Button>()) ||
                !Bound(controllers[0], "gameSession", session))
                return Invalid(out _, "TitleScreenController SC-020 references are not bound.");

            RectTransform rootRect = titleRoot as RectTransform;
            RectTransform buttonRect = button as RectTransform;
            if (rootRect == null || buttonRect == null || !Contained(buttonRect, rootRect))
                return Invalid(out _, "The SC-020 title entry lies outside M6TitleRoot.");
            if (OverlapsNamed(scene, buttonRect, CrisisButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, TitleStatusName, rootRect) ||
                OverlapsNamed(scene, buttonRect, StandardButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, NewGameButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ContinueButtonName, rootRect))
                return Invalid(out _, "The SC-020 title entry overlaps another title control.");
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            Transform canvas = canvases.Length == 1 ? canvases[0] : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            if (canvas == null || session == null || Components<CharacterEducationPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (HasCompleteHierarchy(scene))
                return Repair(scene, save, beforeValidation);

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, PanelName).Any() || Named(scene, TitleButtonName).Any() ||
                Components<CharacterEducationPanel>(scene).Length != 0 ||
                HasSerializedReference(Components<TitleScreenController>(scene).FirstOrDefault(),
                    "educationDemoButton"))
                return UpdateResult.ExistingPartialControl;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName);
                PlaceAboveMapHud(canvas, rootObject.transform);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "子女培养",
                    200f, 44f, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, new Vector2(200f, 44f));

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(760f, 560f);
                panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font, "子女培养与人物成长",
                    0f, 245f, 700f, 38f, 22, TextAnchor.MiddleLeft);
                Text overview = CreateText(scene, panelRect, OverviewName, font,
                    "青叶 千景（15 岁）｜忍术 15　武艺 12　学问 20　经营 10　社交 13",
                    0f, 196f, 700f, 60f, 16, TextAnchor.UpperLeft);
                Transform directions = CreateContainer(scene, panelRect, DirectionContainerName, 108f);
                Transform mentors = CreateContainer(scene, panelRect, MentorContainerName, 14f);
                Button directionTemplate = CreateTemplate(scene, directions, DirectionTemplateName,
                    font, "学问");
                Button mentorTemplate = CreateTemplate(scene, mentors, MentorTemplateName,
                    font, "姓名（专长）");
                Text preview = CreateText(scene, panelRect, PreviewName, font,
                    "选择培养方向与导师后显示预计变化。",
                    0f, -84f, 700f, 90f, 15, TextAnchor.UpperLeft);
                preview.color = new Color(0.75f, 0.9f, 1f, 1f);
                Text message = CreateText(scene, panelRect, MessageName, font, string.Empty,
                    0f, -150f, 700f, 26f, 14, TextAnchor.MiddleLeft);
                message.color = new Color(0.65f, 0.86f, 1f, 1f);
                message.gameObject.SetActive(false);
                Button confirm = CreateButton(scene, panelRect, ConfirmButtonName, font, "提交年度计划",
                    220f, 40f, 16);
                SetCenteredRect(confirm.transform, new Vector2(-130f, -215f), new Vector2(220f, 40f));
                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    160f, 40f, 16);
                SetCenteredRect(close.transform, new Vector2(150f, -215f), new Vector2(160f, 40f));

                CharacterEducationPanel panel = Undo.AddComponent<CharacterEducationPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, panelObject, open, close, overview, directions,
                    directionTemplate, mentors, mentorTemplate, preview, confirm, message);
                SetSerializedReferences(panel, session, panelObject, open, close, overview,
                    directions, directionTemplate, mentors, mentorTemplate, preview, confirm, message);
                EditorUtility.SetDirty(panel);

                if (canvas.Find(TitleRootName) != null)
                    EnsureTitleEntry(scene, canvas, session, font);

                beforeValidation?.Invoke(rootObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);

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
                Debug.LogWarning($"SC-020 character education updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool HasCompleteHierarchy(Scene scene)
        {
            if (Components<CharacterEducationPanel>(scene).Length != 1)
                return false;
            string[] required =
            {
                RootName, OpenButtonName, PanelName, TitleName, OverviewName, DirectionContainerName,
                DirectionTemplateName, MentorContainerName, MentorTemplateName, PreviewName,
                MessageName, ConfirmButtonName, CloseButtonName
            };
            foreach (string name in required)
                if (Named(scene, name).Count() != 1)
                    return false;
            Transform root = Require(scene, RootName);
            return root != null && root.parent != null &&
                   root.parent.name == CanvasName && root.parent.GetComponent<Canvas>() != null;
        }

        /// <summary>
        /// Transactional repair for a complete SC-020 hierarchy. It re-applies layout and
        /// bindings only, then still validates before saving.
        /// </summary>
        private static UpdateResult Repair(Scene scene, bool save, Action<GameObject> beforeValidation)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                Transform root = Require(scene, RootName);
                Transform panel = Require(scene, PanelName);
                Transform open = Require(scene, OpenButtonName);
                Transform title = Require(scene, TitleName);
                Transform overview = Require(scene, OverviewName);
                Transform directions = Require(scene, DirectionContainerName);
                Transform mentors = Require(scene, MentorContainerName);
                Transform directionTemplate = Require(scene, DirectionTemplateName);
                Transform mentorTemplate = Require(scene, MentorTemplateName);
                Transform preview = Require(scene, PreviewName);
                Transform message = Require(scene, MessageName);
                Transform confirm = Require(scene, ConfirmButtonName);
                Transform close = Require(scene, CloseButtonName);
                if (root == null || panel == null || open == null || title == null ||
                    overview == null || directions == null || mentors == null ||
                    directionTemplate == null || mentorTemplate == null || preview == null ||
                    message == null || confirm == null || close == null)
                    throw new InvalidOperationException("Complete SC-020 hierarchy could not be resolved.");

                SetTopLeftRect(open, OpenButtonPosition, new Vector2(200f, 44f));
                SetCenteredRect(panel, Vector2.zero, new Vector2(760f, 560f));
                SetCenteredRect(title, new Vector2(0f, 245f), new Vector2(700f, 38f));
                SetCenteredRect(overview, new Vector2(0f, 196f), new Vector2(700f, 60f));
                SetCenteredRect(directions, new Vector2(0f, 108f), new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(mentors, new Vector2(0f, 14f), new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(preview, new Vector2(0f, -84f), new Vector2(700f, 90f));
                SetCenteredRect(message, new Vector2(0f, -150f), new Vector2(700f, 26f));
                SetCenteredRect(confirm, new Vector2(-130f, -215f), new Vector2(220f, 40f));
                SetCenteredRect(close, new Vector2(150f, -215f), new Vector2(160f, 40f));
                ConfigureContainer(directions);
                ConfigureContainer(mentors);
                SetCenteredRect(directionTemplate, Vector2.zero, new Vector2(CellWidth, CellHeight));
                SetCenteredRect(mentorTemplate, Vector2.zero, new Vector2(CellWidth, CellHeight));
                message.gameObject.SetActive(false);

                CharacterEducationPanel component = root.GetComponent<CharacterEducationPanel>();
                if (component == null)
                    throw new InvalidOperationException("SC020EducationRoot no longer owns CharacterEducationPanel.");
                Undo.RecordObject(component, UndoName);
                GameSession session = Components<GameSession>(scene)[0];
                component.BindReferences(session, panel.gameObject, open.GetComponent<Button>(),
                    close.GetComponent<Button>(), overview.GetComponent<Text>(), directions,
                    directionTemplate.GetComponent<Button>(), mentors,
                    mentorTemplate.GetComponent<Button>(), preview.GetComponent<Text>(),
                    confirm.GetComponent<Button>(), message.GetComponent<Text>());
                SetSerializedReferences(component, session, panel.gameObject,
                    open.GetComponent<Button>(), close.GetComponent<Button>(),
                    overview.GetComponent<Text>(), directions,
                    directionTemplate.GetComponent<Button>(), mentors,
                    mentorTemplate.GetComponent<Button>(), preview.GetComponent<Text>(),
                    confirm.GetComponent<Button>(), message.GetComponent<Text>());
                EditorUtility.SetDirty(component);

                Transform titleRoot = panel.root != null ? panel.root.Find(TitleRootName) : null;
                if (titleRoot == null && root.parent != null)
                    titleRoot = root.parent.Find(TitleRootName);
                if (titleRoot != null)
                    EnsureTitleEntry(scene, titleRoot.parent, session, font);

                beforeValidation?.Invoke(root.gameObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);

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
                Debug.LogWarning($"SC-020 character education repair rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void EnsureTitleEntry(Scene scene, Transform canvas, GameSession session, Font font)
        {
            Transform titleRoot = canvas.Find(TitleRootName);
            if (titleRoot == null)
                return;
            TitleScreenController controller = Components<TitleScreenController>(scene).FirstOrDefault();
            if (controller == null)
                throw new InvalidOperationException("TitleScreenController is missing.");

            Transform existing = titleRoot.Find(TitleButtonName);
            Button button;
            if (existing == null)
            {
                button = CreateTitleButton(scene, titleRoot, TitleButtonName, font,
                    "SC-020 子女培养示范", TitleButtonPosition, new Vector2(320f, 48f));
            }
            else
            {
                button = existing.GetComponent<Button>();
                SetCenteredRect(existing, TitleButtonPosition, new Vector2(320f, 48f));
            }
            if (button == null)
                throw new InvalidOperationException("The SC-020 title entry has no Button component.");

            Transform status = titleRoot.Find(TitleStatusName);
            if (status != null)
                SetCenteredRect(status, new Vector2(0f, -330f), new Vector2(700f, 30f));

            Undo.RecordObject(controller, UndoName);
            controller.BindEducationScenarioButton(button);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("educationDemoButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Button CreateTitleButton(Scene scene, Transform parent, string name, Font font,
            string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            SetCenteredRect(go.transform, position, size);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.32f, 0.42f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            SceneManager.MoveGameObjectToScene(labelObject, scene);
            Undo.RegisterCreatedObjectUndo(labelObject, UndoName);
            Undo.SetTransformParent(labelObject.transform, go.transform, UndoName);
            labelObject.transform.localScale = Vector3.one;
            labelObject.layer = parent.gameObject.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(size.x, size.y - 2f);
            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 17;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return button;
        }

        private static Transform CreateContainer(Scene scene, Transform panel, string name, float y)
        {
            GameObject container = Create(scene, panel, name, typeof(GridLayoutGroup));
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(ContainerWidth, ContainerHeight);
            ConfigureContainer(container.transform);
            return container.transform;
        }

        private static void ConfigureContainer(Transform container)
        {
            if (container == null)
                return;
            RectTransform rect = container as RectTransform;
            if (rect != null)
                Undo.RecordObject(rect, UndoName);
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = Undo.AddComponent<GridLayoutGroup>(container.gameObject);
            else
                Undo.RecordObject(grid, UndoName);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.cellSize = new Vector2(CellWidth, CellHeight);
            grid.spacing = new Vector2(CellSpacing, CellSpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = DirectionColumns;
        }

        private static Button CreateTemplate(Scene scene, Transform container, string name, Font font,
            string label)
        {
            GameObject template = Create(scene, container, name,
                typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = template.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(CellWidth, CellHeight);
            Image image = template.GetComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            Button button = template.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = template.GetComponent<LayoutElement>();
            layout.preferredWidth = CellWidth;
            layout.preferredHeight = CellHeight;
            Text text = CreateText(scene, rect, "Label", font, label,
                0f, 0f, CellWidth - 16f, CellHeight - 8f, 14, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            template.SetActive(false);
            return button;
        }

        private static void SetSerializedReferences(CharacterEducationPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text overview, Transform directions,
            Button directionTemplate, Transform mentors, Button mentorTemplate, Text preview,
            Button confirm, Text message)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("overviewText").objectReferenceValue = overview;
            serialized.FindProperty("directionContainer").objectReferenceValue = directions;
            serialized.FindProperty("directionTemplate").objectReferenceValue = directionTemplate;
            serialized.FindProperty("mentorContainer").objectReferenceValue = mentors;
            serialized.FindProperty("mentorTemplate").objectReferenceValue = mentorTemplate;
            serialized.FindProperty("previewText").objectReferenceValue = preview;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceAboveMapHud(Transform canvas, Transform root)
        {
            Transform title = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            int? anchorIndex = null;
            if (title != null)
                anchorIndex = title.GetSiblingIndex();
            if (eventRoot != null)
                anchorIndex = anchorIndex.HasValue
                    ? Mathf.Min(anchorIndex.Value, eventRoot.GetSiblingIndex())
                    : eventRoot.GetSiblingIndex();
            if (anchorIndex.HasValue)
                root.SetSiblingIndex(anchorIndex.Value);
            else
                root.SetAsLastSibling();
        }

        private static void SetCenteredRect(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopLeftRect(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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

        private static Text CreateText(Scene scene, Transform parent, string name, Font font, string value,
            float x, float y, float width, float height, int fontSize, TextAnchor alignment)
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
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font,
            string label, float width, float height, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
            CreateText(scene, rect, "Label", font, label, 0f, 0f, width - 16f, height - 10f,
                fontSize, TextAnchor.MiddleCenter);
            return button;
        }

        private static bool HasButtonVisual(Button button) =>
            button != null && button.targetGraphic != null &&
            button.GetComponentInChildren<Text>(true) != null;

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static bool HasSerializedReference(UnityEngine.Object target, string field)
        {
            if (target == null)
                return false;
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static RectTransform SuccessionHud(Scene scene)
        {
            Transform hud = Require(scene, SuccessionHudName);
            return hud as RectTransform;
        }

        private static bool OverlapsNamed(Scene scene, RectTransform first, string secondName,
            RectTransform coordinateRoot)
        {
            Transform second = Require(scene, secondName);
            return second is RectTransform secondRect && first != null &&
                   Overlaps(first, secondRect, coordinateRoot);
        }

        private static Transform Require(Scene scene, string name)
        {
            Transform[] matches = Named(scene, name).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static IEnumerable<Transform> Named(Scene scene, string name) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name);

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null)
                return false;
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(corner =>
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                return bounds.Contains(new Vector2(point.x, point.y));
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform coordinateRoot)
        {
            if (first == null || second == null || coordinateRoot == null)
                return false;
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            Vector3 firstMin = coordinateRoot.InverseTransformPoint(firstCorners[0]);
            Vector3 firstMax = coordinateRoot.InverseTransformPoint(firstCorners[2]);
            Vector3 secondMin = coordinateRoot.InverseTransformPoint(secondCorners[0]);
            Vector3 secondMax = coordinateRoot.InverseTransformPoint(secondCorners[2]);
            var firstRect = Rect.MinMaxRect(Mathf.Min(firstMin.x, firstMax.x), Mathf.Min(firstMin.y, firstMax.y),
                Mathf.Max(firstMin.x, firstMax.x), Mathf.Max(firstMin.y, firstMax.y));
            var secondRect = Rect.MinMaxRect(Mathf.Min(secondMin.x, secondMax.x), Mathf.Min(secondMin.y, secondMax.y),
                Mathf.Max(secondMin.x, secondMax.x), Mathf.Max(secondMin.y, secondMax.y));
            return firstRect.Overlaps(secondRect, true);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
