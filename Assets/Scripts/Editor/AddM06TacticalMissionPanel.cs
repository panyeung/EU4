using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Transactional generator for the M06 tactical infiltration page. The town menu keeps
    /// one generated root that carries the entry button and the panel component, while the
    /// mission page itself lives in one hidden canvas-level overlay: a darkened backdrop
    /// plus a centered parchment sheet. That is what makes the page read as its own opaque
    /// surface above the town band instead of a compressed strip sharing the band with the
    /// town overview. The 8x6 map grid, operative selection list and command strip are all
    /// built in a single Undo group, validated before saving, and rolled back on failure.
    /// It never reloads the scene and never rebuilds unknown existing UI.
    /// </summary>
    public static class AddM06TacticalMissionPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = TacticalMissionPanel.RootObjectName;
        public const string OverlayName = TacticalMissionPanel.OverlayObjectName;
        public const string BackdropName = "M6TacticalBackdrop";
        public const string PanelName = TacticalMissionPanel.PageObjectName;
        public const string OpenButtonName = "M6TacticalOpenButton";
        public const string CloseButtonName = "M6TacticalCloseButton";
        public const string StartButtonName = "M6TacticalStartButton";
        public const string NorthButtonName = "M6TacticalNorthButton";
        public const string EastButtonName = "M6TacticalEastButton";
        public const string SouthButtonName = "M6TacticalSouthButton";
        public const string WestButtonName = "M6TacticalWestButton";
        public const string MoveButtonName = "M6TacticalMoveButton";
        public const string SprintButtonName = "M6TacticalSprintButton";
        public const string WaitButtonName = "M6TacticalWaitButton";
        public const string InteractButtonName = "M6TacticalInteractButton";
        public const string ExtractButtonName = "M6TacticalExtractButton";
        public const string WithdrawButtonName = "M6TacticalWithdrawButton";
        public const string SwitchButtonName = "M6TacticalSwitchButton";
        public const string TitleTextName = "M6TacticalTitleText";
        public const string StateTextName = "M6TacticalStateText";
        public const string DateTextName = "M6TacticalDateText";
        public const string ResourceTextName = "M6TacticalResourceText";
        public const string ObjectiveTextName = "M6TacticalObjectiveText";
        public const string TeamTextName = "M6TacticalTeamText";
        public const string StatusTextName = "M6TacticalStatusText";
        public const string GuardTextName = "M6TacticalGuardText";
        public const string ResultTextName = "M6TacticalResultText";
        public const string EventTextName = "M6TacticalEventText";
        public const string MessageTextName = "M6TacticalMessageText";
        public const string ChronicleTextName = "M6TacticalChronicleText";
        public const string SelectionTextName = "M6TacticalSelectionText";
        public const string GridName = "M6TacticalGrid";
        public const string CellNamePrefix = "M6TacticalCell_";
        public const string HeaderBandName = "M6TacticalHeaderBand";
        public const string HeaderRuleName = "M6TacticalHeaderRule";
        public const string LegendTextName = "M6TacticalLegendText";
        public const string CandidateScrollName = "M6TacticalCandidateScroll";
        public const string CandidateViewportName = "M6TacticalCandidateViewport";
        public const string CandidateContentName = "M6TacticalCandidateContent";
        public const string CandidateTemplateName = "M6TacticalCandidateTemplate";

        private const string MenuPath = "Tools/Sengoku Chronicle/Add M06 Tactical Mission Panel";
        private const string UndoName = "Add M06 Tactical Mission Panel";
        private const string CellSizeText = "58x58";

        // The page is a centered overlay on the 1280x720 canvas, so it no longer shares the
        // 268-unit town band with the town overview. 1120x620 leaves 80 units either side and
        // 50 above and below at the reference size, which keeps the sheet opaque and clear of
        // the band edges at 1280x720, 1600x900 and 1920x1080 (all three resolve to the same
        // 1280x720 canvas rect through the 16:9 CanvasScaler). Other aspects resolve to a
        // narrower or shorter canvas rect, so TacticalMissionPanel.FitPageToParent scales this
        // authored sheet into that rect instead of clipping it.
        private static readonly Vector2 PanelSize = new Vector2(1120f, 620f);
        private static readonly Vector2 CenteredAnchor = new Vector2(0.5f, 0.5f);
        // The saved M05 commission entry owns the top-right rect (-24,-16) at 220x40 and
        // the town action row sits at y 42..102. This rect stays clear of both without
        // moving any saved M05 object.
        private static readonly Vector2 OpenPosition = new Vector2(-24f, -100f);
        private static readonly Vector2 OpenSize = new Vector2(220f, 40f);
        private static readonly Vector2 CellSize = new Vector2(58f, 58f);
        // The grid is layout-driven, so the spacing only has to keep the 8x6 block inside
        // the left column of the sheet.
        private static readonly Vector2 CellSpacing = new Vector2(2f, 2f);

        // The sheet is read top to bottom: a 48-unit indigo band for the title and the two
        // header buttons, a gold rule, one 20-unit legend line on paper, then the body. The
        // legend left the band because Ink on parchment measures 10.67:1 while pale type on
        // the band barely cleared 4.5:1, and the band is what the capture called a low
        // contrast strip. The objective block holds the authored briefing: one short line
        // per rule (goal, mode, cost, each result band, the disclosed death risk, scouting
        // and the save lockout), measured so every line stays inside the body column. Ten
        // one-line rows follow at a 22-unit pitch in the right column, with the 8x6 grid
        // filling the left column and the command strip along the bottom edge.
        private const float HeaderBandHeight = 48f;
        private const float HeaderRuleHeight = 2f;
        private const float LegendCenterY = 248f;
        private const float LegendWidth = 1040f;
        private const float BodyTopY = 232f;
        private const float BodyCenterX = 260f;
        private const float BodyWidth = 560f;
        private const float RowHeight = 20f;
        private const float RowPitch = 22f;
        // Eleven briefing lines need ~154 units at 14 px with the serif face; 200 keeps a
        // wide margin for the scouting line and the disclosed names without pushing the
        // last body row or the operative list into the command strip.
        private const float ObjectiveHeight = 200f;
        private const int TitleFontSize = 26;
        // One readable size for every label on the sheet: body rows, cells, buttons and
        // the operative list. 14 px is the reference-size floor for this page.
        private const int BodyFontSize = 14;
        private const string Legend = "图例　壁=墙　掩=掩体　撤=撤离　报=密报　忍=潜入者　守=守卫　目=视野";

        private static readonly Vector2 GridPosition = new Vector2(-300f, 53f);
        private static readonly Vector2 CandidateScrollSize = new Vector2(560f, 104f);
        // The operative list is visible only while briefing mode hides the operation rows,
        // so it sits under the last briefing row and above the command strip.
        private static readonly Vector2 CandidateScrollPosition = new Vector2(260f, -110f);
        private static readonly Vector2 TitlePosition = new Vector2(-290f, 286f);
        private static readonly Vector2 TitleSize = new Vector2(500f, 34f);
        private static readonly Vector2 StartPosition = new Vector2(360f, 286f);
        private static readonly Vector2 StartSize = new Vector2(132f, 34f);
        private static readonly Vector2 ClosePosition = new Vector2(480f, 286f);
        private static readonly Vector2 CloseSize = new Vector2(88f, 34f);
        private static readonly Vector2 CommandSize = new Vector2(92f, 30f);
        private const float CommandY = -270f;
        private const float CommandStartX = -490f;
        private const float CommandStep = 98f;
        // One label per body row; the briefing hint rides the party row because the panel
        // never shows the two together.
        private static readonly string[][] BodyRowNames =
        {
            new[] { ObjectiveTextName },
            new[] { StateTextName },
            new[] { DateTextName },
            new[] { ResourceTextName },
            new[] { TeamTextName, SelectionTextName },
            new[] { StatusTextName },
            new[] { GuardTextName },
            new[] { ResultTextName },
            new[] { EventTextName },
            new[] { ChronicleTextName },
            new[] { MessageTextName }
        };

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene, MissingOrDuplicateObjects,
            InvalidHierarchyOrBindings, ExistingTacticalPanel, InsufficientSpace,
            ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.EditorPlaying
                : CheckScenePreflight(scene.path, scene.isDirty);
            bool created = false;
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null, out created);
            if (result != UpdateResult.Success)
            {
                Debug.LogError($"M06 tactical panel updater refused or rolled back: {result}.");
                if (result == UpdateResult.ExistingTacticalPanel)
                    Debug.LogError("M06 refusal detail: roots=" + Named(scene, RootName).Length +
                                   ", panels=" + Components<TacticalMissionPanel>(scene).Length +
                                   ". A complete generated page is rebuilt; any other " +
                                   "hierarchy is left untouched.");
            }
            else if (created)
                Debug.Log("M06 tactical panel validated; the generated page was saved to FoundationScene.");
            else
                Debug.Log("M06 tactical panel is already present and valid; the scene was not modified.");
        }

        /// <summary>Shared menu guard, exposed so tests need not save or replace FoundationScene.</summary>
        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam for isolated scenes. Never saves.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null, out _);

        /// <summary>Fault injection runs inside the Undo transaction, immediately before validation.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation, out _);

        private static UpdateResult Execute(Scene scene, bool save,
            Action<GameObject> beforeValidation, out bool created)
        {
            created = false;
            UpdateResult preflight = Resolve(scene, out Transform canvas, out Transform town,
                out WorldMapController controller, out GameSession session);
            if (preflight != UpdateResult.Success)
                return preflight;

            // An existing page is either current (a no-op), a page this generator wrote in
            // an older revision (rebuilt in one Undo group), or somebody else's UI (refused).
            TacticalMissionPanel replaceable = null;
            if (Named(scene, RootName).Length != 0 ||
                Components<TacticalMissionPanel>(scene).Length != 0)
            {
                if (Validate(scene, out _))
                    return UpdateResult.Success;
                if (!IsOutdatedGeneratedPage(scene, out replaceable))
                    return UpdateResult.ExistingTacticalPanel;
            }

            SerializedProperty controllerField =
                new SerializedObject(controller).FindProperty("tacticalMissionPanel");
            if (controllerField == null)
                return UpdateResult.InvalidHierarchyOrBindings;
            // Only a reference that resolves to a live component proves another page owns the
            // slot. A binding left behind by a deleted root reads null here while the YAML
            // can still hold a stale entity id, and refusing on that id is what made the
            // supported delete-then-rebuild path answer ExistingTacticalPanel with no object
            // in the scene. The rebuild below overwrites the binding, so a stale id or a
            // reference to the page being replaced is repaired, not honoured.
            var boundPanel = controllerField.objectReferenceValue as TacticalMissionPanel;
            if (boundPanel != null && boundPanel != replaceable)
                return UpdateResult.ExistingTacticalPanel;

            RectTransform townRect = town as RectTransform;
            if (townRect.rect.width < 1200f || townRect.rect.height < 260f)
                return UpdateResult.InsufficientSpace;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                if (replaceable != null)
                {
                    // Same Undo group as the rebuild: a failed validation restores the old
                    // page instead of leaving the town without a tactical entry. An older
                    // revision kept the page under the root; the current one keeps it in the
                    // canvas-level overlay, so that overlay is destroyed with its page.
                    Undo.RecordObject(controller, UndoName);
                    controller.BindTacticalMissionPanel(null);
                    EditorUtility.SetDirty(controller);
                    DestroyStalePageObjects(replaceable);
                    Undo.DestroyObjectImmediate(replaceable.gameObject);
                }

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                    throw new InvalidOperationException("Missing built-in UI font.");

                // The town keeps only the entry button and the panel component. The page is
                // staged in a canvas-level overlay so it renders above the town band as a
                // modal sheet instead of sharing the band with the town overview. The overlay
                // is the last canvas child, which is what puts it on top of the town view.
                GameObject root = Create(scene, canvas, RootName);
                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);
                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "潜入密令",
                    OpenSize, 16);
                RectTransform openRect = open.GetComponent<RectTransform>();
                openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(1f, 1f);
                openRect.anchoredPosition = OpenPosition;

                GameObject overlayObject = Create(scene, canvas, OverlayName);
                RectTransform overlay = overlayObject.GetComponent<RectTransform>();
                Stretch(overlay);
                overlayObject.transform.SetAsLastSibling();
                overlayObject.SetActive(true);

                GameObject backdropObject = Create(scene, overlay, BackdropName, typeof(Image));
                RectTransform backdrop = backdropObject.GetComponent<RectTransform>();
                Stretch(backdrop);
                Image backdropImage = backdropObject.GetComponent<Image>();
                // A near-opaque scrim: the sheet must read as its own surface, and the scrim
                // also swallows clicks aimed at the town band underneath.
                backdropImage.color = new Color(SengokuUiTheme.IndigoDeep.r,
                    SengokuUiTheme.IndigoDeep.g, SengokuUiTheme.IndigoDeep.b, 0.9f);
                backdropImage.raycastTarget = true;

                GameObject pageObject = Create(scene, overlay, PanelName, typeof(Image));
                RectTransform page = pageObject.GetComponent<RectTransform>();
                page.anchorMin = page.anchorMax = page.pivot = CenteredAnchor;
                page.anchoredPosition = Vector2.zero;
                page.sizeDelta = PanelSize;
                pageObject.GetComponent<Image>().color = SengokuUiTheme.Parchment;

                // Opaque header band: the gold title and the two header buttons need a dark
                // surface, and a paper sheet plus one indigo band replaces the full navy
                // slab the earlier capture showed behind every label.
                GameObject headerObject = Create(scene, page, HeaderBandName, typeof(Image));
                RectTransform headerRect = headerObject.GetComponent<RectTransform>();
                headerRect.anchorMin = new Vector2(0f, 1f);
                headerRect.anchorMax = new Vector2(1f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = Vector2.zero;
                headerRect.sizeDelta = new Vector2(0f, HeaderBandHeight);
                Image headerImage = headerObject.GetComponent<Image>();
                headerImage.color = SengokuUiTheme.IndigoDeep;
                headerImage.raycastTarget = false;

                GameObject ruleObject = Create(scene, page, HeaderRuleName, typeof(Image));
                RectTransform ruleRect = ruleObject.GetComponent<RectTransform>();
                ruleRect.anchorMin = new Vector2(0f, 1f);
                ruleRect.anchorMax = new Vector2(1f, 1f);
                ruleRect.pivot = new Vector2(0.5f, 1f);
                ruleRect.anchoredPosition = new Vector2(0f, -HeaderBandHeight);
                ruleRect.sizeDelta = new Vector2(0f, HeaderRuleHeight);
                Image ruleImage = ruleObject.GetComponent<Image>();
                ruleImage.color = SengokuUiTheme.Gold;
                ruleImage.raycastTarget = false;

                // The map carries its terrain and sight meanings as glyphs as well as
                // colours, so one paper line states what each glyph means. Ink on parchment
                // is the readable pairing; pale type on the band measured far below it.
                CreateText(scene, page, LegendTextName, font, BodyFontSize,
                    new Vector2(0f, LegendCenterY), new Vector2(LegendWidth, RowHeight),
                    TextAnchor.MiddleLeft, SengokuUiTheme.Ink, Legend);

                // The scrim blocks the town band, so this button is the reachable way out of
                // the modal: it closes the sheet and drops the player back on the town
                // overview, where ReturnToMapButton is clickable again. The label says so
                // instead of a bare "close".
                Button close = CreateButton(scene, page, CloseButtonName, font, "返回城中",
                    CloseSize, BodyFontSize);
                RectTransform closeRect = close.GetComponent<RectTransform>();
                closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0.5f);
                closeRect.anchoredPosition = ClosePosition;

                Button start = CreateButton(scene, page, StartButtonName, font, "开始潜入",
                    StartSize, BodyFontSize);
                RectTransform startRect = start.GetComponent<RectTransform>();
                startRect.anchorMin = startRect.anchorMax = startRect.pivot =
                    new Vector2(0.5f, 0.5f);
                startRect.anchoredPosition = StartPosition;

                Text title = CreateText(scene, page, TitleTextName, font, TitleFontSize,
                    TitlePosition, TitleSize, TextAnchor.MiddleLeft,
                    SengokuUiTheme.Gold, "潜入密令：夺取密报");
                Text objective = CreateBodyText(scene, page, ObjectiveTextName, font, 0,
                    ObjectiveHeight, TextAnchor.UpperLeft, SengokuUiTheme.Ink);
                Text state = CreateBodyText(scene, page, StateTextName, font, 1,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text date = CreateBodyText(scene, page, DateTextName, font, 2,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text resources = CreateBodyText(scene, page, ResourceTextName, font, 3,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                // The briefing shows the selection hint and the operation shows the party,
                // never both, so they share one row instead of costing two.
                Text team = CreateBodyText(scene, page, TeamTextName, font, 4,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text selection = CreateBodyText(scene, page, SelectionTextName, font, 4,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text status = CreateBodyText(scene, page, StatusTextName, font, 5,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text guard = CreateBodyText(scene, page, GuardTextName, font, 6,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text result = CreateBodyText(scene, page, ResultTextName, font, 7,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text eventLine = CreateBodyText(scene, page, EventTextName, font, 8,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text chronicle = CreateBodyText(scene, page, ChronicleTextName, font, 9,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);
                Text message = CreateBodyText(scene, page, MessageTextName, font, 10,
                    RowHeight, TextAnchor.MiddleLeft, SengokuUiTheme.Ink);

                GameObject gridObject = Create(scene, page, GridName, typeof(GridLayoutGroup));
                RectTransform grid = gridObject.GetComponent<RectTransform>();
                grid.anchorMin = grid.anchorMax = grid.pivot = new Vector2(0.5f, 0.5f);
                grid.anchoredPosition = GridPosition;
                grid.sizeDelta = GridSize();
                GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>();
                layout.cellSize = CellSize;
                layout.spacing = CellSpacing;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = TacticalMissionScenario.Width;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.padding = new RectOffset(0, 0, 0, 0);

                var cellImages =
                    new Image[TacticalMissionScenario.Width * TacticalMissionScenario.Height];
                var cellLabels =
                    new Text[TacticalMissionScenario.Width * TacticalMissionScenario.Height];
                for (int y = 0; y < TacticalMissionScenario.Height; y++)
                {
                    for (int x = 0; x < TacticalMissionScenario.Width; x++)
                    {
                        int index = y * TacticalMissionScenario.Width + x;
                        GameObject cell = Create(scene, grid, CellNamePrefix + x + "_" + y,
                            typeof(Image));
                        RectTransform cellRect = cell.GetComponent<RectTransform>();
                        // The cell grid is layout-driven, but it must also be valid before
                        // the first layout pass. A real scene whose town root sits far from
                        // the world origin made the reparented cell keep a world-preserving
                        // local offset, so pin the authored cell rect explicitly.
                        cellRect.anchorMin = cellRect.anchorMax = cellRect.pivot =
                            new Vector2(0.5f, 0.5f);
                        cellRect.anchoredPosition = Vector2.zero;
                        cellRect.sizeDelta = CellSize;
                        Image image = cell.GetComponent<Image>();
                        image.color = TacticalMissionPanel.CellFill(TacticalTileKind.Floor,
                            TacticalMissionPanel.CellHighlight.None, false);
                        image.raycastTarget = false;
                        cellImages[index] = image;
                        Text label = CreateText(scene, cell.transform, "Glyph", font, BodyFontSize,
                            Vector2.zero, new Vector2(CellSize.x - 2f, CellSize.y - 2f),
                            TextAnchor.MiddleCenter, SengokuUiTheme.ParchmentLight);
                        label.raycastTarget = false;
                        cellLabels[index] = label;
                    }
                }

                Button north = CreateActionButton(scene, page, NorthButtonName, font, "北", 0);
                Button east = CreateActionButton(scene, page, EastButtonName, font, "东", 1);
                Button south = CreateActionButton(scene, page, SouthButtonName, font, "南", 2);
                Button west = CreateActionButton(scene, page, WestButtonName, font, "西", 3);
                Button move = CreateActionButton(scene, page, MoveButtonName, font, "移动", 4);
                Button sprint = CreateActionButton(scene, page, SprintButtonName, font, "疾行", 5);
                Button wait = CreateActionButton(scene, page, WaitButtonName, font, "等待", 6);
                Button interact = CreateActionButton(scene, page, InteractButtonName, font, "夺取", 7);
                Button extract = CreateActionButton(scene, page, ExtractButtonName, font, "撤离", 8);
                Button withdraw = CreateActionButton(scene, page, WithdrawButtonName, font, "撤退", 9);
                Button switchOperative = CreateActionButton(scene, page, SwitchButtonName, font,
                    "换人", 10);

                GameObject scrollObject = Create(scene, page, CandidateScrollName,
                    typeof(Image), typeof(ScrollRect));
                RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
                scrollRect.anchorMin = scrollRect.anchorMax = scrollRect.pivot =
                    new Vector2(0.5f, 0.5f);
                // The briefing hides every operation-only row, so the list sits in the body
                // area under the selection hint and above the message line.
                scrollRect.anchoredPosition = CandidateScrollPosition;
                scrollRect.sizeDelta = CandidateScrollSize;
                Image scrollImage = scrollObject.GetComponent<Image>();
                scrollImage.color = SengokuUiTheme.IndigoPanel;
                scrollImage.raycastTarget = true;

                GameObject viewportObject = Create(scene, scrollRect, CandidateViewportName,
                    typeof(Image), typeof(RectMask2D));
                RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = new Vector2(4f, 4f);
                viewportRect.offsetMax = new Vector2(-4f, -4f);
                Image viewportImage = viewportObject.GetComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                viewportImage.raycastTarget = true;

                GameObject contentObject = Create(scene, viewportRect, CandidateContentName,
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                VerticalLayoutGroup contentLayout =
                    contentObject.GetComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(2, 2, 2, 2);
                contentLayout.spacing = 2f;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
                scroll.content = contentRect;
                scroll.viewport = viewportRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;

                GameObject template = Create(scene, contentRect, CandidateTemplateName,
                    typeof(Image), typeof(Button), typeof(LayoutElement));
                RectTransform templateRect = template.GetComponent<RectTransform>();
                templateRect.anchoredPosition = Vector2.zero;
                templateRect.sizeDelta = new Vector2(552f, 26f);
                Image templateImage = template.GetComponent<Image>();
                templateImage.color = SengokuUiTheme.IndigoLine;
                Button templateButton = template.GetComponent<Button>();
                templateButton.targetGraphic = templateImage;
                LayoutElement templateLayout = template.GetComponent<LayoutElement>();
                templateLayout.preferredWidth = 552f;
                templateLayout.preferredHeight = 26f;
                Text templateText = CreateText(scene, templateRect, "Label", font,
                    BodyFontSize, Vector2.zero, new Vector2(544f, 24f),
                    TextAnchor.MiddleLeft, SengokuUiTheme.ParchmentLight);
                templateText.raycastTarget = false;
                template.SetActive(false);

                // The overlay sits under the active Canvas, so this forced pass places all
                // 48 cells. It runs before the panel component exists, which keeps the
                // generation pass from firing any panel lifecycle callback. Both the sheet
                // and its scrim are hidden again afterwards so the saved scene stays closed.
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid);
                LayoutRebuilder.ForceRebuildLayoutImmediate(page);
                if (!GridCellsAreInTheirSlots(page, grid, layout, out string layoutError))
                    throw new InvalidOperationException(layoutError);
                pageObject.SetActive(false);
                overlayObject.SetActive(false);

                // Move the finished entry root under the town menu and stretch it to the
                // authored rect. The layout pass result is already stored on the cells.
                Undo.SetTransformParent(root.transform, town, UndoName);
                rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);

                TacticalMissionPanel panel = Undo.AddComponent<TacticalMissionPanel>(root);
                panel.BindReferences(session, pageObject, open, close, start, north, east,
                    south, west, move, sprint, wait, interact, extract, withdraw,
                    switchOperative, title, state, date, resources, objective, team, status,
                    guard, result, eventLine, message, chronicle, selection, gridObject,
                    cellImages, cellLabels, contentRect, template, scrollObject);
                panel.BindOverlay(overlayObject);
                Undo.RecordObject(controller, UndoName);
                controller.BindTacticalMissionPanel(panel);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(panel);

                beforeValidation?.Invoke(root);
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);
                Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                failure = UpdateResult.SaveFailed;
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");
                Undo.CollapseUndoOperations(group);
                created = true;
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                // Unity exposes no public clean-state setter; retain the dirty state produced by Undo.
                Debug.LogWarning($"M06 tactical updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            UpdateResult result = Resolve(scene, out Transform canvas, out Transform town,
                out WorldMapController controller, out GameSession session);
            if (result != UpdateResult.Success)
                return Invalid(out error, $"Tactical hierarchy/session preflight: {result}.");

            Transform[] roots = Named(scene, RootName);
            TacticalMissionPanel[] panels = Components<TacticalMissionPanel>(scene);
            Transform[] overlays = Named(scene, OverlayName);
            if (roots.Length != 1 || panels.Length != 1 || overlays.Length != 1)
                return Invalid(out error,
                    "Expected exactly one tactical root, one overlay, and one TacticalMissionPanel.");

            Transform root = roots[0];
            TacticalMissionPanel panel = panels[0];
            Transform overlay = overlays[0];
            RectTransform rootRect = root as RectTransform;
            RectTransform townRect = town as RectTransform;
            RectTransform overlayRect = overlay as RectTransform;
            RectTransform canvasRect = canvas as RectTransform;
            if (root.parent != town || panel.transform != root || rootRect == null ||
                !Contained(rootRect, townRect))
                return Invalid(out error,
                    "Tactical root must be a RectTransform directly under TownMenuRoot.");

            Transform page = overlay.Find(PanelName);
            Transform openTransform = root.Find(OpenButtonName);
            if (page == null || openTransform == null || page.parent != overlay ||
                overlay.parent != canvas || openTransform.parent != root ||
                overlayRect == null || canvasRect == null || !Stretched(overlayRect) ||
                !Bound(controller, "tacticalMissionPanel", panel) ||
                !Bound(panel, "gameSession", session) ||
                !Bound(panel, "panelRoot", page.gameObject) ||
                !Bound(panel, "overlayRoot", overlay.gameObject) ||
                !Bound(panel, "openButton", openTransform.GetComponent<Button>()))
                return Invalid(out error,
                    "Tactical controller, session, overlay, page, open button, or page reference is unbound.");

            RectTransform pageRect = page as RectTransform;
            RectTransform openRect = openTransform as RectTransform;
            if (pageRect == null || openRect == null || page.GetComponent<Image>() == null ||
                pageRect.anchorMin != CenteredAnchor || pageRect.anchorMax != CenteredAnchor ||
                pageRect.pivot != CenteredAnchor ||
                pageRect.anchoredPosition.sqrMagnitude > 0.01f ||
                (pageRect.sizeDelta - PanelSize).sqrMagnitude > 0.01f ||
                !FittedPageStaysInside(pageRect, overlayRect) ||
                !Contained(openRect, rootRect))
                return Invalid(out error,
                    "Tactical page must be the centered 1120x620 sheet and stay inside its " +
                    "overlay at every canvas aspect.");
            if (overlay.GetSiblingIndex() != overlay.parent.childCount - 1)
                return Invalid(out error,
                    "Tactical overlay must be the last Canvas child so the modal clears the " +
                    "town band.");

            if (page.gameObject.activeSelf || overlay.gameObject.activeSelf)
                return Invalid(out error, "The tactical overlay and page must start hidden.");

            Transform backdropTransform = overlay.Find(BackdropName);
            RectTransform backdropRect = backdropTransform as RectTransform;
            Image backdropImage = backdropTransform != null
                ? backdropTransform.GetComponent<Image>()
                : null;
            if (backdropRect == null || backdropImage == null || !Stretched(backdropRect) ||
                backdropImage.color.a < 0.85f || !backdropImage.raycastTarget)
                return Invalid(out error,
                    "The tactical overlay must carry a full-size, near-opaque, click-blocking backdrop.");

            string[] textNames =
            {
                TitleTextName, StateTextName, DateTextName, ResourceTextName, ObjectiveTextName,
                TeamTextName, StatusTextName, GuardTextName, ResultTextName, EventTextName,
                MessageTextName, ChronicleTextName, SelectionTextName
            };
            string[] textFields =
            {
                "titleText", "stateText", "dateText", "resourceText", "objectiveText",
                "teamText", "statusText", "guardText", "resultText", "eventText",
                "messageText", "chronicleText", "selectionText"
            };
            for (int i = 0; i < textNames.Length; i++)
            {
                Transform found = page.Find(textNames[i]);
                Text text = found != null ? found.GetComponent<Text>() : null;
                if (text == null || text.font == null || !Bound(panel, textFields[i], text) ||
                    !Contained((RectTransform)found, pageRect))
                    return Invalid(out error,
                        "Tactical text is missing, unbound, fontless, or outside the page: " +
                        textNames[i] + ".");
            }

            string[] buttonNames =
            {
                CloseButtonName, StartButtonName, NorthButtonName, EastButtonName,
                SouthButtonName, WestButtonName, MoveButtonName, SprintButtonName,
                WaitButtonName, InteractButtonName, ExtractButtonName, WithdrawButtonName,
                SwitchButtonName
            };
            string[] buttonFields =
            {
                "closeButton", "startButton", "northButton", "eastButton", "southButton",
                "westButton", "moveButton", "sprintButton", "waitButton", "interactButton",
                "extractButton", "withdrawButton", "switchButton"
            };
            for (int i = 0; i < buttonNames.Length; i++)
            {
                if (!TransportButton(panel, page, pageRect, buttonNames[i], buttonFields[i],
                        out _, out error))
                    return false;
            }
            if (!TransportButton(panel, root, rootRect, OpenButtonName, "openButton", out _,
                    out error))
                return false;

            Transform grid = page.Find(GridName);
            GridLayoutGroup layout = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
            if (grid == null || layout == null ||
                layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
                layout.constraintCount != TacticalMissionScenario.Width ||
                layout.cellSize != CellSize ||
                !Contained((RectTransform)grid, pageRect))
                return Invalid(out error,
                    "Tactical grid must be an 8-column GridLayoutGroup with " + CellSizeText +
                    " cells inside the page.");

            Transform scroll = page.Find(CandidateScrollName);
            Transform viewport = scroll != null ? scroll.Find(CandidateViewportName) : null;
            Transform content = viewport != null ? viewport.Find(CandidateContentName) : null;
            Transform template = content != null ? content.Find(CandidateTemplateName) : null;
            if (scroll == null || viewport == null || content == null || template == null)
                return Invalid(out error,
                    "Tactical operative list must use a scroll, viewport, content, and template.");
            ScrollRect scrollComponent = scroll.GetComponent<ScrollRect>();
            RectMask2D mask = viewport.GetComponent<RectMask2D>();
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (scrollComponent == null || mask == null || contentLayout == null || fitter == null ||
                scrollComponent.content != (RectTransform)content ||
                scrollComponent.viewport != (RectTransform)viewport ||
                scrollComponent.horizontal || !scrollComponent.vertical ||
                fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
                return Invalid(out error,
                    "Tactical operative list must be a vertical ScrollRect with a masked " +
                    "viewport, vertical layout, and preferred-size content.");
            if (!Bound(panel, "candidateContainer", content) ||
                !Bound(panel, "candidateTemplate", template.gameObject) ||
                !Bound(panel, "candidateScroll", scroll.gameObject) ||
                template.gameObject.activeSelf || template.GetComponent<Button>() == null ||
                template.GetComponent<LayoutElement>() == null)
                return Invalid(out error,
                    "Tactical candidate container or inactive template row is unbound or invalid.");
            if (!Contained((RectTransform)scroll, pageRect) ||
                !Contained((RectTransform)content, (RectTransform)viewport))
                return Invalid(out error,
                    "Tactical operative list extends outside the page or its viewport.");

            Transform townTitle = Named(scene, "TownMenuTitleText").FirstOrDefault();
            Transform returnButton = town.Find("ReturnToMapButton");
            if (!OpenButtonClearsTownEntries(root, town, townRect, openRect, out error))
                return false;
            if ((townTitle != null &&
                 Overlaps(openRect, (RectTransform)townTitle, townRect)) ||
                (returnButton != null &&
                 Overlaps(openRect, (RectTransform)returnButton, townRect)))
                return Invalid(out error,
                    "Tactical open button must not cover the town title or return control.");

            // Read-only on the saved rects: this never activates TownMenuRoot, the page or
            // the panel. The generator already ran the layout pass while the page was
            // staged under the active Canvas, so the scene gate cannot dirty the user's
            // scene, fire sibling panels' OnEnable callbacks, or make a later menu run
            // fail with SceneDirty.
            if (!GridCellsAreInTheirSlots(pageRect, grid, layout, out string layoutError))
                return Invalid(out error, layoutError);
            if (!PageHeaderIsClear(pageRect, out string headerError))
                return Invalid(out error, headerError);
            if (!BodyRowsAreClear(pageRect, out string bodyError))
                return Invalid(out error, bodyError);
            if (!StyleIsCurrent(pageRect, out string styleError))
                return Invalid(out error, styleError);

            SerializedProperty cellImages =
                new SerializedObject(panel).FindProperty("cellImages");
            SerializedProperty cellLabels =
                new SerializedObject(panel).FindProperty("cellLabels");
            if (cellImages == null || cellLabels == null ||
                cellImages.arraySize != TacticalMissionScenario.Width * TacticalMissionScenario.Height ||
                cellLabels.arraySize != TacticalMissionScenario.Width * TacticalMissionScenario.Height)
                return Invalid(out error,
                    "Tactical cell image and label arrays must hold all 48 cells.");
            return panel.IsBound
                ? true
                : Invalid(out error, "Tactical panel references are incomplete.");
        }

        /// <summary>
        /// True when the scene holds a complete, fully bound page this generator wrote. Two
        /// shapes count: the current canvas-level overlay sheet, and the older revision that
        /// kept the page directly under the town root. A page that fails Validate but matches
        /// either shape is an outdated revision and is rebuilt; any other hierarchy is
        /// somebody else's UI and is refused, so the generator never eats a hand-edited page.
        /// </summary>
        private static bool IsOutdatedGeneratedPage(Scene scene, out TacticalMissionPanel panel)
        {
            panel = null;
            Transform[] roots = Named(scene, RootName);
            TacticalMissionPanel[] panels = Components<TacticalMissionPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1)
                return false;
            TacticalMissionPanel candidate = panels[0];
            if (candidate == null || candidate.transform != roots[0] || !candidate.IsBound ||
                roots[0].parent == null)
                return false;
            Transform page = roots[0].Find(PanelName);
            if (page == null)
            {
                Transform[] overlays = Named(scene, OverlayName);
                page = overlays.Length == 1 ? overlays[0].Find(PanelName) : null;
            }
            Transform entry = roots[0].Find(OpenButtonName);
            if (page == null || entry == null || page.GetComponent<Image>() == null ||
                entry.parent != roots[0] || entry.GetComponent<Button>() == null)
                return false;
            panel = candidate;
            return true;
        }

        /// <summary>
        /// Removes the page and the overlay an older revision left outside the root. The
        /// overlay is only destroyed when it is one this generator named, so a hand-built
        /// parent is never eaten, and the whole removal stays inside the rebuild Undo group.
        /// </summary>
        private static void DestroyStalePageObjects(TacticalMissionPanel replaceable)
        {
            var serialized = new SerializedObject(replaceable);
            SerializedProperty field = serialized.FindProperty("panelRoot");
            GameObject stalePage = field != null
                ? field.objectReferenceValue as GameObject
                : null;
            if (stalePage == null || stalePage.transform.IsChildOf(replaceable.transform))
                return;
            Transform staleOverlay = stalePage.transform.parent;
            if (staleOverlay != null && staleOverlay.name == OverlayName)
            {
                Undo.DestroyObjectImmediate(staleOverlay.gameObject);
                return;
            }
            Undo.DestroyObjectImmediate(stalePage);
        }

        /// <summary>
        /// The scene gate for the rendered page. Contrast and legibility are only as good as
        /// the surface, the band and the type scale, so a page that still renders the old
        /// navy slab, a missing band, an 11 px label or the old spacing fails validation
        /// instead of passing as "present".
        /// </summary>
        private static bool StyleIsCurrent(RectTransform pageRect, out string error)
        {
            error = null;
            Image fill = pageRect.GetComponent<Image>();
            if (fill == null || !SameColor(fill.color, SengokuUiTheme.Parchment))
                return Invalid(out error,
                    "Tactical page must be filled with the parchment sheet colour.");

            var band = pageRect.Find(HeaderBandName) as RectTransform;
            Image bandImage = band != null ? band.GetComponent<Image>() : null;
            if (bandImage == null || !SameColor(bandImage.color, SengokuUiTheme.IndigoDeep))
                return Invalid(out error,
                    "Tactical page must carry the indigo header band.");
            if (!Contained(band, pageRect) ||
                Mathf.Abs(band.rect.height - HeaderBandHeight) > 0.1f ||
                Mathf.Abs(band.rect.width - pageRect.rect.width) > 0.1f ||
                Mathf.Abs(band.offsetMax.y) > 0.1f)
                return Invalid(out error,
                    "Tactical header band must span the page width at its top edge.");

            var rule = pageRect.Find(HeaderRuleName) as RectTransform;
            Image ruleImage = rule != null ? rule.GetComponent<Image>() : null;
            if (ruleImage == null || !SameColor(ruleImage.color, SengokuUiTheme.Gold) ||
                Mathf.Abs(rule.rect.height - HeaderRuleHeight) > 0.1f ||
                Mathf.Abs(rule.offsetMax.y + HeaderBandHeight) > 0.1f)
                return Invalid(out error,
                    "Tactical header band must be closed by the gold rule.");

            // The band carries dark-surface type only; the legend moved to the sheet because
            // pale type on indigo was the low-contrast strip the capture reported.
            string[] bandContents = { TitleTextName, StartButtonName, CloseButtonName };
            foreach (string name in bandContents)
            {
                var item = pageRect.Find(name) as RectTransform;
                if (item == null || !Contained(item, band))
                    return Invalid(out error,
                        "Tactical header item must stay inside the header band: " + name + ".");
            }

            var legend = pageRect.Find(LegendTextName) as RectTransform;
            Text legendText = legend != null ? legend.GetComponent<Text>() : null;
            if (legendText == null || !SameColor(legendText.color, SengokuUiTheme.Ink) ||
                Mathf.Abs(legend.anchoredPosition.x) > 0.1f ||
                Mathf.Abs(legend.anchoredPosition.y - LegendCenterY) > 0.1f ||
                Mathf.Abs(legend.sizeDelta.x - LegendWidth) > 0.1f ||
                Mathf.Abs(legend.sizeDelta.y - RowHeight) > 0.1f ||
                !Contained(legend, pageRect) || Overlaps(legend, band, pageRect) ||
                Overlaps(legend,
                    (RectTransform)pageRect.Find(ObjectiveTextName), pageRect))
                return Invalid(out error,
                    "Tactical glyph legend must sit as one readable paper line above the body.");

            Text title = pageRect.Find(TitleTextName).GetComponent<Text>();
            if (title.fontSize != TitleFontSize)
                return Invalid(out error,
                    "Tactical title must be " + TitleFontSize + " px, not " + title.fontSize + ".");

            if (!TypographyIsCurrent(pageRect, out error))
                return false;

            var grid = pageRect.Find(GridName) as RectTransform;
            if (grid == null ||
                (grid.sizeDelta - GridSize()).sqrMagnitude > 0.01f ||
                (grid.anchoredPosition - GridPosition).sqrMagnitude > 0.01f)
                return Invalid(out error,
                    "Tactical grid must use the current " + CellSizeText + " block and position.");

            var scroll = pageRect.Find(CandidateScrollName) as RectTransform;
            if (scroll == null ||
                (scroll.sizeDelta - CandidateScrollSize).sqrMagnitude > 0.01f ||
                (scroll.anchoredPosition - CandidateScrollPosition).sqrMagnitude > 0.01f)
                return Invalid(out error,
                    "Tactical operative list must use the current rect inside the body band.");

            for (int row = 0; row < BodyRowNames.Length; row++)
            {
                string[] names = BodyRowNames[row];
                float height = row == 0 ? ObjectiveHeight : RowHeight;
                foreach (string name in names)
                {
                    var rect = pageRect.Find(name) as RectTransform;
                    if (rect == null ||
                        Mathf.Abs(rect.anchoredPosition.y - BodyRowCenter(row)) > 0.1f ||
                        Mathf.Abs(rect.sizeDelta.x - BodyWidth) > 0.1f ||
                        Mathf.Abs(rect.sizeDelta.y - height) > 0.1f)
                        return Invalid(out error,
                            "Tactical body row must keep its row slot: " + name + ".");
                }
            }
            return true;
        }

        private static bool TypographyIsCurrent(RectTransform pageRect, out string error)
        {
            error = null;
            foreach (Text text in pageRect.GetComponentsInChildren<Text>(true))
            {
                if (text.name == TitleTextName || text.fontSize == BodyFontSize)
                    continue;
                return Invalid(out error,
                    "Tactical label must be " + BodyFontSize + " px, not " + text.fontSize +
                    ": " + text.name + ".");
            }
            return true;
        }

        private static bool SameColor(Color first, Color second) =>
            Mathf.Abs(first.r - second.r) <= 0.004f &&
            Mathf.Abs(first.g - second.g) <= 0.004f &&
            Mathf.Abs(first.b - second.b) <= 0.004f &&
            Mathf.Abs(first.a - second.a) <= 0.004f;

        private static Vector2 GridSize() => new Vector2(
            TacticalMissionScenario.Width * CellSize.x +
            (TacticalMissionScenario.Width - 1) * CellSpacing.x,
            TacticalMissionScenario.Height * CellSize.y +
            (TacticalMissionScenario.Height - 1) * CellSpacing.y);

        /// <summary>
        /// Rejects a second visible town entry that the tactical open button would cover.
        /// Only controls on a fully active path count: a sibling panel's hidden page keeps
        /// its own buttons inactive, so those are not town entries. The generated root is
        /// excluded, which keeps the page's own controls out of the check.
        /// </summary>
        private static bool OpenButtonClearsTownEntries(Transform root, Transform town,
            RectTransform townRect, RectTransform openRect, out string error)
        {
            error = null;
            foreach (Selectable entry in town.GetComponentsInChildren<Selectable>(true))
            {
                Transform target = entry.transform;
                if (target == root || target.IsChildOf(root))
                    continue;
                var targetRect = target as RectTransform;
                if (targetRect == null || !ActivePathTo(target, town))
                    continue;
                if (Overlaps(openRect, targetRect, townRect))
                    return Invalid(out error,
                        "Tactical open button overlaps the town entry '" + entry.name + "'.");
            }
            return true;
        }

        private static bool ActivePathTo(Transform node, Transform town)
        {
            for (Transform current = node; current != null && current != town;
                 current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                    return false;
            }
            return true;
        }

        private static bool GridCellsAreInTheirSlots(RectTransform pageRect, Transform grid,
            GridLayoutGroup layout, out string error)
        {
            error = null;
            var gridRect = (RectTransform)grid;
            Vector2 step = layout.cellSize + layout.spacing;
            var positions = new Vector2[
                TacticalMissionScenario.Width * TacticalMissionScenario.Height];
            var corners = new Vector3[4];
            Vector3 blockMin = new Vector3(float.MaxValue, float.MaxValue);
            Vector3 blockMax = new Vector3(float.MinValue, float.MinValue);
            Vector2 origin = Vector2.zero;
            int cellCount = 0;
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    string name = CellNamePrefix + x + "_" + y;
                    Transform cell = grid.Find(name);
                    if (cell == null)
                        return Invalid(out error, "Tactical grid cell is missing: " + name + ".");
                    // uGUI skips an inactive child when a layout group rebuilds, so one
                    // inactive cell reflows the remaining 47 out of their fixed slots.
                    if (!cell.gameObject.activeSelf)
                        return Invalid(out error,
                            "Tactical grid cells must stay active for the fixed layout: " +
                            name + ".");
                    Image image = cell.GetComponent<Image>();
                    if (image == null)
                        return Invalid(out error, "Tactical grid cell has no Image: " + name + ".");
                    Text label = cell.GetComponentInChildren<Text>(true);
                    if (label == null)
                        return Invalid(out error,
                            "Tactical grid cell has no Text label: " + name + ".");
                    if (label.font == null)
                        return Invalid(out error,
                            "Tactical grid cell label has no font: " + name + ".");
                    var cellRect = (RectTransform)cell;
                    if (!Contained(cellRect, pageRect))
                        return Invalid(out error,
                            "Tactical grid cell is outside the page: " + name + ".");
                    // GridLayoutGroup drives anchors, size and position. Center anchors or an
                    // authored size here mean the layout pass never reached the cell.
                    if (cellRect.anchorMin != Vector2.up || cellRect.anchorMax != Vector2.up ||
                        (cellRect.sizeDelta - layout.cellSize).sqrMagnitude > 0.01f)
                        return Invalid(out error,
                            "Tactical grid cell was not placed by its GridLayoutGroup: " +
                            name + ".");
                    if (cellCount == 0)
                        origin = cellRect.anchoredPosition;
                    Vector2 expected = origin + new Vector2(step.x * x, -step.y * y);
                    if ((cellRect.anchoredPosition - expected).sqrMagnitude > 0.01f)
                        return Invalid(out error,
                            "Tactical grid cell does not match its grid slot: " + name +
                            " at " + cellRect.anchoredPosition + " instead of " + expected +
                            ".");
                    for (int i = 0; i < cellCount; i++)
                    {
                        if ((positions[i] - cellRect.anchoredPosition).sqrMagnitude < 0.01f)
                            return Invalid(out error,
                                "Tactical grid cells share one position: " + name + ".");
                    }
                    positions[cellCount] = cellRect.anchoredPosition;
                    cellCount++;

                    cellRect.GetWorldCorners(corners);
                    for (int i = 0; i < corners.Length; i++)
                    {
                        Vector3 point = gridRect.InverseTransformPoint(corners[i]);
                        blockMin = Vector3.Min(blockMin, point);
                        blockMax = Vector3.Max(blockMax, point);
                    }
                }
            }
            if (cellCount != TacticalMissionScenario.Width * TacticalMissionScenario.Height)
                return Invalid(out error, "The tactical grid must expose all 48 cells.");
            // The grid is sized to exactly fit its cells, so a laid-out block fills it.
            Vector3 blockCenter = (blockMin + blockMax) * 0.5f;
            Vector2 gridCenter = gridRect.rect.center;
            if (new Vector2(blockCenter.x - gridCenter.x, blockCenter.y - gridCenter.y)
                .sqrMagnitude > 0.25f)
                return Invalid(out error,
                    "Tactical grid cells do not fill their grid; the layout pass did not run.");
            return true;
        }

        private static bool PageHeaderIsClear(RectTransform pageRect, out string error)
        {
            error = null;
            if (Overlaps((RectTransform)pageRect.Find(TitleTextName),
                    (RectTransform)pageRect.Find(StartButtonName), pageRect) ||
                Overlaps((RectTransform)pageRect.Find(TitleTextName),
                    (RectTransform)pageRect.Find(CloseButtonName), pageRect))
                return Invalid(out error,
                    "Tactical title and header buttons overlap on the fixed layout.");
            if (Overlaps((RectTransform)pageRect.Find(TitleTextName),
                    (RectTransform)pageRect.Find(LegendTextName), pageRect))
                return Invalid(out error,
                    "Tactical title and glyph legend overlap on the fixed layout.");
            return true;
        }

        /// <summary>
        /// Rejects a body label that collides with another label or the command strip. The
        /// briefing selection hint shares the party row, and the operative list fills the
        /// rows that the briefing hides, so neither is part of the label row set.
        /// </summary>
        private static bool BodyRowsAreClear(RectTransform pageRect, out string error)
        {
            error = null;
            string[] rows =
            {
                ObjectiveTextName, StateTextName, DateTextName, ResourceTextName, TeamTextName,
                StatusTextName, GuardTextName, ResultTextName, EventTextName, ChronicleTextName,
                MessageTextName
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = (RectTransform)pageRect.Find(rows[i]);
                if (row == null)
                    return Invalid(out error, "Tactical body row is missing: " + rows[i] + ".");
                if (Overlaps(row, (RectTransform)pageRect.Find(NorthButtonName), pageRect))
                    return Invalid(out error,
                        "Tactical body row '" + rows[i] + "' overlaps the command strip.");
                for (int j = i + 1; j < rows.Length; j++)
                {
                    if (Overlaps(row, (RectTransform)pageRect.Find(rows[j]), pageRect))
                        return Invalid(out error,
                            "Tactical body rows overlap: " + rows[i] + " and " + rows[j] + ".");
                }
            }

            // Each row owns its slot: two rows in one slot would print over each other.
            foreach (string[] slot in BodyRowNames)
            {
                var first = (RectTransform)pageRect.Find(slot[0]);
                for (int i = 1; i < slot.Length; i++)
                {
                    var other = (RectTransform)pageRect.Find(slot[i]);
                    if (first == null || other == null ||
                        (first.anchoredPosition - other.anchoredPosition).sqrMagnitude > 0.01f)
                        return Invalid(out error,
                            "Tactical rows must share one slot exactly: " + slot[0] + " with " +
                            slot[i] + ".");
                }
            }

            // The operative list is visible only while the operation rows are hidden, so it
            // may share their space; it must still clear every briefing row.
            string[] briefingRows =
            {
                ObjectiveTextName, StateTextName, DateTextName, ResourceTextName, TeamTextName,
                MessageTextName
            };
            var scroll = (RectTransform)pageRect.Find(CandidateScrollName);
            foreach (string name in briefingRows)
            {
                if (Overlaps(scroll, (RectTransform)pageRect.Find(name), pageRect))
                    return Invalid(out error,
                        "Tactical operative list overlaps the briefing row '" + name + "'.");
            }
            return true;
        }

        private static Button CreateActionButton(Scene scene, Transform parent, string name,
            Font font, string label, int index)
        {
            Button button = CreateButton(scene, parent, name, font, label, CommandSize,
                BodyFontSize);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(CommandStartX + index * CommandStep, CommandY);
            return button;
        }

        private static bool TransportButton(TacticalMissionPanel panel, Transform parent,
            RectTransform parentRect, string name, string field, out RectTransform rect,
            out string error)
        {
            error = null;
            rect = null;
            Transform found = parent.Find(name);
            Button button = found != null ? found.GetComponent<Button>() : null;
            Text[] labels = found != null ? found.GetComponentsInChildren<Text>(true) : null;
            if (button == null || button.targetGraphic == null ||
                button.targetGraphic.gameObject != button.gameObject ||
                labels == null || labels.Length != 1 || labels[0].font == null ||
                labels[0].transform.parent != button.transform ||
                string.IsNullOrWhiteSpace(labels[0].text) || button.transform.childCount != 1 ||
                !Bound(panel, field, button) ||
                !Contained((RectTransform)found, parentRect))
            {
                error = $"Tactical button '{name}' is missing, unbound, styling-less, or outside its parent.";
                return false;
            }
            rect = (RectTransform)found;
            return true;
        }

        private static UpdateResult Resolve(Scene scene, out Transform canvas, out Transform town,
            out WorldMapController controller, out GameSession session)
        {
            canvas = null; town = null; controller = null; session = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;
            Transform[] canvasRoots = Named(scene, "Canvas");
            Transform[] map = Named(scene, "M2WorldMapRoot");
            Transform[] towns = Named(scene, "TownMenuRoot");
            Canvas[] canvases = Components<Canvas>(scene);
            WorldMapController[] controllers = Components<WorldMapController>(scene);
            GameSession[] sessions = Components<GameSession>(scene);
            if (canvasRoots.Length != 1 || map.Length != 1 || towns.Length != 1 ||
                canvases.Length != 1 || controllers.Length != 1 || sessions.Length != 1)
                return UpdateResult.MissingOrDuplicateObjects;
            canvas = canvasRoots[0];
            town = towns[0];
            controller = controllers[0];
            session = sessions[0];
            if (canvases[0].transform != canvasRoots[0] ||
                !(canvasRoots[0] is RectTransform) ||
                map[0].parent != canvasRoots[0] || town.parent != map[0] ||
                controller.transform != map[0] || !(town is RectTransform) ||
                !(map[0] is RectTransform) ||
                !Bound(controller, "gameSession", session) ||
                !Bound(controller, "townMenuRoot", town.gameObject))
                return UpdateResult.InvalidHierarchyOrBindings;
            return UpdateResult.Success;
        }

        private static bool Bound(UnityEngine.Object target, string field, UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool Stretched(RectTransform rect) =>
            rect != null &&
            (rect.anchorMin - Vector2.zero).sqrMagnitude <= 0.0001f &&
            (rect.anchorMax - Vector2.one).sqrMagnitude <= 0.0001f &&
            rect.offsetMin.sqrMagnitude <= 0.01f &&
            rect.offsetMax.sqrMagnitude <= 0.01f;

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }

        private static Transform[] Named(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name).ToArray();

        private static T[] Components<T>(Scene scene) where T : Component => scene
            .GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();

        private static GameObject Create(Scene scene, Transform parent, string name,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            // Move before registering Undo so preview-scene tests do not dirty the user's active scene.
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            // Undo.SetTransformParent preserves the world position. A freshly created
            // GameObject sits at the world origin, so in a real scene whose Canvas or town
            // root is far from it the child would inherit a large local offset. Every
            // creator below places its own rect, and this makes the base deterministic.
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components)
                Undo.AddComponent(go, component);
            return go;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font,
            int size, Vector2 position, Vector2 dimensions, TextAnchor alignment, Color color,
            string value = "")
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font,
            string label, Vector2 size, int fontSize)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = SengokuUiTheme.IndigoPanel;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(scene, rect, "Label", font, fontSize, Vector2.zero,
                new Vector2(size.x - 12f, size.y - 6f), TextAnchor.MiddleCenter,
                SengokuUiTheme.ParchmentLight, label);
            text.raycastTarget = false;
            return button;
        }

        /// <summary>
        /// Places one body label on its numbered row of the parchment sheet. Row 0 is the
        /// two-line objective block; every later row is one line on a fixed pitch in the
        /// right-hand column beside the grid.
        /// </summary>
        private static Text CreateBodyText(Scene scene, Transform parent, string name, Font font,
            int row, float height, TextAnchor alignment, Color color) =>
            CreateText(scene, parent, name, font, BodyFontSize,
                new Vector2(BodyCenterX, BodyRowCenter(row)), new Vector2(BodyWidth, height),
                alignment, color);

        private static float BodyRowCenter(int row)
        {
            if (row == 0)
                return BodyTopY - ObjectiveHeight * 0.5f;
            return BodyTopY - ObjectiveHeight - RowHeight * 0.5f - (row - 1) * RowPitch;
        }

        /// <summary>
        /// The authored 1120x620 sheet is not the rendered size: FitPageToParent scales it
        /// into the canvas-sized overlay, and the CanvasScaler derives that overlay from the
        /// window aspect. Comparing the authored sizeDelta with canvasRect.rect therefore
        /// rejected valid 4:3 and super-ultrawide windows, where the overlay is narrower
        /// (about 1108 units at 1024x768) or shorter (about 509 units at 3840x1080) than the
        /// sheet. This gate applies the same fit the runtime applies, so a page the player
        /// will actually see contained is accepted and a page that would underflow the
        /// readable floor is still rejected.
        /// </summary>
        private static bool FittedPageStaysInside(RectTransform page, RectTransform overlay)
        {
            Vector2 available = overlay.rect.size;
            Vector2 authored = page.sizeDelta;
            if (!TacticalMissionPanel.FitsAtFloor(available, authored))
                return false;
            float scale = TacticalMissionPanel.FitScale(available, authored);
            Vector2 half = authored * (0.5f * scale);
            Vector2 center = page.anchoredPosition;
            return center.x - half.x >= overlay.rect.xMin - 0.1f &&
                   center.x + half.x <= overlay.rect.xMax + 0.1f &&
                   center.y - half.y >= overlay.rect.yMin - 0.1f &&
                   center.y + half.y <= overlay.rect.yMax + 0.1f;
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null)
                return false;
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

        private static bool Overlaps(RectTransform first, RectTransform second, RectTransform space)
        {
            if (first == null || second == null || space == null)
                return false;
            Rect a = LocalRect(first, space);
            Rect b = LocalRect(second, space);
            float width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return width > 0.5f && height > 0.5f;
        }

        private static Rect LocalRect(RectTransform rect, RectTransform space)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = space.InverseTransformPoint(corners[0]);
            Vector3 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 point = space.InverseTransformPoint(corners[i]);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
