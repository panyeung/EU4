using NUnit.Framework;
using UnityEngine;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class TownLocalActionsTests
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
        public void ClanSeat_ShowsAvailableEscortAndInfiltrationEntries()
        {
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.NotAccepted));
            Assert.That(_session.CanBeginTacticalMission, Is.True);

            string summary = TownLocalActions.Describe(_session);

            Assert.That(summary, Does.Contain("补给短缺：领取"));
            Assert.That(summary, Does.Contain("密信：领取"));
            Assert.That(summary, Does.Contain("护送委托：领取"));
            Assert.That(summary, Does.Contain("潜入密令：开始"));
        }

        [Test]
        public void Atsuta_ShowsTheAvailableLocalMissionStage()
        {
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            TravelPreview? preview = _session.PreviewTravel(LocationId.Atsuta);
            Assert.That(preview.HasValue, Is.True);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);

            string summary = TownLocalActions.Describe(_session);

            Assert.That(summary, Does.Contain("补给短缺：调查"));
            Assert.That(summary, Does.Not.Contain("护送委托：领取"));
            Assert.That(summary, Does.Not.Contain("潜入密令：开始"));
        }

        [Test]
        public void Atsuta_WithoutAvailableMissionShowsTheEmptyState()
        {
            TravelPreview preview = _session.PreviewTravel(LocationId.Atsuta).Value;
            Assert.That(_session.ConfirmTravel(preview), Is.True);

            Assert.That(TownLocalActions.Describe(_session), Is.EqualTo("当地委托\n当前暂无可办理的本地委托"));
        }

        [Test]
        public void Nagoya_ShowsItsAvailableLetterAndMarketActivities()
        {
            Assert.That(_session.TryAcceptSealedLetterMission().Succeeded, Is.True);
            TravelPreview preview = _session.PreviewTravel(LocationId.Nagoya).Value;
            Assert.That(_session.ConfirmTravel(preview), Is.True);

            string summary = TownLocalActions.Describe(_session);

            Assert.That(summary, Does.Contain("密信：交付"));
            Assert.That(summary, Does.Contain("市集账目：领取"));
            Assert.That(summary, Does.Not.Contain("补给短缺：领取"));
            Assert.That(summary, Does.Not.Contain("护送委托：领取"));
            Assert.That(summary, Does.Not.Contain("潜入密令：开始"));
        }

        [Test]
        public void AwayFromClanSeat_HidesClanSeatOnlyEntries()
        {
            TravelPreview preview = _session.PreviewTravel(LocationId.Nagoya).Value;
            Assert.That(_session.ConfirmTravel(preview), Is.True);

            string summary = TownLocalActions.Describe(_session);

            Assert.That(summary, Does.Not.Contain("护送委托：领取"));
            Assert.That(summary, Does.Not.Contain("潜入密令：开始"));
        }
    }
}
