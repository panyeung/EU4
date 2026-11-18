using NUnit.Framework;
using SengokuChronicle.Presentation;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class HouseholdIndustryFitTests
    {
        // Free central band on the 1280x720 reference canvas, between the head rail,
        // pending-decision column, rail top and page margin.
        private const float BandWidth = 620f;
        private const float BandHeight = 612f;
        private const float AuthoredWidth = 760f;

        private static Rect Band()
        {
            float left = ReferenceGalleryNavigator.HouseholdHeadRailPosition.x +
                         ReferenceGalleryNavigator.HouseholdHeadRailSize.x * 0.5f +
                         ReferenceGalleryNavigator.HouseholdIndustryBandGap;
            float right = ReferenceGalleryNavigator.PendingRailPosition.x -
                          ReferenceGalleryNavigator.PendingRailSize.x * 0.5f -
                          ReferenceGalleryNavigator.HouseholdIndustryBandGap;
            float top = ReferenceGalleryNavigator.HouseholdHeadRailPosition.y +
                        ReferenceGalleryNavigator.HouseholdHeadRailSize.y * 0.5f;
            float bottom = -360f + ReferenceGalleryNavigator.HouseholdIndustryPageMargin;
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        [Test]
        public void Calculate_FreeBandUsesItsWidthAsFitLimit()
        {
            Rect band = Band();
            Assert.That(band.width, Is.EqualTo(BandWidth).Within(0.01f));
            Assert.That(band.height, Is.EqualTo(BandHeight).Within(0.01f));

            HouseholdIndustryFitResult fit = HouseholdIndustryFit.Calculate(AuthoredWidth, 700f,
                band.width, band.height);

            Assert.That(fit.Scale, Is.EqualTo(BandWidth / AuthoredWidth).Within(0.001f));
            Assert.That(fit.Readable, Is.True);
            Assert.That(fit.Width, Is.EqualTo(band.width).Within(0.01f));
            Assert.That(fit.Height, Is.LessThanOrEqualTo(band.height));
        }

        [Test]
        public void Calculate_ShorterAuthoredPanelStillFitsTheSameBand()
        {
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.Calculate(AuthoredWidth, 560f,
                BandWidth, BandHeight);

            Assert.That(fit.Scale, Is.EqualTo(BandWidth / AuthoredWidth).Within(0.001f));
            Assert.That(fit.Height, Is.EqualTo(560f * fit.Scale).Within(0.01f));
            Assert.That(fit.Readable, Is.True);
        }

        [Test]
        public void Calculate_ANarrowBandIsReportedAsUnreadableInsteadOfSilentlyClipped()
        {
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.Calculate(AuthoredWidth, 700f,
                400f, 300f);

            Assert.That(fit.Scale, Is.EqualTo(300f / 700f).Within(0.001f),
                "the shorter band dimension bounds the uniform scale");
            Assert.That(fit.Readable, Is.False);
            Assert.That(fit.Height, Is.EqualTo(300f).Within(0.01f));
            Assert.That(fit.Width, Is.LessThanOrEqualTo(400f));
        }

        [Test]
        public void Calculate_NeverUpscalesABandThatIsAlreadyLarger()
        {
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.Calculate(400f, 300f,
                BandWidth, BandHeight);

            Assert.That(fit.Scale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(fit.ShrunkToFit, Is.False);
            Assert.That(fit.Width, Is.EqualTo(400f).Within(0.01f));
        }

        [Test]
        public void Calculate_InvalidInputStaysFinite()
        {
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.Calculate(0f, 0f, 0f, 0f);

            Assert.That(fit.Scale, Is.EqualTo(1f).Within(0.001f));
            Assert.That(float.IsNaN(fit.Width), Is.False);
            Assert.That(float.IsNaN(fit.Height), Is.False);
            Assert.That(float.IsInfinity(fit.Width), Is.False);
            Assert.That(float.IsInfinity(fit.Height), Is.False);
        }

        [Test]
        public void CalculateReadableViewport_KeepsReadableWidthAndFullScrollableHeight()
        {
            HouseholdIndustryFitResult fit = HouseholdIndustryFit.CalculateReadableViewport(
                760f, 700f, 620f);

            Assert.That(fit.Scale, Is.EqualTo(620f / 760f).Within(0.001f));
            Assert.That(fit.Width, Is.EqualTo(620f).Within(0.01f));
            Assert.That(fit.Height, Is.EqualTo(571.0526f).Within(0.01f));
            Assert.That(fit.Readable, Is.True);
            Assert.That(fit.ShrunkToFit, Is.True);
        }
    }
}
