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
    /// Transactional FoundationScene updater for the SC-019 industry panel.
    /// One Undo group, preflight, validate before saving, Undo rollback on exception.
    /// </summary>
    public static class AddSC019IndustryPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "IndustryRoot";
        public const string OpenButtonName = "IndustryOpenButton";
        public const string PanelName = "IndustryPanel";
        public const string TitleName = "IndustryTitleText";
        public const string ResourceName = "IndustryResourceText";
        public const string BlockReasonName = "IndustryBlockReasonText";
        public const string MessageName = "IndustryMessageText";
        public const string ReportName = "IndustryReportText";
        public const string CloseButtonName = "IndustryCloseButton";
        public const string FarmRowName = "IndustryRowFarm";
        public const string FarmNameTextName = "FarmRowNameText";
        public const string FarmStatusTextName = "FarmRowStatusText";
        public const string FarmAssigneeTextName = "FarmRowAssigneeText";
        public const string FarmUnassignButtonName = "FarmUnassignButton";
        public const string FarmCandidateContainerName = "FarmCandidateContainer";
        public const string TradingRowName = "IndustryRowTradingHouse";
        public const string TradingNameTextName = "TradingRowNameText";
        public const string TradingStatusTextName = "TradingRowStatusText";
        public const string TradingAssigneeTextName = "TradingRowAssigneeText";
        public const string TradingUnassignButtonName = "TradingUnassignButton";
        public const string TradingCandidateContainerName = "TradingCandidateContainer";
        public const string CandidateTemplateName = "IndustryCandidateButtonTemplate";
        public const string ClockFeedbackName = "MonthTurnFeedbackText";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add SC-019 Industry Panel";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventChoiceRootName = "EventChoiceRoot";
        private const string ChronicleOpenButtonName = "FamilyChronicleOpenButton";
        private const string RosterOpenButtonName = "FamilyRosterOpenButton";
        private const string UndoName = "Add SC-019 Industry Panel";

        // The top-left corner is occupied by the active 430x210 SC-010
        // SuccessionHud at (20,-20); this offset keeps the industry entry in the
        // vacant strip directly beneath the HUD instead of covering its head line.
        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -250f);

        private const int CandidateColumns = 3;
        private const int CandidateCapacity = 6;
        private const float CandidateCellWidth = 196f;
        private const float CandidateCellHeight = 28f;
        private const float CandidateSpacing = 8f;
        private const float CandidateAreaWidth = 620f;
        private const float CandidateAreaHeight = 64f;
        private const float RowWidth = 700f;
        private const float RowHeight = 140f;
        private const float FarmRowY = 60f;
        private const float TradingRowY = -82f;

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
                Debug.Log("SC-019 industry panel validated; FoundationScene saved.");
            else
                Debug.LogError($"SC-019 industry updater refused or rolled back: {result}.");
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
            IndustryPanel[] panels = Components<IndustryPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 || roots.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, IndustryPanel and industry root.");

            Transform root = roots[0];
            Transform open = Require(scene, OpenButtonName);
            Transform panel = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform resources = Require(scene, ResourceName);
            Transform blockReason = Require(scene, BlockReasonName);
            Transform message = Require(scene, MessageName);
            Transform report = Require(scene, ReportName);
            Transform close = Require(scene, CloseButtonName);
            Transform farm = Require(scene, FarmRowName);
            Transform trading = Require(scene, TradingRowName);
            if (open == null || panel == null || title == null || resources == null ||
                blockReason == null || message == null || report == null || close == null ||
                farm == null || trading == null)
                return Invalid(out error, "Industry hierarchy is incomplete or ambiguous.");

            if (root.parent != canvases[0] || panels[0].transform != root || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "IndustryRoot must be active, be a direct Canvas child, and own the panel component.");

            Transform canvas = canvases[0];
            Transform mapRoot = canvas.Find(MapRootName);
            Transform titleRoot = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (mapRoot != null && root.GetSiblingIndex() <= mapRoot.GetSiblingIndex())
                return Invalid(out error,
                    "IndustryRoot must render above the map/town HUD so its controls receive raycasts.");
            if (titleRoot != null && root.GetSiblingIndex() >= titleRoot.GetSiblingIndex())
                return Invalid(out error, "IndustryRoot must render below the title overlay.");
            if (eventRoot != null && root.GetSiblingIndex() >= eventRoot.GetSiblingIndex())
                return Invalid(out error, "IndustryRoot must render below the event-choice overlay.");

            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error, "The open button must be an active direct child of the industry root.");
            if (panel.parent != root || panel.gameObject.activeSelf)
                return Invalid(out error, "IndustryPanel must be a hidden direct child of the root.");
            if (title.parent != panel || resources.parent != panel || blockReason.parent != panel ||
                message.parent != panel || report.parent != panel || close.parent != panel ||
                farm.parent != panel || trading.parent != panel)
                return Invalid(out error,
                    "Industry title, resources, block reason, message, rows, report and close button must be panel children.");

            if (!ValidateRow(scene, farm, FarmNameTextName, FarmStatusTextName, FarmAssigneeTextName,
                    FarmUnassignButtonName, FarmCandidateContainerName, out error))
                return false;
            if (!ValidateRow(scene, trading, TradingNameTextName, TradingStatusTextName,
                    TradingAssigneeTextName, TradingUnassignButtonName, TradingCandidateContainerName,
                    out error))
                return false;

            Transform template = Require(scene, CandidateTemplateName);
            Transform farmContainer = Require(scene, FarmCandidateContainerName);
            Transform tradingContainer = Require(scene, TradingCandidateContainerName);
            if (template == null || farmContainer == null || tradingContainer == null ||
                template.gameObject.activeSelf)
                return Invalid(out error, "The candidate template must exist and stay inactive.");
            if (template.parent != farmContainer && template.parent != tradingContainer)
                return Invalid(out error, "The candidate template must live under one candidate container.");
            if (!ValidateCandidateArea(farmContainer, farm, out error) ||
                !ValidateCandidateArea(tradingContainer, trading, out error))
                return false;
            RectTransform templateRect = template as RectTransform;
            RectTransform templateParentRect = template.parent as RectTransform;
            if (templateRect == null || templateParentRect == null ||
                !Contained(templateRect, templateParentRect))
                return Invalid(out error,
                    "The candidate template must lie inside its candidate container bounds.");
            Button templateButton = template.GetComponent<Button>();
            Image templateImage = template.GetComponent<Image>();
            if (templateButton == null || templateImage == null ||
                templateButton.targetGraphic != templateImage || !HasRaycastTarget(templateButton) ||
                template.GetComponentInChildren<Text>(true) == null)
                return Invalid(out error, "The candidate template button, graphic or label is invalid.");

            Text titleText = title.GetComponent<Text>();
            Text resourceText = resources.GetComponent<Text>();
            Text blockText = blockReason.GetComponent<Text>();
            Text messageText = message.GetComponent<Text>();
            Text reportText = report.GetComponent<Text>();
            Button openButton = open.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            if (titleText == null || resourceText == null || blockText == null ||
                messageText == null || reportText == null ||
                titleText.font == null || resourceText.font == null || blockText.font == null ||
                messageText.font == null || reportText.font == null || openButton == null ||
                closeButton == null ||
                !HasButtonVisual(openButton) || !HasButtonVisual(closeButton) ||
                !HasRaycastTarget(openButton) || !HasRaycastTarget(closeButton) ||
                panel.GetComponent<Image>() == null || !panel.GetComponent<Image>().raycastTarget)
                return Invalid(out error, "Industry text, buttons, background or raycast components are invalid.");
            if (message.gameObject.activeSelf)
                return Invalid(out error, "The industry message line must start hidden.");
            if (!titleText.text.Contains("产业"))
                return Invalid(out error, "The industry panel title must contain 产业.");

            IndustryPanel industryPanel = panels[0];
            if (!Bound(industryPanel, "gameSession", sessions[0]) ||
                !Bound(industryPanel, "panelRoot", panel.gameObject) ||
                !Bound(industryPanel, "openButton", openButton) ||
                !Bound(industryPanel, "closeButton", closeButton) ||
                !Bound(industryPanel, "titleText", titleText) ||
                !Bound(industryPanel, "resourceText", resourceText) ||
                !Bound(industryPanel, "blockReasonText", blockText) ||
                !Bound(industryPanel, "messageText", messageText) ||
                !Bound(industryPanel, "reportText", reportText) ||
                !Bound(industryPanel, "rowFarm", farm.gameObject) ||
                !Bound(industryPanel, "rowTradingHouse", trading.gameObject) ||
                !Bound(industryPanel, "farmStatusText", Require(scene, FarmStatusTextName).GetComponent<Text>()) ||
                !Bound(industryPanel, "farmAssigneeText", Require(scene, FarmAssigneeTextName).GetComponent<Text>()) ||
                !Bound(industryPanel, "farmUnassignButton", Require(scene, FarmUnassignButtonName).GetComponent<Button>()) ||
                !Bound(industryPanel, "farmCandidateContainer", farmContainer) ||
                !Bound(industryPanel, "tradingStatusText", Require(scene, TradingStatusTextName).GetComponent<Text>()) ||
                !Bound(industryPanel, "tradingAssigneeText", Require(scene, TradingAssigneeTextName).GetComponent<Text>()) ||
                !Bound(industryPanel, "tradingUnassignButton", Require(scene, TradingUnassignButtonName).GetComponent<Button>()) ||
                !Bound(industryPanel, "tradingCandidateContainer", tradingContainer) ||
                !Bound(industryPanel, "candidateTemplate", template.gameObject))
                return Invalid(out error, "IndustryPanel serialized references are not bound.");

            RectTransform rootRect = root as RectTransform;
            RectTransform panelRect = panel as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform closeRect = close as RectTransform;
            if (rootRect == null || panelRect == null || openRect == null || closeRect == null ||
                !Contained(panelRect, rootRect) || !Contained(openRect, rootRect) ||
                !Contained(closeRect, panelRect) || !Contained((RectTransform)farm, panelRect) ||
                !Contained((RectTransform)trading, panelRect) ||
                !Contained((RectTransform)title, panelRect) ||
                !Contained((RectTransform)resources, panelRect) ||
                !Contained((RectTransform)blockReason, panelRect) ||
                !Contained((RectTransform)message, panelRect) ||
                !Contained((RectTransform)report, panelRect) ||
                Overlaps((RectTransform)title, (RectTransform)resources, panelRect) ||
                Overlaps((RectTransform)resources, (RectTransform)blockReason, panelRect) ||
                Overlaps((RectTransform)blockReason, (RectTransform)message, panelRect) ||
                Overlaps((RectTransform)message, (RectTransform)farm, panelRect) ||
                Overlaps((RectTransform)blockReason, (RectTransform)farm, panelRect) ||
                Overlaps((RectTransform)farm, (RectTransform)trading, panelRect) ||
                Overlaps((RectTransform)trading, (RectTransform)report, panelRect) ||
                Overlaps((RectTransform)report, closeRect, panelRect))
                return Invalid(out error, "Industry controls overlap or lie outside their bounds.");

            Transform chronicleOpen = Require(scene, ChronicleOpenButtonName);
            Transform rosterOpen = Require(scene, RosterOpenButtonName);
            if (chronicleOpen != null && Overlaps(openRect, (RectTransform)chronicleOpen, rootRect))
                return Invalid(out error, "The industry open button overlaps the chronicle button.");
            if (rosterOpen != null && Overlaps(openRect, (RectTransform)rosterOpen, rootRect))
                return Invalid(out error, "The industry open button overlaps the roster button.");
            Transform successionHud = Require(scene, AddSC010SuccessionPanel.HudName);
            if (successionHud is RectTransform hudRect &&
                Overlaps(openRect, hudRect, rootRect))
                return Invalid(out error, "The industry open button overlaps the succession HUD.");

            return ValidateClockFeedback(scene, out error);
        }

        private static bool ValidateClockFeedback(Scene scene, out string error)
        {
            WorldClockPanel[] clocks = Components<WorldClockPanel>(scene);
            if (clocks.Length == 0)
            {
                error = null;
                return true;
            }
            if (clocks.Length != 1)
            {
                error = "Expected at most one WorldClockPanel for month-turn feedback.";
                return false;
            }

            WorldClockPanel clock = clocks[0];
            Transform feedback = Require(scene, ClockFeedbackName);
            Text text = feedback != null ? feedback.GetComponent<Text>() : null;
            if (feedback == null || text == null || text.font == null)
            {
                error = "The clock panel must contain one visible month-turn feedback text.";
                return false;
            }
            if (feedback.parent != clock.transform)
            {
                error = "The month-turn feedback text must be a direct child of the clock panel.";
                return false;
            }
            if (feedback.gameObject.activeSelf)
            {
                error = "The month-turn feedback text must start hidden.";
                return false;
            }
            if (!Bound(clock, "feedbackText", text))
            {
                error = "WorldClockPanel.feedbackText is not bound to the month-turn feedback text.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool ValidateCandidateArea(Transform container, Transform row, out string error)
        {
            RectTransform containerRect = container as RectTransform;
            RectTransform rowRect = row as RectTransform;
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (containerRect == null || rowRect == null || grid == null)
            {
                error = "Candidate container " + container.name + " must use a GridLayoutGroup.";
                return false;
            }
            if (grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                grid.constraintCount != CandidateColumns)
            {
                error = "Candidate container " + container.name + " must use exactly " +
                        CandidateColumns + " fixed columns.";
                return false;
            }
            if (grid.cellSize.x < CandidateCellWidth - 0.5f ||
                grid.cellSize.y < CandidateCellHeight - 0.5f ||
                grid.spacing.x < 0f || grid.spacing.y < 0f)
            {
                error = "Candidate container " + container.name + " has an unusable grid cell size or spacing.";
                return false;
            }

            int rows = (CandidateCapacity + CandidateColumns - 1) / CandidateColumns;
            float requiredWidth = grid.padding.left + grid.padding.right +
                                  CandidateColumns * grid.cellSize.x +
                                  (CandidateColumns - 1) * grid.spacing.x;
            float requiredHeight = grid.padding.top + grid.padding.bottom +
                                   rows * grid.cellSize.y + (rows - 1) * grid.spacing.y;
            if (containerRect.rect.width + 0.5f < requiredWidth ||
                containerRect.rect.height + 0.5f < requiredHeight)
            {
                error = "Candidate container " + container.name +
                        " is too small for six candidates.";
                return false;
            }
            if (!Contained(containerRect, rowRect))
            {
                error = "Candidate container " + container.name + " lies outside its industry row.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool ValidateRow(Scene scene, Transform row, string nameText, string statusText,
            string assigneeText, string unassignButton, string candidateContainer, out string error)
        {
            Transform name = Require(scene, nameText);
            Transform status = Require(scene, statusText);
            Transform assignee = Require(scene, assigneeText);
            Transform unassign = Require(scene, unassignButton);
            Transform container = Require(scene, candidateContainer);
            if (name == null || status == null || assignee == null || unassign == null || container == null)
            {
                error = "Industry row " + row.name + " is missing one of its controls.";
                return false;
            }
            if (name.parent != row || status.parent != row || assignee.parent != row ||
                unassign.parent != row || container.parent != row)
            {
                error = "Industry row " + row.name + " controls must be direct children.";
                return false;
            }
            if (name.GetComponent<Text>() == null || status.GetComponent<Text>() == null ||
                assignee.GetComponent<Text>() == null || unassign.GetComponent<Button>() == null ||
                !HasButtonVisual(unassign.GetComponent<Button>()) ||
                !HasRaycastTarget(unassign.GetComponent<Button>()))
            {
                error = "Industry row " + row.name + " text or button components are invalid.";
                return false;
            }
            error = null;
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
            if (canvas == null || session == null || Components<IndustryPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (HasCompleteIndustryHierarchy(scene))
                return Repair(scene, save, beforeValidation);

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, PanelName).Any() || Named(scene, TitleName).Any() ||
                Named(scene, ResourceName).Any() || Named(scene, BlockReasonName).Any() ||
                Named(scene, MessageName).Any() ||
                Named(scene, ReportName).Any() || Named(scene, CloseButtonName).Any() ||
                Named(scene, FarmRowName).Any() || Named(scene, TradingRowName).Any() ||
                Named(scene, CandidateTemplateName).Any() ||
                Components<IndustryPanel>(scene).Length != 0)
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

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "产业",
                    200f, 44f, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, new Vector2(200f, 44f));

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(760f, 560f);
                panelObject.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font, "产业与人事",
                    0f, 245f, 700f, 38f, 22, TextAnchor.MiddleLeft);
                Text resources = CreateText(scene, panelRect, ResourceName, font, "资金 30　物资 12　情报 0",
                    0f, 205f, 700f, 30f, 17, TextAnchor.MiddleLeft);
                Text block = CreateText(scene, panelRect, BlockReasonName, font, string.Empty,
                    0f, 172f, 700f, 24f, 15, TextAnchor.MiddleLeft);
                block.color = new Color(1f, 0.82f, 0.4f, 1f);
                block.gameObject.SetActive(false);
                Text message = CreateText(scene, panelRect, MessageName, font, string.Empty,
                    0f, 146f, 700f, 24f, 14, TextAnchor.MiddleLeft);
                message.color = new Color(0.65f, 0.86f, 1f, 1f);
                message.gameObject.SetActive(false);

                GameObject farmRow = CreateRow(scene, panelRect, FarmRowName, FarmRowY);
                CreateRowControls(scene, farmRow.transform, font, FarmNameTextName, FarmStatusTextName,
                    FarmAssigneeTextName, FarmUnassignButtonName, FarmCandidateContainerName,
                    "农庄（等级 1・1 个职位・3/4 工时）");

                GameObject tradingRow = CreateRow(scene, panelRect, TradingRowName, TradingRowY);
                CreateRowControls(scene, tradingRow.transform, font, TradingNameTextName,
                    TradingStatusTextName, TradingAssigneeTextName, TradingUnassignButtonName,
                    TradingCandidateContainerName, "商馆（等级 1・1 个职位・3/4 工时）");

                Text report = CreateText(scene, panelRect, ReportName, font, "本月尚未结算。",
                    0f, -190f, 700f, 32f, 15, TextAnchor.UpperLeft);

                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    170f, 38f, 16);
                RectTransform closeRect = close.GetComponent<RectTransform>();
                closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0.5f);
                closeRect.anchoredPosition = new Vector2(0f, -245f);

                Transform farmContainer = Require(scene, FarmCandidateContainerName);
                Transform tradingContainer = Require(scene, TradingCandidateContainerName);
                ConfigureCandidateArea(farmContainer);
                ConfigureCandidateArea(tradingContainer);
                GameObject template = Create(scene, farmContainer, CandidateTemplateName,
                    typeof(Image), typeof(Button), typeof(LayoutElement));
                RectTransform templateRect = template.GetComponent<RectTransform>();
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.sizeDelta = new Vector2(CandidateCellWidth, CandidateCellHeight);
                Image templateImage = template.GetComponent<Image>();
                templateImage.color = new Color(0.2f, 0.3f, 0.4f, 1f);
                Button templateButton = template.GetComponent<Button>();
                templateButton.targetGraphic = templateImage;
                LayoutElement templateLayout = template.GetComponent<LayoutElement>();
                templateLayout.preferredWidth = CandidateCellWidth;
                templateLayout.preferredHeight = CandidateCellHeight;
                Text templateText = CreateText(scene, templateRect, "Label", font, "姓名（剩余 4/4）",
                    0f, 0f, CandidateCellWidth - 16f, 24f, 13, TextAnchor.MiddleCenter);
                templateText.raycastTarget = false;
                template.SetActive(false);
                EnsureClockFeedback(scene, font);

                IndustryPanel panel = Undo.AddComponent<IndustryPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                Transform farmStatus = Require(scene, FarmStatusTextName);
                Transform farmAssignee = Require(scene, FarmAssigneeTextName);
                Transform farmUnassign = Require(scene, FarmUnassignButtonName);
                Transform tradingStatus = Require(scene, TradingStatusTextName);
                Transform tradingAssignee = Require(scene, TradingAssigneeTextName);
                Transform tradingUnassign = Require(scene, TradingUnassignButtonName);
                panel.BindReferences(session, panelObject, open, close, title, resources, block, message,
                    report, farmRow, farmStatus.GetComponent<Text>(), farmAssignee.GetComponent<Text>(),
                    farmUnassign.GetComponent<Button>(), farmContainer,
                    tradingRow, tradingStatus.GetComponent<Text>(), tradingAssignee.GetComponent<Text>(),
                    tradingUnassign.GetComponent<Button>(), tradingContainer, template, false);
                SetSerializedReferences(panel, session, panelObject, open, close, title, resources, block,
                    message, report, farmRow, farmStatus.GetComponent<Text>(),
                    farmAssignee.GetComponent<Text>(), farmUnassign.GetComponent<Button>(), farmContainer,
                    tradingRow, tradingStatus.GetComponent<Text>(), tradingAssignee.GetComponent<Text>(),
                    tradingUnassign.GetComponent<Button>(), tradingContainer, template);
                EditorUtility.SetDirty(panel);

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
                Debug.LogWarning($"SC-019 industry updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static bool HasCompleteIndustryHierarchy(Scene scene)
        {
            if (Components<IndustryPanel>(scene).Length != 1)
                return false;
            string[] required =
            {
                RootName, OpenButtonName, PanelName, TitleName, ResourceName, BlockReasonName,
                ReportName, CloseButtonName, FarmRowName, FarmNameTextName, FarmStatusTextName,
                FarmAssigneeTextName, FarmUnassignButtonName, FarmCandidateContainerName,
                TradingRowName, TradingNameTextName, TradingStatusTextName, TradingAssigneeTextName,
                TradingUnassignButtonName, TradingCandidateContainerName, CandidateTemplateName
            };
            foreach (string name in required)
                if (Named(scene, name).Count() != 1)
                    return false;
            Transform root = Require(scene, RootName);
            return root != null && root.parent != null &&
                   root.parent.name == CanvasName && root.parent.GetComponent<Canvas>() != null;
        }

        /// <summary>
        /// Transactional repair for an already complete SC-019 hierarchy. It changes
        /// only the industry panel, its candidate areas and the clock feedback line,
        /// all under one Undo group, and still validates before saving.
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
                Transform close = Require(scene, CloseButtonName);
                Transform title = Require(scene, TitleName);
                Transform resources = Require(scene, ResourceName);
                Transform block = Require(scene, BlockReasonName);
                Transform report = Require(scene, ReportName);
                Transform farm = Require(scene, FarmRowName);
                Transform trading = Require(scene, TradingRowName);
                Transform farmContainer = Require(scene, FarmCandidateContainerName);
                Transform tradingContainer = Require(scene, TradingCandidateContainerName);
                Transform template = Require(scene, CandidateTemplateName);
                if (root == null || panel == null || open == null || close == null || title == null ||
                    resources == null || block == null || report == null || farm == null ||
                    trading == null || farmContainer == null || tradingContainer == null ||
                    template == null)
                    throw new InvalidOperationException("Complete SC-019 hierarchy could not be resolved.");

                SetTopLeftRect(open, OpenButtonPosition, new Vector2(200f, 44f));
                SetRect(panel, Vector2.zero, new Vector2(760f, 560f));
                SetRect(title, new Vector2(0f, 245f), new Vector2(700f, 38f));
                SetRect(resources, new Vector2(0f, 205f), new Vector2(700f, 30f));
                SetRect(block, new Vector2(0f, 172f), new Vector2(700f, 24f));
                SetRect(report, new Vector2(0f, -190f), new Vector2(700f, 32f));
                SetRect(close, new Vector2(0f, -245f), new Vector2(170f, 38f));
                SetRect(farm, new Vector2(0f, FarmRowY), new Vector2(RowWidth, RowHeight));
                SetRect(trading, new Vector2(0f, TradingRowY), new Vector2(RowWidth, RowHeight));
                RepairRowControls(scene, FarmNameTextName, FarmStatusTextName, FarmAssigneeTextName,
                    FarmUnassignButtonName, FarmCandidateContainerName);
                RepairRowControls(scene, TradingNameTextName, TradingStatusTextName,
                    TradingAssigneeTextName, TradingUnassignButtonName, TradingCandidateContainerName);

                SetRect(template, Vector2.zero,
                    new Vector2(CandidateCellWidth, CandidateCellHeight));
                Text templateLabel = template.GetComponentInChildren<Text>(true);
                if (templateLabel != null)
                {
                    SetRect(templateLabel.transform, Vector2.zero,
                        new Vector2(CandidateCellWidth - 16f, 24f));
                    templateLabel.raycastTarget = false;
                }

                Transform message = Require(scene, MessageName);
                if (message == null)
                {
                    message = CreateText(scene, panel, MessageName, font, string.Empty,
                        0f, 146f, 700f, 24f, 14, TextAnchor.MiddleLeft).transform;
                }
                else
                {
                    SetRect(message, new Vector2(0f, 146f), new Vector2(700f, 24f));
                }
                Text messageText = message.GetComponent<Text>();
                if (messageText != null)
                {
                    messageText.color = new Color(0.65f, 0.86f, 1f, 1f);
                    messageText.raycastTarget = false;
                }
                message.gameObject.SetActive(false);

                EnsureClockFeedback(scene, font);

                IndustryPanel component = root.GetComponent<IndustryPanel>();
                if (component == null)
                    throw new InvalidOperationException("IndustryRoot no longer owns IndustryPanel.");
                Undo.RecordObject(component, UndoName);
                Transform farmStatus = Require(scene, FarmStatusTextName);
                Transform farmAssignee = Require(scene, FarmAssigneeTextName);
                Transform farmUnassign = Require(scene, FarmUnassignButtonName);
                Transform tradingStatus = Require(scene, TradingStatusTextName);
                Transform tradingAssignee = Require(scene, TradingAssigneeTextName);
                Transform tradingUnassign = Require(scene, TradingUnassignButtonName);
                GameSession session = Components<GameSession>(scene)[0];
                component.BindReferences(session, panel.gameObject, open.GetComponent<Button>(),
                    close.GetComponent<Button>(), title.GetComponent<Text>(),
                    resources.GetComponent<Text>(), block.GetComponent<Text>(), messageText,
                    report.GetComponent<Text>(), farm.gameObject,
                    farmStatus.GetComponent<Text>(), farmAssignee.GetComponent<Text>(),
                    farmUnassign.GetComponent<Button>(), farmContainer,
                    trading.gameObject, tradingStatus.GetComponent<Text>(),
                    tradingAssignee.GetComponent<Text>(), tradingUnassign.GetComponent<Button>(),
                    tradingContainer, template.gameObject, false);
                SetSerializedReferences(component, session, panel.gameObject,
                    open.GetComponent<Button>(), close.GetComponent<Button>(),
                    title.GetComponent<Text>(), resources.GetComponent<Text>(),
                    block.GetComponent<Text>(), messageText, report.GetComponent<Text>(),
                    farm.gameObject, farmStatus.GetComponent<Text>(), farmAssignee.GetComponent<Text>(),
                    farmUnassign.GetComponent<Button>(), farmContainer, trading.gameObject,
                    tradingStatus.GetComponent<Text>(), tradingAssignee.GetComponent<Text>(),
                    tradingUnassign.GetComponent<Button>(), tradingContainer, template.gameObject);
                EditorUtility.SetDirty(component);

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
                Debug.LogWarning($"SC-019 repair rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static void RepairRowControls(Scene scene, string nameText, string statusText,
            string assigneeText, string unassignButton, string candidateContainer)
        {
            SetRect(Require(scene, nameText), new Vector2(-180f, 56f), new Vector2(320f, 24f));
            SetRect(Require(scene, statusText), new Vector2(-170f, 32f), new Vector2(340f, 24f));
            SetRect(Require(scene, assigneeText), new Vector2(-170f, 8f), new Vector2(320f, 24f));
            SetRect(Require(scene, unassignButton), new Vector2(270f, 52f), new Vector2(120f, 32f));
            ConfigureCandidateArea(Require(scene, candidateContainer));
        }

        private static void SetRect(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// Top-left anchored controls stay inside narrow canvases; a centre-anchored
        /// fixed offset can leave the canvas as soon as the Game view is narrower
        /// than the design resolution.
        /// </summary>
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

        private static void EnsureClockFeedback(Scene scene, Font font)
        {
            WorldClockPanel[] clocks = Components<WorldClockPanel>(scene);
            if (clocks.Length != 1)
                return;
            WorldClockPanel clock = clocks[0];
            Transform existing = Require(scene, ClockFeedbackName);
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text == null)
                text = CreateText(scene, clock.transform, ClockFeedbackName, font, string.Empty,
                    0f, 62f, 520f, 24f, 14, TextAnchor.MiddleCenter);
            else
                SetRect(existing, new Vector2(0f, 62f), new Vector2(520f, 24f));

            text.color = new Color(1f, 0.85f, 0.45f, 1f);
            text.raycastTarget = false;
            text.gameObject.SetActive(false);

            Undo.RecordObject(clock, UndoName);
            clock.BindFeedback(text);
            var serialized = new SerializedObject(clock);
            SerializedProperty property = serialized.FindProperty("feedbackText");
            if (property != null)
            {
                property.objectReferenceValue = text;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(clock);
        }

        private static GameObject CreateRow(Scene scene, Transform panel, string name, float y)
        {
            GameObject row = Create(scene, panel, name, typeof(Image));
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(RowWidth, RowHeight);
            row.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.19f, 0.9f);
            return row;
        }

        private static void CreateRowControls(Scene scene, Transform row, Font font, string nameTextName,
            string statusTextName, string assigneeTextName, string unassignButtonName,
            string candidateContainerName, string nameValue)
        {
            CreateText(scene, row, nameTextName, font, nameValue,
                -180f, 56f, 320f, 24f, 16, TextAnchor.MiddleLeft);
            CreateText(scene, row, statusTextName, font, "空缺：无人负责，本月无产出",
                -170f, 32f, 340f, 24f, 14, TextAnchor.MiddleLeft);
            CreateText(scene, row, assigneeTextName, font, "负责人：空缺",
                -170f, 8f, 320f, 24f, 14, TextAnchor.MiddleLeft);
            Button unassign = CreateButton(scene, row, unassignButtonName, font, "解除",
                120f, 32f, 14);
            RectTransform unassignRect = unassign.GetComponent<RectTransform>();
            unassignRect.anchorMin = unassignRect.anchorMax = unassignRect.pivot = new Vector2(0.5f, 0.5f);
            unassignRect.anchoredPosition = new Vector2(270f, 52f);

            GameObject container = Create(scene, row, candidateContainerName, typeof(GridLayoutGroup));
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = containerRect.anchorMax = containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0f, -36f);
            containerRect.sizeDelta = new Vector2(CandidateAreaWidth, CandidateAreaHeight);
            ConfigureCandidateArea(container.transform);
        }

        private static void ConfigureCandidateArea(Transform container)
        {
            if (container == null)
                return;
            RectTransform rect = container as RectTransform;
            if (rect != null)
            {
                Undo.RecordObject(rect, UndoName);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -36f);
                rect.sizeDelta = new Vector2(CandidateAreaWidth, CandidateAreaHeight);
            }

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = Undo.AddComponent<GridLayoutGroup>(container.gameObject);
            else
                Undo.RecordObject(grid, UndoName);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.cellSize = new Vector2(CandidateCellWidth, CandidateCellHeight);
            grid.spacing = new Vector2(CandidateSpacing, CandidateSpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CandidateColumns;
        }

        private static void SetSerializedReferences(IndustryPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text title, Text resources, Text block,
            Text message, Text report, GameObject farmRow, Text farmStatus, Text farmAssignee,
            Button farmUnassign, Transform farmContainer, GameObject tradingRow, Text tradingStatus,
            Text tradingAssignee, Button tradingUnassign, Transform tradingContainer, GameObject template)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("resourceText").objectReferenceValue = resources;
            serialized.FindProperty("blockReasonText").objectReferenceValue = block;
            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.FindProperty("reportText").objectReferenceValue = report;
            serialized.FindProperty("rowFarm").objectReferenceValue = farmRow;
            serialized.FindProperty("farmStatusText").objectReferenceValue = farmStatus;
            serialized.FindProperty("farmAssigneeText").objectReferenceValue = farmAssignee;
            serialized.FindProperty("farmUnassignButton").objectReferenceValue = farmUnassign;
            serialized.FindProperty("farmCandidateContainer").objectReferenceValue = farmContainer;
            serialized.FindProperty("rowTradingHouse").objectReferenceValue = tradingRow;
            serialized.FindProperty("tradingStatusText").objectReferenceValue = tradingStatus;
            serialized.FindProperty("tradingAssigneeText").objectReferenceValue = tradingAssignee;
            serialized.FindProperty("tradingUnassignButton").objectReferenceValue = tradingUnassign;
            serialized.FindProperty("tradingCandidateContainer").objectReferenceValue = tradingContainer;
            serialized.FindProperty("candidateTemplate").objectReferenceValue = template;
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

        private static bool HasButtonVisual(Button button)
        {
            if (button == null || button.targetGraphic == null)
                return false;
            return button.GetComponentInChildren<Text>(true) != null;
        }

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
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
