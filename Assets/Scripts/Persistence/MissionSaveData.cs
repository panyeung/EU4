using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>One stable employer/faction relation value introduced by M07.</summary>
    [Serializable]
    public sealed class FamilyRelationSaveEntry
    {
        public string relationId;
        public int value;
    }

    /// <summary>
    /// M07 family standing block: family fame, infiltration concealment and relations
    /// sorted by stable ID. Fame/concealment are 0..100; relations are -100..100.
    /// </summary>
    [Serializable]
    public sealed class FamilyStandingSaveData
    {
        public int fame;
        public int concealment;
        public FamilyRelationSaveEntry[] relations;

        public static FamilyStandingSaveData CreateInitial() => new FamilyStandingSaveData
        {
            fame = FamilyStandingState.InitialFame,
            concealment = FamilyStandingState.InitialConcealment,
            relations = new FamilyRelationSaveEntry[0]
        };

        public static FamilyStandingSaveData Capture(FamilyStandingState state)
        {
            if (state == null)
                return null;
            IReadOnlyList<KeyValuePair<string, int>> rows = state.RelationsSorted();
            var relations = new FamilyRelationSaveEntry[rows.Count];
            for (int i = 0; i < rows.Count; i++)
                relations[i] = new FamilyRelationSaveEntry
                {
                    relationId = rows[i].Key,
                    value = rows[i].Value
                };
            return new FamilyStandingSaveData
            {
                fame = state.Fame,
                concealment = state.Concealment,
                relations = relations
            };
        }

        public FamilyStandingSaveData Clone()
        {
            var clone = new FamilyStandingSaveData
            {
                fame = fame,
                concealment = concealment
            };
            if (relations != null)
            {
                clone.relations = new FamilyRelationSaveEntry[relations.Length];
                for (int i = 0; i < relations.Length; i++)
                {
                    FamilyRelationSaveEntry entry = relations[i];
                    clone.relations[i] = entry == null
                        ? null
                        : new FamilyRelationSaveEntry
                        {
                            relationId = entry.relationId,
                            value = entry.value
                        };
                }
            }
            return clone;
        }

        public SaveOperationResult ValidateForSave()
        {
            if (relations == null)
                return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                    "存档缺少家族名望关系列表（familyStanding.relations）。");
            string previous = null;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (FamilyRelationSaveEntry entry in relations)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.relationId))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "存档家族关系记录缺少稳定 ID。");
                if (!seen.Add(entry.relationId))
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "存档家族关系稳定 ID 重复。");
                if (previous != null && string.CompareOrdinal(previous, entry.relationId) > 0)
                    return SaveOperationResult.Failure(SaveErrorCode.InvalidData,
                        "存档家族关系未按稳定 ID 排序。");
                previous = entry.relationId;
            }
            if (!TryRestore(out _, out string error))
                return SaveOperationResult.Failure(SaveErrorCode.InvalidData, error);
            return SaveOperationResult.Success();
        }

        public bool TryRestore(out FamilyStandingState state, out string error)
        {
            state = null;
            error = string.Empty;
            var rows = new List<KeyValuePair<string, int>>();
            if (relations != null)
                foreach (FamilyRelationSaveEntry entry in relations)
                {
                    if (entry == null)
                    {
                        error = "存档家族关系记录为空。";
                        return false;
                    }
                    rows.Add(new KeyValuePair<string, int>(entry.relationId, entry.value));
                }
            return FamilyStandingState.TryCreate(fame, concealment, rows.AsReadOnly(),
                out state, out error);
        }
    }

    /// <summary>One injured person row.</summary>
    [Serializable]
    public sealed class MissionInjuryPersonSaveEntry
    {
        public string personId;
    }

    /// <summary>M07 injured roster, ordered by stable person ID.</summary>
    [Serializable]
    public sealed class MissionInjurySaveData
    {
        public MissionInjuryPersonSaveEntry[] persons;

        public static MissionInjurySaveData CreateEmpty() => new MissionInjurySaveData
        {
            persons = new MissionInjuryPersonSaveEntry[0]
        };

        public static MissionInjurySaveData Capture(MissionInjuryState state)
        {
            if (state == null)
                return CreateEmpty();
            IReadOnlyList<PersonId> injured = state.Injured;
            var persons = new MissionInjuryPersonSaveEntry[injured.Count];
            for (int i = 0; i < injured.Count; i++)
                persons[i] = new MissionInjuryPersonSaveEntry { personId = injured[i].Value };
            return new MissionInjurySaveData { persons = persons };
        }

        public MissionInjurySaveData Clone()
        {
            var clone = new MissionInjurySaveData();
            if (persons != null)
            {
                clone.persons = new MissionInjuryPersonSaveEntry[persons.Length];
                for (int i = 0; i < persons.Length; i++)
                {
                    MissionInjuryPersonSaveEntry entry = persons[i];
                    clone.persons[i] = entry == null
                        ? null
                        : new MissionInjuryPersonSaveEntry { personId = entry.personId };
                }
            }
            return clone;
        }

        public bool TryRestore(out MissionInjuryState state, out string error)
        {
            state = null;
            error = string.Empty;
            if (persons == null)
            {
                error = "存档缺少伤员列表（injury.persons）。";
                return false;
            }
            var injured = new List<PersonId>();
            string previous = null;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (MissionInjuryPersonSaveEntry entry in persons)
            {
                if (entry == null || !PersonId.TryParse(entry.personId, out PersonId personId))
                {
                    error = "存档伤员记录人物 ID 无效。";
                    return false;
                }
                if (!seen.Add(entry.personId))
                {
                    error = "存档伤员记录重复。";
                    return false;
                }
                if (previous != null && string.CompareOrdinal(previous, entry.personId) > 0)
                {
                    error = "存档伤员记录未按人物 ID 排序。";
                    return false;
                }
                previous = entry.personId;
                injured.Add(personId);
            }
            return MissionInjuryState.TryCreate(injured.AsReadOnly(), out state, out error);
        }
    }

    /// <summary>One relation before/after pair inside a terminal mission record.</summary>
    [Serializable]
    public sealed class MissionHistoryRelationSaveEntry
    {
        public string relationId;
        public int before;
        public int after;
    }

    /// <summary>One participant life status before/after a terminal mission record.</summary>
    [Serializable]
    public sealed class MissionParticipantStateSaveEntry
    {
        public string personId;
        public bool aliveBefore;
        public bool injuredBefore;
        public bool aliveAfter;
        public bool injuredAfter;
    }

    /// <summary>JsonUtility-friendly terminal M07 mission history row.</summary>
    [Serializable]
    public sealed class MissionHistorySaveEntry
    {
        public string runId;
        public string missionId;
        public int kind;
        public int mode;
        public int year;
        public int month;
        public int day;
        public int outcome;
        public string captainPersonId;
        public string[] participantPersonIds;
        public string[] disclosedRiskPersonIds;
        public int preparation;
        public int firstNinjutsu;
        public int secondNinjutsu;
        public int frozenIntelligence;
        public int difficulty;
        public int roll;
        public int score;
        public int fameBefore;
        public int fameAfter;
        public int concealmentBefore;
        public int concealmentAfter;
        public MissionHistoryRelationSaveEntry[] relations;
        public int fundsDelta;
        public int suppliesDelta;
        public int intelligenceDelta;
        public MissionParticipantStateSaveEntry[] participantStates;
        public string chronicleEntryId;

        public static MissionHistorySaveEntry Capture(MissionRecord record)
        {
            if (record == null)
                return null;
            var participants = new string[record.Participants.Count];
            for (int i = 0; i < participants.Length; i++)
                participants[i] = record.Participants[i].Value;
            var risk = new string[record.DisclosedRiskParticipants.Count];
            for (int i = 0; i < risk.Length; i++)
                risk[i] = record.DisclosedRiskParticipants[i].Value;
            var relations = new MissionHistoryRelationSaveEntry[record.RelationChanges.Count];
            for (int i = 0; i < relations.Length; i++)
            {
                MissionStandingChange change = record.RelationChanges[i];
                relations[i] = new MissionHistoryRelationSaveEntry
                {
                    relationId = change.RelationId,
                    before = change.Before,
                    after = change.After
                };
            }
            var states = new MissionParticipantStateSaveEntry[record.ParticipantStates.Count];
            for (int i = 0; i < states.Length; i++)
            {
                MissionParticipantState state = record.ParticipantStates[i];
                states[i] = new MissionParticipantStateSaveEntry
                {
                    personId = state.PersonId.Value,
                    aliveBefore = state.AliveBefore,
                    injuredBefore = state.InjuredBefore,
                    aliveAfter = state.AliveAfter,
                    injuredAfter = state.InjuredAfter
                };
            }
            return new MissionHistorySaveEntry
            {
                runId = record.RunId,
                missionId = record.MissionId,
                kind = (int)record.Kind,
                mode = (int)record.Mode,
                year = record.Date.Year,
                month = record.Date.Month,
                day = record.Date.Day,
                outcome = (int)record.Outcome,
                captainPersonId = record.CaptainId.Value,
                participantPersonIds = participants,
                disclosedRiskPersonIds = risk,
                preparation = (int)record.Preparation,
                firstNinjutsu = record.FirstNinjutsu,
                secondNinjutsu = record.SecondNinjutsu,
                frozenIntelligence = record.FrozenIntelligence,
                difficulty = record.Difficulty,
                roll = record.Roll,
                score = record.Score,
                fameBefore = record.FameBefore,
                fameAfter = record.FameAfter,
                concealmentBefore = record.ConcealmentBefore,
                concealmentAfter = record.ConcealmentAfter,
                relations = relations,
                fundsDelta = record.FundsDelta,
                suppliesDelta = record.SuppliesDelta,
                intelligenceDelta = record.IntelligenceDelta,
                participantStates = states,
                chronicleEntryId = record.ChronicleEntryId
            };
        }

        public static MissionHistorySaveEntry Clone(MissionHistorySaveEntry source)
        {
            if (source == null)
                return null;
            var clone = new MissionHistorySaveEntry
            {
                runId = source.runId,
                missionId = source.missionId,
                kind = source.kind,
                mode = source.mode,
                year = source.year,
                month = source.month,
                day = source.day,
                outcome = source.outcome,
                captainPersonId = source.captainPersonId,
                participantPersonIds = CloneStrings(source.participantPersonIds),
                disclosedRiskPersonIds = CloneStrings(source.disclosedRiskPersonIds),
                preparation = source.preparation,
                firstNinjutsu = source.firstNinjutsu,
                secondNinjutsu = source.secondNinjutsu,
                frozenIntelligence = source.frozenIntelligence,
                difficulty = source.difficulty,
                roll = source.roll,
                score = source.score,
                fameBefore = source.fameBefore,
                fameAfter = source.fameAfter,
                concealmentBefore = source.concealmentBefore,
                concealmentAfter = source.concealmentAfter,
                fundsDelta = source.fundsDelta,
                suppliesDelta = source.suppliesDelta,
                intelligenceDelta = source.intelligenceDelta,
                chronicleEntryId = source.chronicleEntryId
            };
            if (source.relations != null)
            {
                clone.relations = new MissionHistoryRelationSaveEntry[source.relations.Length];
                for (int i = 0; i < source.relations.Length; i++)
                {
                    MissionHistoryRelationSaveEntry entry = source.relations[i];
                    clone.relations[i] = entry == null
                        ? null
                        : new MissionHistoryRelationSaveEntry
                        {
                            relationId = entry.relationId,
                            before = entry.before,
                            after = entry.after
                        };
                }
            }
            if (source.participantStates != null)
            {
                clone.participantStates =
                    new MissionParticipantStateSaveEntry[source.participantStates.Length];
                for (int i = 0; i < source.participantStates.Length; i++)
                {
                    MissionParticipantStateSaveEntry entry = source.participantStates[i];
                    clone.participantStates[i] = entry == null
                        ? null
                        : new MissionParticipantStateSaveEntry
                        {
                            personId = entry.personId,
                            aliveBefore = entry.aliveBefore,
                            injuredBefore = entry.injuredBefore,
                            aliveAfter = entry.aliveAfter,
                            injuredAfter = entry.injuredAfter
                        };
                }
            }
            return clone;
        }

        public bool TryRestore(out MissionRecord record, out string error)
        {
            record = null;
            error = string.Empty;
            if (!MissionRequest.IsStableRunId(runId))
            {
                error = "存档任务历史运行 ID 无效。";
                return false;
            }
            if (!GameDate.TryParse(year, month, day, out GameDate date))
            {
                error = "存档任务历史日期无效。";
                return false;
            }
            bool legacy = mode == (int)MissionOperationMode.LegacyUnknown;
            PersonId captain = default;
            if (!legacy)
            {
                if (!PersonId.TryParse(captainPersonId, out captain) || captain.IsEmpty)
                {
                    error = "存档任务历史队长 ID 无效。";
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(captainPersonId) &&
                     !PersonId.TryParse(captainPersonId, out captain))
            {
                error = "存档任务历史队长 ID 无效。";
                return false;
            }
            if ((participantPersonIds == null || participantPersonIds.Length == 0) && !legacy)
            {
                error = "存档任务历史缺少参与者。";
                return false;
            }
            var participants = new List<PersonId>();
            if (participantPersonIds != null)
                foreach (string id in participantPersonIds)
                {
                    if (!PersonId.TryParse(id, out PersonId personId) || personId.IsEmpty)
                    {
                        error = "存档任务历史参与者 ID 无效。";
                        return false;
                    }
                    participants.Add(personId);
                }
            var risk = new List<PersonId>();
            if (disclosedRiskPersonIds != null)
                foreach (string id in disclosedRiskPersonIds)
                {
                    if (!PersonId.TryParse(id, out PersonId personId) || personId.IsEmpty)
                    {
                        error = "存档任务历史死亡风险人物 ID 无效。";
                        return false;
                    }
                    risk.Add(personId);
                }
            var relationChanges = new List<MissionStandingChange>();
            if (relations != null)
                foreach (MissionHistoryRelationSaveEntry entry in relations)
                {
                    if (entry == null)
                    {
                        error = "存档任务历史关系变化为空。";
                        return false;
                    }
                    relationChanges.Add(new MissionStandingChange(entry.relationId,
                        entry.before, entry.after));
                }
            var states = new List<MissionParticipantState>();
            if (participantStates != null)
                foreach (MissionParticipantStateSaveEntry entry in participantStates)
                {
                    if (entry == null ||
                        !PersonId.TryParse(entry.personId, out PersonId personId))
                    {
                        error = "存档任务历史参与者生命状态无效。";
                        return false;
                    }
                    states.Add(new MissionParticipantState(personId, entry.aliveBefore,
                        entry.injuredBefore, entry.aliveAfter, entry.injuredAfter));
                }
            return MissionRecord.TryCreate(runId, missionId, (MissionKind)kind,
                (MissionOperationMode)mode, date, (SharedMissionOutcome)outcome, captain,
                participants.AsReadOnly(), risk.AsReadOnly(), (MissionPreparation)preparation,
                firstNinjutsu, secondNinjutsu, frozenIntelligence, difficulty, roll, score,
                fameBefore, fameAfter, concealmentBefore, concealmentAfter,
                relationChanges.AsReadOnly(), fundsDelta, suppliesDelta, intelligenceDelta,
                states.AsReadOnly(), chronicleEntryId, out record, out error);
        }

        private static string[] CloneStrings(string[] source)
        {
            if (source == null)
                return null;
            var clone = new string[source.Length];
            source.CopyTo(clone, 0);
            return clone;
        }
    }
}
