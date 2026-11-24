using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 一个旅行枢纽的 GeoNames 区域锚点。坐标是覆盖全日本地图的近似代表点，
    /// 不是城市中心、遗址中心、港口中心，也不是精确历史地点。存档身份仍是
    /// 枢纽 ID；geonameId 只供逐点追溯来源。
    /// </summary>
    public sealed class JapanGeoAnchor
    {
        public JapanGeoAnchor(string hubId, long geoNameId, double latitude, double longitude)
        {
            HubId = hubId;
            GeoNameId = geoNameId;
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>与 <see cref="JapanMapHub.Id"/> 相同的稳定枢纽 ID。</summary>
        public string HubId { get; }

        /// <summary>GeoNames 记录 ID，仅用于追溯单条来源记录。</summary>
        public long GeoNameId { get; }

        /// <summary>WGS84 十进制度纬度，北为正。</summary>
        public double Latitude { get; }

        /// <summary>WGS84 十进制度经度，东为正。</summary>
        public double Longitude { get; }
    }

    /// <summary>
    /// 37 个旅行枢纽的 GeoNames JP dump 区域锚点，按枢纽 ID 索引。
    /// <para>
    /// 数据口径：坐标是区域近似代表锚点；不称城市中心、遗址中心、港口中心或精确
    /// 历史地点。来源为 GeoNames JP dump（WGS84 十进制度），许可 CC BY 4.0，
    /// 按原样提供且不保证精度；geonameId 可逐点追溯。登记见
    /// <c>docs/maps/geonames-attribution.md</c>。
    /// </para>
    /// <para>
    /// 本表与 <see cref="JapanTravelGraph"/> 分开存放，因此改变枢纽的声明顺序不改变
    /// 按 ID 查找的坐标；<see cref="JapanMapHub.MapX"/>/<see cref="JapanMapHub.MapY"/>
    /// 仍是既有布局的兼容坐标，不被当成经纬度。
    /// </para>
    /// </summary>
    public static class JapanGeoAnchors
    {
        /// <summary>地图投影的南界（纬度），与登记底图范围一致。</summary>
        public const double SouthLatitude = 23.5d;

        /// <summary>地图投影的北界（纬度），与登记底图范围一致。</summary>
        public const double NorthLatitude = 46.0d;

        /// <summary>地图投影的西界（经度），与登记底图范围一致。</summary>
        public const double WestLongitude = 122.5d;

        /// <summary>地图投影的东界（经度），与登记底图范围一致。</summary>
        public const double EastLongitude = 147.0d;

        /// <summary>旅行枢纽数量，与 <see cref="JapanTravelGraph.Hubs"/> 一致。</summary>
        public const int AnchorCount = 37;

        private static readonly JapanGeoAnchor[] AnchorList =
        {
            new JapanGeoAnchor("owari.kiyosu", 1859210, 35.21667, 136.83333),
            new JapanGeoAnchor("owari.atsuta", 1864911, 35.12901, 136.89993),
            new JapanGeoAnchor("owari.nagoya", 1856053, 35.18147, 136.90641),
            new JapanGeoAnchor("yamashiro.kyoto", 1857910, 35.02107, 135.75385),
            new JapanGeoAnchor("settsu.osaka", 1853909, 34.69379, 135.50107),
            new JapanGeoAnchor("ise.yamada", 1861450, 34.48333, 136.7),
            new JapanGeoAnchor("mikawa.okazaki", 1854376, 34.95, 137.16667),
            new JapanGeoAnchor("suruga.sunpu", 1851717, 34.98333, 138.38333),
            new JapanGeoAnchor("kai.kofu", 1859100, 35.66667, 138.56667),
            new JapanGeoAnchor("sagami.odawara", 1854747, 35.25556, 139.15972),
            new JapanGeoAnchor("musashi.edo", 1850147, 35.6895, 139.69171),
            new JapanGeoAnchor("mino.inokuchi", 1863641, 35.42291, 136.76039),
            new JapanGeoAnchor("shinano.komoro", 1858858, 36.31667, 138.43333),
            new JapanGeoAnchor("mutsu.aizu", 6822148, 37.49142, 139.94546),
            new JapanGeoAnchor("mutsu.tosaminato", 12908995, 41.02262, 140.32009),
            new JapanGeoAnchor("dewa.yamagata", 2110556, 38.23333, 140.36667),
            new JapanGeoAnchor("echizen.ichijodani", 1863985, 36.06443, 136.22257),
            new JapanGeoAnchor("kaga.kanazawa", 1860243, 36.6, 136.61667),
            new JapanGeoAnchor("etchu.toyama", 1849870, 36.55146, 137.29872),
            new JapanGeoAnchor("echigo.kasugayama", 11811984, 37.14434, 138.22968),
            new JapanGeoAnchor("harima.himeji", 1862627, 34.81667, 134.7),
            new JapanGeoAnchor("bizen.okayama", 1854380, 34.71251, 133.92329),
            new JapanGeoAnchor("aki.koriyama", 11612331, 34.67416, 132.68188),
            new JapanGeoAnchor("nagato.yamaguchi", 1848689, 34.18333, 131.46667),
            new JapanGeoAnchor("nagato.akama", 1852225, 33.95548, 130.93713),
            new JapanGeoAnchor("izumo.izumo", 1861084, 35.36667, 132.76667),
            new JapanGeoAnchor("kii.wakayama", 1926004, 34.23333, 135.16667),
            new JapanGeoAnchor("sanuki.takamatsu", 1851100, 34.33333, 134.05),
            new JapanGeoAnchor("iyo.matsuyama", 1926099, 33.83916, 132.76574),
            new JapanGeoAnchor("iyo.masaki", 1926101, 33.78757, 132.71124),
            new JapanGeoAnchor("tosa.kochi", 1859146, 33.55, 133.53333),
            new JapanGeoAnchor("chikuzen.hakata", 1863967, 33.6, 130.41667),
            new JapanGeoAnchor("bungo.funai", 1854487, 33.23333, 131.6),
            new JapanGeoAnchor("satsuma.kagoshima", 1860827, 31.56667, 130.55),
            new JapanGeoAnchor("ezo.matsumae", 2129282, 41.42732, 140.10298),
            new JapanGeoAnchor("ryukyu.naha", 1856035, 26.213, 127.67851),
            new JapanGeoAnchor("ryukyu.shuri", 1861368, 26.22478, 127.73172),
        };

        private static readonly Dictionary<string, JapanGeoAnchor> AnchorsById = BuildAnchorIndex();

        public static IReadOnlyList<JapanGeoAnchor> Anchors => AnchorList;

        /// <summary>按稳定枢纽 ID 查找锚点。声明顺序不影响结果。</summary>
        public static bool TryGetAnchor(string hubId, out JapanGeoAnchor anchor)
        {
            if (string.IsNullOrEmpty(hubId))
            {
                anchor = null;
                return false;
            }

            return AnchorsById.TryGetValue(hubId, out anchor);
        }

        /// <summary>坐标是否有限且落在声明的地图投影范围内。</summary>
        public static bool IsInsideProjectionBounds(double latitude, double longitude) =>
            IsFinite(latitude) && IsFinite(longitude) &&
            latitude >= SouthLatitude && latitude <= NorthLatitude &&
            longitude >= WestLongitude && longitude <= EastLongitude;

        /// <summary>
        /// 返回锚点表的结构问题；空列表是 EditMode 测试与
        /// <see cref="JapanTravelGraph.Validate"/> 使用的契约。
        /// </summary>
        public static IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            if (AnchorList.Length != AnchorCount)
                issues.Add("锚点数量应为 " + AnchorCount + "，实际 " + AnchorList.Length + "。");

            var hubIds = new HashSet<string>(StringComparer.Ordinal);
            var geoNameIds = new HashSet<long>();
            for (int i = 0; i < AnchorList.Length; i++)
            {
                JapanGeoAnchor anchor = AnchorList[i];
                if (string.IsNullOrEmpty(anchor.HubId))
                {
                    issues.Add("锚点缺少枢纽 ID。");
                    continue;
                }

                if (!hubIds.Add(anchor.HubId))
                    issues.Add("锚点枢纽 ID 重复：" + anchor.HubId);
                if (anchor.GeoNameId <= 0)
                    issues.Add("锚点缺少 GeoNames ID：" + anchor.HubId);
                else if (!geoNameIds.Add(anchor.GeoNameId))
                    issues.Add("锚点 GeoNames ID 重复：" + anchor.HubId);

                if (!IsFinite(anchor.Latitude) || !IsFinite(anchor.Longitude))
                {
                    issues.Add("锚点坐标不是有限数：" + anchor.HubId);
                }
                else
                {
                    if (anchor.Latitude < -90d || anchor.Latitude > 90d)
                        issues.Add("锚点纬度越界：" + anchor.HubId);
                    if (anchor.Longitude < -180d || anchor.Longitude > 180d)
                        issues.Add("锚点经度越界：" + anchor.HubId);
                    if (!IsInsideProjectionBounds(anchor.Latitude, anchor.Longitude))
                        issues.Add("锚点超出自日本地图投影范围：" + anchor.HubId);
                }

                if (!JapanTravelGraph.TryGetHub(anchor.HubId, out _))
                    issues.Add("锚点引用了未知旅行枢纽：" + anchor.HubId);
            }

            IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.Hubs;
            for (int i = 0; i < hubs.Count; i++)
                if (!TryGetAnchor(hubs[i].Id, out _))
                    issues.Add("旅行枢纽缺少 GeoNames 锚点：" + hubs[i].Id);

            return issues;
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static Dictionary<string, JapanGeoAnchor> BuildAnchorIndex()
        {
            var index = new Dictionary<string, JapanGeoAnchor>(StringComparer.Ordinal);
            for (int i = 0; i < AnchorList.Length; i++)
                index.Add(AnchorList[i].HubId, AnchorList[i]);
            return index;
        }
    }
}
