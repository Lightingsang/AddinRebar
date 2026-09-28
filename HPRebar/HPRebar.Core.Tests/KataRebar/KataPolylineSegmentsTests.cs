using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataPolylineSegmentsTests
{
    [Fact]
    public void A_closed_stirrup_keeps_all_four_sides()
    {
        var stirrup = KataStirrupCurveFactory.Create(KataStirrupShapeType.ClosedHoop, 450.0, -121.0, 121.0, -29.0, -571.0, 8.0, 1, 0);

        var segments = KataPolylineSegments.Of(stirrup.Polyline);

        Assert.Equal(4, segments.Count);
        Assert.Equal(segments[0].From, segments[3].To);
    }

    [Fact]
    public void An_open_bar_with_two_legs_has_three_segments()
    {
        var bar = new Polyline3(new List<Point3>
        {
            new(43.0, 0.0, -486.0), new(43.0, 0.0, -43.0), new(6757.0, 0.0, -43.0), new(6757.0, 0.0, -486.0)
        });

        Assert.Equal(3, KataPolylineSegments.Of(bar).Count);
    }

    [Fact]
    public void Vertices_closer_than_the_minimum_are_merged()
    {
        var bar = new Polyline3(new List<Point3> { new(0.0, 0.0, 0.0), new(0.4, 0.0, 0.0), new(1000.0, 0.0, 0.0) });

        var segments = KataPolylineSegments.Of(bar);

        Assert.Single(segments);
        Assert.Equal(new Point3(0.0, 0.0, 0.0), segments[0].From);
    }
}
