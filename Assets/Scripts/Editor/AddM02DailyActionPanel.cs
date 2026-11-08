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
    /// Transactional FoundationScene updater for the M02 daily-action panel. Every
    /// generated object lives beneath one fixed root in a single Undo group; the
    /// existing scene is snapshotted, validation runs before saving, and any failure
    /// reverts the group without reloading the scene. The ordinary menu refuses a dirty
    /// scene; <see cref="IntegrateDirtyWorkspace"/> is the explicit, authorized entry for
    /// the current shared workspace whose dirty SC-024 integration must be preserved.
    /// </summary>
    public static class AddM02DailyActionPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "M02DailyActionRoot";
        public const string OpenButtonName = "M02DailyActionOpenButton";
        public const string PanelName = "M02DailyActionPanel";
        public const string TitleName = "M02DailyActionTitleText";
        public const string SummaryName = "M02DailyActionSummaryText";
        public const string ScrollName = "M02DailyActionScroll";
        public const string ContentName = "M02DailyActionContent";
        public const string RowTemplateName = "M02DailyActionRowTemplate";
        public const string EmptyName = "M02DailyActionEmptyText";
        public const string SelectedName = "M02DailyActionSelectedText";
        public const string DurationLabelName = "M02DailyActionDurationLabel";
        public const string DurationInputName = "M02DailyActionDurationInput";
        public const string RestButtonName = "M02DailyActionRestButton";
        public const string TrainingButtonName = "M02DailyActionTrainingButton";
        public const string WorkButtonName = "M02DailyActionWorkButton";
        public const string TravelKiyosuButtonName = "M02DailyActionTravelKiyosuButton";
        public const string TravelAtsutaButtonName = "M02DailyActionTravelAtsutaButton";
        public const string TravelNagoyaButtonName = "M02DailyActionTravelNagoyaButton";
        public const string InterruptButtonName = "M02DailyActionInterruptButton";
        public const string FeedbackName = "M02DailyActionFeedbackText";
        public const string CloseButtonName = "M02DailyActionCloseButton";
        public const string MenuPath = "Tools/Sengoku Chronicle/Add M02 Daily Action Panel";
        public const string DirtyMenuPath =
            "Tools/Sengoku Chronicle/Integrate M02 Daily Action Panel (dirty workspace)";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string IndustryRootName = "IndustryRoot";
        private const string EducationRootName = "SC020EducationRoot";
        private const string ResearchRootName = "SC021ResearchRoot";
        private const string FormationRootName = "SC022FamilyFormationRoot";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventRootName = "EventChoiceRoot";
        private const string UndoName = "Add M02 Daily Action Panel";

        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, -510f);
        private static readonly Vector2 OpenButtonSize = new Vector2(220f, 44f);
        private static readonly Vector2 PanelSize = new Vector2(980f, 700f);

        // CanvasScaler changes the canvas scale with the Game view, so an element
        // that sits exactly on the panel edge can map back a few 1e-5 units outside
        // it. Half a pixel is invisible and still rejects any real overflow.
        private const float ContainmentTolerance = 0.5f;

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, ExistingPartialControl, ValidationFailed,
            OriginalObjectsChanged, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty,
                EditorApplication.isPlayingOrWillChangePlaymode);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            Report(result, "M02 daily-action panel");
        }

        /// <summary>
        /// Explicit integration entry for the authorized dirty shared workspace. It does
        /// not relax any structural or preservation validation; it only skips the
        /// "scene must be clean" convenience check because the existing SC-024 WIP is the
        /// reason the scene is dirty and must remain in place.
        /// </summary>
        [MenuItem(DirtyMenuPath)]
        public static void IntegrateDirtyWorkspace()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Report(EditorApplication.isPlayingOrWillChangePlaymode
                    ? UpdateResult.EditorPlaying
                    : UpdateResult.SceneMismatch, "M02 daily-action panel");
                return;
            }
            Report(Execute(scene, true, null), "M02 daily-action panel (dirty workspace)");
        }

        private static void Report(UpdateResult result, string label)
        {
            if (result == UpdateResult.Success)
                Debug.Log($"{label} validated and saved into FoundationScene.");
            else
                Debug.LogError($"{label} updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty) =>
            CheckScenePreflight(path, dirty, false);

        public static UpdateResult CheckScenePreflight(string path, bool dirty, bool editorPlaying)
        {
            if (editorPlaying) return UpdateResult.EditorPlaying;
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam. It never saves, opens, or reloads the supplied scene.</summary>
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
            DailyActionPanel[] panels = Components<DailyActionPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            Transform[] opens = Named(scene, OpenButtonName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 ||
                roots.Length != 1 || opens.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, DailyActionPanel, root, and open button.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = opens[0];
            DailyActionPanel panel = panels[0];
            if (root.parent != canvas || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "M02DailyActionRoot must be an active direct child of Canvas.");
            if (panel.transform != root || panel.gameObject != root.gameObject)
                return Invalid(out error, "DailyActionPanel must live on M02DailyActionRoot.");
            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error,
                    "M02DailyActionOpenButton must be an active direct child of the root.");
            if (!HasButtonVisual(open.GetComponent<Button>()) ||
                !HasRaycastTarget(open.GetComponent<Button>()))
                return Invalid(out error, "The daily-action open button is invalid.");

            Transform panelRoot = Require(scene, PanelName);
            Transform title = Require(scene, TitleName);
            Transform summary = Require(scene, SummaryName);
            Transform scroll = Require(scene, ScrollName);
            Transform content = Require(scene, ContentName);
            Transform template = Require(scene, RowTemplateName);
            Transform empty = Require(scene, EmptyName);
            Transform selected = Require(scene, SelectedName);
            Transform durationLabel = Require(scene, DurationLabelName);
            Transform durationInputTransform = Require(scene, DurationInputName);
            Transform rest = Require(scene, RestButtonName);
            Transform training = Require(scene, TrainingButtonName);
            Transform work = Require(scene, WorkButtonName);
            Transform travelKiyosu = Require(scene, TravelKiyosuButtonName);
            Transform travelAtsuta = Require(scene, TravelAtsutaButtonName);
            Transform travelNagoya = Require(scene, TravelNagoyaButtonName);
            Transform interrupt = Require(scene, InterruptButtonName);
            Transform feedback = Require(scene, FeedbackName);
            Transform close = Require(scene, CloseButtonName);
            if (panelRoot == null || title == null || summary == null || scroll == null ||
                content == null || template == null || empty == null || selected == null ||
                durationLabel == null || durationInputTransform == null || rest == null ||
                training == null || work == null || travelKiyosu == null ||
                travelAtsuta == null || travelNagoya == null || interrupt == null ||
                feedback == null || close == null)
                return Invalid(out error,
                    "M02 daily-action hierarchy is incomplete or ambiguous.");

            if (panelRoot.parent != root || panelRoot.gameObject.activeSelf)
                return Invalid(out error,
                    "M02DailyActionPanel must be a hidden direct child of the root.");
            Transform[] panelChildren =
            {
                title, summary, scroll, selected, durationLabel, durationInputTransform,
                rest, training, work, travelKiyosu, travelAtsuta, travelNagoya, interrupt,
                feedback, close
            };
            foreach (Transform child in panelChildren)
                if (child.parent != panelRoot)
                    return Invalid(out error,
                        "All M02 daily-action controls must be direct children of the panel.");
            if (empty.parent != panelRoot)
                return Invalid(out error,
                    "The M02 empty-state label must be a direct child of the panel.");
            if (content.parent != scroll || template.parent != content ||
                template.gameObject.activeSelf)
                return Invalid(out error,
                    "The row template must be inactive inside the scroll content.");

            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            RectTransform contentRect = content as RectTransform;
            RectTransform scrollRectTransform = scroll as RectTransform;
            if (scrollRect == null || contentRect == null || scrollRectTransform == null ||
                scrollRect.content != contentRect || scrollRect.viewport != scrollRectTransform ||
                !scrollRect.vertical || scrollRect.horizontal)
                return Invalid(out error,
                    "M02DailyActionScroll must own a vertical ScrollRect over the content.");
            if (content.GetComponent<VerticalLayoutGroup>() == null ||
                content.GetComponent<ContentSizeFitter>() == null)
                return Invalid(out error,
                    "M02 scroll content needs a vertical layout and size fitter.");
            RectTransform templateRect = template as RectTransform;
            if (templateRect == null ||
                templateRect.rect.width > scrollRectTransform.rect.width)
                return Invalid(out error,
                    "The M02 roster row template must fit inside the left-column scroll area.");

            Text titleText = title.GetComponent<Text>();
            Text summaryText = summary.GetComponent<Text>();
            Text emptyText = empty.GetComponent<Text>();
            Text selectedText = selected.GetComponent<Text>();
            Text durationLabelText = durationLabel.GetComponent<Text>();
            Text feedbackText = feedback.GetComponent<Text>();
            InputField durationInput = durationInputTransform.GetComponent<InputField>();
            Button restButton = rest.GetComponent<Button>();
            Button trainingButton = training.GetComponent<Button>();
            Button workButton = work.GetComponent<Button>();
            Button travelKiyosuButton = travelKiyosu.GetComponent<Button>();
            Button travelAtsutaButton = travelAtsuta.GetComponent<Button>();
            Button travelNagoyaButton = travelNagoya.GetComponent<Button>();
            Button interruptButton = interrupt.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            if (titleText == null || summaryText == null || emptyText == null ||
                selectedText == null || durationLabelText == null || feedbackText == null ||
                durationInput == null || restButton == null || trainingButton == null ||
                workButton == null || travelKiyosuButton == null || travelAtsutaButton == null ||
                travelNagoyaButton == null || interruptButton == null || closeButton == null ||
                titleText.font == null || summaryText.font == null || emptyText.font == null ||
                selectedText.font == null || durationLabelText.font == null ||
                feedbackText.font == null || durationInput.textComponent == null ||
                durationInput.textComponent.font == null || durationInput.placeholder == null ||
                !HasButtonVisual(restButton) || !HasButtonVisual(trainingButton) ||
                !HasButtonVisual(workButton) || !HasButtonVisual(travelKiyosuButton) ||
                !HasButtonVisual(travelAtsutaButton) || !HasButtonVisual(travelNagoyaButton) ||
                !HasButtonVisual(interruptButton) || !HasButtonVisual(closeButton) ||
                !HasRaycastTarget(durationInput) || !HasRaycastTarget(restButton) ||
                !HasRaycastTarget(trainingButton) || !HasRaycastTarget(workButton) ||
                !HasRaycastTarget(travelKiyosuButton) ||
                !HasRaycastTarget(travelAtsutaButton) ||
                !HasRaycastTarget(travelNagoyaButton) || !HasRaycastTarget(interruptButton) ||
                !HasRaycastTarget(closeButton) || !HasRaycastTarget(durationInput) ||
                panelRoot.GetComponent<Image>() == null ||
                !panelRoot.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<Image>() == null || !scroll.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<RectMask2D>() == null)
                return Invalid(out error,
                    "M02 text, input, button, background, mask, or raycast components are invalid.");
            if (durationInput.contentType != InputField.ContentType.IntegerNumber ||
                durationInput.text != "1")
                return Invalid(out error,
                    "The M02 duration input must be an integer field starting at 1.");
            if (!titleText.text.Contains("每日行动"))
                return Invalid(out error, "The M02 title must contain 每日行动.");
            if (feedbackText.verticalOverflow != VerticalWrapMode.Truncate)
                return Invalid(out error,
                    "The M02 feedback text must truncate instead of overflowing its area.");
            if (!selectedText.text.Contains("人物档案"))
                return Invalid(out error,
                    "The selected-member area must start as the character profile block.");
            if (selectedText.alignment != TextAnchor.UpperLeft ||
                selectedText.verticalOverflow != VerticalWrapMode.Truncate ||
                !selectedText.supportRichText || selectedText.fontSize < 16 ||
                selectedText.lineSpacing < 1f)
                return Invalid(out error,
                    "The character profile text must be a wrapping, truncating rich-text block.");
            if (empty.gameObject.activeSelf)
                return Invalid(out error, "The empty-state label must start hidden for a live roster.");

            if (!Bound(panel, "gameSession", sessions[0]) ||
                !Bound(panel, "panelRoot", panelRoot.gameObject) ||                !Bound(panel, "openButton", open.GetComponent<Button>()) ||
                !Bound(panel, "closeButton", closeButton) ||
                !Bound(panel, "titleText", titleText) ||
                !Bound(panel, "summaryText", summaryText) ||
                !Bound(panel, "scrollRect", scrollRect) ||
                !Bound(panel, "content", content) ||
                !Bound(panel, "rowTemplate", template.gameObject) ||
                !Bound(panel, "emptyText", emptyText) ||
                !Bound(panel, "selectedText", selectedText) ||
                !Bound(panel, "durationInput", durationInput) ||
                !Bound(panel, "restButton", restButton) ||
                !Bound(panel, "trainingButton", trainingButton) ||
                !Bound(panel, "workButton", workButton) ||
                !Bound(panel, "travelKiyosuButton", travelKiyosuButton) ||
                !Bound(panel, "travelAtsutaButton", travelAtsutaButton) ||
                !Bound(panel, "travelNagoyaButton", travelNagoyaButton) ||
                !Bound(panel, "interruptButton", interruptButton) ||
                !Bound(panel, "feedbackText", feedbackText))
                return Invalid(out error,
                    "DailyActionPanel serialized references are not bound.");

            WorldClockPanel[] clocks = Components<WorldClockPanel>(scene);
            if (clocks.Length > 1)
                return Invalid(out error, "More than one WorldClockPanel is present.");
            if (clocks.Length == 1 && !Bound(clocks[0], "dailyActionPanel", panel))
                return Invalid(out error,
                    "WorldClockPanel.dailyActionPanel is not bound to the M02 panel.");

            RectTransform canvasRect = canvas as RectTransform;
            RectTransform panelRect = panelRoot as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform titleRect = title as RectTransform;
            RectTransform summaryRect = summary as RectTransform;
            RectTransform scrollAreaRect = scroll as RectTransform;
            RectTransform emptyRect = empty as RectTransform;
            RectTransform selectedRect = selected as RectTransform;
            RectTransform durationLabelRect = durationLabel as RectTransform;
            RectTransform durationInputRect = durationInputTransform as RectTransform;
            RectTransform restRect = rest as RectTransform;
            RectTransform trainingRect = training as RectTransform;
            RectTransform workRect = work as RectTransform;
            RectTransform travelKiyosuRect = travelKiyosu as RectTransform;
            RectTransform travelAtsutaRect = travelAtsuta as RectTransform;
            RectTransform travelNagoyaRect = travelNagoya as RectTransform;
            RectTransform interruptRect = interrupt as RectTransform;
            RectTransform feedbackRect = feedback as RectTransform;
            RectTransform closeRect = close as RectTransform;
            if (canvasRect == null || panelRect == null || openRect == null ||
                titleRect == null || summaryRect == null || scrollAreaRect == null ||
                emptyRect == null || selectedRect == null || durationLabelRect == null ||
                durationInputRect == null || restRect == null || trainingRect == null ||
                workRect == null || travelKiyosuRect == null || travelAtsutaRect == null ||
                travelNagoyaRect == null || interruptRect == null || feedbackRect == null ||
                closeRect == null ||
                // The root intentionally fills the Canvas, so containment is checked
                // against the Canvas for the actual controls rather than the root.
                !Contained(panelRect, canvasRect) || !Contained(openRect, canvasRect))
                return Invalid(out error,
                    "The M02 panel or open button lies outside the Canvas.");

            RectTransform[] allChildren =
            {
                titleRect, summaryRect, scrollAreaRect, emptyRect, selectedRect,
                durationLabelRect, durationInputRect, restRect, trainingRect, workRect,
                travelKiyosuRect, travelAtsutaRect, travelNagoyaRect, interruptRect,
                feedbackRect, closeRect
            };
            foreach (RectTransform child in allChildren)
                if (!Contained(child, panelRect))
                    return Invalid(out error,
                        "An M02 daily-action control lies outside the panel bounds.");

            if (selectedRect.rect.width < 360f || selectedRect.rect.height < 300f)
                return Invalid(out error,
                    "The character profile area must be a substantial right-column block.");
            float rosterRight = scrollAreaRect.anchoredPosition.x +
                                scrollAreaRect.rect.width * 0.5f;
            float profileLeft = selectedRect.anchoredPosition.x -
                                selectedRect.rect.width * 0.5f;
            if (rosterRight > profileLeft - 20f)
                return Invalid(out error,
                    "The roster and character profile must stay in separate columns.");

            if (Overlaps(titleRect, summaryRect, panelRect))
                return Invalid(out error, "M02 overlap: title/summary.");
            if (Overlaps(summaryRect, scrollAreaRect, panelRect))
                return Invalid(out error, "M02 overlap: summary/scroll.");
            if (Overlaps(summaryRect, selectedRect, panelRect))
                return Invalid(out error, "M02 overlap: summary/profile.");
            if (Overlaps(scrollAreaRect, selectedRect, panelRect))
                return Invalid(out error, "M02 overlap: scroll/profile.");
            if (Overlaps(selectedRect, durationLabelRect, panelRect))
                return Invalid(out error, "M02 overlap: selected/duration label.");
            if (Overlaps(selectedRect, durationInputRect, panelRect))
                return Invalid(out error, "M02 overlap: selected/duration input.");
            if (Overlaps(durationLabelRect, durationInputRect, panelRect))
                return Invalid(out error, "M02 overlap: duration label/input.");
            if (Overlaps(durationLabelRect, restRect, panelRect))
                return Invalid(out error, "M02 overlap: duration label/rest.");
            if (Overlaps(durationInputRect, restRect, panelRect))
                return Invalid(out error, "M02 overlap: duration input/rest.");
            if (Overlaps(restRect, trainingRect, panelRect))
                return Invalid(out error, "M02 overlap: rest/training.");
            if (Overlaps(trainingRect, workRect, panelRect))
                return Invalid(out error, "M02 overlap: training/work.");
            if (Overlaps(workRect, interruptRect, panelRect))
                return Invalid(out error, "M02 overlap: work/interrupt.");
            if (Overlaps(restRect, travelKiyosuRect, panelRect))
                return Invalid(out error, "M02 overlap: rest/travel Kiyosu.");
            if (Overlaps(trainingRect, travelAtsutaRect, panelRect))
                return Invalid(out error, "M02 overlap: training/travel Atsuta.");
            if (Overlaps(workRect, travelNagoyaRect, panelRect))
                return Invalid(out error, "M02 overlap: work/travel Nagoya.");
            if (Overlaps(interruptRect, travelNagoyaRect, panelRect))
                return Invalid(out error, "M02 overlap: interrupt/travel Nagoya.");
            if (Overlaps(travelKiyosuRect, travelAtsutaRect, panelRect))
                return Invalid(out error, "M02 overlap: travel Kiyosu/travel Atsuta.");
            if (Overlaps(travelAtsutaRect, travelNagoyaRect, panelRect))
                return Invalid(out error, "M02 overlap: travel Atsuta/travel Nagoya.");
            if (Overlaps(travelNagoyaRect, feedbackRect, panelRect))
                return Invalid(out error, "M02 overlap: travel Nagoya/feedback.");
            if (Overlaps(feedbackRect, closeRect, panelRect))
                return Invalid(out error, "M02 overlap: feedback/close.");

            if (!ContainedInRenderBand(canvas, root))
                return Invalid(out error,
                    "M02DailyActionRoot must render above the map HUD and below the title/event overlays.");
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save,
            Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            Transform canvas = canvases.Length == 1 ? canvases[0] : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            if (canvas == null || session == null || Components<DailyActionPanel>(scene).Length > 1 ||
                Components<WorldClockPanel>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            if (Named(scene, RootName).Any() || Named(scene, OpenButtonName).Any() ||
                Named(scene, PanelName).Any() || Named(scene, TitleName).Any() ||
                Named(scene, SummaryName).Any() || Named(scene, ScrollName).Any() ||
                Named(scene, ContentName).Any() || Named(scene, RowTemplateName).Any() ||
                Named(scene, EmptyName).Any() || Named(scene, SelectedName).Any() ||
                Named(scene, DurationInputName).Any() || Named(scene, RestButtonName).Any() ||
                Components<DailyActionPanel>(scene).Length != 0)
                return UpdateResult.ExistingPartialControl;

            ObjectSnapshot[] originals = Snapshot(scene);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "每日行动",
                    OpenButtonSize.x, OpenButtonSize.y, 16);
                SetTopLeftRect(open.transform, OpenButtonPosition, OpenButtonSize);

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot =
                    new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = PanelSize;
                Image background = panelObject.GetComponent<Image>();
                background.color = new Color(0.08f, 0.1f, 0.13f, 0.98f);
                background.raycastTarget = true;
                panelObject.SetActive(false);

                Text title = CreateText(scene, panelRect, TitleName, font,
                    "人物档案 · 每日行动", 0f, 322f, 920f, 34f, 22,
                    TextAnchor.MiddleLeft);
                title.color = new Color(0.93f, 0.88f, 0.78f, 1f);
                Text summary = CreateText(scene, panelRect, SummaryName, font,
                    "当前日期：—　｜　家族登记 0 人　｜　左侧选择人物，右侧安排行动",
                    0f, 290f, 920f, 26f, 13, TextAnchor.MiddleLeft);
                summary.color = new Color(0.65f, 0.71f, 0.78f, 1f);

                GameObject scrollObject = Create(scene, panelRect, ScrollName,
                    typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
                RectTransform scrollAreaRect = scrollObject.GetComponent<RectTransform>();
                scrollAreaRect.anchorMin = scrollAreaRect.anchorMax =
                    scrollAreaRect.pivot = new Vector2(0.5f, 0.5f);
                scrollAreaRect.anchoredPosition = new Vector2(-225f, 45f);
                scrollAreaRect.sizeDelta = new Vector2(510f, 430f);
                scrollObject.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.08f, 0.9f);
                ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.viewport = scrollAreaRect;

                GameObject contentObject = Create(scene, scrollAreaRect, ContentName,
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                ConfigureVerticalLayout(contentObject.GetComponent<VerticalLayoutGroup>(), 6);
                ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRect.content = contentRect;

                Button template = CreateButton(scene, contentRect, RowTemplateName, font,
                    "家族人物", 500f, 44f, 14);
                Text templateLabel = template.GetComponentInChildren<Text>(true);
                if (templateLabel != null)
                    templateLabel.alignment = TextAnchor.MiddleLeft;
                LayoutElement templateLayout = template.GetComponent<LayoutElement>();
                templateLayout.preferredWidth = 500f;
                templateLayout.preferredHeight = 44f;
                templateLayout.flexibleHeight = 0f;
                template.gameObject.SetActive(false);

                Text empty = CreateText(scene, panelRect, EmptyName, font,
                    "尚无已登记的家族人物。", -225f, 45f, 480f, 60f, 15,
                    TextAnchor.MiddleCenter);
                empty.color = new Color(0.7f, 0.75f, 0.82f, 1f);
                empty.gameObject.SetActive(false);

                // The selected member is the right-column profile focus. Its text is
                // rebuilt from the live roster and daily-action snapshot on every refresh.
                Text selectedText = CreateText(scene, panelRect, SelectedName, font,
                    "<color=#C4553B>人物档案</color>\n当前选择：未选择人物。",
                    270f, 55f, 400f, 370f, 16, TextAnchor.UpperLeft);
                selectedText.color = new Color(0.93f, 0.88f, 0.78f, 1f);
                selectedText.verticalOverflow = VerticalWrapMode.Truncate;
                selectedText.lineSpacing = 1.15f;

                Text durationLabel = CreateText(scene, panelRect, DurationLabelName, font,
                    "持续天数（≥1）", 125f, -166f, 170f, 28f, 14,
                    TextAnchor.MiddleLeft);
                InputField durationInput = CreateIntegerInput(scene, panelRect,
                    DurationInputName, font, "1", 295f, -166f, 110f, 36f);

                Button rest = CreateButton(scene, panelRect, RestButtonName, font, "休息",
                    180f, 40f, 15);
                SetCenteredRect(rest.transform, new Vector2(-300f, -206f), new Vector2(180f, 40f));
                Button training = CreateButton(scene, panelRect, TrainingButtonName, font,
                    "训练", 180f, 40f, 15);
                SetCenteredRect(training.transform, new Vector2(-100f, -206f),
                    new Vector2(180f, 40f));
                Button work = CreateButton(scene, panelRect, WorkButtonName, font, "工作",
                    180f, 40f, 15);
                SetCenteredRect(work.transform, new Vector2(100f, -206f), new Vector2(180f, 40f));
                Button interrupt = CreateButton(scene, panelRect, InterruptButtonName, font,
                    "中断当前行动", 240f, 40f, 15);
                SetCenteredRect(interrupt.transform, new Vector2(340f, -206f),
                    new Vector2(240f, 40f));

                Button travelKiyosu = CreateButton(scene, panelRect, TravelKiyosuButtonName,
                    font, "前往清洲", 220f, 38f, 15);
                SetCenteredRect(travelKiyosu.transform, new Vector2(-260f, -247f),
                    new Vector2(220f, 38f));
                Button travelAtsuta = CreateButton(scene, panelRect, TravelAtsutaButtonName,
                    font, "前往热田", 220f, 38f, 15);
                SetCenteredRect(travelAtsuta.transform, new Vector2(0f, -247f),
                    new Vector2(220f, 38f));
                Button travelNagoya = CreateButton(scene, panelRect, TravelNagoyaButtonName,
                    font, "前往那古野", 220f, 38f, 15);
                SetCenteredRect(travelNagoya.transform, new Vector2(260f, -247f),
                    new Vector2(220f, 38f));

                // Three lines of feedback fit here; anything longer is clipped
                // instead of spilling over the Close button or panel edge.
                Text feedback = CreateText(scene, panelRect, FeedbackName, font, string.Empty,
                    0f, -292f, 920f, 48f, 14, TextAnchor.MiddleLeft);
                feedback.color = new Color(0.65f, 0.86f, 1f, 1f);
                feedback.verticalOverflow = VerticalWrapMode.Truncate;
                feedback.gameObject.SetActive(false);
                Button close = CreateButton(scene, panelRect, CloseButtonName, font, "关闭",
                    160f, 30f, 15);
                SetCenteredRect(close.transform, new Vector2(0f, -333f), new Vector2(160f, 30f));

                PlaceAboveMapHud(canvas, rootObject.transform);

                DailyActionPanel panel = Undo.AddComponent<DailyActionPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                panel.BindReferences(session, panelObject, open, close, title, summary,
                    scrollRect, contentRect, template.gameObject, empty, selectedText,
                    durationInput, rest, training, work, travelKiyosu, travelAtsuta,
                    travelNagoya, interrupt, feedback);
                SetSerializedReferences(panel, session, panelObject, open, close, title,
                    summary, scrollRect, contentRect, template.gameObject, empty, selectedText,
                    durationInput, rest, training, work, travelKiyosu, travelAtsuta,
                    travelNagoya, interrupt, feedback);
                EditorUtility.SetDirty(panel);

                WorldClockPanel[] clocks = Components<WorldClockPanel>(scene);
                if (clocks.Length == 1)
                {
                    Undo.RecordObject(clocks[0], UndoName);
                    clocks[0].BindDailyActionPanel(panel);
                    var clockBindings = new SerializedObject(clocks[0]);
                    SerializedProperty clockPanel = clockBindings.FindProperty("dailyActionPanel");
                    if (clockPanel != null)
                    {
                        clockPanel.objectReferenceValue = panel;
                        clockBindings.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(clocks[0]);
                }

                beforeValidation?.Invoke(rootObject);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);

                failure = UpdateResult.OriginalObjectsChanged;
                if (!Preserved(scene, originals))
                    throw new InvalidOperationException(
                        "An original scene object was removed or changed.");

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
                Debug.LogWarning($"M02 daily-action updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        private static InputField CreateIntegerInput(Scene scene, Transform parent, string name,
            Font font, string value, float x, float y, float width, float height)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(InputField));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.2f, 1f);
            image.raycastTarget = true;
            InputField input = go.GetComponent<InputField>();
            input.targetGraphic = image;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 3;

            Text text = CreateText(scene, rect, "Text", font, value, 0f, 0f,
                width - 14f, height - 8f, 15, TextAnchor.MiddleCenter);
            text.supportRichText = false;
            text.raycastTarget = false;
            input.textComponent = text;
            Text placeholder = CreateText(scene, rect, "Placeholder", font, "天数",
                0f, 0f, width - 14f, height - 8f, 14, TextAnchor.MiddleCenter);
            placeholder.color = new Color(0.6f, 0.65f, 0.7f, 1f);
            placeholder.raycastTarget = false;
            input.placeholder = placeholder;
            input.text = value;
            return input;
        }

        private static void ConfigureVerticalLayout(VerticalLayoutGroup layout, int spacing)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);
        }

        private static bool ContainedInRenderBand(Transform canvas, Transform target)
        {
            int index = target.GetSiblingIndex();
            string[] above =
            {
                MapRootName, IndustryRootName, EducationRootName, ResearchRootName,
                FormationRootName
            };
            string[] below = { TitleRootName, EventRootName };
            foreach (string name in above)
            {
                Transform other = canvas.Find(name);
                if (other != null && index <= other.GetSiblingIndex())
                    return false;
            }
            foreach (string name in below)
            {
                Transform other = canvas.Find(name);
                if (other != null && index >= other.GetSiblingIndex())
                    return false;
            }
            return true;
        }

        private static void PlaceAboveMapHud(Transform canvas, Transform target)
        {
            Transform title = canvas.Find(TitleRootName);
            Transform eventRoot = canvas.Find(EventRootName);
            int? anchorIndex = null;
            if (title != null)
                anchorIndex = title.GetSiblingIndex();
            if (eventRoot != null)
                anchorIndex = anchorIndex.HasValue
                    ? Mathf.Min(anchorIndex.Value, eventRoot.GetSiblingIndex())
                    : eventRoot.GetSiblingIndex();
            if (anchorIndex.HasValue)
                target.SetSiblingIndex(anchorIndex.Value);
            else
                target.SetAsLastSibling();
        }

        private static bool Preserved(Scene scene, ObjectSnapshot[] originals)
        {
            foreach (ObjectSnapshot original in originals)
            {
                Transform transform = original.Transform;
                if (transform == null)
                    return false;
                if (!string.Equals(transform.name, original.Name, StringComparison.Ordinal))
                    return false;
                if (transform.gameObject.activeSelf != original.Active)
                    return false;
                if (transform.parent != original.Parent)
                    return false;
            }
            return true;
        }

        private static ObjectSnapshot[] Snapshot(Scene scene)
        {
            Transform[] all = AllTransforms(scene);
            var snapshots = new ObjectSnapshot[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                snapshots[i] = new ObjectSnapshot
                {
                    Transform = transform,
                    Name = transform.name,
                    Parent = transform.parent,
                    Active = transform.gameObject.activeSelf
                };
            }
            return snapshots;
        }

        private struct ObjectSnapshot
        {
            public Transform Transform;
            public string Name;
            public Transform Parent;
            public bool Active;
        }

        private static void SetSerializedReferences(DailyActionPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text title, Text summary,
            ScrollRect scroll, Transform content, GameObject template, Text empty,
            Text selected, InputField duration, Button rest, Button training, Button work,
            Button travelKiyosu, Button travelAtsuta, Button travelNagoya, Button interrupt,
            Text feedback)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("gameSession").objectReferenceValue = session;
            serialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            serialized.FindProperty("openButton").objectReferenceValue = open;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("summaryText").objectReferenceValue = summary;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.FindProperty("content").objectReferenceValue = content;
            serialized.FindProperty("rowTemplate").objectReferenceValue = template;
            serialized.FindProperty("emptyText").objectReferenceValue = empty;
            serialized.FindProperty("selectedText").objectReferenceValue = selected;
            serialized.FindProperty("durationInput").objectReferenceValue = duration;
            serialized.FindProperty("restButton").objectReferenceValue = rest;
            serialized.FindProperty("trainingButton").objectReferenceValue = training;
            serialized.FindProperty("workButton").objectReferenceValue = work;
            serialized.FindProperty("travelKiyosuButton").objectReferenceValue = travelKiyosu;
            serialized.FindProperty("travelAtsutaButton").objectReferenceValue = travelAtsuta;
            serialized.FindProperty("travelNagoyaButton").objectReferenceValue = travelNagoya;
            serialized.FindProperty("interruptButton").objectReferenceValue = interrupt;
            serialized.FindProperty("feedbackText").objectReferenceValue = feedback;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            image.raycastTarget = true;
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

        private static bool HasButtonVisual(Button button) =>
            button != null && button.targetGraphic != null &&
            button.GetComponentInChildren<Text>(true) != null;

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static bool HasRaycastTarget(InputField input) =>
            input != null && input.targetGraphic != null && input.targetGraphic.raycastTarget;

        private static bool Bound(UnityEngine.Object target, string field,
            UnityEngine.Object expected)
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
            AllTransforms(scene).Where(transform => transform.name == name);

        private static Transform[] AllTransforms(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

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
                return point.x >= bounds.xMin - ContainmentTolerance &&
                       point.x <= bounds.xMax + ContainmentTolerance &&
                       point.y >= bounds.yMin - ContainmentTolerance &&
                       point.y <= bounds.yMax + ContainmentTolerance;
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
