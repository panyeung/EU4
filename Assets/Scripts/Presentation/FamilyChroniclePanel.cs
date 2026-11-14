using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// SC-012 read-only family chronicle panel. The map button opens one fixed panel
    /// that lists the immutable ledger in append order, scrolls to the newest record,
    /// and shows an explicit empty state. Opening or closing never advances time or
    /// mutates any gameplay state; session changes only re-render the visible list.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FamilyChroniclePanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform content;
        [SerializeField] private GameObject rowTemplate;
        [SerializeField] private Text emptyText;
        [SerializeField] private Text titleText;

        private const float MinimumRowHeight = 44f;

        private readonly List<GameObject> _rows = new List<GameObject>();
        private readonly List<Text> _rowTexts = new List<Text>();
        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private int _staticButtonBindings;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null && closeButton != null &&
            scrollRect != null && content != null && rowTemplate != null && emptyText != null &&
            titleText != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int VisibleRowCount => _rows.Count;
        public ScrollRect ScrollRect => scrollRect;
        public bool EmptyStateVisible => emptyText != null && emptyText.gameObject.activeSelf;
        public string EmptyStateText => emptyText != null ? emptyText.text : string.Empty;
        public int StaticButtonBindingCount => _staticButtonBindings;

        public string RowText(int index) =>
            index >= 0 && index < _rowTexts.Count && _rowTexts[index] != null
                ? _rowTexts[index].text
                : string.Empty;

        public void BindReferences(GameSession session, GameObject panel, Button open, Button close,
            ScrollRect scroll, Transform rows, GameObject template, Text empty, Text title)
        {
            Unsubscribe();
            ClearRows();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            scrollRect = scroll;
            content = rows;
            rowTemplate = template;
            emptyText = empty;
            titleText = title;
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
            ClearRows();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>Opens the panel, rebuilds the list and scrolls to the newest record.</summary>
        public void Open()
        {
            if (panelRoot == null)
                return;
            panelRoot.SetActive(true);
            Refresh();
            ScrollToLatest();
        }

        /// <summary>Closes the panel without touching time, location or session state.</summary>
        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// Rebuilds every row from the immutable ledger. Clear-then-rebuild is what
        /// keeps repeated refreshes from duplicating rows or listeners.
        /// </summary>
        public void Refresh()
        {
            ClearRows();
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            titleText.text = "家族编年史";
            IReadOnlyList<FamilyChronicleEntry> entries = gameSession.ChronicleEntries;
            for (int i = 0; i < entries.Count; i++)
            {
                FamilyChronicleEntry entry = entries[i];
                GameObject row = Instantiate(rowTemplate, content);
                row.name = "FamilyChronicleRow_" + entry.Sequence;
                Text text = row.GetComponentInChildren<Text>(true);
                if (text == null)
                {
                    DestroyRow(row);
                    continue;
                }
                text.text = FormatRow(entry);
                row.SetActive(true);
                _rows.Add(row);
                _rowTexts.Add(text);
            }

            ApplyRowHeights();

            emptyText.text = "尚无家族记录。完成事件、任务、关系变化或继承后，这里会按时间列出。";
            emptyText.gameObject.SetActive(entries.Count == 0);
        }

        /// <summary>
        /// Rows must fit whatever the record rendered, including long succession
        /// summaries. The template therefore carries no fixed preferred height; each
        /// live row measures its own wrapped text at the content width and updates its
        /// LayoutElement before the content is laid out.
        /// </summary>
        private void ApplyRowHeights()
        {
            float width = ResolveRowWidth();
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null || _rowTexts[i] == null)
                    continue;
                LayoutElement layout = _rows[i].GetComponent<LayoutElement>();
                if (layout == null)
                    continue;
                layout.preferredHeight = MeasureRowHeight(_rowTexts[i], width);
            }

            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private float ResolveRowWidth()
        {
            float width = content is RectTransform contentRect ? contentRect.rect.width : 0f;
            if (width <= 1f && scrollRect != null && scrollRect.viewport != null)
                width = scrollRect.viewport.rect.width;
            if (width <= 1f && rowTemplate != null && rowTemplate.transform is RectTransform templateRect)
                width = templateRect.rect.width;

            VerticalLayoutGroup layout = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
            if (layout != null)
                width -= layout.padding.horizontal;
            return Mathf.Max(0f, width);
        }

        private static float MeasureRowHeight(Text text, float width)
        {
            if (text == null || width <= 1f)
                return MinimumRowHeight;
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, 0f));
            float unitsPerPixel = text.pixelsPerUnit > 0f ? text.pixelsPerUnit : 1f;
            float height = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings) /
                           unitsPerPixel;
            return Mathf.Max(MinimumRowHeight, Mathf.Ceil(height));
        }

        private void ScrollToLatest()
        {
            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// One display row. Internal so the EditMode suite can prove the player-facing
        /// text for every append-only category without building a preview scene.
        /// </summary>
        internal static string FormatRow(FamilyChronicleEntry entry)
        {
            var builder = new StringBuilder(96);
            builder.Append(entry.Year.ToString("0000")).Append('年')
                .Append(entry.Month).Append("月・")
                .Append(CategoryName(entry.Category)).Append(' ')
                .Append(entry.Title);

            string detail = CategoryDetail(entry);
            if (!string.IsNullOrEmpty(detail))
                builder.Append('\n').Append(detail);
            if (!string.IsNullOrWhiteSpace(entry.Summary))
                builder.Append('\n').Append(entry.Summary);
            return builder.ToString();
        }

        private static string CategoryName(ChronicleCategory category)
        {
            switch (category)
            {
                case ChronicleCategory.Event: return "事件";
                case ChronicleCategory.Relationship: return "关系";
                case ChronicleCategory.Mission: return "任务";
                case ChronicleCategory.Succession: return "继承";
                case ChronicleCategory.Industry: return "产业";
                case ChronicleCategory.Education: return "教育";
                case ChronicleCategory.Research: return "研究";
                case ChronicleCategory.FamilyFormation: return "家族关系";
                case ChronicleCategory.NaturalDeath: return "人物离世";
                default: return category.ToString();
            }
        }

        private static string CategoryDetail(FamilyChronicleEntry entry)
        {
            switch (entry.Category)
            {
                case ChronicleCategory.Relationship:
                    return "关系 " + entry.FromValue + " → " + entry.ToValue;
                case ChronicleCategory.Mission:
                    return "阶段 " + MissionStageName(entry.SubjectId, entry.FromStage) + " → " +
                           MissionStageName(entry.SubjectId, entry.ToStage);
                case ChronicleCategory.Succession:
                    string phases = "继承阶段 " + SuccessionPhaseName(entry.FromPhase) + " → " +
                                    SuccessionPhaseName(entry.ToPhase);
                    if (entry.HasHeadChange)
                    {
                        return entry.FromPhase == entry.ToPhase
                            ? "家督交接"
                            : "家督交接・" + phases;
                    }
                    return phases;
                case ChronicleCategory.Industry:
                    return "资金 " + entry.FromValue + " → " + entry.ToValue;
                case ChronicleCategory.Education:
                    return string.Equals(entry.SourceId, "education.free-growth",
                        System.StringComparison.Ordinal)
                        ? "自由成长"
                        : "能力 " + entry.FromValue + " → " + entry.ToValue;
                case ChronicleCategory.Research:
                    return "进度 " + entry.FromValue + " → " + entry.ToValue;
                case ChronicleCategory.FamilyFormation:
                    return entry.SourceId == FamilyChronicleEntry.MarriageSourceId
                        ? "婚姻登记"
                        : entry.SourceId == FamilyChronicleEntry.AdoptionSourceId
                            ? "普通收养"
                            : string.Empty;
                case ChronicleCategory.NaturalDeath:
                    return "享年 " + entry.FromValue + " 岁";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Resolves a stored mission stage against that mission's own enum. An unknown
        /// mission ID or stage still renders safely with its numeric value instead of
        /// throwing or showing a misleading name from another mission.
        /// </summary>
        private static string MissionStageName(string missionId, int stage)
        {
            if (missionId == FamilyChronicleEntry.SupplyShortageMissionId)
            {
                switch ((SupplyShortageStage)stage)
                {
                    case SupplyShortageStage.NotAccepted: return "未接受";
                    case SupplyShortageStage.Accepted: return "已接受";
                    case SupplyShortageStage.Investigated: return "已调查";
                    case SupplyShortageStage.Negotiated: return "已交涉";
                    case SupplyShortageStage.RewardClaimed: return "已领取报酬";
                }
            }
            else if (missionId == FamilyChronicleEntry.SealedLetterMissionId)
            {
                switch ((SealedLetterStage)stage)
                {
                    case SealedLetterStage.NotAccepted: return "未接受";
                    case SealedLetterStage.Accepted: return "已接受";
                    case SealedLetterStage.RewardClaimed: return "已领取报酬";
                }
            }
            else if (missionId == FamilyChronicleEntry.MarketTallyMissionId)
            {
                switch ((MarketTallyStage)stage)
                {
                    case MarketTallyStage.NotAccepted: return "未接受";
                    case MarketTallyStage.Accepted: return "已接受";
                    case MarketTallyStage.Investigated: return "已调查";
                    case MarketTallyStage.RewardClaimed: return "已领取报酬";
                }
            }
            else if (missionId == FamilyChronicleEntry.ShadowEscortCommissionId)
            {
                // ShadowEscortCommissionPanel.StageLabel adds the current location to the
                // not-accepted label; a history row has no location, so only its
                // location-free part is used here.
                switch ((ShadowEscortStage)stage)
                {
                    case ShadowEscortStage.NotAccepted: return "待接取";
                    case ShadowEscortStage.Accepted: return "已接取，等待指派执行人";
                    case ShadowEscortStage.InProgress: return "执行中";
                    case ShadowEscortStage.Succeeded: return "成功，已结算";
                    case ShadowEscortStage.PartialSuccess: return "部分成功，已结算";
                    case ShadowEscortStage.Failed: return "失败，已结算";
                    case ShadowEscortStage.Abandoned: return "已中止（执行人不可用，无结算结果）";
                }
            }
            else if (missionId == FamilyChronicleEntry.TacticalMissionId)
            {
                switch ((TacticalMissionStage)stage)
                {
                    case TacticalMissionStage.NotStarted: return "未开始";
                    case TacticalMissionStage.InProgress: return "潜入进行中";
                    case TacticalMissionStage.Succeeded: return "成功，已结算";
                    case TacticalMissionStage.Failed: return "失败，已结算";
                    case TacticalMissionStage.Withdrawn: return "撤退，已结算";
                }
            }

            return "未知阶段(" + stage + ")";
        }

        private static string SuccessionPhaseName(int phase)
        {
            switch ((SuccessionPhase)phase)
            {
                case SuccessionPhase.None: return "无待决";
                case SuccessionPhase.AwaitingSuccessionChoice: return "待选继承人";
                case SuccessionPhase.AwaitingRegentChoice: return "待选摄政";
                case SuccessionPhase.Regency: return "摄政";
                case SuccessionPhase.CrisisDecision: return "危机决策";
                case SuccessionPhase.Extinct: return "家族灭亡";
                default: return "未知阶段(" + phase + ")";
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

        private void WireStaticButtons()
        {
            if (openButton != null && _openListener == null)
            {
                _openListener = Open;
                openButton.onClick.AddListener(_openListener);
                _staticButtonBindings++;
            }
            if (closeButton != null && _closeListener == null)
            {
                _closeListener = Close;
                closeButton.onClick.AddListener(_closeListener);
                _staticButtonBindings++;
            }
        }

        private void UnwireStaticButtons()
        {
            if (_openListener != null)
            {
                if (openButton != null)
                    openButton.onClick.RemoveListener(_openListener);
                _openListener = null;
                _staticButtonBindings--;
            }
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
            if (IsOpen)
                Refresh();
        }

        private void ClearRows()
        {
            for (int i = _rows.Count - 1; i >= 0; i--)
                DestroyRow(_rows[i]);
            _rows.Clear();
            _rowTexts.Clear();
        }

        private static void DestroyRow(GameObject row)
        {
            if (row == null)
                return;
            row.SetActive(false);
            if (UnityEngine.Application.isPlaying)
                Destroy(row);
            else
                DestroyImmediate(row);
        }
    }
}
