using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class PromotionRulesTests
    {
        [TestCase(0, PromotionRank.Ashigaru)]
        [TestCase(4, PromotionRank.Ashigaru)]
        [TestCase(5, PromotionRank.Banto)]
        [TestCase(9, PromotionRank.Banto)]
        [TestCase(10, PromotionRank.Kumigashira)]
        [TestCase(19, PromotionRank.Kumigashira)]
        [TestCase(20, PromotionRank.Bushogashira)]
        [TestCase(30, PromotionRank.Bushogashira)]
        [TestCase(-7, PromotionRank.Ashigaru)]
        public void RankFor_MapsThresholdsAndBoundaries(int merit, PromotionRank expected)
        {
            Assert.That(PromotionRules.RankFor(merit), Is.EqualTo(expected));
        }

        [Test]
        public void ChineseName_CoversEveryTier()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (PromotionRank rank in System.Enum.GetValues(typeof(PromotionRank)))
            {
                string name = PromotionRules.ChineseName(rank);
                Assert.That(name, Is.Not.Empty);
                Assert.That(seen.Add(name), Is.True, $"Duplicate name for {rank}.");
            }
            Assert.That(seen.Count, Is.EqualTo(PromotionRules.TierCount));
        }

        [Test]
        public void MinimumMerit_MatchesRankForBoundaries()
        {
            foreach (PromotionRank rank in System.Enum.GetValues(typeof(PromotionRank)))
            {
                int minimum = PromotionRules.MinimumMerit(rank);
                Assert.That(PromotionRules.RankFor(minimum), Is.EqualTo(rank));
                if (minimum > 0)
                    Assert.That(PromotionRules.RankFor(minimum - 1), Is.Not.EqualTo(rank));
            }
        }

        [Test]
        public void TryNextRank_ChainsUpToTop()
        {
            Assert.That(PromotionRules.TryNextRank(
                PromotionRank.Ashigaru, out PromotionRank next, out int needed), Is.True);
            Assert.That(next, Is.EqualTo(PromotionRank.Banto));
            Assert.That(needed, Is.EqualTo(5));

            Assert.That(PromotionRules.TryNextRank(
                PromotionRank.Kumigashira, out next, out needed), Is.True);
            Assert.That(next, Is.EqualTo(PromotionRank.Bushogashira));
            Assert.That(needed, Is.EqualTo(20));

            Assert.That(PromotionRules.TryNextRank(
                PromotionRank.Bushogashira, out next, out needed), Is.False);
            Assert.That(next, Is.EqualTo(PromotionRank.Bushogashira));
            Assert.That(needed, Is.EqualTo(20));
        }

        [Test]
        public void Phase1MaxMerit_ReachesTopRank()
        {
            // 10 + 5 + 5：三项任务全部结算恰达最高军阶。
            Assert.That(PromotionRules.RankFor(20), Is.EqualTo(PromotionRank.Bushogashira));
            Assert.That(PromotionRules.ChineseName(PromotionRank.Bushogashira), Is.EqualTo("武士头"));
        }
    }
}
