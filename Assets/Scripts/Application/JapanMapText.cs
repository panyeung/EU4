using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Text rendering shared by the uGUI panel and tests. Every unsupported edge,
    /// day count or control claim is labelled as a design inference in the output.
    /// </summary>
    public static class JapanMapText
    {
        /// <summary>
        /// 地图锚点的应用内署名与许可链接。GeoNames 数据按原样提供且不保证精度，
        /// 因此文案明确写成区域近似代表点，不称城市中心或精确历史地点。
        /// </summary>
        public const string GeoAttributionNote =
            "区域锚点：GeoNames JP dump（WGS84 十进制度，CC BY 4.0）" +
            " https://www.geonames.org/export/ ；坐标为区域近似代表点，非城市中心或精确历史地点。";

        public static string BuildHeader(JapanMapExplorer explorer)
        {
            if (explorer == null)
                return "日本全国地图：未绑定";

            if (JapanTravelGraph.TryGetHubForLocation(explorer.CurrentLocation,
                    out JapanMapHub hub))
            {
                return "当前地点：" + hub.Name + "（" + ProvinceLabel(hub) + "）\n" +
                       "全国离散地图：地点节点与路线边，不是连续开放世界。";
            }

            return "当前地点：未定位（" + (explorer.CurrentLocation.Value ?? "(空)") + "）\n" +
                   "全国离散地图：地点节点与路线边，不是连续开放世界。";
        }

        public static string BuildCatalog(JapanMapExplorer explorer)
        {
            if (explorer == null)
                return string.Empty;

            var builder = new StringBuilder();
            IReadOnlyList<JapanMapCatalogRow> rows = explorer.BuildCatalogRows();
            for (int i = 0; i < rows.Count; i++)
            {
                JapanMapCatalogRow row = rows[i];
                if (row.Kind == JapanMapRowKind.Domain)
                {
                    builder.Append(row.IsExpanded ? "[-] " : "[+] ")
                        .Append(row.Name)
                        .Append("（")
                        .Append(row.ProvinceCount)
                        .Append("国，")
                        .Append(row.HubCount)
                        .Append("处旅行枢纽）");
                    if (i < rows.Count - 1)
                        builder.Append('\n');
                    continue;
                }

                builder.Append("    ").Append(row.Name).Append("（").Append(row.HubCount).Append("处）");
                if (row.HubCount == 0)
                {
                    builder.Append("：暂无旅行枢纽");
                }
                else
                {
                    builder.Append("：");
                    for (int h = 0; h < row.HubNames.Count; h++)
                    {
                        if (h > 0)
                            builder.Append("、");
                        builder.Append(row.HubNames[h]);
                    }
                }

                if (i < rows.Count - 1)
                    builder.Append('\n');
            }

            IReadOnlyList<JapanMapCatalogRow> regions = explorer.BuildTravelRegionRows();
            if (regions.Count > 0)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append("独立旅行区域（不并入68旧国）");
                for (int i = 0; i < regions.Count; i++)
                {
                    JapanMapCatalogRow region = regions[i];
                    builder.Append('\n').Append("◆ ").Append(region.Name)
                        .Append("（独立区域，").Append(region.HubCount).Append("处旅行枢纽");
                    if (region.HubCount > 0)
                    {
                        builder.Append("：");
                        for (int h = 0; h < region.HubNames.Count; h++)
                        {
                            if (h > 0)
                                builder.Append("、");
                            builder.Append(region.HubNames[h]);
                        }
                    }

                    builder.Append("）");
                }
            }

            return builder.ToString();
        }

        public static string BuildDetail(JapanMapExplorer explorer)
        {
            if (explorer == null)
                return string.Empty;

            JapanMapHubDetail detail = explorer.BuildSelectedHubDetail();
            if (detail == null)
            {
                if (!string.IsNullOrEmpty(explorer.SelectedProvinceId) &&
                    JapanMapCatalog.TryGetProvince(explorer.SelectedProvinceId,
                        out JapanProvince province))
                {
                    return "旧国：" + province.Name + "\n" +
                           "该旧国暂时没有旅行枢纽；旧国本身不是可访问地点。\n\n" +
                           GeoAttributionNote;
                }

                return "尚未选择旅行地点。\n\n" + GeoAttributionNote;
            }

            var builder = new StringBuilder();
            builder.Append("地点：").Append(detail.HubName).Append('\n');
            builder.Append("所属：").Append(detail.ProvinceName)
                .Append("（").Append(detail.DomainName).Append("）").Append('\n');
            builder.Append("身份：")
                .Append(detail.IsLegacyLocation
                    ? "既有旅行地点（M02 存档 ID：" + detail.HubId + "）"
                    : "旅行枢纽（稳定 ID：" + detail.HubId + "）")
                .Append('\n');
            builder.Append(LocationCapabilities.CanUseTownServices(new LocationId(detail.HubId))
                ? "该地点有城镇活动；仍按单人行程逐日推进。\n"
                : "可沿图中直达路线旅行；该枢纽没有城镇活动、居民或委托。\n");
            builder.Append("邻接：");
            if (detail.NeighborNames.Count == 0)
            {
                builder.Append("无");
            }
            else
            {
                for (int i = 0; i < detail.NeighborNames.Count; i++)
                {
                    if (i > 0)
                        builder.Append("、");
                    builder.Append(detail.NeighborNames[i]);
                }
            }

            builder.Append("\n旧国条目只作目录，不把省份本身当成可访问地点。");
            builder.Append("\n\n").Append(GeoAttributionNote);
            return builder.ToString();
        }

        public static string BuildRoutes(JapanMapExplorer explorer)
        {
            if (explorer == null)
                return string.Empty;

            JapanMapHubDetail detail = explorer.BuildSelectedHubDetail();
            if (detail == null)
                return "选择旅行地点后显示直达路线。";

            IReadOnlyList<JapanMapRouteCard> cards = explorer.BuildRouteCards();
            if (cards.Count == 0)
                return "当前筛选下没有直达路线。";

            var builder = new StringBuilder();
            builder.Append("从 ").Append(detail.HubName).Append(" 出发（")
                .Append(FilterLabel(explorer.RouteFilter)).Append("）\n");
            for (int i = 0; i < cards.Count; i++)
            {
                JapanMapRouteCard card = cards[i];
                JapanMapRoute route = card.Route;
                builder.Append(card.OtherHubName)
                    .Append("｜").Append(KindLabel(route.Kind))
                    .Append("｜").Append(route.Days).Append("天")
                    .Append("｜").Append(EvidenceLabel(route.Evidence));
                if (route.IsLegacyRoute)
                    builder.Append("（既有 M2 路线）");
                builder.Append('\n');
                builder.Append("  时期：").Append(route.PeriodNote).Append('\n');
                builder.Append("  控制：").Append(route.ControlNote).Append('\n');
                builder.Append("  说明：").Append(route.EvidenceNote).Append('\n');
                builder.Append("  来源：").Append(route.SourceNote);
                if (i < cards.Count - 1)
                    builder.Append('\n');
            }

            return builder.ToString();
        }

        public static string KindLabel(JapanRouteKind kind)
        {
            return kind == JapanRouteKind.Sea ? "海路" : "陆路";
        }

        public static string EvidenceLabel(JapanRouteEvidence evidence)
        {
            return evidence == JapanRouteEvidence.DocumentedCorridor
                ? "史料走廊支持"
                : "游戏设计推演";
        }

        public static string FilterLabel(JapanRouteKind? filter)
        {
            if (!filter.HasValue)
                return "全部路线";
            return filter.Value == JapanRouteKind.Sea ? "仅海路" : "仅陆路";
        }

        private static string ProvinceLabel(JapanMapHub hub)
        {
            if (!string.IsNullOrEmpty(hub.ProvinceId) &&
                JapanMapCatalog.TryGetProvince(hub.ProvinceId, out JapanProvince province))
                return province.Name;
            return JapanMapExplorer.DomainName(hub.DomainId) + "，独立区域";
        }
    }
}
