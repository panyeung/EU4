using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Life status of one participant around one settlement.</summary>
    public readonly struct MissionParticipantState
    {
        public MissionParticipantState(PersonId personId, bool aliveBefore, bool injuredBefore,
            bool aliveAfter, bool injuredAfter)
        {
            PersonId = personId;
            AliveBefore = aliveBefore;
            InjuredBefore = injuredBefore;
            AliveAfter = aliveAfter;
            InjuredAfter = injuredAfter;
        }

        public PersonId PersonId { get; }
        public bool AliveBefore { get; }
        public bool InjuredBefore { get; }
        public bool AliveAfter { get; }
        public bool InjuredAfter { get; }
    }

    /// <summary>
    /// One immutable M07 terminal mission record. It is the durable proof that a run was
    /// settled exactly once, and it carries the frozen inputs, the actual applied
    /// effects and the linked chronicle entry.
    /// </summary>
    public sealed class MissionRecord
    {
        public MissionRecord(string runId, string missionId, MissionKind kind,
            MissionOperationMode mode, GameDate date, SharedMissionOutcome outcome,
            PersonId captainId, IReadOnlyList<PersonId> participants,
            IReadOnlyList<PersonId> disclosedRiskParticipants, MissionPreparation preparation,
            int firstNinjutsu, int secondNinjutsu, int frozenIntelligence, int difficulty,
            int roll, int score, int fameBefore, int fameAfter, int concealmentBefore,
            int concealmentAfter, IReadOnlyList<MissionStandingChange> relationChanges,
            int fundsDelta, int suppliesDelta, int intelligenceDelta,
            IReadOnlyList<MissionParticipantState> participantStates, string chronicleEntryId)
        {
            RunId = runId;
            MissionId = missionId;
            Kind = kind;
            Mode = mode;
            Date = date;
            Outcome = outcome;
            CaptainId = captainId;
            Participants = participants ?? new PersonId[0];
            DisclosedRiskParticipants = disclosedRiskParticipants ?? new PersonId[0];
            Preparation = preparation;
            FirstNinjutsu = firstNinjutsu;
            SecondNinjutsu = secondNinjutsu;
            FrozenIntelligence = frozenIntelligence;
            Difficulty = difficulty;
            Roll = roll;
            Score = score;
            FameBefore = fameBefore;
            FameAfter = fameAfter;
            ConcealmentBefore = concealmentBefore;
            ConcealmentAfter = concealmentAfter;
            RelationChanges = relationChanges ?? new MissionStandingChange[0];
            FundsDelta = fundsDelta;
            SuppliesDelta = suppliesDelta;
            IntelligenceDelta = intelligenceDelta;
            ParticipantStates = participantStates ?? new MissionParticipantState[0];
            ChronicleEntryId = chronicleEntryId ?? string.Empty;
        }

        public string RunId { get; }
        public string MissionId { get; }
        public MissionKind Kind { get; }
        public MissionOperationMode Mode { get; }
        public GameDate Date { get; }
        public SharedMissionOutcome Outcome { get; }
        public PersonId CaptainId { get; }
        public IReadOnlyList<PersonId> Participants { get; }
        public IReadOnlyList<PersonId> DisclosedRiskParticipants { get; }
        public MissionPreparation Preparation { get; }
        public int FirstNinjutsu { get; }
        public int SecondNinjutsu { get; }
        public int FrozenIntelligence { get; }
        public int Difficulty { get; }
        public int Roll { get; }
        public int Score { get; }
        public int FameBefore { get; }
        public int FameAfter { get; }
        public int ConcealmentBefore { get; }
        public int ConcealmentAfter { get; }
        public IReadOnlyList<MissionStandingChange> RelationChanges { get; }
        public int FundsDelta { get; }
        public int SuppliesDelta { get; }
        public int IntelligenceDelta { get; }
        public IReadOnlyList<MissionParticipantState> ParticipantStates { get; }
        public string ChronicleEntryId { get; }

        public int FameDelta => FameAfter - FameBefore;
        public int ConcealmentDelta => ConcealmentAfter - ConcealmentBefore;
        public bool AnyParticipantDied
        {
            get
            {
                for (int i = 0; i < ParticipantStates.Count; i++)
                    if (ParticipantStates[i].AliveBefore && !ParticipantStates[i].AliveAfter)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Structural validation for one record. Used both when a live settlement appends
        /// and when an untrusted save is restored, so no illegal history can be loaded.
        /// </summary>
        public static bool TryCreate(string runId, string missionId, MissionKind kind,
            MissionOperationMode mode, GameDate date, SharedMissionOutcome outcome,
            PersonId captainId, IReadOnlyList<PersonId> participants,
            IReadOnlyList<PersonId> disclosedRiskParticipants, MissionPreparation preparation,
            int firstNinjutsu, int secondNinjutsu, int frozenIntelligence, int difficulty,
            int roll, int score, int fameBefore, int fameAfter, int concealmentBefore,
            int concealmentAfter, IReadOnlyList<MissionStandingChange> relationChanges,
            int fundsDelta, int suppliesDelta, int intelligenceDelta,
            IReadOnlyList<MissionParticipantState> participantStates, string chronicleEntryId,
            out MissionRecord record, out string error)
        {
            record = null;
            error = string.Empty;

            if (!MissionRequest.IsStableRunId(runId))
            {
                error = "任务历史运行 ID 无效。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(missionId))
            {
                error = "任务历史缺少任务 ID。";
                return false;
            }
            if (!Enum.IsDefined(typeof(MissionKind), kind) ||
                !Enum.IsDefined(typeof(MissionOperationMode), mode) ||
                !Enum.IsDefined(typeof(SharedMissionOutcome), outcome) ||
                !Enum.IsDefined(typeof(MissionPreparation), preparation))
            {
                error = "任务历史包含未定义的枚举值。";
                return false;
            }
            if (!date.IsValid)
            {
                error = "任务历史日期无效。";
                return false;
            }
            if (mode == MissionOperationMode.LegacyUnknown &&
                !runId.StartsWith("legacy.", StringComparison.Ordinal))
            {
                error = "未知操作模式只能用于旧版导入记录（legacy. 前缀）。";
                return false;
            }
            if (mode != MissionOperationMode.LegacyUnknown &&
                runId.StartsWith("legacy.", StringComparison.Ordinal))
            {
                error = "旧版导入记录的操作模式必须是未知。";
                return false;
            }
            bool legacy = mode == MissionOperationMode.LegacyUnknown;
            if (participants == null) participants = new PersonId[0];
            if (disclosedRiskParticipants == null)
                disclosedRiskParticipants = new PersonId[0];
            if (participantStates == null) participantStates = new MissionParticipantState[0];
            if (relationChanges == null) relationChanges = new MissionStandingChange[0];
            if (!legacy)
            {
                if (captainId.IsEmpty)
                {
                    error = "任务历史缺少队长（M05 为执行人）。";
                    return false;
                }
                if (participants == null || participants.Count == 0)
                {
                    error = "任务历史缺少参与者。";
                    return false;
                }
            }

            var unique = new HashSet<PersonId>();
            for (int i = 0; i < (participants == null ? 0 : participants.Count); i++)
            {
                PersonId participant = participants[i];
                if (participant.IsEmpty || !unique.Add(participant))
                {
                    error = "任务历史参与者为空或重复。";
                    return false;
                }
            }
            if (!captainId.IsEmpty && !unique.Contains(captainId))
            {
                error = "任务历史队长必须属于参与者。";
                return false;
            }
            if (!legacy)
            {
                if (kind == MissionKind.TacticalInfiltration && participants.Count != 2)
                {
                    error = "潜入任务历史必须有两名参与者。";
                    return false;
                }
                if (kind == MissionKind.ShadowEscort && participants.Count != 1)
                {
                    error = "护送委托历史必须有一名执行人。";
                    return false;
                }
            }

            int disclosed = disclosedRiskParticipants == null ? 0 : disclosedRiskParticipants.Count;
            if (legacy && disclosed > 0)
            {
                error = "旧版导入记录不得推测死亡风险名单。";
                return false;
            }
            var risk = new HashSet<PersonId>();
            if (disclosedRiskParticipants != null)
            {
                for (int i = 0; i < disclosedRiskParticipants.Count; i++)
                {
                    if (!unique.Contains(disclosedRiskParticipants[i]) ||
                        !risk.Add(disclosedRiskParticipants[i]))
                    {
                        error = "任务历史死亡风险名单包含未出发或重复的人物。";
                        return false;
                    }
                }
            }
            if (kind == MissionKind.ShadowEscort && disclosed != 0)
            {
                error = "M05 护送委托不会造成死亡，历史不应包含死亡风险名单。";
                return false;
            }

            if (firstNinjutsu < 0 || firstNinjutsu > 100 ||
                secondNinjutsu < 0 || secondNinjutsu > 100 ||
                frozenIntelligence < 0 || frozenIntelligence > 100 ||
                difficulty < 0 || difficulty > 100 ||
                score < -1000 || score > 1000)
            {
                error = "任务历史冻结数值超出允许范围。";
                return false;
            }

            if (fameBefore < FamilyStandingState.Minimum || fameBefore > FamilyStandingState.Maximum ||
                fameAfter < FamilyStandingState.Minimum || fameAfter > FamilyStandingState.Maximum ||
                concealmentBefore < FamilyStandingState.Minimum ||
                concealmentBefore > FamilyStandingState.Maximum ||
                concealmentAfter < FamilyStandingState.Minimum ||
                concealmentAfter > FamilyStandingState.Maximum)
            {
                error = "任务历史名望或隐蔽度超出 0..100。";
                return false;
            }

            for (int i = 0; i < (relationChanges == null ? 0 : relationChanges.Count); i++)
            {
                MissionStandingChange change = relationChanges[i];
                if (string.IsNullOrWhiteSpace(change.RelationId))
                {
                    error = "任务历史关系变化缺少稳定 ID。";
                    return false;
                }
                if (change.Before < FamilyStandingState.RelationMinimum ||
                    change.Before > FamilyStandingState.RelationMaximum ||
                    change.After < FamilyStandingState.RelationMinimum ||
                    change.After > FamilyStandingState.RelationMaximum)
                {
                    error = "任务历史关系值超出 -100..100。";
                    return false;
                }
            }

            if (participantStates == null ||
                participantStates.Count != (participants == null ? 0 : participants.Count))
            {
                error = "任务历史参与者生命状态与参与者数量不一致。";
                return false;
            }
            var stateIds = new HashSet<PersonId>();
            bool anyDeath = false;
            for (int i = 0; i < participantStates.Count; i++)
            {
                MissionParticipantState state = participantStates[i];
                if (!stateIds.Add(state.PersonId) || !unique.Contains(state.PersonId))
                {
                    error = "任务历史参与者生命状态的人物无效或重复。";
                    return false;
                }
                if (!state.AliveBefore)
                {
                    error = "任务历史不能记录已死亡人物出发。";
                    return false;
                }
                bool died = !state.AliveAfter;
                if (died)
                {
                    anyDeath = true;
                    if (outcome != SharedMissionOutcome.SevereFailure)
                    {
                        error = "只有严重失败可以造成队员死亡。";
                        return false;
                    }
                    if (!risk.Contains(state.PersonId))
                    {
                        error = "死亡队员必须在简报逐名披露的风险名单中。";
                        return false;
                    }
                }
                if (!state.AliveAfter && state.InjuredAfter)
                {
                    error = "死亡队员不能同时处于受伤状态。";
                    return false;
                }
                if (state.InjuredAfter && outcome != SharedMissionOutcome.SevereFailure)
                {
                    error = "只有严重失败可以新增队员伤势。";
                    return false;
                }
            }
            if (anyDeath && outcome != SharedMissionOutcome.SevereFailure)
            {
                error = "队员死亡与任务结果不一致。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(chronicleEntryId))
            {
                error = "任务历史缺少编年史记录 ID。";
                return false;
            }

            record = new MissionRecord(runId, missionId, kind, mode, date, outcome, captainId,
                Copy(participants), Copy(disclosedRiskParticipants), preparation,
                firstNinjutsu, secondNinjutsu, frozenIntelligence, difficulty, roll, score,
                fameBefore, fameAfter, concealmentBefore, concealmentAfter,
                relationChanges == null ? null : Copy(relationChanges), fundsDelta,
                suppliesDelta, intelligenceDelta, Copy(participantStates), chronicleEntryId);
            return true;
        }

        private static IReadOnlyList<PersonId> Copy(IReadOnlyList<PersonId> source)
        {
            var copy = new PersonId[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<MissionStandingChange> Copy(
            IReadOnlyList<MissionStandingChange> source)
        {
            var copy = new MissionStandingChange[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<MissionParticipantState> Copy(
            IReadOnlyList<MissionParticipantState> source)
        {
            var copy = new MissionParticipantState[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return Array.AsReadOnly(copy);
        }
    }

    /// <summary>
    /// Ordered terminal mission history. A run ID may be appended once; the lookup is
    /// the idempotency key that prevents a reload or a second click from paying twice.
    /// </summary>
    public sealed class MissionHistoryState
    {
        private readonly List<MissionRecord> _records = new List<MissionRecord>();

        public IReadOnlyList<MissionRecord> Records => _records.AsReadOnly();
        public int Count => _records.Count;

        public bool Contains(string runId)
        {
            for (int i = 0; i < _records.Count; i++)
                if (string.Equals(_records[i].RunId, runId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        public bool TryGet(string runId, out MissionRecord record)
        {
            for (int i = 0; i < _records.Count; i++)
            {
                if (string.Equals(_records[i].RunId, runId, StringComparison.Ordinal))
                {
                    record = _records[i];
                    return true;
                }
            }
            record = null;
            return false;
        }

        public bool TryAppend(MissionRecord record, out string error)
        {
            error = string.Empty;
            if (record == null)
            {
                error = "任务终局记录为空。";
                return false;
            }
            if (Contains(record.RunId))
            {
                error = "任务运行 ID 已结算，不能重复提交：" + record.RunId + "。";
                return false;
            }
            _records.Add(record);
            return true;
        }

        public MissionHistoryState Clone()
        {
            var clone = new MissionHistoryState();
            clone._records.AddRange(_records);
            return clone;
        }

        public void CopyFrom(MissionHistoryState source)
        {
            if (source == null)
                return;
            _records.Clear();
            _records.AddRange(source._records);
        }
    }
}
