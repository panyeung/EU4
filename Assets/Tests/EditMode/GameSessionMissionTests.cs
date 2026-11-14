using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public class GameSessionMissionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [TestCase(SupplyShortageStage.NotAccepted, "investigate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.NotAccepted, "negotiate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.NotAccepted, "report", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Accepted, "accept", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Accepted, "negotiate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.Accepted, "report", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Investigated, "accept", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Investigated, "investigate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.Investigated, "report", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Negotiated, "accept", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Negotiated, "investigate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.Negotiated, "negotiate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.RewardClaimed, "accept", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.RewardClaimed, "investigate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.RewardClaimed, "negotiate", "owari.atsuta")]
        [TestCase(SupplyShortageStage.RewardClaimed, "report", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.NotAccepted, "accept", "owari.atsuta")]
        [TestCase(SupplyShortageStage.NotAccepted, "accept", "owari.nagoya")]
        [TestCase(SupplyShortageStage.Accepted, "investigate", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Accepted, "investigate", "owari.nagoya")]
        [TestCase(SupplyShortageStage.Investigated, "negotiate", "owari.kiyosu")]
        [TestCase(SupplyShortageStage.Investigated, "negotiate", "owari.nagoya")]
        [TestCase(SupplyShortageStage.Negotiated, "report", "owari.atsuta")]
        [TestCase(SupplyShortageStage.Negotiated, "report", "owari.nagoya")]
        [TestCase(SupplyShortageStage.Investigated, "negotiate", "owari.atsuta", "npc.sakai.jirobei")]
        [TestCase(SupplyShortageStage.Investigated, "negotiate", "owari.atsuta", "npc.unknown")]
        [TestCase(SupplyShortageStage.Investigated, "negotiate", "owari.atsuta", "player.samurai")]
        [TestCase(SupplyShortageStage.Investigated, "negotiate", "owari.atsuta", null)]
        public void RejectedAction_PreservesMissionAndWorldState(SupplyShortageStage stage, string action, string location, string merchant = "npc.kuroda.iori")
        {
            ReachStage(stage);
            TravelTo(new LocationId(location));
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId currentLocation = _session.CurrentLocation;
            int merit = stage == SupplyShortageStage.RewardClaimed ? 10 : 0;

            SupplyShortageActionResult result = Act(action, new CharacterId(merchant));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Feedback, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Stage, Is.EqualTo(stage));
            Assert.That(result.Merit, Is.EqualTo(merit));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(stage));
            Assert.That(_session.Merit, Is.EqualTo(merit));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(currentLocation));
        }

        [TestCase(SupplyShortageStage.Accepted)]
        [TestCase(SupplyShortageStage.Investigated)]
        [TestCase(SupplyShortageStage.Negotiated)]
        [TestCase(SupplyShortageStage.RewardClaimed)]
        public void InitNewGame_ResetsMissionAndMerit(SupplyShortageStage stage)
        {
            ReachStage(stage);
            _session.InitNewGame();
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.NotAccepted));
            Assert.That(_session.Merit, Is.Zero);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            AssertAction("accept", SupplyShortageStage.Accepted, 0);
        }

        [Test]
        public void MissionActions_DoNotAdvanceClockOrChangeLocation()
        {
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.NotAccepted));
            Assert.That(_session.Merit, Is.Zero);
            AssertAction("accept", SupplyShortageStage.Accepted, 0);
            TravelTo(LocationId.Atsuta);
            AssertAction("investigate", SupplyShortageStage.Investigated, 0);
            AssertAction("negotiate", SupplyShortageStage.Negotiated, 0);
            TravelTo(LocationId.Kiyosu);
            AssertAction("report", SupplyShortageStage.RewardClaimed, 10);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void M4bMissions_CompleteIndependentlyInEitherOrderAndAggregateWithM4a(bool tallyFirst)
        {
            if (tallyFirst) CompleteMarketTally(); else CompleteSealedLetter();
            Assert.That(_session.Merit, Is.EqualTo(5));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.NotAccepted));
            Assert.That(_session.SealedLetterStage, Is.EqualTo(tallyFirst ? SealedLetterStage.NotAccepted : SealedLetterStage.RewardClaimed));
            Assert.That(_session.MarketTallyStage, Is.EqualTo(tallyFirst ? MarketTallyStage.RewardClaimed : MarketTallyStage.NotAccepted));
            if (tallyFirst) CompleteSealedLetter(); else CompleteMarketTally();
            Assert.That(_session.Merit, Is.EqualTo(10));
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.True);
            Assert.That(_session.TryNegotiateSupplyShortage(CharacterId.KurodaIori).Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryReportSupplyShortage().Succeeded, Is.True);
            Assert.That(_session.Merit, Is.EqualTo(20));
            TravelTo(LocationId.Nagoya);
            AssertM4bAction("deliver", false);
            AssertM4bAction("tallyReport", false);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryReportSupplyShortage().Succeeded, Is.False);
            Assert.That(_session.Merit, Is.EqualTo(20));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void InitNewGame_ResetsBothM4bMissionsAndRewards(int progress)
        {
            AssertM4bAction("letterAccept", true);
            TravelTo(LocationId.Nagoya);
            AssertM4bAction("tallyAccept", true);
            if (progress >= 2)
            {
                AssertM4bAction("deliver", true);
                TravelTo(LocationId.Kiyosu);
                AssertM4bAction("tallyInvestigate", true);
            }
            if (progress == 3)
            {
                TravelTo(LocationId.Nagoya);
                AssertM4bAction("tallyReport", true);
            }
            _session.InitNewGame();
            Assert.That(_session.SealedLetterStage, Is.EqualTo(SealedLetterStage.NotAccepted));
            Assert.That(_session.MarketTallyStage, Is.EqualTo(MarketTallyStage.NotAccepted));
            Assert.That(_session.Merit, Is.Zero);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            CompleteSealedLetter();
            CompleteMarketTally();
            Assert.That(_session.Merit, Is.EqualTo(10));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SealedLetter_RejectedSessionActionsPreserveAllState(int progress)
        {
            if (progress > 0) { TravelTo(LocationId.Kiyosu); AssertM4bAction("letterAccept", true); }
            if (progress > 1) { TravelTo(LocationId.Nagoya); AssertM4bAction("deliver", true); }
            foreach (LocationId location in new[] { LocationId.Kiyosu, LocationId.Atsuta, LocationId.Nagoya })
            foreach (string action in new[] { "letterAccept", "deliver" })
            {
                bool valid = action == "letterAccept" ? progress == 0 && location == LocationId.Kiyosu
                    : progress == 1 && location == LocationId.Nagoya;
                if (valid) continue;
                TravelTo(location);
                AssertM4bAction(action, false);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void MarketTally_RejectedSessionActionsPreserveAllState(int progress)
        {
            if (progress > 0) { TravelTo(LocationId.Nagoya); AssertM4bAction("tallyAccept", true); }
            if (progress > 1) { TravelTo(LocationId.Kiyosu); AssertM4bAction("tallyInvestigate", true); }
            if (progress > 2) { TravelTo(LocationId.Nagoya); AssertM4bAction("tallyReport", true); }
            foreach (LocationId location in new[] { LocationId.Kiyosu, LocationId.Atsuta, LocationId.Nagoya })
            foreach (string action in new[] { "tallyAccept", "tallyInvestigate", "tallyReport" })
            {
                bool valid = action == "tallyAccept" ? progress == 0 && location == LocationId.Nagoya
                    : action == "tallyInvestigate" ? progress == 1 && location == LocationId.Kiyosu
                    : progress == 2 && location == LocationId.Nagoya;
                if (valid) continue;
                TravelTo(location);
                AssertM4bAction(action, false);
            }
        }

        [Test]
        public void ExistingMissionChronicles_KeepTheirOriginalSummaries()
        {
            CompleteSealedLetter();
            CompleteMarketTally();
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.True);
            Assert.That(_session.TryNegotiateSupplyShortage(CharacterId.KurodaIori).Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryReportSupplyShortage().Succeeded, Is.True);

            FamilyChronicleEntry sealedLetter = FindMissionEntry(
                FamilyChronicleEntry.SealedLetterMissionId, "deliver");
            FamilyChronicleEntry marketTally = FindMissionEntry(
                FamilyChronicleEntry.MarketTallyMissionId, "report");
            FamilyChronicleEntry supplyShortage = FindMissionEntry(
                FamilyChronicleEntry.SupplyShortageMissionId, "report");

            Assert.That(sealedLetter, Is.Not.Null);
            Assert.That(marketTally, Is.Not.Null);
            Assert.That(supplyShortage, Is.Not.Null);
            Assert.That(sealedLetter.Summary, Is.EqualTo("密信已送达，获得5功绩。"));
            Assert.That(marketTally.Summary, Is.EqualTo("市集账目报告完成，获得5功绩。"));
            Assert.That(supplyShortage.Summary, Is.EqualTo("报告完成，获得10功绩。"));
            foreach (FamilyChronicleEntry entry in _session.ChronicleEntries)
                Assert.That(entry.Summary, Does.Not.Match(@"（\d{4}-\d{2}-\d{2}）"),
                    "only M05 commission records may carry the exact-day suffix.");
        }

        private FamilyChronicleEntry FindMissionEntry(string missionId, string actionId)
        {
            foreach (FamilyChronicleEntry entry in _session.ChronicleEntries)
                if (entry.SubjectId == missionId && entry.SourceId == actionId)
                    return entry;
            return null;
        }

        private void CompleteSealedLetter()
        {
            TravelTo(LocationId.Kiyosu);
            AssertM4bAction("letterAccept", true);
            Assert.That(_session.SealedLetterStage, Is.EqualTo(SealedLetterStage.Accepted));
            TravelTo(LocationId.Nagoya);
            AssertM4bAction("deliver", true);
            Assert.That(_session.SealedLetterStage, Is.EqualTo(SealedLetterStage.RewardClaimed));
        }

        private void CompleteMarketTally()
        {
            TravelTo(LocationId.Nagoya);
            AssertM4bAction("tallyAccept", true);
            Assert.That(_session.MarketTallyStage, Is.EqualTo(MarketTallyStage.Accepted));
            TravelTo(LocationId.Kiyosu);
            AssertM4bAction("tallyInvestigate", true);
            Assert.That(_session.MarketTallyStage, Is.EqualTo(MarketTallyStage.Investigated));
            TravelTo(LocationId.Nagoya);
            AssertM4bAction("tallyReport", true);
            Assert.That(_session.MarketTallyStage, Is.EqualTo(MarketTallyStage.RewardClaimed));
        }

        private void AssertM4bAction(string action, bool expectedSuccess)
        {
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            SealedLetterStage letter = _session.SealedLetterStage;
            MarketTallyStage tally = _session.MarketTallyStage;
            SupplyShortageStage supply = _session.SupplyShortageStage;
            int merit = _session.Merit;
            bool succeeded;
            string feedback;
            if (action == "letterAccept" || action == "deliver")
            {
                SealedLetterActionResult result = action == "letterAccept" ? _session.TryAcceptSealedLetterMission() : _session.TryDeliverSealedLetter();
                succeeded = result.Succeeded;
                feedback = result.Feedback;
                Assert.That(result.Stage, Is.EqualTo(_session.SealedLetterStage));
                Assert.That(result.Merit, Is.EqualTo(_session.SealedLetterStage == SealedLetterStage.RewardClaimed ? 5 : 0));
                Assert.That(_session.MarketTallyStage, Is.EqualTo(tally));
            }
            else
            {
                MarketTallyActionResult result = action == "tallyAccept" ? _session.TryAcceptMarketTallyMission()
                    : action == "tallyInvestigate" ? _session.TryInvestigateMarketTally() : _session.TryReportMarketTally();
                succeeded = result.Succeeded;
                feedback = result.Feedback;
                Assert.That(result.Stage, Is.EqualTo(_session.MarketTallyStage));
                Assert.That(result.Merit, Is.EqualTo(_session.MarketTallyStage == MarketTallyStage.RewardClaimed ? 5 : 0));
                Assert.That(_session.SealedLetterStage, Is.EqualTo(letter));
            }
            Assert.That(succeeded, Is.EqualTo(expectedSuccess), feedback);
            Assert.That(feedback, Is.Not.Null.And.Not.Empty);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(supply));
            if (!expectedSuccess)
            {
                Assert.That(_session.SealedLetterStage, Is.EqualTo(letter));
                Assert.That(_session.MarketTallyStage, Is.EqualTo(tally));
                Assert.That(_session.Merit, Is.EqualTo(merit));
            }
        }

        private void ReachStage(SupplyShortageStage stage)
        {
            if (stage == SupplyShortageStage.NotAccepted) return;
            AssertAction("accept", SupplyShortageStage.Accepted, 0);
            if (stage == SupplyShortageStage.Accepted) return;
            TravelTo(LocationId.Atsuta);
            AssertAction("investigate", SupplyShortageStage.Investigated, 0);
            if (stage == SupplyShortageStage.Investigated) return;
            AssertAction("negotiate", SupplyShortageStage.Negotiated, 0);
            if (stage == SupplyShortageStage.Negotiated) return;
            TravelTo(LocationId.Kiyosu);
            AssertAction("report", SupplyShortageStage.RewardClaimed, 10);
        }

        private void AssertAction(string action, SupplyShortageStage stage, int merit)
        {
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            SupplyShortageActionResult result = Act(action, CharacterId.KurodaIori);
            Assert.That(result.Succeeded, Is.True, result.Feedback);
            Assert.That(result.Feedback, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Stage, Is.EqualTo(stage));
            Assert.That(result.Merit, Is.EqualTo(merit));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(stage));
            Assert.That(_session.Merit, Is.EqualTo(merit));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
        }

        private SupplyShortageActionResult Act(string action, CharacterId merchant)
        {
            switch (action)
            {
                case "accept": return _session.TryAcceptSupplyShortageMission();
                case "investigate": return _session.TryInvestigateSupplyShortage();
                case "negotiate": return _session.TryNegotiateSupplyShortage(merchant);
                case "report": return _session.TryReportSupplyShortage();
                default: throw new System.ArgumentOutOfRangeException(nameof(action));
            }
        }

        private void TravelTo(LocationId destination)
        {
            if (_session.CurrentLocation == destination) return;
            if (_session.CurrentLocation != LocationId.Kiyosu && destination != LocationId.Kiyosu)
                TravelTo(LocationId.Kiyosu);
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }
    }

    /// <summary>
    /// M05 vertical slice: one shadow-clan escort commission from offer through
    /// assignment and timed execution to a settled outcome. Asserts only behavior
    /// that a player can observe in the session: the stage, the assignee, the funds
    /// delta for the settled band, and the chronicle record.
    /// </summary>
    public class M05CommissionSessionTests
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
        public void AcceptAssignAdvance_SettlesOnceAndKeepsAssigneeAndOutcome()
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            int fundsBefore = _session.Industry.Resources.Funds;

            ShadowEscortActionResult accept = _session.TryAcceptCommission();
            Assert.That(accept.Succeeded, Is.True, accept.Feedback);
            Assert.That(accept.Stage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(fundsBefore - ShadowEscortCommission.InvestmentFundsFor(
                    _session.CurrentFamilyCreed)));

            ShadowEscortActionResult assign = _session.TryAssignCommission(assignee);
            Assert.That(assign.Succeeded, Is.True, assign.Feedback);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.CommissionAssignee, Is.EqualTo(assignee));
            Assert.That(_session.CommissionRemainingDays,
                Is.EqualTo(_session.CommissionDurationDays));

            Assert.That(_session.AdvanceDays(_session.CommissionDurationDays), Is.True);
            Assert.That(_session.CommissionStage == ShadowEscortStage.Succeeded ||
                        _session.CommissionStage == ShadowEscortStage.PartialSuccess ||
                        _session.CommissionStage == ShadowEscortStage.Failed, Is.True);
            Assert.That(_session.CommissionAssignee, Is.EqualTo(assignee));
            Assert.That(_session.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None));
            Assert.That(_session.CommissionRemainingDays, Is.Zero);

            IndustryResources resources = _session.Industry.Resources;
            switch (_session.CommissionOutcome)
            {
                case CommissionOutcome.Success:
                    Assert.That(resources.Funds, Is.GreaterThan(fundsBefore - 4));
                    break;
                case CommissionOutcome.Partial:
                    Assert.That(resources.Funds, Is.EqualTo(fundsBefore + 3));
                    break;
                default:
                    Assert.That(resources.Funds, Is.EqualTo(fundsBefore - 4));
                    break;
            }

            int entriesAfterSettlement = _session.ChronicleEntries.Count;
            Assert.That(entriesAfterSettlement, Is.GreaterThan(0));
            Assert.That(_session.AdvanceDays(1), Is.True);
            Assert.That(_session.CommissionStage == ShadowEscortStage.Succeeded ||
                        _session.CommissionStage == ShadowEscortStage.PartialSuccess ||
                        _session.CommissionStage == ShadowEscortStage.Failed, Is.True);
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(entriesAfterSettlement),
                "A settled commission must never resolve twice or append a second record.");
        }

        [Test]
        public void AssigneeWithActiveDailyAction_IsRejectedWithoutStateChange()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            PersonId assignee = _session.Succession.HeadPersonId;
            Assert.That(_session.TryPlanDailyAction(assignee, DailyActionKind.Training, 5,
                default, out string planReason), Is.True, planReason);
            int funds = _session.Industry.Resources.Funds;

            ShadowEscortActionResult assign = _session.TryAssignCommission(assignee);

            Assert.That(assign.Succeeded, Is.False);
            Assert.That(assign.Feedback, Is.Not.Null.And.Not.Empty);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.CommissionAssignee.IsEmpty, Is.True);
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(funds));
        }

        [Test]
        public void CommissionInProgress_BlocksNewDailyActionsForTheAssigneeOnly()
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);

            Assert.That(_session.CanPlanDailyAction(assignee, out string reason), Is.False);
            Assert.That(reason, Does.Contain("委托"));

            PersonId other = FindOtherLivingMember(assignee);
            if (!other.IsEmpty)
                Assert.That(_session.CanPlanDailyAction(other, out _), Is.True);
        }

        [Test]
        public void Commission_InProgressAndSettledOutcome_SurviveSaveAndLoad()
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(1), Is.True);

            SaveData inProgress = CloneSave(_session.CaptureSave());
            Assert.That(inProgress.commission, Is.Not.Null,
                "capture must serialize the commission block.");
            Assert.That(inProgress.commission.stage,
                Is.EqualTo((int)ShadowEscortStage.InProgress));
            Assert.That(inProgress.commission.assigneePersonId, Is.EqualTo(assignee.Value));
            Assert.That(inProgress.commission.elapsedDays, Is.EqualTo(1));
            Assert.That(SaveDataMigrator.TryMigrate(inProgress, out SaveData _, out _), Is.True);
            int elapsed = _session.CommissionElapsedDays;

            GameSession loaded = GameSession.NewGame();
            try
            {
                Assert.That(loaded.TryApplySave(CloneSave(inProgress)).Succeeded, Is.True);
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
                Assert.That(loaded.CommissionAssignee, Is.EqualTo(assignee));
                Assert.That(loaded.CommissionElapsedDays, Is.EqualTo(elapsed));

                int remaining = loaded.CommissionRemainingDays;
                Assert.That(loaded.AdvanceDays(remaining), Is.True);
                Assert.That(loaded.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None));

                SaveData settled = CloneSave(loaded.CaptureSave());
                GameSession reloaded = GameSession.NewGame();
                try
                {
                    Assert.That(reloaded.TryApplySave(CloneSave(settled)).Succeeded, Is.True);
                    Assert.That(reloaded.CommissionStage, Is.EqualTo(loaded.CommissionStage));
                    Assert.That(reloaded.CommissionOutcome, Is.EqualTo(loaded.CommissionOutcome));
                    Assert.That(reloaded.CommissionAssignee, Is.EqualTo(assignee));
                }
                finally
                {
                    Object.DestroyImmediate(reloaded.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void AcceptanceCost_FollowsCurrentCreedAndCreedChangesCost()
        {
            int before = _session.Industry.Resources.Funds;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(before - ShadowEscortCommission.InvestmentFundsFor(
                    _session.CurrentFamilyCreed)));
            Assert.That(ShadowEscortCommission.InvestmentFundsFor(FamilyCreedKind.SecrecyFirst),
                Is.LessThan(ShadowEscortCommission.InvestmentFundsFor(
                    FamilyCreedKind.RighteousnessFirst)),
                "An approved family creed must produce a visible business consequence.");
        }

        [Test]
        public void TravelDays_AdvanceCommissionOncePerDayAndSettleOnTheDueDay()
        {
            PersonId assignee = FindOtherLivingMember(_session.Succession.HeadPersonId);
            Assert.That(assignee.IsEmpty, Is.False, "The fixed roster must offer a non-head member.");
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            Assert.That(_session.CommissionElapsedDays, Is.Zero);

            Assert.That(TravelTo(LocationId.Nagoya), Is.True);
            Assert.That(_session.CommissionElapsedDays, Is.EqualTo(1));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));

            Assert.That(TravelTo(LocationId.Kiyosu), Is.True);
            Assert.That(_session.CommissionElapsedDays, Is.EqualTo(2));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));

            Assert.That(TravelTo(LocationId.Atsuta), Is.True,
                _session.LastTravelBlockReason);
            Assert.That(_session.CommissionElapsedDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));
            Assert.That(_session.CommissionStage, Is.Not.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None));
            Assert.That(_session.CommissionRemainingDays, Is.Zero);
        }

        [Test]
        public void HeadAssignee_TravelIsRejectedWithoutAnyMutation()
        {
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);

            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            IndustryResources resources = _session.Industry.Resources;
            int elapsed = _session.CommissionElapsedDays;
            string rng = JsonUtility.ToJson(_session.CaptureSave().randomState);

            Assert.That(_session.PreviewTravel(LocationId.Nagoya), Is.Null);
            Assert.That(_session.LastTravelBlockReason, Does.Contain("委托"));

            Assert.That(GameDate.TryAddDays(date, 1, out GameDate arrival), Is.True);
            var forged = new TravelPreview(LocationId.Nagoya,
                LocationId.GetChineseName(LocationId.Nagoya), 1, arrival);
            Assert.That(_session.ConfirmTravel(forged), Is.False);
            Assert.That(_session.LastTravelBlockReason, Does.Contain("委托"));

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.CommissionElapsedDays, Is.EqualTo(elapsed));
            Assert.That(JsonUtility.ToJson(_session.CaptureSave().randomState), Is.EqualTo(rng));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
        }

        [Test]
        public void IndustryAssignment_RejectsTheActiveCommissionAssignee()
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);

            IndustryAssignmentResult result =
                _session.TryAssignIndustry(IndustryCatalog.FarmId, assignee);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("委托"));
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);
        }

        [TestCase(ShadowEscortStage.Failed, CommissionOutcome.Failure, 0, 0)]
        [TestCase(ShadowEscortStage.PartialSuccess, CommissionOutcome.Partial,
            ShadowEscortCommission.RewardPartialFunds, ShadowEscortCommission.RewardPartialSupplies)]
        public void SettledFailureOrPartialRow_LoadsThroughApplySave(ShadowEscortStage stage,
            CommissionOutcome outcome, int rewardFunds, int rewardSupplies)
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            SaveData data = SettledCommissionSave(assignee, stage, outcome, rewardFunds,
                rewardSupplies);

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
            GameSession loaded = GameSession.NewGame();
            try
            {
                Assert.That(loaded.TryApplySave(CloneSave(data)).Succeeded, Is.True);
                Assert.That(loaded.CommissionStage, Is.EqualTo(stage));
                Assert.That(loaded.CommissionOutcome, Is.EqualTo(outcome));
                Assert.That(loaded.CommissionAssignee, Is.EqualTo(assignee));
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void CrisisScenarioSwap_IsRejectedWhileACommissionIsOutstanding()
        {
            _session.InitStandardDemo();
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            PersonId head = _session.Succession.HeadPersonId;
            IndustryResources resources = _session.Industry.Resources;

            Assert.That(_session.StartCrisisDemo(), Is.False);
            Assert.That(_session.LastSuccessionMessage, Does.Contain("委托"),
                "the refusal must explain the outstanding commission.");
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head),
                "the standard roster must not be replaced.");
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources),
                "the accepted investment must not be silently lost.");
        }

        [TestCase(ShadowEscortStage.Failed, CommissionOutcome.Failure, 0, 0)]
        [TestCase(ShadowEscortStage.PartialSuccess, CommissionOutcome.Partial,
            ShadowEscortCommission.RewardPartialFunds, ShadowEscortCommission.RewardPartialSupplies)]
        public void SettledCommissionRow_StaysLoadableAfterAssigneeDies(ShadowEscortStage stage,
            CommissionOutcome outcome, int rewardFunds, int rewardSupplies)
        {
            PersonId assignee = M2FixedScenario.TokihimeId;
            SaveData data = SettledCommissionSave(assignee, stage, outcome, rewardFunds,
                rewardSupplies);
            Person(data, assignee).alive = false;

            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True,
                "a settled historical result must not depend on the assignee still being alive.");
            GameSession loaded = GameSession.NewGame();
            try
            {
                Assert.That(loaded.TryApplySave(CloneSave(data)).Succeeded, Is.True);
                Assert.That(loaded.CommissionStage, Is.EqualTo(stage));
                Assert.That(loaded.CommissionAssignee, Is.EqualTo(assignee));
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        /// <summary>
        /// Builds a settled commission fixture through the production path, so the save
        /// carries the one terminal history row and chronicle entry the current schema
        /// requires, then rewrites the settled row to the parameterized case. The linked
        /// history row is rewritten with it, so the fixture never claims a settled outcome
        /// that contradicts its own record.
        /// </summary>
        private SaveData SettledCommissionSave(PersonId assignee, ShadowEscortStage stage,
            CommissionOutcome outcome, int rewardFunds, int rewardSupplies)
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(_session.CommissionDurationDays), Is.True);

            SaveData data = CloneSave(_session.CaptureSave());
            Assert.That(data.commission.stage == (int)ShadowEscortStage.Succeeded ||
                        data.commission.stage == (int)ShadowEscortStage.PartialSuccess ||
                        data.commission.stage == (int)ShadowEscortStage.Failed, Is.True,
                "the fixture must start from a real settlement.");
            data.commission.stage = (int)stage;
            data.commission.outcome = (int)outcome;
            data.commission.elapsedDays = data.commission.durationDays;
            data.commission.score = 10;
            data.commission.roll = 0;
            data.commission.rewardFunds = rewardFunds;
            data.commission.rewardSupplies = rewardSupplies;
            data.commission.lastFeedback = "存档回写用例";

            MissionHistorySaveEntry history = data.missionHistory == null
                ? null
                : Array.Find(data.missionHistory,
                    entry => entry != null && entry.runId == data.commission.runId);
            Assert.That(history, Is.Not.Null,
                "the settlement must record the terminal row the commission points at.");
            Assert.That(ShadowEscortOutcomeRules.TryMap(outcome, out SharedMissionOutcome mapped,
                out string mapError), Is.True, mapError);
            history.outcome = (int)mapped;
            history.fundsDelta = rewardFunds;
            history.suppliesDelta = rewardSupplies;
            return data;
        }

        [Test]
        public void InProgressCommission_WithDeadAssignee_IsRejectedOnValidateAndLoad()
        {
            PersonId assignee = M2FixedScenario.TokihimeId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            SaveData data = CloneSave(_session.CaptureSave());
            Person(data, assignee).alive = false;

            SaveOperationResult validation = SaveDataValidator.Validate(data);
            Assert.That(validation.Succeeded, Is.False);
            Assert.That(validation.Message, Does.Contain("在世"));

            GameSession loaded = GameSession.NewGame();
            try
            {
                Assert.That(loaded.TryApplySave(CloneSave(data)).Succeeded, Is.False);
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.NotAccepted));
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void InProgressCommission_WithBusyAssignee_IsRejectedOnValidate()
        {
            PersonId assignee = M2FixedScenario.TokihimeId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            SaveData data = CloneSave(_session.CaptureSave());
            DailyActionPersonSaveEntry row = DailyRow(data, assignee);
            row.hasAction = true;
            row.actionKind = (int)DailyActionKind.Training;
            row.actionDurationDays = 5;
            row.actionElapsedDays = 0;
            row.actionStartYear = data.year;
            row.actionStartMonth = data.month;
            row.actionStartDay = data.day;

            SaveOperationResult validation = SaveDataValidator.Validate(data);

            Assert.That(validation.Succeeded, Is.False);
            Assert.That(validation.Message, Does.Contain("每日行动"));
        }

        [Test]
        public void InProgressCommission_WithIndustryPost_IsRejectedOnValidate()
        {
            PersonId assignee = M2FixedScenario.TokihimeId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            SaveData data = CloneSave(_session.CaptureSave());
            IndustrySlotSaveEntry slot = IndustrySlot(data, IndustryCatalog.FarmId);
            slot.assignedPersonId = assignee.Value;

            SaveOperationResult validation = SaveDataValidator.Validate(data);

            Assert.That(validation.Succeeded, Is.False);
            Assert.That(validation.Message, Does.Contain("产业"));
        }

        [Test]
        public void CaptureSave_AndDoctrineProjection_ArePureForAnInProgressCommission()
        {
            PersonId assignee = M2FixedScenario.TokihimeId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            ShadowEscortStage stage = _session.CommissionStage;
            int elapsed = _session.CommissionElapsedDays;
            int chronicle = _session.ChronicleEntries.Count;
            IndustryResources resources = _session.Industry.Resources;

            Assert.That(_session.CaptureSave().commission.stage,
                Is.EqualTo((int)ShadowEscortStage.InProgress));
            Assert.That(_session.CaptureSave().succession.pregnancies, Is.Not.Null);
            Assert.That(FamilyDoctrineView.Build(_session), Is.Not.Null);

            Assert.That(_session.CommissionStage, Is.EqualTo(stage),
                "capture and doctrine projection must never settle or reset a commission.");
            Assert.That(_session.CommissionElapsedDays, Is.EqualTo(elapsed));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle),
                "capture must not append chronicle rows.");
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
        }

        [Test]
        public void UnauthorizedInProgressCommission_FailsAtTheSaveBoundaryAndAbandonsOnDayAdvance()
        {
            PersonId assignee = M2FixedScenario.TokihimeId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);

            // A crafted in-memory state that no public command can create: the assignee
            // is no longer a family member while the commission is still in progress.
            Assert.That(_session.Succession.World.TryGetPerson(assignee, out _), Is.True);
            Assert.That(_session.Succession.World.TryRemoveMember(assignee, out string removeError),
                Is.True, removeError);

            SaveData data = _session.CaptureSave();
            Assert.That(data.commission.stage,
                Is.EqualTo((int)ShadowEscortStage.InProgress),
                "capture is a pure snapshot and must not mutate the commission.");
            SaveOperationResult rejected = SaveDataValidator.Validate(data);
            Assert.That(rejected.Succeeded, Is.False,
                "the save boundary must reject a commission that cannot legally be saved.");
            Assert.That(rejected.Message, Does.Contain("在世"));

            string directory = Path.Combine(Path.GetTempPath(),
                "m05-save-boundary-" + Guid.NewGuid().ToString("N"));
            try
            {
                SaveOperationResult saved = _session.SaveTo(new SaveSlotStore(directory));
                Assert.That(saved.Succeeded, Is.False,
                    "SaveTo must report the same rejection instead of writing an unloadable file.");
                Assert.That(saved.Message, Does.Contain("在世"));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }

            // The explicit day transition is where gameplay resolves the failure.
            Assert.That(_session.AdvanceDays(1), Is.True);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned),
                "abandonment is a distinct terminal state, not a shared M07 result.");
            Assert.That(_session.CommissionOutcome, Is.EqualTo(CommissionOutcome.None));
            Assert.That(_session.MissionHistory, Is.Empty,
                "abandonment writes no shared-result history row.");
            Assert.That(_session.CommissionInvestedFunds, Is.GreaterThan(0),
                "the paid investment must stay recorded.");
            SaveData settled = _session.CaptureSave();
            Assert.That(SaveDataValidator.Validate(settled).Succeeded, Is.True);
            Assert.That(settled.chronicle, Has.Some.Matches<ChronicleSaveEntry>(entry =>
                entry != null && entry.subjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                entry.sourceId == "abandon"),
                "the explicit failure transition must leave a readable chronicle record.");
        }

        [Test]
        public void CrisisScenarioSwap_IsRejectedWhileAnInProgressCommissionExists()
        {
            _session.InitStandardDemo();
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);
            IndustryResources resources = _session.Industry.Resources;

            Assert.That(_session.StartCrisisDemo(), Is.False);
            Assert.That(_session.LastSuccessionMessage, Does.Contain("委托"));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
        }

        [Test]
        public void SettledCommission_DoesNotBlockTheCrisisDemo()
        {
            _session.InitStandardDemo();
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);
            Assert.That(_session.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None));

            Assert.That(_session.StartCrisisDemo(), Is.True,
                "a settled historical result must not permanently block the roster demo.");
        }

        /// <summary>
        /// The crisis roster is smaller than the standard one. Swapping it while an
        /// injured person would leave the roster makes every later save invalid, so the
        /// swap is refused with a readable reason and the injury is preserved.
        /// </summary>
        [Test]
        public void CrisisScenarioSwap_IsRejectedWhileANonCrisisPersonIsInjured()
        {
            _session.InitStandardDemo();
            PersonId injured = new PersonId("person.akechi.nobumasa");
            _session.SetInjuredForTest(injured, true);
            Assert.That(_session.IsPersonInjured(injured), Is.True);

            SaveData before = CloneSave(_session.CaptureSave());
            SaveOperationResult beforeValid = SaveDataValidator.Validate(before);
            Assert.That(beforeValid.Succeeded, Is.True, beforeValid.Message);

            Assert.That(_session.CanStartCrisisDemo, Is.True,
                "the entry stays available so it can explain the refusal.");
            Assert.That(_session.StartCrisisDemo(), Is.False);
            Assert.That(_session.LastSuccessionMessage, Does.Contain("伤员"),
                "the refusal must name the orphaned injury.");
            Assert.That(_session.Succession.HeadPersonId,
                Is.EqualTo(SuccessionSample.StandardHeadId),
                "the standard roster must not be replaced.");
            Assert.That(_session.IsPersonInjured(injured), Is.True,
                "the refusal must not delete the injury record.");

            SaveData after = CloneSave(_session.CaptureSave());
            SaveOperationResult afterValid = SaveDataValidator.Validate(after);
            Assert.That(afterValid.Succeeded, Is.True,
                "the refused swap must leave a valid save: " + afterValid.Message);
        }

        /// <summary>
        /// Terminal history keeps stable participant IDs. Replacing the roster with one
        /// that does not register those people would orphan the record, so the swap is
        /// refused instead of rewriting or deleting history.
        /// </summary>
        [Test]
        public void CrisisScenarioSwap_IsRejectedWhenHistoryNamesANonCrisisPerson()
        {
            _session.InitStandardDemo();
            PersonId assignee = FirstEligibleAssigneeOutsideCrisisRoster();
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);
            Assert.That(_session.MissionHistory, Has.Count.EqualTo(1));
            Assert.That(_session.MissionHistory[0].Participants[0], Is.EqualTo(assignee));

            SaveData before = CloneSave(_session.CaptureSave());
            SaveOperationResult beforeValid = SaveDataValidator.Validate(before);
            Assert.That(beforeValid.Succeeded, Is.True, beforeValid.Message);

            Assert.That(_session.CanStartCrisisDemo, Is.True,
                "the entry stays available so it can explain the refusal.");
            Assert.That(_session.StartCrisisDemo(), Is.False);
            Assert.That(_session.LastSuccessionMessage, Does.Contain("终局历史"),
                "the refusal must name the orphaned terminal-history record.");
            Assert.That(_session.Succession.HeadPersonId,
                Is.EqualTo(SuccessionSample.StandardHeadId),
                "the standard roster must not be replaced.");
            Assert.That(_session.MissionHistory, Has.Count.EqualTo(1),
                "the refusal must not delete terminal history.");

            SaveData after = CloneSave(_session.CaptureSave());
            SaveOperationResult afterValid = SaveDataValidator.Validate(after);
            Assert.That(afterValid.Succeeded, Is.True,
                "the refused swap must leave a valid save: " + afterValid.Message);
        }

        private PersonId FirstEligibleAssigneeOutsideCrisisRoster()
        {
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
            {
                if (!option.Eligible)
                    continue;
                if (option.PersonId == SuccessionSample.StandardHeadId ||
                    option.PersonId == new PersonId("person.akechi.chie") ||
                    option.PersonId == SuccessionSample.StandardDesignatedHeirId ||
                    option.PersonId == new PersonId("person.retainer.sakon"))
                    continue;
                return option.PersonId;
            }
            Assert.Fail("the standard roster must offer an assignee outside the crisis roster.");
            return default;
        }

        [Test]
        public void CommissionAssignment_AndResearchDuties_AreMutuallyExclusive()
        {
            PersonId researcher = _session.Succession.HeadPersonId;
            Assert.That(_session.TrySetResearchPlan(
                    FamilyResearchCatalog.SmokeMixingResearchId, researcher).Succeeded, Is.True,
                _session.LastResearchMessage);
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);

            ShadowEscortActionResult blocked = _session.TryAssignCommission(researcher);
            Assert.That(blocked.Succeeded, Is.False);
            Assert.That(blocked.Feedback, Does.Contain("研究"));

            _session.InitNewGame();
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);
            Assert.That(_session.TrySetResearchPlan(
                    FamilyResearchCatalog.SmokeMixingResearchId, head).Succeeded, Is.False);
            Assert.That(_session.LastResearchMessage, Does.Contain("委托"));
        }

        [Test]
        public void CommissionAssignment_AndEducationMentorDuties_AreMutuallyExclusive()
        {
            _session.InitEducationDemoScenario();
            PersonId learner = _session.Education.LearnerId;
            PersonId mentor = _session.EducationMentors[0].PersonId;
            Assert.That(_session.TrySetEducationPlan(learner, EducationDirection.MartialArts,
                mentor).Succeeded, Is.True, _session.LastEducationMessage);

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            ShadowEscortActionResult blocked = _session.TryAssignCommission(mentor);
            Assert.That(blocked.Succeeded, Is.False);
            Assert.That(blocked.Feedback, Does.Contain("导师"));

            _session.InitEducationDemoScenario();
            learner = _session.Education.LearnerId;
            mentor = _session.EducationMentors[0].PersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(mentor).Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.TrySetEducationPlan(learner, EducationDirection.MartialArts,
                mentor).Succeeded, Is.False);
            Assert.That(_session.LastEducationMessage, Does.Contain("委托"));
        }

        [Test]
        public void CommissionDayAdvance_NotifiesCountdownChangeBeforeDue()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(_session.Succession.HeadPersonId).Succeeded,
                Is.True);
            int notifications = 0;
            void OnChanged() => notifications++;

            _session.SessionStateChanged += OnChanged;
            try
            {
                Assert.That(_session.AdvanceOneDay(), Is.True);
            }
            finally
            {
                _session.SessionStateChanged -= OnChanged;
            }

            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.CommissionElapsedDays, Is.EqualTo(1));
            Assert.That(notifications, Is.GreaterThan(0),
                "an elapsed-day change must refresh open commission UI.");
        }

        [Test]
        public void CommissionWindowAndStatus_AreZeroBeforeAssignmentAndSettledHistory()
        {
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.NotAccepted));
            Assert.That(_session.CommissionDurationDays, Is.Zero);
            Assert.That(_session.CommissionRemainingDays, Is.Zero);
            ShadowEscortCommissionStatus status = _session.CommissionStatus;
            Assert.That(status.Stage, Is.EqualTo(ShadowEscortStage.NotAccepted));
            Assert.That(status.IsSettled, Is.False);
            Assert.That(status.DurationDays, Is.Zero);
            Assert.That(status.RemainingDays, Is.Zero);
            Assert.That(status.Feedback, Is.Empty);

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.CommissionDurationDays, Is.Zero,
                "an accepted offer has not started its window.");
            Assert.That(_session.CommissionRemainingDays, Is.Zero);
            Assert.That(_session.CommissionStatus.Feedback, Is.Not.Null.And.Not.Empty);

            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);
            Assert.That(_session.CommissionDurationDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));
            Assert.That(_session.CommissionRemainingDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));

            Assert.That(_session.AdvanceDays(1), Is.True);
            Assert.That(_session.CommissionDurationDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));
            Assert.That(_session.CommissionRemainingDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays - 1));

            Assert.That(_session.AdvanceDays(2), Is.True);
            status = _session.CommissionStatus;
            Assert.That(status.IsSettled, Is.True);
            Assert.That(status.DurationDays, Is.EqualTo(ShadowEscortCommission.DurationDays));
            Assert.That(status.RemainingDays, Is.Zero);
            Assert.That(status.Outcome, Is.Not.EqualTo(CommissionOutcome.None));
            Assert.That(status.Score, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void InProgressCommission_WithResearchDuty_IsRejectedOnValidate()
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            SaveData data = CloneSave(_session.CaptureSave());
            data.research.researchId = FamilyResearchCatalog.SmokeMixingResearchId;
            data.research.researcherPersonId = assignee.Value;
            data.research.progress = 0;
            data.research.knownTechniqueIds = new string[0];

            SaveOperationResult validation = SaveDataValidator.Validate(data);

            Assert.That(validation.Succeeded, Is.False);
            Assert.That(validation.Message, Does.Contain("研究"));
        }

        [Test]
        public void InProgressCommission_WithEducationMentorDuty_IsRejectedOnValidate()
        {
            GameSession mentorSession = GameSession.NewGame();
            try
            {
                mentorSession.InitEducationDemoScenario();
                PersonId learner = mentorSession.Education.LearnerId;
                PersonId mentor = mentorSession.EducationMentors[0].PersonId;
                Assert.That(mentorSession.TrySetEducationPlan(learner,
                    EducationDirection.MartialArts, mentor).Succeeded, Is.True,
                    mentorSession.LastEducationMessage);
                SaveData withPlan = mentorSession.CaptureSave();

                _session.InitEducationDemoScenario();
                Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
                Assert.That(_session.TryAssignCommission(mentor).Succeeded, Is.True,
                    _session.LastCommissionMessage);
                SaveData data = CloneSave(_session.CaptureSave());
                data.development = withPlan.development;

                SaveOperationResult validation = SaveDataValidator.Validate(data);
                Assert.That(validation.Succeeded, Is.False);
                Assert.That(validation.Message, Does.Contain("培养导师"));
            }
            finally
            {
                Object.DestroyImmediate(mentorSession.gameObject);
            }
        }

        [Test]
        public void CommissionStartDateInTheFuture_IsRejectedOnValidate()
        {
            PersonId assignee = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            SaveData data = CloneSave(_session.CaptureSave());
            data.commission.startYear = data.year + 1;
            SaveOperationResult rejection = SaveDataValidator.Validate(data);
            Assert.That(rejection.Succeeded, Is.False);
            Assert.That(rejection.Message, Does.Contain("晚于"));

            SaveData settled = CloneSave(_session.CaptureSave());
            settled.commission.stage = (int)ShadowEscortStage.Failed;
            settled.commission.outcome = (int)CommissionOutcome.Failure;
            settled.commission.elapsedDays = settled.commission.durationDays;
            settled.commission.startYear = settled.year + 1;
            SaveOperationResult settledRejection = SaveDataValidator.Validate(settled);
            Assert.That(settledRejection.Succeeded, Is.False);
            Assert.That(settledRejection.Message, Does.Contain("晚于"));
        }

        [Test]
        public void MonthTurn_SettlesCommissionOnTheSimulatedDueDay()
        {
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True,
                _session.LastEventMessage);
            Assert.That(_session.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None));

            FamilyChronicleEntry settle = default;
            foreach (FamilyChronicleEntry entry in _session.ChronicleEntries)
                if (entry.SubjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                    entry.SourceId == "settle")
                    settle = entry;
            Assert.That(settle, Is.Not.Null, "the settlement must be chronicled.");
            Assert.That(settle.Year, Is.EqualTo(1560));
            Assert.That(settle.Month, Is.EqualTo(4),
                "a 4/1 commission due on 4/4 must not be stamped with the next month's day 1.");
            Assert.That(settle.Summary, Does.Contain("1560-04-04"),
                "the settlement record must expose the exact simulated due day.");
            System.Text.RegularExpressions.Match stamp =
                System.Text.RegularExpressions.Regex.Match(settle.Summary,
                    @"（(\d{4})-(\d{2})-(\d{2})）");
            Assert.That(stamp.Success, Is.True,
                "the M05 due-day summary keeps the unchanged chronicle schema.");
            Assert.That(int.Parse(stamp.Groups[1].Value), Is.EqualTo(settle.Year));
            Assert.That(int.Parse(stamp.Groups[2].Value), Is.EqualTo(settle.Month));
            Assert.That(stamp.Groups[3].Value, Is.EqualTo("04"),
                "the summary and year/month fields must describe the same due day.");
        }

        [Test]
        public void InProgressCommission_ElapsedMustMatchCalendarDaysSinceStart()
        {
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);

            Assert.That(SaveDataValidator.Validate(CloneSave(_session.CaptureSave())).Succeeded,
                Is.True, "a same-day start with zero elapsed days is valid.");

            // The reported exploit: start date == save date but two days of progress.
            SaveData sameDayExcess = CloneSave(_session.CaptureSave());
            sameDayExcess.commission.elapsedDays = 2;
            SaveOperationResult excess = SaveDataValidator.Validate(sameDayExcess);
            Assert.That(excess.Succeeded, Is.False);
            Assert.That(excess.Message, Does.Contain("模拟天数"));

            SaveData sameDayDue = CloneSave(_session.CaptureSave());
            sameDayDue.commission.elapsedDays = ShadowEscortCommission.DurationDays;
            Assert.That(SaveDataValidator.Validate(sameDayDue).Succeeded, Is.False,
                "a same-day row cannot already be due.");

            Assert.That(_session.AdvanceDays(2), Is.True);
            SaveData onTime = CloneSave(_session.CaptureSave());
            Assert.That(onTime.commission.elapsedDays, Is.EqualTo(2));
            Assert.That(SaveDataValidator.Validate(onTime).Succeeded, Is.True,
                "elapsed days matching the two simulated days are valid.");

            // Due-day cap: a row that reached its 3-day window stays loadable on later
            // days so a failed settlement can be retried.
            SaveData capped = CloneSave(onTime);
            capped.commission.startMonth = 3;
            capped.commission.startDay = 30;
            capped.commission.elapsedDays = ShadowEscortCommission.DurationDays;
            Assert.That(SaveDataValidator.Validate(capped).Succeeded, Is.True,
                "a due row must remain valid while it waits for a settlement retry.");
        }

        /// <summary>
        /// M05 work-duty rule: a newborn and a young teenager cannot be offered or
        /// accepted as the escort assignee. The preview row and the command must agree,
        /// and the accepted offer must not change state on rejection.
        /// </summary>
        [Test]
        public void NewbornAndMinorFamilyMembers_AreRejectedByPreviewAndCommand()
        {
            _session.InitStandardDemo();
            GameDate date = _session.WorldClock.CurrentDate;
            var newborn = new PersonId("person.m05.newborn");
            Assert.That(_session.Succession.World.TryAddPerson(new PersonState(newborn, "M05 婴儿"),
                    new SuccessionPersonLife(date.Year, true, false), out string personError),
                Is.True, personError);
            Assert.That(_session.Succession.World.TryAddMember(newborn,
                FamilyMemberRole.DirectKin, out string memberError), Is.True, memberError);
            var teenager = new PersonId("person.akechi.tokihime");

            foreach (PersonId candidate in new[] { newborn, teenager })
            {
                Assert.That(_session.Succession.World.TryGetLife(candidate,
                    out SuccessionPersonLife life), Is.True);
                Assert.That(life.AgeAt(date), Is.LessThan(_session.Succession.AdulthoodAge),
                    "the fixture must be younger than the configured adulthood age.");
            }

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            foreach (PersonId candidate in new[] { newborn, teenager })
            {
                CommissionAssigneeOption option = CommissionAssigneeOptionFor(candidate);
                Assert.That(option.Eligible, Is.False);
                Assert.That(option.Reason, Is.EqualTo("未成年"));
                Assert.That(option.DisplayName, Is.Not.Empty);

                ShadowEscortActionResult assign = _session.TryAssignCommission(candidate);
                Assert.That(assign.Succeeded, Is.False);
                Assert.That(assign.Feedback, Does.Contain("未成年"));
                Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
                Assert.That(_session.CommissionAssignee.IsEmpty, Is.True);
            }
        }

        /// <summary>
        /// M05 work-duty rule: a retired former head stays alive and in the family, yet
        /// may not take a new commission. The handoff is the real retirement command, so
        /// this is the exact state the game creates.
        /// </summary>
        [Test]
        public void RetiredFormerHead_IsRejectedByPreviewAndCommand()
        {
            _session.InitStandardDemo();
            PersonId oldHead = _session.Succession.HeadPersonId;
            PersonId successor = new PersonId("person.akechi.masatsuna");
            RetireHeadWithSuccessor(successor);

            Assert.That(_session.Succession.World.TryGetLife(oldHead,
                out SuccessionPersonLife life), Is.True);
            Assert.That(life.Alive, Is.True);
            Assert.That(life.Retired, Is.True);

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            CommissionAssigneeOption option = CommissionAssigneeOptionFor(oldHead);
            Assert.That(option.Eligible, Is.False);
            Assert.That(option.Reason, Is.EqualTo("已隐居（退休）"));

            ShadowEscortActionResult assign = _session.TryAssignCommission(oldHead);
            Assert.That(assign.Succeeded, Is.False);
            Assert.That(assign.Feedback, Does.Contain("隐居"));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.CommissionAssignee.IsEmpty, Is.True);

            // The duty rule must not make the whole roster ineligible: the new head is an
            // adult family member with no conflicting duty and stays assignable.
            Assert.That(CommissionAssigneeOptionFor(successor).Eligible, Is.True);
            Assert.That(_session.TryAssignCommission(successor).Succeeded, Is.True,
                _session.LastCommissionMessage);
        }

        /// <summary>
        /// M05: a person who retires during a running commission keeps a saveable row,
        /// but settlement can no longer use that assignee. The due date therefore closes
        /// the run once through the approved Abandoned semantics instead of failing every
        /// retry: no shared result, history, standing, reward or refund.
        /// </summary>
        [Test]
        public void AssigneeRetirement_MidCommission_EndsAsSaveableAbandonment()
        {
            _session.InitStandardDemo();
            PersonId oldHead = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(oldHead).Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.AdvanceDays(1), Is.True);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));

            RetireHeadWithSuccessor(new PersonId("person.akechi.masatsuna"));

            Assert.That(_session.Succession.World.TryGetLife(oldHead,
                out SuccessionPersonLife retired), Is.True);
            Assert.That(retired.Retired, Is.True);
            Assert.That(_session.Succession.HeadPersonId, Is.Not.EqualTo(oldHead));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress),
                "retirement alone does not interrupt the running trip.");
            Assert.That(_session.CommissionAssignee, Is.EqualTo(oldHead));
            IndustryResources invested = _session.Industry.Resources;

            SaveData inProgress = CloneSave(_session.CaptureSave());
            SaveOperationResult validation = SaveDataValidator.Validate(inProgress);
            Assert.That(validation.Succeeded, Is.True, validation.Message);

            GameSession loaded = GameSession.NewGame();
            try
            {
                SaveOperationResult applied = loaded.TryApplySave(CloneSave(inProgress));
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
                Assert.That(loaded.CommissionAssignee, Is.EqualTo(oldHead));

                Assert.That(loaded.AdvanceDays(loaded.CommissionRemainingDays), Is.True);
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned),
                    "an unavailable assignee must terminate the run, not wedge it.");
                Assert.That(loaded.CommissionOutcome, Is.EqualTo(CommissionOutcome.None));
                Assert.That(loaded.LastCommissionMessage, Does.Contain("无法执行护送委托"));
                Assert.That(loaded.MissionHistory, Is.Empty,
                    "abandonment writes no shared-result history row.");
                Assert.That(loaded.Industry.Resources, Is.EqualTo(invested),
                    "abandonment pays no reward and refunds no investment.");
                Assert.That(loaded.CommissionInvestedFunds, Is.GreaterThan(0),
                    "the paid investment must stay recorded.");

                SaveData abandoned = CloneSave(loaded.CaptureSave());
                Assert.That(abandoned.commission.stage,
                    Is.EqualTo((int)ShadowEscortStage.Abandoned));
                Assert.That(abandoned.commission.runId, Is.Empty,
                    "abandonment is not a shared result and keeps no run ID.");
                Assert.That(SaveDataValidator.Validate(abandoned).Succeeded, Is.True,
                    "the abandoned row must remain saveable.");
                Assert.That(abandoned.chronicle, Has.Some.Matches<ChronicleSaveEntry>(entry =>
                        entry != null &&
                        entry.subjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                        entry.sourceId == "abandon"),
                    "the abort must retain its own action chronicle row.");

                GameSession reloaded = GameSession.NewGame();
                try
                {
                    SaveOperationResult reloadedApplied = reloaded.TryApplySave(abandoned);
                    Assert.That(reloadedApplied.Succeeded, Is.True, reloadedApplied.Message);
                    Assert.That(reloaded.CommissionStage,
                        Is.EqualTo(ShadowEscortStage.Abandoned));
                    Assert.That(reloaded.MissionHistory, Is.Empty);
                }
                finally
                {
                    Object.DestroyImmediate(reloaded.gameObject);
                }
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        /// <summary>
        /// M05 manual terminal: the due date leaves the row for the explicit settle
        /// button, and clicking it with an unavailable assignee closes the run once
        /// through the same Abandoned semantics as the delegated path. The failed result
        /// is deliberate: nothing was settled, but the row must not stay wedged.
        /// </summary>
        [Test]
        public void ManualSettlement_WithUnavailableAssignee_AbandonsOnceAndStaysSaveable()
        {
            _session.InitStandardDemo();
            PersonId oldHead = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Manual,
                out string modeError), Is.True, modeError);
            Assert.That(_session.TryAssignCommission(oldHead).Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.AdvanceDays(1), Is.True);

            RetireHeadWithSuccessor(new PersonId("person.akechi.masatsuna"));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress),
                "retirement alone does not interrupt the running trip.");

            IndustryResources invested = _session.Industry.Resources;
            int fame = _session.FamilyFame;
            int concealment = _session.Concealment;
            int chronicleRows = _session.ChronicleEntries.Count;

            Assert.That(_session.AdvanceDays(_session.CommissionRemainingDays), Is.True);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress),
                "manual play waits for the explicit settle button on the due date.");
            Assert.That(_session.IsCommissionManualSettlementAvailable, Is.True);

            MissionSettlementResult settlement = _session.TrySettleCommissionManually();
            Assert.That(settlement.Succeeded, Is.False,
                "an unavailable assignee cannot settle a shared result.");
            Assert.That(settlement.Message, Does.Contain("无法执行护送委托"));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned));
            Assert.That(_session.CommissionOutcome, Is.EqualTo(CommissionOutcome.None));
            Assert.That(_session.MissionHistory, Is.Empty,
                "abandonment writes no shared-result history row.");
            Assert.That(_session.Industry.Resources, Is.EqualTo(invested),
                "abandonment pays no reward and refunds no investment.");
            Assert.That(_session.FamilyFame, Is.EqualTo(fame));
            Assert.That(_session.Concealment, Is.EqualTo(concealment));
            Assert.That(_session.CommissionInvestedFunds, Is.GreaterThan(0),
                "the paid investment must stay recorded.");
            Assert.That(_session.CommissionStatus.Feedback, Does.Contain("无法执行护送委托"));

            int abandonRows = 0;
            foreach (FamilyChronicleEntry entry in _session.ChronicleEntries)
                if (entry.Category == ChronicleCategory.Mission &&
                    entry.SubjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                    entry.SourceId == "abandon")
                    abandonRows++;
            Assert.That(abandonRows, Is.EqualTo(1),
                "the manual abort must leave exactly one action chronicle row.");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleRows + 1));

            Assert.That(_session.TrySettleCommissionManually().Succeeded, Is.False,
                "a closed commission cannot be settled again.");
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleRows + 1),
                "a retry must not append a second action row.");

            SaveData abandoned = CloneSave(_session.CaptureSave());
            Assert.That(abandoned.commission.stage,
                Is.EqualTo((int)ShadowEscortStage.Abandoned));
            Assert.That(abandoned.commission.runId, Is.Empty);
            SaveOperationResult validated = SaveDataValidator.Validate(abandoned);
            Assert.That(validated.Succeeded, Is.True,
                "the abandoned row must remain saveable: " + validated.Message);

            GameSession loaded = GameSession.NewGame();
            try
            {
                SaveOperationResult applied = loaded.TryApplySave(CloneSave(abandoned));
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned));
                Assert.That(loaded.MissionHistory, Is.Empty);
                Assert.That(loaded.CommissionStatus.Feedback,
                    Does.Contain("无法执行护送委托"),
                    "the persisted abandon reason must reach the panel after a load.");
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        private CommissionAssigneeOption CommissionAssigneeOptionFor(PersonId personId)
        {
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
                if (option.PersonId == personId)
                    return option;
            Assert.Fail("Missing commission assignee option " + personId.Value);
            return default;
        }

        private void RetireHeadWithSuccessor(PersonId successor)
        {
            SuccessionResult begun = _session.BeginSuccessionRetirement();
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            if (_session.Succession.Phase == SuccessionPhase.AwaitingSuccessionChoice ||
                _session.Succession.Phase == SuccessionPhase.AwaitingRegentChoice)
                Assert.That(_session.SelectSuccessionSuccessor(successor).Succeeded, Is.True,
                    _session.LastSuccessionMessage);
            SuccessionResult confirmed = _session.ConfirmSuccession();
            Assert.That(confirmed.Succeeded, Is.True, confirmed.Message);
        }

        private static SuccessionPersonSaveEntry Person(SaveData data, PersonId personId)
        {
            foreach (SuccessionPersonSaveEntry entry in data.succession.persons)
                if (entry != null && entry.personId == personId.Value)
                    return entry;
            Assert.Fail("Missing saved person " + personId.Value);
            return null;
        }

        private static DailyActionPersonSaveEntry DailyRow(SaveData data, PersonId personId)
        {
            foreach (DailyActionPersonSaveEntry entry in data.dailyActions.persons)
                if (entry != null && entry.personId == personId.Value)
                    return entry;
            Assert.Fail("Missing daily-action row for " + personId.Value);
            return null;
        }

        private static IndustrySlotSaveEntry IndustrySlot(SaveData data, string industryId)
        {
            foreach (IndustrySlotSaveEntry slot in data.industry.slots)
                if (slot != null && slot.industryId == industryId)
                    return slot;
            Assert.Fail("Missing industry slot " + industryId);
            return null;
        }

        private bool TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            if (!preview.HasValue)
                return false;
            return _session.ConfirmTravel(preview.Value);
        }

        private PersonId FindOtherLivingMember(PersonId excluded)
        {
            foreach (PersonState person in _session.Succession.World.Persons)
            {
                if (person.Id == excluded || !_session.Succession.World.IsLiving(person.Id))
                    continue;
                if (_session.Succession.World.Family.IsMember(person.Id))
                    return person.Id;
            }
            return default;
        }

        private static SaveData CloneSave(SaveData source)
        {
            // Round-trip through JSON so each load starts from independent data and no
            // test can pass by sharing the same object graph.
            string json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
