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
        public const int CurrentVersion = 1;

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

            if (!LocationId.TryParse(data.locationId, out _))
                return Invalid($"存档地点无效：{data.locationId ?? "(null)"}。");

            if (!Enum.IsDefined(typeof(SupplyShortageStage), data.supplyShortageStage))
                return Invalid("补给短缺任务阶段无效。");
            if (!Enum.IsDefined(typeof(SealedLetterStage), data.sealedLetterStage))
                return Invalid("密信任务阶段无效。");
            if (!Enum.IsDefined(typeof(MarketTallyStage), data.marketTallyStage))
                return Invalid("市集账目任务阶段无效。");

            return ValidateRelationships(data.relationships);
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

        private static SaveOperationResult Invalid(string message)
        {
            return SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
        }
    }
}
