using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Stable lifecycle stage of the single M05 escort commission. Values are part of
    /// the save contract and must never be renumbered.
    /// </summary>
    public enum ShadowEscortStage
    {
        NotAccepted = 0,
        Accepted = 1,
        InProgress = 2,
        Succeeded = 3,
        PartialSuccess = 4,
        Failed = 5,
        /// <summary>M07: the run was closed because its assignee became unavailable; it has no shared result.</summary>
        Abandoned = 6
    }

    /// <summary>Stable settled band; <see cref="None"/> means the commission is unsettled.</summary>
    public enum CommissionOutcome
    {
        None = 0,
        Success = 1,
        Partial = 2,
        Failure = 3
    }

    /// <summary>Plain result value; never holds Unity objects.</summary>
    public readonly struct ShadowEscortActionResult
    {
        public ShadowEscortActionResult(bool succeeded, ShadowEscortStage stage,
            CommissionOutcome outcome, string feedback, int score = 0, int roll = 0,
            int fundsDelta = 0, int suppliesDelta = 0)
        {
            Succeeded = succeeded;
            Stage = stage;
            Outcome = outcome;
            Feedback = feedback ?? string.Empty;
            Score = score;
            Roll = roll;
            FundsDelta = fundsDelta;
            SuppliesDelta = suppliesDelta;
        }

        public bool Succeeded { get; }
        public ShadowEscortStage Stage { get; }
        public CommissionOutcome Outcome { get; }
        public string Feedback { get; }
        public int Score { get; }
        public int Roll { get; }
        public int FundsDelta { get; }
        public int SuppliesDelta { get; }
    }

    /// <summary>
    /// One hand-authored M05 commission: the clan escorts a merchant convoy through
    /// bandit country. The domain owns the whole lifecycle, but never advances time,
    /// never rolls randomness, and never touches resources on its own. The caller
    /// supplies the current date, the current resources and an already rolled value
    /// from the Missions stream, so settlement is a pure function of saved state.
    ///
    /// Rules:
    /// - Accepting at the clan seat invests funds once; the cost depends on the creed.
    /// - Exactly one living family member is assigned, and only when that person has no
    ///   active daily action and no industry assignment (personnel tradeoff).
    /// - Execution takes <see cref="DurationDays"/> game days of explicit time advance.
    /// - Settlement checks Martial Arts + Management + Social plus the creed modifier
    ///   plus the caller's roll (0..99); bands are success / partial / failure.
    /// </summary>
    public sealed class ShadowEscortCommission
    {
        public const int DurationDays = 3;
        public const int BaseInvestmentFunds = 4;
        public const int SecrecyInvestmentFunds = 2;
        public const int RewardSuccessFunds = 14;
        public const int RewardSuccessSupplies = 4;
        public const int RewardPartialFunds = 7;
        public const int RewardPartialSupplies = 1;
        public const int MeritFirstRewardFundsBonus = 6;
        public const int RighteousnessScoreBonus = 10;
        public const int MeritFirstScoreBonus = 4;
        public const int SuccessThreshold = 55;
        public const int PartialThreshold = 15;
        public const int RollExclusiveMax = 100;

        public ShadowEscortStage Stage { get; private set; }
        public CommissionOutcome Outcome { get; private set; } = CommissionOutcome.None;
        public PersonId AssigneeId { get; private set; }
        /// <summary>M07 stable run ID; empty until execution starts, then fixed for the run.</summary>
        public string RunId { get; private set; } = string.Empty;
        /// <summary>M07 chosen operation mode; frozen when execution starts.</summary>
        public MissionOperationMode Mode { get; private set; } = MissionOperationMode.Delegated;
        public int DurationDaysValue { get; private set; }
        public int ElapsedDays { get; private set; }
        public GameDate StartDate { get; private set; }
        public int InvestedFunds { get; private set; }
        public int Score { get; private set; }
        public int Roll { get; private set; }
        public int RewardFunds { get; private set; }
        public int RewardSupplies { get; private set; }
        public string LastFeedback { get; private set; } = string.Empty;

        public int RemainingDays => Math.Max(0, DurationDaysValue - ElapsedDays);
        public bool IsSettled => Stage == ShadowEscortStage.Succeeded ||
                                 Stage == ShadowEscortStage.PartialSuccess ||
                                 Stage == ShadowEscortStage.Failed;

        /// <summary>Upfront investment for one creed; the only creed that changes cost.</summary>
        public static int InvestmentFundsFor(FamilyCreedKind creed) =>
            creed == FamilyCreedKind.SecrecyFirst ? SecrecyInvestmentFunds : BaseInvestmentFunds;

        /// <summary>
        /// Accepts the offer at the clan seat. The caller passes the current industry
        /// resources so the investment is deducted exactly once, atomically.
        /// </summary>
        public ShadowEscortActionResult TryAccept(LocationId location, FamilyCreedKind creed,
            IndustryResources resources, out IndustryResources nextResources)
        {
            nextResources = resources;
            if (Stage != ShadowEscortStage.NotAccepted)
                return Result(false, "该委托已经领取过，不能重复接取。");
            if (location != LocationId.Kiyosu)
                return Result(false, "请返回清洲本家领取影之一族委托。");

            int cost = InvestmentFundsFor(creed);
            if (resources.Funds < cost)
                return Result(false, "资金不足：接取委托需要 " + cost + " 资金。");
            if (!IndustryResources.TryCreate(resources.Funds - cost, resources.Supplies,
                    resources.Intelligence, out nextResources, out string error))
            {
                nextResources = resources;
                return Result(false, string.IsNullOrEmpty(error) ? "资源变动无效。" : error);
            }

            Stage = ShadowEscortStage.Accepted;
            InvestedFunds = cost;
            return Result(true,
                "已接取影之一族护送委托：预付 " + cost + " 资金，请指定一名家人执行。");
        }

        /// <summary>
        /// Assigns exactly one free family member. Busy people (active daily action,
        /// industry assignment or in transit) are rejected: the commission competes
        /// with autonomous duties instead of silently running beside them.
        /// </summary>
        public ShadowEscortActionResult TryAssign(PersonId assigneeId, bool hasActiveDailyAction,
            bool isIndustryAssignee, bool hasKnownLocation, GameDate startDate)
        {
            if (Stage != ShadowEscortStage.Accepted)
                return Result(false, Stage == ShadowEscortStage.NotAccepted
                    ? "请先接取委托，再指定执行人。"
                    : "该委托已经指定执行人或已经结算。");
            if (assigneeId.IsEmpty)
                return Result(false, "必须指定一名在世的家族成员。");
            if (hasActiveDailyAction)
                return Result(false, "该人物有进行中的每日行动：请先完成或中断，再指派委托。");
            if (isIndustryAssignee)
                return Result(false, "该人物正在负责产业：请先改派产业负责人，再指派委托。");
            if (!hasKnownLocation)
                return Result(false, "该人物正在旅途中或地点未知，无法立刻开始委托。");
            if (!startDate.IsValid)
                return Result(false, "委托开始日期无效。");

            AssigneeId = assigneeId;
            DurationDaysValue = DurationDays;
            ElapsedDays = 0;
            StartDate = startDate;
            Stage = ShadowEscortStage.InProgress;
            return Result(true, "已指派家人执行护送委托，预计 " + DurationDays + " 天完成。");
        }

        /// <summary>
        /// Allocates the M07 run ID exactly when execution starts. It is refused on rows
        /// that are not executing and cannot replace an existing ID, so a settled run can
        /// never be re-keyed and double-settled.
        /// </summary>
        public bool TryAssignRunId(string runId, out string error)
        {
            error = string.Empty;
            if (Stage != ShadowEscortStage.InProgress)
            {
                error = "只有执行中的委托才能分配运行 ID。";
                return false;
            }
            if (!string.IsNullOrEmpty(RunId))
            {
                error = "该委托已经分配运行 ID。";
                return false;
            }
            if (!MissionRequest.IsStableRunId(runId))
            {
                error = "委托运行 ID 无效。";
                return false;
            }
            RunId = runId;
            return true;
        }

        /// <summary>
        /// Chooses how a not-yet-executing commission will settle. Repeating the same
        /// choice is a no-op; once execution starts the mode is frozen.
        /// </summary>
        public bool TrySetMode(MissionOperationMode mode, out string error)
        {
            error = string.Empty;
            if (!Enum.IsDefined(typeof(MissionOperationMode), mode) ||
                mode == MissionOperationMode.LegacyUnknown)
            {
                error = "委托执行方式无效。";
                return false;
            }
            if (Stage != ShadowEscortStage.NotAccepted && Stage != ShadowEscortStage.Accepted)
            {
                error = "委托已经开始执行，执行方式不能再更改。";
                return false;
            }
            Mode = mode;
            return true;
        }

        /// <summary>
        /// Advances the execution window by one explicit game day. Returns true when the
        /// row was advanced; <paramref name="due"/> reports that the window is complete
        /// and settlement is now required. A day after the due date never grows the
        /// window, so a retried settlement can never produce an invalid save row.
        /// </summary>
        public bool AdvanceOneDay(out bool due)
        {
            due = false;
            if (Stage != ShadowEscortStage.InProgress)
                return false;

            // The window never grows past its duration. A settlement that failed on the
            // due day leaves elapsed == duration, so retrying cannot create an invalid
            // row with elapsed > duration.
            if (ElapsedDays < DurationDaysValue)
                ElapsedDays++;
            due = ElapsedDays >= DurationDaysValue;
            return true;
        }

        /// <summary>
        /// Settles a due commission with the caller's rolled value. The attribute check
        /// reads the assignee's existing growth values; nothing is fabricated here.
        /// </summary>
        public ShadowEscortActionResult TryResolve(CharacterGrowthValues abilities,
            int roll, FamilyCreedKind creed, IndustryResources resources,
            out IndustryResources nextResources)
        {
            nextResources = resources;
            if (Stage != ShadowEscortStage.InProgress)
                return Result(false, "只有执行中的委托才能结算。");
            if (ElapsedDays < DurationDaysValue)
                return Result(false, "委托尚未到期：还需 " + RemainingDays + " 天。");
            if (roll < 0 || roll >= RollExclusiveMax)
                return Result(false, "检定随机值超出范围 0..99。");

            int attributes = abilities.MartialArts + abilities.Management + abilities.Social;
            int creedBonus = creed == FamilyCreedKind.RighteousnessFirst ? RighteousnessScoreBonus
                : creed == FamilyCreedKind.MeritFirst ? MeritFirstScoreBonus
                : 0;
            int score = attributes + creedBonus + roll;

            CommissionOutcome outcome;
            if (score >= SuccessThreshold)
                outcome = CommissionOutcome.Success;
            else if (score >= PartialThreshold)
                outcome = CommissionOutcome.Partial;
            else
                outcome = CommissionOutcome.Failure;

            // Secrecy-first never lets a failed escort become a total loss: the house
            // keeps the merchandise moving through quieter channels and settles partial.
            if (outcome == CommissionOutcome.Failure && creed == FamilyCreedKind.SecrecyFirst)
                outcome = CommissionOutcome.Partial;

            int funds = 0;
            int supplies = 0;
            if (outcome == CommissionOutcome.Success)
            {
                funds = RewardSuccessFunds;
                supplies = RewardSuccessSupplies;
                if (creed == FamilyCreedKind.MeritFirst)
                    funds += MeritFirstRewardFundsBonus;
            }
            else if (outcome == CommissionOutcome.Partial)
            {
                funds = RewardPartialFunds;
                supplies = RewardPartialSupplies;
            }

            // A maxed resource pool must not turn a settled commission into a failed
            // command: the reward is clamped to the documented resource ceiling, which
            // keeps settlement total and removes the only overflow failure path.
            int nextFunds = (int)Math.Min(IndustryResources.Maximum, (long)resources.Funds + funds);
            int nextSupplies =
                (int)Math.Min(IndustryResources.Maximum, (long)resources.Supplies + supplies);
            if (!IndustryResources.TryCreate(nextFunds, nextSupplies, resources.Intelligence,
                    out nextResources, out string error))
            {
                nextResources = resources;
                return Result(false, string.IsNullOrEmpty(error) ? "委托报酬无效。" : error);
            }

            // Report what was actually applied, not the nominal table value, so a
            // clamped settlement cannot claim a reward the family never received.
            int appliedFunds = nextFunds - resources.Funds;
            int appliedSupplies = nextSupplies - resources.Supplies;

            Score = score;
            Roll = roll;
            RewardFunds = appliedFunds;
            RewardSupplies = appliedSupplies;
            Outcome = outcome;
            Stage = outcome == CommissionOutcome.Success ? ShadowEscortStage.Succeeded
                : outcome == CommissionOutcome.Partial ? ShadowEscortStage.PartialSuccess
                : ShadowEscortStage.Failed;
            LastFeedback = BuildFeedback(outcome, attributes, creedBonus, score,
                appliedFunds, appliedSupplies);
            return Result(true, LastFeedback);
        }

        public static ShadowEscortCommission TryRestore(ShadowEscortStage stage, string assigneeId,
            int durationDays, int elapsedDays, int startYear, int startMonth, int startDay,
            CommissionOutcome outcome, int score, int roll, int rewardFunds, int rewardSupplies,
            int investedFunds, string lastFeedback, out string error) =>
            TryRestore(stage, assigneeId, durationDays, elapsedDays, startYear, startMonth,
                startDay, outcome, score, roll, rewardFunds, rewardSupplies, investedFunds,
                lastFeedback, null, false, MissionOperationMode.Delegated, out error);

        /// <summary>
        /// M07 restore overload. When <paramref name="requireRunIdForExecution"/> is true,
        /// every executing or settled row must carry one stable run ID, while pristine
        /// not-accepted and accepted rows must not. Legacy import paths pass false only
        /// while they are still converting a v14 payload.
        /// </summary>
        public static ShadowEscortCommission TryRestore(ShadowEscortStage stage, string assigneeId,
            int durationDays, int elapsedDays, int startYear, int startMonth, int startDay,
            CommissionOutcome outcome, int score, int roll, int rewardFunds, int rewardSupplies,
            int investedFunds, string lastFeedback, string runId,
            bool requireRunIdForExecution, MissionOperationMode mode, out string error)
        {
            error = string.Empty;
            var commission = new ShadowEscortCommission();
            if (!Enum.IsDefined(typeof(ShadowEscortStage), stage) ||
                !Enum.IsDefined(typeof(CommissionOutcome), outcome) ||
                !Enum.IsDefined(typeof(MissionOperationMode), mode))
            {
                error = "存档委托阶段、结果或执行方式无效。";
                return null;
            }
            if (investedFunds < 0 || rewardFunds < 0 || rewardSupplies < 0 ||
                score < 0 || roll < 0 || roll >= RollExclusiveMax)
            {
                error = "存档委托的投入、报酬或检定数值无效。";
                return null;
            }

            commission.Mode = mode;
            commission.RunId = runId ?? string.Empty;
            if (commission.RunId.Length > 0 && !MissionRequest.IsStableRunId(commission.RunId))
            {
                error = "存档委托运行 ID 无效。";
                return null;
            }
            bool executing = stage == ShadowEscortStage.InProgress || IsSettledStage(stage);
            // An abandoned row is neither executing nor settled: it records no shared
            // result, so a migrated legacy row keeps its unknown mode instead of guessing
            // whether the old run was manual or delegated. Its run ID is cleared, so
            // there is no run to key and nothing to mistake for an unsettled result.
            bool abandoned = stage == ShadowEscortStage.Abandoned;
            if (mode == MissionOperationMode.LegacyUnknown)
            {
                if (executing &&
                    !commission.RunId.StartsWith("legacy.", StringComparison.Ordinal))
                {
                    error = "未知执行方式只能用于旧版导入的委托。";
                    return null;
                }
                if (!executing && !abandoned)
                {
                    error = "尚未开始执行的委托不能使用未知执行方式。";
                    return null;
                }
            }
            if (requireRunIdForExecution)
            {
                if (executing && commission.RunId.Length == 0)
                {
                    error = "已开始执行的委托缺少运行 ID。";
                    return null;
                }
                if (!executing && commission.RunId.Length > 0)
                {
                    error = "尚未开始执行的委托不应有运行 ID。";
                    return null;
                }
            }

            commission.Stage = stage;
            commission.Outcome = outcome;
            commission.InvestedFunds = investedFunds;
            commission.Score = score;
            commission.Roll = roll;
            commission.RewardFunds = rewardFunds;
            commission.RewardSupplies = rewardSupplies;
            commission.LastFeedback = lastFeedback ?? string.Empty;
            if (outcome != CommissionOutcome.None && !IsSettledStage(stage))
            {
                error = "存档委托尚未结算却带有结算结果。";
                return null;
            }

            // Unsettled rows (including NotAccepted/Accepted) must carry no check or
            // reward data at all; a settled failure must carry no reward.
            if (outcome == CommissionOutcome.None)
            {
                if (score != 0 || roll != 0 || rewardFunds != 0 || rewardSupplies != 0)
                {
                    error = "存档委托尚未结算却带有检定或报酬数据。";
                    return null;
                }
            }
            else if (outcome == CommissionOutcome.Failure &&
                     (rewardFunds != 0 || rewardSupplies != 0))
            {
                error = "存档委托结算为失败却带有报酬。";
                return null;
            }

            if (stage == ShadowEscortStage.NotAccepted)
            {
                if (investedFunds != 0 || outcome != CommissionOutcome.None ||
                    !string.IsNullOrEmpty(assigneeId) || durationDays != 0 || elapsedDays != 0)
                {
                    error = "存档委托在未接取阶段却带有执行人或结算数据。";
                    return null;
                }
                return commission;
            }

            if (investedFunds <= 0)
            {
                error = "存档委托缺少预付投入。";
                return null;
            }

            if (stage == ShadowEscortStage.Accepted)
            {
                if (outcome != CommissionOutcome.None || !string.IsNullOrEmpty(assigneeId) ||
                    durationDays != 0 || elapsedDays != 0)
                {
                    error = "存档委托在待指派阶段却带有执行人或计时数据。";
                    return null;
                }
                return commission;
            }

            if (!PersonId.TryParse(assigneeId, out PersonId assignee))
            {
                error = "存档委托缺少有效的执行人。";
                return null;
            }

            if (IsSettledStage(stage))
            {
                CommissionOutcome expected = ExpectedOutcome(stage);
                if (outcome != expected)
                {
                    error = "存档委托的阶段与结算结果不一致。";
                    return null;
                }
                if (elapsedDays < durationDays)
                {
                    error = "存档委托已结算但未走完执行窗口。";
                    return null;
                }
            }

            if (stage == ShadowEscortStage.Abandoned)
            {
                if (outcome != CommissionOutcome.None || elapsedDays != durationDays ||
                    score != 0 || roll != 0 || rewardFunds != 0 || rewardSupplies != 0 ||
                    !string.IsNullOrEmpty(commission.RunId))
                {
                    error = "存档委托已中止却带有结算结果、运行 ID 或未完成窗口。";
                    return null;
                }
            }

            if (durationDays <= 0 || elapsedDays < 0 || elapsedDays > durationDays)
            {
                error = "存档委托的持续时间或已用天数无效。";
                return null;
            }
            if (durationDays != DurationDays)
            {
                error = "存档委托的执行窗口必须是固定的 " + DurationDays + " 天。";
                return null;
            }
            if (!GameDate.TryParse(startYear, startMonth, startDay, out GameDate startDate))
            {
                error = "存档委托的开始日期无效。";
                return null;
            }

            commission.AssigneeId = assignee;
            commission.DurationDaysValue = durationDays;
            commission.ElapsedDays = elapsedDays;
            commission.StartDate = startDate;
            return commission;
        }

        /// <summary>Captures every field needed to restore the exact lifecycle position.</summary>
        public ShadowEscortCommissionSaveData Capture() => new ShadowEscortCommissionSaveData
        {
            stage = (int)Stage,
            assigneePersonId = AssigneeId.Value ?? string.Empty,
            runId = RunId ?? string.Empty,
            mode = (int)Mode,
            durationDays = DurationDaysValue,
            elapsedDays = ElapsedDays,
            startYear = StartDate.Year,
            startMonth = StartDate.Month,
            startDay = StartDate.Day,
            outcome = (int)Outcome,
            score = Score,
            roll = Roll,
            rewardFunds = RewardFunds,
            rewardSupplies = RewardSupplies,
            investedFunds = InvestedFunds,
            lastFeedback = LastFeedback ?? string.Empty
        };

        /// <summary>
        /// Closes an in-progress commission as a failure when its assignee can no longer
        /// execute it (death, departure, or a conflicting duty). The paid investment and
        /// the start date are retained; the row is immediately valid for saving because
        /// the execution window is treated as closed at its planned end.
        /// </summary>
        public bool TryAbandon(string reason, out ShadowEscortActionResult result)
        {
            if (Stage != ShadowEscortStage.InProgress)
            {
                result = Result(false, "只有执行中的委托才能中止。");
                return false;
            }

            ElapsedDays = DurationDaysValue;
            Outcome = CommissionOutcome.None;
            Score = 0;
            Roll = 0;
            RewardFunds = 0;
            RewardSupplies = 0;
            // Abandonment is not a shared terminal result: it must not keep a run ID
            // that could be mistaken for an unsettled result.
            RunId = string.Empty;
            Stage = ShadowEscortStage.Abandoned;
            LastFeedback = string.IsNullOrWhiteSpace(reason)
                ? "委托执行人已不可用，委托失败。"
                : reason;
            result = Result(true, LastFeedback);
            return true;
        }

        /// <summary>Deep copy used by the month-turn transaction for rollback.</summary>
        public ShadowEscortCommission Clone()
        {
            var clone = new ShadowEscortCommission
            {
                Stage = Stage,
                Outcome = Outcome,
                AssigneeId = AssigneeId,
                RunId = RunId ?? string.Empty,
                Mode = Mode,
                DurationDaysValue = DurationDaysValue,
                ElapsedDays = ElapsedDays,
                StartDate = StartDate,
                InvestedFunds = InvestedFunds,
                Score = Score,
                Roll = Roll,
                RewardFunds = RewardFunds,
                RewardSupplies = RewardSupplies,
                LastFeedback = LastFeedback ?? string.Empty
            };
            return clone;
        }

        private ShadowEscortActionResult Result(bool succeeded, string feedback) =>
            new ShadowEscortActionResult(succeeded, Stage, Outcome, feedback, Score, Roll,
                RewardFunds, RewardSupplies);

        private static bool IsSettledStage(ShadowEscortStage stage) =>
            stage == ShadowEscortStage.Succeeded ||
            stage == ShadowEscortStage.PartialSuccess ||
            stage == ShadowEscortStage.Failed;

        private static CommissionOutcome ExpectedOutcome(ShadowEscortStage stage)
        {
            switch (stage)
            {
                case ShadowEscortStage.Succeeded: return CommissionOutcome.Success;
                case ShadowEscortStage.PartialSuccess: return CommissionOutcome.Partial;
                case ShadowEscortStage.Failed: return CommissionOutcome.Failure;
                default: return CommissionOutcome.None;
            }
        }

        private static string BuildFeedback(CommissionOutcome outcome, int attributes,
            int creedBonus, int score, int funds, int supplies)
        {
            string band = outcome == CommissionOutcome.Success ? "成功"
                : outcome == CommissionOutcome.Partial ? "部分成功"
                : "失败";
            string reward = funds + supplies > 0
                ? "获得 " + funds + " 资金、" + supplies + " 物资"
                : "没有获得报酬";
            return "护送委托" + band + "：能力合计 " + attributes + "，家训修正 " + creedBonus +
                   "，检定总分 " + score + "，" + reward + "。";
        }
    }

    /// <summary>
    /// Immutable read-only snapshot for UI. Remaining days are zero unless the
    /// commission is actually executing, so an accepted-but-unassigned commission is
    /// never presented as a running countdown.
    /// </summary>
    public readonly struct ShadowEscortCommissionStatus
    {
        public ShadowEscortCommissionStatus(ShadowEscortStage stage, CommissionOutcome outcome,
            PersonId assigneeId, int durationDays, int elapsedDays, int remainingDays,
            int investedFunds, int score, int roll, int rewardFunds, int rewardSupplies,
            string feedback)
        {
            Stage = stage;
            Outcome = outcome;
            AssigneeId = assigneeId;
            DurationDays = durationDays;
            ElapsedDays = elapsedDays;
            RemainingDays = remainingDays;
            InvestedFunds = investedFunds;
            Score = score;
            Roll = roll;
            RewardFunds = rewardFunds;
            RewardSupplies = rewardSupplies;
            Feedback = feedback ?? string.Empty;
        }

        public ShadowEscortStage Stage { get; }
        public CommissionOutcome Outcome { get; }
        public PersonId AssigneeId { get; }
        public int DurationDays { get; }
        public int ElapsedDays { get; }
        public int RemainingDays { get; }
        public int InvestedFunds { get; }
        public int Score { get; }
        public int Roll { get; }
        public int RewardFunds { get; }
        public int RewardSupplies { get; }
        public string Feedback { get; }

        public bool IsSettled => Stage == ShadowEscortStage.Succeeded ||
                                 Stage == ShadowEscortStage.PartialSuccess ||
                                 Stage == ShadowEscortStage.Failed;
    }

    /// <summary>
    /// JsonUtility-friendly save block. It carries the whole lifecycle state, so a
    /// commission in progress and a settled outcome both survive save/load.
    /// </summary>
    [Serializable]
    public sealed class ShadowEscortCommissionSaveData
    {
        public int stage;
        public string assigneePersonId;
        /// <summary>M07 stable run ID; empty before execution, fixed once execution starts.</summary>
        public string runId;
        /// <summary>M07 operation mode: Manual or Delegated for live runs.</summary>
        public int mode;
        public int durationDays;
        public int elapsedDays;
        public int startYear;
        public int startMonth;
        public int startDay;
        public int outcome;
        public int score;
        public int roll;
        public int rewardFunds;
        public int rewardSupplies;
        public int investedFunds;
        public string lastFeedback;

        public static ShadowEscortCommissionSaveData CreateInitial() =>
            new ShadowEscortCommissionSaveData
            {
                stage = (int)ShadowEscortStage.NotAccepted,
                assigneePersonId = string.Empty,
                runId = string.Empty,
                mode = (int)MissionOperationMode.Delegated,
                outcome = (int)CommissionOutcome.None,
                lastFeedback = string.Empty
            };

        public ShadowEscortCommissionSaveData Clone() =>
            new ShadowEscortCommissionSaveData
            {
                stage = stage,
                assigneePersonId = assigneePersonId,
                runId = runId,
                mode = mode,
                durationDays = durationDays,
                elapsedDays = elapsedDays,
                startYear = startYear,
                startMonth = startMonth,
                startDay = startDay,
                outcome = outcome,
                score = score,
                roll = roll,
                rewardFunds = rewardFunds,
                rewardSupplies = rewardSupplies,
                investedFunds = investedFunds,
                lastFeedback = lastFeedback
            };
    }
}
