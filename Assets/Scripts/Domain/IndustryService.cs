using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    public enum IndustryAssignmentOutcome
    {
        Succeeded, NoChange, UnknownIndustry, UnknownPerson, NotMember,
        Dead, Retired, Minor, NoRemainingUnits, Blocked
    }

    public readonly struct IndustryAssignmentResult
    {
        public IndustryAssignmentResult(bool succeeded, IndustryAssignmentOutcome outcome, string message,
            PersonId previousAssignee, PersonId currentAssignee)
        {
            Succeeded = succeeded;
            Outcome = outcome;
            Message = message ?? string.Empty;
            PreviousAssignee = previousAssignee;
            CurrentAssignee = currentAssignee;
        }

        public bool Succeeded { get; }
        public IndustryAssignmentOutcome Outcome { get; }
        public string Message { get; }
        public PersonId PreviousAssignee { get; }
        public PersonId CurrentAssignee { get; }
    }

    public readonly struct IndustryVacancy
    {
        public IndustryVacancy(string industryId, PersonId previousAssignee, string reason)
        {
            IndustryId = industryId ?? string.Empty;
            PreviousAssignee = previousAssignee;
            Reason = reason ?? string.Empty;
        }

        public string IndustryId { get; }
        public PersonId PreviousAssignee { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Person eligibility, the 4-unit monthly budget and the 3-unit post reservation.
    /// Assignment is a reservation for the current month plan, never an immediate spend.
    /// </summary>
    public static class IndustryService
    {
        public const int MonthlyWorkUnits = 4;
        public const int PostWorkUnits = 3;

        public static bool CanWork(SuccessionState succession, PersonId personId, GameDate date,
            out string reason)
        {
            if (succession == null || succession.World == null)
            {
                reason = "继承状态为空。";
                return false;
            }
            if (personId.IsEmpty)
            {
                reason = "人物 ID 为空。";
                return false;
            }
            if (!succession.World.TryGetPerson(personId, out _))
            {
                reason = "人物未登记。";
                return false;
            }
            if (!succession.World.Family.IsMember(personId))
            {
                reason = "不是当前家族成员。";
                return false;
            }
            if (!succession.World.TryGetLife(personId, out SuccessionPersonLife life))
            {
                reason = "生命状态缺失。";
                return false;
            }
            if (!life.Alive)
            {
                reason = "已死亡。";
                return false;
            }
            if (life.Retired)
            {
                reason = "已隐居（退休）。";
                return false;
            }
            if (life.AgeAt(date) < succession.AdulthoodAge)
            {
                reason = "未成年。";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>4 units when free, 1 when one 3-unit post is reserved, 0 when unable to work.</summary>
        public static int RemainingWorkUnits(SuccessionState succession, IndustryState state,
            PersonId personId, GameDate date)
        {
            if (!CanWork(succession, personId, date, out _))
                return 0;
            if (state != null)
            {
                foreach (IndustryAssignment assignment in state.Assignments)
                    if (!assignment.PersonId.IsEmpty && assignment.PersonId == personId)
                        return MonthlyWorkUnits - PostWorkUnits;
            }
            return MonthlyWorkUnits;
        }

        public static IndustryAssignmentResult TryAssign(IndustryState state, SuccessionState succession,
            GameDate date, string industryId, PersonId personId)
        {
            if (state == null)
                return Blocked("产业状态为空。");
            if (!IndustryCatalog.TryGet(industryId, out _))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "未知产业：" + (industryId ?? "(null)") + "。", default, default);
            if (personId.IsEmpty || succession == null || succession.World == null ||
                !succession.World.TryGetPerson(personId, out _))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownPerson,
                    "人物未登记。", default, default);

            IndustryAssignmentOutcome ineligible = IneligibilityOutcome(succession, personId, date);
            if (ineligible != IndustryAssignmentOutcome.Succeeded)
                return new IndustryAssignmentResult(false, ineligible,
                    CanWork(succession, personId, date, out string reason) ? "无法任命。" : reason,
                    default, default);

            state.TryGetAssignee(industryId, out PersonId previous);
            if (previous == personId)
                return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.NoChange,
                    "该人物已经是此产业负责人。", previous, personId);

            if (RemainingWorkUnits(succession, state, personId, date) < PostWorkUnits)
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.NoRemainingUnits,
                    "该人物本月时间不足：一个职位需要 3/4 单位。", previous, previous);

            if (!state.TrySetAssignee(industryId, personId, out string error))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    error, previous, previous);

            return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.Succeeded,
                "已任命" + PersonName(succession, personId) + "负责" +
                IndustryName(industryId) + "。", previous, personId);
        }

        public static IndustryAssignmentResult TryUnassign(IndustryState state, SuccessionState succession,
            GameDate date, string industryId)
        {
            if (state == null)
                return Blocked("产业状态为空。");
            if (!IndustryCatalog.TryGet(industryId, out _))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "未知产业：" + (industryId ?? "(null)") + "。", default, default);

            state.TryGetAssignee(industryId, out PersonId previous);
            if (previous.IsEmpty)
                return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.NoChange,
                    "该产业当前空缺，无需解除。", default, default);

            if (!state.TryClearAssignee(industryId, out string error))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    error, previous, previous);

            return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.Succeeded,
                "已解除" + IndustryName(industryId) + "负责人" +
                PersonName(succession, previous) + "的任命。", previous, default);
        }

        /// <summary>
        /// Total cleanup: removes only assignments whose person can no longer work, plus
        /// duplicate assignments (keeping the first by industry ID). Never produces,
        /// charges, advances time or writes history.
        /// </summary>
        public static IReadOnlyList<IndustryVacancy> ReconcileAssignments(IndustryState state,
            SuccessionState succession, GameDate date)
        {
            var vacated = new List<IndustryVacancy>();
            if (state == null)
                return vacated.AsReadOnly();

            var seen = new HashSet<PersonId>();
            foreach (IndustryAssignment assignment in state.Assignments)
            {
                if (assignment.PersonId.IsEmpty)
                    continue;

                if (!CanWork(succession, assignment.PersonId, date, out string reason))
                {
                    state.TryClearAssignee(assignment.IndustryId, out _);
                    vacated.Add(new IndustryVacancy(assignment.IndustryId, assignment.PersonId,
                        "负责人失效：" + reason));
                    continue;
                }

                if (!seen.Add(assignment.PersonId))
                {
                    state.TryClearAssignee(assignment.IndustryId, out _);
                    vacated.Add(new IndustryVacancy(assignment.IndustryId, assignment.PersonId,
                        "重复任命已解除：同一人物不能同时担任两个产业职位。"));
                }
            }

            return vacated.AsReadOnly();
        }

        private static IndustryAssignmentOutcome IneligibilityOutcome(SuccessionState succession,
            PersonId personId, GameDate date)
        {
            if (!succession.World.Family.IsMember(personId))
                return IndustryAssignmentOutcome.NotMember;
            if (!succession.World.TryGetLife(personId, out SuccessionPersonLife life))
                return IndustryAssignmentOutcome.UnknownPerson;
            if (!life.Alive)
                return IndustryAssignmentOutcome.Dead;
            if (life.Retired)
                return IndustryAssignmentOutcome.Retired;
            if (life.AgeAt(date) < succession.AdulthoodAge)
                return IndustryAssignmentOutcome.Minor;
            return IndustryAssignmentOutcome.Succeeded;
        }

        private static IndustryAssignmentResult Blocked(string message) =>
            new IndustryAssignmentResult(false, IndustryAssignmentOutcome.Blocked, message, default, default);

        public static string PersonName(SuccessionState succession, PersonId personId) =>
            succession != null && succession.World != null &&
            succession.World.TryGetPerson(personId, out PersonState person)
                ? person.DisplayName
                : personId.Value ?? string.Empty;

        public static string IndustryName(string industryId) =>
            IndustryCatalog.TryGet(industryId, out IndustryDefinition definition)
                ? definition.DisplayName
                : industryId ?? string.Empty;
    }
}
