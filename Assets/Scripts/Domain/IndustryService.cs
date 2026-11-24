using System;
using System.Collections.Generic;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    public enum IndustryAssignmentOutcome
    {
        Succeeded, NoChange, UnknownIndustry, UnknownPerson, NotMember,
        Dead, Retired, Minor, NoRemainingUnits, Blocked, NoVacantPost
    }

    public readonly struct IndustryAssignmentResult
    {
        public IndustryAssignmentResult(bool succeeded, IndustryAssignmentOutcome outcome, string message,
            PersonId previousAssignee, PersonId currentAssignee, int postIndex = 0)
        {
            Succeeded = succeeded;
            Outcome = outcome;
            Message = message ?? string.Empty;
            PreviousAssignee = previousAssignee;
            CurrentAssignee = currentAssignee;
            PostIndex = postIndex;
        }

        public bool Succeeded { get; }
        public IndustryAssignmentOutcome Outcome { get; }
        public string Message { get; }
        public PersonId PreviousAssignee { get; }
        public PersonId CurrentAssignee { get; }
        /// <summary>Post the command addressed; the farm owns posts 0 and 1.</summary>
        public int PostIndex { get; }
    }

    public readonly struct IndustryVacancy
    {
        public IndustryVacancy(string industryId, int postIndex, PersonId previousAssignee, string reason)
        {
            IndustryId = industryId ?? string.Empty;
            PostIndex = postIndex < 0 ? 0 : postIndex;
            PreviousAssignee = previousAssignee;
            Reason = reason ?? string.Empty;
        }

        public string IndustryId { get; }
        public int PostIndex { get; }
        public PersonId PreviousAssignee { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Person eligibility, the 4-unit monthly budget and the 3-unit post reservation.
    /// Assignment is a reservation for the current month plan, never an immediate spend.
    /// Every industry post holds at most one person and one person holds at most one post.
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

        /// <summary>
        /// Human-readable name of one post. A single-post industry keeps its plain name;
        /// the two-post farm names each plot so reports and the panel cannot conflate them.
        /// </summary>
        public static string PostName(string industryId, int postIndex)
        {
            string name = IndustryName(industryId);
            int postCount = IndustryState.PostCount(industryId);
            if (postCount <= 1)
                return name;
            return name + "（" + PostLabel(postIndex) + "号）";
        }

        /// <summary>First post without an assignee, or -1 when every post is filled.</summary>
        public static int FirstVacantPost(IndustryState state, string industryId)
        {
            if (state == null)
                return -1;
            int postCount = IndustryState.PostCount(industryId);
            for (int post = 0; post < postCount; post++)
                if (state.TryGetAssignee(industryId, post, out PersonId personId) &&
                    personId.IsEmpty)
                    return post;
            return -1;
        }

        /// <summary>
        /// Assigns a person. A single-post industry atomically replaces its holder; a
        /// multi-post industry fills its first vacant post and refuses when every post is
        /// filled, so no appointment is silently evicted.
        /// </summary>
        public static IndustryAssignmentResult TryAssign(IndustryState state,
            SuccessionState succession, GameDate date, string industryId, PersonId personId)
        {
            if (state == null)
                return Blocked("产业状态为空。");
            if (!IndustryCatalog.TryGet(industryId, out IndustryDefinition definition))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "未知产业：" + (industryId ?? "(null)") + "。", default, default);

            int postIndex = 0;
            if (definition.CapacityPosts > 1)
            {
                for (int post = 0; post < definition.CapacityPosts; post++)
                {
                    state.TryGetAssignee(industryId, post, out PersonId existing);
                    if (!personId.IsEmpty && existing == personId)
                        return new IndustryAssignmentResult(true,
                            IndustryAssignmentOutcome.NoChange,
                            "该人物已经是此产业负责人。", existing, personId, post);
                }

                postIndex = FirstVacantPost(state, industryId);
                if (postIndex < 0)
                    return new IndustryAssignmentResult(false,
                        IndustryAssignmentOutcome.NoVacantPost,
                        "该产业所有岗位都已有负责人；请先解除一个岗位。", default, default);
            }

            return TryAssignPost(state, succession, date, industryId, postIndex, personId);
        }

        /// <summary>Assigns or replaces the holder of one explicit post.</summary>
        public static IndustryAssignmentResult TryAssign(IndustryState state,
            SuccessionState succession, GameDate date, string industryId, int postIndex,
            PersonId personId)
        {
            if (state == null)
                return Blocked("产业状态为空。");
            if (!IndustryCatalog.TryGet(industryId, out IndustryDefinition definition))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "未知产业：" + (industryId ?? "(null)") + "。", default, default);
            if (postIndex < 0 || postIndex >= definition.CapacityPosts)
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "岗位序号超出范围：" + postIndex.ToString(CultureInfo.InvariantCulture) + "。",
                    default, default, postIndex);

            return TryAssignPost(state, succession, date, industryId, postIndex, personId);
        }

        private static IndustryAssignmentResult TryAssignPost(IndustryState state,
            SuccessionState succession, GameDate date, string industryId, int postIndex,
            PersonId personId)
        {
            if (personId.IsEmpty || succession == null || succession.World == null ||
                !succession.World.TryGetPerson(personId, out _))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownPerson,
                    "人物未登记。", default, default, postIndex);

            IndustryAssignmentOutcome ineligible = IneligibilityOutcome(succession, personId, date);
            if (ineligible != IndustryAssignmentOutcome.Succeeded)
                return new IndustryAssignmentResult(false, ineligible,
                    CanWork(succession, personId, date, out string reason) ? "无法任命。" : reason,
                    default, default, postIndex);

            state.TryGetAssignee(industryId, postIndex, out PersonId previous);
            if (previous == personId)
                return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.NoChange,
                    "该人物已经是此岗位负责人。", previous, personId, postIndex);

            if (RemainingWorkUnits(succession, state, personId, date) < PostWorkUnits)
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.NoRemainingUnits,
                    "该人物本月时间不足：一个职位需要 3/4 单位。", previous, previous, postIndex);

            if (!state.TrySetAssignee(industryId, postIndex, personId, out string error))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    error, previous, previous, postIndex);

            return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.Succeeded,
                "已任命" + PersonName(succession, personId) + "负责" +
                PostName(industryId, postIndex) + "。", previous, personId, postIndex);
        }

        /// <summary>
        /// Releases the highest-index staffed post, so one click frees one post and its
        /// 3-unit reservation. A single-post industry behaves exactly as before.
        /// </summary>
        public static IndustryAssignmentResult TryUnassign(IndustryState state,
            SuccessionState succession, GameDate date, string industryId)
        {
            if (state == null)
                return Blocked("产业状态为空。");
            if (!IndustryCatalog.TryGet(industryId, out IndustryDefinition definition))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "未知产业：" + (industryId ?? "(null)") + "。", default, default);

            for (int post = definition.CapacityPosts - 1; post >= 0; post--)
            {
                state.TryGetAssignee(industryId, post, out PersonId candidate);
                if (!candidate.IsEmpty)
                    return TryUnassignPost(state, succession, industryId, post);
            }

            return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.NoChange,
                "该产业当前空缺，无需解除。", default, default);
        }

        public static IndustryAssignmentResult TryUnassign(IndustryState state,
            SuccessionState succession, GameDate date, string industryId, int postIndex)
        {
            if (state == null)
                return Blocked("产业状态为空。");
            if (!IndustryCatalog.TryGet(industryId, out IndustryDefinition definition))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "未知产业：" + (industryId ?? "(null)") + "。", default, default);
            if (postIndex < 0 || postIndex >= definition.CapacityPosts)
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    "岗位序号超出范围：" + postIndex.ToString(CultureInfo.InvariantCulture) + "。",
                    default, default, postIndex);

            state.TryGetAssignee(industryId, postIndex, out PersonId previous);
            if (previous.IsEmpty)
                return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.NoChange,
                    "该岗位当前空缺，无需解除。", default, default, postIndex);

            return TryUnassignPost(state, succession, industryId, postIndex);
        }

        private static IndustryAssignmentResult TryUnassignPost(IndustryState state,
            SuccessionState succession, string industryId, int postIndex)
        {
            state.TryGetAssignee(industryId, postIndex, out PersonId previous);
            if (!state.TryClearAssignee(industryId, postIndex, out string error))
                return new IndustryAssignmentResult(false, IndustryAssignmentOutcome.UnknownIndustry,
                    error, previous, previous, postIndex);

            return new IndustryAssignmentResult(true, IndustryAssignmentOutcome.Succeeded,
                "已解除" + PostName(industryId, postIndex) + "负责人" +
                PersonName(succession, previous) + "的任命。", previous, default, postIndex);
        }

        /// <summary>
        /// Total cleanup: removes only assignments whose person can no longer work, plus
        /// duplicate assignments (keeping the first by industry ID and post). Never produces,
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
                    state.TryClearAssignee(assignment.IndustryId, assignment.PostIndex, out _);
                    vacated.Add(new IndustryVacancy(assignment.IndustryId, assignment.PostIndex,
                        assignment.PersonId, "负责人失效：" + reason));
                    continue;
                }

                if (!seen.Add(assignment.PersonId))
                {
                    state.TryClearAssignee(assignment.IndustryId, assignment.PostIndex, out _);
                    vacated.Add(new IndustryVacancy(assignment.IndustryId, assignment.PostIndex,
                        assignment.PersonId,
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

        /// <summary>一号, 二号, … for the small fixed post counts; digits beyond nine.</summary>
        private static string PostLabel(int postIndex)
        {
            switch (postIndex)
            {
                case 0: return "一";
                case 1: return "二";
                case 2: return "三";
                case 3: return "四";
                case 4: return "五";
                default: return (postIndex + 1).ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
