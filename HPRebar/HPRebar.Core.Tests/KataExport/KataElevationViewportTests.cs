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

    [Fact]
    public void ZoomKeepsTheStationUnderTheCursorAndRespectsTheLimits()
    {
        var viewport = new KataElevationViewport(0.1, 20);
        double station = viewport.ToStation(300);

        var zoomed = viewport.ZoomAt(2.0, anchorPx: 300, minScale: 0.05, maxScale: 1.0);
        var capped = viewport.ZoomAt(100.0, anchorPx: 300, minScale: 0.05, maxScale: 1.0);

        Assert.Equal(0.2, zoomed.Scale, 9);
        Assert.Equal(300.0, zoomed.ToScreen(station), 9);
        Assert.Equal(1.0, capped.Scale, 9);
        Assert.Equal(300.0, capped.ToScreen(station), 9);
    }

    [Fact]
    public void EnsureVisiblePansTheLeastAmount()
    {
        var viewport = new KataElevationViewport(0.1, 0);

        Assert.Equal(viewport, viewport.EnsureVisible(new Interval1D(1000, 2000), widthPx: 500, marginPx: 10));
        Assert.Equal(490.0, viewport.EnsureVisible(new Interval1D(5000, 6000), 500, 10).ToScreen(6000), 9);
        Assert.Equal(10.0, viewport.PanBy(-300).EnsureVisible(new Interval1D(1000, 2000), 500, 10).ToScreen(1000), 9);
    }

    [Fact]
    public void RangeWiderThanTheViewStaysPutWhileOnScreenAndIsCentredOtherwise()
    {
        var viewport = new KataElevationViewport(0.1, 0);

        Assert.Equal(viewport, viewport.EnsureVisible(new Interval1D(2000, 9000), widthPx: 500, marginPx: 10));
        Assert.Equal(250.0, viewport.EnsureVisible(new Interval1D(8000, 15000), 500, 10).ToScreen(11500), 9);
    }

    [Theory]
    [InlineData(0.02, 400, 400, 0.15)]    // whole run: raised so a 400 mm beam is 60 px
    [InlineData(0.5, 400, 400, 0.35)]     // zoomed in: capped so the band stays within 140 px
    [InlineData(0.2, 400, 400, 0.2)]      // true scale already readable
    [InlineData(0.02, 400, 1400, 0.1)]    // stepped soffit: the band cap wins over the beam minimum
    public void VerticalScaleIsReadableButBounded(double horizontal, double beamHeight, double band, double expected)
    {
        Assert.Equal(expected, KataElevationViewport.VerticalScale(horizontal, beamHeight, band, minBeamPx: 60, maxBandPx: 140), 9);
    }
}
