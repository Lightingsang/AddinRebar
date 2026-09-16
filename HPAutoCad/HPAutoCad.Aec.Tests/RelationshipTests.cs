using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Relationships;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class RelationshipTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;

    private static Pt P(double x, double y) => new(x, y);

    private static AecObject Obj(string handle, string aecType, PlanShape shape) => new()
    {
        Handle = handle, Type = shape.Closed ? "LWPOLYLINE" : "LINE", Layer = "0", AecType = aecType, Confidence = 1, BoundsMm = shape.Bounds,
        Record = new AecEntityRecord { Handle = handle, Type = shape.Closed ? "LWPOLYLINE" : "LINE", Layer = "0", Shape = shape, BoundsMm = shape.Bounds },
    };

    private static PlanShape Rect(double x, double y, double w, double h) => new([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true);

    private static IReadOnlyList<AecRelationship> Detect(IReadOnlyList<AecObject> sources, IReadOnlyList<AecObject> targets, params string[] relations) =>
        RelationshipDetector.Detect(sources, targets, new HashSet<string>(relations), Tol, 100, CancellationToken.None, 1000).Items;

    [Fact]
    public void Beam_landing_on_a_column_is_connected_with_full_confidence_and_a_gap_lowers_it()
    {
        var column = Obj("C17", AecType.StructuralColumn, Rect(5800, -200, 400, 400));
        var beam = Obj("B25", AecType.StructuralBeam, PlanShape.Segment(P(200, 0), P(5800, 0)));
        var shortBeam = Obj("B26", AecType.StructuralBeam, PlanShape.Segment(P(200, 100), P(5793, 100)));

        var found = Detect([beam, shortBeam], [column], RelationType.Connected).ToDictionary(r => r.Source);

        Assert.Equal(1, found["B25"].Confidence);
        Assert.Equal(0, found["B25"].ValueMm);
        Assert.Equal(AecType.StructuralColumn, found["B25"].TargetAecType);
        Assert.Equal(7, found["B26"].ValueMm);
        Assert.InRange(found["B26"].Confidence, 0.6, 0.7); // 1 - 7/10 * 0.5
        Assert.True(found["B26"].LocationMm!.Value.AlmostEqualsXY(P(5793, 100), 1e-6));
    }

    [Fact]
    public void Far_beam_is_not_connected_but_still_parallel()
    {
        var column = Obj("C1", AecType.StructuralColumn, Rect(0, 0, 400, 400));
        var beamA = Obj("B1", AecType.StructuralBeam, PlanShape.Segment(P(1000, 200), P(6000, 200)));
        var beamB = Obj("B2", AecType.StructuralBeam, PlanShape.Segment(P(1000, 5200), P(6000, 5200)));

        Assert.Empty(Detect([beamA], [column], RelationType.Connected));
        var axes = Detect([beamA], [beamB], RelationType.Parallel, RelationType.Aligned, RelationType.Perpendicular);
        Assert.Equal([RelationType.Parallel], axes.Select(r => r.Relation));
    }

    [Fact]
    public void Aligned_and_perpendicular_use_the_main_axes_of_rectangles_too()
    {
        var wallA = Obj("W1", AecType.ArchitecturalWall, Rect(0, 0, 4000, 200));       // long axis along X at y = 100
        var wallB = Obj("W2", AecType.ArchitecturalWall, Rect(5000, 0, 3000, 200));    // same axis, further along
        var wallC = Obj("W3", AecType.ArchitecturalWall, Rect(4000, 200, 200, 3000));  // perpendicular

        var found = Detect([wallA], [wallB, wallC], RelationType.Aligned, RelationType.Parallel, RelationType.Perpendicular);

        Assert.Contains(found, r => r.Target == "W2" && r.Relation == RelationType.Aligned);
        Assert.Contains(found, r => r.Target == "W2" && r.Relation == RelationType.Parallel);
        Assert.Contains(found, r => r.Target == "W3" && r.Relation == RelationType.Perpendicular);
        Assert.DoesNotContain(found, r => r.Target == "W3" && r.Relation == RelationType.Aligned);
    }

    [Fact]
    public void Inside_contains_touching_intersect_and_near()
    {
        var room = Obj("R1", AecType.Room, Rect(0, 0, 5000, 4000));
        var desk = Obj("F1", AecType.Furniture, Rect(1000, 1000, 1500, 800));
        var pipe = Obj("P1", AecType.Pipe, PlanShape.Segment(P(2500, -1000), P(2500, 2000)));
        var neighbour = Obj("R2", AecType.Room, Rect(5000, 0, 3000, 4000));
        var far = Obj("R3", AecType.Room, Rect(9000, 0, 3000, 4000));

        var found = Detect([room], [desk, pipe, neighbour, far], RelationType.Contains, RelationType.Intersect, RelationType.Touching, RelationType.Near);

        Assert.Contains(found, r => r.Target == "F1" && r.Relation == RelationType.Contains);
        Assert.Contains(found, r => r.Target == "P1" && r.Relation == RelationType.Intersect && r.LocationMm!.Value.AlmostEqualsXY(P(2500, 0), 1e-6));
        Assert.Contains(found, r => r.Target == "R2" && r.Relation == RelationType.Near && r.ValueMm == 0);
        Assert.DoesNotContain(found, r => r.Target == "R3");
        Assert.Contains(Detect([desk], [room], RelationType.Inside), r => r.Relation == RelationType.Inside);
        // rooms sharing a whole edge overlap rather than touch (collinear run), so no `touching` between R1 and R2
        Assert.DoesNotContain(found, r => r.Target == "R2" && r.Relation == RelationType.Touching);
    }

    [Fact]
    public void Connection_gap_reports_the_end_that_reaches_the_other_shape()
    {
        var column = Rect(0, 0, 400, 400);
        var beam = PlanShape.Segment(P(405, 200), P(4000, 200));

        var (gap, at) = RelationshipDetector.ConnectionGap(beam, column, Tol);

        Assert.Equal(5, gap);
        Assert.True(at!.Value.AlmostEqualsXY(P(405, 200), 1e-6));
        Assert.Null(RelationshipDetector.ConnectionGap(Rect(0, 0, 1, 1), Rect(5, 5, 1, 1), Tol).Gap);
    }

    [Fact]
    public void Unknown_relation_names_are_rejected()
    {
        var a = Obj("A", AecType.StructuralBeam, PlanShape.Segment(P(0, 0), P(1, 0)));
        Assert.Throws<ArgumentException>(() => RelationshipDetector.Evaluate(a, a, "adjacent", Tol, 100));
    }

    [Fact]
    public void Self_set_symmetric_relations_are_reported_once_per_pair_and_the_total_counts_past_the_cap()
    {
        var columns = Enumerable.Range(0, 4).Select(i => Obj($"C{i}", AecType.StructuralColumn, Rect(i * 3000, 0, 400, 800))).ToArray();

        var all = RelationshipDetector.Detect(columns, columns, new HashSet<string> { RelationType.Parallel }, Tol, 100, CancellationToken.None, 1000, sameSet: true);
        var capped = RelationshipDetector.Detect(columns, columns, new HashSet<string> { RelationType.Parallel }, Tol, 100, CancellationToken.None, 2, sameSet: true);

        Assert.Equal(6, all.Total); // C(4, 2), not 12
        Assert.Equal(6, all.Items.Count);
        Assert.All(all.Items, r => Assert.True(string.CompareOrdinal(r.Source, r.Target) < 0));
        Assert.Equal(6, capped.Total);
        Assert.Equal(2, capped.Items.Count);

        // directional relations stay directional over one set: the room contains the desk, the desk is inside the room
        var room = Obj("R", AecType.Room, Rect(0, 0, 5000, 4000));
        var desk = Obj("F", AecType.Furniture, Rect(1000, 1000, 1500, 800));
        var directional = RelationshipDetector.Detect([room, desk], [room, desk], new HashSet<string> { RelationType.Contains, RelationType.Inside }, Tol, 100, CancellationToken.None, 1000, sameSet: true);
        Assert.Contains(directional.Items, r => r.Source == "R" && r.Target == "F" && r.Relation == RelationType.Contains);
        Assert.Contains(directional.Items, r => r.Source == "F" && r.Target == "R" && r.Relation == RelationType.Inside);
        Assert.Equal(2, directional.Total);
    }

    [Fact]
    public void Axis_relations_carry_the_angle_and_aligned_carries_the_offset()
    {
        var beam = Obj("B1", AecType.StructuralBeam, PlanShape.Segment(P(0, 0), P(6000, 0)));
        var parallel = Obj("B2", AecType.StructuralBeam, PlanShape.Segment(P(0, 3000), P(6000, 3010)));
        var perpendicular = Obj("B3", AecType.StructuralBeam, PlanShape.Segment(P(3000, 100), P(3000, 4000)));
        var aligned = Obj("B4", AecType.StructuralBeam, PlanShape.Segment(P(7000, 0.4), P(12000, 0.4)));

        var found = Detect([beam], [parallel, perpendicular, aligned], RelationType.Parallel, RelationType.Perpendicular, RelationType.Aligned).ToDictionary(r => (r.Target, r.Relation));

        Assert.InRange(found[("B2", RelationType.Parallel)].AngleDeg!.Value, 0.09, 0.1);
        Assert.Null(found[("B2", RelationType.Parallel)].ValueMm);
        Assert.Null(found[("B2", RelationType.Parallel)].LocationMm);
        Assert.Equal(90, found[("B3", RelationType.Perpendicular)].AngleDeg);
        Assert.Equal(0.4, found[("B4", RelationType.Aligned)].ValueMm);
        Assert.Equal(0, found[("B4", RelationType.Aligned)].AngleDeg);
        Assert.True(found[("B4", RelationType.Aligned)].LocationMm!.Value.AlmostEqualsXY(P(9500, 0.4), 1e-6));
        Assert.False(found.ContainsKey(("B2", RelationType.Aligned)));
    }

    [Fact]
    public void Every_relationship_names_a_real_location_never_the_origin()
    {
        // A desk inside a room intersects it without any boundary crossing: the location must be the desk's centroid, not (0, 0).
        var room = Obj("R", AecType.Room, Rect(2000, 2000, 5000, 4000));
        var desk = Obj("F", AecType.Furniture, Rect(3000, 3000, 1500, 800));
        var chair = Obj("K", AecType.Furniture, Rect(4600, 3000, 500, 500)); // 100 mm from the desk

        var found = Detect([room, desk], [desk, chair], RelationType.Intersect, RelationType.Near, RelationType.Touching, RelationType.Contains, RelationType.Inside);

        Assert.NotEmpty(found);
        Assert.All(found, r => Assert.NotNull(r.LocationMm));
        Assert.All(found, r => Assert.False(r.LocationMm!.Value.AlmostEqualsXY(P(0, 0), 1)));
        Assert.True(found.Single(r => r.Source == "R" && r.Target == "F" && r.Relation == RelationType.Intersect).LocationMm!.Value.AlmostEqualsXY(P(3750, 3400), 1e-6));
        var near = found.Single(r => r.Source == "F" && r.Target == "K" && r.Relation == RelationType.Near);
        Assert.Equal(100, near.ValueMm);
        Assert.Equal(4500, near.LocationMm!.Value.X, 6);
    }

    [Fact]
    public void Axis_relations_over_too_many_pairs_are_refused_before_any_work()
    {
        var many = Enumerable.Range(0, 1415).Select(i => Obj($"H{i}", AecType.StructuralBeam, PlanShape.Segment(P(0, i), P(1000, i)))).ToArray();

        var error = Assert.Throws<ArgumentException>(() => RelationshipDetector.Detect(many, many, new HashSet<string> { RelationType.Parallel }, Tol, 100, CancellationToken.None, 10, sameSet: true));

        Assert.Contains("narrow the filters", error.Message);
        // a proximity relation over the same sets is fine: the grid index keeps the work local
        Assert.True(RelationshipDetector.Detect(many.Take(50).ToArray(), many.Take(50).ToArray(), new HashSet<string> { RelationType.Near }, Tol, 100, CancellationToken.None, 10, sameSet: true).Total > 0);
    }
}
