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
    /// M08 campaign review overlay. It appears only while a real 55-year stage review or
    /// 100-year 百年名门 review is pending, renders only sourced family facts, and offers two
    /// actions: continue, and a new-scenario entry that opens the title overlay's existing
    /// selector. Acknowledgement is owned by GameSession; this panel never changes time,
    /// resources or the Phase 1 demo completion, and the selector only opens a page.
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
        /// Secondary action that opens the title's scenario selector. It never starts a
        /// scenario itself, so a review can be left for the selector without resetting the
        /// running campaign, its date, resources or the save slot.
        /// </summary>
        public const string NewScenarioButtonName = "M08CampaignReviewNewScenarioButton";
        public const string NewScenarioLabelName = "M08CampaignReviewNewScenarioLabel";
        /// <summary>Visible caption of the new-scenario action.</summary>
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
        /// Floor for the fitted card. The card is authored at 1120x640, but the CanvasScaler
        /// hands the canvas a different size in reference units on other aspects, so the card
        /// scales down instead of clipping its header or continue row. This is a guardrail,
        /// not a guarantee: at 0.25 the card still needs a 280x160 reference page. With
        /// CanvasScaler 1280x720 and match 0.5 the page depends only on aspect, never on
        /// resolution: 16:9 is 1280x720 (scale 1.00), 16:10 1214x759 (1.00), 4:3 1108x831
        /// (0.946), 5:4 1073x859 (0.915), 21:9 1478x623 (0.899), 32:9 1810x509 (0.72). A page
        /// below 280x160 would clamp here and could touch an edge, so an aspect wider than
        /// about 48:9 must raise this floor rather than trust the 24 px margin.
        /// </summary>
        public const float MinimumCardScale = 0.25f;

        /// <summary>
        /// Visible margin the fitted card keeps from the page edges on every aspect. The card
        /// is scaled against the page minus this margin, so it never touches the screen edge.
        /// </summary>
        public const float PageSafeMargin = 24f;

        /// <summary>
        /// Keyboard focus state of the continue control: a gold plate with dark ink. The
        /// authored vermilion plate and its ColorTint selected colour (0.961) differ by about
        /// 1.06:1, which is not a usable focus cue. The button tints its graphic, so the
        /// rendered focused plate is about (187,158,101): Ink on that plate measures 5.24:1
        /// and the tinted plate against the card 5.52:1 (the untinted theme pair is 5.67:1 and
        /// 5.97:1). Both still clear AA text and the 3:1 focus-indicator minimum. Both colours
        /// already belong to the shared theme, so nothing global changes.
        /// </summary>
        public static readonly Color FocusedButtonBackground = SengokuUiTheme.Gold;
        public static readonly Color FocusedButtonForeground = SengokuUiTheme.Ink;

        /// <summary>
        /// Ink of the status line, which reports a failed or accepted continue action at 14 px.
        /// The card is authored with IndigoPanel (34,43,61); Gold (195,164,105) measures 5.97:1
        /// against it and clears WCAG 1.4.3's 4.5:1 for normal text. The previous GoldDim
        /// (142,122,81) measured 3.41:1, so the one message that explains a refused action was
        /// the hardest text in the overlay to read. Gold is already the theme's heading ink,
        /// so nothing global changes.
        /// </summary>
        public static readonly Color StatusTextColor = SengokuUiTheme.Gold;

        /// <summary>Runtime scrollbar the review adds when the generated scroll has none.</summary>
        public const string ScrollbarObjectName = "M08CampaignReviewScrollbar";
        /// <summary>
        /// Full pointer target of the review scrollbar. WCAG 2.2 2.5.8 asks for 24x24 CSS px;
        /// the whole 24 px column is clickable, not only the drawn handle.
        /// </summary>
        public const float ScrollbarWidth = 24f;

        [Header("Dependencies")]
        [SerializeField] private GameSession gameSession;
        // Only owner of the scenario selector. Bound by the scene generator when the M6 title
        // overlay exists, and resolved once here before the action is first used otherwise.
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
        // Page size the card was last fitted to. LateUpdate compares it with the live page
        // rect, so a window or canvas resize while the review is open re-fits the card.
        private Vector2 _fittedPageSize = new Vector2(-1f, -1f);
        // Selection that held keyboard focus before the overlay took it, so hiding the
        // review returns focus instead of leaving a hidden action selected.
        private GameObject _previousSelection;
        // True once the pre-review selection was captured. Null is a valid selection, so the
        // captured state needs its own flag instead of a null check.
        private bool _previousSelectionCaptured;
        // Authored appearance and navigation of each review action, restored when the review
        // drops focus so later keyboard use of either button is unchanged.
        private readonly FocusState _continueFocus = new FocusState();
        private readonly FocusState _newScenarioFocus = new FocusState();
        private Scrollbar _bodyScrollbar;
        // A missing card is remembered so LateUpdate's refit does not run a Transform.Find
        // every frame while the review is visible.
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
        /// <summary>Uniform scale applied to the card so it stays inside the page.</summary>
        public float CardScale => Card != null ? Card.localScale.x : 1f;
        /// <summary>The scrollbar bound to the review body, created on first show if absent.</summary>
        public Scrollbar BodyScrollbar => BodyScroll != null ? BodyScroll.verticalScrollbar : null;
        /// <summary>
        /// True while the continue control holds the review's visible keyboard focus: it is the
        /// current selection and the gold focus presentation is applied. Selection set by
        /// somebody else without that presentation does not count.
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
        /// True while the new-scenario action holds the review's visible keyboard focus, by the
        /// same rule as <see cref="IsContinueFocused"/>.
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
            // Refresh is a runtime path: it fits the card, paints keyboard focus and renders the
            // review text and body blocks. The scene generator binds in EditMode and then saves,
            // so snapshot the authored state first and put it back when the editor is not
            // playing. Otherwise a bind with a pending review would bake the current game-view
            // aspect into the saved card scale, a gold plate into the authored button colour,
            // and review text plus cloned body blocks into the authored scene.
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
        /// Puts the authored Text values and the single body-block template back after an
        /// EditMode bind, so a generator pass cannot save rendered review content or cloned
        /// body blocks into the scene.
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

        /// <summary>The card that holds the header, body and continue row.</summary>
        private RectTransform Card
        {
            get
            {
                if (_cardRect == null && !_cardLookupFailed && panelRoot != null)
                {
                    _cardRect = panelRoot.transform.Find(CardObjectName) as RectTransform;
                    // Remember a miss so a renamed or moved card does not cost one
                    // Transform.Find per frame while the review stays open.
                    _cardLookupFailed = _cardRect == null;
                }
                return _cardRect;
            }
        }

        /// <summary>
        /// Uniform scale that keeps the authored card inside the supplied page size. Never
        /// above 1, because the authored card is the reference layout. The CanvasScaler gives
        /// the canvas a narrower or shorter page on other aspect ratios, so the card scales
        /// down instead of clipping its header or continue row.
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
        /// Fits the card to the current page, keeping <see cref="PageSafeMargin"/> visible on
        /// every side. At the 1280x720 reference the card already fits, so the scale is 1. This
        /// is also the resize path: an open review re-fits when the page size changes.
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
        /// Re-fits the card after the page changed size, so an open review stays contained
        /// while the window or canvas is resized.
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
        /// Adds the visible scrollbar column once when the generated scroll has none. The
        /// scene generator does not author one, and the review body is far taller than the
        /// viewport, so without it a player has no cue that the history continues. The bar
        /// reserves its own column in the right margin, so it never covers wrapped text.
        /// <para>
        /// Accessibility: the whole 24 px column is the pointer target, which meets WCAG 2.2
        /// 2.5.8 (24x24) even though the drawn handle can be shorter; Page Up, Page Down,
        /// Home and End are the keyboard equivalent. The PaperLine track blends to about
        /// (174,160,133) over the scroll's ParchmentDeep 0.16 background, 3.67:1, and the Ink
        /// handle is 5.22:1 against that track, so both parts clear 1.4.11's 3:1. The bar is
        /// created in the scroll's own scene, not the active scene, so an EditMode fixture
        /// that calls this cannot dirty the user's authored scene.
        /// </para>
        /// <see cref="Refresh"/> calls this only while playing: creating objects inside the
        /// scene generator's EditMode pass would bypass its Undo group. EditMode fixtures call
        /// it directly.
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
        /// Creates a GameObject inside the supplied scene. The plain constructor would put it
        /// in the active scene first and only a later reparent would move it; that marks the
        /// active scene dirty in the Editor even after the object leaves it.
        /// </summary>
        private static GameObject CreateInScene(Scene scene, string name, params Type[] components)
        {
            var created = new GameObject(name, components);
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        /// <summary>
        /// Scrolls the body by a signed pixel amount, clamped to the content range. The
        /// generated content is anchored and pivoted at the top, where a Y of zero is the
        /// first line and a larger Y scrolls further down.
        /// </summary>
        public void ScrollBodyBy(float delta)
        {
            ScrollRect scroll = BodyScroll;
            if (scroll == null || scroll.content == null || scroll.viewport == null)
                return;
            SetBodyOffset(scroll.content.anchoredPosition.y + delta);
        }

        /// <summary>
        /// Moves the body to an absolute pixel offset from the first line, clamped to the
        /// content range. Home and End use it directly, so they land on the ends even if the
        /// scroll later stops clamping its own movement.
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
        /// Page Up, Page Down, Home and End scroll the review body while the overlay owns
        /// keyboard input. Up and Down stay unbound on purpose: the project binds them to the
        /// gameplay Move action, and PlayerMovement2D reads that action without a UI gate, so
        /// binding them here would walk the character behind the overlay. A higher-priority
        /// overlay keeps the keys for itself.
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
        /// Gives the pending review keyboard focus, preferring the continue action. The page
        /// blocks pointer input only, so without this a control selected behind the overlay
        /// would still answer Enter. The previous selection and each action's authored
        /// navigation and colours are captured once and restored when the review closes. The
        /// gold plate and the navigation trap are applied even when Unity already holds this
        /// control as the selected object: presentation follows focus itself, not the selection
        /// transition into it.
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
            // Only one action shows the focus cue at a time, so the other is put back first.
            if (control != continueButton)
                ClearFocusState(continueButton, _continueFocus);
            if (control != newScenarioButton)
                ClearFocusState(newScenarioButton, _newScenarioFocus);
            ApplyFocusPresentation(StateOf(control));
            if (system.currentSelectedGameObject != control.gameObject)
                system.SetSelectedGameObject(control.gameObject);
        }

        /// <summary>
        /// Keeps focus inside the review while it is open. A pointer click on the page clears
        /// the selection and a click on the body scrollbar selects it, so both are reclaimed;
        /// a control that another overlay selected on purpose is left alone. Either review
        /// action keeps the focus it already holds, so Tab can move between them. An already
        /// selected action still goes through the focus path, so a selection Unity set on its
        /// own is painted and trapped too.
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
        /// Tab moves keyboard focus between the review's two actions, which is the only way to
        /// reach the second one while the navigation trap is on. Arrows stay unbound because
        /// the project binds them to the gameplay Move action that PlayerMovement2D reads
        /// without a UI gate; Tab is not a gameplay binding. The key check and the move are
        /// separate so the focus step can be driven by a test or a probe that cannot deliver a
        /// physical key edge (the Input System uses raw device input, so injected OS keys never
        /// reach <see cref="Keyboard.current"/>).
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
        /// Moves the review's keyboard focus to its other action. Public so a test or a Play
        /// Mode probe can exercise the Tab step where no physical key can be delivered.
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
        /// True while an overlay the design ranks above the review is waiting for the player:
        /// an event choice, an active tactical mission, a pending succession decision
        /// (<see cref="SuccessionState.IsPending"/> covers the heir choice, the regent choice
        /// and the crisis decision), or the M6 title overlay. Those keep their own keyboard
        /// focus. A succession decision is irreversible and blocks time, so the review yields
        /// to it exactly as it yields to an event choice, and reclaims focus once the phase
        /// returns to None. The title renders above this review and owns the selector the
        /// new-scenario action opens, so it yields there too.
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
        /// True while the bound title overlay is on screen. Only the serialized reference is
        /// consulted, so a scene saved before this action existed costs no per-frame lookup;
        /// the reference is resolved once the first time the action is used.
        /// </summary>
        private bool IsTitleOverlayVisible() =>
            titleScreenController != null && titleScreenController.IsTitleVisible;

        /// <summary>
        /// Drops the review's focus without handing it to a control behind another overlay.
        /// The remembered selection is kept, so the review reclaims focus when that overlay
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
        /// Returns focus and both actions' authored navigation and colours to the state before
        /// the review opened.
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
        /// The captured presentation of one review action. The label and control are refreshed
        /// on every focus pass so a rebind cannot leave a stale reference behind.
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
        /// Paints the focused review action as a gold plate with dark ink and traps its
        /// navigation so the selection cannot leave the review. The authored vermilion plate
        /// and the button's ColorTint selected colour differ by about 1.06:1, which reads as no
        /// focus cue at all. The authored colours and navigation are captured once and the
        /// focus colours are written on every call, so a control Unity already had selected is
        /// still repainted.
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

        /// <summary>Puts one action's authored navigation, plate and label back.</summary>
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
        /// One focusable review action's authored presentation, captured when the review paints
        /// its focus and restored when it drops it.
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
        /// Opens the title overlay's existing scenario selector. The title only switches its
        /// own page, so the running campaign, its date, resources and the save slot stay
        /// exactly as they are; the player still has to pick a scenario entry to start one.
        /// The title renders above this review, so the review yields keyboard focus while it
        /// is up and reclaims it when the title closes.
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
            // A title saved before the two-page migration has no selector page. Leave the
            // player on the title's main menu instead of an empty overlay with no way back.
            if (!title.IsScenarioSelectionVisible)
                title.ShowTitle();
        }

        /// <summary>
        /// The serialized title controller, or the scene's one when a scene saved before this
        /// action existed left the reference empty. Resolved once, so the per-frame focus check
        /// never searches.
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
            // The new-scenario action is not counted in StaticButtonBindingCount, which reports
            // the continue action's own listener invariant.
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
