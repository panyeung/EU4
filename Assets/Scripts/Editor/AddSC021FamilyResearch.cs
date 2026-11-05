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
    /// Transactional FoundationScene updater for the SC-021 family-research panel and
    /// title entry. One Undo group, preflight, validate before save, Undo rollback on
    /// exception. It never reloads a scene and never calls DestroyImmediate on scene data.
    /// </summary>
    public static class AddSC021FamilyResearch
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "SC021ResearchRoot";
        public const string OpenButtonName = "SC021ResearchOpenButton";
        public const string PanelName = "SC021ResearchPanel";
        public const string TitleName = "SC021ResearchTitleText";
        public const string ResourceName = "SC021ResearchResourceText";
        public const string TechniqueName = "SC021ResearchTechniqueText";
        public const string StatusName = "SC021ResearchStatusText";
        public const string ProgressName = "SC021ResearchProgressText";
        public const string CostName = "SC021ResearchCostText";
        public const string BlockReasonName = "SC021ResearchBlockReasonText";
        public const string MessageName = "SC021ResearchMessageText";
        public const string ReportName = "SC021ResearchReportText";
        public const string ResearcherContainerName = "SC021ResearcherContainer";
        public const string ResearcherTemplateName = "SC021ResearcherButtonTemplate";
        public const string CloseButtonName = "SC021ResearchCloseButton";
        public const string TitleButtonName = "SC021ResearchDemoButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-021 Family Research";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string IndustryRootName = "IndustryRoot";
        private const string EducationRootName = "SC020EducationRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventChoiceRootName = "EventChoiceRoot";
        private const string TitleStatusName = "M6TitleStatusText";
        private const string NewGameButtonName = "M6NewGameButton";
        private const string ContinueButtonName = "M6ContinueButton";
        private const string StandardButtonName = "SC013StandardSampleButton";
        private const string CrisisButtonName = "SC013CrisisDemoButton";
        private const string EducationOpenButtonName = "SC020EducationOpenButton";
        private const string EducationTitleButtonName = "SC020EducationDemoButton";
        private const string ChronicleOpenButtonName = "FamilyChronicleOpenButton";
        private const string RosterOpenButtonName = "FamilyRosterOpenButton";
        private const string IndustryOpenButtonName = "IndustryOpenButton";
        private const string SuccessionHudName = "SuccessionHud";
        private const string UndoName = "Add SC-021 Family Research";

        // Below the SC-010 HUD (-20..-230), the SC-019 industry entry (-250..-294)
        // and the SC-020 education entry (-302..-346).
        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -354f);
        private static readonly Vector2 TitleButtonPosition = new Vector2(340f, -274f);

        private const int ResearcherColumns = 3;
        private const float CellWidth = 220f;
        private const float CellHeight = 30f;
        private const float CellSpacing = 8f;
        private const float ContainerWidth = 700f;
        private const float ContainerHeight = 68f;

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
                Debug.Log("SC-021 family research panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-021 family research updater refused or rolled back: {result}.");
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
            FamilyResearchPanel[] panels = Components<FamilyResearchPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 || roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, FamilyResearchPanel and research root.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform resource = Require(scene, ResourceName);
            Transform technique = Require(scene, TechniqueName);
            Transform status = Require(scene, StatusName);
            Transform progress = Require(scene, ProgressName);
            Transform cost = Require(scene, CostName);
            Transform blockReason = Require(scene, BlockReasonName);
            Transform message = Require(scene, MessageName);
            Transform report = Require(scene, ReportName);
            Transform researchers = Require(scene, ResearcherContainerName);
            Transform researcherTemplate = Require(scene, ResearcherTemplateName);
            Transform close = Require(scene, CloseButtonName);
            if (open == null || panel == null || title == null || resource == null ||
                technique == null || status == null || progress == null || cost == null ||
                blockReason == null || message == null || report == null || researchers == null ||
                researcherTemplate == null || close == null)
                return Invalid(out error, "SC-021 research hierarchy is incomplete or ambiguous.");

            if (root.parent != canvas || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "SC021ResearchRoot must be active, be a direct Canvas child, and own the panel component.");

            Transform mapRoot = canvas.Find(MapRootName);
            Transform industryRoot = canvas.Find(IndustryRootName);
            Transform educationRoot = canvas.Find(EducationRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC021ResearchRoot must render above the map/town HUD so its controls receive raycasts.");
            if (industryRoot != null && root.GetSiblingIndex() <= industryRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC021ResearchRoot must render above the SC-019 industry HUD.");
            if (educationRoot != null && root.GetSiblingIndex() <= educationRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC021ResearchRoot must render above the SC-020 education HUD.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "SC021ResearchRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "SC021ResearchRoot must render below the event-choice overlay.");

            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error, "The research open button must be an active direct child of its root.");
            if (panel.parent != root || panel.gameObject.activeSelf)
                return Invalid(out error, "SC021ResearchPanel must be a hidden direct child of the root.");
            if (title.parent != panel || resource.parent != panel || technique.parent != panel ||
                status.parent != panel || progress.parent != panel || cost.parent != panel ||
                blockReason.parent != panel || message.parent != panel || report.parent != panel ||
                researchers.parent != panel || close.parent != panel)
                return Invalid(out error,
                    "All SC-021 research controls must be direct children of the panel.");

            if (!ValidateContainer(researchers, researcherTemplate, out error))
                return false;

            Text titleText = title.GetComponent<Text>();
            Text resourceText = resource.GetComponent<Text>();
            Text techniqueText = technique.GetComponent<Text>();
            Text statusText = status.GetComponent<Text>();
            Text progressText = progress.GetComponent<Text>();
            Text costText = cost.GetComponent<Text>();
            Text blockReasonText = blockReason.GetComponent<Text>();
            Text messageText = message.GetComponent<Text>();
            Text reportText = report.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            if (titleText == null || resourceText == null || techniqueText == null ||
                statusText == null || progressText == null || costText == null ||
                blockReasonText == null || messageText == null || reportText == null ||
                titleText.font == null || resourceText.font == null || techniqueText.font == null ||
                statusText.font == null || progressText.font == null || costText.font == null ||
                blockReasonText.font == null || messageText.font == null || reportText.font == null ||
                openButton == null || closeButton == null || !HasButtonVisual(openButton) ||
                !HasButtonVisual(closeButton) || !HasRaycastTarget(openButton) ||
                !HasRaycastTarget(closeButton) || panel.GetComponent<Image>() == null ||
                !panel.GetComponent<Image>().raycastTarget)
                return Invalid(out error,
                    "SC-021 text, buttons, background or raycast components are invalid.");
            if (message.gameObject.activeSelf)
                return Invalid(out error, "The research message line must start hidden.");
            if (blockReason.gameObject.activeSelf)
                return Invalid(out error, "The research block-reason line must start hidden.");
            if (!titleText.text.Contains("研究"))
                return Invalid(out error, "The research panel title must contain 研究.");

            FamilyResearchPanel researchPanel = panels[0];
            if (!Bound(researchPanel, "gameSession", sessions[0]) ||
                !Bound(researchPanel, "panelRoot", panel.gameObject) ||
                !Bound(researchPanel, "openButton", openButton) ||
                !Bound(researchPanel, "closeButton", closeButton) ||
                !Bound(researchPanel, "titleText", titleText) ||
                !Bound(researchPanel, "resourceText", resourceText) ||
                !Bound(researchPanel, "techniqueText", techniqueText) ||
                !Bound(researchPanel, "statusText", statusText) ||
                !Bound(researchPanel, "progressText", progressText) ||
                !Bound(researchPanel, "costText", costText) ||
                !Bound(researchPanel, "blockReasonText", blockReasonText) ||
                !Bound(researchPanel, "messageText", messageText) ||
                !Bound(researchPanel, "reportText", reportText) ||
                !Bound(researchPanel, "researcherContainer", researchers) ||
                !Bound(researchPanel, "researcherTemplate", researcherTemplate.GetComponent<Button>()))
                return Invalid(out error, "FamilyResearchPanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform panelRect = panel as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform closeRect = close as RectTransform;
            if (rootRect == null || panelRect == null || openRect == null || closeRect == null ||
                !Contained(panelRect, rootRect) || !Contained(openRect, rootRect) ||
                !Contained((RectTransform)title, panelRect) ||
                !Contained((RectTransform)resource, panelRect) ||
                !Contained((RectTransform)technique, panelRect) ||
                !Contained((RectTransform)status, panelRect) ||
                !Contained((RectTransform)progress, panelRect) ||
                !Contained((RectTransform)cost, panelRect) ||
                !Contained((RectTransform)blockReason, panelRect) ||
                !Contained((RectTransform)message, panelRect) ||
                !Contained((RectTransform)report, panelRect) ||
                !Contained((RectTransform)researchers, panelRect) ||
                !Contained(closeRect, panelRect) ||
                Overlaps((RectTransform)title, (RectTransform)resource, panelRect) ||
                Overlaps((RectTransform)resource, (RectTransform)technique, panelRect) ||
                Overlaps((RectTransform)technique, (RectTransform)status, panelRect) ||
                Overlaps((RectTransform)status, (RectTransform)progress, panelRect) ||
                Overlaps((RectTransform)progress, (RectTransform)cost, panelRect) ||
                Overlaps((RectTransform)cost, (RectTransform)researchers, panelRect) ||
                Overlaps((RectTransform)researchers, (RectTransform)report, panelRect) ||
                Overlaps((RectTransform)report, (RectTransform)message, panelRect) ||
                Overlaps((RectTransform)message, (RectTransform)blockReason, panelRect) ||
                Overlaps((RectTransform)blockReason, closeRect, panelRect))
                return Invalid(out error, "SC-021 research controls overlap or lie outside their bounds.");

            if (Overlaps(openRect, SuccessionHud(scene), rootRect) ||
                OverlapsNamed(scene, openRect, IndustryOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, EducationOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, ChronicleOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, RosterOpenButtonName, rootRect))
                return Invalid(out error,
                    "The research open button overlaps the succession HUD or another HUD entry.");

            return ValidateTitleEntry(scene, canvas, sessions[0]);
        }

        private static bool ValidateContainer(Transform container, Transform template, out string error)
        {
            RectTransform containerRect = container as RectTransform;
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (containerRect == null || grid == null)
            {
                error = "SC-021 researcher container must use a GridLayoutGroup.";
                return false;
            }
            if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                grid.constraintCount != ResearcherColumns ||
                grid.cellSize.x + 0.5f < CellWidth || grid.cellSize.y + 0.5f < CellHeight ||
                grid.spacing.x < 0f || grid.spacing.y < 0f)
            {
                error = "SC-021 researcher container has an unusable grid layout.";
                return false;
            }
            int rows = 2;
            float requiredWidth = grid.padding.left + grid.padding.right +
                                  ResearcherColumns * grid.cellSize.x +
                                  (ResearcherColumns - 1) * grid.spacing.x;
            float requiredHeight = grid.padding.top + grid.padding.bottom +
                                   rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
            if (containerRect.rect.width + 0.5f < requiredWidth ||
                containerRect.rect.height + 0.5f < requiredHeight)
            {
                error = "SC-021 researcher container is too small for its two rows.";
                return false;
            }
            if (template == null || template.gameObject.activeSelf || template.parent != container)
            {
                error = "The SC-021 researcher template must exist inactive under its container.";
                return false;
            }
            RectTransform templateRect = template as RectTransform;
            if (templateRect == null || !Contained(templateRect, containerRect))
            {
                error = "The SC-021 researcher template must lie inside its container.";
                return false;
            }
            Button button = template.GetComponent<Button>();
            Image image = template.GetComponent<Image>();
            if (button == null || image == null || button.targetGraphic != image ||
                !HasRaycastTarget(button) || template.GetComponentInChildren<Text>(true) == null)
            {
                error = "The SC-021 researcher template graphic or label is invalid.";
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
                return Invalid(out _, "The SC-021 title entry or TitleScreenController is missing.");
            if (button.parent != titleRoot || !button.gameObject.activeSelf)
                return Invalid(out _, "The SC-021 title entry must be active under M6TitleRoot.");
            if (!HasButtonVisual(button.GetComponent<Button>()) ||
                !HasRaycastTarget(button.GetComponent<Button>()))
                return Invalid(out _, "The SC-021 title entry button is invalid.");
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null || !label.text.Contains("研究"))
                return Invalid(out _, "The SC-021 title entry label must contain 研究.");
            if (!Bound(controllers[0], "researchDemoButton", button.GetComponent<Button>()) ||
                !Bound(controllers[0], "gameSession", session))
                return Invalid(out _, "TitleScreenController SC-021 references are not bound.");

            RectTransform rootRect = titleRoot as RectTransform;
            RectTransform buttonRect = button as RectTransform;
            if (rootRect == null || buttonRect == null || !Contained(buttonRect, rootRect))
                return Invalid(out _, "The SC-021 title entry lies outside M6TitleRoot.");
            if (OverlapsNamed(scene, buttonRect, CrisisButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, TitleStatusName, rootRect) ||
                OverlapsNamed(scene, buttonRect, StandardButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, NewGameButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ContinueButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, EducationTitleButtonName, rootRect))
                return Invalid(out _, "The SC-021 title entry overlaps another title control.");
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
            if (canvas == null || session == null || Components<FamilyResearchPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (HasCompleteHierarchy(scene))
                return Repair(scene, save, beforeValidation);

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, PanelName).Any() || Named(scene, TitleButtonName).Any() ||
                Components<FamilyResearchPanel>(scene).Length != 0 ||
                HasSerializedReference(Components<TitleScreenController>(scene).FirstOrDefault(),
                    "researchDemoButton"))
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

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "家族研究",
                    200f, 44f, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, new Vector2(200f, 44f));

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(760f, 520f);
                panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font, "家族研究与技艺传承",
                    0f, 225f, 700f, 38f, 22, TextAnchor.MiddleLeft);
                Text resource = CreateText(scene, panelRect, ResourceName, font,
                    "资金 2　物资 2　情报 0", 0f, 180f, 700f, 30f, 16, TextAnchor.MiddleLeft);
                Text technique = CreateText(scene, panelRect, TechniqueName, font,
                    "可研究技艺：烟幕调制术", 0f, 135f, 700f, 56f, 15,
                    TextAnchor.UpperLeft);
                Text status = CreateText(scene, panelRect, StatusName, font,
                    "尚未安排研究计划。", 0f, 88f, 700f, 28f, 16, TextAnchor.MiddleLeft);
                Text progress = CreateText(scene, panelRect, ProgressName, font,
                    "进度：0/2", 0f, 52f, 700f, 28f, 16, TextAnchor.MiddleLeft);
                Text cost = CreateText(scene, panelRect, CostName, font,
                    "每月消耗 1 资金、1 物资，推进 1 点进度。", 0f, 14f, 700f, 28f, 14,
                    TextAnchor.MiddleLeft);
                cost.color = new Color(0.75f, 0.9f, 1f, 1f);
                Transform researchers = CreateContainer(scene, panelRect, ResearcherContainerName, -42f);
                Button researcherTemplate = CreateTemplate(scene, researchers, ResearcherTemplateName,
                    font, "家人");
                Text report = CreateText(scene, panelRect, ReportName, font,
                    "本月尚未结算研究。", 0f, -118f, 700f, 60f, 14, TextAnchor.UpperLeft);
                report.color = new Color(0.8f, 0.9f, 0.82f, 1f);
                Text message = CreateText(scene, panelRect, MessageName, font, string.Empty,
                    0f, -178f, 700f, 26f, 14, TextAnchor.MiddleLeft);
                message.color = new Color(0.65f, 0.86f, 1f, 1f);
                message.gameObject.SetActive(false);
                Text blockReason = CreateText(scene, panelRect, BlockReasonName, font, string.Empty,
                    0f, -208f, 700f, 26f, 14, TextAnchor.MiddleLeft);
                blockReason.color = new Color(1f, 0.72f, 0.62f, 1f);
                blockReason.gameObject.SetActive(false);
                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    160f, 34f, 16);
                SetCenteredRect(close.transform, new Vector2(0f, -240f), new Vector2(160f, 34f));

                FamilyResearchPanel panel = Undo.AddComponent<FamilyResearchPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, panelObject, open, close, title, resource, technique,
                    status, progress, cost, blockReason, message, report, researchers,
                    researcherTemplate);
                SetSerializedReferences(panel, session, panelObject, open, close, title, resource,
                    technique, status, progress, cost, blockReason, message, report, researchers,
                    researcherTemplate);
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
                Debug.LogWarning($"SC-021 family research updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool HasCompleteHierarchy(Scene scene)
        {
            if (Components<FamilyResearchPanel>(scene).Length != 1)
                return false;
            string[] required =
            {
                RootName, OpenButtonName, PanelName, TitleName, ResourceName, TechniqueName,
                StatusName, ProgressName, CostName, BlockReasonName, MessageName, ReportName,
                ResearcherContainerName, ResearcherTemplateName, CloseButtonName
            };
            foreach (string name in required)
                if (Named(scene, name).Count() != 1)
                    return false;
            Transform root = Require(scene, RootName);
            return root != null && root.parent != null &&
                   root.parent.name == CanvasName && root.parent.GetComponent<Canvas>() != null;
        }

        /// <summary>
        /// Transactional repair for a complete SC-021 hierarchy. It re-applies layout and
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
                Transform resource = Require(scene, ResourceName);
                Transform technique = Require(scene, TechniqueName);
                Transform status = Require(scene, StatusName);
                Transform progress = Require(scene, ProgressName);
                Transform cost = Require(scene, CostName);
                Transform blockReason = Require(scene, BlockReasonName);
                Transform message = Require(scene, MessageName);
                Transform report = Require(scene, ReportName);
                Transform researchers = Require(scene, ResearcherContainerName);
                Transform researcherTemplate = Require(scene, ResearcherTemplateName);
                Transform close = Require(scene, CloseButtonName);
                if (root == null || panel == null || open == null || title == null ||
                    resource == null || technique == null || status == null || progress == null ||
                    cost == null || blockReason == null || message == null || report == null ||
                    researchers == null || researcherTemplate == null || close == null)
                    throw new InvalidOperationException("Complete SC-021 hierarchy could not be resolved.");

                SetTopLeftRect(open, OpenButtonPosition, new Vector2(200f, 44f));
                SetCenteredRect(panel, Vector2.zero, new Vector2(760f, 520f));
                SetCenteredRect(title, new Vector2(0f, 225f), new Vector2(700f, 38f));
                SetCenteredRect(resource, new Vector2(0f, 180f), new Vector2(700f, 30f));
                SetCenteredRect(technique, new Vector2(0f, 135f), new Vector2(700f, 56f));
                SetCenteredRect(status, new Vector2(0f, 88f), new Vector2(700f, 28f));
                SetCenteredRect(progress, new Vector2(0f, 52f), new Vector2(700f, 28f));
                SetCenteredRect(cost, new Vector2(0f, 14f), new Vector2(700f, 28f));
                SetCenteredRect(researchers, new Vector2(0f, -42f),
                    new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(report, new Vector2(0f, -118f), new Vector2(700f, 60f));
                SetCenteredRect(message, new Vector2(0f, -178f), new Vector2(700f, 26f));
                SetCenteredRect(blockReason, new Vector2(0f, -208f), new Vector2(700f, 26f));
                SetCenteredRect(close, new Vector2(0f, -240f), new Vector2(160f, 34f));
                ConfigureContainer(researchers);
                SetCenteredRect(researcherTemplate, Vector2.zero, new Vector2(CellWidth, CellHeight));
                message.gameObject.SetActive(false);
                blockReason.gameObject.SetActive(false);

                FamilyResearchPanel component = root.GetComponent<FamilyResearchPanel>();
                if (component == null)
                    throw new InvalidOperationException("SC021ResearchRoot no longer owns FamilyResearchPanel.");
                Undo.RecordObject(component, UndoName);
                GameSession session = Components<GameSession>(scene)[0];
                component.BindReferences(session, panel.gameObject, open.GetComponent<Button>(),
                    close.GetComponent<Button>(), title.GetComponent<Text>(),
                    resource.GetComponent<Text>(), technique.GetComponent<Text>(),
                    status.GetComponent<Text>(), progress.GetComponent<Text>(),
                    cost.GetComponent<Text>(), blockReason.GetComponent<Text>(),
                    message.GetComponent<Text>(), report.GetComponent<Text>(), researchers,
                    researcherTemplate.GetComponent<Button>());
                SetSerializedReferences(component, session, panel.gameObject,
                    open.GetComponent<Button>(), close.GetComponent<Button>(),
                    title.GetComponent<Text>(), resource.GetComponent<Text>(),
                    technique.GetComponent<Text>(), status.GetComponent<Text>(),
                    progress.GetComponent<Text>(), cost.GetComponent<Text>(),
                    blockReason.GetComponent<Text>(), message.GetComponent<Text>(),
                    report.GetComponent<Text>(), researchers,
                    researcherTemplate.GetComponent<Button>());
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
                Debug.LogWarning($"SC-021 family research repair rolled back: {exception.Message} " +
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
                    "SC-021 家族研究示范", TitleButtonPosition, new Vector2(300f, 48f));
            }
            else
            {
                button = existing.GetComponent<Button>();
                SetCenteredRect(existing, TitleButtonPosition, new Vector2(300f, 48f));
            }
            if (button == null)
                throw new InvalidOperationException("The SC-021 title entry has no Button component.");

            Undo.RecordObject(controller, UndoName);
            controller.BindResearchScenarioButton(button);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("researchDemoButton").objectReferenceValue = button;
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
            grid.constraintCount = ResearcherColumns;
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
                0f, 0f, CellWidth - 16f, CellHeight - 6f, 14, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            template.SetActive(false);
            return button;
        }

        private static void SetSerializedReferences(FamilyResearchPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text title, Text resource,
            Text technique, Text status, Text progress, Text cost, Text blockReason, Text message,
            Text report, Transform researchers, Button researcherTemplate)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("resourceText").objectReferenceValue = resource;
            serialized.FindProperty("techniqueText").objectReferenceValue = technique;
            serialized.FindProperty("statusText").objectReferenceValue = status;
            serialized.FindProperty("progressText").objectReferenceValue = progress;
            serialized.FindProperty("costText").objectReferenceValue = cost;
            serialized.FindProperty("blockReasonText").objectReferenceValue = blockReason;
            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.FindProperty("reportText").objectReferenceValue = report;
            serialized.FindProperty("researcherContainer").objectReferenceValue = researchers;
            serialized.FindProperty("researcherTemplate").objectReferenceValue = researcherTemplate;
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
