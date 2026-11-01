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
        }

        private void OnNewGameButton()
        {
            StartNewGame();
        }

        private void OnContinueButton()
        {
            TryContinue();
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
            SetStatus("已开始新游戏。");
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
