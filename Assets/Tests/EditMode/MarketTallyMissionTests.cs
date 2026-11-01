using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class MarketTallyMissionTests
    {
        [Test]
        public void Report_CompletesNagoyaKiyosuNagoyaRouteAndAwardsFiveOnce()
        {
            var mission = new MarketTallyMission();
            Assert.That(mission.Stage, Is.EqualTo(MarketTallyStage.NotAccepted));
            Assert.That(mission.Merit, Is.Zero);
            AssertResult(mission.TryAccept(LocationId.Nagoya), true, MarketTallyStage.Accepted, 0);
            AssertResult(mission.TryInvestigate(LocationId.Kiyosu), true, MarketTallyStage.Investigated, 0);
            AssertResult(mission.TryReport(LocationId.Nagoya), true, MarketTallyStage.RewardClaimed, 5);
            AssertResult(mission.TryReport(LocationId.Nagoya), false, MarketTallyStage.RewardClaimed, 5);
            Assert.That(mission.Merit, Is.EqualTo(5));
        }

        private static IEnumerable<TestCaseData> RejectedActions()
        {
            foreach (MarketTallyStage stage in System.Enum.GetValues(typeof(MarketTallyStage)))
            foreach (string action in new[] { "accept", "investigate", "report" })
            foreach (string location in new[] { "owari.kiyosu", "owari.atsuta", "owari.nagoya", "unknown", null })
            {
                bool valid = action == "accept" ? stage == MarketTallyStage.NotAccepted && location == "owari.nagoya"
                    : action == "investigate" ? stage == MarketTallyStage.Accepted && location == "owari.kiyosu"
                    : stage == MarketTallyStage.Investigated && location == "owari.nagoya";
                if (!valid) yield return new TestCaseData(stage, action, location);
            }
        }

        [TestCaseSource(nameof(RejectedActions))]
        public void InvalidAction_LeavesStateAndRewardUnchanged(MarketTallyStage stage, string action, string location)
        {
            var mission = new MarketTallyMission();
            if (stage != MarketTallyStage.NotAccepted) mission.TryAccept(LocationId.Nagoya);
            if (stage == MarketTallyStage.Investigated || stage == MarketTallyStage.RewardClaimed) mission.TryInvestigate(LocationId.Kiyosu);
            if (stage == MarketTallyStage.RewardClaimed) mission.TryReport(LocationId.Nagoya);
            int merit = mission.Merit;
            MarketTallyActionResult result;
            switch (action)
            {
                case "accept": result = mission.TryAccept(new LocationId(location)); break;
                case "investigate": result = mission.TryInvestigate(new LocationId(location)); break;
                default: result = mission.TryReport(new LocationId(location)); break;
            }
            AssertResult(result, false, stage, merit);
            Assert.That(mission.Stage, Is.EqualTo(stage));
            Assert.That(mission.Merit, Is.EqualTo(merit));
        }

        private static void AssertResult(MarketTallyActionResult result, bool succeeded, MarketTallyStage stage, int merit)
        {
            Assert.That(result.Succeeded, Is.EqualTo(succeeded), result.Feedback);
            Assert.That(result.Stage, Is.EqualTo(stage));
            Assert.That(result.Merit, Is.EqualTo(merit));
            Assert.That(result.Feedback, Is.Not.Null.And.Not.Empty);
        }
    }
}
