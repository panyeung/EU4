using System;
using System.Collections.Generic;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Deterministic SC-010 succession rules over SC-003 identities, roster and kinship.
    /// Every command validates before mutating; handoffs are atomic and idempotent, and
    /// no branch waits or loops without an explicit result.
    /// </summary>
    public static class SuccessionService
    {
        /// <summary>Roles that can lawfully inherit in this prototype (GDD 4.1, 19.2).</summary>
        public static bool IsSuccessorRole(FamilyMemberRole role) =>
            role == FamilyMemberRole.DirectKin ||
            role == FamilyMemberRole.CollateralKin ||
            role == FamilyMemberRole.AdoptedChild;

        /// <summary>
        /// Roles that can lawfully serve as regent (GDD 4.3, 19.2): adult spouse,
        /// kin (direct/collateral/adopted) or important retainer. Disciples and guests
        /// are deliberately excluded in this prototype.
        /// </summary>
        public static bool IsRegentRole(FamilyMemberRole role) =>
            role == FamilyMemberRole.Spouse ||
            role == FamilyMemberRole.DirectKin ||
            role == FamilyMemberRole.CollateralKin ||
            role == FamilyMemberRole.AdoptedChild ||
            role == FamilyMemberRole.Retainer;

        /// <summary>Living, non-retired direct/collateral kin or adopted children, sorted by permanent ID.</summary>
        public static IReadOnlyList<SuccessionCandidate> GetCandidates(SuccessionState state, GameDate date)
        {
            var candidates = new List<SuccessionCandidate>();
            if (state == null)
                return candidates.AsReadOnly();

            foreach (FamilyMembership membership in state.World.Family.Members)
            {
                if (membership.PersonId == state.HeadPersonId)
                    continue;
                if (!state.World.TryGetLife(membership.PersonId, out SuccessionPersonLife life))
                    continue;
                if (!life.Alive || life.Retired)
                    continue;
                if (!IsSuccessorRole(membership.Role))
                    continue;
                if (!state.World.TryGetPerson(membership.PersonId, out PersonState person))
                    continue;

                int age = life.AgeAt(date);
                candidates.Add(new SuccessionCandidate(membership.PersonId, person.DisplayName, age,
                    age >= state.AdulthoodAge, membership.Role,
                    membership.PersonId == state.DesignatedHeirId));
            }

            candidates.Sort((a, b) => string.Compare(a.PersonId.Value ?? string.Empty,
                b.PersonId.Value ?? string.Empty, StringComparison.Ordinal));
            return candidates.AsReadOnly();
        }

        /// <summary>Living adults who may serve as regent for the selected minor successor.</summary>
        public static IReadOnlyList<SuccessionCandidate> GetRegentCandidates(SuccessionState state, GameDate date)
        {
            var candidates = new List<SuccessionCandidate>();
            if (state == null)
                return candidates.AsReadOnly();

            foreach (FamilyMembership membership in state.World.Family.Members)
            {
                if (membership.PersonId == state.HeadPersonId)
                    continue;
                if (!membership.PersonId.IsEmpty && membership.PersonId == state.PendingSuccessorId)
                    continue;
                if (!state.World.TryGetLife(membership.PersonId, out SuccessionPersonLife life))
                    continue;
                if (!life.Alive || life.Retired)
                    continue;
                if (!IsRegentRole(membership.Role))
                    continue;
                int age = life.AgeAt(date);
                if (age < state.AdulthoodAge)
                    continue;
                if (!state.World.TryGetPerson(membership.PersonId, out PersonState person))
                    continue;

                candidates.Add(new SuccessionCandidate(membership.PersonId, person.DisplayName, age,
                    true, membership.Role, false));
            }

            candidates.Sort((a, b) => string.Compare(a.PersonId.Value ?? string.Empty,
                b.PersonId.Value ?? string.Empty, StringComparison.Ordinal));
            return candidates.AsReadOnly();
        }

        public static bool IsEligibleSuccessor(SuccessionState state, PersonId personId, GameDate date)
        {
            if (state == null || personId.IsEmpty)
                return false;
            foreach (SuccessionCandidate candidate in GetCandidates(state, date))
                if (candidate.PersonId == personId)
                    return true;
            return false;
        }

        public static bool IsEligibleRegent(SuccessionState state, PersonId personId, GameDate date)
        {
            if (state == null || personId.IsEmpty)
                return false;
            foreach (SuccessionCandidate candidate in GetRegentCandidates(state, date))
                if (candidate.PersonId == personId)
                    return true;
            return false;
        }

        /// <summary>
        /// Starts retirement. Repeating the request while a decision is already pending
        /// is a successful no-op so the UI cannot reset a player's selection.
        /// </summary>
        public static SuccessionResult BeginRetirement(SuccessionState state, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default,
                    "继承状态为空。");

            if (state.IsPending)
                return Success(SuccessionOutcome.AlreadyPending, state.Phase, default,
                    "继承决策已在进行中，未重置当前选择。");
            if (state.Phase == SuccessionPhase.Regency)
                return Failure(SuccessionOutcome.AlreadyPending, state.Phase, state.HeadPersonId,
                    "幼主摄政中，继承人成年后才会再次处理继承。");
            if (state.Phase == SuccessionPhase.Extinct)
                return Failure(SuccessionOutcome.Extinction, state.Phase, state.HeadPersonId,
                    "家族已经灭亡。");

            if (!TryGetLivingHead(state, out PersonState head, out string headError))
                return Failure(SuccessionOutcome.InvalidHead, state.Phase, state.HeadPersonId, headError);

            state.PendingSuccessorId = default;
            state.PendingRegentId = default;

            IReadOnlyList<SuccessionCandidate> candidates = GetCandidates(state, date);
            if (candidates.Count == 0)
            {
                return EnterCrisisOrExtinct(state, date,
                    "没有合法继承人。可进行一次危机收养/重建：条件为家族中至少一名在世成年成员作保荐人；代价为收养一名成年养子并记录家族危机。",
                    "没有合法继承人，也没有危机重建条件；家族灭亡。");
            }

            // GDD 19.2: check the designated heir before asking the player to choose.
            // A valid designation is honored immediately instead of being ignored.
            if (!state.DesignatedHeirId.IsEmpty)
            {
                if (!IsEligibleSuccessor(state, state.DesignatedHeirId, date))
                {
                    state.Phase = SuccessionPhase.AwaitingSuccessionChoice;
                    return Success(SuccessionOutcome.DesignatedHeirInvalid, state.Phase,
                        state.DesignatedHeirId,
                        "原指定继承人已死亡、离族或失去资格，必须从合法候选人中重新选择。");
                }

                state.World.TryGetLife(state.DesignatedHeirId, out SuccessionPersonLife designatedLife);
                if (designatedLife.AgeAt(date) >= state.AdulthoodAge)
                {
                    SuccessionResult handoff = PerformHandoff(state, state.DesignatedHeirId,
                        default, date, false);
                    if (!handoff.Succeeded)
                        return handoff;
                    return Success(handoff.Outcome, handoff.Phase, handoff.Subject,
                        "指定继承人 " + PersonName(state, handoff.Subject) + " 已继位。");
                }

                state.PendingSuccessorId = state.DesignatedHeirId;
                if (GetRegentCandidates(state, date).Count == 0)
                {
                    return EnterCrisisOrExtinct(state, date,
                        "指定继承人未成年，且没有合资格的成年配偶、亲族或家臣可摄政；进入一次危机收养决策。",
                        "指定继承人未成年且没有合资格摄政，也没有危机重建条件；家族灭亡。",
                        SuccessionOutcome.RegentUnavailable);
                }

                state.Phase = SuccessionPhase.AwaitingRegentChoice;
                return Success(SuccessionOutcome.RegentRequired, state.Phase, state.PendingSuccessorId,
                    "指定继承人未成年，必须指定一名合资格成年摄政后交接。");
            }

            state.Phase = SuccessionPhase.AwaitingSuccessionChoice;
            return Success(SuccessionOutcome.Succeeded, state.Phase, default,
                "请选择合法继承人。");
        }

        /// <summary>Selects one lawful candidate; a minor moves the decision to regent choice.</summary>
        public static SuccessionResult SelectSuccessor(SuccessionState state, PersonId personId, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default, "继承状态为空。");
            if (state.Phase != SuccessionPhase.AwaitingSuccessionChoice &&
                state.Phase != SuccessionPhase.AwaitingRegentChoice)
                return Failure(SuccessionOutcome.NotPending, state.Phase, personId,
                    "当前没有等待选择的继承决策。");
            if (!IsEligibleSuccessor(state, personId, date))
                return Failure(SuccessionOutcome.CandidateNotEligible, state.Phase, personId,
                    "该人物不是当前合法继承人。");

            state.PendingSuccessorId = personId;
            state.PendingRegentId = default;

            state.World.TryGetLife(personId, out SuccessionPersonLife life);
            if (life.AgeAt(date) < state.AdulthoodAge)
            {
                if (GetRegentCandidates(state, date).Count == 0)
                {
                    return EnterCrisisOrExtinct(state, date,
                        "继承人未成年，且没有合资格的成年配偶、亲族或家臣可摄政；进入一次危机收养决策。",
                        "继承人未成年且没有合资格摄政，也没有危机重建条件；家族灭亡。",
                        SuccessionOutcome.RegentUnavailable);
                }

                state.Phase = SuccessionPhase.AwaitingRegentChoice;
                return Success(SuccessionOutcome.RegentRequired, state.Phase, personId,
                    "继承人是未成年，必须指定一名合资格成年摄政后才能交接。");
            }

            state.Phase = SuccessionPhase.AwaitingSuccessionChoice;
            return Success(SuccessionOutcome.Succeeded, state.Phase, personId,
                "已选择成年继承人，确认后交接家督。");
        }

        /// <summary>Selects one eligible adult regent for the pending minor successor.</summary>
        public static SuccessionResult SelectRegent(SuccessionState state, PersonId personId, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default, "继承状态为空。");
            if (state.Phase != SuccessionPhase.AwaitingRegentChoice)
                return Failure(SuccessionOutcome.NotPending, state.Phase, personId,
                    "当前没有等待摄政选择的继承决策。");
            if (!IsEligibleRegent(state, personId, date))
                return Failure(SuccessionOutcome.InvalidRegent, state.Phase, personId,
                    "该人物不是合资格摄政。");

            state.PendingRegentId = personId;
            return Success(SuccessionOutcome.Succeeded, state.Phase, personId,
                "已指定摄政，确认后由幼主继位。");
        }

        /// <summary>
        /// Confirms the pending selection exactly once. Adult heirs take office
        /// immediately; minor heirs enter regency and keep the regent active.
        /// </summary>
        public static SuccessionResult ConfirmSuccession(SuccessionState state, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default, "继承状态为空。");
            if (state.Phase != SuccessionPhase.AwaitingSuccessionChoice &&
                state.Phase != SuccessionPhase.AwaitingRegentChoice)
                return Failure(SuccessionOutcome.NotPending, state.Phase, default,
                    "当前没有可确认的继承决定。");
            if (state.PendingSuccessorId.IsEmpty)
                return Failure(SuccessionOutcome.InvalidCandidate, state.Phase, default,
                    "尚未选择继承人。");
            if (!IsEligibleSuccessor(state, state.PendingSuccessorId, date))
                return Failure(SuccessionOutcome.CandidateNotEligible, state.Phase,
                    state.PendingSuccessorId, "已选继承人不再合法。");

            state.World.TryGetLife(state.PendingSuccessorId, out SuccessionPersonLife life);
            bool minor = life.AgeAt(date) < state.AdulthoodAge;
            if (minor)
            {
                if (state.PendingRegentId.IsEmpty)
                {
                    if (GetRegentCandidates(state, date).Count == 0)
                    {
                        return EnterCrisisOrExtinct(state, date,
                            "未成年继承人没有合资格摄政可用；进入一次危机收养决策。",
                            "未成年继承人没有合资格摄政，也没有危机重建条件；家族灭亡。",
                            SuccessionOutcome.RegentUnavailable);
                    }

                    return Failure(SuccessionOutcome.RegentRequired, state.Phase, default,
                        "未成年继承人必须先指定摄政。");
                }
                if (!IsEligibleRegent(state, state.PendingRegentId, date))
                {
                    state.PendingRegentId = default;
                    if (GetRegentCandidates(state, date).Count == 0)
                    {
                        return EnterCrisisOrExtinct(state, date,
                            "已选摄政不再合资格，且没有其他合资格成年摄政；进入一次危机收养决策。",
                            "已选摄政不再合资格，且没有危机重建条件；家族灭亡。",
                            SuccessionOutcome.RegentUnavailable);
                    }

                    return Failure(SuccessionOutcome.InvalidRegent, state.Phase,
                        state.PendingRegentId, "已选摄政不再合资格。");
                }
            }

            return PerformHandoff(state, state.PendingSuccessorId,
                minor ? state.PendingRegentId : default, date, false);
        }

        /// <summary>
        /// Completes a regency once the minor head reaches the configured adulthood
        /// threshold. This never advances the clock or grants a second month budget.
        /// </summary>
        public static SuccessionResult TryCompleteRegency(SuccessionState state, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default, "继承状态为空。");
            if (state.Phase != SuccessionPhase.Regency)
                return Failure(SuccessionOutcome.NotPending, state.Phase, state.HeadPersonId,
                    "当前没有摄政状态。");
            if (!TryGetLivingHead(state, out PersonState head, out string headError))
                return Failure(SuccessionOutcome.InvalidHead, state.Phase, state.HeadPersonId, headError);
            if (!state.World.TryGetLife(head.Id, out SuccessionPersonLife life))
                return Failure(SuccessionOutcome.InvalidState, state.Phase, head.Id, "幼主生命状态缺失。");
            if (life.AgeAt(date) < state.AdulthoodAge)
                return Failure(SuccessionOutcome.NotDue, state.Phase, head.Id,
                    "继承人尚未成年，摄政继续。");

            state.Phase = SuccessionPhase.None;
            state.RegencyCompleted = true;
            state.PendingSuccessorId = default;
            state.PendingRegentId = default;
            state.CompletedResultId = string.IsNullOrEmpty(state.CompletedResultId)
                ? "succession.regency.majority." + head.Id.Value
                : state.CompletedResultId + ".majority";
            return Success(SuccessionOutcome.Succeeded, state.Phase, head.Id,
                "继承人已成年，摄政结束，家督控制权交还本人。");
        }

        /// <summary>
        /// One explicit crisis decision: adopt a deterministic adult child, then hand
        /// over immediately. The family must have a living adult sponsor.
        /// </summary>
        public static SuccessionResult AcceptCrisisAdoption(SuccessionState state, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default, "继承状态为空。");
            if (state.Phase != SuccessionPhase.CrisisDecision)
                return Failure(SuccessionOutcome.NotPending, state.Phase, default,
                    "当前没有危机收养决策。");
            if (state.CrisisAdoptionUsed)
                return Failure(SuccessionOutcome.CrisisUnavailable, state.Phase, default,
                    "危机收养已经使用过。");
            if (!CanOfferCrisis(state, date))
            {
                state.Phase = SuccessionPhase.Extinct;
                state.CompletedResultId = "extinction." + state.World.Family.Id.Value;
                return Success(SuccessionOutcome.Extinction, state.Phase, state.HeadPersonId,
                    "没有在世成年保荐人，无法危机收养；家族灭亡。");
            }

            if (!TryGetHandoffHead(state, out PersonState head, out string headError))
                return Failure(SuccessionOutcome.InvalidHead, state.Phase, state.HeadPersonId, headError);
            if (!state.World.TryGetLife(head.Id, out SuccessionPersonLife oldHeadLife))
                return Failure(SuccessionOutcome.InvalidState, state.Phase, head.Id,
                    "家主生命状态缺失。");

            // Snapshot the complete pre-transaction state so the adoption, kinship
            // link and head transfer are all-or-nothing.
            PersonId previousHead = state.HeadPersonId;
            PersonId previousDesignated = state.DesignatedHeirId;
            PersonId previousPendingSuccessor = state.PendingSuccessorId;
            PersonId previousPendingRegent = state.PendingRegentId;
            SuccessionPhase previousPhase = state.Phase;
            int previousCount = state.SuccessionCount;
            string previousResult = state.CompletedResultId ?? string.Empty;
            bool previousCrisis = state.CrisisAdoptionUsed;
            bool previousRegencyCompleted = state.RegencyCompleted;

            PersonId adoptedId = NextAdoptedPersonId(state);
            var adopted = new PersonState(adoptedId, "養子 宗次", PersonGender.Male);
            var life = new SuccessionPersonLife(date.Year - 20, true, false);
            if (!state.World.TryAddPerson(adopted, life, out string error))
                return Failure(SuccessionOutcome.InvalidState, state.Phase, adoptedId, error);
            if (!state.World.TryAddMember(adoptedId, FamilyMemberRole.AdoptedChild, out error))
            {
                state.World.TryRemovePerson(adoptedId);
                return Failure(SuccessionOutcome.InvalidState, state.Phase, adoptedId, error);
            }
            // Validate the append-only adoption link before the handoff so the only
            // later mutation cannot fail after the head has already changed.
            if (!KinshipLink.TryCreate(head.Id, adoptedId, KinshipType.Adoption,
                    out KinshipLink adoption, out error) ||
                state.World.Kinship.Contains(adoption))
            {
                state.World.TryRemoveMember(adoptedId, out _);
                state.World.TryRemovePerson(adoptedId);
                return Failure(SuccessionOutcome.InvalidState, state.Phase, adoptedId,
                    string.IsNullOrEmpty(error) ? "收养亲属关系已存在。" : error);
            }

            state.CrisisAdoptionUsed = true;
            state.PendingSuccessorId = adoptedId;
            SuccessionResult handoff = PerformHandoff(state, adoptedId, default, date, true);
            if (!handoff.Succeeded)
            {
                state.CrisisAdoptionUsed = false;
                state.PendingSuccessorId = default;
                state.World.TryRemoveMember(adoptedId, out _);
                state.World.TryRemovePerson(adoptedId);
                return handoff;
            }

            if (!state.World.TryAddKinship(adoption, out error))
            {
                // KinshipGraph is append-only, so guarantee atomicity by restoring the
                // handoff snapshot and removing the newly adopted person. The pre-check
                // above makes this branch unreachable in normal play, but a failed
                // write must never leave a transferred head without an adoption link.
                RollbackHandoff(state, head.Id, adoptedId, FamilyMemberRole.AdoptedChild,
                    oldHeadLife, life, previousHead, previousDesignated, previousPendingSuccessor,
                    previousPendingRegent, previousPhase, previousCount, previousResult,
                    previousCrisis, previousRegencyCompleted);
                state.World.TryRemoveMember(adoptedId, out _);
                state.World.TryRemovePerson(adoptedId);
                return Failure(SuccessionOutcome.InvalidState, previousPhase, adoptedId,
                    "收养关系写入失败，继承状态已回滚：" + error);
            }

            return new SuccessionResult(handoff.Outcome, true,
                "危机收养成立：養子 宗次成为新家主，家族危机已记录。", handoff.Phase, adoptedId);
        }

        /// <summary>Declines the crisis decision and ends the family explicitly.</summary>
        public static SuccessionResult DeclineCrisis(SuccessionState state)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default, "继承状态为空。");
            if (state.Phase != SuccessionPhase.CrisisDecision)
                return Failure(SuccessionOutcome.NotPending, state.Phase, default,
                    "当前没有危机收养决策。");

            state.Phase = SuccessionPhase.Extinct;
            state.CompletedResultId = "extinction." + state.World.Family.Id.Value;
            return Success(SuccessionOutcome.Extinction, state.Phase, state.HeadPersonId,
                "玩家放弃危机重建；家族灭亡。");
        }

        /// <summary>True when the fixed crisis decision can still offer an adoption.</summary>
        public static bool CanOfferCrisis(SuccessionState state, GameDate date)
        {
            if (state == null || state.CrisisAdoptionUsed)
                return false;
            foreach (FamilyMembership membership in state.World.Family.Members)
            {
                if (membership.PersonId == state.HeadPersonId)
                    continue;
                if (!state.World.TryGetLife(membership.PersonId, out SuccessionPersonLife life))
                    continue;
                if (life.Alive && life.AgeAt(date) >= state.AdulthoodAge)
                    return true;
            }
            return false;
        }

        private static SuccessionResult PerformHandoff(SuccessionState state, PersonId successorId,
            PersonId regentId, GameDate date, bool crisis)
        {
            if (!TryGetHandoffHead(state, out PersonState oldHead, out string error))
                return Failure(SuccessionOutcome.InvalidHead, state.Phase, state.HeadPersonId, error);
            if (!state.World.TryGetPerson(successorId, out PersonState successor))
                return Failure(SuccessionOutcome.InvalidCandidate, state.Phase, successorId,
                    "继承人不存在。");
            if (!state.World.TryGetLife(successorId, out SuccessionPersonLife successorLife))
                return Failure(SuccessionOutcome.InvalidCandidate, state.Phase, successorId,
                    "继承人生命状态缺失。");
            if (!successorLife.Alive || successorLife.Retired)
                return Failure(SuccessionOutcome.CandidateNotEligible, state.Phase, successorId,
                    "继承人已死亡或已退休。");
            if (!state.World.Family.TryGetRole(successorId, out FamilyMemberRole successorRole) ||
                !IsSuccessorRole(successorRole))
                return Failure(SuccessionOutcome.CandidateNotEligible, state.Phase, successorId,
                    "继承人没有合法家族身份。");

            bool minor = successorLife.AgeAt(date) < state.AdulthoodAge;
            if (minor)
            {
                if (regentId.IsEmpty)
                    return Failure(SuccessionOutcome.RegentRequired, state.Phase, successorId,
                        "未成年继承人缺少摄政。");
                if (!state.World.Family.TryGetRole(regentId, out FamilyMemberRole regentRole) ||
                    !IsRegentRole(regentRole))
                    return Failure(SuccessionOutcome.InvalidRegent, state.Phase, regentId,
                        "摄政没有合法家族身份。");
                if (!state.World.TryGetLife(regentId, out SuccessionPersonLife regentLife) ||
                    !regentLife.Alive || regentLife.Retired ||
                    regentLife.AgeAt(date) < state.AdulthoodAge)
                    return Failure(SuccessionOutcome.InvalidRegent, state.Phase, regentId,
                        "摄政必须是存活、未退休的成年成员。");
                if (regentId == successorId || regentId == oldHead.Id)
                    return Failure(SuccessionOutcome.InvalidRegent, state.Phase, regentId,
                        "摄政不能由继承人或退休家主本人担任。");
            }
            else if (!regentId.IsEmpty)
            {
                return Failure(SuccessionOutcome.InvalidRegent, state.Phase, regentId,
                    "成年继承人不应携带摄政。");
            }

            if (!state.World.TryGetLife(oldHead.Id, out SuccessionPersonLife oldHeadLife))
                return Failure(SuccessionOutcome.InvalidState, state.Phase, oldHead.Id,
                    "家主生命状态缺失。");

            int previousCount = state.SuccessionCount;
            string previousResult = state.CompletedResultId ?? string.Empty;
            bool previousCrisis = state.CrisisAdoptionUsed;
            bool previousRegencyCompleted = state.RegencyCompleted;
            PersonId previousHead = state.HeadPersonId;
            PersonId previousDesignated = state.DesignatedHeirId;
            PersonId previousPendingSuccessor = state.PendingSuccessorId;
            PersonId previousPendingRegent = state.PendingRegentId;
            SuccessionPhase previousPhase = state.Phase;

            try
            {
                if (!state.World.TrySetRole(oldHead.Id, FamilyMemberRole.DirectKin, out error))
                    throw new InvalidOperationException(error);
                if (!state.World.TrySetRole(successorId, FamilyMemberRole.Head, out error))
                    throw new InvalidOperationException(error);
                if (!state.World.TrySetLife(oldHead.Id,
                        new SuccessionPersonLife(oldHeadLife.BirthYear, oldHeadLife.Alive, true), out error))
                    throw new InvalidOperationException(error);
                if (!state.World.TrySetLife(successorId,
                        new SuccessionPersonLife(successorLife.BirthYear, true, false), out error))
                    throw new InvalidOperationException(error);
            }
            catch (Exception exception)
            {
                RollbackHandoff(state, oldHead.Id, successorId, successorRole, oldHeadLife, successorLife,
                    previousHead, previousDesignated, previousPendingSuccessor, previousPendingRegent,
                    previousPhase, previousCount, previousResult, previousCrisis, previousRegencyCompleted);
                return Failure(SuccessionOutcome.InvalidState, previousPhase, successorId,
                    "继承交接失败，状态已回滚：" + exception.Message);
            }

            state.HeadPersonId = successorId;
            // The old designation belonged to the previous reign and must not leak
            // into the new head's stable state.
            state.DesignatedHeirId = default;
            state.SuccessionCount = previousCount + 1;
            state.PendingSuccessorId = default;
            state.PendingRegentId = minor ? regentId : default;
            state.Phase = minor ? SuccessionPhase.Regency : SuccessionPhase.None;
            state.RegencyCompleted = false;
            string resultId = "succession." +
                              state.SuccessionCount.ToString(CultureInfo.InvariantCulture) + "." +
                              successorId.Value;
            if (minor)
                resultId += ".regent." + regentId.Value;
            if (crisis)
                resultId = "crisis." + resultId;
            state.CompletedResultId = resultId;

            string message = minor
                ? "幼主 " + successor.DisplayName + " 继位，摄政 " + RegentName(state, regentId) +
                  " 协助管理；成年时暂停，需确认「完成成年交权」。"
                : "家督已交接给 " + successor.DisplayName + "。";
            if (crisis)
                message = "危机收养成立：" + message;
            return Success(SuccessionOutcome.Succeeded, state.Phase, successorId, message);
        }

        private static SuccessionResult EnterCrisisOrExtinct(SuccessionState state, GameDate date,
            string crisisMessage, string extinctionMessage,
            SuccessionOutcome crisisOutcome = SuccessionOutcome.NoCandidates)
        {
            state.PendingSuccessorId = default;
            state.PendingRegentId = default;
            if (CanOfferCrisis(state, date))
            {
                state.Phase = SuccessionPhase.CrisisDecision;
                return Success(crisisOutcome, state.Phase, default, crisisMessage);
            }

            state.Phase = SuccessionPhase.Extinct;
            state.CompletedResultId = "extinction." + state.World.Family.Id.Value;
            return Success(SuccessionOutcome.Extinction, state.Phase, state.HeadPersonId,
                extinctionMessage);
        }

        private static string RegentName(SuccessionState state, PersonId regentId) =>
            state.World.TryGetPerson(regentId, out PersonState regent) ? regent.DisplayName : regentId.Value;

        private static string PersonName(SuccessionState state, PersonId personId) =>
            state.World.TryGetPerson(personId, out PersonState person) ? person.DisplayName : personId.Value;

        private static void RollbackHandoff(SuccessionState state, PersonId oldHeadId, PersonId successorId,
            FamilyMemberRole successorRole, SuccessionPersonLife oldHeadLife, SuccessionPersonLife successorLife,
            PersonId previousHead, PersonId previousDesignated, PersonId previousPendingSuccessor,
            PersonId previousPendingRegent, SuccessionPhase previousPhase, int previousCount,
            string previousResult, bool previousCrisis, bool previousRegencyCompleted)
        {
            state.World.TrySetRole(oldHeadId, FamilyMemberRole.Head, out _);
            state.World.TrySetRole(successorId, successorRole, out _);
            state.World.TrySetLife(oldHeadId, oldHeadLife, out _);
            state.World.TrySetLife(successorId, successorLife, out _);
            state.HeadPersonId = previousHead;
            state.DesignatedHeirId = previousDesignated;
            state.PendingSuccessorId = previousPendingSuccessor;
            state.PendingRegentId = previousPendingRegent;
            state.Phase = previousPhase;
            state.SuccessionCount = previousCount;
            state.CompletedResultId = previousResult;
            state.CrisisAdoptionUsed = previousCrisis;
            state.RegencyCompleted = previousRegencyCompleted;
        }

        private static PersonId NextAdoptedPersonId(SuccessionState state)
        {
            int sequence = state.SuccessionCount + 1;
            while (true)
            {
                string value = "person.adopted." + state.World.Family.Id.Value + "." +
                               sequence.ToString(CultureInfo.InvariantCulture);
                var id = new PersonId(value);
                if (!state.World.TryGetPerson(id, out _))
                    return id;
                sequence++;
            }
        }

        /// <summary>
        /// M07 death transition. A head who died on a disclosed severe-failure check
        /// cannot retire, so this enters the ordinary succession phases without requiring
        /// a living head. Multiple adult candidates always wait for the player's choice;
        /// no heir is picked automatically.
        /// </summary>
        public static SuccessionResult BeginDeathSuccession(SuccessionState state, GameDate date)
        {
            if (state == null)
                return Failure(SuccessionOutcome.InvalidState, SuccessionPhase.None, default,
                    "继承状态为空。");
            if (state.Phase == SuccessionPhase.Extinct)
                return Failure(SuccessionOutcome.Extinction, state.Phase, state.HeadPersonId,
                    "家族已经灭亡。");
            if (!state.World.TryGetLife(state.HeadPersonId, out SuccessionPersonLife headLife) ||
                headLife.Alive)
                return Failure(SuccessionOutcome.InvalidHead, state.Phase, state.HeadPersonId,
                    "家主仍然存活：不能按身故进入继承。");

            state.PendingSuccessorId = default;
            state.PendingRegentId = default;
            state.DesignatedHeirId = default;

            IReadOnlyList<SuccessionCandidate> candidates = GetCandidates(state, date);
            if (candidates.Count == 0)
            {
                return EnterCrisisOrExtinct(state, date,
                    "家主身故且没有合法继承人。可进行一次危机收养/重建：需要至少一名在世成年成员作保荐人。",
                    "家主身故且没有合法继承人，也没有危机重建条件；家族灭亡。");
            }

            state.Phase = SuccessionPhase.AwaitingSuccessionChoice;
            return Success(SuccessionOutcome.Succeeded, state.Phase, default,
                "家主身故：请从合法候选人中选择继承人。");
        }

        /// <summary>
        /// Handoff head lookup. Retirement keeps requiring a living, non-retired head;
        /// only the explicit death phases accept a dead head so the family can continue.
        /// </summary>
        private static bool TryGetHandoffHead(SuccessionState state, out PersonState head,
            out string error)
        {
            head = null;
            if (state.HeadPersonId.IsEmpty)
            {
                error = "当前家主 ID 为空。";
                return false;
            }
            if (!state.World.TryGetPerson(state.HeadPersonId, out head))
            {
                error = "当前家主不是已登记人物。";
                return false;
            }
            if (!state.World.TryGetLife(state.HeadPersonId, out SuccessionPersonLife life))
            {
                error = "当前家主生命状态缺失。";
                return false;
            }
            if (life.Retired)
            {
                error = "当前家主已经退休，不能再次退休。";
                return false;
            }
            if (!state.World.Family.TryGetRole(state.HeadPersonId, out FamilyMemberRole role) ||
                role != FamilyMemberRole.Head)
            {
                error = "当前家主没有 Head 家族身份。";
                return false;
            }
            if (life.Alive)
            {
                error = string.Empty;
                return true;
            }
            bool deathPhase = state.Phase == SuccessionPhase.AwaitingSuccessionChoice ||
                              state.Phase == SuccessionPhase.AwaitingRegentChoice ||
                              state.Phase == SuccessionPhase.CrisisDecision;
            error = deathPhase ? string.Empty : "当前家主已死亡或生命状态缺失。";
            return deathPhase;
        }

        private static bool TryGetLivingHead(SuccessionState state, out PersonState head, out string error)
        {
            head = null;
            if (state.HeadPersonId.IsEmpty)
            {
                error = "当前家主 ID 为空。";
                return false;
            }
            if (!state.World.TryGetPerson(state.HeadPersonId, out head))
            {
                error = "当前家主不是已登记人物。";
                return false;
            }
            if (!state.World.TryGetLife(state.HeadPersonId, out SuccessionPersonLife life) || !life.Alive)
            {
                error = "当前家主已死亡或生命状态缺失。";
                return false;
            }
            if (life.Retired)
            {
                error = "当前家主已经退休，不能再次退休。";
                return false;
            }
            if (!state.World.Family.TryGetRole(state.HeadPersonId, out FamilyMemberRole role) ||
                role != FamilyMemberRole.Head)
            {
                error = "当前家主没有 Head 家族身份。";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private static SuccessionResult Success(SuccessionOutcome outcome, SuccessionPhase phase,
            PersonId subject, string message) =>
            new SuccessionResult(outcome, true, message, phase, subject);

        private static SuccessionResult Failure(SuccessionOutcome outcome, SuccessionPhase phase,
            PersonId subject, string message) =>
            new SuccessionResult(outcome, false, message, phase, subject);
    }
}
