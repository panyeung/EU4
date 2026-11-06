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
        public const int CurrentVersion = 9;

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

            SaveOperationResult familyRelationships =
                ValidateFamilyRelationships(data.familyRelationships, data.succession);
            if (!familyRelationships.Succeeded)
                return familyRelationships;

            if (data.industry == null)
                return Invalid("存档缺少产业状态（industry）。");
            SaveOperationResult industry = data.industry.ValidateForSave(
                new GameDate(data.year, data.month, data.day), data.succession);
            if (!industry.Succeeded)
                return industry;

            if (data.development == null)
                return Invalid("存档缺少人物成长状态（development）。");
            SaveOperationResult development = data.development.ValidateForSave(
                new GameDate(data.year, data.month, data.day), data.succession,
                FormerHeadIds(data.chronicle));
            if (!development.Succeeded)
                return development;

            GameDate date = new GameDate(data.year, data.month, data.day);
            if (data.research == null)
                return Invalid("存档缺少家族研究状态（research）。");
            SaveOperationResult research = data.research.ValidateForSave(date, data.succession);
            if (!research.Succeeded)
                return research;

            if (data.familyDoctrine == null)
                return Invalid("存档缺少家训与抱负状态（familyDoctrine）。");
            SaveOperationResult doctrine = data.familyDoctrine.ValidateForSave(data.succession,
                data.research.knownTechniqueIds);
            if (!doctrine.Succeeded)
                return doctrine;

            SaveOperationResult route = ValidateEventScenarioRoute(data.activeEventScenarioId);
            if (!route.Succeeded)
                return route;

            SaveOperationResult chronicle = ValidateChronicle(data.chronicle);
            if (!chronicle.Succeeded)
                return chronicle;

            if (!RandomStateManager.TryRestore(data.randomState, out _))
                return Invalid("存档随机流状态无效。");

            return ValidateEventRuntimeSnapshot(data.eventRuntime,
                new GameDate(data.year, data.month, data.day));
        }

        /// <summary>
        /// Rejects current-version JSON that omitted a required field instead of
        /// accepting the defaults that <see cref="SaveData"/>'s constructor would
        /// otherwise supply. Legacy v1/v2 payloads lack the v3 succession block and
        /// v1-v3 payloads lack the v4 chronicle; the migrator owns their documented
        /// defaults.
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

            if (probe.saveFormatVersion != 2 && probe.saveFormatVersion != 3 &&
                probe.saveFormatVersion != 4 && probe.saveFormatVersion != 5 &&
                probe.saveFormatVersion != 6 && probe.saveFormatVersion != 7 &&
                probe.saveFormatVersion != 8 && probe.saveFormatVersion != CurrentVersion)
                return SaveOperationResult.Failure(
                    SaveErrorCode.UnsupportedVersion,
                    $"不支持的存档版本：{probe.saveFormatVersion}（当前版本 {CurrentVersion}）。");

            if (probe.saveFormatVersion == 2 && HasRootProperty(json, "succession"))
                return Invalid("v2 存档包含继承状态（succession）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion == 2 && HasRootProperty(json, "chronicle"))
                return Invalid("v2 存档包含编年史（chronicle）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion == 2 && HasRootProperty(json, "familyRelationships"))
                return Invalid("v2 存档包含家庭关系（familyRelationships）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion == 3 && HasRootProperty(json, "chronicle"))
                return Invalid("v3 存档包含编年史（chronicle）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion == 3 && HasRootProperty(json, "familyRelationships"))
                return Invalid("v3 存档包含家庭关系（familyRelationships）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion <= 5 &&
                HasRootProperty(json, "industry"))
                return Invalid("旧版存档包含产业状态（industry）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasNonNullRootProperty(json, "development") && probe.saveFormatVersion < 7)
                return Invalid("旧版存档包含人物成长状态（development）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasRootProperty(json, "research") && probe.saveFormatVersion < 8)
                return Invalid("旧版存档包含家族研究状态（research）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasRootProperty(json, "familyDoctrine"))
                return Invalid("旧版存档包含 v9 家训与抱负状态（familyDoctrine）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasRootProperty(json, "activeEventScenarioId"))
                return Invalid("旧版存档包含 v9 活动事件路线（activeEventScenarioId）；迁移不会静默替换该数据。");

            if (string.IsNullOrWhiteSpace(probe.gameVersion) || probe.rulesVersion <= 0 ||
                probe.eventContentVersion <= 0)
                return Invalid("存档缺少必需的版本元数据。");
            if (probe.randomState == null || !HasRootProperty(json, "randomState"))
                return Invalid("存档缺少随机流状态（randomState）。");
            if (probe.eventRuntime == null || !HasRootProperty(json, "eventRuntime"))
                return Invalid("存档缺少事件运行状态（eventRuntime）。");
            if (probe.saveFormatVersion >= 3 &&
                (probe.succession == null || !HasRootProperty(json, "succession")))
                return Invalid("存档缺少继承状态（succession）。");
            // v4 introduced the chronicle and v5 keeps it. A legacy v4 file must
            // still carry that block; only the family-relationship set is optional
            // before migration.
            if (probe.saveFormatVersion >= 4 &&
                (probe.chronicle == null || !HasRootProperty(json, "chronicle")))
                return Invalid("存档缺少编年史（chronicle）。");
            if (probe.saveFormatVersion >= 5 &&
                (probe.familyRelationships == null || !HasRootProperty(json, "familyRelationships")))
                return Invalid("存档缺少家庭关系（familyRelationships）。");
            if (probe.saveFormatVersion >= 6 &&
                (probe.industry == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "industry"))))
                return Invalid("存档缺少产业状态（industry）。");
            if (probe.saveFormatVersion >= 7 &&
                (probe.development == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "development"))))
                return Invalid("存档缺少人物成长状态（development）。");
            if (probe.saveFormatVersion >= 8 &&
                (probe.research == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "research"))))
                return Invalid("存档缺少家族研究状态（research）。");
            if (probe.saveFormatVersion == CurrentVersion &&
                (probe.familyDoctrine == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "familyDoctrine"))))
                return Invalid("存档缺少家训与抱负状态（familyDoctrine）。");
            if (probe.saveFormatVersion == CurrentVersion &&
                (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "activeEventScenarioId")))
                return Invalid("存档缺少活动事件路线（activeEventScenarioId）。");

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

        /// <summary>
        /// Like <see cref="HasRootProperty"/> but also rejects an explicit JSON null.
        /// JsonUtility materializes a default nested object for a null class field,
        /// so the raw payload is the only reliable presence check for v6 industry.
        /// </summary>
        internal static bool HasNonNullRootProperty(string json, string propertyName)
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
                        if (lookahead >= json.Length || json[lookahead] != ':')
                            continue;
                        lookahead++;
                        while (lookahead < json.Length && char.IsWhiteSpace(json[lookahead]))
                            lookahead++;
                        if (lookahead + 4 <= json.Length &&
                            string.CompareOrdinal(json, lookahead, "null", 0, 4) == 0)
                        {
                            int after = lookahead + 4;
                            while (after < json.Length && char.IsWhiteSpace(json[after]))
                                after++;
                            if (after >= json.Length || json[after] == ',' || json[after] == '}' ||
                                json[after] == ']')
                                return false;
                        }
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

        /// <summary>
        /// v3 already owns the succession block but predates the chronicle. The
        /// migrator copies the saved event history into the v4 chronicle and must not
        /// invent any mission, relationship or succession history.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV3Fields(SaveData data)
        {
            SaveOperationResult legacy = ValidateLegacyV2Fields(data);
            if (!legacy.Succeeded)
                return legacy;
            if (data.succession == null)
                return Invalid("存档缺少继承状态（succession）。");
            return data.succession.ValidateForSave(new GameDate(data.year, data.month, data.day));
        }

        /// <summary>
        /// v4 already owns the chronicle but predates person-pair family relationships.
        /// The migrator adds an empty set; it must not invent any value.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV4Fields(SaveData data)
        {
            SaveOperationResult legacy = ValidateLegacyV3Fields(data);
            if (!legacy.Succeeded)
                return legacy;
            if (data.familyRelationships != null && data.familyRelationships.Length > 0)
                return Invalid("v4 存档包含家庭关系（familyRelationships）；迁移不会静默替换该数据。");
            return ValidateChronicle(data.chronicle);
        }

        /// <summary>
        /// v5 already owns person-pair family relationships. The migrator keeps them
        /// verbatim and adds only the initial industry state.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV5Fields(SaveData data)
        {
            // v5 predates only the industry block. It already owns the chronicle and
            // person-pair relationships, so the v4 "reject relationships" rule must
            // not be applied here.
            SaveOperationResult legacy = ValidateLegacyV3Fields(data);
            if (!legacy.Succeeded)
                return legacy;
            SaveOperationResult chronicle = ValidateChronicle(data.chronicle);
            if (!chronicle.Succeeded)
                return chronicle;
            return ValidateFamilyRelationships(data.familyRelationships, data.succession);
        }

        /// <summary>
        /// v6 already owns every current block except the SC-020 development block. The
        /// migrator keeps relationships, chronicle, events, succession and industry
        /// verbatim and adds zero growth rows for the existing roster.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV6Fields(SaveData data)
        {
            if (data == null)
                return Invalid("存档数据为空。");
            if (!GameDate.IsValidDate(data.year, data.month, data.day))
                return Invalid("存档日期无效。");
            if (string.IsNullOrWhiteSpace(data.gameVersion) || data.eventContentVersion <= 0)
                return Invalid("存档版本元数据无效。");
            if (!LocationId.TryParse(data.locationId, out _))
                return Invalid("存档地点无效。");
            if (!Enum.IsDefined(typeof(SupplyShortageStage), data.supplyShortageStage) ||
                !Enum.IsDefined(typeof(SealedLetterStage), data.sealedLetterStage) ||
                !Enum.IsDefined(typeof(MarketTallyStage), data.marketTallyStage))
                return Invalid("存档任务阶段无效。");

            SaveOperationResult relationships = ValidateRelationships(data.relationships);
            if (!relationships.Succeeded)
                return relationships;
            if (data.succession == null)
                return Invalid("存档缺少继承状态（succession）。");
            GameDate date = new GameDate(data.year, data.month, data.day);
            SaveOperationResult succession = data.succession.ValidateForSave(date);
            if (!succession.Succeeded)
                return succession;
            SaveOperationResult family = ValidateFamilyRelationships(data.familyRelationships,
                data.succession);
            if (!family.Succeeded)
                return family;
            if (data.industry == null)
                return Invalid("存档缺少产业状态（industry）。");
            SaveOperationResult industry = data.industry.ValidateForSave(date, data.succession);
            if (!industry.Succeeded)
                return industry;
            SaveOperationResult chronicle = ValidateChronicle(data.chronicle);
            if (!chronicle.Succeeded)
                return chronicle;
            if (!RandomStateManager.TryRestore(data.randomState, out _))
                return Invalid("存档随机流状态无效。");
            return ValidateEventRuntimeSnapshot(data.eventRuntime, date);
        }

        /// <summary>
        /// v7 owns every block except the SC-021 research state. The migrator keeps the
        /// existing growth rows and adds empty research without replaying a month.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV7Fields(SaveData data)
        {
            SaveOperationResult legacy = ValidateLegacyV6Fields(data);
            if (!legacy.Succeeded)
                return legacy;
            if (data.development == null)
                return Invalid("存档缺少人物成长状态（development）。");
            return data.development.ValidateForSave(
                new GameDate(data.year, data.month, data.day), data.succession,
                FormerHeadIds(data.chronicle));
        }

        /// <summary>
        /// v8 owns every block except the SC-024 doctrine and active event route. The
        /// migrator keeps the research snapshot and adds the documented doctrine default
        /// without replaying any month.
        /// </summary>
        internal static SaveOperationResult ValidateLegacyV8Fields(SaveData data)
        {
            SaveOperationResult legacy = ValidateLegacyV7Fields(data);
            if (!legacy.Succeeded)
                return legacy;
            if (data.research == null)
                return Invalid("存档缺少家族研究状态（research）。");
            return data.research.ValidateForSave(
                new GameDate(data.year, data.month, data.day), data.succession);
        }

        /// <summary>
        /// Only the two known scenario gates and the empty SC-010/no-route value are
        /// accepted. An unknown route would silently alter which saved event history can
        /// be restored, so it is rejected instead of normalized.
        /// </summary>
        internal static SaveOperationResult ValidateEventScenarioRoute(string route)
        {
            if (route == null)
                return Invalid("存档缺少活动事件路线（activeEventScenarioId）。");
            if (route.Length == 0 ||
                string.Equals(route, SaveData.Sc013EventScenarioId, StringComparison.Ordinal) ||
                string.Equals(route, SaveData.Sc024EventScenarioId, StringComparison.Ordinal))
                return SaveOperationResult.Success();

            return Invalid("存档活动事件路线 ID 未知：" + route + "。");
        }

        /// <summary>
        /// Stable former-head IDs proven by real handoff records in the saved chronicle.
        /// This reuses the fields already written by <c>RecordSuccession</c>; it adds no
        /// save field and does not infer a head role from retirement alone.
        /// </summary>
        internal static HashSet<string> FormerHeadIds(ChronicleSaveEntry[] entries)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (entries == null)
                return ids;
            foreach (ChronicleSaveEntry entry in entries)
            {
                if (entry == null || entry.category != (int)ChronicleCategory.Succession)
                    continue;
                if (!SuccessionHistory.IsRealHeadHandoff(entry.fromHeadId, entry.toHeadId))
                    continue;
                ids.Add(entry.fromHeadId);
            }
            return ids;
        }

        /// <summary>
        /// Validates the complete current-version chronicle through the same domain rules used for
        /// capture, so an accepted save is always restorable without a second check.
        /// </summary>
        internal static SaveOperationResult ValidateChronicle(ChronicleSaveEntry[] entries)
        {
            if (entries == null)
                return Invalid("存档缺少编年史（chronicle）。");
            return ChronicleSaveCodec.TryRestore(entries, out _, out string error)
                ? SaveOperationResult.Success()
                : Invalid(string.IsNullOrEmpty(error) ? "存档编年史记录无效。" : error);
        }

        private static SaveOperationResult ValidateFamilyRelationships(
            FamilyRelationshipSaveEntry[] entries, SuccessionSaveData succession)
        {
            if (entries == null)
                return Invalid("存档缺少家庭关系（familyRelationships）。");
            if (succession == null || succession.persons == null || succession.members == null)
                return Invalid("存档家庭关系缺少继承人物名册。");

            var persons = new HashSet<string>(StringComparer.Ordinal);
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && !string.IsNullOrWhiteSpace(person.personId))
                    persons.Add(person.personId);

            var members = new HashSet<string>(StringComparer.Ordinal);
            foreach (SuccessionMemberSaveEntry member in succession.members)
                if (member != null && !string.IsNullOrWhiteSpace(member.personId))
                    members.Add(member.personId);

            // The head key may be a historical head: old-head records remain valid
            // while that person is still a family member. Membership, not
            // current-head equality, is the invariant enforced here.
            var seen = new HashSet<FamilyRelationshipKey>();
            foreach (FamilyRelationshipSaveEntry entry in entries)
            {
                if (entry == null)
                    return Invalid("家庭关系记录为空。");

                if (!PersonId.TryParse(entry.headPersonId, out PersonId head) || head.IsEmpty ||
                    !PersonId.TryParse(entry.memberPersonId, out PersonId member) || member.IsEmpty)
                    return Invalid($"家庭关系人物 ID 无效：{entry.headPersonId ?? "(null)"} -> " +
                                   $"{entry.memberPersonId ?? "(null)"}。");
                if (head == member)
                    return Invalid("家庭关系不能指向家主本人。");
                if (!persons.Contains(head.Value) || !persons.Contains(member.Value) ||
                    !members.Contains(head.Value) || !members.Contains(member.Value))
                    return Invalid($"家庭关系引用了未知或非家庭成员：{head.Value} -> {member.Value}。");

                var key = new FamilyRelationshipKey(head, member);
                if (!seen.Add(key))
                    return Invalid($"家庭关系人物对重复：{head.Value} -> {member.Value}。");
                if (entry.value < FamilyRelationships.MinimumValue ||
                    entry.value > FamilyRelationships.MaximumValue)
                    return Invalid($"家庭关系值超出范围：{head.Value} -> {member.Value}={entry.value}。");
            }

            return SaveOperationResult.Success();
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
