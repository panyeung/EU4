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
        public const int CurrentVersion = 17;

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
            SaveOperationResult commission = ValidateCommission(data.commission);
            if (!commission.Succeeded)
                return commission;

            SaveOperationResult relationships = ValidateRelationships(data.relationships);
            if (!relationships.Succeeded)
                return relationships;

            if (data.succession == null)
                return Invalid("存档缺少继承状态（succession）。");
            SaveOperationResult succession = data.succession.ValidateForSave(
                new GameDate(data.year, data.month, data.day));
            if (!succession.Succeeded)
                return succession;
            if (data.succession.pregnancies == null)
                return Invalid("存档缺少孕期状态（succession.pregnancies）。");

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

            if (data.market == null)
                return Invalid("存档缺少市场状态（market）。");
            SaveOperationResult market = data.market.ValidateForSave(
                new GameDate(data.year, data.month, data.day));
            if (!market.Succeeded)
                return market;

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

            if (data.dailyActions == null)
                return Invalid("存档缺少每日行动状态（dailyActions）。");
            SaveOperationResult dailyActions = data.dailyActions.ValidateForSave(date,
                data.succession);
            if (!dailyActions.Succeeded)
                return dailyActions;

            // M05 cross-block rule: an in-progress commission must name one living
            // current-family member who is not simultaneously busy elsewhere. Settled
            // historical rows are exempt so a completed result stays loadable after
            // the assignee later dies or leaves the roster.
            SaveOperationResult commissionReferences = ValidateCommissionReferences(data, date);
            if (!commissionReferences.Succeeded)
                return commissionReferences;

            if (data.npcAutonomy == null)
                return Invalid("存档缺少 NPC 自主状态（npcAutonomy）。");
            SaveOperationResult npcAutonomy = data.npcAutonomy.ValidateForSave(
                data.succession, date);
            if (!npcAutonomy.Succeeded)
                return npcAutonomy;

            if (data.familyStanding == null)
                return Invalid("存档缺少家族名望状态（familyStanding）。");
            SaveOperationResult standing = data.familyStanding.ValidateForSave();
            if (!standing.Succeeded)
                return standing;

            if (data.injury == null)
                return Invalid("存档缺少伤员状态（injury）。");
            if (!data.injury.TryRestore(out MissionInjuryState injuries, out string injuryError))
                return Invalid(injuryError);
            for (int i = 0; i < injuries.Injured.Count; i++)
            {
                PersonId injured = injuries.Injured[i];
                if (!IsLivingFamilyMember(data.succession, injured))
                    return Invalid("存档伤员必须是当前在世的家族成员。");
            }

            SaveOperationResult history = ValidateMissionHistory(data, date);
            if (!history.Succeeded)
                return history;

            if (data.campaign == null)
                return Invalid("存档缺少家族纪元状态（campaign）。");
            SaveOperationResult campaign = data.campaign.ValidateForSave(date);
            if (!campaign.Succeeded)
                return campaign;

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
        /// M05: the commission row must describe exactly one legal lifecycle position.
        /// Timing, investment, assignee and outcome are only accepted where the stage
        /// can own them, so a corrupt row never restores a free reward or a phantom
        /// assignee.
        /// </summary>
        internal static SaveOperationResult ValidateCommission(ShadowEscortCommissionSaveData data)
        {
            if (data == null)
                return Invalid("存档缺少 M05 护送委托状态（commission）。");

            ShadowEscortCommission restored = ShadowEscortCommission.TryRestore(
                (ShadowEscortStage)data.stage, data.assigneePersonId, data.durationDays,
                data.elapsedDays, data.startYear, data.startMonth, data.startDay,
                (CommissionOutcome)data.outcome, data.score, data.roll, data.rewardFunds,
                data.rewardSupplies, data.investedFunds, data.lastFeedback, data.runId,
                true, (MissionOperationMode)data.mode, out string error);
            if (restored == null)
                return Invalid(string.IsNullOrEmpty(error) ? "存档 M05 护送委托状态无效。" : error);
            return SaveOperationResult.Success();
        }

        /// <summary>
        /// M05 cross-block rule shared by save validation and load. Only an in-progress
        /// commission constrains its assignee: that person must be a living current
        /// family member who is not simultaneously executing a daily action or holding
        /// an industry post. Settled rows keep their historical assignee regardless of
        /// later death or departure, so a completed result never becomes unloadable.
        ///
        /// Work-duty eligibility (adult, not retired) is enforced when an assignee is
        /// chosen, not here; that matches <c>GameSession.CommissionAssigneeBlockReason</c>,
        /// so a person who retires while escorting finishes the trip. Rejecting retirement
        /// here would strand a save produced by the legal retire-then-save order, and
        /// rejecting age here would strand an in-progress row written before that rule
        /// existed. Both rules live at the assignment boundary instead.
        /// </summary>
        internal static SaveOperationResult ValidateCommissionReferences(SaveData data,
            GameDate currentDate)
        {
            if (data == null || data.commission == null)
                return SaveOperationResult.Success();

            // Historical rows may name a person who has since died or left, but no row
            // can claim an execution window that starts in the future.
            GameDate startDate = default;
            bool hasStart = false;
            if (data.commission.stage != (int)ShadowEscortStage.NotAccepted &&
                data.commission.stage != (int)ShadowEscortStage.Accepted)
            {
                if (!GameDate.TryParse(data.commission.startYear, data.commission.startMonth,
                        data.commission.startDay, out startDate))
                    return Invalid("存档委托开始日期无效。");
                hasStart = true;
                if (startDate > currentDate)
                    return Invalid("存档委托开始日期晚于存档日期。");
            }

            if (data.commission.stage != (int)ShadowEscortStage.InProgress)
                return SaveOperationResult.Success();

            // An executing commission consumes exactly one elapsed day per simulated
            // calendar day since it started, capped at the fixed window so a due row can
            // still be retried after its settlement failed. A same-day row with elapsed
            // progress would otherwise settle after fewer simulated days than the rule.
            if (!hasStart)
                return Invalid("存档委托开始日期无效。");
            int simulatedDays = CalendarDaysBetween(startDate, currentDate);
            int expectedElapsed = Math.Min(ShadowEscortCommission.DurationDays, simulatedDays);
            if (data.commission.elapsedDays != expectedElapsed)
                return Invalid("存档委托已用天数与开始日期以来的模拟天数不一致。");
            if (!PersonId.TryParse(data.commission.assigneePersonId, out PersonId assignee) ||
                assignee.IsEmpty)
                return Invalid("存档委托执行人 ID 无效。");
            if (data.succession == null || !IsLivingFamilyMember(data.succession, assignee))
                return Invalid("存档委托执行人必须是当前在世的家族成员。");
            if (HasActiveDailyAction(data.dailyActions, assignee))
                return Invalid("存档委托执行人同时正在执行每日行动。");
            if (HasIndustryPost(data.industry, assignee))
                return Invalid("存档委托执行人同时担任产业职务。");
            if (HasResearchDuty(data.research, assignee))
                return Invalid("存档委托执行人同时负责家族研究。");
            if (HasEducationMentorDuty(data.development, assignee))
                return Invalid("存档委托执行人同时担任培养导师。");
            return SaveOperationResult.Success();
        }

        /// <summary>
        /// M07 terminal mission history. Every row must be structurally valid, run IDs
        /// are unique, rows are date-ordered and each linked chronicle entry must exist.
        /// A settled M05 commission must have exactly one matching history row, while an
        /// in-progress run must not have one yet.
        /// </summary>
        private static SaveOperationResult ValidateMissionHistory(SaveData data, GameDate date)
        {
            if (data.missionHistory == null)
                return Invalid("存档缺少任务终局历史（missionHistory）。");

            var history = new MissionHistoryState();
            var chronicleIds = new HashSet<string>(StringComparer.Ordinal);
            if (data.chronicle != null)
                foreach (ChronicleSaveEntry entry in data.chronicle)
                    if (entry != null && !string.IsNullOrEmpty(entry.entryId))
                        chronicleIds.Add(entry.entryId);

            GameDate previousDate = default;
            for (int i = 0; i < data.missionHistory.Length; i++)
            {
                MissionHistorySaveEntry entry = data.missionHistory[i];
                if (entry == null)
                    return Invalid("存档任务终局历史包含空行。");
                if (!entry.TryRestore(out MissionRecord record, out string recordError))
                    return Invalid(recordError);
                if (!history.TryAppend(record, out string appendError))
                    return Invalid(appendError);
                if (previousDate.IsValid && record.Date < previousDate)
                    return Invalid("存档任务终局历史未按日期排序。");
                if (record.Date > date)
                    return Invalid("存档任务终局历史日期晚于存档日期。");
                previousDate = record.Date;
                if (!chronicleIds.Contains(record.ChronicleEntryId))
                    return Invalid("存档任务终局历史引用了不存在的编年史记录。");
                for (int p = 0; p < record.ParticipantStates.Count; p++)
                {
                    MissionParticipantState state = record.ParticipantStates[p];
                    bool savedAlive = IsLivingFamilyMember(data.succession, state.PersonId);
                    bool known = HasPerson(data.succession, state.PersonId);
                    if (!known)
                        return Invalid("存档任务终局历史引用了未登记人物。");
                    if (state.AliveBefore && !state.AliveAfter && savedAlive)
                        return Invalid("存档任务终局历史记录队员阵亡，但人物仍存活。");
                }
            }

            if (data.commission != null)
            {
                ShadowEscortStage stage = (ShadowEscortStage)data.commission.stage;
                bool executing = stage == ShadowEscortStage.InProgress;
                bool settled = stage == ShadowEscortStage.Succeeded ||
                               stage == ShadowEscortStage.PartialSuccess ||
                               stage == ShadowEscortStage.Failed;
                if (executing && !string.IsNullOrEmpty(data.commission.runId) &&
                    history.Contains(data.commission.runId))
                    return Invalid("执行中的委托不得已有终局记录。");
                if (settled)
                {
                    if (string.IsNullOrEmpty(data.commission.runId) ||
                        !history.Contains(data.commission.runId))
                        return Invalid("已结算的委托缺少匹配的任务终局记录。");
                }
            }
            return SaveOperationResult.Success();
        }

        private static bool HasPerson(SuccessionSaveData succession, PersonId personId)
        {
            if (succession == null || succession.persons == null)
                return false;
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && person.personId == personId.Value)
                    return true;
            return false;
        }

        private static int CalendarDaysBetween(GameDate from, GameDate to)
        {
            // Only the capped distance matters: once the fixed window is reached, a
            // settlement retry keeps the row at DurationDays regardless of how many
            // later days have passed, so there is no reason to count years of days.
            int days = 0;
            GameDate cursor = from;
            while (days < ShadowEscortCommission.DurationDays && cursor < to &&
                   GameDate.TryNext(cursor, out GameDate next))
            {
                cursor = next;
                days++;
            }
            return days;
        }

        private static bool HasResearchDuty(FamilyResearchSaveData research, PersonId personId) =>
            research != null && !string.IsNullOrEmpty(research.researchId) &&
            research.researcherPersonId == personId.Value;

        /// <summary>
        /// True when the person mentors an annual education plan whose year has not
        /// settled yet; a retained plan for an already settled year is historical.
        /// </summary>
        private static bool HasEducationMentorDuty(CharacterDevelopmentSaveData development,
            PersonId personId)
        {
            if (development == null || development.persons == null)
                return false;
            foreach (CharacterDevelopmentPersonSaveEntry person in development.persons)
            {
                if (person == null || person.plans == null)
                    continue;
                foreach (EducationPlanSaveEntry plan in person.plans)
                {
                    if (plan == null || plan.mentorPersonId != personId.Value)
                        continue;
                    bool settled = false;
                    if (person.results != null)
                        foreach (EducationResultSaveEntry result in person.results)
                            if (result != null && result.ageYear == plan.ageYear)
                            {
                                settled = true;
                                break;
                            }
                    if (!settled)
                        return true;
                }
            }
            return false;
        }

        private static bool IsLivingFamilyMember(SuccessionSaveData succession, PersonId personId)
        {
            if (succession.persons == null || succession.members == null)
                return false;
            bool alive = false;
            foreach (SuccessionPersonSaveEntry person in succession.persons)
            {
                if (person != null && person.personId == personId.Value)
                {
                    alive = person.alive;
                    break;
                }
            }
            if (!alive)
                return false;
            foreach (SuccessionMemberSaveEntry member in succession.members)
                if (member != null && member.personId == personId.Value)
                    return true;
            return false;
        }

        private static bool HasActiveDailyAction(DailyActionStateSaveData dailyActions,
            PersonId personId)
        {
            if (dailyActions == null || dailyActions.persons == null)
                return false;
            foreach (DailyActionPersonSaveEntry person in dailyActions.persons)
                if (person != null && person.personId == personId.Value && person.hasAction)
                    return true;
            return false;
        }

        private static bool HasIndustryPost(IndustryStateSaveData industry, PersonId personId)
        {
            if (industry == null || industry.slots == null)
                return false;
            foreach (IndustrySlotSaveEntry slot in industry.slots)
                if (slot != null && !string.IsNullOrEmpty(slot.assignedPersonId) &&
                    slot.assignedPersonId == personId.Value)
                    return true;
            return false;
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
                probe.saveFormatVersion != 8 && probe.saveFormatVersion != 9 &&
                probe.saveFormatVersion != 10 && probe.saveFormatVersion != 11 &&
                probe.saveFormatVersion != 12 &&
                probe.saveFormatVersion != 13 &&
                probe.saveFormatVersion != 14 &&
                probe.saveFormatVersion != 15 &&
                probe.saveFormatVersion != 16 &&
                probe.saveFormatVersion != CurrentVersion)
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
            // v7 introduced development; a v7 payload may legitimately carry it. Only
            // v2-v6 cannot own that block, so v7 falls through to the research check.
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 7 &&
                HasNonNullRootProperty(json, "development"))
                return Invalid("旧版存档包含人物成长状态（development）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasRootProperty(json, "research") && probe.saveFormatVersion < 8)
                return Invalid("旧版存档包含家族研究状态（research）；迁移不会静默替换该数据。");
            // v9 introduced doctrine and the active event route. v10 and v11 may
            // legitimately carry both; only v2-v8 predate them.
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 9 &&
                HasRootProperty(json, "familyDoctrine"))
                return Invalid("旧版存档包含 v9 家训与抱负状态（familyDoctrine）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 9 &&
                HasRootProperty(json, "activeEventScenarioId"))
                return Invalid("旧版存档包含 v9 活动事件路线（activeEventScenarioId）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasRootProperty(json, "dailyActions") && probe.saveFormatVersion < 10)
                return Invalid("旧版存档包含 v10 每日行动状态（dailyActions）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 11 &&
                HasRootProperty(json, "npcAutonomy"))
                return Invalid("旧版存档包含 v11 NPC 自主状态（npcAutonomy）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 14 &&
                HasRootProperty(json, "commission"))
                return Invalid("旧版存档包含 v14 护送委托状态（commission）；迁移不会静默替换该数据。");
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 15)
            {
                if (HasRootProperty(json, "familyStanding"))
                    return Invalid("旧版存档包含 M07 家族名望状态（familyStanding）；迁移不会静默替换该数据。");
                if (HasRootProperty(json, "injury"))
                    return Invalid("旧版存档包含 M07 伤员状态（injury）；迁移不会静默替换该数据。");
                if (HasRootProperty(json, "missionHistory"))
                    return Invalid("旧版存档包含 M07 任务终局历史（missionHistory）；迁移不会静默替换该数据。");
            }
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < 16 &&
                HasRootProperty(json, "campaign"))
                return Invalid("旧版存档包含 M08 家族纪元状态（campaign）；迁移不会静默替换该数据。");
            // v17 introduced the market block. A pre-v17 file that already carries one
            // cannot own it, so migration must refuse instead of overwriting the data.
            if (probe.saveFormatVersion >= 2 && probe.saveFormatVersion < CurrentVersion &&
                HasRootProperty(json, "market"))
                return Invalid("旧版存档包含 M08B 市场状态（market）；迁移不会静默替换该数据。");

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
            if (probe.saveFormatVersion >= 9 &&
                (probe.familyDoctrine == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "familyDoctrine"))))
                return Invalid("存档缺少家训与抱负状态（familyDoctrine）。");
            if (probe.saveFormatVersion >= 9 &&
                (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "activeEventScenarioId")))
                return Invalid("存档缺少活动事件路线（activeEventScenarioId）。");
            if (probe.saveFormatVersion >= 10 &&
                (probe.dailyActions == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "dailyActions"))))
                return Invalid("存档缺少每日行动状态（dailyActions）。");
            if (probe.saveFormatVersion >= 11 &&
                (probe.npcAutonomy == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "npcAutonomy"))))
                return Invalid("存档缺少 NPC 自主状态（npcAutonomy）。");
            if (probe.saveFormatVersion >= 12 && probe.npcAutonomy != null)
            {
                if (probe.npcAutonomy.persons == null)
                    return Invalid("存档缺少 NPC 自主人物记录（npcAutonomy.persons）。");
                for (int i = 0; i < probe.npcAutonomy.persons.Length; i++)
                {
                    NpcAutonomyPersonSaveEntry person = probe.npcAutonomy.persons[i];
                    if (person == null || person.memories == null)
                        return Invalid(
                            "存档缺少 NPC 行动记忆列表（npcAutonomy.persons[].memories）。");
                }
            }

            // v13 introduced person gender and active pregnancies. A v12 payload is
            // migrated into that shape by SaveDataMigrator. v13 already owns the
            // pregnancy list, so a v13 payload that omits it is malformed and must be
            // rejected instead of being silently replaced with an empty list.
            if (probe.saveFormatVersion >= 13 &&
                (probe.succession == null || probe.succession.pregnancies == null))
                return Invalid("存档缺少孕期状态（succession.pregnancies）。");
            if (probe.saveFormatVersion >= 14 &&
                (probe.commission == null ||
                 (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "commission"))))
                return Invalid("存档缺少 M05 护送委托状态（commission）。");
            if (probe.saveFormatVersion >= 15)
            {
                if (probe.familyStanding == null ||
                    (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "familyStanding")))
                    return Invalid("存档缺少 M07 家族名望状态（familyStanding）。");
                if (probe.injury == null ||
                    (!string.IsNullOrEmpty(json) && !HasRootProperty(json, "injury")))
                    return Invalid("存档缺少 M07 伤员状态（injury）。");
                if (probe.missionHistory == null ||
                    (!string.IsNullOrEmpty(json) && !HasRootProperty(json, "missionHistory")))
                    return Invalid("存档缺少 M07 任务终局历史（missionHistory）。");
            }
            if (probe.saveFormatVersion >= 16)
            {
                if (probe.campaign == null ||
                    (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "campaign")))
                    return Invalid("存档缺少 M08 家族纪元状态（campaign）。");
            }
            if (probe.saveFormatVersion >= CurrentVersion)
            {
                if (probe.market == null ||
                    (!string.IsNullOrEmpty(json) && !HasNonNullRootProperty(json, "market")))
                    return Invalid("存档缺少 M08B 市场状态（market）。");
            }

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
