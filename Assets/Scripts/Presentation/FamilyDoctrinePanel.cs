using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// SC-024 family-doctrine panel. It projects <see cref="FamilyDoctrineView"/> and only
    /// submits the current head's one ambition through
    /// <see cref="GameSession.TryStartCurrentHeadAmbition"/>. Creed changes stay exclusive to
    /// the family council event, so this panel never edits the creed or save state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FamilyDoctrinePanel : MonoBehaviour
    {
        private const int AmbitionChoiceCount = 3;

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text creedText;
        [SerializeField] private Text creedChoicesText;
        [SerializeField] private Text effectText;
        [SerializeField] private Text headText;
        [SerializeField] private Text ambitionText;
        [SerializeField] private Text blockReasonText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button developHouseholdButton;
        [SerializeField] private Button passDownTechniqueButton;
        [SerializeField] private Button educateHeirButton;

        private readonly Button[] _ambitionButtons = new Button[AmbitionChoiceCount];
        private readonly UnityAction[] _ambitionListeners = new UnityAction[AmbitionChoiceCount];
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private int _ambitionButtonBindings;
        private string _lastSessionMessage = string.Empty;
        private PersonId _messageOwnerHeadId;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && creedText != null && creedChoicesText != null &&
            effectText != null && headText != null && ambitionText != null &&
            blockReasonText != null && messageText != null &&
            developHouseholdButton != null && passDownTechniqueButton != null &&
            educateHeirButton != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int RefreshCount { get; private set; }
        public int AmbitionCommitCount { get; private set; }

        public string TitleLine => titleText != null ? titleText.text : string.Empty;
        public string CreedLine => creedText != null ? creedText.text : string.Empty;
        public string CreedChoicesLine =>
            creedChoicesText != null ? creedChoicesText.text : string.Empty;
        public string EffectLine => effectText != null ? effectText.text : string.Empty;
        public string HeadLine => headText != null ? headText.text : string.Empty;
        public string AmbitionLine => ambitionText != null ? ambitionText.text : string.Empty;
        public string BlockReasonLine =>
            blockReasonText != null ? blockReasonText.text : string.Empty;
        public bool BlockReasonVisible =>
            blockReasonText != null && blockReasonText.gameObject.activeSelf;
        public string MessageLine => messageText != null ? messageText.text : string.Empty;
        public bool MessageVisible => messageText != null && messageText.gameObject.activeSelf;

        public int AmbitionChoiceCountValue => AmbitionChoiceCount;

        /// <summary>
        /// Number of ambition scene buttons currently wired to this panel. Exposed so
        /// EditMode regressions can prove each choice is bound exactly once.
        /// </summary>
        public int AmbitionButtonBindingCount => _ambitionButtonBindings;
        public bool AmbitionButtonsWired => _ambitionButtonBindings == AmbitionChoiceCount;

        public string AmbitionLabel(int index)
        {
            Button button = ButtonAt(index);
            if (button == null)
                return string.Empty;
            Text label = button.GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
        }

        public bool AmbitionInteractable(int index)
        {
            Button button = ButtonAt(index);
            return button != null && button.interactable;
        }

        /// <summary>Editor/test binding seam; it wires every reference before runtime use.</summary>
        public void BindReferences(GameSession session, GameObject root, Button open, Button close,
            Text title, Text creed, Text creedChoices, Text effect, Text head, Text ambition,
            Text blockReason, Text message, Button developHousehold, Button passDownTechnique,
            Button educateHeir)
        {
            Unsubscribe();
            _lastSessionMessage = string.Empty;
            _messageOwnerHeadId = default;
            gameSession = session;
            panelRoot = root;
            openButton = open;
            closeButton = close;
            titleText = title;
            creedText = creed;
            creedChoicesText = creedChoices;
            effectText = effect;
            headText = head;
            ambitionText = ambition;
            blockReasonText = blockReason;
            messageText = message;
            developHouseholdButton = developHousehold;
            passDownTechniqueButton = passDownTechnique;
            educateHeirButton = educateHeir;
            _ambitionButtons[0] = developHousehold;
            _ambitionButtons[1] = passDownTechnique;
            _ambitionButtons[2] = educateHeir;
            Subscribe();
            if (UnityEngine.Application.isPlaying)
                Refresh();
        }

        public void Open()
        {
            if (panelRoot == null)
                return;
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Refresh()
        {
            RefreshCount++;
            EnsureButtons();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            FamilyDoctrineView view = FamilyDoctrineView.Build(gameSession);
            titleText.text = "家训与抱负";
            creedText.text = "当前家训：" + view.CreedName + "（" + view.CreedId + "）";
            creedChoicesText.text = "可选家训：" + CreedChoices(view);
            effectText.text = "议事方式：" + view.CouncilName + "；" + view.CouncilEffect;
            headText.text = "当前家主：" +
                            (view.HeadId.IsEmpty || string.IsNullOrEmpty(view.HeadDisplayName)
                                ? "无"
                                : view.HeadDisplayName);
            ambitionText.text = view.AmbitionProgressLine;
            blockReasonText.text = view.BlockReason;
            blockReasonText.gameObject.SetActive(!string.IsNullOrWhiteSpace(view.BlockReason));
            RenderMessage(view);

            for (int i = 0; i < _ambitionButtons.Length && i < view.Ambitions.Count; i++)
            {
                FamilyAmbitionOption option = view.Ambitions[i];
                Button button = _ambitionButtons[i];
                if (button == null)
                    continue;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = option.Label;
                button.interactable = view.CanStartAmbition;
            }
        }

        /// <summary>
        /// Projects the session's doctrine feedback only for the head that produced it.
        /// The session keeps one transient message for the whole family, so without this
        /// check the previous head's selection confirmation would still be shown after a
        /// succession while the new head's own ambition is still unselected.
        /// </summary>
        private void RenderMessage(FamilyDoctrineView view)
        {
            TrackMessage(view.HeadId, view.LastMessage);
            string visible = view.LastMessage ?? string.Empty;
            if (visible.Length > 0 && !_messageOwnerHeadId.IsEmpty &&
                !_messageOwnerHeadId.Equals(view.HeadId))
                visible = string.Empty;

            messageText.text = visible;
            messageText.gameObject.SetActive(!string.IsNullOrWhiteSpace(visible));
        }

        /// <summary>
        /// Remembers which head produced the current session message. It runs on every
        /// session change, even while the panel is closed, so a confirmation raised before
        /// the new head took over is still attributed to the old head when it is finally
        /// projected.
        /// </summary>
        private void TrackMessage(PersonId headId, string message)
        {
            message = message ?? string.Empty;
            if (string.Equals(message, _lastSessionMessage, StringComparison.Ordinal))
                return;
            _lastSessionMessage = message;
            _messageOwnerHeadId = message.Length > 0 ? headId : default;
        }

        private PersonId CurrentHeadId() =>
            gameSession != null && gameSession.Succession != null
                ? gameSession.Succession.HeadPersonId
                : default;

        public void ClickAmbition(int index)
        {
            EnsureButtons();
            if (gameSession == null || index < 0 || index >= AmbitionChoiceCount)
                return;
            FamilyDoctrineView view = FamilyDoctrineView.Build(gameSession);
            if (!view.CanStartAmbition || index >= view.Ambitions.Count)
                return;

            AmbitionCommitCount++;
            if (gameSession.TryStartCurrentHeadAmbition(view.Ambitions[index].Kind))
                _messageOwnerHeadId = view.HeadId;
            if (IsOpen)
                Refresh();
        }

        public void ClickDevelopHousehold() => ClickAmbition(0);
        public void ClickPassDownTechnique() => ClickAmbition(1);
        public void ClickEducateHeir() => ClickAmbition(2);

        private static string CreedChoices(FamilyDoctrineView view)
        {
            string text = string.Empty;
            for (int i = 0; i < view.Creeds.Count; i++)
            {
                if (i > 0)
                    text += " / ";
                text += view.Creeds[i].Label;
            }
            return text;
        }

        private Button ButtonAt(int index)
        {
            if (index < 0 || index >= AmbitionChoiceCount)
                return null;
            EnsureButtons();
            if (_ambitionButtons[index] == null)
            {
                string[] names = { "developHouseholdButton", "passDownTechniqueButton",
                    "educateHeirButton" };
                Debug.LogWarning("FamilyDoctrinePanel has no ambition button at index " + index +
                                 " (" + names[index] + ").", this);
                return null;
            }
            return _ambitionButtons[index];
        }

        private void EnsureButtons()
        {
            if (_ambitionButtons[0] == null)
                _ambitionButtons[0] = developHouseholdButton;
            if (_ambitionButtons[1] == null)
                _ambitionButtons[1] = passDownTechniqueButton;
            if (_ambitionButtons[2] == null)
                _ambitionButtons[2] = educateHeirButton;
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            if (openButton != null && _openListener == null)
            {
                _openListener = Open;
                openButton.onClick.AddListener(_openListener);
            }
            if (closeButton != null && _closeListener == null)
            {
                _closeListener = Close;
                closeButton.onClick.AddListener(_closeListener);
            }
            WireAmbitionButtons();
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            if (openButton != null && _openListener != null)
                openButton.onClick.RemoveListener(_openListener);
            _openListener = null;
            if (closeButton != null && _closeListener != null)
                closeButton.onClick.RemoveListener(_closeListener);
            _closeListener = null;
            UnwireAmbitionButtons();
        }

        /// <summary>Wires the three static scene buttons exactly once.</summary>
        private void WireAmbitionButtons()
        {
            EnsureButtons();
            for (int i = 0; i < AmbitionChoiceCount; i++)
            {
                Button button = _ambitionButtons[i];
                if (button == null || _ambitionListeners[i] != null)
                    continue;
                int index = i;
                UnityAction listener = () => ClickAmbition(index);
                _ambitionListeners[i] = listener;
                button.onClick.AddListener(listener);
                _ambitionButtonBindings++;
            }
        }

        private void UnwireAmbitionButtons()
        {
            EnsureButtons();
            for (int i = 0; i < AmbitionChoiceCount; i++)
            {
                if (_ambitionListeners[i] == null)
                    continue;
                if (_ambitionButtons[i] != null)
                    _ambitionButtons[i].onClick.RemoveListener(_ambitionListeners[i]);
                _ambitionListeners[i] = null;
                _ambitionButtonBindings--;
            }
            if (_ambitionButtonBindings < 0)
                _ambitionButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            if (gameSession != null)
                TrackMessage(CurrentHeadId(), gameSession.LastFamilyDoctrineMessage);
            // Entering SC-024 must not steal focus; the title entry only starts the route.
            if (IsOpen)
                Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            if (UnityEngine.Application.isPlaying)
            {
                if (gameSession != null)
                    TrackMessage(CurrentHeadId(), gameSession.LastFamilyDoctrineMessage);
                Refresh();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (IsOpen && panelRoot != gameObject)
                panelRoot.SetActive(false);
        }
    }
}
