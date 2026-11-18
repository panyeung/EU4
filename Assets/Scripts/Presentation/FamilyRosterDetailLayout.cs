using System;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Horizontal split of the family roster page: the scrollable graph keeps the
    /// left side and the selected-person detail column takes the right side.
    /// Values are relative to the panel centre so callers can apply them directly
    /// to centred RectTransforms. Pure math, no Unity types.
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
    /// The family roster page cannot show the graph and the selected-person detail
    /// column side by side at every width. This helper is the single place that
    /// decides the split, so the page styler, the panel and the tests agree on the
    /// geometry. When the panel is too narrow for two readable cards plus the column,
    /// no split is returned and the panel switches to its stacked detail view instead
    /// of shrinking a card or dropping the details.
    /// </summary>
    public static class FamilyRosterDetailLayout
    {
        /// <summary>Side margin already used by the serialized 700-wide scroll in a 760 panel.</summary>
        public const float SideInset = 30f;
        /// <summary>Clear space between the graph viewport and the detail column.</summary>
        public const float Gap = 14f;
        /// <summary>
        /// Widest detail column. At the 760 panel this still leaves the two-card
        /// viewport defined by <see cref="MinimumScrollWidth"/> inside the card.
        /// </summary>
        public const float MaximumDetailWidth = 208f;
        /// <summary>Narrowest detail column that keeps one full fact line readable.</summary>
        public const float MinimumDetailWidth = 184f;
        /// <summary>
        /// Width needed to show two person cards with the layout's own left and right
        /// margins: both outer margins, both cards and the gap between them.
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
