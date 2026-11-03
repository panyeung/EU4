using System;
using System.Collections.Generic;
using System.Globalization;
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
        PendingSuccession = 5
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
        private RandomStateManager _randomStateManager;
        private EventRuntimeState _eventRuntimeState;
        private EventCatalogData _eventCatalog;
        private EventTextCatalog _eventTextCatalog;
        private EventEngine _eventEngine;
        private SuccessionState _succession;
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
                    CurrentGameDate, out _);
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
                    CurrentGameDate, out string reason);
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
                PersonId heir = M2FixedScenario.ExpectedHeir(Succession);
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

                PersonId heir = M2FixedScenario.ExpectedHeir(Succession);
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
            M2FixedScenario.BuildRoster(Succession, CurrentGameDate, Succession.DesignatedHeirId);

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

        private void InitSession(ulong initialSeed, SuccessionState succession)
        {
            WorldClock = new WorldClock();
            CurrentLocation = LocationId.Kiyosu;
            _relationships = new CharacterRelationships();
            _randomStateManager = RandomStateManager.Create(initialSeed);
            _eventRuntimeState = new EventRuntimeState();
            LoadEventContent();
            _supplyShortageMission = new SupplyShortageMission();
            _sealedLetterMission = new SealedLetterMission();
            _marketTallyMission = new MarketTallyMission();
            _succession = succession ?? throw new ArgumentNullException(nameof(succession));
            M2FixedScenario.SyncDesignation(_succession);
            _historyLedger = HistoryLedger.CreateEmpty();
            LastSuccessionMessage = string.Empty;
            _dialogues.Clear();
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
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
                    CurrentGameDate, out string gateReason))
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
                PersonId expected = M2FixedScenario.ExpectedHeir(_succession);
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
                M2FixedScenario.SyncDesignation(_succession);
            if (before.DiffersFrom(_succession))
                RecordSuccession(before, result);
            SessionStateChanged?.Invoke();
            return result;
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
                _succession = M2FixedScenario.CreateSuccession();
                M2FixedScenario.SyncDesignation(_succession);
            }
        }

        /// <summary>Captures the current runtime state as versioned save data.</summary>
        public SaveData CaptureSave()
        {
            EnsureRelationships();
            EnsureRandomState();
            EnsureEventRuntimeState();
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
                randomState = _randomStateManager.CaptureState(),
                eventRuntime = _eventRuntimeState.Capture(),
                succession = SuccessionSaveData.Capture(Succession),
                chronicle = ChronicleSaveCodec.Capture(EnsureHistoryLedger())
            };
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
            if (_eventCatalog == null ||
                (data.eventContentVersion != _eventCatalog.contentVersion &&
                 !(data.eventContentVersion == 1 && _eventCatalog.contentVersion == 2)))
                return InvalidSave("存档事件内容版本与当前目录不匹配。");
            if (!SuccessionSaveData.TryRestore(data.succession, date,
                    out SuccessionState succession, out string successionError))
                return InvalidSave(string.IsNullOrEmpty(successionError) ? "存档继承状态无效。" : successionError);
            if (!_eventEngine.TryRestoreState(data.eventRuntime, date, BuildEventRuleContext(succession),
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

            WorldClock = new WorldClock(date);
            CurrentLocation = location;
            _supplyShortageMission = supplyShortage;
            _sealedLetterMission = sealedLetter;
            _marketTallyMission = marketTally;
            _relationships = relationships;
            _randomStateManager = randomState;
            _eventRuntimeState = eventRuntime;
            _succession = succession;
            M2FixedScenario.SyncDesignation(_succession);
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
            EventChoiceResult result = _eventEngine.TryChooseEventOption(eventId, optionId, committedResultId,
                currentDate, _relationships, _eventRuntimeState, BuildEventRuleContext());
            if (result.Succeeded)
            {
                RecordEventChoice(result);
                foreach (RelationshipResult relationship in result.AppliedRelationships)
                    RecordRelationshipChange(relationship, "event");
                LastEventMessage = result.Feedback;
                SessionStateChanged?.Invoke();
            }
            return result;
        }

        /// <summary>
        /// Snapshot the SC-010 succession facts used by event offer gates. Settled count is
        /// the raw count only while no succession decision, regency, or extinction owns play.
        /// </summary>
        private EventRuleContext BuildEventRuleContext() => BuildEventRuleContext(Succession);

        /// <summary>
        /// Builds the same scenario/succession snapshot for live play and for an
        /// already-validated load candidate.
        /// </summary>
        private static EventRuleContext BuildEventRuleContext(SuccessionState succession)
        {
            if (succession == null)
                return EventRuleContext.Default;
            int settled = succession.Phase == SuccessionPhase.None ? succession.SuccessionCount : 0;
            string scenario = M2FixedScenario.IsActive(succession) ? EventScenarioIds.Sc013 : string.Empty;
            return new EventRuleContext(succession.SuccessionCount, settled, scenario);
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
            if (!WorldClock.AdvanceOneDay())
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
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
            if (!WorldClock.AdvanceDays(count))
            {
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            if (NoticeRegencyHandoffIfDue())
                SessionStateChanged?.Invoke();
            return true;
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

            RandomStateSaveData rngBefore = _randomStateManager.CaptureState();
            EventRuntimeState nextRuntime = _eventRuntimeState.Clone();
            EventScheduleResult schedule = _eventEngine.ScheduleMonthly(nextDate, _relationships,
                _randomStateManager.GetStream(RandomStreamKind.Events), nextRuntime, BuildEventRuleContext());
            if (!schedule.Succeeded)
            {
                RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                LastMonthTurnOutcome = MonthTurnOutcome.SchedulingFailed;
                LastEventMessage = schedule.Message;
                return false;
            }
            if (!WorldClock.TryAdvanceToNextMonth())
            {
                RandomStateManager.TryRestore(rngBefore, out _randomStateManager);
                LastMonthTurnOutcome = MonthTurnOutcome.DateOutOfRange;
                return false;
            }
            _eventRuntimeState = nextRuntime;
            NoticeRegencyHandoffIfDue();
            LastEventMessage = string.Empty;
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
            SessionStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 预览前往 destination 的旅行：返回目的地 ID、中文名称、耗时与预计到达 GameDate。
        /// 绝不修改当前日期或位置；可直接丢弃。
        /// 未知地点、当前位置、无直达路线或日期越界均返回 null。
        /// </summary>
        public TravelPreview? PreviewTravel(LocationId destination)
        {
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

            if (!destination.IsValid)
                return null;
            if (destination == CurrentLocation)
                return null;
            if (!WorldMapRules.TryGetTravelDays(CurrentLocation, destination, out int days))
                return null;
            if (!GameDate.TryAddDays(WorldClock.CurrentDate, days, out GameDate arrival))
                return null;

            return new TravelPreview(destination, LocationId.GetChineseName(destination), days, arrival);
        }

        /// <summary>
        /// 确认旅行：重新校验目标/路线/耗时/日期后推进 WorldClock 并更新位置。
        /// 不盲信过期或伪造的预览数据；任何失败均返回 false 且日期与位置均保持不变。
        /// </summary>
        public bool ConfirmTravel(TravelPreview preview)
        {
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

            if (!WorldClock.AdvanceDays(days))
                return false;
            CurrentLocation = preview.Destination;
            if (NoticeRegencyHandoffIfDue())
                SessionStateChanged?.Invoke();
            return true;
        }
    }
}
