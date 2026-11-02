using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    public class GameSessionSuccessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void NewGame_ExposesStandardHeadAndLawfulCandidates()
        {
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 晴信"));
            Assert.That(_session.SuccessionCandidates.Count, Is.EqualTo(3));
            Assert.That(_session.SuccessionCandidates[0].PersonId.Value,
                Is.EqualTo("person.akechi.masatsuna"));
        }

        [Test]
        public void RetirementPending_DoesNotChangeCampaignStateOrRelationships()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 7).Succeeded, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            int merit = _session.Merit;

            SuccessionResult begun = _session.BeginSuccessionRetirement();

            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.Merit, Is.EqualTo(merit));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(7));
            SaveData snapshot = _session.CaptureSave();
            Assert.That(SaveDataValidator.Validate(snapshot).Succeeded, Is.True);
            Assert.That(snapshot.succession.phase,
                Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));
        }

        [Test]
        public void AdultSuccession_TransfersHeadAndPreservesCampaignState()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.HoshinoAya, 4).Succeeded, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;

            _session.BeginSuccessionRetirement();
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            SuccessionResult confirmed = _session.ConfirmSuccession();

            Assert.That(confirmed.Succeeded, Is.True, confirmed.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.TryGetRelationship(CharacterId.HoshinoAya).CurrentValue, Is.EqualTo(4));
        }

        [Test]
        public void MinorSuccession_PausesAtAdulthoodAndRequiresExplicitHandoff()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.TachibanaSuzu, 3).Succeeded, Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;

            _session.BeginSuccessionRetirement();
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.tokihime")).Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionRegent(
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));

            Assert.That(_session.AdvanceDays(800), Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency),
                "Adulthood must pause the regency instead of transferring silently.");
            Assert.That(_session.Succession.IsRegencyHandoffDue(_session.WorldClock.CurrentDate), Is.True);
            GameDate pausedDate = _session.WorldClock.CurrentDate;

            Assert.That(_session.AdvanceDays(30), Is.False,
                "Time advancement must be blocked while the adult handoff awaits the player.");
            Assert.That(_session.LastEventMessage, Does.Contain("完成成年交权"),
                "A blocked multi-day advance must report the handoff it waits for.");
            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.PendingSuccession));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(pausedDate));

            SuccessionResult handoff = _session.TryCompleteSuccessionRegency();

            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.Succession.RegencyCompleted, Is.True);
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 時姫"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(pausedDate),
                "The handoff itself must not advance the clock.");
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.TryGetRelationship(CharacterId.TachibanaSuzu).CurrentValue, Is.EqualTo(3));
            Assert.That(_session.WorldClock.CurrentDate, Is.GreaterThan(date));
            Assert.That(_session.AdvanceDays(1), Is.True,
                "Time advancement resumes only after the player completes the handoff.");
        }

        [Test]
        public void MonthlyAdvance_ReportsPendingSuccessionSeparatelyFromPendingEvent()
        {
            _session.BeginSuccessionRetirement();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.PendingSuccession));
        }

        [Test]
        public void AdvanceOneDay_WhileSuccessionChoicePending_ReportsPendingSuccessionWithoutAdvancing()
        {
            GameDate date = _session.WorldClock.CurrentDate;
            _session.BeginSuccessionRetirement();

            Assert.That(_session.AdvanceOneDay(), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date),
                "A blocked day advance must not move the campaign date.");
            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.PendingSuccession),
                "The pause must be reported through the existing outcome enum.");
            Assert.That(_session.LastEventMessage, Does.Contain("继承"),
                "The pause must state that a succession decision is what blocks time.");
            Assert.That(_session.LastEventMessage, Does.Contain("推进时间"),
                "The pause must state what the player has to do before time can move.");
        }

        [Test]
        public void AdvanceOneDay_WhileAdultHandoffAwaitsConfirmation_ReportsHandoffPauseWithoutAdvancing()
        {
            _session.BeginSuccessionRetirement();
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.tokihime")).Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionRegent(
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(800), Is.True);
            Assert.That(_session.Succession.IsRegencyHandoffDue(
                _session.WorldClock.CurrentDate), Is.True);
            GameDate pausedDate = _session.WorldClock.CurrentDate;

            Assert.That(_session.AdvanceOneDay(), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(pausedDate));
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency),
                "Only the explicit handoff may end the regency.");
            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.PendingSuccession));
            Assert.That(_session.LastEventMessage, Does.Contain("完成成年交权"),
                "The day-advance pause must name the explicit handoff action, not claim it happens automatically.");
        }

        [Test]
        public void ValidDesignatedHeir_TransfersImmediatelyOnRetirement()
        {
            var heir = new PersonId("person.akechi.masatsuna");
            _session.Succession.DesignatedHeirId = heir;

            SuccessionResult begun = _session.BeginSuccessionRetirement();

            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            Assert.That(_session.Succession.DesignatedHeirId.IsEmpty, Is.True);
        }

        [Test]
        public void CrisisDemo_AcceptAdoptionCreatesAdultHeir()
        {
            Assert.That(_session.StartCrisisDemo(), Is.True);

            SuccessionResult begun = _session.BeginSuccessionRetirement();
            Assert.That(begun.Outcome, Is.EqualTo(SuccessionOutcome.NoCandidates));
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));

            SuccessionResult accepted = _session.AcceptCrisisAdoption();

            Assert.That(accepted.Succeeded, Is.True, accepted.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("養子 宗次"));
            Assert.That(_session.Succession.CrisisAdoptionUsed, Is.True);
        }

        [Test]
        public void CrisisDemo_DeclineEndsFamily()
        {
            Assert.That(_session.StartCrisisDemo(), Is.True);
            _session.BeginSuccessionRetirement();

            SuccessionResult declined = _session.DeclineCrisis();

            Assert.That(declined.Succeeded, Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.False);
        }

        [Test]
        public void StartCrisisDemo_RefusedWhileDecisionIsPending()
        {
            _session.BeginSuccessionRetirement();

            Assert.That(_session.StartCrisisDemo(), Is.False);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
        }

        [Test]
        public void SuccessfulSuccessionCommandsRaiseSessionStateChanged()
        {
            int changes = 0;
            _session.SessionStateChanged += () => changes++;

            _session.BeginSuccessionRetirement();
            _session.SelectSuccessionSuccessor(new PersonId("person.akechi.masatsuna"));
            _session.ConfirmSuccession();

            Assert.That(changes, Is.EqualTo(3));
        }

        [Test]
        public void ExtinctFamily_RejectsEveryTimeAndTravelMutationAndPreservesState()
        {
            Assert.That(_session.PreviewTravel(LocationId.Atsuta).HasValue, Is.True);
            TravelPreview atsutaPreview = _session.PreviewTravel(LocationId.Atsuta).Value;
            Assert.That(_session.StartCrisisDemo(), Is.True);
            _session.BeginSuccessionRetirement();
            Assert.That(_session.DeclineCrisis().Succeeded, Is.True);

            Assert.That(_session.IsFamilyExtinct, Is.True);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;

            Assert.That(_session.AdvanceOneDay(), Is.False,
                "A day advance must not move an extinct family.");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.PendingSuccession));
            Assert.That(_session.LastEventMessage, Does.Contain("灭亡"),
                "The refusal must say the family is extinct, not that a decision is merely pending.");

            Assert.That(_session.AdvanceDays(3), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.PendingSuccession));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));

            Assert.That(_session.PreviewTravel(LocationId.Nagoya), Is.Null,
                "Travel preview must be unavailable once the family is extinct.");
            Assert.That(_session.ConfirmTravel(atsutaPreview), Is.False,
                "A stale preview must not move an extinct family.");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.False);
            Assert.That(_session.StartCrisisDemo(), Is.False);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
        }

        [Test]
        public void ExtinctFamily_CanStillLoadAnEarlierCheckpoint()
        {
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            Assert.That(_session.StartCrisisDemo(), Is.True);
            _session.BeginSuccessionRetirement();
            SaveData checkpoint = _session.CaptureSave();
            Assert.That(checkpoint.succession.phase,
                Is.EqualTo((int)SuccessionPhase.CrisisDecision));

            Assert.That(_session.DeclineCrisis().Succeeded, Is.True);
            Assert.That(_session.IsFamilyExtinct, Is.True);

            SaveOperationResult loaded = _session.TryApplySave(checkpoint);

            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(_session.IsFamilyExtinct, Is.False);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision),
                "Loading a pre-extinction checkpoint must reopen the decision instead of ending the run.");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
        }
    }
}
