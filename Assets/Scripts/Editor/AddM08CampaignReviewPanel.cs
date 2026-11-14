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
    /// scrollable sourced-history body, one continue button and the status line. The
    /// page starts hidden; CampaignReviewPanel shows it only while a real 55-year or
    /// 100-year review is pending. Everything is created in one Undo group, validated
    /// before saving, and rolled back through Undo on failure without reloading the scene.
    /// An existing page whose only defect is its Canvas order is moved back under the M6
    /// title in one undoable step instead of being rebuilt.
    /// </summary>
    public static class AddM08CampaignReviewPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = CampaignReviewPanel.RootObjectName;
        // Canvas siblings share the M6 title overlay's ordering contract: Unity renders
        // later siblings on top, so the full-screen title and the tactical modal are the
        // only overlays allowed above everything else on the Canvas.
        public const string TitleRootName = "M6TitleRoot";
        public const string EventChoiceRootName = "EventChoiceRoot";
        public const string TacticalOverlayName = TacticalMissionPanel.OverlayObjectName;
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
                return RepairableOrderError(error)
                    ? RepairOrdering(scene, canvas, save)
                    : UpdateResult.ExistingCampaignPanel;
            }

            RectTransform canvasRect = canvas as RectTransform;
            Vector2 design = ResolveDesignSize(canvasRect);
            if (design.x < 960f || design.y < 540f)
                return UpdateResult.InsufficientSpace;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
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
                card.sizeDelta = CardSize(design);
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
                    new Vector2(1000f, 20f), TextAnchor.MiddleLeft, SengokuUiTheme.GoldDim,
                    string.Empty);
                AddPreferredHeight(status.gameObject, 20f);

                GameObject rowObject = Create(scene, card, ButtonRowName, typeof(LayoutElement));
                rowObject.GetComponent<LayoutElement>().preferredHeight = 52f;
                Button cont = CreateButton(scene, rowObject.transform, ContinueButtonName, font,
                    "继续家族", new Vector2(380f, 42f), 16);
                Text label = cont.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.name = ContinueLabelName;

                CampaignReviewPanel panel = Undo.AddComponent<CampaignReviewPanel>(root);
                panel.BindReferences(session, pageObject, title, context, body, label, status, cont);
                pageObject.SetActive(false);
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
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogWarning($"M08 campaign review updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

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

            if (title.text.Length == 0 || context.text.Length == 0)
                return Invalid(out error,
                    "Campaign review header text must be initialized.");
            return true;
        }

        /// <summary>
        /// Unity renders later Canvas siblings above earlier ones, so the review overlay must
        /// sit under the M6 title overlay. AddM6TitleScreen.Validate requires the title to be
        /// the sibling immediately below EventChoiceRoot and allows nothing else above it, and
        /// AddM06TacticalMissionPanel.Validate requires the tactical overlay to stay last.
        /// The root is created at the end of the Canvas, so this moves it under the
        /// title/event pair instead of leaving it above the title and the tactical modal.
        /// </summary>
        private static void PlaceBelowTitleOverlay(Transform canvas, Transform root) =>
            root.SetSiblingIndex(ResolveOrderIndex(canvas));

        /// <summary>
        /// Canvas index the review root renders at: below the M6 title overlay, or directly
        /// below the M06 tactical overlay when no title exists. Creating the root last and
        /// moving it here shifts the title and EventChoiceRoot above it one step up together.
        /// </summary>
        private static int ResolveOrderIndex(Transform canvas)
        {
            Transform title = canvas.Find(TitleRootName);
            Transform tactical = canvas.Find(TacticalOverlayName);
            int index = title != null ? title.GetSiblingIndex()
                : tactical != null ? tactical.GetSiblingIndex()
                : canvas.childCount - 1;
            return Mathf.Clamp(index, 0, canvas.childCount - 1);
        }

        /// <summary>
        /// Only a misplaced review root can be repaired. Title/EventChoiceRoot adjacency is
        /// reported separately because moving the review root cannot fix it.
        /// </summary>
        private static bool RepairableOrderError(string error) =>
            error == TacticalLastError || error == TacticalOrderError || error == TitleOrderError;

        /// <summary>
        /// A complete generated page saved by an older revision can carry nothing but the
        /// wrong Canvas order. Moving the existing root back under the M6 title in one Undo
        /// group repairs the scene without rebuilding it; anything else stays refused, and a
        /// failed re-validation restores the previous order.
        /// </summary>
        private static UpdateResult RepairOrdering(Scene scene, Transform canvas, bool save)
        {
            Transform[] roots = Named(scene, RootName);
            CampaignReviewPanel[] panels = Components<CampaignReviewPanel>(scene);
            if (roots.Length != 1 || panels.Length != 1 ||
                panels[0].transform != roots[0] || roots[0].parent != canvas)
                return UpdateResult.ExistingCampaignPanel;

            Transform root = roots[0];
            int originalIndex = root.GetSiblingIndex();
            int targetIndex = ResolveOrderIndex(canvas);
            if (targetIndex == originalIndex)
                return UpdateResult.ExistingCampaignPanel;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.ExistingCampaignPanel;
            try
            {
                // Sibling order lives in the parent RectTransform, so record the parent too.
                Undo.RegisterCompleteObjectUndo(canvas, UndoName);
                root.SetSiblingIndex(targetIndex);
                Undo.FlushUndoRecordObjects();
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
                if (root.GetSiblingIndex() != originalIndex)
                {
                    root.SetSiblingIndex(originalIndex);
                    EditorUtility.SetDirty(root);
                }
                Debug.LogWarning($"M08 campaign review updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        /// <summary>
        /// The generated ordering is validated with the reviewed overlay, so a later edit
        /// cannot append the root back above the M6 title or the M06 tactical modal.
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
            if (title == null)
                return true;
            if (index >= title.GetSiblingIndex())
                return Invalid(out error, TitleOrderError);

            Transform eventRoot = canvas.Find(EventChoiceRootName);
            if (eventRoot != null &&
                title.GetSiblingIndex() != eventRoot.GetSiblingIndex() - 1)
                return Invalid(out error, EventOrderError);
            return true;
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
