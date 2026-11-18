using System;

namespace SengokuChronicle.Presentation
{
    /// <summary>嵌入式产业面板在家宅页面带内的放置结果。</summary>
    public readonly struct HouseholdIndustryFitResult
    {
        public HouseholdIndustryFitResult(float scale, float width, float height, bool readable,
            bool shrunkToFit)
        {
            Scale = scale;
            Width = width;
            Height = height;
            Readable = readable;
            ShrunkToFit = shrunkToFit;
        }

        public float Scale { get; }
        public float Width { get; }
        public float Height { get; }
        /// <summary>页面带迫使文字低于可读下限时为 false。</summary>
        public bool Readable { get; }
        /// <summary>面板按小于原始尺寸显示时为 true。</summary>
        public bool ShrunkToFit { get; }
    }

    /// <summary>
    /// 把 SC-019 产业面板嵌入家宅页面的纯几何。面板由它自己的更新器生成
    /// （当前 760×560，M05 源变更后 760×700），因此家族页面绝不能写死
    /// 原始高度：它按比例整体缩放面板，直到放进页面实际给出的带区。
    /// 面板自己的 Image 就是羊皮纸，缩放整个矩形能让所有子节点留在纸上，
    /// 而不是让标题与报告溢出。
    /// </summary>
    public static class HouseholdIndustryFit
    {
        /// <summary>
        /// 低于此缩放，嵌入副本就小于页面为 560 高面板已经交付的跨页标准
        /// （0.816），因此算几何限制，而不是布局选择。
        /// </summary>
        public const float ReadableScale = 0.8f;

        /// <summary>
        /// 把 <paramref name="authoredWidth"/>×<paramref name="authoredHeight"/>
        /// 放进带区，不拉伸也不放大。输入非法时返回 1:1，
        /// 坏场景绝不会产出 NaN 几何。
        /// </summary>
        public static HouseholdIndustryFitResult Calculate(float authoredWidth,
            float authoredHeight, float bandWidth, float bandHeight)
        {
            if (authoredWidth <= 0f || authoredHeight <= 0f || bandWidth <= 0f || bandHeight <= 0f)
                return new HouseholdIndustryFitResult(1f, Math.Max(0f, authoredWidth),
                    Math.Max(0f, authoredHeight), false, false);

            float scale = Math.Min(bandWidth / authoredWidth, bandHeight / authoredHeight);
            if (scale > 1f)
                scale = 1f;
            if (scale <= 0f)
                scale = 1f;

            float width = authoredWidth * scale;
            float height = authoredHeight * scale;
            bool shrunk = scale < 1f;
            return new HouseholdIndustryFitResult(scale, width, height, scale >= ReadableScale,
                shrunk);
        }

        /// <summary>
        /// 面板高于所在视口时使用的缩放。宽度是唯一限制，因为视口不能
        /// 变宽，多出的高度靠纵向滚动可达，而不是缩小文字。此重载返回的
        /// 高度是缩放后的完整内容高度，预期会超过视口高度。
        /// </summary>
        public static HouseholdIndustryFitResult CalculateReadableViewport(float authoredWidth,
            float authoredHeight, float viewportWidth)
        {
            if (authoredWidth <= 0f || authoredHeight <= 0f || viewportWidth <= 0f)
                return new HouseholdIndustryFitResult(1f, Math.Max(0f, authoredWidth),
                    Math.Max(0f, authoredHeight), false, false);

            float scale = Math.Min(1f, viewportWidth / authoredWidth);
            return new HouseholdIndustryFitResult(scale, authoredWidth * scale,
                authoredHeight * scale, scale >= ReadableScale, scale < 1f);
        }
    }
}
