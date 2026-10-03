using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataExport;

public sealed class KataElevationViewportTests
{
    [Fact]
    public void FitCentresTheRangeInsideTheMargins()
    {
        var viewport = KataElevationViewport.Fit(new Interval1D(0, 10000), widthPx: 1040, marginPx: 20);

        Assert.Equal(0.1, viewport.Scale, 9);
        Assert.Equal(20.0, viewport.ToScreen(0), 9);
        Assert.Equal(1020.0, viewport.ToScreen(10000), 9);
    }

    [Fact]
    public void FitOnARangeAwayFromZeroStillCentresIt()
    {
        var viewport = KataElevationViewport.Fit(new Interval1D(4000, 6000), widthPx: 400, marginPx: 0);

        Assert.Equal(0.0, viewport.ToScreen(4000), 9);
        Assert.Equal(400.0, viewport.ToScreen(6000), 9);
    }

    [Theory]
    [InlineData(0.02)]
    [InlineData(0.5)]
    public void OneMillimetreIsAsLongAcrossAsDownAtEveryZoom(double scale)
    {
        var viewport = new KataElevationViewport(scale, 10, 20).ZoomAt(1.25, 300, 180, 0.001, 50.0).PanBy(7, -3);

        Assert.Equal(viewport.ToScreen(1000) - viewport.ToScreen(0), viewport.ToScreenY(1000) - viewport.ToScreenY(0), 9);
    }

    [Fact]
    public void ZoomKeepsThePointUnderTheCursorStillInBothDirections()
    {
        var viewport = new KataElevationViewport(0.02, 20, 100);
        double station = viewport.ToStation(300);
        double depth = (180 - viewport.OffsetYPx) / viewport.Scale;

        foreach (double factor in new[] { 2.0, 20.0, 0.5 })
        {
            var zoomed = viewport.ZoomAt(factor, anchorX: 300, anchorY: 180, minScale: 0.01, maxScale: 3.0);

            Assert.Equal(300.0, zoomed.ToScreen(station), 9);
            Assert.Equal(180.0, zoomed.ToScreenY(depth), 9);
        }
    }

    [Fact]
    public void ZoomRespectsTheLimitsAndStillKeepsTheAnchor()
    {
        var viewport = new KataElevationViewport(0.1, 20, 100);
        double station = viewport.ToStation(300);

        var capped = viewport.ZoomAt(100.0, anchorX: 300, anchorY: 150, minScale: 0.05, maxScale: 3.0);
        var floored = viewport.ZoomAt(0.001, anchorX: 300, anchorY: 150, minScale: 0.05, maxScale: 3.0);

        Assert.Equal(3.0, capped.Scale, 9);
        Assert.Equal(300.0, capped.ToScreen(station), 9);
        Assert.Equal(150.0, capped.ToScreenY((150 - viewport.OffsetYPx) / viewport.Scale), 9);
        Assert.Equal(0.05, floored.Scale, 9);
    }

    [Fact]
    public void ALimitAlreadyPassedStopsOnlyFurtherMovementThatWay()
    {
        var pastTheMinimum = new KataElevationViewport(0.01, 0);        // the window grew after zooming out fully

        Assert.Equal(0.01, pastTheMinimum.ZoomAt(0.8, 100, 0, minScale: 0.05, maxScale: 3.0).Scale, 9);
        Assert.Equal(0.012, pastTheMinimum.ZoomAt(1.2, 100, 0, minScale: 0.05, maxScale: 3.0).Scale, 9);
    }

    [Fact]
    public void ShrinkToHeightZoomsOutAShortSpanUntilItsLabelsFit()
    {
        // A 1.5 m cantilever framed at 0.48 px/mm: a 350 mm beam is 168 px, but only 88 px are left between the labels.
        var framed = new KataElevationViewport(0.48, -100, 0);
        double station = framed.ToStation(500);

        var shrunk = framed.ShrinkToHeight(bandDepthMm: 350, heightPx: 326, abovePx: 88, belowPx: 150, anchorX: 500);

        Assert.Equal(88.0 / 350.0, shrunk.Scale, 9);
        Assert.Equal(88.0, 350 * shrunk.Scale, 9);
        Assert.Equal(500.0, shrunk.ToScreen(station), 9);
        Assert.Equal(framed, framed.ShrinkToHeight(350, 1000, 88, 150, 500));             // already fits
        Assert.Equal(framed with { Scale = 0.2 }, (framed with { Scale = 0.2 }).ShrinkToHeight(350, 326, 88, 150, 500));     // 70 px: fits
    }

    [Fact]
    public void PanMovesBothDirections()
    {
        var viewport = new KataElevationViewport(0.1, 20, 100).PanBy(-30, 45);

        Assert.Equal(-10.0, viewport.OffsetPx, 9);
        Assert.Equal(145.0, viewport.OffsetYPx, 9);
    }

    [Fact]
    public void CentreBandPutsLabelsAndBandInTheMiddleOrKeepsTheTopLabelsVisible()
    {
        var viewport = new KataElevationViewport(0.02, 0, 0);   // 400 mm band = 8 px

        var fits = viewport.CentreBand(bandDepthMm: 400, heightPx: 400, abovePx: 80, belowPx: 150);
        var tooTall = viewport.ZoomAt(100, 0, 0, 0.01, 3.0).CentreBand(400, 400, 80, 150);

        Assert.Equal((400 - 238) / 2.0 + 80, fits.OffsetYPx, 9);
        Assert.Equal(80.0, tooTall.OffsetYPx, 9);
    }

    [Fact]
    public void EnsureVisiblePansTheLeastAmountAndKeepsTheVerticalPosition()
    {
        var viewport = new KataElevationViewport(0.1, 0, 77);

        Assert.Equal(viewport, viewport.EnsureVisible(new Interval1D(1000, 2000), widthPx: 500, marginPx: 10));
        Assert.Equal(490.0, viewport.EnsureVisible(new Interval1D(5000, 6000), 500, 10).ToScreen(6000), 9);
        Assert.Equal(10.0, viewport.PanBy(-300).EnsureVisible(new Interval1D(1000, 2000), 500, 10).ToScreen(1000), 9);
        Assert.Equal(77.0, viewport.EnsureVisible(new Interval1D(5000, 6000), 500, 10).OffsetYPx, 9);
    }

    [Fact]
    public void RangeWiderThanTheViewStaysPutWhileOnScreenAndIsCentredOtherwise()
    {
        var viewport = new KataElevationViewport(0.1, 0);

        Assert.Equal(viewport, viewport.EnsureVisible(new Interval1D(2000, 9000), widthPx: 500, marginPx: 10));
        Assert.Equal(250.0, viewport.EnsureVisible(new Interval1D(8000, 15000), 500, 10).ToScreen(11500), 9);
    }
}
