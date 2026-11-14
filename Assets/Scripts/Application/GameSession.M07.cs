using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// M07 shared three-layer settlement: one frozen mission request, one shared result,
    /// one terminal settlement for manual and delegated play, family standing, injury,
    /// death disclosure, mission history and succession on a head's death.
    /// </summary>
    public sealed partial class GameSession
    {
        /// <summary>Stable run ID of the single M05 escort commission per campaign.</summary>
        public const string CommissionRunId = "run.m05.shadow-escort";
        /// <summary>Stable run ID of the single M06 infiltration per campaign.</summary>
        public const string TacticalRunId = "run.m06.intercepted-dispatch";

        private FamilyStandingState _standing;
        private MissionInjuryState _injuries;
        private MissionHistoryState _missionHistory;
        private MissionRequest _tacticalRequest;

        /// <summary>M07 family standing (fame, concealment, stable relations).</summary>
        public FamilyStandingState Standing => EnsureStanding();

        public int FamilyFame => EnsureStanding().Fame;
        public int Concealment => EnsureStanding().Concealment;
        public int RelationValue(string relationId) => EnsureStanding().GetRelation(relationId);

        /// <summary>Read-only terminal mission history in stable append order.</summary>
        public IReadOnlyList<MissionRecord> MissionHistory => EnsureMissionHistory().Records;

        public bool IsPersonInjured(PersonId personId) => EnsureInjuries().IsInjured(personId);

        internal void SetInjuredForTest(PersonId personId, bool injured) =>
            EnsureInjuries().TrySetInjured(personId, injured, out _);

        /// <summary>M05 frozen operation mode; shown in the briefing.</summary>
        public MissionOperationMode CommissionMode => _commission.Mode;

        public bool IsCommissionDue =>
            _commission.Stage == ShadowEscortStage.InProgress &&
            _commission.ElapsedDays >= _commission.DurationDaysValue;

        public bool IsCommissionManualSettlementAvailable =>
            IsCommissionDue && _commission.Mode == MissionOperationMode.Manual;

        /// <summary>
        /// A delegated row normally settles on its due day. If that settlement fails, the
        /// row stays <see cref="ShadowEscortStage.InProgress"/> at the completed window;
        /// this retry re-enters the same shared settlement with the same frozen run ID,
        /// start date and mode instead of waiting for the next day advance.
        /// </summary>
        public bool IsCommissionDelegatedSettlementRetryAvailable =>
            IsCommissionDue && _commission.Mode == MissionOperationMode.Delegated;

        /// <summary>
        /// Player-facing label for a stored stable relation ID. Persistence, save
        /// validation and mission records keep the stable ID; only display text changes.
        /// </summary>
        public static string RelationDisplayName(string relationId)
        {
            if (relationId == MissionRelationIds.ShadowEscortMerchant)
                return "尾张商队信用";
            if (relationId == MissionRelationIds.InterceptedDispatchTarget)
                return "截获密令目标势力";
            return "未知关系";
        }

        private MissionHistorySaveEntry[] CaptureMissionHistory()
        {
            IReadOnlyList<MissionRecord> records = EnsureMissionHistory().Records;
            var ordered = new List<MissionRecord>(records);
            // Settlement order is not guaranteed to be chronological when one mission
            // was launched before another was settled. The save contract is a
            // date-ordered list, so sort by date and then stable run ID.
            ordered.Sort((a, b) =>
            {
                int byDate = a.Date.CompareTo(b.Date);
                return byDate != 0 ? byDate : string.CompareOrdinal(a.RunId, b.RunId);
            });
            var entries = new MissionHistorySaveEntry[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
                entries[i] = MissionHistorySaveEntry.Capture(ordered[i]);
            return entries;
        }

        private static bool TryRestoreMissionHistory(MissionHistorySaveEntry[] entries,
            SuccessionSaveData succession, out MissionHistoryState state, out string error)
        {
            state = null;
            error = string.Empty;
            if (entries == null)
            {
                error = "存档缺少任务终局历史（missionHistory）。";
                return false;
            }
            var restored = new MissionHistoryState();
            var knownPersons = new HashSet<string>(StringComparer.Ordinal);
            if (succession != null && succession.persons != null)
                foreach (SuccessionPersonSaveEntry person in succession.persons)
                    if (person != null)
                        knownPersons.Add(person.personId);
            GameDate previousDate = default;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].TryRestore(out MissionRecord record, out string recordError))
                {
                    error = recordError;
                    return false;
                }
                if (!restored.TryAppend(record, out string appendError))
                {
                    error = appendError;
                    return false;
                }
                if (previousDate.IsValid && record.Date < previousDate)
                {
                    error = "存档任务终局历史未按日期排序。";
                    return false;
                }
                previousDate = record.Date;
                for (int p = 0; p < record.Participants.Count; p++)
                    if (!knownPersons.Contains(record.Participants[p].Value))
                    {
                        error = "存档任务终局历史引用了未登记人物：" +
                                record.Participants[p].Value + "。";
                        return false;
                    }
            }
            state = restored;
            return true;
        }

        private FamilyStandingState EnsureStanding()
        {
            if (_standing == null)
                _standing = FamilyStandingState.CreateInitial();
            return _standing;
        }

        private MissionInjuryState EnsureInjuries()
        {
            if (_injuries == null)
                _injuries = MissionInjuryState.TryCreate(null, out MissionInjuryState created, out _)
                    ? created
                    : new MissionInjuryState();
            return _injuries;
        }

        private MissionHistoryState EnsureMissionHistory()
        {
            if (_missionHistory == null)
                _missionHistory = new MissionHistoryState();
            return _missionHistory;
        }

        /// <summary>
        /// Player-visible mode choice for M05. It is frozen once execution starts, so the
        /// same reward/consequence contract applies to either entry point.
        /// </summary>
        public bool TrySetCommissionMode(MissionOperationMode mode, out string error)
        {
            bool changed = _commission.TrySetMode(mode, out error);
            if (changed)
            {
                LastCommissionMessage = mode == MissionOperationMode.Delegated
                    ? "已选择自动委派：委托到期时将自动结算。"
                    : "已选择亲自执行：委托到期后请点击「结算委托」。";
                SessionStateChanged?.Invoke();
            }
            return changed;
        }

        /// <summary>M05 manual terminal: the player clicks settle only after the due date.</summary>
        public MissionSettlementResult TrySettleCommissionManually()
        {
            if (_commission.Stage != ShadowEscortStage.InProgress)
                return MissionSettlementResult.Failure("只有执行中的委托才能结算。");
            if (!IsCommissionDue)
                return MissionSettlementResult.Failure(
                    "委托尚未到期：还需 " + CommissionRemainingDays + " 天。");
            if (_commission.Mode != MissionOperationMode.Manual)
                return MissionSettlementResult.Failure(
                    "该委托已选择自动委派：到期时会自动结算一次。");
            MissionSettlementResult result = SettleCommission(CurrentGameDate,
                MissionOperationMode.Manual);
            if (!result.Succeeded)
                LastCommissionMessage = result.Message;
            return result;
        }

        /// <summary>
        /// Delegated retry for a due row whose automatic settlement failed. It runs the
        /// same shared, idempotent settlement as the due-day call, keeps the frozen
        /// delegated mode and run ID, and therefore records the canonical due date once.
        /// </summary>
        public MissionSettlementResult TryRetryCommissionDelegated()
        {
            if (_commission.Stage != ShadowEscortStage.InProgress)
                return MissionSettlementResult.Failure("只有执行中的委托才能结算。");
            if (!IsCommissionDue)
                return MissionSettlementResult.Failure(
                    "委托尚未到期：还需 " + CommissionRemainingDays + " 天。");
            if (_commission.Mode != MissionOperationMode.Delegated)
                return MissionSettlementResult.Failure(
                    "该委托选择亲自执行：请在到期后点击「结算委托」。");
            MissionSettlementResult result = SettleCommission(CurrentGameDate,
                MissionOperationMode.Delegated);
            if (!result.Succeeded)
                LastCommissionMessage = result.Message;
            return result;
        }

        /// <summary>
        /// Delegated M06 entry. It freezes the same briefing the manual path uses, rolls
        /// one reproducible d100 from the Missions stream and commits through the same
        /// settlement as a manual terminal.
        /// </summary>
        public TacticalActionResult TryDelegateTacticalMission(PersonId first, PersonId second,
            PersonId captain, MissionPreparation preparation)
        {
            string error = ValidateTacticalStart(first, second, captain, preparation);
            if (!string.IsNullOrEmpty(error))
                return TacticalReject(error);

            PersonId[] ordered = OrderTacticalOperatives(first, second);
            PersonId orderedCaptain = captain.IsEmpty ? ordered[0] : captain;
            if (orderedCaptain != ordered[0] && orderedCaptain != ordered[1])
                return TacticalReject("潜入队长必须是两名出发成员之一。");

            EnsureDevelopment();
            if (!_development.TryGetValues(ordered[0], out CharacterGrowthValues firstValues) ||
                !_development.TryGetValues(ordered[1], out CharacterGrowthValues secondValues))
                return TacticalReject("出发成员缺少成长记录，无法自动检定。");

            EnsureRandomState();
            DeterministicRandomStream stream =
                _randomStateManager.GetStream(RandomStreamKind.Missions);
            RandomStateSaveData rngBefore = _randomStateManager.CaptureState();
            if (!stream.TryNextInt(TacticalMissionResolutionRules.DieRollExclusiveMax,
                    out int zeroBasedRoll))
                return TacticalReject("潜入检定随机流不可用。");
            int roll = zeroBasedRoll + 1;
            int bonus = TacticalMissionResolutionRules.PreparationBonus(preparation);
            int score = TacticalMissionResolutionRules.Score(firstValues.Ninjutsu,
                secondValues.Ninjutsu, FrozenMissionIntelligence(), bonus, roll);
            SharedMissionOutcome outcome = TacticalMissionResolutionRules.Classify(score);

            var participants = new[] { ordered[0], ordered[1] };
            var risk = new[] { ordered[0], ordered[1] };
            if (!MissionRequest.TryCreate(TacticalRunId,
                    FamilyChronicleEntry.TacticalMissionId, MissionKind.TacticalInfiltration,
                    MissionOperationMode.Delegated, preparation, CurrentGameDate, orderedCaptain,
                    participants, risk, firstValues.Ninjutsu, secondValues.Ninjutsu,
                    FrozenMissionIntelligence(),
                    TacticalMissionResolutionRules.Difficulty,
                    TacticalMissionResolutionRules.BaseSupplyCost,
                    TacticalMissionResolutionRules.PreparationSupplyCost(preparation),
                    roll, score, out MissionRequest request, out string requestError))
            {
                RestoreMissionRandom(rngBefore);
                return TacticalReject(string.IsNullOrEmpty(requestError)
                    ? "潜入请求无效。"
                    : requestError);
            }

            int rewardFunds = IsTacticalSuccess(outcome)
                ? TacticalMissionResolutionRules.SuccessFunds
                : 0;
            int rewardIntelligence = IsTacticalSuccess(outcome)
                ? TacticalMissionResolutionRules.SuccessIntelligence
                : 0;
            // The chronicle source is the resolved mission action, not the entry point:
            // a manual terminal and a delegated check with the same frozen input must
            // write identical chronicle data. Mode stays in the mission record.
            string chronicleSourceId = TacticalOutcomeActionId(TacticalOutcomeFor(outcome));
            if (!TryCommitMissionSettlement(request, outcome, rewardFunds, 0, rewardIntelligence,
                    CurrentGameDate, chronicleSourceId, (int)TacticalMissionStage.InProgress,
                    TacticalChronicleStageFor(outcome), out MissionRecord record,
                    out string commitError))
            {
                RestoreMissionRandom(rngBefore);
                return TacticalReject(string.IsNullOrEmpty(commitError)
                    ? "潜入结果结算失败。"
                    : commitError);
            }

            LastTacticalMessage = BuildDelegatedTacticalMessage(request, outcome, record);
            TacticalAppliedFunds = record.FundsDelta;
            TacticalAppliedSupplies = record.SuppliesDelta;
            TacticalAppliedIntelligence = record.IntelligenceDelta;
            SessionStateChanged?.Invoke();
            return new TacticalActionResult(true, TacticalStageFor(outcome),
                TacticalOutcomeFor(outcome), LastTacticalMessage, 0, LastTacticalMessage);
        }

        /// <summary>
        /// The briefing freezes the approved intelligence contribution, not the raw pool.
        /// The score rule counts at most
        /// <see cref="TacticalMissionResolutionRules.IntelligenceMaximumBonus"/> points, and
        /// the request range accepts 0..100, while a legal resource pool can hold far more.
        /// Freezing the raw pool would refuse to start a mission from a rich save and would
        /// claim the run used intelligence it never counted.
        /// </summary>
        private int FrozenMissionIntelligence() => Math.Clamp(
            EnsureIndustryState().Resources.Intelligence, 0,
            TacticalMissionResolutionRules.IntelligenceMaximumBonus);

        private string ValidateTacticalStart(PersonId first, PersonId second, PersonId captain,
            MissionPreparation preparation)
        {
            EnsureSuccession();
            EnsureIndustryState();
            if (IsTacticalMissionActive)
                return "潜入正在进行中。";
            if (IsTacticalMissionCompleted)
                return "此次潜入已经结束，不能重复进行。";
            if (CurrentLocation != LocationId.Kiyosu)
                return "请返回清洲本家，从城镇菜单开始潜入。";
            if (first.IsEmpty || second.IsEmpty || first == second)
                return "必须选择两名不同的在世家人。";
            if (!Enum.IsDefined(typeof(MissionPreparation), preparation))
                return "潜入准备选项无效。";
            if (captain.IsEmpty || (captain != first && captain != second))
                return "潜入队长必须是两名出发成员之一。";

            CommissionAssigneeCheck firstCheck = CheckTacticalOperative(first);
            if (!firstCheck.Eligible)
                return firstCheck.Message;
            CommissionAssigneeCheck secondCheck = CheckTacticalOperative(second);
            if (!secondCheck.Eligible)
                return secondCheck.Message;

            int required = TacticalMissionResolutionRules.RequiredStartingSupplies(preparation);
            if (EnsureIndustryState().Resources.Supplies < required)
                return "物资不足：潜入需要至少 " + required + " 物资（" +
                       TacticalMissionResolutionRules.BaseSupplyCost + " 基础" +
                       (TacticalMissionResolutionRules.PreparationSupplyCost(preparation) > 0
                           ? "＋1 侦察准备"
                           : string.Empty) +
                       "，结束时一次扣除）。";
            return string.Empty;
        }

        /// <summary>Restores the Missions stream so a failed attempt can retry the same input.</summary>
        private void RestoreMissionRandom(RandomStateSaveData snapshot) =>
            TryRestoreRandomState(snapshot);

        /// <summary>
        /// Canonical terminal date of the running commission: its start date plus the fixed
        /// execution window. Operation mode changes only how the player interacts with the
        /// run, so a manual settlement clicked after the due date records the same
        /// mission-history and chronicle date as delegated auto-settlement. The simulated
        /// date stays the fallback when the saved row cannot describe a due date.
        /// </summary>
        private GameDate CommissionDueDate(GameDate fallback)
        {
            if (!_commission.StartDate.IsValid || _commission.DurationDaysValue <= 0)
                return fallback;
            return GameDate.TryAddDays(_commission.StartDate, _commission.DurationDaysValue,
                out GameDate due)
                ? due
                : fallback;
        }

        private static bool IsTacticalSuccess(SharedMissionOutcome outcome) =>
            outcome == SharedMissionOutcome.FullSuccess ||
            outcome == SharedMissionOutcome.CostlySuccess;

        private static TacticalMissionStage TacticalStageFor(SharedMissionOutcome outcome)
        {
            switch (outcome)
            {
                case SharedMissionOutcome.FullSuccess:
                case SharedMissionOutcome.CostlySuccess:
                    return TacticalMissionStage.Succeeded;
                case SharedMissionOutcome.SafeFailure:
                    return TacticalMissionStage.Withdrawn;
                default:
                    return TacticalMissionStage.Failed;
            }
        }

        private static TacticalMissionOutcome TacticalOutcomeFor(SharedMissionOutcome outcome)
        {
            switch (outcome)
            {
                case SharedMissionOutcome.FullSuccess:
                case SharedMissionOutcome.CostlySuccess:
                    return TacticalMissionOutcome.Succeeded;
                case SharedMissionOutcome.SafeFailure:
                    return TacticalMissionOutcome.Withdrawn;
                default:
                    return TacticalMissionOutcome.Failed;
            }
        }

        private static int TacticalChronicleStageFor(SharedMissionOutcome outcome) =>
            (int)TacticalStageFor(outcome);

        private string BuildDelegatedTacticalMessage(MissionRequest request,
            SharedMissionOutcome outcome, MissionRecord record)
        {
            return "自动委派结算：" + MissionOutcomeTable.OutcomeName(outcome) +
                   "｜检定 " + request.Score.ToString(CultureInfo.InvariantCulture) +
                   "（忍术 " + request.FirstNinjutsu + "／" + request.SecondNinjutsu +
                   "，情报 " + request.FrozenIntelligence + "，准备加值 " +
                   TacticalMissionResolutionRules.PreparationBonus(request.Preparation) +
                   "，d100 " + request.Roll + "，难度 " +
                   TacticalMissionResolutionRules.Difficulty + "）" +
                   "｜资金 " + Signed(record.FundsDelta) + "、物资 " +
                   Signed(record.SuppliesDelta) + "、情报 " + Signed(record.IntelligenceDelta) +
                   "｜名望 " + record.FameBefore + "→" + record.FameAfter +
                   "，隐蔽度 " + record.ConcealmentBefore + "→" + record.ConcealmentAfter + "。";
        }

        /// <summary>
        /// Closes an in-progress commission whose assignee can no longer execute it with
        /// the approved Abandoned semantics: one action chronicle row, no shared result,
        /// no standing, no reward, no investment refund and no run ID left behind.
        /// </summary>
        private bool TryAbandonCommission(GameDate date, string reason)
        {
            if (_commission.Stage != ShadowEscortStage.InProgress)
                return false;
            ShadowEscortStage before = _commission.Stage;
            if (!_commission.TryAbandon(reason, out ShadowEscortActionResult abandoned))
                return false;
            LastCommissionMessage = abandoned.Feedback;
            RecordMission(date, FamilyChronicleEntry.ShadowEscortCommissionId,
                "abandon", "护送委托", (int)before, (int)abandoned.Stage, true,
                abandoned.Feedback);
            return true;
        }

        /// <summary>
        /// One settlement entrance for both M05 paths. It freezes the request from saved
        /// commission state, rolls exactly one Missions value, applies the existing
        /// commission band, then commits the shared M07 consequences. The terminal record
        /// and chronicle use the commission's due date, so a manual click made after the
        /// due date records the same date as delegated auto-settlement.
        /// </summary>
        private MissionSettlementResult SettleCommission(GameDate simulatedDate,
            MissionOperationMode mode)
        {
            EnsureRandomState();
            EnsureDevelopment();
            CommissionAssigneeCheck check = CheckCommissionAssignee(_commission.AssigneeId);
            if (!check.Eligible)
            {
                // A running commission can outlive its assignee's ability to work: the
                // runtime rule deliberately lets a retired member finish the trip, so the
                // due date is the first place settlement notices. Close the row once with
                // the approved Abandoned semantics instead of failing every retry forever.
                if (!TryAbandonCommission(simulatedDate, check.Message))
                    return MissionSettlementResult.Failure(check.Message);
                return MissionSettlementResult.Failure(LastCommissionMessage);
            }
            if (!_development.TryGetValues(_commission.AssigneeId,
                    out CharacterGrowthValues abilities))
                return MissionSettlementResult.Failure(
                    "委托执行人缺少成长记录：委托未结算，请检查存档。");

            DeterministicRandomStream stream =
                _randomStateManager.GetStream(RandomStreamKind.Missions);
            RandomStateSaveData rngBefore = _randomStateManager.CaptureState();
            if (!stream.TryNextInt(ShadowEscortCommission.RollExclusiveMax, out int roll))
                return MissionSettlementResult.Failure("委托检定随机流不可用：委托未结算。");

            ShadowEscortCommission commissionBefore = _commission.Clone();
            IndustryResources resourcesBefore = EnsureIndustryState().Resources;
            ShadowEscortActionResult resolved = _commission.TryResolve(abilities, roll,
                CurrentFamilyCreed, resourcesBefore, out _);
            if (!resolved.Succeeded)
            {
                _commission = commissionBefore;
                RestoreMissionRandom(rngBefore);
                return MissionSettlementResult.Failure(resolved.Feedback);
            }

            if (!ShadowEscortOutcomeRules.TryMap(_commission.Outcome,
                    out SharedMissionOutcome outcome, out string mapError))
            {
                _commission = commissionBefore;
                RestoreMissionRandom(rngBefore);
                return MissionSettlementResult.Failure(mapError);
            }

            if (!TryBuildCommissionRequest(mode, roll, _commission.Score,
                    out MissionRequest request, out string requestError))
            {
                _commission = commissionBefore;
                RestoreMissionRandom(rngBefore);
                return MissionSettlementResult.Failure(requestError);
            }

            int chronicleToStage = (int)_commission.Stage;
            GameDate terminalDate = CommissionDueDate(simulatedDate);
            if (!TryCommitMissionSettlement(request, outcome, _commission.RewardFunds,
                    _commission.RewardSupplies, 0, terminalDate, "settle",
                    (int)ShadowEscortStage.InProgress, chronicleToStage,
                    out MissionRecord record, out string commitError))
            {
                _commission = commissionBefore;
                RestoreMissionRandom(rngBefore);
                return MissionSettlementResult.Failure(commitError);
            }

            LastCommissionMessage = _commission.LastFeedback + " " + BuildSettlementTail(record);
            SessionStateChanged?.Invoke();
            return MissionSettlementResult.Success(record, LastCommissionMessage);
        }

        private bool TryBuildCommissionRequest(MissionOperationMode mode, int roll, int score,
            out MissionRequest request, out string error)
        {
            request = null;
            error = string.Empty;
            if (string.IsNullOrEmpty(_commission.RunId))
            {
                if (!_commission.TryAssignRunId(CommissionRunId, out string runError))
                {
                    error = string.IsNullOrEmpty(runError) ? "委托运行 ID 分配失败。" : runError;
                    return false;
                }
            }
            var participants = new[] { _commission.AssigneeId };
            return MissionRequest.TryCreate(_commission.RunId,
                FamilyChronicleEntry.ShadowEscortCommissionId, MissionKind.ShadowEscort, mode,
                MissionPreparation.None, _commission.StartDate, _commission.AssigneeId,
                participants, new PersonId[0], 0, 0, 0, 0, 0, 0, roll, score,
                out request, out error);
        }

        /// <summary>
        /// The single transactional settlement. Every resource, standing, life, random
        /// stream, chronicle and history change is prepared first, then committed, and any
        /// failure restores the exact prior snapshot so the same run ID can retry.
        /// </summary>
        private bool TryCommitMissionSettlement(MissionRequest request,
            SharedMissionOutcome outcome, int rewardFunds, int rewardSupplies,
            int rewardIntelligence, GameDate date, string chronicleSourceId,
            int chronicleFromStage, int chronicleToStage, out MissionRecord record,
            out string error)
        {
            record = null;
            error = string.Empty;
            if (request == null)
            {
                error = "任务请求为空。";
                return false;
            }
            MissionHistoryState history = EnsureMissionHistory();
            if (history.Contains(request.RunId))
            {
                error = "任务运行 ID 已结算，不能重复提交：" + request.RunId + "。";
                return false;
            }
            if (!MissionOutcomeTable.TryGetEffects(request.Kind, outcome,
                    out MissionOutcomeEffect effect, out string effectError))
            {
                error = effectError;
                return false;
            }

            IndustryState industryBefore = EnsureIndustryState().Clone();
            FamilyStandingState standingBefore = EnsureStanding().Clone();
            MissionInjuryState injuriesBefore = EnsureInjuries().Clone();
            MissionHistoryState historyBefore = history.Clone();
            ShadowEscortCommission commissionBefore = _commission.Clone();
            EnsureSuccession();
            SuccessionSaveData successionBefore = SuccessionSaveData.Capture(_succession);
            DailyActionState dailyBefore = EnsureDailyActions().Clone();
            NpcAutonomyState autonomyBefore = CloneNpcAutonomy();
            RandomStateSaveData rngBefore = _randomStateManager.CaptureState();
            FamilyChronicleEntry[] ledgerBefore = EnsureHistoryLedger().Capture();
            string commissionMessageBefore = LastCommissionMessage;
            string tacticalMessageBefore = LastTacticalMessage;
            string successionMessageBefore = LastSuccessionMessage;

            // The transaction opens before the first mutation and before the participant
            // planning, because a severe failure draws its death checks from the Missions
            // stream there. Every later failure, including resource, standing or chronicle
            // preparation failures, therefore restores the stream with the rest of the
            // snapshot instead of leaving consumed draws for the retry.
            bool applied = false;
            try
            {
                IndustryResources before = industryBefore.Resources;
                int supplyCost = request.TotalSupplyCost + effect.ExtraSupplyCost;
                if (!TryCreateMissionResources(before, rewardFunds, rewardSupplies - supplyCost,
                        rewardIntelligence, out IndustryResources after, out error))
                    return false;

                var relationDeltas = new[]
                {
                    new MissionRelationDelta(effect.RelationId, effect.RelationDelta)
                };
                FamilyStandingState standingPreview = standingBefore.Clone();
                if (!standingPreview.TryApplyDeltas(effect.FameDelta, effect.ConcealmentDelta,
                        relationDeltas, out MissionStandingResult standing,
                        out string standingError))
                {
                    error = standingError;
                    return false;
                }

                List<MissionParticipantState> states;
                List<PersonId> deaths;
                if (!TryPlanParticipantStates(request, outcome, out states, out deaths,
                        out string lifeError))
                {
                    error = lifeError;
                    return false;
                }

                string summary = BuildMissionSummary(request, outcome, date, after, before,
                    supplyCost, rewardFunds, rewardSupplies, rewardIntelligence, standing, states);
                HistoryLedger ledger = EnsureHistoryLedger();
                if (!TryPrepareSharedMissionChronicle(ledger.Count + 1, date, request.MissionId,
                        chronicleSourceId, chronicleFromStage, chronicleToStage,
                        request.Kind == MissionKind.TacticalInfiltration ? "潜入密令" : "护送委托",
                        summary, out FamilyChronicleEntry entry, out string chronicleError))
                {
                    error = chronicleError;
                    return false;
                }

                if (!EnsureIndustryState().TrySetResources(after, out string resourceError))
                {
                    error = string.IsNullOrEmpty(resourceError) ? "结算资源无效。" : resourceError;
                    return false;
                }
                EnsureStanding().CopyFrom(standingPreview);
                for (int i = 0; i < states.Count; i++)
                {
                    MissionParticipantState state = states[i];
                    if (!EnsureInjuries().TrySetInjured(state.PersonId, state.InjuredAfter,
                            out string injuryError))
                    {
                        error = injuryError;
                        return false;
                    }
                }

                if (request.Kind == MissionKind.TacticalInfiltration)
                    TacticalChronicleAppendFaultInjection?.Invoke(ledger);
                if (!ledger.TryAppendPrepared(entry, out string appendError))
                {
                    error = "任务结果未能写入家族编年史：" +
                            (string.IsNullOrEmpty(appendError) ? "未知原因" : appendError);
                    return false;
                }

                if (!TryApplyDeaths(request, deaths, date, out string deathError))
                {
                    error = deathError;
                    return false;
                }

                MissionRecord prepared;
                var relationChanges = new List<MissionStandingChange>(standing.RelationChanges);
                if (!MissionRecord.TryCreate(request.RunId, request.MissionId, request.Kind,
                        request.Mode, date, outcome, request.CaptainId, request.Participants,
                        request.DisclosedRiskParticipants, request.Preparation,
                        request.FirstNinjutsu, request.SecondNinjutsu,
                        request.FrozenIntelligence, request.Difficulty, request.Roll,
                        request.Score, standing.FameBefore, standing.FameAfter,
                        standing.ConcealmentBefore, standing.ConcealmentAfter,
                        relationChanges.AsReadOnly(), after.Funds - before.Funds,
                        after.Supplies - before.Supplies,
                        after.Intelligence - before.Intelligence, states.AsReadOnly(),
                        entry.EntryId, out prepared, out string recordError))
                {
                    error = recordError;
                    return false;
                }
                if (!history.TryAppend(prepared, out string historyError))
                {
                    error = historyError;
                    return false;
                }

                applied = true;
                record = prepared;
                return true;
            }
            finally
            {
                if (!applied)
                {
                    _industryState = industryBefore;
                    EnsureStanding().CopyFrom(standingBefore);
                    EnsureInjuries().CopyFrom(injuriesBefore);
                    EnsureMissionHistory().CopyFrom(historyBefore);
                    _commission = commissionBefore;
                    // Rollback is reported, never assumed: every snapshot comes from the
                    // live session, so a rejected restore means the session is not back at
                    // the pre-attempt state and the caller must not treat it as clean.
                    var rollbackFailures = new List<string>(2);
                    if (!TryRestoreHistoryLedger(ledgerBefore))
                        rollbackFailures.Add("编年史快照");
                    if (!TryRestoreRandomState(rngBefore))
                        rollbackFailures.Add("随机状态快照");
                    if (successionBefore != null)
                    {
                        if (SuccessionSaveData.TryRestoreTransactionSnapshot(successionBefore,
                                out SuccessionState restoredSuccession,
                                out string successionRestoreError))
                            _succession = restoredSuccession;
                        else
                            rollbackFailures.Add("继承快照" +
                                (string.IsNullOrEmpty(successionRestoreError)
                                    ? string.Empty
                                    : "：" + successionRestoreError));
                    }
                    _dailyActions = dailyBefore;
                    _npcAutonomy = autonomyBefore;
                    LastCommissionMessage = commissionMessageBefore;
                    LastTacticalMessage = tacticalMessageBefore;
                    LastSuccessionMessage = successionMessageBefore;
                    if (rollbackFailures.Count > 0)
                        error = (string.IsNullOrEmpty(error) ? string.Empty : error + " ") +
                                "结算未完成且回滚不完整，请读取较早存档：" +
                                string.Join("、", rollbackFailures) + "。";
                }
            }
        }

        /// <summary>
        /// Applies the requested mission deltas to a resource pool. A reward that would push
        /// a pool past <see cref="IndustryResources.Maximum"/> is capped there instead of
        /// failing the settlement: a rich save must still be able to finish its mission, and
        /// the record carries the delta actually applied (zero at the ceiling). A cost that
        /// would push a pool below the floor still fails, because a silently free cost would
        /// break the announced price.
        /// </summary>
        private static bool TryCreateMissionResources(IndustryResources before, int fundsDelta,
            int suppliesDelta, int intelligenceDelta, out IndustryResources after,
            out string error)
        {
            error = string.Empty;
            after = before;
            long funds = (long)before.Funds + fundsDelta;
            long supplies = (long)before.Supplies + suppliesDelta;
            long intelligence = (long)before.Intelligence + intelligenceDelta;
            if (funds < IndustryResources.Minimum ||
                supplies < IndustryResources.Minimum ||
                intelligence < IndustryResources.Minimum)
            {
                error = "任务结算所需的资源变化超出允许范围。";
                return false;
            }
            if (funds > IndustryResources.Maximum) funds = IndustryResources.Maximum;
            if (supplies > IndustryResources.Maximum) supplies = IndustryResources.Maximum;
            if (intelligence > IndustryResources.Maximum)
                intelligence = IndustryResources.Maximum;
            if (!IndustryResources.TryCreate((int)funds, (int)supplies, (int)intelligence,
                    out after, out error))
            {
                error = string.IsNullOrEmpty(error) ? "任务结算资源无效。" : error;
                return false;
            }
            return true;
        }

        private bool TryPlanParticipantStates(MissionRequest request,
            SharedMissionOutcome outcome, out List<MissionParticipantState> states,
            out List<PersonId> deaths, out string error)
        {
            states = new List<MissionParticipantState>(request.Participants.Count);
            deaths = new List<PersonId>();
            error = string.Empty;
            EnsureSuccession();
            EnsureRandomState();
            DeterministicRandomStream stream = null;
            if (outcome == SharedMissionOutcome.SevereFailure &&
                request.DisclosedRiskParticipants.Count > 0)
                stream = _randomStateManager.GetStream(RandomStreamKind.Missions);

            for (int i = 0; i < request.Participants.Count; i++)
            {
                PersonId personId = request.Participants[i];
                bool aliveBefore = _succession.World.IsLiving(personId);
                bool injuredBefore = EnsureInjuries().IsInjured(personId);
                bool aliveAfter = aliveBefore;
                bool injuredAfter = injuredBefore;
                if (outcome == SharedMissionOutcome.SevereFailure &&
                    ContainsPerson(request.DisclosedRiskParticipants, personId))
                {
                    if (stream == null || !stream.TryNextInt(
                            TacticalMissionResolutionRules.DieRollExclusiveMax, out int dieRoll))
                    {
                        error = "严重失败的死亡检定随机流不可用。";
                        return false;
                    }
                    if (dieRoll < TacticalMissionResolutionRules.DeathCheckPercent)
                    {
                        aliveAfter = false;
                        injuredAfter = false;
                        deaths.Add(personId);
                    }
                    else
                    {
                        injuredAfter = true;
                    }
                }
                states.Add(new MissionParticipantState(personId, aliveBefore, injuredBefore,
                    aliveAfter, injuredAfter));
            }
            return true;
        }

        private bool TryApplyDeaths(MissionRequest request, IReadOnlyList<PersonId> deaths,
            GameDate date, out string error)
        {
            error = string.Empty;
            if (deaths == null || deaths.Count == 0)
                return true;
            EnsureSuccession();
            bool headDied = false;
            for (int i = 0; i < deaths.Count; i++)
            {
                PersonId personId = deaths[i];
                if (!_succession.World.TryGetLife(personId, out SuccessionPersonLife life))
                {
                    error = "死亡队员的生命状态缺失。";
                    return false;
                }
                if (!_succession.World.TrySetLife(personId,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                        out string lifeError))
                {
                    error = lifeError;
                    return false;
                }
                if (personId == _succession.HeadPersonId)
                    headDied = true;
            }

            if (headDied)
            {
                SuccessionResult transition =
                    SuccessionService.BeginDeathSuccession(_succession, date);
                if (!transition.Succeeded)
                {
                    error = transition.Message;
                    return false;
                }
                LastSuccessionMessage = transition.Message;
            }

            EnsureIndustryAssignmentsValid();
            ReconcileDailyActions();
            return true;
        }

        private static bool ContainsPerson(IReadOnlyList<PersonId> ids, PersonId personId)
        {
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == personId)
                    return true;
            return false;
        }

        private static bool TryPrepareSharedMissionChronicle(int sequence, GameDate date,
            string missionId, string sourceId, int fromStage, int toStage, string title,
            string summary, out FamilyChronicleEntry entry, out string error)
        {
            entry = null;
            string entryId = FamilyChronicleEntry.BuildEntryId(sequence);
            return FamilyChronicleEntry.TryCreate(sequence, entryId, date.Year, date.Month,
                ChronicleCategory.Mission, missionId, sourceId, title, summary, 0, 0,
                fromStage, toStage, 0, 0, null, null, out entry, out error);
        }

        private static string FormatDate(GameDate date) =>
            date.Year.ToString("0000", CultureInfo.InvariantCulture) + "-" +
            date.Month.ToString("00", CultureInfo.InvariantCulture) + "-" +
            date.Day.ToString("00", CultureInfo.InvariantCulture);

        private string BuildMissionSummary(MissionRequest request, SharedMissionOutcome outcome,
            GameDate settlementDate, IndustryResources after, IndustryResources before,
            int supplyCost, int rewardFunds, int rewardSupplies, int rewardIntelligence,
            MissionStandingResult standing, IReadOnlyList<MissionParticipantState> states)
        {
            string startText = FormatDate(request.Date);
            string settleText = FormatDate(settlementDate);
            var builder = new StringBuilder(160);
            // Keep the long-standing M05 chronicle stamp: the exact settlement day in
            // full-width parentheses, and the record's year/month describe that day.
            builder.Append("（").Append(settleText).Append("）");
            builder.Append(MissionOutcomeTable.KindName(request.Kind));
            builder.Append("｜").Append(MissionOutcomeTable.OutcomeName(outcome));
            if (request.Kind == MissionKind.TacticalInfiltration &&
                outcome == SharedMissionOutcome.SafeFailure)
                builder.Append("（撤退）");
            builder.Append("｜").Append(MissionOutcomeTable.ModeName(request.Mode));
            builder.Append("｜开始 ").Append(startText);
            if (request.Kind == MissionKind.TacticalInfiltration && _tacticalMission != null)
                builder.Append("｜回合 ").Append(_tacticalMission.Turn);
            if (request.Kind == MissionKind.TacticalInfiltration)
                builder.Append("｜准备：")
                    .Append(MissionOutcomeTable.PreparationName(request.Preparation));
            builder.Append("｜队长 ").Append(PersonNameOf(request.CaptainId));
            builder.Append("｜成员：");
            for (int i = 0; i < states.Count; i++)
            {
                if (i > 0) builder.Append("、");
                builder.Append(PersonNameOf(states[i].PersonId));
                if (states[i].AliveBefore && !states[i].AliveAfter)
                    builder.Append("（阵亡）");
                else if (states[i].InjuredAfter)
                    builder.Append("（负伤）");
            }
            builder.Append("｜资金 ").Append(Signed(after.Funds - before.Funds));
            builder.Append("、物资 ").Append(Signed(after.Supplies - before.Supplies));
            builder.Append("、情报 ").Append(Signed(after.Intelligence - before.Intelligence));
            builder.Append("（任务成本 ").Append(supplyCost).Append(" 物资");
            builder.Append("；奖励 资金 ").Append(Signed(rewardFunds))
                .Append("、物资 ").Append(Signed(rewardSupplies))
                .Append("、情报 ").Append(Signed(rewardIntelligence));
            builder.Append("）");
            builder.Append("｜名望 ").Append(standing.FameBefore).Append("→")
                .Append(standing.FameAfter);
            builder.Append("，隐蔽度 ").Append(standing.ConcealmentBefore).Append("→")
                .Append(standing.ConcealmentAfter);
            for (int i = 0; i < standing.RelationChanges.Count; i++)
            {
                MissionStandingChange change = standing.RelationChanges[i];
                builder.Append("，").Append(RelationDisplayName(change.RelationId)).Append(" ")
                    .Append(change.Before).Append("→").Append(change.After);
            }
            builder.Append("。");
            return builder.ToString();
        }

        private string PersonNameOf(PersonId personId)
        {
            EnsureSuccession();
            // A settled history row can name a person the current roster no longer holds
            // (for example after the crisis demo replaces the family). Chronicle text is
            // player-facing, so it must never fall back to the raw stable ID.
            return _succession != null && _succession.World != null &&
                   _succession.World.TryGetPerson(personId, out PersonState person)
                ? person.DisplayName
                : "已离开或不可用的家人";
        }

        private static string BuildSettlementTail(MissionRecord record)
        {
            return "实际结算：名望 " + record.FameBefore + "→" + record.FameAfter +
                   "，隐蔽度 " + record.ConcealmentBefore + "→" + record.ConcealmentAfter +
                   "，资金 " + Signed(record.FundsDelta) + "、物资 " +
                   Signed(record.SuppliesDelta) + "、情报 " +
                   Signed(record.IntelligenceDelta) + "。";
        }
    }

    /// <summary>Plain M07 settlement result for the panel and tests.</summary>
    public readonly struct MissionSettlementResult
    {
        public MissionSettlementResult(bool succeeded, string message,
            SharedMissionOutcome outcome, string runId, string chronicleEntryId,
            int fundsDelta, int suppliesDelta, int intelligenceDelta)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            Outcome = outcome;
            RunId = runId ?? string.Empty;
            ChronicleEntryId = chronicleEntryId ?? string.Empty;
            FundsDelta = fundsDelta;
            SuppliesDelta = suppliesDelta;
            IntelligenceDelta = intelligenceDelta;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        public SharedMissionOutcome Outcome { get; }
        public string RunId { get; }
        public string ChronicleEntryId { get; }
        public int FundsDelta { get; }
        public int SuppliesDelta { get; }
        public int IntelligenceDelta { get; }

        public static MissionSettlementResult Failure(string message) =>
            new MissionSettlementResult(false, message, SharedMissionOutcome.SafeFailure,
                string.Empty, string.Empty, 0, 0, 0);

        public static MissionSettlementResult Success(MissionRecord record, string message) =>
            new MissionSettlementResult(true, message, record.Outcome, record.RunId,
                record.ChronicleEntryId, record.FundsDelta, record.SuppliesDelta,
                record.IntelligenceDelta);
    }
}
