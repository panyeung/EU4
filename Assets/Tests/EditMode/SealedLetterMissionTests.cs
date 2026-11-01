using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class SealedLetterMissionTests
    {
        [Test]
        public void Deliver_CompletesKiyosuToNagoyaRouteAndAwardsFiveOnce()
        {
            var mission = new SealedLetterMission();
            Assert.That(mission.Stage, Is.EqualTo(SealedLetterStage.NotAccepted));
            Assert.That(mission.Merit, Is.Zero);
            AssertResult(mission.TryAccept(LocationId.Kiyosu), true, SealedLetterStage.Accepted, 0);
            AssertResult(mission.TryDeliver(LocationId.Nagoya), true, SealedLetterStage.RewardClaimed, 5);
            AssertResult(mission.TryDeliver(LocationId.Nagoya), false, SealedLetterStage.RewardClaimed, 5);
            Assert.That(mission.Merit, Is.EqualTo(5));
        }

        private static IEnumerable<TestCaseData> RejectedActions()
        {
            foreach (SealedLetterStage stage in System.Enum.GetValues(typeof(SealedLetterStage)))
            foreach (string action in new[] { "accept", "deliver" })
            foreach (string location in new[] { "owari.kiyosu", "owari.atsuta", "owari.nagoya", "unknown", null })
            {
                bool valid = action == "accept"
                    ? stage == SealedLetterStage.NotAccepted && location == "owari.kiyosu"
                    : stage == SealedLetterStage.Accepted && location == "owari.nagoya";
                if (!valid) yield return new TestCaseData(stage, action, location);
            }
        }

        [TestCaseSource(nameof(RejectedActions))]
        public void InvalidAction_LeavesStateAndRewardUnchanged(SealedLetterStage stage, string action, string location)
        {
            var mission = new SealedLetterMission();
            if (stage != SealedLetterStage.NotAccepted) mission.TryAccept(LocationId.Kiyosu);
            if (stage == SealedLetterStage.RewardClaimed) mission.TryDeliver(LocationId.Nagoya);
            int merit = mission.Merit;
            var result = action == "accept" ? mission.TryAccept(new LocationId(location)) : mission.TryDeliver(new LocationId(location));
            AssertResult(result, false, stage, merit);
            Assert.That(mission.Stage, Is.EqualTo(stage));
            Assert.That(mission.Merit, Is.EqualTo(merit));
        }

        private static void AssertResult(SealedLetterActionResult result, bool succeeded, SealedLetterStage stage, int merit)
        {
            Assert.That(result.Succeeded, Is.EqualTo(succeeded), result.Feedback);
            Assert.That(result.Stage, Is.EqualTo(stage));
            Assert.That(result.Merit, Is.EqualTo(merit));
            Assert.That(result.Feedback, Is.Not.Null.And.Not.Empty);
        }
    }
}
