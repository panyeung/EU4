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
    /// SC-020 child-education panel. It only previews and submits an annual direction and
    /// mentor through <see cref="GameSession.TrySetEducationPlan"/>; growth and chronicle
    /// records are written solely by the December-to-January month settlement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterEducationPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text overviewText;
        [SerializeField] private Transform directionContainer;
        [SerializeField] private Button directionTemplate;
        [SerializeField] private Transform mentorContainer;
        [SerializeField] private Button mentorTemplate;
        [SerializeField] private Text previewText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text messageText;

        private const string DirectionButtonPrefix = "SC020Direction_";
        private const string MentorButtonPrefix = "SC020Mentor_";

        private readonly List<GameObject> _directions = new List<GameObject>();
        private readonly List<GameObject> _mentors = new List<GameObject>();
        private bool _subscribed;
        private bool _educationScenarioActive;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private UnityAction _confirmListener;
        private EducationDirection _direction = EducationDirection.FreeGrowth;
        private PersonId _mentor;
        private bool _selectionInitialized;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            overviewText != null && directionContainer != null && directionTemplate != null &&
            mentorContainer != null && mentorTemplate != null && previewText != null &&
            confirmButton != null && messageText != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public string OverviewLine => overviewText != null ? overviewText.text : string.Empty;
        public string PreviewLine => previewText != null ? previewText.text : string.Empty;
        public string MessageLine => messageText != null ? messageText.text : string.Empty;
        public bool MessageVisible => messageText != null && messageText.gameObject.activeSelf;
        public int DirectionCount => _directions.Count;
        public int MentorCount => _mentors.Count;
        public EducationDirection SelectedDirection => _direction;
        public PersonId SelectedMentor => _mentor;
        public bool ConfirmInteractable => confirmButton != null && confirmButton.interactable;

        public string DirectionLabel(int index) => LabelOf(_directions, index);
        public string MentorLabel(int index) => LabelOf(_mentors, index);

        public void BindReferences(GameSession session, GameObject panel, Button open, Button close,
            Text overview, Transform directions, Button direction, Transform mentors, Button mentor,
            Text preview, Button confirm, Text message)
        {
            Unsubscribe();
            ClearButtons();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            overviewText = overview;
            directionContainer = directions;
            directionTemplate = direction;
            mentorContainer = mentors;
            mentorTemplate = mentor;
            previewText = preview;
            confirmButton = confirm;
            messageText = message;
            _educationScenarioActive = session != null && SC020EducationScenario.IsActive(session.Succession);
            _selectionInitialized = false;
            Subscribe();
            // Never build button clones while the scene is being generated or inspected in
            // the editor: they would be serialized into FoundationScene without runtime
            // listeners and then swallow pointer clicks in the first grid cells.
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
            ClearButtons();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            EducationView view = gameSession.Education;
            overviewText.text = view.OverviewLine + "\n" + view.PlanLine + "\n" + view.ResultLine;
            previewText.text = string.Empty;

            if (!view.HasLearner)
            {
                messageText.text = "当前没有可培养的家族成员。";
                messageText.gameObject.SetActive(true);
                confirmButton.interactable = false;
                return;
            }

            NormalizeSelection(view);
            _selectionInitialized = true;
            BuildDirections(view);
            BuildMentors(view);
            RefreshPreview(view);

            bool showMessage = !string.IsNullOrWhiteSpace(view.LastMessage);
            messageText.text = view.LastMessage;
            messageText.gameObject.SetActive(showMessage);
        }

        private void NormalizeSelection(EducationView view)
        {
            if (_selectionInitialized)
            {
                if (!view.HasPlan && _direction == EducationDirection.FreeGrowth)
                    _mentor = default;
                return;
            }

            if (view.HasPlan)
            {
                _direction = view.Plan.Direction;
                _mentor = view.Plan.Mentor;
                return;
            }

            if (!System.Enum.IsDefined(typeof(EducationDirection), _direction))
                _direction = EducationDirection.FreeGrowth;
            if (_direction == EducationDirection.FreeGrowth)
                _mentor = default;
        }

        private void BuildDirections(EducationView view)
        {
            if (directionTemplate == null || directionContainer == null)
                return;

            var directions = new[]
            {
                EducationDirection.Ninjutsu,
                EducationDirection.MartialArts,
                EducationDirection.Scholarship,
                EducationDirection.Management,
                EducationDirection.Social,
                EducationDirection.FreeGrowth
            };
            foreach (EducationDirection direction in directions)
            {
                EducationDirection captured = direction;
                GameObject buttonObject = CreateButton(directionTemplate, directionContainer,
                    "SC020Direction_" + direction,
                    (_direction == direction ? "▶ " : string.Empty) +
                    CharacterGrowthRules.DirectionName(direction));
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = view.CanEdit && view.IsEligible;
                    button.onClick.AddListener(() => SelectDirection(captured));
                }
                _directions.Add(buttonObject);
            }
        }

        private void BuildMentors(EducationView view)
        {
            if (mentorTemplate == null || mentorContainer == null)
                return;

            IReadOnlyList<EducationMentorOption> options = gameSession.EducationMentors;
            bool needMentor = _direction != EducationDirection.FreeGrowth;
            if (!needMentor)
            {
                if (!_mentor.IsEmpty)
                    _mentor = default;
                return;
            }

            bool selectedStillPresent = false;
            foreach (EducationMentorOption option in options)
                if (option.PersonId == _mentor)
                    selectedStillPresent = true;
            if (!selectedStillPresent)
                _mentor = options.Count > 0 ? options[0].PersonId : default;

            foreach (EducationMentorOption option in options)
            {
                EducationMentorOption captured = option;
                GameObject buttonObject = CreateButton(mentorTemplate, mentorContainer,
                    "SC020Mentor_" + option.PersonId.Value,
                    (option.PersonId == _mentor ? "▶ " : string.Empty) + option.DisplayName +
                    "（" + option.SpecialtyName + "）");
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = view.CanEdit && view.IsEligible;
                    button.onClick.AddListener(() => SelectMentor(captured.PersonId));
                }
                _mentors.Add(buttonObject);
            }
        }

        private void RefreshPreview(EducationView view)
        {
            if (!view.CanEdit || !view.IsEligible)
            {
                previewText.text = string.IsNullOrWhiteSpace(view.BlockReason)
                    ? "儿童年度培养已结束。"
                    : view.BlockReason;
                confirmButton.interactable = false;
                return;
            }

            EducationPreview preview = gameSession.PreviewEducation(_direction, _mentor);
            previewText.text = preview.PreviewLine;
            confirmButton.interactable = preview.Succeeded;
        }

        public void SelectDirection(EducationDirection direction)
        {
            _direction = direction;
            if (direction == EducationDirection.FreeGrowth)
                _mentor = default;
            Refresh();
        }

        public void ClickDirection(int index)
        {
            if (index < 0 || index >= _directions.Count)
                return;
            Button button = _directions[index].GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke();
        }

        public void SelectMentor(PersonId mentor)
        {
            _mentor = mentor;
            Refresh();
        }

        public void ClickMentor(int index)
        {
            if (index < 0 || index >= _mentors.Count)
                return;
            Button button = _mentors[index].GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke();
        }

        public void ClickConfirm()
        {
            if (gameSession == null)
                return;
            EducationPlanCommandResult result = gameSession.TrySetEducationPlan(
                gameSession.Education.LearnerId, _direction, _mentor);
            if (messageText != null)
            {
                messageText.text = result.Message;
                messageText.gameObject.SetActive(!string.IsNullOrWhiteSpace(result.Message));
            }
            Refresh();
        }

        private static string LabelOf(List<GameObject> buttons, int index)
        {
            if (index < 0 || index >= buttons.Count || buttons[index] == null)
                return string.Empty;
            Text label = buttons[index].GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
        }

        private GameObject CreateButton(Button template, Transform container, string name, string label)
        {
            GameObject buttonObject = Instantiate(template.gameObject, container);
            buttonObject.name = name;
            buttonObject.SetActive(true);
            Text text = buttonObject.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;
            return buttonObject;
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
            // The generator only serializes the confirm button; its runtime click handler
            // must be bound here, otherwise the visible submit button does nothing.
            if (confirmButton != null && _confirmListener == null)
            {
                _confirmListener = ClickConfirm;
                confirmButton.onClick.AddListener(_confirmListener);
            }
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
            if (confirmButton != null && _confirmListener != null)
                confirmButton.onClick.RemoveListener(_confirmListener);
            _confirmListener = null;
        }

        private void OnSessionStateChanged()
        {
            bool active = gameSession != null && SC020EducationScenario.IsActive(gameSession.Succession);
            bool entered = active && !_educationScenarioActive;
            _educationScenarioActive = active;
            if (entered)
            {
                _selectionInitialized = false;
                Open();
            }
            else if (IsOpen)
                Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            if (UnityEngine.Application.isPlaying)
                Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearButtons();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void ClearButtons()
        {
            ClearButtonList(_directions);
            ClearButtonList(_mentors);
            RemoveSerializedClones(directionContainer, directionTemplate, DirectionButtonPrefix);
            RemoveSerializedClones(mentorContainer, mentorTemplate, MentorButtonPrefix);
        }

        /// <summary>
        /// Removes generated clones that an older edit-time Refresh serialized into the
        /// scene. They are not tracked by the runtime lists, have no runtime listeners and
        /// stay interactable=false, so they would occupy the first grid cells and absorb
        /// clicks meant for the live buttons. Only our own generated names are touched.
        /// </summary>
        private static void RemoveSerializedClones(Transform container, Button template, string prefix)
        {
            if (container == null)
                return;
            Transform templateTransform = template != null ? template.transform : null;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (child == null || child == templateTransform ||
                    !child.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                child.gameObject.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    UnityEngine.Object.Destroy(child.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void ClearButtonList(List<GameObject> buttons)
        {
            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                GameObject button = buttons[i];
                if (button == null)
                    continue;
                button.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(button);
                else
                    DestroyImmediate(button);
            }
            buttons.Clear();
        }
    }
}
