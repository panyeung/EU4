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
