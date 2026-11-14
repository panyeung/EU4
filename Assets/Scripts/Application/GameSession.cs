using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Application
{
    /// <summary>Why the most recent time-advance (day, multi-day, or month) request was accepted or refused.</summary>
    public enum MonthTurnOutcome
    {
        Succeeded = 0,
        PendingEvent = 1,
        ContentUnavailable = 2,
        DateOutOfRange = 3,
        SchedulingFailed = 4,
        PendingSuccession = 5,
        MonthSettlementRequired = 6,
        IndustryStateInvalid = 7,
        SettlementFailed = 8
    }

    /// <summary>
    /// 游戏会话 MonoBehaviour：持有 WorldClock 与当前位置，并提供
    /// 旅行预览与确认旅行的最小 API。
    /// 新局固定从 1560-04-01 开始，当前位置固定为清洲。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class GameSession : MonoBehaviour
    {
        /// <summary>Documented fixed sample seed used by the reproducible FoundationScene demo.</summary>
        public const ulong DefaultRandomSeed = 0x5343414E4B4F3031UL;

        public WorldClock WorldClock { get; private set; }
        public LocationId CurrentLocation { get; private set; }
        public SupplyShortageStage SupplyShortageStage => _supplyShortageMission.Stage;
        public SealedLetterStage SealedLetterStage => _sealedLetterMission.Stage;
        public MarketTallyStage MarketTallyStage => _marketTallyMission.Stage;
        public int Merit => _supplyShortageMission.Merit + _sealedLetterMission.Merit + _marketTallyMission.Merit;

        // ---- M05 commission surface -------------------------------------------------
        public ShadowEscortStage CommissionStage => _commission.Stage;
        public CommissionOutcome CommissionOutcome => _commission.Outcome;
        public PersonId CommissionAssignee => _commission.AssigneeId;
        public int CommissionElapsedDays => _commission.ElapsedDays;
        public int CommissionRemainingDays => _commission.Stage == ShadowEscortStage.InProgress
            ? _commission.RemainingDays
            : 0;
        public int CommissionInvestedFunds => _commission.InvestedFunds;

        /// <summary>
        /// Assigned execution window; 0 before an assignee exists, so the UI can tell an
        /// unstarted commission apart from a countdown.
        /// </summary>
        public int CommissionDurationDays => _commission.DurationDaysValue;

        /// <summary>Last M05 commission command or settlement feedback.</summary>
        public string LastCommissionMessage { get; private set; } = string.Empty;

        /// <summary>Immutable read-only commission snapshot for the upcoming panel.</summary>
        public ShadowEscortCommissionStatus CommissionStatus => new ShadowEscortCommissionStatus(
            _commission.Stage, _commission.Outcome, _commission.AssigneeId, CommissionDurationDays,
            _commission.ElapsedDays, CommissionRemainingDays, _commission.InvestedFunds,
            _commission.Score, _commission.Roll, _commission.RewardFunds,
            _commission.RewardSupplies,
            // A settled or abandoned outcome keeps its own readable feedback across
            // save/load; the transient command message only covers the offer, assignment
            // and rejections. Abandonment is terminal without being a shared result, so
            // its reason must survive the same round trip.
            (_commission.IsSettled || _commission.Stage == ShadowEscortStage.Abandoned) &&
            !string.IsNullOrEmpty(_commission.LastFeedback)
                ? _commission.LastFeedback
                : LastCommissionMessage);

        /// <summary>
        /// Read-only assignee candidates in stable family order. Every living current-family
        /// member appears, and each row's eligibility and short reason come from the same
        /// check that gates <see cref="TryAssignCommission"/>, including the shared
        /// work-duty rule (adult, not retired).
        /// </summary>
        public IReadOnlyList<CommissionAssigneeOption> CommissionAssignees
        {
            get
            {
                var options = new List<CommissionAssigneeOption>();
                EnsureSuccession();
                if (_succession.World == null)
                    return options.AsReadOnly();
                foreach (FamilyMembership membership in _succession.World.Family.Members)
                {
                    if (!_succession.World.IsLiving(membership.PersonId))
                        continue;
                    CommissionAssigneeCheck check = CheckCommissionAssignee(membership.PersonId);
                    options.Add(new CommissionAssigneeOption(membership.PersonId,
                        IndustryService.PersonName(_succession, membership.PersonId),
                        check.Eligible, check.Reason));
                }
                return options.AsReadOnly();
            }
        }

        // ---- M06 tactical infiltration surface -------------------------------------

        /// <summary>True while the session-owning tactical operation is running.</summary>
        public bool IsTacticalMissionActive =>
            _tacticalMission != null && _tacticalMission.Stage == TacticalMissionStage.InProgress;

        /// <summary>
        /// The saved family chronicle is the completion authority: any recorded M06
        /// mission row makes the one-time operation unavailable, across save and load.
        /// </summary>
        public bool IsTacticalMissionCompleted => FindTacticalMissionRecord() != null;

        /// <summary>
        /// Read-only projection for the panel; null before the operation starts in this
        /// session. Rebuilding it never advances a turn or moves a guard.
        /// </summary>
        public TacticalMissionSnapshot TacticalMissionStatus =>
            _tacticalMission != null ? _tacticalMission.CreateSnapshot() : null;

        /// <summary>Last M06 command, rejection or settlement feedback.</summary>
        public string LastTacticalMessage { get; private set; } = string.Empty;

        /// <summary>Resources actually applied by the one terminal settlement.</summary>
        public int TacticalAppliedFunds { get; private set; }
        public int TacticalAppliedSupplies { get; private set; }
        public int TacticalAppliedIntelligence { get; private set; }

        /// <summary>
        /// True when two eligible operatives, the clan seat, supplies for the announced
        /// cost and an unfinished operation all allow a fresh start.
        /// </summary>
        public bool CanBeginTacticalMission =>
            !IsTacticalMissionActive && !IsTacticalMissionCompleted &&
            CurrentLocation == LocationId.Kiyosu &&
            CountEligibleTacticalOperatives() >= TacticalMissionScenario.OperativeCount &&
            EnsureIndustryState().Resources.Supplies >= TacticalMissionScenario.SupplyCost;

        /// <summary>
        /// Read-only candidate list in stable family order. Each row's eligibility and
        /// short reason come from the same check that gates <see cref="TryBeginTacticalMission"/>.
        /// </summary>
        public IReadOnlyList<CommissionAssigneeOption> TacticalOperativeOptions
        {
            get
            {
                var options = new List<CommissionAssigneeOption>();
                EnsureSuccession();
                if (_succession.World == null)
                    return options.AsReadOnly();
                foreach (FamilyMembership membership in _succession.World.Family.Members)
                {
                    if (!_succession.World.IsLiving(membership.PersonId))
                        continue;
                    CommissionAssigneeCheck check = CheckTacticalOperative(membership.PersonId);
                    options.Add(new CommissionAssigneeOption(membership.PersonId,
                        IndustryService.PersonName(_succession, membership.PersonId),
                        check.Eligible, check.Reason));
                }
                return options.AsReadOnly();
            }
        }

        /// <summary>Phase 1 演示收尾条件：三项任务均已到达结算（RewardClaimed）阶段。</summary>
        public bool IsDemoComplete =>
            SupplyShortageStage == SupplyShortageStage.RewardClaimed &&
            SealedLetterStage == SealedLetterStage.RewardClaimed &&
            MarketTallyStage == MarketTallyStage.RewardClaimed;

        /// <summary>当前军阶：由派生的总功绩推导，不进入存档。</summary>
        public PromotionRank Rank => PromotionRules.RankFor(Merit);

        /// <summary>当前军阶中文名。</summary>
        public string RankName => PromotionRules.ChineseName(Rank);

        private SealedLetterMission _sealedLetterMission = new SealedLetterMission();
        private MarketTallyMission _marketTallyMission = new MarketTallyMission();
        private SupplyShortageMission _supplyShortageMission = new SupplyShortageMission();
        private ShadowEscortCommission _commission = new ShadowEscortCommission();
        private TacticalMissionState _tacticalMission;
        private CharacterRelationships _relationships;
        private FamilyRelationships _familyRelationships;
        private RandomStateManager _randomStateManager;
        private EventRuntimeState _eventRuntimeState;
        private EventCatalogData _eventCatalog;
        private EventTextCatalog _eventTextCatalog;
        private EventEngine _eventEngine;
        private SuccessionState _succession;
        private IndustryState _industryState;
        private CharacterDevelopmentState _development;
        private FamilyResearchState _researchState;
        private FamilyDoctrineState _familyDoctrine;
        private DailyActionState _dailyActions;
        private NpcAutonomyState _npcAutonomy;
        private int _autonomyInterruptionSuppression;
        private int _dailyActionNotificationSuppression;
        private string _eventScenarioId = string.Empty;
        private HistoryLedger _historyLedger = HistoryLedger.CreateEmpty();
        private readonly Dictionary<Guid, DialogueState> _dialogues = new Dictionary<Guid, DialogueState>();

        public event Action SessionStateChanged;
        public string LastEventMessage { get; private set; } = string.Empty;
        public MonthTurnOutcome LastMonthTurnOutcome { get; private set; } = MonthTurnOutcome.Succeeded;
        public string PendingEventId => _eventRuntimeState != null ? _eventRuntimeState.PendingEventId : string.Empty;
        public bool EventCatalogReady => _eventEngine != null && _eventEngine.IsReady;
        public EventCatalogData EventCatalog => _eventCatalog;
        public EventTextCatalog EventText => _eventTextCatalog;

        /// <summary>Read-only SC-012 record view in append order.</summary>
        public IReadOnlyList<FamilyChronicleEntry> ChronicleEntries =>
            EnsureHistoryLedger().Entries;

        /// <summary>SC-010 succession state (head, pending decision, roster and kinship).</summary>
        public SuccessionState Succession
        {
            get
            {
                EnsureSuccession();
                return _succession;
            }
        }

        /// <summary>Last succession command feedback for the panel and diagnostics.</summary>
        public string LastSuccessionMessage { get; private set; } = string.Empty;

        /// <summary>Read-only SC-019 industry snapshot for the panel and tests.</summary>
        public IndustryView Industry => IndustryView.Build(EnsureIndustryState(), Succession,
            CurrentGameDate, LastIndustryReport, LastIndustryChronicleSummary,
            CanEditIndustryAssignments, IndustryBlockReason, LastIndustryMessage,
            EnsureMarket(), LastMarketReport);

        /// <summary>True only at a management stable point with no pending event or succession decision.</summary>
        public bool CanEditIndustryAssignments
        {
            get
            {
                if (_industryState == null || IsFamilyExtinct || Succession.IsPending ||
                    Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return false;
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return false;
                return IndustryStateIsConsistent;
            }
        }

        /// <summary>Actionable Chinese reason when industry appointments are locked.</summary>
        public string IndustryBlockReason
        {
            get
            {
                if (IsFamilyExtinct)
                    return "任命暂停：家族已经灭亡。";
                if (Succession.IsPending)
                    return "任命暂停：尚有继承决策待处理。";
                if (Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return "任命暂停：幼主已成年，请先完成成年交权。";
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return "任命暂停：尚有事件等待选择。";
                if (_industryState != null && !IndustryStateIsConsistent)
                    return "产业状态与当前月份不一致：请读取稳定存档。";
                return string.Empty;
            }
        }

        /// <summary>Transient report of the most recent month settlement; null after load or a new game.</summary>
        public IndustryMonthReport LastIndustryReport { get; private set; }

        /// <summary>Transient industry feedback, including safe-save cleanup.</summary>
        public string LastIndustryMessage { get; private set; } = string.Empty;

        /// <summary>Read-only child-growth snapshot for the education panel and tests.</summary>
        public EducationView Education => EducationView.Build(Succession, EnsureDevelopment(),
            CurrentGameDate, CanEditEducationPlans, EducationBlockReason, LastEducationMessage);

        /// <summary>Stable growth state keyed by permanent person ID.</summary>
        public CharacterDevelopmentState Development => EnsureDevelopment();

        /// <summary>True when the child-education phase is still open for the displayed learner.</summary>
        public bool CanEditEducationPlans
        {
            get
            {
                if (_development == null || IsFamilyExtinct || Succession.IsPending ||
                    Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return false;
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return false;
                return HasEligibleEducationLearner();
            }
        }

        /// <summary>Actionable Chinese reason when education plans are locked.</summary>
        public string EducationBlockReason
        {
            get
            {
                if (IsFamilyExtinct)
                    return "培养暂停：家族已经灭亡。";
                if (Succession.IsPending)
                    return "培养暂停：尚有继承决策待处理。";
                if (Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return "培养暂停：幼主已成年，请先完成成年交权。";
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return "培养暂停：尚有事件等待选择。";
                if (_development == null)
                    return "培养暂停：缺少人物成长记录。";
                if (!HasEligibleEducationLearner())
                    return "培养暂停：没有 0..15 岁的未成年家人，儿童年度培养已结束。";
                return string.Empty;
            }
        }

        /// <summary>Transient summary of the most recent annual education settlement.</summary>
        public string LastEducationMessage { get; private set; } = string.Empty;

        /// <summary>Read-only SC-021 research snapshot for the panel and tests.</summary>
        public FamilyResearchView Research => FamilyResearchView.Build(EnsureResearch(), Succession,
            EnsureIndustryState(), CurrentGameDate, CanEditResearch, ResearchBlockReason,
            LastResearchReport, LastResearchMessage);

        /// <summary>Stable research state keyed by permanent person and technique IDs.</summary>
        public FamilyResearchState ResearchState => EnsureResearch();

        /// <summary>True only at a management stable point; the project itself may pause later.</summary>
        public bool CanEditResearch
        {
            get
            {
                if (_researchState == null || IsFamilyExtinct || Succession.IsPending ||
                    Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return false;
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return false;
                return true;
            }
        }

        /// <summary>Actionable Chinese reason when research plans are locked.</summary>
        public string ResearchBlockReason
        {
            get
            {
                if (IsFamilyExtinct)
                    return "研究暂停：家族已经灭亡。";
                if (Succession.IsPending)
                    return "研究暂停：尚有继承决策待处理。";
                if (Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return "研究暂停：幼主已成年，请先完成成年交权。";
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return "研究暂停：尚有事件等待选择。";
                if (_researchState == null)
                    return "研究暂停：缺少家族研究记录。";
                return string.Empty;
            }
        }

        /// <summary>Transient report of the most recent monthly research settlement.</summary>
        public FamilyResearchMonthReport LastResearchReport { get; private set; }

        /// <summary>Transient research feedback, including pause reasons.</summary>
        public string LastResearchMessage { get; private set; } = string.Empty;

        /// <summary>Persistent SC-024 family creed; survives succession unchanged.</summary>
        public FamilyCreedKind CurrentFamilyCreed => EnsureFamilyDoctrine().Creed;

        /// <summary>Current head's ambition record, or null when this head has not chosen one.</summary>
        public FamilyAmbitionRecord CurrentHeadAmbition
        {
            get
            {
                EnsureSuccession();
                return EnsureFamilyDoctrine().TryGetAmbition(Succession.HeadPersonId,
                    out FamilyAmbitionRecord record)
                    ? record
                    : null;
            }
        }

        /// <summary>Reads one head's retained ambition record without exposing mutable state.</summary>
        public bool TryGetFamilyAmbition(PersonId headId, out FamilyAmbitionRecord record) =>
            EnsureFamilyDoctrine().TryGetAmbition(headId, out record);

        /// <summary>Last SC-024 family-doctrine command feedback.</summary>
        public string LastFamilyDoctrineMessage { get; private set; } = string.Empty;

        /// <summary>Read-only SC-022 family-formation snapshot for the panel and tests.</summary>
        public FamilyFormationView FamilyFormation => FamilyFormationView.Build(Succession,
            CurrentGameDate, CanEditFamilyFormation, FamilyFormationBlockReason,
            LastFamilyFormationMessage);

        /// <summary>Explicit projection accessor used by the SC-022 panel and tests.</summary>
        public FamilyFormationView GetFamilyFormationView() => FamilyFormation;

        /// <summary>True only at a management stable point inside the SC-022 scenario.</summary>
        public bool CanEditFamilyFormation
        {
            get
            {
                if (!SC022FamilyFormationScenario.IsActive(Succession))
                    return false;
                if (IsFamilyExtinct || Succession.IsPending ||
                    Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return false;
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return false;
                return true;
            }
        }

        /// <summary>Actionable Chinese reason when marriage/adoption commands are locked.</summary>
        public string FamilyFormationBlockReason
        {
            get
            {
                if (IsFamilyExtinct)
                    return "家族管理暂停：家族已经灭亡。";
                if (!SC022FamilyFormationScenario.IsActive(Succession))
                    return "家族管理暂停：当前不是 SC-022 婚姻与收养示范。";
                if (Succession.IsPending)
                    return "家族管理暂停：尚有继承决策待处理。";
                if (Succession.IsRegencyHandoffDue(CurrentGameDate))
                    return "家族管理暂停：幼主已成年，请先完成成年交权。";
                if (_eventRuntimeState != null && _eventRuntimeState.HasPendingEvents)
                    return "家族管理暂停：尚有事件等待选择。";
                return string.Empty;
            }
        }

        /// <summary>Transient SC-022 command feedback for the panel.</summary>
        public string LastFamilyFormationMessage { get; private set; } = string.Empty;

        /// <summary>Read-only eligible researcher options; stable roster order.</summary>
        public IReadOnlyList<FamilyResearchResearcherOption> ResearchResearchers
        {
            get
            {
                var options = new List<FamilyResearchResearcherOption>();
                EnsureSuccession();
                EnsureIndustryState();
                if (Succession.World == null)
                    return options.AsReadOnly();
                foreach (FamilyMembership membership in Succession.World.Family.Members)
                {
                    if (!FamilyResearchSettlement.IsEligibleResearcher(Succession,
                            membership.PersonId, CurrentGameDate, _industryState, out _))
                        continue;
                    options.Add(new FamilyResearchResearcherOption(membership.PersonId,
                        IndustryService.PersonName(Succession, membership.PersonId)));
                }
                return options.AsReadOnly();
            }
        }

        /// <summary>Read-only mentor options for the current learner; stable roster order.</summary>
        public IReadOnlyList<EducationMentorOption> EducationMentors
        {
            get
            {
                var options = new List<EducationMentorOption>();
                EnsureSuccession();
                EnsureDevelopment();
                PersonId learner = Education.HasLearner ? Education.LearnerId : default;
                foreach (FamilyMembership membership in Succession.World.Family.Members)
                {
                    if (!IsEligibleEducationMentor(membership.PersonId, learner, CurrentGameDate))
                        continue;
                    string name = Succession.World.TryGetPerson(membership.PersonId, out PersonState person)
                        ? person.DisplayName
                        : membership.PersonId.Value;
                    options.Add(new EducationMentorOption(membership.PersonId, name,
                        EducationScenarioProvider.SpecialtyOf(Succession, membership.PersonId),
                        EducationScenarioProvider.MentorRoleLabel(Succession, membership.PersonId)));
                }
                return options.AsReadOnly();
            }
        }

        /// <summary>
        /// Test-only fault injection executed after the clock crosses into the next
        /// month but before the settlement transaction commits. Any exception proves
        /// that every snapshot (date, resources, lastSettled, chronicle, RNG, runtime)
        /// is restored atomically. Never set by production code.
        /// </summary>
        internal Action MonthSettlementFaultInjection { get; set; }

        /// <summary>
        /// Test-only hook invoked after a family-formation command has added the new
        /// member and kinship fact but before the prepared chronicle entry is appended.
        /// Tests use it to advance the ledger so the real append fails, proving the
        /// command rolls membership and kinship back. Never set by production code.
        /// </summary>
        internal Action<HistoryLedger> FamilyFormationAppendFaultInjection { get; set; }

        /// <summary>
        /// Test-only hook invoked after the SC-024 council entries are prepared but
        /// before they are appended. Tests advance the ledger here, exactly like the
        /// family-formation seam, so the prepared append genuinely fails and proves the
        /// creed, relationships, queue and ledger all roll back together. Never set by
        /// production code.
        /// </summary>
        internal Action<HistoryLedger> Sc024ChronicleAppendFaultInjection { get; set; }

        /// <summary>
        /// Test-only hook invoked after the M06 terminal result is prepared but before it
        /// is appended. Tests advance the ledger here so the prepared append genuinely
        /// fails, proving resources roll back and the operation stays retryable. Never
        /// set by production code.
        /// </summary>
        internal Action<HistoryLedger> TacticalChronicleAppendFaultInjection { get; set; }

        /// <summary>True only when one more day stays inside the current month and settlement is consistent.</summary>
        public bool CanAdvanceOneDayWithinMonth
        {
            get
            {
                if (WorldClock == null)
                    return false;
                if (_industryState == null || !IndustryStateIsConsistent)
                    return false;
                return GameDate.TryNext(CurrentGameDate, out GameDate next) &&
                       next.Year == CurrentGameDate.Year && next.Month == CurrentGameDate.Month;
            }
        }

        /// <summary>Last visible travel refusal reason; cleared by a successful preview/confirm.</summary>
        public string LastTravelBlockReason { get; private set; } = string.Empty;

        /// <summary>Stable ordinal snapshot of every person's daily action/location state.</summary>
        public IReadOnlyList<DailyActionPersonState> DailyActions => EnsureDailyActions().Persons;

        /// <summary>Outcomes produced by the most recent successful day/month advance or command.</summary>
        public IReadOnlyList<DailyActionResult> LastDailyActionOutcomes { get; private set; } =
            new DailyActionResult[0];

        /// <summary>Readable feedback for the most recent daily-action command or completion.</summary>
        public string LastDailyActionMessage { get; private set; } = string.Empty;

        /// <summary>True while one action is running for this person.</summary>
        public bool HasActiveDailyAction(PersonId personId) =>
            EnsureDailyActions().HasActiveAction(personId);

        /// <summary>Reads one person's in-flight action without exposing writable state.</summary>
        public bool TryGetDailyAction(PersonId personId, out DailyActionTask task) =>
            EnsureDailyActions().TryGetActive(personId, out task);

        /// <summary>True when the person is between the travel endpoints, at neither one.</summary>
        public bool IsPersonInTransit(PersonId personId) =>
            EnsureDailyActions().TryGetActive(personId, out DailyActionTask task) &&
            task.IsInTransit;

        /// <summary>
        /// Reads a usable person location. False means in transit or no trustworthy
        /// location, so callers cannot treat a traveller as standing at either endpoint.
        /// </summary>
        public bool TryGetPersonLocation(PersonId personId, out LocationId location) =>
            EnsureDailyActions().TryGetLocation(personId, out location);

        /// <summary>Session-only newest-first decision log for the two pilots.</summary>
        public IReadOnlyList<NpcAutonomyDecisionRecord> NpcAutonomyDecisions =>
            _npcAutonomy != null
                ? _npcAutonomy.Decisions
                : (IReadOnlyList<NpcAutonomyDecisionRecord>)Array.Empty<NpcAutonomyDecisionRecord>();

        /// <summary>Read-only snapshots in stable ordinal pilot order; never mutates state.</summary>
        public IReadOnlyList<NpcAutonomySnapshot> NpcAutonomySnapshots
        {
            get
            {
                var snapshots = new List<NpcAutonomySnapshot>(2);
                foreach (NpcAutonomyProfile profile in NpcAutonomyRules.Profiles)
                {
                    if (TryGetNpcAutonomy(profile.PersonId, out NpcAutonomySnapshot snapshot))
                        snapshots.Add(snapshot);
                }
                return snapshots.AsReadOnly();
            }
        }

        /// <summary>
        /// Read-only projection for the Inspector. It never schedules, interrupts,
        /// replans, reconciles or creates autonomy state; lifecycle commands own those
        /// mutations. A missing session state therefore yields no snapshot instead of
        /// scheduling a side effect during a read.
        /// </summary>
        public bool TryGetNpcAutonomy(PersonId personId, out NpcAutonomySnapshot snapshot)
        {
            snapshot = null;
            if (!NpcAutonomyRules.IsPilot(personId) || _npcAutonomy == null)
                return false;
            if (!_npcAutonomy.TryGetPerson(personId, out NpcAutonomyPersonState person))
                return false;

            NpcAutonomyRules.TryGetProfile(personId, out NpcAutonomyProfile profile);
            DailyActionTask task = null;
            if (_dailyActions != null)
                _dailyActions.TryGetActive(personId, out task);
            bool hasActiveAction = task != null;
            bool hasKnownLocation = _dailyActions != null &&
                _dailyActions.TryGetLocation(personId, out _);
            NpcAutonomyEvaluation current = NpcAutonomyRules.Evaluate(personId, person.Needs,
                person.Memories, CurrentGameDate, true, hasKnownLocation, hasActiveAction,
                NpcEligibilityFor(personId));
            NpcAutonomyEvaluation latest = person.LatestEvaluation ?? current;
            string displayName = profile.DisplayName;
            if (_succession != null && _succession.World != null &&
                _succession.World.TryGetPerson(personId, out PersonState personState) &&
                !string.IsNullOrWhiteSpace(personState.DisplayName))
                displayName = personState.DisplayName;
            snapshot = new NpcAutonomySnapshot(personId, displayName, person.Needs, profile,
                person.Status, task, latest, current, person.LastOutcome, person.HasLastOutcome,
                person.Memories, _npcAutonomy.RecentDecisionsFor(personId, 8));
            return true;
        }

        private bool IndustryStateIsConsistent =>
            _industryState != null &&
            _industryState.LastSettledMonthIndex == IndustryCalendar.MonthIndex(CurrentGameDate) - 1;

        /// <summary>
        /// True once the family has no lawful continuation and the player declined the
        /// crisis decision. This is a terminal gameplay state: time and travel stop and
        /// only loading an earlier save can resume play.
        /// </summary>
        public bool IsFamilyExtinct => Succession.Phase == SuccessionPhase.Extinct;

        /// <summary>
        /// True when an active succession decision, a due adult handoff or extinction
        /// currently owns the world clock and therefore also blocks travel.
        /// </summary>
        public bool IsTravelBlockedBySuccession => SuccessionBlocksTimeAdvance;

        /// <summary>
        /// Interaction gate for the crisis-demo entry: true only while the session still
        /// holds the pristine pre-succession standard roster, so the fixed demo never
        /// replaces completed family history. Reference validity is checked by
        /// <see cref="StartCrisisDemo"/> instead of here, so the entry stays clickable and
        /// can explain why a swap would orphan an injury or a terminal-history row.
        /// </summary>
        public bool CanStartCrisisDemo => IsPristineStandardSuccession(Succession);

        /// <summary>
        /// Reason the crisis roster swap would orphan an M07 person reference, or null
        /// when every injured person and every terminal-history participant survives the
        /// replacement. The swap is refused rather than deleting an injury or rewriting
        /// terminal history, so the player keeps the record and can retry later. The
        /// replacement is derived from the same factory the swap installs, so the check
        /// cannot drift from the roster it installs.
        /// </summary>
        private string CrisisRosterOrphanReason()
        {
            SuccessionState crisis = SuccessionSample.CreateCrisis();
            IReadOnlyList<PersonId> injured = EnsureInjuries().Injured;
            for (int i = 0; i < injured.Count; i++)
                if (!CrisisRosterKeeps(crisis, injured[i]))
                    return "伤员 " + PersonNameOf(injured[i]) +
                           " 不在危机示范名单中：切换会丢失伤势记录，请先在下一次月结后恢复，再进入危机示范。";
            IReadOnlyList<MissionRecord> records = EnsureMissionHistory().Records;
            for (int i = 0; i < records.Count; i++)
            {
                IReadOnlyList<PersonId> participants = records[i].Participants;
                for (int p = 0; p < participants.Count; p++)
                    if (!CrisisRosterKeeps(crisis, participants[p]))
                        return "任务终局历史包含危机示范名单之外的人物 " +
                               PersonNameOf(participants[p]) +
                               "：切换会孤立终局记录，无法进入危机示范。";
            }
            return null;
        }

        private static bool CrisisRosterKeeps(SuccessionState crisis, PersonId personId)
        {
            foreach (PersonState person in crisis.World.Persons)
                if (person.Id == personId)
                    return true;
            return false;
        }

        /// <summary>True while the session owns the approved SC-013 fixed three-generation roster.</summary>
        public bool IsM2ScenarioActive => M2FixedScenario.IsActive(Succession);

        /// <summary>True after the third-generation finale choice has been committed.</summary>
        public bool IsM2FinaleCommitted =>
            M2FixedScenario.IsActive(Succession) && M2FixedScenario.IsFinaleCommitted(EnsureEventRuntimeState());

        /// <summary>True when the M2-specific retirement gate allows the current handoff.</summary>
        public bool CanBeginSuccessionRetirement
        {
            get
            {
                EnsureSuccession();
                if (IsFamilyExtinct || _succession.Phase != SuccessionPhase.None)
                    return false;
                if (HasPendingEventForSuccessionGate())
                    return false;
                if (!M2FixedScenario.IsActive(_succession))
                    return true;
                return M2FixedScenario.CanBeginRetirement(_succession, EnsureEventRuntimeState(),
                    CurrentGameDate, _eventScenarioId, out _);
            }
        }

        /// <summary>
        /// A queued event must be chosen before any command can hand the head to a new person.
        /// The event panel resolves its options against the current head, exactly like the
        /// month turn that refuses to advance while an event is pending; confirming first would
        /// leave a family scene addressed to the outgoing head with no selectable option, and
        /// the player could neither choose nor advance. The gate changes only the order of two
        /// player decisions: it never retargets an effect and never changes a succession rule.
        /// </summary>
        private bool RejectSuccessionWhileEventPending(out SuccessionResult rejection)
        {
            rejection = default;
            if (!HasPendingEventForSuccessionGate())
                return false;
            const string message = "尚有事件待处理：请先作出事件选择，再处理继承交接。";
            LastSuccessionMessage = message;
            SessionStateChanged?.Invoke();
            rejection = new SuccessionResult(SuccessionOutcome.BlockedByPendingEvent, false, message,
                _succession != null ? _succession.Phase : SuccessionPhase.None,
                _succession != null ? _succession.HeadPersonId : default);
            return true;
        }

        private bool HasPendingEventForSuccessionGate()
        {
            EnsureEventRuntimeState();
            return _eventRuntimeState != null && _eventRuntimeState.HasPendingEvents;
        }

        /// <summary>Visible reason when <see cref="CanBeginSuccessionRetirement"/> is false.</summary>
        public string SuccessionRetirementBlockReason
        {
            get
            {
                EnsureSuccession();
                if (IsFamilyExtinct)
                    return "家族已经灭亡，无法继续交接。";
                if (_succession.Phase != SuccessionPhase.None)
                    return "尚有继承决策待处理，请先完成当前选择。";
                if (HasPendingEventForSuccessionGate())
                    return "尚有事件待处理：请先作出事件选择，再处理继承交接。";
                if (!M2FixedScenario.IsActive(_succession))
                    return "当前可按通常继承规则安排交接。";
                M2FixedScenario.CanBeginRetirement(_succession, EnsureEventRuntimeState(),
                    CurrentGameDate, _eventScenarioId, out string reason);
                return reason;
            }
        }

        public string SuccessionHeadName =>
            Succession.TryGetHead(out PersonState head) ? head.DisplayName : string.Empty;

        /// <summary>Visible fixed-heir line for the M2 succession HUD; empty in SC-010 modes.</summary>
        public string FixedHeirSummary
        {
            get
            {
                if (!M2FixedScenario.IsActive(Succession))
                    return string.Empty;
                PersonId heir = M2FixedScenario.ExpectedHeir(Succession, _eventScenarioId);
                if (heir.IsEmpty)
                    return IsM2FinaleCommitted
                        ? "固定继承人：无（第三代终局已完成）"
                        : "固定继承人：无（当前已是最后一代）";
                string name = Succession.World.TryGetPerson(heir, out PersonState person)
                    ? person.DisplayName
                    : heir.Value;
                bool hasLife = Succession.World.TryGetLife(heir, out SuccessionPersonLife life);
                int age = hasLife ? life.AgeAt(CurrentGameDate) : 0;
                string validity = hasLife && life.Alive && !life.Retired ? string.Empty : "（已失效）";
                return "固定继承人：" + name + "（" + age + "岁）" + validity;
            }
        }

        /// <summary>Derived third-generation ending; no separate terminal save field.</summary>
        public M2EndingView M2Ending
        {
            get
            {
                EnsureEventRuntimeState();
                int iori = TryGetRelationship(CharacterId.KurodaIori).CurrentValue;
                M2FixedScenario.TryBuildEnding(Succession, _eventRuntimeState, iori, out M2EndingView ending);
                return ending;
            }
        }

        public IReadOnlyList<SuccessionCandidate> SuccessionCandidates
        {
            get
            {
                IReadOnlyList<SuccessionCandidate> candidates =
                    SuccessionService.GetCandidates(Succession, CurrentGameDate);
                if (!M2FixedScenario.IsActive(Succession) || IsM2FinaleCommitted)
                    return candidates;

                // The fixed line only narrows a real choice. A dead or unavailable
                // fixed heir, or a last-generation head with no fixed heir, must leave
                // the ordinary eligible living candidates instead of an empty list that
                // the death/crisis decision cannot resolve.
                PersonId heir = AvailableFixedHeir();
                if (heir.IsEmpty)
                    return candidates;

                var filtered = new List<SuccessionCandidate>(1);
                foreach (SuccessionCandidate candidate in candidates)
                    if (candidate.PersonId == heir)
                        filtered.Add(candidate);
                return filtered.AsReadOnly();
            }
        }

        /// <summary>
        /// The SC-013 fixed heir while that person is still a normal eligible candidate.
        /// An empty result means the fixed line cannot narrow the current choice, so the
        /// callers use the ordinary eligible living list.
        /// </summary>
        private PersonId AvailableFixedHeir()
        {
            if (!M2FixedScenario.IsActive(Succession))
                return default;
            PersonId heir = M2FixedScenario.ExpectedHeir(Succession, _eventScenarioId);
            if (heir.IsEmpty)
                return default;
            IReadOnlyList<SuccessionCandidate> candidates =
                SuccessionService.GetCandidates(Succession, CurrentGameDate);
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].PersonId == heir)
                    return heir;
            return default;
        }

        public IReadOnlyList<SuccessionCandidate> SuccessionRegentCandidates =>
            SuccessionService.GetRegentCandidates(Succession, CurrentGameDate);

        /// <summary>Read-only family roster rows for the SC-013 family panel.</summary>
        public IReadOnlyList<M2RosterRow> FamilyRoster =>
            M2FixedScenario.BuildRoster(Succession, CurrentGameDate, Succession.DesignatedHeirId,
                _eventScenarioId);

        /// <summary>
        /// Sorted read-only active pregnancy snapshot (M04B). Live records are owned by
        /// <see cref="SuccessionWorld.Pregnancies"/> and change only at month settlement.
        /// </summary>
        public IReadOnlyList<FamilyPregnancyRecord> FamilyPregnancies =>
            Succession.World.Pregnancies.Records;

        private GameDate CurrentGameDate =>
            WorldClock != null ? WorldClock.CurrentDate : SengokuChronicle.Domain.WorldClock.NewGameDate;

        public RandomStateManager RandomState
        {
            get
            {
                EnsureRandomState();
                return _randomStateManager;
            }
        }

        public EventRuntimeSaveData EventRuntimeStateForSave =>
            _eventRuntimeState != null ? _eventRuntimeState.Capture() : new EventRuntimeSaveData();

        private sealed class DialogueState
        {
            public CharacterId CharacterId;
            public DialogueDefinition Definition;
            public string CurrentNodeId;
            public bool Ended;
        }


        public DialogueStartResult TryStartDialogue(CharacterId characterId)
        {
            if (!BranchingDialogueCatalog.TryGet(characterId, out DialogueDefinition definition) || !definition.TryGetNode(definition.StartNodeId, out _))
                return new DialogueStartResult(Guid.Empty, string.Empty, false, "No dialogue is configured for this character.");
            Guid id = Guid.NewGuid();
            _dialogues.Add(id, new DialogueState { CharacterId = characterId, Definition = definition, CurrentNodeId = definition.StartNodeId });
            return new DialogueStartResult(id, definition.StartNodeId, true, string.Empty);
        }

        public DialogueSelectionResult TrySelectDialogueOption(Guid dialogueInstanceId, string nodeId, DialogueOptionId optionId)
        {
            if (!_dialogues.TryGetValue(dialogueInstanceId, out DialogueState state))
                return new DialogueSelectionResult(string.Empty, false, false, "Dialogue instance is invalid.", false, default);
            if (state.Ended || !string.Equals(state.CurrentNodeId, nodeId, StringComparison.Ordinal) || !state.Definition.TryGetNode(nodeId, out DialogueNode node))
                return new DialogueSelectionResult(state.CurrentNodeId, state.Ended, false, "Dialogue node is invalid or no longer current.", false, default);
            DialogueOption selected = null;
            foreach (DialogueOption option in node.Options) if (option != null && option.Id == optionId) { selected = option; break; }
            if (selected == null || !state.Definition.TryGetNode(selected.TargetNodeId, out DialogueNode target))
                return new DialogueSelectionResult(state.CurrentNodeId, false, false, "Dialogue option or target is invalid.", false, default);
            bool hasEffect = !selected.EffectTarget.IsEmpty && selected.RelationshipDelta != 0;
            RelationshipResult relationship = default;
            if (hasEffect)
            {
                EnsureRelationships();
                relationship = _relationships.TryGet(selected.EffectTarget);
                if (!relationship.Succeeded) return new DialogueSelectionResult(state.CurrentNodeId, false, false, relationship.Error, false, relationship);
            }
            state.CurrentNodeId = target.Id;
            state.Ended = target.Options.Count == 0;
            if (hasEffect)
            {
                relationship = _relationships.TryChange(selected.EffectTarget, selected.RelationshipDelta);
                if (RecordRelationshipChange(relationship, "dialogue"))
                    SessionStateChanged?.Invoke();
            }
            return new DialogueSelectionResult(state.CurrentNodeId, state.Ended, true, string.Empty, hasEffect && relationship.Succeeded, relationship);
        }

        private void Awake()
        {
            if (WorldClock == null)
                InitNewGame();
        }

        /// <summary>以新局状态重置/初始化时钟（1560-04-01）与位置（清洲）。</summary>
        public void InitNewGame()
        {
            InitNewGame(DefaultRandomSeed);
        }

        /// <summary>Starts the default SC-013 fixed three-generation scenario.</summary>
        public void InitNewGame(ulong initialSeed) =>
            InitSession(initialSeed, M2FixedScenario.CreateSuccession(), WorldClock.NewGameDate,
                null, null, null, CampaignOriginCatalog.Sc013OriginId);

        /// <summary>Starts the separate SC-010 standard sample used by regression demos.</summary>
        public void InitStandardDemo() => InitStandardDemo(DefaultRandomSeed);

        /// <summary>Starts the separate SC-010 standard sample with a caller-selected seed.</summary>
        public void InitStandardDemo(ulong initialSeed) =>
            InitSession(initialSeed, SuccessionSample.CreateStandard(), WorldClock.NewGameDate,
                null, null, null, CampaignOriginCatalog.Sc010OriginId);

        /// <summary>Starts the separate fixed SC-020 child-education demonstration.</summary>
        public void InitEducationDemoScenario() => InitEducationDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-020 demonstration with a caller-selected deterministic seed.</summary>
        public void InitEducationDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC020EducationScenario.CreateSuccession(),
                SC020EducationScenario.StartDate, SC020EducationScenario.CreateDevelopmentState(),
                null, null, CampaignOriginCatalog.Sc020OriginId);
        }

        /// <summary>Starts the separate fixed SC-023 seclusion and mentorship demonstration.</summary>
        public void InitSeclusionDemoScenario() => InitSeclusionDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-023 demonstration with a caller-selected deterministic seed.</summary>
        public void InitSeclusionDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC023SeclusionScenario.CreateSuccession(),
                SC023SeclusionScenario.StartDate, SC023SeclusionScenario.CreateDevelopmentState(),
                null, null, CampaignOriginCatalog.Sc023OriginId);
        }

        /// <summary>Starts the separate fixed SC-021 family-research demonstration.</summary>
        public void InitResearchDemoScenario() => InitResearchDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-021 demonstration with a caller-selected deterministic seed.</summary>
        public void InitResearchDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC021FamilyResearchScenario.CreateSuccession(),
                SC021FamilyResearchScenario.StartDate, null,
                SC021FamilyResearchScenario.CreateIndustryState(), null,
                CampaignOriginCatalog.Sc021OriginId);
        }

        /// <summary>Starts the separate fixed SC-022 marriage and adoption demonstration.</summary>
        public void InitFamilyFormationDemoScenario() =>
            InitFamilyFormationDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-022 demonstration with a caller-selected deterministic seed.</summary>
        public void InitFamilyFormationDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC022FamilyFormationScenario.CreateSuccession(),
                SC022FamilyFormationScenario.StartDate, null, null, null,
                CampaignOriginCatalog.Sc022OriginId);
        }

        /// <summary>Starts the separate fixed SC-024 family-creed demonstration.</summary>
        public void InitFamilyDoctrineDemoScenario() => InitFamilyDoctrineDemoScenario(DefaultRandomSeed);

        /// <summary>
        /// Starts the SC-024 demonstration with a caller-selected deterministic seed. It
        /// reuses the SC-013 six-person roster, adds the SC-024-only fourth-generation
        /// child, and tags the event route as SC-024 so the creed council rows are
        /// eligible only in this explicit demonstration.
        /// </summary>
        public void InitFamilyDoctrineDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC024FamilyDoctrineScenario.CreateSuccession(),
                SC024FamilyDoctrineScenario.StartDate, null, null,
                SC024FamilyDoctrineScenario.ScenarioId, CampaignOriginCatalog.Sc024OriginId);
            EnsureFamilyDoctrine().TrySetCreed(FamilyCreedKind.RighteousnessFirst, out _);
            TryStartCurrentHeadAmbition(FamilyAmbitionKind.DevelopHousehold);
        }

        private void InitSession(ulong initialSeed, SuccessionState succession, GameDate startDate,
            CharacterDevelopmentState development, IndustryState industry, string eventScenarioId,
            string campaignOriginId)
        {
            WorldClock = new WorldClock(startDate);
            _campaignProgress = CreateCampaignProgress(startDate, campaignOriginId);
            CurrentLocation = LocationId.Kiyosu;
            _relationships = new CharacterRelationships();
            _familyRelationships = FamilyRelationships.CreateEmpty();
            _randomStateManager = RandomStateManager.Create(initialSeed);
            _eventRuntimeState = new EventRuntimeState();
            LoadEventContent();
            _supplyShortageMission = new SupplyShortageMission();
            _sealedLetterMission = new SealedLetterMission();
            _marketTallyMission = new MarketTallyMission();
            _commission = new ShadowEscortCommission();
            _tacticalMission = null;
            _tacticalRequest = null;
            _standing = FamilyStandingState.CreateInitial();
            _injuries = MissionInjuryState.TryCreate(null, out MissionInjuryState initialInjuries, out _)
                ? initialInjuries
                : new MissionInjuryState();
            _missionHistory = new MissionHistoryState();
            _succession = succession ?? throw new ArgumentNullException(nameof(succession));
            M2FixedScenario.SyncDesignation(_succession, eventScenarioId);
            _dailyActions = CreateDailyActionsFor(_succession, CurrentLocation);
            _npcAutonomy = null;
            _development = development ?? CreateDevelopmentFor(_succession);
            _industryState = industry ?? IndustryState.CreateInitial(WorldClock.CurrentDate);
            _marketState = CreateInitialMarket(WorldClock.CurrentDate, _succession);
            _lastMarketReport = null;
            _researchState = FamilyResearchState.CreateInitial(WorldClock.CurrentDate);
            _familyDoctrine = new FamilyDoctrineState();
            _eventScenarioId = eventScenarioId ?? string.Empty;
            LastFamilyDoctrineMessage = string.Empty;
            LastCommissionMessage = string.Empty;
            LastTacticalMessage = string.Empty;
            TacticalAppliedFunds = 0;
            TacticalAppliedSupplies = 0;
            TacticalAppliedIntelligence = 0;
            LastIndustryReport = null;
            LastIndustryMessage = string.Empty;
            LastEducationMessage = string.Empty;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastFamilyFormationMessage = string.Empty;
            LastTravelBlockReason = string.Empty;
            LastCampaignReviewMessage = string.Empty;
            _historyLedger = HistoryLedger.CreateEmpty();
            LastSuccessionMessage = string.Empty;
            _dialogues.Clear();
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            LastDailyActionOutcomes = new DailyActionResult[0];
            LastDailyActionMessage = string.Empty;
            InitializeNpcAutonomy();
            SessionStateChanged?.Invoke();
        }

        /// <summary>
        /// Starts the fixed SC-010 crisis roster only from the pristine pre-succession
        /// standard state. A completed succession, crisis adoption or regency must not
        /// be silently erased by replacing the permanent family snapshot. The swap is
        /// also refused while a running task or an M06 operation holds old-roster people,
        /// and when it would orphan an injured person or a terminal-history participant,
        /// because those blocks validate against the current roster.
        /// </summary>
        public bool StartCrisisDemo()
        {
            EnsureSuccession();
            if (IsTacticalMissionActive)
            {
                // The M06 operation lives in session state and names two old-roster
                // operatives. Replacing the family snapshot would leave a live mission
                // pointing at people the crisis roster does not contain, so the entry
                // refuses until the operation reaches its terminal result.
                LastSuccessionMessage =
                    "潜入密令正在进行中：请先完成或撤退，再切换到危机示范。";
                LastEventMessage = LastSuccessionMessage;
                SessionStateChanged?.Invoke();
                return false;
            }
            if (_commission.Stage == ShadowEscortStage.Accepted ||
                _commission.Stage == ShadowEscortStage.InProgress)
            {
                // The crisis roster replaces the whole family snapshot. Swapping it while
                // an accepted-but-unsettled commission (waiting for an assignee or still
                // executing) holds an old-roster assignee would silently strand the paid
                // investment and its chronicle entry, so require settlement first. A
                // settled result is historical and does not block the demo.
                LastSuccessionMessage = _commission.Stage == ShadowEscortStage.Accepted
                    ? "存在已接取但尚未结算的护送委托（待指派执行人）：请先完成或结算，再切换到危机示范。"
                    : "存在已接取但尚未结算的护送委托（执行中）：请先结算，再切换到危机示范。";
                LastEventMessage = LastSuccessionMessage;
                SessionStateChanged?.Invoke();
                return false;
            }
            if (!IsPristineStandardSuccession(Succession))
            {
                // The entry stays clickable so a refusal can explain itself instead of
                // silently disabling the button.
                LastSuccessionMessage = "只能在尚未完成任何继承的新局进入固定危机示范。";
                LastEventMessage = LastSuccessionMessage;
                SessionStateChanged?.Invoke();
                return false;
            }
            string orphan = CrisisRosterOrphanReason();
            if (orphan != null)
            {
                // The replacement roster is smaller than the standard one. Refuse the
                // swap instead of deleting an injury or rewriting terminal history, and
                // say why so the player can recover first and retry.
                LastSuccessionMessage = orphan;
                LastEventMessage = orphan;
                SessionStateChanged?.Invoke();
                return false;
            }
            _succession = SuccessionSample.CreateCrisis();
            // The crisis roster replaces the standard family snapshot, so any
            // appointment held by a person outside the new roster is invalid.
            // Reconcile through the same total function used by succession so the
            // demo never starts with an unusable industry assignment.
            ReconcileIndustryAssignments();
            // The pristine guard above means no education history can exist yet;
            // rebuild growth rows for the replacement roster instead of leaving IDs
            // that no longer belong to this family.
            _development = CreateDevelopmentFor(_succession);
            // The crisis roster replaces the whole family snapshot, so the daily timeline
            // is rebuilt for the replacement roster at the current confirmed location.
            _dailyActions = CreateDailyActionsFor(_succession, CurrentLocation);
            _npcAutonomy = null;
            LastDailyActionOutcomes = new DailyActionResult[0];
            LastDailyActionMessage = string.Empty;
            // The crisis roster replaces the whole family snapshot, so any research plan
            // that referenced the old roster is rebuilt empty for the new family.
            _researchState = FamilyResearchState.CreateInitial(CurrentGameDate);
            LastEducationMessage = string.Empty;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastSuccessionMessage = "已进入固定示范：无合法继承人危机。";
            SessionStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Explicit title-screen crisis entry: initialize the SC-010 standard state,
        /// then hand it to the existing pristine-only crisis transition.
        /// </summary>
        public bool StartCrisisDemoScenario()
        {
            InitStandardDemo();
            return StartCrisisDemo();
        }

        private static bool IsPristineStandardSuccession(SuccessionState state)
        {
            return state != null &&
                   state.Phase == SuccessionPhase.None &&
                   state.SuccessionCount == 0 &&
                   string.IsNullOrEmpty(state.CompletedResultId) &&
                   !state.CrisisAdoptionUsed &&
                   !state.RegencyCompleted &&
                   state.HeadPersonId == SuccessionSample.StandardHeadId &&
                   state.World.Family.Id == SuccessionSample.StandardFamilyId;
        }

        public SuccessionResult BeginSuccessionRetirement()
        {
            EnsureSuccession();
            EnsureEventRuntimeState();
            if (RejectSuccessionWhileEventPending(out SuccessionResult blocked))
                return blocked;
            if (M2FixedScenario.IsActive(_succession) &&
                _succession.Phase == SuccessionPhase.None &&
                !M2FixedScenario.CanBeginRetirement(_succession, _eventRuntimeState,
                    CurrentGameDate, _eventScenarioId, out string gateReason))
            {
                LastSuccessionMessage = gateReason;
                LastEventMessage = gateReason;
                SessionStateChanged?.Invoke();
                return new SuccessionResult(SuccessionOutcome.NotDue, false, gateReason,
                    _succession.Phase, _succession.HeadPersonId);
            }

            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before, SuccessionService.BeginRetirement(_succession, CurrentGameDate));
        }

        public SuccessionResult SelectSuccessionSuccessor(PersonId personId)
        {
            EnsureSuccession();
            EnsureEventRuntimeState();
            if (M2FixedScenario.IsActive(_succession) &&
                _succession.Phase != SuccessionPhase.None &&
                !IsM2FinaleCommitted)
            {
                // Only narrow the selection while the fixed heir can actually succeed.
                // Once that person is dead or unavailable the ordinary eligible living
                // list is the only legal path, matching SuccessionCandidates.
                PersonId expected = AvailableFixedHeir();
                if (!expected.IsEmpty && personId != expected)
                {
                    const string message = "SC-013 固定剧本只允许选择当前固定继承人。";
                    LastSuccessionMessage = message;
                    SessionStateChanged?.Invoke();
                    return new SuccessionResult(SuccessionOutcome.CandidateNotEligible, false,
                        message, _succession.Phase, personId);
                }
            }

            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before,
                SuccessionService.SelectSuccessor(_succession, personId, CurrentGameDate));
        }

        public SuccessionResult SelectSuccessionRegent(PersonId personId)
        {
            EnsureSuccession();
            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before,
                SuccessionService.SelectRegent(_succession, personId, CurrentGameDate));
        }

        public SuccessionResult ConfirmSuccession()
        {
            EnsureSuccession();
            EnsureEventRuntimeState();
            if (RejectSuccessionWhileEventPending(out SuccessionResult blocked))
                return blocked;
            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before,
                SuccessionService.ConfirmSuccession(_succession, CurrentGameDate));
        }

        public SuccessionResult AcceptCrisisAdoption()
        {
            EnsureSuccession();
            EnsureEventRuntimeState();
            if (RejectSuccessionWhileEventPending(out SuccessionResult blocked))
                return blocked;
            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before,
                SuccessionService.AcceptCrisisAdoption(_succession, CurrentGameDate));
        }

        public SuccessionResult DeclineCrisis()
        {
            EnsureSuccession();
            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before, SuccessionService.DeclineCrisis(_succession));
        }

        /// <summary>Completes a due regency after time advances; never advances time itself.</summary>
        public SuccessionResult TryCompleteSuccessionRegency()
        {
            EnsureSuccession();
            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before,
                SuccessionService.TryCompleteRegency(_succession, CurrentGameDate));
        }

        /// <summary>
        /// Single post-command boundary for every succession wrapper. The command has
        /// already mutated the state, so the pre-snapshot decides whether a record is
        /// warranted: only a real phase, head or completed-result change is recorded.
        /// Failed commands and successful no-ops (for example a repeated retirement
        /// request) append nothing but still refresh listeners exactly once.
        /// </summary>
        private SuccessionResult ApplySuccessionResult(in SuccessionSnapshot before, SuccessionResult result)
        {
            if (!result.Succeeded)
                return result;
            LastSuccessionMessage = result.Message;
            if (M2FixedScenario.IsActive(_succession))
                M2FixedScenario.SyncDesignation(_succession, _eventScenarioId);
            if (!before.HeadId.IsEmpty && before.HeadId != _succession.HeadPersonId)
                InterruptDailyActionForPerson(before.HeadId,
                    "家主交接：上一代家主的未完成行动已取消。");
            ReconcileDailyActions();
            EnsureNpcAutonomy();
            PlanNpcEligibilityChange();
            ReconcileDevelopmentRoster();
            if (before.DiffersFrom(_succession))
                RecordSuccession(before, result);
            IReadOnlyList<IndustryVacancy> vacated = ReconcileIndustryAssignments();
            if (vacated.Count > 0)
                RecordIndustryPersonnel(vacated);
            SessionStateChanged?.Invoke();
            return result;
        }

        /// <summary>
        /// Assigns or atomically replaces the head of one industry. This is a
        /// reservation for the current month plan; it never charges resources or
        /// writes history. Only management stable points may change appointments.
        /// </summary>
        public IndustryAssignmentResult TryAssignIndustry(string industryId, PersonId personId)
        {
            EnsureSuccession();
            EnsureIndustryState();
            if (IsTacticalOperative(personId))
            {
                LastIndustryMessage = "该人物正在执行潜入密令：请先完成或撤退，再任命产业职务。";
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.Blocked,
                    LastIndustryMessage, default, default);
            }
            if (!CanEditIndustryAssignments)
            {
                LastIndustryMessage = string.IsNullOrEmpty(IndustryBlockReason)
                    ? "当前不能调整产业任命。"
                    : IndustryBlockReason;
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.Blocked,
                    LastIndustryMessage, default, default);
            }

            IndustryAssignmentResult result;
            if (_commission.Stage == ShadowEscortStage.InProgress &&
                _commission.AssigneeId == personId)
            {
                LastIndustryMessage = "该人物正在执行家族委托：请等待委托结算，再任命产业职务。";
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.Blocked,
                    LastIndustryMessage, default, default);
            }

            result = IndustryService.TryAssign(_industryState, _succession,
                CurrentGameDate, industryId, personId);
            LastIndustryMessage = result.Message;
            if (result.Succeeded)
                SessionStateChanged?.Invoke();
            return result;
        }

        /// <summary>Clears one industry post and releases its 3-unit reservation.</summary>
        public IndustryAssignmentResult TryUnassignIndustry(string industryId)
        {
            EnsureSuccession();
            EnsureIndustryState();
            if (!CanEditIndustryAssignments)
            {
                LastIndustryMessage = string.IsNullOrEmpty(IndustryBlockReason)
                    ? "当前不能调整产业任命。"
                    : IndustryBlockReason;
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.Blocked,
                    LastIndustryMessage, default, default);
            }

            IndustryAssignmentResult result = IndustryService.TryUnassign(_industryState, _succession,
                CurrentGameDate, industryId);
            LastIndustryMessage = result.Message;
            if (result.Succeeded)
                SessionStateChanged?.Invoke();
            return result;
        }

        /// <summary>
        /// Computes the exact deterministic preview for one direction/mentor pair without
        /// modifying any state. Free growth ignores the mentor argument.
        /// </summary>
        public EducationPreview PreviewEducation(EducationDirection direction, PersonId mentor)
        {
            EnsureSuccession();
            EnsureDevelopment();
            EducationView view = Education;
            if (!view.HasLearner)
                return new EducationPreview(false, "当前没有可培养的家族成员。", direction, mentor,
                    string.Empty, MentorSpecialty.None, string.Empty, default, default,
                    string.Empty);
            if (!Enum.IsDefined(typeof(EducationDirection), direction))
                return new EducationPreview(false, "培养方向无效。", direction, mentor, string.Empty,
                    MentorSpecialty.None, string.Empty, view.Values, view.Values, string.Empty);

            EducationPreview Failure(string message) =>
                new EducationPreview(false, message, direction, mentor, string.Empty,
                    MentorSpecialty.None, string.Empty, view.Values, view.Values, string.Empty);

            try
            {
                if (!view.IsEligible)
                    return Failure("孩子已成年，儿童年度培养已结束。");

                MentorSpecialty specialty = MentorSpecialty.None;
                PersonId effectiveMentor = default;
                if (direction == EducationDirection.FreeGrowth)
                {
                    if (!mentor.IsEmpty)
                        return Failure("自由成长不能指定导师。");
                }
                else
                {
                    if (!TryMentorSpecialty(mentor, view.LearnerId, CurrentGameDate, out specialty))
                        return Failure(EducationScenarioProvider.MentorRequirementMessage(Succession));
                    effectiveMentor = mentor;
                }

                if (!CharacterGrowthRules.TryResolve(view.Values, direction,
                        EducationScenarioProvider.HasAptitude(Succession, view.LearnerId),
                        EducationScenarioProvider.AptitudeOf(Succession, view.LearnerId), specialty,
                        EducationScenarioProvider.IsSupportiveEnvironment(Succession, view.LearnerId),
                        out EducationYearResult resolved, out string error))
                    return Failure(error);

                string mentorName = effectiveMentor.IsEmpty
                    ? string.Empty
                    : Succession.World.TryGetPerson(effectiveMentor, out PersonState person)
                        ? person.DisplayName
                        : effectiveMentor.Value;
                string roleLabel = effectiveMentor.IsEmpty
                    ? string.Empty
                    : EducationScenarioProvider.MentorRoleLabel(Succession, effectiveMentor);
                return new EducationPreview(true, string.Empty, direction, effectiveMentor,
                    mentorName, specialty, roleLabel, resolved.Before, resolved.After,
                    resolved.Explanation);
            }
            catch (Exception exception)
            {
                return Failure("培养预览失败：" + exception.Message);
            }
        }

        /// <summary>
        /// Stores or replaces the pending annual plan at a management stable point. It
        /// grants no growth and writes no chronicle; the December-to-January settlement
        /// is the only writer of education results. A settled age year cannot be replanned.
        /// </summary>
        public EducationPlanCommandResult TrySetEducationPlan(PersonId learner,
            EducationDirection direction, PersonId mentor)
        {
            EnsureSuccession();
            EnsureDevelopment();
            // The active-mission duty is checked before learner eligibility so an operative
            // can never be silently reused as a mentor, whatever the learner's age.
            if (IsTacticalOperative(mentor))
            {
                LastEducationMessage = "该人物正在执行潜入密令：请先完成或撤退，再担任培养导师。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidMentor,
                    LastEducationMessage);
            }
            if (learner.IsEmpty || !_development.Contains(learner) ||
                !_succession.World.Family.IsMember(learner))
            {
                LastEducationMessage = "培养对象不是当前家族的成长记录成员。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidLearner,
                    LastEducationMessage);
            }
            if (!_succession.World.TryGetLife(learner, out SuccessionPersonLife life) || !life.Alive)
            {
                LastEducationMessage = "培养对象不是存活人物。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidLearner,
                    LastEducationMessage);
            }

            // Learner identity and age stay specific even when no learner is currently
            // eligible (for example after the child turns 16). Only an otherwise valid
            // request is reported as Blocked by a pending event, succession or regency.
            int age = life.AgeAt(CurrentGameDate);
            if (age < EducationScenarioProvider.MinimumLearnerAge ||
                age > EducationScenarioProvider.MaximumLearnerAge)
            {
                LastEducationMessage = "培养对象已成年（" + age + " 岁），儿童年度培养已结束。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidAge,
                    LastEducationMessage);
            }

            if (!CanEditEducationPlans)
            {
                LastEducationMessage = string.IsNullOrEmpty(EducationBlockReason)
                    ? "当前不能提交培养计划。"
                    : EducationBlockReason;
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.Blocked,
                    LastEducationMessage);
            }

            if (!Enum.IsDefined(typeof(EducationDirection), direction))
            {
                LastEducationMessage = "培养方向无效。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidDirection,
                    LastEducationMessage);
            }
            if (direction == EducationDirection.FreeGrowth)
            {
                if (!mentor.IsEmpty)
                {
                    LastEducationMessage = "自由成长不能指定导师。";
                    return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidMentor,
                        LastEducationMessage);
                }
            }
            else if (_commission.Stage == ShadowEscortStage.InProgress &&
                     _commission.AssigneeId == mentor)
            {
                LastEducationMessage = "该人物正在执行家族委托：请等待委托结算，再担任培养导师。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidMentor,
                    LastEducationMessage);
            }
            else if (!TryMentorSpecialty(mentor, learner, CurrentGameDate, out _))
            {
                LastEducationMessage = EducationScenarioProvider.MentorRequirementMessage(Succession);
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidMentor,
                    LastEducationMessage);
            }

            if (_development.HasResult(learner, age))
            {
                LastEducationMessage = "该年龄年度已经结算，不能再次提交计划。";
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.AlreadySettled,
                    LastEducationMessage);
            }
            if (!EducationYearPlan.TryCreate(learner, age, direction, mentor,
                    out EducationYearPlan plan, out string planError))
            {
                LastEducationMessage = planError;
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.InvalidState,
                    LastEducationMessage);
            }

            CharacterDevelopmentState next = _development.Clone();
            if (!next.TrySetPlan(plan, out string error))
            {
                LastEducationMessage = error;
                return EducationPlanCommandResult.Failure(EducationPlanOutcome.AlreadySettled, error);
            }

            _development = next;
            LastEducationMessage = "已提交 " + age + " 岁年度计划：" +
                CharacterGrowthRules.DirectionName(direction) +
                (direction == EducationDirection.FreeGrowth
                    ? "（无导师）"
                    : "／导师 " + PersonDisplayName(mentor, "家人")) + "。";
            SessionStateChanged?.Invoke();
            return new EducationPlanCommandResult(true, EducationPlanOutcome.Succeeded,
                LastEducationMessage, plan);
        }

        /// <summary>
        /// Starts or replaces the single active research project at a management stable
        /// point. It grants no progress and writes no chronicle; only the monthly
        /// settlement advances or completes a project. Replacing the researcher keeps
        /// the same project's progress.
        /// </summary>
        public FamilyResearchPlanCommandResult TrySetResearchPlan(string researchId,
            PersonId researcherId)
        {
            EnsureSuccession();
            EnsureIndustryState();
            EnsureResearch();
            if (IsTacticalOperative(researcherId))
            {
                LastResearchMessage = "该人物正在执行潜入密令：请先完成或撤退，再安排研究工作。";
                return FamilyResearchPlanCommandResult.Failure(FamilyResearchPlanOutcome.Blocked,
                    LastResearchMessage, researchId, researcherId);
            }
            if (!FamilyResearchCatalog.TryGet(researchId, out FamilyResearchDefinition definition))
            {
                LastResearchMessage = "未知研究项目：" + (researchId ?? "(null)") + "。";
                return FamilyResearchPlanCommandResult.Failure(
                    FamilyResearchPlanOutcome.UnknownResearch, LastResearchMessage, researchId,
                    researcherId);
            }
            if (researcherId.IsEmpty)
            {
                LastResearchMessage = "请选择一位存活、未退休的成年家族研究者。";
                return FamilyResearchPlanCommandResult.Failure(
                    FamilyResearchPlanOutcome.InvalidResearcher, LastResearchMessage, researchId,
                    researcherId);
            }
            if (_researchState.IsKnownTechnique(definition.TechniqueId))
            {
                LastResearchMessage = "「" + definition.TechniqueDisplayName +
                                      "」已经记录为家族知识，无需再次研究。";
                return FamilyResearchPlanCommandResult.Failure(
                    FamilyResearchPlanOutcome.AlreadyKnown, LastResearchMessage, researchId,
                    researcherId);
            }
            if (!CanEditResearch)
            {
                LastResearchMessage = string.IsNullOrEmpty(ResearchBlockReason)
                    ? "当前不能安排研究计划。"
                    : ResearchBlockReason;
                return FamilyResearchPlanCommandResult.Failure(FamilyResearchPlanOutcome.Blocked,
                    LastResearchMessage, researchId, researcherId);
            }
            if (_commission.Stage == ShadowEscortStage.InProgress &&
                _commission.AssigneeId == researcherId)
            {
                LastResearchMessage = "该人物正在执行家族委托：请等待委托结算，再安排研究。";
                return FamilyResearchPlanCommandResult.Failure(
                    FamilyResearchPlanOutcome.InvalidResearcher, LastResearchMessage, researchId,
                    researcherId);
            }
            if (!FamilyResearchSettlement.IsEligibleResearcher(Succession, researcherId,
                    CurrentGameDate, _industryState, out string reason))
            {
                LastResearchMessage = "研究者不符合条件：" + reason;
                return FamilyResearchPlanCommandResult.Failure(
                    FamilyResearchPlanOutcome.InvalidResearcher, LastResearchMessage, researchId,
                    researcherId);
            }

            FamilyResearchState next = _researchState.Clone();
            if (!next.TryStartOrReplacePlan(researchId, researcherId, out string error))
            {
                LastResearchMessage = error;
                return FamilyResearchPlanCommandResult.Failure(
                    FamilyResearchPlanOutcome.InvalidState, error, researchId, researcherId);
            }

            _researchState = next;
            LastResearchMessage = "已安排「" + definition.ProjectDisplayName + "」。";
            SessionStateChanged?.Invoke();
            return new FamilyResearchPlanCommandResult(true, FamilyResearchPlanOutcome.Succeeded,
                LastResearchMessage, researchId, researcherId);
        }

        /// <summary>
        /// Registers one formal marriage between a living adult family member and one
        /// registered, living, adult non-member candidate. Multiple spouses are allowed;
        /// self, duplicate spouse facts and direct blood/adoptive ancestors/descendants
        /// are rejected. Sibling facts do not block the marriage.
        /// </summary>
        public FamilyFormationCommandResult TryRegisterMarriage(PersonId memberId,
            PersonId candidateId)
        {
            EnsureSuccession();
            EnsureHistoryLedger();
            if (!SC022FamilyFormationScenario.IsActive(_succession))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidState,
                    "当前不是 SC-022 婚姻与收养示范。", memberId, candidateId);
            if (memberId.IsEmpty || candidateId.IsEmpty)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "请选择一位家族成员与一位候选人。", memberId, candidateId);
            if (memberId == candidateId)
                return FailFamilyFormation(FamilyFormationOutcome.SelfRelation,
                    "不能与本人登记婚姻。", memberId, candidateId);
            if (!_succession.World.TryGetPerson(memberId, out PersonState member))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "婚姻发起人不是已登记人物。", memberId, candidateId);
            if (!_succession.World.Family.IsMember(memberId))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "婚姻发起人不是当前家族成员。", memberId, candidateId);
            if (!_succession.World.TryGetLife(memberId, out SuccessionPersonLife memberLife) ||
                !memberLife.Alive)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "婚姻发起人必须是在世人物。", memberId, candidateId);
            if (memberLife.AgeAt(CurrentGameDate) < _succession.AdulthoodAge)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "婚姻发起人必须达到家族成年年龄。", memberId, candidateId);

            if (!_succession.World.TryGetPerson(candidateId, out PersonState candidate))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidCandidate,
                    "婚姻候选人不是已登记人物。", memberId, candidateId);
            if (!_succession.World.TryGetLife(candidateId,
                    out SuccessionPersonLife candidateLife) || !candidateLife.Alive)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidCandidate,
                    "婚姻候选人必须是在世人物。", memberId, candidateId);
            if (!SC022FamilyFormationScenario.IsScenarioCandidate(candidateId))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidCandidate,
                    "婚姻候选人不在 SC-022 示范名单中。", memberId, candidateId);

            if (HasKinshipLink(memberId, candidateId, KinshipType.Spouse))
                return FailFamilyFormation(FamilyFormationOutcome.DuplicateRelation,
                    "这两位人物已经登记为正式配偶。", memberId, candidateId);
            if (_succession.World.Family.IsMember(candidateId))
                return FailFamilyFormation(FamilyFormationOutcome.AlreadyMember,
                    "候选人已经属于当前家族。", memberId, candidateId);
            if (candidateLife.AgeAt(CurrentGameDate) < _succession.AdulthoodAge)
                return FailFamilyFormation(FamilyFormationOutcome.CandidateNotEligible,
                    "婚姻候选人必须达到家族成年年龄。", memberId, candidateId);
            if (!CanEditFamilyFormation)
                return FailFamilyFormation(FamilyFormationOutcome.Blocked,
                    FamilyFormationBlockReason, memberId, candidateId);
            if (FamilyFormationRules.IsDirectLineRelation(_succession.World.Kinship, memberId,
                    candidateId))
                return FailFamilyFormation(FamilyFormationOutcome.DirectLineRelation,
                    "直系血亲或直系养亲不能登记婚姻。", memberId, candidateId);

            GameDate date = CurrentGameDate;
            string memberName = member.DisplayName;
            string candidateName = candidate.DisplayName;
            string summary = "婚姻登记：" + memberName + " 与 " + candidateName +
                             " 结为正式配偶。";
            if (!EnsureHistoryLedger().TryPrepareFamilyFormation(date.Year, date.Month,
                    memberId.Value, FamilyChronicleEntry.MarriageSourceId, "家族婚姻", summary,
                    out FamilyChronicleEntry entry, out string chronicleError))
                return FailFamilyFormation(FamilyFormationOutcome.ChronicleFailed,
                    "家族编年史记录无法创建：" + chronicleError, memberId, candidateId);

            if (!_succession.World.TryAddMember(candidateId, FamilyMemberRole.Spouse,
                    out string error))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidState, error, memberId,
                    candidateId);
            var spouseLink = new KinshipLink(memberId, candidateId, KinshipType.Spouse);
            if (!_succession.World.TryAddKinship(spouseLink, out error))
            {
                _succession.World.TryRemoveMember(candidateId, out _);
                return FailFamilyFormation(FamilyFormationOutcome.InvalidState, error, memberId,
                    candidateId);
            }
            FamilyFormationAppendFaultInjection?.Invoke(EnsureHistoryLedger());
            if (!EnsureHistoryLedger().TryAppendPrepared(entry, out error))
            {
                // A prepared entry can still fail if the ledger advanced after
                // preparation. Roll back both relationship mutations so a rejected
                // command leaves membership, kinship and chronicle unchanged.
                _succession.World.Kinship.TryRemove(spouseLink, out _);
                _succession.World.TryRemoveMember(candidateId, out _);
                ReportChronicleAppendFailure("家族婚姻", error);
                return FailFamilyFormation(FamilyFormationOutcome.ChronicleFailed,
                    "家族编年史记录写入失败：" + error, memberId, candidateId);
            }

            LastFamilyFormationMessage = "已登记婚姻：" + memberName + " 与 " + candidateName + "。";
            SessionStateChanged?.Invoke();
            return new FamilyFormationCommandResult(true, FamilyFormationOutcome.Succeeded,
                LastFamilyFormationMessage, memberId, candidateId);
        }

        /// <summary>
        /// Registers one ordinary adoption. The candidate may be a child or an adult;
        /// the adopter must be a living adult family member. Self, a formal spouse,
        /// duplicate adoption facts and candidates that would close a directed
        /// parent/child cycle are rejected. Blood facts and succession state are not
        /// changed.
        /// </summary>
        public FamilyFormationCommandResult TryAdopt(PersonId adopterId, PersonId candidateId)
        {
            EnsureSuccession();
            EnsureHistoryLedger();
            if (!SC022FamilyFormationScenario.IsActive(_succession))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidState,
                    "当前不是 SC-022 婚姻与收养示范。", adopterId, candidateId);
            if (adopterId.IsEmpty || candidateId.IsEmpty)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "请选择一位收养人与一位候选人。", adopterId, candidateId);
            if (adopterId == candidateId)
                return FailFamilyFormation(FamilyFormationOutcome.SelfRelation,
                    "不能收养本人。", adopterId, candidateId);
            if (!_succession.World.TryGetPerson(adopterId, out PersonState adopter))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "收养人不是已登记人物。", adopterId, candidateId);
            if (!_succession.World.Family.IsMember(adopterId))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "收养人不是当前家族成员。", adopterId, candidateId);
            if (!_succession.World.TryGetLife(adopterId, out SuccessionPersonLife adopterLife) ||
                !adopterLife.Alive)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "收养人必须是在世人物。", adopterId, candidateId);
            if (adopterLife.AgeAt(CurrentGameDate) < _succession.AdulthoodAge)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidInitiator,
                    "收养人必须达到家族成年年龄。", adopterId, candidateId);

            if (!_succession.World.TryGetPerson(candidateId, out PersonState candidate))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidCandidate,
                    "收养候选人不是已登记人物。", adopterId, candidateId);
            if (!_succession.World.TryGetLife(candidateId,
                    out SuccessionPersonLife candidateLife) || !candidateLife.Alive)
                return FailFamilyFormation(FamilyFormationOutcome.InvalidCandidate,
                    "收养候选人必须是在世人物。", adopterId, candidateId);
            if (HasKinshipLink(adopterId, candidateId, KinshipType.Spouse))
                return FailFamilyFormation(FamilyFormationOutcome.SpouseRelation,
                    "不能收养自己的正式配偶。", adopterId, candidateId);
            if (_succession.World.Family.IsMember(candidateId))
                return FailFamilyFormation(FamilyFormationOutcome.AlreadyMember,
                    "候选人已经属于当前家族。", adopterId, candidateId);
            if (!SC022FamilyFormationScenario.IsScenarioCandidate(candidateId))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidCandidate,
                    "收养候选人不在 SC-022 示范名单中。", adopterId, candidateId);
            if (HasKinshipLink(adopterId, candidateId, KinshipType.Adoption))
                return FailFamilyFormation(FamilyFormationOutcome.DuplicateRelation,
                    "这条收养关系已经登记。", adopterId, candidateId);
            if (!CanEditFamilyFormation)
                return FailFamilyFormation(FamilyFormationOutcome.Blocked,
                    FamilyFormationBlockReason, adopterId, candidateId);
            if (FamilyFormationRules.WouldCreateAdoptionCycle(_succession.World.Kinship, adopterId,
                    candidateId))
                return FailFamilyFormation(FamilyFormationOutcome.AdoptionCycle,
                    "该收养会使亲子关系形成循环。", adopterId, candidateId);

            GameDate date = CurrentGameDate;
            string adopterName = adopter.DisplayName;
            string candidateName = candidate.DisplayName;
            string summary = "普通收养：" + adopterName + " 收养 " + candidateName +
                             " 为养子/养女。";
            if (!EnsureHistoryLedger().TryPrepareFamilyFormation(date.Year, date.Month,
                    adopterId.Value, FamilyChronicleEntry.AdoptionSourceId, "家族收养", summary,
                    out FamilyChronicleEntry entry, out string chronicleError))
                return FailFamilyFormation(FamilyFormationOutcome.ChronicleFailed,
                    "家族编年史记录无法创建：" + chronicleError, adopterId, candidateId);

            if (!_succession.World.TryAddMember(candidateId, FamilyMemberRole.AdoptedChild,
                    out string error))
                return FailFamilyFormation(FamilyFormationOutcome.InvalidState, error, adopterId,
                    candidateId);
            var adoptionLink = new KinshipLink(adopterId, candidateId, KinshipType.Adoption);
            if (!_succession.World.TryAddKinship(adoptionLink, out error))
            {
                _succession.World.TryRemoveMember(candidateId, out _);
                return FailFamilyFormation(FamilyFormationOutcome.InvalidState, error, adopterId,
                    candidateId);
            }
            FamilyFormationAppendFaultInjection?.Invoke(EnsureHistoryLedger());
            if (!EnsureHistoryLedger().TryAppendPrepared(entry, out error))
            {
                // Same atomic rollback as marriage: the append can fail if the ledger
                // advanced after preparation, so remove the uncommitted member and
                // kinship fact before reporting the failure.
                _succession.World.Kinship.TryRemove(adoptionLink, out _);
                _succession.World.TryRemoveMember(candidateId, out _);
                ReportChronicleAppendFailure("家族收养", error);
                return FailFamilyFormation(FamilyFormationOutcome.ChronicleFailed,
                    "家族编年史记录写入失败：" + error, adopterId, candidateId);
            }

            LastFamilyFormationMessage = "已办理收养：" + adopterName + " 收养 " +
                                         candidateName + "。";
            SessionStateChanged?.Invoke();
            return new FamilyFormationCommandResult(true, FamilyFormationOutcome.Succeeded,
                LastFamilyFormationMessage, adopterId, candidateId);
        }

        private FamilyFormationCommandResult FailFamilyFormation(FamilyFormationOutcome outcome,
            string message, PersonId initiatorId, PersonId candidateId)
        {
            LastFamilyFormationMessage = message ?? string.Empty;
            return FamilyFormationCommandResult.Failure(outcome, LastFamilyFormationMessage,
                initiatorId, candidateId);
        }

        private bool HasKinshipLink(PersonId from, PersonId to, KinshipType type) =>
            _succession != null && _succession.World != null &&
            _succession.World.Kinship.Contains(new KinshipLink(from, to, type));

        /// <summary>
        /// Keeps the v7 growth rows aligned with the current succession roster after a
        /// successful succession command. Crisis adoption adds one person; the new row
        /// starts at zero and existing values, plans and results are never rewritten.
        /// Roster replacement (starting the crisis demo) rebuilds the whole state instead.
        /// </summary>
        private void ReconcileDevelopmentRoster()
        {
            if (_development == null || _succession == null || _succession.World == null)
                return;
            foreach (PersonState person in _succession.World.Persons)
            {
                if (_development.Contains(person.Id))
                    continue;
                _development.TryAddPerson(person.Id,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0), out _);
            }
        }

        /// <summary>Total cleanup used after succession and before capture; no production or history.</summary>
        private IReadOnlyList<IndustryVacancy> ReconcileIndustryAssignments()
        {
            if (_industryState == null)
                return Array.Empty<IndustryVacancy>();
            IReadOnlyList<IndustryVacancy> vacated = IndustryService.ReconcileAssignments(
                _industryState, Succession, CurrentGameDate);
            if (vacated.Count > 0)
                LastIndustryMessage = DescribeVacancies(vacated);
            return vacated;
        }

        private void EnsureIndustryAssignmentsValid()
        {
            EnsureSuccession();
            EnsureIndustryState();
            ReconcileIndustryAssignments();
        }

        /// <summary>
        /// Readable completion record. The stable research/technique IDs stay in the
        /// ledger fields; the player-facing title and summary contain only names and
        /// the exact progress/month values.
        /// </summary>
        private string BuildResearchChronicleSummary(FamilyResearchSettlementResult result,
            GameDate completedMonth)
        {
            string technique = FamilyResearchCatalog.TechniqueDisplayName(result.TechniqueId);
            string researcher = IndustryService.PersonName(Succession, result.ResearcherId);
            return "「" + technique + "」研究完成：研究者" + researcher + "，进度 " +
                   result.BeforeProgress.ToString(CultureInfo.InvariantCulture) + " → " +
                   result.AfterProgress.ToString(CultureInfo.InvariantCulture) + "，完成于 " +
                   completedMonth.Year.ToString("0000", CultureInfo.InvariantCulture) + " 年 " +
                   completedMonth.Month.ToString(CultureInfo.InvariantCulture) + " 月。";
        }

        private string DescribeVacancies(IReadOnlyList<IndustryVacancy> vacated)
        {
            var builder = new StringBuilder(96);
            builder.Append("已移除失效产业任命：");
            for (int i = 0; i < vacated.Count; i++)
            {
                if (i > 0)
                    builder.Append('；');
                builder.Append(IndustryService.IndustryName(vacated[i].IndustryId)).Append('/')
                    .Append(IndustryService.PersonName(Succession, vacated[i].PreviousAssignee))
                    .Append("（").Append(vacated[i].Reason).Append('）');
            }
            builder.Append('。');
            return builder.ToString();
        }

        /// <summary>Appends exactly one personnel record for a real succession-driven cleanup.</summary>
        private void RecordIndustryPersonnel(IReadOnlyList<IndustryVacancy> vacated)
        {
            GameDate date = CurrentGameDate;
            var builder = new StringBuilder(128);
            builder.Append(DescribeVacancies(vacated));
            foreach (IndustryAssignment assignment in EnsureIndustryState().Assignments)
            {
                if (assignment.PersonId.IsEmpty)
                    continue;
                builder.Append(IndustryService.IndustryName(assignment.IndustryId)).Append("仍由")
                    .Append(IndustryService.PersonName(Succession, assignment.PersonId))
                    .Append("负责。");
            }

            if (!EnsureHistoryLedger().TryAppendIndustry(date.Year, date.Month,
                    "industry.assignments", "succession", 0, 0, "产业人事", builder.ToString(),
                    out _, out string error))
                ReportChronicleAppendFailure("产业人事", error);
        }

        private readonly struct SuccessionSnapshot
        {
            private SuccessionSnapshot(SuccessionPhase phase, PersonId headId, string completedResultId)
            {
                Phase = phase;
                HeadId = headId;
                CompletedResultId = completedResultId ?? string.Empty;
            }

            public SuccessionPhase Phase { get; }
            public PersonId HeadId { get; }
            public string CompletedResultId { get; }

            public static SuccessionSnapshot Capture(SuccessionState state) =>
                new SuccessionSnapshot(state.Phase, state.HeadPersonId, state.CompletedResultId);

            public bool DiffersFrom(SuccessionState state) =>
                state != null &&
                (Phase != state.Phase ||
                 HeadId != state.HeadPersonId ||
                 !string.Equals(CompletedResultId, state.CompletedResultId ?? string.Empty,
                     StringComparison.Ordinal));
        }

        private bool NoticeRegencyHandoffIfDue()
        {
            EnsureSuccession();
            if (!_succession.IsRegencyHandoffDue(CurrentGameDate))
                return false;
            const string message = "幼主已成年：时间推进已暂停，请点击「完成成年交权」。";
            bool changed = !string.Equals(LastSuccessionMessage, message, StringComparison.Ordinal);
            LastSuccessionMessage = message;
            return changed;
        }

        private bool SuccessionBlocksTimeAdvance =>
            _succession != null &&
            (_succession.Phase == SuccessionPhase.Extinct ||
             _succession.IsPending ||
             _succession.IsRegencyHandoffDue(CurrentGameDate));

        /// <summary>
        /// Publishes the reason a time advance is refused while succession owns the pause.
        /// It reuses the existing month-turn outcome and event-message surfaces so the HUD
        /// can show the same reason for day and month advances.
        /// </summary>
        private void ReportSuccessionTimePause()
        {
            LastMonthTurnOutcome = MonthTurnOutcome.PendingSuccession;
            if (_succession != null && _succession.Phase == SuccessionPhase.Extinct)
            {
                LastEventMessage = "家族已经灭亡：无法再推进时间或旅行。请读取较早的存档以回到灭亡前。";
                return;
            }
            LastEventMessage = Succession.IsRegencyHandoffDue(CurrentGameDate)
                ? "幼主已成年：时间推进已暂停，请点击「完成成年交权」。"
                : "尚有继承决策待处理：请先完成当前继承选择，再推进时间。";
        }

        /// <summary>
        /// Publishes the succession reason for a refused travel preview or confirmation.
        /// Travel reuses the same outcome/message surfaces as time advancement so the HUD
        /// can replace its generic route/date error with the actual pending decision.
        /// </summary>
        private void ReportSuccessionTravelPause()
        {
            LastMonthTurnOutcome = MonthTurnOutcome.PendingSuccession;
            if (_succession != null && _succession.Phase == SuccessionPhase.Extinct)
            {
                LastEventMessage = "家族已经灭亡：无法再旅行。请读取较早的存档以回到灭亡前。";
                return;
            }
            LastEventMessage = Succession.IsRegencyHandoffDue(CurrentGameDate)
                ? "幼主已成年：旅行已暂停，请点击「完成成年交权」后再旅行。"
                : "尚有继承决策待处理：请先完成当前继承选择，再旅行。";
        }

        private void EnsureSuccession()
        {
            if (_succession == null)
            {
                _succession = SC024FamilyDoctrineScenario.IsRoute(_eventScenarioId)
                    ? SC024FamilyDoctrineScenario.CreateSuccession()
                    : M2FixedScenario.CreateSuccession();
                M2FixedScenario.SyncDesignation(_succession, _eventScenarioId);
            }
        }

        private IndustryState EnsureIndustryState()
        {
            if (_industryState == null)
                _industryState = IndustryState.CreateInitial(CurrentGameDate);
            return _industryState;
        }

        private CharacterDevelopmentState EnsureDevelopment()
        {
            if (_development == null)
                _development = CreateDevelopmentFor(Succession);
            return _development;
        }

        private FamilyResearchState EnsureResearch()
        {
            if (_researchState == null)
                _researchState = FamilyResearchState.CreateInitial(CurrentGameDate);
            return _researchState;
        }

        private FamilyDoctrineState EnsureFamilyDoctrine()
        {
            if (_familyDoctrine == null)
                _familyDoctrine = new FamilyDoctrineState();
            return _familyDoctrine;
        }

        private DailyActionState EnsureDailyActions()
        {
            EnsureSuccession();
            if (_dailyActions == null)
                _dailyActions = CreateDailyActionsFor(_succession, CurrentLocation);
            if (_succession.World != null)
                foreach (PersonState person in _succession.World.Persons)
                    _dailyActions.EnsurePerson(person.Id, CurrentLocation);
            return _dailyActions;
        }

        private NpcAutonomyState EnsureNpcAutonomy()
        {
            EnsureSuccession();
            IReadOnlyList<PersonId> registered = RegisteredPilotIds();
            if (_npcAutonomy == null)
                _npcAutonomy = NpcAutonomyState.CreateForRoster(registered);
            else
                _npcAutonomy.Reconcile(registered);
            return _npcAutonomy;
        }

        /// <summary>Living pilot IDs in the fixed M2 family, in ordinal profile order.</summary>
        private IReadOnlyList<PersonId> RegisteredPilotIds()
        {
            var ids = new List<PersonId>(2);
            if (_succession == null || _succession.World == null ||
                !M2FixedScenario.IsActive(_succession))
                return ids.AsReadOnly();
            foreach (NpcAutonomyProfile profile in NpcAutonomyRules.Profiles)
            {
                if (!_succession.World.TryGetPerson(profile.PersonId, out _) ||
                    !_succession.World.IsLiving(profile.PersonId) ||
                    !_succession.World.Family.IsMember(profile.PersonId))
                    continue;
                ids.Add(profile.PersonId);
            }
            return ids.AsReadOnly();
        }

        /// <summary>
        /// M03 eligibility facts read straight from the live session. The injury and
        /// industry ledgers are never created here, so the read-only Inspector projection
        /// stays a pure read: an absent ledger means "no injury" and "no duty".
        /// </summary>
        private NpcActionEligibility NpcEligibilityFor(PersonId personId)
        {
            bool isInjured = _injuries != null && _injuries.IsInjured(personId);
            bool hasNoSupplies = _industryState != null &&
                                 _industryState.Resources.Supplies == 0;
            return new NpcActionEligibility(isInjured, HasIndustryDuty(personId),
                hasNoSupplies);
        }

        /// <summary>True while the person holds any industry post; never creates state.</summary>
        private bool HasIndustryDuty(PersonId personId)
        {
            if (_industryState == null || personId.IsEmpty)
                return false;
            for (int i = 0; i < _industryState.Assignments.Count; i++)
                if (_industryState.Assignments[i].PersonId == personId)
                    return true;
            return false;
        }

        private NpcAutonomyEvaluation BuildNpcEvaluation(PersonId personId, NpcNeedValues needs,
            IReadOnlyList<NpcActionMemory> memories)
        {
            DailyActionState actions = EnsureDailyActions();
            bool hasLocation = actions.TryGetLocation(personId, out _);
            bool hasActive = actions.HasActiveAction(personId);
            return NpcAutonomyRules.Evaluate(personId, needs, memories, CurrentGameDate,
                true, hasLocation, hasActive, NpcEligibilityFor(personId));
        }

        private NpcAutonomyEvaluation BuildNpcEvaluation(PersonId personId, NpcNeedValues needs,
            IReadOnlyList<NpcActionMemory> memories, bool hasActiveAction)
        {
            DailyActionState actions = EnsureDailyActions();
            bool hasLocation = actions.TryGetLocation(personId, out _);
            return NpcAutonomyRules.Evaluate(personId, needs, memories, CurrentGameDate,
                true, hasLocation, hasActiveAction, NpcEligibilityFor(personId));
        }

        /// <summary>Initial plan for a new session; never runs during save restore.</summary>
        private void InitializeNpcAutonomy()
        {
            _npcAutonomy = NpcAutonomyState.CreateForRoster(RegisteredPilotIds());
            _dailyActionNotificationSuppression++;
            try
            {
                PlanIdlePilots(NpcAutonomyTrigger.Initialization, null);
            }
            finally
            {
                _dailyActionNotificationSuppression--;
            }
        }

        /// <summary>
        /// Applies one day's need growth before action progression. Returns true when at
        /// least one pilot row grew, so callers can publish the new needs even when no
        /// action completed and no replan occurred.
        /// </summary>
        private bool GrowNpcNeeds(int days)
        {
            if (days <= 0)
                return false;
            if (_npcAutonomy == null)
                _npcAutonomy = NpcAutonomyState.CreateForRoster(RegisteredPilotIds());
            if (_npcAutonomy.Persons.Count == 0)
                return false;
            _npcAutonomy.GrowAll(days);
            return true;
        }

        /// <summary>Reduces the matching need once for every completed pilot action.</summary>
        private void ApplyNpcActionCompletions(IReadOnlyList<DailyActionResult> outcomes)
        {
            if (_npcAutonomy == null || outcomes == null)
                return;
            foreach (DailyActionResult outcome in outcomes)
            {
                if (outcome.Outcome != DailyActionOutcomeKind.Completed ||
                    !NpcAutonomyRules.IsPilot(outcome.PersonId))
                    continue;
                _npcAutonomy.ApplyCompletedAction(outcome.PersonId, outcome.Kind,
                    outcome.Date, outcome.Reason);
            }
        }

        /// <summary>
        /// Records one committed pilot interruption in the same transaction as the
        /// daily-action state change. Rejected or rolled-back commands never call this.
        /// </summary>
        private void ApplyNpcActionInterruption(DailyActionResult outcome)
        {
            if (_npcAutonomy == null || outcome.Outcome != DailyActionOutcomeKind.Interrupted ||
                !NpcAutonomyRules.IsPilot(outcome.PersonId))
                return;
            _npcAutonomy.ApplyInterruptedAction(outcome.PersonId, outcome.Kind,
                outcome.Date, outcome.Reason);
        }

        /// <summary>
        /// Explicit eligibility-change pass used by succession/roster and manual-travel
        /// flows. It evaluates every authored pilot, so a pilot missing from the roster
        /// produces an observable Rejected decision instead of silently doing nothing.
        /// </summary>
        private bool PlanNpcEligibilityChange()
        {
            NpcAutonomyState state = EnsureNpcAutonomy();
            int suppression = _dailyActionNotificationSuppression;
            _dailyActionNotificationSuppression++;
            try
            {
                bool changed = false;
                foreach (NpcAutonomyProfile profile in NpcAutonomyRules.Profiles)
                {
                    if (state.Contains(profile.PersonId))
                        continue;
                    PlanOnePilot(profile.PersonId, NpcAutonomyTrigger.EligibilityChanged,
                        out _);
                    changed = true;
                }
                changed |= PlanIdlePilots(NpcAutonomyTrigger.EligibilityChanged, null);
                return changed;
            }
            finally
            {
                _dailyActionNotificationSuppression = suppression;
            }
        }

        /// <summary>
        /// Replans pilots whose action completed on this advance, then checks the
        /// remaining pilots with the supplied idle trigger. A pilot handled by the
        /// completion pass is excluded from the idle pass so one blocked replan cannot
        /// append the same Deferred/Rejected decision twice in one day.
        /// </summary>
        private bool PlanNpcAfterAdvance(IReadOnlyList<DailyActionResult> outcomes,
            NpcAutonomyTrigger idleTrigger = NpcAutonomyTrigger.DailyCheck)
        {
            if (_npcAutonomy == null || _npcAutonomy.Persons.Count == 0)
                return false;
            var completed = new HashSet<PersonId>();
            if (outcomes != null)
            {
                foreach (DailyActionResult outcome in outcomes)
                    if (outcome.Outcome == DailyActionOutcomeKind.Completed &&
                        NpcAutonomyRules.IsPilot(outcome.PersonId))
                        completed.Add(outcome.PersonId);
            }

            int suppression = _dailyActionNotificationSuppression;
            _dailyActionNotificationSuppression++;
            try
            {
                bool changed = false;
                if (completed.Count > 0)
                    changed |= PlanIdlePilots(NpcAutonomyTrigger.ActionCompleted, completed);

                var idleOnly = new HashSet<PersonId>();
                foreach (NpcAutonomyPersonState person in _npcAutonomy.Persons)
                    if (!completed.Contains(person.PersonId))
                        idleOnly.Add(person.PersonId);
                // An idle pilot is checked again on the next simulated day. This also
                // resumes autonomy after a load or a cleared short-term block, while an
                // active pilot is always skipped and never replaced.
                if (idleOnly.Count > 0)
                    changed |= PlanIdlePilots(idleTrigger, idleOnly);
                return changed;
            }
            finally
            {
                _dailyActionNotificationSuppression = suppression;
            }
        }

        /// <summary>Plans idle pilots; <paramref name="only"/> null means every idle pilot.</summary>
        private bool PlanIdlePilots(NpcAutonomyTrigger trigger, ICollection<PersonId> only)
        {
            NpcAutonomyState state = EnsureNpcAutonomy();
            IReadOnlyList<DailyActionResult> outcomesBefore = LastDailyActionOutcomes;
            string messageBefore = LastDailyActionMessage;
            bool changed = false;

            foreach (NpcAutonomyPersonState person in state.Persons)
            {
                if (only != null && !only.Contains(person.PersonId))
                    continue;
                if (EnsureDailyActions().HasActiveAction(person.PersonId))
                {
                    state.SetStatus(person.PersonId, NpcAutonomyStatus.Executing);
                    // Active pilots are never re-scored for scheduling. The Inspector
                    // derives current diagnostic scores from live needs on read, while
                    // LatestEvaluation keeps the explanation that selected this action.
                    changed = true;
                    continue;
                }

                PlanOnePilot(person.PersonId, trigger, out _);
                changed = true;
            }

            // Autonomy decisions are logged on the snapshot; the shared completion
            // outcome channel still reports only the M02 day/action results.
            LastDailyActionOutcomes = outcomesBefore;
            LastDailyActionMessage = messageBefore;
            return changed;
        }

        /// <summary>
        /// The single scheduling path for autonomous pilots. A failed schedule after an
        /// eligible candidate is a Deferred command, not a rejection of the candidate.
        /// </summary>
        private NpcAutonomyCommandOutcomeKind PlanOnePilot(PersonId personId,
            NpcAutonomyTrigger trigger, out string reason)
        {
            reason = string.Empty;
            NpcAutonomyState state = EnsureNpcAutonomy();
            if (!state.TryGetPerson(personId, out NpcAutonomyPersonState person))
            {
                reason = "人物未登记为自主 NPC。";
                state.RecordDecision(new NpcAutonomyDecisionRecord(CurrentGameDate, personId,
                    NpcAutonomyCommandOutcomeKind.Rejected, DailyActionKind.Rest, false, trigger,
                    reason));
                return NpcAutonomyCommandOutcomeKind.Rejected;
            }

            bool hasLocation = EnsureDailyActions().TryGetLocation(personId, out _);
            NpcAutonomyEvaluation evaluation = NpcAutonomyRules.Evaluate(personId,
                person.Needs, person.Memories, CurrentGameDate, true, hasLocation, false,
                NpcEligibilityFor(personId));
            state.SetLatestEvaluation(personId, evaluation);
            if (!evaluation.HasSelection)
            {
                state.SetStatus(personId, NpcAutonomyStatus.Idle);
                reason = evaluation.Summary;
                state.RecordDecision(new NpcAutonomyDecisionRecord(CurrentGameDate, personId,
                    NpcAutonomyCommandOutcomeKind.Rejected, DailyActionKind.Rest, false, trigger,
                    reason));
                return NpcAutonomyCommandOutcomeKind.Rejected;
            }

            NpcActionCandidate selected = evaluation.Selected;
            if (TryPlanDailyAction(personId, selected.Kind,
                    NpcAutonomyRules.PlannedDuration(selected.Kind), default, out reason))
            {
                state.SetStatus(personId, NpcAutonomyStatus.Executing);
                string accepted = "按需求与偏好选择" + selected.KindName + "（分数 " +
                                  selected.Score + "）。";
                state.RecordDecision(new NpcAutonomyDecisionRecord(CurrentGameDate, personId,
                    NpcAutonomyCommandOutcomeKind.Accepted, selected.Kind, true, trigger,
                    accepted));
                reason = accepted;
                return NpcAutonomyCommandOutcomeKind.Accepted;
            }

            state.SetStatus(personId, NpcAutonomyStatus.Idle);
            string deferredReason = reason + (HasPendingEventForPlanning()
                ? "下次检查：事件处理后的下一个模拟日。"
                : "下次检查：下一个模拟日。");
            state.RecordDecision(new NpcAutonomyDecisionRecord(CurrentGameDate, personId,
                NpcAutonomyCommandOutcomeKind.Deferred, selected.Kind, true, trigger,
                deferredReason));
            reason = deferredReason;
            return NpcAutonomyCommandOutcomeKind.Deferred;
        }

        private bool HasPendingEventForPlanning() =>
            _eventRuntimeState != null && _eventRuntimeState.HasPendingEvents;

        /// <summary>Refreshes only missing diagnostics; never overwrites a stored plan rationale.</summary>
        private void RefreshNpcAutonomyDiagnostics()
        {
            NpcAutonomyState state = EnsureNpcAutonomy();
            foreach (NpcAutonomyPersonState person in state.Persons)
            {
                if (person.LatestEvaluation == null)
                    state.SetLatestEvaluation(person.PersonId,
                        BuildNpcEvaluation(person.PersonId, person.Needs, person.Memories));
                if (EnsureDailyActions().HasActiveAction(person.PersonId))
                    state.SetStatus(person.PersonId, NpcAutonomyStatus.Executing);
            }
        }

        /// <summary>
        /// Loads a session without scheduling anything. Active tasks remain authoritative;
        /// idle pilots are checked on the next simulated day, so a stable checkpoint can be
        /// captured and restored byte-for-byte without replaying autonomy decisions.
        /// </summary>
        private void PlanNpcOnLoad()
        {
            if (_npcAutonomy == null)
                return;
            RefreshNpcAutonomyDiagnostics();
        }

        private NpcAutonomyState CloneNpcAutonomy() =>
            _npcAutonomy != null ? _npcAutonomy.Clone() : null;

        /// <summary>Selects the current head's one lifetime ambition; old heads are retained.</summary>
        public bool TryStartCurrentHeadAmbition(FamilyAmbitionKind kind)
        {
            EnsureSuccession();
            EnsureResearch();
            FamilyDoctrineState doctrine = EnsureFamilyDoctrine();
            if (Succession == null || Succession.HeadPersonId.IsEmpty)
            {
                LastFamilyDoctrineMessage = "当前没有可选定抱负的家主。";
                return false;
            }
            if (!doctrine.TryStartAmbition(Succession.HeadPersonId, kind,
                    _researchState.KnownTechniqueIds, out string error))
            {
                LastFamilyDoctrineMessage = error;
                return false;
            }

            LastFamilyDoctrineMessage = "家主已选定抱负：" + FamilyAmbitionName(kind) + "。";
            SessionStateChanged?.Invoke();
            return true;
        }

        private static string FamilyAmbitionName(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold: return "发展家业";
                case FamilyAmbitionKind.PassDownTechnique: return "传承家技";
                case FamilyAmbitionKind.EducateHeir: return "培养继承人";
                default: return "未知抱负";
            }
        }

        /// <summary>Creates zero growth rows for every registered family person.</summary>
        private static CharacterDevelopmentState CreateDevelopmentFor(SuccessionState succession)
        {
            var development = CharacterDevelopmentState.CreateEmpty();
            if (succession == null || succession.World == null)
                return development;

            foreach (PersonState person in succession.World.Persons)
            {
                CharacterGrowthValues values = EducationScenarioProvider.InitialValuesFor(succession,
                    person.Id);
                development.TryAddPerson(person.Id, values, out _);
            }
            return development;
        }

        /// <summary>Creates one daily-action row per registered person at the start location.</summary>
        private static DailyActionState CreateDailyActionsFor(SuccessionState succession,
            LocationId location)
        {
            if (succession == null || succession.World == null)
                return DailyActionState.CreateEmpty();
            var ids = new List<PersonId>(succession.World.PersonCount);
            foreach (PersonState person in succession.World.Persons)
                ids.Add(person.Id);
            return DailyActionState.CreateForRoster(ids, location);
        }

        /// <summary>
        /// Applies one pure M04B plan to the working succession snapshot. Existing
        /// pregnancies are removed before starts are added, and every newborn is
        /// registered through the same person/member/kinship paths as the fixed rosters.
        /// Growth rows use the existing scenario defaults; no autonomy, industry,
        /// research, education plan or relationship value is created here.
        /// </summary>
        private static bool TryApplyBirthSettlement(SuccessionState succession,
            FamilyBirthSettlementPlan plan, ref CharacterDevelopmentState development,
            out string error)
        {
            error = string.Empty;
            if (succession == null || succession.World == null || plan == null)
            {
                error = "出生结算状态为空。";
                return false;
            }

            foreach (FamilyPregnancyRecord record in plan.PregnancyPlan.Cancellations)
                if (!succession.World.Pregnancies.TryRemove(record, out error))
                    return false;
            foreach (FamilyPregnancyRecord record in plan.PregnancyPlan.Completions)
                if (!succession.World.Pregnancies.TryRemove(record, out error))
                    return false;
            foreach (FamilyPregnancyRecord record in plan.PregnancyPlan.Starts)
                if (!succession.World.Pregnancies.TryAdd(record, out error))
                    return false;

            if (plan.Births.Count == 0)
                return true;

            development = development != null
                ? development.Clone()
                : CharacterDevelopmentState.CreateEmpty();

            foreach (FamilyBirthFact birth in plan.Births)
            {
                var person = new PersonState(birth.NewbornId, birth.DisplayName, birth.Gender);
                if (!succession.World.TryAddPerson(person,
                        new SuccessionPersonLife(birth.BirthDate.Year, true, false), out error))
                    return false;
                if (!succession.World.TryAddMember(birth.NewbornId,
                        FamilyMemberRole.DirectKin, out error))
                    return false;

                if (!TryAddBirthKinship(succession, birth.GestatingParentId,
                        birth.NewbornId, out error) ||
                    !TryAddBirthKinship(succession, birth.OtherParentId,
                        birth.NewbornId, out error))
                    return false;

                foreach (PersonId siblingId in birth.FullSiblingIds)
                {
                    var siblingLink = new KinshipLink(birth.NewbornId, siblingId,
                        KinshipType.Sibling);
                    if (succession.World.Kinship.Contains(siblingLink))
                        continue;
                    if (!succession.World.TryAddKinship(siblingLink, out error))
                        return false;
                }

                CharacterGrowthValues values =
                    EducationScenarioProvider.InitialValuesFor(succession, birth.NewbornId);
                if (!development.TryAddPerson(birth.NewbornId, values, out error))
                    return false;
            }

            return true;
        }

        private static bool TryAddBirthKinship(SuccessionState succession, PersonId parentId,
            PersonId childId, out string error)
        {
            if (!KinshipLink.TryCreate(parentId, childId, KinshipType.ParentChild,
                    out KinshipLink link, out error))
                return false;
            if (succession.World.Kinship.Contains(link))
            {
                error = "新生儿亲子关系已存在。";
                return false;
            }
            return succession.World.TryAddKinship(link, out error);
        }

        private bool HasEligibleEducationLearner()
        {
            if (_development == null || _succession == null || _succession.World == null)
                return false;
            foreach (PersonId personId in _development.PersonIds)
            {
                if (!_succession.World.Family.IsMember(personId) ||
                    !_succession.World.TryGetLife(personId, out SuccessionPersonLife life) ||
                    !life.Alive)
                    continue;
                int age = life.AgeAt(CurrentGameDate);
                if (age >= EducationScenarioProvider.MinimumLearnerAge &&
                    age <= EducationScenarioProvider.MaximumLearnerAge)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// One shared mentor rule for preview, plan submission, settlement and save
        /// validation. The learner is never its own mentor; a mentor must be a living adult
        /// family member. SC-023 additionally admits its retired direct-kin former head,
        /// proven by a real head handoff already recorded in the succession chronicle.
        /// </summary>
        private bool IsEligibleEducationMentor(PersonId mentorId, PersonId learnerId, GameDate date)
        {
            if (mentorId.IsEmpty || mentorId == learnerId || _succession == null ||
                _succession.World == null ||
                !_succession.World.Family.IsMember(mentorId) ||
                !_succession.World.Family.TryGetRole(mentorId, out FamilyMemberRole role) ||
                !_succession.World.TryGetLife(mentorId, out SuccessionPersonLife life))
                return false;
            string familyId = _succession.World.Family.Id.Value;
            bool hasHeldHeadRole = EducationMentorRules.IsSeclusionScenario(familyId) &&
                                   SuccessionHistory.HasHeldHeadRole(
                                       EnsureHistoryLedger().Entries, mentorId);
            return EducationMentorRules.IsEligible(familyId, mentorId,
                learnerId, true, life.Alive, life.Retired, life.AgeAt(date),
                _succession.AdulthoodAge, role, hasHeldHeadRole);
        }

        private bool TryMentorSpecialty(PersonId mentorId, PersonId learnerId, GameDate date,
            out MentorSpecialty specialty)
        {
            if (!IsEligibleEducationMentor(mentorId, learnerId, date))
            {
                specialty = MentorSpecialty.None;
                return false;
            }
            specialty = EducationScenarioProvider.SpecialtyOf(Succession, mentorId);
            return true;
        }

        private bool HasAptitude(PersonId personId) =>
            EducationScenarioProvider.HasAptitude(Succession, personId);

        private GrowthAttribute AptitudeOf(PersonId personId) =>
            EducationScenarioProvider.AptitudeOf(Succession, personId);

        private bool IsSupportiveEnvironment(PersonId personId) =>
            EducationScenarioProvider.IsSupportiveEnvironment(Succession, personId);

        private string LastIndustryChronicleSummary
        {
            get
            {
                IReadOnlyList<FamilyChronicleEntry> entries = EnsureHistoryLedger().Entries;
                for (int i = entries.Count - 1; i >= 0; i--)
                    if (entries[i].Category == ChronicleCategory.Industry)
                        return entries[i].Summary;
                return string.Empty;
            }
        }

        /// <summary>Captures the current runtime state as versioned save data.</summary>
        public SaveData CaptureSave()
        {
            EnsureRelationships();
            EnsureRandomState();
            EnsureEventRuntimeState();
            // A stable save point must remain saveable after a death, retirement or
            // departure. Cleanup is a total function: no production, no chronicle.
            EnsureIndustryAssignmentsValid();
            ReconcileDailyActions();
            GameDate date = WorldClock != null ? WorldClock.CurrentDate : SengokuChronicle.Domain.WorldClock.NewGameDate;

            return new SaveData
            {
                saveFormatVersion = SaveData.CurrentGameSchemaVersion,
                gameVersion = SaveData.CurrentGameVersion,
                rulesVersion = SaveData.CurrentRulesVersion,
                eventContentVersion = _eventCatalog != null
                    ? _eventCatalog.contentVersion
                    : SaveData.CurrentEventContentVersion,
                year = date.Year,
                month = date.Month,
                day = date.Day,
                locationId = CurrentLocation.Value,
                supplyShortageStage = (int)_supplyShortageMission.Stage,
                sealedLetterStage = (int)_sealedLetterMission.Stage,
                marketTallyStage = (int)_marketTallyMission.Stage,
                commission = _commission.Capture(),
                relationships = CaptureRelationships(),
                familyRelationships = CaptureFamilyRelationships(),
                randomState = _randomStateManager.CaptureState(),
                eventRuntime = _eventRuntimeState.Capture(),
                succession = SuccessionSaveData.Capture(Succession),
                chronicle = ChronicleSaveCodec.Capture(EnsureHistoryLedger()),
                industry = IndustryStateSaveData.Capture(EnsureIndustryState()),
                market = MarketSaveData.Capture(EnsureMarket()),
                development = CharacterDevelopmentSaveData.Capture(EnsureDevelopment()),
                research = FamilyResearchSaveData.Capture(EnsureResearch()),
                familyDoctrine = FamilyDoctrineSaveData.Capture(EnsureFamilyDoctrine()),
                activeEventScenarioId = _eventScenarioId ?? string.Empty,
                dailyActions = DailyActionStateSaveData.Capture(EnsureDailyActions()),
                npcAutonomy = NpcAutonomyStateSaveData.Capture(EnsureNpcAutonomy()),
                familyStanding = FamilyStandingSaveData.Capture(EnsureStanding()),
                injury = MissionInjurySaveData.Capture(EnsureInjuries()),
                missionHistory = CaptureMissionHistory(),
                campaign = CampaignProgressSaveData.Capture(EnsureCampaignProgress())
            };
        }

        /// <summary>
        /// Accepts the active content version and older additive versions. Future
        /// versions are rejected because this build cannot know their extra events.
        /// </summary>
        private bool IsCompatibleEventContentVersion(int savedVersion)
        {
            if (_eventCatalog == null || savedVersion <= 0)
                return false;
            return savedVersion <= _eventCatalog.contentVersion;
        }

        /// <summary>
        /// Validates and applies save data atomically: replacement state is built first,
        /// so a rejected load leaves the live session untouched. Clears transient dialogue state.
        /// </summary>
        public SaveOperationResult TryApplySave(SaveData data)
        {
            // A public apply is a load boundary too: replacing the campaign would drop the
            // session-only operation without a terminal cost or chronicle row.
            if (IsTacticalMissionActive)
                return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                    "潜入正在进行：无法读档。请先完成或撤退。");
            SaveOperationResult validation = SaveDataValidator.Validate(data);
            if (!validation.Succeeded)
                return validation;

            if (!RandomStateManager.TryRestore(data.randomState, out RandomStateManager randomState))
                return InvalidSave("存档随机流状态无效。");
            if (!GameDate.TryParse(data.year, data.month, data.day, out GameDate date))
                return InvalidSave("存档日期无效。");
            if (_eventEngine == null)
                return InvalidSave(string.IsNullOrEmpty(LastEventMessage) ? "事件目录不可用。" : LastEventMessage);
            if (_eventCatalog == null || !IsCompatibleEventContentVersion(data.eventContentVersion))
                return InvalidSave("存档事件内容版本与当前目录不匹配。");
            if (!SuccessionSaveData.TryRestore(data.succession, date,
                    out SuccessionState succession, out string successionError))
                return InvalidSave(string.IsNullOrEmpty(successionError) ? "存档继承状态无效。" : successionError);
            if (!IndustryStateSaveData.TryRestore(data.industry, data.succession, date,
                    out IndustryState industry, out string industryError))
                return InvalidSave(string.IsNullOrEmpty(industryError) ? "存档产业状态无效。" : industryError);
            if (data.market == null)
                return InvalidSave("存档缺少市场状态（market）。");
            if (!data.market.TryRestore(out MarketState market, out string marketError))
                return InvalidSave(string.IsNullOrEmpty(marketError) ? "存档市场状态无效。" : marketError);
            if (!CharacterDevelopmentSaveData.TryRestore(data.development, succession,
                    out CharacterDevelopmentState development, out string developmentError))
                return InvalidSave(string.IsNullOrEmpty(developmentError)
                    ? "存档人物成长状态无效。"
                    : developmentError);
            if (!FamilyResearchSaveData.TryRestore(data.research, date, data.succession,
                    out FamilyResearchState research, out string researchError))
                return InvalidSave(string.IsNullOrEmpty(researchError)
                    ? "存档家族研究状态无效。"
                    : researchError);
            if (!FamilyDoctrineSaveData.TryRestore(data.familyDoctrine, data.succession,
                    data.research.knownTechniqueIds,
                    out FamilyDoctrineState familyDoctrine, out string doctrineError))
                return InvalidSave(string.IsNullOrEmpty(doctrineError)
                    ? "存档家训与抱负状态无效。"
                    : doctrineError);
            if (!DailyActionStateSaveData.TryRestore(data.dailyActions, data.succession, date,
                    out DailyActionState dailyActions, out string dailyError))
                return InvalidSave(string.IsNullOrEmpty(dailyError)
                    ? "存档每日行动状态无效。"
                    : dailyError);
            if (!NpcAutonomyStateSaveData.TryRestore(data.npcAutonomy, data.succession, date,
                    out NpcAutonomyState npcAutonomy, out string npcError))
                return InvalidSave(string.IsNullOrEmpty(npcError)
                    ? "存档 NPC 自主状态无效。"
                    : npcError);
            if (data.familyStanding == null)
                return InvalidSave("存档缺少家族名望状态（familyStanding）。");
            if (!data.familyStanding.TryRestore(out FamilyStandingState standing,
                    out string standingError))
                return InvalidSave(string.IsNullOrEmpty(standingError)
                    ? "存档家族名望状态无效。"
                    : standingError);
            if (data.injury == null)
                return InvalidSave("存档缺少伤员状态（injury）。");
            if (!data.injury.TryRestore(out MissionInjuryState injuries,
                    out string injuryError))
                return InvalidSave(string.IsNullOrEmpty(injuryError)
                    ? "存档伤员状态无效。"
                    : injuryError);
            if (!TryRestoreMissionHistory(data.missionHistory, data.succession,
                    out MissionHistoryState missionHistory, out string historyError))
                return InvalidSave(string.IsNullOrEmpty(historyError)
                    ? "存档任务终局历史无效。"
                    : historyError);
            if (data.campaign == null)
                return InvalidSave("存档缺少家族纪元状态（campaign）。");
            if (!data.campaign.TryRestore(out CampaignProgress campaign,
                    out string campaignError))
                return InvalidSave(string.IsNullOrEmpty(campaignError)
                    ? "存档家族纪元状态无效。"
                    : campaignError);

            // Resolve and validate the saved route before the event runtime, so SC-024
            // history is accepted only in the matching sc024 route. SC-024 reuses the
            // M2 roster, so a tagged route is valid only for that fixed family.
            string eventScenarioId = data.activeEventScenarioId ?? string.Empty;
            if (eventScenarioId.Length > 0 && !M2FixedScenario.IsActive(succession))
                return InvalidSave("存档活动事件路线与当前家族不符。");

            if (!_eventEngine.TryRestoreState(data.eventRuntime, date,
                    BuildEventRuleContext(succession, eventScenarioId, campaign.OriginId),
                    out EventRuntimeState eventRuntime, out string eventError))
                return InvalidSave(string.IsNullOrEmpty(eventError) ? "存档事件状态无效。" : eventError);
            if (!ChronicleSaveCodec.TryRestore(data.chronicle,
                    out HistoryLedger historyLedger, out string chronicleError))
                return InvalidSave(string.IsNullOrEmpty(chronicleError) ? "存档编年史无效。" : chronicleError);

            if (!LocationId.TryParse(data.locationId, out LocationId location))
                return InvalidSave("存档地点无效。");

            SupplyShortageMission supplyShortage =
                SupplyShortageMission.TryRestore((SupplyShortageStage)data.supplyShortageStage);
            SealedLetterMission sealedLetter =
                SealedLetterMission.TryRestore((SealedLetterStage)data.sealedLetterStage);
            MarketTallyMission marketTally =
                MarketTallyMission.TryRestore((MarketTallyStage)data.marketTallyStage);
            if (supplyShortage == null || sealedLetter == null || marketTally == null)
                return InvalidSave("存档任务阶段无效。");
            if (data.commission == null)
                return InvalidSave("存档缺少 M05 护送委托状态（commission）。");
            ShadowEscortCommission commission = ShadowEscortCommission.TryRestore(
                (ShadowEscortStage)data.commission.stage, data.commission.assigneePersonId,
                data.commission.durationDays, data.commission.elapsedDays,
                data.commission.startYear, data.commission.startMonth, data.commission.startDay,
                (CommissionOutcome)data.commission.outcome, data.commission.score,
                data.commission.roll, data.commission.rewardFunds, data.commission.rewardSupplies,
                data.commission.investedFunds, data.commission.lastFeedback, data.commission.runId,
                true, (MissionOperationMode)data.commission.mode, out string commissionError);
            if (commission == null)
                return InvalidSave(string.IsNullOrEmpty(commissionError)
                    ? "存档 M05 护送委托状态无效。"
                    : commissionError);

            var relationships = new CharacterRelationships();
            if (data.relationships != null)
            {
                foreach (RelationshipSaveEntry entry in data.relationships)
                {
                    if (entry == null || !CharacterId.TryParse(entry.characterId, out CharacterId npcId))
                        return InvalidSave("存档关系数据无效。");

                    RelationshipResult changed = relationships.TryChange(npcId, entry.value);
                    if (!changed.Succeeded)
                        return InvalidSave(changed.Error);
                }
            }

            var familyRelationships = FamilyRelationships.CreateEmpty();
            var familyPairs = new HashSet<FamilyRelationshipKey>();
            if (data.familyRelationships != null)
            {
                foreach (FamilyRelationshipSaveEntry entry in data.familyRelationships)
                {
                    if (entry == null ||
                        !PersonId.TryParse(entry.headPersonId, out PersonId headId) || headId.IsEmpty ||
                        !PersonId.TryParse(entry.memberPersonId, out PersonId memberId) || memberId.IsEmpty ||
                        headId == memberId)
                        return InvalidSave("存档家庭关系数据无效。");
                    if (!succession.World.Family.IsMember(headId) ||
                        !succession.World.Family.IsMember(memberId))
                        return InvalidSave("存档家庭关系引用了非家庭成员。");
                    if (!familyPairs.Add(new FamilyRelationshipKey(headId, memberId)))
                        return InvalidSave("存档家庭关系人物对重复。");

                    FamilyRelationshipResult changed =
                        familyRelationships.TryChange(headId, memberId, entry.value);
                    if (!changed.Succeeded)
                        return InvalidSave(changed.Error);
                }
            }

            WorldClock = new WorldClock(date);
            CurrentLocation = location;
            _supplyShortageMission = supplyShortage;
            _sealedLetterMission = sealedLetter;
            _marketTallyMission = marketTally;
            _commission = commission;
            _relationships = relationships;
            _familyRelationships = familyRelationships;
            _randomStateManager = randomState;
            _eventRuntimeState = eventRuntime;
            _succession = succession;
            M2FixedScenario.SyncDesignation(_succession, eventScenarioId);
            _industryState = industry;
            _marketState = market;
            _lastMarketReport = null;
            _development = development;
            _researchState = research;
            _familyDoctrine = familyDoctrine;
            _dailyActions = dailyActions;
            _npcAutonomy = npcAutonomy;
            _standing = standing;
            _injuries = injuries;
            _missionHistory = missionHistory;
            _campaignProgress = campaign;
            _tacticalRequest = null;
            _eventScenarioId = eventScenarioId;
            // In-progress tactical state is session-only and never part of a save. A
            // successful load after a terminal result replaces the whole campaign, so the
            // finished operation display state is dropped with it.
            _tacticalMission = null;
            LastFamilyDoctrineMessage = string.Empty;
            LastCommissionMessage = string.Empty;
            LastTacticalMessage = string.Empty;
            TacticalAppliedFunds = 0;
            TacticalAppliedSupplies = 0;
            TacticalAppliedIntelligence = 0;
            LastIndustryReport = null;
            LastIndustryMessage = string.Empty;
            LastEducationMessage = string.Empty;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastFamilyFormationMessage = string.Empty;
            LastTravelBlockReason = string.Empty;
            LastCampaignReviewMessage = string.Empty;
            LastDailyActionOutcomes = new DailyActionResult[0];
            LastDailyActionMessage = string.Empty;
            _historyLedger = historyLedger;
            LastSuccessionMessage = string.Empty;
            NoticeRegencyHandoffIfDue();
            PlanNpcOnLoad();
            _dialogues.Clear();
            LastEventMessage = string.Empty;
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            SessionStateChanged?.Invoke();
            return SaveOperationResult.Success();
        }

        /// <summary>Saves the current session to the supplied single slot.</summary>
        public SaveOperationResult SaveTo(SaveSlotStore store)
        {
            if (IsTacticalMissionActive)
                return SaveOperationResult.Failure(SaveErrorCode.WriteFailed,
                    "潜入正在进行：无法存档。请先完成或撤退。");
            if (store == null)
                return SaveOperationResult.Failure(SaveErrorCode.WriteFailed, "存档槽为空。");

            return store.Save(CaptureSave());
        }

        /// <summary>Loads the supplied single slot and applies it atomically.</summary>
        public SaveOperationResult LoadFrom(SaveSlotStore store)
        {
            if (IsTacticalMissionActive)
                return SaveOperationResult.Failure(SaveErrorCode.ReadFailed,
                    "潜入正在进行：无法读档。请先完成或撤退。");
            if (store == null)
                return SaveOperationResult.Failure(SaveErrorCode.ReadFailed, "存档槽为空。");

            SaveLoadResult loaded = store.Load();
            if (!loaded.Succeeded)
                return SaveOperationResult.Failure(loaded.Error, loaded.Message);

            return TryApplySave(loaded.Data);
        }

        private RelationshipSaveEntry[] CaptureRelationships()
        {
            var entries = new List<RelationshipSaveEntry>();
            foreach (CharacterDefinition npc in CharacterCatalog.Npcs)
            {
                RelationshipResult result = _relationships.TryGet(npc.Id);
                if (result.Succeeded)
                    entries.Add(new RelationshipSaveEntry
                    {
                        characterId = npc.Id.Value,
                        value = result.CurrentValue
                    });
            }

            return entries.ToArray();
        }

        private FamilyRelationshipSaveEntry[] CaptureFamilyRelationships()
        {
            EnsureRelationships();
            IReadOnlyList<FamilyRelationshipEntry> source = _familyRelationships.Entries;
            var entries = new FamilyRelationshipSaveEntry[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                FamilyRelationshipEntry entry = source[i];
                entries[i] = new FamilyRelationshipSaveEntry
                {
                    headPersonId = entry.HeadId.Value,
                    memberPersonId = entry.MemberId.Value,
                    value = entry.Value
                };
            }
            return entries;
        }

        private static SaveOperationResult InvalidSave(string message)
        {
            return SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
        }

        public SealedLetterActionResult TryAcceptSealedLetterMission()
        {
            SealedLetterStage before = _sealedLetterMission.Stage;
            SealedLetterActionResult result = _sealedLetterMission.TryAccept(CurrentLocation);
            RecordMission(FamilyChronicleEntry.SealedLetterMissionId, "accept", "密信", (int)before, (int)result.Stage,
                result.Succeeded, result.Feedback);
            return result;
        }

        public SealedLetterActionResult TryDeliverSealedLetter()
        {
            SealedLetterStage before = _sealedLetterMission.Stage;
            SealedLetterActionResult result = _sealedLetterMission.TryDeliver(CurrentLocation);
            RecordMission(FamilyChronicleEntry.SealedLetterMissionId, "deliver", "密信", (int)before, (int)result.Stage,
                result.Succeeded, result.Feedback);
            return result;
        }

        public MarketTallyActionResult TryAcceptMarketTallyMission()
        {
            MarketTallyStage before = _marketTallyMission.Stage;
            MarketTallyActionResult result = _marketTallyMission.TryAccept(CurrentLocation);
            RecordMission(FamilyChronicleEntry.MarketTallyMissionId, "accept", "市集账目", (int)before, (int)result.Stage,
                result.Succeeded, result.Feedback);
            return result;
        }

        public MarketTallyActionResult TryInvestigateMarketTally()
        {
            MarketTallyStage before = _marketTallyMission.Stage;
            MarketTallyActionResult result = _marketTallyMission.TryInvestigate(CurrentLocation);
            RecordMission(FamilyChronicleEntry.MarketTallyMissionId, "investigate", "市集账目", (int)before,
                (int)result.Stage, result.Succeeded, result.Feedback);
            return result;
        }

        public MarketTallyActionResult TryReportMarketTally()
        {
            MarketTallyStage before = _marketTallyMission.Stage;
            MarketTallyActionResult result = _marketTallyMission.TryReport(CurrentLocation);
            RecordMission(FamilyChronicleEntry.MarketTallyMissionId, "report", "市集账目", (int)before, (int)result.Stage,
                result.Succeeded, result.Feedback);
            return result;
        }

        public SupplyShortageActionResult TryAcceptSupplyShortageMission()
        {
            SupplyShortageStage before = _supplyShortageMission.Stage;
            SupplyShortageActionResult result = _supplyShortageMission.TryAccept(CurrentLocation);
            RecordMission(FamilyChronicleEntry.SupplyShortageMissionId, "accept", "补给短缺", (int)before,
                (int)result.Stage, result.Succeeded, result.Feedback);
            return result;
        }

        public SupplyShortageActionResult TryInvestigateSupplyShortage()
        {
            SupplyShortageStage before = _supplyShortageMission.Stage;
            SupplyShortageActionResult result = _supplyShortageMission.TryInvestigate(CurrentLocation);
            RecordMission(FamilyChronicleEntry.SupplyShortageMissionId, "investigate", "补给短缺", (int)before,
                (int)result.Stage, result.Succeeded, result.Feedback);
            return result;
        }

        public SupplyShortageActionResult TryNegotiateSupplyShortage(CharacterId merchantId)
        {
            SupplyShortageStage before = _supplyShortageMission.Stage;
            SupplyShortageActionResult result =
                _supplyShortageMission.TryNegotiate(CurrentLocation, merchantId);
            RecordMission(FamilyChronicleEntry.SupplyShortageMissionId, "negotiate", "补给短缺", (int)before,
                (int)result.Stage, result.Succeeded, result.Feedback);
            return result;
        }

        public SupplyShortageActionResult TryReportSupplyShortage()
        {
            SupplyShortageStage before = _supplyShortageMission.Stage;
            SupplyShortageActionResult result = _supplyShortageMission.TryReport(CurrentLocation);
            RecordMission(FamilyChronicleEntry.SupplyShortageMissionId, "report", "补给短缺", (int)before,
                (int)result.Stage, result.Succeeded, result.Feedback);
            return result;
        }

        /// <summary>
        /// M05: accepts the single hardcoded escort commission at the clan seat and pays
        /// the creed-dependent investment out of the industry funds exactly once.
        /// </summary>
        public ShadowEscortActionResult TryAcceptCommission()
        {
            EnsureIndustryState();
            EnsureFamilyDoctrine();
            IndustryResources before = _industryState.Resources;
            ShadowEscortStage stageBefore = _commission.Stage;
            ShadowEscortCommission commissionBefore = _commission.Clone();
            ShadowEscortActionResult result = _commission.TryAccept(CurrentLocation,
                CurrentFamilyCreed, before, out IndustryResources after);
            LastCommissionMessage = result.Feedback;
            if (!result.Succeeded)
                return result;

            if (!_industryState.TrySetResources(after, out string error))
            {
                _commission = commissionBefore;
                LastCommissionMessage = string.IsNullOrEmpty(error) ? "委托预付资源无效。" : error;
                return new ShadowEscortActionResult(false, stageBefore, CommissionOutcome.None,
                    LastCommissionMessage);
            }

            RecordMission(FamilyChronicleEntry.ShadowEscortCommissionId, "accept", "护送委托",
                (int)stageBefore, (int)result.Stage, true, result.Feedback);
            return result;
        }

        /// <summary>
        /// M05: assigns one free family member and starts the timed execution. A person
        /// with a daily action, an industry assignment, or an unknown location is
        /// rejected, so the commission competes with existing duties instead of
        /// running beside them.
        /// </summary>
        public ShadowEscortActionResult TryAssignCommission(PersonId assigneeId)
        {
            EnsureSuccession();
            DailyActionState actions = EnsureDailyActions();
            EnsureIndustryState();
            ShadowEscortStage stageBefore = _commission.Stage;
            ShadowEscortCommission commissionBefore = _commission.Clone();
            CommissionAssigneeCheck check = CheckCommissionAssignee(assigneeId);
            ShadowEscortActionResult result;
            if (!check.Eligible)
            {
                result = new ShadowEscortActionResult(false, stageBefore, _commission.Outcome,
                    check.Message);
            }
            else
            {
                result = _commission.TryAssign(assigneeId, actions.HasActiveAction(assigneeId),
                    IsIndustryAssignee(assigneeId),
                    actions.TryGetLocation(assigneeId, out _), CurrentGameDate);
            }

            LastCommissionMessage = result.Feedback;
            if (!result.Succeeded)
                return result;

            if (!_commission.TryAssignRunId(CommissionRunId, out string runIdError))
            {
                _commission = commissionBefore;
                LastCommissionMessage = string.IsNullOrEmpty(runIdError)
                    ? "委托运行 ID 分配失败。"
                    : runIdError;
                return new ShadowEscortActionResult(false, stageBefore,
                    CommissionOutcome.None, LastCommissionMessage);
            }

            RecordMission(FamilyChronicleEntry.ShadowEscortCommissionId, "assign", "护送委托",
                (int)stageBefore, (int)result.Stage, true, result.Feedback);
            return result;
        }

        /// <summary>
        /// One person-level eligibility check shared by the read-only assignee list and
        /// <see cref="TryAssignCommission"/>. The full message is returned so a rejected
        /// command keeps its established feedback; the short reason labels a list row.
        ///
        /// New work follows the project work-duty rule
        /// (<see cref="IndustryService.CanWork"/>): a living current-family member must be
        /// an adult and not retired. That rule is enforced at the assignment boundary
        /// only. A commission that is already executing keeps running when its assignee
        /// later retires, so <see cref="CommissionAssigneeBlockReason"/> and
        /// <see cref="SengokuChronicle.Persistence.SaveDataValidator.ValidateCommissionReferences"/>
        /// stay mutually consistent and an in-progress save is never stranded.
        /// </summary>
        private CommissionAssigneeCheck CheckCommissionAssignee(PersonId personId)
        {
            EnsureSuccession();
            DailyActionState actions = EnsureDailyActions();
            EnsureIndustryState();
            if (_succession.World == null ||
                !_succession.World.TryGetPerson(personId, out _) ||
                !_succession.World.IsLiving(personId) ||
                !_succession.World.Family.IsMember(personId))
            {
                return new CommissionAssigneeCheck(false, "已离世或离开家族",
                    "必须指定一名在世的家族成员。");
            }
            if (IsTacticalOperative(personId))
            {
                return new CommissionAssigneeCheck(false, "正在执行潜入密令",
                    "该人物正在执行潜入密令：请先完成或撤退，再指派护送委托。");
            }
            if (EnsureInjuries().IsInjured(personId))
            {
                return new CommissionAssigneeCheck(false, "负伤休养",
                    "该人物负伤休养：下一次月结前不能参加体力或潜入任务。");
            }
            // Reuse the shared duty rule instead of a second age/retirement copy, so long
            // as the preview label and the command feedback read from the same reason.
            if (!IndustryService.CanWork(_succession, personId, CurrentGameDate,
                    out string workReason))
            {
                string label = workReason.TrimEnd('。');
                return new CommissionAssigneeCheck(false, label,
                    "该人物" + label + "，无法执行护送委托。");
            }
            if (IsResearchResearcher(personId))
            {
                return new CommissionAssigneeCheck(false, "负责家族研究",
                    "该人物正在负责家族研究：请先更换研究者，再指派委托。");
            }
            if (IsEducationMentor(personId))
            {
                return new CommissionAssigneeCheck(false, "担任培养导师",
                    "该人物正在担任培养导师：请先改派导师，再指派委托。");
            }
            if (actions.HasActiveAction(personId))
            {
                return new CommissionAssigneeCheck(false, "有进行中的每日行动",
                    "该人物有进行中的每日行动：请先完成或中断，再指派委托。");
            }
            if (IsIndustryAssignee(personId))
            {
                return new CommissionAssigneeCheck(false, "负责产业",
                    "该人物正在负责产业：请先改派产业负责人，再指派委托。");
            }
            if (!actions.TryGetLocation(personId, out _))
            {
                return new CommissionAssigneeCheck(false, "在旅途中",
                    "该人物正在旅途中或地点未知，无法立刻开始委托。");
            }
            return new CommissionAssigneeCheck(true, "可指派", string.Empty);
        }

        private readonly struct CommissionAssigneeCheck
        {
            public CommissionAssigneeCheck(bool eligible, string reason, string message)
            {
                Eligible = eligible;
                Reason = reason ?? string.Empty;
                Message = message ?? string.Empty;
            }

            public bool Eligible { get; }
            public string Reason { get; }
            public string Message { get; }
        }

        /// <summary>
        /// Returns why an in-progress assignee cannot execute, or null when eligible.
        /// This is the same rule SaveDataValidator applies to an in-progress row. It is
        /// evaluated at explicit state-transition boundaries (day/month advancement), not
        /// during capture, so a snapshot never mutates gameplay state.
        ///
        /// Work-duty eligibility (adult, not retired) is deliberately absent from this
        /// day-step rule: retirement alone does not interrupt the trip, and the save
        /// validator mirrors that choice, so an in-progress row stays writable and
        /// loadable. Settlement is where unavailability resolves: an assignee who can no
        /// longer work is closed once through the approved Abandoned semantics instead of
        /// failing every retry.
        /// </summary>
        private string CommissionAssigneeBlockReason()
        {
            EnsureSuccession();
            DailyActionState actions = EnsureDailyActions();
            EnsureIndustryState();
            PersonId assignee = _commission.AssigneeId;
            if (assignee.IsEmpty)
                return "委托记录缺少执行人，委托失败。";
            if (_succession.World == null ||
                !_succession.World.TryGetPerson(assignee, out _) ||
                !_succession.World.IsLiving(assignee) ||
                !_succession.World.Family.IsMember(assignee))
                return CommissionAbandonedByDeathReason;
            if (actions.HasActiveAction(assignee))
                return "委托执行人同时被安排了每日行动，委托失败。";
            if (IsIndustryAssignee(assignee))
                return "委托执行人同时担任产业职务，委托失败。";
            if (IsResearchResearcher(assignee))
                return "委托执行人同时负责家族研究，委托失败。";
            if (IsEducationMentor(assignee))
                return "委托执行人同时担任培养导师，委托失败。";
            return null;
        }

        /// <summary>True when the person currently leads the family research project.</summary>
        private bool IsResearchResearcher(PersonId personId)
        {
            EnsureResearch();
            return _researchState.HasPlan && _researchState.ResearcherId == personId;
        }

        /// <summary>True when the person mentors any unsettled annual education plan.</summary>
        private bool IsEducationMentor(PersonId personId)
        {
            CharacterDevelopmentState development = EnsureDevelopment();
            foreach (PersonId learner in development.PersonIds)
                foreach (int ageYear in development.PlannedAgeYears(learner))
                    if (!development.HasResult(learner, ageYear) &&
                        development.TryGetPlan(learner, ageYear, out EducationYearPlan plan) &&
                        !plan.IsFreeGrowth && plan.Mentor == personId)
                        return true;
            return false;
        }

        /// <summary>True when the person is the named assignee of any industry line.</summary>
        private bool IsIndustryAssignee(PersonId personId)
        {
            IndustryState industry = EnsureIndustryState();
            for (int i = 0; i < industry.Assignments.Count; i++)
                if (industry.Assignments[i].PersonId == personId)
                    return true;
            return false;
        }

        /// <summary>
        /// Advances the commission by one simulated day. When the window closes it uses
        /// the frozen operation mode: manual leaves the due row for the explicit settle
        /// button, delegated settles once here. Both call the same M07 settlement.
        /// </summary>
        private bool AdvanceCommissionDay(GameDate simulatedDate)
        {
            if (_commission.Stage != ShadowEscortStage.InProgress)
                return false;

            string block = CommissionAssigneeBlockReason();
            if (block != null)
            {
                // An explicit day/month transition is the boundary that closes an
                // unexecutable commission; capture never does this. Abandonment is not a
                // shared M07 result: it keeps its action chronicle row and writes no
                // terminal history, no standing effect and no reward.
                return TryAbandonCommission(simulatedDate, block);
            }

            if (!_commission.AdvanceOneDay(out bool due))
                return false;
            if (!due)
                return true; // the elapsed day changed, so countdown observers must refresh

            if (_commission.Mode == MissionOperationMode.Manual)
            {
                LastCommissionMessage = "委托已到期：请点击「结算委托」完成手动结算。";
                return true;
            }

            MissionSettlementResult settlement =
                SettleCommission(simulatedDate, _commission.Mode);
            if (!settlement.Succeeded)
            {
                LastCommissionMessage = settlement.Message;
                return false;
            }
            return true;
        }

        // ---- M06 tactical infiltration -------------------------------------------------

        /// <summary>
        /// Opens the single M06 operation with exactly two eligible family members. The
        /// announced supply cost is not spent here: it is applied once with the terminal
        /// result, so an abandoned session costs the saved campaign nothing.
        /// </summary>
        public TacticalActionResult TryBeginTacticalMission(PersonId first, PersonId second) =>
            TryBeginTacticalMission(first, second, first, MissionPreparation.None);

        /// <summary>
        /// M07 manual entry: the captain and preparation choice are frozen into the
        /// briefing request and the tactical board then decides the terminal result.
        /// </summary>
        public TacticalActionResult TryBeginTacticalMission(PersonId first, PersonId second,
            PersonId captain, MissionPreparation preparation)
        {
            string startError = ValidateTacticalStart(first, second, captain, preparation);
            if (!string.IsNullOrEmpty(startError))
                return TacticalReject(startError);

            PersonId[] ordered = OrderTacticalOperatives(first, second);
            PersonId orderedCaptain = captain.IsEmpty ? ordered[0] : captain;
            if (!TacticalMissionState.TryCreate(ordered[0], ordered[1], orderedCaptain,
                    preparation, CurrentGameDate, out TacticalMissionState state,
                    out string error))
                return TacticalReject(string.IsNullOrEmpty(error) ? "潜入无法开始。" : error);

            EnsureDevelopment();
            if (!_development.TryGetValues(ordered[0], out CharacterGrowthValues firstValues) ||
                !_development.TryGetValues(ordered[1], out CharacterGrowthValues secondValues))
                return TacticalReject("出发成员缺少成长记录，无法冻结任务简报。");
            var participants = new[] { ordered[0], ordered[1] };
            if (!MissionRequest.TryCreate(TacticalRunId,
                    FamilyChronicleEntry.TacticalMissionId, MissionKind.TacticalInfiltration,
                    MissionOperationMode.Manual, preparation, CurrentGameDate, orderedCaptain,
                    participants, participants, firstValues.Ninjutsu, secondValues.Ninjutsu,
                    FrozenMissionIntelligence(),
                    TacticalMissionResolutionRules.Difficulty,
                    TacticalMissionResolutionRules.BaseSupplyCost,
                    TacticalMissionResolutionRules.PreparationSupplyCost(preparation),
                    0, 0, out MissionRequest request, out string requestError))
                return TacticalReject(string.IsNullOrEmpty(requestError)
                    ? "潜入简报无法冻结。"
                    : requestError);

            _tacticalMission = state;
            _tacticalRequest = request;
            TacticalAppliedFunds = 0;
            TacticalAppliedSupplies = 0;
            TacticalAppliedIntelligence = 0;
            LastTacticalMessage = state.Feedback;
            SessionStateChanged?.Invoke();
            return new TacticalActionResult(true, state.Stage, state.Outcome, state.Feedback,
                state.Turn, state.LastEvent);
        }

        public TacticalActionResult TryTacticalMove(PersonId operativeId,
            TacticalDirection direction) =>
            RunTacticalCommand(state => state.TryMove(operativeId, direction));

        public TacticalActionResult TryTacticalSprint(PersonId operativeId,
            TacticalDirection direction) =>
            RunTacticalCommand(state => state.TrySprint(operativeId, direction));

        public TacticalActionResult TryTacticalWait(PersonId operativeId) =>
            RunTacticalCommand(state => state.TryWait(operativeId));

        public TacticalActionResult TryTacticalInteract(PersonId operativeId) =>
            RunTacticalCommand(state => state.TryInteract(operativeId));

        public TacticalActionResult TryTacticalExtract() =>
            RunTacticalCommand(state => state.TryExtract());

        public TacticalActionResult TryTacticalWithdraw() =>
            RunTacticalCommand(state => state.TryWithdraw());

        private TacticalActionResult RunTacticalCommand(
            Func<TacticalMissionState, TacticalActionResult> command)
        {
            if (_tacticalMission == null)
                return TacticalReject("尚未开始潜入。");
            if (!_tacticalMission.IsActive)
                return TacticalReject("潜入已经结束。");

            TacticalActionResult result = command(_tacticalMission);
            LastTacticalMessage = result.Feedback;
            if (_tacticalMission.IsTerminal && !CommitTacticalOutcome())
                return new TacticalActionResult(false, _tacticalMission.Stage,
                    _tacticalMission.Outcome, LastTacticalMessage, _tacticalMission.Turn,
                    _tacticalMission.LastEvent);

            SessionStateChanged?.Invoke();
            return result;
        }

        /// <summary>
        /// Applies the one terminal settlement for manual play: maps the tactical result
        /// to the shared M07 result, charges base plus selected preparation cost, applies
        /// the disclosed death checks and commits resources, standing, life, one chronicle
        /// row and one history row in a single transaction.
        /// </summary>
        private bool CommitTacticalOutcome()
        {
            if (_tacticalMission == null)
            {
                RevertTerminal("潜入状态缺失。");
                return false;
            }

            if (!TacticalMissionResolutionRules.TryMapManual(_tacticalMission.Outcome,
                    _tacticalMission.AlertTriggered, out SharedMissionOutcome outcome,
                    out string mapError))
            {
                RevertTerminal(mapError);
                return false;
            }

            MissionRequest request = _tacticalRequest;
            if (request == null)
            {
                EnsureDevelopment();
                PersonId first = _tacticalMission.OperativeId(0);
                PersonId second = _tacticalMission.OperativeId(1);
                if (!_development.TryGetValues(first, out CharacterGrowthValues firstValues) ||
                    !_development.TryGetValues(second, out CharacterGrowthValues secondValues))
                {
                    RevertTerminal("出发成员缺少成长记录。");
                    return false;
                }
                var participants = new[] { first, second };
                if (!MissionRequest.TryCreate(TacticalRunId,
                        FamilyChronicleEntry.TacticalMissionId, MissionKind.TacticalInfiltration,
                        MissionOperationMode.Manual, _tacticalMission.Preparation,
                        _tacticalMission.LaunchDate, _tacticalMission.CaptainId, participants,
                        participants, firstValues.Ninjutsu, secondValues.Ninjutsu,
                        FrozenMissionIntelligence(),
                        TacticalMissionResolutionRules.Difficulty,
                        TacticalMissionResolutionRules.BaseSupplyCost,
                        TacticalMissionResolutionRules.PreparationSupplyCost(
                            _tacticalMission.Preparation), 0, 0,
                        out MissionRequest rebuilt, out string requestError))
                {
                    RevertTerminal(requestError);
                    return false;
                }
                request = rebuilt;
            }

            int rewardFunds = IsTacticalSuccess(outcome)
                ? TacticalMissionResolutionRules.SuccessFunds
                : 0;
            int rewardIntelligence = IsTacticalSuccess(outcome)
                ? TacticalMissionResolutionRules.SuccessIntelligence
                : 0;
            if (!TryCommitMissionSettlement(request, outcome, rewardFunds, 0, rewardIntelligence,
                    _tacticalMission.LaunchDate,
                    TacticalOutcomeActionId(_tacticalMission.Outcome),
                    (int)TacticalMissionStage.InProgress, TacticalChronicleStageFor(outcome),
                    out MissionRecord record, out string commitError))
            {
                RevertTerminal(commitError);
                return false;
            }

            TacticalAppliedFunds = record.FundsDelta;
            TacticalAppliedSupplies = record.SuppliesDelta;
            TacticalAppliedIntelligence = record.IntelligenceDelta;
            LastTacticalMessage = "潜入结算（" + MissionOutcomeTable.OutcomeName(outcome) +
                                  "）：" + record.FameBefore + "→" + record.FameAfter +
                                  " 名望，" + record.ConcealmentBefore + "→" +
                                  record.ConcealmentAfter + " 隐蔽度；资金 " +
                                  Signed(record.FundsDelta) + "、物资 " +
                                  Signed(record.SuppliesDelta) + "、情报 " +
                                  Signed(record.IntelligenceDelta) + "。";
            return true;
        }

        private void RevertTerminal(string message)
        {
            _tacticalMission?.RevertTerminalResultForRetry();
            LastTacticalMessage = string.IsNullOrEmpty(message) ? "潜入结果结算失败。" : message;
        }

        /// <summary>
        /// True when the person is one of the two operatives of the running operation.
        /// Campaign duties that would claim the same person are refused while active.
        /// </summary>
        private bool IsTacticalOperative(PersonId personId) =>
            IsTacticalMissionActive && _tacticalMission != null &&
            _tacticalMission.IsOperative(personId);

        /// <summary>
        /// The campaign calendar must not advance during the operation. The tactical page
        /// is not a modal screen, so the application boundary refuses day, month and
        /// travel commands until the operation reaches a terminal result.
        /// </summary>
        private bool RejectWhileTacticalMissionActive()
        {
            if (!IsTacticalMissionActive)
                return false;
            LastTacticalMessage = "潜入正在进行：请先完成或撤退。";
            LastEventMessage = LastTacticalMessage;
            return true;
        }

        private TacticalActionResult TacticalReject(string message)
        {
            LastTacticalMessage = message;
            SessionStateChanged?.Invoke();
            return new TacticalActionResult(false,
                _tacticalMission != null ? _tacticalMission.Stage : TacticalMissionStage.NotStarted,
                _tacticalMission != null ? _tacticalMission.Outcome : TacticalMissionOutcome.None,
                message,
                _tacticalMission != null ? _tacticalMission.Turn : 0,
                _tacticalMission != null ? _tacticalMission.LastEvent : string.Empty);
        }

        /// <summary>
        /// One person-level gate shared by the read-only list and the start command. It
        /// reuses the project work-duty rule (adult, not retired) and the existing
        /// location record, and additionally requires the person to be at 清洲.
        /// </summary>
        private CommissionAssigneeCheck CheckTacticalOperative(PersonId personId)
        {
            EnsureSuccession();
            DailyActionState actions = EnsureDailyActions();
            EnsureIndustryState();
            if (_succession.World == null ||
                !_succession.World.TryGetPerson(personId, out _) ||
                !_succession.World.IsLiving(personId) ||
                !_succession.World.Family.IsMember(personId))
            {
                return new CommissionAssigneeCheck(false, "已离世或离开家族",
                    "必须选择两名在世的家族成员。");
            }
            if (_commission.Stage == ShadowEscortStage.InProgress &&
                _commission.AssigneeId == personId)
            {
                return new CommissionAssigneeCheck(false, "正在执行护送委托",
                    "该人物正在执行护送委托：请等待结算，再参加潜入。");
            }
            if (EnsureInjuries().IsInjured(personId))
            {
                return new CommissionAssigneeCheck(false, "负伤休养",
                    "该人物负伤休养：下一次月结前不能参加体力或潜入任务。");
            }
            if (!IndustryService.CanWork(_succession, personId, CurrentGameDate,
                    out string workReason))
            {
                string label = workReason.TrimEnd('。');
                return new CommissionAssigneeCheck(false, label,
                    "该人物" + label + "，无法参加潜入。");
            }
            if (IsResearchResearcher(personId))
            {
                return new CommissionAssigneeCheck(false, "负责家族研究",
                    "该人物正在负责家族研究：请先更换研究者，再参加潜入。");
            }
            if (IsEducationMentor(personId))
            {
                return new CommissionAssigneeCheck(false, "担任培养导师",
                    "该人物正在担任培养导师：请先改派导师，再参加潜入。");
            }
            if (actions.HasActiveAction(personId))
            {
                return new CommissionAssigneeCheck(false, "有进行中的每日行动",
                    "该人物有进行中的每日行动：请先完成或中断，再参加潜入。");
            }
            if (IsIndustryAssignee(personId))
            {
                return new CommissionAssigneeCheck(false, "负责产业",
                    "该人物正在负责产业：请先改派产业负责人，再参加潜入。");
            }
            if (!actions.TryGetLocation(personId, out LocationId location))
            {
                return new CommissionAssigneeCheck(false, "在旅途中",
                    "该人物正在旅途中或地点未知，无法立刻参加潜入。");
            }
            if (location != LocationId.Kiyosu)
            {
                return new CommissionAssigneeCheck(false, "不在清洲",
                    "该人物不在清洲：请先在清洲集合，再开始潜入。");
            }
            return new CommissionAssigneeCheck(true, "可参加", string.Empty);
        }

        private int CountEligibleTacticalOperatives()
        {
            EnsureSuccession();
            if (_succession.World == null)
                return 0;
            int count = 0;
            foreach (FamilyMembership membership in _succession.World.Family.Members)
            {
                if (!_succession.World.IsLiving(membership.PersonId))
                    continue;
                if (CheckTacticalOperative(membership.PersonId).Eligible)
                    count++;
            }
            return count;
        }

        /// <summary>Orders the two chosen operatives by stable family roster position.</summary>
        private PersonId[] OrderTacticalOperatives(PersonId first, PersonId second)
        {
            EnsureSuccession();
            var ordered = new List<PersonId>(2);
            if (_succession.World != null)
            {
                foreach (FamilyMembership membership in _succession.World.Family.Members)
                {
                    if (membership.PersonId == first || membership.PersonId == second)
                        ordered.Add(membership.PersonId);
                    if (ordered.Count == 2)
                        break;
                }
            }
            if (ordered.Count != 2)
            {
                ordered.Clear();
                ordered.Add(first);
                ordered.Add(second);
            }
            return ordered.ToArray();
        }

        private FamilyChronicleEntry FindTacticalMissionRecord()
        {
            IReadOnlyList<FamilyChronicleEntry> entries = EnsureHistoryLedger().Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                FamilyChronicleEntry entry = entries[i];
                if (entry.Category == ChronicleCategory.Mission &&
                    string.Equals(entry.SubjectId, FamilyChronicleEntry.TacticalMissionId,
                        StringComparison.Ordinal))
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Stable chronicle action ID for a manual or delegated terminal result. Both
        /// entry points resolve through this one mapping, so equal frozen input writes
        /// equal chronicle data.
        /// </summary>
        private static string TacticalOutcomeActionId(TacticalMissionOutcome outcome)
        {
            switch (outcome)
            {
                case TacticalMissionOutcome.Succeeded: return "succeeded";
                case TacticalMissionOutcome.Failed: return "failed";
                default: return "withdrawn";
            }
        }

        private static string Signed(int value) =>
            (value >= 0 ? "+" : string.Empty) + value.ToString(CultureInfo.InvariantCulture);

        /// <summary>Reads the player's relationship with a known NPC.</summary>
        public RelationshipResult TryGetRelationship(CharacterId npcId)
        {
            EnsureRelationships();
            return _relationships.TryGet(npcId);
        }

        /// <summary>Applies an integer delta to one NPC relationship, clamped to [-100, 100].</summary>
        public RelationshipResult TryChangeRelationship(CharacterId npcId, int delta)
        {
            EnsureRelationships();
            RelationshipResult result = _relationships.TryChange(npcId, delta);
            if (RecordRelationshipChange(result, "direct"))
                SessionStateChanged?.Invoke();
            return result;
        }

        /// <summary>Reads the value of the current head and member pair.</summary>
        public FamilyRelationshipResult TryGetFamilyRelationship(PersonId memberId) =>
            TryGetFamilyRelationship(Succession != null ? Succession.HeadPersonId : default, memberId);

        /// <summary>Reads one explicit (head, member) pair value; used by tests and history review.</summary>
        public FamilyRelationshipResult TryGetFamilyRelationship(PersonId headId, PersonId memberId)
        {
            EnsureRelationships();
            return _familyRelationships.TryGet(headId, memberId);
        }

        /// <summary>Applies a delta to the current head and member pair value.</summary>
        public FamilyRelationshipResult TryChangeFamilyRelationship(PersonId memberId, int delta) =>
            TryChangeFamilyRelationship(Succession != null ? Succession.HeadPersonId : default, memberId, delta);

        /// <summary>
        /// Applies a delta to one explicit (head, member) pair. The head must be the
        /// current head, both people must belong to the active family, and the member
        /// may not be the head. Historical pairs remain readable but are never
        /// rewritten by a later head.
        /// </summary>
        public FamilyRelationshipResult TryChangeFamilyRelationship(PersonId headId, PersonId memberId, int delta)
        {
            EnsureRelationships();
            SuccessionState succession = Succession;
            if (succession == null || succession.World == null ||
                headId != succession.HeadPersonId)
                return new FamilyRelationshipResult(headId, memberId, 0, 0, false,
                    "家庭关系只能由当前家主变更。");
            if (!succession.World.Family.IsMember(headId) || !succession.World.Family.IsMember(memberId))
                return new FamilyRelationshipResult(headId, memberId, 0, 0, false,
                    "家庭关系目标不是当前家庭成员。");
            if (headId == memberId)
                return new FamilyRelationshipResult(headId, memberId, 0, 0, false,
                    "家庭关系不能指向家主本人。");

            FamilyRelationshipResult result = _familyRelationships.TryChange(headId, memberId, delta);
            if (RecordFamilyRelationshipChange(result, "direct"))
                SessionStateChanged?.Invoke();
            return result;
        }

        /// <summary>
        /// Appends one relationship record only when the change really happened.
        /// Callers pass the applied <see cref="RelationshipResult"/> so the record shows
        /// the exact clamped previous/current pair. Returns true for every real value
        /// change, even if the separate bookkeeping append itself failed.
        /// </summary>
        private bool RecordRelationshipChange(RelationshipResult result, string channel)
        {
            if (!result.Succeeded || result.PreviousValue == result.CurrentValue)
                return false;

            GameDate date = CurrentGameDate;
            string name = CharacterCatalog.TryGet(result.NpcId, out CharacterDefinition definition)
                ? definition.DisplayName
                : result.NpcId.Value;
            if (!EnsureHistoryLedger().TryAppendRelationship(date.Year, date.Month,
                    result.NpcId.Value, channel,
                    result.PreviousValue, result.CurrentValue, name,
                    "与" + name + "的关系 " + result.PreviousValue.ToString(CultureInfo.InvariantCulture) +
                    " → " + result.CurrentValue.ToString(CultureInfo.InvariantCulture) +
                    "（" + RelationshipChannelName(channel) + "）。",
                    out _, out string error))
                ReportChronicleAppendFailure("关系", error);

            // The relationship value already changed, so callers must still refresh
            // observers even when the separate chronicle bookkeeping append failed.
            return true;
        }

        /// <summary>
        /// Appends one person-pair family relationship record with both display names
        /// and the exact before/after values. Raw person IDs never become the title.
        /// </summary>
        private bool RecordFamilyRelationshipChange(FamilyRelationshipResult result, string channel)
        {
            if (!result.Succeeded || result.PreviousValue == result.CurrentValue)
                return false;

            GameDate date = CurrentGameDate;
            string headName = PersonDisplayName(result.HeadId, "家主");
            string memberName = PersonDisplayName(result.MemberId, "家人");
            // Key order is (head, member); the summary below carries the numeric change.
            string title = headName + " 与 " + memberName;
            string summary = "家主" + headName + "与" + memberName + "的关系 " +
                             result.PreviousValue.ToString(CultureInfo.InvariantCulture) + " → " +
                             result.CurrentValue.ToString(CultureInfo.InvariantCulture) +
                             "（" + RelationshipChannelName(channel) + "）。";
            if (!EnsureHistoryLedger().TryAppendRelationship(date.Year, date.Month,
                    result.HeadId.Value + "->" + result.MemberId.Value, channel,
                    result.PreviousValue, result.CurrentValue, title, summary,
                    out _, out string error))
                ReportChronicleAppendFailure("家庭关系", error);

            return true;
        }

        private string PersonDisplayName(PersonId personId, string fallback)
        {
            SuccessionState succession = Succession;
            return succession != null && succession.World != null &&
                   succession.World.TryGetPerson(personId, out PersonState person) &&
                   !string.IsNullOrWhiteSpace(person.DisplayName)
                ? person.DisplayName
                : fallback;
        }

        /// <summary>
        /// Appends one mission record only when the action succeeded and actually moved
        /// the stage. Rejected actions and repeats stay silent; a real change refreshes
        /// the session once so an open chronicle panel updates.
        /// </summary>
        private void RecordMission(string missionId, string actionId, string missionName,
            int beforeStage, int afterStage, bool succeeded, string feedback) =>
            RecordMission(CurrentGameDate, missionId, actionId, missionName, beforeStage,
                afterStage, succeeded, feedback);

        /// <summary>
        /// Appends one mission record for an explicit simulated date. Commission
        /// settlement inside a month turn must use the day the window actually closed,
        /// not the next month's first day still held by the clock at commit time.
        /// </summary>
        private void RecordMission(GameDate date, string missionId, string actionId,
            string missionName, int beforeStage, int afterStage, bool succeeded, string feedback)
        {
            if (!succeeded || beforeStage == afterStage)
                return;

            // The chronicle stores year/month only. Commission records get the exact
            // simulated day appended so a month-turn settlement can be audited to its
            // due date; every other mission summary keeps its original text byte-for-byte.
            string summary = feedback ?? string.Empty;
            if (string.Equals(missionId, FamilyChronicleEntry.ShadowEscortCommissionId,
                    StringComparison.Ordinal))
                summary += "（" + date.Year.ToString(CultureInfo.InvariantCulture) + "-" +
                           date.Month.ToString("00", CultureInfo.InvariantCulture) + "-" +
                           date.Day.ToString("00", CultureInfo.InvariantCulture) + "）";
            if (!EnsureHistoryLedger().TryAppendMission(date.Year, date.Month, missionId, actionId,
                    beforeStage, afterStage, missionName, summary, out _, out string error))
                ReportChronicleAppendFailure("任务", error);
            SessionStateChanged?.Invoke();
        }

        private void RecordEventChoice(EventChoiceResult result)
        {
            GameDate date = CurrentGameDate;
            string title = _eventEngine != null &&
                           _eventEngine.TryGetEventText(result.EventId, out EventTextEntry text)
                ? text.title
                : result.EventId;
            if (!EnsureHistoryLedger().TryAppendEvent(date.Year, date.Month, result.EventId,
                    result.OptionId, title, result.Feedback, out _, out string error))
                ReportChronicleAppendFailure("事件", error);
        }

        /// <summary>
        /// Chronicle appends are bookkeeping outside the gameplay transaction, so a
        /// rejected append must not throw and break play. It must still be observable
        /// in every build instead of being swallowed by a discarded false result.
        /// </summary>
        private static void ReportChronicleAppendFailure(string category, string error)
        {
            Debug.LogError("家族编年史记录写入失败（" + category + "）：" +
                           (string.IsNullOrEmpty(error) ? "未知原因" : error));
        }

        private void RecordSuccession(in SuccessionSnapshot before, SuccessionResult result)
        {
            GameDate date = CurrentGameDate;
            PersonId subject = result.Subject.IsEmpty ? _succession.HeadPersonId : result.Subject;
            string subjectId = subject.Value;
            // The completed result ID only belongs to this record when this command
            // actually produced a new one. Reusing a stale value from an earlier
            // succession would misattribute a later retirement.
            string sourceId = !string.Equals(before.CompletedResultId,
                _succession.CompletedResultId ?? string.Empty, StringComparison.Ordinal)
                ? _succession.CompletedResultId
                : result.Outcome.ToString();
            string subjectName = _succession.World.TryGetPerson(subject, out PersonState person)
                ? person.DisplayName
                : subjectId;
            if (!EnsureHistoryLedger().TryAppendSuccession(date.Year, date.Month, subjectId, sourceId,
                    (int)before.Phase, (int)_succession.Phase, subjectName, result.Message,
                    before.HeadId.Value, _succession.HeadPersonId.Value,
                    out _, out string error))
                ReportChronicleAppendFailure("继承", error);
        }

        private static string RelationshipChannelName(string channel)
        {
            switch (channel)
            {
                case "dialogue": return "对话";
                case "event": return "事件";
                default: return "直接行动";
            }
        }

        private HistoryLedger EnsureHistoryLedger()
        {
            if (_historyLedger == null)
                _historyLedger = HistoryLedger.CreateEmpty();
            return _historyLedger;
        }

        private void EnsureRelationships()
        {
            if (_relationships == null)
                _relationships = new CharacterRelationships();
            if (_familyRelationships == null)
                _familyRelationships = FamilyRelationships.CreateEmpty();
        }

        private void EnsureRandomState()
        {
            if (_randomStateManager == null)
                _randomStateManager = RandomStateManager.Create(DefaultRandomSeed);
        }

        /// <summary>
        /// Restores a captured random state only when the snapshot is valid. A rejected
        /// snapshot must never replace the live manager with a null out value.
        /// </summary>
        private bool TryRestoreRandomState(RandomStateSaveData snapshot)
        {
            if (!RandomStateManager.TryRestore(snapshot, out RandomStateManager restored))
                return false;
            _randomStateManager = restored;
            return true;
        }

        /// <summary>
        /// Restores a captured chronicle list only when every snapshot row is valid, so a
        /// rejected snapshot cannot drop the live ledger.
        /// </summary>
        private bool TryRestoreHistoryLedger(FamilyChronicleEntry[] snapshot)
        {
            if (!HistoryLedger.TryRestore(snapshot, out HistoryLedger restored, out _))
                return false;
            _historyLedger = restored;
            return true;
        }

        private EventRuntimeState EnsureEventRuntimeState()
        {
            if (_eventRuntimeState == null)
            {
                _eventRuntimeState = new EventRuntimeState();
                LoadEventContent();
            }
            return _eventRuntimeState;
        }

        private void LoadEventContent()
        {
            _eventCatalog = null;
            _eventTextCatalog = null;
            _eventEngine = null;
            TextAsset catalogAsset = Resources.Load<TextAsset>("Events/event-catalog-v1");
            TextAsset textAsset = Resources.Load<TextAsset>("Localization/event-text-zh-v1");
            if (catalogAsset == null || textAsset == null)
            {
                LastEventMessage = "事件目录或本地化文字未加载。";
                return;
            }
            try
            {
                _eventCatalog = JsonUtility.FromJson<EventCatalogData>(catalogAsset.text);
                _eventTextCatalog = JsonUtility.FromJson<EventTextCatalog>(textAsset.text);
            }
            catch (Exception exception)
            {
                LastEventMessage = "事件 JSON 无法解析：" + exception.Message;
                _eventCatalog = null;
                _eventTextCatalog = null;
                return;
            }

            _eventEngine = new EventEngine(_eventCatalog, _eventTextCatalog);
            LastEventMessage = _eventEngine.IsReady ? string.Empty : _eventEngine.ValidationMessage;
        }

        public bool TryGetEventOptionAvailability(string eventId, string optionId, out bool enabled, out string reason)
        {
            EnsureEventRuntimeState();
            if (_eventEngine == null)
            {
                enabled = false;
                reason = LastEventMessage;
                return false;
            }
            EnsureRelationships();
            return _eventEngine.TryGetOptionAvailability(eventId, optionId, _relationships,
                BuildEventRuleContext(), _familyRelationships, EnsureFamilyDoctrine(),
                out enabled, out reason);
        }

        public EventChoiceResult TryChooseEventOption(string eventId, string optionId, string committedResultId)
        {
            EnsureEventRuntimeState();
            if (_eventEngine == null)
                return new EventChoiceResult(false, eventId, optionId, committedResultId,
                    LastEventMessage, string.Empty);
            GameDate currentDate = WorldClock != null
                ? WorldClock.CurrentDate
                : SengokuChronicle.Domain.WorldClock.NewGameDate;
            EnsureRelationships();
            bool sc024 = IsSc024CouncilEvent(eventId);
            FamilyDoctrineState doctrineBefore = sc024 ? EnsureFamilyDoctrine().Clone() : null;
            FamilyRelationshipEntry[] familyBefore = sc024 ? CaptureFamilyRelationshipEntries() : null;
            FamilyChronicleEntry[] chronicleBefore = sc024 ? EnsureHistoryLedger().Capture() : null;
            EventRuntimeState runtimeBefore = sc024 ? _eventRuntimeState.Clone() : null;
            EventChoiceResult result = _eventEngine.TryChooseEventOption(eventId, optionId, committedResultId,
                currentDate, _relationships, _eventRuntimeState, BuildEventRuleContext(), _familyRelationships,
                EnsureFamilyDoctrine().Creed, EnsureFamilyDoctrine());
            if (result.Succeeded)
            {
                if (sc024)
                {
                    if (!TryRecordSc024Council(result, out string appendError))
                    {
                        RestoreSc024Choice(doctrineBefore, familyBefore, chronicleBefore, runtimeBefore);
                        return new EventChoiceResult(false, eventId, optionId, committedResultId,
                            "家族编年史写入失败，家训与关系已回滚：" + appendError, string.Empty);
                    }
                }
                else
                {
                    RecordEventChoice(result);
                    foreach (RelationshipResult relationship in result.AppliedRelationships)
                        RecordRelationshipChange(relationship, "event");
                    foreach (FamilyRelationshipResult relationship in result.AppliedFamilyRelationships)
                        RecordFamilyRelationshipChange(relationship, "event");
                }
                LastEventMessage = result.Feedback;
                SessionStateChanged?.Invoke();
            }
            return result;
        }

        private static bool IsSc024CouncilEvent(string eventId) =>
            string.Equals(eventId, SC024EventIds.SecrecyCouncilEventId, StringComparison.Ordinal) ||
            string.Equals(eventId, SC024EventIds.RighteousnessCouncilEventId, StringComparison.Ordinal) ||
            string.Equals(eventId, SC024EventIds.MeritCouncilEventId, StringComparison.Ordinal);

        private FamilyRelationshipEntry[] CaptureFamilyRelationshipEntries()
        {
            IReadOnlyList<FamilyRelationshipEntry> entries = _familyRelationships.Entries;
            var copy = new FamilyRelationshipEntry[entries.Count];
            for (int i = 0; i < entries.Count; i++)
                copy[i] = entries[i];
            return copy;
        }

        private void RestoreSc024Choice(FamilyDoctrineState doctrineBefore,
            FamilyRelationshipEntry[] familyBefore, FamilyChronicleEntry[] chronicleBefore,
            EventRuntimeState runtimeBefore)
        {
            _familyDoctrine = doctrineBefore;
            var restored = FamilyRelationships.CreateEmpty();
            if (familyBefore != null)
                foreach (FamilyRelationshipEntry entry in familyBefore)
                    restored.TryChange(entry.HeadId, entry.MemberId, entry.Value);
            _familyRelationships = restored;
            TryRestoreHistoryLedger(chronicleBefore);
            _eventRuntimeState = runtimeBefore;
        }

        /// <summary>
        /// Writes the council event record and both exact relationship records. Any
        /// failed append reports false so the caller can restore the whole choice.
        /// </summary>
        private bool TryRecordSc024Council(EventChoiceResult result, out string error)
        {
            GameDate date = CurrentGameDate;
            string title = _eventEngine != null &&
                           _eventEngine.TryGetEventText(result.EventId, out EventTextEntry text)
                ? text.title
                : result.EventId;

            // Prepare every record against the same ledger snapshot first. Like the
            // family-formation path, a test seam may advance the ledger before the
            // prepared records are appended, so the real TryAppendPrepared call fails
            // and the caller restores the whole choice.
            var entries = new List<FamilyChronicleEntry>();
            int sequence = EnsureHistoryLedger().Count + 1;
            if (!TryPrepareChronicleEntry(sequence, date, ChronicleCategory.Event,
                    result.EventId, result.OptionId, title, result.Feedback, 0, 0,
                    out FamilyChronicleEntry eventEntry, out error))
                return false;
            entries.Add(eventEntry);

            foreach (FamilyRelationshipResult relationship in result.AppliedFamilyRelationships)
            {
                string headName = PersonDisplayName(relationship.HeadId, "家主");
                string memberName = PersonDisplayName(relationship.MemberId, "家人");
                string summary = "家主" + headName + "与" + memberName + "的关系 " +
                                 relationship.PreviousValue.ToString(CultureInfo.InvariantCulture) +
                                 " → " +
                                 relationship.CurrentValue.ToString(CultureInfo.InvariantCulture) +
                                 "（家族议事）。";
                sequence++;
                if (!TryPrepareChronicleEntry(sequence, date, ChronicleCategory.Relationship,
                        relationship.HeadId.Value + "->" + relationship.MemberId.Value, "event",
                        headName + " 与 " + memberName, summary,
                        relationship.PreviousValue, relationship.CurrentValue,
                        out FamilyChronicleEntry relationshipEntry, out error))
                    return false;
                entries.Add(relationshipEntry);
            }

            Sc024ChronicleAppendFaultInjection?.Invoke(EnsureHistoryLedger());
            foreach (FamilyChronicleEntry entry in entries)
                if (!EnsureHistoryLedger().TryAppendPrepared(entry, out error))
                    return false;

            error = string.Empty;
            return true;
        }

        private static bool TryPrepareChronicleEntry(int sequence, GameDate date,
            ChronicleCategory category, string subjectId, string sourceId, string title,
            string summary, int fromValue, int toValue, out FamilyChronicleEntry entry,
            out string error)
        {
            entry = null;
            string entryId = FamilyChronicleEntry.BuildEntryId(sequence);
            return FamilyChronicleEntry.TryCreate(sequence, entryId, date.Year, date.Month,
                category, subjectId, sourceId, title, summary, fromValue, toValue,
                0, 0, 0, 0, out entry, out error);
        }

        /// <summary>
        /// Snapshot the SC-010 succession facts used by event offer gates. Settled count is
        /// the raw count only while no succession decision, regency, or extinction owns play.
        /// </summary>
        private EventRuleContext BuildEventRuleContext() =>
            BuildEventRuleContext(Succession, _eventScenarioId, _campaignProgress?.OriginId);

        private static EventRuleContext BuildEventRuleContext(SuccessionState succession,
            string eventScenarioId, string campaignOriginId)
        {
            if (succession == null || succession.World == null)
                return EventRuleContext.Default;
            int settled = succession.Phase == SuccessionPhase.None ? succession.SuccessionCount : 0;
            string scenario = ResolveEventScenarioId(succession, eventScenarioId, campaignOriginId);
            IReadOnlyList<FamilyMembership> memberships = succession.World.Family.Members;
            var members = new PersonId[memberships.Count];
            for (int i = 0; i < memberships.Count; i++)
                members[i] = memberships[i].PersonId;
            return new EventRuleContext(succession.SuccessionCount, settled, scenario,
                succession.HeadPersonId, members);
        }

        /// <summary>
        /// Resolves the event route for one succession snapshot. An explicit route wins so
        /// SC-024 keeps its own rows; the SC-013 fixed roster stays the existing fallback;
        /// the SC-010 standard campaign is identified by its campaign origin because it
        /// carries no explicit route. Every other demo origin stays route-less, so a
        /// scenario-tagged row never leaks between campaigns. Live scheduling and the
        /// save-load event-runtime check both call this one resolver, and nothing here is
        /// persisted.
        /// </summary>
        private static string ResolveEventScenarioId(SuccessionState succession,
            string eventScenarioId, string campaignOriginId)
        {
            if (!string.IsNullOrWhiteSpace(eventScenarioId))
                return eventScenarioId;
            if (M2FixedScenario.IsActive(succession))
                return EventScenarioIds.Sc013;
            return string.Equals(campaignOriginId, CampaignOriginCatalog.Sc010OriginId,
                StringComparison.Ordinal)
                ? EventScenarioIds.Sc010
                : string.Empty;
        }

        /// <summary>Creates an idempotent event-result ID from stable content and current game date.</summary>
        public string CreateEventResultId(string eventId, string optionId)
        {
            if (WorldClock == null || string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(optionId))
                return string.Empty;
            GameDate date = WorldClock.CurrentDate;
            return eventId + ":" + date.Year.ToString("0000", CultureInfo.InvariantCulture) +
                   date.Month.ToString("00", CultureInfo.InvariantCulture) + ":" + optionId;
        }

        /// <summary>创建新游戏会话，日期固定为 1560-04-01，位置固定为清洲。</summary>
        public static GameSession NewGame()
        {
            var go = new GameObject(nameof(GameSession));
            var session = go.AddComponent<GameSession>();
            session.InitNewGame();
            return session;
        }

        /// <summary>
        /// True when the person may plan a new activity from the current stable point.
        /// Time ownership, pending events, succession and industry consistency reuse the
        /// same gates as month or day advancement.
        /// </summary>
        public bool CanPlanDailyAction(PersonId personId, out string reason)
        {
            reason = string.Empty;
            if (WorldClock == null)
            {
                reason = "游戏尚未初始化。";
                return false;
            }
            if (IsFamilyExtinct)
            {
                reason = "家族已经灭亡：无法再安排人物行动。";
                return false;
            }
            EnsureEventRuntimeState();
            if (_eventRuntimeState.HasPendingEvents)
            {
                reason = "尚有事件待回应：请先处理当前事件，再安排行动。";
                return false;
            }
            if (SuccessionBlocksTimeAdvance)
            {
                reason = Succession.IsRegencyHandoffDue(CurrentGameDate)
                    ? "幼主成年交权尚未完成：请先完成交权，再安排行动。"
                    : "尚有继承决策待处理：请先完成继承选择，再安排行动。";
                return false;
            }
            if (!ValidateIndustryMonthConsistency())
            {
                reason = LastEventMessage;
                return false;
            }
            EnsureSuccession();
            if (_succession.World == null ||
                !_succession.World.TryGetPerson(personId, out _) ||
                !_succession.World.IsLiving(personId) ||
                !_succession.World.Family.IsMember(personId))
            {
                reason = "只有当前在世的家族成员才能安排每日行动。";
                return false;
            }
            if (IsTacticalOperative(personId))
            {
                reason = "该人物正在执行潜入密令：请先完成或撤退，再安排每日行动。";
                return false;
            }
            if (EnsureDailyActions().HasActiveAction(personId))
            {
                reason = "该人物已有进行中的行动；不能同时执行两个行动。";
                return false;
            }
            if (_commission.Stage == ShadowEscortStage.InProgress &&
                _commission.AssigneeId == personId)
            {
                reason = "该人物正在执行家族委托：请等待委托结算，再安排新的行动。";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Plans one duration-bound action. Travel duration comes from
        /// <see cref="WorldMapRules"/>; other kinds require the caller's explicit duration.
        /// </summary>
        public bool TryPlanDailyAction(PersonId personId, DailyActionKind kind,
            int durationDays, LocationId destination, out string reason)
        {
            reason = string.Empty;
            if (!CanPlanDailyAction(personId, out reason))
            {
                LastDailyActionMessage = reason;
                LastDailyActionOutcomes = new[]
                {
                    new DailyActionResult(personId, kind, DailyActionOutcomeKind.Rejected,
                        CurrentGameDate, reason)
                };
                return false;
            }

            DailyActionState actions = EnsureDailyActions();
            if (!actions.TryGetLocation(personId, out LocationId location))
            {
                reason = "该人物正在旅途中或地点未知，无法安排新的行动。";
                LastDailyActionMessage = reason;
                LastDailyActionOutcomes = new[]
                {
                    new DailyActionResult(personId, kind, DailyActionOutcomeKind.Rejected,
                        CurrentGameDate, reason)
                };
                return false;
            }

            DailyActionResult result;
            if (kind == DailyActionKind.Travel)
            {
                if (!destination.IsValid)
                {
                    reason = "旅行目的地无效。";
                    return RejectAction(personId, kind, reason);
                }
                if (!WorldMapRules.TryGetTravelDays(location, destination, out int routeDays))
                {
                    reason = location == destination
                        ? "旅行目的地必须不同于出发地。"
                        : "两地之间没有直达路线。";
                    return RejectAction(personId, kind, reason);
                }
                if (durationDays > 0 && durationDays != routeDays)
                {
                    reason = "旅行持续时间必须等于世界地图规则给出的天数。";
                    return RejectAction(personId, kind, reason);
                }
                if (!actions.TryPlan(personId, kind, routeDays, location, destination,
                        CurrentGameDate, "前往" + LocationId.GetChineseName(destination) + "。",
                        out result))
                {
                    reason = result.Reason;
                    return RejectAction(personId, kind, reason);
                }
            }
            else if (!actions.TryPlan(personId, kind, durationDays, location, default,
                         CurrentGameDate,
                         DailyActionState.KindName(kind) + "行动已安排。", out result))
            {
                reason = result.Reason;
                return RejectAction(personId, kind, reason);
            }

            LastDailyActionOutcomes = new[] { result };
            LastDailyActionMessage = result.Reason;
            if (NpcAutonomyRules.IsPilot(personId) && _npcAutonomy != null)
                _npcAutonomy.SetStatus(personId, NpcAutonomyStatus.Executing);
            NotifyDailyActionChanged();
            return true;
        }

        public bool TryPlanRestAction(PersonId personId, int durationDays, out string reason) =>
            TryPlanDailyAction(personId, DailyActionKind.Rest, durationDays, default, out reason);

        public bool TryPlanTrainingAction(PersonId personId, int durationDays, out string reason) =>
            TryPlanDailyAction(personId, DailyActionKind.Training, durationDays, default, out reason);

        public bool TryPlanWorkAction(PersonId personId, int durationDays, out string reason) =>
            TryPlanDailyAction(personId, DailyActionKind.Work, durationDays, default, out reason);

        public bool TryPlanTravelAction(PersonId personId, LocationId destination,
            out string reason) =>
            TryPlanDailyAction(personId, DailyActionKind.Travel, 0, destination, out reason);

        /// <summary>
        /// Cancels one active action with an explicit reason. A traveller returns to the
        /// departure point because this foundation has no mid-route position model.
        /// </summary>
        public bool TryInterruptDailyAction(PersonId personId, string reason)
        {
            string message = string.IsNullOrWhiteSpace(reason) ? "玩家取消了当前行动。" : reason;
            if (!EnsureDailyActions().TryInterrupt(personId, message, CurrentGameDate,
                    out DailyActionResult result))
            {
                LastDailyActionMessage = result.Reason;
                LastDailyActionOutcomes = new[] { result };
                return false;
            }

            LastDailyActionOutcomes = new[] { result };
            LastDailyActionMessage = result.Reason;
            if (NpcAutonomyRules.IsPilot(personId) && _npcAutonomy != null &&
                _npcAutonomy.TryGetPerson(personId, out _))
            {
                ApplyNpcActionInterruption(result);
                _npcAutonomy.RecordDecision(new NpcAutonomyDecisionRecord(CurrentGameDate,
                    personId, NpcAutonomyCommandOutcomeKind.Interrupted, result.Kind, true,
                    NpcAutonomyTrigger.ActionInterrupted, result.Reason));

                if (_autonomyInterruptionSuppression == 0)
                {
                    _dailyActionNotificationSuppression++;
                    try
                    {
                        NpcAutonomyCommandOutcomeKind outcome = PlanOnePilot(personId,
                            NpcAutonomyTrigger.ActionInterrupted, out string planReason);
                        var combined = new List<DailyActionResult>(2) { result };
                        if (outcome == NpcAutonomyCommandOutcomeKind.Accepted &&
                            LastDailyActionOutcomes.Count > 0)
                            combined.Add(LastDailyActionOutcomes[0]);
                        LastDailyActionOutcomes = combined.AsReadOnly();
                        LastDailyActionMessage = outcome == NpcAutonomyCommandOutcomeKind.Accepted
                            ? result.Reason + " " +
                              (combined.Count > 1 ? combined[1].Reason : planReason)
                            : result.Reason + " " + planReason;
                    }
                    finally
                    {
                        _dailyActionNotificationSuppression--;
                    }
                }
            }

            NotifyDailyActionChanged();
            return true;
        }

        /// <summary>
        /// Atomically replaces an active action with a new plan. The old action is recorded
        /// as interrupted first; if the new plan is rejected, the old action is restored.
        /// Notifications are suppressed until the final replacement state is committed, so
        /// observers never see a temporary no-action gap.
        /// </summary>
        public bool TryReplanDailyAction(PersonId personId, DailyActionKind kind,
            int durationDays, LocationId destination, out string reason)
        {
            reason = string.Empty;
            DailyActionState before = EnsureDailyActions().Clone();
            NpcAutonomyState autonomyBefore = CloneNpcAutonomy();
            IReadOnlyList<DailyActionResult> outcomesBefore = LastDailyActionOutcomes;
            string messageBefore = LastDailyActionMessage;
            _autonomyInterruptionSuppression++;
            _dailyActionNotificationSuppression++;
            try
            {
                if (!TryInterruptDailyAction(personId, "重新规划：取消原行动。"))
                {
                    reason = LastDailyActionMessage;
                    _npcAutonomy = autonomyBefore;
                    LastDailyActionOutcomes = outcomesBefore;
                    LastDailyActionMessage = messageBefore;
                    return false;
                }
                DailyActionResult interrupted = LastDailyActionOutcomes[0];
                if (!TryPlanDailyAction(personId, kind, durationDays, destination, out reason))
                {
                    _dailyActions = before;
                    _npcAutonomy = autonomyBefore;
                    LastDailyActionOutcomes = outcomesBefore;
                    LastDailyActionMessage = "重新规划失败，已恢复原行动：" + reason;
                    return false;
                }

                var outcomes = new List<DailyActionResult>(2)
                {
                    interrupted
                };
                outcomes.Add(LastDailyActionOutcomes[0]);
                LastDailyActionOutcomes = outcomes.AsReadOnly();
                LastDailyActionMessage = "已重新规划：" + LastDailyActionMessage;
            }
            finally
            {
                _autonomyInterruptionSuppression--;
                _dailyActionNotificationSuppression--;
            }

            NotifyDailyActionChanged();
            return true;
        }

        private bool RejectAction(PersonId personId, DailyActionKind kind, string reason)
        {
            LastDailyActionMessage = reason;
            LastDailyActionOutcomes = new[]
            {
                new DailyActionResult(personId, kind, DailyActionOutcomeKind.Rejected,
                    CurrentGameDate, reason)
            };
            return false;
        }

        /// <summary>
        /// Explicit daily control: a normal day uses the existing within-month path; the
        /// last day of a month rolls into the existing monthly transaction exactly once.
        /// </summary>
        public bool AdvanceActionDay()
        {
            if (WorldClock == null)
                InitNewGame();
            GameDate current = WorldClock.CurrentDate;
            if (GameDate.TryNext(current, out GameDate next) &&
                (next.Year != current.Year || next.Month != current.Month))
                return TryAdvanceToNextMonth();
            return AdvanceOneDay();
        }

        /// <summary>推进一天。成功返回 true；失败返回 false 且状态不变，并通过既有结果/消息通道说明原因。</summary>
        public bool AdvanceOneDay()
        {
            if (RejectWhileTacticalMissionActive())
                return false;
            if (WorldClock == null)
                InitNewGame();
            if (IsFamilyExtinct)
            {
                ReportSuccessionTimePause();
                return false;
            }
            EnsureEventRuntimeState();
            if (_eventRuntimeState.HasPendingEvents)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.PendingEvent;
                return false;
            }
            if (SuccessionBlocksTimeAdvance)
            {
                ReportSuccessionTimePause();
                return false;
            }
            if (!ValidateIndustryMonthConsistency())
                return false;
            GameDate previousDate = WorldClock.CurrentDate;
            if (!GameDate.TryNext(WorldClock.CurrentDate, out GameDate nextDate))
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            if (nextDate.Year != WorldClock.CurrentDate.Year ||
                nextDate.Month != WorldClock.CurrentDate.Month)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.MonthSettlementRequired;
                LastEventMessage = "日期将跨月：请使用「结束本月」完成月度结算。";
                return false;
            }
            if (!WorldClock.AdvanceOneDay())
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            bool autonomyChanged = AdvanceAutonomyDay(previousDate,
                out IReadOnlyList<DailyActionResult> outcomes);
            PublishDailyActionReport(outcomes);
            SyncCurrentLocationFromDailyActions();
            bool commissionChanged = AdvanceCommissionDay(WorldClock.CurrentDate);
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            if (NoticeRegencyHandoffIfDue() || autonomyChanged || commissionChanged)
                SessionStateChanged?.Invoke();
            return true;
        }

        /// <summary>推进多天。count 必须为正整数；失败返回 false 且状态不变，并说明原因。</summary>
        public bool AdvanceDays(int count)
        {
            if (RejectWhileTacticalMissionActive())
                return false;
            if (WorldClock == null)
                InitNewGame();
            if (IsFamilyExtinct)
            {
                ReportSuccessionTimePause();
                return false;
            }
            EnsureEventRuntimeState();
            if (_eventRuntimeState.HasPendingEvents)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.PendingEvent;
                return false;
            }
            if (SuccessionBlocksTimeAdvance)
            {
                ReportSuccessionTimePause();
                return false;
            }
            if (count <= 0)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            if (!ValidateIndustryMonthConsistency())
                return false;
            GameDate previousDate = WorldClock.CurrentDate;
            if (!GameDate.TryAddDays(WorldClock.CurrentDate, count, out GameDate targetDate))
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            if (targetDate.Year != WorldClock.CurrentDate.Year ||
                targetDate.Month != WorldClock.CurrentDate.Month)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.MonthSettlementRequired;
                LastEventMessage = "日期将跨月：请使用「结束本月」完成月度结算。";
                return false;
            }
            int suppression = _dailyActionNotificationSuppression;
            _dailyActionNotificationSuppression++;
            var allOutcomes = new List<DailyActionResult>();
            bool autonomyChanged = false;
            bool commissionChanged = false;
            GameDate cursor = previousDate;
            try
            {
                for (int day = 0; day < count; day++)
                {
                    if (!WorldClock.AdvanceOneDay())
                    {
                        LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                        return false;
                    }
                    autonomyChanged |= AdvanceAutonomyDay(cursor,
                        out IReadOnlyList<DailyActionResult> dayOutcomes);
                    allOutcomes.AddRange(dayOutcomes);
                    commissionChanged |= AdvanceCommissionDay(WorldClock.CurrentDate);
                    cursor = WorldClock.CurrentDate;
                }
            }
            finally
            {
                _dailyActionNotificationSuppression = suppression;
            }

            PublishDailyActionReport(allOutcomes);
            SyncCurrentLocationFromDailyActions();
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            if (NoticeRegencyHandoffIfDue() || autonomyChanged || commissionChanged)
                SessionStateChanged?.Invoke();
            return true;
        }

        /// <summary>One applied education year used only inside the month transaction.</summary>
        private readonly struct EducationSettlementRecord
        {
            public EducationSettlementRecord(PersonId learner, int ageYear, EducationYearResult result)
            {
                Learner = learner;
                AgeYear = ageYear;
                Result = result;
            }

            public PersonId Learner { get; }
            public int AgeYear { get; }
            public EducationYearResult Result { get; }
        }

        /// <summary>
        /// Applies every open 0..15 age year to a clone. A missing plan settles as free
        /// growth; a plan whose mentor is no longer eligible also falls back to free growth
        /// so a death or retirement cannot freeze the world clock forever.
        /// </summary>
        private bool TryPlanEducationSettlement(CharacterDevelopmentState working,
            GameDate previousDate, out List<EducationSettlementRecord> records, out string error)
        {
            records = new List<EducationSettlementRecord>();
            foreach (PersonId personId in working.PersonIds)
            {
                if (!_succession.World.Family.IsMember(personId) ||
                    !_succession.World.TryGetLife(personId, out SuccessionPersonLife life) ||
                    !life.Alive)
                    continue;
                int ageYear = life.AgeAt(previousDate);
                if (ageYear < EducationScenarioProvider.MinimumLearnerAge ||
                    ageYear > EducationScenarioProvider.MaximumLearnerAge ||
                    working.HasResult(personId, ageYear))
                    continue;

                bool hasPlan = working.TryGetPlan(personId, ageYear, out EducationYearPlan plan);
                bool mentorUnavailable = false;
                MentorSpecialty specialty = MentorSpecialty.None;
                if (hasPlan && !plan.IsFreeGrowth &&
                    !TryMentorSpecialty(plan.Mentor, personId, previousDate, out specialty))
                    mentorUnavailable = true;

                if (!working.TrySettleYear(personId, ageYear, HasAptitude(personId),
                        AptitudeOf(personId), specialty, IsSupportiveEnvironment(personId),
                        mentorUnavailable, out EducationYearResult result, out string settleError))
                {
                    error = "教育年度结算失败：" + settleError;
                    return false;
                }
                records.Add(new EducationSettlementRecord(personId, ageYear, result));
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Appends exactly one chronicle record per settled education year.</summary>
        private bool TryAppendEducationChronicles(List<EducationSettlementRecord> records,
            GameDate date, out string error)
        {
            foreach (EducationSettlementRecord record in records)
            {
                string learnerName = PersonDisplayName(record.Learner, "家人");
                string mentorName = record.Result.Mentor.IsEmpty
                    ? "无导师"
                    : PersonDisplayName(record.Result.Mentor, "家人");
                int fromValue = 0;
                int toValue = 0;
                if (CharacterGrowthRules.TryGetAttribute(record.Result.Direction,
                        out GrowthAttribute attribute))
                {
                    fromValue = record.Result.Before.Get(attribute);
                    toValue = record.Result.After.Get(attribute);
                }

                string summary = learnerName + "：" +
                    CharacterGrowthRules.DirectionName(record.Result.Direction) + "培养，导师 " +
                    mentorName + "；" + DescribeGrowthChange(record.Result) + "。" +
                    record.Result.Explanation;
                if (!EnsureHistoryLedger().TryAppendEducation(date.Year, date.Month,
                        record.Learner.Value, EducationSourceId(record.Result.Direction),
                        fromValue, toValue, learnerName + "的年度培养", summary,
                        out _, out string appendError))
                {
                    error = "教育年度编年史写入失败：" +
                            (string.IsNullOrEmpty(appendError) ? "未知原因" : appendError);
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static string DescribeGrowthChange(EducationYearResult result)
        {
            var builder = new StringBuilder(96);
            AppendGrowth(builder, "忍术", result.Before.Ninjutsu, result.After.Ninjutsu);
            builder.Append('；');
            AppendGrowth(builder, "武艺", result.Before.MartialArts, result.After.MartialArts);
            builder.Append('；');
            AppendGrowth(builder, "学问", result.Before.Scholarship, result.After.Scholarship);
            builder.Append('；');
            AppendGrowth(builder, "经营", result.Before.Management, result.After.Management);
            builder.Append('；');
            AppendGrowth(builder, "社交", result.Before.Social, result.After.Social);
            return builder.ToString();
        }

        private static void AppendGrowth(StringBuilder builder, string label, int before, int after)
        {
            builder.Append(label).Append(' ').Append(before);
            if (after != before)
                builder.Append(" → ").Append(after);
        }

        private static string EducationSourceId(EducationDirection direction)
        {
            switch (direction)
            {
                case EducationDirection.Ninjutsu: return "education.ninjutsu";
                case EducationDirection.MartialArts: return "education.martial-arts";
                case EducationDirection.Scholarship: return "education.scholarship";
                case EducationDirection.Management: return "education.management";
                case EducationDirection.Social: return "education.social";
                case EducationDirection.FreeGrowth: return "education.free-growth";
                default: return "education.unknown";
            }
        }

        /// <summary>
        /// Advances only the current head's matching ambition from results this month
        /// already produced. A different ambition kind, an absent record or a completed
        /// record is a no-op; the doctrine methods themselves reject duplicate months,
        /// techniques and education years.
        /// </summary>
        private static FamilyAmbitionRecord AdvanceFamilyDoctrineForSettlement(
            FamilyDoctrineState doctrine, PersonId headId, IndustryMonthReport industryReport,
            bool researchCompleted, string techniqueId, PersonId designatedHeirId,
            List<EducationSettlementRecord> educationRecords)
        {
            if (doctrine == null || headId.IsEmpty ||
                !doctrine.TryGetAmbition(headId, out FamilyAmbitionRecord record) ||
                record.IsComplete)
                return null;

            switch (record.Kind)
            {
                case FamilyAmbitionKind.DevelopHousehold:
                    if (industryReport != null)
                        doctrine.TryAdvanceHouseholdAmbition(headId, industryReport, out _);
                    break;
                case FamilyAmbitionKind.PassDownTechnique:
                    if (researchCompleted && !string.IsNullOrWhiteSpace(techniqueId))
                        doctrine.TryAdvanceTechniqueAmbition(headId, new[] { techniqueId }, out _);
                    break;
                case FamilyAmbitionKind.EducateHeir:
                    if (designatedHeirId.IsEmpty || educationRecords == null)
                        break;
                    foreach (EducationSettlementRecord educationRecord in educationRecords)
                    {
                        if (educationRecord.Learner != designatedHeirId)
                            continue;
                        doctrine.TryAdvanceHeirEducationAmbition(headId, designatedHeirId,
                            educationRecord.AgeYear, educationRecord.Result, out _);
                    }
                    break;
            }

            // Return the record only for the single transition into completion so the
            // monthly boundary appends exactly one chronicle result.
            return doctrine.TryGetAmbition(headId, out FamilyAmbitionRecord advanced) &&
                   advanced.IsComplete
                ? advanced
                : null;
        }

        /// <summary>
        /// Appends the one completion record for a month that just finished an ambition.
        /// It reuses the existing event chronicle schema and grants no bonus resource or
        /// ability; the caller treats a failed append as a failed month transaction.
        /// </summary>
        private bool TryAppendAmbitionCompletion(GameDate date, PersonId headId,
            FamilyAmbitionRecord record, out string error)
        {
            string headName = PersonDisplayName(headId, "家主");
            string title = headName + "的抱负达成";
            string summary = "家主" + headName + "完成抱负「" + FamilyAmbitionName(record.Kind) +
                             "」：进度 " +
                             record.Progress.ToString(CultureInfo.InvariantCulture) + "/" +
                             record.Target.ToString(CultureInfo.InvariantCulture) +
                             "。未获得额外资源或能力。";
            return EnsureHistoryLedger().TryAppendEvent(date.Year, date.Month, headId.Value,
                AmbitionSourceId(record.Kind), title, summary, out _, out error);
        }

        private static string AmbitionSourceId(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold: return "ambition.develop-household";
                case FamilyAmbitionKind.PassDownTechnique: return "ambition.pass-down-technique";
                case FamilyAmbitionKind.EducateHeir: return "ambition.educate-heir";
                default: return "ambition.unknown";
            }
        }

        /// <summary>
        /// 结束本月：推进到下月 1 日并排定一个可用月度事件。
        /// 有待处理事件、内容无效或日期越界时拒绝推进。
        /// </summary>
        public bool TryAdvanceToNextMonth()
        {
            if (RejectWhileTacticalMissionActive())
                return false;
            if (WorldClock == null)
                InitNewGame();
            if (IsFamilyExtinct)
            {
                ReportSuccessionTimePause();
                return false;
            }
            EnsureRandomState();
            EnsureEventRuntimeState();
            if (_eventRuntimeState.HasPendingEvents)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.PendingEvent;
                return false;
            }
            if (SuccessionBlocksTimeAdvance)
            {
                ReportSuccessionTimePause();
                return false;
            }
            if (_eventEngine == null || !_eventEngine.IsReady)
            {
                LastMonthTurnOutcome = MonthTurnOutcome.ContentUnavailable;
                LastEventMessage = _eventEngine != null ? _eventEngine.ValidationMessage : LastEventMessage;
                return false;
            }

            GameDate previousDate = WorldClock.CurrentDate;
            int nextYear = previousDate.Month == 12 ? previousDate.Year + 1 : previousDate.Year;
            int nextMonth = previousDate.Month == 12 ? 1 : previousDate.Month + 1;
            if (!GameDate.TryParse(nextYear, nextMonth, 1, out GameDate nextDate))
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }

            IndustryState industryBefore = EnsureIndustryState().Clone();
            CharacterDevelopmentState developmentBefore = EnsureDevelopment().Clone();
            FamilyResearchState researchBefore = EnsureResearch().Clone();
            FamilyDoctrineState doctrineBefore = EnsureFamilyDoctrine().Clone();
            DailyActionState dailyBefore = EnsureDailyActions().Clone();
            NpcAutonomyState autonomyBefore = CloneNpcAutonomy();
            ShadowEscortCommission commissionBefore = _commission.Clone();
            FamilyStandingState standingBefore = EnsureStanding().Clone();
            MissionInjuryState injuriesBefore = EnsureInjuries().Clone();
            MissionHistoryState missionHistoryBefore = EnsureMissionHistory().Clone();
            LocationId locationBefore = CurrentLocation;
            RandomStateSaveData rngBefore = _randomStateManager.CaptureState();
            FamilyChronicleEntry[] chronicleBefore = EnsureHistoryLedger().Capture();
            SuccessionSaveData successionBefore = SuccessionSaveData.Capture(_succession);
            EventRuntimeState runtimeBefore = _eventRuntimeState.Clone();
            IndustryMonthReport reportBefore = LastIndustryReport;
            MarketState marketBefore = EnsureMarket().Clone();
            MarketMonthReport marketReportBefore = LastMarketReport;
            string industryMessageBefore = LastIndustryMessage;
            string educationMessageBefore = LastEducationMessage;
            FamilyResearchMonthReport researchReportBefore = LastResearchReport;
            string researchMessageBefore = LastResearchMessage;
            IReadOnlyList<DailyActionResult> dailyOutcomesBefore = LastDailyActionOutcomes;
            string dailyMessageBefore = LastDailyActionMessage;
            string commissionMessageBefore = LastCommissionMessage;
            try
            {
                // M08B: the industry month is priced with the previously settled
                // production-material index carried by the month-start market snapshot,
                // exactly like the household pays the previously settled food index.
                IndustrySettlementResult settlement = IndustrySettlement.Plan(
                    _industryState, Succession, previousDate,
                    marketBefore.MaterialsPriceIndexPermille);
                if (settlement.Outcome == IndustrySettlementOutcome.SettlementGap ||
                    settlement.Outcome == IndustrySettlementOutcome.InvalidState)
                {
                    LastMonthTurnOutcome = MonthTurnOutcome.IndustryStateInvalid;
                    LastEventMessage = settlement.Message;
                    return false;
                }

                IndustryState nextIndustry = settlement.NextState ?? industryBefore.Clone();

                // M08B: the market settles inside this same month transaction, right after
                // industry production and material costs. The household still reads the
                // month-start roster here (births and deaths apply to a later clone) and
                // pays the previously settled food index. Any rejection or failure below
                // leaves market, resources, population, date, RNG and chronicle untouched.
                MarketSettlementResult marketSettlement = MarketSettlement.Plan(
                    marketBefore, nextIndustry.Resources, Succession, settlement.Report,
                    previousDate);
                bool industryAlreadySettled =
                    settlement.Outcome == IndustrySettlementOutcome.AlreadySettled;
                if (marketSettlement.Outcome == MarketSettlementOutcome.AlreadySettled)
                {
                    if (!industryAlreadySettled)
                    {
                        LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                        LastEventMessage = marketSettlement.Message;
                        return false;
                    }
                }
                else if (!marketSettlement.Succeeded)
                {
                    LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                    LastEventMessage = marketSettlement.Message;
                    return false;
                }
                else
                {
                    nextIndustry.TrySetResources(marketSettlement.NextResources, out _);
                }
                MarketState nextMarket = marketSettlement.NextMarket ?? marketBefore;

                FamilyResearchSettlementResult researchSettlement = FamilyResearchSettlement.Plan(
                    _researchState, nextIndustry.Resources, Succession, previousDate, nextIndustry);
                if (researchSettlement.Outcome == FamilyResearchOutcome.SettlementGap ||
                    researchSettlement.Outcome == FamilyResearchOutcome.InvalidState)
                {
                    LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                    LastEventMessage = researchSettlement.Message;
                    return false;
                }
                nextIndustry.TrySetResources(researchSettlement.NextResources, out _);

                // The annual education year settles exactly at the December-to-January
                // boundary, after the industry and research plans and before the new month
                // is scheduled.
                CharacterDevelopmentState nextDevelopment = developmentBefore;
                List<EducationSettlementRecord> educationRecords = null;
                if (previousDate.Month == 12)
                {
                    nextDevelopment = developmentBefore.Clone();
                    if (!TryPlanEducationSettlement(nextDevelopment, previousDate,
                            out educationRecords, out string educationError))
                    {
                        LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                        LastEventMessage = educationError;
                        return false;
                    }
                    if (!TryAppendEducationChronicles(educationRecords, previousDate,
                            out string educationChronicleError))
                    {
                        TryRestoreHistoryLedger(chronicleBefore);
                        LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                        LastEventMessage = educationChronicleError;
                        return false;
                    }
                }

                EventRuntimeState nextRuntime = runtimeBefore.Clone();
                EventScheduleResult schedule = _eventEngine.ScheduleMonthly(nextDate, _relationships,
                    _randomStateManager.GetStream(RandomStreamKind.Events), nextRuntime,
                    BuildEventRuleContext(), EnsureFamilyDoctrine().Creed);
                if (!schedule.Succeeded)
                {
                    TryRestoreRandomState(rngBefore);
                    TryRestoreHistoryLedger(chronicleBefore);
                    LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                    LastEventMessage = schedule.Message;
                    return false;
                }

                // M04B: plan conception, progress and due births with the persisted
                // WorldSimulation stream. The plan is pure; mutation is applied to a
                // cloned succession snapshot so any later failure leaves the live family
                // untouched until the month commits.
                DeterministicRandomStream worldStream =
                    _randomStateManager.GetStream(RandomStreamKind.WorldSimulation);
                FamilyBirthSettlementPlan birthSettlement = FamilyBirthSettlement.Plan(
                    _succession, previousDate, nextDate, worldStream);
                if (!birthSettlement.Succeeded)
                {
                    TryRestoreRandomState(rngBefore);
                    TryRestoreHistoryLedger(chronicleBefore);
                    LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                    LastEventMessage = "家族出生规划失败：" + birthSettlement.Error;
                    return false;
                }

                // M08A: both annual candidate lists are frozen before any birth is
                // applied, so a newborn can never enter the same mortality settlement and
                // a person widowed by this settlement keeps the living spouse it had when
                // the settlement began. The RNG consumption order is fixed for the whole
                // month: due births and conceptions, then annual deaths, then autonomous
                // marriages.
                bool annualBoundary = FamilyLifeCycleRules.IsAnnualBoundary(previousDate,
                    nextDate);
                IReadOnlyList<PersonId> deathCandidates = annualBoundary
                    ? FamilyLifeCycleRules.CaptureDeathCandidateIds(_succession)
                    : Array.Empty<PersonId>();
                IReadOnlyList<PersonId> marriageCandidates = annualBoundary
                    ? FamilyLifeCycleRules.CaptureMarriageCandidateIds(_succession, nextDate)
                    : Array.Empty<PersonId>();
                IReadOnlyList<PersonId> naturalDeaths = FamilyLifeCycleRules.PlanNaturalDeaths(
                    deathCandidates, _succession, previousDate, nextDate, worldStream,
                    FamilyLifeCycleRules.MortalityBonusForFoodShortage(
                        nextMarket.ConsecutiveFoodShortageMonths));

                SuccessionState nextSuccession = null;
                bool headDied = false;
                string headDeathMessage = string.Empty;
                if (birthSettlement.PregnancyPlan.HasChanges || naturalDeaths.Count > 0)
                {
                    if (!SuccessionSaveData.TryRestoreTransactionSnapshot(successionBefore,
                            out nextSuccession, out string successionCloneError))
                    {
                        TryRestoreRandomState(rngBefore);
                        TryRestoreHistoryLedger(chronicleBefore);
                        LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                        LastEventMessage = "家族出生状态无法应用：" + successionCloneError;
                        return false;
                    }

                    if (birthSettlement.PregnancyPlan.HasChanges &&
                        !TryApplyBirthSettlement(nextSuccession, birthSettlement,
                            ref nextDevelopment, out string birthError))
                    {
                        TryRestoreRandomState(rngBefore);
                        TryRestoreHistoryLedger(chronicleBefore);
                        LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                        LastEventMessage = "家族出生结算失败：" + birthError;
                        return false;
                    }

                    if (naturalDeaths.Count > 0 &&
                        !TryApplyNaturalDeaths(nextSuccession, naturalDeaths, nextDate,
                            out headDied, out headDeathMessage, out string deathError))
                    {
                        TryRestoreRandomState(rngBefore);
                        TryRestoreHistoryLedger(chronicleBefore);
                        LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                        LastEventMessage = "家族自然死亡结算失败：" + deathError;
                        return false;
                    }
                }

                if (birthSettlement.PregnancyPlan.HasChanges)
                {
                    foreach (FamilyBirthChronicleFact chronicle in birthSettlement.Chronicles)
                    {
                        if (!EnsureHistoryLedger().TryAppendBirth(chronicle.Date.Year,
                                chronicle.Date.Month, chronicle.NewbornId.Value,
                                chronicle.Title, chronicle.Summary,
                                out _, out string birthChronicleError))
                        {
                            TryRestoreRandomState(rngBefore);
                            TryRestoreHistoryLedger(chronicleBefore);
                            LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                            LastEventMessage = "家族新生编年史写入失败：" +
                                               (string.IsNullOrEmpty(birthChronicleError)
                                                   ? "未知原因"
                                                   : birthChronicleError);
                            return false;
                        }
                    }
                }

                // M08A: autonomous marriages apply the settlement-start candidate snapshot
                // to the post-death clone and are skipped whenever the month produced a
                // pending succession decision, because the player must resolve the head's
                // death first.
                FamilyMarriagePlan marriagePlan = FamilyMarriagePlan.Empty;
                bool familyEnded = nextSuccession != null &&
                                   nextSuccession.Phase == SuccessionPhase.Extinct;
                if (annualBoundary && !familyEnded &&
                    !(nextSuccession != null && nextSuccession.IsPending))
                {
                    IReadOnlyList<NpcMarriageProposal> marriageProposals =
                        FamilyLifeCycleRules.PlanNpcMarriages(marriageCandidates,
                            nextSuccession ?? _succession, previousDate, nextDate, worldStream);
                    if (marriageProposals.Count > 0)
                    {
                        if (nextSuccession == null &&
                            !SuccessionSaveData.TryRestoreTransactionSnapshot(successionBefore,
                                out nextSuccession, out string marriageCloneError))
                        {
                            TryRestoreRandomState(rngBefore);
                            TryRestoreHistoryLedger(chronicleBefore);
                            LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                            LastEventMessage = "家族婚姻状态无法应用：" + marriageCloneError;
                            return false;
                        }

                        marriagePlan = FamilyMarriageSettlement.Plan(nextSuccession,
                            marriageProposals, nextDate);
                        if (!marriagePlan.Succeeded)
                        {
                            TryRestoreRandomState(rngBefore);
                            TryRestoreHistoryLedger(chronicleBefore);
                            LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                            LastEventMessage = "家族婚姻规划失败：" + marriagePlan.Error;
                            return false;
                        }

                        if (!FamilyMarriageSettlement.TryApply(nextSuccession, marriagePlan,
                                ref nextDevelopment, out string marriageError))
                        {
                            TryRestoreRandomState(rngBefore);
                            TryRestoreHistoryLedger(chronicleBefore);
                            LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                            LastEventMessage = "家族婚姻结算失败：" + marriageError;
                            return false;
                        }
                    }
                }

                if (!TryAppendFamilyLifeCycleChronicles(nextSuccession ?? _succession,
                        naturalDeaths, marriagePlan.Chronicles, nextDate,
                        out string lifeCycleChronicleError))
                {
                    TryRestoreRandomState(rngBefore);
                    TryRestoreHistoryLedger(chronicleBefore);
                    LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                    LastEventMessage = "家族人口编年史写入失败：" +
                                       (string.IsNullOrEmpty(lifeCycleChronicleError)
                                           ? "未知原因"
                                           : lifeCycleChronicleError);
                    return false;
                }

                bool appendMonthly = settlement.Succeeded && settlement.Report != null &&
                                     (settlement.Report.AnyStaffed || settlement.Report.Vacated.Count > 0);
                if (appendMonthly)
                {
                    if (!EnsureHistoryLedger().TryAppendIndustry(previousDate.Year, previousDate.Month,
                            "industry.monthly", "settlement",
                            settlement.Report.Before.Funds, settlement.Report.After.Funds,
                            "产业月结", settlement.Report.Summary,
                            out _, out string chronicleError))
                    {
                        TryRestoreRandomState(rngBefore);
                        TryRestoreHistoryLedger(chronicleBefore);
                        LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                        LastEventMessage = "产业月结编年史写入失败：" +
                                           (string.IsNullOrEmpty(chronicleError) ? "未知原因" : chronicleError);
                        return false;
                    }
                }

                if (researchSettlement.Completed)
                {
                    string summary = BuildResearchChronicleSummary(researchSettlement, previousDate);
                    if (!EnsureHistoryLedger().TryAppendResearch(previousDate.Year, previousDate.Month,
                            researchSettlement.ResearchId, researchSettlement.TechniqueId,
                            researchSettlement.BeforeProgress, researchSettlement.AfterProgress,
                            "家族研究", summary, out _, out string researchChronicleError))
                    {
                        TryRestoreRandomState(rngBefore);
                        TryRestoreHistoryLedger(chronicleBefore);
                        LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                        LastEventMessage = "研究编年史写入失败：" +
                                           (string.IsNullOrEmpty(researchChronicleError)
                                               ? "未知原因"
                                               : researchChronicleError);
                        return false;
                    }
                }

                FamilyDoctrineState nextDoctrine = doctrineBefore.Clone();
                FamilyAmbitionRecord completedAmbition = AdvanceFamilyDoctrineForSettlement(
                    nextDoctrine, _succession.HeadPersonId,
                    settlement.Succeeded ? settlement.Report : null, researchSettlement.Completed,
                    researchSettlement.TechniqueId, _succession.DesignatedHeirId, educationRecords);
                if (completedAmbition != null &&
                    !TryAppendAmbitionCompletion(previousDate, _succession.HeadPersonId,
                        completedAmbition, out string ambitionChronicleError))
                {
                    TryRestoreRandomState(rngBefore);
                    TryRestoreHistoryLedger(chronicleBefore);
                    LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                    LastEventMessage = "抱负达成编年史写入失败：" +
                                       (string.IsNullOrEmpty(ambitionChronicleError)
                                           ? "未知原因"
                                           : ambitionChronicleError);
                    return false;
                }

                // The monthly jump consumes calendar days for in-flight actions exactly
                // once. Each simulated day resolves autonomy independently so a pilot
                // that completes early in the month is not stranded until the end.
                int elapsedDays = CountCalendarDays(previousDate, nextDate);
                var allOutcomes = new List<DailyActionResult>();
                int suppression = _dailyActionNotificationSuppression;
                _dailyActionNotificationSuppression++;
                try
                {
                    GameDate cursor = previousDate;
                    if (elapsedDays <= 0)
                    {
                        _industryState = nextIndustry;
                        _marketState = nextMarket;
                        _eventRuntimeState = nextRuntime;
                        _development = nextDevelopment;
                        _researchState = researchSettlement.NextState ?? researchBefore;
                        _familyDoctrine = nextDoctrine;
                        AdvanceAutonomyDay(cursor, out IReadOnlyList<DailyActionResult> none);
                        allOutcomes.AddRange(none);
                    }
                    for (int day = 0; day < elapsedDays; day++)
                    {
                        if (!WorldClock.AdvanceOneDay())
                            throw new InvalidOperationException(
                                "月度结算无法逐日推进到目标日期。");
                        if (day == elapsedDays - 1)
                        {
                            // Commit the monthly state before the final day's
                            // planning so the industry gate and the newly scheduled
                            // event runtime see the settled month exactly as the
                            // equivalent daily path would.
                            _industryState = nextIndustry;
                            _marketState = nextMarket;
                            _eventRuntimeState = nextRuntime;
                            _development = nextDevelopment;
                            _researchState = researchSettlement.NextState ?? researchBefore;
                            _familyDoctrine = nextDoctrine;
                        }
                        AdvanceAutonomyDay(cursor,
                            out IReadOnlyList<DailyActionResult> dayOutcomes);
                        allOutcomes.AddRange(dayOutcomes);
                        cursor = WorldClock.CurrentDate;
                    }
                }
                finally
                {
                    _dailyActionNotificationSuppression = suppression;
                }

                // M05: the commission consumes the same skipped calendar days, but it
                // settles only after the month's industry state is committed so its
                // reward is never overwritten by the monthly settlement snapshot. Each
                // step is stamped with the day it actually simulated, not the next
                // month's day 1 that the clock already holds at commit time.
                GameDate commissionCursor = previousDate;
                for (int day = 0; day < elapsedDays; day++)
                {
                    if (!GameDate.TryNext(commissionCursor, out GameDate commissionDay))
                        break;
                    commissionCursor = commissionDay;

                    // M08A: natural death takes effect at the Jan 1 annual boundary. A
                    // commission still running when the simulated date reaches that
                    // boundary is abandoned before the day advances or resolves, so
                    // death wins: no mission result, standing, history row or Missions
                    // roll can be produced for an assignee who died in this settlement.
                    // A commission already terminal on Dec 31 never reaches this branch.
                    if (commissionDay == nextDate && naturalDeaths.Count > 0 &&
                        !TryCloseCommissionForAnnualDeath(commissionDay, naturalDeaths,
                            out string annualDeathCommissionError))
                    {
                        throw new InvalidOperationException(annualDeathCommissionError);
                    }

                    AdvanceCommissionDay(commissionDay);
                }

                if (nextSuccession != null)
                {
                    // Sync the confirmed post-month location before adding the newborn
                    // row so the new idle row does not inherit the month-start location.
                    SyncCurrentLocationFromDailyActions();
                    _succession = nextSuccession;
                    EnsureDailyActions();
                }

                // M08A: a dead person keeps every recorded family fact but can no longer
                // hold an industry post, continue an in-flight daily action or execute a
                // running M05 commission.
                if (naturalDeaths.Count > 0)
                {
                    ReconcileFamilyLifeCycleAfterCommit(nextDate);
                    LastSuccessionMessage = string.IsNullOrEmpty(headDeathMessage)
                        ? LastSuccessionMessage
                        : headDeathMessage;
                }

                // Fault-injection seam: the date has already crossed, so any exception
                // below must restore the clock as well as resources/history/RNG.
                MonthSettlementFaultInjection?.Invoke();

                // M07: injuries recover only here, after the month date transition has
                // committed. A rejected or rolled-back month leaves the date unchanged and
                // therefore preserves the pre-boundary injury state.
                EnsureInjuries().RecoverAll();

                PublishDailyActionReport(allOutcomes);
                SyncCurrentLocationFromDailyActions();
                LastIndustryReport = settlement.Report;
                if (settlement.Report != null)
                    LastIndustryMessage = settlement.Report.Summary;
                LastMarketReport = marketSettlement.Report;
                LastResearchReport = researchSettlement.Report;
                LastResearchMessage = researchSettlement.Message;
                if (educationRecords != null && educationRecords.Count > 0)
                    LastEducationMessage = "年度培养已结算：" +
                        educationRecords.Count.ToString(CultureInfo.InvariantCulture) + " 人。";
                NoticeRegencyHandoffIfDue();
                LastEventMessage = string.Empty;
                LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
                SessionStateChanged?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                WorldClock.TryRestoreDate(previousDate);
                _industryState = industryBefore;
                _marketState = marketBefore;
                _eventRuntimeState = runtimeBefore;
                _development = developmentBefore;
                _researchState = researchBefore;
                _familyDoctrine = doctrineBefore;
                _dailyActions = dailyBefore;
                _npcAutonomy = autonomyBefore;
                _commission = commissionBefore;
                EnsureStanding().CopyFrom(standingBefore);
                EnsureInjuries().CopyFrom(injuriesBefore);
                EnsureMissionHistory().CopyFrom(missionHistoryBefore);
                LastCommissionMessage = commissionMessageBefore;
                CurrentLocation = locationBefore;
                LastDailyActionOutcomes = dailyOutcomesBefore;
                LastDailyActionMessage = dailyMessageBefore;
                bool successionRestored = true;
                if (successionBefore != null)
                {
                    successionRestored =
                        SuccessionSaveData.TryRestoreTransactionSnapshot(successionBefore,
                            out SuccessionState restoredSuccession, out string restoreError);
                    if (successionRestored)
                        _succession = restoredSuccession;
                    else if (!string.IsNullOrEmpty(restoreError))
                        Debug.LogWarning("M04B succession rollback failed: " + restoreError);
                }
                TryRestoreRandomState(rngBefore);
                TryRestoreHistoryLedger(chronicleBefore);
                LastIndustryReport = reportBefore;
                LastMarketReport = marketReportBefore;
                LastIndustryMessage = successionRestored
                    ? "月结异常，已回滚到本月初始状态：" + exception.Message
                    : "月结异常，家族快照未能恢复：" + exception.Message;
                LastEducationMessage = educationMessageBefore;
                LastResearchReport = researchReportBefore;
                LastResearchMessage = researchMessageBefore;
                LastEventMessage = successionRestored
                    ? "月结异常，日期、资源、成长、研究和编年史均保持不变：" +
                      exception.Message
                    : "月结异常，且家族快照恢复失败；当前家族状态可能不一致，请读取较早存档：" +
                      exception.Message;
                LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                Debug.LogWarning(successionRestored
                    ? $"SC-021 month settlement rolled back: {exception.Message}"
                    : $"M04B month settlement rollback incomplete: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 预览前往 destination 的旅行：返回目的地 ID、中文名称、耗时与预计到达 GameDate。
        /// 绝不修改当前日期或位置；可直接丢弃。
        /// 未知地点、当前位置、无直达路线或日期越界均返回 null。
        /// </summary>
        public TravelPreview? PreviewTravel(LocationId destination)
        {
            LastTravelBlockReason = string.Empty;
            if (WorldClock == null)
                InitNewGame();
            if (IsFamilyExtinct)
            {
                ReportSuccessionTravelPause();
                return null;
            }
            EnsureEventRuntimeState();
            if (_eventRuntimeState.HasPendingEvents)
                return null;
            if (SuccessionBlocksTimeAdvance)
            {
                ReportSuccessionTravelPause();
                return null;
            }
            if (!ValidateIndustryMonthConsistency())
            {
                LastTravelBlockReason = LastEventMessage;
                return null;
            }
            EnsureSuccession();
            if (HasActiveDailyAction(_succession.HeadPersonId))
            {
                LastTravelBlockReason = "当前家主已有进行中的行动；请先完成或中断行动，再直接旅行。";
                return null;
            }
            if (_commission.Stage == ShadowEscortStage.InProgress &&
                _commission.AssigneeId == _succession.HeadPersonId)
            {
                LastTravelBlockReason = "当前家主正在执行家族委托：请等待委托结算，再直接旅行。";
                return null;
            }

            if (!destination.IsValid)
                return null;
            if (destination == CurrentLocation)
                return null;
            if (!WorldMapRules.TryGetTravelDays(CurrentLocation, destination, out int days))
                return null;
            if (!GameDate.TryAddDays(WorldClock.CurrentDate, days, out GameDate arrival))
                return null;
            if (arrival.Year != WorldClock.CurrentDate.Year ||
                arrival.Month != WorldClock.CurrentDate.Month)
            {
                LastTravelBlockReason = "行程将跨越月末：请先结束本月，再安排旅行。";
                return null;
            }

            return new TravelPreview(destination, LocationId.GetChineseName(destination), days, arrival);
        }

        /// <summary>
        /// 确认旅行：重新校验目标/路线/耗时/日期后推进 WorldClock 并更新位置。
        /// 不盲信过期或伪造的预览数据；任何失败均返回 false 且日期与位置均保持不变。
        /// </summary>
        public bool ConfirmTravel(TravelPreview preview)
        {
            LastTravelBlockReason = string.Empty;
            if (RejectWhileTacticalMissionActive())
            {
                LastTravelBlockReason = LastEventMessage;
                return false;
            }
            if (WorldClock == null)
                InitNewGame();
            if (IsFamilyExtinct)
            {
                ReportSuccessionTravelPause();
                return false;
            }
            EnsureEventRuntimeState();
            if (_eventRuntimeState.HasPendingEvents)
                return false;
            if (SuccessionBlocksTimeAdvance)
            {
                ReportSuccessionTravelPause();
                return false;
            }
            if (!ValidateIndustryMonthConsistency())
            {
                LastTravelBlockReason = LastEventMessage;
                return false;
            }
            EnsureSuccession();
            if (HasActiveDailyAction(_succession.HeadPersonId))
            {
                LastTravelBlockReason = "当前家主已有进行中的行动；请先完成或中断行动，再直接旅行。";
                return false;
            }
            if (_commission.Stage == ShadowEscortStage.InProgress &&
                _commission.AssigneeId == _succession.HeadPersonId)
            {
                LastTravelBlockReason = "当前家主正在执行家族委托：请等待委托结算，再直接旅行。";
                return false;
            }

            if (!preview.Destination.IsValid)
                return false;
            if (preview.Destination == CurrentLocation)
                return false;
            if (!WorldMapRules.TryGetTravelDays(CurrentLocation, preview.Destination, out int days))
                return false;
            if (days != preview.TravelDays)
                return false;
            if (!GameDate.TryAddDays(WorldClock.CurrentDate, days, out GameDate arrival))
                return false;
            if (arrival != preview.EstimatedArrivalDate)
                return false;
            if (arrival.Year != WorldClock.CurrentDate.Year ||
                arrival.Month != WorldClock.CurrentDate.Month)
            {
                LastTravelBlockReason = "行程将跨越月末：请先结束本月，再安排旅行。";
                return false;
            }

            GameDate previousDate = WorldClock.CurrentDate;
            int suppression = _dailyActionNotificationSuppression;
            _dailyActionNotificationSuppression++;
            var allOutcomes = new List<DailyActionResult>();
            bool needsGrown = false;
            bool commissionChanged = false;
            GameDate cursor = previousDate;
            try
            {
                for (int day = 0; day < days; day++)
                {
                    if (!WorldClock.AdvanceOneDay())
                        return false;
                    needsGrown |= GrowNpcNeeds(1);
                    IReadOnlyList<DailyActionResult> outcomes =
                        EnsureDailyActions().AdvanceDays(cursor, 1);
                    allOutcomes.AddRange(outcomes);
                    ApplyNpcActionCompletions(outcomes);
                    // A direct trip simulates each travel day, so the commission must
                    // consume the same days and settle on the due day, exactly once.
                    commissionChanged |= AdvanceCommissionDay(WorldClock.CurrentDate);
                    cursor = WorldClock.CurrentDate;
                }

                CurrentLocation = preview.Destination;
                EnsureDailyActions().TrySetLocation(_succession.HeadPersonId,
                    preview.Destination);
                // Direct travel bypasses the day loop's planner, so explicitly run the
                // eligibility pass after the new location is authoritative.
                PlanNpcAfterAdvance(allOutcomes, NpcAutonomyTrigger.EligibilityChanged);
            }
            finally
            {
                _dailyActionNotificationSuppression = suppression;
            }

            PublishDailyActionReport(allOutcomes);
            SyncCurrentLocationFromDailyActions();
            LastTravelBlockReason = string.Empty;
            if (NoticeRegencyHandoffIfDue() || allOutcomes.Count > 0 || needsGrown ||
                commissionChanged)
                SessionStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Fixture-only date seam for legacy cross-year tests. It is internal, never
        /// used by UI or production paths, performs no production, and realigns the
        /// industry settled month so the save invariant stays valid.
        /// </summary>
        internal bool AdvanceDaysForLegacyFixtureSetup(int count)
        {
            if (WorldClock == null)
                InitNewGame();
            if (count <= 0 || !WorldClock.AdvanceDays(count))
                return false;
            EnsureIndustryState();
            _industryState.TrySetLastSettledMonthIndex(
                IndustryCalendar.MonthIndex(WorldClock.CurrentDate) - 1, out _);
            EnsureResearch().TrySetLastSettledMonthIndex(
                IndustryCalendar.MonthIndex(WorldClock.CurrentDate) - 1, out _);
            // M08B: this fixture seam skips whole months for the market exactly as it does
            // for the industry and research ledgers. Stock and both price indices are kept;
            // no skipped month's consumption, shortage or basket history is replayed.
            MarketState marketBeforeJump = EnsureMarket();
            if (MarketState.TryCreate(marketBeforeJump.FoodInventory,
                    marketBeforeJump.FoodPriceIndexPermille,
                    marketBeforeJump.MaterialsPriceIndexPermille,
                    IndustryCalendar.MonthIndex(WorldClock.CurrentDate) - 1, 0, null,
                    out MarketState marketAfterJump, out _))
                _marketState = marketAfterJump;
            LastMarketReport = null;
            LastIndustryReport = null;
            LastIndustryMessage = string.Empty;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastEventMessage = string.Empty;
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            return true;
        }

        private bool ValidateIndustryMonthConsistency()
        {
            EnsureIndustryState();
            if (IndustryStateIsConsistent)
                return true;
            LastMonthTurnOutcome = MonthTurnOutcome.IndustryStateInvalid;
            LastEventMessage = "产业状态与当前月份不一致：请读取稳定存档。";
            return false;
        }

        /// <summary>
        /// Daily-action commands funnel through here so a compound replan can commit the
        /// replacement before publishing exactly one state notification.
        /// </summary>
        private void NotifyDailyActionChanged()
        {
            if (_dailyActionNotificationSuppression > 0)
                return;
            SessionStateChanged?.Invoke();
        }

        /// <summary>
        /// One simulated day: needs grow, in-flight actions advance, completed pilot
        /// needs are reduced, then only idle/completed pilots may be replanned. Callers
        /// that advance several days must call this once per simulated day so a pilot
        /// that finishes early is not stranded until the end of a bulk range.
        /// </summary>
        private bool AdvanceAutonomyDay(GameDate previousDate,
            out IReadOnlyList<DailyActionResult> outcomes)
        {
            bool needsGrown = GrowNpcNeeds(1);
            outcomes = EnsureDailyActions().AdvanceDays(previousDate, 1);
            ApplyNpcActionCompletions(outcomes);
            bool autonomyChanged = PlanNpcAfterAdvance(outcomes);
            return needsGrown || autonomyChanged;
        }

        /// <summary>
        /// Publishes the aggregated M02 outcome/message channel after one or more
        /// simulated days. Autonomous pilot outcomes stay in the Inspector decision log
        /// and readable message but not in the player-issued outcome list.
        /// </summary>
        private void PublishDailyActionReport(IReadOnlyList<DailyActionResult> outcomes)
        {
            var playerOutcomes = new List<DailyActionResult>(outcomes.Count);
            foreach (DailyActionResult outcome in outcomes)
                if (!NpcAutonomyRules.IsPilot(outcome.PersonId))
                    playerOutcomes.Add(outcome);
            LastDailyActionOutcomes = playerOutcomes.AsReadOnly();
            if (outcomes.Count == 0)
                return;

            var builder = new StringBuilder();
            for (int i = 0; i < outcomes.Count; i++)
            {
                if (i > 0)
                    builder.Append("；");
                DailyActionResult outcome = outcomes[i];
                builder.Append(PersonDisplayName(outcome.PersonId, outcome.PersonId.Value));
                builder.Append(outcome.Reason);
            }
            LastDailyActionMessage = builder.ToString();
        }

        /// <summary>
        /// Mirrors the head's known location back to the legacy CurrentLocation surface.
        /// An in-transit head has no known location, so the last confirmed location stays
        /// visible and <see cref="IsPersonInTransit"/> remains the authority.
        /// </summary>
        private void SyncCurrentLocationFromDailyActions()
        {
            EnsureSuccession();
            if (_succession == null || _succession.HeadPersonId.IsEmpty)
                return;
            if (EnsureDailyActions().TryGetLocation(_succession.HeadPersonId, out LocationId location))
                CurrentLocation = location;
        }

        /// <summary>Number of calendar days advanced between two valid saved dates.</summary>
        private static int CountCalendarDays(GameDate from, GameDate to)
        {
            int days = 0;
            GameDate cursor = from;
            while (cursor < to)
            {
                if (!GameDate.TryNext(cursor, out GameDate next))
                    break;
                cursor = next;
                days++;
            }
            return days;
        }

        /// <summary>
        /// Interrupts actions of people who died, left the family or disappeared from the
        /// roster. This is a total cleanup: no production, no chronicle, no time advance.
        /// </summary>
        private void ReconcileDailyActions()
        {
            EnsureSuccession();
            DailyActionState actions = EnsureDailyActions();
            if (_succession.World == null)
                return;

            foreach (PersonState person in _succession.World.Persons)
                actions.EnsurePerson(person.Id, CurrentLocation);

            var outcomes = new List<DailyActionResult>();
            foreach (DailyActionPersonState row in actions.Persons)
            {
                if (!row.HasActiveAction)
                    continue;
                bool stillAvailable =
                    _succession.World.TryGetPerson(row.PersonId, out _) &&
                    _succession.World.IsLiving(row.PersonId) &&
                    _succession.World.Family.IsMember(row.PersonId);
                if (stillAvailable)
                    continue;
                if (actions.TryInterrupt(row.PersonId,
                        "人物离世或离开家族，行动已中断。", CurrentGameDate,
                        out DailyActionResult result))
                {
                    ApplyNpcActionInterruption(result);
                    outcomes.Add(result);
                }
            }

            if (outcomes.Count == 0)
                return;
            LastDailyActionOutcomes = outcomes.AsReadOnly();
            LastDailyActionMessage = "有 " +
                outcomes.Count.ToString(CultureInfo.InvariantCulture) +
                " 项行动因人物状态变化而中断。";
        }

        /// <summary>Interrupts one person's active action with a caller-supplied reason.</summary>
        private void InterruptDailyActionForPerson(PersonId personId, string reason)
        {
            if (personId.IsEmpty || _dailyActions == null)
                return;
            if (_dailyActions.TryInterrupt(personId, reason, CurrentGameDate,
                    out DailyActionResult result))
            {
                ApplyNpcActionInterruption(result);
                LastDailyActionOutcomes = new[] { result };
                LastDailyActionMessage = result.Reason;
            }
        }
    }
}
