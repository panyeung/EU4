using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Validates untrusted save data before it is written or applied.
    /// Returns the first problem found; never throws on malformed input.
    /// </summary>
    public static class SaveDataValidator
    {
        public const int CurrentVersion = 3;

        public static SaveOperationResult Validate(SaveData data)
        {
            if (data == null)
                return Invalid("存档数据为空。");

            if (data.saveFormatVersion != CurrentVersion)
                return SaveOperationResult.Failure(
                    SaveErrorCode.UnsupportedVersion,
                    $"不支持的存档版本：{data.saveFormatVersion}（当前版本 {CurrentVersion}）。");

            if (!GameDate.IsValidDate(data.year, data.month, data.day))
                return Invalid($"存档日期无效：{data.year:0000}-{data.month:00}-{data.day:00}。");

            if (string.IsNullOrWhiteSpace(data.gameVersion) || data.rulesVersion <= 0 || data.eventContentVersion <= 0)
                return Invalid("存档版本元数据无效。");
            if (data.rulesVersion != SaveData.CurrentRulesVersion)
                return SaveOperationResult.Failure(SaveErrorCode.UnsupportedVersion,
                    "存档规则版本当前不受支持。");

            if (!LocationId.TryParse(data.locationId, out _))
                return Invalid($"存档地点无效：{data.locationId ?? "(null)"}。");

            if (!Enum.IsDefined(typeof(SupplyShortageStage), data.supplyShortageStage))
                return Invalid("补给短缺任务阶段无效。");
            if (!Enum.IsDefined(typeof(SealedLetterStage), data.sealedLetterStage))
                return Invalid("密信任务阶段无效。");
            if (!Enum.IsDefined(typeof(MarketTallyStage), data.marketTallyStage))
                return Invalid("市集账目任务阶段无效。");

            SaveOperationResult relationships = ValidateRelationships(data.relationships);
            if (!relationships.Succeeded)
                return relationships;

            if (data.succession == null)
                return Invalid("存档缺少继承状态（succession）。");
            SaveOperationResult succession = data.succession.ValidateForSave(
                new GameDate(data.year, data.month, data.day));
            if (!succession.Succeeded)
                return succession;

            if (!RandomStateManager.TryRestore(data.randomState, out _))
                return Invalid("存档随机流状态无效。");

            return ValidateEventRuntimeSnapshot(data.eventRuntime,
                new GameDate(data.year, data.month, data.day));
        }

        /// <summary>
        /// Rejects v3 JSON that omitted a required field instead of accepting the
        /// defaults that <see cref="SaveData"/>'s constructor would otherwise supply.
        /// Legacy v1/v2 payloads intentionally lack the v3 succession block; the
        /// migrator owns their documented defaults.
        /// </summary>
        /// <param name="probe">
        /// Parsed scalar fields plus defensive null checks. Unity's JsonUtility still
        /// materializes nested <c>[Serializable]</c> classes when their property is
        /// absent, so reference-type probe fields cannot prove presence by themselves.
        /// </param>
        /// <param name="json">Raw save JSON used to confirm top-level property presence.</param>
        public static SaveOperationResult ValidatePresence(SaveDataPresenceProbe probe, string json)
        {
            if (probe == null || string.IsNullOrWhiteSpace(json))
                return Invalid("存档数据为空。");

            if (probe.saveFormatVersion == 1)
                return SaveOperationResult.Success();

            if (probe.saveFormatVersion != 2 && probe.saveFormatVersion != CurrentVersion)
                return SaveOperationResult.Failure(
                    SaveErrorCode.UnsupportedVersion,
                    $"不支持的存档版本：{probe.saveFormatVersion}（当前版本 {CurrentVersion}）。");

            if (probe.saveFormatVersion == 2 && HasRootProperty(json, "succession"))
                return Invalid("v2 存档包含继承状态（succession）；迁移不会静默替换该数据。");

            if (string.IsNullOrWhiteSpace(probe.gameVersion) || probe.rulesVersion <= 0 ||
                probe.eventContentVersion <= 0)
                return Invalid("存档缺少必需的版本元数据。");
            if (probe.randomState == null || !HasRootProperty(json, "randomState"))
                return Invalid("存档缺少随机流状态（randomState）。");
            if (probe.eventRuntime == null || !HasRootProperty(json, "eventRuntime"))
                return Invalid("存档缺少事件运行状态（eventRuntime）。");
            if (probe.saveFormatVersion == CurrentVersion &&
                (probe.succession == null || !HasRootProperty(json, "succession")))
                return Invalid("存档缺少继承状态（succession）。");

            return SaveOperationResult.Success();
        }

        /// <summary>
        /// Returns true when <paramref name="propertyName"/> is a direct property of
        /// the root JSON object. Values nested inside child objects or arrays do not
        /// count, and strings are skipped so text content cannot masquerade as a key.
        /// </summary>
        internal static bool HasRootProperty(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(propertyName))
                return false;

            int depth = 0;
            int index = 0;
            while (index < json.Length)
            {
                char current = json[index];
                if (current == '"')
                {
                    int contentStart = ++index;
                    bool escaped = false;
                    while (index < json.Length)
                    {
                        char character = json[index];
                        if (escaped)
                            escaped = false;
                        else if (character == '\\')
                            escaped = true;
                        else if (character == '"')
                            break;
                        index++;
                    }

                    if (index >= json.Length)
                        return false;

                    int contentLength = index - contentStart;
                    index++;

                    if (depth == 1 && contentLength == propertyName.Length &&
                        string.CompareOrdinal(json, contentStart, propertyName, 0, contentLength) == 0)
                    {
                        int lookahead = index;
                        while (lookahead < json.Length && char.IsWhiteSpace(json[lookahead]))
                            lookahead++;
                        if (lookahead < json.Length && json[lookahead] == ':')
                            return true;
                    }

                    continue;
                }

                if (current == '{' || current == '[')
                {
                    depth++;
                }
                else if (current == '}' || current == ']')
                {
                    if (depth == 0)
                        return false;
                    depth--;
                }

                index++;
            }

            return false;
        }

        internal static SaveOperationResult ValidateLegacyV1Fields(SaveData data)
        {
            if (data == null)
                return Invalid("存档数据为空。");
            if (!GameDate.IsValidDate(data.year, data.month, data.day))
                return Invalid("存档日期无效。");
            if (!LocationId.TryParse(data.locationId, out _))
                return Invalid("存档地点无效。");
            if (!Enum.IsDefined(typeof(SupplyShortageStage), data.supplyShortageStage) ||
                !Enum.IsDefined(typeof(SealedLetterStage), data.sealedLetterStage) ||
                !Enum.IsDefined(typeof(MarketTallyStage), data.marketTallyStage))
                return Invalid("存档任务阶段无效。");
            return ValidateRelationships(data.relationships);
        }

        /// <summary>
        /// v2 already owns RNG and event runtime state but has no succession block;
        /// the migrator supplies the deterministic default roster.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV2Fields(SaveData data)
        {
            SaveOperationResult legacy = ValidateLegacyV1Fields(data);
            if (!legacy.Succeeded)
                return legacy;
            if (!RandomStateManager.TryRestore(data.randomState, out _))
                return Invalid("存档随机流状态无效。");
            return ValidateEventRuntimeSnapshot(data.eventRuntime,
                new GameDate(data.year, data.month, data.day));
        }

        private static SaveOperationResult ValidateRelationships(RelationshipSaveEntry[] entries)
        {
            if (entries == null)
                return SaveOperationResult.Success();

            var seen = new HashSet<CharacterId>();
            foreach (RelationshipSaveEntry entry in entries)
            {
                if (entry == null)
                    return Invalid("关系记录为空。");

                if (!CharacterId.TryParse(entry.characterId, out CharacterId id) ||
                    !CharacterCatalog.TryGet(id, out CharacterDefinition definition) ||
                    definition.Role != CharacterRole.Npc)
                    return Invalid($"关系目标不是已知 NPC：{entry.characterId ?? "(null)"}。");

                if (!seen.Add(id))
                    return Invalid($"关系目标重复：{entry.characterId}。");

                if (entry.value < CharacterRelationships.MinimumValue ||
                    entry.value > CharacterRelationships.MaximumValue)
                    return Invalid($"关系值超出范围：{entry.characterId}={entry.value}。");
            }

            return SaveOperationResult.Success();
        }

        /// <summary>
        /// Validates one event-runtime snapshot. When a date is supplied, a pending
        /// event that is still on cooldown at that date is rejected; a pending event
        /// that is already marked once-per-game is always rejected.
        /// </summary>
        public static SaveOperationResult ValidateEventRuntimeSnapshot(EventRuntimeSaveData state, GameDate? currentDate)
        {
            if (state == null || !Enum.IsDefined(typeof(StableGamePhase), state.phase) ||
                state.pendingEventIds == null || state.cooldowns == null || state.onceEventIds == null ||
                state.history == null || state.committedResultIds == null)
                return Invalid("存档事件状态为空或阶段无效。");

            if ((state.phase == (int)StableGamePhase.AwaitingEventChoice) != (state.pendingEventIds.Length > 0))
                return Invalid("存档阶段与待处理事件队列不一致。");

            var pendingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string eventId in state.pendingEventIds)
                if (string.IsNullOrWhiteSpace(eventId) || !pendingIds.Add(eventId))
                    return Invalid("待处理事件 ID 为空或重复。");

            var cooldownMonths = new Dictionary<string, int>(StringComparer.Ordinal);
            string previousCooldownId = null;
            foreach (EventCooldownSaveEntry cooldown in state.cooldowns)
            {
                if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.eventId) || cooldown.availableAtMonth < 0 ||
                    cooldownMonths.ContainsKey(cooldown.eventId))
                    return Invalid("事件冷却标记无效或重复。");
                if (previousCooldownId != null && string.CompareOrdinal(previousCooldownId, cooldown.eventId) > 0)
                    return Invalid("事件冷却标记顺序不稳定。");
                previousCooldownId = cooldown.eventId;
                cooldownMonths.Add(cooldown.eventId, cooldown.availableAtMonth);
            }

            if (!ValidateUniqueIds(state.onceEventIds))
                return Invalid("一次性事件标记为空、重复或未按稳定顺序排列。");
            if (!ValidateUniqueIds(state.committedResultIds))
                return Invalid("已提交结果标记为空、重复或未按稳定顺序排列。");

            var onceIds = new HashSet<string>(state.onceEventIds, StringComparer.Ordinal);
            long currentMonthIndex = currentDate.HasValue ? MonthIndex(currentDate.Value) : long.MinValue;
            foreach (string pendingId in state.pendingEventIds)
            {
                if (onceIds.Contains(pendingId))
                    return Invalid($"待处理事件与一次性标记冲突：{pendingId}。");
                if (currentDate.HasValue && cooldownMonths.TryGetValue(pendingId, out int availableAtMonth) &&
                    availableAtMonth > currentMonthIndex)
                    return Invalid($"待处理事件仍在冷却中：{pendingId}。");
            }

            var committed = new HashSet<string>(state.committedResultIds, StringComparer.Ordinal);
            var historyResults = new HashSet<string>(StringComparer.Ordinal);
            foreach (EventHistorySaveEntry history in state.history)
            {
                if (history == null || string.IsNullOrWhiteSpace(history.eventId) ||
                    string.IsNullOrWhiteSpace(history.optionId) || string.IsNullOrWhiteSpace(history.resultId) ||
                    !GameDate.IsValidDate(history.year, history.month, 1) ||
                    !historyResults.Add(history.resultId) || !committed.Contains(history.resultId))
                    return Invalid("事件历史记录无效，或结果未登记为已提交。");
            }

            return SaveOperationResult.Success();
        }

        private static long MonthIndex(GameDate date) => (long)date.Year * 12L + date.Month;

        private static bool ValidateUniqueIds(string[] ids)
        {
            if (ids == null)
                return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string previous = null;
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id) ||
                    (previous != null && string.CompareOrdinal(previous, id) > 0))
                    return false;
                previous = id;
            }
            return true;
        }

        private static SaveOperationResult Invalid(string message)
        {
            return SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
        }
    }
}
