using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// 经纬度地理布局的回归：不同视口下 37 个点都有限、在界内且互不重叠；
    /// 落点由锚点决定而不是 MapX/MapY；按枢纽 ID 查找的结果不随输入顺序变化。
    /// </summary>
    public sealed class JapanMapGeoLayoutTests
    {
        [TestCase(864f, 324f)]
        [TestCase(648f, 432f)]
        [TestCase(1280f, 320f)]
        [TestCase(253f, 300f)]
        [TestCase(100f, 100f)]
        [TestCase(100f, 1000f)]
        [TestCase(1000f, 100f)]
        [TestCase(1600f, 40f)]
        public void Layout_AtRepresentativeSizes_StaysFiniteInBoundsAndDistinct(
            float width, float height)
        {
            JapanMapLayoutResult layout = JapanMapGeoLayout.ComputeLayout(
                JapanTravelGraph.Hubs, new Vector2(width, height));

            Assert.That(layout.Positions.Length,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(layout.NodeSize.x, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.y, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.x, Is.LessThanOrEqualTo(JapanMapLayout.NodeWidth));
            Assert.That(layout.NodeSize.y, Is.LessThanOrEqualTo(JapanMapLayout.NodeHeight));

            var distinct = new HashSet<Vector2>();
            for (int i = 0; i < layout.Positions.Length; i++)
            {
                Vector2 position = layout.Positions[i];
                Assert.That(float.IsNaN(position.x) || float.IsInfinity(position.x), Is.False,
                    "落点 X 不是有限数：" + JapanTravelGraph.Hubs[i].Id);
                Assert.That(float.IsNaN(position.y) || float.IsInfinity(position.y), Is.False,
                    "落点 Y 不是有限数：" + JapanTravelGraph.Hubs[i].Id);
                Assert.That(distinct.Add(position), Is.True,
                    "两个枢纽复用了同一落点：" + position);

                Assert.That(position.x - layout.NodeSize.x * 0.5f,
                    Is.GreaterThanOrEqualTo(-width * 0.5f - 0.01f));
                Assert.That(position.x + layout.NodeSize.x * 0.5f,
                    Is.LessThanOrEqualTo(width * 0.5f + 0.01f));
                Assert.That(position.y - layout.NodeSize.y * 0.5f,
                    Is.GreaterThanOrEqualTo(-height * 0.5f - 0.01f));
                Assert.That(position.y + layout.NodeSize.y * 0.5f,
                    Is.LessThanOrEqualTo(height * 0.5f + 0.01f));
            }
        }

        [TestCase(864f, 324f)]
        [TestCase(648f, 432f)]
        public void Layout_AtPlayableViewports_KeepsClickTargetsApart(float width, float height)
        {
            JapanMapLayoutResult layout = JapanMapGeoLayout.ComputeLayout(
                JapanTravelGraph.Hubs, new Vector2(width, height));
            Vector2 nodeSize = layout.NodeSize;

            for (int i = 0; i < layout.Positions.Length; i++)
            {
                for (int j = i + 1; j < layout.Positions.Length; j++)
                {
                    float deltaX = Mathf.Abs(layout.Positions[i].x - layout.Positions[j].x);
                    float deltaY = Mathf.Abs(layout.Positions[i].y - layout.Positions[j].y);
                    bool overlaps = deltaX < nodeSize.x - 0.01f &&
                                    deltaY < nodeSize.y - 0.01f;
                    Assert.That(overlaps, Is.False,
                        "点击目标重叠：" + JapanTravelGraph.Hubs[i].Id + " / " +
                        JapanTravelGraph.Hubs[j].Id);
                }
            }
        }

        [Test]
        public void Layout_IsDrivenByAnchorsNotByMapXAndMapY()
        {
            var viewport = new Vector2(864f, 324f);
            IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.Hubs;
            JapanMapLayoutResult byGeo = JapanMapGeoLayout.ComputeLayout(hubs, viewport);

            var flat = new List<JapanMapHub>(hubs.Count);
            for (int i = 0; i < hubs.Count; i++)
                flat.Add(FlatCopy(hubs[i]));

            JapanMapLayoutResult flatByGeo = JapanMapGeoLayout.ComputeLayout(flat, viewport);
            for (int i = 0; i < hubs.Count; i++)
                Assert.That(flatByGeo.Positions[i], Is.EqualTo(byGeo.Positions[i]),
                    "把 MapX/MapY 压到中心改变了地理落点：" + hubs[i].Id);

            // 只留相距最远的一对，MapX/MapY 完全相同；落点仍须按经纬度分开：
            // 西南的那霸在松前以西、以南。
            Assert.That(JapanTravelGraph.TryGetHub("ryukyu.naha", out JapanMapHub naha),
                Is.True);
            Assert.That(JapanTravelGraph.TryGetHub("ezo.matsumae", out JapanMapHub matsumae),
                Is.True);
            var pair = new List<JapanMapHub> { FlatCopy(naha), FlatCopy(matsumae) };
            JapanMapLayoutResult pairLayout =
                JapanMapGeoLayout.ComputeLayout(pair, viewport);
            Vector2 nahaPosition = pairLayout.Positions[0];
            Vector2 matsumaePosition = pairLayout.Positions[1];
            Assert.That(nahaPosition.x, Is.LessThan(matsumaePosition.x),
                "经纬度投影没有把那霸放在松前以西。");
            Assert.That(nahaPosition.y, Is.LessThan(matsumaePosition.y),
                "经纬度投影没有把那霸放在松前以南。");
        }

        private static JapanMapHub FlatCopy(JapanMapHub hub)
        {
            Assert.That(hub, Is.Not.Null);
            return new JapanMapHub(hub.Id, hub.Name, hub.ProvinceId, hub.DomainId,
                0.5f, 0.5f, hub.IsLegacyLocation);
        }

        [Test]
        public void Layout_PositionsByHubId_AreIndependentOfInputOrder()
        {
            const float width = 864f;
            const float height = 324f;
            IReadOnlyList<JapanMapHub> hubs = JapanTravelGraph.Hubs;
            var reversed = new List<JapanMapHub>(hubs.Count);
            for (int i = hubs.Count - 1; i >= 0; i--)
                reversed.Add(hubs[i]);

            JapanMapLayoutResult forward = JapanMapGeoLayout.ComputeLayout(
                hubs, new Vector2(width, height));
            JapanMapLayoutResult backward = JapanMapGeoLayout.ComputeLayout(
                reversed, new Vector2(width, height));

            var positionsById = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            for (int i = 0; i < hubs.Count; i++)
                positionsById.Add(hubs[i].Id, forward.Positions[i]);

            for (int i = 0; i < reversed.Count; i++)
                Assert.That(backward.Positions[i], Is.EqualTo(positionsById[reversed[i].Id]),
                    "输入顺序改变了枢纽落点：" + reversed[i].Id);
        }

        [TestCase(1f, 1f)]
        [TestCase(1f, 100f)]
        [TestCase(100f, 1f)]
        public void Layout_AtDeclaredMinimumSize_IsExplicitlyDegenerate(float width, float height)
        {
            JapanMapLayoutResult layout = JapanMapGeoLayout.ComputeLayout(
                JapanTravelGraph.Hubs, new Vector2(width, height));

            Assert.That(layout.Positions.Length, Is.EqualTo(JapanTravelGraph.Hubs.Count));
            for (int i = 0; i < layout.Positions.Length; i++)
                Assert.That(layout.Positions[i], Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Layout_ThrowsWhenAHubHasNoAnchor()
        {
            var hubs = new List<JapanMapHub>
            {
                new JapanMapHub("no.anchor", "无锚点", "province.owari", "tokaido",
                    0.5f, 0.5f, false),
            };

            Assert.Throws<InvalidOperationException>(
                () => JapanMapGeoLayout.ComputeLayout(hubs, new Vector2(864f, 324f)));
        }

        [Test]
        public void Layout_EmptyHubList_ReturnsEmptyResult()
        {
            JapanMapLayoutResult layout = JapanMapGeoLayout.ComputeLayout(
                Array.Empty<JapanMapHub>(), new Vector2(864f, 324f));

            Assert.That(layout.Positions, Is.Empty);
            Assert.That(layout.NodeSize, Is.EqualTo(
                new Vector2(JapanMapLayout.NodeWidth, JapanMapLayout.NodeHeight)));
        }
    }
}
