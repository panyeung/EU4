using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Events;

namespace SengokuChronicle.Presentation
{
    /// <summary>Localized, runtime-generated event choices for the current queue head.</summary>
    [DisallowMultipleComponent]
    public sealed class EventChoicePanel : MonoBehaviour
    {
        /// <summary>
        /// Post-choice feedback is a banner pinned to the canvas top edge. The map HUD
        /// owns the lower band of the 1280x720 reference canvas (its confirm/cancel row
        /// sits at canvas y=-322) and the always-visible clock panel occupies the centre,
        /// so a centered-bottom banner was drawn across live controls and became
        /// unreadable after choosing an option.
        /// </summary>
        public const float FeedbackTopMargin = 18f;
        public const float FeedbackWidth = 1180f;
        public const float FeedbackHeight = 42f;
        public const int FeedbackFontSize = 18;

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Transform optionsContent;
        [SerializeField] private Button optionButtonPrefab;
        [SerializeField] private Text feedbackText;

        private readonly List<OwnedButton> _ownedButtons = new List<OwnedButton>();
        private readonly Dictionary<string, EventTextEntry> _textById =
            new Dictionary<string, EventTextEntry>(StringComparer.Ordinal);
        private EventCatalogData _catalog;
        private EventTextCatalog _textCatalog;
        private bool _subscribed;
        private bool _selectionInProgress;
        private int _renderGeneration;

        private sealed class OwnedButton
        {
            public GameObject GameObject;
            public Button Button;
            public UnityAction Listener;
        }

        public bool IsBound => gameSession != null && _catalog != null && _textCatalog != null &&
                               modalRoot != null && titleText != null && bodyText != null &&
                               optionsContent != null && optionButtonPrefab != null && feedbackText != null;

        /// <summary>
        /// Pins the feedback banner inside the top band. Applied at runtime too so an
        /// already-generated scene picks up the layout without a scene edit.
        /// </summary>
        public static void ApplyFeedbackBannerLayout(RectTransform feedback)
        {
            if (feedback == null)
                return;
            feedback.anchorMin = feedback.anchorMax = new Vector2(0.5f, 1f);
            feedback.pivot = new Vector2(0.5f, 1f);
            feedback.anchoredPosition = new Vector2(0f, -FeedbackTopMargin);
            feedback.sizeDelta = new Vector2(FeedbackWidth, FeedbackHeight);
            feedback.localScale = Vector3.one;
            Text text = feedback.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = FeedbackFontSize;
                text.alignment = TextAnchor.UpperCenter;
                text.raycastTarget = false;
            }
        }

        /// <summary>Layout contract shared by the updater, validator, and tests.</summary>
        public static bool IsFeedbackBannerLayout(RectTransform feedback)
        {
            if (feedback == null)
                return false;
            Text text = feedback.GetComponent<Text>();
            return text != null &&
                   feedback.anchorMin == feedback.anchorMax &&
                   Mathf.Approximately(feedback.anchorMin.x, 0.5f) &&
                   Mathf.Approximately(feedback.anchorMin.y, 1f) &&
                   Mathf.Approximately(feedback.pivot.x, 0.5f) &&
                   Mathf.Approximately(feedback.pivot.y, 1f) &&
                   Mathf.Approximately(feedback.anchoredPosition.x, 0f) &&
                   Mathf.Approximately(feedback.anchoredPosition.y, -FeedbackTopMargin) &&
                   Mathf.Approximately(feedback.sizeDelta.x, FeedbackWidth) &&
                   Mathf.Approximately(feedback.sizeDelta.y, FeedbackHeight) &&
                   text.alignment == TextAnchor.UpperCenter &&
                   !text.raycastTarget;
        }

        public void BindReferences(GameSession session, EventCatalogData catalog, EventTextCatalog text,
            GameObject modal, Text title, Text body, Transform optionRows, Button optionButton, Text feedback)
        {
            Unsubscribe();
            ClearOwnedButtons();
            gameSession = session;
            _catalog = catalog;
            _textCatalog = text;
            modalRoot = modal;
            titleText = title;
            bodyText = body;
            optionsContent = optionRows;
            optionButtonPrefab = optionButton;
            feedbackText = feedback;
            RebuildTextLookup();
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
            // GameSession Awake order is not guaranteed against UI; refresh once after
            // all scene objects have completed Awake so resource DTOs are available.
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
            if (feedbackText != null)
                ApplyFeedbackBannerLayout(feedbackText.rectTransform);
            _renderGeneration++;
            ClearOwnedButtons();
            ResolveSessionContent();

            if (modalRoot == null || titleText == null || bodyText == null ||
                optionsContent == null || optionButtonPrefab == null)
            {
                if (modalRoot != null) modalRoot.SetActive(false);
                SetFeedback("事件面板 UI 尚未完整配置。");
                return;
            }
            if (gameSession == null)
            {
                modalRoot.SetActive(false);
                SetFeedback("游戏会话不可用，无法显示事件。");
                return;
            }
            if (_catalog == null || _textCatalog == null)
            {
                modalRoot.SetActive(false);
                SetFeedback(string.IsNullOrWhiteSpace(gameSession.LastEventMessage)
                    ? "事件目录尚未加载。"
                    : gameSession.LastEventMessage);
                return;
            }

            string eventId = gameSession.PendingEventId;
            if (string.IsNullOrEmpty(eventId))
            {
                modalRoot.SetActive(false);
                titleText.text = string.Empty;
                bodyText.text = string.Empty;
                SetFeedback(gameSession.LastEventMessage);
                return;
            }

            EventDefinitionData definition = FindEvent(eventId);
            if (definition == null || !TryGetText(definition.textId, out EventTextEntry eventText))
            {
                modalRoot.SetActive(false);
                SetFeedback("待处理事件或本地化文字不存在：" + eventId);
                return;
            }

            modalRoot.SetActive(true);
            titleText.text = eventText.title;
            bodyText.text = eventText.body;
            SetFeedback(string.Empty);

            int generation = _renderGeneration;
            for (int i = 0; i < definition.options.Length; i++)
            {
                EventOptionData option = definition.options[i];
                if (option == null || !TryGetText(option.textId, out EventTextEntry optionText))
                    continue;

                Button button = Instantiate(optionButtonPrefab, optionsContent);
                OwnedButton owned = RegisterOwnedButton(button);
                button.gameObject.name = "EventOption_" + option.optionId;
                Text label = button.GetComponentInChildren<Text>(true);
                if (label == null)
                {
                    DestroyOwnedButton(owned);
                    continue;
                }

                bool available = gameSession.TryGetEventOptionAvailability(
                    eventId, option.optionId, out bool enabled, out string reason) && enabled;
                label.text = FormatOptionLabel(option, optionText, available, reason);
                button.interactable = available;
                if (available)
                {
                    string capturedEventId = eventId;
                    string capturedOptionId = option.optionId;
                    UnityAction listener = () => HandleChoice(capturedEventId, capturedOptionId, generation);
                    owned.Listener = listener;
                    button.onClick.AddListener(listener);
                }
                button.gameObject.SetActive(true);
            }

            if (_ownedButtons.Count == 0)
                SetFeedback("当前事件没有可显示的选项。");
        }

        /// <summary>
        /// Resolves only the current pending event. The GameSession rechecks all
        /// requirements and the stable result ID, so this also safely rejects stale UI calls.
        /// </summary>
        public bool TryChooseOption(string eventId, string optionId)
        {
            if (!IsBound || _selectionInProgress || gameSession.PendingEventId != eventId)
                return false;
            if (!gameSession.TryGetEventOptionAvailability(eventId, optionId,
                    out bool enabled, out string reason) || !enabled)
            {
                SetFeedback(string.IsNullOrWhiteSpace(reason) ? "此选项当前不可用。" : reason);
                return false;
            }

            _selectionInProgress = true;
            SetOwnedButtonsInteractable(false);
            string resultId = gameSession.CreateEventResultId(eventId, optionId);
            EventChoiceResult result = gameSession.TryChooseEventOption(eventId, optionId, resultId);
            _selectionInProgress = false;
            if (!result.Succeeded)
            {
                Refresh();
                SetFeedback(result.Message);
                return false;
            }

            Refresh();
            SetFeedback(result.Feedback);
            return true;
        }

        private void HandleChoice(string eventId, string optionId, int generation)
        {
            if (generation != _renderGeneration)
                return;
            TryChooseOption(eventId, optionId);
        }

        private void ResolveSessionContent()
        {
            if (gameSession == null)
                return;
            if (_catalog == null && gameSession.EventCatalogReady)
                _catalog = gameSession.EventCatalog;
            if (_textCatalog == null && gameSession.EventCatalogReady)
                _textCatalog = gameSession.EventText;
            if (_textById.Count == 0)
                RebuildTextLookup();
        }

        private void RebuildTextLookup()
        {
            _textById.Clear();
            if (_textCatalog == null || _textCatalog.entries == null)
                return;
            foreach (EventTextEntry entry in _textCatalog.entries)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.textId) && !_textById.ContainsKey(entry.textId))
                    _textById.Add(entry.textId, entry);
        }

        private EventDefinitionData FindEvent(string eventId)
        {
            if (_catalog == null || _catalog.events == null)
                return null;
            foreach (EventDefinitionData definition in _catalog.events)
                if (definition != null && string.Equals(definition.eventId, eventId, StringComparison.Ordinal))
                    return definition;
            return null;
        }

        private bool TryGetText(string textId, out EventTextEntry entry)
        {
            return _textById.TryGetValue(textId ?? string.Empty, out entry);
        }

        private string FormatOptionLabel(EventOptionData option, EventTextEntry optionText,
            bool available, string disabledReason)
        {
            string label = optionText.title;
            if (!string.IsNullOrWhiteSpace(optionText.body))
                label += "\n" + optionText.body;
            if (!string.IsNullOrWhiteSpace(option.summaryTextId) &&
                TryGetText(option.summaryTextId, out EventTextEntry summary))
            {
                string summaryText = string.IsNullOrWhiteSpace(summary.body) ? summary.title : summary.body;
                if (!string.IsNullOrWhiteSpace(summaryText))
                    label += "\n结果预览：" + summaryText;
            }
            if (!available)
                label += "\n不可用：" + (string.IsNullOrWhiteSpace(disabledReason) ? "条件未满足。" : disabledReason);
            return label;
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
        }

        private void OnSessionStateChanged()
        {
            Refresh();
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message ?? string.Empty;
        }

        private OwnedButton RegisterOwnedButton(Button button)
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

        private void SetOwnedButtonsInteractable(bool interactable)
        {
            foreach (OwnedButton owned in _ownedButtons)
                if (owned != null && owned.Button != null)
                    owned.Button.interactable = interactable;
        }

        private void ClearOwnedButtons()
        {
            _renderGeneration++;
            for (int i = _ownedButtons.Count - 1; i >= 0; i--)
                DestroyOwnedButton(_ownedButtons[i]);
            _ownedButtons.Clear();
        }

        private void DestroyOwnedButton(OwnedButton owned)
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
    }
}
