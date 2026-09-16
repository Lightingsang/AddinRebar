using HPAutoCad.Aec.Geometry;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class GeometryMathTests
{
    private static Pt P(double x, double y) => new(x, y);

    [Fact]
    public void Crossing_segments_intersect_at_the_expected_point()
    {
        var kind = GeometryMath.IntersectXY(new Seg(P(0, 0), P(10, 10)), new Seg(P(0, 10), P(10, 0)), 0.1, out var point);

        Assert.Equal(IntersectionKind.Point, kind);
        Assert.True(point.AlmostEqualsXY(P(5, 5), 1e-9));
    }

    [Fact]
    public void Segments_that_miss_by_more_than_the_tolerance_do_not_intersect()
    {
        Assert.Equal(IntersectionKind.None, GeometryMath.IntersectXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(5, 1), P(5, 10)), 0.5, out _));
        Assert.Equal(IntersectionKind.Point, GeometryMath.IntersectXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(5, 0.4), P(5, 10)), 0.5, out _));
    }

    [Fact]
    public void Parallel_segments_never_intersect_unless_collinear()
    {
        Assert.Equal(IntersectionKind.None, GeometryMath.IntersectXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(0, 5), P(10, 5)), 0.5, out _));

        var overlap = GeometryMath.IntersectXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(5, 0), P(15, 0)), 0.5, out var mid);
        Assert.Equal(IntersectionKind.Overlap, overlap);
        Assert.True(mid.AlmostEqualsXY(P(7.5, 0), 1e-9));

        var touch = GeometryMath.IntersectXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(10, 0), P(20, 0)), 0.5, out var end);
        Assert.Equal(IntersectionKind.Point, touch);
        Assert.True(end.AlmostEqualsXY(P(10, 0), 1e-9));
    }

    [Fact]
    public void Proper_crossing_excludes_endpoint_contact()
    {
        Assert.True(GeometryMath.CrossesProperlyXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(5, -5), P(5, 5)), 0.1, out _));
        Assert.False(GeometryMath.CrossesProperlyXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(10, -5), P(10, 5)), 0.1, out _)); // T at the end
        Assert.False(GeometryMath.CrossesProperlyXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(5, 0), P(5, 5)), 0.1, out _)); // T at the other's end
    }

    [Theory]
    [InlineData(0, 0, 10, 0, 0, 5, 10, 5, 0.5, true)]    // parallel
    [InlineData(0, 0, 10, 0, 0, 5, 10, 5.2, 0.5, false)]  // 1.1° off
    [InlineData(0, 0, 10, 0, 5, 0, 5, 10, 0.5, false)]    // perpendicular is not parallel
    public void Parallel_test_uses_the_angle_tolerance(double ax, double ay, double bx, double by, double cx, double cy, double dx, double dy, double tol, bool expected)
    {
        Assert.Equal(expected, GeometryMath.AreParallelXY(new Seg(P(ax, ay), P(bx, by)), new Seg(P(cx, cy), P(dx, dy)), tol));
    }

    [Fact]
    public void Perpendicular_and_collinear_tests()
    {
        Assert.True(GeometryMath.ArePerpendicularXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(3, -2), P(3, 9)), 0.5));
        Assert.True(GeometryMath.AreCollinearXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(20, 0.5), P(30, 0.5)), 1, 0.5));
        Assert.False(GeometryMath.AreCollinearXY(new Seg(P(0, 0), P(10, 0)), new Seg(P(20, 3), P(30, 3)), 1, 0.5));
    }

    [Fact]
    public void Ring_area_centroid_and_containment()
    {
        var ring = new[] { P(0, 0), P(4000, 0), P(4000, 3000), P(0, 3000) };

        Assert.Equal(12_000_000, GeometryMath.AreaXY(ring), 6);
        Assert.True(GeometryMath.CentroidXY(ring).AlmostEqualsXY(P(2000, 1500), 1e-9));
        Assert.True(GeometryMath.ContainsPointXY(ring, P(100, 100), 0));
        Assert.False(GeometryMath.ContainsPointXY(ring, P(-100, 100), 0));
        Assert.True(GeometryMath.ContainsPointXY(ring, P(4000, 1500), 0.5), "boundary counts as inside");
    }

    [Fact]
    public void Direction_angles_are_counter_clockwise_from_x()
    {
        Assert.Equal(0, GeometryMath.DirectionDegreesXY(new Seg(P(0, 0), P(1, 0))), 6);
        Assert.Equal(90, GeometryMath.DirectionDegreesXY(new Seg(P(0, 0), P(0, 1))), 6);
        Assert.Equal(270, GeometryMath.DirectionDegreesXY(new Seg(P(0, 0), P(0, -1))), 6);
        Assert.Equal(45, GeometryMath.AngleBetweenXY(P(1, 0), P(1, 1)), 6);
    }

    [Fact]
    public void Boxes_intersect_contain_and_measure_distance()
    {
        var a = Box.Of(P(0, 0), P(10, 10));
        var b = Box.Of(P(12, 0), P(20, 10));

        Assert.False(a.IntersectsXY(b));
        Assert.True(a.IntersectsXY(b, 2.5));
        Assert.Equal(2, a.DistanceXY(b), 9);
        Assert.True(a.ContainsXY(Box.Of(P(1, 1), P(9, 9))));
        Assert.True(a.Union(b).ContainsXY(P(15, 5)));
        Assert.True(Box.Empty.IsEmpty);
        Assert.False(Box.Empty.IntersectsXY(a));
    }

    [Fact]
    public void Segment_distance_and_closest_point()
    {
        var s = new Seg(P(0, 0), P(10, 0));

        Assert.Equal(3, s.DistanceXY(P(5, 3)), 9);
        Assert.Equal(5, s.DistanceXY(P(13, 4)), 9); // beyond the end: distance to the endpoint
        Assert.True(s.ClosestPointXY(P(5, 3)).AlmostEqualsXY(P(5, 0), 1e-9));
        Assert.Equal(0, s.DistanceXY(new Seg(P(5, -1), P(5, 1))), 9);
        Assert.Equal(2, s.DistanceXY(new Seg(P(0, 2), P(10, 2))), 9);
    }

    [Fact]
    public void Arc_tessellation_stays_within_the_chord_error()
    {
        var points = PlanShape.ArcPoints(P(0, 0), 1000, 0, 90, 0.25);
        Assert.True(points.Count >= 5);
        Assert.True(points[0].AlmostEqualsXY(P(1000, 0), 1e-6));
        Assert.True(points[^1].AlmostEqualsXY(P(0, 1000), 1e-6));
        foreach (var p in points) Assert.Equal(1000, p.LengthXY, 6);
        for (var i = 0; i + 1 < points.Count; i++)
        {
            var mid = Pt.Mid(points[i], points[i + 1]);
            Assert.True(1000 - mid.LengthXY <= 0.25 + 1e-9, "sagitta above the chord error");
        }

        var circle = new PlanShape(PlanShape.ArcPoints(P(0, 0), 500, 0, 360, 0.25), true);
        Assert.True(circle.Closed);
        Assert.InRange(circle.AreaMm2, Math.PI * 500 * 500 * 0.999, Math.PI * 500 * 500);
    }
}
