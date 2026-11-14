using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M05 shadow-escort commission page. It presents the single approved commission:
    /// the offer and creed-dependent investment, the living-family assignee list, the
    /// three-day execution window, and the settled outcome with its applied rewards and
    /// chronicle record. Accepting and assigning go through
    /// <see cref="GameSession.TryAcceptCommission"/> and
    /// <see cref="GameSession.TryAssignCommission"/>; the panel never advances time,
    /// moves the party, spends resources or writes the chronicle itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShadowEscortCommissionPanel : MonoBehaviour
    {
        /// <summary>Fixed scene root the generated commission UI hangs from.</summary>
        public const string RootObjectName = "M5ShadowEscortCommissionRoot";

        /// <summary>Generated commission page; it owns the indigo surface and its colors.</summary>
        public const string PageObjectName = "M5ShadowEscortCommissionPage";

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text dateText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text creedText;
        [SerializeField] private Text offerText;
        [SerializeField] private Text consequenceText;
        [SerializeField] private Text assigneeText;
        [SerializeField] private Text outcomeText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text chronicleText;
        [SerializeField] private Transform candidateContainer;
        [SerializeField] private GameObject candidateTemplate;

        private readonly List<GameObject> _candidates = new List<GameObject>();
        private readonly List<PersonId> _candidatePersons = new List<PersonId>();
        private readonly List<GameObject> _optionRows = new List<GameObject>();
        private WorldMapController _worldMapController;
        private bool _isOpen;
        private bool _subscribed;

        public bool IsOpen => _isOpen;
        public int RefreshCount { get; private set; }
        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            acceptButton != null && titleText != null && stateText != null && dateText != null &&
            resourceText != null && creedText != null && offerText != null &&
            consequenceText != null && assigneeText != null && outcomeText != null &&
            messageText != null && chronicleText != null && candidateContainer != null &&
            candidateTemplate != null;

        public string StateLine => TextOf(stateText);
        public string DateLine => TextOf(dateText);
        public string ResourceLine => TextOf(resourceText);
        public string CreedLine => TextOf(creedText);
        public string OfferLine => TextOf(offerText);
        public string ConsequenceLine => TextOf(consequenceText);
        public string AssigneeLine => TextOf(assigneeText);
        public string OutcomeLine => TextOf(outcomeText);
        public string MessageLine => TextOf(messageText);
        public string ChronicleLine => TextOf(chronicleText);
        public bool AcceptInteractable => acceptButton != null && acceptButton.interactable;
        public int CandidateCount => _candidates.Count;
        public int EligibleCandidateCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _candidates.Count; i++)
                    if (CandidateInteractable(i))
                        count++;
                return count;
            }
        }

        public string CandidateLabel(int index)
        {
            if (index < 0 || index >= _candidates.Count || _candidates[index] == null)
                return string.Empty;
            Text label = _candidates[index].GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
        }

        public bool CandidateInteractable(int index)
        {
            if (index < 0 || index >= _candidates.Count || _candidates[index] == null)
                return false;
            Button button = _candidates[index].GetComponent<Button>();
            return button != null && button.interactable;
        }

        public PersonId CandidatePerson(int index)
        {
            if (index < 0 || index >= _candidatePersons.Count)
                return default;
            return _candidatePersons[index];
        }

        public void ClickCandidate(int index)
        {
            if (index < 0 || index >= _candidates.Count || _candidates[index] == null)
                return;
            Button button = _candidates[index].GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke();
        }

        public void BindReferences(GameSession session, GameObject panel, Button open,
            Button close, Button accept, Text title, Text state, Text date, Text resources,
            Text creed, Text offer, Text consequence, Text assignee, Text outcome, Text message,
            Text chronicle, Transform candidates, GameObject template)
        {
            Unsubscribe();
            ClearCandidates();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            acceptButton = accept;
            titleText = title;
            stateText = state;
            dateText = date;
            resourceText = resources;
            creedText = creed;
            offerText = offer;
            consequenceText = consequence;
            assigneeText = assignee;
            outcomeText = outcome;
            messageText = message;
            chronicleText = chronicle;
            candidateContainer = candidates;
            candidateTemplate = template;
            _isOpen = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);
            Subscribe();
            Refresh();
        }

        /// <summary>Rebinds after a load or session swap without touching gameplay state.</summary>
        public void Bind(GameSession session)
        {
            if (gameSession != session)
            {
                Unsubscribe();
                gameSession = session;
                _isOpen = false;
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                ClearCandidates();
            }
            Subscribe();
            Refresh();
        }

        /// <summary>
        /// Gives the page its town-flow owner, so the entry button opens the page through
        /// <see cref="WorldMapController.ToggleShadowEscortCommissionPanel"/> instead of
        /// bypassing the controller. Null keeps the panel usable in isolation for tests.
        /// </summary>
        public void BindController(WorldMapController controller)
        {
            _worldMapController = controller;
        }

        public void OpenFromEntryButton()
        {
            if (_worldMapController != null)
            {
                _worldMapController.ToggleShadowEscortCommissionPanel();
                return;
            }
            Open();
        }

        public void Open()
        {
            _isOpen = true;
            if (panelRoot != null)
                panelRoot.SetActive(true);
            if (openButton != null)
                openButton.gameObject.SetActive(false);
            Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            ClearCandidates();
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Read-only projection of the current commission. Refreshing never advances
        /// time, moves the party, spends resources or writes a chronicle record.
        /// </summary>
        public void Refresh()
        {
            RefreshCount++;
            ClearCandidates();
            FitPageToParent();
            if (gameSession == null)
            {
                SetText(titleText, "护送委托");
                SetText(stateText, "状态：不可用");
                SetText(dateText, "日期：—");
                SetText(resourceText, "资金 —　物资 —");
                SetText(creedText, "缺少 GameSession，无法读取家训与投入。");
                SetText(offerText, "缺少 GameSession。");
                SetText(consequenceText, string.Empty);
                SetText(assigneeText, "执行人：—");
                SetText(outcomeText, "结果：—");
                SetText(messageText, "缺少 GameSession，无法执行委托操作。");
                SetText(chronicleText, string.Empty);
                SetAccept(false);
                return;
            }

            ShadowEscortCommissionStatus status = gameSession.CommissionStatus;
            IndustryResources resources = gameSession.Industry.Resources;
            FamilyCreedKind creed = gameSession.CurrentFamilyCreed;
            int investment = ShadowEscortCommission.InvestmentFundsFor(creed);

            SetText(titleText, "护送委托");
            SetText(stateText, "状态：" + StageLabel(status.Stage, gameSession.CurrentLocation));
            SetText(dateText, "日期：" + CurrentDate() + "（" +
                              LocationId.GetChineseName(gameSession.CurrentLocation) + "）");
            SetText(resourceText, "资金 " + resources.Funds + "　物资 " + resources.Supplies +
                                  "　情报 " + resources.Intelligence);
            SetText(creedText, BuildCreedLine(creed, investment));
            SetText(offerText, BuildOfferLine());
            SetText(consequenceText, BuildConsequenceLine(creed));
            SetText(assigneeText, BuildAssigneeLine(status));
            SetText(outcomeText, BuildOutcomeLine(status));
            SetText(messageText, status.Feedback);
            SetText(chronicleText, BuildChronicleLine());

            bool canAccept = status.Stage == ShadowEscortStage.NotAccepted &&
                             gameSession.CurrentLocation == LocationId.Kiyosu &&
                             _isOpen && isActiveAndEnabled;
            bool canSettleManually = gameSession.IsCommissionManualSettlementAvailable &&
                                     _isOpen && isActiveAndEnabled;
            bool canRetryDelegated =
                gameSession.IsCommissionDelegatedSettlementRetryAvailable &&
                _isOpen && isActiveAndEnabled;
            SetAccept(canAccept || canSettleManually || canRetryDelegated);
            SetAcceptLabel(canSettleManually ? "结算委托"
                : canRetryDelegated ? "重试结算"
                : "接取委托");

            if (_isOpen && status.Stage == ShadowEscortStage.Accepted)
                BuildCandidates();
            if (_isOpen && (status.Stage == ShadowEscortStage.NotAccepted ||
                            status.Stage == ShadowEscortStage.Accepted))
                BuildModeRows(status.Stage);
        }

        /// <summary>
        /// Two explicit, player-visible mode choices built from the authored candidate
        /// template under the panel's existing candidate container. No scene or prefab is
        /// touched; the row stays available until execution starts and then freezes.
        /// </summary>
        private void BuildModeRows(ShadowEscortStage stage)
        {
            if (candidateContainer == null || candidateTemplate == null)
                return;
            bool interactable = _isOpen && isActiveAndEnabled &&
                                (stage == ShadowEscortStage.NotAccepted ||
                                 stage == ShadowEscortStage.Accepted);
            AddModeRow(MissionOperationMode.Manual, interactable);
            AddModeRow(MissionOperationMode.Delegated, interactable);
            if (candidateContainer is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void AddModeRow(MissionOperationMode mode, bool interactable)
        {
            GameObject row = Instantiate(candidateTemplate, candidateContainer);
            row.name = "CommissionMode_" + mode;
            row.SetActive(true);
            bool selected = gameSession.CommissionMode == mode;
            Text label = row.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = (selected ? "✔ " : string.Empty) +
                             (mode == MissionOperationMode.Manual
                                 ? "亲自执行（到期后手动点击结算）"
                                 : "自动委派（到期时自动结算）");
            Button button = row.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
                MissionOperationMode captured = mode;
                button.onClick.AddListener(() => SelectMode(captured));
            }
            _optionRows.Add(row);
        }

        private void SelectMode(MissionOperationMode mode)
        {
            if (gameSession == null)
                return;
            gameSession.TrySetCommissionMode(mode, out _);
            if (_isOpen)
                Refresh();
        }

        private void SetAcceptLabel(string label)
        {
            if (acceptButton == null)
                return;
            Text text = acceptButton.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;
        }

        private GameDate CurrentDate() =>
            gameSession != null && gameSession.WorldClock != null
                ? gameSession.WorldClock.CurrentDate
                : default;

        /// <summary>
        /// Keeps the fixed 1160x260 page inside its town parent on narrower windows.
        /// The CanvasScaler uses ScaleWithScreenSize at a 1280x720 reference, but the
        /// town root narrows with the window, so the page scales down uniformly instead
        /// of clipping its right-hand candidate column. At the reference size the scale
        /// is exactly 1 and the authored layout is untouched.
        /// </summary>
        private void FitPageToParent()
        {
            if (panelRoot == null || !(panelRoot.transform is RectTransform page) ||
                !(page.parent is RectTransform parent))
                return;
            float availableWidth = parent.rect.width - 16f;
            float availableHeight = parent.rect.height - 8f;
            if (availableWidth <= 0f || availableHeight <= 0f ||
                page.rect.width <= 0f || page.rect.height <= 0f)
                return;
            float scale = Mathf.Min(1f,
                Mathf.Min(availableWidth / page.rect.width, availableHeight / page.rect.height));
            scale = Mathf.Max(0.25f, scale);
            page.localScale = new Vector3(scale, scale, 1f);
        }

        public void Accept()
        {
            if (gameSession == null)
            {
                Refresh();
                return;
            }
            if (gameSession.IsCommissionManualSettlementAvailable)
            {
                MissionSettlementResult settlement = gameSession.TrySettleCommissionManually();
                if (!settlement.Succeeded && _isOpen)
                    Refresh();
                return;
            }
            if (gameSession.IsCommissionDelegatedSettlementRetryAvailable)
            {
                MissionSettlementResult retry = gameSession.TryRetryCommissionDelegated();
                if (!retry.Succeeded && _isOpen)
                    Refresh();
                return;
            }
            ShadowEscortActionResult result = gameSession.TryAcceptCommission();
            // A successful write already publishes SessionStateChanged; refresh only a
            // rejection here so its visible reason appears once.
            if (!result.Succeeded && _isOpen)
                Refresh();
        }

        private void Assign(PersonId personId)
        {
            if (gameSession == null)
                return;
            ShadowEscortActionResult result = gameSession.TryAssignCommission(personId);
            if (!result.Succeeded && _isOpen)
                Refresh();
        }

        private void BuildCandidates()
        {
            if (candidateContainer == null || candidateTemplate == null)
                return;

            IReadOnlyList<CommissionAssigneeOption> options = gameSession.CommissionAssignees;
            int eligible = 0;
            for (int i = 0; i < options.Count; i++)
                if (options[i].Eligible)
                    eligible++;

            for (int i = 0; i < options.Count; i++)
            {
                CommissionAssigneeOption option = options[i];
                GameObject buttonObject = Instantiate(candidateTemplate, candidateContainer);
                buttonObject.name = "CommissionCandidate_" + option.PersonId.Value;
                buttonObject.SetActive(true);
                Text label = buttonObject.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = option.Eligible
                        ? option.DisplayName + "（可指派）"
                        : option.DisplayName + "｜" + option.Reason;
                }
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = option.Eligible && _isOpen && isActiveAndEnabled;
                    PersonId captured = option.PersonId;
                    button.onClick.AddListener(() => Assign(captured));
                }
                _candidates.Add(buttonObject);
                _candidatePersons.Add(option.PersonId);
            }

            if (options.Count == 0)
                SetText(assigneeText, "执行人：没有在世的家族成员可指派。");
            else if (eligible == 0)
                SetText(assigneeText, "执行人：无符合条件的家人（共 " + options.Count +
                                      " 名在世家人），请先处理上列占用。");
            else
                SetText(assigneeText, "执行人：待指派（可指派 " + eligible + " 人，共 " +
                                      options.Count + " 名在世家人）");

            if (candidateContainer is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private static string StageLabel(ShadowEscortStage stage, LocationId location)
        {
            switch (stage)
            {
                case ShadowEscortStage.Accepted:
                    return "已接取，等待指派执行人";
                case ShadowEscortStage.InProgress:
                    return "执行中";
                case ShadowEscortStage.Succeeded:
                    return "成功，已结算";
                case ShadowEscortStage.PartialSuccess:
                    return "部分成功，已结算";
                case ShadowEscortStage.Failed:
                    return "失败，已结算";
                case ShadowEscortStage.Abandoned:
                    return "已中止（执行人不可用，无结算结果）";
                default:
                    return location == LocationId.Kiyosu
                        ? "待接取（清洲本家）"
                        : "待接取（需返回清洲本家）";
            }
        }

        private static string BuildCreedLine(FamilyCreedKind creed, int investment)
        {
            return "家训：" + FamilyDoctrineView.CreedNameOf(creed) + "\n接取投入：" + investment +
                   " 资金（不退还）\n" + CreedEffect(creed);
        }

        private static string CreedEffect(FamilyCreedKind creed)
        {
            switch (creed)
            {
                case FamilyCreedKind.SecrecyFirst:
                    return "检定 +0；失败降为部分成功";
                case FamilyCreedKind.MeritFirst:
                    return "检定 +" + ShadowEscortCommission.MeritFirstScoreBonus + "；成功额外 +" +
                           ShadowEscortCommission.MeritFirstRewardFundsBonus + " 资金";
                case FamilyCreedKind.RighteousnessFirst:
                    return "检定 +" + ShadowEscortCommission.RighteousnessScoreBonus;
                default:
                    return "检定 +0";
            }
        }

        private static string BuildOfferLine()
        {
            return "委托方：尾张商队（护送车队）\n" +
                   "期限：" + ShadowEscortCommission.DurationDays + " 个模拟日\n" +
                   "检定：武艺＋管理＋社交＋家训修正＋掷值 0–99\n" +
                   "总分 ≥" + ShadowEscortCommission.SuccessThreshold + " 成功；" +
                   ShadowEscortCommission.PartialThreshold + "–" +
                   (ShadowEscortCommission.SuccessThreshold - 1) + " 部分成功；其余失败\n" +
                   "参与人物：1 名可工作的在世家人；未成年、已隐居或有其他职责者不可用。";
        }

        private string BuildConsequenceLine(FamilyCreedKind creed)
        {
            int successFunds = ShadowEscortCommission.RewardSuccessFunds +
                               (creed == FamilyCreedKind.MeritFirst
                                   ? ShadowEscortCommission.MeritFirstRewardFundsBonus
                                   : 0);
            string failure = creed == FamilyCreedKind.SecrecyFirst
                ? "失败：家训「守秘为先」降为部分成功，按部分成功结算。"
                : "失败：无报酬。";
            return "成功：+" + successFunds + " 资金、+" +
                   ShadowEscortCommission.RewardSuccessSupplies + " 物资\n" +
                   "部分成功：+" + ShadowEscortCommission.RewardPartialFunds + " 资金、+" +
                   ShadowEscortCommission.RewardPartialSupplies + " 物资\n" +
                   failure + "\n" +
                   "预付投入在接取时扣除，任何结果都不退还；失败或部分成功同样消耗投入。\n" +
                   "M07 共用结果：完全成功 名望+3／关系+5／隐蔽度+3；部分成功 +1／+1／0；" +
                   "失败并安全撤离 -1／-3／-2。\n" +
                   "本委托不会造成死亡，也不会造成新的伤势；伤员在下一次月结前不能参加。";
        }

        private string BuildAssigneeLine(ShadowEscortCommissionStatus status)
        {
            switch (status.Stage)
            {
                case ShadowEscortStage.InProgress:
                    return "执行人：" + PersonName(status.AssigneeId) + "　已过 " +
                           status.ElapsedDays + "/" + status.DurationDays + " 天，剩余 " +
                           status.RemainingDays + " 天";
                case ShadowEscortStage.Succeeded:
                case ShadowEscortStage.PartialSuccess:
                case ShadowEscortStage.Failed:
                    return "执行人：" + PersonName(status.AssigneeId) + "（" +
                           status.DurationDays + " 天执行窗口已结束）";
                case ShadowEscortStage.Abandoned:
                    return "执行人：" + PersonName(status.AssigneeId) + "（执行中止）";
                case ShadowEscortStage.Accepted:
                    return "执行人：待指派";
                default:
                    return "执行人：接取后可指派 1 名在世家人";
            }
        }

        private string BuildOutcomeLine(ShadowEscortCommissionStatus status)
        {
            if (status.Stage == ShadowEscortStage.Abandoned)
                return "结果：执行中止（执行人不可用）。\n没有共享结算结果、没有名望或关系变化，" +
                       "预付投入不退；中止行动单独记录在编年史。";
            if (!status.IsSettled)
            {
                if (status.Stage == ShadowEscortStage.InProgress)
                {
                    if (gameSession.IsCommissionManualSettlementAvailable)
                        return "结果：执行中，尚未结算。\n日推进、旅行或结束本月都会消耗执行日。" +
                               "\n已到期：请点击「结算委托」。";
                    if (gameSession.IsCommissionDelegatedSettlementRetryAvailable)
                        return "结果：执行中，到期自动结算未完成。\n" +
                               "可点击「重试结算」再次提交同一委托；不会重复支付。";
                    return "结果：执行中，尚未结算。\n日推进、旅行或结束本月都会消耗执行日。" +
                           "\n到期后自动委派会自行结算。";
                }
                if (status.Stage == ShadowEscortStage.Accepted)
                    return "结果：尚未开始。\n指派后开始 " + ShadowEscortCommission.DurationDays +
                           " 天倒计时。";
                return "结果：尚未接取。";
            }

            string band = status.Outcome == CommissionOutcome.Success ? "完全成功"
                : status.Outcome == CommissionOutcome.Partial ? "部分成功"
                : "失败并安全撤离";
            string reward = status.RewardFunds + status.RewardSupplies > 0
                ? "报酬已计入：+" + status.RewardFunds + " 资金、+" + status.RewardSupplies + " 物资"
                : "报酬：无；预付 " + status.InvestedFunds + " 资金已消耗";
            return "结果：" + band + "｜总分 " + status.Score + "（掷值 " + status.Roll + "）\n" +
                   reward + "\n" + BuildStandingLine();
        }

        /// <summary>Actual standing before/after of the last settled M05 run.</summary>
        private string BuildStandingLine()
        {
            IReadOnlyList<MissionRecord> records = gameSession.MissionHistory;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                MissionRecord record = records[i];
                if (record.Kind != MissionKind.ShadowEscort)
                    continue;
                var builder = new System.Text.StringBuilder(96);
                builder.Append("名望 ").Append(record.FameBefore).Append("→")
                    .Append(record.FameAfter).Append("，隐蔽度 ")
                    .Append(record.ConcealmentBefore).Append("→")
                    .Append(record.ConcealmentAfter);
                for (int r = 0; r < record.RelationChanges.Count; r++)
                {
                    MissionStandingChange change = record.RelationChanges[r];
                    builder.Append("，").Append(GameSession.RelationDisplayName(change.RelationId))
                        .Append(" ").Append(change.Before).Append("→").Append(change.After);
                }
                return builder.ToString();
            }
            return "名望与关系：本局尚无 M05 结算记录。";
        }

        private string BuildChronicleLine()
        {
            FamilyChronicleEntry last = null;
            IReadOnlyList<FamilyChronicleEntry> entries = gameSession.ChronicleEntries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                FamilyChronicleEntry entry = entries[i];
                if (entry.Category == ChronicleCategory.Mission &&
                    string.Equals(entry.SubjectId, FamilyChronicleEntry.ShadowEscortCommissionId,
                        System.StringComparison.Ordinal))
                {
                    last = entry;
                    break;
                }
            }
            return last != null
                ? "编年史：" + last.Title + "｜" + last.Summary
                : "编年史：尚无护送委托记录。";
        }

        private string PersonName(PersonId personId)
        {
            if (gameSession == null || personId.IsEmpty)
                return "—";
            SuccessionState succession = gameSession.Succession;
            if (succession != null && succession.World != null &&
                succession.World.TryGetPerson(personId, out PersonState person))
                return person.DisplayName;
            // A historical M05 row can outlive its assignee: the crisis demo replaces the
            // whole family snapshot, so the recorded PersonId may no longer be present.
            // Read as plain player text; the stable ID is not player-facing.
            return "已离开或不可用的家人";
        }

        private void SetAccept(bool interactable)
        {
            if (acceptButton != null)
                acceptButton.interactable = interactable;
        }

        private static string TextOf(Text text) => text != null ? text.text : string.Empty;

        private static void SetText(Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private void OnEnable()
        {
            if (openButton != null)
                openButton.gameObject.SetActive(!_isOpen);
            if (panelRoot != null)
                panelRoot.SetActive(_isOpen);
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            _isOpen = false;
            Unsubscribe();
            ClearCandidates();
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);
        }

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (gameSession != null && !_subscribed)
            {
                gameSession.SessionStateChanged += OnSessionStateChanged;
                _subscribed = true;
            }
            BindButton(acceptButton, Accept, true);
            BindButton(closeButton, Close, true);
            BindButton(openButton, OpenFromEntryButton, true);
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            BindButton(acceptButton, Accept, false);
            BindButton(closeButton, Close, false);
            BindButton(openButton, OpenFromEntryButton, false);
        }

        private void OnSessionStateChanged()
        {
            if (_isOpen)
                Refresh();
        }

        private static void BindButton(Button button, UnityAction action, bool add)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            if (add)
                button.onClick.AddListener(action);
        }

        private void ClearCandidates()
        {
            for (int i = _candidates.Count - 1; i >= 0; i--)
            {
                GameObject candidate = _candidates[i];
                if (candidate == null)
                    continue;
                candidate.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(candidate);
                else
                    DestroyImmediate(candidate);
            }
            _candidates.Clear();
            _candidatePersons.Clear();
            for (int i = _optionRows.Count - 1; i >= 0; i--)
            {
                GameObject row = _optionRows[i];
                if (row == null)
                    continue;
                row.SetActive(false);
                if (UnityEngine.Application.isPlaying)
                    Destroy(row);
                else
                    DestroyImmediate(row);
            }
            _optionRows.Clear();
        }
    }
}
