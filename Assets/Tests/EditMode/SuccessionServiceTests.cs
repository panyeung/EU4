using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class SuccessionServiceTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        [Test]
        public void Sc010Rosters_RemainUnchangedAfterTheM2FactoryIsAdded()
        {
            SuccessionState standard = SuccessionSample.CreateStandard();
            Assert.That(standard.World.Family.Id.Value, Is.EqualTo("family.akechi"));
            Assert.That(standard.World.PersonCount, Is.EqualTo(7));
            Assert.That(standard.HeadPersonId, Is.EqualTo(SuccessionSample.StandardHeadId));
            Assert.That(standard.DesignatedHeirId, Is.EqualTo(SuccessionSample.StandardDesignatedHeirId));
            Assert.That(SuccessionService.GetCandidates(standard, Start).Count, Is.EqualTo(3));

            SuccessionState crisis = SuccessionSample.CreateCrisis();
            Assert.That(crisis.World.Family.Id.Value, Is.EqualTo("family.akechi"));
            Assert.That(crisis.World.PersonCount, Is.EqualTo(4));
            Assert.That(crisis.HeadPersonId, Is.EqualTo(SuccessionSample.StandardHeadId));
            Assert.That(crisis.DesignatedHeirId, Is.EqualTo(SuccessionSample.StandardDesignatedHeirId));
            Assert.That(SuccessionService.GetCandidates(crisis, Start).Count, Is.Zero);
            Assert.That(SuccessionService.CanOfferCrisis(crisis, Start), Is.True);
        }

        [Test]
        public void StandardRoster_ListsOnlyLivingQualifiedCandidatesAndFlagsDeadDesignation()
        {
            SuccessionState state = SuccessionSample.CreateStandard();

            IReadOnlyList<SuccessionCandidate> candidates = SuccessionService.GetCandidates(state, Start);

            Assert.That(candidates.Count, Is.EqualTo(3));
            Assert.That(Ids(candidates), Does.Contain("person.akechi.masatsuna"));
            Assert.That(Ids(candidates), Does.Contain("person.akechi.tokihime"));
            Assert.That(Ids(candidates), Does.Contain("person.akechi.nobumasa"));
            Assert.That(Ids(candidates), Does.Not.Contain(SuccessionSample.StandardDesignatedHeirId.Value));
            Assert.That(Ids(candidates), Does.Not.Contain("person.akechi.chie"));
            Assert.That(Ids(candidates), Does.Not.Contain("person.retainer.sakon"));
            Assert.That(SuccessionService.IsEligibleSuccessor(state,
                SuccessionSample.StandardDesignatedHeirId, Start), Is.False);

            SuccessionResult begun = SuccessionService.BeginRetirement(state, Start);

            Assert.That(begun.Succeeded, Is.True);
            Assert.That(begun.Outcome, Is.EqualTo(SuccessionOutcome.DesignatedHeirInvalid));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(SuccessionService.GetCandidates(state, Start).Count, Is.EqualTo(3));
        }

        [Test]
        public void BeginRetirement_IsIdempotentAndDoesNotResetPendingSelection()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            SuccessionService.BeginRetirement(state, Start);
            Assert.That(SuccessionService.SelectSuccessor(state,
                new PersonId("person.akechi.masatsuna"), Start).Succeeded, Is.True);
            PersonId selected = state.PendingSuccessorId;

            SuccessionResult repeated = SuccessionService.BeginRetirement(state, Start);

            Assert.That(repeated.Succeeded, Is.True);
            Assert.That(repeated.Outcome, Is.EqualTo(SuccessionOutcome.AlreadyPending));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(state.PendingSuccessorId, Is.EqualTo(selected));
        }

        [Test]
        public void AdultHandoff_TransfersHeadExactlyOnceAndPreservesIdsAndRoster()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            int personsBefore = state.World.PersonCount;
            int kinshipBefore = state.World.Kinship.Count;
            var successorId = new PersonId("person.akechi.masatsuna");

            SuccessionService.BeginRetirement(state, Start);
            Assert.That(SuccessionService.SelectSuccessor(state, successorId, Start).Succeeded, Is.True);
            SuccessionResult confirmed = SuccessionService.ConfirmSuccession(state, Start);

            Assert.That(confirmed.Succeeded, Is.True, confirmed.Message);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(state.HeadPersonId, Is.EqualTo(successorId));
            Assert.That(state.SuccessionCount, Is.EqualTo(1));
            Assert.That(state.CompletedResultId, Does.StartWith("succession.1.person.akechi.masatsuna"));
            Assert.That(state.World.PersonCount, Is.EqualTo(personsBefore));
            Assert.That(state.World.Kinship.Count, Is.EqualTo(kinshipBefore));
            Assert.That(state.World.Family.TryGetRole(SuccessionSample.StandardHeadId,
                out FamilyMemberRole oldRole), Is.True);
            Assert.That(oldRole, Is.Not.EqualTo(FamilyMemberRole.Head));
            Assert.That(state.World.Family.TryGetRole(successorId, out FamilyMemberRole newRole), Is.True);
            Assert.That(newRole, Is.EqualTo(FamilyMemberRole.Head));
            Assert.That(state.World.TryGetLife(SuccessionSample.StandardHeadId,
                out SuccessionPersonLife oldLife), Is.True);
            Assert.That(oldLife.Alive, Is.True);
            Assert.That(oldLife.Retired, Is.True);
            Assert.That(state.DesignatedHeirId.IsEmpty, Is.True,
                "A completed transfer must not keep the previous reign's designation.");

            SuccessionResult again = SuccessionService.ConfirmSuccession(state, Start);

            Assert.That(again.Succeeded, Is.False);
            Assert.That(again.Outcome, Is.EqualTo(SuccessionOutcome.NotPending));
            Assert.That(state.HeadPersonId, Is.EqualTo(successorId));
            Assert.That(state.SuccessionCount, Is.EqualTo(1));
        }

        [Test]
        public void MinorSuccessor_RequiresEligibleAdultRegentAndCompletesAtThreshold()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            var minorId = new PersonId("person.akechi.tokihime");
            var regentId = new PersonId("person.akechi.chie");

            SuccessionService.BeginRetirement(state, Start);
            SuccessionResult selected = SuccessionService.SelectSuccessor(state, minorId, Start);

            Assert.That(selected.Succeeded, Is.True);
            Assert.That(selected.Outcome, Is.EqualTo(SuccessionOutcome.RegentRequired));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingRegentChoice));

            SuccessionResult missingRegent = SuccessionService.ConfirmSuccession(state, Start);
            Assert.That(missingRegent.Succeeded, Is.False);
            Assert.That(missingRegent.Outcome, Is.EqualTo(SuccessionOutcome.RegentRequired));
            Assert.That(state.HeadPersonId, Is.EqualTo(SuccessionSample.StandardHeadId));

            Assert.That(SuccessionService.SelectRegent(state, regentId, Start).Succeeded, Is.True);
            SuccessionResult confirmed = SuccessionService.ConfirmSuccession(state, Start);

            Assert.That(confirmed.Succeeded, Is.True, confirmed.Message);
            Assert.That(confirmed.Message, Does.Contain("完成成年交权"),
                "The regency message must name the explicit confirmation the UI requires.");
            Assert.That(confirmed.Message, Does.Not.Contain("自动"),
                "The regency message must not claim the handoff happens automatically.");
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.Regency));
            Assert.That(state.HeadPersonId, Is.EqualTo(minorId));
            Assert.That(state.PendingRegentId, Is.EqualTo(regentId));
            Assert.That(state.SuccessionCount, Is.EqualTo(1));

            SuccessionResult tooEarly = SuccessionService.TryCompleteRegency(state, Start);
            Assert.That(tooEarly.Succeeded, Is.False);
            Assert.That(tooEarly.Outcome, Is.EqualTo(SuccessionOutcome.NotDue));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.Regency));

            var maturity = new GameDate(1562, 4, 1);
            Assert.That(state.IsRegencyHandoffDue(Start), Is.False);
            Assert.That(state.IsRegencyHandoffDue(maturity), Is.True);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.Regency),
                "Reaching adulthood must pause for an explicit player handoff instead of transferring silently.");

            SuccessionResult majority = SuccessionService.TryCompleteRegency(state, maturity);

            Assert.That(majority.Succeeded, Is.True, majority.Message);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(state.RegencyCompleted, Is.True);
            Assert.That(state.HeadPersonId, Is.EqualTo(minorId));
            Assert.That(state.CompletedResultId, Does.EndWith(".majority"));
            Assert.That(SuccessionService.TryCompleteRegency(state, new GameDate(1563, 4, 1)).Succeeded, Is.False);
        }

        [Test]
        public void InvalidRegent_LeavesPendingStateUnchanged()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            SuccessionService.BeginRetirement(state, Start);
            SuccessionService.SelectSuccessor(state, new PersonId("person.akechi.tokihime"), Start);

            Assert.That(SuccessionService.SelectRegent(state,
                new PersonId("person.akechi.nobumasa"), Start).Outcome,
                Is.EqualTo(SuccessionOutcome.InvalidRegent));
            Assert.That(SuccessionService.SelectRegent(state,
                new PersonId("person.akechi.tokihime"), Start).Outcome,
                Is.EqualTo(SuccessionOutcome.InvalidRegent));
            Assert.That(SuccessionService.SelectRegent(state,
                SuccessionSample.StandardHeadId, Start).Outcome,
                Is.EqualTo(SuccessionOutcome.InvalidRegent));
            Assert.That(SuccessionService.SelectRegent(state,
                SuccessionSample.StandardDesignatedHeirId, Start).Outcome,
                Is.EqualTo(SuccessionOutcome.InvalidRegent));
            Assert.That(state.PendingRegentId.IsEmpty, Is.True);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingRegentChoice));

            Assert.That(SuccessionService.SelectRegent(state,
                new PersonId("person.retainer.sakon"), Start).Succeeded, Is.True);
            Assert.That(state.PendingRegentId, Is.EqualTo(new PersonId("person.retainer.sakon")));
        }

        [Test]
        public void CrisisDecision_AcceptAdoptionCreatesAdultHeirAndRecordsCrisisMarker()
        {
            SuccessionState state = SuccessionSample.CreateCrisis();
            int personsBefore = state.World.PersonCount;

            SuccessionResult begun = SuccessionService.BeginRetirement(state, Start);

            Assert.That(begun.Succeeded, Is.True);
            Assert.That(begun.Outcome, Is.EqualTo(SuccessionOutcome.NoCandidates));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));
            Assert.That(SuccessionService.CanOfferCrisis(state, Start), Is.True);

            SuccessionResult accepted = SuccessionService.AcceptCrisisAdoption(state, Start);

            Assert.That(accepted.Succeeded, Is.True, accepted.Message);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(state.HeadPersonId.Value, Does.StartWith("person.adopted.family.akechi."));
            Assert.That(state.World.PersonCount, Is.EqualTo(personsBefore + 1));
            Assert.That(state.CrisisAdoptionUsed, Is.True);
            Assert.That(state.SuccessionCount, Is.EqualTo(1));
            Assert.That(state.CompletedResultId, Does.StartWith("crisis.succession.1."));
            Assert.That(state.World.TryGetPerson(state.HeadPersonId, out PersonState head), Is.True);
            Assert.That(head.DisplayName, Is.EqualTo("養子 宗次"));
            Assert.That(state.World.Family.TryGetRole(state.HeadPersonId, out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Head));
            Assert.That(state.World.Kinship.Contains(new KinshipLink(SuccessionSample.StandardHeadId,
                state.HeadPersonId, KinshipType.Adoption)), Is.True);

            int after = state.World.PersonCount;
            Assert.That(SuccessionService.AcceptCrisisAdoption(state, Start).Succeeded, Is.False);
            Assert.That(state.World.PersonCount, Is.EqualTo(after));
        }

        [Test]
        public void CrisisDecision_DeclineEndsFamilyExplicitly()
        {
            SuccessionState state = SuccessionSample.CreateCrisis();
            SuccessionService.BeginRetirement(state, Start);

            SuccessionResult declined = SuccessionService.DeclineCrisis(state);

            Assert.That(declined.Succeeded, Is.True);
            Assert.That(declined.Outcome, Is.EqualTo(SuccessionOutcome.Extinction));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            Assert.That(state.CompletedResultId, Does.StartWith("extinction."));
            Assert.That(SuccessionService.BeginRetirement(state, Start).Succeeded, Is.False);
            Assert.That(SuccessionService.AcceptCrisisAdoption(state, Start).Succeeded, Is.False);
        }

        [Test]
        public void NoCandidatesWithoutSponsor_EndsFamilyImmediately()
        {
            SuccessionState state = SuccessionSample.CreateCrisis();
            SetLife(state, new PersonId("person.akechi.chie"), 1532, false, false);
            SetLife(state, new PersonId("person.retainer.sakon"), 1520, false, false);

            SuccessionResult begun = SuccessionService.BeginRetirement(state, Start);

            Assert.That(begun.Succeeded, Is.True);
            Assert.That(begun.Outcome, Is.EqualTo(SuccessionOutcome.Extinction));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            Assert.That(SuccessionService.CanOfferCrisis(state, Start), Is.False);
        }

        [Test]
        public void CandidateDeathBeforeConfirm_RejectsHandoffWithoutMutation()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            var successorId = new PersonId("person.akechi.masatsuna");
            SuccessionService.BeginRetirement(state, Start);
            SuccessionService.SelectSuccessor(state, successorId, Start);
            SetLife(state, successorId, 1536, false, false);

            SuccessionResult confirmed = SuccessionService.ConfirmSuccession(state, Start);

            Assert.That(confirmed.Succeeded, Is.False);
            Assert.That(confirmed.Outcome, Is.EqualTo(SuccessionOutcome.CandidateNotEligible));
            Assert.That(state.HeadPersonId, Is.EqualTo(SuccessionSample.StandardHeadId));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(state.World.Family.TryGetRole(SuccessionSample.StandardHeadId,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Head));
        }

        [Test]
        public void BeginRetirement_HonorsValidAdultDesignatedHeirImmediately()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            var heirId = new PersonId("person.akechi.masatsuna");
            state.DesignatedHeirId = heirId;

            SuccessionResult begun = SuccessionService.BeginRetirement(state, Start);

            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(state.HeadPersonId, Is.EqualTo(heirId));
            Assert.That(state.SuccessionCount, Is.EqualTo(1));
            Assert.That(state.DesignatedHeirId.IsEmpty, Is.True,
                "Stale designation must be cleared when the designated heir takes office.");
        }

        [Test]
        public void BeginRetirement_ValidMinorDesignatedHeirKeepsSelectionAndRequiresRegent()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            var heirId = new PersonId("person.akechi.tokihime");
            state.DesignatedHeirId = heirId;

            SuccessionResult begun = SuccessionService.BeginRetirement(state, Start);

            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(begun.Outcome, Is.EqualTo(SuccessionOutcome.RegentRequired));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingRegentChoice));
            Assert.That(state.PendingSuccessorId, Is.EqualTo(heirId));
            Assert.That(state.HeadPersonId, Is.EqualTo(SuccessionSample.StandardHeadId));
        }

        [Test]
        public void MinorSuccessorWithoutEligibleRegent_EntersCrisisDecisionInsteadOfDeadlocking()
        {
            SuccessionState state = CreateRegentlessMinorState();

            SuccessionService.BeginRetirement(state, Start);
            SuccessionResult selected = SuccessionService.SelectSuccessor(state,
                new PersonId("person.test.minor"), Start);

            Assert.That(selected.Succeeded, Is.True, selected.Message);
            Assert.That(selected.Outcome, Is.EqualTo(SuccessionOutcome.RegentUnavailable));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));
            Assert.That(state.PendingSuccessorId.IsEmpty, Is.True);
            Assert.That(state.PendingRegentId.IsEmpty, Is.True);
            Assert.That(SuccessionService.CanOfferCrisis(state, Start), Is.True);
        }

        [Test]
        public void RegentRoles_ExcludeDisciplesAndGuests()
        {
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.Spouse), Is.True);
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.DirectKin), Is.True);
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.CollateralKin), Is.True);
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.AdoptedChild), Is.True);
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.Retainer), Is.True);
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.Disciple), Is.False);
            Assert.That(SuccessionService.IsRegentRole(FamilyMemberRole.Guest), Is.False);

            SuccessionState state = SuccessionSample.CreateStandard();
            var retainerId = new PersonId("person.retainer.sakon");
            Assert.That(state.World.TrySetRole(retainerId, FamilyMemberRole.Disciple,
                out string error), Is.True, error);
            SuccessionService.BeginRetirement(state, Start);

            SuccessionResult selected = SuccessionService.SelectSuccessor(state,
                new PersonId("person.akechi.tokihime"), Start);

            Assert.That(selected.Succeeded, Is.True, selected.Message);
            Assert.That(SuccessionService.SelectRegent(state, retainerId, Start).Outcome,
                Is.EqualTo(SuccessionOutcome.InvalidRegent));
        }

        [Test]
        public void RetiredCurrentHead_IsRejectedForRetirementAndLoadRules()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            SetLife(state, SuccessionSample.StandardHeadId, 1528, true, true);

            SuccessionResult begun = SuccessionService.BeginRetirement(state, Start);

            Assert.That(begun.Succeeded, Is.False);
            Assert.That(begun.Outcome, Is.EqualTo(SuccessionOutcome.InvalidHead));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));
        }

        [Test]
        public void CrisisAdoption_WhenHeadCannotTransfer_LeavesRosterUnchanged()
        {
            SuccessionState state = SuccessionSample.CreateCrisis();
            SuccessionService.BeginRetirement(state, Start);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));
            int personsBefore = state.World.PersonCount;
            int membersBefore = state.World.Family.Members.Count;
            SetLife(state, SuccessionSample.StandardHeadId, 1528, true, true);

            SuccessionResult accepted = SuccessionService.AcceptCrisisAdoption(state, Start);

            Assert.That(accepted.Succeeded, Is.False);
            Assert.That(accepted.Outcome, Is.EqualTo(SuccessionOutcome.InvalidHead));
            Assert.That(state.World.PersonCount, Is.EqualTo(personsBefore));
            Assert.That(state.World.Family.Members.Count, Is.EqualTo(membersBefore));
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));
            Assert.That(state.CrisisAdoptionUsed, Is.False);
        }

        private static SuccessionState CreateRegentlessMinorState()
        {
            var world = new SuccessionWorld(new FamilyId("family.test"), "测试家");
            AddPerson(world, new PersonId("person.test.head"), "测试 家主", 1528,
                FamilyMemberRole.Head);
            AddPerson(world, new PersonId("person.test.minor"), "测试 幼主", 1548,
                FamilyMemberRole.DirectKin);
            AddPerson(world, new PersonId("person.test.disciple"), "测试 弟子", 1530,
                FamilyMemberRole.Disciple);
            return new SuccessionState(world)
            {
                HeadPersonId = new PersonId("person.test.head")
            };
        }

        private static void AddPerson(SuccessionWorld world, PersonId personId, string name,
            int birthYear, FamilyMemberRole role)
        {
            Assert.That(world.TryAddPerson(new PersonState(personId, name),
                new SuccessionPersonLife(birthYear, true, false), out string error), Is.True, error);
            Assert.That(world.TryAddMember(personId, role, out error), Is.True, error);
        }

        private static List<string> Ids(IReadOnlyList<SuccessionCandidate> candidates)
        {
            var ids = new List<string>();
            foreach (SuccessionCandidate candidate in candidates)
                ids.Add(candidate.PersonId.Value);
            return ids;
        }

        private static void SetLife(SuccessionState state, PersonId personId, int birthYear,
            bool alive, bool retired)
        {
            Assert.That(state.World.TrySetLife(personId,
                new SuccessionPersonLife(birthYear, alive, retired), out string error), Is.True, error);
        }
    }
}
