using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class GeometryIssueDetectorTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;
    private static readonly IReadOnlySet<string> All = new HashSet<string>(GeometryIssueType.All);

    private static Pt P(double x, double y) => new(x, y);

    private static AecEntityRecord Entity(string handle, PlanShape? shape, string type = "LINE", string layer = "0", string? note = null) =>
        new() { Handle = handle, Type = type, Layer = layer, Shape = shape, GeometryNote = shape is null ? note ?? "no plan geometry" : null, BoundsMm = shape?.Bounds };

    private static IReadOnlyList<GeometryIssue> Detect(params AecEntityRecord[] records) => GeometryIssueDetector.Detect(records, All, Tol, CancellationToken.None);

    [Fact]
    public void Exact_duplicate_lines_in_either_direction()
    {
        var issues = Detect(
            Entity("A", PlanShape.Segment(P(0, 0), P(1000, 0))),
            Entity("B", PlanShape.Segment(P(1000, 0), P(0, 0))),
            Entity("C", PlanShape.Segment(P(0, 500), P(1000, 500))));

        var duplicate = Assert.Single(issues, i => i.Type == GeometryIssueType.Duplicate);
        Assert.Equal(["A", "B"], duplicate.Handles);
        Assert.Equal("GEO-0001", duplicate.IssueId);
        Assert.Equal(IssueSeverity.Warning, duplicate.Severity);
    }

    [Fact]
    public void Near_duplicate_within_the_duplicate_tolerance_but_not_point_equality()
    {
        var issues = Detect(
            Entity("A", PlanShape.Segment(P(0, 0), P(1000, 0))),
            Entity("B", PlanShape.Segment(P(0, 0.8), P(1000, 0.8))));

        Assert.Contains(issues, i => i.Type == GeometryIssueType.NearDuplicate && i.Handles.SequenceEqual(["A", "B"]));
        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.Duplicate);
        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.OverlappingSegments);
    }

    [Fact]
    public void Rotated_ring_counts_as_the_same_polygon()
    {
        var a = new PlanShape([P(0, 0), P(100, 0), P(100, 100), P(0, 100)], true);
        var b = new PlanShape([P(100, 100), P(0, 100), P(0, 0), P(100, 0)], true);

        Assert.True(GeometryIssueDetector.SameVertices(a, b, Tol.PointEquality));
        Assert.Single(Detect(Entity("A", a, "LWPOLYLINE"), Entity("B", b, "LWPOLYLINE")), i => i.Type == GeometryIssueType.Duplicate);
    }

    [Fact]
    public void Overlapping_collinear_segments_between_two_entities()
    {
        var issues = Detect(
            Entity("A", PlanShape.Segment(P(0, 0), P(3000, 0))),
            Entity("B", PlanShape.Segment(P(2000, 0), P(6000, 0))));

        var overlap = Assert.Single(issues, i => i.Type == GeometryIssueType.OverlappingSegments);
        Assert.True(overlap.LocationMm!.Value.AlmostEqualsXY(P(2500, 0), 1e-6));
        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.EndpointGap);
    }

    [Fact]
    public void Endpoint_gap_between_meant_to_meet_lines()
    {
        var issues = Detect(
            Entity("A", PlanShape.Segment(P(0, 0), P(2993, 0))),
            Entity("B", PlanShape.Segment(P(3000, 0), P(3000, 2000))));

        var gap = Assert.Single(issues, i => i.Type == GeometryIssueType.EndpointGap);
        Assert.Equal(7, gap.ValueMm!.Value, 6);
        Assert.Equal(Tol.EndpointConnection, gap.ToleranceMm);
        Assert.NotNull(gap.SuggestedAction);
    }

    [Fact]
    public void Touching_endpoints_are_not_a_gap_and_far_ends_are_ignored()
    {
        var issues = Detect(
            Entity("A", PlanShape.Segment(P(0, 0), P(3000, 0))),
            Entity("B", PlanShape.Segment(P(3000, 0), P(3000, 2000))),
            Entity("C", PlanShape.Segment(P(3100, 0), P(3100, 2000))));

        Assert.DoesNotContain(issues, i => i.Type == GeometryIssueType.EndpointGap);
    }

    [Fact]
    public void Tiny_zero_length_open_polyline_and_self_intersection()
    {
        var tiny = new PlanShape([P(0, 0), P(1000, 0), P(1002, 0), P(1002, 1000)], false);
        var zero = PlanShape.Segment(P(50, 50), P(50, 50.1));
        var almostClosed = new PlanShape([P(0, 0), P(4000, 0), P(4000, 3000), P(0, 3000), P(0, 12)], false);
        var bowTie = new PlanShape([P(0, 0), P(100, 100), P(100, 0), P(0, 100)], true);

        var issues = Detect(Entity("T", tiny, "LWPOLYLINE"), Entity("Z", zero), Entity("O", almostClosed, "LWPOLYLINE"), Entity("X", bowTie, "LWPOLYLINE"));

        Assert.Contains(issues, i => i.Type == GeometryIssueType.TinySegment && i.Handles[0] == "T" && Math.Abs(i.ValueMm!.Value - 2) < 1e-6);
        Assert.Contains(issues, i => i.Type == GeometryIssueType.ZeroLength && i.Handles[0] == "Z");
        Assert.Contains(issues, i => i.Type == GeometryIssueType.OpenPolyline && i.Handles[0] == "O" && Math.Abs(i.ValueMm!.Value - 12) < 1e-6);
        Assert.Contains(issues, i => i.Type == GeometryIssueType.SelfIntersection && i.Handles[0] == "X" && i.LocationMm!.Value.AlmostEqualsXY(P(50, 50), 1e-6));
    }

    [Fact]
    public void Unreadable_geometry_is_reported_as_info_and_types_can_be_restricted()
    {
        var records = new[] { Entity("U", null, "ACAD_PROXY_ENTITY", note: "custom object"), Entity("A", PlanShape.Segment(P(0, 0), P(10, 0))), Entity("B", PlanShape.Segment(P(0, 0), P(10, 0))) };

        var all = GeometryIssueDetector.Detect(records, All, Tol, CancellationToken.None);
        Assert.Contains(all, i => i.Type == GeometryIssueType.InvalidGeometry && i.Severity == IssueSeverity.Info);
        Assert.Contains(all, i => i.Type == GeometryIssueType.Duplicate);

        var onlyGaps = GeometryIssueDetector.Detect(records, new HashSet<string> { GeometryIssueType.EndpointGap }, Tol, CancellationToken.None);
        Assert.Empty(onlyGaps);
    }

    [Fact]
    public void Issue_ids_are_sequential_and_the_cap_holds()
    {
        var records = Enumerable.Range(0, 30).Select(i => Entity($"H{i}", PlanShape.Segment(P(0, 0), P(1000, 0)))).ToArray();

        var issues = GeometryIssueDetector.Detect(records, All, Tol, CancellationToken.None, maxIssues: 10);

        Assert.Equal(10, issues.Count);
        Assert.Equal("GEO-0010", issues[^1].IssueId);
    }
}
