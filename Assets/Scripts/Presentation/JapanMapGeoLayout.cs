using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;
using UnityEngine;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 把 WGS84 经纬度线性归一化到登记底图范围。x 东为正，y 北为正，两者都在
    /// 0..1；因此北在上、东在右。这是等距矩形式的线性归一化，不是等面积或真实比例
    /// 制图，只用于给地图节点提供经纬度偏好格位。
    /// <para>
    /// 范围与 <c>ui设计规划.md</c> §10 登记的
    /// <c>japan_geographic_basemap.png</c> 一致：经度 122.5–147.0、纬度 23.5–46.0，
    /// 登记像素 1824×2048。地图矩形按登记底图纵横比 1824/2048 拟合视口，节点与
    /// 底图共用这一个矩形，避免把经度、纬度分别拉伸到不同纵横比。
    /// </para>
    /// <para>
    /// 计划记载等距圆柱并按全境中心纬度修正经度尺度，理论纵横比为
    /// <see cref="EquirectangularAspect"/>（约 0.8941）；登记底图实际纵横比为
    /// <see cref="RegisteredBasemapAspect"/>（约 0.8906），两者相差约 0.4%。
    /// 底图生成器使用的精确投影没有在登记资料中写明，因此不声称像素级地理配准；
    /// 地图矩形采用登记 PNG 的实际纵横比，使底图按原比例铺满而不被拉伸。
    /// </para>
    /// </summary>
    public static class JapanGeoProjection
    {
        /// <summary>登记底图宽度（像素），见 <c>ui设计规划.md</c> §10。</summary>
        public const int RegisteredBasemapPixelWidth = 1824;

        /// <summary>登记底图高度（像素），见 <c>ui设计规划.md</c> §10。</summary>
        public const int RegisteredBasemapPixelHeight = 2048;

        /// <summary>全境中心纬度，用于等距圆柱的经度尺度修正。</summary>
        public static double CenterLatitude =>
            (JapanGeoAnchors.SouthLatitude + JapanGeoAnchors.NorthLatitude) * 0.5d;

        /// <summary>登记底图的宽高比，节点与底图共用该纵横比。</summary>
        public static float RegisteredBasemapAspect =>
            RegisteredBasemapPixelWidth / (float)RegisteredBasemapPixelHeight;

        /// <summary>
        /// 等距圆柱投影按中心纬度修正经度尺度后的理论宽高比。它与登记 PNG 的实际
        /// 宽高比相差约 0.4%，差异保留为有记录的近似限制。
        /// </summary>
        public static float EquirectangularAspect =>
            (float)((JapanGeoAnchors.EastLongitude - JapanGeoAnchors.WestLongitude) *
                    Math.Cos(CenterLatitude * Math.PI / 180d) /
                    (JapanGeoAnchors.NorthLatitude - JapanGeoAnchors.SouthLatitude));

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

        /// <summary>
        /// 在给定视口内按登记底图纵横比拟合地图矩形。两边都不能超过视口，较宽的一侧
        /// 留作背景；节点布局与底图图层都居中放到这个矩形里。
        /// </summary>
        public static Vector2 FitMapSize(Vector2 available)
        {
            float width = Mathf.Max(0f, available.x);
            float height = Mathf.Max(0f, available.y);
            if (width <= 0f || height <= 0f)
                return Vector2.zero;

            float aspect = RegisteredBasemapAspect;
            if (width / height > aspect)
                return new Vector2(height * aspect, height);
            return new Vector2(width, width / aspect);
        }
    }

    /// <summary>
    /// 用 GeoNames 区域锚点的经纬度决定每个旅行枢纽的落点，并复用
    /// <see cref="JapanMapLayout"/> 的网格容量、节点尺寸与最近空格规则，保证
    /// 37 个 74x28 点击目标在界内互不重叠。
    /// <para>
    /// 落点先在按登记底图纵横比拟合的地图矩形内计算，因此节点与底图共用同一
    /// 地理范围、方向和纵横比；较宽视口的左右留白不属于地图内容。
    /// </para>
    /// <para>
    /// 经纬度只提供偏好格位：真实锚点在通讯距离上很密（清洲、热田、那古野相距
    /// 数公里），若把点击目标放在精确投影点会完全重叠。最近空格分配只做可点击性
    /// 所需的微调，不改变枢纽 ID、路线、旅行天数或存档形状；投影本身的经纬度保序
    /// 由 <see cref="JapanGeoProjection"/> 单独保证。落点是离散格位，不是底图像素级
    /// 地理配准。
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

            Vector2 mapSize = JapanGeoProjection.FitMapSize(viewport);
            if (mapSize.x <= JapanMapLayout.MinSupportedSize ||
                mapSize.y <= JapanMapLayout.MinSupportedSize)
                return new JapanMapLayoutResult(new Vector2[count],
                    new Vector2(JapanMapLayout.NodeWidth, JapanMapLayout.NodeHeight));

            JapanMapGrid grid = JapanMapLayout.ResolveGrid(count, mapSize.x, mapSize.y);
            return new JapanMapLayoutResult(
                JapanMapLayout.PlaceNodes(grid, mapSize.x, mapSize.y, preferred,
                    HubIdOrder(hubs)),
                JapanMapLayout.ResolveNodeSize(grid, mapSize.x, mapSize.y));
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
