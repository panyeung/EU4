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
    /// M08 战役回顾覆盖层的事务型生成器。它在既有 Canvas 之下建一个固定
    /// 根节点：全屏页面、居中卡片、可滚动的来源记录正文、继续动作、
    /// 打开标题覆盖层选择器的新剧本动作，以及状态行。页面初始隐藏；
    /// CampaignReviewPanel 只在真的有 55 年或 100 年回顾待处理时显示它。
    /// 所有对象都在同一个 Undo 组里创建，保存前先校验，失败时通过
    /// Undo 回滚且不重载场景。既有页面若只有 Canvas 次序一项缺陷，
    /// 就在一个可撤销步骤里移回 M6 标题之下，而不是重建。
    /// </summary>
    public static class AddM08CampaignReviewPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = CampaignReviewPanel.RootObjectName;
        // Canvas 兄弟节点共用一条覆盖层带（CanvasLayerOrder）：战役回顾渲染在 M02 页面与
        // M03 查看器之上，并在全屏标题、EventChoiceRoot 与最后一个 M06 战术弹窗之下。
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
        // 动作行几何。两个动作并排、一起居中，各自在任何布局
        // 计算之前就留在卡片内。
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
                // 两动作形态之前生成的页面没有新剧本入口，
                // 状态行也早于可读墨色。只替换这一已知的生成层级，
                // 旧场景无需手工删除即可升级；其它非法页面仍走
                // 下面的拒绝路径。
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
        /// 在同一个 Undo 组内创建整套回顾层级，或替换掉旧的生成页面。
        /// 替换范围限于 <see cref="IsLegacyGeneratedPage"/>，因此回顾根节点
        /// 之外的东西不会被删除。校验之前先修复共享覆盖层带区，
        /// 因为旧更新器保存的页面或继承根节点可能坐在错误的槽位。
        /// 失败时回滚整个 Undo 组，并恢复尝试前的 Canvas 子节点次序。
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
                // 兄弟次序保存在父 RectTransform 上，因此父节点也要记录。
                Undo.RegisterCompleteObjectUndo(canvas, UndoName);
                if (replaceExisting)
                {
                    Transform legacy = canvas.Find(RootName);
                    if (legacy == null)
                        throw new InvalidOperationException(
                            "旧的战役回顾根节点在被替换前已消失。");
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

        /// <summary>在既有 Canvas 之下构建生成的回顾层级。</summary>
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
                throw new InvalidOperationException("缺少内置 UI 字体。");

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
            // 正文 Text 是第一个区块模板。CampaignReviewPanel 按块克隆它，
            // 因为单个 Text 渲染不了 89,212 字符的 55 年回顾：网格超出
            // uGUI CanvasRenderer 上限，整个视口会渲染成空白。
            // 内容的 VerticalLayoutGroup 按正文顺序、以 Text 自身的首选高度
            // 堆叠各区块。

            Text status = CreateText(scene, card, StatusTextName, font, 14,
                new Vector2(1000f, 20f), TextAnchor.MiddleLeft,
                CampaignReviewPanel.StatusTextColor, string.Empty);
            AddPreferredHeight(status.gameObject, 20f);

            GameObject rowObject = Create(scene, card, ButtonRowName, typeof(LayoutElement));
            RectTransform row = rowObject.GetComponent<RectTransform>();
            // 运行时由卡片的 VerticalLayoutGroup 掌管这一行。预先给出尺寸，
            // 使包含性检查在任何 Canvas 布局计算之前就能确认两个动作
            // 留在卡片内。
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
        /// 仅当页面是新剧本动作与可读状态墨色出现之前由本生成器产出的形态时为 true：
        /// 已知层级完整，且根节点下每个对象都带生成名。这一狭窄匹配
        /// 让更新器无需手工删除即可替换旧的工具生成页面，同时拒绝
        /// 用户添加过对象的页面。已经带有新剧本动作的页面绝不重建，
        /// 因此那里的结构缺陷仍保持既有拒绝。
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

        /// <summary>生成器在回顾根节点内拥有的全部名称。</summary>
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

            // 滚动条在正文滚动区自己的场景里运行时创建。若保存的页面
            // 已经有一个，它必须是带手柄的 24 px 生成列，且视口必须
            // 预留该列，使滚动条永不盖住换行文字。
            RectTransform viewportRect = (RectTransform)viewport;
            bool authoredInset =
                Inset(viewportRect.offsetMin, 8f, 6f) && Inset(viewportRect.offsetMax, -8f, -6f);
            bool reservedInset =
                Inset(viewportRect.offsetMin, 8f, 6f) &&
                Inset(viewportRect.offsetMax,
                    -(2f + CampaignReviewPanel.ScrollbarWidth + 8f), -6f);
            if (!authoredInset && !reservedInset)
                return Invalid(out error,
                    "战役回顾正文视口必须保留原始内边距，或预留 " +
                    "24 px 的滚动条列。");
            Scrollbar authoredBar = scrollComponent.verticalScrollbar;
            if (authoredBar != null)
            {
                RectTransform barRect = authoredBar.transform as RectTransform;
                if (authoredBar.transform.parent != scroll || barRect == null ||
                    Mathf.Abs(barRect.rect.width - CampaignReviewPanel.ScrollbarWidth) > 0.01f ||
                    authoredBar.handleRect == null ||
                    authoredBar.handleRect.parent != barRect)
                    return Invalid(out error,
                        "战役回顾正文滚动条必须是滚动区内带手柄的 " +
                        "24 px 列。");
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
                    "战役回顾新剧本按钮或标签缺失、未绑定，或位于" +
                    "卡片之外。");

            TitleScreenController[] titles = Components<TitleScreenController>(scene);
            if (titles.Length > 1)
                return Invalid(out error,
                    "最多只允许一个 TitleScreenController 掌管剧本选择器。");
            if (titles.Length == 1)
            {
                if (!Bound(panel, "titleScreenController", titles[0]))
                    return Invalid(out error,
                        "战役回顾新剧本动作必须指向场景中的 " +
                        "TitleScreenController。");
            }
            else if (Reference(panel, "titleScreenController") != null)
                return Invalid(out error,
                    "场景没有 TitleScreenController 时，战役回顾的标题引用" +
                    "必须保持为空。");

            // WCAG 1.4.3：14 px 状态行用于报告被拒绝的操作，因此相对原始卡片
            // 需要 4.5:1 的正文对比度，而不是大字的 3:1。
            if (!MeetsNormalTextContrast(status.color, card.GetComponent<Image>().color))
                return Invalid(out error,
                    "战役回顾状态文字相对卡片的对比度必须达到 4.5:1。");

            if (title.text.Length == 0 || context.text.Length == 0)
                return Invalid(out error,
                    "Campaign review header text must be initialized.");
            return true;
        }

        /// <summary>
        /// Unity 让 Canvas 中靠后的兄弟节点渲染在靠前的之上，因此战役回顾覆盖层必须位于
        /// M02 页面与 M03 查看器之上、待处理的继承决定之下，并在 M6 标题覆盖层之下。
        /// <see cref="AddM6TitleScreen"/>.Validate 要求标题紧邻 EventChoiceRoot 之下且其上不得有
        /// 其它节点，<see cref="AddM06TacticalMissionPanel"/>.Validate 要求战术覆盖层保持最后。
        /// 根节点建在 Canvas 末尾，这里把它放进 <see cref="CanvasLayerOrder"/> 的座次槽位，
        /// 不移动周围的根节点。
        /// </summary>
        private static void PlaceBelowTitleOverlay(Transform canvas, Transform root) =>
            CanvasLayerOrder.PlaceAtRank(canvas, root);

        /// <summary>
        /// 旧版本保存的完整生成页面，可能唯一的缺陷就是 Canvas 次序错误。
        /// 只有放错位置的根节点可以修复。标题/EventChoiceRoot 相邻性问题
        /// 与兄弟根节点错位会分别报告，因为只移动回顾根节点修不好它们；
        /// 修复流程在报告成功之前会校验整条共享带区
        /// （M02、M03、SC-010、标题、事件选择、战术弹窗）。
        /// </summary>
        private static bool RepairableOrderError(string error) =>
            error == TacticalLastError || error == TacticalOrderError || error == TitleOrderError ||
            error == CanvasLayerOrder.CampaignReviewAboveDailyActionError ||
            error == CanvasLayerOrder.CampaignReviewAboveInspectorError ||
            (error != null && error.StartsWith(CanvasLayerOrder.RankOrderError,
                StringComparison.Ordinal));

        /// <summary>
        /// 旧版本保存的完整生成页面，可能唯一的缺陷就是 Canvas 次序错误。
        /// 在一个 Undo 组内恢复共享覆盖层带区即可修复场景，无需重建。
        /// 只移动确实乱序的根节点，因此位置正确的 M02 页、M03 查看器、
        /// 继承根节点或 M06 弹窗都保留原位；其它情况仍被拒绝，
        /// 复校验失败会恢复准确的原先次序。即使带区名次已递增，
        /// 战术弹窗也最后放回，因为此时唯一缺陷可能就是它后面
        /// 保存了一个未排名根节点。
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

        /// <summary>Canvas 子节点次序的快照，用于回滚失败的修复。</summary>
        private static Transform[] Children(Transform canvas)
        {
            var children = new Transform[canvas.childCount];
            for (int i = 0; i < children.Length; i++)
                children[i] = canvas.GetChild(i);
            return children;
        }

        /// <summary>修复失败后把捕获的 Canvas 子节点次序放回。</summary>
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
        /// 生成的次序按受审覆盖层校验，因此后续编辑无法把根节点
        /// 追加回 M6 标题、M02 页、M03 查看器或 M06 战术弹窗之上，
        /// 别的运行顺序的更新器也无法悄悄颠倒带区。
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

            // 回顾自身的槽位可能正确，而带区里另一对次序相反；
            // 共享校验让整条带区守同一份约定，而不只是这个根节点。
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

        /// <summary>一个原始视口内边距，容忍保存场景的误差。</summary>
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
        /// M6 生成器已运行时，场景里的标题控制器。它是剧本选择器的唯一
        /// 所有者，因此回顾面板借用它，而不是自己开一局战役。
        /// 没有它的场景仍能生成，面板会在首次使用该动作之前
        /// 于运行时解析引用。
        /// </summary>
        private static TitleScreenController TitleScreen(Scene scene)
        {
            TitleScreenController[] controllers = Components<TitleScreenController>(scene);
            return controllers.Length == 1 ? controllers[0] : null;
        }

        /// <summary>WCAG 1.4.3 的正文对比度，用于 14 px 状态行。</summary>
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
