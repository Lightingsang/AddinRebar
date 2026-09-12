using System;
using System.Collections.Generic;
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

    /// <summary>Segments shorter than this are dropped before the bar reaches Revit, which rejects them.</summary>
    private const double MinimumSegmentMm = 1.0;

    /// <summary>The same thinning the rebar creator applies before it turns points into curves.</summary>
    private static IReadOnlyList<Point3> Simplify(IReadOnlyList<Point3> points)
    {
        var kept = new List<Point3> { points[0] };

        for (var i = 1; i < points.Count; i++)
        {
            if (BarPolylineBuilder.Distance(kept[kept.Count - 1], points[i]) >= MinimumSegmentMm)
            {
                kept.Add(points[i]);
            }
        }

        return kept;
    }

    /// <summary>How far <paramref name="point"/> sits off the straight run from one corner to the next.</summary>
    private static double DistanceToSegment(Point3 point, Point3 from, Point3 to)
    {
        var ux = to.X - from.X;
        var uy = to.Y - from.Y;
        var uz = to.Z - from.Z;

        var lengthSquared = ux * ux + uy * uy + uz * uz;

        if (lengthSquared <= 0) return BarPolylineBuilder.Distance(point, from);

        var along = ((point.X - from.X) * ux + (point.Y - from.Y) * uy + (point.Z - from.Z) * uz) / lengthSquared;
        along = Math.Max(0d, Math.Min(1d, along));

        var closest = new Point3(from.X + along * ux, from.Y + along * uy, from.Z + along * uz);

        return BarPolylineBuilder.Distance(point, closest);
    }

    private static double GreatestDeviation(IReadOnlyList<Point3> points, IReadOnlyList<Point3> corners)
    {
        var worst = 0d;

        foreach (var point in points)
        {
            var nearest = double.MaxValue;

            for (var i = 1; i < corners.Count; i++)
            {
                nearest = Math.Min(nearest, DistanceToSegment(point, corners[i - 1], corners[i]));
            }

            worst = Math.Max(worst, nearest);
        }

        return worst;
    }

    [Fact]
    public void CornersReducesAStraightRunToItsTwoEnds()
    {
        var points = new[]
        {
            new Point3(100, 200, 0),
            new Point3(100, 200, 1500),
            new Point3(100, 200, 2600),
            new Point3(100, 200, 3000)
        };

        var corners = BarPolylineBuilder.Corners(points);

        Assert.Equal(2, corners.Count);
        Assert.Equal(0d, corners[0].Z, Precision);
        Assert.Equal(3000d, corners[1].Z, Precision);
    }

    [Fact]
    public void CornersKeepsEveryPointThatTurns()
    {
        var points = new[]
        {
            new Point3(100, 160, 0),
            new Point3(100, 200, 0),
            new Point3(100, 200, 2400),
            new Point3(250, 200, 3000),
            new Point3(250, 200, 3700)
        };

        var corners = BarPolylineBuilder.Corners(points);

        Assert.Equal(5, corners.Count);
    }

    [Fact]
    public void CornersTreatsADoublingBackAsATurnEvenThoughItIsCollinear()
    {
        var points = new[]
        {
            new Point3(0, 0, 0),
            new Point3(0, 0, 1000),
            new Point3(0, 0, 400)
        };

        var corners = BarPolylineBuilder.Corners(points);

        Assert.Equal(3, corners.Count);
        Assert.Equal(1000d, corners[1].Z, Precision);
    }

    /// <summary>
    ///     A bar hooked at the base, bending across into the column above, whose top anchorage is too short
    ///     to survive the minimum-segment thinning. That thinning shifts the last three points along, and the
    ///     creator used to read the shifted pair as proof the whole middle of the bar was one straight run —
    ///     dropping the point where it starts to bend and running the bar diagonally from its base instead.
    /// </summary>
    [Fact]
    public void CornersKeepsTheBendStartWhenTheTopAnchorIsTooShortToSurvive()
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);

        var splice = new SpliceSpec
        {
            IsBottomDowels = true,
            BottomDowelsType = 1,
            LbBottom = 100,
            LaBottom = 40,
            IsTopDowels = true,
            TopDowelsType = 0,
            LbTop = 0
        };

        var polyline = BarPolylineBuilder.Build(section, spec, bar, splice, new PlanPoint(bar.X0 + 150, bar.Y0));
        var points = Simplify(polyline.Points);

        Assert.Equal(4, points.Count);

        var corners = BarPolylineBuilder.Corners(points);

        Assert.Equal(4, corners.Count);
        Assert.Equal(section.TopPosition - section.BendDepth, corners[2].Z, Precision);
        Assert.Equal(bar.X0, corners[2].X, Precision);
    }

    /// <summary>
    ///     The property the old two-line shortcut broke: whatever survives the thinning has to end up on the
    ///     bar that gets built, otherwise the bar is a different shape from the one that was calculated.
    /// </summary>
    [Theory]
    [InlineData(0d, 150d)]
    [InlineData(0.5d, 150d)]
    [InlineData(700d, 150d)]
    [InlineData(700d, 0d)]
    [InlineData(0d, 0d)]
    public void CornersNeverMovesAPointOffTheBar(double lbTop, double crossOverOffset)
    {
        var section = TestSections.Rectangle();
        var spec = TestSections.Grid();
        var bar = FirstBar(section, spec);

        var splice = new SpliceSpec
        {
            IsBottomDowels = true,
            BottomDowelsType = 1,
            LbBottom = 100,
            LaBottom = 40,
            IsTopDowels = true,
            TopDowelsType = 0,
            LbTop = lbTop
        };

        var upper = new PlanPoint(bar.X0 + crossOverOffset, bar.Y0);
        var points = Simplify(BarPolylineBuilder.Build(section, spec, bar, splice, upper).Points);
        var corners = BarPolylineBuilder.Corners(points);

        Assert.True(GreatestDeviation(points, corners) < 1e-6);
    }

    [Fact]
    public void CornersRefusesASinglePoint()
    {
        Assert.Throws<ArgumentException>(() => BarPolylineBuilder.Corners(new[] { new Point3(0, 0, 0) }));
    }
}
