using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Minimal single-slot save/load control: two buttons and a status line.
    /// All state lives in GameSession and SaveSlotStore; this panel only forwards input
    /// and reports the exact result.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaveLoadPanel : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GameSession gameSession;

        [SerializeField]
        private WorldMapController worldMapController;

        [Header("UI")]
        [SerializeField]
        private Button saveButton;

        [SerializeField]
        private Button loadButton;

        [SerializeField]
        private Text statusText;

        private SaveSlotStore _store;
        private string _status = string.Empty;

        /// <summary>Result of the most recent save or load.</summary>
        public bool LastOperationSucceeded { get; private set; }

        /// <summary>Current status message mirrored to the UI.</summary>
        public string Status => _status;

        /// <summary>The slot store in use; exposed for tests and diagnostics.</summary>
        public SaveSlotStore Store => _store;

        public void Bind(
            GameSession session,
            SaveSlotStore store,
            Button save,
            Button load,
            Text status,
            WorldMapController map)
        {
            gameSession = session;
            if (store != null)
                _store = store;
            saveButton = save;
            loadButton = load;
            statusText = status;
            worldMapController = map;
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
                gameSession = GetComponentInParent<GameSession>() ?? FindAnyObjectByType<GameSession>();
            if (worldMapController == null)
                worldMapController = FindAnyObjectByType<WorldMapController>();
            if (_store == null)
                _store = SaveSlotStore.CreateDefault();

            BindButtons(true);
            RefreshStatus();
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
            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(Save);
                if (add)
                    saveButton.onClick.AddListener(Save);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(Load);
                if (add)
                    loadButton.onClick.AddListener(Load);
            }
        }

        /// <summary>Saves the current session to the single slot.</summary>
        public void Save()
        {
            if (gameSession == null)
            {
                SetStatus(false, "缺少 GameSession，无法存档。");
                return;
            }

            if (_store == null)
            {
                SetStatus(false, "存档槽未初始化。");
                return;
            }

            SaveOperationResult result = gameSession.SaveTo(_store);
            SetStatus(result.Succeeded, result.Succeeded ? "已保存当前进度。" : result.Message);
        }

        /// <summary>Loads the single slot and refreshes the visible game state.</summary>
        public void Load()
        {
            if (gameSession == null)
            {
                SetStatus(false, "缺少 GameSession，无法读档。");
                return;
            }

            if (_store == null)
            {
                SetStatus(false, "存档槽未初始化。");
                return;
            }

            SaveOperationResult result = gameSession.LoadFrom(_store);
            if (!result.Succeeded)
            {
                SetStatus(false, result.Message);
                return;
            }

            if (worldMapController != null)
                worldMapController.ReturnToMap();

            SetStatus(true, "已载入存档。");
        }

        private void SetStatus(bool succeeded, string message)
        {
            LastOperationSucceeded = succeeded;
            _status = message ?? string.Empty;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText != null)
                statusText.text = _status;
        }
    }
}
