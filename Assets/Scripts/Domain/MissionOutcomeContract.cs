using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Stable mission families that share the M07 outcome contract.</summary>
    public enum MissionKind
    {
        ShadowEscort = 0,
        TacticalInfiltration = 1
    }

    /// <summary>
    /// The five shared M07 mission results. Values are part of the save contract and
    /// must never be renumbered.
    /// </summary>
    public enum SharedMissionOutcome
    {
        FullSuccess = 0,
        CostlySuccess = 1,
        PartialSuccess = 2,
        SafeFailure = 3,
        SevereFailure = 4
    }

    /// <summary>
    /// How the player reached a result. Mode never changes reward or consequence; it is
    /// recorded for display and audit only. <see cref="LegacyUnknown"/> is reserved for
    /// rows imported from a pre-M07 save and is never produced by a live settlement.
    /// </summary>
    public enum MissionOperationMode
    {
        Manual = 0,
        Delegated = 1,
        LegacyUnknown = 2
    }

    /// <summary>Approved M06 preparation choice; it is frozen into the mission request.</summary>
    public enum MissionPreparation
    {
        None = 0,
        Scouting = 1
    }

    /// <summary>One standing-value change, captured before and after application.</summary>
    public readonly struct MissionStandingChange
    {
        public MissionStandingChange(string relationId, int before, int after)
        {
            RelationId = relationId ?? string.Empty;
            Before = before;
            After = after;
        }

        public string RelationId { get; }
        public int Before { get; }
        public int After { get; }
    }

    /// <summary>Actual standing values around one settlement; clamped values are reported.</summary>
    public sealed class MissionStandingResult
    {
        public MissionStandingResult(int fameBefore, int fameAfter, int concealmentBefore,
            int concealmentAfter, IReadOnlyList<MissionStandingChange> relationChanges)
        {
            FameBefore = fameBefore;
            FameAfter = fameAfter;
            ConcealmentBefore = concealmentBefore;
            ConcealmentAfter = concealmentAfter;
            RelationChanges = relationChanges ?? new MissionStandingChange[0];
        }

        public int FameBefore { get; }
        public int FameAfter { get; }
        public int ConcealmentBefore { get; }
        public int ConcealmentAfter { get; }
        public IReadOnlyList<MissionStandingChange> RelationChanges { get; }

        public int FameDelta => FameAfter - FameBefore;
        public int ConcealmentDelta => ConcealmentAfter - ConcealmentBefore;
    }

    /// <summary>
    /// Approved standing effect for one mission/result pair. Resource rewards stay with
    /// the mission that already owns them (M05 commission bands, M06 success reward);
    /// only the shared standing contract and the extra mission cost live here.
    /// </summary>
    public readonly struct MissionOutcomeEffect
    {
        public MissionOutcomeEffect(int fameDelta, int relationDelta, int concealmentDelta,
            int extraSupplyCost, string relationId)
        {
            FameDelta = fameDelta;
            RelationDelta = relationDelta;
            ConcealmentDelta = concealmentDelta;
            ExtraSupplyCost = extraSupplyCost;
            RelationId = relationId ?? string.Empty;
        }

        public int FameDelta { get; }
        public int RelationDelta { get; }
        public int ConcealmentDelta { get; }
        /// <summary>Additional supply cost charged once at the shared terminal settlement.</summary>
        public int ExtraSupplyCost { get; }
        /// <summary>Stable employer-credit or faction ID this mission affects.</summary>
        public string RelationId { get; }
    }

    /// <summary>Stable relationship IDs introduced by M07.</summary>
    public static class MissionRelationIds
    {
        public const string ShadowEscortMerchant = "employer.shadow-escort-merchant";
        public const string InterceptedDispatchTarget = "faction.intercepted-dispatch-target";

        public static bool IsKnown(string relationId) =>
            relationId == ShadowEscortMerchant || relationId == InterceptedDispatchTarget;
    }

    /// <summary>
    /// The user-approved M07 result table. Every result that a mission does not support
    /// is an explicit configuration error instead of a silent zero settlement.
    /// </summary>
    public static class MissionOutcomeTable
    {
        public const int FullSuccessFame = 3;
        public const int CostlySuccessFame = 2;
        public const int PartialSuccessFame = 1;
        public const int SafeFailureFame = -1;
        public const int SevereFailureFame = -3;

        public const int FullSuccessRelation = 5;
        public const int CostlySuccessRelation = 2;
        public const int PartialSuccessRelation = 1;
        public const int SafeFailureRelation = -3;
        public const int SevereFailureRelation = -5;

        public const int FullSuccessConcealment = 3;
        public const int CostlySuccessConcealment = 1;
        public const int PartialSuccessConcealment = 0;
        public const int SafeFailureConcealment = -2;
        public const int SevereFailureConcealment = -5;

        public static int FameDeltaFor(SharedMissionOutcome outcome)
        {
            switch (outcome)
            {
                case SharedMissionOutcome.FullSuccess: return FullSuccessFame;
                case SharedMissionOutcome.CostlySuccess: return CostlySuccessFame;
                case SharedMissionOutcome.PartialSuccess: return PartialSuccessFame;
                case SharedMissionOutcome.SafeFailure: return SafeFailureFame;
                case SharedMissionOutcome.SevereFailure: return SevereFailureFame;
                default: return 0;
            }
        }

        public static int RelationDeltaFor(SharedMissionOutcome outcome)
        {
            switch (outcome)
            {
                case SharedMissionOutcome.FullSuccess: return FullSuccessRelation;
                case SharedMissionOutcome.CostlySuccess: return CostlySuccessRelation;
                case SharedMissionOutcome.PartialSuccess: return PartialSuccessRelation;
                case SharedMissionOutcome.SafeFailure: return SafeFailureRelation;
                case SharedMissionOutcome.SevereFailure: return SevereFailureRelation;
                default: return 0;
            }
        }

        public static int ConcealmentDeltaFor(SharedMissionOutcome outcome)
        {
            switch (outcome)
            {
                case SharedMissionOutcome.FullSuccess: return FullSuccessConcealment;
                case SharedMissionOutcome.CostlySuccess: return CostlySuccessConcealment;
                case SharedMissionOutcome.PartialSuccess: return PartialSuccessConcealment;
                case SharedMissionOutcome.SafeFailure: return SafeFailureConcealment;
                case SharedMissionOutcome.SevereFailure: return SevereFailureConcealment;
                default: return 0;
            }
        }

        /// <summary>
        /// Approved effects for one mission and result. M05 supports full success,
        /// partial success and safe failure only; M06 supports full success, costly
        /// success, safe failure and severe failure and never generates a partial result.
        /// </summary>
        public static bool TryGetEffects(MissionKind kind, SharedMissionOutcome outcome,
            out MissionOutcomeEffect effect, out string error)
        {
            effect = default;
            error = string.Empty;
            if (!Enum.IsDefined(typeof(MissionKind), kind) ||
                !Enum.IsDefined(typeof(SharedMissionOutcome), outcome))
            {
                error = "任务类型或共享结果无效。";
                return false;
            }

            if (kind == MissionKind.ShadowEscort)
            {
                if (outcome == SharedMissionOutcome.CostlySuccess)
                {
                    error = "M05 护送委托不支持“成功但有代价”结果。";
                    return false;
                }
                if (outcome == SharedMissionOutcome.SevereFailure)
                {
                    error = "M05 护送委托不支持“严重失败”结果。";
                    return false;
                }
                effect = new MissionOutcomeEffect(FameDeltaFor(outcome), RelationDeltaFor(outcome),
                    ConcealmentDeltaFor(outcome), 0, MissionRelationIds.ShadowEscortMerchant);
                return true;
            }

            if (outcome == SharedMissionOutcome.PartialSuccess)
            {
                error = "M06 潜入只有单一密报目标，不生成“部分成功”结果。";
                return false;
            }

            int extraSupply = outcome == SharedMissionOutcome.CostlySuccess ? 1 : 0;
            effect = new MissionOutcomeEffect(FameDeltaFor(outcome), RelationDeltaFor(outcome),
                ConcealmentDeltaFor(outcome), extraSupply,
                MissionRelationIds.InterceptedDispatchTarget);
            return true;
        }

        public static string OutcomeName(SharedMissionOutcome outcome)
        {
            switch (outcome)
            {
                case SharedMissionOutcome.FullSuccess: return "完全成功";
                case SharedMissionOutcome.CostlySuccess: return "成功但有代价";
                case SharedMissionOutcome.PartialSuccess: return "部分成功";
                case SharedMissionOutcome.SafeFailure: return "失败并安全撤离";
                case SharedMissionOutcome.SevereFailure: return "严重失败";
                default: return "未知结果";
            }
        }

        public static string ModeName(MissionOperationMode mode)
        {
            switch (mode)
            {
                case MissionOperationMode.Manual: return "亲自执行";
                case MissionOperationMode.Delegated: return "自动委派";
                case MissionOperationMode.LegacyUnknown: return "旧版导入（模式未知）";
                default: return "未知模式";
            }
        }

        public static string PreparationName(MissionPreparation preparation) =>
            preparation == MissionPreparation.Scouting ? "侦察路线" : "不准备";

        public static string KindName(MissionKind kind) =>
            kind == MissionKind.TacticalInfiltration ? "潜入密令" : "护送委托";
    }

    /// <summary>
    /// M05 mapping required by M07. The commission's own attribute check and creed
    /// modifiers are unchanged; only the shared result band is derived from its outcome.
    /// </summary>
    public static class ShadowEscortOutcomeRules
    {
        public static bool TryMap(CommissionOutcome outcome, out SharedMissionOutcome mapped,
            out string error)
        {
            error = string.Empty;
            switch (outcome)
            {
                case CommissionOutcome.Success:
                    mapped = SharedMissionOutcome.FullSuccess;
                    return true;
                case CommissionOutcome.Partial:
                    mapped = SharedMissionOutcome.PartialSuccess;
                    return true;
                case CommissionOutcome.Failure:
                    mapped = SharedMissionOutcome.SafeFailure;
                    return true;
                default:
                    mapped = SharedMissionOutcome.SafeFailure;
                    error = "护送委托尚未结算，无法映射共享结果。";
                    return false;
            }
        }
    }
}
