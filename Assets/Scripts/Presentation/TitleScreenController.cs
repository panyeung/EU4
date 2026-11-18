// Unity 6.6 deprecates the DEVELOPMENT_BUILD directive (UAC0009). The approved
// Debug Mode plan requires a compile-time boundary that exists only in the Editor
// and development builds, so the directive stays and its deprecation notice is
// silenced for this file only.
#pragma warning disable UAC0009
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Phase 1 标题界面：全屏覆盖层。主菜单只提供「继续游戏」与「开始新游戏」；
    /// 「开始新游戏」打开剧本选择页，由选择页启动既有剧本，不在打开时改动会话。
    /// 继续直接读取 M5 单槽存档；缺档或坏档只在标题上报告，会话与槽文件保持不变。
    /// 所有游戏状态都在 GameSession 与 SaveSlotStore 中；本组件只转发输入并显示结果。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GameSession gameSession;

        [SerializeField]
        private WorldMapController worldMapController;

        [Header("UI")]
        [SerializeField]
        private GameObject titleRoot;

        [SerializeField]
        private GameObject mainMenuRoot;

        [SerializeField]
        private GameObject scenarioSelectionRoot;

        [SerializeField]
        private Button newGameButton;

        [SerializeField]
        private Button continueButton;

        [Header("Scenario selection page")]
        [SerializeField]
        private Button fixedScenarioButton;

        [SerializeField]
        private Button scenarioBackButton;

        [Header("SC-013 scenario entries")]
        [SerializeField]
        private Button standardSampleButton;

        [SerializeField]
        private Button crisisDemoButton;

        [Header("SC-020 education entry")]
        [SerializeField]
        private Button educationDemoButton;

        [Header("SC-021 family research entry")]
        [SerializeField]
        private Button researchDemoButton;

        [Header("SC-022 family formation entry")]
        [SerializeField]
        private Button familyFormationDemoButton;

        [Header("SC-023 seclusion entry")]
        [SerializeField]
        private Button seclusionDemoButton;

        [Header("SC-024 family doctrine entry")]
        [SerializeField]
        private Button familyDoctrineDemoButton;

        [SerializeField]
        private Text statusText;

        private SaveSlotStore _store;
        private string _status = string.Empty;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string EventOverlayName = "EventChoiceRoot";
        private const string TacticalOverlayName = "M6TacticalMissionOverlay";

        [Header("Developer Debug Mode entry")]
        [SerializeField]
        private Button debugModeButton;

        /// <summary>
        /// The scene-authored root the guarded runtime code turns into the debug window.
        /// No <see cref="DebugModePanel"/> is ever serialized, because its type does not
        /// exist in a release player and a stored component would resolve as a missing
        /// script there.
        /// </summary>
        [SerializeField]
        private GameObject debugModeRoot;

        private DebugModePanel debugModePanel;

        /// <summary>True when the development-only Debug Mode entry is wired.</summary>
        public bool DebugModeEntryBound => debugModeButton != null;

        /// <summary>True when the development window root the entry opens is wired.</summary>
        public bool DebugModePanelBound => debugModePanel != null || debugModeRoot != null;
#endif

        /// <summary>
        /// Number of times this component has read the player slot. The ordinary startup
        /// path must leave it at zero, and the Debug Mode entry must never increase it.
        /// </summary>
        public int SlotLoadCount { get; private set; }

        /// <summary>Result of the most recent start/continue action.</summary>
        public bool LastOperationSucceeded { get; private set; }

        /// <summary>Current status message mirrored to the UI.</summary>
        public string Status => _status;

        /// <summary>True while the title overlay is blocking the map.</summary>
        public bool IsTitleVisible => titleRoot != null && titleRoot.activeSelf;

        /// <summary>True while the two-action main menu page is actually shown.</summary>
        public bool IsMainMenuVisible => IsTitleVisible && mainMenuRoot != null && mainMenuRoot.activeSelf;

        /// <summary>True while the scenario-selection page is actually shown.</summary>
        public bool IsScenarioSelectionVisible =>
            IsTitleVisible && scenarioSelectionRoot != null && scenarioSelectionRoot.activeSelf;

        /// <summary>The slot store in use; exposed for tests and diagnostics.</summary>
        public SaveSlotStore Store => _store;

        /// <summary>True when both separate SC-010 title entries are wired.</summary>
        public bool ScenarioEntriesBound => standardSampleButton != null && crisisDemoButton != null;

        /// <summary>True when the separate SC-020 education entry is wired.</summary>
        public bool EducationScenarioBound => educationDemoButton != null;

        /// <summary>True when the separate SC-021 family-research entry is wired.</summary>
        public bool ResearchScenarioBound => researchDemoButton != null;

        /// <summary>True when the separate SC-022 family-formation entry is wired.</summary>
        public bool FamilyFormationScenarioBound => familyFormationDemoButton != null;

        /// <summary>True when the separate SC-023 seclusion entry is wired.</summary>
        public bool SeclusionScenarioBound => seclusionDemoButton != null;

        /// <summary>True when the separate SC-024 family-doctrine entry is wired.</summary>
        public bool FamilyDoctrineScenarioBound => familyDoctrineDemoButton != null;

        /// <summary>True when the fixed SC-013 selector entry is wired.</summary>
        public bool FixedScenarioBound => fixedScenarioButton != null;

        /// <summary>True when the selector back button is wired.</summary>
        public bool BackButtonBound => scenarioBackButton != null;

        public void Bind(
            GameSession session,
            SaveSlotStore store,
            WorldMapController map,
            GameObject root,
            Button newGame,
            Button cont,
            Text status)
        {
            gameSession = session;
            if (store != null)
                _store = store;
            worldMapController = map;
            titleRoot = root;
            newGameButton = newGame;
            continueButton = cont;
            statusText = status;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>
        /// Binds the two title pages: the two-action main menu and the scenario selector.
        /// The main-menu New Game action only opens the selector; starting a scenario is
        /// reserved for the fixed-entry and existing scenario buttons.
        /// </summary>
        public void BindScenarioSelection(
            GameObject mainMenuRoot,
            GameObject scenarioSelectionRoot,
            Button fixedScenarioButton,
            Button backButton)
        {
            BindButtons(false);
            this.mainMenuRoot = mainMenuRoot;
            this.scenarioSelectionRoot = scenarioSelectionRoot;
            this.fixedScenarioButton = fixedScenarioButton;
            scenarioBackButton = backButton;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Binds the separate SC-010 standard and crisis title entries.</summary>
        public void BindScenarioButtons(Button standardSample, Button crisisDemo)
        {
            standardSampleButton = standardSample;
            crisisDemoButton = crisisDemo;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Binds the separate SC-020 child-education title entry.</summary>
        public void BindEducationScenarioButton(Button educationDemo)
        {
            educationDemoButton = educationDemo;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Binds the separate SC-021 family-research title entry.</summary>
        public void BindResearchScenarioButton(Button researchDemo)
        {
            researchDemoButton = researchDemo;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Binds the separate SC-022 family-formation title entry.</summary>
        public void BindFamilyFormationScenarioButton(Button familyFormationDemo)
        {
            familyFormationDemoButton = familyFormationDemo;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Binds the separate SC-023 seclusion title entry.</summary>
        public void BindSeclusionScenarioButton(Button seclusionDemo)
        {
            seclusionDemoButton = seclusionDemo;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Binds the separate SC-024 family-doctrine title entry.</summary>
        public void BindFamilyDoctrineScenarioButton(Button familyDoctrineDemo)
        {
            familyDoctrineDemoButton = familyDoctrineDemo;
            BindButtons(isActiveAndEnabled);
        }

        /// <summary>Editor generator / test seam for the slot store.</summary>
        public void SetStore(SaveSlotStore store)
        {
            _store = store;
        }

        private void Awake()
        {
            if (_store == null)
                _store = SaveSlotStore.CreateDefault();
        }

        private void Start()
        {
            if (gameSession == null)
                gameSession = GetComponent<GameSession>() ?? FindAnyObjectByType<GameSession>();
            if (worldMapController == null)
                worldMapController = FindAnyObjectByType<WorldMapController>();
            if (_store == null)
                _store = SaveSlotStore.CreateDefault();

            BindButtons(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // A scene may assign the window in the Inspector; bind it here as well so the
            // reset/exit event is always observed even when no code calls the bind method.
            BindDebugModePanel(debugModePanel);
#endif
            ShowTitle();
            // The startup path never inspects the save slot: only an explicit continue
            // attempt reads it, so a Debug Mode entry can start without touching it.
            SetStatus("选择开始方式。继续游戏会读取存档，开始新游戏不会。");
            SengokuUiTheme.ApplyFont(titleRoot != null ? titleRoot.transform : transform);
        }

        private void OnEnable()
        {
            BindButtons(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BindDebugModePanel(debugModePanel);
#endif
        }

        private void OnDisable()
        {
            BindButtons(false);
        }

        private void BindButtons(bool add)
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(OnNewGameButton);
                if (add)
                    newGameButton.onClick.AddListener(OnNewGameButton);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueButton);
                if (add)
                    continueButton.onClick.AddListener(OnContinueButton);
            }

            if (fixedScenarioButton != null)
            {
                fixedScenarioButton.onClick.RemoveListener(OnFixedScenarioButton);
                if (add)
                    fixedScenarioButton.onClick.AddListener(OnFixedScenarioButton);
            }

            if (scenarioBackButton != null)
            {
                scenarioBackButton.onClick.RemoveListener(OnScenarioBackButton);
                if (add)
                    scenarioBackButton.onClick.AddListener(OnScenarioBackButton);
            }

            if (standardSampleButton != null)
            {
                standardSampleButton.onClick.RemoveListener(OnStandardSampleButton);
                if (add)
                    standardSampleButton.onClick.AddListener(OnStandardSampleButton);
            }

            if (crisisDemoButton != null)
            {
                crisisDemoButton.onClick.RemoveListener(OnCrisisDemoButton);
                if (add)
                    crisisDemoButton.onClick.AddListener(OnCrisisDemoButton);
            }

            if (educationDemoButton != null)
            {
                educationDemoButton.onClick.RemoveListener(OnEducationDemoButton);
                if (add)
                    educationDemoButton.onClick.AddListener(OnEducationDemoButton);
            }

            if (researchDemoButton != null)
            {
                researchDemoButton.onClick.RemoveListener(OnResearchDemoButton);
                if (add)
                    researchDemoButton.onClick.AddListener(OnResearchDemoButton);
            }

            if (familyFormationDemoButton != null)
            {
                familyFormationDemoButton.onClick.RemoveListener(OnFamilyFormationDemoButton);
                if (add)
                    familyFormationDemoButton.onClick.AddListener(OnFamilyFormationDemoButton);
            }

            if (seclusionDemoButton != null)
            {
                seclusionDemoButton.onClick.RemoveListener(OnSeclusionDemoButton);
                if (add)
                    seclusionDemoButton.onClick.AddListener(OnSeclusionDemoButton);
            }

            if (familyDoctrineDemoButton != null)
            {
                familyDoctrineDemoButton.onClick.RemoveListener(OnFamilyDoctrineDemoButton);
                if (add)
                    familyDoctrineDemoButton.onClick.AddListener(OnFamilyDoctrineDemoButton);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugModeButton != null)
            {
                debugModeButton.onClick.RemoveListener(OnDebugModeButton);
                if (add)
                    debugModeButton.onClick.AddListener(OnDebugModeButton);
            }
#endif
        }

        private void OnNewGameButton()
        {
            // A scene saved before the two-page migration still has no selector root.
            // Keep the old direct-start behaviour there instead of silently doing nothing.
            if (scenarioSelectionRoot == null)
            {
                Debug.LogWarning("M6ScenarioSelectRoot is missing; starting SC-013 directly. " +
                                 "Run Tools > Sengoku Chronicle/Add M6 Title Screen to migrate.");
                StartNewGame();
                return;
            }

            ShowScenarioSelection();
        }

        private void OnFixedScenarioButton()
        {
            StartNewGame();
        }

        private void OnScenarioBackButton()
        {
            ShowMainMenu();
        }

        private void OnContinueButton()
        {
            TryContinue();
        }

        private void OnStandardSampleButton()
        {
            StartStandardSample();
        }

        private void OnCrisisDemoButton()
        {
            StartCrisisDemo();
        }

        private void OnEducationDemoButton()
        {
            StartEducationDemo();
        }

        private void OnResearchDemoButton()
        {
            StartResearchDemo();
        }

        private void OnFamilyFormationDemoButton()
        {
            StartFamilyFormationDemo();
        }

        private void OnSeclusionDemoButton()
        {
            StartSeclusionDemo();
        }

        private void OnFamilyDoctrineDemoButton()
        {
            StartFamilyDoctrineDemo();
        }

        /// <summary>显示标题覆盖层，并总是回到两动作主菜单。</summary>
        public void ShowTitle()
        {
            if (titleRoot != null)
                titleRoot.SetActive(true);
            ShowMainMenu();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RefreshDebugEntry();
#endif
        }

        /// <summary>显示主菜单页并隐藏剧本选择页；不启动或改动任何剧本/存档状态。</summary>
        public void ShowMainMenu()
        {
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(true);
            if (scenarioSelectionRoot != null)
                scenarioSelectionRoot.SetActive(false);
        }

        /// <summary>
        /// 打开剧本选择页。此操作只切换标题内部页面，
        /// 不初始化剧本、不读取或写入存档，也不改动会话。
        /// </summary>
        public void ShowScenarioSelection()
        {
            if (titleRoot != null)
                titleRoot.SetActive(true);
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);
            if (scenarioSelectionRoot != null)
                scenarioSelectionRoot.SetActive(true);
        }

        /// <summary>隐藏标题覆盖层，把输入交还地图。</summary>
        public void HideTitle()
        {
            if (titleRoot != null)
                titleRoot.SetActive(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RefreshDebugEntry();
#endif
        }

        /// <summary>
        /// 依据槽位状态刷新「继续」按钮与提示：缺档/坏档禁用并显示具体原因，
        /// 有效存档启用并提示可继续。只读取存档，不改动会话。
        /// </summary>
        public void RefreshTitleState()
        {
            bool canContinue = false;
            string message;

            if (_store == null)
                message = "存档槽未初始化。";
            else
            {
                SlotLoadCount++;
                SaveLoadResult loaded = _store.Load();
                if (loaded.Error == SaveErrorCode.NotFound)
                    message = "尚未找到存档，可开始新游戏。";
                else if (!loaded.Succeeded)
                    message = loaded.Message;
                else
                {
                    canContinue = true;
                    message = "检测到存档，可继续上次进度。" + DescribeLoadedCampaign(loaded.Data);
                }
            }

            if (continueButton != null)
                continueButton.interactable = canContinue;
            SetStatus(message);
        }

        /// <summary>新游戏：重置会话到 1560-04-01 清洲，刷新地图并隐藏标题。</summary>
        public bool StartNewGame()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法开始新游戏。");
                return false;
            }

            gameSession.InitNewGame();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-013 固定三代剧本。");
            return true;
        }

        /// <summary>Starts the separate SC-010 standard sample.</summary>
        public bool StartStandardSample()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法开始标准样例。");
                return false;
            }

            gameSession.InitStandardDemo();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-010 标准样例。");
            return true;
        }

        /// <summary>Starts the separate SC-010 crisis demo from a fresh standard state.</summary>
        public bool StartCrisisDemo()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入危机示范。");
                return false;
            }

            bool started = gameSession.StartCrisisDemoScenario();
            if (!started)
            {
                LastOperationSucceeded = false;
                SetStatus("危机示范无法开始。");
                return false;
            }

            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已进入 SC-010 危机示范。");
            return true;
        }

        /// <summary>Starts the separate fixed SC-020 child-education demonstration.</summary>
        public bool StartEducationDemo()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入子女培养示范。");
                return false;
            }

            gameSession.InitEducationDemoScenario();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-020 子女培养示范。");
            return true;
        }

        /// <summary>Starts the separate fixed SC-021 family-research demonstration.</summary>
        public bool StartResearchDemo()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入家族研究示范。");
                return false;
            }

            gameSession.InitResearchDemoScenario();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-021 家族研究示范。");
            return true;
        }

        /// <summary>Starts the separate fixed SC-022 marriage and adoption demonstration.</summary>
        public bool StartFamilyFormationDemo()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入婚姻与收养示范。");
                return false;
            }

            gameSession.InitFamilyFormationDemoScenario();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-022 婚姻与收养示范。");
            return true;
        }

        /// <summary>Starts the separate fixed SC-023 seclusion and mentorship demonstration.</summary>
        public bool StartSeclusionDemo()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入隐居与后见人示范。");
                return false;
            }

            gameSession.InitSeclusionDemoScenario();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-023 隐居与后见人示范。");
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Binds the development-only Debug Mode entry; release builds have no such API.</summary>
        public void BindDebugModeEntry(Button button)
        {
            if (debugModeButton != null)
                debugModeButton.onClick.RemoveListener(OnDebugModeButton);
            debugModeButton = button;
            BindButtons(isActiveAndEnabled);
            RefreshDebugEntry();
        }

        /// <summary>
        /// Places and shows the development entry for the current screen. On the title page
        /// it belongs under the title root; while a debug session is live and the title is
        /// hidden it moves to the root Canvas immediately below the forced-event and tactical
        /// overlays so it can never cover modal UI. It stays hidden while the window is open
        /// and never appears during ordinary gameplay.
        /// </summary>
        private void RefreshDebugEntry()
        {
            if (debugModeButton == null)
                return;
            bool titleVisible = titleRoot == null || IsTitleVisible;
            bool panelOpen = debugModePanel != null && debugModePanel.IsOpen;
            bool sessionActive = gameSession != null && gameSession.IsDebugSessionActive;

            PlaceDebugEntry(titleVisible, sessionActive);

            bool visible = titleVisible || (sessionActive && !panelOpen);
            if (debugModeButton.gameObject.activeSelf != visible)
                debugModeButton.gameObject.SetActive(visible);
        }

        private void PlaceDebugEntry(bool titleVisible, bool sessionActive)
        {
            Transform title = titleRoot != null ? titleRoot.transform : null;
            if (title != null && (titleVisible || !sessionActive))
            {
                if (debugModeButton.transform.parent != title)
                    debugModeButton.transform.SetParent(title, false);
                debugModeButton.transform.SetAsLastSibling();
                return;
            }

            Canvas canvas = title != null ? title.GetComponentInParent<Canvas>(true) : null;
            Transform host = canvas != null ? canvas.rootCanvas.transform : null;
            if (host == null && debugModePanel != null)
                host = debugModePanel.transform.parent;
            if (host == null)
                return;
            if (debugModeButton.transform.parent != host)
                debugModeButton.transform.SetParent(host, false);

            Transform eventRoot = host.Find(EventOverlayName);
            if (eventRoot != null)
            {
                PlaceBefore(host, eventRoot);
                return;
            }
            Transform tactical = host.Find(TacticalOverlayName);
            if (tactical != null)
            {
                PlaceBefore(host, tactical);
                return;
            }
            if (title != null && title.parent == host)
            {
                int titleIndex = title.GetSiblingIndex();
                if (debugModeButton.transform.GetSiblingIndex() != titleIndex + 1)
                    debugModeButton.transform.SetSiblingIndex(titleIndex + 1);
            }
        }

        /// <summary>
        /// Puts the entry in the slot immediately before one modal marker. Idempotent: a
        /// second call with the entry already in place leaves the sibling order alone.
        /// </summary>
        private void PlaceBefore(Transform host, Transform marker)
        {
            int markerIndex = marker.GetSiblingIndex();
            if (debugModeButton.transform.parent == host &&
                debugModeButton.transform.GetSiblingIndex() == markerIndex - 1)
                return;
            debugModeButton.transform.SetSiblingIndex(markerIndex);
        }

        /// <summary>
        /// Binds the development window the Debug entry must open and listens for its reset
        /// and exit, so leaving the debug session always restores the start page. The
        /// subscription is reset on every call, which also covers a window that only the
        /// Inspector assigned: the serialized field never passes through a "changed" branch.
        /// </summary>
        public void BindDebugModePanel(DebugModePanel panel)
        {
            if (debugModePanel != null)
            {
                debugModePanel.DebugSessionEnded -= OnDebugSessionEnded;
                debugModePanel.WindowVisibilityChanged -= OnDebugWindowVisibilityChanged;
            }
            debugModePanel = panel;
            if (debugModePanel != null)
            {
                debugModePanel.DebugSessionEnded += OnDebugSessionEnded;
                debugModePanel.WindowVisibilityChanged += OnDebugWindowVisibilityChanged;
            }
            // Re-place the entry now that the window exists: it must sit under the title
            // root on the title page and below the modal overlays over the game.
            RefreshDebugEntry();
        }

        private void OnDebugWindowVisibilityChanged()
        {
            RefreshDebugEntry();
        }

        /// <summary>
        /// Creates the dev window component on the scene root the first time it is needed.
        /// Attaching it at runtime keeps the serialized FoundationScene free of a type that
        /// a release player does not compile, so no missing-script component is ever saved
        /// or shown.
        /// </summary>
        private DebugModePanel EnsureDebugPanel()
        {
            if (debugModePanel != null)
                return debugModePanel;
            if (debugModeRoot == null)
                return null;
            debugModePanel = debugModeRoot.GetComponent<DebugModePanel>();
            if (debugModePanel == null)
                debugModePanel = debugModeRoot.AddComponent<DebugModePanel>();
            debugModePanel.BindSceneRoot(gameSession, debugModeRoot, debugModeButton);
            BindDebugModePanel(debugModePanel);
            return debugModePanel;
        }

        private void OnDebugSessionEnded(string message)
        {
            ShowTitle();
            SetStatus(string.IsNullOrEmpty(message)
                ? "已离开 Debug 模式：临时会话已丢弃。"
                : message);
        }

        private void OnDestroy()
        {
            if (debugModePanel != null)
            {
                debugModePanel.DebugSessionEnded -= OnDebugSessionEnded;
                debugModePanel.WindowVisibilityChanged -= OnDebugWindowVisibilityChanged;
            }
        }

        private void OnDebugModeButton()
        {
            StartDebugSession();
        }

        /// <summary>
        /// Opens the isolated Debug Mode through its window. The window owns session start,
        /// so the title entry can never enter a session without a visible panel, and it
        /// never reads the player slot or writes a save.
        /// </summary>
        public bool StartDebugSession()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入 Debug 模式。");
                return false;
            }

            // 只在标题页可见时允许进入调试会话；会话已激活时则允许重开窗口。
            // 场景里的入口是普通 UnityEvent，验证脚本或任意代码可以直接 onClick.Invoke()，
            // 即使入口在隐藏的标题层下 active 为假也拦不住；该路径曾在普通战役中打开
            // 调试会话，使玩家槽存档被拒。没有 titleRoot 的历史场景沿用「标题视为可见」
            // 的既有判定，与 RefreshDebugEntry() 一致。
            bool titleShown = titleRoot == null || IsTitleVisible;
            if (!titleShown && !gameSession.IsDebugSessionActive)
            {
                LastOperationSucceeded = false;
                SetStatus("Debug 入口只在标题页可用，当前未进入调试会话。");
                return false;
            }

            DebugModePanel panel = EnsureDebugPanel();
            if (panel == null)
            {
                LastOperationSucceeded = false;
                SetStatus("Debug 测试窗口未接入，未进入调试会话。");
                return false;
            }

            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            panel.Open();
            RefreshDebugEntry();
            LastOperationSucceeded = true;
            SetStatus("已进入 Debug 模式：会话只驻留内存，不会读写玩家存档。");
            return true;
        }
#endif

        /// <summary>Starts the separate fixed SC-024 family-creed and ambition demonstration.</summary>
        public bool StartFamilyDoctrineDemo()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法进入家训与抱负示范。");
                return false;
            }

            gameSession.InitFamilyDoctrineDemoScenario();
            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已开始 SC-024 家训与抱负示范。");
            return true;
        }

        /// <summary>
        /// 继续：读取单槽存档并原子应用。失败时标题保持、会话不变；
        /// 成功时刷新地图后隐藏标题。
        /// </summary>
        public bool TryContinue()
        {
            if (gameSession == null)
            {
                LastOperationSucceeded = false;
                SetStatus("缺少 GameSession，无法继续游戏。");
                return false;
            }

            if (_store == null)
            {
                LastOperationSucceeded = false;
                SetStatus("存档槽未初始化。");
                return false;
            }

            SlotLoadCount++;
            SaveOperationResult result = gameSession.LoadFrom(_store);
            if (!result.Succeeded)
            {
                LastOperationSucceeded = false;
                SetStatus(result.Message);
                return false;
            }

            if (worldMapController != null)
                worldMapController.ReturnToMap();
            HideTitle();
            LastOperationSucceeded = true;
            SetStatus("已继续上次进度。");
            return true;
        }

        private void SetStatus(string message)
        {
            _status = message ?? string.Empty;
            if (statusText != null)
                statusText.text = _status;
        }

        /// <summary>
        /// Adds the persisted campaign origin to the continue hint. A migrated legacy save
        /// with an unprovable origin says so instead of implying a start date.
        /// </summary>
        private static string DescribeLoadedCampaign(SaveData data)
        {
            if (data == null || data.campaign == null)
                return " 旧档开局日期未知：不判定五十五周年评价或百年名门。";
            if (!data.campaign.TryRestore(out CampaignProgress progress, out _))
                return " 存档家族纪元状态无效。";
            GameDate today = GameDate.TryParse(data.year, data.month, data.day,
                out GameDate date)
                ? date
                : default;
            return " " + CampaignReviewView.DescribeCampaign(progress, today);
        }
    }
}
#pragma warning restore UAC0009
