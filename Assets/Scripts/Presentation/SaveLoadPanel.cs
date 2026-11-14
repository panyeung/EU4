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
        private bool _subscribed;
        private bool _blockedByTacticalMission;

        /// <summary>Result of the most recent save or load.</summary>
        public bool LastOperationSucceeded { get; private set; }

        /// <summary>Current status message mirrored to the UI.</summary>
        public string Status => _status;

        /// <summary>Current interactability of the save control, for UI tests.</summary>
        public bool SaveInteractable => saveButton != null && saveButton.interactable;

        /// <summary>Current interactability of the load control, for UI tests.</summary>
        public bool LoadInteractable => loadButton != null && loadButton.interactable;

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
            if (gameSession != session)
                Unsubscribe();
            gameSession = session;
            if (store != null)
                _store = store;
            saveButton = save;
            loadButton = load;
            statusText = status;
            worldMapController = map;
            BindButtons(isActiveAndEnabled);
            Subscribe();
            RefreshTacticalGuard();
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
            Subscribe();
            RefreshTacticalGuard();
            RefreshStatus();
        }

        private void OnEnable()
        {
            BindButtons(true);
            Subscribe();
            RefreshTacticalGuard();
        }

        private void OnDisable()
        {
            BindButtons(false);
            Unsubscribe();
        }

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (gameSession != null && !_subscribed)
            {
                gameSession.SessionStateChanged += OnSessionStateChanged;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
        }

        private void OnSessionStateChanged() => RefreshTacticalGuard();

        /// <summary>
        /// The M06 operation owns a session-only state and must not be saved over or
        /// replaced, so both controls are disabled while it runs and re-enabled when it
        /// reaches a terminal result.
        /// </summary>
        private void RefreshTacticalGuard()
        {
            bool blocked = gameSession != null && gameSession.IsTacticalMissionActive;
            if (saveButton != null)
                saveButton.interactable = !blocked;
            if (loadButton != null)
                loadButton.interactable = !blocked;
            if (blocked && !_blockedByTacticalMission)
            {
                _status = "潜入进行中：无法存档或读档。";
                RefreshStatus();
            }
            else if (!blocked && _blockedByTacticalMission)
            {
                _status = string.Empty;
                RefreshStatus();
            }
            _blockedByTacticalMission = blocked;
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

            if (gameSession.IsTacticalMissionActive)
            {
                SetStatus(false, "潜入进行中：无法存档。请先完成或撤退。");
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

            if (gameSession.IsTacticalMissionActive)
            {
                SetStatus(false, "潜入进行中：无法读档。请先完成或撤退。");
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
