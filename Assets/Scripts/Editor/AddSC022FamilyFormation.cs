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
    /// Transactional FoundationScene updater for the SC-022 marriage/adoption panel and
    /// title entry. One Undo group, preflight, validate before save, Undo rollback on
    /// exception. It never reloads a scene and never calls DestroyImmediate on scene data.
    /// </summary>
    public static class AddSC022FamilyFormation
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "SC022FamilyFormationRoot";
        public const string OpenButtonName = "SC022FamilyFormationOpenButton";
        public const string PanelName = "SC022FamilyFormationPanel";
        public const string TitleName = "SC022FamilyFormationTitleText";
        public const string BlockReasonName = "SC022FamilyFormationBlockReasonText";
        public const string MarriageHeadingName = "SC022MarriageHeading";
        public const string MarriageMemberInfoName = "SC022MarriageMemberInfo";
        public const string MarriageMemberContainerName = "SC022MarriageMemberContainer";
        public const string MarriageMemberTemplateName = "SC022MarriageMemberTemplate";
        public const string MarriageCandidateInfoName = "SC022MarriageCandidateInfo";
        public const string MarriageCandidateContainerName = "SC022MarriageCandidateContainer";
        public const string MarriageCandidateTemplateName = "SC022MarriageCandidateTemplate";
        public const string MarriageConfirmButtonName = "SC022MarriageConfirmButton";
        public const string MarriageMessageName = "SC022MarriageMessage";
        public const string AdoptionHeadingName = "SC022AdoptionHeading";
        public const string AdoptionMemberInfoName = "SC022AdoptionMemberInfo";
        public const string AdoptionMemberContainerName = "SC022AdoptionMemberContainer";
        public const string AdoptionMemberTemplateName = "SC022AdoptionMemberTemplate";
        public const string AdoptionCandidateInfoName = "SC022AdoptionCandidateInfo";
        public const string AdoptionCandidateContainerName = "SC022AdoptionCandidateContainer";
        public const string AdoptionCandidateTemplateName = "SC022AdoptionCandidateTemplate";
        public const string AdoptionConfirmButtonName = "SC022AdoptionConfirmButton";
        public const string AdoptionMessageName = "SC022AdoptionMessage";
        public const string HelpName = "SC022FamilyFormationHelpText";
        public const string CloseButtonName = "SC022FamilyFormationCloseButton";
        public const string TitleButtonName = "SC022FamilyFormationDemoButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-022 Family Formation";

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
        private const string ResearchOpenButtonName = "SC021ResearchOpenButton";
        private const string ResearchTitleButtonName = "SC021ResearchDemoButton";
        private const string ChronicleOpenButtonName = "FamilyChronicleOpenButton";
        private const string RosterOpenButtonName = "FamilyRosterOpenButton";
        private const string IndustryOpenButtonName = "IndustryOpenButton";
        private const string SuccessionHudName = "SuccessionHud";
        private const string UndoName = "Add SC-022 Family Formation";

        // Below the SC-021 research entry (-354..-398).
        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -406f);
        // Left of the centered SC-020 entry and opposite the SC-021 entry.
        private static readonly Vector2 TitleButtonPosition = new Vector2(-340f, -274f);

        private const int CandidateColumns = 2;
        private const float CellWidth = 180f;
        private const float CellHeight = 28f;
        private const float CellSpacing = 6f;
        private const float ContainerWidth = 410f;
        private const float ContainerHeight = 100f;

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
                Debug.Log("SC-022 family-formation panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-022 family-formation updater refused or rolled back: {result}.");
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
            FamilyFormationPanel[] panels = Components<FamilyFormationPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 ||
                roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, FamilyFormationPanel and root.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform blockReason = Require(scene, BlockReasonName);
            Transform marriageHeading = Require(scene, MarriageHeadingName);
            Transform marriageMemberInfo = Require(scene, MarriageMemberInfoName);
            Transform marriageMembers = Require(scene, MarriageMemberContainerName);
            Transform marriageMemberTemplate = Require(scene, MarriageMemberTemplateName);
            Transform marriageCandidateInfo = Require(scene, MarriageCandidateInfoName);
            Transform marriageCandidates = Require(scene, MarriageCandidateContainerName);
            Transform marriageCandidateTemplate = Require(scene, MarriageCandidateTemplateName);
            Transform marriageConfirm = Require(scene, MarriageConfirmButtonName);
            Transform marriageMessage = Require(scene, MarriageMessageName);
            Transform adoptionHeading = Require(scene, AdoptionHeadingName);
            Transform adoptionMemberInfo = Require(scene, AdoptionMemberInfoName);
            Transform adoptionMembers = Require(scene, AdoptionMemberContainerName);
            Transform adoptionMemberTemplate = Require(scene, AdoptionMemberTemplateName);
            Transform adoptionCandidateInfo = Require(scene, AdoptionCandidateInfoName);
            Transform adoptionCandidates = Require(scene, AdoptionCandidateContainerName);
            Transform adoptionCandidateTemplate = Require(scene, AdoptionCandidateTemplateName);
            Transform adoptionConfirm = Require(scene, AdoptionConfirmButtonName);
            Transform adoptionMessage = Require(scene, AdoptionMessageName);
            Transform help = Require(scene, HelpName);
            Transform close = Require(scene, CloseButtonName);
            if (open == null || panel == null || title == null || blockReason == null ||
                marriageHeading == null || marriageMemberInfo == null || marriageMembers == null ||
                marriageMemberTemplate == null || marriageCandidateInfo == null ||
                marriageCandidates == null || marriageCandidateTemplate == null ||
                marriageConfirm == null || marriageMessage == null || adoptionHeading == null ||
                adoptionMemberInfo == null || adoptionMembers == null ||
                adoptionMemberTemplate == null || adoptionCandidateInfo == null ||
                adoptionCandidates == null || adoptionCandidateTemplate == null ||
                adoptionConfirm == null || adoptionMessage == null || help == null || close == null)
                return Invalid(out error, "SC-022 family-formation hierarchy is incomplete or ambiguous.");

            if (root.parent != canvas || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "SC022FamilyFormationRoot must be active, be a direct Canvas child, and own the panel component.");

            Transform mapRoot = canvas.Find(MapRootName);
            Transform industryRoot = canvas.Find(IndustryRootName);
            Transform educationRoot = canvas.Find(EducationRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC022FamilyFormationRoot must render above the map/town HUD so its controls receive raycasts.");
            if (industryRoot != null && root.GetSiblingIndex() <= industryRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC022FamilyFormationRoot must render above the SC-019 industry HUD.");
            if (educationRoot != null && root.GetSiblingIndex() <= educationRoot.GetSiblingIndex())
                return Invalid(out error,
                    "SC022FamilyFormationRoot must render above the SC-020 education HUD.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "SC022FamilyFormationRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "SC022FamilyFormationRoot must render below the event-choice overlay.");

            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error, "The family-formation open button must be an active direct child of its root.");
            if (panel.parent != root || panel.gameObject.activeSelf)
                return Invalid(out error, "SC022FamilyFormationPanel must be a hidden direct child of the root.");
            Transform[] panelChildren =
            {
                title, blockReason, marriageHeading, marriageMemberInfo, marriageMembers,
                marriageCandidateInfo, marriageCandidates, marriageConfirm, marriageMessage,
                adoptionHeading, adoptionMemberInfo, adoptionMembers, adoptionCandidateInfo,
                adoptionCandidates, adoptionConfirm, adoptionMessage, help, close
            };
            foreach (Transform child in panelChildren)
                if (child.parent != panel)
                    return Invalid(out error,
                        "All SC-022 family-formation controls must be direct children of the panel.");

            if (!ValidateContainer(marriageMembers, marriageMemberTemplate, out error))
                return false;
            if (!ValidateContainer(adoptionMembers, adoptionMemberTemplate, out error))
                return false;
            if (!ValidateContainer(marriageCandidates, marriageCandidateTemplate, out error))
                return false;
            if (!ValidateContainer(adoptionCandidates, adoptionCandidateTemplate, out error))
                return false;

            Text titleText = title.GetComponent<Text>();
            Text blockText = blockReason.GetComponent<Text>();
            Text marriageMemberText = marriageMemberInfo.GetComponent<Text>();
            Text marriageCandidateText = marriageCandidateInfo.GetComponent<Text>();
            Text marriageMessageText = marriageMessage.GetComponent<Text>();
            Text adoptionMemberText = adoptionMemberInfo.GetComponent<Text>();
            Text adoptionCandidateText = adoptionCandidateInfo.GetComponent<Text>();
            Text adoptionMessageText = adoptionMessage.GetComponent<Text>();
            Text helpText = help.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            Button marriageConfirmButton = marriageConfirm.GetComponent<Button>();
            Button adoptionConfirmButton = adoptionConfirm.GetComponent<Button>();
            if (titleText == null || blockText == null || marriageMemberText == null ||
                marriageCandidateText == null || marriageMessageText == null ||
                adoptionMemberText == null || adoptionCandidateText == null ||
                adoptionMessageText == null || helpText == null ||
                titleText.font == null || blockText.font == null ||
                marriageMemberText.font == null || marriageCandidateText.font == null ||
                marriageMessageText.font == null || adoptionMemberText.font == null ||
                adoptionCandidateText.font == null || adoptionMessageText.font == null ||
                helpText.font == null || openButton == null || closeButton == null ||
                marriageConfirmButton == null || adoptionConfirmButton == null ||
                !HasButtonVisual(openButton) || !HasButtonVisual(closeButton) ||
                !HasButtonVisual(marriageConfirmButton) || !HasButtonVisual(adoptionConfirmButton) ||
                !HasRaycastTarget(openButton) || !HasRaycastTarget(closeButton) ||
                !HasRaycastTarget(marriageConfirmButton) ||
                !HasRaycastTarget(adoptionConfirmButton) ||
                panel.GetComponent<Image>() == null || !panel.GetComponent<Image>().raycastTarget)
                return Invalid(out error,
                    "SC-022 text, buttons, background or raycast components are invalid.");
            if (blockReason.gameObject.activeSelf)
                return Invalid(out error, "The family-formation block-reason line must start hidden.");
            if (marriageMessage.gameObject.activeSelf || adoptionMessage.gameObject.activeSelf)
                return Invalid(out error, "The family-formation message lines must start hidden.");
            if (!titleText.text.Contains("婚姻") || !titleText.text.Contains("收养"))
                return Invalid(out error, "The family-formation panel title must contain 婚姻 and 收养.");

            FamilyFormationPanel formationPanel = panels[0];
            if (!Bound(formationPanel, "gameSession", sessions[0]) ||
                !Bound(formationPanel, "panelRoot", panel.gameObject) ||
                !Bound(formationPanel, "openButton", openButton) ||
                !Bound(formationPanel, "closeButton", closeButton) ||
                !Bound(formationPanel, "titleText", titleText) ||
                !Bound(formationPanel, "blockReasonText", blockText) ||
                !Bound(formationPanel, "marriageMemberInfo", marriageMemberText) ||
                !Bound(formationPanel, "marriageMemberContainer", marriageMembers) ||
                !Bound(formationPanel, "marriageMemberTemplate",
                    marriageMemberTemplate.GetComponent<Button>()) ||
                !Bound(formationPanel, "marriageCandidateInfo", marriageCandidateText) ||
                !Bound(formationPanel, "marriageCandidateContainer", marriageCandidates) ||
                !Bound(formationPanel, "marriageCandidateTemplate",
                    marriageCandidateTemplate.GetComponent<Button>()) ||
                !Bound(formationPanel, "marriageConfirmButton", marriageConfirmButton) ||
                !Bound(formationPanel, "marriageMessage", marriageMessageText) ||
                !Bound(formationPanel, "adoptionMemberInfo", adoptionMemberText) ||
                !Bound(formationPanel, "adoptionMemberContainer", adoptionMembers) ||
                !Bound(formationPanel, "adoptionMemberTemplate",
                    adoptionMemberTemplate.GetComponent<Button>()) ||
                !Bound(formationPanel, "adoptionCandidateInfo", adoptionCandidateText) ||
                !Bound(formationPanel, "adoptionCandidateContainer", adoptionCandidates) ||
                !Bound(formationPanel, "adoptionCandidateTemplate",
                    adoptionCandidateTemplate.GetComponent<Button>()) ||
                !Bound(formationPanel, "adoptionConfirmButton", adoptionConfirmButton) ||
                !Bound(formationPanel, "adoptionMessage", adoptionMessageText))
                return Invalid(out error, "FamilyFormationPanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform panelRect = panel as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform closeRect = close as RectTransform;
            RectTransform titleRect = title as RectTransform;
            RectTransform blockRect = blockReason as RectTransform;
            RectTransform helpRect = help as RectTransform;
            if (rootRect == null || panelRect == null || openRect == null || closeRect == null ||
                titleRect == null || blockRect == null || helpRect == null ||
                !Contained(panelRect, rootRect) || !Contained(openRect, rootRect) ||
                !Contained(titleRect, panelRect) || !Contained(blockRect, panelRect) ||
                !Contained(helpRect, panelRect) || !Contained(closeRect, panelRect))
                return Invalid(out error, "SC-022 family-formation controls lie outside their bounds.");

            RectTransform[] leftColumn =
            {
                marriageHeading as RectTransform, marriageMemberInfo as RectTransform,
                marriageMembers as RectTransform, marriageCandidateInfo as RectTransform,
                marriageCandidates as RectTransform, marriageConfirm as RectTransform,
                marriageMessage as RectTransform
            };
            RectTransform[] rightColumn =
            {
                adoptionHeading as RectTransform, adoptionMemberInfo as RectTransform,
                adoptionMembers as RectTransform, adoptionCandidateInfo as RectTransform,
                adoptionCandidates as RectTransform, adoptionConfirm as RectTransform,
                adoptionMessage as RectTransform
            };
            foreach (RectTransform rect in leftColumn.Concat(rightColumn))
                if (rect == null || !Contained(rect, panelRect))
                    return Invalid(out error,
                        "SC-022 family-formation controls lie outside the panel bounds.");
            for (int i = 1; i < leftColumn.Length; i++)
                if (Overlaps(leftColumn[i - 1], leftColumn[i], panelRect))
                    return Invalid(out error,
                        "SC-022 marriage controls overlap each other.");
            for (int i = 1; i < rightColumn.Length; i++)
                if (Overlaps(rightColumn[i - 1], rightColumn[i], panelRect))
                    return Invalid(out error,
                        "SC-022 adoption controls overlap each other.");
            if (Overlaps(leftColumn[0], rightColumn[0], panelRect) ||
                Overlaps(leftColumn[1], rightColumn[1], panelRect) ||
                Overlaps(leftColumn[2], rightColumn[2], panelRect) ||
                Overlaps(leftColumn[3], rightColumn[3], panelRect) ||
                Overlaps(leftColumn[4], rightColumn[4], panelRect) ||
                Overlaps(leftColumn[5], rightColumn[5], panelRect) ||
                Overlaps(leftColumn[6], rightColumn[6], panelRect))
                return Invalid(out error, "SC-022 marriage and adoption columns overlap.");
            if (Overlaps(titleRect, blockRect, panelRect) ||
                Overlaps(blockRect, leftColumn[0], panelRect) ||
                Overlaps(leftColumn[6], helpRect, panelRect) ||
                Overlaps(helpRect, closeRect, panelRect))
                return Invalid(out error, "SC-022 header, help or close controls overlap.");

            if (Overlaps(openRect, SuccessionHud(scene), rootRect) ||
                OverlapsNamed(scene, openRect, IndustryOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, EducationOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, ResearchOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, ChronicleOpenButtonName, rootRect) ||
                OverlapsNamed(scene, openRect, RosterOpenButtonName, rootRect))
                return Invalid(out error,
                    "The family-formation open button overlaps the succession HUD or another HUD entry.");

            return ValidateTitleEntry(scene, canvas, sessions[0]);
        }

        private static bool ValidateContainer(Transform container, Transform template, out string error)
        {
            RectTransform containerRect = container as RectTransform;
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (containerRect == null || grid == null)
            {
                error = "SC-022 candidate container must use a GridLayoutGroup.";
                return false;
            }
            if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                grid.constraintCount != CandidateColumns ||
                grid.cellSize.x + 0.5f < CellWidth || grid.cellSize.y + 0.5f < CellHeight ||
                grid.spacing.x < 0f || grid.spacing.y < 0f)
            {
                error = "SC-022 candidate container has an unusable grid layout.";
                return false;
            }
            int rows = 3;
            float requiredWidth = grid.padding.left + grid.padding.right +
                                  CandidateColumns * grid.cellSize.x +
                                  (CandidateColumns - 1) * grid.spacing.x;
            float requiredHeight = grid.padding.top + grid.padding.bottom +
                                   rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
            if (containerRect.rect.width + 0.5f < requiredWidth ||
                containerRect.rect.height + 0.5f < requiredHeight)
            {
                error = "SC-022 candidate container is too small for its three rows.";
                return false;
            }
            if (template == null || template.gameObject.activeSelf || template.parent != container)
            {
                error = "The SC-022 candidate template must exist inactive under its container.";
                return false;
            }
            RectTransform templateRect = template as RectTransform;
            if (templateRect == null || !Contained(templateRect, containerRect))
            {
                error = "The SC-022 candidate template must lie inside its container.";
                return false;
            }
            Button button = template.GetComponent<Button>();
            Image image = template.GetComponent<Image>();
            if (button == null || image == null || button.targetGraphic != image ||
                !HasRaycastTarget(button) || template.GetComponentInChildren<Text>(true) == null)
            {
                error = "The SC-022 candidate template graphic or label is invalid.";
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
                return Invalid(out _, "The SC-022 title entry or TitleScreenController is missing.");
            if (button.parent != titleRoot || !button.gameObject.activeSelf)
                return Invalid(out _, "The SC-022 title entry must be active under M6TitleRoot.");
            if (!HasButtonVisual(button.GetComponent<Button>()) ||
                !HasRaycastTarget(button.GetComponent<Button>()))
                return Invalid(out _, "The SC-022 title entry button is invalid.");
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null || !label.text.Contains("婚姻") || !label.text.Contains("收养"))
                return Invalid(out _, "The SC-022 title entry label must contain 婚姻 and 收养.");
            if (!Bound(controllers[0], "familyFormationDemoButton",
                    button.GetComponent<Button>()) ||
                !Bound(controllers[0], "gameSession", session))
                return Invalid(out _, "TitleScreenController SC-022 references are not bound.");

            RectTransform rootRect = titleRoot as RectTransform;
            RectTransform buttonRect = button as RectTransform;
            if (rootRect == null || buttonRect == null || !Contained(buttonRect, rootRect))
                return Invalid(out _, "The SC-022 title entry lies outside M6TitleRoot.");
            if (OverlapsNamed(scene, buttonRect, CrisisButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, TitleStatusName, rootRect) ||
                OverlapsNamed(scene, buttonRect, StandardButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, NewGameButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ContinueButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, EducationTitleButtonName, rootRect) ||
                OverlapsNamed(scene, buttonRect, ResearchTitleButtonName, rootRect))
                return Invalid(out _, "The SC-022 title entry overlaps another title control.");
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
            if (canvas == null || session == null ||
                Components<FamilyFormationPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (HasCompleteHierarchy(scene))
                return Repair(scene, save, beforeValidation);

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, PanelName).Any() || Named(scene, TitleButtonName).Any() ||
                Components<FamilyFormationPanel>(scene).Length != 0 ||
                HasSerializedReference(Components<TitleScreenController>(scene).FirstOrDefault(),
                    "familyFormationDemoButton"))
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

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "婚姻与收养",
                    200f, 44f, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, new Vector2(200f, 44f));

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(920f, 620f);
                panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font, "婚姻与收养",
                    0f, 280f, 860f, 36f, 22, TextAnchor.MiddleLeft);
                Text blockReason = CreateText(scene, panelRect, BlockReasonName, font, string.Empty,
                    0f, 246f, 860f, 24f, 14, TextAnchor.MiddleLeft);
                blockReason.color = new Color(1f, 0.72f, 0.62f, 1f);
                blockReason.gameObject.SetActive(false);

                CreateHeading(scene, panelRect, MarriageHeadingName, font, "婚姻", -225f);
                Text marriageMemberInfo = CreateText(scene, panelRect, MarriageMemberInfoName, font,
                    "请选择婚姻发起人。", -225f, 178f, 410f, 30f, 14, TextAnchor.MiddleLeft);
                Transform marriageMembers = CreateContainer(scene, panelRect,
                    MarriageMemberContainerName, -225f, 112f);
                Button marriageMemberTemplate = CreateTemplate(scene, marriageMembers,
                    MarriageMemberTemplateName, font, "家人");
                Text marriageCandidateInfo = CreateText(scene, panelRect,
                    MarriageCandidateInfoName, font, "请先选择婚姻发起人。", -225f, 38f, 410f, 40f,
                    14, TextAnchor.UpperLeft);
                Transform marriageCandidates = CreateContainer(scene, panelRect,
                    MarriageCandidateContainerName, -225f, -34f);
                Button marriageCandidateTemplate = CreateTemplate(scene, marriageCandidates,
                    MarriageCandidateTemplateName, font, "候选人");
                Button marriageConfirm = CreateButton(scene, panelRect, MarriageConfirmButtonName,
                    font, "确认登记婚姻", 240f, 36f, 15);
                SetCenteredRect(marriageConfirm.transform, new Vector2(-225f, -104f),
                    new Vector2(240f, 36f));
                Text marriageMessage = CreateText(scene, panelRect, MarriageMessageName, font,
                    string.Empty, -225f, -144f, 410f, 24f, 13, TextAnchor.MiddleLeft);
                marriageMessage.color = new Color(0.65f, 0.86f, 1f, 1f);
                marriageMessage.gameObject.SetActive(false);

                CreateHeading(scene, panelRect, AdoptionHeadingName, font, "收养", 225f);
                Text adoptionMemberInfo = CreateText(scene, panelRect, AdoptionMemberInfoName, font,
                    "请选择收养人。", 225f, 178f, 410f, 30f, 14, TextAnchor.MiddleLeft);
                Transform adoptionMembers = CreateContainer(scene, panelRect,
                    AdoptionMemberContainerName, 225f, 112f);
                Button adoptionMemberTemplate = CreateTemplate(scene, adoptionMembers,
                    AdoptionMemberTemplateName, font, "家人");
                Text adoptionCandidateInfo = CreateText(scene, panelRect,
                    AdoptionCandidateInfoName, font, "请先选择收养人。", 225f, 38f, 410f, 40f,
                    14, TextAnchor.UpperLeft);
                Transform adoptionCandidates = CreateContainer(scene, panelRect,
                    AdoptionCandidateContainerName, 225f, -34f);
                Button adoptionCandidateTemplate = CreateTemplate(scene, adoptionCandidates,
                    AdoptionCandidateTemplateName, font, "候选人");
                Button adoptionConfirm = CreateButton(scene, panelRect, AdoptionConfirmButtonName,
                    font, "确认办理收养", 240f, 36f, 15);
                SetCenteredRect(adoptionConfirm.transform, new Vector2(225f, -104f),
                    new Vector2(240f, 36f));
                Text adoptionMessage = CreateText(scene, panelRect, AdoptionMessageName, font,
                    string.Empty, 225f, -144f, 410f, 24f, 13, TextAnchor.MiddleLeft);
                adoptionMessage.color = new Color(0.65f, 0.86f, 1f, 1f);
                adoptionMessage.gameObject.SetActive(false);

                Text help = CreateText(scene, panelRect, HelpName, font,
                    "选择家人与候选人后，确认会立即写入成员身份、亲属事实和家族编年史；" +
                    "正式关系不可撤销，普通收养不改变继承状态。",
                    0f, -190f, 860f, 44f, 13, TextAnchor.MiddleCenter);
                help.color = new Color(0.75f, 0.9f, 1f, 1f);
                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    160f, 36f, 16);
                SetCenteredRect(close.transform, new Vector2(0f, -258f), new Vector2(160f, 36f));

                FamilyFormationPanel panel = Undo.AddComponent<FamilyFormationPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, panelObject, open, close, title, blockReason,
                    marriageMemberInfo, marriageMembers, marriageMemberTemplate,
                    marriageCandidateInfo, marriageCandidates, marriageCandidateTemplate,
                    marriageConfirm, marriageMessage, adoptionMemberInfo, adoptionMembers,
                    adoptionMemberTemplate, adoptionCandidateInfo, adoptionCandidates,
                    adoptionCandidateTemplate, adoptionConfirm, adoptionMessage);
                SetSerializedReferences(panel, session, panelObject, open, close, title,
                    blockReason, marriageMemberInfo, marriageMembers, marriageMemberTemplate,
                    marriageCandidateInfo, marriageCandidates, marriageCandidateTemplate,
                    marriageConfirm, marriageMessage, adoptionMemberInfo, adoptionMembers,
                    adoptionMemberTemplate, adoptionCandidateInfo, adoptionCandidates,
                    adoptionCandidateTemplate, adoptionConfirm, adoptionMessage);
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
                Debug.LogWarning($"SC-022 family-formation updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool HasCompleteHierarchy(Scene scene)
        {
            if (Components<FamilyFormationPanel>(scene).Length != 1)
                return false;
            string[] required =
            {
                RootName, OpenButtonName, PanelName, TitleName, BlockReasonName,
                MarriageHeadingName, MarriageMemberInfoName, MarriageMemberContainerName,
                MarriageMemberTemplateName, MarriageCandidateInfoName,
                MarriageCandidateContainerName, MarriageCandidateTemplateName,
                MarriageConfirmButtonName, MarriageMessageName, AdoptionHeadingName,
                AdoptionMemberInfoName, AdoptionMemberContainerName, AdoptionMemberTemplateName,
                AdoptionCandidateInfoName, AdoptionCandidateContainerName,
                AdoptionCandidateTemplateName, AdoptionConfirmButtonName, AdoptionMessageName,
                HelpName, CloseButtonName
            };
            foreach (string name in required)
                if (Named(scene, name).Count() != 1)
                    return false;
            Transform root = Require(scene, RootName);
            return root != null && root.parent != null &&
                   root.parent.name == CanvasName && root.parent.GetComponent<Canvas>() != null;
        }

        /// <summary>
        /// Transactional repair for a complete SC-022 hierarchy. It re-applies layout and
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
                Transform blockReason = Require(scene, BlockReasonName);
                Transform marriageHeading = Require(scene, MarriageHeadingName);
                Transform marriageMemberInfo = Require(scene, MarriageMemberInfoName);
                Transform marriageMembers = Require(scene, MarriageMemberContainerName);
                Transform marriageMemberTemplate = Require(scene, MarriageMemberTemplateName);
                Transform marriageCandidateInfo = Require(scene, MarriageCandidateInfoName);
                Transform marriageCandidates = Require(scene, MarriageCandidateContainerName);
                Transform marriageCandidateTemplate = Require(scene, MarriageCandidateTemplateName);
                Transform marriageConfirm = Require(scene, MarriageConfirmButtonName);
                Transform marriageMessage = Require(scene, MarriageMessageName);
                Transform adoptionHeading = Require(scene, AdoptionHeadingName);
                Transform adoptionMemberInfo = Require(scene, AdoptionMemberInfoName);
                Transform adoptionMembers = Require(scene, AdoptionMemberContainerName);
                Transform adoptionMemberTemplate = Require(scene, AdoptionMemberTemplateName);
                Transform adoptionCandidateInfo = Require(scene, AdoptionCandidateInfoName);
                Transform adoptionCandidates = Require(scene, AdoptionCandidateContainerName);
                Transform adoptionCandidateTemplate = Require(scene, AdoptionCandidateTemplateName);
                Transform adoptionConfirm = Require(scene, AdoptionConfirmButtonName);
                Transform adoptionMessage = Require(scene, AdoptionMessageName);
                Transform help = Require(scene, HelpName);
                Transform close = Require(scene, CloseButtonName);
                if (root == null || panel == null || open == null || title == null ||
                    blockReason == null || marriageHeading == null || marriageMemberInfo == null ||
                    marriageMembers == null || marriageMemberTemplate == null ||
                    marriageCandidateInfo == null || marriageCandidates == null ||
                    marriageCandidateTemplate == null || marriageConfirm == null ||
                    marriageMessage == null || adoptionHeading == null ||
                    adoptionMemberInfo == null || adoptionMembers == null ||
                    adoptionMemberTemplate == null || adoptionCandidateInfo == null ||
                    adoptionCandidates == null || adoptionCandidateTemplate == null ||
                    adoptionConfirm == null || adoptionMessage == null || help == null || close == null)
                    throw new InvalidOperationException(
                        "Complete SC-022 hierarchy could not be resolved.");

                SetTopLeftRect(open, OpenButtonPosition, new Vector2(200f, 44f));
                SetCenteredRect(panel, Vector2.zero, new Vector2(920f, 620f));
                SetCenteredRect(title, new Vector2(0f, 280f), new Vector2(860f, 36f));
                SetCenteredRect(blockReason, new Vector2(0f, 246f), new Vector2(860f, 24f));
                SetCenteredRect(marriageHeading, new Vector2(-225f, 208f), new Vector2(410f, 28f));
                SetCenteredRect(marriageMemberInfo, new Vector2(-225f, 178f), new Vector2(410f, 30f));
                SetCenteredRect(marriageMembers, new Vector2(-225f, 112f),
                    new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(marriageCandidateInfo, new Vector2(-225f, 38f),
                    new Vector2(410f, 40f));
                SetCenteredRect(marriageCandidates, new Vector2(-225f, -34f),
                    new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(marriageConfirm, new Vector2(-225f, -104f), new Vector2(240f, 36f));
                SetCenteredRect(marriageMessage, new Vector2(-225f, -144f), new Vector2(410f, 24f));
                SetCenteredRect(adoptionHeading, new Vector2(225f, 208f), new Vector2(410f, 28f));
                SetCenteredRect(adoptionMemberInfo, new Vector2(225f, 178f), new Vector2(410f, 30f));
                SetCenteredRect(adoptionMembers, new Vector2(225f, 112f),
                    new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(adoptionCandidateInfo, new Vector2(225f, 38f),
                    new Vector2(410f, 40f));
                SetCenteredRect(adoptionCandidates, new Vector2(225f, -34f),
                    new Vector2(ContainerWidth, ContainerHeight));
                SetCenteredRect(adoptionConfirm, new Vector2(225f, -104f), new Vector2(240f, 36f));
                SetCenteredRect(adoptionMessage, new Vector2(225f, -144f), new Vector2(410f, 24f));
                SetCenteredRect(help, new Vector2(0f, -190f), new Vector2(860f, 44f));
                SetCenteredRect(close, new Vector2(0f, -258f), new Vector2(160f, 36f));
                ConfigureContainer(marriageMembers);
                ConfigureContainer(marriageCandidates);
                ConfigureContainer(adoptionMembers);
                ConfigureContainer(adoptionCandidates);
                SetCenteredRect(marriageMemberTemplate, Vector2.zero,
                    new Vector2(CellWidth, CellHeight));
                SetCenteredRect(marriageCandidateTemplate, Vector2.zero,
                    new Vector2(CellWidth, CellHeight));
                SetCenteredRect(adoptionMemberTemplate, Vector2.zero,
                    new Vector2(CellWidth, CellHeight));
                SetCenteredRect(adoptionCandidateTemplate, Vector2.zero,
                    new Vector2(CellWidth, CellHeight));
                blockReason.gameObject.SetActive(false);
                marriageMessage.gameObject.SetActive(false);
                adoptionMessage.gameObject.SetActive(false);

                FamilyFormationPanel component = root.GetComponent<FamilyFormationPanel>();
                if (component == null)
                    throw new InvalidOperationException(
                        "SC022FamilyFormationRoot no longer owns FamilyFormationPanel.");
                Undo.RecordObject(component, UndoName);
                GameSession session = Components<GameSession>(scene)[0];
                component.BindReferences(session, panel.gameObject, open.GetComponent<Button>(),
                    close.GetComponent<Button>(), title.GetComponent<Text>(),
                    blockReason.GetComponent<Text>(), marriageMemberInfo.GetComponent<Text>(),
                    marriageMembers, marriageMemberTemplate.GetComponent<Button>(),
                    marriageCandidateInfo.GetComponent<Text>(), marriageCandidates,
                    marriageCandidateTemplate.GetComponent<Button>(),
                    marriageConfirm.GetComponent<Button>(), marriageMessage.GetComponent<Text>(),
                    adoptionMemberInfo.GetComponent<Text>(), adoptionMembers,
                    adoptionMemberTemplate.GetComponent<Button>(),
                    adoptionCandidateInfo.GetComponent<Text>(), adoptionCandidates,
                    adoptionCandidateTemplate.GetComponent<Button>(),
                    adoptionConfirm.GetComponent<Button>(), adoptionMessage.GetComponent<Text>());
                SetSerializedReferences(component, session, panel.gameObject,
                    open.GetComponent<Button>(), close.GetComponent<Button>(),
                    title.GetComponent<Text>(), blockReason.GetComponent<Text>(),
                    marriageMemberInfo.GetComponent<Text>(), marriageMembers,
                    marriageMemberTemplate.GetComponent<Button>(),
                    marriageCandidateInfo.GetComponent<Text>(), marriageCandidates,
                    marriageCandidateTemplate.GetComponent<Button>(),
                    marriageConfirm.GetComponent<Button>(), marriageMessage.GetComponent<Text>(),
                    adoptionMemberInfo.GetComponent<Text>(), adoptionMembers,
                    adoptionMemberTemplate.GetComponent<Button>(),
                    adoptionCandidateInfo.GetComponent<Text>(), adoptionCandidates,
                    adoptionCandidateTemplate.GetComponent<Button>(),
                    adoptionConfirm.GetComponent<Button>(), adoptionMessage.GetComponent<Text>());
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
                Debug.LogWarning($"SC-022 family-formation repair rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void EnsureTitleEntry(Scene scene, Transform canvas, GameSession session,
            Font font)
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
                    "SC-022 婚姻与收养示范", TitleButtonPosition, new Vector2(300f, 48f));
            }
            else
            {
                button = existing.GetComponent<Button>();
                SetCenteredRect(existing, TitleButtonPosition, new Vector2(300f, 48f));
            }
            if (button == null)
                throw new InvalidOperationException(
                    "The SC-022 title entry has no Button component.");

            Undo.RecordObject(controller, UndoName);
            controller.BindFamilyFormationScenarioButton(button);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("familyFormationDemoButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void CreateHeading(Scene scene, Transform panel, string name, Font font,
            string label, float x)
        {
            Text heading = CreateText(scene, panel, name, font, label, x, 208f, 410f, 28f, 17,
                TextAnchor.MiddleLeft);
            heading.color = new Color(0.9f, 0.82f, 0.6f, 1f);
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

        private static Transform CreateContainer(Scene scene, Transform panel, string name, float x,
            float y)
        {
            GameObject container = Create(scene, panel, name, typeof(GridLayoutGroup));
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
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
            grid.constraintCount = CandidateColumns;
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
                0f, 0f, CellWidth - 12f, CellHeight - 4f, 13, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            template.SetActive(false);
            return button;
        }

        private static void SetSerializedReferences(FamilyFormationPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text title, Text blockReason,
            Text marriageMemberInfo, Transform marriageMembers, Button marriageMemberTemplate,
            Text marriageCandidateInfo, Transform marriageCandidates,
            Button marriageCandidateTemplate, Button marriageConfirm, Text marriageMessage,
            Text adoptionMemberInfo, Transform adoptionMembers, Button adoptionMemberTemplate,
            Text adoptionCandidateInfo, Transform adoptionCandidates,
            Button adoptionCandidateTemplate, Button adoptionConfirm, Text adoptionMessage)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("blockReasonText").objectReferenceValue = blockReason;
            serialized.FindProperty("marriageMemberInfo").objectReferenceValue = marriageMemberInfo;
            serialized.FindProperty("marriageMemberContainer").objectReferenceValue = marriageMembers;
            serialized.FindProperty("marriageMemberTemplate").objectReferenceValue =
                marriageMemberTemplate;
            serialized.FindProperty("marriageCandidateInfo").objectReferenceValue =
                marriageCandidateInfo;
            serialized.FindProperty("marriageCandidateContainer").objectReferenceValue =
                marriageCandidates;
            serialized.FindProperty("marriageCandidateTemplate").objectReferenceValue =
                marriageCandidateTemplate;
            serialized.FindProperty("marriageConfirmButton").objectReferenceValue = marriageConfirm;
            serialized.FindProperty("marriageMessage").objectReferenceValue = marriageMessage;
            serialized.FindProperty("adoptionMemberInfo").objectReferenceValue = adoptionMemberInfo;
            serialized.FindProperty("adoptionMemberContainer").objectReferenceValue = adoptionMembers;
            serialized.FindProperty("adoptionMemberTemplate").objectReferenceValue =
                adoptionMemberTemplate;
            serialized.FindProperty("adoptionCandidateInfo").objectReferenceValue =
                adoptionCandidateInfo;
            serialized.FindProperty("adoptionCandidateContainer").objectReferenceValue =
                adoptionCandidates;
            serialized.FindProperty("adoptionCandidateTemplate").objectReferenceValue =
                adoptionCandidateTemplate;
            serialized.FindProperty("adoptionConfirmButton").objectReferenceValue = adoptionConfirm;
            serialized.FindProperty("adoptionMessage").objectReferenceValue = adoptionMessage;
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

        private static GameObject Create(Scene scene, Transform parent, string name,
            params Type[] components)
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

        private static Text CreateText(Scene scene, Transform parent, string name, Font font,
            string value, float x, float y, float width, float height, int fontSize,
            TextAnchor alignment)
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
            GameObject go = Create(scene, parent, name,
                typeof(Image), typeof(Button), typeof(LayoutElement));
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

        private static bool Overlaps(RectTransform first, RectTransform second,
            RectTransform coordinateRoot)
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
            var firstRect = Rect.MinMaxRect(Mathf.Min(firstMin.x, firstMax.x),
                Mathf.Min(firstMin.y, firstMax.y), Mathf.Max(firstMin.x, firstMax.x),
                Mathf.Max(firstMin.y, firstMax.y));
            var secondRect = Rect.MinMaxRect(Mathf.Min(secondMin.x, secondMax.x),
                Mathf.Min(secondMin.y, secondMax.y), Mathf.Max(secondMin.x, secondMax.x),
                Mathf.Max(secondMin.y, secondMax.y));
            return firstRect.Overlaps(secondRect, true);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }
    }
}
