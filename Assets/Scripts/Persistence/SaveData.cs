using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Flat, versioned root of the single Phase 1 save slot.
    /// Contains only plain data and stable IDs; never Unity objects or asset references.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Single schema-version source; mirrors <see cref="SaveDataValidator.CurrentVersion"/>.</summary>
        public const int CurrentGameSchemaVersion = SaveDataValidator.CurrentVersion;

        /// <summary>Rules contract version shared by capture, validation and migration.</summary>
        public const int CurrentRulesVersion = 7;

        /// <summary>
        /// Fallback event-content version used only when no catalog is loaded. The
        /// active catalog's <c>contentVersion</c> is authoritative for capture/load.
        /// </summary>
        public const int CurrentEventContentVersion = 3;

        /// <summary>Displayed game build version. Older values are not rejected on load.</summary>
        public const string CurrentGameVersion = "0.1.0";

        /// <summary>
        /// Stable active-event-route IDs persisted by v9. Persistence cannot reference
        /// the Events assembly, so these mirror <c>EventScenarioIds</c> intentionally;
        /// unknown values are rejected instead of being normalized.
        /// </summary>
        public const string Sc013EventScenarioId = "sc013";
        public const string Sc024EventScenarioId = "sc024";

        public int saveFormatVersion;
        public string gameVersion;
        public int rulesVersion;
        public int eventContentVersion;
        public int year;
        public int month;
        public int day;
        public string locationId;
        public int supplyShortageStage;
        public int sealedLetterStage;
        public int marketTallyStage;
        public RelationshipSaveEntry[] relationships;
        public FamilyRelationshipSaveEntry[] familyRelationships;
        public RandomStateSaveData randomState;
        public EventRuntimeSaveData eventRuntime;
        public SuccessionSaveData succession;
        public ChronicleSaveEntry[] chronicle;
        public IndustryStateSaveData industry;
        public CharacterDevelopmentSaveData development;
        public FamilyResearchSaveData research;
        public FamilyDoctrineSaveData familyDoctrine;
        public string activeEventScenarioId;
        public DailyActionStateSaveData dailyActions;
        public NpcAutonomyStateSaveData npcAutonomy;

        public SaveData()
        {
            saveFormatVersion = SaveDataValidator.CurrentVersion;
            gameVersion = CurrentGameVersion;
            rulesVersion = CurrentRulesVersion;
            eventContentVersion = CurrentEventContentVersion;
            relationships = new RelationshipSaveEntry[0];
            familyRelationships = new FamilyRelationshipSaveEntry[0];
            randomState = RandomStateManager.Create(0UL).CaptureState();
            eventRuntime = new EventRuntimeSaveData();
            succession = SuccessionSaveData.CreateDefault();
            chronicle = new ChronicleSaveEntry[0];
            industry = IndustryStateSaveData.CreateInitial(WorldClock.NewGameDate);
            // A new SaveData owns the default roster. Its v7 development block must
            // cover exactly that roster so the constructor's baseline is a valid save;
            // callers that replace the succession snapshot must supply matching rows.
            development = CharacterDevelopmentSaveData.CreateEmpty(succession);
            research = FamilyResearchSaveData.CreateInitial(WorldClock.NewGameDate);
            familyDoctrine = FamilyDoctrineSaveData.CreateDefault();
            activeEventScenarioId = string.Empty;
            dailyActions = DailyActionStateSaveData.CreateInitial(WorldClock.NewGameDate,
                succession, LocationId.Kiyosu.Value);
            npcAutonomy = NpcAutonomyStateSaveData.CreateInitial(succession);
        }
    }

    /// <summary>
    /// Presence probe parsed from raw save JSON before <see cref="SaveData"/>.
    /// Unity's <c>JsonUtility.FromJson</c> runs the parameterless constructor and
    /// field initializers, so a v3 file that omits a field would otherwise be
    /// indistinguishable from that field's constructor default. Scalar fields here
    /// stay zero/empty when absent. Nested <c>[Serializable]</c> classes are still
    /// materialized by JsonUtility even when their property is absent, so callers
    /// must pair this probe with a root-property check on the raw JSON.
    /// </summary>
    [Serializable]
    public sealed class SaveDataPresenceProbe
    {
        public int saveFormatVersion;
        public string gameVersion;
        public int rulesVersion;
        public int eventContentVersion;
        public RandomStateSaveData randomState;
        public EventRuntimeSaveData eventRuntime;
        public SuccessionSaveData succession;
        public ChronicleSaveEntry[] chronicle;
        public FamilyRelationshipSaveEntry[] familyRelationships;
        public IndustryStateSaveData industry;
        public CharacterDevelopmentSaveData development;
        public FamilyResearchSaveData research;
        public FamilyDoctrineSaveData familyDoctrine;
        public string activeEventScenarioId;
        public DailyActionStateSaveData dailyActions;
        public NpcAutonomyStateSaveData npcAutonomy;
    }

    /// <summary>One player-to-NPC relationship value, keyed by stable character ID.</summary>
    [Serializable]
    public sealed class RelationshipSaveEntry
    {
        public string characterId;
        public int value;
    }

    /// <summary>One ordered (head, family member) pair value.</summary>
    [Serializable]
    public sealed class FamilyRelationshipSaveEntry
    {
        public string headPersonId;
        public string memberPersonId;
        public int value;
    }

    /// <summary>
    /// Versioned SC-019 industry snapshot: the three resources, the last settled
    /// month and exactly two stable slots. Runtime reports are not persisted.
    /// </summary>
    [Serializable]
    public sealed class IndustryStateSaveData
    {
        public int funds;
        public int supplies;
        public int intelligence;
        public int lastSettledMonthIndex;
        public IndustrySlotSaveEntry[] slots;

        /// <summary>Opening state for a new game or a migrated legacy save.</summary>
        public static IndustryStateSaveData CreateInitial(GameDate date)
        {
            var slots = new IndustrySlotSaveEntry[IndustryCatalog.All.Count];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = new IndustrySlotSaveEntry
                {
                    industryId = IndustryCatalog.All[i].Id,
                    assignedPersonId = string.Empty
                };

            return new IndustryStateSaveData
            {
                funds = 30,
                supplies = 12,
                intelligence = 0,
                lastSettledMonthIndex = IndustryCalendar.MonthIndex(date) - 1,
                slots = slots
            };
        }

        public static IndustryStateSaveData Capture(IndustryState state)
        {
            if (state == null)
                return null;

            var slots = new IndustrySlotSaveEntry[state.Assignments.Count];
            for (int i = 0; i < slots.Length; i++)
            {
                IndustryAssignment assignment = state.Assignments[i];
                slots[i] = new IndustrySlotSaveEntry
                {
                    industryId = assignment.IndustryId,
                    assignedPersonId = assignment.PersonId.IsEmpty ? string.Empty : assignment.PersonId.Value
                };
            }

            return new IndustryStateSaveData
            {
                funds = state.Resources.Funds,
                supplies = state.Resources.Supplies,
                intelligence = state.Resources.Intelligence,
                lastSettledMonthIndex = state.LastSettledMonthIndex,
                slots = slots
            };
        }

        public IndustryStateSaveData Clone()
        {
            var clone = new IndustryStateSaveData
            {
                funds = funds,
                supplies = supplies,
                intelligence = intelligence,
                lastSettledMonthIndex = lastSettledMonthIndex,
                slots = new IndustrySlotSaveEntry[slots != null ? slots.Length : 0]
            };
            for (int i = 0; i < clone.slots.Length; i++)
            {
                IndustrySlotSaveEntry entry = slots[i];
                clone.slots[i] = entry == null ? null : new IndustrySlotSaveEntry
                {
                    industryId = entry.industryId,
                    assignedPersonId = entry.assignedPersonId
                };
            }
            return clone;
        }

        /// <summary>Structural and cross-reference validation; never throws.</summary>
        public SaveOperationResult ValidateForSave(GameDate date, SuccessionSaveData succession)
        {
            if (funds < IndustryResources.Minimum || funds > IndustryResources.Maximum)
                return Invalid("存档产业资金超出范围 0..999999。");
            if (supplies < IndustryResources.Minimum || supplies > IndustryResources.Maximum)
                return Invalid("存档产业物资超出范围 0..999999。");
            if (intelligence < IndustryResources.Minimum || intelligence > IndustryResources.Maximum)
                return Invalid("存档产业情报超出范围 0..999999。");
            if (lastSettledMonthIndex != IndustryCalendar.MonthIndex(date) - 1)
                return Invalid("存档产业已结算月份与日期不一致。");
            if (slots == null || slots.Length != IndustryCatalog.All.Count)
                return Invalid("存档产业槽位必须恰好包含两个产业。");
            if (succession == null || succession.persons == null || succession.members == null)
                return Invalid("存档产业缺少继承人物名册。");

            var persons = new Dictionary<PersonId, SuccessionPersonSaveEntry>();
            foreach (SuccessionPersonSaveEntry person in succession.persons)
            {
                if (person != null && PersonId.TryParse(person.personId, out PersonId personId))
                    persons[personId] = person;
            }
            var members = new HashSet<PersonId>();
            foreach (SuccessionMemberSaveEntry member in succession.members)
            {
                if (member != null && PersonId.TryParse(member.personId, out PersonId memberId))
                    members.Add(memberId);
            }

            var seenIndustries = new HashSet<string>(StringComparer.Ordinal);
            var seenPersons = new HashSet<PersonId>();
            string previousIndustryId = null;
            foreach (IndustrySlotSaveEntry slot in slots)
            {
                if (slot == null)
                    return Invalid("存档产业槽位为空。");
                if (string.IsNullOrWhiteSpace(slot.industryId) ||
                    !IndustryCatalog.TryGet(slot.industryId, out _))
                    return Invalid("存档产业槽位包含未知产业 ID。");
                if (!seenIndustries.Add(slot.industryId))
                    return Invalid("存档产业槽位产业 ID 重复。");
                if (previousIndustryId != null &&
                    string.CompareOrdinal(previousIndustryId, slot.industryId) > 0)
                    return Invalid("存档产业槽位顺序不稳定。");
                previousIndustryId = slot.industryId;

                if (string.IsNullOrWhiteSpace(slot.assignedPersonId))
                    continue;
                if (!PersonId.TryParse(slot.assignedPersonId, out PersonId assignee) ||
                    !persons.TryGetValue(assignee, out SuccessionPersonSaveEntry personEntry))
                    return Invalid("存档产业负责人不是已登记人物。");
                if (!members.Contains(assignee))
                    return Invalid("存档产业负责人不是当前家庭成员。");
                if (!personEntry.alive)
                    return Invalid("存档产业负责人已死亡。");
                if (personEntry.retired)
                    return Invalid("存档产业负责人已退休。");
                if (date.Year - personEntry.birthYear < succession.adulthoodAge)
                    return Invalid("存档产业负责人未成年。");
                if (!seenPersons.Add(assignee))
                    return Invalid("同一人物不能同时担任两个产业职位。");
            }

            return SaveOperationResult.Success();
        }

        /// <summary>Rebuilds a validated runtime state; the caller must not assign it on failure.</summary>
        public static bool TryRestore(IndustryStateSaveData data, SuccessionSaveData succession,
            GameDate date, out IndustryState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档缺少产业状态（industry）。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(date, succession);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            if (!IndustryResources.TryCreate(data.funds, data.supplies, data.intelligence,
                    out IndustryResources resources, out error))
                return false;

            IndustryState restored = IndustryState.CreateInitial(date);
            restored.TrySetResources(resources, out _);
            restored.TrySetLastSettledMonthIndex(data.lastSettledMonthIndex, out _);
            foreach (IndustrySlotSaveEntry slot in data.slots)
            {
                if (!restored.TrySetAssignee(slot.industryId,
                        string.IsNullOrWhiteSpace(slot.assignedPersonId)
                            ? default
                            : new PersonId(slot.assignedPersonId), out error))
                    return false;
            }

            state = restored;
            error = string.Empty;
            return true;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class IndustrySlotSaveEntry
    {
        public string industryId;
        public string assignedPersonId;
    }

    /// <summary>
    /// Plain-data v7 SC-020 snapshot. Every family person has one sorted row with the
    /// five growth values, at most one plan and at most one settled result per 0..15
    /// age year. Runtime reports and display names never enter the save.
    /// </summary>
    [Serializable]
    public sealed class CharacterDevelopmentSaveData
    {
        public CharacterDevelopmentPersonSaveEntry[] persons;

        public static CharacterDevelopmentSaveData CreateEmpty() =>
            new CharacterDevelopmentSaveData { persons = new CharacterDevelopmentPersonSaveEntry[0] };

        /// <summary>Migration default: every existing person gets five zeros and no history.</summary>
        public static CharacterDevelopmentSaveData CreateEmpty(SuccessionSaveData succession)
        {
            if (succession == null || succession.persons == null)
                return CreateEmpty();

            var persons = new CharacterDevelopmentPersonSaveEntry[succession.persons.Length];
            for (int i = 0; i < persons.Length; i++)
                persons[i] = new CharacterDevelopmentPersonSaveEntry
                {
                    personId = succession.persons[i] != null ? succession.persons[i].personId : string.Empty,
                    plans = new EducationPlanSaveEntry[0],
                    results = new EducationResultSaveEntry[0]
                };
            return new CharacterDevelopmentSaveData { persons = persons };
        }

        public static CharacterDevelopmentSaveData Capture(CharacterDevelopmentState state)
        {
            if (state == null)
                return CreateEmpty();

            IReadOnlyList<PersonId> ids = state.PersonIds;
            var persons = new CharacterDevelopmentPersonSaveEntry[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                PersonId personId = ids[i];
                state.TryGetValues(personId, out CharacterGrowthValues values);
                IReadOnlyList<int> plannedYears = state.PlannedAgeYears(personId);
                var plans = new EducationPlanSaveEntry[plannedYears.Count];
                for (int p = 0; p < plans.Length; p++)
                {
                    state.TryGetPlan(personId, plannedYears[p], out EducationYearPlan plan);
                    plans[p] = new EducationPlanSaveEntry
                    {
                        ageYear = plan.AgeYear,
                        direction = (int)plan.Direction,
                        mentorPersonId = plan.Mentor.IsEmpty ? string.Empty : plan.Mentor.Value
                    };
                }

                IReadOnlyList<int> settledYears = state.SettledAgeYears(personId);
                var results = new EducationResultSaveEntry[settledYears.Count];
                for (int r = 0; r < results.Length; r++)
                {
                    state.TryGetResult(personId, settledYears[r], out EducationYearResult result);
                    results[r] = new EducationResultSaveEntry
                    {
                        ageYear = settledYears[r],
                        direction = (int)result.Direction,
                        mentorPersonId = result.Mentor.IsEmpty ? string.Empty : result.Mentor.Value,
                        before = new GrowthValuesSaveEntry
                        {
                            ninjutsu = result.Before.Ninjutsu,
                            martialArts = result.Before.MartialArts,
                            scholarship = result.Before.Scholarship,
                            management = result.Before.Management,
                            social = result.Before.Social
                        },
                        after = new GrowthValuesSaveEntry
                        {
                            ninjutsu = result.After.Ninjutsu,
                            martialArts = result.After.MartialArts,
                            scholarship = result.After.Scholarship,
                            management = result.After.Management,
                            social = result.After.Social
                        },
                        explanation = result.Explanation,
                        usedFreeGrowthFallback = result.UsedFreeGrowthFallback
                    };
                }

                persons[i] = new CharacterDevelopmentPersonSaveEntry
                {
                    personId = personId.Value,
                    ninjutsu = values.Ninjutsu,
                    martialArts = values.MartialArts,
                    scholarship = values.Scholarship,
                    management = values.Management,
                    social = values.Social,
                    plans = plans,
                    results = results
                };
            }

            return new CharacterDevelopmentSaveData { persons = persons };
        }

        public CharacterDevelopmentSaveData Clone()
        {
            var clone = new CharacterDevelopmentSaveData
            {
                persons = new CharacterDevelopmentPersonSaveEntry[persons != null ? persons.Length : 0]
            };
            for (int i = 0; i < clone.persons.Length; i++)
            {
                CharacterDevelopmentPersonSaveEntry entry = persons[i];
                if (entry == null)
                {
                    clone.persons[i] = null;
                    continue;
                }

                var plans = new EducationPlanSaveEntry[entry.plans != null ? entry.plans.Length : 0];
                for (int p = 0; p < plans.Length; p++)
                {
                    EducationPlanSaveEntry plan = entry.plans[p];
                    plans[p] = plan == null ? null : new EducationPlanSaveEntry
                    {
                        ageYear = plan.ageYear,
                        direction = plan.direction,
                        mentorPersonId = plan.mentorPersonId
                    };
                }

                var results = new EducationResultSaveEntry[entry.results != null ? entry.results.Length : 0];
                for (int r = 0; r < results.Length; r++)
                {
                    EducationResultSaveEntry result = entry.results[r];
                    results[r] = result == null ? null : new EducationResultSaveEntry
                    {
                        ageYear = result.ageYear,
                        direction = result.direction,
                        mentorPersonId = result.mentorPersonId,
                        before = CloneGrowth(result.before),
                        after = CloneGrowth(result.after),
                        explanation = result.explanation,
                        usedFreeGrowthFallback = result.usedFreeGrowthFallback
                    };
                }

                clone.persons[i] = new CharacterDevelopmentPersonSaveEntry
                {
                    personId = entry.personId,
                    ninjutsu = entry.ninjutsu,
                    martialArts = entry.martialArts,
                    scholarship = entry.scholarship,
                    management = entry.management,
                    social = entry.social,
                    plans = plans,
                    results = results
                };
            }
            return clone;
        }

        private static GrowthValuesSaveEntry CloneGrowth(GrowthValuesSaveEntry source) =>
            source == null ? null : new GrowthValuesSaveEntry
            {
                ninjutsu = source.ninjutsu,
                martialArts = source.martialArts,
                scholarship = source.scholarship,
                management = source.management,
                social = source.social
            };

        /// <summary>
        /// Validates every row, key, plan, mentor and result against the succession
        /// snapshot and the save date. The person set must match the roster exactly, so a
        /// missing growth block is rejected instead of being silently defaulted.
        /// </summary>
        public SaveOperationResult ValidateForSave(GameDate date, SuccessionSaveData succession) =>
            ValidateForSave(date, succession, null);

        /// <summary>
        /// Same validation with the former-head proof derived from the saved succession
        /// chronicle. SC-023 uses it to distinguish a retired former head from any other
        /// retired direct kin without adding a save field or schema version.
        /// </summary>
        public SaveOperationResult ValidateForSave(GameDate date, SuccessionSaveData succession,
            HashSet<string> formerHeadIds)
        {
            if (succession == null || succession.persons == null || succession.members == null)
                return Invalid("存档缺少人物成长所需的名册。");
            if (persons == null)
                return Invalid("存档缺少人物成长（development）。");

            var roster = new Dictionary<PersonId, SuccessionPersonSaveEntry>();
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && PersonId.TryParse(person.personId, out PersonId personId))
                    roster[personId] = person;
            var members = new HashSet<PersonId>();
            var roles = new Dictionary<PersonId, FamilyMemberRole>();
            foreach (SuccessionMemberSaveEntry member in succession.members)
                if (member != null && PersonId.TryParse(member.personId, out PersonId memberId))
                {
                    members.Add(memberId);
                    if (Enum.IsDefined(typeof(FamilyMemberRole), member.role))
                        roles[memberId] = (FamilyMemberRole)member.role;
                }

            if (persons.Length != roster.Count)
                return Invalid("存档人物成长记录必须覆盖名册中的每个人物。");

            var seenPersons = new HashSet<PersonId>();
            string previousPersonId = null;
            foreach (CharacterDevelopmentPersonSaveEntry entry in persons)
            {
                if (entry == null)
                    return Invalid("存档人物成长记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId) || !roster.ContainsKey(personId))
                    return Invalid("存档人物成长记录引用了未知人物。");
                if (!seenPersons.Add(personId))
                    return Invalid("存档人物成长记录人物重复。");
                if (previousPersonId != null &&
                    string.CompareOrdinal(previousPersonId, entry.personId) > 0)
                    return Invalid("存档人物成长记录顺序不稳定。");
                previousPersonId = entry.personId;
                if (!ValidGrowth(entry.ninjutsu, entry.martialArts, entry.scholarship,
                        entry.management, entry.social))
                    return Invalid("存档人物成长能力超出范围 0..100。");

                if (!ValidatePlans(entry, personId, members, roles, roster, date,
                        succession.adulthoodAge, succession.familyId, formerHeadIds,
                        out string planError))
                    return Invalid(planError);
                if (!ValidateResults(entry, personId, roster, out string resultError))
                    return Invalid(resultError);
                if (!ValidateValuesMatchLatestResult(entry, out string valueError))
                    return Invalid(valueError);
            }

            foreach (PersonId rosterId in roster.Keys)
                if (!seenPersons.Contains(rosterId))
                    return Invalid("存档缺少名册人物的成长记录：" + rosterId.Value + "。");

            return SaveOperationResult.Success();
        }

        private bool ValidatePlans(CharacterDevelopmentPersonSaveEntry entry, PersonId learner,
            HashSet<PersonId> members, Dictionary<PersonId, FamilyMemberRole> roles,
            Dictionary<PersonId, SuccessionPersonSaveEntry> roster, GameDate date, int adulthoodAge,
            string familyId, HashSet<string> formerHeadIds, out string error)
        {
            if (entry.plans == null)
            {
                error = "存档人物成长计划列表为空。";
                return false;
            }

            var seen = new HashSet<int>();
            int previousAge = -1;
            foreach (EducationPlanSaveEntry plan in entry.plans)
            {
                if (plan == null)
                {
                    error = "存档人物成长计划为空。";
                    return false;
                }
                if (plan.ageYear < EducationYearPlan.MinimumAgeYear ||
                    plan.ageYear > EducationYearPlan.MaximumAgeYear)
                {
                    error = "存档人物成长计划年度超出 0..15。";
                    return false;
                }
                if (plan.ageYear > date.Year - roster[learner].birthYear)
                {
                    error = "存档人物成长计划年度超出当前年龄。";
                    return false;
                }
                if (!seen.Add(plan.ageYear) || plan.ageYear <= previousAge)
                {
                    error = "存档人物成长计划年度重复或顺序不稳定。";
                    return false;
                }
                previousAge = plan.ageYear;

                bool free = plan.direction == (int)EducationDirection.FreeGrowth;
                if (!Enum.IsDefined(typeof(EducationDirection), plan.direction))
                {
                    error = "存档人物成长计划方向无效。";
                    return false;
                }
                bool hasMentor = !string.IsNullOrWhiteSpace(plan.mentorPersonId);
                if (free == hasMentor)
                {
                    error = free
                        ? "存档自由成长计划不应有导师。"
                        : "存档定向培养计划缺少导师。";
                    return false;
                }
                if (free)
                    continue;

                if (!PersonId.TryParse(plan.mentorPersonId, out PersonId mentorId) ||
                    mentorId == learner || !roster.TryGetValue(mentorId, out SuccessionPersonSaveEntry mentor))
                {
                    error = "存档培养计划导师不是合法的成年家族成员。";
                    return false;
                }

                // SC-023 keeps a plan whose retired former-head mentor later dies, leaves
                // the family or otherwise loses eligibility: the annual settlement is the
                // single authority and falls back to free growth. The stored identity must
                // still resolve to a known person, and the shared rule decides whether it is
                // still an eligible mentor; an ineligible-but-known plan is retained.
                bool hasRole = roles.TryGetValue(mentorId, out FamilyMemberRole mentorRole);
                bool heldHeadRole = formerHeadIds != null && formerHeadIds.Contains(mentorId.Value);
                bool stillEligible = members.Contains(mentorId) && hasRole &&
                    EducationMentorRules.IsEligible(familyId, mentorId, learner, true,
                        mentor.alive, mentor.retired, date.Year - mentor.birthYear, adulthoodAge,
                        mentorRole, heldHeadRole);
                if (EducationMentorRules.AllowsRetainedFallback(familyId))
                {
                    if (!stillEligible)
                    {
                        // A retired direct kin who never appears as the departing head of a
                        // real handoff was never an SC-023 mentor. Do not launder that into
                        // a free-growth fallback plan; only genuine former heads may retain
                        // an eligibility-lost plan.
                        if (mentor.retired && !heldHeadRole)
                        {
                            error = "存档培养计划导师是退休非前任家主，不能担任隐居导师。";
                            return false;
                        }
                        continue;
                    }
                }
                else if (!stillEligible)
                {
                    error = "存档培养计划导师已失效或未成年。";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private bool ValidateResults(CharacterDevelopmentPersonSaveEntry entry, PersonId learner,
            Dictionary<PersonId, SuccessionPersonSaveEntry> roster, out string error)
        {
            if (entry.results == null)
            {
                error = "存档人物成长结果列表为空。";
                return false;
            }

            var seen = new HashSet<int>();
            int previousAge = -1;
            foreach (EducationResultSaveEntry result in entry.results)
            {
                if (result == null)
                {
                    error = "存档人物成长结果为空。";
                    return false;
                }
                if (result.ageYear < EducationYearPlan.MinimumAgeYear ||
                    result.ageYear > EducationYearPlan.MaximumAgeYear)
                {
                    error = "存档人物成长结果年度超出 0..15。";
                    return false;
                }
                if (!seen.Add(result.ageYear) || result.ageYear <= previousAge)
                {
                    error = "存档人物成长结果年度重复或顺序不稳定。";
                    return false;
                }
                previousAge = result.ageYear;
                if (!Enum.IsDefined(typeof(EducationDirection), result.direction))
                {
                    error = "存档人物成长结果方向无效。";
                    return false;
                }
                if (result.before == null || result.after == null ||
                    !ValidGrowth(result.before) || !ValidGrowth(result.after) ||
                    string.IsNullOrWhiteSpace(result.explanation))
                {
                    error = "存档人物成长结果能力值或说明无效。";
                    return false;
                }
                bool free = result.direction == (int)EducationDirection.FreeGrowth;
                bool hasMentor = !string.IsNullOrWhiteSpace(result.mentorPersonId);
                if (free == hasMentor)
                {
                    error = free
                        ? "存档自由成长结果不应有导师。"
                        : "存档定向培养结果缺少导师。";
                    return false;
                }
                if (result.usedFreeGrowthFallback && (!free || hasMentor))
                {
                    error = "存档培养回退结果必须是无导师的自由成长。";
                    return false;
                }
                if (hasMentor &&
                    (!PersonId.TryParse(result.mentorPersonId, out PersonId mentorId) ||
                     !roster.ContainsKey(mentorId) || mentorId == learner))
                {
                    error = "存档人物成长结果导师 ID 无效。";
                    return false;
                }

                bool fallbackSettled = result.usedFreeGrowthFallback &&
                                       result.direction == (int)EducationDirection.FreeGrowth &&
                                       string.IsNullOrWhiteSpace(result.mentorPersonId);
                foreach (EducationPlanSaveEntry plan in entry.plans)
                {
                    if (plan != null && plan.ageYear == result.ageYear && !fallbackSettled &&
                        (plan.direction != result.direction ||
                         !string.Equals(plan.mentorPersonId ?? string.Empty,
                             result.mentorPersonId ?? string.Empty, StringComparison.Ordinal)))
                    {
                        error = "存档人物成长计划与同年度结果不一致。";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateValuesMatchLatestResult(CharacterDevelopmentPersonSaveEntry entry,
            out string error)
        {
            EducationResultSaveEntry latest = null;
            foreach (EducationResultSaveEntry result in entry.results)
                if (latest == null || result.ageYear > latest.ageYear)
                    latest = result;
            if (latest != null &&
                (latest.after.ninjutsu != entry.ninjutsu ||
                 latest.after.martialArts != entry.martialArts ||
                 latest.after.scholarship != entry.scholarship ||
                 latest.after.management != entry.management ||
                 latest.after.social != entry.social))
            {
                error = "存档人物成长能力与最近年度结算结果不一致。";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidGrowth(int ninjutsu, int martialArts, int scholarship, int management,
            int social) =>
            ninjutsu >= CharacterGrowthValues.Minimum && ninjutsu <= CharacterGrowthValues.Maximum &&
            martialArts >= CharacterGrowthValues.Minimum && martialArts <= CharacterGrowthValues.Maximum &&
            scholarship >= CharacterGrowthValues.Minimum && scholarship <= CharacterGrowthValues.Maximum &&
            management >= CharacterGrowthValues.Minimum && management <= CharacterGrowthValues.Maximum &&
            social >= CharacterGrowthValues.Minimum && social <= CharacterGrowthValues.Maximum;

        private static bool ValidGrowth(GrowthValuesSaveEntry values) =>
            values != null && ValidGrowth(values.ninjutsu, values.martialArts, values.scholarship,
                values.management, values.social);

        public static bool TryRestore(CharacterDevelopmentSaveData data, SuccessionState succession,
            out CharacterDevelopmentState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档缺少人物成长状态（development）。";
                return false;
            }
            error = string.Empty;

            var restored = CharacterDevelopmentState.CreateEmpty();
            if (data.persons == null)
            {
                error = "存档缺少人物成长记录（development.persons）。";
                return false;
            }

            foreach (CharacterDevelopmentPersonSaveEntry entry in data.persons)
            {
                if (entry == null ||
                    !PersonId.TryParse(entry.personId, out PersonId personId))
                {
                    error = "存档人物成长记录无效。";
                    return false;
                }
                if (!CharacterGrowthValues.TryCreate(entry.ninjutsu, entry.martialArts,
                        entry.scholarship, entry.management, entry.social,
                        out CharacterGrowthValues values, out string valuesError))
                {
                    error = string.IsNullOrEmpty(valuesError)
                        ? "存档人物成长记录无效。"
                        : valuesError;
                    return false;
                }
                if (!restored.TryAddPerson(personId, values, out error))
                    return false;
            }

            foreach (CharacterDevelopmentPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (entry.plans != null)
                {
                    foreach (EducationPlanSaveEntry plan in entry.plans)
                    {
                        if (plan == null ||
                            !EducationYearPlan.TryCreate(personId, plan.ageYear,
                                (EducationDirection)plan.direction,
                                string.IsNullOrWhiteSpace(plan.mentorPersonId)
                                    ? default
                                    : new PersonId(plan.mentorPersonId), out EducationYearPlan domainPlan,
                                out error) ||
                            !restored.TryRestorePlan(domainPlan, out error))
                        {
                            error = string.IsNullOrEmpty(error)
                                ? "存档人物成长计划无效。"
                                : error;
                            return false;
                        }
                    }
                }
            }

            foreach (CharacterDevelopmentPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (entry.results == null)
                    continue;
                foreach (EducationResultSaveEntry result in entry.results)
                {
                    if (result == null || result.before == null || result.after == null ||
                        !CharacterGrowthValues.TryCreate(result.before.ninjutsu,
                            result.before.martialArts, result.before.scholarship,
                            result.before.management, result.before.social,
                            out CharacterGrowthValues before, out error) ||
                        !CharacterGrowthValues.TryCreate(result.after.ninjutsu,
                            result.after.martialArts, result.after.scholarship,
                            result.after.management, result.after.social,
                            out CharacterGrowthValues after, out error))
                    {
                        error = string.IsNullOrEmpty(error)
                            ? "存档人物成长结果能力值无效。"
                            : error;
                        return false;
                    }

                    var domainResult = new EducationYearResult(
                        (EducationDirection)result.direction,
                        string.IsNullOrWhiteSpace(result.mentorPersonId)
                            ? default
                            : new PersonId(result.mentorPersonId),
                        before, after, result.explanation, result.usedFreeGrowthFallback);
                    if (!restored.TryRestoreSettledYear(personId, result.ageYear, domainResult, out error))
                        return false;
                }
            }

            if (!restored.TryValidate(out error))
                return false;
            state = restored;
            error = string.Empty;
            return true;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class CharacterDevelopmentPersonSaveEntry
    {
        public string personId;
        public int ninjutsu;
        public int martialArts;
        public int scholarship;
        public int management;
        public int social;
        public EducationPlanSaveEntry[] plans;
        public EducationResultSaveEntry[] results;
    }

    [Serializable]
    public sealed class EducationPlanSaveEntry
    {
        public int ageYear;
        public int direction;
        public string mentorPersonId;
    }

    [Serializable]
    public sealed class EducationResultSaveEntry
    {
        public int ageYear;
        public int direction;
        public string mentorPersonId;
        public GrowthValuesSaveEntry before;
        public GrowthValuesSaveEntry after;
        public string explanation;
        public bool usedFreeGrowthFallback;
    }

    [Serializable]
    public sealed class GrowthValuesSaveEntry
    {
        public int ninjutsu;
        public int martialArts;
        public int scholarship;
        public int management;
        public int social;
    }

    /// <summary>
    /// Plain-data v8 SC-021 research snapshot. One optional plan is represented by
    /// <see cref="researchId"/> plus <see cref="researcherPersonId"/>; empty strings
    /// mean no active project. Only stable IDs, progress, the last settled month and
    /// sorted unique family technique IDs are stored. There is no person-mastery field:
    /// family knowledge must not make an individual able to use a technique.
    /// </summary>
    [Serializable]
    public sealed class FamilyResearchSaveData
    {
        public string researchId;
        public string researcherPersonId;
        public int progress;
        public int lastSettledMonthIndex;
        public string[] knownTechniqueIds;

        public static FamilyResearchSaveData CreateInitial(GameDate date) =>
            new FamilyResearchSaveData
            {
                researchId = string.Empty,
                researcherPersonId = string.Empty,
                progress = 0,
                lastSettledMonthIndex = IndustryCalendar.MonthIndex(date) - 1,
                knownTechniqueIds = new string[0]
            };

        public static FamilyResearchSaveData Capture(FamilyResearchState state)
        {
            if (state == null)
                return null;

            string[] known = new string[state.KnownTechniqueIds.Count];
            for (int i = 0; i < known.Length; i++)
                known[i] = state.KnownTechniqueIds[i];

            return new FamilyResearchSaveData
            {
                researchId = state.HasPlan ? state.ResearchId : string.Empty,
                researcherPersonId = state.HasPlan ? state.ResearcherId.Value : string.Empty,
                progress = state.HasPlan ? state.Progress : 0,
                lastSettledMonthIndex = state.LastSettledMonthIndex,
                knownTechniqueIds = known
            };
        }

        public FamilyResearchSaveData Clone() => new FamilyResearchSaveData
        {
            researchId = researchId,
            researcherPersonId = researcherPersonId,
            progress = progress,
            lastSettledMonthIndex = lastSettledMonthIndex,
            knownTechniqueIds = knownTechniqueIds != null
                ? (string[])knownTechniqueIds.Clone()
                : null
        };

        public SaveOperationResult ValidateForSave(GameDate date, SuccessionSaveData succession)
        {
            if (lastSettledMonthIndex != IndustryCalendar.MonthIndex(date) - 1)
                return Invalid("存档研究已结算月份与日期不一致。");
            if (knownTechniqueIds == null)
                return Invalid("存档缺少家族技艺知识列表（knownTechniqueIds）。");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            string previous = null;
            foreach (string techniqueId in knownTechniqueIds)
            {
                if (string.IsNullOrWhiteSpace(techniqueId) ||
                    !FamilyResearchCatalog.IsKnownTechniqueId(techniqueId))
                    return Invalid("存档包含未知家族技艺 ID：" + (techniqueId ?? "(null)") + "。");
                if (!seen.Add(techniqueId))
                    return Invalid("存档家族技艺重复：" + techniqueId + "。");
                if (previous != null && string.CompareOrdinal(previous, techniqueId) > 0)
                    return Invalid("存档家族技艺顺序不稳定。");
                previous = techniqueId;
            }

            bool hasResearch = !string.IsNullOrWhiteSpace(researchId);
            bool hasResearcher = !string.IsNullOrWhiteSpace(researcherPersonId);
            if (!hasResearch)
            {
                if (hasResearcher || progress != 0)
                    return Invalid("存档研究计划缺少项目 ID，但仍有研究者或进度。");
                return SaveOperationResult.Success();
            }

            if (!FamilyResearchCatalog.TryGet(researchId, out FamilyResearchDefinition definition))
                return Invalid("存档研究项目 ID 未知：" + researchId + "。");
            if (!hasResearcher)
                return Invalid("存档研究计划缺少研究者 ID。");
            if (progress < 0 || progress >= definition.RequiredProgress)
                return Invalid("存档研究进度必须在 0.." + (definition.RequiredProgress - 1) + " 之间。");
            if (seen.Contains(definition.TechniqueId))
                return Invalid("存档研究计划与已记录家族知识冲突。");
            if (succession == null || succession.persons == null || succession.members == null)
                return Invalid("存档研究缺少继承人物名册。");

            if (!PersonId.TryParse(researcherPersonId, out PersonId researcher))
                return Invalid("存档研究者 ID 无效。");
            bool knownPerson = false;
            foreach (SuccessionPersonSaveEntry person in succession.persons)
            {
                if (person == null || !PersonId.TryParse(person.personId, out PersonId personId))
                    continue;
                if (personId == researcher)
                {
                    knownPerson = true;
                    break;
                }
            }
            if (!knownPerson)
                return Invalid("存档研究者不是已登记人物。");

            // Do not require current family membership here. A researcher can die,
            // retire, leave the family or take an industry post after the plan starts;
            // the monthly settlement then pauses without charging or progressing. The
            // paused plan must still round-trip through save/load so the player can
            // inspect or replace the researcher later. Only an unregistered person ID
            // is rejected.

            return SaveOperationResult.Success();
        }

        public static bool TryRestore(FamilyResearchSaveData data, GameDate date,
            SuccessionSaveData succession, out FamilyResearchState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档缺少家族研究状态（research）。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(date, succession);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            return FamilyResearchState.TryRestore(data.researchId, data.researcherPersonId,
                data.progress, data.lastSettledMonthIndex, data.knownTechniqueIds,
                out state, out error);
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    /// <summary>
    /// Plain-data v9 SC-024 snapshot. It stores the persistent family creed by stable
    /// enum-name ID and one ambition row per head. Rows are ordered by ordinal head ID
    /// and each row keeps the selection-time technique snapshot so a known technique is
    /// never counted as a new research result after a load. Progress, target and the
    /// completion flag are stored together and must agree on validation.
    /// </summary>
    [Serializable]
    public sealed class FamilyDoctrineSaveData
    {
        public string creedId;
        public FamilyAmbitionSaveEntry[] ambitions;

        public static FamilyDoctrineSaveData CreateDefault() =>
            new FamilyDoctrineSaveData
            {
                creedId = nameof(FamilyCreedKind.RighteousnessFirst),
                ambitions = new FamilyAmbitionSaveEntry[0]
            };

        public static FamilyDoctrineSaveData Capture(FamilyDoctrineState state)
        {
            if (state == null)
                return CreateDefault();

            IReadOnlyList<PersonId> headIds = state.HeadIds;
            var entries = new List<FamilyAmbitionSaveEntry>(headIds.Count);
            for (int i = 0; i < headIds.Count; i++)
            {
                PersonId headId = headIds[i];
                if (!state.TryGetAmbition(headId, out FamilyAmbitionRecord record) || record == null)
                    continue;

                var known = new string[record.KnownTechniqueIds.Count];
                for (int k = 0; k < known.Length; k++)
                    known[k] = record.KnownTechniqueIds[k];

                entries.Add(new FamilyAmbitionSaveEntry
                {
                    headPersonId = record.HeadId.Value,
                    ambitionId = record.Kind.ToString(),
                    progress = record.Progress,
                    target = record.Target,
                    isComplete = record.IsComplete,
                    knownTechniqueIds = known
                });
            }

            return new FamilyDoctrineSaveData
            {
                creedId = state.Creed.ToString(),
                ambitions = entries.ToArray()
            };
        }

        public FamilyDoctrineSaveData Clone() => new FamilyDoctrineSaveData
        {
            creedId = creedId,
            ambitions = CloneAmbitions(ambitions)
        };

        /// <summary>Structural and cross-reference validation; never throws.</summary>
        public SaveOperationResult ValidateForSave(SuccessionSaveData succession,
            IReadOnlyCollection<string> savedKnownTechniqueIds)
        {
            if (!TryParseCreed(creedId, out FamilyCreedKind _))
                return Invalid("存档家训 ID 未知：" + (creedId ?? "(null)") + "。");
            if (ambitions == null)
                return Invalid("存档缺少家主抱负列表（familyDoctrine.ambitions）。");
            if (succession == null || succession.persons == null)
                return Invalid("存档家训与抱负缺少继承人物名册。");
            if (savedKnownTechniqueIds == null)
                return Invalid("存档缺少家族研究知识，无法验证抱负技艺基线。");

            var persons = new HashSet<string>(StringComparer.Ordinal);
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && !string.IsNullOrWhiteSpace(person.personId))
                    persons.Add(person.personId);
            var knownTechniques = new HashSet<string>(savedKnownTechniqueIds, StringComparer.Ordinal);

            var seenHeads = new HashSet<PersonId>();
            string previousHead = null;
            foreach (FamilyAmbitionSaveEntry entry in ambitions)
            {
                if (entry == null)
                    return Invalid("存档家主抱负记录为空。");

                if (!PersonId.TryParse(entry.headPersonId, out PersonId headId) || headId.IsEmpty)
                    return Invalid("存档家主抱负记录的人物 ID 无效：" +
                                   (entry.headPersonId ?? "(null)") + "。");
                if (!persons.Contains(headId.Value))
                    return Invalid("存档家主抱负记录引用了未知人物：" + headId.Value + "。");
                if (!seenHeads.Add(headId))
                    return Invalid("存档家主抱负记录重复：" + headId.Value + "。");
                if (previousHead != null && string.CompareOrdinal(previousHead, headId.Value) > 0)
                    return Invalid("存档家主抱负记录顺序不稳定。");
                previousHead = headId.Value;

                if (!TryParseAmbition(entry.ambitionId, out FamilyAmbitionKind kind))
                    return Invalid("存档抱负 ID 未知：" + (entry.ambitionId ?? "(null)") + "。");
                int expectedTarget = TargetFor(kind);
                if (entry.target != expectedTarget)
                    return Invalid("存档抱负目标无效：" + headId.Value + "=" + entry.target +
                                   "（应为 " + expectedTarget + "）。");
                if (entry.progress < 0 || entry.progress > entry.target)
                    return Invalid("存档抱负进度必须在 0.." + entry.target + " 之间：" +
                                   headId.Value + "=" + entry.progress + "。");
                if (entry.isComplete != (entry.progress >= entry.target))
                    return Invalid("存档抱负完成状态与进度不一致：" + headId.Value + "。");

                SaveOperationResult baseline = ValidateTechniqueBaseline(entry.knownTechniqueIds,
                    headId.Value);
                if (!baseline.Succeeded)
                    return baseline;
                if (kind == FamilyAmbitionKind.PassDownTechnique)
                {
                    foreach (string techniqueId in entry.knownTechniqueIds)
                        if (!knownTechniques.Contains(techniqueId))
                            return Invalid("存档抱负技艺基线不在当前家族研究知识中：" +
                                           techniqueId + "。");
                }
            }

            return SaveOperationResult.Success();
        }

        /// <summary>Rebuilds a validated runtime state; the caller must not assign it on failure.</summary>
        public static bool TryRestore(FamilyDoctrineSaveData data, SuccessionSaveData succession,
            IReadOnlyCollection<string> savedKnownTechniqueIds, out FamilyDoctrineState state,
            out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档缺少家训与抱负状态（familyDoctrine）。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(succession, savedKnownTechniqueIds);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            TryParseCreed(data.creedId, out FamilyCreedKind creed);
            var records = new List<FamilyAmbitionRecord>(data.ambitions.Length);
            foreach (FamilyAmbitionSaveEntry entry in data.ambitions)
            {
                TryParseAmbition(entry.ambitionId, out FamilyAmbitionKind kind);
                var known = new string[entry.knownTechniqueIds.Length];
                Array.Copy(entry.knownTechniqueIds, known, known.Length);
                records.Add(new FamilyAmbitionRecord(new PersonId(entry.headPersonId), kind,
                    entry.progress, entry.target, Array.AsReadOnly(known)));
            }

            state = FamilyDoctrineState.CreateRestored(creed, records);
            error = string.Empty;
            return true;
        }

        private static SaveOperationResult ValidateTechniqueBaseline(string[] techniqueIds,
            string headPersonId)
        {
            if (techniqueIds == null)
                return Invalid("存档抱负缺少已知技艺基线（knownTechniqueIds）：" +
                               headPersonId + "。");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            string previous = null;
            foreach (string techniqueId in techniqueIds)
            {
                if (string.IsNullOrWhiteSpace(techniqueId) ||
                    !FamilyResearchCatalog.IsKnownTechniqueId(techniqueId))
                    return Invalid("存档抱负包含未知技艺 ID：" +
                                   (techniqueId ?? "(null)") + "。");
                if (!seen.Add(techniqueId))
                    return Invalid("存档抱负技艺基线重复：" + techniqueId + "。");
                if (previous != null && string.CompareOrdinal(previous, techniqueId) > 0)
                    return Invalid("存档抱负技艺基线顺序不稳定。");
                previous = techniqueId;
            }

            return SaveOperationResult.Success();
        }

        private static FamilyAmbitionSaveEntry[] CloneAmbitions(FamilyAmbitionSaveEntry[] source)
        {
            if (source == null)
                return null;

            var clone = new FamilyAmbitionSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                FamilyAmbitionSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new FamilyAmbitionSaveEntry
                {
                    headPersonId = entry.headPersonId,
                    ambitionId = entry.ambitionId,
                    progress = entry.progress,
                    target = entry.target,
                    isComplete = entry.isComplete,
                    knownTechniqueIds = entry.knownTechniqueIds != null
                        ? (string[])entry.knownTechniqueIds.Clone()
                        : null
                };
            }
            return clone;
        }

        private static bool TryParseCreed(string value, out FamilyCreedKind creed)
        {
            if (!string.IsNullOrEmpty(value) &&
                Enum.TryParse(value, false, out creed) &&
                Enum.IsDefined(typeof(FamilyCreedKind), creed) &&
                string.Equals(Enum.GetName(typeof(FamilyCreedKind), creed), value,
                    StringComparison.Ordinal))
                return true;

            creed = default;
            return false;
        }

        private static bool TryParseAmbition(string value, out FamilyAmbitionKind kind)
        {
            if (!string.IsNullOrEmpty(value) &&
                Enum.TryParse(value, false, out kind) &&
                Enum.IsDefined(typeof(FamilyAmbitionKind), kind) &&
                string.Equals(Enum.GetName(typeof(FamilyAmbitionKind), kind), value,
                    StringComparison.Ordinal))
                return true;

            kind = default;
            return false;
        }

        private static int TargetFor(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold:
                    return FamilyDoctrineState.DevelopHouseholdTarget;
                case FamilyAmbitionKind.PassDownTechnique:
                    return FamilyDoctrineState.PassDownTechniqueTarget;
                case FamilyAmbitionKind.EducateHeir:
                    return FamilyDoctrineState.EducateHeirTarget;
                default:
                    return 0;
            }
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class FamilyAmbitionSaveEntry
    {
        public string headPersonId;
        public string ambitionId;
        public int progress;
        public int target;
        public bool isComplete;
        public string[] knownTechniqueIds;
    }

    /// <summary>
    /// v12 durable autonomy state: needs and up to two action memories for the two
    /// approved pilots only. Recent evaluation and decision records are session-only.
    /// </summary>
    [Serializable]
    public sealed class NpcAutonomyStateSaveData
    {
        public NpcAutonomyPersonSaveEntry[] persons;

        public static NpcAutonomyStateSaveData CreateEmpty() =>
            new NpcAutonomyStateSaveData { persons = new NpcAutonomyPersonSaveEntry[0] };

        /// <summary>
        /// Opening or migration state: approved initial needs for living pilot rows in
        /// the saved roster. Non-M2 rosters receive an empty block.
        /// </summary>
        public static NpcAutonomyStateSaveData CreateInitial(SuccessionSaveData succession)
        {
            if (succession == null || succession.persons == null || succession.members == null)
                return CreateEmpty();

            var livingMembers = new HashSet<PersonId>();
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && person.alive &&
                    PersonId.TryParse(person.personId, out PersonId personId))
                    livingMembers.Add(personId);
            var members = new HashSet<PersonId>();
            foreach (SuccessionMemberSaveEntry member in succession.members)
                if (member != null && PersonId.TryParse(member.personId, out PersonId memberId))
                    members.Add(memberId);
            livingMembers.IntersectWith(members);

            var ids = new List<PersonId>();
            foreach (NpcAutonomyProfile profile in NpcAutonomyRules.Profiles)
                if (livingMembers.Contains(profile.PersonId))
                    ids.Add(profile.PersonId);
            ids.Sort((left, right) =>
                string.Compare(left.Value, right.Value, StringComparison.Ordinal));

            var entries = new NpcAutonomyPersonSaveEntry[ids.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                NpcNeedValues needs = NpcAutonomyRules.InitialNeedsFor(ids[i]);
                entries[i] = new NpcAutonomyPersonSaveEntry
                {
                    personId = ids[i].Value,
                    rest = needs.Rest,
                    learning = needs.Learning,
                    duty = needs.Duty,
                    memories = new NpcActionMemorySaveEntry[0]
                };
            }
            return new NpcAutonomyStateSaveData { persons = entries };
        }

        public static NpcAutonomyStateSaveData Capture(NpcAutonomyState state)
        {
            if (state == null)
                return CreateEmpty();
            IReadOnlyList<NpcAutonomyPersonState> people = state.Persons;
            var entries = new NpcAutonomyPersonSaveEntry[people.Count];
            for (int i = 0; i < people.Count; i++)
            {
                NpcAutonomyPersonState person = people[i];
                IReadOnlyList<NpcActionMemory> memories = person.Memories;
                var memoryEntries = new NpcActionMemorySaveEntry[memories.Count];
                for (int m = 0; m < memoryEntries.Length; m++)
                {
                    NpcActionMemory memory = memories[m];
                    memoryEntries[m] = new NpcActionMemorySaveEntry
                    {
                        personId = memory.PersonId.Value,
                        kind = (int)memory.Kind,
                        action = (int)memory.Action,
                        year = memory.Date.Year,
                        month = memory.Date.Month,
                        day = memory.Date.Day,
                        reason = memory.Reason
                    };
                }

                entries[i] = new NpcAutonomyPersonSaveEntry
                {
                    personId = person.PersonId.Value,
                    rest = person.Needs.Rest,
                    learning = person.Needs.Learning,
                    duty = person.Needs.Duty,
                    memories = memoryEntries
                };
            }
            return new NpcAutonomyStateSaveData { persons = entries };
        }

        public NpcAutonomyStateSaveData Clone()
        {
            var clone = new NpcAutonomyStateSaveData
            {
                persons = new NpcAutonomyPersonSaveEntry[persons != null ? persons.Length : 0]
            };
            for (int i = 0; i < clone.persons.Length; i++)
            {
                NpcAutonomyPersonSaveEntry entry = persons[i];
                if (entry == null)
                {
                    clone.persons[i] = null;
                    continue;
                }
                var memories = new NpcActionMemorySaveEntry[
                    entry.memories != null ? entry.memories.Length : 0];
                for (int m = 0; m < memories.Length; m++)
                {
                    NpcActionMemorySaveEntry memory = entry.memories[m];
                    memories[m] = memory == null ? null : new NpcActionMemorySaveEntry
                    {
                        personId = memory.personId,
                        kind = memory.kind,
                        action = memory.action,
                        year = memory.year,
                        month = memory.month,
                        day = memory.day,
                        reason = memory.reason
                    };
                }
                clone.persons[i] = new NpcAutonomyPersonSaveEntry
                {
                    personId = entry.personId,
                    rest = entry.rest,
                    learning = entry.learning,
                    duty = entry.duty,
                    memories = memories
                };
            }
            return clone;
        }

        /// <summary>
        /// v11 migration support: keep every saved need row and add an empty memory list
        /// to each. No memory is inferred from old logs or history.
        /// </summary>
        public static NpcAutonomyStateSaveData CreateMigratedV11(NpcAutonomyStateSaveData source)
        {
            if (source == null)
                return CreateEmpty();
            var entries = new NpcAutonomyPersonSaveEntry[
                source.persons != null ? source.persons.Length : 0];
            for (int i = 0; i < entries.Length; i++)
            {
                NpcAutonomyPersonSaveEntry person = source.persons[i];
                entries[i] = person == null
                    ? null
                    : new NpcAutonomyPersonSaveEntry
                    {
                        personId = person.personId,
                        rest = person.rest,
                        learning = person.learning,
                        duty = person.duty,
                        memories = new NpcActionMemorySaveEntry[0]
                    };
            }
            return new NpcAutonomyStateSaveData { persons = entries };
        }

        public SaveOperationResult ValidateForSave(SuccessionSaveData succession,
            GameDate currentDate)
        {
            if (persons == null)
                return Invalid("存档缺少 NPC 自主状态（npcAutonomy.persons）。");
            if (succession == null || succession.persons == null || succession.members == null)
                return Invalid("存档 NPC 自主状态缺少继承人物名册。");

            var livingMembers = new HashSet<PersonId>();
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && person.alive &&
                    PersonId.TryParse(person.personId, out PersonId personId))
                    livingMembers.Add(personId);
            var members = new HashSet<PersonId>();
            foreach (SuccessionMemberSaveEntry member in succession.members)
                if (member != null && PersonId.TryParse(member.personId, out PersonId memberId))
                    members.Add(memberId);
            livingMembers.IntersectWith(members);

            var seen = new HashSet<PersonId>();
            string previous = null;
            foreach (NpcAutonomyPersonSaveEntry entry in persons)
            {
                if (entry == null)
                    return Invalid("存档 NPC 自主人物记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId) ||
                    !NpcAutonomyRules.IsPilot(personId))
                    return Invalid("存档 NPC 自主人物 ID 不是受支持的试点人物。");
                if (!livingMembers.Contains(personId))
                    return Invalid("存档 NPC 自主人物不在当前存活家臣名册中。");
                if (!seen.Add(personId))
                    return Invalid("存档 NPC 自主人物重复。");
                if (previous != null && string.CompareOrdinal(previous, entry.personId) > 0)
                    return Invalid("存档 NPC 自主人物顺序不稳定。");
                previous = entry.personId;

                if (entry.rest < NpcNeedValues.Minimum || entry.rest > NpcNeedValues.Maximum ||
                    entry.learning < NpcNeedValues.Minimum ||
                    entry.learning > NpcNeedValues.Maximum ||
                    entry.duty < NpcNeedValues.Minimum || entry.duty > NpcNeedValues.Maximum)
                    return Invalid("存档 NPC 需求值必须在 0..100 之间。");

                SaveOperationResult memories = ValidateMemories(entry, personId, currentDate);
                if (!memories.Succeeded)
                    return memories;
            }

            foreach (NpcAutonomyProfile profile in NpcAutonomyRules.Profiles)
                if (livingMembers.Contains(profile.PersonId) && !seen.Contains(profile.PersonId))
                    return Invalid("存档缺少 NPC 自主人物记录：" + profile.PersonId.Value + "。");
            return SaveOperationResult.Success();
        }

        private static SaveOperationResult ValidateMemories(NpcAutonomyPersonSaveEntry entry,
            PersonId owner, GameDate currentDate)
        {
            if (entry.memories == null)
                return Invalid("存档缺少 NPC 行动记忆列表（npcAutonomy.persons[].memories）。");
            if (entry.memories.Length > 2)
                return Invalid("存档 NPC 行动记忆最多两条。");

            var seenKinds = new HashSet<NpcActionMemoryKind>();
            int previousKind = -1;
            foreach (NpcActionMemorySaveEntry memory in entry.memories)
            {
                if (memory == null)
                    return Invalid("存档 NPC 行动记忆记录为空。");
                if (!PersonId.TryParse(memory.personId, out PersonId memoryPerson) ||
                    memoryPerson != owner)
                    return Invalid("存档 NPC 行动记忆人物归属不一致。");
                if (!Enum.IsDefined(typeof(NpcActionMemoryKind), memory.kind))
                    return Invalid("存档 NPC 行动记忆类型无效。");
                if (!Enum.IsDefined(typeof(DailyActionKind), memory.action))
                    return Invalid("存档 NPC 行动记忆行动类型无效。");
                if (memory.reason == null)
                    return Invalid("存档 NPC 行动记忆缺少原因字段（reason）。");
                if (!GameDate.IsValidDate(memory.year, memory.month, memory.day))
                    return Invalid("存档 NPC 行动记忆日期无效。");
                var date = new GameDate(memory.year, memory.month, memory.day);
                if (currentDate.IsValid && date > currentDate)
                    return Invalid("存档 NPC 行动记忆日期不能晚于当前日期。");
                var kind = (NpcActionMemoryKind)memory.kind;
                if (!seenKinds.Add(kind))
                    return Invalid("存档 NPC 行动记忆同一类型重复。");
                if ((int)kind <= previousKind)
                    return Invalid("存档 NPC 行动记忆顺序不稳定。");
                previousKind = (int)kind;
                if (kind == NpcActionMemoryKind.Completed)
                {
                    if (!string.IsNullOrWhiteSpace(memory.reason))
                        return Invalid("存档完成记忆不应保存中断原因。");
                }
                else if (string.IsNullOrWhiteSpace(memory.reason))
                {
                    return Invalid("存档中断记忆缺少原因。");
                }
            }
            return SaveOperationResult.Success();
        }

        public static bool TryRestore(NpcAutonomyStateSaveData data,
            SuccessionSaveData succession, GameDate currentDate,
            out NpcAutonomyState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档缺少 NPC 自主状态（npcAutonomy）。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(succession, currentDate);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            var restored = NpcAutonomyState.CreateEmpty();
            foreach (NpcAutonomyPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                var needs = new NpcNeedValues(entry.rest, entry.learning, entry.duty);
                if (!restored.TryAddRestoredPerson(personId, needs))
                {
                    error = "存档 NPC 自主人物无法恢复：" + entry.personId + "。";
                    return false;
                }
            }

            foreach (NpcAutonomyPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                foreach (NpcActionMemorySaveEntry memory in entry.memories)
                {
                    PersonId.TryParse(memory.personId, out PersonId memoryPerson);
                    var date = new GameDate(memory.year, memory.month, memory.day);
                    if (!restored.TryRestoreMemory(memoryPerson,
                            (NpcActionMemoryKind)memory.kind,
                            (DailyActionKind)memory.action, date, memory.reason))
                    {
                        error = "存档 NPC 行动记忆无法恢复：" + entry.personId + "。";
                        return false;
                    }
                }
            }
            state = restored;
            error = string.Empty;
            return true;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class NpcAutonomyPersonSaveEntry
    {
        public string personId;
        public int rest;
        public int learning;
        public int duty;
        public NpcActionMemorySaveEntry[] memories;
    }

    [Serializable]
    public sealed class NpcActionMemorySaveEntry
    {
        public string personId;
        // Missing JSON enums must fail validation instead of becoming Completed/Rest.
        public int kind = -1;
        public int action = -1;
        public int year;
        public int month;
        public int day;
        public string reason;
    }

    /// <summary>
    /// v10 M02 daily-action snapshot. Every roster person has exactly one sorted row;
    /// a row owns at most one in-flight duration-bound action, so mutual exclusion is
    /// structural. A travelling row deliberately has no location: the task's origin
    /// and destination are route metadata, not two simultaneous positions.
    /// </summary>
    [Serializable]
    public sealed class DailyActionStateSaveData
    {
        public DailyActionPersonSaveEntry[] persons;

        public static DailyActionStateSaveData CreateEmpty() =>
            new DailyActionStateSaveData { persons = new DailyActionPersonSaveEntry[0] };

        /// <summary>Opening or migration state: sorted rows at the saved location, no actions.</summary>
        public static DailyActionStateSaveData CreateInitial(GameDate date,
            SuccessionSaveData succession, string locationId)
        {
            if (succession == null || succession.persons == null)
                return CreateEmpty();

            LocationId.TryParse(locationId, out LocationId location);
            var ids = new List<string>();
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && PersonId.TryParse(person.personId, out PersonId personId))
                    ids.Add(personId.Value);
            ids.Sort(StringComparer.Ordinal);

            var entries = new DailyActionPersonSaveEntry[ids.Count];
            for (int i = 0; i < entries.Length; i++)
                entries[i] = new DailyActionPersonSaveEntry
                {
                    personId = ids[i],
                    hasLocation = location.IsValid,
                    locationId = location.IsValid ? location.Value : string.Empty
                };
            return new DailyActionStateSaveData { persons = entries };
        }

        public static DailyActionStateSaveData Capture(DailyActionState state)
        {
            if (state == null)
                return CreateEmpty();

            IReadOnlyList<DailyActionPersonState> persons = state.Persons;
            var entries = new DailyActionPersonSaveEntry[persons.Count];
            for (int i = 0; i < persons.Count; i++)
            {
                DailyActionPersonState person = persons[i];
                DailyActionTask task = person.ActiveAction;
                var entry = new DailyActionPersonSaveEntry
                {
                    personId = person.PersonId.Value,
                    hasLocation = person.HasLocation,
                    locationId = person.HasLocation ? person.Location.Value : string.Empty,
                    hasAction = task != null
                };
                if (task != null)
                {
                    entry.actionKind = (int)task.Kind;
                    entry.actionDurationDays = task.DurationDays;
                    entry.actionElapsedDays = task.ElapsedDays;
                    entry.actionStartYear = task.StartDate.Year;
                    entry.actionStartMonth = task.StartDate.Month;
                    entry.actionStartDay = task.StartDate.Day;
                    entry.travelOriginId = task.IsTravel ? task.Origin.Value : string.Empty;
                    entry.travelDestinationId = task.IsTravel ? task.Destination.Value : string.Empty;
                    entry.actionReason = task.Reason ?? string.Empty;
                }
                entries[i] = entry;
            }
            return new DailyActionStateSaveData { persons = entries };
        }

        public DailyActionStateSaveData Clone()
        {
            var clone = new DailyActionStateSaveData
            {
                persons = new DailyActionPersonSaveEntry[persons != null ? persons.Length : 0]
            };
            for (int i = 0; i < clone.persons.Length; i++)
            {
                DailyActionPersonSaveEntry entry = persons[i];
                clone.persons[i] = entry == null ? null : new DailyActionPersonSaveEntry
                {
                    personId = entry.personId,
                    hasLocation = entry.hasLocation,
                    locationId = entry.locationId,
                    hasAction = entry.hasAction,
                    actionKind = entry.actionKind,
                    actionDurationDays = entry.actionDurationDays,
                    actionElapsedDays = entry.actionElapsedDays,
                    actionStartYear = entry.actionStartYear,
                    actionStartMonth = entry.actionStartMonth,
                    actionStartDay = entry.actionStartDay,
                    travelOriginId = entry.travelOriginId,
                    travelDestinationId = entry.travelDestinationId,
                    actionReason = entry.actionReason
                };
            }
            return clone;
        }

        public SaveOperationResult ValidateForSave(GameDate date, SuccessionSaveData succession)
        {
            if (persons == null)
                return Invalid("存档缺少每日行动状态（dailyActions.persons）。");
            if (succession == null || succession.persons == null || succession.members == null)
                return Invalid("存档每日行动缺少继承人物名册。");

            var roster = new Dictionary<PersonId, SuccessionPersonSaveEntry>();
            foreach (SuccessionPersonSaveEntry person in succession.persons)
                if (person != null && PersonId.TryParse(person.personId, out PersonId personId))
                    roster[personId] = person;
            var members = new HashSet<PersonId>();
            foreach (SuccessionMemberSaveEntry member in succession.members)
                if (member != null && PersonId.TryParse(member.personId, out PersonId memberId))
                    members.Add(memberId);

            if (persons.Length != roster.Count)
                return Invalid("存档每日行动记录必须覆盖名册中的每个人物。");

            var seen = new HashSet<PersonId>();
            string previous = null;
            foreach (DailyActionPersonSaveEntry entry in persons)
            {
                if (entry == null)
                    return Invalid("存档每日行动记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId) ||
                    !roster.TryGetValue(personId, out SuccessionPersonSaveEntry rosterEntry))
                    return Invalid("存档每日行动引用了未知人物。");
                if (!seen.Add(personId))
                    return Invalid("存档每日行动人物重复。");
                if (previous != null && string.CompareOrdinal(previous, entry.personId) > 0)
                    return Invalid("存档每日行动顺序不稳定。");
                previous = entry.personId;

                if (entry.hasLocation)
                {
                    if (!LocationId.TryParse(entry.locationId, out _))
                        return Invalid("存档每日行动地点无效：" + (entry.locationId ?? "(null)") + "。");
                }
                else if (!string.IsNullOrEmpty(entry.locationId))
                {
                    return Invalid("存档每日行动在无地点时仍带有地点 ID。");
                }

                if (!entry.hasAction)
                {
                    if (entry.actionDurationDays != 0 || entry.actionElapsedDays != 0 ||
                        entry.actionStartYear != 0 || entry.actionStartMonth != 0 ||
                        entry.actionStartDay != 0 || !string.IsNullOrEmpty(entry.travelOriginId) ||
                        !string.IsNullOrEmpty(entry.travelDestinationId) ||
                        !string.IsNullOrEmpty(entry.actionReason))
                        return Invalid("存档每日行动没有进行中的行动，却仍有行动数据。");
                    continue;
                }

                if (!Enum.IsDefined(typeof(DailyActionKind), entry.actionKind))
                    return Invalid("存档每日行动类型无效。");
                if (entry.actionDurationDays <= 0 || entry.actionElapsedDays < 0 ||
                    entry.actionElapsedDays >= entry.actionDurationDays)
                    return Invalid("存档每日行动进度必须在 0..持续时间-1 之间。");
                if (!GameDate.IsValidDate(entry.actionStartYear, entry.actionStartMonth,
                        entry.actionStartDay) ||
                    new GameDate(entry.actionStartYear, entry.actionStartMonth,
                        entry.actionStartDay) > date)
                    return Invalid("存档每日行动开始日期无效。");
                if (!rosterEntry.alive || !members.Contains(personId))
                    return Invalid("存档每日行动人物已死亡或不再属于家族。");

                var kind = (DailyActionKind)entry.actionKind;
                if (kind == DailyActionKind.Travel)
                {
                    if (entry.hasLocation)
                        return Invalid("存档旅行中的人物不能同时停在起点或终点。");
                    if (!LocationId.TryParse(entry.travelOriginId, out LocationId origin) ||
                        !LocationId.TryParse(entry.travelDestinationId, out LocationId destination) ||
                        origin == destination ||
                        !WorldMapRules.TryGetTravelDays(origin, destination, out int routeDays) ||
                        routeDays != entry.actionDurationDays)
                        return Invalid("存档旅行行动不符合世界地图直达路线规则。");
                }
                else
                {
                    if (!entry.hasLocation)
                        return Invalid("存档非旅行行动缺少人物地点。");
                    if (!string.IsNullOrEmpty(entry.travelOriginId) ||
                        !string.IsNullOrEmpty(entry.travelDestinationId))
                        return Invalid("存档非旅行行动不应带有旅行端点。");
                }
            }

            foreach (PersonId rosterId in roster.Keys)
                if (!seen.Contains(rosterId))
                    return Invalid("存档缺少每日行动人物记录：" + rosterId.Value + "。");
            return SaveOperationResult.Success();
        }

        public static bool TryRestore(DailyActionStateSaveData data, SuccessionSaveData succession,
            GameDate date, out DailyActionState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档缺少每日行动状态（dailyActions）。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(date, succession);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            var restored = DailyActionState.CreateEmpty();
            foreach (DailyActionPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                LocationId.TryParse(entry.locationId, out LocationId location);
                if (!restored.TryAddPersonForRestore(personId, entry.hasLocation, location, out error))
                    return false;
            }

            foreach (DailyActionPersonSaveEntry entry in data.persons)
            {
                if (!entry.hasAction)
                    continue;
                PersonId.TryParse(entry.personId, out PersonId personId);
                var startDate = new GameDate(entry.actionStartYear, entry.actionStartMonth,
                    entry.actionStartDay);
                bool travel = (DailyActionKind)entry.actionKind == DailyActionKind.Travel;
                LocationId.TryParse(travel ? entry.travelOriginId : entry.locationId,
                    out LocationId origin);
                LocationId.TryParse(entry.travelDestinationId, out LocationId destination);
                if (!restored.TryRestoreActive(personId, (DailyActionKind)entry.actionKind,
                        entry.actionDurationDays, entry.actionElapsedDays, startDate, origin,
                        destination, entry.actionReason, out error))
                    return false;
            }

            state = restored;
            error = string.Empty;
            return true;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class DailyActionPersonSaveEntry
    {
        public string personId;
        public bool hasLocation;
        public string locationId;
        public bool hasAction;
        public int actionKind;
        public int actionDurationDays;
        public int actionElapsedDays;
        public int actionStartYear;
        public int actionStartMonth;
        public int actionStartDay;
        public string travelOriginId;
        public string travelDestinationId;
        public string actionReason;
    }

    /// <summary>Stable phases in which a save is allowed.</summary>
    public enum StableGamePhase
    {
        Management = 0,
        AwaitingEventChoice = 1
    }

    /// <summary>Version-neutral persistence DTO for deterministic event runtime state.</summary>
    [Serializable]
    public sealed class EventRuntimeSaveData
    {
        public int phase;
        public string[] pendingEventIds;
        public EventCooldownSaveEntry[] cooldowns;
        public string[] onceEventIds;
        public EventHistorySaveEntry[] history;
        public string[] committedResultIds;

        public EventRuntimeSaveData()
        {
            phase = (int)StableGamePhase.Management;
            pendingEventIds = new string[0];
            cooldowns = new EventCooldownSaveEntry[0];
            onceEventIds = new string[0];
            history = new EventHistorySaveEntry[0];
            committedResultIds = new string[0];
        }
    }

    [Serializable]
    public sealed class EventCooldownSaveEntry
    {
        public string eventId;
        public int availableAtMonth;
    }

    [Serializable]
    public sealed class EventHistorySaveEntry
    {
        public string eventId;
        public string optionId;
        public string resultId;
        public int year;
        public int month;
        public string summary;
    }

    /// <summary>
    /// Version-neutral SC-010 succession snapshot. It stores only stable IDs and
    /// plain data; the live SC-003 types are rebuilt by <see cref="TryRestore"/>.
    /// </summary>
    [Serializable]
    public sealed class SuccessionSaveData
    {
        public int phase;
        public string familyId;
        public string familyName;
        public string headPersonId;
        public string designatedHeirId;
        public string pendingSuccessorId;
        public string pendingRegentId;
        public int adulthoodAge;
        public int successionCount;
        public string completedResultId;
        public bool crisisAdoptionUsed;
        public bool regencyCompleted;
        public SuccessionPersonSaveEntry[] persons;
        public SuccessionMemberSaveEntry[] members;
        public SuccessionKinshipSaveEntry[] kinship;
        public FamilyPregnancySaveEntry[] pregnancies;

        /// <summary>Default roster used by a brand-new session and by v1/v2 migration.</summary>
        public static SuccessionSaveData CreateDefault() => Capture(SuccessionSample.CreateStandard());

        public static SuccessionSaveData Capture(SuccessionState state)
        {
            if (state == null)
                return null;

            var persons = new List<SuccessionPersonSaveEntry>();
            foreach (PersonState person in state.World.Persons)
            {
                state.World.TryGetLife(person.Id, out SuccessionPersonLife life);
                persons.Add(new SuccessionPersonSaveEntry
                {
                    personId = person.Id.Value,
                    displayName = person.DisplayName,
                    gender = (int)person.Gender,
                    birthYear = life.BirthYear,
                    alive = life.Alive,
                    retired = life.Retired
                });
            }

            var members = new List<SuccessionMemberSaveEntry>();
            foreach (FamilyMembership membership in state.World.Family.Members)
                members.Add(new SuccessionMemberSaveEntry
                {
                    personId = membership.PersonId.Value,
                    role = (int)membership.Role
                });

            var kinship = new List<SuccessionKinshipSaveEntry>();
            foreach (KinshipLink link in state.World.Kinship.Links)
                kinship.Add(new SuccessionKinshipSaveEntry
                {
                    from = link.From.Value,
                    to = link.To.Value,
                    type = (int)link.Type
                });
            kinship.Sort(CompareKinshipEntries);

            var pregnancies = new List<FamilyPregnancySaveEntry>();
            foreach (FamilyPregnancyRecord record in state.World.Pregnancies.Records)
                pregnancies.Add(FamilyPregnancySaveEntry.FromRecord(record));
            pregnancies.Sort(ComparePregnancyEntries);

            return new SuccessionSaveData
            {
                phase = (int)state.Phase,
                familyId = state.World.Family.Id.Value,
                familyName = state.World.Family.DisplayName,
                headPersonId = state.HeadPersonId.Value,
                designatedHeirId = state.DesignatedHeirId.Value,
                pendingSuccessorId = state.PendingSuccessorId.Value,
                pendingRegentId = state.PendingRegentId.Value,
                adulthoodAge = state.AdulthoodAge,
                successionCount = state.SuccessionCount,
                completedResultId = state.CompletedResultId ?? string.Empty,
                crisisAdoptionUsed = state.CrisisAdoptionUsed,
                regencyCompleted = state.RegencyCompleted,
                persons = persons.ToArray(),
                members = members.ToArray(),
                kinship = kinship.ToArray(),
                pregnancies = pregnancies.ToArray()
            };
        }

        public SuccessionSaveData Clone()
        {
            var clone = new SuccessionSaveData
            {
                phase = phase,
                familyId = familyId,
                familyName = familyName,
                headPersonId = headPersonId,
                designatedHeirId = designatedHeirId,
                pendingSuccessorId = pendingSuccessorId,
                pendingRegentId = pendingRegentId,
                adulthoodAge = adulthoodAge,
                successionCount = successionCount,
                completedResultId = completedResultId,
                crisisAdoptionUsed = crisisAdoptionUsed,
                regencyCompleted = regencyCompleted
            };
            clone.persons = ClonePersons(persons);
            clone.members = CloneMembers(members);
            clone.kinship = CloneKinship(kinship);
            clone.pregnancies = ClonePregnancies(pregnancies);
            return clone;
        }

        /// <summary>
        /// Structural and cross-reference validation for untrusted v3 save data.
        /// Returns the first problem; never throws.
        /// </summary>
        public SaveOperationResult ValidateForSave(GameDate date)
        {
            if (!Enum.IsDefined(typeof(SuccessionPhase), phase))
                return Invalid("存档继承阶段无效。");
            if (!FamilyId.TryParse(familyId, out _))
                return Invalid("存档继承家族 ID 无效。");
            if (string.IsNullOrWhiteSpace(familyName))
                return Invalid("存档继承家族名称为空。");
            if (adulthoodAge < 14 || adulthoodAge > 25)
                return Invalid("存档成年年龄阈值无效。");
            if (successionCount < 0)
                return Invalid("存档继承次数无效。");
            if (persons == null || persons.Length == 0)
                return Invalid("存档继承人物列表为空。");

            var personMap = new Dictionary<PersonId, SuccessionPersonSaveEntry>();
            string previousPersonId = null;
            foreach (SuccessionPersonSaveEntry entry in persons)
            {
                if (entry == null)
                    return Invalid("存档继承人物记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId))
                    return Invalid("存档继承人物 ID 无效。");
                if (personMap.ContainsKey(personId))
                    return Invalid("存档继承人物 ID 重复。");
                if (previousPersonId != null &&
                    string.CompareOrdinal(previousPersonId, entry.personId) > 0)
                    return Invalid("存档继承人物顺序不稳定。");
                previousPersonId = entry.personId;
                if (string.IsNullOrWhiteSpace(entry.displayName))
                    return Invalid("存档继承人物名称为空。");
                if (!Enum.IsDefined(typeof(PersonGender), entry.gender))
                    return Invalid("存档继承人物性别无效。");
                if (entry.birthYear < 1400 || entry.birthYear > date.Year)
                    return Invalid("存档继承人物出生年份无效。");
                personMap.Add(personId, entry);
            }

            if (members == null || members.Length == 0)
                return Invalid("存档继承家族成员列表为空。");
            var roleMap = new Dictionary<PersonId, FamilyMemberRole>();
            string previousMemberId = null;
            foreach (SuccessionMemberSaveEntry entry in members)
            {
                if (entry == null)
                    return Invalid("存档继承成员记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId) ||
                    !personMap.ContainsKey(personId))
                    return Invalid("存档继承成员不是已登记人物。");
                if (!Enum.IsDefined(typeof(FamilyMemberRole), entry.role))
                    return Invalid("存档继承成员角色无效。");
                if (roleMap.ContainsKey(personId))
                    return Invalid("存档继承成员重复。");
                if (previousMemberId != null &&
                    string.CompareOrdinal(previousMemberId, entry.personId) > 0)
                    return Invalid("存档继承成员顺序不稳定。");
                previousMemberId = entry.personId;
                roleMap.Add(personId, (FamilyMemberRole)entry.role);
            }

            int headCount = 0;
            foreach (FamilyMemberRole role in roleMap.Values)
                if (role == FamilyMemberRole.Head)
                    headCount++;
            if (headCount != 1)
                return Invalid("存档继承家族必须恰好有一名 Head。");

            if (!PersonId.TryParse(headPersonId, out PersonId headId) ||
                !roleMap.TryGetValue(headId, out FamilyMemberRole headRole) ||
                headRole != FamilyMemberRole.Head)
                return Invalid("存档继承家主 ID 与 Head 身份不一致。");
            if (!personMap.TryGetValue(headId, out SuccessionPersonSaveEntry headEntry) ||
                !headEntry.alive || headEntry.retired)
                return Invalid("存档继承家主必须是存活且未退休的人物。");

            PersonId designated = default;
            if (!TryParseOptional(designatedHeirId, out designated, personMap))
                return Invalid("存档指定继承人 ID 无效。");
            PersonId pendingSuccessor = default;
            if (!TryParseOptional(pendingSuccessorId, out pendingSuccessor, personMap))
                return Invalid("存档待定继承人 ID 无效。");
            PersonId pendingRegent = default;
            if (!TryParseOptional(pendingRegentId, out pendingRegent, personMap))
                return Invalid("存档待定摄政 ID 无效。");

            SuccessionPhase parsedPhase = (SuccessionPhase)phase;
            if (parsedPhase == SuccessionPhase.AwaitingSuccessionChoice)
            {
                if (!pendingRegent.IsEmpty)
                    return Invalid("存档继承选择阶段不应有摄政。");
                if (!pendingSuccessor.IsEmpty &&
                    !IsValidCandidate(personMap, roleMap, pendingSuccessor, headId, adulthoodAge,
                        date, requireMinor: false))
                    return Invalid("存档待定继承人无效。");
            }
            else if (parsedPhase == SuccessionPhase.AwaitingRegentChoice)
            {
                if (pendingSuccessor.IsEmpty ||
                    !IsValidCandidate(personMap, roleMap, pendingSuccessor, headId, adulthoodAge,
                        date, requireMinor: true))
                    return Invalid("存档待定未成年继承人无效。");
                if (!pendingRegent.IsEmpty &&
                    !IsValidRegent(personMap, roleMap, pendingRegent, headId, pendingSuccessor,
                        adulthoodAge, date))
                    return Invalid("存档待定摄政无效。");
            }
            else if (parsedPhase == SuccessionPhase.Regency)
            {
                if (!pendingSuccessor.IsEmpty)
                    return Invalid("存档摄政阶段不应有待定继承人。");
                if (pendingRegent.IsEmpty ||
                    !IsValidRegent(personMap, roleMap, pendingRegent, headId, default,
                        adulthoodAge, date))
                    return Invalid("存档摄政阶段缺少合资格摄政。");
            }
            else if (parsedPhase == SuccessionPhase.CrisisDecision)
            {
                if (!pendingSuccessor.IsEmpty || !pendingRegent.IsEmpty)
                    return Invalid("存档危机阶段不应有待定人选。");
                if (crisisAdoptionUsed)
                    return Invalid("存档危机阶段与已使用标记冲突。");
            }
            else if (parsedPhase == SuccessionPhase.None || parsedPhase == SuccessionPhase.Extinct)
            {
                if (!pendingSuccessor.IsEmpty || !pendingRegent.IsEmpty)
                    return Invalid("存档稳定阶段不应有待定人选。");
            }

            if (kinship == null)
                return Invalid("存档继承亲属关系列表为空。");
            var seenKinship = new HashSet<string>(StringComparer.Ordinal);
            SuccessionKinshipSaveEntry previousKinship = null;
            foreach (SuccessionKinshipSaveEntry entry in kinship)
            {
                if (entry == null)
                    return Invalid("存档继承亲属记录为空。");
                if (!PersonId.TryParse(entry.from, out PersonId from) ||
                    !PersonId.TryParse(entry.to, out PersonId to) ||
                    from == to)
                    return Invalid("存档继承亲属端点无效。");
                if (!personMap.ContainsKey(from) || !personMap.ContainsKey(to))
                    return Invalid("存档继承亲属端点不是已登记人物。");
                if (!Enum.IsDefined(typeof(KinshipType), entry.type))
                    return Invalid("存档继承亲属类型无效。");
                var link = new KinshipLink(from, to, (KinshipType)entry.type);
                string normalized = NormalizeKinshipKey(link);
                if (!seenKinship.Add(normalized))
                    return Invalid("存档继承亲属关系重复。");
                if (previousKinship != null && CompareKinshipEntries(previousKinship, entry) > 0)
                    return Invalid("存档继承亲属顺序不稳定。");
                previousKinship = entry;
            }

            if (pregnancies != null)
            {
                SaveOperationResult pregnancyValidation = ValidatePregnancies(personMap, roleMap);
                if (!pregnancyValidation.Succeeded)
                    return pregnancyValidation;
            }

            return SaveOperationResult.Success();
        }

        /// <summary>
        /// Validates active pregnancy rows against the saved roster. The gestating parent
        /// must still be a living family member; the other parent may be deceased but must
        /// remain an identified family member. Ordering must be stable by gestating ID and
        /// neither a female nor a spouse pair may appear twice.
        /// </summary>
        private SaveOperationResult ValidatePregnancies(
            Dictionary<PersonId, SuccessionPersonSaveEntry> persons,
            Dictionary<PersonId, FamilyMemberRole> roles)
        {
            var seenFemales = new HashSet<string>(StringComparer.Ordinal);
            var seenPairs = new HashSet<string>(StringComparer.Ordinal);
            string previousGestating = null;
            foreach (FamilyPregnancySaveEntry entry in pregnancies)
            {
                if (entry == null)
                    return Invalid("存档孕期记录为空。");
                if (!PersonId.TryParse(entry.gestatingPersonId, out PersonId gestating) ||
                    !PersonId.TryParse(entry.otherParentId, out PersonId other) ||
                    gestating == other)
                    return Invalid("存档孕期人物 ID 无效。");
                if (!persons.TryGetValue(gestating, out SuccessionPersonSaveEntry gestatingEntry) ||
                    !persons.TryGetValue(other, out SuccessionPersonSaveEntry otherEntry))
                    return Invalid("存档孕期引用了未登记人物。");
                if (!roles.ContainsKey(gestating) || !roles.ContainsKey(other))
                    return Invalid("存档孕期引用了非家庭成员。");
                if (!gestatingEntry.alive)
                    return Invalid("存档孕期人物必须存活。");
                if (gestatingEntry.gender != (int)PersonGender.Female ||
                    otherEntry.gender != (int)PersonGender.Male)
                    return Invalid("存档孕期双方性别无效。");
                string recordError = string.Empty;
                if (!GameDate.TryParse(entry.conceptionYear, entry.conceptionMonth,
                        entry.conceptionDay, out GameDate conception) ||
                    !GameDate.TryParse(entry.dueYear, entry.dueMonth, entry.dueDay,
                        out GameDate due) ||
                    !FamilyPregnancyRecord.TryCreate(gestating, other, conception, due,
                        out _, out recordError))
                    return Invalid(string.IsNullOrEmpty(recordError)
                        ? "存档孕期日期无效。"
                        : "存档孕期记录无效：" + recordError);
                if (previousGestating != null &&
                    string.CompareOrdinal(previousGestating, entry.gestatingPersonId) > 0)
                    return Invalid("存档孕期顺序不稳定。");
                previousGestating = entry.gestatingPersonId;
                if (!seenFemales.Add(entry.gestatingPersonId))
                    return Invalid("存档孕期同一女性重复。");
                if (!seenPairs.Add(PregnancyPairKey(gestating, other)))
                    return Invalid("存档孕期同一配偶对重复。");
            }
            return SaveOperationResult.Success();
        }

        private static string PregnancyPairKey(PersonId a, PersonId b)
        {
            string first = a.Value ?? string.Empty;
            string second = b.Value ?? string.Empty;
            if (string.CompareOrdinal(first, second) > 0)
            {
                string swap = first;
                first = second;
                second = swap;
            }
            return first.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ":" + first + "|" +
                   second.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ":" + second;
        }

        /// <summary>
        /// Rebuilds a validated domain snapshot. The caller must not assign the
        /// result until this method succeeds, preserving atomic load semantics.
        /// </summary>
        public static bool TryRestore(SuccessionSaveData data, GameDate date,
            out SuccessionState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档继承状态为空。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(date);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            return TryBuildState(data, out state, out error);
        }

        /// <summary>
        /// Rebuilds a succession snapshot captured by this same process for an
        /// application transaction. It deliberately skips semantic save validation:
        /// a live session can be temporarily outside save invariants (for example a
        /// gestating parent marked dead), and the monthly transaction must still be
        /// able to clone, cancel the pregnancy and roll back. Only
        /// <see cref="Capture"/> output is a valid input.
        /// </summary>
        public static bool TryRestoreTransactionSnapshot(SuccessionSaveData data,
            out SuccessionState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "事务继承快照为空。";
                return false;
            }
            return TryBuildState(data, out state, out error);
        }

        private static bool TryBuildState(SuccessionSaveData data, out SuccessionState state,
            out string error)
        {
            state = null;
            if (data.persons == null || data.members == null || data.kinship == null)
            {
                error = "事务继承快照缺少人物、成员或亲属列表。";
                return false;
            }

            if (!FamilyId.TryParse(data.familyId, out FamilyId familyId))
            {
                error = "存档继承家族 ID 无效。";
                return false;
            }

            var world = new SuccessionWorld(familyId, data.familyName);
            foreach (SuccessionPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (!world.TryAddPerson(new PersonState(personId, entry.displayName,
                        (PersonGender)entry.gender),
                        new SuccessionPersonLife(entry.birthYear, entry.alive, entry.retired),
                        out error))
                    return false;
            }
            foreach (SuccessionMemberSaveEntry entry in data.members)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (!world.TryAddMember(personId, (FamilyMemberRole)entry.role, out error))
                    return false;
            }
            foreach (SuccessionKinshipSaveEntry entry in data.kinship)
            {
                PersonId.TryParse(entry.from, out PersonId from);
                PersonId.TryParse(entry.to, out PersonId to);
                if (!KinshipLink.TryCreate(from, to, (KinshipType)entry.type,
                        out KinshipLink link, out error) ||
                    !world.TryAddKinship(link, out error))
                    return false;
            }
            if (data.pregnancies != null)
            {
                foreach (FamilyPregnancySaveEntry entry in data.pregnancies)
                {
                    PersonId.TryParse(entry.gestatingPersonId, out PersonId gestating);
                    PersonId.TryParse(entry.otherParentId, out PersonId other);
                    GameDate.TryParse(entry.conceptionYear, entry.conceptionMonth,
                        entry.conceptionDay, out GameDate conception);
                    GameDate.TryParse(entry.dueYear, entry.dueMonth, entry.dueDay,
                        out GameDate due);
                    if (!FamilyPregnancyRecord.TryCreate(gestating, other, conception, due,
                            out FamilyPregnancyRecord record, out error) ||
                        !world.Pregnancies.TryAdd(record, out error))
                        return false;
                }
            }

            TryParseOptional(data.headPersonId, out PersonId headId, null);
            TryParseOptional(data.designatedHeirId, out PersonId designated, null);
            TryParseOptional(data.pendingSuccessorId, out PersonId pendingSuccessor, null);
            TryParseOptional(data.pendingRegentId, out PersonId pendingRegent, null);
            state = new SuccessionState(world)
            {
                Phase = (SuccessionPhase)data.phase,
                HeadPersonId = headId,
                DesignatedHeirId = designated,
                PendingSuccessorId = pendingSuccessor,
                PendingRegentId = pendingRegent,
                AdulthoodAge = data.adulthoodAge,
                SuccessionCount = data.successionCount,
                CompletedResultId = data.completedResultId ?? string.Empty,
                CrisisAdoptionUsed = data.crisisAdoptionUsed,
                RegencyCompleted = data.regencyCompleted
            };
            error = string.Empty;
            return true;
        }

        private static bool TryParseOptional(string value, out PersonId personId,
            Dictionary<PersonId, SuccessionPersonSaveEntry> known)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                personId = default;
                return true;
            }
            if (!PersonId.TryParse(value, out personId))
                return false;
            return known == null || known.ContainsKey(personId);
        }

        private static bool IsValidCandidate(Dictionary<PersonId, SuccessionPersonSaveEntry> persons,
            Dictionary<PersonId, FamilyMemberRole> roles, PersonId personId, PersonId headId,
            int adulthoodAge, GameDate date, bool requireMinor)
        {
            if (personId == headId || !persons.TryGetValue(personId, out SuccessionPersonSaveEntry entry) ||
                !roles.TryGetValue(personId, out FamilyMemberRole role))
                return false;
            if (!entry.alive || entry.retired || !SuccessionService.IsSuccessorRole(role))
                return false;
            int age = date.Year - entry.birthYear;
            return requireMinor ? age < adulthoodAge : age >= adulthoodAge;
        }

        private static bool IsValidRegent(Dictionary<PersonId, SuccessionPersonSaveEntry> persons,
            Dictionary<PersonId, FamilyMemberRole> roles, PersonId personId, PersonId headId,
            PersonId pendingSuccessor, int adulthoodAge, GameDate date)
        {
            if (personId == headId || personId == pendingSuccessor ||
                !persons.TryGetValue(personId, out SuccessionPersonSaveEntry entry) ||
                !roles.TryGetValue(personId, out FamilyMemberRole role))
                return false;
            if (!entry.alive || entry.retired || !SuccessionService.IsRegentRole(role))
                return false;
            return date.Year - entry.birthYear >= adulthoodAge;
        }

        /// <summary>
        /// The single ordinal kinship ordering rule shared by capture and validation.
        /// Tuple order (from, then to, then type) is unambiguous even when one person
        /// ID is a textual prefix of another.
        /// </summary>
        internal static int CompareKinshipKeys(string fromA, string toA, int typeA,
            string fromB, string toB, int typeB)
        {
            int byFrom = string.CompareOrdinal(fromA ?? string.Empty, fromB ?? string.Empty);
            if (byFrom != 0)
                return byFrom;
            int byTo = string.CompareOrdinal(toA ?? string.Empty, toB ?? string.Empty);
            if (byTo != 0)
                return byTo;
            return typeA.CompareTo(typeB);
        }

        internal static int CompareKinshipEntries(SuccessionKinshipSaveEntry a,
            SuccessionKinshipSaveEntry b) =>
            CompareKinshipKeys(a?.from, a?.to, a?.type ?? 0, b?.from, b?.to, b?.type ?? 0);

        /// <summary>Stable pregnancy capture order: ordinal gestating-person ID.</summary>
        internal static int ComparePregnancyEntries(FamilyPregnancySaveEntry a,
            FamilyPregnancySaveEntry b) =>
            string.CompareOrdinal(a?.gestatingPersonId ?? string.Empty,
                b?.gestatingPersonId ?? string.Empty);

        private static string NormalizeKinshipKey(KinshipLink link)
        {
            string from = link.From.Value ?? string.Empty;
            string to = link.To.Value ?? string.Empty;
            if (link.IsSymmetric && string.CompareOrdinal(from, to) > 0)
            {
                string swap = from;
                from = to;
                to = swap;
            }

            // Length prefixes keep the duplicate key unambiguous for IDs containing
            // separator-like characters (for example "a|b").
            return from.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                   from + to.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                   to + ((int)link.Type).ToString(
                       "D3", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static SuccessionPersonSaveEntry[] ClonePersons(SuccessionPersonSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new SuccessionPersonSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                SuccessionPersonSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new SuccessionPersonSaveEntry
                {
                    personId = entry.personId,
                    displayName = entry.displayName,
                    gender = entry.gender,
                    birthYear = entry.birthYear,
                    alive = entry.alive,
                    retired = entry.retired
                };
            }
            return clone;
        }

        private static SuccessionMemberSaveEntry[] CloneMembers(SuccessionMemberSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new SuccessionMemberSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                SuccessionMemberSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new SuccessionMemberSaveEntry
                {
                    personId = entry.personId,
                    role = entry.role
                };
            }
            return clone;
        }

        private static SuccessionKinshipSaveEntry[] CloneKinship(SuccessionKinshipSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new SuccessionKinshipSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                SuccessionKinshipSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new SuccessionKinshipSaveEntry
                {
                    from = entry.from,
                    to = entry.to,
                    type = entry.type
                };
            }
            return clone;
        }

        /// <summary>
        /// Clones pregnancy rows. A null legacy array becomes an empty current array:
        /// v13 always writes a concrete list, and every migration branch ends in a
        /// current-version snapshot that must satisfy that invariant.
        /// </summary>
        private static FamilyPregnancySaveEntry[] ClonePregnancies(
            FamilyPregnancySaveEntry[] source)
        {
            if (source == null)
                return new FamilyPregnancySaveEntry[0];
            var clone = new FamilyPregnancySaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                FamilyPregnancySaveEntry entry = source[i];
                clone[i] = entry == null ? null : new FamilyPregnancySaveEntry
                {
                    gestatingPersonId = entry.gestatingPersonId,
                    otherParentId = entry.otherParentId,
                    conceptionYear = entry.conceptionYear,
                    conceptionMonth = entry.conceptionMonth,
                    conceptionDay = entry.conceptionDay,
                    dueYear = entry.dueYear,
                    dueMonth = entry.dueMonth,
                    dueDay = entry.dueDay
                };
            }
            return clone;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class SuccessionPersonSaveEntry
    {
        public string personId;
        public string displayName;
        public int gender;
        public int birthYear;
        public bool alive;
        public bool retired;
    }

    [Serializable]
    public sealed class SuccessionMemberSaveEntry
    {
        public string personId;
        public int role;
    }

    [Serializable]
    public sealed class SuccessionKinshipSaveEntry
    {
        public string from;
        public string to;
        public int type;
    }

    /// <summary>Plain-data v13 pregnancy row; dates stay as explicit calendar fields.</summary>
    [Serializable]
    public sealed class FamilyPregnancySaveEntry
    {
        public string gestatingPersonId;
        public string otherParentId;
        public int conceptionYear;
        public int conceptionMonth;
        public int conceptionDay;
        public int dueYear;
        public int dueMonth;
        public int dueDay;

        public static FamilyPregnancySaveEntry FromRecord(FamilyPregnancyRecord record) =>
            new FamilyPregnancySaveEntry
            {
                gestatingPersonId = record.GestatingPersonId.Value,
                otherParentId = record.OtherParentId.Value,
                conceptionYear = record.ConceptionDate.Year,
                conceptionMonth = record.ConceptionDate.Month,
                conceptionDay = record.ConceptionDate.Day,
                dueYear = record.DueDate.Year,
                dueMonth = record.DueDate.Month,
                dueDay = record.DueDate.Day
            };
    }

    /// <summary>
    /// Plain-data v4 chronicle record. Field names and values are the single save
    /// format; the runtime <see cref="HistoryLedger"/> rebuild is owned by
    /// <see cref="ChronicleSaveCodec"/>.
    /// </summary>
    [Serializable]
    public sealed class ChronicleSaveEntry
    {
        public string entryId;
        public int sequence;
        public int year;
        public int month;
        public int category;
        public string subjectId;
        public string sourceId;
        public string title;
        public string summary;
        public int fromValue;
        public int toValue;
        public int fromStage;
        public int toStage;
        public int fromPhase;
        public int toPhase;
        public string fromHeadId;
        public string toHeadId;

        public static ChronicleSaveEntry FromEntry(FamilyChronicleEntry entry)
        {
            if (entry == null)
                return null;

            return new ChronicleSaveEntry
            {
                entryId = entry.EntryId,
                sequence = entry.Sequence,
                year = entry.Year,
                month = entry.Month,
                category = (int)entry.Category,
                subjectId = entry.SubjectId,
                sourceId = entry.SourceId,
                title = entry.Title,
                summary = entry.Summary,
                fromValue = entry.FromValue,
                toValue = entry.ToValue,
                fromStage = entry.FromStage,
                toStage = entry.ToStage,
                fromPhase = entry.FromPhase,
                toPhase = entry.ToPhase,
                fromHeadId = entry.FromHeadId,
                toHeadId = entry.ToHeadId
            };
        }
    }

    /// <summary>
    /// Maps the chronicle persistence DTO to and from the immutable domain ledger.
    /// Restore is atomic: a malformed or duplicate record yields no ledger at all.
    /// </summary>
    public static class ChronicleSaveCodec
    {
        public static ChronicleSaveEntry[] Capture(HistoryLedger ledger)
        {
            if (ledger == null || ledger.Count == 0)
                return new ChronicleSaveEntry[0];

            var captured = new ChronicleSaveEntry[ledger.Count];
            for (int i = 0; i < ledger.Count; i++)
                captured[i] = ChronicleSaveEntry.FromEntry(ledger.Entries[i]);
            return captured;
        }

        public static bool TryRestore(ChronicleSaveEntry[] saved, out HistoryLedger ledger,
            out string error)
        {
            ledger = null;
            error = string.Empty;
            if (saved == null || saved.Length == 0)
            {
                ledger = HistoryLedger.CreateEmpty();
                return true;
            }

            var entries = new FamilyChronicleEntry[saved.Length];
            for (int i = 0; i < saved.Length; i++)
            {
                ChronicleSaveEntry dto = saved[i];
                if (dto == null)
                {
                    error = "存档编年史记录为空。";
                    return false;
                }
                if (!FamilyChronicleEntry.TryCreate(dto.sequence, dto.entryId, dto.year, dto.month,
                        (ChronicleCategory)dto.category, dto.subjectId, dto.sourceId, dto.title,
                        dto.summary, dto.fromValue, dto.toValue, dto.fromStage, dto.toStage,
                        dto.fromPhase, dto.toPhase, dto.fromHeadId, dto.toHeadId,
                        out entries[i], out error))
                    return false;
            }

            return HistoryLedger.TryRestore(entries, out ledger, out error);
        }

        public static ChronicleSaveEntry[] Clone(ChronicleSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new ChronicleSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ChronicleSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new ChronicleSaveEntry
                {
                    entryId = entry.entryId,
                    sequence = entry.sequence,
                    year = entry.year,
                    month = entry.month,
                    category = entry.category,
                    subjectId = entry.subjectId,
                    sourceId = entry.sourceId,
                    title = entry.title,
                    summary = entry.summary,
                    fromValue = entry.fromValue,
                    toValue = entry.toValue,
                    fromStage = entry.fromStage,
                    toStage = entry.toStage,
                    fromPhase = entry.fromPhase,
                    toPhase = entry.toPhase,
                    fromHeadId = entry.fromHeadId,
                    toHeadId = entry.toHeadId
                };
            }
            return clone;
        }
    }
}
