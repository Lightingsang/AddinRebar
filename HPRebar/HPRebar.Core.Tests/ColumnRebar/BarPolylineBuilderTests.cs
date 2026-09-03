using System.Linq;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class BarPolylineBuilderTests
{
    private const int Precision = 6;

    private static BarPosition FirstBar(ColumnSection section, BarLayoutSpec spec) =>
        BarLayoutCalculator.Compute(section, spec)[0];

    [Fact]
    public void ABarThatCarriesOnUpwardHasThreePointsAboveItsBase()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);
        var splice = new SpliceSpec { IsTopDowels = true, TopDowelsType = 0, LbTop = 700 };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(50, 60));

        Assert.Equal(4, polyline.Points.Count);
        Assert.Equal(section.BottomPosition, polyline.Points[0].Z, Precision);
        Assert.Equal(section.TopPosition - section.BendDepth, polyline.Points[1].Z, Precision);
        Assert.Equal(section.TopPosition, polyline.Points[2].Z, Precision);
        Assert.Equal(section.TopPosition + 700, polyline.Points[3].Z, Precision);
    }

    [Fact]
    public void TheCrossOverMovesToTheUpperPositionAndStaysThere()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);
        var splice = new SpliceSpec { IsTopDowels = true, TopDowelsType = 0, LbTop = 700 };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(50, 60));

        Assert.Equal(bar.X0, polyline.Points[1].X, Precision);
        Assert.Equal(50d, polyline.Points[2].X, Precision);
        Assert.Equal(60d, polyline.Points[2].Y, Precision);
        Assert.Equal(50d, polyline.Points[3].X, Precision);
    }

    [Fact]
    public void WithNoBeamTheBendStartsAWholePlanDimensionBelowTheTop()
    {
        var section = TestSections.Rectangle();

        Assert.Equal(600d, section.BendDepth, Precision);
    }

    [Fact]
    public void WithABeamTheBendStartsUnderTheBeamSoffit()
    {
        var section = TestSections.Rectangle() with { Hb = 500, Zb = 100 };

        Assert.Equal(600d, section.BendDepth, Precision);
    }

    [Fact]
    public void ACircularColumnFallsBackToItsDiameterWhenThereIsNoBeam()
    {
        var section = TestSections.Circular();

        Assert.Equal(500d, section.BendDepth, Precision);
    }

    [Fact]
    public void ABarWithNoTopDowelsSimplyStopsInsideTheCover()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);
        var splice = new SpliceSpec { IsTopDowels = false };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(0, 0));

        Assert.Equal(2, polyline.Points.Count);
        Assert.Equal(section.TopPosition - TestSections.Cover, polyline.Points[1].Z, Precision);
        Assert.True(BarPolylineBuilder.IsStraight(polyline.Points));
    }

    [Fact]
    public void AStoppedDowelWithAHookTurnsInwardFromItsOwnFace()
    {
        var section = TestSections.Rectangle() with { Zb = 100 };
        var spec = TestSections.Grid();
        var bars = BarLayoutCalculator.Compute(section, spec);
        var splice = new SpliceSpec { IsTopDowels = true, TopDowelsType = 1, LaTop = 300 };

        var south = BarPolylineBuilder.Build(section, spec, bars[0], splice, new PlanPoint(0, 0));
        var east = BarPolylineBuilder.Build(section, spec, bars[3], splice, new PlanPoint(0, 0));
        var north = BarPolylineBuilder.Build(section, spec, bars[5], splice, new PlanPoint(0, 0));
        var west = BarPolylineBuilder.Build(section, spec, bars[8], splice, new PlanPoint(0, 0));

        Assert.Equal(bars[0].Y0 - 300, south.Points.Last().Y, Precision);
        Assert.Equal(bars[3].X0 + 300, east.Points.Last().X, Precision);
        Assert.Equal(bars[5].Y0 + 300, north.Points.Last().Y, Precision);
        Assert.Equal(bars[8].X0 - 300, west.Points.Last().X, Precision);
    }

    [Fact]
    public void AStoppedDowelWithNoHookGetsNoExtraPoint()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);
        var splice = new SpliceSpec { IsTopDowels = true, TopDowelsType = 1, LaTop = 0 };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(0, 0));

        Assert.Equal(2, polyline.Points.Count);
        Assert.True(BarPolylineBuilder.IsStraight(polyline.Points));
    }

    [Fact]
    public void ABottomDowelOfTypeZeroStartsClearOfTheBase()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);
        var splice = new SpliceSpec { IsBottomDowels = true, BottomDowelsType = 0, LcBottom = 400, IsTopDowels = false };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(0, 0));

        Assert.Equal(section.BottomPosition + 400, polyline.Points[0].Z, Precision);
    }

    [Fact]
    public void ABottomDowelThatRunsDownPassesBelowTheBase()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);
        var splice = new SpliceSpec { IsBottomDowels = true, BottomDowelsType = 1, LaBottom = 0, LbBottom = 400, IsTopDowels = false };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(0, 0));

        Assert.Equal(section.BottomPosition - 400, polyline.Points[0].Z, Precision);
    }

    [Fact]
    public void ABottomHookAddsItsLegBeforeTheVerticalStarts()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bars = BarLayoutCalculator.Compute(section, spec);
        var splice = new SpliceSpec { IsBottomDowels = true, BottomDowelsType = 1, LaBottom = 250, LbBottom = 400, IsTopDowels = false };

        var polyline = BarPolylineBuilder.Build(section, spec, bars[0], splice, new PlanPoint(0, 0));

        Assert.Equal(3, polyline.Points.Count);
        Assert.Equal(bars[0].Y0 - 250, polyline.Points[0].Y, Precision);
        Assert.Equal(bars[0].Y0, polyline.Points[1].Y, Precision);
        Assert.Equal(section.BottomPosition - 400, polyline.Points[0].Z, Precision);
        Assert.Equal(section.BottomPosition - 400, polyline.Points[1].Z, Precision);
    }

    [Fact]
    public void ACircularHookRunsRadiallyOutward()
    {
        var section = TestSections.Circular();
        var spec = TestSections.Ring();
        var bars = BarLayoutCalculator.Compute(section, spec);
        var splice = new SpliceSpec { IsBottomDowels = true, BottomDowelsType = 1, LaBottom = 100, LbBottom = 400, IsTopDowels = false };

        var polyline = BarPolylineBuilder.Build(section, spec, bars[0], splice, new PlanPoint(0, 0));

        Assert.Equal(307d, polyline.Points[0].X, Precision);
        Assert.Equal(0d, polyline.Points[0].Y, Precision);
    }

    [Fact]
    public void StaggeredBarsEndAtDifferentHeightsSoNeighboursDoNotStopTogether()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bars = BarLayoutCalculator.Compute(section, spec);

        var odd = BarPolylineBuilder.Build(section, spec, bars[0],
            SpliceSpec.Default(bars[0].BarNumber, 20, 50, 35), new PlanPoint(0, 0));

        var even = BarPolylineBuilder.Build(section, spec, bars[1],
            SpliceSpec.Default(bars[1].BarNumber, 20, 50, 35), new PlanPoint(0, 0));

        Assert.Equal(section.TopPosition + 1400, odd.Points.Last().Z, Precision);
        Assert.Equal(section.TopPosition + 700, even.Points.Last().Z, Precision);
    }

    [Fact]
    public void LengthSumsEverySegment()
    {
        var points = new[]
        {
            new Point3(0, 0, 0),
            new Point3(0, 0, 3000),
            new Point3(0, 400, 3000)
        };

        Assert.Equal(3400d, BarPolylineBuilder.Length(points), Precision);
    }
}
