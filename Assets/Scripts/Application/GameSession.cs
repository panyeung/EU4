using System;
using System.Collections.Generic;
using UnityEngine;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// 游戏会话 MonoBehaviour：持有 WorldClock 与当前位置，并提供
    /// 旅行预览与确认旅行的最小 API。
    /// 新局固定从 1560-04-01 开始，当前位置固定为清洲。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
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

        private SealedLetterMission _sealedLetterMission = new SealedLetterMission();
        private MarketTallyMission _marketTallyMission = new MarketTallyMission();
        private SupplyShortageMission _supplyShortageMission = new SupplyShortageMission();
        private CharacterRelationships _relationships;
        private readonly Dictionary<Guid, DialogueState> _dialogues = new Dictionary<Guid, DialogueState>();

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
            WorldClock = new WorldClock();
            CurrentLocation = LocationId.Kiyosu;
            _relationships = new CharacterRelationships();
            _supplyShortageMission = new SupplyShortageMission();
            _sealedLetterMission = new SealedLetterMission();
            _marketTallyMission = new MarketTallyMission();
            _dialogues.Clear();
        }

        /// <summary>Captures the current runtime state as versioned save data.</summary>
        public SaveData CaptureSave()
        {
            EnsureRelationships();
            GameDate date = WorldClock != null ? WorldClock.CurrentDate : SengokuChronicle.Domain.WorldClock.NewGameDate;

            return new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = date.Year,
                month = date.Month,
                day = date.Day,
                locationId = CurrentLocation.Value,
                supplyShortageStage = (int)_supplyShortageMission.Stage,
                sealedLetterStage = (int)_sealedLetterMission.Stage,
                marketTallyStage = (int)_marketTallyMission.Stage,
                relationships = CaptureRelationships()
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

            if (!GameDate.TryParse(data.year, data.month, data.day, out GameDate date))
                return InvalidSave("存档日期无效。");
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
            _dialogues.Clear();
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

        /// <summary>创建新游戏会话，日期固定为 1560-04-01，位置固定为清洲。</summary>
        public static GameSession NewGame()
        {
            var go = new GameObject(nameof(GameSession));
            var session = go.AddComponent<GameSession>();
            session.InitNewGame();
            return session;
        }

        /// <summary>推进一天。成功返回 true；失败返回 false 且状态不变。</summary>
        public bool AdvanceOneDay()
        {
            if (WorldClock == null)
                InitNewGame();
            return WorldClock.AdvanceOneDay();
        }

        /// <summary>推进多天。count 必须为正整数；失败返回 false 且状态不变。</summary>
        public bool AdvanceDays(int count)
        {
            if (WorldClock == null)
                InitNewGame();
            return WorldClock.AdvanceDays(count);
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
            return true;
        }
    }
}
