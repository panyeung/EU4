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
        [SerializeField] private Text farmDetailText;
        [SerializeField] private Text farmAssigneeText;
        [SerializeField] private Button farmUnassignButton;
        [SerializeField] private Transform farmCandidateContainer;
        [SerializeField] private Text tradingStatusText;
        [SerializeField] private Text tradingDetailText;
        [SerializeField] private Text tradingAssigneeText;
        [SerializeField] private Button tradingUnassignButton;
        [SerializeField] private Transform tradingCandidateContainer;
        [SerializeField] private GameObject candidateTemplate;
        [SerializeField] private Text previewText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        /// <summary>Reason shown when the scene has no confirmation controls.</summary>
        public const string ConfirmUnboundNotice =
            "任命确认控件未绑定，本页暂不能选择候选人；请重新生成产业面板。";

        /// <summary>
        /// Lines the authored status band can hold before the panel stops echoing the full
        /// text. The generator derives its band height from this budget.
        /// </summary>
        public const int StatusBandSafeLines = 3;

        /// <summary>Concise status used when the full message is too long for the band.</summary>
        public const string ReportPointerStatus = "本月产业结算已完成，详见月报。";

        private readonly List<GameObject> _farmCandidates = new List<GameObject>();
        private readonly List<GameObject> _tradingCandidates = new List<GameObject>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private UnityAction _confirmListener;
        private UnityAction _cancelListener;
        private string _pendingIndustryId = string.Empty;
        private PersonId _pendingPersonId;
        private readonly List<PersonId> _eligiblePeople = new List<PersonId>();

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

        /// <summary>True when the band shows <see cref="ReportPointerStatus"/> instead of the full text.</summary>
        public bool StatusPointerActive { get; private set; }

        /// <summary>Lines the band can hold with the authored font and height.</summary>
        public int StatusBandLineBudget { get; private set; }

        /// <summary>Wrapped line count of the full message at the band width; 0 when unmeasured.</summary>
        public int StatusLineCount { get; private set; }
        public string ReportLine => reportText != null ? reportText.text : string.Empty;
        public string FarmStatus => farmStatusText != null ? farmStatusText.text : string.Empty;
        public string FarmAssignee => farmAssigneeText != null ? farmAssigneeText.text : string.Empty;
        public string TradingStatus => tradingStatusText != null ? tradingStatusText.text : string.Empty;
        public string TradingAssignee => tradingAssigneeText != null ? tradingAssigneeText.text : string.Empty;
        public int CandidateCount => _farmCandidates.Count + _tradingCandidates.Count;
        public int FarmCandidateCount => _farmCandidates.Count;
        public int TradingCandidateCount => _tradingCandidates.Count;

        public string FarmDetail => farmDetailText != null ? farmDetailText.text : string.Empty;
        public bool FarmDetailVisible => farmDetailText != null && farmDetailText.gameObject.activeSelf;
        public string TradingDetail => tradingDetailText != null ? tradingDetailText.text : string.Empty;
        public bool TradingDetailVisible =>
            tradingDetailText != null && tradingDetailText.gameObject.activeSelf;
        public string PreviewLine => previewText != null ? previewText.text : string.Empty;
        public bool PreviewVisible => previewText != null && previewText.gameObject.activeSelf;

        /// <summary>Industry awaiting confirmation; empty when nothing is selected.</summary>
        public string PendingIndustryId => _pendingIndustryId;

        /// <summary>Person awaiting confirmation; empty when nothing is selected.</summary>
        public PersonId PendingPersonId => _pendingPersonId;
        public bool HasPendingAssignment => !string.IsNullOrEmpty(_pendingIndustryId);
        public bool ConfirmVisible => confirmButton != null && confirmButton.gameObject.activeSelf;
        public bool ConfirmInteractable => confirmButton != null && confirmButton.interactable;
        public bool CancelVisible => cancelButton != null && cancelButton.gameObject.activeSelf;

        /// <summary>
        /// A scene authored before the confirm controls existed can project appointments but
        /// cannot commit them. Selection is refused and explained instead of becoming a
        /// one-click dead end.
        /// </summary>
        public bool CanConfirmSelection => confirmButton != null && cancelButton != null;
        public int EligibleCandidateTotal => _eligiblePeople.Count;

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
        public void ClickConfirm() => ConfirmPending();
        public void ClickCancel() => CancelPending();

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
            Transform tradingCandidates, GameObject template, bool refresh = true,
            Text farmDetail = null, Text tradingDetail = null, Text preview = null,
            Button confirm = null, Button cancel = null)
        {
            Unsubscribe();
            ClearCandidates();
            ClearPending();
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
            farmDetailText = farmDetail;
            farmAssigneeText = farmAssignee;
            farmUnassignButton = farmUnassign;
            farmCandidateContainer = farmCandidates;
            rowTradingHouse = tradingRow;
            tradingStatusText = tradingStatus;
            tradingDetailText = tradingDetail;
            tradingAssigneeText = tradingAssignee;
            tradingUnassignButton = tradingUnassign;
            tradingCandidateContainer = tradingCandidates;
            candidateTemplate = template;
            previewText = preview;
            confirmButton = confirm;
            cancelButton = cancel;
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
            _eligiblePeople.Clear();
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

            // Built first: the status line falls back to a report pointer only when the
            // month report already carries the full text.
            string reportLine = BuildReportLine(view);
            string statusLine = SelectStatusLine(view.LastMessage, reportLine);
            messageText.text = statusLine;
            messageText.gameObject.SetActive(statusLine.Length > 0);

            // A selection the domain can no longer accept is dropped before the rows are
            // rebuilt, so no candidate keeps a marker or a preview for an invalid choice.
            if (HasPendingAssignment && !CanStillAssign(view, _pendingIndustryId, _pendingPersonId))
                ClearPending();

            RefreshRow(IndustryCatalog.FarmId, view, rowFarm, farmStatusText, farmDetailText,
                farmAssigneeText, farmUnassignButton, farmCandidateContainer);
            RefreshRow(IndustryCatalog.TradingHouseId, view, rowTradingHouse, tradingStatusText,
                tradingDetailText, tradingAssigneeText, tradingUnassignButton,
                tradingCandidateContainer);
            RebuildCandidateLayout();

            reportText.text = reportLine;
            RefreshPendingPreview(view);
        }

        /// <summary>
        /// Picks the status text. The band is bounded: when the full message needs more lines
        /// than it can show, the band repeats only what the monthly report already carries in
        /// full. A message the report does not carry stays as-is and is clipped by the band's
        /// authored truncation, so it can never paint over the rows.
        /// </summary>
        private string SelectStatusLine(string message, string reportLine)
        {
            StatusPointerActive = false;
            StatusLineCount = 0;
            StatusBandLineBudget = ComputeStatusBandLineBudget();
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            StatusLineCount = CountStatusLines(message);
            if (StatusLineCount <= 0 || StatusBandLineBudget <= 0 ||
                StatusLineCount <= StatusBandLineBudget)
                return message;
            if (reportLine.IndexOf(message, System.StringComparison.Ordinal) >= 0)
            {
                StatusPointerActive = true;
                return ReportPointerStatus;
            }
            return message;
        }

        /// <summary>Rendered lines available in the band, measured with its own font and height.</summary>
        private int ComputeStatusBandLineBudget()
        {
            if (!TryBuildStatusSettings(out TextGenerationSettings settings, out Rect band))
                return 0;
            float lineHeight = messageText.cachedTextGeneratorForLayout
                                   .GetPreferredHeight("国", settings) /
                               Mathf.Max(0.0001f, messageText.pixelsPerUnit);
            return lineHeight <= 0f ? 0 : Mathf.FloorToInt((band.height + 0.5f) / lineHeight);
        }

        /// <summary>Wrapped line count of one candidate message at the band width.</summary>
        private int CountStatusLines(string message)
        {
            if (!TryBuildStatusSettings(out TextGenerationSettings settings, out _))
                return 0;
            TextGenerator generator = messageText.cachedTextGeneratorForLayout;
            generator.Populate(message, settings);
            return generator.lineCount;
        }

        /// <summary>
        /// Measurement settings for the band. Vertical overflow is forced open so a long
        /// candidate is measured in full instead of being clamped by the authored truncation.
        /// </summary>
        private bool TryBuildStatusSettings(out TextGenerationSettings settings, out Rect band)
        {
            settings = default;
            band = default;
            if (messageText == null || messageText.font == null)
                return false;
            RectTransform target = messageText.rectTransform;
            band = target != null ? target.rect : default;
            if (band.width <= 0f || band.height <= 0f)
                return false;
            settings = messageText.GetGenerationSettings(band.size);
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;
            return true;
        }

        /// <summary>
        /// Reuses the existing report label: the committed industry month, then the committed
        /// market month, then the persisted shortage status. A load clears both reports, so the
        /// shortage line keeps the last durable state visible instead of an expired report.
        /// </summary>
        private static string BuildReportLine(IndustryView view)
        {
            string line = !string.IsNullOrEmpty(view.LastReport?.Summary)
                ? view.LastReport.Summary
                : !string.IsNullOrWhiteSpace(view.LastChronicleSummary)
                    ? view.LastChronicleSummary
                    : "本月尚未结算。";
            if (!string.IsNullOrEmpty(view.MarketSummary))
                line += " " + view.MarketSummary;
            if (!string.IsNullOrEmpty(view.ShortageLine))
                line += " " + view.ShortageLine;
            return line;
        }

        private void RefreshRow(string industryId, IndustryView view, GameObject row, Text statusText,
            Text detailText, Text assigneeText, Button unassignButton, Transform candidateContainer)
        {
            if (row == null)
                return;
            IndustrySlotView slot = view.Slot(industryId);
            IndustryDefinition definition = IndustryCatalog.TryGet(industryId, out IndustryDefinition found)
                ? found
                : default;
            row.SetActive(true);

            statusText.text = slot.StatusReason;
            RefreshDetail(detailText, definition);
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
            BuildCandidates(industryId, view, candidateContainer);
            RebuildLayout(candidateContainer);
        }

        /// <summary>
        /// Authored per-month input and output of one industry, taken from the read-only
        /// catalog rule. Fields the catalog does not define (location) stay hidden instead
        /// of being invented, and a missing definition hides the line entirely.
        /// </summary>
        private static void RefreshDetail(Text detailText, IndustryDefinition definition)
        {
            if (detailText == null)
                return;
            string line = BuildDetailLine(definition);
            detailText.text = line.Length > 0 ? "每月规则：" + line : string.Empty;
            detailText.gameObject.SetActive(line.Length > 0);
        }

        private static string BuildDetailLine(IndustryDefinition definition)
        {
            if (string.IsNullOrEmpty(definition.Id))
                return string.Empty;
            string upkeep;
            if (definition.MaintainsFromOutput)
            {
                upkeep = "维护 " + definition.UpkeepSupplies + " 物资（自留存）";
            }
            else
            {
                upkeep = "维护 " + definition.UpkeepFunds + " 资金";
                if (definition.UpkeepSupplies > 0)
                    upkeep += "、" + definition.UpkeepSupplies + " 物资";
            }

            string output = "产出 +" + definition.OutputFunds + " 资金";
            if (definition.OutputSupplies > 0)
                output += "、+" + definition.OutputSupplies + " 物资";
            if (definition.OutputIntelligence > 0)
                output += "、+" + definition.OutputIntelligence + " 情报";
            if (definition.OutputFood > 0)
                output += "、+" + definition.OutputFood + " 粮食";
            return upkeep + "｜" + output;
        }

        private void BuildCandidates(string industryId, IndustryView view, Transform container)
        {
            if (candidateTemplate == null || container == null || !view.CanEditAssignments)
                return;

            bool canSelect = CanConfirmSelection;
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

                RememberEligible(personId);
                bool pending = IsPending(industryId, personId);
                GameObject buttonObject = Instantiate(candidateTemplate, container);
                buttonObject.name = "IndustryCandidate_" + industryId + "_" + personId.Value;
                buttonObject.SetActive(true);
                Text label = buttonObject.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = (pending ? "▶ " : string.Empty) +
                                 IndustryService.PersonName(succession, personId) +
                                 "（剩余 " + remaining + "/" + IndustryService.MonthlyWorkUnits + "）";
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    // Without the confirm controls a click could only change the highlight,
                    // so the candidates stay disabled and the preview explains why.
                    button.interactable = canSelect;
                    PersonId captured = personId;
                    string capturedIndustry = industryId;
                    button.onClick.AddListener(() => SelectPending(capturedIndustry, captured));
                }
                if (string.Equals(industryId, IndustryCatalog.FarmId, System.StringComparison.Ordinal))
                    _farmCandidates.Add(buttonObject);
                else
                    _tradingCandidates.Add(buttonObject);
            }
        }

        /// <summary>Counts each selectable person once even when both rows list them.</summary>
        private void RememberEligible(PersonId personId)
        {
            for (int i = 0; i < _eligiblePeople.Count; i++)
                if (_eligiblePeople[i] == personId)
                    return;
            _eligiblePeople.Add(personId);
        }

        private bool IsPending(string industryId, PersonId personId) =>
            !string.IsNullOrEmpty(_pendingIndustryId) && _pendingPersonId == personId &&
            string.Equals(_pendingIndustryId, industryId, System.StringComparison.Ordinal);

        /// <summary>
        /// Selecting a candidate only previews the appointment; the domain changes when
        /// the player confirms. Cancel clears the preview and writes nothing.
        /// </summary>
        public void SelectPending(string industryId, PersonId personId)
        {
            if (!CanConfirmSelection || string.IsNullOrEmpty(industryId) || personId.IsEmpty)
                return;
            _pendingIndustryId = industryId;
            _pendingPersonId = personId;
            if (IsOpen)
                Refresh();
        }

        private void ConfirmPending()
        {
            if (gameSession == null || string.IsNullOrEmpty(_pendingIndustryId))
                return;
            IndustryAssignmentResult result =
                gameSession.TryAssignIndustry(_pendingIndustryId, _pendingPersonId);
            if (result.Succeeded)
                ClearPending();
            // Success already publishes SessionStateChanged; refresh a rejection or a cleared
            // preview here so the panel never shows a stale preview.
            if (IsOpen)
                Refresh();
        }

        private void CancelPending()
        {
            if (!HasPendingAssignment)
                return;
            ClearPending();
            if (IsOpen)
                Refresh();
        }

        private void ClearPending()
        {
            _pendingIndustryId = string.Empty;
            _pendingPersonId = default;
        }

        /// <summary>
        /// Paints the pending recipient and the confirm/cancel choices. A scene authored
        /// before the preview label existed has nowhere to paint that text, so the reason
        /// for the disabled candidates goes to the message line it already binds instead.
        /// </summary>
        private void RefreshPendingPreview(IndustryView view)
        {
            if (previewText != null)
            {
                string line = BuildPreviewLine(view);
                previewText.text = line;
                previewText.gameObject.SetActive(line.Length > 0);
            }
            else if (!CanConfirmSelection)
            {
                ShowConfirmUnboundNotice();
            }

            bool show = HasPendingAssignment && view.CanEditAssignments;
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(show);
                confirmButton.interactable = show;
            }
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(show);
        }

        /// <summary>
        /// Keeps the disabled candidates explainable on a pre-preview scene. The message
        /// line is the surface such a scene already binds; the block-reason line is used
        /// only when the message line is missing, and existing domain text keeps its place
        /// after the notice rather than being replaced.
        /// </summary>
        private void ShowConfirmUnboundNotice()
        {
            Text surface = messageText != null ? messageText : blockReasonText;
            if (surface == null)
                return;
            string existing = surface.text;
            surface.text = string.IsNullOrWhiteSpace(existing)
                ? ConfirmUnboundNotice
                : ConfirmUnboundNotice + " " + existing;
            surface.gameObject.SetActive(true);
        }

        private bool CanStillAssign(IndustryView view, string industryId, PersonId personId)
        {
            if (!view.CanEditAssignments || !IndustryCatalog.TryGet(industryId, out _))
                return false;
            SuccessionState succession = gameSession.Succession;
            GameDate date = gameSession.WorldClock != null
                ? gameSession.WorldClock.CurrentDate
                : SengokuChronicle.Domain.WorldClock.NewGameDate;
            if (!IndustryService.CanWork(succession, personId, date, out _))
                return false;
            foreach (IndustrySlotView slot in view.Slots)
                if (!slot.AssigneeId.IsEmpty && slot.AssigneeId == personId)
                    return slot.WorkUnitsRemaining >= IndustryService.PostWorkUnits;
            return IndustryService.MonthlyWorkUnits >= IndustryService.PostWorkUnits;
        }

        /// <summary>
        /// Preview of a pending appointment built from the catalog rule and the projected
        /// slots only. It restates the authored monthly rule; the monthly settlement stays
        /// the single authority on whether the month operates.
        /// </summary>
        private string BuildPreviewLine(IndustryView view)
        {
            if (!CanConfirmSelection)
                return ConfirmUnboundNotice;
            if (!view.CanEditAssignments)
                return string.Empty;
            if (!HasPendingAssignment)
            {
                return "选择一名家人后确认任命；下方「确认任命」提交，「取消」不改变现状。（可任命 " +
                       EligibleCandidateTotal + " 人）\n" +
                       "未成年、已隐居或本月工时不足者不列出；地点未在产业规则中定义，本页暂不显示。";
            }
            if (!IndustryCatalog.TryGet(_pendingIndustryId, out IndustryDefinition definition))
                return string.Empty;

            SuccessionState succession = gameSession.Succession;
            int remaining = IndustryService.MonthlyWorkUnits;
            foreach (IndustrySlotView slot in view.Slots)
                if (!slot.AssigneeId.IsEmpty && slot.AssigneeId == _pendingPersonId)
                    remaining = slot.WorkUnitsRemaining;

            int after = remaining - IndustryService.PostWorkUnits;
            IndustrySlotView target = view.Slot(_pendingIndustryId);
            string holder = target.AssigneeId.IsEmpty
                ? "职位空缺"
                : "替换现任 " + target.AssigneeName;

            return "已选择：" + IndustryService.PersonName(succession, _pendingPersonId) +
                   " → " + definition.DisplayName + "（待确认）｜" + holder +
                   "｜占用工时 " + IndustryService.PostWorkUnits + "/" +
                   IndustryService.MonthlyWorkUnits + "，剩余 " + after + "\n" +
                   "每月规则：" + BuildDetailLine(definition) + "。";
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
            if (confirmButton != null && _confirmListener == null)
            {
                _confirmListener = ConfirmPending;
                confirmButton.onClick.AddListener(_confirmListener);
            }
            if (cancelButton != null && _cancelListener == null)
            {
                _cancelListener = CancelPending;
                cancelButton.onClick.AddListener(_cancelListener);
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
            if (confirmButton != null && _confirmListener != null)
                confirmButton.onClick.RemoveListener(_confirmListener);
            _confirmListener = null;
            if (cancelButton != null && _cancelListener != null)
                cancelButton.onClick.RemoveListener(_cancelListener);
            _cancelListener = null;
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
