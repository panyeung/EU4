using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;
using UnityEngine;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 把 WGS84 经纬度线性归一化到日本地图投影范围。x 东为正，y 北为正，两者都在
    /// 0..1；因此北在上、东在右。这是等距矩形式的线性归一化，不是等面积或真实比例
    /// 制图，只用于给地图节点提供经纬度偏好格位。
    /// </summary>
    public static class JapanGeoProjection
    {
        /// <summary>
        /// 经纬度必须有限、在合法经纬度范围内，并落在
        /// <see cref="JapanGeoAnchors"/> 声明的投影范围内；否则抛出
        /// <see cref="ArgumentOutOfRangeException"/>，避免非法点被静默夹到地图边缘。
        /// </summary>
        public static Vector2 ToNormalized(double latitude, double longitude)
        {
            if (latitude < -90d || latitude > 90d || double.IsNaN(latitude))
                throw new ArgumentOutOfRangeException(nameof(latitude),
                    "纬度必须在 -90..90 之间：" + latitude);
            if (longitude < -180d || longitude > 180d || double.IsNaN(longitude))
                throw new ArgumentOutOfRangeException(nameof(longitude),
                    "经度必须在 -180..180 之间：" + longitude);
            if (!JapanGeoAnchors.IsInsideProjectionBounds(latitude, longitude))
                throw new ArgumentOutOfRangeException(nameof(latitude),
                    "坐标 (" + latitude + ", " + longitude + ") 超出自日本地图投影范围。");

            float x = (float)((longitude - JapanGeoAnchors.WestLongitude) /
                              (JapanGeoAnchors.EastLongitude - JapanGeoAnchors.WestLongitude));
            float y = (float)((latitude - JapanGeoAnchors.SouthLatitude) /
                              (JapanGeoAnchors.NorthLatitude - JapanGeoAnchors.SouthLatitude));
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// 用 GeoNames 区域锚点的经纬度决定每个旅行枢纽的落点，并复用
    /// <see cref="JapanMapLayout"/> 的网格容量、节点尺寸与最近空格规则，保证
    /// 37 个 74x28 点击目标在界内互不重叠。
    /// <para>
    /// 经纬度只提供偏好格位：真实锚点在通讯距离上很密（清洲、热田、那古野相距
    /// 数公里），若把点击目标放在精确投影点会完全重叠。最近空格分配只做可点击性
    /// 所需的微调，不改变枢纽 ID、路线、旅行天数或存档形状；投影本身的经纬度保序
    /// 由 <see cref="JapanGeoProjection"/> 单独保证。
    /// </para>
    /// <para>
    /// 分配顺序按枢纽 ID 排序，不按输入列表顺序，因此改变枢纽声明顺序不会改变
    /// 任一 ID 的落点。
    /// </para>
    /// </summary>
    public static class JapanMapGeoLayout
    {
        /// <summary>
        /// 计算地理布局。每个枢纽都必须有 <see cref="JapanGeoAnchors"/> 锚点；
        /// 缺少锚点或坐标非法会抛出异常，而不是回退到 <c>MapX</c>/<c>MapY</c>。
        /// </summary>
        public static JapanMapLayoutResult ComputeLayout(IReadOnlyList<JapanMapHub> hubs,
            Vector2 viewport)
        {
            if (hubs == null || hubs.Count == 0)
                return new JapanMapLayoutResult(Array.Empty<Vector2>(),
                    new Vector2(JapanMapLayout.NodeWidth, JapanMapLayout.NodeHeight));

            int count = hubs.Count;
            var preferred = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                if (!JapanGeoAnchors.TryGetAnchor(hubs[i].Id, out JapanGeoAnchor anchor))
                    throw new InvalidOperationException("旅行枢纽缺少 GeoNames 锚点：" + hubs[i].Id);

                Vector2 northUp = JapanGeoProjection.ToNormalized(
                    anchor.Latitude, anchor.Longitude);
                // 网格行号向南增长，投影 y 向北增长。
                preferred[i] = new Vector2(northUp.x, 1f - northUp.y);
            }

            float width = Mathf.Max(0f, viewport.x);
            float height = Mathf.Max(0f, viewport.y);
            if (width <= JapanMapLayout.MinSupportedSize ||
                height <= JapanMapLayout.MinSupportedSize)
                return new JapanMapLayoutResult(new Vector2[count],
                    new Vector2(JapanMapLayout.NodeWidth, JapanMapLayout.NodeHeight));

            JapanMapGrid grid = JapanMapLayout.ResolveGrid(count, width, height);
            return new JapanMapLayoutResult(
                JapanMapLayout.PlaceNodes(grid, width, height, preferred,
                    HubIdOrder(hubs)),
                JapanMapLayout.ResolveNodeSize(grid, width, height));
        }

        private static int[] HubIdOrder(IReadOnlyList<JapanMapHub> hubs)
        {
            int[] order = JapanMapLayout.IdentityOrder(hubs.Count);
            Array.Sort(order, (left, right) =>
                string.CompareOrdinal(hubs[left].Id, hubs[right].Id));
            return order;
        }
    }
}
