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
    /// Visual composition for the M02 "person profile + daily actions" page. It owns only
    /// presentation: every value is read from the live <see cref="GameSession"/> snapshot
    /// supplied by <see cref="DailyActionPanel"/> and every control either drives an
    /// existing M02 command through the panel or switches a local information tab. It never
    /// mutates session state and never invents stats, relationships or rewards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterDailyPageView : MonoBehaviour
    {
        public const int TabProfile = 0;
        public const int TabRelations = 1;
        public const int TabHistory = 2;

        [Header("Sources")]
        [SerializeField] private GameSession gameSession;
        [SerializeField] private DailyActionPanel dailyActionPanel;
        [SerializeField] private NpcAiInspectorPanel inspectorPanel;

        [Header("Header")]
        [SerializeField] private Text headerDateText;
        [SerializeField] private Text headerLocationText;
        [SerializeField] private Text headerMeritText;
        [SerializeField] private Text headerPeopleText;
        [SerializeField] private Text headerFundsText;
        [SerializeField] private Text headerSuppliesText;
        [SerializeField] private Text headerIntelText;

        [Header("Profile")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text profileNameText;
        [SerializeField] private Text profileAgeText;
        [SerializeField] private Text profileIdentityText;
        [SerializeField] private Text profileRoleText;
        [SerializeField] private Text profileLocationText;
        [SerializeField] private Text profileStatusText;
        [SerializeField] private Text creedChipText;
        [SerializeField] private Text ambitionChipText;
        [SerializeField] private Text traitSummaryText;
        [SerializeField] private Text ninjutsuValueText;
        [SerializeField] private Text martialValueText;
        [SerializeField] private Text scholarshipValueText;
        [SerializeField] private Text socialValueText;
        [SerializeField] private Image ninjutsuFill;
        [SerializeField] private Image martialFill;
        [SerializeField] private Image scholarshipFill;
        [SerializeField] private Image socialFill;
        [SerializeField] private GameObject[] relationRoots;
        [SerializeField] private Text[] relationNameTexts;
        [SerializeField] private Text[] relationDetailTexts;
        // Optional truthful empty state; older generated scenes simply omit it.
        [SerializeField] private Text relationsEmptyText;

        [Header("Tabs")]
        [SerializeField] private GameObject profileBody;
        [SerializeField] private GameObject relationsBody;
        [SerializeField] private GameObject historyBody;
        [SerializeField] private Button profileTabButton;
        [SerializeField] private Button relationsTabButton;
        [SerializeField] private Button historyTabButton;
        [SerializeField] private Image profileTabImage;
        [SerializeField] private Image relationsTabImage;
        [SerializeField] private Image historyTabImage;
        [SerializeField] private Text historyText;

        [Header("Daily actions")]
        [SerializeField] private Text actionSubtitleText;
        [SerializeField] private Text progressLabelText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text scheduleText;
        [SerializeField] private Text travelInfoText;
        [SerializeField] private Image travelSceneImage;
        [SerializeField] private Button travelCardButton;
        [SerializeField] private Image restCardImage;
        [SerializeField] private Image trainingCardImage;
        [SerializeField] private Image workCardImage;
        [SerializeField] private Image travelCardImage;

        [Header("Inspector rail")]
        [SerializeField] private Text inspectorSubjectText;
        [SerializeField] private Text inspectorHintText;
        [SerializeField] private Button inspectorOpenButton;

        private int _tab = TabProfile;
        private bool _wired;
        private UnityAction _profileTabListener;
        private UnityAction _relationsTabListener;
        private UnityAction _historyTabListener;
        private UnityAction _inspectorListener;

        public bool IsBound =>
            gameSession != null && dailyActionPanel != null && headerDateText != null &&
            headerLocationText != null && headerMeritText != null && headerPeopleText != null &&
            headerFundsText != null && headerSuppliesText != null && headerIntelText != null &&
            portraitImage != null && profileNameText != null && profileAgeText != null &&
            profileIdentityText != null && profileRoleText != null &&
            profileLocationText != null && profileStatusText != null &&
            creedChipText != null && ambitionChipText != null &&
            traitSummaryText != null && ninjutsuValueText != null &&
            martialValueText != null && scholarshipValueText != null &&
            socialValueText != null && ninjutsuFill != null && martialFill != null &&
            scholarshipFill != null && socialFill != null && relationRoots != null &&
            relationRoots.Length == 3 && relationNameTexts != null &&
            relationNameTexts.Length == 3 && relationDetailTexts != null &&
            relationDetailTexts.Length == 3 && profileBody != null && relationsBody != null &&
            historyBody != null && profileTabButton != null && relationsTabButton != null &&
            historyTabButton != null && profileTabImage != null && relationsTabImage != null &&
            historyTabImage != null && historyText != null && actionSubtitleText != null &&
            progressLabelText != null && progressFill != null && scheduleText != null &&
            travelInfoText != null && travelSceneImage != null && travelCardButton != null &&
            restCardImage != null && trainingCardImage != null && workCardImage != null &&
            travelCardImage != null && inspectorSubjectText != null &&
            inspectorHintText != null && inspectorOpenButton != null;

        public int CurrentTab => _tab;
        public string HeaderDateLine => headerDateText != null ? headerDateText.text : string.Empty;
        public string HeaderLocationLine =>
            headerLocationText != null ? headerLocationText.text : string.Empty;
        public string ProfileNameLine =>
            profileNameText != null ? profileNameText.text : string.Empty;
        public string ProfileStatusLine =>
            profileStatusText != null ? profileStatusText.text : string.Empty;
        public string TraitLine => traitSummaryText != null ? traitSummaryText.text : string.Empty;
        public string StatLine => ninjutsuValueText != null
            ? ninjutsuValueText.text + "/" + martialValueText.text + "/" +
              scholarshipValueText.text + "/" + socialValueText.text
            : string.Empty;
        public string RelationLine =>
            relationNameTexts != null && relationNameTexts.Length > 0 && relationNameTexts[0] != null
                ? relationNameTexts[0].text
                : string.Empty;
        public string ProgressLine =>
            progressLabelText != null ? progressLabelText.text : string.Empty;
        public string ScheduleLine => scheduleText != null ? scheduleText.text : string.Empty;
        public string TravelLine => travelInfoText != null ? travelInfoText.text : string.Empty;
        public string InspectorSubjectLine =>
            inspectorSubjectText != null ? inspectorSubjectText.text : string.Empty;
        public string InspectorHintLine =>
            inspectorHintText != null ? inspectorHintText.text : string.Empty;
        public string HistoryTextLine =>
            historyText != null ? historyText.text : string.Empty;

        /// <summary>Editor/generator seam: the live sources, already assigned references.</summary>
        public void BindSources(GameSession session, DailyActionPanel panel,
            NpcAiInspectorPanel inspector)
        {
            UnwireButtons();
            gameSession = session;
            dailyActionPanel = panel;
            inspectorPanel = inspector;
            WireButtons();
        }

        public void SetTab(int tab)
        {
            _tab = Mathf.Clamp(tab, TabProfile, TabHistory);
            if (profileBody != null) profileBody.SetActive(_tab == TabProfile);
            if (relationsBody != null) relationsBody.SetActive(_tab == TabRelations);
            if (historyBody != null) historyBody.SetActive(_tab == TabHistory);
            PaintTab(profileTabImage, _tab == TabProfile);
            PaintTab(relationsTabImage, _tab == TabRelations);
            PaintTab(historyTabImage, _tab == TabHistory);
        }

        private static void PaintTab(Image image, bool selected)
        {
            if (image == null)
                return;
            image.color = selected ? SengokuUiTheme.Vermilion : SengokuUiTheme.ParchmentDeep;
        }

        /// <summary>
        /// Re-renders every value from the session snapshot. Read-only: no session command
        /// is called from here.
        /// </summary>
        public void Render(GameSession session, PersonId selectedId,
            IReadOnlyList<DailyActionPersonState> persons)
        {
            if (!IsBound)
                return;
            gameSession = session;

            DailyActionPersonState selected = null;
            if (persons != null)
                for (int i = 0; i < persons.Count; i++)
                    if (persons[i].PersonId == selectedId)
                    {
                        selected = persons[i];
                        break;
                    }

            RenderHeader(session, selected);
            RenderProfile(session, selectedId, selected);
            RenderMeasurements(session, selectedId);
            RenderRelations(session, selectedId);
            RenderSchedule(selected);
            RenderTravel(selected);
            RenderInspector(session, selectedId);
            RenderHistory(session, selectedId);
            SetTab(_tab);
        }

        private void RenderHeader(GameSession session, DailyActionPersonState selected)
        {
            GameDate date = session != null && session.WorldClock != null
                ? session.WorldClock.CurrentDate
                : default;
            if (headerDateText != null)
                headerDateText.text = date.Year > 0
                    ? date.Year + " 年 " + date.Month + " 月 " + date.Day + " 日"
                    : "日期未初始化";

            string location = "未知";
            if (selected != null)
                location = selected.IsInTransit && selected.ActiveAction != null
                    ? LocationId.GetChineseName(selected.ActiveAction.Origin) + "→" +
                      LocationId.GetChineseName(selected.ActiveAction.Destination)
                    : selected.HasLocation
                        ? LocationId.GetChineseName(selected.Location)
                        : "行踪不明";
            if (headerLocationText != null)
                headerLocationText.text = "所在　" + location;

            if (headerMeritText != null)
                headerMeritText.text = session != null
                    ? session.Merit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
            if (headerPeopleText != null)
                headerPeopleText.text = session != null && session.FamilyRoster != null
                    ? session.FamilyRoster.Count.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
            IndustryResources resources = default;
            bool hasResources = false;
            if (session != null)
            {
                try
                {
                    resources = session.Industry.Resources;
                    hasResources = true;
                }
                catch (Exception)
                {
                    hasResources = false;
                }
            }
            if (headerFundsText != null)
                headerFundsText.text = hasResources
                    ? resources.Funds.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
            if (headerSuppliesText != null)
                headerSuppliesText.text = hasResources
                    ? resources.Supplies.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
            if (headerIntelText != null)
                headerIntelText.text = hasResources
                    ? resources.Intelligence.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
        }

        private void RenderProfile(GameSession session, PersonId selectedId,
            DailyActionPersonState selected)
        {
            string name = PersonName(session, selectedId);
            if (profileNameText != null)
                profileNameText.text = string.IsNullOrEmpty(name) ? "未选择人物" : name;

            M2RosterRow row = default;
            bool hasRow = TryRosterRow(session, selectedId, out row);
            if (profileAgeText != null)
                profileAgeText.text = hasRow && row.Age > 0
                    ? row.Age.ToString(System.Globalization.CultureInfo.InvariantCulture) + " 岁"
                    : "年龄未记录";
            if (profileIdentityText != null)
                profileIdentityText.text = hasRow
                    ? row.GenerationLabel + " · 族人"
                    : "身份未记录";
            if (profileRoleText != null)
                profileRoleText.text = hasRow ? row.RoleName : "职务未记录";
            if (profileLocationText != null)
                profileLocationText.text = selected == null
                    ? "行踪不明"
                    : selected.IsInTransit && selected.ActiveAction != null
                        ? LocationId.GetChineseName(selected.ActiveAction.Origin) + "→" +
                          LocationId.GetChineseName(selected.ActiveAction.Destination)
                        : selected.HasLocation
                            ? LocationId.GetChineseName(selected.Location)
                            : "行踪不明";
            if (profileStatusText != null)
            {
                // Never say "正常": the page reports the real action state from the session.
                if (selected == null)
                    profileStatusText.text = "未选择";
                else if (selected.ActiveAction == null)
                    profileStatusText.text = "空闲";
                else
                {
                    profileStatusText.text = selected.ActiveAction.KindName + " " +
                        selected.ActiveAction.ElapsedDays.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + "/" +
                        selected.ActiveAction.DurationDays.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) +
                        " 天，剩 " +
                        selected.ActiveAction.RemainingDays.ToString(
                            System.Globalization.CultureInfo.InvariantCulture) + " 天";
                }
            }

            if (creedChipText != null)
                creedChipText.text = session != null
                    ? FamilyDoctrineView.CreedNameOf(session.CurrentFamilyCreed)
                    : "家训未记录";
            FamilyAmbitionRecord ambition = session != null ? session.CurrentHeadAmbition : null;
            if (ambitionChipText != null)
                ambitionChipText.text = ambition != null
                    ? FamilyDoctrineView.AmbitionNameOf(ambition.Kind)
                    : "未立抱负";
            if (traitSummaryText != null)
            {
                var builder = new StringBuilder(140);
                builder.Append("家训「")
                    .Append(session != null
                        ? FamilyDoctrineView.CreedNameOf(session.CurrentFamilyCreed)
                        : "未记录")
                    .Append("」；");
                if (ambition != null)
                    builder.Append("家主抱负「")
                        .Append(FamilyDoctrineView.AmbitionNameOf(ambition.Kind))
                        .Append("」进度 ")
                        .Append(ambition.Progress.ToString(
                            System.Globalization.CultureInfo.InvariantCulture))
                        .Append('/')
                        .Append(ambition.Target.ToString(
                            System.Globalization.CultureInfo.InvariantCulture))
                        .Append("。");
                else
                    builder.Append("家主尚未选定抱负。");
                if (hasRow && !string.IsNullOrWhiteSpace(row.Kinship))
                    builder.Append('\n').Append("亲族：").Append(row.Kinship);
                traitSummaryText.text = builder.ToString();
            }
        }

        private void RenderMeasurements(GameSession session, PersonId selectedId)
        {
            CharacterGrowthValues values = default;
            bool hasValues = session != null && session.Development != null &&
                             session.Development.TryGetValues(selectedId, out values);
            SetStat(ninjutsuValueText, ninjutsuFill, hasValues ? values.Ninjutsu : 0, hasValues);
            SetStat(martialValueText, martialFill, hasValues ? values.MartialArts : 0, hasValues);
            SetStat(scholarshipValueText, scholarshipFill,
                hasValues ? values.Scholarship : 0, hasValues);
            SetStat(socialValueText, socialFill, hasValues ? values.Social : 0, hasValues);
        }

        private static void SetStat(Text value, Image fill, int amount, bool hasValues)
        {
            if (value != null)
                value.text = hasValues
                    ? amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
            if (fill != null)
                fill.fillAmount = hasValues ? Mathf.Clamp01(amount / 100f) : 0f;
        }

        private void RenderRelations(GameSession session, PersonId selectedId)
        {
            IReadOnlyList<M2RosterRow> roster = session != null ? session.FamilyRoster : null;
            RenderRelations(roster, selectedId);
        }

        /// <summary>
        /// Renders the relation area from an explicit roster projection. This is the
        /// single path used by <see cref="Render"/>; it is public so previews/tests can
        /// exercise real 0/1/3-other-member and selection-switch states without
        /// synthesising generated Text objects.
        /// </summary>
        public void RenderRelations(IReadOnlyList<M2RosterRow> roster, PersonId selectedId)
        {
            if (relationRoots == null || relationNameTexts == null ||
                relationDetailTexts == null)
                return;

            bool hasSelection = !selectedId.IsEmpty;
            var others = new List<M2RosterRow>();
            if (hasSelection && roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                    if (roster[i].PersonId != selectedId)
                        others.Add(roster[i]);
                others.Sort((left, right) =>
                {
                    if (left.IsHead != right.IsHead)
                        return left.IsHead ? -1 : 1;
                    return 0;
                });
            }

            // Truthful empty copy: no selection, empty roster and a selected person
            // without other members are distinct states, never fabricated relations.
            string emptyCopy = null;
            if (!hasSelection)
                emptyCopy = roster != null && roster.Count == 0
                    ? "家族名册为空，暂无可展示的关系。"
                    : "尚未选择人物，无法展示关系。";
            else if (others.Count == 0)
                emptyCopy = roster != null && roster.Count == 0
                    ? "家族名册为空，暂无可展示的关系。"
                    : "该人物暂无其他可展示的关系。";

            if (relationsEmptyText != null)
            {
                relationsEmptyText.gameObject.SetActive(emptyCopy != null);
                if (emptyCopy != null)
                    relationsEmptyText.text = emptyCopy;
            }
            for (int i = 0; i < relationRoots.Length; i++)
            {
                bool show = i < others.Count;
                if (relationRoots[i] != null)
                    relationRoots[i].SetActive(show);
                if (!show)
                    continue;
                M2RosterRow row = others[i];
                if (relationNameTexts[i] != null)
                    relationNameTexts[i].text = row.DisplayName;
                if (relationDetailTexts[i] != null)
                    relationDetailTexts[i].text = row.RoleName +
                        (string.IsNullOrWhiteSpace(row.Kinship) ? string.Empty : "\n" + row.Kinship);
            }
        }

        private void RenderSchedule(DailyActionPersonState selected)
        {
            DailyActionTask task = selected != null ? selected.ActiveAction : null;
            string name = profileNameText != null ? profileNameText.text : "人物";
            if (actionSubtitleText != null)
                actionSubtitleText.text = "为 " + name + " 安排本回合的行动计划";

            if (task == null)
            {
                if (progressLabelText != null)
                    progressLabelText.text = "行动天数　尚未安排";
                if (progressFill != null)
                    progressFill.fillAmount = 0f;
                if (scheduleText != null)
                    scheduleText.text = "本回合尚无日程。\n从上方可选行动中加入一项，即可开始计时。";
                SetCardActive(null);
                return;
            }

            if (progressLabelText != null)
                progressLabelText.text = "行动天数　已用 " +
                    task.ElapsedDays.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    " / 共 " +
                    task.DurationDays.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    " 天　剩余 " +
                    task.RemainingDays.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                    " 天";
            if (progressFill != null)
                progressFill.fillAmount = task.DurationDays > 0
                    ? Mathf.Clamp01((float)task.ElapsedDays / task.DurationDays)
                    : 0f;

            var builder = new StringBuilder(160);
            builder.Append("第 1–")
                .Append(task.DurationDays.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" 天　｜　")
                .Append(task.KindName)
                .Append("（")
                .Append(task.DurationDays.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" 天）");
            if (task.IsTravel)
                builder.Append('\n')
                    .Append(LocationId.GetChineseName(task.Origin))
                    .Append('→')
                    .Append(LocationId.GetChineseName(task.Destination))
                    .Append("　｜　行旅中")
                    .Append(task.IsInTransit ? "（尚未抵达）" : string.Empty);
            else
                builder.Append("\n开始于 ").Append(task.StartDate.ToString())
                    .Append("　｜　进行中");
            if (scheduleText != null)
                scheduleText.text = builder.ToString();

            SetCardActive(task.Kind);
        }

        private void SetCardActive(DailyActionKind? activeKind)
        {
            PaintCard(restCardImage, activeKind == DailyActionKind.Rest);
            PaintCard(trainingCardImage, activeKind == DailyActionKind.Training);
            PaintCard(workCardImage, activeKind == DailyActionKind.Work);
            PaintCard(travelCardImage, activeKind == DailyActionKind.Travel);
        }

        private static void PaintCard(Image image, bool active)
        {
            if (image == null)
                return;
            image.color = active
                ? new Color32(0xE6, 0xC6, 0xAF, 0xFF)
                : new Color32(0xF4, 0xEB, 0xD8, 0xFF);
        }

        private void RenderTravel(DailyActionPersonState selected)
        {
            if (travelInfoText == null)
                return;
            if (selected == null || selected.IsInTransit || !selected.HasLocation)
            {
                travelInfoText.text = "旅途中无法再次启程。";
                return;
            }

            var builder = new StringBuilder(150);
            LocationId origin = selected.Location;
            for (int i = 0; i < LocationId.All.Count; i++)
            {
                LocationId destination = LocationId.All[i];
                if (!WorldMapRules.TryGetTravelDays(origin, destination, out int days))
                    continue;
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(LocationId.GetChineseName(origin)).Append(" → ")
                    .Append(LocationId.GetChineseName(destination)).Append("　")
                    .Append(days.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天");
            }
            travelInfoText.text = builder.Length == 0 ? "当前没有可直达的目的地。" : builder.ToString();
        }

        private void RenderInspector(GameSession session, PersonId selectedId)
        {
            string name = profileNameText != null ? profileNameText.text : "未选择";
            bool pilot = NpcAutonomyRules.IsPilot(selectedId);
            if (inspectorSubjectText != null)
                inspectorSubjectText.text = "对象：" + name +
                    (pilot ? "（自治试点）" : "（手动安排）");
            if (inspectorHintText == null)
                return;

            NpcAutonomySnapshot snapshot = null;
            if (pilot && session != null)
            {
                IReadOnlyList<NpcAutonomySnapshot> snapshots = session.NpcAutonomySnapshots;
                if (snapshots != null)
                    for (int i = 0; i < snapshots.Count; i++)
                        if (snapshots[i].PersonId == selectedId)
                        {
                            snapshot = snapshots[i];
                            break;
                        }
            }

            if (snapshot == null)
            {
                inspectorHintText.text = pilot
                    ? "该试点暂无自主状态。\n可用下方按钮打开完整只读调试面板。"
                    : "该人物不在 NPC 自主性试点名单，\n当前日程完全来自左侧的手动安排。\n\n只读调试：本栏不提供排程、\n中断或重新规划入口。";
                return;
            }

            var builder = new StringBuilder(320);
            builder.Append("状态：").Append(snapshot.StatusName).Append('\n');
            if (snapshot.HasActiveAction)
            {
                DailyActionTask task = snapshot.ActiveAction;
                builder.Append("当前行动：").Append(task.KindName)
                    .Append(' ')
                    .Append(task.ElapsedDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append('/')
                    .Append(task.DurationDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天，剩 ")
                    .Append(task.RemainingDays.ToString(
                        System.Globalization.CultureInfo.InvariantCulture))
                    .Append(" 天\n");
            }
            else
            {
                builder.Append("当前行动：无\n");
            }
            builder.Append("需求：").Append(snapshot.Needs.Describe()).Append('\n')
                .Append("偏好：").Append(snapshot.Profile.PreferenceSummary).Append('\n');
            GameDate today = session.WorldClock != null
                ? session.WorldClock.CurrentDate
                : default;
            IReadOnlyList<NpcActionMemory> memories = snapshot.Memories;
            builder.Append("记忆：");
            if (memories == null || memories.Count == 0)
            {
                builder.Append("暂无\n");
            }
            else
            {
                foreach (NpcActionMemory memory in memories)
                {
                    int adjustment = NpcAutonomyRules.MemoryAdjustmentFor(memory,
                        memory.Action, today);
                    builder.Append('\n').Append("- ").Append(memory.KindName).Append(' ')
                        .Append(memory.ActionName).Append(' ')
                        .Append(memory.Date.ToString())
                        .Append("　当前修正 ")
                        .Append(NpcAutonomyRules.Signed(adjustment))
                        .Append(NpcAutonomyRules.IsMemoryActive(memory, today)
                            ? "（生效）"
                            : "（已过期，仅展示）");
                }
                builder.Append('\n');
            }
            NpcAutonomyEvaluation evaluation = snapshot.LatestEvaluation;
            if (evaluation != null && evaluation.HasSelection)
            {
                NpcActionCandidate selected = evaluation.Selected;
                builder.Append("上次选择：").Append(selected.KindName)
                    .Append("　基础分数 ").Append(selected.BaseScore)
                    .Append("　记忆修正 ")
                    .Append(NpcAutonomyRules.Signed(selected.MemoryAdjustment))
                    .Append("　最终分数 ").Append(selected.FinalScore).Append('\n');
            }
            IReadOnlyList<NpcAutonomyDecisionRecord> decisions = snapshot.RecentDecisions;
            if (decisions != null && decisions.Count > 0)
            {
                NpcAutonomyDecisionRecord latest = decisions[decisions.Count - 1];
                builder.Append("最近决策：")
                    .Append(DailyActionState.KindName(latest.Action))
                    .Append('｜').Append(latest.Reason).Append('\n');
            }
            builder.Append("（只读投影，不提供排程操作）");
            inspectorHintText.text = builder.ToString();
        }

        private void RenderHistory(GameSession session, PersonId selectedId)
        {
            if (historyText == null)
                return;
            if (session == null || session.ChronicleEntries == null ||
                session.ChronicleEntries.Count == 0)
            {
                historyText.text = "家族编年史尚无记录。";
                return;
            }

            string name = PersonName(session, selectedId);
            var builder = new StringBuilder(320);
            IReadOnlyList<FamilyChronicleEntry> entries = session.ChronicleEntries;
            int shown = 0;
            for (int i = entries.Count - 1; i >= 0 && shown < 6; i--)
            {
                FamilyChronicleEntry entry = entries[i];
                bool related = string.Equals(entry.SubjectId, selectedId.Value,
                                   StringComparison.Ordinal) ||
                               (!string.IsNullOrEmpty(name) &&
                                ((entry.Title != null && entry.Title.Contains(name)) ||
                                 (entry.Summary != null && entry.Summary.Contains(name))));
                if (!related)
                    continue;
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(entry.Year).Append('年').Append(entry.Month).Append("月　")
                    .Append(entry.Title);
                if (!string.IsNullOrWhiteSpace(entry.Summary))
                    builder.Append("\n　").Append(entry.Summary);
                shown++;
            }
            historyText.text = shown == 0
                ? name + " 尚无编年史记录。"
                : builder.ToString();
        }

        private static string PersonName(GameSession session, PersonId personId)
        {
            if (session != null && session.FamilyRoster != null)
            {
                IReadOnlyList<M2RosterRow> roster = session.FamilyRoster;
                for (int i = 0; i < roster.Count; i++)
                    if (roster[i].PersonId == personId &&
                        !string.IsNullOrWhiteSpace(roster[i].DisplayName))
                        return roster[i].DisplayName;
            }
            return personId.IsEmpty ? "未选择人物" : personId.Value;
        }

        private static bool TryRosterRow(GameSession session, PersonId personId,
            out M2RosterRow row)
        {
            row = default;
            if (session == null || session.FamilyRoster == null)
                return false;
            IReadOnlyList<M2RosterRow> roster = session.FamilyRoster;
            for (int i = 0; i < roster.Count; i++)
                if (roster[i].PersonId == personId)
                {
                    row = roster[i];
                    return true;
                }
            return false;
        }

        private void WireButtons()
        {
            if (_wired)
                return;
            _wired = true;
            if (profileTabButton != null)
            {
                _profileTabListener = () => SetTab(TabProfile);
                profileTabButton.onClick.AddListener(_profileTabListener);
            }
            if (relationsTabButton != null)
            {
                _relationsTabListener = () => SetTab(TabRelations);
                relationsTabButton.onClick.AddListener(_relationsTabListener);
            }
            if (historyTabButton != null)
            {
                _historyTabListener = () => SetTab(TabHistory);
                historyTabButton.onClick.AddListener(_historyTabListener);
            }
            if (inspectorOpenButton != null)
            {
                _inspectorListener = () =>
                {
                    if (inspectorPanel != null)
                    {
                        inspectorPanel.Open();
                        if (inspectorHintText != null)
                            inspectorHintText.text = "调试视图已展开（只读）。";
                    }
                };
                inspectorOpenButton.onClick.AddListener(_inspectorListener);
            }
        }

        private void UnwireButtons()
        {
            if (!_wired)
                return;
            if (profileTabButton != null && _profileTabListener != null)
                profileTabButton.onClick.RemoveListener(_profileTabListener);
            if (relationsTabButton != null && _relationsTabListener != null)
                relationsTabButton.onClick.RemoveListener(_relationsTabListener);
            if (historyTabButton != null && _historyTabListener != null)
                historyTabButton.onClick.RemoveListener(_historyTabListener);
            if (inspectorOpenButton != null && _inspectorListener != null)
                inspectorOpenButton.onClick.RemoveListener(_inspectorListener);
            _profileTabListener = null;
            _relationsTabListener = null;
            _historyTabListener = null;
            _inspectorListener = null;
            _wired = false;
        }

        private void Awake()
        {
            ApplyRuntimeTheme();
            WireButtons();
            SetTab(_tab);
        }

        private void OnEnable()
        {
            WireButtons();
            ApplyRuntimeTheme();
        }

        /// <summary>
        /// Swaps the serialized built-in face for the serif/CJK OS font. Play Mode only:
        /// an editor-mode swap would bake a non-serializable runtime font into the scene.
        /// </summary>
        private void ApplyRuntimeTheme()
        {
            if (!UnityEngine.Application.isPlaying)
                return;
            SengokuUiTheme.ApplyFont(transform);
        }

        private void OnDisable() => UnwireButtons();
    }
}
