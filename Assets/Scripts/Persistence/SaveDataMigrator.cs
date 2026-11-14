using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Explicit v1-to-current save migration. The source object and file are never
    /// mutated. Each legacy version gains only the documented empty blocks; historical
    /// research, industry, growth or chronicle months are never replayed.
    /// </summary>
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

            if (legacy.saveFormatVersion < SaveDataValidator.CurrentVersion &&
                !string.IsNullOrEmpty(rawJson))
            {
                // Version-specific blocks take precedence over the generic development
                // rule. A legacy payload that wrongly carries a later block must report
                // the earliest field that its own version cannot own, matching
                // ValidatePresence's order (industry before development). v7 owns
                // development, so only v2-v6 may be rejected for carrying it.
                if (legacy.saveFormatVersion >= 2 && legacy.saveFormatVersion <= 5 &&
                    SaveDataValidator.HasRootProperty(rawJson, "industry"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含产业状态（industry）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion == 6 &&
                    (!SaveDataValidator.HasRootProperty(rawJson, "industry") ||
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "industry")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v6 存档缺少产业状态（industry）；迁移不会用初始状态静默替换。");
                    return false;
                }
                if (legacy.saveFormatVersion >= 2 && legacy.saveFormatVersion <= 6 &&
                    SaveDataValidator.HasNonNullRootProperty(rawJson, "development"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含人物成长状态（development）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 8 &&
                    SaveDataValidator.HasRootProperty(rawJson, "research"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含家族研究状态（research）；迁移不会静默替换该数据。");
                    return false;
                }
                // v9 introduced doctrine and the active event route. v10 already
                // owns both, so only v2-v8 are rejected for carrying them.
                if (legacy.saveFormatVersion < 9 &&
                    SaveDataValidator.HasRootProperty(rawJson, "familyDoctrine"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 v9 家训与抱负状态（familyDoctrine）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 9 &&
                    SaveDataValidator.HasRootProperty(rawJson, "activeEventScenarioId"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 v9 活动事件路线（activeEventScenarioId）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 10 &&
                    SaveDataValidator.HasRootProperty(rawJson, "dailyActions"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 v10 每日行动状态（dailyActions）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 11 &&
                    SaveDataValidator.HasRootProperty(rawJson, "npcAutonomy"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 v11 NPC 自主状态（npcAutonomy）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 14 &&
                    SaveDataValidator.HasRootProperty(rawJson, "commission"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 v14 护送委托状态（commission）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 16 &&
                    SaveDataValidator.HasRootProperty(rawJson, "campaign"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 M08 家族纪元状态（campaign）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.saveFormatVersion < 17 &&
                    SaveDataValidator.HasRootProperty(rawJson, "market"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含 M08B 市场状态（market）；迁移不会静默替换该数据。");
                    return false;
                }
            }

            if (legacy.saveFormatVersion == 10 ||
                legacy.saveFormatVersion == 11 ||
                legacy.saveFormatVersion == 12 ||
                legacy.saveFormatVersion == 13 ||
                legacy.saveFormatVersion == 14 ||
                legacy.saveFormatVersion == 15 ||
                legacy.saveFormatVersion == 16 ||
                legacy.saveFormatVersion == SaveDataValidator.CurrentVersion)
            {
                int version = legacy.saveFormatVersion;
                bool currentVersion = version == SaveDataValidator.CurrentVersion;
                string label = "v" + version;
                if (legacy.industry == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "industry")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少产业状态（industry）；不会用初始状态静默替换。");
                    return false;
                }
                if (legacy.development == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "development")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少人物成长状态（development）；不会用空记录静默替换。");
                    return false;
                }
                if (legacy.research == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "research")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少家族研究状态（research）；不会用空记录静默替换。");
                    return false;
                }
                if (legacy.familyDoctrine == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "familyDoctrine")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少家训与抱负状态（familyDoctrine）；不会用默认家训静默替换。");
                    return false;
                }
                if (legacy.activeEventScenarioId == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasRootProperty(rawJson, "activeEventScenarioId")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少活动事件路线（activeEventScenarioId）；不会用空路线静默替换。");
                    return false;
                }
                if (legacy.dailyActions == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "dailyActions")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少每日行动状态（dailyActions）；不会用空记录静默替换。");
                    return false;
                }
                if (version >= 11 &&
                    (legacy.npcAutonomy == null ||
                     (!string.IsNullOrEmpty(rawJson) &&
                      !SaveDataValidator.HasNonNullRootProperty(rawJson, "npcAutonomy"))))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少 NPC 自主状态（npcAutonomy）；不会用初始需求静默替换。");
                    return false;
                }
                if (version >= 15)
                {
                    // v15 owns the three M07 blocks. Clone() would otherwise replace a
                    // missing familyStanding with the initial fame/concealment, inventing
                    // state that the raw-JSON presence guard rejects for files.
                    if (legacy.familyStanding == null ||
                        (!string.IsNullOrEmpty(rawJson) &&
                         !SaveDataValidator.HasNonNullRootProperty(rawJson, "familyStanding")))
                    {
                        result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                            label + " 存档缺少 M07 家族名望状态（familyStanding）；不会用初始状态静默替换。");
                        return false;
                    }
                    if (legacy.injury == null ||
                        (!string.IsNullOrEmpty(rawJson) &&
                         !SaveDataValidator.HasRootProperty(rawJson, "injury")))
                    {
                        result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                            label + " 存档缺少 M07 伤员状态（injury）；不会用空记录静默替换。");
                        return false;
                    }
                    if (legacy.missionHistory == null ||
                        (!string.IsNullOrEmpty(rawJson) &&
                         !SaveDataValidator.HasRootProperty(rawJson, "missionHistory")))
                    {
                        result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                            label + " 存档缺少 M07 任务终局历史（missionHistory）；不会用空列表静默替换。");
                        return false;
                    }
                }
                if (version >= 11 && legacy.rulesVersion != ExpectedRulesVersion(version))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.UnsupportedVersion,
                        label + " 存档规则版本不受支持：" + legacy.rulesVersion +
                        "（v" + version + " 应为 " + ExpectedRulesVersion(version) + "）。");
                    return false;
                }
                if (version == 11 && ContainsMemoryPayload(legacy.npcAutonomy))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v11 存档包含 v12 行动记忆（npcAutonomy.persons[].memories）；迁移不会静默替换该数据。");
                    return false;
                }

                current = Clone(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                if (version == 10)
                {
                    // v10 predates M03. Migration initializes only the approved pilot
                    // rows from the fixed roster and preserves dailyActions unchanged.
                    current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(
                        current.succession);
                }
                else if (version == 11)
                {
                    // v11 owns needs but predates memories. Only empty lists are added;
                    // no past action is inferred from logs or history.
                    current.npcAutonomy = NpcAutonomyStateSaveData.CreateMigratedV11(
                        legacy.npcAutonomy);
                }
                if (version < 14)
                {
                    // v13 predates the M05 commission. Migration adds only the pristine
                    // not-accepted row; no offer, investment or outcome is invented.
                    current.commission = ShadowEscortCommissionSaveData.CreateInitial();
                } else if (legacy.commission == null ||
                           (!string.IsNullOrEmpty(rawJson) &&
                            !SaveDataValidator.HasNonNullRootProperty(rawJson, "commission")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        label + " 存档缺少 M05 护送委托状态（commission）；不会用初始状态静默替换。");
                    return false;
                }
                if (version < 13)
                {
                    // v13 already owns concrete genders and pregnancy rows. Only v10-v12
                    // need the legacy normalization; running it on v13 would wipe valid
                    // pregnancy data and reset authored genders.
                    PrepareMigratedSuccession(current);
                }
                if (version < 15)
                {
                    result = ApplyM07Migration(current, version);
                    if (!result.Succeeded)
                    {
                        current = null;
                        return false;
                    }
                }
                if (version < 16)
                {
                    // M08 adds only the campaign origin and empty acknowledgements.
                    // Origin is inferred from the saved scenario identity when provable;
                    // no elapsed time, review or historical event is manufactured.
                    current.campaign = CampaignProgressSaveData.CreateMigrated(current, true);
                }
                result = FinishMigration(current, version);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 9)
            {
                // v9 owns the SC-024 doctrine and active route. Migration adds only
                // the empty M02 daily timeline at the saved roster/location and never
                // replays a day or a month.
                current = CloneLegacy(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                current.dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    legacy.succession, legacy.locationId);
                current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(legacy.succession);
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 8)
            {
                if (legacy.industry == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "industry")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v8 存档缺少产业状态（industry）；不会用初始状态静默替换。");
                    return false;
                }
                if (legacy.development == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "development")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v8 存档缺少人物成长状态（development）；不会用空记录静默替换。");
                    return false;
                }
                if (legacy.research == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "research")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v8 存档缺少家族研究状态（research）；不会用空记录静默替换。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV8Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = CloneLegacy(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                // v8 predates SC-024. Migration adds only the approved default creed,
                // no ambitions and no active SC-024 route; no month is replayed.
                current.familyDoctrine = FamilyDoctrineSaveData.CreateDefault();
                current.activeEventScenarioId = string.Empty;
                current.dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    current.succession, current.locationId);
                current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(current.succession);
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 7)
            {
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "research"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v7 存档包含家族研究状态（research）；迁移不会静默替换该数据。");
                    return false;
                }
                if (legacy.industry == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "industry")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v7 存档缺少产业状态（industry）；不会用初始状态静默替换。");
                    return false;
                }
                if (legacy.development == null ||
                    (!string.IsNullOrEmpty(rawJson) &&
                     !SaveDataValidator.HasNonNullRootProperty(rawJson, "development")))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v7 存档缺少人物成长状态（development）；不会用空记录静默替换。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV7Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = CloneLegacy(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                // Migration adds empty research at the legacy date. It never replays a
                // historical research month or writes a chronicle record.
                current.research = FamilyResearchSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day));
                current.familyDoctrine = FamilyDoctrineSaveData.CreateDefault();
                current.activeEventScenarioId = string.Empty;
                current.dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    current.succession, current.locationId);
                current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(current.succession);
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 6)
            {
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "development"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v6 存档包含人物成长状态（development）；迁移不会静默替换该数据。");
                    return false;
                }
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "research"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v6 存档包含家族研究状态（research）；迁移不会静默替换该数据。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV6Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = CloneLegacy(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                // Migration adds five zero abilities and empty education history for each
                // existing person. It never replays an education year or writes chronicle.
                current.development = CharacterDevelopmentSaveData.CreateEmpty(legacy.succession);
                current.research = FamilyResearchSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day));
                current.familyDoctrine = FamilyDoctrineSaveData.CreateDefault();
                current.activeEventScenarioId = string.Empty;
                current.dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    current.succession, current.locationId);
                current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(current.succession);
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 5)
            {
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "industry"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v5 存档包含产业状态（industry）；迁移不会静默替换该数据。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV5Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = CloneLegacy(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                // Migration adds the documented opening state; it never replays
                // historical production or appends a chronicle record.
                current.industry = IndustryStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day));
                current.development = CharacterDevelopmentSaveData.CreateEmpty(current.succession);
                current.research = FamilyResearchSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day));
                current.familyDoctrine = FamilyDoctrineSaveData.CreateDefault();
                current.activeEventScenarioId = string.Empty;
                current.dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    current.succession, current.locationId);
                current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(current.succession);
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 4)
            {
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "industry"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v4 存档包含产业状态（industry）；迁移不会静默替换该数据。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV4Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = CloneLegacy(legacy);
                current.saveFormatVersion = SaveDataValidator.CurrentVersion;
                current.rulesVersion = SaveData.CurrentRulesVersion;
                // Person-pair family relationships did not exist in v4. Migration must
                // not infer values from kinship, NPC relations, or current head state.
                current.familyRelationships = new FamilyRelationshipSaveEntry[0];
                current.industry = IndustryStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day));
                current.development = CharacterDevelopmentSaveData.CreateEmpty(current.succession);
                current.research = FamilyResearchSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day));
                current.familyDoctrine = FamilyDoctrineSaveData.CreateDefault();
                current.activeEventScenarioId = string.Empty;
                current.dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    current.succession, current.locationId);
                current.npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(current.succession);
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 3)
            {
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "industry"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v3 存档包含产业状态（industry）；迁移不会静默替换该数据。");
                    return false;
                }

                bool containsChronicleBlock = string.IsNullOrEmpty(rawJson)
                    ? legacy.chronicle != null && legacy.chronicle.Length > 0
                    : SaveDataValidator.HasRootProperty(rawJson, "chronicle");
                if (containsChronicleBlock)
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v3 存档包含编年史（chronicle）；迁移不会静默替换该数据。");
                    return false;
                }

                result = SaveDataValidator.ValidateLegacyV3Fields(legacy);
                if (!result.Succeeded)
                    return false;

                current = new SaveData
                {
                    saveFormatVersion = SaveDataValidator.CurrentVersion,
                    gameVersion = string.IsNullOrWhiteSpace(legacy.gameVersion)
                        ? SaveData.CurrentGameVersion
                        : legacy.gameVersion,
                    rulesVersion = SaveData.CurrentRulesVersion,
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
                    familyRelationships = new FamilyRelationshipSaveEntry[0],
                    randomState = CloneRandomState(legacy.randomState),
                    eventRuntime = CloneEventRuntime(legacy.eventRuntime),
                    succession = legacy.succession != null
                        ? legacy.succession.Clone()
                        : SuccessionSaveData.CreateDefault(),
                    chronicle = MigrateEventHistory(legacy.eventRuntime),
                    industry = IndustryStateSaveData.CreateInitial(
                        new GameDate(legacy.year, legacy.month, legacy.day)),
                    development = CharacterDevelopmentSaveData.CreateEmpty(
                        legacy.succession != null ? legacy.succession : SuccessionSaveData.CreateDefault()),
                    research = FamilyResearchSaveData.CreateInitial(
                        new GameDate(legacy.year, legacy.month, legacy.day)),
                    familyDoctrine = FamilyDoctrineSaveData.CreateDefault(),
                    activeEventScenarioId = string.Empty,
                    dailyActions = DailyActionStateSaveData.CreateInitial(
                        new GameDate(legacy.year, legacy.month, legacy.day),
                        legacy.succession != null ? legacy.succession : SuccessionSaveData.CreateDefault(),
                        legacy.locationId),
                    npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(
                        legacy.succession != null ? legacy.succession : SuccessionSaveData.CreateDefault())
                };

                // v3 owns a real succession block, so a known authored family identity can
                // still prove the campaign origin. No review state is claimed.
                PrepareMigratedSuccession(current, true);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion == 2)
            {
                if (!string.IsNullOrEmpty(rawJson) &&
                    SaveDataValidator.HasRootProperty(rawJson, "industry"))
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v2 存档包含产业状态（industry）；迁移不会静默替换该数据。");
                    return false;
                }

                bool containsSuccessionBlock = string.IsNullOrEmpty(rawJson)
                    ? legacy.succession != null
                    : SaveDataValidator.HasRootProperty(rawJson, "succession");
                if (containsSuccessionBlock)
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v2 存档包含继承状态（succession）；迁移不会静默替换该数据。");
                    return false;
                }

                // A programmatic v2 object can carry a chronicle array even though the
                // file format predates it. Reject it instead of silently replacing the
                // caller's data with the empty migration default. The object path must
                // test null because SaveData's constructor creates an empty array; the
                // raw-JSON path uses the root-property check instead.
                bool containsChronicleBlock = string.IsNullOrEmpty(rawJson)
                    ? legacy.chronicle != null
                    : SaveDataValidator.HasRootProperty(rawJson, "chronicle");
                if (containsChronicleBlock)
                {
                    result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "v2 存档包含编年史（chronicle）；迁移不会静默替换该数据。");
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
                    rulesVersion = SaveData.CurrentRulesVersion,
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
                    familyRelationships = new FamilyRelationshipSaveEntry[0],
                    randomState = CloneRandomState(legacy.randomState),
                    eventRuntime = CloneEventRuntime(legacy.eventRuntime),
                    succession = SuccessionSaveData.CreateDefault(),
                    chronicle = new ChronicleSaveEntry[0],
                    industry = IndustryStateSaveData.CreateInitial(
                        new GameDate(legacy.year, legacy.month, legacy.day)),
                    development = CharacterDevelopmentSaveData.CreateEmpty(
                        SuccessionSaveData.CreateDefault()),
                    research = FamilyResearchSaveData.CreateInitial(
                        new GameDate(legacy.year, legacy.month, legacy.day)),
                    familyDoctrine = FamilyDoctrineSaveData.CreateDefault(),
                    activeEventScenarioId = string.Empty,
                    dailyActions = DailyActionStateSaveData.CreateInitial(
                        new GameDate(legacy.year, legacy.month, legacy.day),
                        SuccessionSaveData.CreateDefault(), legacy.locationId),
                    npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(
                        SuccessionSaveData.CreateDefault())
                };

                PrepareMigratedSuccession(current);
                result = FinishMigration(current, legacy.saveFormatVersion);
                if (!result.Succeeded)
                    current = null;
                return result.Succeeded;
            }

            if (legacy.saveFormatVersion != 1)
            {
                result = SaveOperationResult.Failure(
                    SaveErrorCode.UnsupportedVersion,
                    $"不支持的存档版本：{legacy.saveFormatVersion}（仅支持 v1–v16 迁移到 v{SaveDataValidator.CurrentVersion}）。");
                return false;
            }

            if (!string.IsNullOrEmpty(rawJson) &&
                SaveDataValidator.HasRootProperty(rawJson, "industry"))
            {
                result = SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                    "v1 存档包含产业状态（industry）；迁移不会静默替换该数据。");
                return false;
            }

            result = SaveDataValidator.ValidateLegacyV1Fields(legacy);
            if (!result.Succeeded)
                return false;

            current = new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                rulesVersion = SaveData.CurrentRulesVersion,
                year = legacy.year,
                month = legacy.month,
                day = legacy.day,
                locationId = legacy.locationId,
                supplyShortageStage = legacy.supplyShortageStage,
                sealedLetterStage = legacy.sealedLetterStage,
                marketTallyStage = legacy.marketTallyStage,
                relationships = CloneRelationships(legacy.relationships),
                familyRelationships = new FamilyRelationshipSaveEntry[0],
                randomState = RandomStateManager.Create(V1DefaultSeed).CaptureState(),
                eventRuntime = new EventRuntimeSaveData(),
                succession = SuccessionSaveData.CreateDefault(),
                chronicle = new ChronicleSaveEntry[0],
                industry = IndustryStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day)),
                development = CharacterDevelopmentSaveData.CreateEmpty(SuccessionSaveData.CreateDefault()),
                research = FamilyResearchSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day)),
                familyDoctrine = FamilyDoctrineSaveData.CreateDefault(),
                activeEventScenarioId = string.Empty,
                dailyActions = DailyActionStateSaveData.CreateInitial(
                    new GameDate(legacy.year, legacy.month, legacy.day),
                    SuccessionSaveData.CreateDefault(), legacy.locationId),
                npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(
                    SuccessionSaveData.CreateDefault())
            };

            PrepareMigratedSuccession(current);
            result = FinishMigration(current, legacy.saveFormatVersion);
            if (!result.Succeeded)
                current = null;
            return result.Succeeded;
        }

        private static int ExpectedRulesVersion(int version) =>
            version >= 15 ? SaveData.CurrentRulesVersion : 7;

        /// <summary>
        /// Completes every pre-v17 migration path. The market block is derived once from
        /// the already-migrated clone (its industry stock and living roster), then the final
        /// shape is validated. A current-version payload keeps its own market block.
        /// </summary>
        private static SaveOperationResult FinishMigration(SaveData current, int sourceVersion)
        {
            if (sourceVersion < SaveDataValidator.CurrentVersion)
                current.market = MarketSaveData.CreateMigrated(current);
            return SaveDataValidator.Validate(current);
        }

        /// <summary>
        /// Adds the M07 blocks to a pre-v15 payload without replaying a settlement.
        /// Standing starts at the approved initial values; the M05 commission gets a
        /// stable legacy run ID and, when already settled, one history row linked to its
        /// unique chronicle line. Legacy M06 rows are derived only from explicit M06
        /// chronicle rows and never infer people from free text.
        /// </summary>
        private static SaveOperationResult ApplyM07Migration(SaveData current, int version)
        {
            current.familyStanding = FamilyStandingSaveData.CreateInitial();
            current.injury = MissionInjurySaveData.CreateEmpty();
            current.missionHistory = new MissionHistorySaveEntry[0];
            if (current.commission == null)
                current.commission = ShadowEscortCommissionSaveData.CreateInitial();

            ShadowEscortStage stage = (ShadowEscortStage)current.commission.stage;
            bool settled = stage == ShadowEscortStage.Succeeded ||
                           stage == ShadowEscortStage.PartialSuccess ||
                           stage == ShadowEscortStage.Failed;
            bool executing = stage == ShadowEscortStage.InProgress || settled;
            if (executing)
            {
                // The mode of a pre-M07 run is unknown; the plan forbids guessing it.
                current.commission.runId = "legacy.m05.current";
                current.commission.mode = (int)MissionOperationMode.LegacyUnknown;
            }
            else
            {
                current.commission.runId = string.Empty;
                current.commission.mode = (int)MissionOperationMode.Delegated;
            }

            var entries = new List<ChronicleSaveEntry>();
            if (current.chronicle != null)
                entries.AddRange(current.chronicle);

            var records = new List<MissionRecord>();
            if (settled)
            {
                var matches = new List<ChronicleSaveEntry>();
                foreach (ChronicleSaveEntry entry in entries)
                    if (entry != null &&
                        entry.category == (int)ChronicleCategory.Mission &&
                        string.Equals(entry.subjectId,
                            FamilyChronicleEntry.ShadowEscortCommissionId,
                            System.StringComparison.Ordinal) &&
                        string.Equals(entry.sourceId, "settle",
                            System.StringComparison.Ordinal))
                        matches.Add(entry);
                if (matches.Count > 1)
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版存档包含多条护送委托结算编年史，无法唯一归属 M07 终局记录。");

                ChronicleSaveEntry link = matches.Count == 1 ? matches[0] : null;
                if (link == null)
                {
                    int sequence = entries.Count + 1;
                    link = new ChronicleSaveEntry
                    {
                        entryId = FamilyChronicleEntry.BuildEntryId(sequence),
                        sequence = sequence,
                        year = current.commission.startYear,
                        month = current.commission.startMonth,
                        category = (int)ChronicleCategory.Mission,
                        subjectId = FamilyChronicleEntry.ShadowEscortCommissionId,
                        sourceId = "settle",
                        title = "护送委托（旧版导入）",
                        summary = "旧版存档导入的护送委托终局记录。",
                        fromStage = (int)ShadowEscortStage.InProgress,
                        toStage = current.commission.stage
                    };
                    entries.Add(link);
                }

                if (!PersonId.TryParse(current.commission.assigneePersonId,
                        out PersonId assignee) || assignee.IsEmpty)
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版已结算委托缺少有效执行人，无法回填 M07 终局记录。");
                if (!GameDate.TryParse(current.commission.startYear,
                        current.commission.startMonth, current.commission.startDay,
                        out GameDate date))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版已结算委托的开始日期无效。");
                if (!ShadowEscortOutcomeRules.TryMap(
                        (CommissionOutcome)current.commission.outcome,
                        out SharedMissionOutcome mapped, out string mapError))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData, mapError);

                if (!MissionRecord.TryCreate("legacy.m05.current",
                        FamilyChronicleEntry.ShadowEscortCommissionId,
                        MissionKind.ShadowEscort, MissionOperationMode.LegacyUnknown, date,
                        mapped, assignee, new[] { assignee }, new PersonId[0],
                        MissionPreparation.None, 0, 0, 0, 0, current.commission.roll,
                        current.commission.score, FamilyStandingState.InitialFame,
                        FamilyStandingState.InitialFame,
                        FamilyStandingState.InitialConcealment,
                        FamilyStandingState.InitialConcealment,
                        new MissionStandingChange[0], current.commission.rewardFunds,
                        current.commission.rewardSupplies, 0,
                        new[]
                        {
                            new MissionParticipantState(assignee, true, false, true, false)
                        }, link.entryId, out MissionRecord commissionRecord,
                        out string recordError))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData, recordError);
                records.Add(commissionRecord);
            }

            foreach (ChronicleSaveEntry entry in entries)
            {
                if (entry == null ||
                    entry.category != (int)ChronicleCategory.Mission ||
                    !string.Equals(entry.subjectId, FamilyChronicleEntry.TacticalMissionId,
                        System.StringComparison.Ordinal))
                    continue;
                if (!GameDate.TryParse(entry.year, entry.month, 1, out GameDate date))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "旧版潜入编年史日期无效。");
                SharedMissionOutcome mapped;
                switch ((TacticalMissionStage)entry.toStage)
                {
                    case TacticalMissionStage.Succeeded:
                        mapped = SharedMissionOutcome.FullSuccess;
                        break;
                    case TacticalMissionStage.Withdrawn:
                        mapped = SharedMissionOutcome.SafeFailure;
                        break;
                    case TacticalMissionStage.Failed:
                        mapped = SharedMissionOutcome.SevereFailure;
                        break;
                    default:
                        return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                            "旧版潜入编年史没有可映射的终局阶段。");
                }
                string runId = "legacy.m06." + entry.entryId;
                if (!MissionRecord.TryCreate(runId, FamilyChronicleEntry.TacticalMissionId,
                        MissionKind.TacticalInfiltration, MissionOperationMode.LegacyUnknown,
                        date, mapped, default, new PersonId[0], new PersonId[0],
                        MissionPreparation.None, 0, 0, 0, 0, 0, 0,
                        FamilyStandingState.InitialFame, FamilyStandingState.InitialFame,
                        FamilyStandingState.InitialConcealment,
                        FamilyStandingState.InitialConcealment,
                        new MissionStandingChange[0], 0, 0, 0,
                        new MissionParticipantState[0], entry.entryId,
                        out MissionRecord tacticalRecord, out string tacticalError))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData, tacticalError);
                records.Add(tacticalRecord);
            }

            records.Sort((a, b) =>
            {
                int byDate = a.Date.CompareTo(b.Date);
                return byDate != 0
                    ? byDate
                    : string.CompareOrdinal(a.RunId, b.RunId);
            });
            var history = new MissionHistorySaveEntry[records.Count];
            for (int i = 0; i < records.Count; i++)
                history[i] = MissionHistorySaveEntry.Capture(records[i]);
            current.missionHistory = history;
            current.chronicle = entries.ToArray();
            return SaveOperationResult.Success();
        }

        private static void PrepareMigratedSuccession(SaveData current, bool originProvable = false)
        {
            // Every pre-v13 source predates both fields. Start from Unspecified, resolve
            // only explicit authored catalog IDs, and write a concrete empty pregnancy
            // list before current-version validation. Unknown identities stay Unspecified.
            ResolveLegacyGenders(current != null ? current.succession : null);
            if (current != null && current.succession != null)
                current.succession.pregnancies = new FamilyPregnancySaveEntry[0];
            if (current != null)
            {
                // M08 adds only the campaign origin when the saved scenario identity proves
                // it. A manufactured default roster (v1/v2) never proves one.
                current.campaign = CampaignProgressSaveData.CreateMigrated(current, originProvable);
            }
        }

        private static void ResolveLegacyGenders(SuccessionSaveData succession)
        {
            if (succession == null || succession.persons == null)
                return;
            foreach (SuccessionPersonSaveEntry person in succession.persons)
            {
                if (person == null)
                    continue;
                PersonGender gender = PersonGender.Unspecified;
                if (PersonId.TryParse(person.personId, out PersonId personId))
                    PersonGenderCatalog.TryGet(personId, out gender);
                person.gender = (int)gender;
            }
        }

        private static bool ContainsMemoryPayload(NpcAutonomyStateSaveData data)
        {
            if (data == null || data.persons == null)
                return false;
            foreach (NpcAutonomyPersonSaveEntry person in data.persons)
                if (person != null && person.memories != null)
                    return true;
            return false;
        }

        /// <summary>
        /// Clone used by the pre-v10 branches. Those versions predate the M05 commission,
        /// so the row is normalized to pristine even when the source object still carries
        /// the current class's field (for example a downgraded current capture).
        /// </summary>
        private static SaveData CloneLegacy(SaveData source)
        {
            SaveData clone = Clone(source);
            clone.commission = ShadowEscortCommissionSaveData.CreateInitial();
            clone.familyStanding = FamilyStandingSaveData.CreateInitial();
            clone.injury = MissionInjurySaveData.CreateEmpty();
            clone.missionHistory = new MissionHistorySaveEntry[0];
            clone.market = null;
            return clone;
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
                familyRelationships = CloneFamilyRelationships(source.familyRelationships),
                randomState = CloneRandomState(source.randomState),
                eventRuntime = CloneEventRuntime(source.eventRuntime),
                succession = source.succession != null
                    ? source.succession.Clone()
                    : SuccessionSaveData.CreateDefault(),
                chronicle = ChronicleSaveCodec.Clone(source.chronicle),
                // Preserve null so the v6 validation path can reject a malformed
                // payload instead of silently repairing it with opening values.
                industry = source.industry != null ? source.industry.Clone() : null,
                development = source.development != null ? source.development.Clone() : null,
                research = source.research != null ? source.research.Clone() : null,
                familyDoctrine = source.familyDoctrine != null ? source.familyDoctrine.Clone() : null,
                activeEventScenarioId = source.activeEventScenarioId,
                dailyActions = source.dailyActions != null ? source.dailyActions.Clone() : null,
                npcAutonomy = source.npcAutonomy != null ? source.npcAutonomy.Clone() : null,
                // Legacy payloads predate M05. Their documented migration default is the
                // pristine not-accepted row; a current-version payload must carry its own.
                commission = source.commission != null
                    ? source.commission.Clone()
                    : ShadowEscortCommissionSaveData.CreateInitial(),
                familyStanding = source.familyStanding != null
                    ? source.familyStanding.Clone()
                    : FamilyStandingSaveData.CreateInitial(),
                // Preserve a malformed null so validation rejects it, matching the
                // neighbouring industry and missionHistory blocks instead of silently
                // repairing it with an empty injury list.
                injury = source.injury != null ? source.injury.Clone() : null,
                missionHistory = CloneMissionHistory(source.missionHistory),
                // Current-version payloads keep their own campaign block; legacy branches
                // replace it through their documented migration default.
                campaign = source.campaign != null ? source.campaign.Clone() : null,
                market = source.market != null ? source.market.Clone() : null
            };
            return clone;
        }

        private static MissionHistorySaveEntry[] CloneMissionHistory(
            MissionHistorySaveEntry[] source)
        {
            if (source == null)
                return null;
            var clone = new MissionHistorySaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
                clone[i] = MissionHistorySaveEntry.Clone(source[i]);
            return clone;
        }

        /// <summary>
        /// Copies v3 event history into the v4 chronicle in its original order. Only the
        /// saved year/month/summary and stable IDs are reused; the missing day is never
        /// invented, and no mission, relationship or succession history is fabricated.
        /// </summary>
        private static ChronicleSaveEntry[] MigrateEventHistory(EventRuntimeSaveData runtime)
        {
            if (runtime == null || runtime.history == null || runtime.history.Length == 0)
                return new ChronicleSaveEntry[0];

            var entries = new ChronicleSaveEntry[runtime.history.Length];
            for (int i = 0; i < runtime.history.Length; i++)
            {
                EventHistorySaveEntry history = runtime.history[i];
                int sequence = i + 1;
                entries[i] = new ChronicleSaveEntry
                {
                    entryId = FamilyChronicleEntry.BuildEntryId(sequence),
                    sequence = sequence,
                    year = history.year,
                    month = history.month,
                    category = (int)ChronicleCategory.Event,
                    subjectId = history.eventId,
                    sourceId = history.optionId,
                    title = "既往事件",
                    summary = history.summary ?? string.Empty
                };
            }
            return entries;
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

        private static FamilyRelationshipSaveEntry[] CloneFamilyRelationships(
            FamilyRelationshipSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new FamilyRelationshipSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                FamilyRelationshipSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new FamilyRelationshipSaveEntry
                {
                    headPersonId = entry.headPersonId,
                    memberPersonId = entry.memberPersonId,
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
