using System;
using System.Collections.Generic;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Approved M06 auto-resolution constants. Score is deterministic for a frozen
    /// request: two Ninjutsu values, the frozen intelligence snapshot, the frozen
    /// preparation bonus and one reproducible d100 from the Missions stream.
    /// </summary>
    public static class TacticalMissionResolutionRules
    {
        public const int Difficulty = 50;
        public const int FullSuccessThreshold = 90;
        public const int CostlySuccessThreshold = 65;
        public const int SafeFailureThreshold = 15;
        public const int ScoutingBonus = 10;
        public const int ScoutingSupplyCost = 1;
        public const int IntelligenceMaximumBonus = 10;
        public const int BaseSupplyCost = 2;
        public const int SuccessFunds = 4;
        public const int SuccessIntelligence = 6;
        public const int DeathCheckPercent = 10;
        public const int DieRollExclusiveMax = 100;

        /// <summary>The exact approved d100 score; inputs are clamped, never silently adjusted.</summary>
        public static int Score(int firstNinjutsu, int secondNinjutsu, int frozenIntelligence,
            int preparationBonus, int d100)
        {
            if (firstNinjutsu < 0) firstNinjutsu = 0;
            if (secondNinjutsu < 0) secondNinjutsu = 0;
            if (frozenIntelligence < 0) frozenIntelligence = 0;
            if (frozenIntelligence > IntelligenceMaximumBonus)
                frozenIntelligence = IntelligenceMaximumBonus;
            if (preparationBonus < 0) preparationBonus = 0;
            if (preparationBonus > ScoutingBonus) preparationBonus = ScoutingBonus;
            int average = (firstNinjutsu + secondNinjutsu) / 2;
            return average + frozenIntelligence + preparationBonus + d100 - Difficulty;
        }

        public static SharedMissionOutcome Classify(int score)
        {
            if (score >= FullSuccessThreshold)
                return SharedMissionOutcome.FullSuccess;
            if (score >= CostlySuccessThreshold)
                return SharedMissionOutcome.CostlySuccess;
            if (score >= SafeFailureThreshold)
                return SharedMissionOutcome.SafeFailure;
            return SharedMissionOutcome.SevereFailure;
        }

        /// <summary>
        /// Manual terminal mapping approved for M07. A clean extraction is a full
        /// success; extracting after any guard reached alert is a costly success;
        /// voluntary withdrawal is a safe failure; capture is a severe failure.
        /// Partial success is intentionally unreachable.
        /// </summary>
        public static bool TryMapManual(TacticalMissionOutcome outcome, bool alertTriggered,
            out SharedMissionOutcome mapped, out string error)
        {
            error = string.Empty;
            switch (outcome)
            {
                case TacticalMissionOutcome.Succeeded:
                    mapped = alertTriggered
                        ? SharedMissionOutcome.CostlySuccess
                        : SharedMissionOutcome.FullSuccess;
                    return true;
                case TacticalMissionOutcome.Withdrawn:
                    mapped = SharedMissionOutcome.SafeFailure;
                    return true;
                case TacticalMissionOutcome.Failed:
                    mapped = SharedMissionOutcome.SevereFailure;
                    return true;
                default:
                    mapped = SharedMissionOutcome.SafeFailure;
                    error = "潜入尚未结束，无法映射共享结果。";
                    return false;
            }
        }

        public static int PreparationBonus(MissionPreparation preparation) =>
            preparation == MissionPreparation.Scouting ? ScoutingBonus : 0;

        public static int PreparationSupplyCost(MissionPreparation preparation) =>
            preparation == MissionPreparation.Scouting ? ScoutingSupplyCost : 0;

        /// <summary>Approved start/investment threshold for M06, including preparation.</summary>
        public static int RequiredStartingSupplies(MissionPreparation preparation) =>
            BaseSupplyCost + PreparationSupplyCost(preparation);
    }

    /// <summary>
    /// Immutable, validated mission request frozen when the mission starts. Settlement
    /// only reads this request and its recorded result; the manual/delegated mode is
    /// never consulted for rewards or consequences.
    /// </summary>
    public sealed class MissionRequest
    {
        public const int MaximumRunIdLength = 64;

        private MissionRequest(string runId, string missionId, MissionKind kind,
            MissionOperationMode mode, MissionPreparation preparation, GameDate date,
            PersonId captainId, IReadOnlyList<PersonId> participants,
            IReadOnlyList<PersonId> disclosedRiskParticipants, int firstNinjutsu,
            int secondNinjutsu, int frozenIntelligence, int difficulty, int baseSupplyCost,
            int preparationSupplyCost, int roll, int score)
        {
            RunId = runId;
            MissionId = missionId;
            Kind = kind;
            Mode = mode;
            Preparation = preparation;
            Date = date;
            CaptainId = captainId;
            Participants = participants;
            DisclosedRiskParticipants = disclosedRiskParticipants;
            FirstNinjutsu = firstNinjutsu;
            SecondNinjutsu = secondNinjutsu;
            FrozenIntelligence = frozenIntelligence;
            Difficulty = difficulty;
            BaseSupplyCost = baseSupplyCost;
            PreparationSupplyCost = preparationSupplyCost;
            Roll = roll;
            Score = score;
        }

        public string RunId { get; }
        public string MissionId { get; }
        public MissionKind Kind { get; }
        public MissionOperationMode Mode { get; }
        public MissionPreparation Preparation { get; }
        public GameDate Date { get; }
        public PersonId CaptainId { get; }
        public IReadOnlyList<PersonId> Participants { get; }
        /// <summary>Participants named one by one in the briefing as at death risk.</summary>
        public IReadOnlyList<PersonId> DisclosedRiskParticipants { get; }
        /// <summary>Frozen Ninjutsu of the first participant; M05 stores the assignee value twice.</summary>
        public int FirstNinjutsu { get; }
        public int SecondNinjutsu { get; }
        public int AverageNinjutsu => (FirstNinjutsu + SecondNinjutsu) / 2;
        public int FrozenIntelligence { get; }
        public int Difficulty { get; }
        public int BaseSupplyCost { get; }
        public int PreparationSupplyCost { get; }
        /// <summary>Frozen Missions roll: 0..99 for M05, 1..100 for M06.</summary>
        public int Roll { get; }
        public int Score { get; }

        public int TotalSupplyCost => BaseSupplyCost + PreparationSupplyCost;

        public static bool TryCreate(string runId, string missionId, MissionKind kind,
            MissionOperationMode mode, MissionPreparation preparation, GameDate date,
            PersonId captainId, IReadOnlyList<PersonId> participants,
            IReadOnlyList<PersonId> disclosedRiskParticipants, int firstNinjutsu,
            int secondNinjutsu, int frozenIntelligence, int difficulty, int baseSupplyCost,
            int preparationSupplyCost, int roll, int score,
            out MissionRequest request, out string error)
        {
            request = null;
            error = string.Empty;

            if (!IsStableRunId(runId))
            {
                error = "任务运行 ID 无效：必须为 1-" + MaximumRunIdLength + " 个稳定字符。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(missionId))
            {
                error = "任务 ID 为空。";
                return false;
            }
            if (!Enum.IsDefined(typeof(MissionKind), kind) ||
                !Enum.IsDefined(typeof(MissionOperationMode), mode) ||
                !Enum.IsDefined(typeof(MissionPreparation), preparation))
            {
                error = "任务请求包含未定义的枚举值。";
                return false;
            }
            if (!date.IsValid)
            {
                error = "任务请求日期无效。";
                return false;
            }
            if (captainId.IsEmpty)
            {
                error = "任务请求缺少队长（M05 为执行人）。";
                return false;
            }
            if (participants == null || participants.Count == 0)
            {
                error = "任务请求缺少出发队伍。";
                return false;
            }

            var unique = new HashSet<PersonId>();
            for (int i = 0; i < participants.Count; i++)
            {
                PersonId participant = participants[i];
                if (participant.IsEmpty)
                {
                    error = "任务队伍包含空人物 ID。";
                    return false;
                }
                if (!unique.Add(participant))
                {
                    error = "任务队伍包含重复人物。";
                    return false;
                }
            }
            if (!unique.Contains(captainId))
            {
                error = "任务队长必须属于出发队伍。";
                return false;
            }
            if (kind == MissionKind.TacticalInfiltration && participants.Count != 2)
            {
                error = "潜入任务必须恰好有两名出发成员。";
                return false;
            }
            if (kind == MissionKind.ShadowEscort && participants.Count != 1)
            {
                error = "护送委托必须恰好有一名执行人。";
                return false;
            }

            var risk = new HashSet<PersonId>();
            if (disclosedRiskParticipants != null)
            {
                for (int i = 0; i < disclosedRiskParticipants.Count; i++)
                {
                    PersonId disclosed = disclosedRiskParticipants[i];
                    if (!unique.Contains(disclosed))
                    {
                        error = "死亡风险名单包含未出发的人物。";
                        return false;
                    }
                    if (!risk.Add(disclosed))
                    {
                        error = "死亡风险名单重复列出同一人物。";
                        return false;
                    }
                }
            }
            if (kind == MissionKind.TacticalInfiltration &&
                disclosedRiskParticipants != null &&
                disclosedRiskParticipants.Count != participants.Count)
            {
                error = "M06 简报必须逐名披露全部出发成员的严重失败死亡风险。";
                return false;
            }
            if (kind == MissionKind.ShadowEscort && risk.Count > 0)
            {
                error = "M05 护送委托不会造成死亡，不应披露死亡风险名单。";
                return false;
            }

            if (firstNinjutsu < 0 || firstNinjutsu > 100 ||
                secondNinjutsu < 0 || secondNinjutsu > 100 ||
                frozenIntelligence < 0 || frozenIntelligence > 100 ||
                difficulty < 0 || difficulty > 100 ||
                baseSupplyCost < 0 || baseSupplyCost > 99 ||
                preparationSupplyCost < 0 || preparationSupplyCost > 99 ||
                score < -1000 || score > 1000)
            {
                error = "任务请求的冻结数值超出允许范围。";
                return false;
            }

            if (kind == MissionKind.TacticalInfiltration)
            {
                int expectedBonus = TacticalMissionResolutionRules.PreparationBonus(preparation);
                int expectedCost = TacticalMissionResolutionRules.PreparationSupplyCost(preparation);
                if (baseSupplyCost != TacticalMissionResolutionRules.BaseSupplyCost)
                {
                    error = "M06 基础物资成本必须是 " +
                            TacticalMissionResolutionRules.BaseSupplyCost + "。";
                    return false;
                }
                if (preparationSupplyCost != expectedCost)
                {
                    error = "准备成本与准备选择不一致：应为 " + expectedCost + "。";
                    return false;
                }
                if (difficulty != TacticalMissionResolutionRules.Difficulty)
                {
                    error = "M06 难度必须是 " + TacticalMissionResolutionRules.Difficulty + "。";
                    return false;
                }
                if (mode == MissionOperationMode.Manual)
                {
                    // Manual play resolves the terminal result from the tactical board, so
                    // no random check is frozen.
                    if (roll != 0 || score != 0)
                    {
                        error = "手动潜入不应冻结自动检定随机值或分数。";
                        return false;
                    }
                }
                else if (roll < 1 || roll > TacticalMissionResolutionRules.DieRollExclusiveMax)
                {
                    error = "M06 自动检定随机值必须是 1–100。";
                    return false;
                }
                else
                {
                    int expectedScore = TacticalMissionResolutionRules.Score(
                        firstNinjutsu, secondNinjutsu, frozenIntelligence, expectedBonus, roll);
                    if (score != expectedScore)
                    {
                        error = "M06 冻结分数与批准公式不一致。";
                        return false;
                    }
                }
            }
            else
            {
                if (baseSupplyCost != 0 || preparationSupplyCost != 0 ||
                    preparation != MissionPreparation.None)
                {
                    error = "M05 护送委托没有准备选项或额外交付物资。";
                    return false;
                }
                if (roll < 0 || roll >= ShadowEscortCommission.RollExclusiveMax)
                {
                    error = "M05 检定随机值必须在 0..99。";
                    return false;
                }
            }

            var participantsCopy = new PersonId[participants.Count];
            for (int i = 0; i < participants.Count; i++)
                participantsCopy[i] = participants[i];
            var riskCopy = disclosedRiskParticipants == null
                ? new PersonId[0]
                : new PersonId[disclosedRiskParticipants.Count];
            if (disclosedRiskParticipants != null)
                for (int i = 0; i < disclosedRiskParticipants.Count; i++)
                    riskCopy[i] = disclosedRiskParticipants[i];

            request = new MissionRequest(runId, missionId, kind, mode, preparation, date,
                captainId, Array.AsReadOnly(participantsCopy), Array.AsReadOnly(riskCopy),
                firstNinjutsu, secondNinjutsu, frozenIntelligence, difficulty, baseSupplyCost,
                preparationSupplyCost, roll, score);
            return true;
        }

        public static bool IsStableRunId(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId) || runId.Length > MaximumRunIdLength)
                return false;
            for (int i = 0; i < runId.Length; i++)
            {
                char c = runId[i];
                bool legal = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                             (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_' ||
                             c == ':';
                if (!legal)
                    return false;
            }
            return true;
        }

        public string BuildBriefingLine()
        {
            string line = MissionOutcomeTable.KindName(Kind) + "｜" +
                          MissionOutcomeTable.ModeName(Mode) + "｜队员 " +
                          Participants.Count.ToString(CultureInfo.InvariantCulture) + " 人";
            if (Kind == MissionKind.TacticalInfiltration)
            {
                line += "｜准备：" + MissionOutcomeTable.PreparationName(Preparation) +
                        "（额外物资 " + PreparationSupplyCost.ToString(CultureInfo.InvariantCulture) + "）" +
                        "｜基础物资 " + BaseSupplyCost.ToString(CultureInfo.InvariantCulture);
            }
            return line;
        }
    }
}
