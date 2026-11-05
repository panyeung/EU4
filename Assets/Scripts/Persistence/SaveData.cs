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
        public const int CurrentRulesVersion = 4;

        /// <summary>
        /// Fallback event-content version used only when no catalog is loaded. The
        /// active catalog's <c>contentVersion</c> is authoritative for capture/load.
        /// </summary>
        public const int CurrentEventContentVersion = 2;

        /// <summary>Displayed game build version. Older values are not rejected on load.</summary>
        public const string CurrentGameVersion = "0.1.0";

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
                kinship = kinship.ToArray()
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

            return SaveOperationResult.Success();
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

            if (!FamilyId.TryParse(data.familyId, out FamilyId familyId))
            {
                error = "存档继承家族 ID 无效。";
                return false;
            }

            var world = new SuccessionWorld(familyId, data.familyName);
            foreach (SuccessionPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (!world.TryAddPerson(new PersonState(personId, entry.displayName),
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

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class SuccessionPersonSaveEntry
    {
        public string personId;
        public string displayName;
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
