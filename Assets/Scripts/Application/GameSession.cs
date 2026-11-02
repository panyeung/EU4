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
        private readonly Dictionary<Guid, DialogueState> _dialogues = new Dictionary<Guid, DialogueState>();

        public event Action SessionStateChanged;
        public string LastEventMessage { get; private set; } = string.Empty;
        public MonthTurnOutcome LastMonthTurnOutcome { get; private set; } = MonthTurnOutcome.Succeeded;
        public string PendingEventId => _eventRuntimeState != null ? _eventRuntimeState.PendingEventId : string.Empty;
        public bool EventCatalogReady => _eventEngine != null && _eventEngine.IsReady;
        public EventCatalogData EventCatalog => _eventCatalog;
        public EventTextCatalog EventText => _eventTextCatalog;

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

        public string SuccessionHeadName =>
            Succession.TryGetHead(out PersonState head) ? head.DisplayName : string.Empty;

        public IReadOnlyList<SuccessionCandidate> SuccessionCandidates =>
            SuccessionService.GetCandidates(Succession, CurrentGameDate);

        public IReadOnlyList<SuccessionCandidate> SuccessionRegentCandidates =>
            SuccessionService.GetRegentCandidates(Succession, CurrentGameDate);

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
            if (hasEffect) relationship = _relationships.TryChange(selected.EffectTarget, selected.RelationshipDelta);
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

        /// <summary>Starts a reproducible new game with a caller-selected stable seed.</summary>
        public void InitNewGame(ulong initialSeed)
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
            _succession = SuccessionSample.CreateStandard();
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

        public SuccessionResult BeginSuccessionRetirement() =>
            ApplySuccessionResult(SuccessionService.BeginRetirement(Succession, CurrentGameDate));

        public SuccessionResult SelectSuccessionSuccessor(PersonId personId) =>
            ApplySuccessionResult(SuccessionService.SelectSuccessor(Succession, personId, CurrentGameDate));

        public SuccessionResult SelectSuccessionRegent(PersonId personId) =>
            ApplySuccessionResult(SuccessionService.SelectRegent(Succession, personId, CurrentGameDate));

        public SuccessionResult ConfirmSuccession() =>
            ApplySuccessionResult(SuccessionService.ConfirmSuccession(Succession, CurrentGameDate));

        public SuccessionResult AcceptCrisisAdoption() =>
            ApplySuccessionResult(SuccessionService.AcceptCrisisAdoption(Succession, CurrentGameDate));

        public SuccessionResult DeclineCrisis() =>
            ApplySuccessionResult(SuccessionService.DeclineCrisis(Succession));

        /// <summary>Completes a due regency after time advances; never advances time itself.</summary>
        public SuccessionResult TryCompleteSuccessionRegency() =>
            ApplySuccessionResult(SuccessionService.TryCompleteRegency(Succession, CurrentGameDate));

        private SuccessionResult ApplySuccessionResult(SuccessionResult result)
        {
            if (!result.Succeeded)
                return result;
            LastSuccessionMessage = result.Message;
            SessionStateChanged?.Invoke();
            return result;
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
                _succession = SuccessionSample.CreateStandard();
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
                succession = SuccessionSaveData.Capture(Succession)
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
            if (_eventCatalog == null || data.eventContentVersion != _eventCatalog.contentVersion)
                return InvalidSave("存档事件内容版本与当前目录不匹配。");
            if (!_eventEngine.TryRestoreState(data.eventRuntime, date,
                    out EventRuntimeState eventRuntime, out string eventError))
                return InvalidSave(string.IsNullOrEmpty(eventError) ? "存档事件状态无效。" : eventError);
            if (!SuccessionSaveData.TryRestore(data.succession, date,
                    out SuccessionState succession, out string successionError))
                return InvalidSave(string.IsNullOrEmpty(successionError) ? "存档继承状态无效。" : successionError);

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
            return _sealedLetterMission.TryAccept(CurrentLocation);
        }

        public SealedLetterActionResult TryDeliverSealedLetter()
        {
            return _sealedLetterMission.TryDeliver(CurrentLocation);
        }

        public MarketTallyActionResult TryAcceptMarketTallyMission()
        {
            return _marketTallyMission.TryAccept(CurrentLocation);
        }

        public MarketTallyActionResult TryInvestigateMarketTally()
        {
            return _marketTallyMission.TryInvestigate(CurrentLocation);
        }

        public MarketTallyActionResult TryReportMarketTally()
        {
            return _marketTallyMission.TryReport(CurrentLocation);
        }

        public SupplyShortageActionResult TryAcceptSupplyShortageMission()
        {
            return _supplyShortageMission.TryAccept(CurrentLocation);
        }

        public SupplyShortageActionResult TryInvestigateSupplyShortage()
        {
            return _supplyShortageMission.TryInvestigate(CurrentLocation);
        }

        public SupplyShortageActionResult TryNegotiateSupplyShortage(CharacterId merchantId)
        {
            return _supplyShortageMission.TryNegotiate(CurrentLocation, merchantId);
        }

        public SupplyShortageActionResult TryReportSupplyShortage()
        {
            return _supplyShortageMission.TryReport(CurrentLocation);
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
            return _relationships.TryChange(npcId, delta);
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

        private void EnsureEventRuntimeState()
        {
            if (_eventRuntimeState == null)
            {
                _eventRuntimeState = new EventRuntimeState();
                LoadEventContent();
            }
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
                currentDate, _relationships, _eventRuntimeState);
            if (result.Succeeded)
            {
                LastEventMessage = result.Feedback;
                SessionStateChanged?.Invoke();
            }
            return result;
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
                _randomStateManager.GetStream(RandomStreamKind.Events), nextRuntime);
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
