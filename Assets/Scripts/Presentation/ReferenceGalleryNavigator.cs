using System;
using System.Collections.Generic;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    public enum ReferenceGalleryPage
    {
        Household,
        Character,
        Family,
        Map,
        Events
    }

    [Flags]
    public enum ReferenceGalleryPageMask
    {
        None = 0,
        Household = 1 << (int)ReferenceGalleryPage.Household,
        Character = 1 << (int)ReferenceGalleryPage.Character,
        Family = 1 << (int)ReferenceGalleryPage.Family,
        Map = 1 << (int)ReferenceGalleryPage.Map,
        Events = 1 << (int)ReferenceGalleryPage.Events
    }

    [Serializable]
    public struct ReferenceGalleryPageRoot
    {
        [SerializeField] private GameObject root;
        [SerializeField] private ReferenceGalleryPageMask visibleOn;

        public GameObject Root => root;
        public ReferenceGalleryPageMask VisibleOn => visibleOn;

        public ReferenceGalleryPageRoot(GameObject root, ReferenceGalleryPageMask visibleOn)
        {
            this.root = root;
            this.visibleOn = visibleOn;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ReferenceGalleryNavigator : MonoBehaviour
    {
        public const int PageCount = 5;
        public const int CurrentConfigurationVersion = 2;

        // The reference pages are composed at the 1280x720 design canvas. The
        // fractions below keep every generated region inside the safe canvas for
        // any parent size and are shared with the EditMode layout tests.
        private const float DesignWidth = 1280f;
        private const float DesignHeight = 720f;

        [SerializeField] private int configurationVersion;
        [SerializeField] private ReferenceGalleryPageRoot[] pageRoots;
        [SerializeField] private ReferenceGalleryPageRoot[] featureLaunchers;
        [SerializeField] private Button[] navigationButtons;
        [SerializeField] private Button launcherButton;
        [SerializeField] private GameObject navigationRoot;
        [SerializeField] private GameObject headerRoot;
        [SerializeField] private GameObject characterPanelRoot;
        [SerializeField] private GameObject familyPanelRoot;
        [SerializeField] private GameObject householdPanelRoot;
        [SerializeField] private GameObject mapPanelRoot;
        [SerializeField] private GameSession gameSession;

        private readonly List<Button> _wiredButtons = new List<Button>();
        private readonly List<UnityAction> _listeners = new List<UnityAction>();
        private UnityAction _launcherListener;
        private bool _wired;
        private bool _hasShownPage;
        private GameSession _subscribedSession;
        private int _mapReturnCount;
        private int _householdCompositionBuildCount;
        private int _eventPanelRefreshCount;
        private ReferenceGalleryPage _currentPage = ReferenceGalleryPage.Character;
        private GameObject _eventsEmptyState;
        private GameObject _householdComposition;
        private GameObject _pendingEventsRail;
        private RectTransform _householdIndustryPanel;
        private Vector2 _householdIndustryAuthored;
        private bool _householdIndustryScrolls;
        private Transform _householdIndustryHomeParent;
        private int _householdIndustryHomeSibling = -1;
        private Vector2 _lastPageSize;

        public bool IsBound
        {
            get
            {
                if (configurationVersion != CurrentConfigurationVersion ||
                    pageRoots == null || navigationButtons == null ||
                    navigationButtons.Length != PageCount ||
                    launcherButton == null || navigationRoot == null || headerRoot == null ||
                    characterPanelRoot == null || familyPanelRoot == null ||
                    householdPanelRoot == null || mapPanelRoot == null)
                    return false;
                for (int i = 0; i < navigationButtons.Length; i++)
                    if (navigationButtons[i] == null)
                        return false;
                return true;
            }
        }

        public ReferenceGalleryPage CurrentPage => _currentPage;

        /// <summary>
        /// Number of feature-entry launchers bound to page tabs. Each bound root owns a
        /// launcher control (the root itself or its first direct button child) that is
        /// placed on the shared right-hand shelf and rendered above the page it belongs to.
        /// </summary>
        public int FeatureLauncherCount => featureLaunchers != null ? featureLaunchers.Length : 0;

        // The launcher shelf lives in the page's free right column: below the M02 inspector
        // copy on the Character page and below the succession HUD on the Family page. The
        // rects are anchored to the canvas top-right so they track the live page size; the
        // depth pass keeps the band above the page root and below the page's own content.
        public static readonly Vector2 FeatureLauncherSize = new Vector2(200f, 44f);
        public const float FeatureLauncherRightInset = 56f;
        public const float FeatureLauncherCharacterTop = 460f;
        public const float FeatureLauncherFamilyTop = 380f;
        public const float FeatureLauncherSpacing = 52f;

        /// <summary>
        /// Binds the feature-entry launcher roots (education, research, formation, doctrine).
        /// They stay registered page roots; this list adds the shared shelf placement and
        /// keeps their roots in the render band between the page root and page content.
        /// </summary>
        public void BindFeatureLaunchers(ReferenceGalleryPageRoot[] launchers)
        {
            featureLaunchers = launchers;
        }

        /// <summary>Diagnostics proving a same-page click does not replay page side effects.</summary>
        public int MapReturnCount => _mapReturnCount;
        public int HouseholdCompositionBuildCount => _householdCompositionBuildCount;
        public int EventPanelRefreshCount => _eventPanelRefreshCount;

        /// <summary>Optional live-data binding for household/event composition.</summary>
        public void BindGameSession(GameSession session)
        {
            UnsubscribeSession();
            gameSession = session;
            SubscribeSession();
        }

        public GameObject HouseholdComposition => _householdComposition;
        /// <summary>最近一次套用到嵌入式产业面板的适配结果（诊断/测试用）。</summary>
        public HouseholdIndustryFitResult LastHouseholdIndustryFit { get; private set; }
        /// <summary>家宅卡片迫使产业面板缩到可读比例以下时为 true。</summary>
        public bool HouseholdIndustryBelowReadableScale { get; private set; }
        /// <summary>面板与月度区块被约束在其内的家宅卡片。</summary>
        public Rect LastHouseholdCardRect { get; private set; }
        /// <summary>适配后的产业面板矩形，以页面单位计。</summary>
        public Rect LastHouseholdIndustryPanelRect { get; private set; }
        public GameObject PendingEventsRail => _pendingEventsRail;

        /// <summary>
        /// 家宅页面在设计单位（画布居中）下的参考区域。中带在 760x560
        /// 家宅卡片内按原始纵横比放置任命/产业面板，左轨列出实时名册，
        /// 右列堆叠待处理决策。月度摘要放在卡片本身上，因此
        /// <see cref="HouseholdMonthEventPosition"/> 只记录其原始槽位。
        /// </summary>
        public static readonly Vector2 HouseholdIndustrySize = new Vector2(620f, 456f);
        public static readonly Vector2 HouseholdIndustryPosition = new Vector2(38f, 48f);
        /// <summary>首部轨道、页面边缘与嵌入式产业面板之间的间隙。</summary>
        public const float HouseholdIndustryBandGap = 16f;
        /// <summary>面板带区下方使用的安全页面边距。</summary>
        public const float HouseholdIndustryPageMargin = 24f;
        /// <summary>场景面板没有可用序列化矩形时使用的原始尺寸。</summary>
        public const float HouseholdIndustryFallbackWidth = 760f;
        public const float HouseholdIndustryFallbackHeight = 700f;
        /// <summary>
        /// 场景原始设计的、嵌入 SC-019 面板的家宅卡片。面板允许更高
        /// （M05 变更后为 760x700）；此时按本卡片适配，并报告为低于可读比例，
        /// 而不是悄悄溢出卡片。
        /// </summary>
        public const float HouseholdCardWidth = 760f;
        public const float HouseholdCardHeight = 560f;
        /// <summary>面板与月度区块距家宅卡片边缘保留的净空。</summary>
        public const float HouseholdCardEdgeInset = 8f;
        /// <summary>
        /// 原始的月度摘要底块。放置后的区块最多继承该高度，
        /// 但尺寸由家宅卡片推导（<c>TryGetHouseholdMonthBlockRect</c>），
        /// 因为原始右列槽位落在 760x560 卡片之外。
        /// </summary>
        public static readonly Vector2 HouseholdMonthEventSize = new Vector2(252f, 320f);
        /// <summary>
        /// 月度摘要与产业面板之间的净空。适配流程在求解面板缩放之前
        /// 预留这条带区加上该间隙。
        /// </summary>
        public const float HouseholdMonthGap = HouseholdIndustryBandGap * 0.5f;
        /// <summary>
        /// 能容纳摘要 18 px 标题与一行 13 px 正文的最小带区。
        /// 卡片若无法在适配后的面板上方留出这么多空间，
        /// 就完全不画摘要区块，而不是画一个被裁掉的。
        /// </summary>
        public const float HouseholdMonthMinimumHeight = 40f;
        /// <summary>月度底块的原始右列槽位；卡片带区会替代它。</summary>
        public static readonly Vector2 HouseholdMonthEventPosition = new Vector2(486f, -192f);
        public static readonly Vector2 HouseholdHeadRailSize = new Vector2(184f, 636f);
        public static readonly Vector2 HouseholdHeadRailPosition = new Vector2(-384f, -42f);
        public static readonly Vector2 PendingRailSize = new Vector2(252f, 240f);
        public static readonly Vector2 PendingRailPosition = new Vector2(486f, 105f);
        public static readonly Vector2 HouseholdModalSize = new Vector2(252f, 320f);
        public static readonly Vector2 HouseholdModalPosition = new Vector2(486f, -192f);
        public static readonly Vector2 EventsModalSize = new Vector2(720f, 596f);
        public static readonly Vector2 EventsModalPosition = new Vector2(-120f, -42f);
        public static readonly Vector2 EventsRailSize = new Vector2(252f, 636f);
        public static readonly Vector2 EventsRailPosition = new Vector2(486f, -42f);

        /// <summary>Map page: central illustration child created under the map root.</summary>
        public const string MapIllustrationChildName = "ReferenceMapIllustration";

        /// <summary>Map page: central illustration box, in fractions of the map root.</summary>
        public static readonly Vector2 MapRootSizeFraction = new Vector2(1060f / DesignWidth, 636f / DesignHeight);
        public static readonly Vector2 MapRootPosition = new Vector2(75f, -42f);
        /// <summary>
        /// Central map box half-width, as a fraction of the map root. Sized so the
        /// wide generated world map (1672x941) aspect-fits clear of both side
        /// columns, whose inner edges sit at 0.389 of the root width minus 120px.
        /// </summary>
        public const float MapBoxHalfWidthFraction = 0.27f;
        public const float MapBoxTopFraction = 0.412f;

        public void BindReferences(ReferenceGalleryPageRoot[] roots, Button[] buttons,
            GameObject characterPanel, GameObject familyPanel, GameObject householdPanel,
            GameObject mapPanel, Button launcher,
            GameObject navRoot, GameObject header)
        {
            UnwireButtons();
            _hasShownPage = false;
            pageRoots = roots;
            configurationVersion = CurrentConfigurationVersion;
            navigationButtons = buttons;
            characterPanelRoot = characterPanel;
            familyPanelRoot = familyPanel;
            householdPanelRoot = householdPanel;
            mapPanelRoot = mapPanel;
            launcherButton = launcher;
            navigationRoot = navRoot;
            headerRoot = header;
            // Bind the tab listeners at bind time so the editor/test seam drives the
            // same Button path as runtime instead of only exposing ShowPage directly.
            WireButtons();
            if (UnityEngine.Application.isPlaying)
                Initialize();
        }

        private void Start()
        {
            if (UnityEngine.Application.isPlaying)
                Initialize();
        }

        private void OnEnable() => SubscribeSession();

        private void LateUpdate()
        {
            if (!_hasShownPage)
                return;
            // A modal opened by a page root (succession, chronicle) must stay above the
            // shelf; the pass is idempotent and lifts it on the first frame it paints.
            EnforceShelfDepth();
            if (PageSize() == _lastPageSize)
                return;
            StyleSharedFrame();
            if (_currentPage == ReferenceGalleryPage.Map)
                StyleMapPage();
            else if (_currentPage == ReferenceGalleryPage.Family)
                StyleFamilyPage();
            else if (_currentPage == ReferenceGalleryPage.Household)
            {
                // 面板适配与月度带区都由页面尺寸推导，否则缩放窗口
                // 会把卡片留在旧几何上。
                StyleHouseholdPage();
                BuildHouseholdComposition();
                UpdatePendingRail();
            }
        }

        private void OnDisable()
        {
            UnwireButtons();
            UnsubscribeSession();
        }

        private void SubscribeSession()
        {
            if (gameSession == null || _subscribedSession == gameSession)
                return;
            UnsubscribeSession();
            _subscribedSession = gameSession;
            _subscribedSession.SessionStateChanged += OnSessionStateChanged;
        }

        private void UnsubscribeSession()
        {
            if (_subscribedSession != null)
                _subscribedSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribedSession = null;
        }

        /// <summary>
        /// Shared refresh is driven only by a real session-state change. Individual
        /// page controllers refresh themselves; the navigator owns just the composed
        /// household regions and the pending-events rail.
        /// </summary>
        private void OnSessionStateChanged()
        {
            if (!_hasShownPage)
                return;
            EnforceShelfDepth();
            if (_currentPage == ReferenceGalleryPage.Household)
            {
                BuildHouseholdComposition();
                UpdatePendingRail();
            }
            else if (_currentPage == ReferenceGalleryPage.Events)
            {
                UpdatePendingRail();
                SetEventsEmptyState(!HasPendingEvent());
            }
        }

        public void ShowPage(ReferenceGalleryPage page)
        {
            if (!Enum.IsDefined(typeof(ReferenceGalleryPage), page))
                return;

            // Re-selecting the active tab is a true no-op. Page controllers already
            // subscribe to GameSession.SessionStateChanged for their own refresh, so
            // a same-page click must not replay ReturnToMap, rebuild the household
            // composition or refresh the event view.
            if (_hasShownPage && page == _currentPage)
            {
                ConfigureRosterRail();
                RestoreCurrentPageVisibility();
                // Re-presenting the page repairs the shelf after a same-tab click and
                // picks up any modal opened from a page root since the last pass.
                PresentFeatureLaunchers(page);
                StyleNavigationSelection(page);
                return;
            }

            _hasShownPage = true;
            _currentPage = page;
            SetPanel(navigationRoot, true);
            SetPanel(headerRoot, true);
            StyleSharedFrame();
            ReferenceGalleryPageMask mask = (ReferenceGalleryPageMask)(1 << (int)page);
            FamilyRosterPanel familyRoster = familyPanelRoot != null
                ? familyPanelRoot.GetComponentInParent<FamilyRosterPanel>()
                : null;
            DailyActionPanel dailyAction = characterPanelRoot != null
                ? characterPanelRoot.GetComponentInParent<DailyActionPanel>(true)
                : null;
            if (page != ReferenceGalleryPage.Family && familyRoster != null)
                familyRoster.Close();
            // The doctrine body root is excluded from page-root toggling (it is owned by
            // its launcher), so leaving the Family page must close it explicitly instead
            // of leaving an orphaned panel over the next page.
            if (page != ReferenceGalleryPage.Family)
                CloseFamilyDoctrinePanels();
            if (pageRoots != null)
                for (int i = 0; i < pageRoots.Length; i++)
                {
                    GameObject root = pageRoots[i].Root;
                    if (root != null)
                    {
                        if (IsModalOverlay(root))
                            continue;
                        bool visible = (pageRoots[i].VisibleOn & mask) != 0;
                        if (root == characterPanelRoot && dailyAction != null)
                            visible = false;
                        root.SetActive(visible);
                        if (root.name == "M03NpcAiInspectorRoot")
                        {
                            Transform opener = root.transform.Find("M03NpcAiInspectorOpenButton");
                            if (opener != null)
                                opener.gameObject.SetActive(false);
                        }
                        if (root.name == "IndustryRoot")
                        {
                            Transform opener = root.transform.Find("IndustryOpenButton");
                            if (opener != null)
                                opener.gameObject.SetActive(false);
                            Image rootImage = root.GetComponent<Image>();
                            if (rootImage != null)
                                rootImage.raycastTarget = false;
                        }
                    }
                }

            SetPanel(launcherButton != null ? launcherButton.gameObject : null, false);
            SetPanel(characterPanelRoot,
                page == ReferenceGalleryPage.Character && dailyAction == null);
            SetPanel(familyPanelRoot, page == ReferenceGalleryPage.Family);
            SetPanel(householdPanelRoot, page == ReferenceGalleryPage.Household);
            SetPanel(mapPanelRoot, page == ReferenceGalleryPage.Map);
            if (page != ReferenceGalleryPage.Household)
                HideHouseholdComposition();
            if (page != ReferenceGalleryPage.Household && page != ReferenceGalleryPage.Events)
                SetActiveIfNotNull(_pendingEventsRail, false);
            if (page == ReferenceGalleryPage.Events)
            {
                EventChoicePanel eventPanel = GetEventPanel();
                if (eventPanel != null)
                {
                    _eventPanelRefreshCount++;
                    eventPanel.Refresh();
                }
            }
            SetEventsEmptyState(page == ReferenceGalleryPage.Events && !HasPendingEvent());
            if (page == ReferenceGalleryPage.Character)
            {
                if (dailyAction != null)
                    dailyAction.Open();
            }
            else if (page == ReferenceGalleryPage.Family)
            {
                if (familyRoster != null)
                    familyRoster.Open();
                StyleFamilyPage();
            }
            else if (page == ReferenceGalleryPage.Household)
            {
                StyleHouseholdPage();
                BuildHouseholdComposition();
                StyleEventModal(false);
                UpdatePendingRail();
            }
            else if (page == ReferenceGalleryPage.Map)
            {
                WorldMapController worldMap = mapPanelRoot != null
                    ? mapPanelRoot.GetComponent<WorldMapController>()
                    : null;
                if (worldMap != null)
                {
                    _mapReturnCount++;
                    worldMap.ReturnToMap();
                }
                StyleMapPage();
            }
            else if (page == ReferenceGalleryPage.Events)
            {
                HideHouseholdComposition();
                StyleEventModal(true);
                UpdatePendingRail();
            }

            PresentFeatureLaunchers(page);
            StyleNavigationSelection(page);
        }

        /// <summary>
        /// Handles the Character page's Return control. The M02 page root is both the
        /// character panel and the daily-action panel, so hiding that root on its own
        /// would empty the whole page area while the Character tab still read as
        /// selected. Returning therefore switches to the household overview - a real,
        /// already-populated page - and a normal Character tab click re-enters the
        /// dossier. Navigation never advances the clock or commits a session command.
        /// Returns true when the Return click was handled by leaving the page.
        /// </summary>
        public bool TryReturnFromCharacterPage()
        {
            if (_currentPage != ReferenceGalleryPage.Character)
                return false;
            ShowPage(ReferenceGalleryPage.Household);
            return true;
        }

        /// <summary>
        /// A same-tab click must not replay page commands, but it must restore a page
        /// that closed itself. Only the Character page owns a Close/Open toggle today:
        /// reactivate its page roots and reopen the daily-action panel only when it is
        /// actually closed, so an already-visible page stays a strict no-op.
        /// </summary>
        private void RestoreCurrentPageVisibility()
        {
            if (_currentPage != ReferenceGalleryPage.Character)
                return;

            if (pageRoots != null)
            {
                ReferenceGalleryPageMask mask = ReferenceGalleryPageMask.Character;
                for (int i = 0; i < pageRoots.Length; i++)
                {
                    GameObject root = pageRoots[i].Root;
                    if (root != null && root != characterPanelRoot &&
                        !IsModalOverlay(root) &&
                        (pageRoots[i].VisibleOn & mask) != 0 &&
                        !root.activeSelf)
                    {
                        root.SetActive(true);
                        if (root.name == "M03NpcAiInspectorRoot")
                        {
                            Transform opener = root.transform.Find(
                                "M03NpcAiInspectorOpenButton");
                            if (opener != null)
                                opener.gameObject.SetActive(false);
                        }
                    }
                }
            }

            if (characterPanelRoot == null)
                return;
            DailyActionPanel dailyAction =
                characterPanelRoot.GetComponentInParent<DailyActionPanel>(true);
            if (dailyAction != null)
            {
                if (!dailyAction.IsOpen)
                    dailyAction.Open();
                else
                    SetPanel(characterPanelRoot, true);
            }
            else
            {
                SetPanel(characterPanelRoot, true);
            }
        }

        /// <summary>
        /// Paints the current page with the vermilion seal accent while keeping every
        /// tab interactive. Selecting the already-active page simply re-applies the
        /// same presentation state and never commits a session command.
        /// </summary>
        private void StyleNavigationSelection(ReferenceGalleryPage page)
        {
            if (navigationButtons == null)
                return;
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                Button button = navigationButtons[i];
                if (button == null)
                    continue;
                bool selected = i == (int)page;
                button.interactable = true;
                SetButton(button,
                    selected ? PageSelectedBackground : PageIdleBackground,
                    selected ? PageSelectedForeground : PageIdleForeground);
            }
        }

        private void Initialize()
        {
            if (!IsBound)
                return;
            SubscribeSession();
            WireButtons();
            if (launcherButton != null && _launcherListener == null)
            {
                _launcherListener = () => ShowPage(ReferenceGalleryPage.Character);
                launcherButton.onClick.AddListener(_launcherListener);
            }
            ShowPage(_currentPage);
        }

        private void WireButtons()
        {
            if (_wired || navigationButtons == null || navigationButtons.Length != PageCount)
                return;
            _wired = true;
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                if (navigationButtons[i] == null)
                    continue;
                int selected = i;
                UnityAction listener = () => ShowPage((ReferenceGalleryPage)selected);
                navigationButtons[i].onClick.AddListener(listener);
                _wiredButtons.Add(navigationButtons[i]);
                _listeners.Add(listener);
            }
        }

        private void UnwireButtons()
        {
            for (int i = 0; i < _wiredButtons.Count; i++)
                if (_wiredButtons[i] != null && i < _listeners.Count && _listeners[i] != null)
                    _wiredButtons[i].onClick.RemoveListener(_listeners[i]);
            _wiredButtons.Clear();
            _listeners.Clear();
            if (launcherButton != null && _launcherListener != null)
                launcherButton.onClick.RemoveListener(_launcherListener);
            _launcherListener = null;
            _wired = false;
        }

        private static void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null && panel.activeSelf != visible)
                panel.SetActive(visible);
        }

        private static void SetActiveIfNotNull(GameObject panel, bool visible)
        {
            if (panel != null)
                panel.SetActive(visible);
        }

        /// <summary>A same-page root whose opaque content can cover the launcher shelf.</summary>
        private sealed class ShelfContent
        {
            public Transform Root;
            public Graphic[] Graphics;
        }

        private const float ShelfOpacityThreshold = 0.35f;

        private readonly List<Transform> _shelf = new List<Transform>();
        private readonly List<Button> _shelfButtons = new List<Button>();
        private readonly List<ShelfContent> _shelfContent = new List<ShelfContent>();

        /// <summary>
        /// Places the bound feature launchers of the current page on the shared right-hand
        /// shelf. Page roots and panels are opaque surfaces, so a launcher registered below
        /// one is active but invisible and unclickable; <see cref="EnforceShelfDepth"/> owns
        /// the render order.
        /// </summary>
        private void PresentFeatureLaunchers(ReferenceGalleryPage page)
        {
            _shelf.Clear();
            _shelfButtons.Clear();
            _shelfContent.Clear();

            float top = page == ReferenceGalleryPage.Character
                ? FeatureLauncherCharacterTop
                : page == ReferenceGalleryPage.Family ? FeatureLauncherFamilyTop : -1f;
            Transform host = transform.parent;
            if (top < 0f || host == null || featureLaunchers == null ||
                featureLaunchers.Length == 0)
                return;

            ReferenceGalleryPageMask mask = (ReferenceGalleryPageMask)(1 << (int)page);
            for (int i = 0; i < featureLaunchers.Length; i++)
            {
                GameObject root = featureLaunchers[i].Root;
                if (root == null || (featureLaunchers[i].VisibleOn & mask) == 0 ||
                    root.transform.parent != host)
                    continue;
                Button button = LauncherButton(root.transform);
                if (button == null)
                    continue;
                PlaceFeatureLauncher((RectTransform)button.transform, top, _shelf.Count);
                _shelf.Add(root.transform);
                _shelfButtons.Add(button);
            }
            if (_shelf.Count == 0)
                return;

            CacheShelfContent(host, mask);
            EnforceShelfDepth();
        }

        /// <summary>
        /// Remembers the same-page surfaces whose content must never be covered by the
        /// shelf: the roots this page shows plus the lifecycle overlays. The graphics are
        /// captured once per page presentation so the per-frame depth pass only measures
        /// rects instead of walking every panel hierarchy again.
        /// </summary>
        private void CacheShelfContent(Transform host, ReferenceGalleryPageMask mask)
        {
            for (int i = 0; i < host.childCount; i++)
            {
                Transform child = host.GetChild(i);
                if (child == transform || _shelf.Contains(child) ||
                    !child.gameObject.activeInHierarchy)
                    continue;
                if (!IsShelfCeiling(child.name) && !SharesPage(child, mask))
                    continue;
                _shelfContent.Add(new ShelfContent
                {
                    Root = child,
                    Graphics = child.GetComponentsInChildren<Graphic>(true)
                });
            }
        }

        private const string InspectorRootName = "M03NpcAiInspectorRoot";
        private const string TitleOverlayRootName = "M6TitleRoot";
        private const string EventOverlayRootName = "EventChoiceRoot";
        private const string DoctrineRootName = "FamilyDoctrineRoot";
        private const string DebugOverlayRootName = "M09DebugModeRoot";

        /// <summary>
        /// Modal overlays own their own visibility: the event panel shows itself whenever the
        /// session has a pending event, and the development debug window is driven by the
        /// title entry and its session. Page navigation must never hide them, even when an
        /// older scene registered them as page roots, because a hidden event modal strands a
        /// pending decision (and a hidden debug window strands its session).
        /// </summary>
        private static bool IsModalOverlay(GameObject root) =>
            root != null &&
            (root.GetComponent<EventChoicePanel>() != null ||
             string.Equals(root.name, DebugOverlayRootName, StringComparison.Ordinal));

        /// <summary>Roots that own their lifecycle and always render above page content.</summary>
        private static bool IsShelfCeiling(string name) =>
            name == InspectorRootName || name == TitleOverlayRootName ||
            name == EventOverlayRootName || name == DoctrineRootName ||
            name == TacticalMissionPanel.OverlayObjectName ||
            name == CampaignReviewPanel.RootObjectName;

        private bool SharesPage(Transform root, ReferenceGalleryPageMask mask)
        {
            if (pageRoots == null)
                return false;
            for (int i = 0; i < pageRoots.Length; i++)
                if (pageRoots[i].Root != null && pageRoots[i].Root.transform == root)
                    return (pageRoots[i].VisibleOn & mask) != 0;
            return false;
        }

        /// <summary>
        /// Keeps the shelf directly above the page root and below every same-page surface
        /// that paints over it. The insertion point is read from the live hierarchy for
        /// each root: moving a root that starts below the page root shifts every later
        /// index, so a single anchor computed before the first move would place the shelf
        /// above the title, event, tactical and inspector overlays.
        ///
        /// A page root created before the M02 root (the succession HUD and its modal, the
        /// family roster, the chronicle) sits below the page root as well. Roots whose
        /// content does not reach the shelf stay where they are, while an open modal that
        /// does reach it is lifted above the shelf so its own controls keep the pointer.
        /// The pass is idempotent and is repeated once per frame, so a modal opened after
        /// the page was shown is raised on the next frame instead of swallowing a click.
        /// </summary>
        private void EnforceShelfDepth()
        {
            if (_shelf.Count == 0 || transform.parent == null)
                return;

            for (int i = _shelf.Count - 1; i >= 0; i--)
                if (_shelf[i] != null)
                    MoveDirectlyAbovePage(_shelf[i]);

            Rect bounds = ShelfBounds(transform.parent);
            for (int i = _shelfContent.Count - 1; i >= 0; i--)
            {
                ShelfContent content = _shelfContent[i];
                if (content.Root == null || !content.Root.gameObject.activeInHierarchy)
                    continue;
                if (!PaintsOver(content.Graphics, transform.parent, bounds))
                    continue;
                RaiseAboveShelf(content.Root);
            }
        }

        private void MoveDirectlyAbovePage(Transform element)
        {
            int pageIndex = transform.GetSiblingIndex();
            int current = element.GetSiblingIndex();
            int insertAt = current <= pageIndex ? pageIndex : pageIndex + 1;
            if (current != insertAt)
                element.SetSiblingIndex(insertAt);
        }

        private void RaiseAboveShelf(Transform element)
        {
            int shelfTop = transform.GetSiblingIndex() + 1 + _shelf.Count;
            if (element.GetSiblingIndex() >= shelfTop)
                return;
            element.SetSiblingIndex(shelfTop - 1);
        }

        private Rect ShelfBounds(Transform space)
        {
            bool any = false;
            Rect bounds = new Rect();
            for (int i = 0; i < _shelfButtons.Count; i++)
            {
                Button button = _shelfButtons[i];
                if (button == null)
                    continue;
                Rect rect = RectIn((RectTransform)button.transform, space);
                bounds = any
                    ? Rect.MinMaxRect(Mathf.Min(bounds.xMin, rect.xMin),
                        Mathf.Min(bounds.yMin, rect.yMin), Mathf.Max(bounds.xMax, rect.xMax),
                        Mathf.Max(bounds.yMax, rect.yMax))
                    : rect;
                any = true;
            }
            return bounds;
        }

        private static bool PaintsOver(Graphic[] graphics, Transform space, Rect bounds)
        {
            if (graphics == null)
                return false;
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || !graphic.enabled ||
                    !graphic.gameObject.activeInHierarchy ||
                    graphic.color.a <= ShelfOpacityThreshold)
                    continue;
                if (RectIn(graphic.rectTransform, space).Overlaps(bounds, true))
                    return true;
            }
            return false;
        }

        /// <summary>The rect in the plane of a common ancestor, used for overlap tests.</summary>
        private static Rect RectIn(RectTransform rect, Transform space)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = space.InverseTransformPoint(corners[0]);
            Vector3 max = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void PlaceFeatureLauncher(RectTransform rect, float top, int slot)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = FeatureLauncherSize;
            rect.anchoredPosition = new Vector2(-FeatureLauncherRightInset,
                -(top + slot * FeatureLauncherSpacing));
            rect.localScale = Vector3.one;
        }

        /// <summary>
        /// The launcher control of a feature root: a doctrine-style root is itself the
        /// button, while the other feature roots keep their entry as the first direct
        /// button child. Nested panel buttons are never mistaken for the entry.
        /// </summary>
        private static Button LauncherButton(Transform root)
        {
            if (root == null)
                return null;
            Button button = root.GetComponent<Button>();
            if (button != null)
                return button;
            for (int i = 0; i < root.childCount; i++)
            {
                button = root.GetChild(i).GetComponent<Button>();
                if (button != null)
                    return button;
            }
            return null;
        }

        private void CloseFamilyDoctrinePanels()
        {
            CloseFamilyDoctrineIn(pageRoots);
            CloseFamilyDoctrineIn(featureLaunchers);
        }

        private static void CloseFamilyDoctrineIn(ReferenceGalleryPageRoot[] roots)
        {
            if (roots == null)
                return;
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i].Root;
                if (root == null)
                    continue;
                FamilyDoctrinePanel panel = root.GetComponent<FamilyDoctrinePanel>();
                if (panel != null)
                    panel.Close();
            }
        }

        private void StyleSharedFrame()
        {
            _lastPageSize = PageSize();
            LayoutSharedHeader(_lastPageSize);
            ConfigureRosterRail();
            Image navigation = navigationRoot != null ? navigationRoot.GetComponent<Image>() : null;
            if (navigation != null)
            {
                navigation.color = SengokuUiTheme.IndigoRail;
                SengokuUiArt.ApplyIndigoPatternOverlay(navigation.transform,
                    NavPatternChildName);
            }
            StyleNavigationIcons();

            Transform canvasRoot = transform.parent != null ? transform.parent : transform;
            Transform backdrop = canvasRoot.Find("ReferenceGalleryBackdrop");
            if (backdrop == null)
            {
                var backgroundObject = new GameObject("ReferenceGalleryBackdrop",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backgroundObject.transform.SetParent(canvasRoot, false);
                RectTransform rect = backgroundObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                backgroundObject.GetComponent<Image>().raycastTarget = false;
                backdrop = backgroundObject.transform;
            }
            backdrop.SetAsFirstSibling();
            Image background = backdrop.GetComponent<Image>();
            if (background != null)
                background.color = SengokuUiTheme.IndigoDeep;

            Transform pageBackdrop = transform.Find("M02PageBackdrop");
            Image characterBackground = pageBackdrop != null
                ? pageBackdrop.GetComponent<Image>() : null;
            if (characterBackground != null)
                characterBackground.color = SengokuUiTheme.IndigoDeep;

            // The character page's scene-baked chrome: the generated indigo textile
            // overlays the header bar and the parchment tile textures the two light
            // columns. Both are idempotent so a regenerated scene and a runtime pass
            // converge on the same surfaces.
            Transform topBar = transform.Find("M02TopBar");
            if (topBar != null)
                SengokuUiArt.ApplyIndigoPatternOverlay(topBar, "ReferenceTopBarPattern");
            Transform panelHost = transform.Find("M02DailyActionPanel");
            if (panelHost != null)
            {
                ApplyParchmentToChild(panelHost, "M02ProfilePanel");
                ApplyParchmentToChild(panelHost, "M02ActionPanel");
            }
        }

        /// <summary>顶栏资源条的字段顺序，与场景生成器一致。</summary>
        public static readonly string[] HeaderChipNames = { "功名", "人口", "资金", "物资", "情报" };

        /// <summary>家族名册滚动区在设计稿里的高度，栏高不足时按剩余空间收缩。</summary>
        public const float MaxRosterHeight = 320f;

        public const float HeaderChipSpacing = 116f;
        public const float HeaderChipWidth = 108f;
        public const float HeaderChipRowGap = 12f;
        public const float HeaderChipCompactOriginX = 166f;
        public const float HeaderChipWideOriginX = 662f;

        /// <summary>资源条总宽度：五个 chip 加四个间距。</summary>
        public static float HeaderChipRowWidth =>
            HeaderChipNames.Length * HeaderChipWidth
            + (HeaderChipNames.Length - 1) * (HeaderChipSpacing - HeaderChipWidth);

        /// <summary>低于此宽度时资源条换到日期行下方，避免与日期、所在挤在同一行。</summary>
        public static float HeaderCompactWidth =>
            HeaderChipWideOriginX + JapanMapSceneBootstrap.LauncherReservedWidth
            + HeaderChipRowGap + HeaderChipRowWidth;

        /// <summary>
        /// 资源条左端起点：宽屏保留设计稿的 662，窗口变窄时整体左移，
        /// 始终在右侧给全国地图入口留出空位，两者不会重叠。
        /// </summary>
        public static float ResolveHeaderChipOriginX(float pageWidth, float preferredOrigin)
        {
            float limit = pageWidth - JapanMapSceneBootstrap.LauncherReservedWidth
                - HeaderChipRowGap - HeaderChipRowWidth;
            return Mathf.Max(8f, Mathf.Min(preferredOrigin, limit));
        }

        private void LayoutSharedHeader(Vector2 page)
        {
            bool compact = page.x < HeaderCompactWidth;
            float height = compact ? 82f : 52f;
            if (headerRoot != null && headerRoot.transform is RectTransform header)
            {
                header.sizeDelta = new Vector2(page.x, height);
                float originX = ResolveHeaderChipOriginX(page.x, compact
                    ? HeaderChipCompactOriginX : HeaderChipWideOriginX);
                for (int i = 0; i < HeaderChipNames.Length; i++)
                    if (header.Find("M02Chip_" + HeaderChipNames[i]) is RectTransform chip)
                        chip.anchoredPosition = compact
                            ? new Vector2(originX + i * HeaderChipSpacing, -44f)
                            : new Vector2(originX + i * HeaderChipSpacing, -8f);
                Transform subtitle = header.Find("M02BrandSubText");
                if (subtitle != null) subtitle.gameObject.SetActive(!compact);
                if (header.Find("M02HeaderGoldLine") is RectTransform line)
                {
                    line.anchoredPosition = new Vector2(0, -height + 2f);
                    line.sizeDelta = new Vector2(page.x, 2f);
                }
            }
            if (navigationRoot != null && navigationRoot.transform is RectTransform rail)
            {
                rail.anchorMin = rail.anchorMax = rail.pivot = new Vector2(0, 1);
                rail.anchoredPosition = new Vector2(0, -height);
                rail.sizeDelta = new Vector2(150f, Mathf.Max(0f, page.y - height - 18f));
            }
        }

        private void ConfigureRosterRail()
        {
            if (navigationRoot == null || characterPanelRoot == null)
                return;
            Transform rail = navigationRoot.transform;
            Transform roster = rail.Find("M02DailyActionScroll") ??
                characterPanelRoot.transform.Find("M02DailyActionScroll");
            if (!(roster is RectTransform rect))
                return;
            if (roster.parent != rail)
            {
                // The opaque shared rail draws after the page; its roster must be a child.
                roster.SetParent(rail, false);
                rect.anchoredPosition = new Vector2(8f, -330f);
            }

            // The rail shortens when the header grows taller on narrow windows, so the
            // roster must stop at the rail bottom edge instead of spilling past it.
            float railHeight = rail is RectTransform railRect ? railRect.rect.height : 0f;
            if (railHeight > 0f)
            {
                float top = -rect.anchoredPosition.y;
                float height = Mathf.Clamp(railHeight - top, 0f, MaxRosterHeight);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            }

            SetPanel(roster.gameObject, _currentPage == ReferenceGalleryPage.Character);
        }

        private static void ApplyParchmentToChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            Image image = child != null ? child.GetComponent<Image>() : null;
            if (image != null)
                SengokuUiArt.ApplyParchmentSurface(image);
        }

        /// <summary>
        /// Icon atlas indices for the five reference pages, in page order: crest for
        /// the household, the lord for the character dossier, the family tree for
        /// genealogy, the compass for travel and the sealed letter for events.
        /// </summary>
        public static readonly int[] PageIconIndices =
        {
            SengokuUiArt.IconCrest,
            SengokuUiArt.IconLord,
            SengokuUiArt.IconFamilyTree,
            SengokuUiArt.IconCompass,
            SengokuUiArt.IconSealedLetter
        };

        /// <summary>
        /// Selected/idle visuals for the five page tabs. The selected tab stays
        /// clickable so re-selecting the current page is an idempotent no-op; the
        /// vermilion seal is the page-selection affordance from the reference.
        /// </summary>
        public static readonly Color PageSelectedBackground = SengokuUiTheme.VermilionDark;
        public static readonly Color PageSelectedForeground = SengokuUiTheme.ParchmentLight;
        public static readonly Color PageIdleBackground = SengokuUiTheme.IndigoPanel;
        public static readonly Color PageIdleForeground = SengokuUiTheme.ParchmentLight;

        public const string NavIconChildName = "ReferenceNavIcon";

        /// <summary>
        /// Name of the indigo textile overlay the generator bakes and the navigator
        /// refreshes on the shared navigation rail, so both paths converge.
        /// </summary>
        public const string NavPatternChildName = "ReferenceNavPattern";

        /// <summary>
        /// Gives each page button its atlas icon as a non-interactive child. Buttons
        /// keep their labels and commands; when the atlas is absent the icons simply
        /// stay hidden.
        /// </summary>
        private void StyleNavigationIcons()
        {
            if (navigationButtons == null)
                return;
            for (int i = 0; i < navigationButtons.Length; i++)
            {
                Button button = navigationButtons[i];
                if (button == null || button.transform == null)
                    continue;
                Sprite icon = i >= 0 && i < PageIconIndices.Length
                    ? SengokuUiArt.Icon(PageIconIndices[i]) : null;
                EnsureIconChild(button.transform, NavIconChildName, icon);
            }
        }

        private static void EnsureIconChild(Transform parent, string name, Sprite icon)
        {
            Transform existing = parent.Find(name);
            if (icon == null)
            {
                if (existing != null)
                    existing.gameObject.SetActive(false);
                return;
            }
            GameObject iconObject;
            if (existing != null)
            {
                iconObject = existing.gameObject;
            }
            else
            {
                iconObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image));
                iconObject.transform.SetParent(parent, false);
            }
            iconObject.SetActive(true);
            RectTransform rect = (RectTransform)iconObject.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(6f, 0f);
            rect.sizeDelta = new Vector2(28f, 28f);
            rect.localScale = Vector3.one;
            Image image = iconObject.GetComponent<Image>();
            image.sprite = icon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private void StyleFamilyPage()
        {
            if (familyPanelRoot != null)
            {
                FamilyRosterPanel roster = familyPanelRoot.GetComponentInParent<FamilyRosterPanel>();
                if (roster != null)
                {
                    SengokuUiTheme.ApplyFont(roster.transform);
                    RectTransform panelRect = roster.transform.Find("FamilyRosterPanel") as RectTransform;
                    if (panelRect != null)
                    {
                        // Shrink the viewport, not the family graph or its type.
                        Vector2 page = PageSize();
                        bool compact = page.x < 1240f;
                        float width = compact ? Mathf.Max(240f, page.x - 522f) : 760f;
                        panelRect.anchoredPosition = new Vector2(compact ? -95f : -100f, -26f);
                        panelRect.sizeDelta = new Vector2(width, 620f);
                        SetChildRect(panelRect, "FamilyRosterTitleText", new Vector2(0, 280), new Vector2(width - 40, 36));
                        SetChildRect(panelRect, "FamilyRosterLegendText", new Vector2(0, compact ? 240 : 246),
                            new Vector2(width - 40, compact ? 40 : 22));
                        // 名册掌管图谱几何：卡片够宽时为选中人物列预留空间，
                        // 并报告剩余视口。
                        roster.ApplyPageLayout();
                        float scrollWidth = roster.HasDetailColumn
                            ? roster.AppliedScrollWidth
                            : width - 60f;
                        float scrollCenterX = roster.HasDetailColumn
                            ? roster.AppliedScrollCenterX
                            : 0f;
                        SetChildRect(panelRect, "FamilyRosterScroll",
                            new Vector2(scrollCenterX, 5), new Vector2(scrollWidth, 410));
                        SetChildRect(panelRect, "FamilyRosterHeirText", new Vector2(0, -239),
                            new Vector2(width - 60, 34));
                        ScrollRect graphScroll = panelRect.Find("FamilyRosterScroll")?.GetComponent<ScrollRect>();
                        if (graphScroll != null)
                            ConfigureFamilyRosterScrollbar(panelRect, graphScroll, scrollWidth,
                                scrollCenterX);
                        Image panelImage = panelRect.GetComponent<Image>();
                        if (panelImage != null)
                        {
                            panelImage.color = SengokuUiTheme.Parchment;
                            SengokuUiArt.ApplyParchmentSurface(panelImage);
                        }
                    }
                    SetText(roster.transform, "FamilyRosterTitleText", SengokuUiTheme.Ink);
                    SetText(roster.transform, "FamilyRosterLegendText", SengokuUiTheme.InkSoft);
                    SetText(roster.transform, "FamilyRosterHeirText", SengokuUiTheme.Ink);
                    SetText(roster.transform, "FamilyRosterSectionTemplate", SengokuUiTheme.InkSoft);
                    SetImage(roster.transform, "FamilyRosterScroll", SengokuUiTheme.ParchmentLight);
                    SetImage(roster.transform, "FamilyRosterNodeTemplate", SengokuUiTheme.ParchmentDeep);
                    SetText(roster.transform, "Label", SengokuUiTheme.Ink);
                    foreach (Image image in roster.GetComponentsInChildren<Image>(true))
                        if (image.gameObject.name.StartsWith("FamilyRosterNode_", StringComparison.Ordinal))
                        {
                            image.color = SengokuUiTheme.ParchmentDeep;
                            SengokuUiArt.ApplyParchmentSurface(image);
                        }
                    foreach (Text label in roster.GetComponentsInChildren<Text>(true))
                        if (label.transform.parent != null &&
                            label.transform.parent.name.StartsWith("FamilyRosterNode_", StringComparison.Ordinal))
                            label.color = SengokuUiTheme.Ink;
                }
            }

            if (pageRoots == null)
                return;
            for (int i = 0; i < pageRoots.Length; i++)
            {
                GameObject root = pageRoots[i].Root;
                if (root == null || root.name != "SuccessionRoot")
                    continue;
                RectTransform hudRect = root.transform.Find("SuccessionHud") as RectTransform;
                if (hudRect != null)
                {
                    hudRect.anchorMin = hudRect.anchorMax = hudRect.pivot = Vector2.one;
                    hudRect.anchoredPosition = new Vector2(-16f, PageSize().x < 1240f ? -100f : -70f);
                    hudRect.sizeDelta = new Vector2(340f, 260f);
                    Image image = hudRect.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = SengokuUiTheme.IndigoRail;
                        SengokuUiArt.ApplyIndigoPatternOverlay(hudRect,
                            "SuccessionHudPattern");
                    }
                    foreach (Text label in hudRect.GetComponentsInChildren<Text>(true))
                        label.color = SengokuUiTheme.Parchment;
                    Transform retire = hudRect.Find("SuccessionRetireButton");
                    Transform crisis = hudRect.Find("SuccessionCrisisDemoButton");
                    if (retire is RectTransform retireRect)
                    {
                        retireRect.anchoredPosition = new Vector2(0f, -28f);
                        retireRect.sizeDelta = new Vector2(300f, 36f);
                    }
                    if (crisis is RectTransform crisisRect)
                    {
                        crisisRect.anchoredPosition = new Vector2(0f, -70f);
                        crisisRect.sizeDelta = new Vector2(300f, 36f);
                    }
                    SetChildTextRect(hudRect, "SuccessionHeadText", new Vector2(0f, 92f),
                        new Vector2(300f, 34f));
                    SetChildTextRect(hudRect, "SuccessionStatusText", new Vector2(0f, 43f),
                        new Vector2(300f, 56f));
                    SetChildTextRect(hudRect, "SuccessionFeedbackText", new Vector2(0f, -116f),
                        new Vector2(300f, 30f));
                }
                SuccessionPanel succession = root.GetComponent<SuccessionPanel>();
                if (succession != null)
                    SengokuUiTheme.ApplyFont(root.transform);
                break;
            }
        }

        private static void ConfigureFamilyRosterScrollbar(RectTransform panel, ScrollRect graphScroll,
            float width, float centerX = 0f)
        {
            const string scrollbarName = "FamilyRosterHorizontalScrollbar";
            Transform existing = panel.Find(scrollbarName);
            GameObject scrollbarObject = existing != null
                ? existing.gameObject
                : new GameObject(scrollbarName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.SetParent(panel, false);
            scrollbarRect.anchorMin = scrollbarRect.anchorMax = scrollbarRect.pivot = new Vector2(0.5f, 0.5f);
            scrollbarRect.anchoredPosition = new Vector2(centerX, -210);
            scrollbarRect.sizeDelta = new Vector2(width, 12);

            Transform handleTransform = scrollbarObject.transform.Find("Handle");
            bool createHandle = handleTransform == null;
            GameObject handleObject = handleTransform != null
                ? handleTransform.gameObject
                : new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.SetParent(scrollbarRect, false);
            if (createHandle)
            {
                handleRect.anchorMin = new Vector2(0, 0);
                handleRect.anchorMax = new Vector2(0, 1);
                handleRect.sizeDelta = Vector2.zero;
            }

            Image track = scrollbarObject.GetComponent<Image>();
            Image handle = handleObject.GetComponent<Image>();
            track.color = SengokuUiTheme.PaperLine;
            handle.color = SengokuUiTheme.Gold;
            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            if (scrollbar.handleRect != handleRect)
                scrollbar.handleRect = handleRect;
            if (scrollbar.targetGraphic != handle)
                scrollbar.targetGraphic = handle;
            if (scrollbar.direction != Scrollbar.Direction.LeftToRight)
                scrollbar.direction = Scrollbar.Direction.LeftToRight;
            if (graphScroll.horizontalScrollbar != scrollbar)
                graphScroll.horizontalScrollbar = scrollbar;
            if (graphScroll.horizontalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHide)
                graphScroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            if (graphScroll.horizontalScrollbarSpacing != 2)
                graphScroll.horizontalScrollbarSpacing = 2;
        }

        /// <summary>
        /// 把嵌入式 SC-019 面板放进页面实际绘制的家宅卡片内。
        /// 卡片是 <see cref="StyleHouseholdPage"/> 记录的显式 760x560 布局矩形，
        /// 因此更高的原始面板按其必须留在其上的面来判定，
        /// 而不是按导航器的 1280x720 父级。
        ///
        /// 月度摘要占用卡片顶部的带区，因此面板得到带区下方的矩形：
        /// 视口高度取决于卡片与带区，绝不取决于原始高度。
        /// 面板只按宽度缩放，在放得下时于可用带区内居中；
        /// 原始面板更高时，把完整面板放进纵向滚动视口，
        /// 而不是缩到 <see cref="HouseholdIndustryFit.ReadableScale"/> 以下。
        /// 页面太矮、连带区都放不下时，保留较早的卡片居中统一适配，
        /// 并省略摘要。
        /// </summary>
        private void ApplyEmbeddedIndustryFit(RectTransform panel, Vector2 authored, Rect card)
        {
            Rect band = HouseholdIndustryBand();
            Vector2 centre = band.center;
            float inset = HouseholdCardEdgeInset;
            _householdIndustryPanel = panel;
            _householdIndustryAuthored = authored;
            _householdIndustryScrolls = false;
            RestoreHouseholdIndustryPanelHome(panel);

            float halfWidth = Mathf.Min(band.width * 0.5f,
                Mathf.Min(centre.x - card.xMin, card.xMax - centre.x) - inset);
            // 带区中心必须位于卡片上沿之下，摘要带区才有空间；
            // 更矮的卡片改用较早的卡片居中回退方案。
            float cardTopHalf = card.yMax - centre.y - inset;
            float stripTop = HouseholdMonthStripTop(card, band);
            float topLimit = stripTop - HouseholdMonthMinimumHeight - HouseholdMonthGap;
            float bottomLimit = Mathf.Max(band.yMin, card.yMin + inset);
            float viewportHeight = topLimit - bottomLimit;
            if (halfWidth <= 0f || cardTopHalf <= 0f || viewportHeight <= 0f)
            {
                // 页面远小于设计画布时，带区会塌到卡片之外，
                // 没有可遵守的带区适配：让整块面板留在卡片上，
                // 省略摘要，而不是把它裁到面板上。
                ApplyCardCentredIndustryFallback(panel, authored, card);
                return;
            }

            float viewportWidth = halfWidth * 2f;
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.CalculateReadableViewport(
                authored.x, authored.y, viewportWidth);
            LastHouseholdIndustryFit = fit;
            HouseholdIndustryBelowReadableScale = !fit.Readable;
            if (fit.Height > viewportHeight + 0.5f)
            {
                // 高过视口一次能显示的高度：保留可读的宽度缩放，
                // 让完整面板纵向滚动。
                _householdIndustryScrolls = true;
                LastHouseholdIndustryPanelRect = Rect.MinMaxRect(centre.x - viewportWidth * 0.5f,
                    bottomLimit, centre.x + viewportWidth * 0.5f, topLimit);
                if (!fit.Readable)
                    WarnBelowReadableScale(card, authored, viewportWidth, viewportHeight, fit);
                return;
            }

            // 保留既有的带区居中构图，但绝不让面板长进摘要
            // 在上方预留的带区。
            float centreY = Mathf.Min(centre.y, topLimit - fit.Height * 0.5f);
            centreY = Mathf.Max(centreY, bottomLimit + fit.Height * 0.5f);
            LastHouseholdIndustryPanelRect = new Rect(centre.x - fit.Width * 0.5f,
                centreY - fit.Height * 0.5f, fit.Width, fit.Height);
            if (!fit.Readable)
                WarnBelowReadableScale(card, authored, viewportWidth, viewportHeight, fit);

            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = authored;
            panel.localScale = new Vector3(fit.Scale, fit.Scale, 1f);
            panel.anchoredPosition = new Vector2(centre.x, centreY);
        }

        /// <summary>
        /// 页面太矮，放不下摘要带区，因此面板保留较早的
        /// 卡片居中统一适配，摘要继续省略。
        /// </summary>
        private void ApplyCardCentredIndustryFallback(RectTransform panel, Vector2 authored,
            Rect card)
        {
            float inset = HouseholdCardEdgeInset;
            float halfWidth = Mathf.Max(0f, card.width * 0.5f - inset);
            float halfHeight = Mathf.Max(0f, card.height * 0.5f - inset);
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.Calculate(authored.x,
                authored.y, halfWidth * 2f, halfHeight * 2f);
            _householdIndustryScrolls = false;
            LastHouseholdIndustryFit = fit;
            LastHouseholdIndustryPanelRect = new Rect(card.center.x - fit.Width * 0.5f,
                card.center.y - fit.Height * 0.5f, fit.Width, fit.Height);
            HouseholdIndustryBelowReadableScale = !fit.Readable;
            if (!fit.Readable)
                WarnBelowReadableScale(card, authored, halfWidth * 2f, halfHeight * 2f, fit);

            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = authored;
            panel.localScale = new Vector3(fit.Scale, fit.Scale, 1f);
            panel.anchoredPosition = card.center;
        }

        private void WarnBelowReadableScale(Rect card, Vector2 authored, float availableWidth,
            float availableHeight, HouseholdIndustryFitResult fit)
        {
            Debug.LogWarning(
                "M04 家宅产业面板：家宅卡片 " + card.width.ToString("0") + "x" +
                card.height.ToString("0") + " 只为原始 " +
                authored.x.ToString("0") + "x" + authored.y.ToString("0") +
                " 面板留出 " + availableWidth.ToString("0") + "x" +
                availableHeight.ToString("0") + "，因此可读适配为 " +
                fit.Scale.ToString("0.###") + "，低于可读下限 " +
                HouseholdIndustryFit.ReadableScale.ToString("0.###") +
                "。请加宽卡片或给面板加滚动区，而不是继续缩小。");
        }

        /// <summary>
        /// 本页面的原始产业面板。解析过一次后优先使用缓存面板，
        /// 因此面板被托进滚动视口后的刷新仍测量同一个场景对象；
        /// 绑定到别的页面根节点的面板会被拒绝，改从层级解析。
        /// </summary>
        private RectTransform ResolveHouseholdIndustryPanel(Transform root)
        {
            if (_householdIndustryPanel != null && root != null &&
                (_householdIndustryPanel == root ||
                 _householdIndustryPanel.IsChildOf(root)))
                return _householdIndustryPanel;
            Transform industryPanel = root != null ? root.Find("IndustryPanel") : null;
            if (industryPanel == null && householdPanelRoot != null &&
                householdPanelRoot.name == "IndustryPanel")
                industryPanel = householdPanelRoot.transform;
            return industryPanel as RectTransform;
        }

        /// <summary>
        /// 把面板放回场景原始位置。滚动视口随构图一起重建，
        /// 因此面板必须在构图清空之前离开它；否则清空视口会连场景面板一起销毁。
        /// </summary>
        private void RestoreHouseholdIndustryPanelHome(RectTransform panel)
        {
            if (panel == null)
                return;
            if (_householdIndustryHomeParent == null)
            {
                _householdIndustryHomeParent = panel.parent;
                _householdIndustryHomeSibling = panel.GetSiblingIndex();
            }
            if (_householdIndustryHomeParent == null || panel.parent == _householdIndustryHomeParent)
                return;
            panel.SetParent(_householdIndustryHomeParent, false);
            int index = Mathf.Clamp(_householdIndustryHomeSibling, 0,
                _householdIndustryHomeParent.childCount - 1);
            panel.SetSiblingIndex(index);
        }

        private void RestoreHouseholdIndustryPanelHome()
        {
            RestoreHouseholdIndustryPanelHome(_householdIndustryPanel);
        }

        /// <summary>
        /// 把完整的更高面板托进构图下方的纯纵向滚动视口。
        /// 视口保留月度带区让出的矩形，因此摘要永不与面板共用带区，
        /// 面板文字也保持可读的宽度缩放；面板其余部分靠滚动到达，
        /// 而不是缩小。
        /// </summary>
        private void BuildHouseholdIndustryScrollViewport()
        {
            RectTransform panel = _householdIndustryPanel;
            Rect viewport = LastHouseholdIndustryPanelRect;
            HouseholdIndustryFitResult fit = LastHouseholdIndustryFit;
            if (panel == null || _householdComposition == null ||
                viewport.width <= 1f || viewport.height <= 1f || fit.Scale <= 0f)
                return;

            var viewportObject = new GameObject("HouseholdIndustryScrollViewport",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(ScrollRect), typeof(RectMask2D));
            viewportObject.transform.SetParent(_householdComposition.transform, false);
            var viewportRect = (RectTransform)viewportObject.transform;
            viewportRect.anchorMin = viewportRect.anchorMax = viewportRect.pivot =
                new Vector2(0.5f, 0.5f);
            viewportRect.sizeDelta = viewport.size;
            viewportRect.anchoredPosition = viewport.center;
            Image surface = viewportObject.GetComponent<Image>();
            // 面板在任何滚动位置都完整覆盖该矩形；图形存在只是为了
            // 让滚轮与拖拽事件到达 ScrollRect。
            surface.color = Color.clear;
            surface.raycastTarget = true;

            var contentObject = new GameObject("HouseholdIndustryScrollContent",
                typeof(RectTransform));
            contentObject.transform.SetParent(viewportRect, false);
            var content = (RectTransform)contentObject.transform;
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(viewport.width, fit.Height);
            content.anchoredPosition = Vector2.zero;

            ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            panel.SetParent(content, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = _householdIndustryAuthored;
            panel.localScale = new Vector3(fit.Scale, fit.Scale, 1f);
            // 缩放后的面板正好等于内容尺寸，因此零偏移可使其在内容矩形内居中；
            // 此处任何纵向偏移都会把它推出可滚动范围，
            // 并在视口顶部留出空白。
            panel.anchoredPosition = Vector2.zero;
        }

        private static Vector2 AuthoredIndustryPanelSize(RectTransform panel)
        {
            Vector2 size = panel.sizeDelta;
            if (size.x > 1f && size.y > 1f)
                return size;
            Vector2 rect = panel.rect.size;
            if (rect.x > 1f && rect.y > 1f)
                return rect;
            return new Vector2(HouseholdIndustryFallbackWidth,
                HouseholdIndustryFallbackHeight);
        }

        /// <summary>
        /// 页面绘制产业面板与月度摘要所用的家宅卡片：声明的 760x560 布局矩形，
        /// 并按实时页面尺寸封顶，使更小的窗口也能容纳它。
        /// 它是显式的生成/布局矩形，而不是对面板的测量，
        /// 因此适配与摘要总有真实的面可留在其内——
        /// 即使场景面板缺失或原始尺寸更高（M05 变更后为 760x700）。
        /// </summary>
        private Rect HouseholdCardRect()
        {
            Vector2 page = PageSize();
            float width = Mathf.Max(1f, Mathf.Min(HouseholdCardWidth, page.x));
            float height = Mathf.Max(1f, Mathf.Min(HouseholdCardHeight, page.y));
            return Rect.MinMaxRect(-width * 0.5f, -height * 0.5f, width * 0.5f, height * 0.5f);
        }

        /// <summary>
        /// 嵌入式面板周围的可用页面空间：首部轨道右侧、待处理决策列左侧、
        /// 轨道顶部之下、页面边距之上。这是页面提供的空间；
        /// <see cref="ApplyEmbeddedIndustryFit"/> 还会把它限制在家宅卡片内。
        /// 它从不检查构图，因此没有区域是测量自可能尚不存在的区块。
        /// </summary>
        private Rect HouseholdIndustryBand()
        {
            Vector2 page = PageSize();
            float left = HouseholdHeadRailPosition.x + HouseholdHeadRailSize.x * 0.5f +
                         HouseholdIndustryBandGap;
            float right = page.x * 0.5f - HouseholdIndustryPageMargin;
            // 有待处理事件时右列归待处理决策轨所有，
            // 因此它的槽位即使隐藏也要空着。
            float pendingLeft = PendingRailPosition.x - PendingRailSize.x * 0.5f -
                                HouseholdIndustryBandGap;
            if (pendingLeft < right)
                right = pendingLeft;
            float top = HouseholdHeadRailPosition.y + HouseholdHeadRailSize.y * 0.5f;
            float bottom = -page.y * 0.5f + HouseholdIndustryPageMargin;
            if (right <= left || top <= bottom)
                return new Rect(left, bottom, 1f, 1f);
            return new Rect(left, bottom, right - left, top - bottom);
        }

        /// <summary>
        /// 月度摘要保留在产业面板上方带区的顶边：取卡片与带区顶部较低者，
        /// 并像其它卡片边缘一样内缩。适配流程预留其下方空间，
        /// 区块填充该空间，因此预留与绘制一致。
        /// </summary>
        private float HouseholdMonthStripTop(Rect card, Rect band) =>
            Mathf.Min(card.yMax, band.yMax) - HouseholdCardEdgeInset;

        /// <summary>
        /// 月度摘要的卡片矩形：适配后面板正上方的可用带区，宽度与面板相同
        /// （不窄于原始底块），高度不超过原始底块。底边来自
        /// <see cref="LastHouseholdIndustryPanelRect"/>，顶边取卡片边缘与带区
        /// 顶部较低者，因此区块不会离开卡片、越过首部轨道或与面板重叠。
        ///
        /// 没有可如实绘制的内容时返回 false：没有显式卡片、没有已放置的面板，
        /// 或带区小于 <see cref="HouseholdMonthMinimumHeight"/>。
        /// 此时调用方完全省略该区块，而不是创建一个标题压在面板上的零高度区块。
        /// </summary>
        private bool TryGetHouseholdMonthBlockRect(out Rect block)
        {
            block = default;
            Rect card = LastHouseholdCardRect;
            Rect panel = LastHouseholdIndustryPanelRect;
            if (card.width <= 1f || card.height <= 1f ||
                panel.width <= 1f || panel.height <= 1f)
                return false;

            float inset = HouseholdCardEdgeInset;
            float bottom = panel.yMax + HouseholdMonthGap;
            float height = Mathf.Min(
                HouseholdMonthStripTop(card, HouseholdIndustryBand()) - bottom,
                HouseholdMonthEventSize.y);
            float width = Mathf.Min(card.width - inset * 2f,
                Mathf.Max(panel.width, HouseholdMonthEventSize.x));
            // 适配流程正好预留最小值，因此绑定纵横比在浮点误差内落在其上；
            // 明显更矮的一律省略而不是裁切。
            const float tolerance = 0.01f;
            if (height < HouseholdMonthMinimumHeight - tolerance || width <= 1f)
                return false;

            float centreX = Mathf.Clamp(panel.center.x, card.xMin + inset + width * 0.5f,
                card.xMax - inset - width * 0.5f);
            block = Rect.MinMaxRect(centreX - width * 0.5f, bottom, centreX + width * 0.5f,
                bottom + height);
            return true;
        }

        private void StyleHouseholdPage()
        {
            if (householdPanelRoot == null)
                return;
            // 上一次刷新可能已把面板托进构图的滚动视口；
            // 在读取或重新设计原始层级之前先把它带回来。
            RestoreHouseholdIndustryPanelHome();
            SengokuUiTheme.ApplyFont(householdPanelRoot.transform);
            RectTransform root = householdPanelRoot.transform as RectTransform;
            RectTransform panel = ResolveHouseholdIndustryPanel(householdPanelRoot.transform);
            Transform industryPanel = panel != null ? panel.transform : null;
            // 本页面的根节点可能就是面板本身，而根节点在下面被拉伸到页面。
            // 原始面板矩形在该拉伸之前测量，卡片是显式的 760x560 布局矩形，
            // 因此两者都不会悄悄变成父画布。
            Vector2 authored = panel != null
                ? AuthoredIndustryPanelSize(panel)
                : new Vector2(HouseholdIndustryFallbackWidth, HouseholdIndustryFallbackHeight);
            Rect card = HouseholdCardRect();
            // 适配之前先在此记录卡片，使月度带区与公开诊断始终描述
            // 本页面绘制的面——包括 IndustryPanel 子节点无法解析的场景
            // （没有画布回退）。
            LastHouseholdCardRect = card;
            LastHouseholdIndustryPanelRect = default;
            LastHouseholdIndustryFit = default;
            HouseholdIndustryBelowReadableScale = false;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = root.offsetMax = Vector2.zero;
            }

            if (panel != null)
                ApplyEmbeddedIndustryFit(panel, authored, card);
            else
                _householdIndustryScrolls = false;
            Image surface = industryPanel != null ? industryPanel.GetComponent<Image>() : null;
            if (surface != null)
            {
                surface.color = SengokuUiTheme.Parchment;
                SengokuUiArt.ApplyParchmentSurface(surface);
            }
            Transform styleRoot = industryPanel != null ? industryPanel : householdPanelRoot.transform;
            foreach (Text label in styleRoot.GetComponentsInChildren<Text>(true))
                label.color = SengokuUiTheme.Ink;
            SetText(styleRoot, "IndustryTitleText", SengokuUiTheme.Vermilion);
            SetText(styleRoot, "IndustryBlockReasonText", SengokuUiTheme.VermilionDark);
            SetText(styleRoot, "IndustryMessageText", SengokuUiTheme.InkSoft);
            SetText(styleRoot, "IndustryReportText", SengokuUiTheme.InkSoft);
            SetImage(styleRoot, "IndustryRowFarm", SengokuUiTheme.ParchmentDeep);
            SetImage(styleRoot, "IndustryRowTradingHouse", SengokuUiTheme.ParchmentDeep);
            foreach (Button button in styleRoot.GetComponentsInChildren<Button>(true))
                SetButton(button, button.name.Contains("Unassign")
                        ? SengokuUiTheme.VermilionDark : SengokuUiTheme.IndigoPanel,
                    SengokuUiTheme.ParchmentLight);
            Transform close = styleRoot.Find("IndustryCloseButton");
            if (close != null)
                close.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ household page

        private GameSession ResolveSession()
        {
            if (gameSession == null)
            {
                gameSession = UnityEngine.Object.FindFirstObjectByType<GameSession>();
                SubscribeSession();
            }
            return gameSession;
        }

        private void HideHouseholdComposition()
        {
            RestoreHouseholdIndustryPanelHome();
            SetActiveIfNotNull(_householdComposition, false);
        }

        /// <summary>
        /// Builds (or refreshes) the Household page composition from live session
        /// data only: head rail with the real roster, the month-event block from
        /// the latest settlement strings, and pending decisions. Sections without
        /// live data stay absent. All decorations are non-interactive so no
        /// invisible raycast blockers remain.
        /// </summary>
        private void BuildHouseholdComposition()
        {
            GameSession session = ResolveSession();
            if (session == null)
            {
                HideHouseholdComposition();
                return;
            }

            _householdCompositionBuildCount++;
            if (_householdComposition == null)
            {
                _householdComposition = new GameObject("ReferenceHouseholdComposition",
                    typeof(RectTransform));
                _householdComposition.transform.SetParent(
                    transform.parent != null ? transform.parent : transform, false);
                RectTransform rootRect = (RectTransform)_householdComposition.transform;
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            }
            // 清空构图会销毁其滚动视口，因此场景面板必须先离开视口内容，
            // 否则会随之被销毁。
            RestoreHouseholdIndustryPanelHome();
            _householdComposition.SetActive(true);
            _householdComposition.transform.SetAsLastSibling();
            ClearChildren(_householdComposition.transform);
            if (_householdIndustryScrolls)
                BuildHouseholdIndustryScrollViewport();

            GameDate date = session.WorldClock != null
                ? session.WorldClock.CurrentDate
                : WorldClock.NewGameDate;
            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(session.Succession, date);

            // Left rail: head of household, date, fixed heir, and the member roster.
            RectTransform headRail = CreateDecor("HouseholdHeadRail", _householdComposition.transform,
                HouseholdHeadRailPosition, HouseholdHeadRailSize, SengokuUiTheme.IndigoRail);
            CreateLabel(headRail, "HouseholdHeadTitle", "家 内", Vector2.zero, new Vector2(0f, 292f),
                22, SengokuUiTheme.Vermilion, 170f);
            CreateLabel(headRail, "HouseholdHeadName",
                string.IsNullOrWhiteSpace(session.SuccessionHeadName)
                    ? "家主：未确定" : "家主：" + session.SuccessionHeadName,
                Vector2.zero, new Vector2(0f, 258f), 16, SengokuUiTheme.Parchment, 170f,
                TextAnchor.MiddleLeft);
            if (date.IsValid)
                CreateLabel(headRail, "HouseholdHeadDate", date.Year + "年" + date.Month + "月",
                    Vector2.zero, new Vector2(0f, 232f), 15, SengokuUiTheme.Gold, 170f,
                    TextAnchor.MiddleLeft);
            if (!string.IsNullOrWhiteSpace(session.FixedHeirSummary))
                CreateLabel(headRail, "HouseholdHeir", session.FixedHeirSummary,
                    Vector2.zero, new Vector2(0f, 206f), 12, SengokuUiTheme.Parchment, 172f,
                    TextAnchor.UpperLeft, true);
            CreateLabel(headRail, "HouseholdRosterHeader", "家族成员", Vector2.zero,
                new Vector2(0f, 160f), 16, SengokuUiTheme.Gold, 170f, TextAnchor.MiddleLeft);
            if (graph.Persons.Count == 0)
                CreateLabel(headRail, "HouseholdRosterEmpty", "暂无家族成员记录。", Vector2.zero,
                    new Vector2(0f, 132f), 12, SengokuUiTheme.ParchmentLight, 172f,
                    TextAnchor.MiddleLeft);

            float rowY = 132f;
            int shown = 0;
            for (int i = 0; i < graph.Persons.Count && rowY >= -280f; i++)
            {
                FamilyTreePersonNode person = graph.Persons[i];
                string line = person.DisplayName + " · " + person.RoleName;
                if (person.HasAge)
                    line += " · " + person.Age + "岁";
                CreateLabel(headRail, "HouseholdRosterRow_" + i, line, Vector2.zero,
                    new Vector2(0f, rowY), 12, SengokuUiTheme.Parchment, 172f,
                    TextAnchor.MiddleLeft);
                rowY -= 22f;
                shown++;
            }
            int remaining = graph.Persons.Count - shown;
            if (remaining > 0)
                CreateLabel(headRail, "HouseholdRosterMore", "…另有 " + remaining + " 人",
                    Vector2.zero, new Vector2(0f, rowY), 12, SengokuUiTheme.InkSoft, 172f,
                    TextAnchor.MiddleLeft);

            // 面板上方的月度摘要，放在家宅卡片上：只用实时数据。
            var monthSources = new List<string>();
            IndustryMonthReport report = session.LastIndustryReport;
            if (report != null && !string.IsNullOrWhiteSpace(report.Summary))
                monthSources.Add(report.Summary);
            if (!string.IsNullOrWhiteSpace(session.LastIndustryMessage))
                monthSources.Add(session.LastIndustryMessage);
            if (!string.IsNullOrWhiteSpace(session.LastEventMessage))
                monthSources.Add(session.LastEventMessage);
            IReadOnlyList<string> monthLines = HouseholdMonthSummary.Distinct(monthSources);
            if (monthLines.Count > 0 && TryGetHouseholdMonthBlockRect(out Rect block))
            {
                RectTransform monthBlock = CreateDecor("HouseholdMonthEvent",
                    _householdComposition.transform, block.center, block.size,
                    SengokuUiTheme.IndigoPanel);
                // 区块是宽而矮的条带，因此标题在正文旁边而不是上方，
                // 换行文字可占用区块的完整可用高度。
                float inset = HouseholdCardEdgeInset;
                const float titleWidth = 56f;
                float textWidth = Mathf.Max(1f, block.width - titleWidth - inset * 3f);
                CreateLabel(monthBlock, "HouseholdMonthEventTitle", "本月", Vector2.zero,
                    new Vector2(-block.width * 0.5f + inset + titleWidth * 0.5f, 0f), 18,
                    SengokuUiTheme.Gold, titleWidth, TextAnchor.MiddleLeft);
                // 用一段换行正文代替固定 26 px 的行：结算报告与产业消息
                // 携带同一句话，两行换行会互相压住。
                CreateLabel(monthBlock, "HouseholdMonthEventBody",
                    HouseholdMonthSummary.Join(monthLines), Vector2.zero,
                    new Vector2(-block.width * 0.5f + inset * 2f + titleWidth + textWidth * 0.5f,
                        0f), 13, SengokuUiTheme.Parchment, textWidth, TextAnchor.UpperLeft, true);
                if (monthBlock.Find("HouseholdMonthEventBody") is RectTransform monthBody)
                    monthBody.sizeDelta = new Vector2(textWidth,
                        Mathf.Max(0f, block.height - inset * 2f));
            }
        }

        // ------------------------------------------------------------ pending decisions

        private void UpdatePendingRail()
        {
            GameSession session = ResolveSession();
            string[] pendingIds = session != null && session.EventRuntimeStateForSave != null
                ? session.EventRuntimeStateForSave.pendingEventIds
                : null;
            if (pendingIds == null)
                pendingIds = Array.Empty<string>();

            // Absent data stays absent: with no live pending decisions the rail is
            // hidden (the events empty-state page already tells the truth).
            if (pendingIds.Length == 0)
            {
                SetActiveIfNotNull(_pendingEventsRail, false);
                return;
            }

            bool eventsPage = _currentPage == ReferenceGalleryPage.Events;
            if (_pendingEventsRail == null)
            {
                _pendingEventsRail = new GameObject("ReferencePendingEventsRail",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                _pendingEventsRail.transform.SetParent(
                    transform.parent != null ? transform.parent : transform, false);
            }
            _pendingEventsRail.SetActive(true);
            _pendingEventsRail.transform.SetAsLastSibling();
            ClearChildren(_pendingEventsRail.transform);

            RectTransform railRect = (RectTransform)_pendingEventsRail.transform;
            railRect.anchorMin = railRect.anchorMax = railRect.pivot = new Vector2(0.5f, 0.5f);
            railRect.anchoredPosition = eventsPage ? EventsRailPosition : PendingRailPosition;
            railRect.sizeDelta = eventsPage ? EventsRailSize : PendingRailSize;
            Image railImage = _pendingEventsRail.GetComponent<Image>();
            railImage.color = SengokuUiTheme.IndigoRail;
            railImage.raycastTarget = false;
            SengokuUiArt.ApplyIndigoPatternOverlay(railImage.transform,
                "ReferencePendingRailPattern");

            CreateLabel(railRect, "PendingRailTitle", "待处理事项", Vector2.zero,
                new Vector2(0f, (eventsPage ? EventsRailSize.y : PendingRailSize.y) / 2f - 24f),
                16, SengokuUiTheme.Gold, 230f);
            CreateLabel(railRect, "PendingRailCount", pendingIds.Length.ToString(), Vector2.zero,
                new Vector2(0f, (eventsPage ? EventsRailSize.y : PendingRailSize.y) / 2f - 52f),
                24, SengokuUiTheme.Parchment, 230f);

            float railHeight = eventsPage ? EventsRailSize.y : PendingRailSize.y;
            float y = railHeight / 2f - 92f;
            for (int i = 0; i < pendingIds.Length && y > -railHeight / 2f + 30f; i++)
            {
                string title = ResolveEventTitle(session, pendingIds[i]);
                CreateLabel(railRect, "PendingEventRow_" + pendingIds[i],
                    (i + 1) + ". " + title, Vector2.zero, new Vector2(0f, y), 13,
                    SengokuUiTheme.Parchment, 230f, TextAnchor.UpperLeft, true);
                y -= 34f;
            }
        }

        private static string ResolveEventTitle(GameSession session, string eventId)
        {
            try
            {
                EventCatalogData catalog = session.EventCatalogReady ? session.EventCatalog : null;
                EventTextCatalog text = session.EventCatalogReady ? session.EventText : null;
                if (catalog?.events == null || text?.entries == null || eventId == null)
                    return eventId ?? string.Empty;
                string textId = null;
                foreach (EventDefinitionData definition in catalog.events)
                    if (definition != null &&
                        string.Equals(definition.eventId, eventId, StringComparison.Ordinal))
                    {
                        textId = definition.textId;
                        break;
                    }
                if (textId == null)
                    return eventId;
                foreach (EventTextEntry entry in text.entries)
                    if (entry != null && string.Equals(entry.textId, textId, StringComparison.Ordinal))
                        return string.IsNullOrWhiteSpace(entry.title) ? eventId : entry.title;
            }
            catch (Exception)
            {
                // Presentation lookup failures fall back to the raw live id.
            }
            return eventId;
        }

        // ------------------------------------------------------------ event page

        /// <summary>
        /// Reflows the live EventChoiceRoot modal into the reference geometry:
        /// a centred story/choice card on the Events page, a compact pending
        /// decision card inside the Household right column otherwise. The pending
        /// event itself continues to come from GameSession; nothing is hard-coded.
        /// </summary>
        private void StyleEventModal(bool eventsPage)
        {
            GameObject eventRoot = FindRootByComponent<EventChoicePanel>();
            if (eventRoot == null)
                return;
            Transform modal = FindDeep(eventRoot.transform, "EventChoiceModal");
            RectTransform modalRect = modal as RectTransform;
            if (modalRect == null)
                return;

            Vector2 position = eventsPage ? EventsModalPosition : HouseholdModalPosition;
            Vector2 size = eventsPage ? EventsModalSize : HouseholdModalSize;
            modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = position;
            modalRect.sizeDelta = size;
            Image modalImage = modalRect.GetComponent<Image>();
            if (modalImage != null)
            {
                modalImage.color = SengokuUiTheme.Parchment;
                SengokuUiArt.ApplyParchmentSurface(modalImage);
            }
            SengokuUiTheme.ApplyFont(modalRect);

            float illustrationHeight = ApplyEventIllustration(modalRect, eventsPage);

            RectTransform title = FindDeep(modalRect, "EventChoiceTitleText") as RectTransform;
            if (title != null)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.anchoredPosition = new Vector2(0f, -8f);
                title.sizeDelta = new Vector2(-24f, 40f);
                Text titleText = title.GetComponent<Text>();
                if (titleText != null)
                    titleText.color = SengokuUiTheme.Vermilion;
            }
            RectTransform body = FindDeep(modalRect, "EventChoiceBodyText") as RectTransform;
            if (body != null)
            {
                body.anchorMin = new Vector2(0f, 0.38f);
                body.anchorMax = new Vector2(1f, 1f);
                body.pivot = new Vector2(0.5f, 1f);
                body.offsetMin = new Vector2(12f, size.y * 0.08f);
                body.offsetMax = new Vector2(-12f, -(54f + illustrationHeight));
                Text bodyText = body.GetComponent<Text>();
                if (bodyText != null)
                    bodyText.color = SengokuUiTheme.Ink;
            }
            RectTransform options = FindDeep(modalRect, "EventChoiceOptions") as RectTransform;
            if (options != null)
            {
                options.anchorMin = new Vector2(0f, 0f);
                options.anchorMax = new Vector2(1f, 0.38f);
                options.pivot = new Vector2(0.5f, 0f);
                options.offsetMin = new Vector2(10f, 8f);
                options.offsetMax = new Vector2(-10f, 0f);
            }
            if (options != null)
            {
                foreach (Button button in options.GetComponentsInChildren<Button>(true))
                    if (button.gameObject.name != "EventChoiceOptionButtonTemplate")
                        SetButton(button, SengokuUiTheme.IndigoPanel, SengokuUiTheme.ParchmentLight);
            }
        }

        public const string EventIllustrationChildName = "EventChoiceIllustration";
        public const float EventIllustrationHeight = 196f;

        /// <summary>
        /// Places the original rainy-visitor illustration as the story banner of the
        /// full-size Events page card. The picture is decoration only: the live event
        /// title, body and options continue to come from GameSession, and the compact
        /// household decision card keeps the illustration hidden. Returns the banner
        /// height the story text must leave free (0 when hidden or unavailable).
        /// </summary>
        private static float ApplyEventIllustration(RectTransform modal, bool eventsPage)
        {
            Sprite art = SengokuUiArt.EventIllustration;
            bool visible = eventsPage && art != null;
            RectTransform rect = FindDeep(modal, EventIllustrationChildName) as RectTransform;
            if (rect == null && visible)
            {
                var illustrationObject = new GameObject(EventIllustrationChildName,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                illustrationObject.transform.SetParent(modal, false);
                rect = (RectTransform)illustrationObject.transform;
            }
            if (rect == null)
                return 0f;
            if (!visible)
            {
                rect.gameObject.SetActive(false);
                return 0f;
            }
            rect.gameObject.SetActive(true);
            Image image = rect.GetComponent<Image>();
            if (image == null)
                image = rect.gameObject.AddComponent<Image>();
            image.sprite = art;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -52f);
            rect.sizeDelta = new Vector2(-24f, EventIllustrationHeight);
            rect.localScale = Vector3.one;
            return EventIllustrationHeight;
        }

        // ------------------------------------------------------------ map page

        private void StyleMapPage()
        {
            if (mapPanelRoot == null)
                return;
            RectTransform root = mapPanelRoot.transform as RectTransform;
            if (root == null)
                return;
            SengokuUiTheme.ApplyFont(mapPanelRoot.transform);
            {
                Vector2 page = PageSize();
                root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(page.x * MapRootSizeFraction.x,
                    page.y * MapRootSizeFraction.y);
                root.anchoredPosition = new Vector2(MapRootPosition.x * page.x / DesignWidth,
                    MapRootPosition.y * page.y / DesignHeight);
                root.localScale = Vector3.one;
            }

            Image surface = mapPanelRoot.GetComponent<Image>();
            if (surface != null)
            {
                surface.enabled = false;
                surface.raycastTarget = false;
            }

            // Left travel-plan column.
            float columnX = -root.sizeDelta.x * 0.389f;
            SetChildRect(mapPanelRoot.transform, "DestinationNameText",
                new Vector2(columnX, 250f), new Vector2(240f, 24f));
            SetChildRect(mapPanelRoot.transform, "DestButton_0",
                new Vector2(columnX, 170f), new Vector2(240f, 56f));
            SetChildRect(mapPanelRoot.transform, "DestButton_1",
                new Vector2(columnX, 96f), new Vector2(240f, 56f));
            SetChildRect(mapPanelRoot.transform, "DestButton_2",
                new Vector2(columnX, 22f), new Vector2(240f, 56f));
            SetChildRect(mapPanelRoot.transform, "TravelTimeText",
                new Vector2(columnX, -56f), new Vector2(240f, 24f));
            SetChildRect(mapPanelRoot.transform, "ArrivalDateText",
                new Vector2(columnX, -86f), new Vector2(240f, 24f));
            SetChildRect(mapPanelRoot.transform, "ConfirmButton",
                new Vector2(columnX, -160f), new Vector2(240f, 48f));
            SetChildRect(mapPanelRoot.transform, "CancelButton",
                new Vector2(columnX, -232f), new Vector2(240f, 48f));

            // Right information column.
            float rightX = root.sizeDelta.x * 0.389f;
            SetChildRect(mapPanelRoot.transform, "CurrentDateText",
                new Vector2(rightX, 290f), new Vector2(240f, 24f));
            SetChildRect(mapPanelRoot.transform, "LocationMarkerText",
                new Vector2(rightX, 256f), new Vector2(240f, 24f));
            SetChildRect(mapPanelRoot.transform, "FeedbackText",
                new Vector2(rightX, 140f), new Vector2(240f, 200f));

            // Title stays above the central map box.
            SetChildRect(mapPanelRoot.transform, "TitleText",
                new Vector2(0f, 292f), new Vector2(600f, 36f));

            Image mapIllustration = mapPanelRoot.transform.Find(MapIllustrationChildName)
                ?.GetComponent<Image>();
            if (mapIllustration == null)
            {
                // The generated full-region map is the only illustration source. When it
                // is absent the page keeps its dark map surface instead of drawing an
                // owner-supplied reference crop.
                Sprite generatedMap = SengokuUiArt.WorldMapIllustration;
                if (generatedMap != null)
                {
                    mapIllustration = CreateMapIllustration(mapPanelRoot.transform,
                        generatedMap);
                }
            }
            if (mapIllustration != null)
            {
                mapIllustration.transform.SetAsFirstSibling();
                mapIllustration.raycastTarget = false;
                ApplyAspectFitToMapBox(mapIllustration, root.sizeDelta);
            }

            Transform clockRoot = root.parent != null ? root.parent.Find("WorldClockPanel") : null;
            if (clockRoot != null)
                clockRoot.SetAsLastSibling();

            // The map root also hosts the M5 commission page, which draws its own indigo
            // sheet, so the recursive dark-map restyle stops at that subtree.
            Transform commissionRoot =
                FindDeep(mapPanelRoot.transform, ShadowEscortCommissionPanel.RootObjectName);

            SetText(mapPanelRoot.transform, "TitleText", SengokuUiTheme.Vermilion);
            foreach (Text label in mapPanelRoot.GetComponentsInChildren<Text>(true))
                if (label.gameObject.name != "TitleText" &&
                    !IsUnder(label.transform, commissionRoot))
                    label.color = SengokuUiTheme.Ink;
            SetText(mapPanelRoot.transform, "CurrentDateText", SengokuUiTheme.Parchment);
            SetText(mapPanelRoot.transform, "LocationMarkerText", SengokuUiTheme.Parchment);
            SetText(mapPanelRoot.transform, "FeedbackText", SengokuUiTheme.Parchment);
            Button[] buttons = mapPanelRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                if (!IsUnder(buttons[i].transform, commissionRoot))
                    SetButton(buttons[i], SengokuUiTheme.ParchmentDeep, SengokuUiTheme.Ink);
            ImmersiveWorldMapView view = mapPanelRoot.GetComponent<ImmersiveWorldMapView>();
            if (view == null) view = mapPanelRoot.AddComponent<ImmersiveWorldMapView>();
            view.BindNavigation(this);
            view.Configure();
        }

        public void OpenMapFamilyRecord(bool chronicle)
        {
            ShowPage(ReferenceGalleryPage.Family);
            FamilyRosterPanel roster = familyPanelRoot != null
                ? familyPanelRoot.GetComponentInParent<FamilyRosterPanel>(true) : null;
            FamilyChroniclePanel ledger = FindAnyObjectByType<FamilyChroniclePanel>(FindObjectsInactive.Include);
            if (roster != null)
            {
                if (chronicle) roster.Close();
                else roster.Open();
            }
            if (ledger != null)
            {
                ledger.gameObject.SetActive(true);
                if (chronicle) ledger.Open();
                else ledger.Close();
            }
        }

        /// <summary>
        /// Creates the map page's central illustration as the first sibling of the
        /// map root so it renders behind every route control, non-interactive.
        /// </summary>
        private static Image CreateMapIllustration(Transform mapRoot, Sprite sprite)
        {
            var illustrationObject = new GameObject(MapIllustrationChildName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            illustrationObject.transform.SetParent(mapRoot, false);
            illustrationObject.transform.SetAsFirstSibling();
            Image illustration = illustrationObject.GetComponent<Image>();
            illustration.sprite = sprite;
            illustration.raycastTarget = false;
            return illustration;
        }

        /// <summary>
        /// Central map box: horizontal quarter-bands keep the side columns clear,
        /// the vertical range sits below the title and above the root bottom. The
        /// sprite is aspect-fit inside that box and centred, so the illustration
        /// preserves its source ratio and never covers route/travel controls.
        /// </summary>
        private static void ApplyAspectFitToMapBox(Image illustration, Vector2 rootSize)
        {
            RectTransform rect = illustration.rectTransform;
            float halfWidth = rootSize.x * MapBoxHalfWidthFraction;
            float top = rootSize.y * MapBoxTopFraction;
            float bottom = -rootSize.y / 2f;
            float boxWidth = halfWidth * 2f;
            float boxHeight = top - bottom;
            if (boxWidth <= 0f || boxHeight <= 0f)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                return;
            }

            Sprite sprite = illustration.sprite;
            float spriteWidth = sprite != null ? sprite.rect.width : boxWidth;
            float spriteHeight = sprite != null ? sprite.rect.height : boxHeight;
            if (spriteWidth <= 0f) spriteWidth = boxWidth;
            if (spriteHeight <= 0f) spriteHeight = boxHeight;
            float scale = Mathf.Min(boxWidth / spriteWidth, boxHeight / spriteHeight);
            float fitWidth = spriteWidth * scale;
            float fitHeight = spriteHeight * scale;

            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, (top + bottom) / 2f);
            rect.sizeDelta = new Vector2(fitWidth, fitHeight);
            rect.localScale = Vector3.one;
        }

        private Vector2 PageSize()
        {
            Transform parent = transform.parent;
            if (parent is RectTransform parentRect &&
                parentRect.rect.width > 8f && parentRect.rect.height > 8f)
                return new Vector2(parentRect.rect.width, parentRect.rect.height);
            return new Vector2(DesignWidth, DesignHeight);
        }

        // ------------------------------------------------------------ shared helpers

        private bool HasPendingEvent()
        {
            if (pageRoots == null)
                return false;
            for (int i = 0; i < pageRoots.Length; i++)
            {
                GameObject root = pageRoots[i].Root;
                EventChoicePanel panel = root != null ? root.GetComponent<EventChoicePanel>() : null;
                if (panel != null)
                    return panel.HasPendingEvent;
            }
            return false;
        }

        private void SetEventsEmptyState(bool visible)
        {
            if (visible && _eventsEmptyState == null)
            {
                _eventsEmptyState = new GameObject("ReferenceEventsEmptyState",
                    typeof(RectTransform), typeof(Image));
                _eventsEmptyState.transform.SetParent(
                    transform.parent != null ? transform.parent : transform, false);
                _eventsEmptyState.transform.SetAsLastSibling();
                RectTransform rect = (RectTransform)_eventsEmptyState.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-110f, -18f);
                rect.sizeDelta = new Vector2(770f, 610f);
                Image surface = _eventsEmptyState.GetComponent<Image>();
                surface.color = SengokuUiTheme.Parchment;
                surface.raycastTarget = false;
                SengokuUiArt.ApplyParchmentSurface(surface);
                GameObject queueRail = new GameObject("PendingEventsRail",
                    typeof(RectTransform), typeof(Image));
                queueRail.transform.SetParent(_eventsEmptyState.transform, false);
                RectTransform queueRect = (RectTransform)queueRail.transform;
                queueRect.anchorMin = queueRect.anchorMax = queueRect.pivot = new Vector2(0.5f, 0.5f);
                queueRect.anchoredPosition = new Vector2(610f, 0f);
                queueRect.sizeDelta = new Vector2(260f, 610f);
                Image queueImage = queueRail.GetComponent<Image>();
                queueImage.color = SengokuUiTheme.IndigoRail;
                queueImage.raycastTarget = false;
                SengokuUiArt.ApplyIndigoPatternOverlay(queueRect,
                    "ReferenceEmptyQueuePattern");
                AddEmptyStateText("Title", "待处理事件", new Vector2(0f, 245f), 26,
                    SengokuUiTheme.Vermilion);
                AddEmptyStateText("Status", "当前没有待处理事件。", new Vector2(0f, 150f), 21,
                    SengokuUiTheme.Ink);
                AddEmptyStateText("QueueTitle", "待处理事项", new Vector2(610f, 245f), 20,
                    SengokuUiTheme.Gold, 220f);
                AddEmptyStateText("QueueCount", "0", new Vector2(610f, 195f), 36,
                    SengokuUiTheme.Parchment, 220f);
                AddEmptyStateText("QueueStatus", "暂无未处理事件", new Vector2(610f, 135f), 16,
                    SengokuUiTheme.Parchment, 220f);
            }
            if (_eventsEmptyState != null)
                _eventsEmptyState.SetActive(visible);
        }

        private EventChoicePanel GetEventPanel()
        {
            GameObject root = FindRootByComponent<EventChoicePanel>();
            return root != null ? root.GetComponent<EventChoicePanel>() : null;
        }

        private GameObject FindRootByComponent<T>() where T : Component
        {
            if (pageRoots == null)
                return null;
            for (int i = 0; i < pageRoots.Length; i++)
            {
                GameObject root = pageRoots[i].Root;
                if (root != null && root.GetComponent<T>() != null)
                    return root;
            }
            return null;
        }

        private void AddEmptyStateText(string name, string value, Vector2 position, int size, Color color,
            float width = 700f)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(_eventsEmptyState.transform, false);
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 42f);
            Text text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = value;
            text.raycastTarget = false;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (UnityEngine.Application.isPlaying)
                    UnityEngine.Object.Destroy(child);
                else
                    UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private RectTransform CreateDecor(string name, Transform parent, Vector2 position,
            Vector2 size, Color color)
        {
            GameObject decor = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            decor.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)decor.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            Image image = decor.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            // Household rails and month blocks are dark surfaces: the generated
            // indigo textile reads as their surface while the flat colour stays
            // as the fallback tint underneath.
            SengokuUiArt.ApplyIndigoPatternOverlay(rect, "ReferenceDecorPattern");
            return rect;
        }

        private void CreateLabel(RectTransform parent, string name, string value, Vector2 anchor,
            Vector2 position, int size, Color color, float width,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool wrap = false)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, wrap ? 40f : 26f);
            Text text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.text = value;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            foreach (Transform child in root)
            {
                if (child.name == name)
                    return child;
                Transform found = FindDeep(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void SetChildRect(Transform root, string name, Vector2 position, Vector2 size)
        {
            if (root is RectTransform parentRect)
                SetChildRect(parentRect, name, position, size);
        }

        private static void SetChildRect(RectTransform root, string name, Vector2 position,
            Vector2 size)
        {
            Transform child = FindDeep(root, name);
            if (child is RectTransform rect)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                rect.localScale = Vector3.one;
            }
        }

        private static void SetText(Transform root, string name, Color color)
        {
            Text[] texts = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
                if (texts[i].gameObject.name == name)
                    texts[i].color = color;
        }

        private static void SetImage(Transform root, string name, Color color)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.gameObject.name == name)
                    image.color = color;
        }

        private static void SetButton(Transform root, string name, Color background, Color foreground)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button.gameObject.name == name)
                    SetButton(button, background, foreground);
        }

        private static void SetButton(Button button, Color background, Color foreground)
        {
            if (button == null)
                return;
            if (button.targetGraphic != null)
                button.targetGraphic.color = background;
            Text[] labels = button.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
                labels[i].color = foreground;
        }

        /// <summary>True when <paramref name="transform"/> sits inside (or is) the styled subtree.</summary>
        private static bool IsUnder(Transform transform, Transform ancestor)
        {
            if (ancestor == null)
                return false;
            for (Transform current = transform; current != null; current = current.parent)
                if (current == ancestor)
                    return true;
            return false;
        }

        private static void SetChildTextRect(Transform parent, string name, Vector2 position,
            Vector2 size)
        {
            Transform child = parent.Find(name);
            if (child is RectTransform rect)
            {
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
            }
        }
    }
}
