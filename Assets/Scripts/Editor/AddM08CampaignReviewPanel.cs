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
    /// Transactional generator for the M08 campaign review overlay. It builds one fixed
    /// root under the existing Canvas, a full-screen page with a centred card, a
    /// scrollable sourced-history body, a continue action, a new-scenario action that opens
    /// the title overlay's selector, and the status line. The page starts hidden;
    /// CampaignReviewPanel shows it only while a real 55-year or 100-year review is pending.
    /// Everything is created in one Undo group, validated before saving, and rolled back
    /// through Undo on failure without reloading the scene. An existing page whose only
    /// defect is its Canvas order is moved back under the M6 title in one undoable step
    /// instead of being rebuilt.
    /// </summary>
    public static class AddM08CampaignReviewPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = CampaignReviewPanel.RootObjectName;
        // Canvas siblings share one ranked overlay band (CanvasLayerOrder): the campaign
        // review renders above the M02 page and the M03 inspector and beneath the full-screen
        // M6 title, EventChoiceRoot and the last M06 tactical modal.
        public const string TitleRootName = CanvasLayerOrder.TitleRootName;
        public const string EventChoiceRootName = CanvasLayerOrder.EventChoiceRootName;
        public const string TacticalOverlayName = CanvasLayerOrder.TacticalOverlayName;
        public const string PageName = CampaignReviewPanel.PageObjectName;
        public const string CardName = CampaignReviewPanel.CardObjectName;
        public const string TitleTextName = "M08CampaignReviewTitleText";
        public const string ContextTextName = "M08CampaignReviewContextText";
        public const string BodyScrollName = "M08CampaignReviewScroll";
        public const string BodyViewportName = "M08CampaignReviewViewport";
        public const string BodyContentName = "M08CampaignReviewContent";
        public const string BodyTextName = "M08CampaignReviewBodyText";
        public const string StatusTextName = "M08CampaignReviewStatusText";
        public const string ButtonRowName = "M08CampaignReviewButtonRow";
        public const string ContinueButtonName = "M08CampaignReviewContinueButton";
        public const string ContinueLabelName = "M08CampaignReviewContinueLabel";
        public const string NewScenarioButtonName = CampaignReviewPanel.NewScenarioButtonName;
        public const string NewScenarioLabelName = CampaignReviewPanel.NewScenarioLabelName;
        public const string ContinueCaption = "继续家族";

        private const string MenuPath = "Tools/Sengoku Chronicle/Add M08 Campaign Review Panel";
        private const string UndoName = "Add M08 Campaign Review Panel";
        // The only failures an existing page may be repaired for. A root saved by an older
        // revision sits above the title and the tactical modal, so repair is limited to the
        // errors a misplaced root produces; every other defect stays refused untouched.
        private const string TacticalLastError =
            "The M06 tactical overlay must remain the last Canvas child.";
        private const string TacticalOrderError =
            "Campaign review root must render below the M06 tactical overlay.";
        private const string TitleOrderError =
            "Campaign review root must render below the M6 title overlay.";
        private const string EventOrderError =
            "M6 title root must stay immediately below EventChoiceRoot when it exists.";
        private const float ReferenceCardWidth = 1120f;
        private const float ReferenceCardHeight = 640f;
        private const float PageMargin = 32f;
        // Action row geometry. The two actions sit side by side, centred together, and each
        // stays inside the authored card before any layout pass runs.
        private const float ActionRowHeight = 52f;
        private const float ActionButtonWidth = 380f;
        private const float ActionButtonHeight = 42f;
        private const float ActionButtonGap = 12f;

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene, MissingOrDuplicateObjects,
            InvalidHierarchyOrBindings, ExistingCampaignPanel, InsufficientSpace,
            ValidationFailed, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.EditorPlaying
                : CheckScenePreflight(scene.path, scene.isDirty);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("M08 campaign review panel validated; the reviewed overlay was saved in FoundationScene.");
            else
                Debug.LogError($"M08 campaign review updater refused or rolled back: {result}.");
        }

        /// <summary>Shared menu guard, exposed so tests need not save or replace FoundationScene.</summary>
        public static UpdateResult CheckScenePreflight(string path, bool dirty)
        {
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam for isolated scenes. Never saves.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault injection runs inside the Undo transaction, immediately before validation.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        private static UpdateResult Execute(Scene scene, bool save,
            Action<GameObject> beforeValidation)
        {
            UpdateResult preflight = Resolve(scene, out Transform canvas, out GameSession session);
            if (preflight != UpdateResult.Success)
                return preflight;

            if (Named(scene, RootName).Length != 0 ||
                Components<CampaignReviewPanel>(scene).Length != 0)
            {
                if (Validate(scene, out string error))
                    return UpdateResult.Success;
                // A page generated before the two-action shape has no new-scenario entry and
                // a status line that predates the readable ink. Only that known generated
                // hierarchy is replaced, so the older scene upgrades without manual deletion;
                // any other invalid page keeps the refusal path below.
                if (IsLegacyGeneratedPage(scene, canvas))
                    return Rebuild(scene, canvas, session, save, null, true);
                return RepairableOrderError(error)
                    ? RepairOrdering(scene, canvas, save, beforeValidation)
                    : UpdateResult.ExistingCampaignPanel;
            }

            RectTransform canvasRect = canvas as RectTransform;
            Vector2 design = ResolveDesignSize(canvasRect);
            if (design.x < 960f || design.y < 540f)
                return UpdateResult.InsufficientSpace;

            return Rebuild(scene, canvas, session, save, beforeValidation, false);
        }

        /// <summary>
        /// Creates, or replaces a legacy generated page with, the whole review hierarchy inside
        /// one Undo group. Replacement is limited to <see cref="IsLegacyGeneratedPage"/>, so
        /// nothing outside the review root is deleted. The shared overlay band is repaired
        /// before validation, because a page or succession root saved by an older updater can
        /// be in the wrong slot. A failure rolls the Undo group back and restores the exact
        /// Canvas child order found before the attempt.
        /// </summary>
        private static UpdateResult Rebuild(Scene scene, Transform canvas, GameSession session,
            bool save, Action<GameObject> beforeValidation, bool replaceExisting)
        {
            Transform[] orderBefore = Children(canvas);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                // Sibling order lives in the parent RectTransform, so record the parent too.
                Undo.RegisterCompleteObjectUndo(canvas, UndoName);
                if (replaceExisting)
                {
                    Transform legacy = canvas.Find(RootName);
                    if (legacy == null)
                        throw new InvalidOperationException(
                            "The legacy campaign review root vanished before it could be replaced.");
                    Undo.DestroyObjectImmediate(legacy.gameObject);
                }

                GameObject root = BuildPanel(scene, canvas, session);
                beforeValidation?.Invoke(root);
                CanvasLayerOrder.PlaceBandAboveUnranked(canvas);
                Undo.FlushUndoRecordObjects();
                failure = UpdateResult.ValidationFailed;
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);
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
                RestoreOrder(canvas, orderBefore);
                Debug.LogWarning($"M08 campaign review updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        /// <summary>Builds the generated review hierarchy beneath the existing Canvas.</summary>
        private static GameObject BuildPanel(Scene scene, Transform canvas, GameSession session)
        {
            GameObject root = Create(scene, canvas, RootName);
            PlaceBelowTitleOverlay(canvas, root.transform);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                throw new InvalidOperationException("Missing built-in UI font.");

            GameObject pageObject = Create(scene, rootRect, PageName, typeof(Image));
            RectTransform page = pageObject.GetComponent<RectTransform>();
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;
            Image pageImage = pageObject.GetComponent<Image>();
            pageImage.color = SengokuUiTheme.WithAlpha(SengokuUiTheme.IndigoBackdrop, 0.96f);
            pageImage.raycastTarget = true;
            pageObject.SetActive(false);

            GameObject cardObject = Create(scene, page, CardName, typeof(Image),
                typeof(VerticalLayoutGroup));
            RectTransform card = cardObject.GetComponent<RectTransform>();
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = CardSize(ResolveDesignSize(canvas as RectTransform));
            Image cardImage = cardObject.GetComponent<Image>();
            cardImage.color = SengokuUiTheme.IndigoPanel;
            cardImage.raycastTarget = true;

            VerticalLayoutGroup layout = cardObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 20);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            Text title = CreateText(scene, card, TitleTextName, font, 30,
                new Vector2(1000f, 40f), TextAnchor.MiddleLeft, SengokuUiTheme.Gold,
                "家族评价");
            AddPreferredHeight(title.gameObject, 40f);

            Text context = CreateText(scene, card, ContextTextName, font, 15,
                new Vector2(1000f, 40f), TextAnchor.UpperLeft, SengokuUiTheme.Parchment,
                "剧本来源与评价日期");
            AddPreferredHeight(context.gameObject, 40f);

            GameObject scrollObject = Create(scene, card, BodyScrollName, typeof(Image),
                typeof(ScrollRect), typeof(LayoutElement));
            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = SengokuUiTheme.WithAlpha(SengokuUiTheme.ParchmentDeep, 0.16f);
            scrollImage.raycastTarget = true;
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.minHeight = 180f;
            scrollLayout.flexibleHeight = 1f;

            GameObject viewportObject = Create(scene, scrollObject.transform,
                BodyViewportName, typeof(Image), typeof(RectMask2D));
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 6f);
            viewport.offsetMax = new Vector2(-8f, -6f);
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;

            GameObject contentObject = Create(scene, viewport, BodyContentName,
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout =
                contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter contentFitter =
                contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            Text body = CreateText(scene, content, BodyTextName, font, 17,
                Vector2.zero, TextAnchor.UpperLeft, SengokuUiTheme.Parchment, string.Empty);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            // The body Text is the first block template. CampaignReviewPanel clones it
            // per bounded block, because one Text cannot render the 89,212-character
            // 55-year review: its mesh exceeds the uGUI CanvasRenderer limit and the
            // whole viewport renders blank. The content's VerticalLayoutGroup stacks
            // the blocks with Text's own preferred height, in body order.

            Text status = CreateText(scene, card, StatusTextName, font, 14,
                new Vector2(1000f, 20f), TextAnchor.MiddleLeft,
                CampaignReviewPanel.StatusTextColor, string.Empty);
            AddPreferredHeight(status.gameObject, 20f);

            GameObject rowObject = Create(scene, card, ButtonRowName, typeof(LayoutElement));
            RectTransform row = rowObject.GetComponent<RectTransform>();
            // The card's VerticalLayoutGroup owns this row at runtime. Sizing it up front
            // keeps the two actions inside the card for the containment check, which runs
            // before any canvas layout pass.
            row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = Vector2.zero;
            row.sizeDelta = new Vector2(card.rect.size.x - 48f, ActionRowHeight);
            rowObject.GetComponent<LayoutElement>().preferredHeight = ActionRowHeight;
            float actionOffset = (ActionButtonWidth + ActionButtonGap) * 0.5f;
            Button cont = CreateButton(scene, row, ContinueButtonName, font, ContinueCaption,
                new Vector2(ActionButtonWidth, ActionButtonHeight), 16);
            cont.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-actionOffset, 0f);
            Text label = cont.GetComponentInChildren<Text>(true);
            if (label != null)
                label.name = ContinueLabelName;

            Button startNew = CreateButton(scene, row, NewScenarioButtonName, font,
                CampaignReviewPanel.NewScenarioLabel,
                new Vector2(ActionButtonWidth, ActionButtonHeight), 16);
            startNew.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(actionOffset, 0f);
            Text startNewLabel = startNew.GetComponentInChildren<Text>(true);
            if (startNewLabel != null)
                startNewLabel.name = NewScenarioLabelName;

            CampaignReviewPanel panel = Undo.AddComponent<CampaignReviewPanel>(root);
            panel.BindReferences(session, pageObject, title, context, body, label, status, cont,
                startNew, startNewLabel, TitleScreen(scene));
            pageObject.SetActive(false);
            EditorUtility.SetDirty(panel);
            return root;
        }

        /// <summary>
        /// True only for the page shape this generator produced before the new-scenario action
        /// and the readable status ink existed: the whole known hierarchy is present and every
        /// object under the root carries a generated name. That narrow match lets the updater
        /// replace an old tool-generated page without manual deletion while refusing a page a
        /// user added objects to. A page that already has the new-scenario action is never
        /// rebuilt, so a structural defect there keeps the existing refusal.
        /// </summary>
        private static bool IsLegacyGeneratedPage(Scene scene, Transform canvas)
        {
            Transform[] roots = Named(scene, RootName);
            CampaignReviewPanel[] panels = Components<CampaignReviewPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1 ||
                roots[0].parent != canvas || panels[0].transform != roots[0])
                return false;
            Transform root = roots[0];
            if (FindDeep(root, NewScenarioButtonName) != null)
                return false;
            string[] required =
            {
                PageName, CardName, TitleTextName, ContextTextName, BodyScrollName,
                BodyViewportName, BodyContentName, BodyTextName, StatusTextName,
                ButtonRowName, ContinueButtonName, ContinueLabelName
            };
            for (int i = 0; i < required.Length; i++)
                if (FindDeep(root, required[i]) == null)
                    return false;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (!IsGeneratedObjectName(all[i].name))
                    return false;
            return true;
        }

        /// <summary>Every name the generator owns inside the review root.</summary>
        private static bool IsGeneratedObjectName(string name) =>
            name == RootName || name == PageName || name == CardName ||
            name == TitleTextName || name == ContextTextName || name == BodyScrollName ||
            name == BodyViewportName || name == BodyContentName || name == BodyTextName ||
            name == StatusTextName || name == ButtonRowName || name == ContinueButtonName || name == ContinueLabelName ||
            name == NewScenarioButtonName || name == NewScenarioLabelName ||
            (name != null && name.StartsWith(BodyTextName + CampaignReviewPanel.BodyBlockNameSuffix,
                StringComparison.Ordinal));

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            UpdateResult result = Resolve(scene, out Transform canvas, out GameSession session);
            if (result != UpdateResult.Success)
                return Invalid(out error, $"Campaign review hierarchy/session preflight: {result}.");

            Transform[] roots = Named(scene, RootName);
            CampaignReviewPanel[] panels = Components<CampaignReviewPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1)
                return Invalid(out error,
                    "Expected exactly one campaign review root and one CampaignReviewPanel.");

            Transform root = roots[0];
            CampaignReviewPanel panel = panels[0];
            RectTransform rootRect = root as RectTransform;
            if (root.parent != canvas || panel.transform != root || rootRect == null ||
                !Stretch(rootRect))
                return Invalid(out error,
                    "Campaign review root must be a full-screen RectTransform under Canvas.");
            if (!Ordered(canvas, root, out error))
                return false;

            Transform page = root.Find(PageName);
            Transform card = page != null ? page.Find(CardName) : null;
            RectTransform pageRect = page as RectTransform;
            RectTransform cardRect = card as RectTransform;
            if (page == null || card == null || page.parent != root || card.parent != page ||
                pageRect == null || cardRect == null ||
                page.GetComponent<Image>() == null || card.GetComponent<Image>() == null ||
                !Stretch(pageRect))
                return Invalid(out error,
                    "Campaign review page must be a full-screen overlay with a contained card.");

            Vector2 design = ResolveDesignSize(rootRect);
            if (cardRect.rect.width > design.x + 0.1f ||
                cardRect.rect.height > design.y + 0.1f)
                return Invalid(out error,
                    "Campaign review card must fit the resolved canvas design size.");

            if (!Bound(panel, "gameSession", session) ||
                !Bound(panel, "panelRoot", page.gameObject))
                return Invalid(out error,
                    "Campaign review session or page reference is unbound.");

            Text title = ChildText(card, TitleTextName);
            Text context = ChildText(card, ContextTextName);
            Text status = ChildText(card, StatusTextName);
            Text body = FindText(page, BodyTextName);
            if (title == null || context == null || status == null || body == null ||
                title.font == null || context.font == null || status.font == null ||
                body.font == null ||
                !Bound(panel, "titleText", title) ||
                !Bound(panel, "contextText", context) ||
                !Bound(panel, "statusText", status) ||
                !Bound(panel, "bodyText", body))
                return Invalid(out error,
                    "Campaign review title, context, status or body text is missing or unbound.");

            Transform[] textParents = { title.transform.parent, context.transform.parent,
                status.transform.parent };
            for (int i = 0; i < textParents.Length; i++)
                if (textParents[i] != card)
                    return Invalid(out error,
                        "Campaign review header/status text must sit directly on the card.");

            Transform scroll = card.Find(BodyScrollName);
            Transform viewport = scroll != null ? scroll.Find(BodyViewportName) : null;
            Transform content = viewport != null ? viewport.Find(BodyContentName) : null;
            if (scroll == null || viewport == null || content == null)
                return Invalid(out error,
                    "Campaign review body must use a scroll, viewport and content hierarchy.");
            ScrollRect scrollComponent = scroll.GetComponent<ScrollRect>();
            RectMask2D mask = viewport.GetComponent<RectMask2D>();
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (scrollComponent == null || mask == null || contentLayout == null ||
                fitter == null ||
                scrollComponent.content != (RectTransform)content ||
                scrollComponent.viewport != (RectTransform)viewport ||
                scrollComponent.horizontal || !scrollComponent.vertical ||
                fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize ||
                !contentLayout.childControlHeight || contentLayout.childForceExpandHeight ||
                !TopAnchored((RectTransform)content) ||
                body.transform.parent != content ||
                body.verticalOverflow != VerticalWrapMode.Overflow)
                return Invalid(out error,
                    "Campaign review body must be a vertical scroll whose content stacks " +
                    "bounded, auto-sized Text blocks from a top anchor.");

            // The scrollbar is created at runtime in the body scroll's own scene. When a saved
            // page already owns one, it must be the generated 24 px column with a handle, and
            // the viewport must reserve that column so the bar never covers wrapped text.
            RectTransform viewportRect = (RectTransform)viewport;
            bool authoredInset =
                Inset(viewportRect.offsetMin, 8f, 6f) && Inset(viewportRect.offsetMax, -8f, -6f);
            bool reservedInset =
                Inset(viewportRect.offsetMin, 8f, 6f) &&
                Inset(viewportRect.offsetMax,
                    -(2f + CampaignReviewPanel.ScrollbarWidth + 8f), -6f);
            if (!authoredInset && !reservedInset)
                return Invalid(out error,
                    "Campaign review body viewport must keep the authored inset or reserve " +
                    "the 24 px scrollbar column.");
            Scrollbar authoredBar = scrollComponent.verticalScrollbar;
            if (authoredBar != null)
            {
                RectTransform barRect = authoredBar.transform as RectTransform;
                if (authoredBar.transform.parent != scroll || barRect == null ||
                    Mathf.Abs(barRect.rect.width - CampaignReviewPanel.ScrollbarWidth) > 0.01f ||
                    authoredBar.handleRect == null ||
                    authoredBar.handleRect.parent != barRect)
                    return Invalid(out error,
                        "Campaign review body scrollbar must be a 24 px column inside the " +
                        "scroll with its own handle.");
            }

            Text label = FindText(page, ContinueLabelName);
            Button cont = FindButton(page, ContinueButtonName);
            if (cont == null || label == null || label.font == null ||
                string.IsNullOrWhiteSpace(label.text) ||
                label.transform.parent != cont.transform || cont.transform.childCount != 1 ||
                cont.targetGraphic == null ||
                cont.targetGraphic.gameObject != cont.gameObject ||
                !Bound(panel, "continueButton", cont) ||
                !Bound(panel, "continueLabelText", label) ||
                !Contained((RectTransform)cont.transform, cardRect))
                return Invalid(out error,
                    "Campaign review continue button or label is missing, unbound or outside the card.");

            Transform row = card.Find(ButtonRowName);
            Button startNew = FindButton(page, NewScenarioButtonName);
            Text startNewLabel = FindText(page, NewScenarioLabelName);
            if (row == null || row.parent != card || startNew == null || startNewLabel == null ||
                startNew == cont || startNewLabel.font == null ||
                startNewLabel.transform.parent != startNew.transform ||
                startNew.transform.parent != row ||
                !startNew.interactable || startNew.transform.childCount != 1 ||
                startNew.targetGraphic == null ||
                startNew.targetGraphic.gameObject != startNew.gameObject ||
                startNewLabel.text != CampaignReviewPanel.NewScenarioLabel ||
                !Bound(panel, "newScenarioButton", startNew) ||
                !Bound(panel, "newScenarioLabelText", startNewLabel) ||
                !Contained((RectTransform)startNew.transform, cardRect))
                return Invalid(out error,
                    "Campaign review new-scenario button or label is missing, unbound or " +
                    "outside the card.");

            TitleScreenController[] titles = Components<TitleScreenController>(scene);
            if (titles.Length > 1)
                return Invalid(out error,
                    "Expected at most one TitleScreenController to own the scenario selector.");
            if (titles.Length == 1)
            {
                if (!Bound(panel, "titleScreenController", titles[0]))
                    return Invalid(out error,
                        "Campaign review new-scenario action must point at the scene's " +
                        "TitleScreenController.");
            }
            else if (Reference(panel, "titleScreenController") != null)
                return Invalid(out error,
                    "Campaign review title reference must stay empty when the scene has no " +
                    "TitleScreenController.");

            // WCAG 1.4.3: the 14 px status line reports a refused action, so it needs the
            // 4.5:1 normal-text ratio against the authored card, not the large-text 3:1.
            if (!MeetsNormalTextContrast(status.color, card.GetComponent<Image>().color))
                return Invalid(out error,
                    "Campaign review status text must reach 4.5:1 against the card.");

            if (title.text.Length == 0 || context.text.Length == 0)
                return Invalid(out error,
                    "Campaign review header text must be initialized.");
            return true;
        }

        /// <summary>
        /// Unity renders later Canvas siblings above earlier ones, so the review overlay must
        /// sit above the M02 page and the M03 inspector, below a pending succession decision,
        /// and under the M6 title overlay. AddM6TitleScreen.Validate requires the title to be
        /// the sibling immediately below EventChoiceRoot and allows nothing else above it, and
        /// AddM06TacticalMissionPanel.Validate requires the tactical overlay to stay last.
        /// The root is created at the end of the Canvas, so this places it in its rank slot
        /// from <see cref="CanvasLayerOrder"/> without shifting the roots around it.
        /// </summary>
        private static void PlaceBelowTitleOverlay(Transform canvas, Transform root) =>
            CanvasLayerOrder.PlaceAtRank(canvas, root);

        /// <summary>
        /// A complete generated page saved by an older revision can carry nothing but the
        /// wrong Canvas order. Only a misplaced root is repairable. Title/EventChoiceRoot
        /// adjacency and a misplaced sibling root are reported separately because moving the
        /// review root alone cannot fix them, and the repair validates the whole shared band
        /// (M02, M03, SC-010, title, event choice, tactical) before it reports success.
        /// </summary>
        private static bool RepairableOrderError(string error) =>
            error == TacticalLastError || error == TacticalOrderError || error == TitleOrderError ||
            error == CanvasLayerOrder.CampaignReviewAboveDailyActionError ||
            error == CanvasLayerOrder.CampaignReviewAboveInspectorError ||
            (error != null && error.StartsWith(CanvasLayerOrder.RankOrderError,
                StringComparison.Ordinal));

        /// <summary>
        /// A complete generated page saved by an older revision can carry nothing but the
        /// wrong Canvas order. Restoring the shared overlay band in one Undo group repairs the
        /// scene without rebuilding it. Only roots that are out of order move, so a correctly
        /// placed M02 page, M03 inspector, succession root or M06 modal keeps its slot;
        /// anything else stays refused, and a failed re-validation restores the exact previous
        /// order. The tactical modal is put back last even when the band ranks already ascend,
        /// because an unranked root saved after it is the only defect then.
        /// </summary>
        private static UpdateResult RepairOrdering(Scene scene, Transform canvas, bool save,
            Action<GameObject> beforeValidation)
        {
            Transform[] roots = Named(scene, RootName);
            CampaignReviewPanel[] panels = Components<CampaignReviewPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1 ||
                panels[0].transform != roots[0] || roots[0].parent != canvas)
                return UpdateResult.ExistingCampaignPanel;

            Transform[] orderBefore = Children(canvas);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.ExistingCampaignPanel;
            try
            {
                // Sibling order lives in the parent RectTransform, so record the parent too.
                Undo.RegisterCompleteObjectUndo(canvas, UndoName);
                Transform tactical = canvas.Find(TacticalOverlayName);
                if (tactical != null && tactical.GetSiblingIndex() != canvas.childCount - 1)
                    tactical.SetAsLastSibling();
                CanvasLayerOrder.RepairBand(canvas);
                Undo.FlushUndoRecordObjects();
                beforeValidation?.Invoke(roots[0].gameObject);
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);
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
                // Sibling order lives in the parent's child list and must be restored even
                // if the parent undo record is not.
                RestoreOrder(canvas, orderBefore);
                Debug.LogWarning($"M08 campaign review updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        /// <summary>Snapshot of the Canvas child order, used to roll back a failed repair.</summary>
        private static Transform[] Children(Transform canvas)
        {
            var children = new Transform[canvas.childCount];
            for (int i = 0; i < children.Length; i++)
                children[i] = canvas.GetChild(i);
            return children;
        }

        /// <summary>Puts the captured Canvas child order back after a failed repair.</summary>
        private static void RestoreOrder(Transform canvas, Transform[] order)
        {
            if (canvas == null || order == null)
                return;
            bool moved = false;
            for (int i = 0; i < order.Length; i++)
            {
                Transform child = order[i];
                if (child == null || child.parent != canvas || child.GetSiblingIndex() == i)
                    continue;
                child.SetSiblingIndex(i);
                moved = true;
            }
            if (moved)
                EditorUtility.SetDirty(canvas);
        }

        /// <summary>
        /// The generated ordering is validated with the reviewed overlay, so a later edit
        /// cannot append the root back above the M6 title, above the M02 page, above the M03
        /// inspector or above the M06 tactical modal, and an updater run in another order
        /// cannot silently invert the band.
        /// </summary>
        private static bool Ordered(Transform canvas, Transform root, out string error)
        {
            error = null;
            int index = root.GetSiblingIndex();
            Transform tactical = canvas.Find(TacticalOverlayName);
            if (tactical != null)
            {
                if (tactical.GetSiblingIndex() != canvas.childCount - 1)
                    return Invalid(out error, TacticalLastError);
                if (index > tactical.GetSiblingIndex())
                    return Invalid(out error, TacticalOrderError);
            }

            Transform title = canvas.Find(TitleRootName);
            if (title != null)
            {
                if (index >= title.GetSiblingIndex())
                    return Invalid(out error, TitleOrderError);

                Transform eventRoot = canvas.Find(EventChoiceRootName);
                if (eventRoot != null &&
                    title.GetSiblingIndex() != eventRoot.GetSiblingIndex() - 1)
                    return Invalid(out error, EventOrderError);
            }

            Transform dailyAction = canvas.Find(CanvasLayerOrder.DailyActionRootName);
            if (dailyAction != null && index <= dailyAction.GetSiblingIndex())
                return Invalid(out error, CanvasLayerOrder.CampaignReviewAboveDailyActionError);

            Transform inspector = canvas.Find(CanvasLayerOrder.InspectorRootName);
            if (inspector != null && index <= inspector.GetSiblingIndex())
                return Invalid(out error, CanvasLayerOrder.CampaignReviewAboveInspectorError);

            // The review's own slot can be right while another pair in the band is inverted;
            // the shared check keeps the whole band on one contract, not just this root.
            return CanvasLayerOrder.ValidateBand(canvas, out error);
        }

        private static Vector2 CardSize(Vector2 design) => new Vector2(
            Mathf.Min(ReferenceCardWidth, design.x - PageMargin * 2f),
            Mathf.Min(ReferenceCardHeight, design.y - PageMargin * 2f));

        /// <summary>
        /// The Canvas RectTransform can still be zero-sized in edit mode until the Canvas
        /// lays out. Fall back to the CanvasScaler reference resolution so generation does
        /// not depend on the current game-view size.
        /// </summary>
        private static Vector2 ResolveDesignSize(RectTransform canvas)
        {
            if (canvas != null && canvas.rect.width >= 320f && canvas.rect.height >= 240f)
                return canvas.rect.size;
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            if (scaler != null && scaler.referenceResolution.x >= 320f &&
                scaler.referenceResolution.y >= 240f)
                return scaler.referenceResolution;
            return new Vector2(1280f, 720f);
        }

        private static UpdateResult Resolve(Scene scene, out Transform canvas, out GameSession session)
        {
            canvas = null;
            session = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;
            Transform[] canvases = Named(scene, "Canvas");
            Transform[] maps = Named(scene, "M2WorldMapRoot");
            Canvas[] canvasComponents = Components<Canvas>(scene);
            GameSession[] sessions = Components<GameSession>(scene);
            if (canvases.Length != 1 || maps.Length != 1 || canvasComponents.Length != 1 ||
                sessions.Length != 1)
                return UpdateResult.MissingOrDuplicateObjects;
            canvas = canvases[0];
            session = sessions[0];
            if (canvasComponents[0].transform != canvas || !(canvas is RectTransform) ||
                maps[0].parent != canvas)
                return UpdateResult.InvalidHierarchyOrBindings;
            return UpdateResult.Success;
        }

        private static void AddPreferredHeight(GameObject target, float height)
        {
            LayoutElement element = target.GetComponent<LayoutElement>();
            if (element == null)
                element = Undo.AddComponent<LayoutElement>(target);
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private static bool Stretch(RectTransform rect) =>
            rect != null &&
            rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one &&
            Mathf.Abs(rect.offsetMin.x) < 0.5f && Mathf.Abs(rect.offsetMin.y) < 0.5f &&
            Mathf.Abs(rect.offsetMax.x) < 0.5f && Mathf.Abs(rect.offsetMax.y) < 0.5f;

        /// <summary>One authored viewport inset, within the tolerance a saved scene keeps.</summary>
        private static bool Inset(Vector2 actual, float x, float y) =>
            Mathf.Abs(actual.x - x) < 0.5f && Mathf.Abs(actual.y - y) < 0.5f;

        /// <summary>
        /// The scroll content grows downward from the top of the viewport. The panel resets a
        /// new review body to anchored Y zero, which is the first line only while this holds.
        /// </summary>
        private static bool TopAnchored(RectTransform rect) =>
            rect != null &&
            Mathf.Abs(rect.anchorMin.x) < 0.001f && Mathf.Abs(rect.anchorMin.y - 1f) < 0.001f &&
            Mathf.Abs(rect.anchorMax.x - 1f) < 0.001f && Mathf.Abs(rect.anchorMax.y - 1f) < 0.001f &&
            Mathf.Abs(rect.pivot.x - 0.5f) < 0.001f && Mathf.Abs(rect.pivot.y - 1f) < 0.001f;

        private static Text ChildText(Transform parent, string name)
        {
            Transform found = parent != null ? parent.Find(name) : null;
            return found != null ? found.GetComponent<Text>() : null;
        }

        private static Text FindText(Transform parent, string name)
        {
            Transform found = FindDeep(parent, name);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private static Button FindButton(Transform parent, string name)
        {
            Transform found = FindDeep(parent, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null)
                return null;
            Transform[] all = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name)
                    return all[i];
            return null;
        }

        private static bool Bound(UnityEngine.Object target, string field,
            UnityEngine.Object expected)
        {
            if (target == null || expected == null)
                return false;
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && property.objectReferenceValue == expected;
        }

        private static UnityEngine.Object Reference(UnityEngine.Object target, string field)
        {
            if (target == null)
                return null;
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null ? property.objectReferenceValue : null;
        }

        /// <summary>
        /// The scene's title controller, when the M6 generator has already run. It is the only
        /// owner of the scenario selector, so the review borrows it instead of starting a
        /// campaign itself. A scene without one still generates, and the panel resolves the
        /// reference at runtime before the action is first used.
        /// </summary>
        private static TitleScreenController TitleScreen(Scene scene)
        {
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            return controllers.Length == 1 ? controllers[0] : null;
        }

        /// <summary>WCAG 1.4.3's normal-text ratio, used for the 14 px status line.</summary>
        private static bool MeetsNormalTextContrast(Color foreground, Color background) =>
            ContrastRatio(foreground, background) >= 4.5f;

        private static float ContrastRatio(Color first, Color second)
        {
            float high = Mathf.Max(RelativeLuminance(first), RelativeLuminance(second));
            float low = Mathf.Min(RelativeLuminance(first), RelativeLuminance(second));
            return (high + 0.05f) / (low + 0.05f);
        }

        private static float RelativeLuminance(Color color) =>
            0.2126f * LinearChannel(color.r) + 0.7152f * LinearChannel(color.g) +
            0.0722f * LinearChannel(color.b);

        private static float LinearChannel(float channel) => channel <= 0.04045f
            ? channel / 12.92f
            : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);

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
            // GameObject sits at the world origin, so a layout-driven child (the button row)
            // would keep that offset from the centred card until a layout pass runs. Validation
            // happens before any canvas update, so start every child at its parent's origin.
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components)
                Undo.AddComponent(go, component);
            return go;
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font,
            int size, Vector2 dimensions, TextAnchor alignment, Color color, string value)
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
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
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = SengokuUiTheme.Vermilion;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(scene, rect, "Label", font, fontSize,
                new Vector2(size.x - 12f, size.y - 6f), TextAnchor.MiddleCenter,
                SengokuUiTheme.ParchmentLight, label);
            text.raycastTarget = false;
            return button;
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
    }
}
