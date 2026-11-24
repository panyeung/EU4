using UnityEngine;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 已登记原创底图 <c>japan_geographic_basemap.png</c> 的运行时接入点。该图
    /// 1824×2048，范围经度 122.5–147.0、纬度 23.5–46.0，由 Natural Earth 公有领域
    /// 数据生成；登记与许可见 <c>ui设计规划.md</c> §10。底图与节点共用
    /// <see cref="JapanGeoProjection.FitMapSize"/> 拟合出的同一矩形，作为
    /// <c>MapContent</c> 的最底层，随路线与枢纽一起缩放、平移。
    /// </summary>
    public static class JapanMapBasemap
    {
        /// <summary>Resources 下的底图主资源路径（不含扩展名）。</summary>
        public const string ResourcePath = "Generated/japan_geographic_basemap";

        /// <summary>底图以 Multiple Sprite 导入，唯一子精灵名带 <c>_0</c> 后缀。</summary>
        public const string SpriteResourceName = ResourcePath + "_0";

        /// <summary>
        /// 加载底图精灵。导入失败时返回 null；调用方必须禁用空 Image，不能用
        /// 白色方块盖住地图。
        /// </summary>
        public static Sprite LoadSprite()
        {
            Sprite sprite = Resources.Load<Sprite>(SpriteResourceName);
            if (sprite != null)
                return sprite;

            Sprite[] sprites = Resources.LoadAll<Sprite>(ResourcePath);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }
    }
}
