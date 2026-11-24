using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// GeoNames 区域锚点的数据契约：37 个稳定枢纽 ID 各有一条唯一、合法、
    /// 可追溯的锚点；锚点按 ID 查找，与枢纽声明顺序无关；投影保持北在上、
    /// 东在右，并拒绝非法经纬度。坐标只作区域近似代表点。
    /// </summary>
    public sealed class JapanGeoAnchorTests
    {
        [Test]
        public void Anchors_CoverEveryHubExactlyOnceAndPassValidation()
        {
            IReadOnlyList<string> issues = JapanGeoAnchors.Validate();
            Assert.That(issues, Is.Empty, string.Join("; ", issues));
            Assert.That(JapanGeoAnchors.Anchors.Count,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(JapanGeoAnchors.Anchors.Count, Is.EqualTo(37));

            var hubIds = new HashSet<string>(StringComparer.Ordinal);
            var geoNameIds = new HashSet<long>();
            for (int i = 0; i < JapanGeoAnchors.Anchors.Count; i++)
            {
                JapanGeoAnchor anchor = JapanGeoAnchors.Anchors[i];
                Assert.That(hubIds.Add(anchor.HubId), Is.True, "锚点枢纽 ID 重复：" + anchor.HubId);
                Assert.That(geoNameIds.Add(anchor.GeoNameId), Is.True,
                    "锚点 GeoNames ID 重复：" + anchor.HubId);
                Assert.That(anchor.GeoNameId, Is.GreaterThan(0L), anchor.HubId);

                Assert.That(JapanTravelGraph.TryGetHub(anchor.HubId, out JapanMapHub hub), Is.True,
                    "锚点引用了未知枢纽：" + anchor.HubId);
                Assert.That(hub.Id, Is.EqualTo(anchor.HubId));

                Assert.That(double.IsNaN(anchor.Latitude) || double.IsInfinity(anchor.Latitude),
                    Is.False, anchor.HubId);
                Assert.That(double.IsNaN(anchor.Longitude) || double.IsInfinity(anchor.Longitude),
                    Is.False, anchor.HubId);
                Assert.That(anchor.Latitude, Is.InRange(-90d, 90d), anchor.HubId);
                Assert.That(anchor.Longitude, Is.InRange(-180d, 180d), anchor.HubId);
                Assert.That(JapanGeoAnchors.IsInsideProjectionBounds(
                    anchor.Latitude, anchor.Longitude), Is.True,
                    "锚点超出自日本地图投影范围：" + anchor.HubId);
            }

            for (int i = 0; i < JapanTravelGraph.Hubs.Count; i++)
            {
                string hubId = JapanTravelGraph.Hubs[i].Id;
                Assert.That(JapanGeoAnchors.TryGetAnchor(hubId, out JapanGeoAnchor anchor), Is.True,
                    "旅行枢纽缺少 GeoNames 锚点：" + hubId);
                Assert.That(anchor.HubId, Is.EqualTo(hubId));
            }
        }

        [TestCase("owari.kiyosu", 1859210L, 35.21667, 136.83333)]
        [TestCase("musashi.edo", 1850147L, 35.6895, 139.69171)]
        [TestCase("mutsu.tosaminato", 12908995L, 41.02262, 140.32009)]
        [TestCase("ezo.matsumae", 2129282L, 41.42732, 140.10298)]
        [TestCase("ryukyu.naha", 1856035L, 26.213, 127.67851)]
        [TestCase("ryukyu.shuri", 1861368L, 26.22478, 127.73172)]
        public void Anchors_KeepTraceableGeoNameRecords(string hubId, long geoNameId,
            double latitude, double longitude)
        {
            Assert.That(JapanGeoAnchors.TryGetAnchor(hubId, out JapanGeoAnchor anchor), Is.True, hubId);
            Assert.That(anchor.HubId, Is.EqualTo(hubId));
            Assert.That(anchor.GeoNameId, Is.EqualTo(geoNameId));
            Assert.That(anchor.Latitude, Is.EqualTo(latitude).Within(1e-9));
            Assert.That(anchor.Longitude, Is.EqualTo(longitude).Within(1e-9));
        }

        [Test]
        public void AnchorLookup_IsIndependentOfHubDeclarationOrder()
        {
            IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.Hubs;
            var expected = new Dictionary<string, JapanGeoAnchor>(StringComparer.Ordinal);
            for (int i = 0; i < hubs.Count; i++)
            {
                Assert.That(JapanGeoAnchors.TryGetAnchor(hubs[i].Id, out JapanGeoAnchor anchor),
                    Is.True, hubs[i].Id);
                expected.Add(hubs[i].Id, anchor);
            }

            for (int i = hubs.Count - 1; i >= 0; i--)
            {
                string hubId = hubs[i].Id;
                Assert.That(JapanGeoAnchors.TryGetAnchor(hubId, out JapanGeoAnchor anchor), Is.True,
                    hubId);
                Assert.That(anchor, Is.SameAs(expected[hubId]));
                Assert.That(anchor.Latitude, Is.EqualTo(expected[hubId].Latitude));
                Assert.That(anchor.Longitude, Is.EqualTo(expected[hubId].Longitude));
                Assert.That(anchor.GeoNameId, Is.EqualTo(expected[hubId].GeoNameId));
            }
        }

        [Test]
        public void Projection_KeepsNorthUpAndEastRightForEveryPair()
        {
            IReadOnlyList<JapanGeoAnchor> anchors = JapanGeoAnchors.Anchors;
            var latitudes = new HashSet<double>();
            var longitudes = new HashSet<double>();
            for (int i = 0; i < anchors.Count; i++)
            {
                latitudes.Add(anchors[i].Latitude);
                longitudes.Add(anchors[i].Longitude);
                Vector2 normalized = JapanGeoProjection.ToNormalized(
                    anchors[i].Latitude, anchors[i].Longitude);
                Assert.That(normalized.x, Is.GreaterThan(0f).And.LessThan(1f),
                    anchors[i].HubId);
                Assert.That(normalized.y, Is.GreaterThan(0f).And.LessThan(1f),
                    anchors[i].HubId);
            }

            Assert.That(latitudes.Count, Is.EqualTo(anchors.Count),
                "严格南北保序的比较要求每个锚点的纬度互不相同。");
            Assert.That(longitudes.Count, Is.EqualTo(anchors.Count),
                "严格东西保序的比较要求每个锚点的经度互不相同。");

            for (int i = 0; i < anchors.Count; i++)
            {
                for (int j = i + 1; j < anchors.Count; j++)
                {
                    JapanGeoAnchor a = anchors[i];
                    JapanGeoAnchor b = anchors[j];
                    Vector2 pa = JapanGeoProjection.ToNormalized(a.Latitude, a.Longitude);
                    Vector2 pb = JapanGeoProjection.ToNormalized(b.Latitude, b.Longitude);

                    if (a.Latitude > b.Latitude)
                        Assert.That(pa.y, Is.GreaterThan(pb.y),
                            "北向投影错误：" + a.HubId + " 应在 " + b.HubId + " 北侧。");
                    else
                        Assert.That(pa.y, Is.LessThan(pb.y),
                            "北向投影错误：" + a.HubId + " 应在 " + b.HubId + " 南侧。");

                    if (a.Longitude > b.Longitude)
                        Assert.That(pa.x, Is.GreaterThan(pb.x),
                            "东向投影错误：" + a.HubId + " 应在 " + b.HubId + " 东侧。");
                    else
                        Assert.That(pa.x, Is.LessThan(pb.x),
                            "东向投影错误：" + a.HubId + " 应在 " + b.HubId + " 西侧。");
                }
            }
        }

        [TestCase(double.NaN, 136d)]
        [TestCase(double.PositiveInfinity, 136d)]
        [TestCase(35d, double.NaN)]
        [TestCase(35d, double.NegativeInfinity)]
        [TestCase(91d, 136d)]
        [TestCase(-91d, 136d)]
        [TestCase(35d, 181d)]
        [TestCase(35d, -181d)]
        [TestCase(23d, 136d)]
        [TestCase(47d, 136d)]
        [TestCase(35d, 121d)]
        [TestCase(35d, 147d)]
        public void Projection_RejectsIllegalCoordinates(double latitude, double longitude)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => JapanGeoProjection.ToNormalized(latitude, longitude));
        }

        [Test]
        public void DetailText_CarriesGeoNameAttributionAndApproximationCaveat()
        {
            var explorer = new JapanMapExplorer(LocationId.Kiyosu);
            Assert.That(explorer.SelectHub("yamashiro.kyoto"), Is.True);
            string detail = JapanMapText.BuildDetail(explorer);

            Assert.That(detail, Does.Contain("GeoNames"));
            Assert.That(detail, Does.Contain("CC BY 4.0"));
            Assert.That(detail, Does.Contain("https://www.geonames.org/export/"));
            Assert.That(detail, Does.Contain("区域近似代表点"));
            Assert.That(detail, Does.Contain("非城市中心或精确历史地点"));
        }
    }
}
