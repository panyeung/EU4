using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;

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
}
