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
    public sealed class GameSession : MonoBehaviour
    {
        /// <summary>Documented fixed sample seed used by the reproducible FoundationScene demo.</summary>
        public const ulong DefaultRandomSeed = 0x5343414E4B4F3031UL;

        public WorldClock WorldClock { get; private set; }
        public LocationId CurrentLocation { get; private set; }
        public SupplyShortageStage SupplyShortageStage => _supplyShortageMission.Stage;
        public SealedLetterStage SealedLetterStage => _sealedLetterMission.Stage;
        public MarketTallyStage MarketTallyStage => _marketTallyMission.Stage;
        public int Merit => _supplyShortageMission.Merit + _sealedLetterMission.Merit + _marketTallyMission.Merit;

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
            CanEditIndustryAssignments, IndustryBlockReason, LastIndustryMessage);

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
        /// True only while the session still holds the pristine pre-succession standard
        /// roster; the fixed crisis demo must never replace completed family history.
        /// </summary>
        public bool CanStartCrisisDemo => IsPristineStandardSuccession(Succession);

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
                if (!M2FixedScenario.IsActive(_succession))
                    return true;
                return M2FixedScenario.CanBeginRetirement(_succession, EnsureEventRuntimeState(),
                    CurrentGameDate, _eventScenarioId, out _);
            }
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

                PersonId heir = M2FixedScenario.ExpectedHeir(Succession, _eventScenarioId);
                var filtered = new List<SuccessionCandidate>(1);
                foreach (SuccessionCandidate candidate in candidates)
                    if (candidate.PersonId == heir)
                        filtered.Add(candidate);
                return filtered.AsReadOnly();
            }
        }

        public IReadOnlyList<SuccessionCandidate> SuccessionRegentCandidates =>
            SuccessionService.GetRegentCandidates(Succession, CurrentGameDate);

        /// <summary>Read-only family roster rows for the SC-013 family panel.</summary>
        public IReadOnlyList<M2RosterRow> FamilyRoster =>
            M2FixedScenario.BuildRoster(Succession, CurrentGameDate, Succession.DesignatedHeirId,
                _eventScenarioId);

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
            InitSession(initialSeed, M2FixedScenario.CreateSuccession());

        /// <summary>Starts the separate SC-010 standard sample used by regression demos.</summary>
        public void InitStandardDemo() => InitStandardDemo(DefaultRandomSeed);

        /// <summary>Starts the separate SC-010 standard sample with a caller-selected seed.</summary>
        public void InitStandardDemo(ulong initialSeed) =>
            InitSession(initialSeed, SuccessionSample.CreateStandard());

        /// <summary>Starts the separate fixed SC-020 child-education demonstration.</summary>
        public void InitEducationDemoScenario() => InitEducationDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-020 demonstration with a caller-selected deterministic seed.</summary>
        public void InitEducationDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC020EducationScenario.CreateSuccession(),
                SC020EducationScenario.StartDate, SC020EducationScenario.CreateDevelopmentState());
        }

        /// <summary>Starts the separate fixed SC-023 seclusion and mentorship demonstration.</summary>
        public void InitSeclusionDemoScenario() => InitSeclusionDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-023 demonstration with a caller-selected deterministic seed.</summary>
        public void InitSeclusionDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC023SeclusionScenario.CreateSuccession(),
                SC023SeclusionScenario.StartDate, SC023SeclusionScenario.CreateDevelopmentState());
        }

        /// <summary>Starts the separate fixed SC-021 family-research demonstration.</summary>
        public void InitResearchDemoScenario() => InitResearchDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-021 demonstration with a caller-selected deterministic seed.</summary>
        public void InitResearchDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC021FamilyResearchScenario.CreateSuccession(),
                SC021FamilyResearchScenario.StartDate, null,
                SC021FamilyResearchScenario.CreateIndustryState());
        }

        /// <summary>Starts the separate fixed SC-022 marriage and adoption demonstration.</summary>
        public void InitFamilyFormationDemoScenario() =>
            InitFamilyFormationDemoScenario(DefaultRandomSeed);

        /// <summary>Starts the SC-022 demonstration with a caller-selected deterministic seed.</summary>
        public void InitFamilyFormationDemoScenario(ulong initialSeed)
        {
            InitSession(initialSeed, SC022FamilyFormationScenario.CreateSuccession(),
                SC022FamilyFormationScenario.StartDate, null);
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
                SC024FamilyDoctrineScenario.ScenarioId);
            EnsureFamilyDoctrine().TrySetCreed(FamilyCreedKind.RighteousnessFirst, out _);
            TryStartCurrentHeadAmbition(FamilyAmbitionKind.DevelopHousehold);
        }

        private void InitSession(ulong initialSeed, SuccessionState succession) =>
            InitSession(initialSeed, succession, WorldClock.NewGameDate, null);

        private void InitSession(ulong initialSeed, SuccessionState succession, GameDate startDate,
            CharacterDevelopmentState development) =>
            InitSession(initialSeed, succession, startDate, development, null);

        private void InitSession(ulong initialSeed, SuccessionState succession, GameDate startDate,
            CharacterDevelopmentState development, IndustryState industry) =>
            InitSession(initialSeed, succession, startDate, development, industry, null);

        private void InitSession(ulong initialSeed, SuccessionState succession, GameDate startDate,
            CharacterDevelopmentState development, IndustryState industry, string eventScenarioId)
        {
            WorldClock = new WorldClock(startDate);
            CurrentLocation = LocationId.Kiyosu;
            _relationships = new CharacterRelationships();
            _familyRelationships = FamilyRelationships.CreateEmpty();
            _randomStateManager = RandomStateManager.Create(initialSeed);
            _eventRuntimeState = new EventRuntimeState();
            LoadEventContent();
            _supplyShortageMission = new SupplyShortageMission();
            _sealedLetterMission = new SealedLetterMission();
            _marketTallyMission = new MarketTallyMission();
            _succession = succession ?? throw new ArgumentNullException(nameof(succession));
            M2FixedScenario.SyncDesignation(_succession, eventScenarioId);
            _dailyActions = CreateDailyActionsFor(_succession, CurrentLocation);
            _development = development ?? CreateDevelopmentFor(_succession);
            _industryState = industry ?? IndustryState.CreateInitial(WorldClock.CurrentDate);
            _researchState = FamilyResearchState.CreateInitial(WorldClock.CurrentDate);
            _familyDoctrine = new FamilyDoctrineState();
            _eventScenarioId = eventScenarioId ?? string.Empty;
            LastFamilyDoctrineMessage = string.Empty;
            LastIndustryReport = null;
            LastIndustryMessage = string.Empty;
            LastEducationMessage = string.Empty;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastFamilyFormationMessage = string.Empty;
            LastTravelBlockReason = string.Empty;
            _historyLedger = HistoryLedger.CreateEmpty();
            LastSuccessionMessage = string.Empty;
            _dialogues.Clear();
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            LastDailyActionOutcomes = new DailyActionResult[0];
            LastDailyActionMessage = string.Empty;
            SessionStateChanged?.Invoke();
        }

        /// <summary>
        /// Starts the fixed SC-010 crisis roster only from the pristine pre-succession
        /// standard state. A completed succession, crisis adoption or regency must not
        /// be silently erased by replacing the permanent family snapshot.
        /// </summary>
        public bool StartCrisisDemo()
        {
            EnsureSuccession();
            if (!CanStartCrisisDemo)
                return false;
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
                PersonId expected = M2FixedScenario.ExpectedHeir(_succession, _eventScenarioId);
                if (personId != expected)
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
            SuccessionSnapshot before = SuccessionSnapshot.Capture(_succession);
            return ApplySuccessionResult(before,
                SuccessionService.ConfirmSuccession(_succession, CurrentGameDate));
        }

        public SuccessionResult AcceptCrisisAdoption()
        {
            EnsureSuccession();
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
            if (!CanEditIndustryAssignments)
            {
                LastIndustryMessage = string.IsNullOrEmpty(IndustryBlockReason)
                    ? "当前不能调整产业任命。"
                    : IndustryBlockReason;
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.Blocked,
                    LastIndustryMessage, default, default);
            }

            IndustryAssignmentResult result = IndustryService.TryAssign(_industryState, _succession,
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
                relationships = CaptureRelationships(),
                familyRelationships = CaptureFamilyRelationships(),
                randomState = _randomStateManager.CaptureState(),
                eventRuntime = _eventRuntimeState.Capture(),
                succession = SuccessionSaveData.Capture(Succession),
                chronicle = ChronicleSaveCodec.Capture(EnsureHistoryLedger()),
                industry = IndustryStateSaveData.Capture(EnsureIndustryState()),
                development = CharacterDevelopmentSaveData.Capture(EnsureDevelopment()),
                research = FamilyResearchSaveData.Capture(EnsureResearch()),
                familyDoctrine = FamilyDoctrineSaveData.Capture(EnsureFamilyDoctrine()),
                activeEventScenarioId = _eventScenarioId ?? string.Empty,
                dailyActions = DailyActionStateSaveData.Capture(EnsureDailyActions())
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

            // Resolve and validate the saved route before the event runtime, so SC-024
            // history is accepted only in the matching sc024 route. SC-024 reuses the
            // M2 roster, so a tagged route is valid only for that fixed family.
            string eventScenarioId = data.activeEventScenarioId ?? string.Empty;
            if (eventScenarioId.Length > 0 && !M2FixedScenario.IsActive(succession))
                return InvalidSave("存档活动事件路线与当前家族不符。");

            if (!_eventEngine.TryRestoreState(data.eventRuntime, date,
                    BuildEventRuleContext(succession, eventScenarioId),
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
            _relationships = relationships;
            _familyRelationships = familyRelationships;
            _randomStateManager = randomState;
            _eventRuntimeState = eventRuntime;
            _succession = succession;
            M2FixedScenario.SyncDesignation(_succession, eventScenarioId);
            _industryState = industry;
            _development = development;
            _researchState = research;
            _familyDoctrine = familyDoctrine;
            _dailyActions = dailyActions;
            _eventScenarioId = eventScenarioId;
            LastFamilyDoctrineMessage = string.Empty;
            LastIndustryReport = null;
            LastIndustryMessage = string.Empty;
            LastEducationMessage = string.Empty;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastFamilyFormationMessage = string.Empty;
            LastTravelBlockReason = string.Empty;
            LastDailyActionOutcomes = new DailyActionResult[0];
            LastDailyActionMessage = string.Empty;
            _historyLedger = historyLedger;
            LastSuccessionMessage = string.Empty;
            NoticeRegencyHandoffIfDue();
            _dialogues.Clear();
            LastEventMessage = string.Empty;
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            SessionStateChanged?.Invoke();
            return SaveOperationResult.Success();
        }

        /// <summary>Saves the current session to the supplied single slot.</summary>
        public SaveOperationResult SaveTo(SaveSlotStore store)
        {
            if (store == null)
                return SaveOperationResult.Failure(SaveErrorCode.WriteFailed, "存档槽为空。");

            return store.Save(CaptureSave());
        }

        /// <summary>Loads the supplied single slot and applies it atomically.</summary>
        public SaveOperationResult LoadFrom(SaveSlotStore store)
        {
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
            int beforeStage, int afterStage, bool succeeded, string feedback)
        {
            if (!succeeded || beforeStage == afterStage)
                return;

            GameDate date = CurrentGameDate;
            if (!EnsureHistoryLedger().TryAppendMission(date.Year, date.Month, missionId, actionId,
                    beforeStage, afterStage, missionName, feedback, out _, out string error))
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
            return _eventEngine.TryGetOptionAvailability(eventId, optionId, _relationships, out enabled, out reason);
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
            HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
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
            BuildEventRuleContext(Succession, _eventScenarioId);

        /// <summary>
        /// Builds the same scenario/succession snapshot for live play and for an
        /// already-validated load candidate.
        /// </summary>
        private static EventRuleContext BuildEventRuleContext(SuccessionState succession) =>
            BuildEventRuleContext(succession, string.Empty);

        private static EventRuleContext BuildEventRuleContext(SuccessionState succession,
            string eventScenarioId)
        {
            if (succession == null || succession.World == null)
                return EventRuleContext.Default;
            int settled = succession.Phase == SuccessionPhase.None ? succession.SuccessionCount : 0;
            string scenario = !string.IsNullOrWhiteSpace(eventScenarioId)
                ? eventScenarioId
                : M2FixedScenario.IsActive(succession) ? EventScenarioIds.Sc013 : string.Empty;
            IReadOnlyList<FamilyMembership> memberships = succession.World.Family.Members;
            var members = new PersonId[memberships.Count];
            for (int i = 0; i < memberships.Count; i++)
                members[i] = memberships[i].PersonId;
            return new EventRuleContext(succession.SuccessionCount, settled, scenario,
                succession.HeadPersonId, members);
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
            if (EnsureDailyActions().HasActiveAction(personId))
            {
                reason = "该人物已有进行中的行动；不能同时执行两个行动。";
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
            IReadOnlyList<DailyActionResult> outcomesBefore = LastDailyActionOutcomes;
            string messageBefore = LastDailyActionMessage;
            _dailyActionNotificationSuppression++;
            try
            {
                if (!TryInterruptDailyAction(personId, "重新规划：取消原行动。"))
                {
                    reason = LastDailyActionMessage;
                    LastDailyActionOutcomes = outcomesBefore;
                    LastDailyActionMessage = messageBefore;
                    return false;
                }
                DailyActionResult interrupted = LastDailyActionOutcomes[0];
                if (!TryPlanDailyAction(personId, kind, durationDays, destination, out reason))
                {
                    _dailyActions = before;
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
            ApplyDailyActionAdvance(previousDate, 1);
            SyncCurrentLocationFromDailyActions();
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            if (NoticeRegencyHandoffIfDue())
                SessionStateChanged?.Invoke();
            return true;
        }

        /// <summary>推进多天。count 必须为正整数；失败返回 false 且状态不变，并说明原因。</summary>
        public bool AdvanceDays(int count)
        {
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
            if (!WorldClock.AdvanceDays(count))
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            ApplyDailyActionAdvance(previousDate, count);
            SyncCurrentLocationFromDailyActions();
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            if (NoticeRegencyHandoffIfDue())
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
            LocationId locationBefore = CurrentLocation;
            RandomStateSaveData rngBefore = _randomStateManager.CaptureState();
            FamilyChronicleEntry[] chronicleBefore = EnsureHistoryLedger().Capture();
            EventRuntimeState runtimeBefore = _eventRuntimeState.Clone();
            IndustryMonthReport reportBefore = LastIndustryReport;
            string industryMessageBefore = LastIndustryMessage;
            string educationMessageBefore = LastEducationMessage;
            FamilyResearchMonthReport researchReportBefore = LastResearchReport;
            string researchMessageBefore = LastResearchMessage;
            IReadOnlyList<DailyActionResult> dailyOutcomesBefore = LastDailyActionOutcomes;
            string dailyMessageBefore = LastDailyActionMessage;
            try
            {
                IndustrySettlementResult settlement = IndustrySettlement.Plan(
                    _industryState, Succession, previousDate);
                if (settlement.Outcome == IndustrySettlementOutcome.SettlementGap ||
                    settlement.Outcome == IndustrySettlementOutcome.InvalidState)
                {
                    LastMonthTurnOutcome = MonthTurnOutcome.IndustryStateInvalid;
                    LastEventMessage = settlement.Message;
                    return false;
                }

                IndustryState nextIndustry = settlement.NextState ?? industryBefore.Clone();
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
                        HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
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
                    RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                    HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
                    LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                    LastEventMessage = schedule.Message;
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
                        RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                        HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
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
                        RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                        HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
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
                    RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                    HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
                    LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                    LastEventMessage = "抱负达成编年史写入失败：" +
                                       (string.IsNullOrEmpty(ambitionChronicleError)
                                           ? "未知原因"
                                           : ambitionChronicleError);
                    return false;
                }

                if (!WorldClock.TryAdvanceToNextMonth())
                {
                    RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                    HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
                    _industryState = industryBefore;
                    LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                    return false;
                }

                // The monthly jump consumes calendar days for in-flight actions exactly
                // once, before the fault seam so the transaction owns the result too.
                int elapsedDays = CountCalendarDays(previousDate, nextDate);
                if (elapsedDays > 0)
                    ApplyDailyActionAdvance(previousDate, elapsedDays);

                // Fault-injection seam: the date has already crossed, so any exception
                // below must restore the clock as well as resources/history/RNG.
                MonthSettlementFaultInjection?.Invoke();

                _industryState = nextIndustry;
                _eventRuntimeState = nextRuntime;
                _development = nextDevelopment;
                _researchState = researchSettlement.NextState ?? researchBefore;
                _familyDoctrine = nextDoctrine;
                SyncCurrentLocationFromDailyActions();
                LastIndustryReport = settlement.Report;
                if (settlement.Report != null)
                    LastIndustryMessage = settlement.Report.Summary;
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
                _eventRuntimeState = runtimeBefore;
                _development = developmentBefore;
                _researchState = researchBefore;
                _familyDoctrine = doctrineBefore;
                _dailyActions = dailyBefore;
                CurrentLocation = locationBefore;
                LastDailyActionOutcomes = dailyOutcomesBefore;
                LastDailyActionMessage = dailyMessageBefore;
                RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                HistoryLedger.TryRestore(chronicleBefore, out _historyLedger, out _);
                LastIndustryReport = reportBefore;
                LastIndustryMessage = "月结异常，已回滚到本月初始状态：" + exception.Message;
                LastEducationMessage = educationMessageBefore;
                LastResearchReport = researchReportBefore;
                LastResearchMessage = researchMessageBefore;
                LastEventMessage = "月结异常，日期、资源、成长、研究和编年史均保持不变：" +
                                   exception.Message;
                LastMonthTurnOutcome = MonthTurnOutcome.SettlementFailed;
                Debug.LogWarning($"SC-021 month settlement rolled back: {exception.Message}");
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

            if (!WorldClock.AdvanceDays(days))
                return false;
            CurrentLocation = preview.Destination;
            EnsureDailyActions().TrySetLocation(_succession.HeadPersonId, preview.Destination);
            LastTravelBlockReason = string.Empty;
            if (NoticeRegencyHandoffIfDue())
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

        /// <summary>Advances in-flight actions by explicit calendar days and records outcomes.</summary>
        private void ApplyDailyActionAdvance(GameDate previousDate, int days)
        {
            IReadOnlyList<DailyActionResult> outcomes = EnsureDailyActions().AdvanceDays(
                previousDate, days);
            LastDailyActionOutcomes = outcomes;
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
                    outcomes.Add(result);
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
                LastDailyActionOutcomes = new[] { result };
                LastDailyActionMessage = result.Reason;
            }
        }
    }
}
