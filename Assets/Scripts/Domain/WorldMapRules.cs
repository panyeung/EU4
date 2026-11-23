using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 世界地图规则的无状态门面。路线、耗时与端点全部来自
    /// <see cref="JapanTravelGraph"/>；本类只保留三条仅供存档恢复的退役在途边，
    /// 不把它们作为可规划路线。旅行仍是图中的单条无向直达边，不做多段寻路；
    /// 新增枢纽自动沿用同一规则。
    /// </summary>
    public static class WorldMapRules
    {
        /// <summary>
        /// 查询两地间是否存在无向直达路线并返回耗时（天）。
        /// 同地、无直达路线均返回 false。
        /// </summary>
        public static bool TryGetTravelDays(LocationId from, LocationId to, out int days)
        {
            days = 0;
            if (from == to)
                return false;

            if (!JapanTravelGraph.TryGetHubForLocation(from, out JapanMapHub fromHub) ||
                !JapanTravelGraph.TryGetHubForLocation(to, out JapanMapHub toHub))
                return false;

            if (!JapanTravelGraph.TryGetRoute(fromHub.Id, toHub.Id, out JapanMapRoute route))
                return false;

            days = route.Days;
            return true;
        }

        /// <summary>
        /// 存档恢复专用：接受图中当前路线，也接受三条已被港口接驳替换、
        /// 但仍可能出现在开发期 v17/rules 8 在途行动中的旧直达边。
        /// 该查询不得用于旅行规划、预览或地图呈现；那些入口只调用
        /// <see cref="TryGetTravelDays"/>，因此旧边不会重新变成可选路线。
        /// </summary>
        public static bool TryGetInFlightTravelDays(LocationId from, LocationId to,
            out int days, bool allowRetiredRoutes = true)
        {
            if (TryGetTravelDays(from, to, out days))
                return true;

            days = 0;
            if (!allowRetiredRoutes)
                return false;
            if (IsRetiredPair(from, to, "sanuki.takamatsu", "iyo.matsuyama"))
            {
                days = 2;
                return true;
            }
            if (IsRetiredPair(from, to, "nagato.yamaguchi", "chikuzen.hakata"))
            {
                days = 2;
                return true;
            }
            if (IsRetiredPair(from, to, "satsuma.kagoshima", "ryukyu.shuri"))
            {
                days = 8;
                return true;
            }
            return false;
        }

        private static bool IsRetiredPair(LocationId from, LocationId to, string first,
            string second) =>
            (string.Equals(from.Value, first, StringComparison.Ordinal) &&
             string.Equals(to.Value, second, StringComparison.Ordinal)) ||
            (string.Equals(from.Value, second, StringComparison.Ordinal) &&
             string.Equals(to.Value, first, StringComparison.Ordinal));

        /// <summary>查询耗时；无路线返回 -1。</summary>
        public static int GetTravelDays(LocationId from, LocationId to)
        {
            return TryGetTravelDays(from, to, out int days) ? days : -1;
        }

        /// <summary>两地是否为同一点（不可向自身旅行）。</summary>
        public static bool IsSameLocation(LocationId a, LocationId b) => a == b;
    }
}