using System.Text.Json;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Spatial;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>Defects found by the first code review of the engine, each pinned so it cannot come back.</summary>
public sealed class ReviewRegressionTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;

    private static Pt P(double x, double y) => new(x, y);

    [Fact]
    public void Oversized_item_is_found_from_any_corner_regardless_of_insertion_order()
    {
        // A 30 m site ring among 200 small boxes (cell ≈ 400 mm) spans far more than 4096 cells.
        foreach (var ringFirst in new[] { true, false })
        {
            var index = new SpatialIndex<string>();
            if (ringFirst) index.Insert(Box.Of(P(0, 0), P(30000, 30000)), "site");
            for (var i = 0; i < 200; i++) index.Insert(Box.Of(P(i * 150, 0), P(i * 150 + 200, 200)), "b" + i);
            if (!ringFirst) index.Insert(Box.Of(P(0, 0), P(30000, 30000)), "site");

            Assert.Contains("site", index.Query(Box.Of(P(29000, 29000), P(29100, 29100))));
            Assert.Contains("site", index.Query(Box.Of(P(100, 100), P(110, 110))));
        }
    }

    [Fact]
    public void Huge_query_box_falls_back_to_a_linear_scan_and_still_returns_everything_once()
    {
        var index = new SpatialIndex<int>();
        for (var i = 0; i < 500; i++) index.Insert(Box.Of(P(i * 100, 0), P(i * 100 + 50, 50)), i);

        var all = index.Query(Box.Of(P(-1e6, -1e6), P(1e6, 1e6))).ToArray();

        Assert.Equal(500, all.Length);
        Assert.Equal(500, all.Distinct().Count());
    }

    [Fact]
    public void Nearly_parallel_coincident_lines_intersect_overlap_and_do_not_merely_touch()
    {
        var a = PlanShape.Segment(P(0, 0), P(10000, 0));
        var b = PlanShape.Segment(P(2000, 0.2), P(8000, 0.3)); // 0.001° off, within the point tolerance

        Assert.True(SpatialPredicates.Evaluate(a, b, SpatialRelation.Intersects, Tol).Holds);
        Assert.True(SpatialPredicates.Evaluate(a, b, SpatialRelation.Overlaps, Tol).Holds);
        Assert.False(SpatialPredicates.Evaluate(a, b, SpatialRelation.Touches, Tol).Holds, "a shared run is an overlap, not a touch");
    }

    [Fact]
    public void Degenerate_segment_intersection_is_symmetric()
    {
        var point = new Seg(P(5, 0), P(5, 0.1));
        var line = new Seg(P(0, 0), P(10, 0));

        Assert.Equal(GeometryMath.IntersectXY(point, line, 0.5, out _), GeometryMath.IntersectXY(line, point, 0.5, out _));
        Assert.Equal(IntersectionKind.Point, GeometryMath.IntersectXY(line, point, 0.5, out var p));
        Assert.True(p.AlmostEqualsXY(P(5, 0), 0.2));
        Assert.Equal(IntersectionKind.None, GeometryMath.IntersectXY(line, new Seg(P(5, 3), P(5, 3.1)), 0.5, out _));
    }

    [Fact]
    public void Within_rejects_a_chord_across_a_concave_notch()
    {
        var lShape = new PlanShape([P(0, 0), P(4000, 0), P(4000, 1000), P(1000, 1000), P(1000, 4000), P(0, 4000)], true);
        var chord = PlanShape.Segment(P(500, 4000), P(4000, 500)); // ends on the boundary, middle outside the L

        Assert.False(SpatialPredicates.IsWithin(chord, lShape, Tol));
        Assert.True(SpatialPredicates.IsWithin(PlanShape.Segment(P(200, 200), P(3800, 800)), lShape, Tol));
    }

    [Fact]
    public void Approximate_shapes_never_produce_segment_level_issues()
    {
        var circle = new PlanShape(PlanShape.ArcPoints(P(0, 0), 10, 0, 360, Tol.ChordError), true, approximate: true); // r = 10 mm → chords ≈ 2 mm
        var textBox = PlanShape.Rectangle(Box.Of(P(1000, 0), P(2400, 200)));                                            // text sitting on a wall line
        var wall = PlanShape.Segment(P(0, 0), P(5000, 0));

        var issues = GeometryIssueDetector.Detect(
        [
            new AecEntityRecord { Handle = "C", Type = "CIRCLE", Layer = "0", Shape = circle },
            new AecEntityRecord { Handle = "T", Type = "TEXT", Layer = "0", Shape = textBox },
            new AecEntityRecord { Handle = "W", Type = "LINE", Layer = "0", Shape = wall },
        ], new HashSet<string>(GeometryIssueType.All), Tol, CancellationToken.None);

        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.TinySegment);
        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.OverlappingSegments);
        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.SelfIntersection);
    }

    [Fact]
    public void Detail_geometry_caps_the_vertices_and_reports_the_real_count()
    {
        var circle = new AecEntityRecord { Handle = "C", Type = "CIRCLE", Layer = "0", Shape = new PlanShape(PlanShape.ArcPoints(P(0, 0), 5000, 0, 360, 0.05), true, approximate: true) };

        var geometry = circle.ToGeometry()!;

        Assert.Equal(EntityGeometry.MaxVertices, geometry.Vertices.Count);
        Assert.True(geometry.VertexCount > EntityGeometry.MaxVertices);
        Assert.True(geometry.VerticesTruncated);
        Assert.True(geometry.Approximate);
    }

    [Fact]
    public void A_full_page_of_issues_or_detail_records_stays_under_the_bridge_cap()
    {
        const int capBytes = 65536;
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

        var issues = new AnalysisResult<GeometryIssue> { Count = 100 };
        issues.Items = Enumerable.Range(0, 100).Select(i => new GeometryIssue($"GEO-{i:0000}", GeometryIssueType.OverlappingSegments, IssueSeverity.Warning, ["1A2B3C", "1A2B3D"],
            new Pt(123456.7, 98765.4, 0), 1234.56, 10, "LWPOLYLINE 1A2B3C and LINE 1A2B3D overlap along a shared collinear run (layers A-WALL-EXTERIOR / A-WALL-INTERIOR).", "Trim or join so each edge is drawn once.")).ToArray();
        Assert.True(JsonSerializer.SerializeToUtf8Bytes(issues, options).Length < capBytes);

        var records = new AnalysisResult<Dictionary<string, object?>> { Count = 20 };
        var ring = new PlanShape(PlanShape.ArcPoints(P(123456, 654321), 5000, 0, 360, 0.05), true, approximate: true);
        records.Items = Enumerable.Range(0, AecTools.MaxDetailLimit).Select(i =>
        {
            var record = new AecEntityRecord { Handle = "1A2B" + i, Type = "LWPOLYLINE", Layer = "A-WALL-EXTERIOR", Color = "ByLayer", Linetype = "Continuous", Lineweight = "ByLayer", Space = "Model", Visible = true, BoundsMm = ring.Bounds, LengthMm = 31415.9, AreaMm2 = 78539816.3, Shape = ring, Text = "OFFICE 01", BlockName = "DOOR-0900", Attributes = new Dictionary<string, string> { ["TAG"] = "D01", ["WIDTH"] = "900" }, PositionMm = P(1, 2) };
            record.Geometry = record.ToGeometry();
            return Cad.EntityQueryService.Project(record, null, true);
        }).ToArray();
        Assert.True(JsonSerializer.SerializeToUtf8Bytes(records, options).Length < capBytes);
    }

    [Fact]
    public void Non_positive_tolerance_overrides_are_reported_not_silently_defaulted()
    {
        var tol = GeometryTolerance.From(new HPRebar.McpBridge.Core.Scripting.ScriptArgs(JsonSerializer.Deserialize<JsonElement>("""{"pointEquality":0,"duplicate":-3,"roomGap":40}""")), out var unknown);

        Assert.Equal(GeometryTolerance.Default.PointEquality, tol.PointEquality);
        Assert.Equal(40, tol.RoomGap);
        Assert.Equal(["pointEquality", "duplicate"], unknown);
    }
}
