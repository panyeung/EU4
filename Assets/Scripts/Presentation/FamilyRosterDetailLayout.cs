using System;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 家族名册页的横向分割：可滚动的图谱占左侧，选中人物详情栏占右侧。
    /// 数值相对面板中心，调用方可以直接套用到居中的 RectTransform 上。
    /// 纯数学，不依赖 Unity 类型。
    /// </summary>
    public readonly struct FamilyRosterSplit
    {
        public FamilyRosterSplit(float scrollWidth, float scrollCenterX, float detailWidth,
            float detailCenterX)
        {
            ScrollWidth = scrollWidth;
            ScrollCenterX = scrollCenterX;
            DetailWidth = detailWidth;
            DetailCenterX = detailCenterX;
        }

        public float ScrollWidth { get; }
        public float ScrollCenterX { get; }
        public float DetailWidth { get; }
        public float DetailCenterX { get; }
    }

    /// <summary>
    /// 家族名册页并非在任何宽度下都能并排显示图谱与选中人物详情栏。
    /// 本辅助类是唯一决定分割位置的地方，因此页面样式器、面板与测试
    /// 对几何保持一致。面板太窄、放不下两张可读卡片加详情栏时，
    /// 不返回分割结果，面板改用上下堆叠的详情视图，而不是压缩卡片
    /// 或丢掉详情。
    /// </summary>
    public static class FamilyRosterDetailLayout
    {
        /// <summary>760 宽面板里，序列化的 700 宽滚动区域已让出的侧边距。</summary>
        public const float SideInset = 30f;
        /// <summary>图谱视口与详情栏之间的净空。</summary>
        public const float Gap = 14f;
        /// <summary>
        /// 详情栏的最大宽度。在 760 面板下，卡片内仍能保留
        /// <see cref="MinimumScrollWidth"/> 定义的两卡视口。
        /// </summary>
        public const float MaximumDetailWidth = 208f;
        /// <summary>仍能完整显示一行事实的最窄详情栏。</summary>
        public const float MinimumDetailWidth = 184f;
        /// <summary>
        /// 显示两张人物卡所需的总宽度，含布局自身的左右边距：
        /// 两侧外边距、两张卡以及两者之间的间隙。
        /// </summary>
        public const float MinimumScrollWidth = FamilyTreeLayout.Margin * 2f +
                                                FamilyTreeLayout.NodeWidth * 2f +
                                                FamilyTreeLayout.HorizontalGap;

        public static bool TryCalculate(float panelWidth, out FamilyRosterSplit split)
        {
            split = default;
            if (float.IsNaN(panelWidth) || float.IsInfinity(panelWidth) || panelWidth <= 0f)
                return false;

            float innerWidth = panelWidth - SideInset * 2f;
            if (innerWidth <= 0f)
                return false;

            float detail = Math.Min(MaximumDetailWidth, innerWidth * 0.36f);
            float scroll = innerWidth - detail - Gap;
            if (scroll < MinimumScrollWidth)
            {
                detail = innerWidth - Gap - MinimumScrollWidth;
                scroll = MinimumScrollWidth;
            }
            if (detail < MinimumDetailWidth)
                return false;

            float left = -panelWidth / 2f + SideInset;
            split = new FamilyRosterSplit(scroll, left + scroll / 2f, detail,
                left + scroll + Gap + detail / 2f);
            return true;
        }
    }
}
