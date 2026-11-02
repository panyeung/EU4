using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>Explicit v1/v2-to-v3 save migration. The source object and file are never mutated.</summary>
    public static class SaveDataMigrator
    {
        // Fixed deterministic seed for legacy v1 sessions that did not store RNG state.
        public const ulong V1DefaultSeed = 0x53434C414E563031UL;

        public static bool TryMigrate(SaveData legacy, out SaveData current, out SaveOperationResult result) =>
            TryMigrate(legacy, null, out current, out result);

        /// <summary>
        /// Migrates legacy data while using the raw JSON root-property check for v2.
        /// JsonUtility materializes nested classes even when their property is absent,
        /// so the raw payload is the only reliable way to tell whether v2 carried a
        /// succession block that must be rejected instead of silently replaced.
        /// </summary>
        public static bool TryMigrate(SaveData legacy, string rawJson, out SaveData current,
            out SaveOperationResult result)
        {
            current = null;
            if (legacy == null)
            {
                result = SaveOperationResult.Failure(SaveErrorCode.InvalidData, "存档数据为空。");
                return false;
            }

            if (legacy.saveFormatVersion == SaveDataValidator.CurrentVersion)
            {
                current = Clone(legacy);
                result = SaveDataValidator.Validate(current);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 2)
            {
                bool containsSuccessionBlock = string.IsNullOrEmpty(rawJson)
                    ? legacy.succession != null
                    : SaveDataValidator.HasRootProperty(rawJson, "succession");
                if (containsSuccessionBlock)
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v2 存档包含继承状态（succession）；迁移不会静默替换该数据。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV2Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = new SaveData
                {
                    saveFormatVersion = SaveDataValidator.CurrentVersion,
                    gameVersion = string.IsNullOrWhiteSpace(legacy.gameVersion)
                        ? SaveData.CurrentGameVersion
                        : legacy.gameVersion,
                    rulesVersion = legacy.rulesVersion > 0 ? legacy.rulesVersion : SaveData.CurrentRulesVersion,
                    eventContentVersion = legacy.eventContentVersion > 0
                        ? legacy.eventContentVersion
                        : SaveData.CurrentEventContentVersion,
                    year = legacy.year,
                    month = legacy.month,
                    day = legacy.day,
                    locationId = legacy.locationId,
                    supplyShortageStage = legacy.supplyShortageStage,
                    sealedLetterStage = legacy.sealedLetterStage,
                    marketTallyStage = legacy.marketTallyStage,
                    relationships = CloneRelationships(legacy.relationships),
                    randomState = CloneRandomState(legacy.randomState),
                    eventRuntime = CloneEventRuntime(legacy.eventRuntime),
                    succession = SuccessionSaveData.CreateDefault()
                };

                result = SaveDataValidator.Validate(current);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion != 1)
            {
                result = SaveOperationResult.Failure(
                    SaveErrorCode.UnsupportedVersion,
                    $"不支持的存档版本：{legacy.saveFormatVersion}（仅支持 v1/v2 迁移到 v{SaveDataValidator.CurrentVersion}）。");
                return false;
            }

            result = SaveDataValidator.ValidateLegacyV1Fields(legacy);
            if (!result.Succeeded)
                return false;

            current = new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = legacy.year,
                month = legacy.month,
                day = legacy.day,
                locationId = legacy.locationId,
                supplyShortageStage = legacy.supplyShortageStage,
                sealedLetterStage = legacy.sealedLetterStage,
                marketTallyStage = legacy.marketTallyStage,
                relationships = CloneRelationships(legacy.relationships),
                randomState = RandomStateManager.Create(V1DefaultSeed).CaptureState(),
                eventRuntime = new EventRuntimeSaveData(),
                succession = SuccessionSaveData.CreateDefault()
            };

            result = SaveDataValidator.Validate(current);
            if (!result.Succeeded)
                current = null;
            return result.Succeeded;
        }

        private static SaveData Clone(SaveData source)
        {
            var clone = new SaveData
            {
                saveFormatVersion = source.saveFormatVersion,
                gameVersion = source.gameVersion,
                rulesVersion = source.rulesVersion,
                eventContentVersion = source.eventContentVersion,
                year = source.year,
                month = source.month,
                day = source.day,
                locationId = source.locationId,
                supplyShortageStage = source.supplyShortageStage,
                sealedLetterStage = source.sealedLetterStage,
                marketTallyStage = source.marketTallyStage,
                relationships = CloneRelationships(source.relationships),
                randomState = CloneRandomState(source.randomState),
                eventRuntime = CloneEventRuntime(source.eventRuntime),
                succession = source.succession != null
                    ? source.succession.Clone()
                    : SuccessionSaveData.CreateDefault()
            };
            return clone;
        }

        internal static EventRuntimeSaveData CloneEventRuntime(EventRuntimeSaveData source)
        {
            if (source == null) return null;
            var clone = new EventRuntimeSaveData
            {
                phase = source.phase,
                pendingEventIds = CloneStrings(source.pendingEventIds),
                onceEventIds = CloneStrings(source.onceEventIds),
                committedResultIds = CloneStrings(source.committedResultIds)
            };
            if (source.cooldowns != null)
            {
                clone.cooldowns = new EventCooldownSaveEntry[source.cooldowns.Length];
                for (int i = 0; i < source.cooldowns.Length; i++)
                {
                    EventCooldownSaveEntry entry = source.cooldowns[i];
                    clone.cooldowns[i] = entry == null ? null : new EventCooldownSaveEntry
                    {
                        eventId = entry.eventId,
                        availableAtMonth = entry.availableAtMonth
                    };
                }
            }
            if (source.history != null)
            {
                clone.history = new EventHistorySaveEntry[source.history.Length];
                for (int i = 0; i < source.history.Length; i++)
                {
                    EventHistorySaveEntry entry = source.history[i];
                    clone.history[i] = entry == null ? null : new EventHistorySaveEntry
                    {
                        eventId = entry.eventId,
                        optionId = entry.optionId,
                        resultId = entry.resultId,
                        year = entry.year,
                        month = entry.month,
                        summary = entry.summary
                    };
                }
            }
            return clone;
        }

        private static RelationshipSaveEntry[] CloneRelationships(RelationshipSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new RelationshipSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                RelationshipSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new RelationshipSaveEntry
                {
                    characterId = entry.characterId,
                    value = entry.value
                };
            }
            return clone;
        }

        private static RandomStateSaveData CloneRandomState(RandomStateSaveData source)
        {
            if (source == null) return null;
            var clone = new RandomStateSaveData { initialSeedHex = source.initialSeedHex };
            if (source.streams != null)
            {
                clone.streams = new RandomStreamSaveEntry[source.streams.Length];
                for (int i = 0; i < source.streams.Length; i++)
                {
                    RandomStreamSaveEntry entry = source.streams[i];
                    clone.streams[i] = entry == null ? null : new RandomStreamSaveEntry
                    {
                        streamId = entry.streamId,
                        algorithmVersion = entry.algorithmVersion,
                        state0Hex = entry.state0Hex,
                        state1Hex = entry.state1Hex
                    };
                }
            }
            return clone;
        }

        private static string[] CloneStrings(string[] source)
        {
            if (source == null) return null;
            var clone = new string[source.Length];
            source.CopyTo(clone, 0);
            return clone;
        }
    }
}
