using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// SC-013 third-generation ending feedback. The ending is derived from committed
    /// event history and the two settled successions; this panel only renders it and
    /// never writes a new terminal save field. Closing it resumes normal play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2EndingPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button closeButton;

        private bool _subscribed;
        private UnityAction _closeListener;
        private int _staticButtonBindings;
        private string _dismissedOptionId = string.Empty;

        public bool IsBound =>
            gameSession != null && panelRoot != null && titleText != null && bodyText != null &&
            closeButton != null;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public string Title => titleText != null ? titleText.text : string.Empty;
        public string Body => bodyText != null ? bodyText.text : string.Empty;
        public int StaticButtonBindingCount => _staticButtonBindings;

        public void BindReferences(GameSession session, GameObject panel, Text title, Text body,
            Button close)
        {
            Unsubscribe();
            gameSession = session;
            panelRoot = panel;
            titleText = title;
            bodyText = body;
            closeButton = close;
            _dismissedOptionId = string.Empty;
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
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Close()
        {
            M2EndingView ending = gameSession != null ? gameSession.M2Ending : M2EndingView.Unavailable;
            if (ending.IsAvailable)
                _dismissedOptionId = ending.OptionId;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// Shows the derived ending once per committed finale option. Dismissing it does
        /// not change game state or block time; a different ending option shows again.
        /// </summary>
        public void Refresh()
        {
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            M2EndingView ending = gameSession.M2Ending;
            if (!ending.IsAvailable)
            {
                _dismissedOptionId = string.Empty;
                panelRoot.SetActive(false);
                return;
            }
            if (string.Equals(_dismissedOptionId, ending.OptionId, System.StringComparison.Ordinal))
            {
                panelRoot.SetActive(false);
                return;
            }

            titleText.text = ending.Title;
            bodyText.text = ending.Summary;
            panelRoot.SetActive(true);
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            if (closeButton != null && _closeListener == null)
            {
                _closeListener = Close;
                closeButton.onClick.AddListener(_closeListener);
                _staticButtonBindings++;
            }
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            if (_closeListener != null)
            {
                if (closeButton != null)
                    closeButton.onClick.RemoveListener(_closeListener);
                _closeListener = null;
                _staticButtonBindings--;
            }
            if (_staticButtonBindings < 0)
                _staticButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            Refresh();
        }
    }
}
