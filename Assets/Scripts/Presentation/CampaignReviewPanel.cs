using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M08 战役回顾覆盖层。只在真的有 55 年阶段回顾或 100 年百年名门回顾
    /// 待处理时出现，只渲染有来源的家族事实，并提供两个动作：继续，
    /// 以及打开标题覆盖层既有选择器的新剧本入口。确认归 GameSession 掌管；
    /// 本面板从不改动时间、资源或第一阶段 demo 的完成状态，
    /// 选择器也只是打开一个页面。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignReviewPanel : MonoBehaviour
    {
        /// <summary>Fixed scene root generated beneath the existing Canvas.</summary>
        public const string RootObjectName = "M08CampaignReviewRoot";
        /// <summary>Full-screen overlay page toggled by <see cref="Refresh"/>.</summary>
        public const string PageObjectName = "M08CampaignReviewPage";
        public const string CardObjectName = "M08CampaignReviewCard";
        /// <summary>Suffix that marks the second and later rendered body blocks.</summary>
        public const string BodyBlockNameSuffix = ".block.";
        /// <summary>
        /// 打开标题剧本选择器的次要动作。它自己从不启动剧本，
        /// 因此可以离开回顾去选择器，而不会重置正在运行的战役、
        /// 其日期、资源或存档槽。
        /// </summary>
        public const string NewScenarioButtonName = "M08CampaignReviewNewScenarioButton";
        public const string NewScenarioLabelName = "M08CampaignReviewNewScenarioLabel";
        /// <summary>新剧本动作的可见标题。</summary>
        public const string NewScenarioLabel = "开始新剧本";

        /// <summary>
        /// Largest character count one rendered body Text may hold. uGUI Text builds a
        /// quad per laid-out character, so a single Text is capped by the CanvasRenderer
        /// mesh limit long before its string ends. The live 55-year review produced
        /// 89,212 characters in one Text, 317,860 vertices, and a blank viewport because
        /// the mesh was rejected. 4,000 characters stay at 16,000 vertices, four times
        /// below the 65,535-vertex limit, while keeping the block count low.
        /// </summary>
        public const int MaxBodyBlockCharacters = 4000;

        /// <summary>Vertex budget one block may use at four vertices per character.</summary>
        public const int MaxBodyBlockVertices = MaxBodyBlockCharacters * 4;

        /// <summary>
        /// 适配后卡片的下限。卡片按 1120x640 设计，但 CanvasScaler 在其它
        /// 纵横比下给出的参考单位尺寸不同，因此卡片缩小，而不是裁掉标题行
        /// 或继续行。这是护栏而非保证：取 0.25 时卡片仍需 280x160 的参考页面。
        /// 在 CanvasScaler 1280x720、match 0.5 下，页面只取决于纵横比，
        /// 与分辨率无关：16:9 为 1280x720（1.00），16:10 为 1214x759（1.00），
        /// 4:3 为 1108x831（0.946），5:4 为 1073x859（0.915），
        /// 21:9 为 1478x623（0.899），32:9 为 1810x509（0.72）。
        /// 页面小于 280x160 会在此被夹住并可能贴边，因此宽于约 48:9 的纵横比
        /// 必须抬高此下限，而不能指望 24 px 边距。
        /// </summary>
        public const float MinimumCardScale = 0.25f;

        /// <summary>
        /// 适配后的卡片在任何纵横比下与页面边缘保持的可见边距。
        /// 卡片按页面减去该边距缩放，因此永不贴到屏幕边缘。
        /// </summary>
        public const float PageSafeMargin = 24f;

        /// <summary>
        /// 继续控件的键盘焦点状态：金色底版配深色墨字。原始朱红底版与其
        /// ColorTint 选中色（0.961）相差约 1.06:1，无法作为可用的焦点提示。
        /// 按钮会给图形着色，因此实际渲染的焦点底版约为 (187,158,101)：
        /// 该底版上的墨字对比度为 5.24:1，着色底版相对卡片为 5.52:1
        /// （未着色的主题配色为 5.67:1 与 5.97:1）。两者都满足 AA 正文
        /// 与 3:1 的焦点指示下限。两种颜色都已在共享主题里，
        /// 因此不改动任何全局设置。
        /// </summary>
        public static readonly Color FocusedButtonBackground = SengokuUiTheme.Gold;
        public static readonly Color FocusedButtonForeground = SengokuUiTheme.Ink;

        /// <summary>
        /// 状态行的墨色，14 px 下报告继续动作失败或通过。卡片按 IndigoPanel
        /// (34,43,61) 设计；Gold (195,164,105) 相对它测得 5.97:1，满足
        /// WCAG 1.4.3 的正文 4.5:1。原先的 GoldDim (142,122,81) 测得
        /// 3.41:1，于是这条解释操作被拒的唯一消息反而成了整个覆盖层
        /// 最难读的文字。Gold 本就是主题的标题墨色，因此不改动任何全局设置。
        /// </summary>
        public static readonly Color StatusTextColor = SengokuUiTheme.Gold;

        /// <summary>生成的滚动区没有滚动条时，回顾在运行时补上的滚动条。</summary>
        public const string ScrollbarObjectName = "M08CampaignReviewScrollbar";
        /// <summary>
        /// 回顾滚动条的完整指针目标。WCAG 2.2 2.5.8 要求 24x24 CSS px；
        /// 整根 24 px 列都可点击，而不只是绘制出的手柄。
        /// </summary>
        public const float ScrollbarWidth = 24f;

        [Header("Dependencies")]
        [SerializeField] private GameSession gameSession;
        // 剧本选择器的唯一所有者。M6 标题覆盖层存在时由场景生成器绑定，
        // 否则在首次使用该动作之前于此处解析一次。
        [SerializeField] private TitleScreenController titleScreenController;

        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text contextText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text continueLabelText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newScenarioButton;
        [SerializeField] private Text newScenarioLabelText;

        private bool _subscribed;
        private UnityAction _continueListener;
        private UnityAction _newScenarioListener;
        private int _staticButtonBindings;
        private readonly List<Text> _bodyBlocks = new List<Text>();
        private string _bodyFullText;
        private ScrollRect _bodyScroll;
        private RectTransform _cardRect;
        // 卡片上次适配的页面尺寸。LateUpdate 与实时页面矩形比较，
        // 因此回顾打开时窗口或画布缩放会重新适配卡片。
        private Vector2 _fittedPageSize = new Vector2(-1f, -1f);
        // 覆盖层取得焦点之前持有键盘焦点的选中对象，隐藏回顾时
        // 把焦点还回去，而不是留下一个被选中的隐藏动作。
        private GameObject _previousSelection;
        // 已捕获回顾前选中状态后为 true。null 是合法选中，
        // 因此捕获状态需要自己的标志位，不能靠 null 判断。
        private bool _previousSelectionCaptured;
        // 每个回顾动作的原始外观与导航，回顾释放焦点时恢复，
        // 之后任一按钮的键盘使用都不受影响。
        private readonly FocusState _continueFocus = new FocusState();
        private readonly FocusState _newScenarioFocus = new FocusState();
        private Scrollbar _bodyScrollbar;
        // 记录卡片缺失，免得回顾可见期间 LateUpdate 的重新适配
        // 每帧都跑一次 Transform.Find。
        private bool _cardLookupFailed;

        public bool IsBound =>
            gameSession != null && panelRoot != null && titleText != null &&
            contextText != null && bodyText != null && continueButton != null;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public string Title => titleText != null ? titleText.text : string.Empty;
        public string Context => contextText != null ? contextText.text : string.Empty;
        /// <summary>
        /// The whole review body in source order. It is the concatenation of every
        /// rendered block, so no character of the formatted review is dropped.
        /// </summary>
        public string Body => _bodyFullText ?? (bodyText != null ? bodyText.text : string.Empty);
        public string ContinueLabel => continueLabelText != null ? continueLabelText.text : string.Empty;
        public string Status => statusText != null ? statusText.text : string.Empty;
        public int StaticButtonBindingCount => _staticButtonBindings;
        /// <summary>Number of Text blocks the current body is rendered with.</summary>
        public int BodyBlockCount => _bodyBlocks.Count;
        /// <summary>套用到卡片上的统一缩放，使卡片留在页面内。</summary>
        public float CardScale => Card != null ? Card.localScale.x : 1f;
        /// <summary>绑定到回顾正文的滚动条，首次显示时若缺失则创建。</summary>
        public Scrollbar BodyScrollbar => BodyScroll != null ? BodyScroll.verticalScrollbar : null;
        /// <summary>
        /// 继续控件持有回顾可见键盘焦点期间为 true：
        /// 它是当前选中对象且已应用金色焦点呈现。
        /// 他人设置但未应用该呈现的选中不算。
        /// </summary>
        public bool IsContinueFocused
        {
            get
            {
                EventSystem system = EventSystem.current;
                return _continueFocus.Applied && system != null && continueButton != null &&
                       system.currentSelectedGameObject == continueButton.gameObject;
            }
        }

        /// <summary>
        /// 新剧本动作持有回顾可见键盘焦点期间为 true，
        /// 判定规则同 <see cref="IsContinueFocused"/>。
        /// </summary>
        public bool IsNewScenarioFocused
        {
            get
            {
                EventSystem system = EventSystem.current;
                return _newScenarioFocus.Applied && system != null && newScenarioButton != null &&
                       system.currentSelectedGameObject == newScenarioButton.gameObject;
            }
        }

        public void BindReferences(GameSession session, GameObject panel, Text title, Text context,
            Text body, Text continueLabel, Text status, Button continueControl,
            Button newScenarioControl, Text newScenarioLabel, TitleScreenController titleScreen)
        {
            Unsubscribe();
            ReleaseReviewFocus();
            gameSession = session;
            panelRoot = panel;
            _cardRect = null;
            _cardLookupFailed = false;
            _bodyScroll = null;
            _bodyScrollbar = null;
            _fittedPageSize = new Vector2(-1f, -1f);
            titleText = title;
            contextText = context;
            bodyText = body;
            continueLabelText = continueLabel;
            statusText = status;
            continueButton = continueControl;
            newScenarioButton = newScenarioControl;
            newScenarioLabelText = newScenarioLabel;
            titleScreenController = titleScreen;
            // Refresh 是运行时路径：它适配卡片、绘制键盘焦点并渲染回顾
            // 文字与正文区块。场景生成器在 EditMode 绑定后保存，因此先
            // 快照原始状态，编辑器不在播放时再放回去。否则带待处理回顾的
            // 绑定会把当前游戏视图纵横比烘进保存的卡片缩放、把金色底版
            // 烘进原始按钮颜色，还会把回顾文字与克隆的正文区块写进原始场景。
            RectTransform card = Card;
            Vector3 authoredCardScale = card != null ? card.localScale : Vector3.one;
            string authoredTitle = titleText != null ? titleText.text : null;
            string authoredContext = contextText != null ? contextText.text : null;
            string authoredBody = bodyText != null ? bodyText.text : null;
            string authoredLabel = continueLabelText != null ? continueLabelText.text : null;
            string authoredStatus = statusText != null ? statusText.text : null;
            Subscribe();
            Refresh();
            if (!UnityEngine.Application.isPlaying)
            {
                ReleaseReviewFocus();
                if (card != null)
                    card.localScale = authoredCardScale;
                RestoreAuthoredText(authoredTitle, authoredContext, authoredBody,
                    authoredLabel, authoredStatus);
                _fittedPageSize = new Vector2(-1f, -1f);
            }
        }

        /// <summary>
        /// EditMode 绑定后把原始 Text 值与单个正文区块模板放回，
        /// 使生成器运行不会把渲染后的回顾内容或克隆的正文区块存进场景。
        /// </summary>
        private void RestoreAuthoredText(string title, string context, string body,
            string label, string status)
        {
            EnsureBodyBlocks(1);
            if (titleText != null && title != null)
                titleText.text = title;
            if (contextText != null && context != null)
                contextText.text = context;
            if (bodyText != null && body != null)
                bodyText.text = body;
            _bodyFullText = body;
            if (continueLabelText != null && label != null)
                continueLabelText.text = label;
            if (statusText != null && status != null)
                statusText.text = status;
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            if (gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseReviewFocus();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!IsVisible)
                return;
            RefitIfPageResized();
            TrapReviewFocus();
            HandleFocusKeyboardNavigation();
            HandleBodyKeyboardScroll();
        }

        /// <summary>Rebuilds the overlay from the current pending review; never mutates state.</summary>
        public void Refresh()
        {
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                ReleaseReviewFocus();
                return;
            }

            CampaignReviewView review = gameSession.CampaignReview;
            if (!review.IsAvailable)
            {
                panelRoot.SetActive(false);
                ReleaseReviewFocus();
                return;
            }

            titleText.text = review.Title;
            contextText.text = review.ContextLine + "｜评价日：" + review.ReviewDateLine;
            RenderBody(review.Subtitle + "\n\n" + review.FormatBody());
            if (continueLabelText != null)
                continueLabelText.text = review.ContinueLabel;
            SetStatus(gameSession.LastCampaignReviewMessage);
            panelRoot.SetActive(true);
            if (UnityEngine.Application.isPlaying)
                EnsureBodyScrollbar();
            FitCardToPage();
            FocusReviewControl(continueButton);
        }

        /// <summary>
        /// Renders one body into the vertically scrolling content as bounded Text blocks.
        /// The generated body Text is the first block and the panel clones it for the rest,
        /// so every block shares the reviewed font, size and colour and no single Text mesh
        /// grows past <see cref="MaxBodyBlockCharacters"/>. An unchanged body is left alone
        /// so later state notifications do not rebuild the blocks.
        /// </summary>
        internal void RenderBody(string body)
        {
            string text = body ?? string.Empty;
            if (bodyText == null)
                return;
            bool blocksMatchTemplate = _bodyBlocks.Count > 0 && _bodyBlocks[0] == bodyText;
            if (_bodyFullText == text && blocksMatchTemplate)
                return;

            IReadOnlyList<string> blocks = SplitBody(text, MaxBodyBlockCharacters);
            EnsureBodyBlocks(blocks.Count);
            _bodyFullText = text;
            for (int i = 0; i < _bodyBlocks.Count; i++)
            {
                Text block = _bodyBlocks[i];
                if (block == null)
                    continue;
                block.text = blocks[i];
                block.gameObject.SetActive(true);
            }
            ResetBodyScroll();
        }

        /// <summary>
        /// Puts a newly rendered body back at its first line. The 100-year review follows an
        /// acknowledged 55-year review in the same scroll, so keeping the old offset would open
        /// it partway down. The generated content is anchored and pivoted at the top, where an
        /// anchored Y of zero is exactly the first line.
        /// </summary>
        private void ResetBodyScroll()
        {
            ScrollRect scroll = BodyScroll;
            if (scroll == null)
                return;
            scroll.StopMovement();
            if (scroll.content == null)
                return;
            Vector2 position = scroll.content.anchoredPosition;
            position.y = 0f;
            scroll.content.anchoredPosition = position;
        }

        private ScrollRect BodyScroll
        {
            get
            {
                if (_bodyScroll == null && bodyText != null)
                    _bodyScroll = bodyText.GetComponentInParent<ScrollRect>(true);
                return _bodyScroll;
            }
        }

        /// <summary>承载标题、正文与继续行的卡片。</summary>
        private RectTransform Card
        {
            get
            {
                if (_cardRect == null && !_cardLookupFailed && panelRoot != null)
                {
                    _cardRect = panelRoot.transform.Find(CardObjectName) as RectTransform;
                    // 记住未命中，免得被改名或移走的卡片在回顾打开期间
                    // 每帧都付一次 Transform.Find。
                    _cardLookupFailed = _cardRect == null;
                }
                return _cardRect;
            }
        }

        /// <summary>
        /// 让原始卡片留在给定页面尺寸内的统一缩放。绝不超过 1，
        /// 因为原始卡片就是参考布局。CanvasScaler 在其它纵横比下
        /// 给画布的页面更窄或更矮，因此卡片缩小，
        /// 而不是裁掉标题行或继续行。
        /// </summary>
        public static float FitScale(Vector2 availableSize, Vector2 cardSize)
        {
            if (availableSize.x <= 0f || availableSize.y <= 0f ||
                cardSize.x <= 0f || cardSize.y <= 0f)
                return 1f;
            float scale = Mathf.Min(availableSize.x / cardSize.x, availableSize.y / cardSize.y);
            return Mathf.Clamp(scale, MinimumCardScale, 1f);
        }

        /// <summary>
        /// 把卡片适配到当前页面，四周都保留 <see cref="PageSafeMargin"/>。
        /// 1280x720 参考尺寸下卡片本就合适，缩放为 1。这也是缩放窗口的路径：
        /// 回顾打开时页面尺寸变化会重新适配。
        /// </summary>
        public void FitCardToPage()
        {
            if (panelRoot == null || !(panelRoot.transform is RectTransform page))
                return;
            Vector2 pageSize = page.rect.size;
            if (pageSize.x <= 0f || pageSize.y <= 0f)
                return;
            RectTransform card = Card;
            if (card == null)
                return;
            Vector2 authored = card.rect.size;
            if (authored.x <= 0f || authored.y <= 0f)
                return;
            _fittedPageSize = pageSize;
            Vector2 inset = new Vector2(
                Mathf.Max(1f, pageSize.x - PageSafeMargin * 2f),
                Mathf.Max(1f, pageSize.y - PageSafeMargin * 2f));
            float scale = FitScale(inset, authored);
            card.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// 页面尺寸变化后重新适配卡片，
        /// 使窗口或画布缩放期间打开的回顾仍留在页面内。
        /// </summary>
        private void RefitIfPageResized()
        {
            if (panelRoot == null || !(panelRoot.transform is RectTransform page))
                return;
            if ((page.rect.size - _fittedPageSize).sqrMagnitude <= 0.01f)
                return;
            FitCardToPage();
        }

        /// <summary>
        /// 生成的滚动区没有滚动条时，补上一列可见滚动条。场景生成器不生成它，
        /// 而回顾正文远高于视口，没有它玩家就看不出记录还有后续。
        /// 滚动条在右边距里预留自己的列，因此永不盖住换行文字。
        /// <para>
        /// 无障碍：整根 24 px 列都是指针目标，满足 WCAG 2.2
        /// 2.5.8（24x24），即使绘制出的手柄可能更短；Page Up、Page Down、
        /// Home 与 End 是对应的键盘操作。PaperLine 轨道叠在滚动区
        /// ParchmentDeep 0.16 背景上约为 (174,160,133)，对比度 3.67:1；
        /// Ink 手柄相对该轨道为 5.22:1，两部分都满足 1.4.11 的 3:1。
        /// 滚动条创建在滚动区自己的场景里，而不是活动场景，因此调用它的
        /// EditMode 夹具不会弄脏用户的原始场景。
        /// </para>
        /// <see cref="Refresh"/> 只在播放时调用它：在场景生成器的 EditMode
        /// 阶段创建对象会绕过它的 Undo 组。EditMode 夹具直接调用本方法。
        /// </summary>
        public void EnsureBodyScrollbar()
        {
            ScrollRect scroll = BodyScroll;
            if (scroll == null)
                return;
            if (scroll.verticalScrollbar != null)
            {
                _bodyScrollbar = scroll.verticalScrollbar;
                return;
            }

            const float margin = 2f;
            const float gap = 8f;
            const float inset = 6f;
            Scene scene = scroll.gameObject.scene;

            if (scroll.viewport is RectTransform viewportRect)
            {
                Vector2 offsetMax = viewportRect.offsetMax;
                float reserved = -(margin + ScrollbarWidth + gap);
                offsetMax.x = Mathf.Min(offsetMax.x, reserved);
                viewportRect.offsetMax = offsetMax;
            }

            GameObject trackObject = CreateInScene(scene, ScrollbarObjectName,
                typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            trackObject.transform.SetParent(scroll.transform, false);
            RectTransform track = (RectTransform)trackObject.transform;
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = new Vector2(1f, 1f);
            track.pivot = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(ScrollbarWidth, -inset * 2f);
            track.anchoredPosition = new Vector2(-margin, 0f);
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.color = SengokuUiTheme.WithAlpha(SengokuUiTheme.PaperLine, 0.8f);
            trackImage.raycastTarget = true;

            GameObject handleObject = CreateInScene(scene, "Handle", typeof(RectTransform),
                typeof(Image));
            handleObject.transform.SetParent(track, false);
            RectTransform handle = (RectTransform)handleObject.transform;
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = SengokuUiTheme.Ink;
            handleImage.raycastTarget = true;

            Scrollbar bar = trackObject.GetComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handle;
            bar.targetGraphic = trackImage;
            bar.transition = Selectable.Transition.None;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };
            bar.value = 1f;

            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 0f;
            _bodyScrollbar = bar;
        }

        /// <summary>
        /// 在给定场景内创建 GameObject。直接用普通构造函数会先把它放进
        /// 活动场景，只有之后重设父级才移走；即使对象已经离开，
        /// 编辑器里的活动场景仍会被标脏。
        /// </summary>
        private static GameObject CreateInScene(Scene scene, string name, params Type[] components)
        {
            var created = new GameObject(name, components);
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        /// <summary>
        /// 按带符号的像素量滚动正文，并夹在内容范围内。生成的内容顶部
        /// 锚定且轴心在上，Y 为零即第一行，Y 越大越往下滚。
        /// </summary>
        public void ScrollBodyBy(float delta)
        {
            ScrollRect scroll = BodyScroll;
            if (scroll == null || scroll.content == null || scroll.viewport == null)
                return;
            SetBodyOffset(scroll.content.anchoredPosition.y + delta);
        }

        /// <summary>
        /// 把正文移到距第一行的绝对像素偏移，并夹在内容范围内。
        /// Home 与 End 直接使用它，因此即使滚动组件之后不再夹住
        /// 自身位移，它们也能落到两端。
        /// </summary>
        private void SetBodyOffset(float offset)
        {
            ScrollRect scroll = BodyScroll;
            if (scroll == null || scroll.content == null || scroll.viewport == null)
                return;
            float max = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
            scroll.StopMovement();
            Vector2 position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(offset, 0f, max);
            scroll.content.anchoredPosition = position;
        }

        /// <summary>
        /// 覆盖层掌管键盘输入期间，Page Up、Page Down、Home 与 End
        /// 滚动回顾正文。上下方向键故意不绑定：项目把它们绑到玩法
        /// Move 动作，而 PlayerMovement2D 读取该动作时没有 UI 门禁，
        /// 绑在这里会让角色在覆盖层背后走动。更高优先级的覆盖层
        /// 会自己保留这些按键。
        /// </summary>
        private void HandleBodyKeyboardScroll()
        {
            if (HasHigherPriorityOverlay())
                return;
            Keyboard keyboard = Keyboard.current;
            ScrollRect scroll = BodyScroll;
            if (keyboard == null || scroll == null || scroll.content == null ||
                scroll.viewport == null)
                return;

            float page = Mathf.Max(24f, scroll.viewport.rect.height * 0.9f);
            if (keyboard.pageUpKey.wasPressedThisFrame)
                SetBodyOffset(scroll.content.anchoredPosition.y - page);
            else if (keyboard.pageDownKey.wasPressedThisFrame)
                SetBodyOffset(scroll.content.anchoredPosition.y + page);
            else if (keyboard.homeKey.wasPressedThisFrame)
                SetBodyOffset(0f);
            else if (keyboard.endKey.wasPressedThisFrame)
                SetBodyOffset(float.MaxValue);
        }

        /// <summary>
        /// 把键盘焦点交给待处理的回顾，优先给继续动作。页面只拦截指针输入，
        /// 没有这一步，覆盖层背后被选中的控件仍会响应 Enter。
        /// 回顾前选中对象以及每个动作的原始导航与颜色只捕获一次，
        /// 回顾关闭时恢复。即使 Unity 已把该控件作为选中对象，
        /// 也照样应用金色底版与导航陷阱：呈现跟随焦点本身，
        /// 而不是进入该控件的选中切换。
        /// </summary>
        private void FocusReviewControl(Selectable control)
        {
            if (control == null || HasHigherPriorityOverlay())
                return;
            EventSystem system = EventSystem.current;
            if (system == null)
                return;
            if (!_previousSelectionCaptured)
            {
                _previousSelection = system.currentSelectedGameObject;
                _previousSelectionCaptured = true;
            }
            // 同一时刻只有一个动作显示焦点提示，因此先把另一个放回原状。
            if (control != continueButton)
                ClearFocusState(continueButton, _continueFocus);
            if (control != newScenarioButton)
                ClearFocusState(newScenarioButton, _newScenarioFocus);
            ApplyFocusPresentation(StateOf(control));
            if (system.currentSelectedGameObject != control.gameObject)
                system.SetSelectedGameObject(control.gameObject);
        }

        /// <summary>
        /// 回顾打开期间把焦点留在回顾内。指针点击页面会清空选中，
        /// 点击正文滚动条会选中它，两者都会被重新收回；别的覆盖层
        /// 有意选中的控件则不动。回顾的任一动作都保留自己已有的焦点，
        /// 因此 Tab 可在两者之间移动。已被选中的动作仍走焦点路径，
        /// 所以 Unity 自行设置的选中也会被绘制并困住。
        /// </summary>
        private void TrapReviewFocus()
        {
            if (HasHigherPriorityOverlay())
            {
                ClearReviewFocus();
                return;
            }
            EventSystem system = EventSystem.current;
            if (system == null || continueButton == null)
                return;
            GameObject selected = system.currentSelectedGameObject;
            if (selected != null && selected != _previousSelection && !IsInsideReview(selected))
                return;
            if (newScenarioButton != null && selected == newScenarioButton.gameObject)
                FocusReviewControl(newScenarioButton);
            else
                FocusReviewControl(continueButton);
        }

        /// <summary>
        /// Tab 在回顾的两个动作之间移动键盘焦点，这是导航陷阱开启时
        /// 到达第二个动作的唯一方式。方向键不绑定，因为项目把它们
        /// 绑到玩法 Move 动作，PlayerMovement2D 读取时没有 UI 门禁；
        /// Tab 不是玩法绑定。按键检测与移动分开，便于测试或探针在
        /// 无法产生物理按键边沿时驱动焦点步骤（Input System 使用原始
        /// 设备输入，注入的操作系统按键到不了 <see cref="Keyboard.current"/>）。
        /// </summary>
        private void HandleFocusKeyboardNavigation()
        {
            if (newScenarioButton == null || HasHigherPriorityOverlay())
                return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
                return;
            FocusNextReviewAction();
        }

        /// <summary>
        /// 把回顾的键盘焦点移到另一个动作。公开是为了让测试或 Play Mode
        /// 探针在无法投递物理按键时演练 Tab 步骤。
        /// </summary>
        public void FocusNextReviewAction()
        {
            if (newScenarioButton == null || continueButton == null || HasHigherPriorityOverlay())
                return;
            EventSystem system = EventSystem.current;
            if (system == null)
                return;
            FocusReviewControl(system.currentSelectedGameObject == newScenarioButton.gameObject
                ? continueButton
                : newScenarioButton);
        }

        private bool IsReviewAction(GameObject candidate)
        {
            if (candidate == null)
                return false;
            return (continueButton != null && candidate == continueButton.gameObject) ||
                   (newScenarioButton != null && candidate == newScenarioButton.gameObject);
        }

        private bool IsInsideReview(GameObject candidate)
        {
            if (panelRoot == null || candidate == null)
                return false;
            Transform target = candidate.transform;
            return target == panelRoot.transform || target.IsChildOf(panelRoot.transform);
        }

        /// <summary>
        /// 设计上排在回顾之上的某个覆盖层正在等待玩家时为 true：
        /// 事件选择、进行中的战术任务、待处理的继承决策
        /// （<see cref="SuccessionState.IsPending"/> 涵盖继承人选择、摄政选择
        /// 与危机决策），或 M6 标题覆盖层。它们各自保留键盘焦点。
        /// 继承决策不可逆且锁住时间，因此回顾像让位给事件选择一样让给它，
        /// 阶段回到 None 后再收回焦点。标题渲染在本回顾之上，并掌管
        /// 新剧本动作要打开的选择器，因此那里也同样让位。
        /// </summary>
        private bool HasHigherPriorityOverlay()
        {
            if (gameSession == null)
                return false;
            return !string.IsNullOrEmpty(gameSession.PendingEventId) ||
                   gameSession.IsTacticalMissionActive ||
                   gameSession.Succession.IsPending ||
                   IsTitleOverlayVisible();
        }

        /// <summary>
        /// 绑定的标题覆盖层在屏上期间为 true。只查询序列化引用，
        /// 因此在该动作出现之前保存的场景不承担逐帧查找；
        /// 首次使用该动作时解析一次引用。
        /// </summary>
        private bool IsTitleOverlayVisible() =>
            titleScreenController != null && titleScreenController.IsTitleVisible;

        /// <summary>
        /// 放下回顾的焦点，但不交给别的覆盖层背后的控件。
        /// 记住的选中对象会保留，等那个覆盖层关闭后回顾再收回焦点。
        /// closes.
        /// </summary>
        private void ClearReviewFocus()
        {
            ClearFocusState(continueButton, _continueFocus);
            ClearFocusState(newScenarioButton, _newScenarioFocus);
            EventSystem system = EventSystem.current;
            if (system != null && IsReviewAction(system.currentSelectedGameObject))
                system.SetSelectedGameObject(null);
        }

        /// <summary>
        /// 把焦点与两个动作的原始导航、颜色恢复到回顾打开之前的状态。
        /// </summary>
        private void ReleaseReviewFocus()
        {
            EventSystem system = EventSystem.current;
            GameObject previous = _previousSelection;
            _previousSelection = null;
            _previousSelectionCaptured = false;
            ClearFocusState(continueButton, _continueFocus);
            ClearFocusState(newScenarioButton, _newScenarioFocus);
            if (system == null || !IsReviewAction(system.currentSelectedGameObject))
                return;
            system.SetSelectedGameObject(
                previous != null && previous.activeInHierarchy ? previous : null);
        }

        /// <summary>
        /// 一个回顾动作被捕获的呈现。标签与控件在每次焦点处理时刷新，
        /// 因此重绑定不会留下过期引用。
        /// </summary>
        private FocusState StateOf(Selectable control)
        {
            if (control == null)
                return null;
            if (control == continueButton)
            {
                _continueFocus.Control = continueButton;
                _continueFocus.Label = continueLabelText;
                return _continueFocus;
            }
            if (control == newScenarioButton)
            {
                _newScenarioFocus.Control = newScenarioButton;
                _newScenarioFocus.Label = newScenarioLabelText;
                return _newScenarioFocus;
            }
            return null;
        }

        /// <summary>
        /// 把获得焦点的回顾动作画成金色底版配深色墨字，并困住其导航，
        /// 使选中无法离开回顾。原始朱红底版与按钮的 ColorTint 选中色
        /// 相差约 1.06:1，等于完全没有焦点提示。原始颜色与导航只捕获一次，
        /// 焦点颜色每次调用都重写，因此 Unity 已经选中的控件
        /// 也会被重绘。
        /// </summary>
        private static void ApplyFocusPresentation(FocusState state)
        {
            if (state == null || state.Control == null)
                return;
            if (!state.NavigationCaptured)
            {
                state.Navigation = state.Control.navigation;
                state.NavigationCaptured = true;
            }
            Navigation trapped = state.Control.navigation;
            if (trapped.mode != Navigation.Mode.None)
            {
                trapped.mode = Navigation.Mode.None;
                state.Control.navigation = trapped;
            }
            if (!state.Applied)
            {
                state.Plate = state.Control.targetGraphic;
                state.PlateColor = state.Plate != null ? state.Plate.color : Color.white;
                if (state.Label != null)
                {
                    state.LabelColor = state.Label.color;
                    state.LabelStyle = state.Label.fontStyle;
                }
                state.Applied = true;
            }
            if (state.Label != null)
            {
                state.Label.color = FocusedButtonForeground;
                state.Label.fontStyle = FontStyle.Bold;
            }
            if (state.Plate != null)
                state.Plate.color = FocusedButtonBackground;
        }

        /// <summary>把一个动作的原始导航、底版与标签放回。</summary>
        private static void ClearFocusState(Selectable control, FocusState state)
        {
            if (state == null)
                return;
            if (state.NavigationCaptured && control != null)
                control.navigation = state.Navigation;
            state.NavigationCaptured = false;
            if (!state.Applied)
                return;
            if (state.Plate != null)
                state.Plate.color = state.PlateColor;
            if (state.Label != null)
            {
                state.Label.color = state.LabelColor;
                state.Label.fontStyle = state.LabelStyle;
            }
            state.Plate = null;
            state.Applied = false;
        }

        /// <summary>
        /// 一个可聚焦回顾动作的原始呈现，回顾绘制焦点时捕获，
        /// 释放焦点时恢复。
        /// </summary>
        private sealed class FocusState
        {
            public Selectable Control;
            public Graphic Plate;
            public Color PlateColor;
            public Text Label;
            public Color LabelColor;
            public FontStyle LabelStyle;
            public Navigation Navigation;
            public bool NavigationCaptured;
            public bool Applied;
        }

        /// <summary>
        /// Splits one review body into ordered blocks that each stay under the uGUI Text
        /// mesh limit. A block ends at a line break when one occurs inside its window, so a
        /// history row is never cut in half, and a surrogate pair is never separated.
        /// Concatenating the blocks in order reproduces <paramref name="body"/> exactly.
        /// </summary>
        public static IReadOnlyList<string> SplitBody(string body, int maxCharacters)
        {
            if (maxCharacters < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCharacters));
            if (string.IsNullOrEmpty(body))
                return new[] { string.Empty };

            var blocks = new List<string>((body.Length / maxCharacters) + 1);
            int start = 0;
            while (start < body.Length)
            {
                int end = start + maxCharacters;
                if (end >= body.Length)
                {
                    blocks.Add(body.Substring(start));
                    break;
                }

                int lineBreak = body.LastIndexOf('\n', end - 1, end - start);
                if (lineBreak > start)
                    end = lineBreak + 1;
                if (char.IsHighSurrogate(body[end - 1]) && char.IsLowSurrogate(body[end]))
                    end--;
                if (end <= start)
                    end = start + maxCharacters;
                blocks.Add(body.Substring(start, end - start));
                start = end;
            }
            return blocks;
        }

        private void EnsureBodyBlocks(int count)
        {
            for (int i = _bodyBlocks.Count - 1; i >= 0; i--)
                if (_bodyBlocks[i] == null)
                    _bodyBlocks.RemoveAt(i);
            if (_bodyBlocks.Count == 0)
                _bodyBlocks.Add(bodyText);
            else if (_bodyBlocks[0] != bodyText)
            {
                for (int i = 1; i < _bodyBlocks.Count; i++)
                    DestroyBodyBlock(_bodyBlocks[i]);
                _bodyBlocks.Clear();
                _bodyBlocks.Add(bodyText);
            }

            while (_bodyBlocks.Count < count)
                _bodyBlocks.Add(CreateBodyBlock(_bodyBlocks.Count));
            while (_bodyBlocks.Count > count)
            {
                int last = _bodyBlocks.Count - 1;
                Text extra = _bodyBlocks[last];
                _bodyBlocks.RemoveAt(last);
                DestroyBodyBlock(extra);
            }
        }

        private static void DestroyBodyBlock(Text block)
        {
            if (block == null)
                return;
            block.gameObject.SetActive(false);
            if (UnityEngine.Application.isPlaying)
                UnityEngine.Object.Destroy(block.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(block.gameObject);
        }

        private Text CreateBodyBlock(int index)
        {
            Text block = Instantiate(bodyText, bodyText.transform.parent);
            block.gameObject.name = bodyText.name + BodyBlockNameSuffix + index;
            block.transform.SetAsLastSibling();
            block.rectTransform.anchoredPosition = Vector2.zero;
            block.rectTransform.localScale = Vector3.one;
            block.rectTransform.localRotation = Quaternion.identity;
            block.text = string.Empty;
            block.raycastTarget = false;
            return block;
        }

        /// <summary>
        /// Acknowledges the single pending review. Failure keeps the overlay open and the
        /// message explains why; success persists the flag and closes this review.
        /// </summary>
        public void Continue()
        {
            if (gameSession == null)
                return;
            CampaignReviewResult result = gameSession.AcknowledgeCampaignReview();
            SetStatus(result.Message);
            Refresh();
            if (result.Succeeded && statusText != null)
                statusText.text = result.Message;
        }

        /// <summary>
        /// 打开标题覆盖层既有的剧本选择器。标题只切换自己的页面，
        /// 因此正在运行的战役、其日期、资源与存档槽保持原样；
        /// 玩家仍须挑选一个剧本条目才会开始。标题渲染在本回顾之上，
        /// 因此标题在屏期间回顾让出键盘焦点，标题关闭后再收回。
        /// </summary>
        public void OpenScenarioSelection()
        {
            TitleScreenController title = ResolveTitleScreen();
            if (title == null)
            {
                SetStatus("未找到标题界面，无法开始新剧本。");
                return;
            }
            title.ShowScenarioSelection();
            // 两页迁移之前保存的标题没有选择器页面。让玩家留在标题的
            // 主菜单上，而不是进入一个无法返回的空覆盖层。
            if (!title.IsScenarioSelectionVisible)
                title.ShowTitle();
        }

        /// <summary>
        /// 序列化的标题控制器；若场景在此动作出现之前保存、引用为空，
        /// 则取场景里的那一个。只解析一次，逐帧焦点检查因此不做搜索。
        /// </summary>
        private TitleScreenController ResolveTitleScreen()
        {
            if (titleScreenController == null)
                titleScreenController = FindAnyObjectByType<TitleScreenController>();
            return titleScreenController;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            if (continueButton != null && _continueListener == null)
            {
                _continueListener = Continue;
                continueButton.onClick.AddListener(_continueListener);
                _staticButtonBindings++;
            }
            // 新剧本动作不计入 StaticButtonBindingCount，那个计数
            // 报告的是继续动作自身的监听器不变量。
            if (newScenarioButton != null && _newScenarioListener == null)
            {
                _newScenarioListener = OpenScenarioSelection;
                newScenarioButton.onClick.AddListener(_newScenarioListener);
            }
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            if (_newScenarioListener != null)
            {
                if (newScenarioButton != null)
                    newScenarioButton.onClick.RemoveListener(_newScenarioListener);
                _newScenarioListener = null;
            }
            if (_continueListener != null)
            {
                if (continueButton != null)
                    continueButton.onClick.RemoveListener(_continueListener);
                _continueListener = null;
                _staticButtonBindings--;
            }
            if (_staticButtonBindings < 0)
                _staticButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            Refresh();
        }
    }
}
