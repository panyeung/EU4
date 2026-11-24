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

        /// <summary>场景没有确认控件时显示的说明。</summary>
        public const string ConfirmUnboundNotice =
            "任命确认控件未绑定，本页暂不能选择候选人；请重新生成产业面板。";

        /// <summary>
        /// Serialized child name of the farm row's title label. The scene updater uses the
        /// same constant, so the runtime text and the authored text cannot drift.
        /// </summary>
        public const string FarmRowNameChildName = "FarmRowNameText";

        /// <summary>
        /// Farm row title derived from the catalog instead of a hand-written post count.
        /// The level stays the authored one; the post count follows the rules.
        /// </summary>
        public static string FarmRowLabel =>
            "农庄（等级 1・" + IndustryState.PostCount(IndustryCatalog.FarmId) + " 个职位・" +
            IndustryService.PostWorkUnits + "/" + IndustryService.MonthlyWorkUnits + " 工时）";

        /// <summary>
        /// 原始状态带在面板停止回显全文之前能容纳的行数。
        /// 生成器据此推导带区高度。
        /// </summary>
        public const int StatusBandSafeLines = 3;

        /// <summary>
        /// 全文超出带区时使用的简短状态。它指向报告区，而不宣称结果，
        /// 因此回滚或被拒的操作永远不会被说成已完成的结算。
        /// </summary>
        public const string ReportPointerStatus = "本页状态文字较长，完整内容见下方报告，可上下滚动查看。";

        /// <summary>
        /// 携带状态消息与其后报告事实之间的分隔符。
        /// 报告区是本面板拥有的唯一另一处文字面。
        /// </summary>
        private const string ReportCarrySeparator = " ｜ ";

        // 760x700 面板里原始的报告带。家宅陈列展示 760x560 的卡片，
        // 因此实时带区会上移并缩短，以留在那张卡片内。
        private const float ReportBandHeight = 68f;
        private const float ReportBandCenterY = -266f;
        private const float ReportBandBottomMargin = 4f;

        private readonly List<GameObject> _farmCandidates = new List<GameObject>();
        private readonly List<GameObject> _tradingCandidates = new List<GameObject>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private UnityAction _confirmListener;
        private UnityAction _cancelListener;
        private string _pendingIndustryId = string.Empty;
        private PersonId _pendingPersonId;
        private string _lastReportLine = string.Empty;
        private float _fittedPanelHeight = float.NaN;
        private readonly List<PersonId> _eligiblePeople = new List<PersonId>();

        /// <summary>
        /// 必需的绑定。消息带不在此列：在它出现之前构建的场景仍然可用，
        /// 阻塞原因标签承载了带区会显示的同样提示。
        /// 其它可选控件都不能让面板失效。
        /// </summary>
        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            titleText != null && resourceText != null && blockReasonText != null && reportText != null &&
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

        /// <summary>带区显示 <see cref="ReportPointerStatus"/> 而非全文时为 true。</summary>
        public bool StatusPointerActive { get; private set; }

        /// <summary>按原始字体与高度，带区能容纳的行数。</summary>
        public int StatusBandLineBudget { get; private set; }

        /// <summary>全文在带区宽度下的换行行数；未测量时为 0。</summary>
        public int StatusLineCount { get; private set; }
        public string ReportLine => reportText != null ? reportText.text : string.Empty;
        public string FarmStatus => farmStatusText != null ? farmStatusText.text : string.Empty;
        public string FarmAssignee => farmAssigneeText != null ? farmAssigneeText.text : string.Empty;
        public string TradingStatus => tradingStatusText != null ? tradingStatusText.text : string.Empty;
        public string TradingAssignee => tradingAssigneeText != null ? tradingAssigneeText.text : string.Empty;
        public int CandidateCount => _farmCandidates.Count + _tradingCandidates.Count;
        public int FarmCandidateCount => _farmCandidates.Count;
        public int TradingCandidateCount => _tradingCandidates.Count;
        /// <summary>Farm post count exposed to tests and diagnostics; the farm owns two.</summary>
        public int FarmPostCount => IndustryState.PostCount(IndustryCatalog.FarmId);
        public bool FarmUnassignInteractable =>
            farmUnassignButton != null && farmUnassignButton.interactable;
        /// <summary>Authored farm row title as the player reads it, after the post-count refresh.</summary>
        public string FarmRowTitle
        {
            get
            {
                Text nameText = rowFarm != null
                    ? rowFarm.transform.Find(FarmRowNameChildName)?.GetComponent<Text>()
                    : null;
                return nameText != null ? nameText.text : string.Empty;
            }
        }

        public string FarmDetail => farmDetailText != null ? farmDetailText.text : string.Empty;
        public bool FarmDetailVisible => farmDetailText != null && farmDetailText.gameObject.activeSelf;
        public string TradingDetail => tradingDetailText != null ? tradingDetailText.text : string.Empty;
        public bool TradingDetailVisible =>
            tradingDetailText != null && tradingDetailText.gameObject.activeSelf;
        public string PreviewLine => previewText != null ? previewText.text : string.Empty;
        public bool PreviewVisible => previewText != null && previewText.gameObject.activeSelf;

        /// <summary>待确认的产业；没有选中时为空。</summary>
        public string PendingIndustryId => _pendingIndustryId;

        /// <summary>待确认的人物；没有选中时为空。</summary>
        public PersonId PendingPersonId => _pendingPersonId;
        public bool HasPendingAssignment => !string.IsNullOrEmpty(_pendingIndustryId);
        public bool ConfirmVisible => confirmButton != null && confirmButton.gameObject.activeSelf;
        public bool ConfirmInteractable => confirmButton != null && confirmButton.interactable;
        public bool CancelVisible => cancelButton != null && cancelButton.gameObject.activeSelf;

        /// <summary>
        /// 在确认控件出现之前构建的场景可以预演任命，但无法提交。
        /// 选中会被拒绝并说明原因，而不是变成一键死路。
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

        /// <summary>
        /// 隐藏页面并丢弃未确认的选择，因此重新打开不会恢复
        /// 玩家离开时已无法看到的选中。
        /// </summary>
        public void Close()
        {
            ClearPending();
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

            // 先量带区再排行，放不下的消息会变成报告指针，
            // 全文随后交给报告区。
            string reportLine = BuildReportLine(view);
            string statusLine = SelectStatusLine(view.LastMessage);
            // 带区显示不了的内容仍留在报告区可读，
            // 而不是消失在带区原始的截断里。
            if (CarriesFullMessage(view.LastMessage) &&
                !ContainsReportText(reportLine, view.LastMessage))
                reportLine = CarryIntoReport(reportLine, view.LastMessage);
            if (messageText != null)
            {
                messageText.text = statusLine;
                messageText.gameObject.SetActive(statusLine.Length > 0);
            }

            // 领域已无法接受的选中会在重建行之前丢弃，
            // 因此没有候选会为无效选择保留标记或预览。
            if (HasPendingAssignment && !CanStillAssign(view, _pendingIndustryId, _pendingPersonId))
                ClearPending();

            RefreshRow(IndustryCatalog.TradingHouseId, view, rowTradingHouse, tradingStatusText,
                tradingDetailText, tradingAssigneeText, tradingUnassignButton,
                tradingCandidateContainer);
            RefreshFarmRow(view);
            RebuildCandidateLayout();

            // 报告标签横向换行，滚动内容增高到完整携带文字的高度，
            // 因此回滚或长结算仍可达，而不是被裁在原始视口里。
            reportText.horizontalOverflow = HorizontalWrapMode.Wrap;
            reportText.verticalOverflow = VerticalWrapMode.Truncate;
            reportText.text = reportLine;
            FitReportRegion();
            RefreshPendingPreview(view);
        }

        /// <summary>
        /// 选择状态文字。带区有界，因此需要行数超过可显示量的消息
        /// 会被换成报告指针并交给报告区，由那里保存全文。
        /// </summary>
        private string SelectStatusLine(string message)
        {
            StatusPointerActive = false;
            StatusLineCount = 0;
            StatusBandLineBudget = messageText != null ? ComputeStatusBandLineBudget() : 0;
            if (messageText == null || string.IsNullOrWhiteSpace(message))
                return string.Empty;

            StatusLineCount = CountStatusLines(message);
            if (StatusLineCount <= 0 || StatusBandLineBudget <= 0 ||
                StatusLineCount <= StatusBandLineBudget)
                return message;
            StatusPointerActive = true;
            return ReportPointerStatus;
        }

        /// <summary>
        /// 带区未完整显示消息时为 true：没有标签、显示的是报告指针，
        /// 或无法按原始带区测量。
        /// </summary>
        private bool CarriesFullMessage(string message) =>
            !string.IsNullOrWhiteSpace(message) &&
            (messageText == null || StatusPointerActive || StatusLineCount <= 0 ||
             StatusBandLineBudget <= 0);

        /// <summary>
        /// 把滚动的报告内容增高到标签完整换行高度，原始视口尺寸不变。
        /// 没有滚动层级的场景保持原先的裁切行为，而不是报错。
        /// </summary>
        private void FitReportRegion()
        {
            RectTransform reportRect = reportText.rectTransform;
            RectTransform content = reportRect.parent as RectTransform;
            RectTransform viewport = content != null ? content.parent as RectTransform : null;
            if (content == null || viewport == null ||
                viewport.GetComponent<RectMask2D>() == null)
                return;

            // 视口嵌套在其 ScrollRect 之下，因此滚动对象是祖先，
            // 能收到从视口冒泡上来的滚轮与拖拽事件。
            ScrollRect scroll = viewport.GetComponentInParent<ScrollRect>(true);
            FitReportBandToPanel(scroll, viewport);

            content.sizeDelta = new Vector2(content.sizeDelta.x,
                Mathf.Max(viewport.rect.height, reportText.preferredHeight));

            if (scroll != null && reportText.text != _lastReportLine)
            {
                _lastReportLine = reportText.text;
                scroll.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// 让原始的 700x68 报告带留在实时面板内。家宅页面使用的卡片比面板
        /// 原始尺寸矮，因此带区上移并缩短，留在动作行与卡片下沿之间。
        /// </summary>
        private void FitReportBandToPanel(ScrollRect scroll, RectTransform viewport)
        {
            RectTransform panel = panelRoot != null ? panelRoot.transform as RectTransform : null;
            if (scroll == null || panel == null)
                return;

            _fittedPanelHeight = panel.rect.height;
            float halfHeight = _fittedPanelHeight * 0.5f;
            if (halfHeight >= 350f)
            {
                SetReportBand(scroll, viewport, ReportBandCenterY, ReportBandHeight);
                return;
            }

            RectTransform action = confirmButton != null
                ? confirmButton.transform as RectTransform
                : null;
            float topLimit = action != null
                ? action.anchoredPosition.y - action.rect.height * 0.5f - 2f
                : halfHeight - 2f;
            float bottomLimit = -halfHeight + ReportBandBottomMargin;
            float height = Mathf.Min(ReportBandHeight, topLimit - bottomLimit);
            float center = (topLimit + bottomLimit) * 0.5f;
            SetReportBand(scroll, viewport, center, Mathf.Max(0f, height));
        }

        private static void SetReportBand(ScrollRect scroll, RectTransform viewport,
            float centerY, float height)
        {
            RectTransform scrollRect = scroll.transform as RectTransform;
            if (scrollRect != null &&
                (Mathf.Abs(scrollRect.anchoredPosition.y - centerY) > 0.01f ||
                 Mathf.Abs(scrollRect.rect.height - height) > 0.01f))
            {
                scrollRect.anchoredPosition = new Vector2(scrollRect.anchoredPosition.x, centerY);
                scrollRect.sizeDelta = new Vector2(scrollRect.sizeDelta.x, height);
            }

            RectTransform bar = scroll.verticalScrollbar != null
                ? scroll.verticalScrollbar.transform as RectTransform
                : null;
            if (bar != null &&
                (Mathf.Abs(bar.anchoredPosition.y - centerY) > 0.01f ||
                 Mathf.Abs(bar.rect.height - height) > 0.01f))
            {
                bar.anchoredPosition = new Vector2(bar.anchoredPosition.x, centerY);
                bar.sizeDelta = new Vector2(bar.sizeDelta.x, height);
            }

            viewport.sizeDelta = Vector2.zero;
        }

        private static bool ContainsReportText(string reportLine, string message) =>
            !string.IsNullOrEmpty(reportLine) &&
            reportLine.IndexOf(message, System.StringComparison.Ordinal) >= 0;

        /// <summary>
        /// 携带的消息排在报告之前，因此玩家刚触发的反馈保留可见行；
        /// 月度事实紧随其后，仍在同一滚动区内，
        /// 使整份报告留在原始视口内。
        /// </summary>
        private static string CarryIntoReport(string reportLine, string message) =>
            string.IsNullOrEmpty(reportLine) ? message : message + ReportCarrySeparator + reportLine;

        /// <summary>带区可用的渲染行数，按其自身字体与高度测量。</summary>
        private int ComputeStatusBandLineBudget()
        {
            if (!TryBuildStatusSettings(out TextGenerationSettings settings, out Rect band))
                return 0;
            float lineHeight = messageText.cachedTextGeneratorForLayout
                                   .GetPreferredHeight("国", settings) /
                               Mathf.Max(0.0001f, messageText.pixelsPerUnit);
            return lineHeight <= 0f ? 0 : Mathf.FloorToInt((band.height + 0.5f) / lineHeight);
        }

        /// <summary>一条候选消息在带区宽度下的换行行数。</summary>
        private int CountStatusLines(string message)
        {
            if (!TryBuildStatusSettings(out TextGenerationSettings settings, out _))
                return 0;
            TextGenerator generator = messageText.cachedTextGeneratorForLayout;
            generator.Populate(message, settings);
            return generator.lineCount;
        }

        /// <summary>
        /// 带区的测量设置。强制开启纵向溢出，使长候选被完整测量，
        /// 而不是被原始截断夹住。
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
        /// The farm owns two posts. Both are shown on the one authored farm row, because
        /// the scene holds one row per industry: a compact per-post status line and a
        /// per-post assignee line, one shared candidate list that fills the first vacant
        /// post, and one release button that frees the highest-index staffed post.
        /// </summary>
        private void RefreshFarmRow(IndustryView view)
        {
            if (rowFarm == null)
                return;
            IndustryDefinition definition = IndustryCatalog.TryGet(IndustryCatalog.FarmId,
                out IndustryDefinition found)
                ? found
                : default;
            rowFarm.SetActive(true);

            IReadOnlyList<IndustrySlotView> posts = view.PostsOf(IndustryCatalog.FarmId);
            Text nameText = rowFarm.transform.Find(FarmRowNameChildName)?.GetComponent<Text>();
            if (nameText != null)
                nameText.text = FarmRowLabel;
            farmStatusText.text = BuildPostStatusLine(posts);
            RefreshDetail(farmDetailText, definition);
            farmAssigneeText.text = BuildPostAssigneeLine(posts);
            bool anyStaffed = false;
            for (int i = 0; i < posts.Count; i++)
                if (!posts[i].AssigneeId.IsEmpty)
                    anyStaffed = true;
            farmUnassignButton.interactable = anyStaffed && view.CanEditAssignments;
            BuildCandidates(IndustryCatalog.FarmId, view, farmCandidateContainer);
            RebuildLayout(farmCandidateContainer);
        }

        private static string BuildPostStatusLine(IReadOnlyList<IndustrySlotView> posts)
        {
            if (posts.Count == 0)
                return string.Empty;
            string line = string.Empty;
            for (int i = 0; i < posts.Count; i++)
            {
                if (i > 0)
                    line += "；";
                line += PostLabel(posts[i].PostIndex) + "：" + ShortStatus(posts[i].Status);
            }
            return line;
        }

        private static string BuildPostAssigneeLine(IReadOnlyList<IndustrySlotView> posts)
        {
            if (posts.Count == 0)
                return "负责人：空缺";
            string line = string.Empty;
            for (int i = 0; i < posts.Count; i++)
            {
                if (i > 0)
                    line += "｜";
                IndustrySlotView slot = posts[i];
                line += PostLabel(slot.PostIndex) + " ";
                line += slot.AssigneeId.IsEmpty
                    ? "空缺"
                    : slot.AssigneeName + "（剩" + slot.WorkUnitsRemaining + "）";
            }
            return line;
        }

        private static string PostLabel(int postIndex)
        {
            switch (postIndex)
            {
                case 0: return "一号";
                case 1: return "二号";
                case 2: return "三号";
                default: return (postIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "号";
            }
        }

        private static string ShortStatus(IndustryLineStatus status)
        {
            switch (status)
            {
                case IndustryLineStatus.Operated: return "运作中";
                case IndustryLineStatus.Vacant: return "空缺";
                case IndustryLineStatus.Stalled: return "停产";
                case IndustryLineStatus.InvalidAssignment: return "任命失效";
                default: return "未知";
            }
        }

        /// <summary>
        /// 一个产业原始的月度投入与产出，取自只读图鉴规则。
        /// 图鉴未定义的字段（地点）保持隐藏而不是编造，
        /// 缺少定义时整行隐藏。
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
            // A multi-post industry only lists candidates while a post is actually vacant:
            // filling a filled industry would have to evict someone silently.
            if (IndustryState.PostCount(industryId) > 1 &&
                view.FirstVacantPost(industryId) < 0)
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
                    // 没有确认控件时，点击只能改变高亮，
                    // 因此候选保持禁用，由预览说明原因。
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

        /// <summary>每个可选人物只计一次，即使两行都列出他。</summary>
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
        /// 选中候选只预演任命；领域在玩家确认时才改变。
        /// 取消清空预览，不写入任何内容。
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
            // 成功时 SessionStateChanged 已发布；这里刷新被拒或已清空的
            // 预览，使面板永不显示过期预览。
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
        /// 绘制待定对象与确认/取消选项。预览标签出现之前构建的场景
        /// 没有地方绘制该文字，因此候选禁用的原因改写到它已绑定的
        /// 消息行上。
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
        /// 让预览出现之前的场景也能解释候选为何禁用。消息行是这类场景
        /// 已绑定的面；只有消息行缺失时才用阻塞原因行，
        /// 且既有领域文字排在提示之后，而不是被替换。
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
            // A multi-post industry needs a vacant post for the pending selection; the
            // single-post trading house keeps its historical replace-on-confirm behavior.
            if (IndustryState.PostCount(industryId) > 1 &&
                view.FirstVacantPost(industryId) < 0)
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
        /// 仅按图鉴规则与预演槽位构建的待定任命预览。它复述原始的月度规则；
        /// 月份是否运作仍以月度结算为唯一权威。
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
            int postIndex = IndustryState.PostCount(_pendingIndustryId) > 1
                ? view.FirstVacantPost(_pendingIndustryId)
                : 0;
            if (postIndex < 0)
                return string.Empty;
            IndustrySlotView target = view.Slot(_pendingIndustryId, postIndex);
            string holder = target.AssigneeId.IsEmpty
                ? "岗位空缺"
                : "替换现任 " + target.AssigneeName;

            return "已选择：" + IndustryService.PersonName(succession, _pendingPersonId) +
                   " → " + target.DisplayName + "（待确认）｜" + holder +
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

        /// <summary>
        /// 家宅页面会在首次刷新后把本面板调到 560 高的卡片，因此按原始
        /// 700 高页面适配的报告带会悬在卡片之外。LateUpdate 在该布局变化
        /// 之后、绘制帧之前运行，这是已知实时高度、且带区仍能按玩家
        /// 实际看到的卡片重新适配的第一个时点。
        /// </summary>
        private void LateUpdate()
        {
            if (!IsOpen || reportText == null)
                return;
            RectTransform panel = panelRoot.transform as RectTransform;
            if (panel == null || Mathf.Abs(panel.rect.height - _fittedPanelHeight) <= 0.01f)
                return;
            FitReportRegion();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearPending();
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
