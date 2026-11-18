using System;

namespace SengokuChronicle.Presentation
{
    /// <summary>How the embedded industry panel is placed inside the household page band.</summary>
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
        /// <summary>False when the band forces text below the readable floor.</summary>
        public bool Readable { get; }
        /// <summary>True when the panel is shown smaller than its authored size.</summary>
        public bool ShrunkToFit { get; }
    }

    /// <summary>
    /// Plain geometry for embedding the SC-019 industry panel in the household page.
    /// The panel is authored by its own updater (760×560 today, 760×700 after the M05
    /// source change), so the family page must never hard-code the authored height: it
    /// scales the whole panel uniformly until it fits the band the page actually gives
    /// it. The panel's own Image is the parchment, so scaling the complete rect keeps
    /// every child inside the paper instead of letting the title and report spill out.
    /// </summary>
    public static class HouseholdIndustryFit
    {
        /// <summary>
        /// Below this scale the embedded copy is smaller than the cross-page standard the
        /// page already ships for the 560-high panel (0.816), so it counts as a geometry
        /// limitation rather than a layout choice.
        /// </summary>
        public const float ReadableScale = 0.8f;

        /// <summary>
        /// Fits <paramref name="authoredWidth"/>×<paramref name="authoredHeight"/> into the
        /// band without distortion or upscaling. Invalid input yields a 1:1 result so a
        /// broken scene can never produce NaN geometry.
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
        /// Scale for a panel taller than the viewport it is shown in. Width is the only
        /// limit, because the viewport cannot grow wider and the extra height stays
        /// reachable by vertical scrolling instead of shrinking the text. The result's
        /// height is the full scaled content height, which is expected to exceed the
        /// viewport height when this overload is used.
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
