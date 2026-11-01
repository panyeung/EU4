namespace SengokuChronicle.Domain
{
    /// <summary>Phase 2 起始军阶。游戏化简化的成长标签，不代表史实官职。</summary>
    public enum PromotionRank
    {
        Ashigaru,
        Banto,
        Kumigashira,
        Bushogashira
    }

    /// <summary>
    /// 按总功绩推导军阶的纯规则。阈值集中在不可变表中；
    /// 军阶永不写入存档，由任务阶段推导的 merit 派生，读档无法伪造。
    /// </summary>
    public static class PromotionRules
    {
        private struct RankTier
        {
            public RankTier(PromotionRank rank, int minimumMerit, string chineseName)
            {
                Rank = rank;
                MinimumMerit = minimumMerit;
                ChineseName = chineseName;
            }

            public readonly PromotionRank Rank;
            public readonly int MinimumMerit;
            public readonly string ChineseName;
        }

        // 升序阈值表：Phase 1 满功绩 20（10+5+5）恰达最高阶。
        private static readonly RankTier[] Tiers =
        {
            new RankTier(PromotionRank.Ashigaru, 0, "足轻"),
            new RankTier(PromotionRank.Banto, 5, "番头"),
            new RankTier(PromotionRank.Kumigashira, 10, "组头"),
            new RankTier(PromotionRank.Bushogashira, 20, "武士头"),
        };

        /// <summary>达到的最高军阶；负数按 0 处理。</summary>
        public static PromotionRank RankFor(int merit)
        {
            int effective = merit < 0 ? 0 : merit;
            PromotionRank result = Tiers[0].Rank;
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (effective >= Tiers[i].MinimumMerit)
                    result = Tiers[i].Rank;
            }
            return result;
        }

        /// <summary>军阶中文名。</summary>
        public static string ChineseName(PromotionRank rank)
        {
            for (int i = 0; i < Tiers.Length; i++)
                if (Tiers[i].Rank == rank)
                    return Tiers[i].ChineseName;
            return Tiers[0].ChineseName;
        }

        /// <summary>军阶的起始功绩。</summary>
        public static int MinimumMerit(PromotionRank rank)
        {
            for (int i = 0; i < Tiers.Length; i++)
                if (Tiers[i].Rank == rank)
                    return Tiers[i].MinimumMerit;
            return Tiers[0].MinimumMerit;
        }

        /// <summary>下一军阶与其起始功绩；顶级返回 false。</summary>
        public static bool TryNextRank(PromotionRank rank, out PromotionRank next, out int meritNeeded)
        {
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (Tiers[i].Rank == rank)
                {
                    if (i + 1 < Tiers.Length)
                    {
                        next = Tiers[i + 1].Rank;
                        meritNeeded = Tiers[i + 1].MinimumMerit;
                        return true;
                    }
                    next = rank;
                    meritNeeded = MinimumMerit(rank);
                    return false;
                }
            }
            next = Tiers[0].Rank;
            meritNeeded = Tiers[0].MinimumMerit;
            return false;
        }

        /// <summary>军阶总数（测试用于遍历一致性检查）。</summary>
        public static int TierCount => Tiers.Length;
    }
}
