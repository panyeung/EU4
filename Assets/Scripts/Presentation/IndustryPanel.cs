using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// SC-019 industry and personnel panel. It is read-only except for assigning,
    /// replacing and unassigning industry heads; it never advances time, saves,
    /// mutates events or touches succession.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IndustryPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text blockReasonText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text reportText;
        [SerializeField] private GameObject rowFarm;
        [SerializeField] private GameObject rowTradingHouse;
        [SerializeField] private Text farmStatusText;
        [SerializeField] private Text farmAssigneeText;
        [SerializeField] private Button farmUnassignButton;
        [SerializeField] private Transform farmCandidateContainer;
        [SerializeField] private Text tradingStatusText;
        [SerializeField] private Text tradingAssigneeText;
        [SerializeField] private Button tradingUnassignButton;
        [SerializeField] private Transform tradingCandidateContainer;
        [SerializeField] private GameObject candidateTemplate;

        private readonly List<GameObject> _farmCandidates = new List<GameObject>();
        private readonly List<GameObject> _tradingCandidates = new List<GameObject>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && resourceText != null && blockReasonText != null && reportText != null &&
            messageText != null &&
            rowFarm != null && rowTradingHouse != null && farmStatusText != null &&
            farmAssigneeText != null && farmUnassignButton != null && farmCandidateContainer != null &&
            tradingStatusText != null && tradingAssigneeText != null &&
            tradingUnassignButton != null && tradingCandidateContainer != null && candidateTemplate != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public string ResourceLine => resourceText != null ? resourceText.text : string.Empty;
        public string BlockReasonLine => blockReasonText != null ? blockReasonText.text : string.Empty;
        public bool BlockReasonVisible => blockReasonText != null && blockReasonText.gameObject.activeSelf;
        public string MessageLine => messageText != null ? messageText.text : string.Empty;
        public bool MessageVisible => messageText != null && messageText.gameObject.activeSelf;
        public string ReportLine => reportText != null ? reportText.text : string.Empty;
        public string FarmStatus => farmStatusText != null ? farmStatusText.text : string.Empty;
        public string FarmAssignee => farmAssigneeText != null ? farmAssigneeText.text : string.Empty;
        public string TradingStatus => tradingStatusText != null ? tradingStatusText.text : string.Empty;
        public string TradingAssignee => tradingAssigneeText != null ? tradingAssigneeText.text : string.Empty;
        public int CandidateCount => _farmCandidates.Count + _tradingCandidates.Count;
        public int FarmCandidateCount => _farmCandidates.Count;
        public int TradingCandidateCount => _tradingCandidates.Count;

        public string FarmCandidateLabel(int index) => CandidateLabel(_farmCandidates, index);
        public string TradingCandidateLabel(int index) => CandidateLabel(_tradingCandidates, index);
        public Vector2 FarmCandidatePosition(int index) => CandidatePosition(_farmCandidates, index);
        public Vector2 TradingCandidatePosition(int index) => CandidatePosition(_tradingCandidates, index);

        public RectTransform FarmCandidateArea =>
            farmCandidateContainer != null ? farmCandidateContainer as RectTransform : null;

        public RectTransform TradingCandidateArea =>
            tradingCandidateContainer != null ? tradingCandidateContainer as RectTransform : null;

        /// <summary>Forces the scene layout groups to place every live candidate this frame.</summary>
        public void RebuildCandidateLayout()
        {
            RebuildLayout(farmCandidateContainer);
            RebuildLayout(tradingCandidateContainer);
        }

        private static void RebuildLayout(Transform container)
        {
            if (container is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        public void ClickFarmCandidate(int index) => ClickCandidate(_farmCandidates, index);
        public void ClickTradingCandidate(int index) => ClickCandidate(_tradingCandidates, index);
        public void ClickFarmUnassign() => OnUnassignFarm();
        public void ClickTradingUnassign() => OnUnassignTrading();

        private static string CandidateLabel(List<GameObject> candidates, int index)
        {
            if (index < 0 || index >= candidates.Count || candidates[index] == null)
                return string.Empty;
            Text label = candidates[index].GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
        }

        private static Vector2 CandidatePosition(List<GameObject> candidates, int index)
        {
            if (index < 0 || index >= candidates.Count || candidates[index] == null)
                return Vector2.zero;
            var rect = candidates[index].GetComponent<RectTransform>();
            // GridLayoutGroup anchors its children to the container's top-left, so
            // anchoredPosition is not expressed around the container centre. The
            // local position is the stable container-relative coordinate used by
            // tests and bounds checks.
            return rect != null ? (Vector2)rect.localPosition : Vector2.zero;
        }

        private static void ClickCandidate(List<GameObject> candidates, int index)
        {
            if (index < 0 || index >= candidates.Count || candidates[index] == null)
                return;
            Button button = candidates[index].GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke();
        }

        public void BindReferences(GameSession session, GameObject panel, Button open, Button close,
            Text title, Text resources, Text blockReason, Text message, Text report, GameObject farmRow,
            Text farmStatus, Text farmAssignee, Button farmUnassign, Transform farmCandidates,
            GameObject tradingRow, Text tradingStatus, Text tradingAssignee, Button tradingUnassign,
            Transform tradingCandidates, GameObject template, bool refresh = true)
        {
            Unsubscribe();
            ClearCandidates();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            titleText = title;
            resourceText = resources;
            blockReasonText = blockReason;
            messageText = message;
            reportText = report;
            rowFarm = farmRow;
            farmStatusText = farmStatus;
            farmAssigneeText = farmAssignee;
            farmUnassignButton = farmUnassign;
            farmCandidateContainer = farmCandidates;
            rowTradingHouse = tradingRow;
            tradingStatusText = tradingStatus;
            tradingAssigneeText = tradingAssignee;
            tradingUnassignButton = tradingUnassign;
            tradingCandidateContainer = tradingCandidates;
            candidateTemplate = template;
            Subscribe();
            if (refresh)
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
            ClearCandidates();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            titleText.text = "产业与人事";
            IndustryView view = gameSession.Industry;
            resourceText.text = view.ResourceLine;

            bool showBlock = !string.IsNullOrWhiteSpace(view.BlockReason);
            blockReasonText.text = view.BlockReason;
            blockReasonText.gameObject.SetActive(showBlock);

            bool showMessage = !string.IsNullOrWhiteSpace(view.LastMessage);
            messageText.text = view.LastMessage;
            messageText.gameObject.SetActive(showMessage);

            RefreshRow(IndustryCatalog.FarmId, view, rowFarm, farmStatusText, farmAssigneeText,
                farmUnassignButton, farmCandidateContainer);
            RefreshRow(IndustryCatalog.TradingHouseId, view, rowTradingHouse, tradingStatusText,
                tradingAssigneeText, tradingUnassignButton, tradingCandidateContainer);
            RebuildCandidateLayout();

            reportText.text = !string.IsNullOrEmpty(view.LastReport?.Summary)
                ? view.LastReport.Summary
                : !string.IsNullOrWhiteSpace(view.LastChronicleSummary)
                    ? view.LastChronicleSummary
                    : "本月尚未结算。";
        }

        private void RefreshRow(string industryId, IndustryView view, GameObject row, Text statusText,
            Text assigneeText, Button unassignButton, Transform candidateContainer)
        {
            if (row == null)
                return;
            IndustrySlotView slot = view.Slot(industryId);
            IndustryDefinition definition = IndustryCatalog.TryGet(industryId, out IndustryDefinition found)
                ? found
                : default;
            row.SetActive(true);

            statusText.text = slot.StatusReason;
            if (slot.AssigneeId.IsEmpty)
            {
                assigneeText.text = "负责人：空缺";
            }
            else
            {
                assigneeText.text = "负责人：" + slot.AssigneeName + "（" +
                                    slot.WorkUnitsRequired + "/" + IndustryService.MonthlyWorkUnits +
                                    " 已用，" + slot.WorkUnitsRemaining + " 可用）";
            }

            unassignButton.interactable = !slot.AssigneeId.IsEmpty && view.CanEditAssignments;
            BuildCandidates(industryId, definition, view, candidateContainer);
            RebuildLayout(candidateContainer);
        }

        private void BuildCandidates(string industryId, IndustryDefinition definition, IndustryView view,
            Transform container)
        {
            if (candidateTemplate == null || container == null || !view.CanEditAssignments)
                return;

            SuccessionState succession = gameSession.Succession;
            GameDate date = gameSession.WorldClock != null
                ? gameSession.WorldClock.CurrentDate
                : SengokuChronicle.Domain.WorldClock.NewGameDate;
            foreach (FamilyMembership membership in succession.World.Family.Members)
            {
                PersonId personId = membership.PersonId;
                if (!IndustryService.CanWork(succession, personId, date, out _))
                    continue;
                int remaining = IndustryService.MonthlyWorkUnits;
                foreach (IndustrySlotView slot in view.Slots)
                {
                    if (!slot.AssigneeId.IsEmpty && slot.AssigneeId == personId)
                    {
                        remaining = slot.WorkUnitsRemaining;
                        break;
                    }
                }
                if (remaining < IndustryService.PostWorkUnits)
                    continue;

                GameObject buttonObject = Instantiate(candidateTemplate, container);
                buttonObject.name = "IndustryCandidate_" + industryId + "_" + personId.Value;
                buttonObject.SetActive(true);
                Text label = buttonObject.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = IndustryService.PersonName(succession, personId) +
                                 "（剩余 " + remaining + "/" + IndustryService.MonthlyWorkUnits + "）";
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    PersonId captured = personId;
                    string capturedIndustry = industryId;
                    button.onClick.AddListener(() =>
                    {
                        IndustryAssignmentResult result =
                            gameSession.TryAssignIndustry(capturedIndustry, captured);
                        // Success already publishes SessionStateChanged; refresh here
                        // only for a rejection so its visible reason appears.
                        if (!result.Succeeded && IsOpen)
                            Refresh();
                    });
                }
                if (string.Equals(industryId, IndustryCatalog.FarmId, System.StringComparison.Ordinal))
                    _farmCandidates.Add(buttonObject);
                else
                    _tradingCandidates.Add(buttonObject);
            }
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
            if (farmUnassignButton != null)
            {
                farmUnassignButton.onClick.RemoveListener(OnUnassignFarm);
                farmUnassignButton.onClick.AddListener(OnUnassignFarm);
            }
            if (tradingUnassignButton != null)
            {
                tradingUnassignButton.onClick.RemoveListener(OnUnassignTrading);
                tradingUnassignButton.onClick.AddListener(OnUnassignTrading);
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
            if (farmUnassignButton != null)
                farmUnassignButton.onClick.RemoveListener(OnUnassignFarm);
            if (tradingUnassignButton != null)
                tradingUnassignButton.onClick.RemoveListener(OnUnassignTrading);
        }

        private void OnUnassignFarm() => Unassign(IndustryCatalog.FarmId);
        private void OnUnassignTrading() => Unassign(IndustryCatalog.TradingHouseId);

        private void Unassign(string industryId)
        {
            if (gameSession == null)
                return;
            IndustryAssignmentResult result = gameSession.TryUnassignIndustry(industryId);
            if (!result.Succeeded && IsOpen)
                Refresh();
        }

        private void OnSessionStateChanged()
        {
            if (IsOpen)
                Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearCandidates();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void ClearCandidates()
        {
            ClearCandidateList(_farmCandidates);
            ClearCandidateList(_tradingCandidates);
        }

        private static void ClearCandidateList(List<GameObject> candidates)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                GameObject candidate = candidates[i];
                if (candidate == null)
                    continue;
                candidate.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(candidate);
                else
                    DestroyImmediate(candidate);
            }
            candidates.Clear();
        }
    }
}
