using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M06 vertical slice: start gating, the session-only operation, one terminal
    /// settlement per result, atomic chronicle rollback, replay prevention, save/load
    /// refusal during the operation and terminal persistence through the existing
    /// chronicle and resource systems.
    /// </summary>
    public class M06TacticalMissionSessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Start_NeedsTwoEligibleMembersAtKiyosuAndSpendsNothing()
        {
            List<PersonId> eligible = Eligible(_session);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2),
                "the fixed roster must offer two free members at 清洲.");

            IndustryResources before = _session.Industry.Resources;
            int chronicle = _session.ChronicleEntries.Count;

            Assert.That(_session.TryBeginTacticalMission(eligible[0], eligible[0]).Succeeded,
                Is.False, "the same person cannot fill both slots.");

            TacticalActionResult start =
                _session.TryBeginTacticalMission(eligible[0], eligible[1]);
            Assert.That(start.Succeeded, Is.True, start.Feedback);
            Assert.That(_session.IsTacticalMissionActive, Is.True);
            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.InProgress));
            Assert.That(_session.TacticalMissionStatus.Operatives.Count, Is.EqualTo(2));
            Assert.That(_session.Industry.Resources, Is.EqualTo(before),
                "the announced cost is applied once at the terminal result, not at start.");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));

            Assert.That(_session.TryBeginTacticalMission(eligible[0], eligible[1]).Succeeded,
                Is.False, "a second start must be refused.");
            Assert.That(_session.Industry.Resources, Is.EqualTo(before));
        }

        [Test]
        public void Start_RefusesAWrongLocationWithoutTouchingResources()
        {
            List<PersonId> eligible = Eligible(_session);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2));
            TravelPreview preview = _session.PreviewTravel(LocationId.Nagoya).Value;
            Assert.That(_session.ConfirmTravel(preview), Is.True);

            IndustryResources before = _session.Industry.Resources;
            TacticalActionResult start =
                _session.TryBeginTacticalMission(eligible[0], eligible[1]);

            Assert.That(start.Succeeded, Is.False);
            Assert.That(start.Feedback, Does.Contain("清洲"));
            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_session.Industry.Resources, Is.EqualTo(before));
        }

        [Test]
        public void Start_RefusesWhenEverybodyIsBusyWithoutTouchingResources()
        {
            var baseline = new List<CommissionAssigneeOption>(_session.TacticalOperativeOptions);
            Assert.That(baseline.Count, Is.GreaterThanOrEqualTo(2));
            foreach (CommissionAssigneeOption option in baseline)
            {
                if (!option.Eligible)
                    continue;
                Assert.That(_session.TryPlanDailyAction(option.PersonId, DailyActionKind.Rest,
                    2, default, out string reason), Is.True, reason);
            }

            int eligible = 0;
            PersonId first = default;
            PersonId second = default;
            foreach (CommissionAssigneeOption option in _session.TacticalOperativeOptions)
            {
                if (!option.Eligible)
                    continue;
                eligible++;
                if (first.IsEmpty) first = option.PersonId;
                else if (second.IsEmpty) second = option.PersonId;
            }
            Assert.That(eligible, Is.Zero, "every family member is now busy.");

            IndustryResources before = _session.Industry.Resources;
            int chronicle = _session.ChronicleEntries.Count;
            Assert.That(_session.CanBeginTacticalMission, Is.False);
            Assert.That(_session.TryBeginTacticalMission(first, second).Succeeded, Is.False);
            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_session.Industry.Resources, Is.EqualTo(before));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));
        }

        [Test]
        public void Withdraw_ChargesTheCostOnceAndRecordsOneTerminalRow()
        {
            List<PersonId> eligible = Eligible(_session);
            Assert.That(_session.TryBeginTacticalMission(eligible[0], eligible[1]).Succeeded,
                Is.True);
            IndustryResources before = _session.Industry.Resources;

            TacticalActionResult withdraw = _session.TryTacticalWithdraw();

            Assert.That(withdraw.Succeeded, Is.True, withdraw.Feedback);
            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Withdrawn));
            IndustryResources after = _session.Industry.Resources;
            Assert.That(after.Supplies, Is.EqualTo(before.Supplies -
                TacticalMissionScenario.SupplyCost));
            Assert.That(after.Funds, Is.EqualTo(before.Funds), "withdrawal earns no funds.");
            Assert.That(after.Intelligence, Is.EqualTo(before.Intelligence));
            Assert.That(_session.TacticalAppliedSupplies,
                Is.EqualTo(-TacticalMissionScenario.SupplyCost));

            FamilyChronicleEntry row = LastRecord(_session);
            Assert.That(row, Is.Not.Null);
            Assert.That(row.SubjectId, Is.EqualTo(FamilyChronicleEntry.TacticalMissionId));
            Assert.That(row.SourceId, Is.EqualTo("withdrawn"));
            Assert.That(row.ToStage, Is.EqualTo((int)TacticalMissionStage.Withdrawn));
            Assert.That(row.Summary, Does.Contain("撤退").And.Contain("成员："));

            int chronicle = _session.ChronicleEntries.Count;
            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.False);
            Assert.That(_session.TryTacticalExtract().Succeeded, Is.False);
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));
            Assert.That(_session.Industry.Resources, Is.EqualTo(after),
                "a repeated terminal command must not charge the cost twice.");
        }

        [Test]
        public void ActiveOperation_BlocksDayMonthAndTravelWithoutChangingTheDate()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            TravelPreview preview = _session.PreviewTravel(LocationId.Nagoya).Value;

            Assert.That(_session.AdvanceOneDay(), Is.False);
            Assert.That(_session.AdvanceDays(3), Is.False);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.ConfirmTravel(preview), Is.False);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date),
                "the campaign calendar must not move during the operation.");
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.TacticalMissionStatus.Turn, Is.Zero);
            Assert.That(_session.IsTacticalMissionActive, Is.True);
            Assert.That(_session.LastTacticalMessage, Does.Contain("潜入"));
        }

        [Test]
        public void FullSuccessRoute_IsPlayableAndPaysTheAnnouncedReward()
        {
            TacticalActionResult start = StartWithTwoFreeMembers();
            Assert.That(start.Succeeded, Is.True, start.Feedback);
            IndustryResources before = _session.Industry.Resources;
            TacticalMissionSnapshot snapshot = _session.TacticalMissionStatus;
            PersonId carrier = snapshot.Operatives[0].PersonId;
            PersonId spotter = snapshot.Operatives[1].PersonId;

            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.East);
            Move(carrier, TacticalDirection.East);
            Move(carrier, TacticalDirection.East);
            Move(carrier, TacticalDirection.East);
            Move(carrier, TacticalDirection.East);

            snapshot = _session.TacticalMissionStatus;
            Assert.That(snapshot.IsActive, Is.True,
                "the authored route must stay concealed from both guards.");
            Assert.That(_session.TryTacticalInteract(carrier).Succeeded, Is.True);
            Assert.That(_session.TacticalMissionStatus.DispatchTaken, Is.True);

            Move(carrier, TacticalDirection.West);
            Move(carrier, TacticalDirection.South);
            Move(carrier, TacticalDirection.South);
            Move(carrier, TacticalDirection.South);
            Move(carrier, TacticalDirection.South);
            Move(carrier, TacticalDirection.West);
            Move(carrier, TacticalDirection.West);
            Move(spotter, TacticalDirection.West);
            Move(carrier, TacticalDirection.West);

            snapshot = _session.TacticalMissionStatus;
            Assert.That(snapshot.Operatives[0].Position,
                Is.EqualTo(TacticalMissionScenario.ExtractionCells[1]));
            Assert.That(snapshot.Operatives[1].Position,
                Is.EqualTo(TacticalMissionScenario.ExtractionCells[0]));

            TacticalActionResult extract = _session.TryTacticalExtract();
            Assert.That(extract.Succeeded, Is.True, extract.Feedback);
            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Succeeded));

            IndustryResources after = _session.Industry.Resources;
            Assert.That(after.Funds,
                Is.EqualTo(before.Funds + TacticalMissionScenario.SuccessFunds));
            Assert.That(after.Intelligence,
                Is.EqualTo(before.Intelligence + TacticalMissionScenario.SuccessIntelligence));
            Assert.That(after.Supplies,
                Is.EqualTo(before.Supplies - TacticalMissionScenario.SupplyCost));
            Assert.That(_session.TacticalAppliedFunds,
                Is.EqualTo(TacticalMissionScenario.SuccessFunds));
            Assert.That(_session.TacticalAppliedIntelligence,
                Is.EqualTo(TacticalMissionScenario.SuccessIntelligence));

            FamilyChronicleEntry row = LastRecord(_session);
            Assert.That(row.SubjectId, Is.EqualTo(FamilyChronicleEntry.TacticalMissionId));
            Assert.That(row.SourceId, Is.EqualTo("succeeded"));
            Assert.That(row.Summary, Does.Contain("成功"));
            Assert.That(row.Summary, Does.Contain("回合 " + _session.TacticalMissionStatus.Turn));
            Assert.That(row.Summary, Does.Contain("资金 +" + TacticalMissionScenario.SuccessFunds));
            // The chronicle renders localized display names, so identity is proven through
            // the terminal record's stable IDs; the row keeps the readable names.
            Assert.That(_session.MissionHistory, Has.Count.EqualTo(1));
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Participants, Is.EquivalentTo(new[] { carrier, spotter }));
            Assert.That(record.CaptainId, Is.EqualTo(carrier),
                "the first operative starts as the briefing captain.");
            Assert.That(row.Summary, Does.Contain(
                    IndustryService.PersonName(_session.Succession, carrier)),
                "the chronicle row names the operative in the player's language.");
            Assert.That(row.Summary, Does.Contain(
                IndustryService.PersonName(_session.Succession, spotter)));
            Assert.That(row.Year, Is.EqualTo(_session.WorldClock.CurrentDate.Year));
            Assert.That(row.Month, Is.EqualTo(_session.WorldClock.CurrentDate.Month),
                "the entry carries the campaign date at launch.");
        }

        [Test]
        public void Failure_KeepsTheCostAndRecordsTheFailedOutcome()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            IndustryResources before = _session.Industry.Resources;
            TacticalMissionSnapshot snapshot = _session.TacticalMissionStatus;
            PersonId caught = snapshot.Operatives[0].PersonId;

            // Walk into the stationary guard's sight and stop on the dispatch cell.
            Move(caught, TacticalDirection.North);
            Move(caught, TacticalDirection.North);
            Move(caught, TacticalDirection.North);
            Move(caught, TacticalDirection.North);
            for (int i = 0; i < 5; i++)
                Move(caught, TacticalDirection.East);
            TacticalActionResult exposure =
                _session.TryTacticalMove(caught, TacticalDirection.East);
            TacticalMissionSnapshot afterExposure = _session.TacticalMissionStatus;
            Assert.That(exposure.Succeeded, Is.False,
                "stepping in front of the stationary guard must end the operation. turn=" +
                afterExposure.Turn + " stage=" + afterExposure.Stage + " op0=" +
                afterExposure.Operatives[0].Position + " guard0=" +
                afterExposure.Guards[0].Position + "/" + afterExposure.Guards[0].State +
                " guard1=" + afterExposure.Guards[1].Position + "/" +
                afterExposure.Guards[1].State + " event=" + afterExposure.LastEvent);

            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Failed));
            IndustryResources after = _session.Industry.Resources;
            Assert.That(after.Supplies,
                Is.EqualTo(before.Supplies - TacticalMissionScenario.SupplyCost));
            Assert.That(after.Funds, Is.EqualTo(before.Funds));
            Assert.That(after.Intelligence, Is.EqualTo(before.Intelligence));

            FamilyChronicleEntry row = LastRecord(_session);
            Assert.That(row.SourceId, Is.EqualTo("failed"));
            Assert.That(row.Summary, Does.Contain("失败"));
        }

        /// <summary>
        /// Production path for the approved severe-failure rule: the manual board never
        /// rolls the mission check, so the terminal settlement's disclosed death checks
        /// are the first Missions draws, one per disclosed participant. Seeding both
        /// draws makes "both survive and are injured" deterministic without bypassing
        /// settlement.
        /// </summary>
        [Test]
        public void SevereFailure_InjuresEveryDisclosedParticipantWhoSurvives()
        {
            SaveData seeded = WithSeededMissionDraws(_session.CaptureSave(), 2,
                draw => draw >= TacticalMissionResolutionRules.DeathCheckPercent);
            SaveOperationResult applied = _session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);

            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            TacticalMissionSnapshot start = _session.TacticalMissionStatus;
            PersonId caught = start.Operatives[0].PersonId;
            PersonId other = start.Operatives[1].PersonId;
            RunIntoTheStationaryGuard(caught);

            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Failed));
            Assert.That(_session.MissionHistory, Has.Count.EqualTo(1));
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Outcome, Is.EqualTo(SharedMissionOutcome.SevereFailure));
            Assert.That(record.DisclosedRiskParticipants, Has.Count.EqualTo(2),
                "both operatives are disclosed before the mission starts.");
            Assert.That(record.ParticipantStates, Has.Count.EqualTo(2));

            for (int i = 0; i < record.DisclosedRiskParticipants.Count; i++)
            {
                PersonId personId = record.DisclosedRiskParticipants[i];
                MissionParticipantState state = ParticipantStateOf(record, personId);
                Assert.That(state.AliveAfter, Is.True,
                    "a disclosed participant who passes the death check survives.");
                Assert.That(state.InjuredAfter, Is.True,
                    "every surviving participant of a severe failure becomes injured: " +
                    personId.Value);
                Assert.That(_session.IsPersonInjured(personId), Is.True,
                    "the injury ledger must hold every disclosed survivor: " + personId.Value);
                Assert.That(_session.Succession.World.IsLiving(personId), Is.True);
            }

            Assert.That(_session.IsPersonInjured(other), Is.True,
                "the second operative is a disclosed risk participant even though only " +
                "the first one was caught.");

            SaveOperationResult validated = SaveDataValidator.Validate(_session.CaptureSave());
            Assert.That(validated.Succeeded, Is.True, validated.Message);
        }

        /// <summary>
        /// The same production path with the fatal draw: the named disclosed participant
        /// dies, the injury ledger does not keep a dead person, and a head's death enters
        /// the existing succession flow so the campaign stays saveable and continuable.
        /// </summary>
        [Test]
        public void SevereFailure_DeathRollKillsTheDisclosedHeadAndSuccessionContinues()
        {
            PersonId head = _session.Succession.HeadPersonId;
            List<PersonId> eligible = Eligible(_session);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2));
            PersonId other = eligible[0] == head ? eligible[1] : eligible[0];
            // The briefing discloses both operatives in deterministic family order, and the
            // death checks run in that same order, so the fatal draw belongs to the head's
            // slot. Ordering is a production decision, not a test fixture choice.
            int headIndex = eligible.IndexOf(head) < eligible.IndexOf(other) ? 0 : 1;
            SaveData seeded = WithSeededMissionDeathChecks(_session.CaptureSave(), headIndex, 2);
            SaveOperationResult applied = _session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);

            head = _session.Succession.HeadPersonId;
            TacticalActionResult start = _session.TryBeginTacticalMission(head, other, head,
                MissionPreparation.None);
            Assert.That(start.Succeeded, Is.True, start.Feedback);
            TacticalMissionSnapshot snapshot = _session.TacticalMissionStatus;
            Assert.That(snapshot.Operatives[headIndex].PersonId, Is.EqualTo(head),
                "the death checks follow the disclosed briefing order.");
            Assert.That(snapshot.Operatives[1 - headIndex].PersonId, Is.EqualTo(other));

            // The authored capture route starts on the first operative's cell, which is a
            // board slot, not an identity: either disclosed participant can raise the alarm.
            RunIntoTheStationaryGuard(snapshot.Operatives[0].PersonId);

            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Failed));
            Assert.That(_session.MissionHistory, Has.Count.EqualTo(1));
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Outcome, Is.EqualTo(SharedMissionOutcome.SevereFailure));
            Assert.That(record.AnyParticipantDied, Is.True);
            Assert.That(record.DisclosedRiskParticipants, Does.Contain(head),
                "the death roll only applies to a participant disclosed in the briefing.");
            Assert.That(ParticipantStateOf(record, head).AliveAfter, Is.False);
            Assert.That(ParticipantStateOf(record, other).AliveAfter, Is.True,
                "only the disclosed head takes the fatal check.");
            Assert.That(_session.Succession.World.IsLiving(head), Is.False);
            Assert.That(_session.IsPersonInjured(head), Is.False,
                "a dead participant cannot also be recorded as injured.");

            Assert.That(_session.Succession.Phase,
                Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice),
                "the dead head enters the existing succession flow instead of leaving an invalid save.");
            SaveOperationResult pending = SaveDataValidator.Validate(_session.CaptureSave());
            Assert.That(pending.Succeeded, Is.True, pending.Message);

            IReadOnlyList<SuccessionCandidate> candidates =
                SuccessionService.GetCandidates(_session.Succession,
                    _session.WorldClock.CurrentDate);
            Assert.That(candidates.Count, Is.GreaterThan(0));
            PersonId successor = default;
            for (int i = 0; i < candidates.Count && successor.IsEmpty; i++)
                if (_session.SelectSuccessionSuccessor(candidates[i].PersonId).Succeeded)
                    successor = candidates[i].PersonId;
            Assert.That(successor.IsEmpty, Is.False,
                "the death succession must accept one legal successor: " +
                _session.LastSuccessionMessage);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True,
                _session.LastSuccessionMessage);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(successor));

            SaveOperationResult continued = SaveDataValidator.Validate(_session.CaptureSave());
            Assert.That(continued.Succeeded, Is.True, continued.Message);
        }

        [Test]
        public void RejectedChronicleRow_RollsResourcesBackAndLeavesTheOperationRetryable()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            IndustryResources before = _session.Industry.Resources;
            int chronicle = _session.ChronicleEntries.Count;

            _session.TacticalChronicleAppendFaultInjection = ledger =>
                ledger.TryAppendMission(1560, 4, FamilyChronicleEntry.ShadowEscortCommissionId,
                    "test", (int)ShadowEscortStage.NotAccepted,
                    (int)ShadowEscortStage.Accepted, "测试", "推进序号。", out _, out _);

            TacticalActionResult failed = _session.TryTacticalWithdraw();

            Assert.That(failed.Succeeded, Is.False);
            Assert.That(_session.LastTacticalMessage, Does.Contain("编年史"));
            Assert.That(_session.IsTacticalMissionActive, Is.True,
                "a rejected chronicle row must not commit the terminal result.");
            Assert.That(_session.Industry.Resources, Is.EqualTo(before),
                "the cost and the reward are restored as one transaction.");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle),
                "the whole settlement ledger change is rolled back, including the injected row.");

            _session.TacticalChronicleAppendFaultInjection = null;
            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.True);
            Assert.That(_session.IsTacticalMissionActive, Is.False);
        }

        [Test]
        public void SaveAndLoad_AreRefusedWhileActiveAndTerminalResultsPersist()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "SengokuChronicleTests", System.Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            try
            {
                Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
                Assert.That(_session.SaveTo(store).Succeeded, Is.False,
                    "an active operation cannot be saved.");
                Assert.That(_session.LoadFrom(store).Succeeded, Is.False,
                    "an active operation cannot be loaded over.");

                Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.True);
                IndustryResources settled = _session.Industry.Resources;
                int chronicle = _session.ChronicleEntries.Count;

                Assert.That(_session.SaveTo(store).Succeeded, Is.True);

                GameSession loaded = GameSession.NewGame();
                try
                {
                    Assert.That(loaded.LoadFrom(store).Succeeded, Is.True);
                    Assert.That(loaded.Industry.Resources, Is.EqualTo(settled));
                    Assert.That(loaded.ChronicleEntries.Count, Is.EqualTo(chronicle));
                    Assert.That(loaded.IsTacticalMissionCompleted, Is.True);
                    Assert.That(loaded.IsTacticalMissionActive, Is.False);
                    Assert.That(loaded.TryBeginTacticalMission(
                        Eligible(loaded)[0], Eligible(loaded)[1]).Succeeded, Is.False,
                        "a loaded campaign must not replay the one-time operation.");
                }
                finally
                {
                    Object.DestroyImmediate(loaded.gameObject);
                }
            }
            finally
            {
                if (System.IO.Directory.Exists(directory))
                    System.IO.Directory.Delete(directory, true);
            }
        }

        [Test]
        public void OnceCompleted_TheOperationCannotStartAgain()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.True);
            IndustryResources settled = _session.Industry.Resources;
            int chronicle = _session.ChronicleEntries.Count;
            List<PersonId> eligible = Eligible(_session);

            Assert.That(_session.IsTacticalMissionCompleted, Is.True);
            Assert.That(_session.CanBeginTacticalMission, Is.False);
            if (eligible.Count >= 2)
                Assert.That(_session.TryBeginTacticalMission(eligible[0], eligible[1]).Succeeded,
                    Is.False);
            Assert.That(_session.Industry.Resources, Is.EqualTo(settled));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));
        }

        [Test]
        public void Start_RejectsAnM05CommissionAssignee()
        {
            List<PersonId> eligible = Eligible(_session);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            PersonId escortAssignee = eligible[0];
            Assert.That(_session.TryAssignCommission(escortAssignee).Succeeded, Is.True,
                _session.LastCommissionMessage);

            bool listed = false;
            foreach (CommissionAssigneeOption option in _session.TacticalOperativeOptions)
            {
                if (option.PersonId != escortAssignee)
                    continue;
                listed = true;
                Assert.That(option.Eligible, Is.False);
                Assert.That(option.Reason, Does.Contain("护送"));
            }
            Assert.That(listed, Is.True, "the busy escort assignee is listed with its reason.");

            PersonId other = default;
            foreach (CommissionAssigneeOption option in _session.TacticalOperativeOptions)
            {
                if (!option.Eligible)
                    continue;
                other = option.PersonId;
                break;
            }
            Assert.That(other.IsEmpty, Is.False);

            Assert.That(_session.TryBeginTacticalMission(escortAssignee, other).Succeeded,
                Is.False);
            Assert.That(_session.IsTacticalMissionActive, Is.False);
        }

        [Test]
        public void StartingM06_BlocksTheM05AssignmentOfAnOperative()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            PersonId operative = _session.TacticalMissionStatus.Operatives[0].PersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);

            bool listed = false;
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
            {
                if (option.PersonId != operative)
                    continue;
                listed = true;
                Assert.That(option.Eligible, Is.False);
                Assert.That(option.Reason, Does.Contain("潜入"));
            }
            Assert.That(listed, Is.True);

            ShadowEscortActionResult assign = _session.TryAssignCommission(operative);
            Assert.That(assign.Succeeded, Is.False);
            Assert.That(assign.Feedback, Does.Contain("潜入"));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.IsTacticalMissionActive, Is.True);
        }

        [Test]
        public void MidOperation_RejectsIndustryResearchMentorAndDailyDuties()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            PersonId operative = _session.TacticalMissionStatus.Operatives[0].PersonId;

            IndustryAssignmentResult industry = _session.TryAssignIndustry(
                IndustryCatalog.All[0].Id, operative);
            Assert.That(industry.Succeeded, Is.False);
            Assert.That(industry.Message, Does.Contain("潜入"));

            FamilyResearchPlanCommandResult research = _session.TrySetResearchPlan(
                FamilyResearchCatalog.All[0].Id, operative);
            Assert.That(research.Succeeded, Is.False);
            Assert.That(research.Message, Does.Contain("潜入"));

            Assert.That(_session.Education.HasLearner, Is.True);
            // The learner is past the education window here, so the operative refusal
            // proves the mission guard runs before learner eligibility validation.
            Assert.That(_session.Education.IsEligible, Is.False);
            EducationPlanCommandResult education = _session.TrySetEducationPlan(
                _session.Education.LearnerId, EducationDirection.MartialArts, operative);
            Assert.That(education.Succeeded, Is.False);
            Assert.That(education.Message, Does.Contain("潜入"),
                "the active mission must be refused before the learner's age is reported.");

            Assert.That(_session.TryPlanDailyAction(operative, DailyActionKind.Rest, 2,
                default, out string reason), Is.False);
            Assert.That(reason, Does.Contain("潜入"));
            Assert.That(_session.CanPlanDailyAction(operative, out string canReason), Is.False);
            Assert.That(canReason, Does.Contain("潜入"));
            Assert.That(_session.HasActiveDailyAction(operative), Is.False);

            Assert.That(_session.TacticalMissionStatus.Turn, Is.Zero,
                "a refused campaign duty must not advance the operation.");
        }

        [Test]
        public void TryApplySave_IsRefusedDuringTheOperationWithoutClearingIt()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            Move(_session.TacticalMissionStatus.Operatives[0].PersonId,
                TacticalDirection.North);
            TacticalMissionSnapshot before = _session.TacticalMissionStatus;
            SaveData data = _session.CaptureSave();

            SaveOperationResult applied = _session.TryApplySave(data);

            Assert.That(applied.Succeeded, Is.False);
            Assert.That(applied.Message, Does.Contain("潜入"));
            Assert.That(_session.IsTacticalMissionActive, Is.True);
            TacticalMissionSnapshot after = _session.TacticalMissionStatus;
            Assert.That(after.Turn, Is.EqualTo(before.Turn));
            Assert.That(after.Operatives[0].Position,
                Is.EqualTo(before.Operatives[0].Position));
            Assert.That(after.Stage, Is.EqualTo(TacticalMissionStage.InProgress));
        }

        [Test]
        public void Start_RefusesInsufficientSuppliesWithoutMutatingAnything()
        {
            SaveData data = _session.CaptureSave();
            data.industry.supplies = 1;
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);
            List<PersonId> eligible = Eligible(_session);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2));
            IndustryResources before = _session.Industry.Resources;

            Assert.That(_session.CanBeginTacticalMission, Is.False);
            TacticalActionResult start =
                _session.TryBeginTacticalMission(eligible[0], eligible[1]);

            Assert.That(start.Succeeded, Is.False);
            Assert.That(start.Feedback, Does.Contain("物资"));
            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_session.Industry.Resources, Is.EqualTo(before));
        }

        [Test]
        public void Success_ClampsAtTheResourceCeilingAndReportsActualDeltas()
        {
            SaveData data = _session.CaptureSave();
            data.industry.funds = IndustryResources.Maximum;
            data.industry.intelligence = IndustryResources.Maximum;
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            TacticalMissionSnapshot snapshot = _session.TacticalMissionStatus;

            RunAuthoredSuccessRoute(snapshot.Operatives[0].PersonId,
                snapshot.Operatives[1].PersonId);

            Assert.That(_session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Succeeded));
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(IndustryResources.Maximum));
            Assert.That(_session.Industry.Resources.Intelligence,
                Is.EqualTo(IndustryResources.Maximum));
            Assert.That(_session.TacticalAppliedFunds, Is.Zero,
                "a maxed fund pool reports the applied reward, which is zero.");
            Assert.That(_session.TacticalAppliedIntelligence, Is.Zero);
            Assert.That(_session.TacticalAppliedSupplies,
                Is.EqualTo(-TacticalMissionScenario.SupplyCost));
            Assert.That(_session.MissionHistory[0].FrozenIntelligence,
                Is.EqualTo(TacticalMissionResolutionRules.IntelligenceMaximumBonus),
                "the briefing freezes the approved +10 cap, not the raw resource pool.");
            FamilyChronicleEntry row = LastRecord(_session);
            Assert.That(row.Summary, Does.Contain("资金 +0").And.Contain("情报 +0"));
        }

        [Test]
        public void TacticalCommands_DoNotConsumeRandomStreams()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            PersonId operative = _session.TacticalMissionStatus.Operatives[0].PersonId;
            string before = JsonUtility.ToJson(_session.RandomState.CaptureState());

            Move(operative, TacticalDirection.North);
            Move(operative, TacticalDirection.North);
            Move(operative, TacticalDirection.North);
            Move(operative, TacticalDirection.North);
            Assert.That(_session.TryTacticalSprint(operative, TacticalDirection.East).Succeeded,
                Is.True);
            Assert.That(_session.TacticalMissionStatus.Turn, Is.EqualTo(5));

            string after = JsonUtility.ToJson(_session.RandomState.CaptureState());
            Assert.That(after, Is.EqualTo(before),
                "the deterministic operation must not consume any random stream.");
        }

        private void RunAuthoredSuccessRoute(PersonId carrier, PersonId spotter)
        {
            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.North);
            Move(carrier, TacticalDirection.North);
            for (int i = 0; i < 5; i++)
                Move(carrier, TacticalDirection.East);
            Assert.That(_session.TryTacticalInteract(carrier).Succeeded, Is.True,
                _session.LastTacticalMessage);
            Move(carrier, TacticalDirection.West);
            for (int i = 0; i < 4; i++)
                Move(carrier, TacticalDirection.South);
            Move(carrier, TacticalDirection.West);
            Move(carrier, TacticalDirection.West);
            Move(spotter, TacticalDirection.West);
            Move(carrier, TacticalDirection.West);
            TacticalActionResult extract = _session.TryTacticalExtract();
            Assert.That(extract.Succeeded, Is.True, extract.Feedback);
        }

        private TacticalActionResult StartWithTwoFreeMembers()
        {
            List<PersonId> eligible = Eligible(_session);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2),
                "the fixed roster must offer two free members at 清洲.");
            return _session.TryBeginTacticalMission(eligible[0], eligible[1]);
        }

        private void Move(PersonId operative, TacticalDirection direction)
        {
            TacticalActionResult result = _session.TryTacticalMove(operative, direction);
            Assert.That(result.Succeeded, Is.True,
                "move " + TacticalMissionScenario.DirectionName(direction) + " failed: " +
                result.Feedback + " | " + _session.TacticalMissionStatus.LastEvent);
        }

        private static List<PersonId> Eligible(GameSession session)
        {
            var eligible = new List<PersonId>();
            foreach (CommissionAssigneeOption option in session.TacticalOperativeOptions)
                if (option.Eligible)
                    eligible.Add(option.PersonId);
            return eligible;
        }

        /// <summary>
        /// Walks the first operative into the stationary guard's sight, which is the
        /// authored capture route. The exposure command fails because the operation ends.
        /// </summary>
        private void RunIntoTheStationaryGuard(PersonId operative)
        {
            Move(operative, TacticalDirection.North);
            Move(operative, TacticalDirection.North);
            Move(operative, TacticalDirection.North);
            Move(operative, TacticalDirection.North);
            for (int i = 0; i < 5; i++)
                Move(operative, TacticalDirection.East);
            TacticalActionResult exposure =
                _session.TryTacticalMove(operative, TacticalDirection.East);
            Assert.That(exposure.Succeeded, Is.False,
                "stepping in front of the stationary guard must end the operation: " +
                _session.TacticalMissionStatus.LastEvent);
        }

        private static MissionParticipantState ParticipantStateOf(MissionRecord record,
            PersonId personId)
        {
            for (int i = 0; i < record.ParticipantStates.Count; i++)
                if (record.ParticipantStates[i].PersonId == personId)
                    return record.ParticipantStates[i];
            Assert.Fail("the terminal record must carry a state for " + personId.Value);
            return default;
        }

        /// <summary>
        /// Replaces only the Missions stream with a fresh stream whose next
        /// <paramref name="draws"/> values all satisfy the predicate. The terminal
        /// settlement draws one death check per disclosed participant, so seeding every
        /// draw keeps a multi-participant outcome deterministic.
        /// </summary>
        private static SaveData WithSeededMissionDraws(SaveData data, int draws,
            Func<int, bool> accept) =>
            WithSeededMissionDraws(data, draws, (index, draw) => accept(draw));

        /// <summary>
        /// Seeds the disclosed death checks so exactly the participant at
        /// <paramref name="fatalIndex"/> takes the fatal roll and every other disclosed
        /// participant survives. The settlement resolves one check per participant in
        /// briefing order, so the index is a stable production input.
        /// </summary>
        private static SaveData WithSeededMissionDeathChecks(SaveData data, int fatalIndex,
            int participants) =>
            WithSeededMissionDraws(data, participants, (index, draw) =>
                (index == fatalIndex) ==
                (draw < TacticalMissionResolutionRules.DeathCheckPercent));

        /// <summary>
        /// Replaces only the Missions stream with a fresh stream whose next
        /// <paramref name="draws"/> values satisfy the index-aware predicate.
        /// </summary>
        private static SaveData WithSeededMissionDraws(SaveData data, int draws,
            Func<int, int, bool> accept)
        {
            int missionsIndex = -1;
            for (int i = 0; i < data.randomState.streams.Length; i++)
                if (data.randomState.streams[i].streamId ==
                    RandomStateManager.StableId(RandomStreamKind.Missions))
                    missionsIndex = i;
            Assert.That(missionsIndex, Is.GreaterThanOrEqualTo(0),
                "the saved random state must contain the Missions stream.");

            for (ulong seed = 1; seed < 100000UL; seed++)
            {
                RandomStateManager probe = RandomStateManager.Create(seed);
                RandomStateSaveData candidate = probe.CaptureState();
                DeterministicRandomStream stream = probe.GetStream(RandomStreamKind.Missions);
                bool accepted = true;
                for (int i = 0; i < draws && accepted; i++)
                    accepted = stream.TryNextInt(
                        TacticalMissionResolutionRules.DieRollExclusiveMax, out int draw) &&
                        accept(i, draw);
                if (!accepted)
                    continue;
                data.randomState.streams[missionsIndex] = candidate.streams[missionsIndex];
                return data;
            }

            Assert.Fail("no Missions seed produces " + draws + " accepted death check(s).");
            return data;
        }

        private static FamilyChronicleEntry LastRecord(GameSession session)
        {
            IReadOnlyList<FamilyChronicleEntry> entries = session.ChronicleEntries;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].Category == ChronicleCategory.Mission &&
                    entries[i].SubjectId == FamilyChronicleEntry.TacticalMissionId)
                    return entries[i];
            return null;
        }
    }
}
