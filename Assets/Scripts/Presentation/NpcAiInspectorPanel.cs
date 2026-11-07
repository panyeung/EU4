using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M03 read-only NPC-autonomy Inspector. It projects the two pilots' needs, fixed
    /// preferences, current task, latest candidate evaluation and bounded decision log.
    /// Its only controls select a pilot, open/close the panel, or refresh the projection;
    /// it never calls a scheduling, interruption or replanning API on the session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcAiInspectorPanel : MonoBehaviour
    {
        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button masatsunaButton;
        [SerializeField] private Button nobumasaButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text needsText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text candidatesText;
        [SerializeField] private Text logText;

        private bool _subscribed;
        private UnityAction _openListener;
        private UnityAction _closeListener;
        private UnityAction _masatsunaListener;
        private UnityAction _nobumasaListener;
        private PersonId _selectedPersonId;
        private int _staticButtonBindings;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null &&
            closeButton != null && masatsunaButton != null && nobumasaButton != null &&
            titleText != null && summaryText != null && needsText != null &&
            statusText != null && candidatesText != null && logText != null;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public int StaticButtonBindingCount => _staticButtonBindings;
        public PersonId SelectedPersonId => _selectedPersonId;
        public string TitleLine => titleText != null ? titleText.text : string.Empty;
        public string SummaryLine => summaryText != null ? summaryText.text : string.Empty;
        public string NeedsLine => needsText != null ? needsText.text : string.Empty;
        public string StatusLine => statusText != null ? statusText.text : string.Empty;
        public string CandidatesLine => candidatesText != null ? candidatesText.text : string.Empty;
        public string LogLine => logText != null ? logText.text : string.Empty;

        /// <summary>Plan-facing bind seam; references must already be assigned.</summary>
        public void Bind(GameSession session)
        {
            Unsubscribe();
            gameSession = session;
            Subscribe();
            if (UnityEngine.Application.isPlaying && IsOpen)
                Refresh();
        }

        /// <summary>Editor/test binding seam; wires every serialized reference.</summary>
        public void BindReferences(GameSession session, GameObject root, Button open, Button close,
            Button masatsuna, Button nobumasa, Text title, Text summary, Text needs,
            Text status, Text candidates, Text log)
        {
            Unsubscribe();
            _selectedPersonId = default;
            gameSession = session;
            panelRoot = root;
            openButton = open;
            closeButton = close;
            masatsunaButton = masatsuna;
            nobumasaButton = nobumasa;
            titleText = title;
            summaryText = summary;
            needsText = needs;
            statusText = status;
            candidatesText = candidates;
            logText = log;
            Subscribe();
            if (UnityEngine.Application.isPlaying && IsOpen)
                Refresh();
        }

        public void Open()
        {
            if (panelRoot == null)
                return;
            EnsureSelection();
            panelRoot.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public bool SelectPilot(PersonId personId)
        {
            if (!NpcAutonomyRules.IsPilot(personId))
                return false;
            _selectedPersonId = personId;
            Refresh();
            return true;
        }

        public void SelectFirstPilot()
        {
            IReadOnlyList<NpcAutonomySnapshot> snapshots =
                gameSession != null ? gameSession.NpcAutonomySnapshots : null;
            if (snapshots != null && snapshots.Count > 0)
                _selectedPersonId = snapshots[0].PersonId;
            Refresh();
        }

        /// <summary>Rebuilds only presentation state from read-only session snapshots.</summary>
        public void Refresh()
        {
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            titleText.text = "NPC 自主性（只读）";
            IReadOnlyList<NpcAutonomySnapshot> snapshots = gameSession.NpcAutonomySnapshots;
            EnsureSelection(snapshots);
            summaryText.text = "当前日期：" +
                (gameSession.WorldClock != null
                    ? gameSession.WorldClock.CurrentDate.ToString()
                    : "未初始化") +
                "　试点人物：" +
                snapshots.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                "　本面板不提供排程、中断或重新规划操作。";

            NpcAutonomySnapshot selected = null;
            for (int i = 0; i < snapshots.Count; i++)
                if (snapshots[i].PersonId == _selectedPersonId)
                {
                    selected = snapshots[i];
                    break;
                }

            if (selected == null)
            {
                needsText.text = "尚无登记的自主 NPC。";
                statusText.text = "状态：空闲";
                candidatesText.text = "暂无候选。";
                logText.text = "暂无决策记录。";
                return;
            }

            needsText.text = "需求（0..100）：" + selected.Needs.Describe() + "　｜　" +
                             selected.Profile.PreferenceSummary;
            statusText.text = BuildStatus(selected);
            candidatesText.text = BuildCandidates(selected);
            logText.text = BuildLog(selected);
        }

        private string BuildStatus(NpcAutonomySnapshot snapshot)
        {
            var builder = new StringBuilder(96);
            builder.Append("状态：").Append(snapshot.StatusName);
            if (snapshot.HasActiveAction)
            {
                DailyActionTask task = snapshot.ActiveAction;
                builder.Append("　｜　当前行动：").Append(task.KindName)
                    .Append(' ')
                    .Append(task.ElapsedDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append('/')
                    .Append(task.DurationDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天，剩余 ")
                    .Append(task.RemainingDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天");
            }
            else
            {
                builder.Append("　｜　当前行动：无");
            }
            return builder.ToString();
        }

        private static string BuildCandidates(NpcAutonomySnapshot snapshot)
        {
            NpcAutonomyEvaluation evaluation = snapshot.LatestEvaluation;
            if (evaluation == null)
                return "尚无评估。";
            var builder = new StringBuilder(256);
            builder.Append("最近一次评估：").Append(evaluation.Summary);
            foreach (NpcActionCandidate candidate in evaluation.Candidates)
            {
                builder.Append('\n').Append("- ").Append(candidate.KindName).Append("：");
                if (candidate.IsEligible)
                {
                    builder.Append("分数 ").Append(candidate.Score).Append("（")
                        .Append(candidate.Explanation).Append('）');
                    if (evaluation.HasSelection &&
                        evaluation.Selected.Kind == candidate.Kind)
                        builder.Append("　→ 已选中");
                }
                else
                {
                    builder.Append("排除：").Append(candidate.Explanation);
                }
            }
            return builder.ToString();
        }

        private static string BuildLog(NpcAutonomySnapshot snapshot)
        {
            IReadOnlyList<NpcAutonomyDecisionRecord> records = snapshot.RecentDecisions;
            if (records == null || records.Count == 0)
                return "暂无决策记录。";
            var builder = new StringBuilder(320);
            builder.Append("最近决策（本局内，最多 8 条）：");
            foreach (NpcAutonomyDecisionRecord record in records)
            {
                builder.Append('\n').Append(record.Date.ToString()).Append("　")
                    .Append(record.OutcomeName);
                if (record.HasAction)
                    builder.Append(' ').Append(DailyActionState.KindName(record.Action));
                builder.Append("　触发：").Append(record.TriggerName)
                    .Append("　").Append(record.Reason);
            }
            return builder.ToString();
        }

        private void EnsureSelection()
        {
            EnsureSelection(gameSession != null ? gameSession.NpcAutonomySnapshots : null);
        }

        private void EnsureSelection(IReadOnlyList<NpcAutonomySnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                _selectedPersonId = default;
                return;
            }
            for (int i = 0; i < snapshots.Count; i++)
                if (snapshots[i].PersonId == _selectedPersonId)
                    return;
            _selectedPersonId = snapshots[0].PersonId;
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
            if (masatsunaButton != null && _masatsunaListener == null)
            {
                _masatsunaListener = () => SelectPilot(NpcAutonomyRules.MasatsunaId);
                masatsunaButton.onClick.AddListener(_masatsunaListener);
                _staticButtonBindings++;
            }
            if (nobumasaButton != null && _nobumasaListener == null)
            {
                _nobumasaListener = () => SelectPilot(NpcAutonomyRules.NobumasaId);
                nobumasaButton.onClick.AddListener(_nobumasaListener);
                _staticButtonBindings++;
            }
        }

        private void UnwireStaticButtons()
        {
            if (_openListener != null)
            {
                if (openButton != null) openButton.onClick.RemoveListener(_openListener);
                _openListener = null;
                _staticButtonBindings--;
            }
            if (_closeListener != null)
            {
                if (closeButton != null) closeButton.onClick.RemoveListener(_closeListener);
                _closeListener = null;
                _staticButtonBindings--;
            }
            if (_masatsunaListener != null)
            {
                if (masatsunaButton != null)
                    masatsunaButton.onClick.RemoveListener(_masatsunaListener);
                _masatsunaListener = null;
                _staticButtonBindings--;
            }
            if (_nobumasaListener != null)
            {
                if (nobumasaButton != null)
                    nobumasaButton.onClick.RemoveListener(_nobumasaListener);
                _nobumasaListener = null;
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

        private void OnEnable()
        {
            Subscribe();
            if (UnityEngine.Application.isPlaying && IsOpen)
                Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (IsOpen && panelRoot != gameObject)
                panelRoot.SetActive(false);
        }
    }
}
