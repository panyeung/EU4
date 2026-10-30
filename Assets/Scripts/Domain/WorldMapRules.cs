using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 世界地图规则：仅三条无向直达路线，每条固定耗时 1 天。
    /// 清洲 ↔ 热田、清洲 ↔ 那古野；热田与那古野无直达路线。
    /// 不做多段寻路；路线与耗时是简化游戏规则，不代表历史事实。
    /// </summary>
    public static class WorldMapRules
    {
        private sealed class Route
        {
            public readonly LocationId A;
            public readonly LocationId B;
            public readonly int Days;

            public Route(LocationId a, LocationId b, int days)
            {
                A = a;
                B = b;
                Days = days;
            }
        }

        private static readonly Route[] Routes =
        {
            new Route(LocationId.Kiyosu, LocationId.Atsuta, 1),
            new Route(LocationId.Kiyosu, LocationId.Nagoya, 1),
        };

        /// <summary>
        /// 查询两地间是否存在无向直达路线并返回耗时（天）。
        /// 同地、无直达路线均返回 false。
        /// </summary>
        public static bool TryGetTravelDays(LocationId from, LocationId to, out int days)
        {
            days = 0;
            if (from == to)
                return false;

            for (int i = 0; i < Routes.Length; i++)
            {
                Route r = Routes[i];
                if ((from == r.A && to == r.B) || (from == r.B && to == r.A))
                {
                    days = r.Days;
                    return true;
                }
            }

            return false;
        }

        /// <summary>查询耗时；无路线返回 -1。</summary>
        public static int GetTravelDays(LocationId from, LocationId to)
        {
            return TryGetTravelDays(from, to, out int days) ? days : -1;
        }

        /// <summary>两地是否为同一点（不可向自身旅行）。</summary>
        public static bool IsSameLocation(LocationId a, LocationId b) => a == b;
    }
}