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
    /// SC-021 family-research panel. It only projects <see cref="GameSession.Research"/>
    /// and submits a researcher choice through <see cref="GameSession.TrySetResearchPlan"/>.
    /// Research progress and family knowledge are written solely by the monthly settlement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FamilyResearchPanel : MonoBehaviour
    {
        private const string ResearcherButtonPrefix = "SC021Researcher_";

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text techniqueText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text costText;
        [SerializeField] private Text blockReasonText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text reportText;
        [SerializeField] private Transform researcherContainer;
        [SerializeField] private Button researcherTemplate;

        private readonly List<GameObject> _researchers = new List<GameObject>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && resourceText != null && techniqueText != null && statusText != null &&
            progressText != null && costText != null && blockReasonText != null &&
            messageText != null && reportText != null && researcherContainer != null &&
            researcherTemplate != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int VisibleResearcherCount => _researchers.Count;
        public int RefreshCount { get; private set; }
        public string TitleLine => titleText != null ? titleText.text : string.Empty;
        public string ResourceLine => resourceText != null ? resourceText.text : string.Empty;
        public string TechniqueLine => techniqueText != null ? techniqueText.text : string.Empty;
        public string StatusLine => statusText != null ? statusText.text : string.Empty;
        public string ProgressLine => progressText != null ? progressText.text : string.Empty;
        public string CostLine => costText != null ? costText.text : string.Empty;
        public string BlockReasonLine => blockReasonText != null ? blockReasonText.text : string.Empty;
        public bool BlockReasonVisible => blockReasonText != null && blockReasonText.gameObject.activeSelf;
        public string MessageLine => messageText != null ? messageText.text : string.Empty;
        public bool MessageVisible => messageText != null && messageText.gameObject.activeSelf;
        public string ReportLine => reportText != null ? reportText.text : string.Empty;

        public string ResearcherLabel(int index) => LabelOf(_researchers, index);

        public void ClickResearcher(int index)
        {
            if (index < 0 || index >= _researchers.Count || _researchers[index] == null)
                return;
            Button button = _researchers[index].GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke();
        }

        public void BindReferences(GameSession session, GameObject panel, Button open, Button close,
            Text title, Text resources, Text technique, Text status, Text progress, Text cost,
            Text blockReason, Text message, Text report, Transform researcherRoot,
            Button researcherButton)
        {
            Unsubscribe();
            ClearResearchers();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            titleText = title;
            resourceText = resources;
            techniqueText = technique;
            statusText = status;
            progressText = progress;
            costText = cost;
            blockReasonText = blockReason;
            messageText = message;
            reportText = report;
            researcherContainer = researcherRoot;
            researcherTemplate = researcherButton;
            Subscribe();
            // Never build button clones in the editor: they would be serialized into the
            // scene without listeners and intercept pointer clicks.
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
            ClearResearchers();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            FamilyResearchView view = gameSession.Research;
            titleText.text = "家族研究与技艺传承";
            resourceText.text = view.ResourceLine;
            techniqueText.text = view.TechniqueLine + "\n" + view.KnownLine;
            statusText.text = view.StatusLine;
            progressText.text = view.ProgressLine;
            costText.text = view.CostLine;
            reportText.text = view.ReportLine;

            bool showBlock = !string.IsNullOrWhiteSpace(view.BlockReason);
            blockReasonText.text = view.BlockReason;
            blockReasonText.gameObject.SetActive(showBlock);

            bool showMessage = !string.IsNullOrWhiteSpace(view.LastMessage);
            messageText.text = view.LastMessage;
            messageText.gameObject.SetActive(showMessage);

            BuildResearchers(view);
        }

        private void BuildResearchers(FamilyResearchView view)
        {
            if (researcherTemplate == null || researcherContainer == null || !view.CanStartPlan)
                return;

            string researchId = !string.IsNullOrEmpty(view.ResearchId)
                ? view.ResearchId
                : FamilyResearchCatalog.SmokeMixingResearchId;
            foreach (FamilyResearchResearcherOption option in view.EligibleResearchers)
            {
                FamilyResearchResearcherOption captured = option;
                GameObject buttonObject = Instantiate(researcherTemplate.gameObject, researcherContainer);
                buttonObject.name = ResearcherButtonPrefix + option.PersonId.Value;
                buttonObject.SetActive(true);
                Text label = buttonObject.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = option.DisplayName;
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = view.CanStartPlan;
                    button.onClick.AddListener(() =>
                    {
                        FamilyResearchPlanCommandResult result =
                            gameSession.TrySetResearchPlan(researchId, captured.PersonId);
                        // Success already publishes SessionStateChanged, which refreshes
                        // the open panel exactly once. Refresh here only for a rejection.
                        if (!result.Succeeded && IsOpen)
                            Refresh();
                    });
                }
                _researchers.Add(buttonObject);
            }

            if (researcherContainer is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private static string LabelOf(List<GameObject> buttons, int index)
        {
            if (index < 0 || index >= buttons.Count || buttons[index] == null)
                return string.Empty;
            Text label = buttons[index].GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
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
        }

        private void OnSessionStateChanged()
        {
            // Entering the SC-021 scenario must not steal focus: the demo asks the
            // player to open the research panel explicitly instead of showing it
            // unprompted on top of the map and clock.
            if (IsOpen)
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
            ClearResearchers();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void ClearResearchers()
        {
            for (int i = _researchers.Count - 1; i >= 0; i--)
            {
                GameObject button = _researchers[i];
                if (button == null)
                    continue;
                button.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(button);
                else
                    DestroyImmediate(button);
            }
            _researchers.Clear();
            RemoveSerializedClones();
        }

        /// <summary>
        /// Removes generated clones serialized by an older edit-time refresh. Only our own
        /// generated names are touched, never the hidden template or unrelated children.
        /// </summary>
        private void RemoveSerializedClones()
        {
            if (researcherContainer == null)
                return;
            Transform templateTransform = researcherTemplate != null ? researcherTemplate.transform : null;
            for (int i = researcherContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = researcherContainer.GetChild(i);
                if (child == null || child == templateTransform ||
                    !child.name.StartsWith(ResearcherButtonPrefix, StringComparison.Ordinal))
                    continue;
                child.gameObject.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
