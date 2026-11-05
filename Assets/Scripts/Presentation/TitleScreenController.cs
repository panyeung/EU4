using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Phase 1 标题界面：全屏覆盖层，提供「新游戏」与「继续」。
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
        private Button newGameButton;

        [SerializeField]
        private Button continueButton;

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

        [SerializeField]
        private Text statusText;

        private SaveSlotStore _store;
        private string _status = string.Empty;

        /// <summary>Result of the most recent start/continue action.</summary>
        public bool LastOperationSucceeded { get; private set; }

        /// <summary>Current status message mirrored to the UI.</summary>
        public string Status => _status;

        /// <summary>True while the title overlay is blocking the map.</summary>
        public bool IsTitleVisible => titleRoot != null && titleRoot.activeSelf;

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
            ShowTitle();
            RefreshTitleState();
        }

        private void OnEnable()
        {
            BindButtons(true);
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
        }

        private void OnNewGameButton()
        {
            StartNewGame();
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

        /// <summary>显示标题覆盖层。</summary>
        public void ShowTitle()
        {
            if (titleRoot != null)
                titleRoot.SetActive(true);
        }

        /// <summary>隐藏标题覆盖层，把输入交还地图。</summary>
        public void HideTitle()
        {
            if (titleRoot != null)
                titleRoot.SetActive(false);
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
                SaveLoadResult loaded = _store.Load();
                if (loaded.Error == SaveErrorCode.NotFound)
                    message = "尚未找到存档，可开始新游戏。";
                else if (!loaded.Succeeded)
                    message = loaded.Message;
                else
                {
                    canContinue = true;
                    message = "检测到存档，可继续上次进度。";
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
    }
}
