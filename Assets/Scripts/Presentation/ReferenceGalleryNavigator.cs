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
        public GameObject PendingEventsRail => _pendingEventsRail;

        /// <summary>
        /// Household page regions in design (canvas-centred) units: centre column
        /// holds the appointments/industry panel with the month-event block below
        /// it, the left rail lists the live roster, the right rail pending decisions.
        /// </summary>
        public static readonly Vector2 HouseholdIndustrySize = new Vector2(620f, 456f);
        public static readonly Vector2 HouseholdIndustryPosition = new Vector2(38f, 48f);
        public static readonly Vector2 HouseholdMonthEventSize = new Vector2(620f, 116f);
        public static readonly Vector2 HouseholdMonthEventPosition = new Vector2(38f, -296f);
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
                RestoreCurrentPageVisibility();
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
            if (page != ReferenceGalleryPage.Family && familyRoster != null)
                familyRoster.Close();
            if (pageRoots != null)
                for (int i = 0; i < pageRoots.Length; i++)
                {
                    GameObject root = pageRoots[i].Root;
                    if (root != null)
                    {
                        root.SetActive((pageRoots[i].VisibleOn & mask) != 0);
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
            SetPanel(characterPanelRoot, page == ReferenceGalleryPage.Character);
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
                DailyActionPanel dailyAction = characterPanelRoot != null
                    ? characterPanelRoot.GetComponentInParent<DailyActionPanel>()
                    : null;
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
                    if (root != null && (pageRoots[i].VisibleOn & mask) != 0 &&
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

        private void StyleSharedFrame()
        {
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
                        // Centre the genealogy card between the 150px left rail
                        // and the 340px succession HUD, with no overlap at 1280x720.
                        panelRect.anchoredPosition = new Vector2(-100f, -26f);
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
                    hudRect.anchoredPosition = new Vector2(-16f, -70f);
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

        private void StyleHouseholdPage()
        {
            if (householdPanelRoot == null)
                return;
            SengokuUiTheme.ApplyFont(householdPanelRoot.transform);
            RectTransform root = householdPanelRoot.transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = root.offsetMax = Vector2.zero;
            }

            Transform industryPanel = householdPanelRoot.transform.Find("IndustryPanel");
            if (industryPanel == null && householdPanelRoot.name == "IndustryPanel")
                industryPanel = householdPanelRoot.transform;
            RectTransform panel = industryPanel as RectTransform;
            if (panel != null)
            {
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
                panel.anchoredPosition = HouseholdIndustryPosition;
                panel.sizeDelta = new Vector2(760f, 560f);
                panel.localScale = new Vector3(
                    HouseholdIndustrySize.x / 760f, HouseholdIndustrySize.y / 560f, 1f);
            }
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
            _householdComposition.SetActive(true);
            _householdComposition.transform.SetAsLastSibling();
            ClearChildren(_householdComposition.transform);

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

            // Centre bottom: this month's settlement/event text, live data only.
            List<string> monthLines = new List<string>();
            IndustryMonthReport report = session.LastIndustryReport;
            if (report != null && !string.IsNullOrWhiteSpace(report.Summary))
                monthLines.Add(report.Summary);
            if (!string.IsNullOrWhiteSpace(session.LastIndustryMessage))
                monthLines.Add(session.LastIndustryMessage);
            if (!string.IsNullOrWhiteSpace(session.LastEventMessage))
                monthLines.Add(session.LastEventMessage);
            if (monthLines.Count > 0)
            {
                RectTransform monthBlock = CreateDecor("HouseholdMonthEvent",
                    _householdComposition.transform, HouseholdMonthEventPosition,
                    HouseholdMonthEventSize, SengokuUiTheme.IndigoPanel);
                CreateLabel(monthBlock, "HouseholdMonthEventTitle", "本月", Vector2.zero,
                    new Vector2(0f, 44f), 18, SengokuUiTheme.Gold, 200f, TextAnchor.MiddleLeft);
                float y = 18f;
                for (int i = 0; i < monthLines.Count && y >= -44f; i++)
                {
                    CreateLabel(monthBlock, "HouseholdMonthEventLine_" + i, monthLines[i],
                        Vector2.zero, new Vector2(0f, y), 13, SengokuUiTheme.Parchment, 590f,
                        TextAnchor.UpperLeft, true);
                    y -= 26f;
                }
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
                Sprite generatedMap = SengokuUiArt.WorldMapIllustration;
                if (generatedMap != null)
                {
                    mapIllustration = CreateMapIllustration(mapPanelRoot.transform,
                        generatedMap);
                }
                else
                {
                    // Null-safe fallback: the owner's reference-map crop stays on
                    // the map page until the generated sprite is imported.
                    Texture2D mapTexture =
                        Resources.Load<Texture2D>("ReferenceArt/map_illustration");
                    if (mapTexture != null)
                    {
                        mapIllustration = CreateMapIllustration(mapPanelRoot.transform,
                            Sprite.Create(mapTexture,
                                new Rect(0f, 0f, mapTexture.width, mapTexture.height),
                                new Vector2(0.5f, 0.5f), 100f));
                    }
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

            SetText(mapPanelRoot.transform, "TitleText", SengokuUiTheme.Vermilion);
            foreach (Text label in mapPanelRoot.GetComponentsInChildren<Text>(true))
                if (label.gameObject.name != "TitleText")
                    label.color = SengokuUiTheme.Ink;
            SetText(mapPanelRoot.transform, "CurrentDateText", SengokuUiTheme.Parchment);
            SetText(mapPanelRoot.transform, "LocationMarkerText", SengokuUiTheme.Parchment);
            SetText(mapPanelRoot.transform, "FeedbackText", SengokuUiTheme.Parchment);
            Button[] buttons = mapPanelRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                SetButton(buttons[i], SengokuUiTheme.ParchmentDeep, SengokuUiTheme.Ink);
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
