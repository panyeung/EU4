using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class SupplyShortageMissionTests
    {
        [Test]
        public void NewMission_StartsNotAcceptedWithZeroMerit()
        {
            var mission = new SupplyShortageMission();
            Assert.That(mission.Stage, Is.EqualTo(SupplyShortageStage.NotAccepted));
            Assert.That(mission.Merit, Is.Zero);
        }

        [TestCase("owari.atsuta")]
        [TestCase("owari.nagoya")]
        [TestCase("unknown")]
        [TestCase(null)]
        public void Accept_SucceedsOnlyInKiyosu(string wrongLocation)
        {
            var mission = new SupplyShortageMission();
            AssertResult(mission.TryAccept(new LocationId(wrongLocation)), false, SupplyShortageStage.NotAccepted, 0);
            AssertResult(mission.TryAccept(LocationId.Kiyosu), true, SupplyShortageStage.Accepted, 0);
            Assert.That(mission.Stage, Is.EqualTo(SupplyShortageStage.Accepted));
            Assert.That(mission.Merit, Is.Zero);
        }

        [Test]
        public void Investigate_RequiresAcceptanceInAtsuta()
        {
            var mission = new SupplyShortageMission();
            AssertResult(mission.TryInvestigate(LocationId.Atsuta), false, SupplyShortageStage.NotAccepted, 0);
            AssertResult(mission.TryAccept(LocationId.Kiyosu), true, SupplyShortageStage.Accepted, 0);
            AssertResult(mission.TryInvestigate(LocationId.Kiyosu), false, SupplyShortageStage.Accepted, 0);
            AssertResult(mission.TryInvestigate(LocationId.Atsuta), true, SupplyShortageStage.Investigated, 0);
            Assert.That(mission.Stage, Is.EqualTo(SupplyShortageStage.Investigated));
            Assert.That(mission.Merit, Is.Zero);
        }

        [Test]
        public void Negotiate_RequiresInvestigationAndKurodaInAtsuta()
        {
            var mission = new SupplyShortageMission();
            AssertResult(mission.TryNegotiate(LocationId.Atsuta, CharacterId.KurodaIori), false, SupplyShortageStage.NotAccepted, 0);
            mission.TryAccept(LocationId.Kiyosu);
            AssertResult(mission.TryNegotiate(LocationId.Atsuta, CharacterId.KurodaIori), false, SupplyShortageStage.Accepted, 0);
            mission.TryInvestigate(LocationId.Atsuta);
            AssertResult(mission.TryNegotiate(LocationId.Kiyosu, CharacterId.KurodaIori), false, SupplyShortageStage.Investigated, 0);
            AssertResult(mission.TryNegotiate(LocationId.Atsuta, CharacterId.SakaiJirobei), false, SupplyShortageStage.Investigated, 0);
            AssertResult(mission.TryNegotiate(LocationId.Atsuta, new CharacterId("unknown")), false, SupplyShortageStage.Investigated, 0);
            AssertResult(mission.TryNegotiate(LocationId.Atsuta, CharacterId.KurodaIori), true, SupplyShortageStage.Negotiated, 0);
            Assert.That(mission.Stage, Is.EqualTo(SupplyShortageStage.Negotiated));
            Assert.That(mission.Merit, Is.Zero);
        }

        [Test]
        public void Report_RequiresNegotiationAndKiyosu()
        {
            var mission = new SupplyShortageMission();
            AssertResult(mission.TryReport(LocationId.Kiyosu), false, SupplyShortageStage.NotAccepted, 0);
            mission.TryAccept(LocationId.Kiyosu);
            AssertResult(mission.TryReport(LocationId.Kiyosu), false, SupplyShortageStage.Accepted, 0);
            mission.TryInvestigate(LocationId.Atsuta);
            AssertResult(mission.TryReport(LocationId.Kiyosu), false, SupplyShortageStage.Investigated, 0);
            mission.TryNegotiate(LocationId.Atsuta, CharacterId.KurodaIori);
            AssertResult(mission.TryReport(LocationId.Atsuta), false, SupplyShortageStage.Negotiated, 0);
            AssertResult(mission.TryReport(LocationId.Kiyosu), true, SupplyShortageStage.RewardClaimed, 10);
            Assert.That(mission.Stage, Is.EqualTo(SupplyShortageStage.RewardClaimed));
            Assert.That(mission.Merit, Is.EqualTo(10));
        }

        [Test]
        public void Report_AwardsTenMeritExactlyOnce()
        {
            var mission = new SupplyShortageMission();
            mission.TryAccept(LocationId.Kiyosu);
            mission.TryInvestigate(LocationId.Atsuta);
            mission.TryNegotiate(LocationId.Atsuta, CharacterId.KurodaIori);
            AssertResult(mission.TryReport(LocationId.Kiyosu), true, SupplyShortageStage.RewardClaimed, 10);
            AssertResult(mission.TryReport(LocationId.Kiyosu), false, SupplyShortageStage.RewardClaimed, 10);
            Assert.That(mission.Stage, Is.EqualTo(SupplyShortageStage.RewardClaimed));
            Assert.That(mission.Merit, Is.EqualTo(10));
        }

        private static void AssertResult(SupplyShortageActionResult result, bool succeeded, SupplyShortageStage stage, int merit)
        {
            Assert.That(result.Succeeded, Is.EqualTo(succeeded), result.Feedback);
            Assert.That(result.Stage, Is.EqualTo(stage));
            Assert.That(result.Merit, Is.EqualTo(merit));
            Assert.That(result.Feedback, Is.Not.Null.And.Not.Empty);
        }
    }
}
