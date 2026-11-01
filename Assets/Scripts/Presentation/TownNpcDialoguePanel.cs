using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 城镇 NPC 名单与分支对话面板。
    /// 只销毁由本组件实例化并登记所有权的动态按钮。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TownNpcDialoguePanel : MonoBehaviour
    {
        [Header("Town and roster")]
        [SerializeField] private GameObject townMenuRoot;
        [SerializeField] private GameObject rosterRoot;
        [SerializeField] private Transform rosterContent;
        [SerializeField] private Button npcRowPrefab;
        [SerializeField] private Text rosterStatusText;

        [Header("Dialogue")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Transform optionsContent;
        [SerializeField] private Button optionButtonPrefab;
        [SerializeField] private Button backToRosterButton;
        [SerializeField] private Button rosterBackToTownButton;
        [SerializeField] private Button backToTownButton;

        private readonly List<OwnedButton> _rosterButtons = new List<OwnedButton>();
        private readonly List<OwnedButton> _optionButtons = new List<OwnedButton>();

        private GameSession _session;
        private WorldMapController _worldMapController;

        // 以下字段只在 TryStartDialogue 成功后写入；
        // 选择失败时保留上一个有效的本地对话状态。
        private CharacterId _selectedNpc;
        private Guid _dialogueInstanceId;
        private string _currentNodeId = string.Empty;
        private bool _dialogueEnded;
        private bool _selectionInProgress;

        private sealed class OwnedButton
        {
            public GameObject GameObject;
            public Button Button;
            public UnityAction Listener;
        }

        /// <summary>为场景 UI 或 EditMode fixture 注入引用。</summary>
        public void BindReferences(
            GameObject townRoot,
            GameObject roster,
            Transform rosterRows,
            Button npcRow,
            Text rosterStatus,
            GameObject dialogue,
            Text nodeText,
            Transform optionRows,
            Button optionButton,
            Button rosterBack,
            Button rosterBackToTown,
            Button townBack)
        {
            BindNavigationListeners(false);

            townMenuRoot = townRoot;
            rosterRoot = roster;
            rosterContent = rosterRows;
            npcRowPrefab = npcRow;
            rosterStatusText = rosterStatus;
            dialogueRoot = dialogue;
            dialogueText = nodeText;
            optionsContent = optionRows;
            optionButtonPrefab = optionButton;
            backToRosterButton = rosterBack;
            rosterBackToTownButton = rosterBackToTown;
            backToTownButton = townBack;

            BindNavigationListeners(true);
        }

        public void BindWorldMapController(WorldMapController controller)
        {
            _worldMapController = controller;
        }

        private void OnEnable()
        {
            BindNavigationListeners(true);
        }

        private void OnDisable()
        {
            BindNavigationListeners(false);
            ClearOwnedButtons(_rosterButtons);
            ClearOwnedButtons(_optionButtons);
            ResetDialogueState();
        }

        /// <summary>
        /// 显示会话当前位置的 NPC 名单。相同会话的重复打开是幂等的。
        /// </summary>
        public void OpenRoster(GameSession session)
        {
            if (session != null &&
                object.ReferenceEquals(_session, session) &&
                rosterRoot != null &&
                rosterRoot.activeSelf &&
                _rosterButtons.Count > 0 &&
                (dialogueRoot == null || !dialogueRoot.activeSelf))
            {
                return;
            }

            CloseDialogueStateOnly();
            _session = session;

            if (townMenuRoot != null)
                townMenuRoot.SetActive(false);
            if (dialogueRoot != null)
                dialogueRoot.SetActive(false);
            if (rosterRoot != null)
                rosterRoot.SetActive(true);

            RefreshRoster();
        }

        /// <summary>
        /// 关闭面板自身的名单/对话根节点。
        /// 不销毁、不启用或禁用 WorldMapController 的城镇菜单对象。
        /// </summary>
        public void Close()
        {
            ClearOwnedButtons(_rosterButtons);
            ClearOwnedButtons(_optionButtons);
            ResetDialogueState();
            _session = null;

            if (rosterRoot != null)
                rosterRoot.SetActive(false);
            if (dialogueRoot != null)
                dialogueRoot.SetActive(false);
        }

        private void RefreshRoster()
        {
            ClearOwnedButtons(_rosterButtons);

            if (rosterStatusText != null)
                rosterStatusText.text = string.Empty;

            if (_session == null)
            {
                SetRosterStatus("会话不可用，无法读取城镇居民。");
                return;
            }

            if (rosterContent == null || npcRowPrefab == null)
            {
                SetRosterStatus("NPC 列表 UI 尚未配置。");
                return;
            }

            IReadOnlyList<CharacterId> npcIds =
                TownNpcAssignments.GetNpcs(_session.CurrentLocation.Value);

            if (npcIds.Count == 0)
            {
                SetRosterStatus("此地没有登记的 NPC。");
                return;
            }

            for (int i = 0; i < npcIds.Count; i++)
            {
                CharacterId npcId = npcIds[i];

                string displayName = CharacterCatalog.TryGet(
                    npcId, out CharacterDefinition definition)
                    ? definition.DisplayName
                    : npcId.ToString();

                RelationshipResult relationship =
                    _session.TryGetRelationship(npcId);

                string relationshipValue = relationship.Succeeded
                    ? relationship.CurrentValue.ToString("+0;-0;0")
                    : "—";

                bool hasDialogue =
                    BranchingDialogueCatalog.TryGet(npcId, out _);

                // 克隆后立刻登记所有权，再检查克隆上的必要组件。
                Button row = Instantiate(npcRowPrefab, rosterContent);
                OwnedButton ownedRow = RegisterOwnedButton(row, _rosterButtons);

                Text label = row.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    DestroyOwnedButton(ownedRow, _rosterButtons);
                    continue;
                }

                label.text = $"{displayName} · 关系 {relationshipValue}" +
                             (hasDialogue ? string.Empty : " · 暂无对话");

                // 无对话 NPC 保留显示，但不注册入口监听且不可交互。
                row.interactable = hasDialogue;
                if (!hasDialogue)
                    continue;

                CharacterId capturedNpcId = npcId;
                UnityAction listener = () => SelectNpc(capturedNpcId);
                RegisterOwnedListener(ownedRow, listener);
            }
        }

        private void SelectNpc(CharacterId npcId)
        {
            // UI 点击入口也检查角色、目录配置和会话，防止过期克隆调用。
            if (_session == null ||
                !CharacterCatalog.TryGet(npcId, out _) ||
                !BranchingDialogueCatalog.TryGet(npcId, out _))
            {
                ShowDialogueError("此 NPC 当前没有可用对话。");
                return;
            }

            DialogueStartResult started = _session.TryStartDialogue(npcId);
            if (!started.Succeeded || started.DialogueInstanceId == Guid.Empty)
            {
                ShowDialogueError(string.IsNullOrEmpty(started.Error)
                    ? "无法开始对话。"
                    : started.Error);
                return;
            }

            // 仅在启动成功后提交本地对话状态。
            ClearOwnedButtons(_optionButtons);
            _selectedNpc = npcId;
            _dialogueInstanceId = started.DialogueInstanceId;
            _currentNodeId = started.CurrentNodeId;
            _dialogueEnded = false;
            _selectionInProgress = false;

            if (rosterRoot != null)
                rosterRoot.SetActive(false);
            if (dialogueRoot != null)
                dialogueRoot.SetActive(true);

            RenderCurrentNode();
        }

        private void RenderCurrentNode()
        {
            ClearOwnedButtons(_optionButtons);

            if (_session == null ||
                _dialogueInstanceId == Guid.Empty ||
                !CharacterCatalog.TryGet(_selectedNpc, out _) ||
                !BranchingDialogueCatalog.TryGet(
                    _selectedNpc, out DialogueDefinition definition) ||
                !definition.TryGetNode(_currentNodeId, out DialogueNode node))
            {
                ShowDialogueError("NPC、对话实例或当前节点不可用。");
                return;
            }

            RelationshipResult relationship =
                _session.TryGetRelationship(_selectedNpc);

            string relationshipValue = relationship.Succeeded
                ? relationship.CurrentValue.ToString("+0;-0;0")
                : "—";

            if (dialogueText != null)
                dialogueText.text = $"{node.Text}\n关系：{relationshipValue}";

            // 终端节点没有选项；结束状态也强制不显示选项。
            if (_dialogueEnded || node.Options.Count == 0)
                return;

            if (optionsContent == null || optionButtonPrefab == null)
            {
                if (dialogueText != null)
                    dialogueText.text += "\n对话选项 UI 尚未配置。";
                return;
            }

            for (int i = 0; i < node.Options.Count; i++)
            {
                DialogueOption option = node.Options[i];
                if (option == null)
                    continue;

                Button button = Instantiate(optionButtonPrefab, optionsContent);
                OwnedButton ownedOption =
                    RegisterOwnedButton(button, _optionButtons);

                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    DestroyOwnedButton(ownedOption, _optionButtons);
                    continue;
                }

                label.text = option.Text;
                string capturedNodeId = node.Id;
                DialogueOptionId capturedOptionId = option.Id;
                UnityAction listener = () =>
                    SelectOption(capturedNodeId, capturedOptionId);

                RegisterOwnedListener(ownedOption, listener);
            }
        }

        private void SelectOption(string nodeId, DialogueOptionId optionId)
        {
            // 防止旧节点、终端节点、无效实例和重复点击提交选择。
            if (_session == null ||
                _dialogueInstanceId == Guid.Empty ||
                _dialogueEnded ||
                _selectionInProgress ||
                !string.Equals(
                    nodeId, _currentNodeId, StringComparison.Ordinal))
            {
                return;
            }

            _selectionInProgress = true;
            SetOwnedButtonsInteractable(_optionButtons, false);

            DialogueSelectionResult result =
                _session.TrySelectDialogueOption(
                    _dialogueInstanceId, nodeId, optionId);

            _selectionInProgress = false;

            if (!result.Succeeded)
            {
                // 不从失败结果推进本地节点或设置终端状态。
                // GameSession 对无效/过期选择不提交关系效果。
                string error = result.Error;
                RenderCurrentNode();
                if (dialogueText != null && !string.IsNullOrEmpty(error))
                    dialogueText.text += $"\n{error}";
                return;
            }

            // 仅成功选择后推进本地对话状态并重新读取关系值。
            _currentNodeId = result.CurrentNodeId;
            _dialogueEnded = result.IsEnded;
            RenderCurrentNode();
        }

        private void OnBackToRoster()
        {
            CloseDialogueStateOnly();

            if (dialogueRoot != null)
                dialogueRoot.SetActive(false);
            if (rosterRoot != null)
                rosterRoot.SetActive(true);

            RefreshRoster();
        }

        private void OnBackToTown()
        {
            if (_worldMapController != null)
            {
                _worldMapController.OpenTownMenu();
                return;
            }

            Close();
            if (townMenuRoot != null)
                townMenuRoot.SetActive(true);
        }

        private void ShowDialogueError(string message)
        {
            ClearOwnedButtons(_optionButtons);

            if (rosterRoot != null)
                rosterRoot.SetActive(false);
            if (dialogueRoot != null)
                dialogueRoot.SetActive(true);
            if (dialogueText != null)
                dialogueText.text = message ?? string.Empty;
        }

        private void SetRosterStatus(string message)
        {
            if (rosterStatusText != null)
                rosterStatusText.text = message;
        }

        private void CloseDialogueStateOnly()
        {
            ClearOwnedButtons(_optionButtons);
            ResetDialogueState();
        }

        private void ResetDialogueState()
        {
            _selectedNpc = default;
            _dialogueInstanceId = Guid.Empty;
            _currentNodeId = string.Empty;
            _dialogueEnded = false;
            _selectionInProgress = false;
        }

        private void BindNavigationListeners(bool add)
        {
            BindButton(backToRosterButton, OnBackToRoster, add);
            BindButton(rosterBackToTownButton, OnBackToTown, add);
            BindButton(backToTownButton, OnBackToTown, add);
        }

        private static void BindButton(
            Button button, UnityAction listener, bool add)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(listener);
            if (add)
                button.onClick.AddListener(listener);
        }

        private static OwnedButton RegisterOwnedButton(
            Button button, List<OwnedButton> ownerList)
        {
            var owned = new OwnedButton
            {
                GameObject = button != null ? button.gameObject : null,
                Button = button,
                Listener = null
            };

            // 调用方在检查 Text 等克隆组件前登记所有权。
            ownerList.Add(owned);
            return owned;
        }

        private static void RegisterOwnedListener(
            OwnedButton owned, UnityAction listener)
        {
            if (owned == null || owned.Button == null || listener == null)
                return;

            owned.Listener = listener;
            owned.Button.onClick.AddListener(listener);
        }

        private static void SetOwnedButtonsInteractable(
            List<OwnedButton> buttons, bool interactable)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].Button != null)
                    buttons[i].Button.interactable = interactable;
            }
        }

        private static void ClearOwnedButtons(List<OwnedButton> ownerList)
        {
            for (int i = ownerList.Count - 1; i >= 0; i--)
            {
                OwnedButton owned = ownerList[i];
                if (owned == null)
                    continue;

                if (owned.Button != null)
                {
                    if (owned.Listener != null)
                        owned.Button.onClick.RemoveListener(owned.Listener);

                    // 先立即禁用克隆，避免 Destroy 延迟期间仍可点击。
                    owned.Button.interactable = false;
                }

                if (owned.GameObject != null)
                {
                    owned.GameObject.SetActive(false);
                    // Play Mode 延迟销毁；EditMode 即时销毁，且目标仅为已登记克隆。
                    DestroyOwnedGameObject(owned.GameObject);
                }
            }

            ownerList.Clear();
        }

        private static void DestroyOwnedButton(
            OwnedButton owned, List<OwnedButton> ownerList)
        {
            if (owned == null)
                return;

            if (owned.Button != null)
            {
                if (owned.Listener != null)
                    owned.Button.onClick.RemoveListener(owned.Listener);
                owned.Button.interactable = false;
            }

            if (owned.GameObject != null)
            {
                owned.GameObject.SetActive(false);
                DestroyOwnedGameObject(owned.GameObject);
            }

            ownerList.Remove(owned);
        }

        private static void DestroyOwnedGameObject(GameObject target)
        {
            if (target == null)
                return;

            if (UnityEngine.Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
