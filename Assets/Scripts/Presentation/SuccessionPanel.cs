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
    /// SC-010 succession HUD and decision modal. The HUD is always visible and the
    /// modal opens for a pending choice, a due adult handoff, or the terminal extinction
    /// result, mirroring the event panel's stable-root pattern so the updater can
    /// validate one fixed hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuccessionPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Text headText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button retireButton;
        [SerializeField] private Button crisisDemoButton;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text modalStatusText;
        [SerializeField] private Transform candidateContent;
        [SerializeField] private Button candidateButtonPrefab;
        [SerializeField] private Transform regentContent;
        [SerializeField] private Button regentButtonPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button acceptCrisisButton;
        [SerializeField] private Button declineCrisisButton;

        private readonly List<OwnedButton> _ownedButtons = new List<OwnedButton>();
        private bool _subscribed;
        private UnityAction _retireListener;
        private UnityAction _crisisDemoListener;
        private UnityAction _confirmListener;
        private UnityAction _acceptCrisisListener;
        private UnityAction _declineCrisisListener;
        private int _staticButtonBindings;

        private sealed class OwnedButton
        {
            public GameObject GameObject;
            public Button Button;
            public UnityAction Listener;
        }

        public bool IsBound =>
            gameSession != null && modalRoot != null && headText != null && statusText != null &&
            retireButton != null && crisisDemoButton != null && feedbackText != null &&
            modalStatusText != null && candidateContent != null && candidateButtonPrefab != null &&
            regentContent != null && regentButtonPrefab != null && confirmButton != null &&
            acceptCrisisButton != null && declineCrisisButton != null;

        public bool IsModalVisible => modalRoot != null && modalRoot.activeSelf;
        public int VisibleCandidateCount => CountActiveChildren(candidateContent);
        public int VisibleRegentCount => CountActiveChildren(regentContent);
        public bool ConfirmInteractable => confirmButton != null && confirmButton.interactable;
        public bool ConfirmVisible => confirmButton != null && confirmButton.gameObject.activeSelf;
        public bool RetireInteractable => retireButton != null && retireButton.interactable;
        public bool CrisisDemoInteractable => crisisDemoButton != null && crisisDemoButton.interactable;
        /// <summary>True while the family-extinction result is the visible terminal overlay.</summary>
        public bool IsTerminalExtinctionVisible =>
            IsModalVisible && gameSession != null && gameSession.IsFamilyExtinct;
        public bool CrisisActionsVisible =>
            acceptCrisisButton != null && acceptCrisisButton.gameObject.activeSelf &&
            declineCrisisButton != null && declineCrisisButton.gameObject.activeSelf;
        public string HeadSummary => headText != null ? headText.text : string.Empty;
        public string StatusMessage => statusText != null ? statusText.text : string.Empty;
        public string ModalStatusMessage => modalStatusText != null ? modalStatusText.text : string.Empty;
        public string FeedbackMessage => feedbackText != null ? feedbackText.text : string.Empty;

        /// <summary>
        /// Number of static scene buttons currently wired to this panel. Exposed so
        /// EditMode regressions can prove each of the five buttons is bound once.
        /// </summary>
        public int StaticButtonBindingCount => _staticButtonBindings;
        public bool StaticButtonsWired => _staticButtonBindings == 5;

        public void BindReferences(GameSession session, GameObject modal, Text head, Text status,
            Button retire, Button crisisDemo, Text feedback, Text modalStatus,
            Transform candidates, Button candidateTemplate, Transform regents, Button regentTemplate,
            Button confirm, Button acceptCrisis, Button declineCrisis)
        {
            Unsubscribe();
            ClearOwnedButtons();
            gameSession = session;
            modalRoot = modal;
            headText = head;
            statusText = status;
            retireButton = retire;
            crisisDemoButton = crisisDemo;
            feedbackText = feedback;
            modalStatusText = modalStatus;
            candidateContent = candidates;
            candidateButtonPrefab = candidateTemplate;
            regentContent = regents;
            regentButtonPrefab = regentTemplate;
            confirmButton = confirm;
            acceptCrisisButton = acceptCrisis;
            declineCrisisButton = declineCrisis;
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearOwnedButtons();
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        public void Refresh()
        {
            ClearOwnedButtons();
            if (!IsBound)
            {
                if (modalRoot != null) modalRoot.SetActive(false);
                if (statusText != null) statusText.text = "继承面板 UI 尚未完整配置。";
                return;
            }

            GameDate date = gameSession.WorldClock != null
                ? gameSession.WorldClock.CurrentDate
                : SengokuChronicle.Domain.WorldClock.NewGameDate;
            SuccessionState state = gameSession.Succession;
            headText.text = "当代家主：" + gameSession.SuccessionHeadName + "（" +
                            state.HeadAge(date) + "岁）";
            feedbackText.text = gameSession.LastSuccessionMessage ?? string.Empty;

            retireButton.interactable = gameSession.CanBeginSuccessionRetirement;
            crisisDemoButton.interactable = gameSession.CanStartCrisisDemo;

            bool regencyDue = state.IsRegencyHandoffDue(date);
            bool choiceModal = state.Phase == SuccessionPhase.AwaitingSuccessionChoice ||
                               state.Phase == SuccessionPhase.AwaitingRegentChoice;
            bool modal = choiceModal ||
                         state.Phase == SuccessionPhase.CrisisDecision ||
                         state.Phase == SuccessionPhase.Extinct ||
                         regencyDue;
            modalRoot.SetActive(modal);
            candidateContent.gameObject.SetActive(choiceModal);
            regentContent.gameObject.SetActive(state.Phase == SuccessionPhase.AwaitingRegentChoice);
            confirmButton.gameObject.SetActive(choiceModal || regencyDue);
            acceptCrisisButton.gameObject.SetActive(state.Phase == SuccessionPhase.CrisisDecision);
            declineCrisisButton.gameObject.SetActive(state.Phase == SuccessionPhase.CrisisDecision);

            Text confirmLabel = confirmButton.GetComponentInChildren<Text>(true);
            if (confirmLabel != null)
                confirmLabel.text = regencyDue ? "完成成年交权" : "确认交接";

            statusText.text = BuildStatus(state, date);
            modalStatusText.text = BuildModalStatus(state, date);
            confirmButton.interactable =
                (state.Phase == SuccessionPhase.AwaitingSuccessionChoice &&
                 !state.PendingSuccessorId.IsEmpty) ||
                (state.Phase == SuccessionPhase.AwaitingRegentChoice &&
                 !state.PendingRegentId.IsEmpty) ||
                regencyDue;
            acceptCrisisButton.interactable = state.Phase == SuccessionPhase.CrisisDecision;
            declineCrisisButton.interactable = state.Phase == SuccessionPhase.CrisisDecision;

            if (state.Phase == SuccessionPhase.AwaitingSuccessionChoice ||
                state.Phase == SuccessionPhase.AwaitingRegentChoice)
                BuildCandidateButtons(state);
            if (state.Phase == SuccessionPhase.AwaitingRegentChoice)
                BuildRegentButtons(state);
        }

        public void BeginRetirement()
        {
            if (gameSession == null) return;
            SuccessionResult result = gameSession.BeginSuccessionRetirement();
            Refresh();
            if (!result.Succeeded) feedbackText.text = result.Message;
        }

        public void SelectCandidate(PersonId personId)
        {
            if (gameSession == null) return;
            SuccessionResult result = gameSession.SelectSuccessionSuccessor(personId);
            Refresh();
            if (!result.Succeeded) feedbackText.text = result.Message;
        }

        public void SelectRegent(PersonId personId)
        {
            if (gameSession == null) return;
            SuccessionResult result = gameSession.SelectSuccessionRegent(personId);
            Refresh();
            if (!result.Succeeded) feedbackText.text = result.Message;
        }

        public void ConfirmSelection()
        {
            if (gameSession == null) return;
            SuccessionResult result = gameSession.Succession.Phase == SuccessionPhase.Regency
                ? gameSession.TryCompleteSuccessionRegency()
                : gameSession.ConfirmSuccession();
            Refresh();
            if (!result.Succeeded) feedbackText.text = result.Message;
        }

        public void AcceptCrisis()
        {
            if (gameSession == null) return;
            SuccessionResult result = gameSession.AcceptCrisisAdoption();
            Refresh();
            if (!result.Succeeded) feedbackText.text = result.Message;
        }

        public void DeclineCrisis()
        {
            if (gameSession == null) return;
            SuccessionResult result = gameSession.DeclineCrisis();
            Refresh();
            if (!result.Succeeded) feedbackText.text = result.Message;
        }

        public void StartCrisisDemo()
        {
            if (gameSession == null) return;
            bool started = gameSession.StartCrisisDemo();
            Refresh();
            if (!started)
                feedbackText.text = "只能在尚未完成任何继承的新局进入固定危机示范。";
        }

        private string BuildStatus(SuccessionState state, GameDate date)
        {
            switch (state.Phase)
            {
                case SuccessionPhase.AwaitingSuccessionChoice:
                    return "继承待定：请从合法候选人中选择继承人。";
                case SuccessionPhase.AwaitingRegentChoice:
                    return "继承人未成年：请选择一名合资格成年摄政。";
                case SuccessionPhase.Regency:
                    string restricted = state.IsRegencyHandoffDue(date)
                        ? "受限决策：时间推进已暂停，成年交权待确认；确认前不能再次使用「退休并安排继承」或更换家主。"
                        : "受限决策：摄政期间不能再次使用「退休并安排继承」或更换家主；成年时暂停并等待确认交权。";
                    return "摄政中：家主 " + gameSession.SuccessionHeadName + "（" +
                           state.HeadAge(date) + "岁），摄政 " + RegentName(state) +
                           "。" + restricted;
                case SuccessionPhase.CrisisDecision:
                    return "无合法继承人：可进行一次危机收养/重建，或放弃并结束家族。";
                case SuccessionPhase.Extinct:
                    // CompletedResultId is an internal save-stable key such as
                    // "extinction.family.sc010"; never render it in player text.
                    return "家族灭亡：家族血脉已经断绝，无人能够在世继承家业。";
                default:
                    if (gameSession.IsM2ScenarioActive)
                    {
                        string heir = gameSession.FixedHeirSummary;
                        if (gameSession.IsM2FinaleCommitted)
                            return heir + "\n第三代终局已完成；可继续经营，或按通常规则安排交接。";
                        if (gameSession.CanBeginSuccessionRetirement)
                            return heir + "\n主线条件已满足；点击「退休并安排继承」执行本次固定交接。";
                        return heir + "\n" + gameSession.SuccessionRetirementBlockReason;
                    }
                    if (state.SuccessionCount > 0)
                    {
                        // CompletedResultId is an internal, save-stable identifier such as
                        // "succession.1.person.sc021.heir"; never show it to the player.
                        string headName = gameSession.SuccessionHeadName;
                        return string.IsNullOrEmpty(headName)
                            ? "家主在位。上次继承已完成。"
                            : "家主在位。上次继承：" + headName + " 接任。";
                    }
                    return "家主在位。可退休并安排继承。";
            }
        }

        private string BuildModalStatus(SuccessionState state, GameDate date)
        {
            switch (state.Phase)
            {
                case SuccessionPhase.AwaitingSuccessionChoice:
                    string text = "合法候选人只包括存活、未退休的直系/旁系亲族及养子养女。";
                    if (gameSession.IsM2ScenarioActive)
                        text = gameSession.FixedHeirSummary + "\n" + text;
                    if (!state.DesignatedHeirId.IsEmpty &&
                        !SuccessionService.IsEligibleSuccessor(state, state.DesignatedHeirId, date))
                        text += "\n原指定继承人已死亡、离族或失去资格，必须重新选择。";
                    return text;
                case SuccessionPhase.AwaitingRegentChoice:
                    return "未成年继承人必须先指定成年摄政；摄政可为配偶、亲族或重要家臣。";
                case SuccessionPhase.Regency:
                    return state.IsRegencyHandoffDue(date)
                        ? "幼主已成年，摄政结束条件已满足。时间推进暂停，请点击「完成成年交权」把控制权交还家主。"
                        : string.Empty;
                case SuccessionPhase.CrisisDecision:
                    return "条件：家族中至少一名在世成年成员作保荐人。\n" +
                           "代价：收养一名成年养子并记录家族危机；放弃则家族灭亡。";
                case SuccessionPhase.Extinct:
                    return "家族已经灭亡：无法再推进时间或旅行，也没有可继续的继承选择。\n" +
                           "如需继续游玩，请读取较早的存档以回到灭亡前。";
                default:
                    return string.Empty;
            }
        }

        private string RegentName(SuccessionState state) =>
            state.World.TryGetPerson(state.PendingRegentId, out PersonState regent)
                ? regent.DisplayName
                : state.PendingRegentId.Value;

        private void BuildCandidateButtons(SuccessionState state)
        {
            foreach (SuccessionCandidate candidate in gameSession.SuccessionCandidates)
            {
                Button button = Instantiate(candidateButtonPrefab, candidateContent);
                OwnedButton owned = Register(button);
                button.gameObject.name = "SuccessionCandidate_" + candidate.PersonId.Value;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    DestroyOwned(owned);
                    continue;
                }

                string marker = candidate.PersonId == state.PendingSuccessorId ? " [已选]" : string.Empty;
                label.text = candidate.DisplayName + "（" + candidate.Age + "岁，" +
                             candidate.RoleName + "，" + (candidate.IsAdult ? "成年" : "未成年") +
                             "）" + marker;
                button.interactable = true;
                PersonId captured = candidate.PersonId;
                UnityAction listener = () => SelectCandidate(captured);
                owned.Listener = listener;
                button.onClick.AddListener(listener);
                button.gameObject.SetActive(true);
            }
        }

        private void BuildRegentButtons(SuccessionState state)
        {
            foreach (SuccessionCandidate candidate in gameSession.SuccessionRegentCandidates)
            {
                Button button = Instantiate(regentButtonPrefab, regentContent);
                OwnedButton owned = Register(button);
                button.gameObject.name = "SuccessionRegent_" + candidate.PersonId.Value;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    DestroyOwned(owned);
                    continue;
                }

                string marker = candidate.PersonId == state.PendingRegentId ? " [已选]" : string.Empty;
                label.text = candidate.DisplayName + "（" + candidate.Age + "岁，" +
                             candidate.RoleName + "）" + marker;
                button.interactable = true;
                PersonId captured = candidate.PersonId;
                UnityAction listener = () => SelectRegent(captured);
                owned.Listener = listener;
                button.onClick.AddListener(listener);
                button.gameObject.SetActive(true);
            }
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            WireStaticButtons();
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            UnwireStaticButtons();
        }

        /// <summary>
        /// Wires the five static scene buttons exactly once. Dynamic candidate/regent
        /// buttons are owned and wired separately in BuildCandidateButtons/BuildRegentButtons.
        /// </summary>
        private void WireStaticButtons()
        {
            if (retireButton != null && _retireListener == null)
            {
                _retireListener = BeginRetirement;
                retireButton.onClick.AddListener(_retireListener);
                _staticButtonBindings++;
            }
            if (crisisDemoButton != null && _crisisDemoListener == null)
            {
                _crisisDemoListener = StartCrisisDemo;
                crisisDemoButton.onClick.AddListener(_crisisDemoListener);
                _staticButtonBindings++;
            }
            if (confirmButton != null && _confirmListener == null)
            {
                _confirmListener = ConfirmSelection;
                confirmButton.onClick.AddListener(_confirmListener);
                _staticButtonBindings++;
            }
            if (acceptCrisisButton != null && _acceptCrisisListener == null)
            {
                _acceptCrisisListener = AcceptCrisis;
                acceptCrisisButton.onClick.AddListener(_acceptCrisisListener);
                _staticButtonBindings++;
            }
            if (declineCrisisButton != null && _declineCrisisListener == null)
            {
                _declineCrisisListener = DeclineCrisis;
                declineCrisisButton.onClick.AddListener(_declineCrisisListener);
                _staticButtonBindings++;
            }
        }

        private void UnwireStaticButtons()
        {
            if (_retireListener != null)
            {
                if (retireButton != null)
                    retireButton.onClick.RemoveListener(_retireListener);
                _retireListener = null;
                _staticButtonBindings--;
            }
            if (_crisisDemoListener != null)
            {
                if (crisisDemoButton != null)
                    crisisDemoButton.onClick.RemoveListener(_crisisDemoListener);
                _crisisDemoListener = null;
                _staticButtonBindings--;
            }
            if (_confirmListener != null)
            {
                if (confirmButton != null)
                    confirmButton.onClick.RemoveListener(_confirmListener);
                _confirmListener = null;
                _staticButtonBindings--;
            }
            if (_acceptCrisisListener != null)
            {
                if (acceptCrisisButton != null)
                    acceptCrisisButton.onClick.RemoveListener(_acceptCrisisListener);
                _acceptCrisisListener = null;
                _staticButtonBindings--;
            }
            if (_declineCrisisListener != null)
            {
                if (declineCrisisButton != null)
                    declineCrisisButton.onClick.RemoveListener(_declineCrisisListener);
                _declineCrisisListener = null;
                _staticButtonBindings--;
            }

            if (_staticButtonBindings < 0)
                _staticButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            Refresh();
        }

        private OwnedButton Register(Button button)
        {
            var owned = new OwnedButton
            {
                GameObject = button != null ? button.gameObject : null,
                Button = button,
                Listener = null
            };
            _ownedButtons.Add(owned);
            return owned;
        }

        private void ClearOwnedButtons()
        {
            for (int i = _ownedButtons.Count - 1; i >= 0; i--)
                DestroyOwned(_ownedButtons[i]);
            _ownedButtons.Clear();
        }

        private void DestroyOwned(OwnedButton owned)
        {
            if (owned == null) return;
            if (owned.Button != null)
            {
                if (owned.Listener != null)
                    owned.Button.onClick.RemoveListener(owned.Listener);
                owned.Button.interactable = false;
            }
            if (owned.GameObject != null)
            {
                owned.GameObject.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(owned.GameObject);
                else
                    DestroyImmediate(owned.GameObject);
            }
        }

        private static int CountActiveChildren(Transform parent)
        {
            if (parent == null) return 0;
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).gameObject.activeSelf)
                    count++;
            return count;
        }
    }
}
