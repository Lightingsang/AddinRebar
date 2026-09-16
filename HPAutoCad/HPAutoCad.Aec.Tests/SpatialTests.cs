using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Spatial;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class SpatialTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;

    private static Pt P(double x, double y) => new(x, y);

    private static PlanShape Rect(double x0, double y0, double x1, double y1) => PlanShape.Rectangle(Box.Of(P(x0, y0), P(x1, y1)));

    private static PlanShape Line(double x0, double y0, double x1, double y1) => PlanShape.Segment(P(x0, y0), P(x1, y1));

    [Fact]
    public void Within_and_contains_are_inverse_and_need_a_closed_target()
    {
        var room = Rect(0, 0, 5000, 4000);
        var column = Rect(1000, 1000, 1400, 1400);

        Assert.True(SpatialPredicates.Evaluate(column, room, SpatialRelation.Within, Tol).Holds);
        Assert.True(SpatialPredicates.Evaluate(room, column, SpatialRelation.Contains, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(room, column, SpatialRelation.Within, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(column, Line(0, 0, 5000, 0), SpatialRelation.Within, Tol).Holds, "an open target contains nothing");
        Assert.True(SpatialPredicates.Evaluate(column, room, SpatialRelation.InsidePolygon, Tol).Holds);
        Assert.True(SpatialPredicates.Evaluate(column, room, SpatialRelation.InsideBbox, Tol).Holds);
    }

    [Fact]
    public void Intersects_crosses_touches_are_distinguished()
    {
        var beam = Line(0, 500, 6000, 500);
        var column = Rect(2800, 300, 3200, 700); // beam passes through the column
        var wall = Line(6000, 0, 6000, 3000);    // beam ends on the wall

        Assert.True(SpatialPredicates.Evaluate(beam, column, SpatialRelation.Intersects, Tol).Holds);
        Assert.True(SpatialPredicates.Evaluate(beam, column, SpatialRelation.Crosses, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(beam, column, SpatialRelation.Touches, Tol).Holds);

        Assert.True(SpatialPredicates.Evaluate(beam, wall, SpatialRelation.Intersects, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(beam, wall, SpatialRelation.Crosses, Tol).Holds, "end contact is not a proper crossing");
        Assert.True(SpatialPredicates.Evaluate(beam, wall, SpatialRelation.Touches, Tol).Holds);

        var far = Line(0, 2000, 6000, 2000);
        Assert.False(SpatialPredicates.Evaluate(beam, far, SpatialRelation.Intersects, Tol).Holds);
    }

    [Fact]
    public void Touches_uses_the_endpoint_connection_tolerance()
    {
        var beam = Line(0, 500, 5992, 500); // 8 mm short of the wall
        var wall = Line(6000, 0, 6000, 3000);

        Assert.True(SpatialPredicates.Evaluate(beam, wall, SpatialRelation.Touches, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(beam, wall, SpatialRelation.Touches, Tol with { EndpointConnection = 5 }).Holds);
    }

    [Fact]
    public void Overlaps_detects_shared_collinear_runs_and_partial_ring_overlap()
    {
        var a = Line(0, 0, 3000, 0);
        var b = Line(2000, 0, 5000, 0);
        var overlap = SpatialPredicates.Evaluate(a, b, SpatialRelation.Overlaps, Tol);
        Assert.True(overlap.Holds);
        Assert.True(overlap.Points[0].AlmostEqualsXY(P(2500, 0), 1e-6));

        Assert.True(SpatialPredicates.Evaluate(Rect(0, 0, 100, 100), Rect(50, 50, 150, 150), SpatialRelation.Overlaps, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(Rect(0, 0, 100, 100), Rect(10, 10, 20, 20), SpatialRelation.Overlaps, Tol).Holds, "containment is not overlap");
        Assert.False(SpatialPredicates.Evaluate(Line(0, 0, 100, 0), Line(0, 5, 100, 5), SpatialRelation.Overlaps, Tol).Holds);
    }

    [Fact]
    public void Distance_between_shapes_is_zero_when_they_meet()
    {
        Assert.Equal(300, SpatialPredicates.DistanceXY(Line(0, 0, 1000, 0), Line(0, 300, 1000, 300), Tol), 6);
        Assert.Equal(0, SpatialPredicates.DistanceXY(Rect(0, 0, 100, 100), Line(50, 50, 500, 500), Tol), 6);
        Assert.Equal(0, SpatialPredicates.DistanceXY(Rect(0, 0, 100, 100), Rect(10, 10, 20, 20), Tol), 6);
        Assert.Equal(100, SpatialPredicates.DistanceXY(PlanShape.Point(P(200, 50)), Rect(0, 0, 100, 100), Tol), 6);

        var far = SpatialPredicates.Evaluate(Line(0, 0, 1000, 0), Line(0, 300, 1000, 300), SpatialRelation.DistanceTo, Tol, 100);
        Assert.False(far.Holds);
        Assert.Equal(300, far.DistanceMm!.Value, 6);
        Assert.True(SpatialPredicates.Evaluate(Line(0, 0, 1000, 0), Line(0, 300, 1000, 300), SpatialRelation.DistanceTo, Tol, 400).Holds);
    }

    [Fact]
    public void Spatial_index_returns_each_neighbour_once_and_only_neighbours()
    {
        var index = new SpatialIndex<string>();
        index.Insert(Box.Of(P(0, 0), P(100, 100)), "a");
        index.Insert(Box.Of(P(90, 90), P(300, 300)), "b");   // straddles cells
        index.Insert(Box.Of(P(5000, 5000), P(5100, 5100)), "c");

        var near = index.Query(Box.Of(P(50, 50), P(60, 60))).ToArray();
        Assert.Equal(["a"], near);

        var both = index.Query(Box.Of(P(95, 95), P(96, 96))).OrderBy(x => x).ToArray();
        Assert.Equal(["a", "b"], both);

        Assert.Empty(index.Query(Box.Of(P(1000, 1000), P(1001, 1001))));
        Assert.Contains("c", index.Query(Box.Of(P(4990, 4990), P(4991, 4991)), tolerance: 20));
        Assert.Equal(3, index.All().Count());
    }

    [Theory]
    [InlineData("within", SpatialRelation.Within)]
    [InlineData("distance_to", SpatialRelation.DistanceTo)]
    [InlineData("DistanceTo", SpatialRelation.DistanceTo)]
    [InlineData("inside-bbox", SpatialRelation.InsideBbox)]
    [InlineData("Nearest", SpatialRelation.Nearest)]
    public void Relation_names_parse_in_every_spelling(string text, SpatialRelation expected)
    {
        Assert.True(SpatialRelations.TryParse(text, out var relation));
        Assert.Equal(expected, relation);
    }

    [Fact]
    public void Unknown_relation_is_rejected_and_names_round_trip()
    {
        Assert.False(SpatialRelations.TryParse("near", out _));
        foreach (var name in SpatialRelations.Names)
        {
            Assert.True(SpatialRelations.TryParse(name, out var relation));
            Assert.Equal(name, SpatialRelations.Name(relation));
        }
    }
}
