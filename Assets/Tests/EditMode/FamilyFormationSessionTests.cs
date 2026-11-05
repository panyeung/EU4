using System.Linq;
using System.Text;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using UnityEngine;
using UnityEngine.TestTools;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-022 Task 2: atomic marriage/adoption commands and candidate projection.</summary>
    public sealed class FamilyFormationSessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitFamilyFormationDemoScenario();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Marriage_AllowsMultipleSpousesAndWritesOneReadableChronicleEach()
        {
            FamilyFormationCommandResult first = _session.TryRegisterMarriage(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId);

            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.MarriageCandidateOneId, out FamilyMemberRole role),
                Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(HasSpouse(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId), Is.True);
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(_session.ChronicleEntries[0].Category,
                Is.EqualTo(ChronicleCategory.FamilyFormation));
            Assert.That(_session.ChronicleEntries[0].Title, Is.EqualTo("家族婚姻"));
            Assert.That(_session.ChronicleEntries[0].Summary,
                Does.Contain(SC022FamilyFormationScenario.HeadName));
            Assert.That(_session.ChronicleEntries[0].Summary,
                Does.Contain(SC022FamilyFormationScenario.MarriageCandidateOneName));
            Assert.That(_session.ChronicleEntries[0].Summary, Does.Not.Contain("person."));

            FamilyFormationCommandResult second = _session.TryRegisterMarriage(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateTwoId);

            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(HasSpouse(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateTwoId), Is.True);
            Assert.That(HasSpouse(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SpouseId), Is.True,
                "the existing spouse fact must survive a new marriage");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(2));

            FamilyFormationView view = _session.GetFamilyFormationView();
            Assert.That(view.MarriageCandidatesFor(SC022FamilyFormationScenario.HeadId)
                .Any(candidate => candidate.PersonId ==
                    SC022FamilyFormationScenario.MarriageCandidateOneId), Is.False);
            Assert.That(view.MarriageCandidatesFor(SC022FamilyFormationScenario.HeadId)
                .Any(candidate => candidate.PersonId ==
                    SC022FamilyFormationScenario.MarriageCandidateTwoId), Is.False);
        }

        [Test]
        public void Marriage_SiblingCandidateIsNotSeparatelyBlocked()
        {
            FamilyFormationCommandResult result = _session.TryRegisterMarriage(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(HasSibling(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId), Is.True,
                "the existing sibling fact must be preserved");
            Assert.That(HasSpouse(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId), Is.True);
        }

        [Test]
        public void Marriage_RejectsSelfDuplicateDirectLinesMinorsDeadAndNonScenario()
        {
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.HeadId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.SelfRelation));

            _session = Fresh();
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Succeeded, Is.True);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.DuplicateRelation));

            _session = Fresh();
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.DirectLineRelation));

            _session = Fresh();
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.AdoptedChildCandidateId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.DirectLineRelation));

            _session = Fresh();
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.CandidateNotEligible));

            _session = Fresh();
            _session.Succession.World.TrySetLife(SC022FamilyFormationScenario.MarriageCandidateOneId,
                new SuccessionPersonLife(
                    SC022FamilyFormationScenario.MarriageCandidateOneBirthYear, false, false), out _);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.InvalidCandidate));

            _session = Fresh();
            _session.Succession.World.TrySetLife(SC022FamilyFormationScenario.HeadId,
                new SuccessionPersonLife(SC022FamilyFormationScenario.HeadBirthYear, false, false),
                out _);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.InvalidInitiator));

            _session = Fresh();
            _session.Succession.World.TrySetLife(SC022FamilyFormationScenario.HeadId,
                new SuccessionPersonLife(1550, true, false), out _);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.InvalidInitiator));

            _session = Fresh();
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                new PersonId("person.sc022.outside")).Outcome,
                Is.EqualTo(FamilyFormationOutcome.InvalidCandidate));
        }

        [Test]
        public void Adoption_ChildAndAdultSucceedAndPreserveExistingBloodFacts()
        {
            string before = KinshipSnapshot();
            FamilyFormationCommandResult child = _session.TryAdopt(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId);

            Assert.That(child.Succeeded, Is.True, child.Message);
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.ChildAdoptionCandidateId, out FamilyMemberRole role),
                Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.AdoptedChild));
            Assert.That(HasAdoption(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId), Is.True);
            Assert.That(KinshipSnapshot(), Does.Contain(before),
                "all pre-existing kinship facts must be preserved");
            Assert.That(_session.ChronicleEntries.Single().Category,
                Is.EqualTo(ChronicleCategory.FamilyFormation));
            Assert.That(_session.ChronicleEntries.Single().Title, Is.EqualTo("家族收养"));
            Assert.That(_session.ChronicleEntries.Single().Summary,
                Does.Contain(SC022FamilyFormationScenario.ChildAdoptionCandidateName));

            _session = Fresh();
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.AdultAdoptionCandidateId).Succeeded, Is.True);

            _session = Fresh();
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId).Succeeded, Is.True);
            Assert.That(HasSibling(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId), Is.True);
            Assert.That(HasAdoption(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId), Is.True);
        }

        [Test]
        public void Adoption_RejectsSelfSpouseDuplicateCycleMemberAndNonScenario()
        {
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.HeadId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.SelfRelation));

            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SpouseId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.SpouseRelation));

            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.AdoptedChildCandidateId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.DuplicateRelation));

            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.AdoptionCycle));

            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.CollateralId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.AlreadyMember));

            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                new PersonId("person.sc022.outside")).Outcome,
                Is.EqualTo(FamilyFormationOutcome.InvalidCandidate));
        }

        [Test]
        public void RejectedCommands_LeaveMembershipKinshipChronicleSuccessionDateResourcesAndRngUnchanged()
        {
            string before = Snapshot();
            _session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId);
            _session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.HeadId);
            _session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SpouseId);
            _session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId);
            _session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                new PersonId("person.sc022.outside"));

            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void SuccessfulCommands_DoNotChangeDateResourcesRngOrSuccessionState()
        {
            SuccessionPhase phase = _session.Succession.Phase;
            PersonId head = _session.Succession.HeadPersonId;
            PersonId heir = _session.Succession.DesignatedHeirId;
            int count = _session.Succession.SuccessionCount;
            GameDate date = _session.WorldClock.CurrentDate;
            IndustryResources resources = _session.Industry.Resources;
            string rng = JsonUtility.ToJson(_session.RandomState.CaptureState());

            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Succeeded, Is.True);
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId).Succeeded, Is.True);

            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.Succession.DesignatedHeirId, Is.EqualTo(heir));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(count));
            Assert.That(_session.Succession.CrisisAdoptionUsed, Is.False);
            Assert.That(_session.Succession.RegencyCompleted, Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(JsonUtility.ToJson(_session.RandomState.CaptureState()), Is.EqualTo(rng));
        }

        [Test]
        public void Marriage_ChronicleAppendFailure_RollsBackMembershipKinshipAndState()
        {
            SuccessionPhase phase = _session.Succession.Phase;
            PersonId head = _session.Succession.HeadPersonId;
            PersonId heir = _session.Succession.DesignatedHeirId;
            int count = _session.Succession.SuccessionCount;
            GameDate date = _session.WorldClock.CurrentDate;
            IndustryResources resources = _session.Industry.Resources;
            string rng = JsonUtility.ToJson(_session.RandomState.CaptureState());
            string kinshipBefore = KinshipSnapshot();
            string membersBefore = MembershipSnapshot();

            _session.FamilyFormationAppendFaultInjection = ledger =>
                Assert.That(ledger.TryAppendFamilyFormation(date.Year, date.Month,
                    "person.sc022.injected", FamilyChronicleEntry.MarriageSourceId,
                    "注入记录", "测试注入记录", out _, out _), Is.True);
            LogAssert.Expect(LogType.Error,
                "家族编年史记录写入失败（家族婚姻）：编年史预备记录序号无效。");

            FamilyFormationCommandResult result = _session.TryRegisterMarriage(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(FamilyFormationOutcome.ChronicleFailed));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.MarriageCandidateOneId, out _), Is.False);
            Assert.That(HasSpouse(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId), Is.False);
            Assert.That(KinshipSnapshot(), Is.EqualTo(kinshipBefore));
            Assert.That(MembershipSnapshot(), Is.EqualTo(membersBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(_session.ChronicleEntries[0].Title, Is.EqualTo("注入记录"));
            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.Succession.DesignatedHeirId, Is.EqualTo(heir));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(count));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(JsonUtility.ToJson(_session.RandomState.CaptureState()), Is.EqualTo(rng));

            _session.FamilyFormationAppendFaultInjection = null;
        }

        [Test]
        public void Adoption_ChronicleAppendFailure_RollsBackMembershipKinshipAndState()
        {
            SuccessionPhase phase = _session.Succession.Phase;
            PersonId head = _session.Succession.HeadPersonId;
            PersonId heir = _session.Succession.DesignatedHeirId;
            int count = _session.Succession.SuccessionCount;
            GameDate date = _session.WorldClock.CurrentDate;
            IndustryResources resources = _session.Industry.Resources;
            string rng = JsonUtility.ToJson(_session.RandomState.CaptureState());
            string kinshipBefore = KinshipSnapshot();
            string membersBefore = MembershipSnapshot();

            _session.FamilyFormationAppendFaultInjection = ledger =>
                Assert.That(ledger.TryAppendFamilyFormation(date.Year, date.Month,
                    "person.sc022.injected", FamilyChronicleEntry.AdoptionSourceId,
                    "注入记录", "测试注入记录", out _, out _), Is.True);
            LogAssert.Expect(LogType.Error,
                "家族编年史记录写入失败（家族收养）：编年史预备记录序号无效。");

            FamilyFormationCommandResult result = _session.TryAdopt(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Outcome, Is.EqualTo(FamilyFormationOutcome.ChronicleFailed));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC022FamilyFormationScenario.ChildAdoptionCandidateId, out _), Is.False);
            Assert.That(HasAdoption(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId), Is.False);
            Assert.That(KinshipSnapshot(), Is.EqualTo(kinshipBefore));
            Assert.That(MembershipSnapshot(), Is.EqualTo(membersBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(1));
            Assert.That(_session.ChronicleEntries[0].Title, Is.EqualTo("注入记录"));
            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.Succession.DesignatedHeirId, Is.EqualTo(heir));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(count));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(JsonUtility.ToJson(_session.RandomState.CaptureState()), Is.EqualTo(rng));

            _session.FamilyFormationAppendFaultInjection = null;
        }

        [Test]
        public void PendingEvent_BlocksCommandsWithoutChangingState()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Not.Empty);
            string before = Snapshot();

            FamilyFormationCommandResult result = _session.TryRegisterMarriage(
                SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId);

            Assert.That(result.Outcome, Is.EqualTo(FamilyFormationOutcome.Blocked));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void View_FiltersBySelectionAndShowsExistingSpouse()
        {
            FamilyFormationView view = _session.GetFamilyFormationView();

            Assert.That(view.IsScenarioActive, Is.True);
            Assert.That(view.Members.Select(member => member.PersonId),
                Is.EquivalentTo(new[]
                {
                    SC022FamilyFormationScenario.HeadId,
                    SC022FamilyFormationScenario.SpouseId,
                    SC022FamilyFormationScenario.CollateralId
                }));
            Assert.That(view.SpouseNamesFor(SC022FamilyFormationScenario.HeadId),
                Does.Contain(SC022FamilyFormationScenario.SpouseName));

            string[] marriageNames = view.MarriageCandidatesFor(
                    SC022FamilyFormationScenario.HeadId)
                .Select(candidate => candidate.DisplayName).ToArray();
            Assert.That(marriageNames, Does.Contain(SC022FamilyFormationScenario.MarriageCandidateOneName));
            Assert.That(marriageNames, Does.Contain(SC022FamilyFormationScenario.MarriageCandidateTwoName));
            Assert.That(marriageNames, Does.Contain(SC022FamilyFormationScenario.SiblingCandidateName));
            Assert.That(marriageNames, Does.Not.Contain(SC022FamilyFormationScenario.BloodAncestorName));
            Assert.That(marriageNames, Does.Not.Contain(SC022FamilyFormationScenario.AdoptedChildCandidateName));

            string[] adoptionNames = view.AdoptionCandidatesFor(
                    SC022FamilyFormationScenario.HeadId)
                .Select(candidate => candidate.DisplayName).ToArray();
            Assert.That(adoptionNames, Does.Contain(SC022FamilyFormationScenario.ChildAdoptionCandidateName));
            Assert.That(adoptionNames, Does.Contain(SC022FamilyFormationScenario.AdultAdoptionCandidateName));
            Assert.That(adoptionNames, Does.Not.Contain(SC022FamilyFormationScenario.BloodAncestorName));
            Assert.That(adoptionNames, Does.Not.Contain(SC022FamilyFormationScenario.AdoptedChildCandidateName));

            Assert.That(view.RelationNote(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SiblingCandidateId), Is.EqualTo("兄弟姐妹"));
            Assert.That(view.RelationNote(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId), Is.EqualTo("直系血亲/养亲"));
        }

        [Test]
        public void NonScenarioSession_RejectsCommandsAndReportsInactiveView()
        {
            _session = Fresh();
            _session.InitStandardDemo();

            Assert.That(_session.GetFamilyFormationView().IsScenarioActive, Is.False);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Outcome,
                Is.EqualTo(FamilyFormationOutcome.InvalidState));
        }

        private GameSession Fresh()
        {
            Object.DestroyImmediate(_session.gameObject);
            _session = GameSession.NewGame();
            _session.InitFamilyFormationDemoScenario();
            return _session;
        }

        private bool HasSpouse(PersonId left, PersonId right) =>
            _session.Succession.World.Kinship.Contains(
                new KinshipLink(left, right, KinshipType.Spouse));

        private bool HasSibling(PersonId left, PersonId right) =>
            _session.Succession.World.Kinship.Contains(
                new KinshipLink(left, right, KinshipType.Sibling));

        private bool HasAdoption(PersonId from, PersonId to) =>
            _session.Succession.World.Kinship.Contains(
                new KinshipLink(from, to, KinshipType.Adoption));

        private string KinshipSnapshot()
        {
            var builder = new StringBuilder();
            foreach (KinshipLink link in _session.Succession.World.Kinship.Links)
                builder.Append(link).Append(';');
            return builder.ToString();
        }

        private string MembershipSnapshot()
        {
            var builder = new StringBuilder();
            foreach (FamilyMembership member in _session.Succession.World.Family.Members)
                builder.Append(member.PersonId).Append(':').Append((int)member.Role).Append(';');
            return builder.ToString();
        }

        private string Snapshot()
        {
            var builder = new StringBuilder();
            builder.Append((int)_session.Succession.Phase).Append('|')
                .Append(_session.Succession.HeadPersonId).Append('|')
                .Append(_session.Succession.DesignatedHeirId).Append('|')
                .Append(_session.Succession.PendingSuccessorId).Append('|')
                .Append(_session.Succession.PendingRegentId).Append('|')
                .Append(_session.Succession.SuccessionCount).Append('|')
                .Append(_session.Succession.CrisisAdoptionUsed).Append('|')
                .Append(_session.Succession.RegencyCompleted).Append('|')
                .Append(_session.WorldClock.CurrentDate.Year).Append('-')
                .Append(_session.WorldClock.CurrentDate.Month).Append('-')
                .Append(_session.WorldClock.CurrentDate.Day).Append('|')
                .Append(_session.Industry.Resources.Funds).Append('/')
                .Append(_session.Industry.Resources.Supplies).Append('/')
                .Append(_session.Industry.Resources.Intelligence).Append('|')
                .Append(JsonUtility.ToJson(_session.RandomState.CaptureState())).Append('|');
            foreach (FamilyMembership member in _session.Succession.World.Family.Members)
                builder.Append(member.PersonId).Append(':').Append((int)member.Role).Append(';');
            builder.Append('|');
            foreach (KinshipLink link in _session.Succession.World.Kinship.Links)
                builder.Append(link).Append(';');
            builder.Append('|');
            foreach (FamilyChronicleEntry entry in _session.ChronicleEntries)
                builder.Append(entry.EntryId).Append(':').Append((int)entry.Category).Append(':')
                    .Append(entry.Title).Append(':').Append(entry.Summary).Append(';');
            return builder.ToString();
        }
    }
}
