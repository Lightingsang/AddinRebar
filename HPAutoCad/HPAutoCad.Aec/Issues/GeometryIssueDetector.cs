using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Spatial;

namespace HPAutoCad.Aec.Issues;

/// <summary>
///     Finds drafting defects in a set of entity shapes: exact and near duplicates, collinear overlaps
///     between different entities, tiny and zero-length segments, polylines that are almost closed, gaps
///     between endpoints that were meant to meet, self-intersecting polylines, and entities whose geometry
///     could not be read. Pairwise checks go through the spatial index, so a floor plan stays near-linear.
/// </summary>
public static class GeometryIssueDetector
{
    /// <summary>
    ///     Every space is checked on its own: a title block repeated on two layouts is not a duplicate and a sheet border is never
    ///     "near" a model-space grid line. Records without a space count as model space. Ids are sequential over the whole run.
    /// </summary>
    public static IReadOnlyList<GeometryIssue> Detect(IReadOnlyList<AecEntityRecord> records, IReadOnlySet<string> types, GeometryTolerance tol, CancellationToken ct, int maxIssues = 2000)
    {
        var groups = records.GroupBy(r => r.Space ?? "Model", StringComparer.OrdinalIgnoreCase).ToArray();
        if (groups.Length <= 1) return DetectInSpace(records, types, tol, ct, maxIssues);
        var all = new List<GeometryIssue>();
        foreach (var group in groups)
        {
            var remaining = maxIssues - all.Count;
            if (remaining <= 0) break;
            all.AddRange(DetectInSpace(group.ToArray(), types, tol, ct, remaining));
        }

        return all.Select((i, n) => i with { IssueId = $"GEO-{n + 1:0000}" }).ToArray();
    }

    private static IReadOnlyList<GeometryIssue> DetectInSpace(IReadOnlyList<AecEntityRecord> records, IReadOnlySet<string> types, GeometryTolerance tol, CancellationToken ct, int maxIssues)
    {
        var issues = new List<GeometryIssue>();
        var withShape = records.Where(r => r.Shape is not null && !r.Shape.IsPoint).ToArray();

        if (types.Contains(GeometryIssueType.InvalidGeometry))
            foreach (var r in records.Where(r => r.Shape is null && r.GeometryNote is not null))
                Add(issues, GeometryIssueType.InvalidGeometry, IssueSeverity.Info, [r.Handle], r.BoundsMm?.Center, null, null,
                    $"{r.Type} {r.Handle}: {r.GeometryNote}", "Check the entity with AUDIT or LIST; custom objects need their enabler.");

        // Tessellated curves and stand-in rectangles are not drafted segments: no tiny-segment, overlap or self-intersection verdicts on them.
        if (types.Contains(GeometryIssueType.ZeroLength) || types.Contains(GeometryIssueType.TinySegment))
            foreach (var r in withShape.Where(r => !r.Shape!.Approximate)) SingleShapeSegmentChecks(issues, r, types, tol);

        if (types.Contains(GeometryIssueType.OpenPolyline))
            foreach (var r in withShape)
            {
                var s = r.Shape!;
                if (s.Closed || s.Vertices.Count < 3) continue;
                var gap = s.Start.DistanceXY(s.End);
                if (gap <= tol.RoomGap)
                    Add(issues, GeometryIssueType.OpenPolyline, IssueSeverity.Warning, [r.Handle], Pt.Mid(s.Start, s.End), gap, tol.RoomGap,
                        $"{r.Type} {r.Handle} is open but its ends are {gap:0.#} mm apart — probably meant to be closed.", "Close the polyline (PEDIT → Close) or join the ends.");
            }

        if (types.Contains(GeometryIssueType.SelfIntersection))
            foreach (var r in withShape.Where(r => !r.Shape!.Approximate)) SelfIntersections(issues, r, tol);

        var pairwise = types.Contains(GeometryIssueType.Duplicate) || types.Contains(GeometryIssueType.NearDuplicate)
                       || types.Contains(GeometryIssueType.OverlappingSegments) || types.Contains(GeometryIssueType.EndpointGap);
        if (pairwise && withShape.Length > 1) PairwiseChecks(issues, withShape, types, tol, ct, maxIssues);

        return issues.Count > maxIssues ? issues.Take(maxIssues).ToArray() : issues;
    }

    private static void SingleShapeSegmentChecks(List<GeometryIssue> issues, AecEntityRecord r, IReadOnlySet<string> types, GeometryTolerance tol)
    {
        var s = r.Shape!;
        if (s.Vertices.Count == 2 && s.LengthMm <= tol.PointEquality)
        {
            if (types.Contains(GeometryIssueType.ZeroLength))
                Add(issues, GeometryIssueType.ZeroLength, IssueSeverity.Warning, [r.Handle], s.Start, s.LengthMm, tol.PointEquality,
                    $"{r.Type} {r.Handle} has zero length.", "Erase it (OVERKILL removes these too).");
            return;
        }

        if (!types.Contains(GeometryIssueType.TinySegment)) return;
        var reported = 0;
        foreach (var seg in s.Segments)
        {
            var length = seg.LengthXY;
            if (length > tol.TinySegment) continue;
            var isZero = length <= tol.PointEquality;
            if (isZero && !types.Contains(GeometryIssueType.ZeroLength)) continue;
            Add(issues, isZero ? GeometryIssueType.ZeroLength : GeometryIssueType.TinySegment, isZero ? IssueSeverity.Warning : IssueSeverity.Info,
                [r.Handle], seg.Mid, length, isZero ? tol.PointEquality : tol.TinySegment,
                $"{r.Type} {r.Handle} has a {length:0.##} mm segment.", isZero ? "Remove the repeated vertex." : "Check for an accidental vertex; simplify if unintended.");
            if (++reported >= 5) break; // one entity with a hundred micro-segments is one problem, not a hundred
        }
    }

    private static void SelfIntersections(List<GeometryIssue> issues, AecEntityRecord r, GeometryTolerance tol)
    {
        var segments = r.Shape!.Segments;
        if (segments.Count < 3) return;
        var reported = 0;
        for (var i = 0; i < segments.Count; i++)
        for (var j = i + 2; j < segments.Count; j++)
        {
            if (r.Shape.Closed && i == 0 && j == segments.Count - 1) continue; // ring closure neighbours
            if (!segments[i].Bounds.Expand(tol.PointEquality).IntersectsXY(segments[j].Bounds)) continue;
            if (!GeometryMath.CrossesProperlyXY(segments[i], segments[j], tol.PointEquality, out var p)) continue;
            Add(issues, GeometryIssueType.SelfIntersection, IssueSeverity.Warning, [r.Handle], p, null, tol.PointEquality,
                $"{r.Type} {r.Handle} crosses itself near {p.Rounded()}.", "Untangle the vertices; hatches and areas on this outline are unreliable.");
            if (++reported >= 5) return;
        }
    }

    private static void PairwiseChecks(List<GeometryIssue> issues, AecEntityRecord[] records, IReadOnlySet<string> types, GeometryTolerance tol, CancellationToken ct, int maxIssues)
    {
        var index = new SpatialIndex<int>();
        for (var i = 0; i < records.Length; i++) index.Insert(records[i].Shape!.Bounds, i);
        var reach = Math.Max(tol.EndpointConnection, Math.Max(tol.Duplicate, tol.Collinearity));
        var seenPairs = new HashSet<(int, int)>();

        for (var i = 0; i < records.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (issues.Count >= maxIssues) return;
            var a = records[i];
            foreach (var j in index.Query(a.Shape!.Bounds, reach))
            {
                if (j <= i || !seenPairs.Add((i, j))) continue;
                var b = records[j];
                CheckPair(issues, a, b, types, tol);
            }
        }
    }

    private static void CheckPair(List<GeometryIssue> issues, AecEntityRecord a, AecEntityRecord b, IReadOnlySet<string> types, GeometryTolerance tol)
    {
        var sa = a.Shape!;
        var sb = b.Shape!;

        if ((types.Contains(GeometryIssueType.Duplicate) || types.Contains(GeometryIssueType.NearDuplicate)) && SameType(a, b))
        {
            var exact = SameVertices(sa, sb, tol.PointEquality);
            var near = !exact && SameVertices(sa, sb, tol.Duplicate);
            if (exact && types.Contains(GeometryIssueType.Duplicate))
            {
                Add(issues, GeometryIssueType.Duplicate, IssueSeverity.Warning, [a.Handle, b.Handle], sa.Bounds.Center, 0, tol.PointEquality,
                    $"{a.Type} {a.Handle} and {b.Handle} are identical (same vertices on {LayerNote(a, b)}).", "Erase one (OVERKILL).");
                return;
            }

            if (near && types.Contains(GeometryIssueType.NearDuplicate))
            {
                Add(issues, GeometryIssueType.NearDuplicate, IssueSeverity.Info, [a.Handle, b.Handle], sa.Bounds.Center, MaxVertexDistance(sa, sb), tol.Duplicate,
                    $"{a.Type} {a.Handle} and {b.Handle} nearly coincide (vertices within {tol.Duplicate} mm, {LayerNote(a, b)}).", "Check which one is right; erase the other.");
                return;
            }
        }

        if (types.Contains(GeometryIssueType.OverlappingSegments) && !sa.Approximate && !sb.Approximate)
        {
            var runs = SpatialPredicates.CollinearOverlaps(sa, sb, tol);
            if (runs.Count > 0 && !SameVertices(sa, sb, tol.Duplicate))
                Add(issues, GeometryIssueType.OverlappingSegments, IssueSeverity.Warning, [a.Handle, b.Handle], runs[0], null, tol.Collinearity,
                    $"{a.Type} {a.Handle} and {b.Type} {b.Handle} overlap along a shared collinear run ({LayerNote(a, b)}).", "Trim or join so each edge is drawn once.");
        }

        // Two chains that already meet at one end are connected: their other ends being close is not a gap.
        if (types.Contains(GeometryIssueType.EndpointGap) && !sa.Endpoints.Any(pa => sb.Endpoints.Any(pb => pa.AlmostEqualsXY(pb, tol.PointEquality))))
            foreach (var pa in sa.Endpoints)
            foreach (var pb in sb.Endpoints)
            {
                var gap = pa.DistanceXY(pb);
                if (gap <= tol.PointEquality || gap > tol.EndpointConnection) continue;
                Add(issues, GeometryIssueType.EndpointGap, IssueSeverity.Warning, [a.Handle, b.Handle], Pt.Mid(pa, pb), gap, tol.EndpointConnection,
                    $"Ends of {a.Type} {a.Handle} and {b.Type} {b.Handle} are {gap:0.##} mm apart.", "Snap the endpoints together (FILLET radius 0 or EXTEND).");
            }
    }

    private static bool SameType(AecEntityRecord a, AecEntityRecord b) => string.Equals(a.Type, b.Type, StringComparison.OrdinalIgnoreCase);

    private static string LayerNote(AecEntityRecord a, AecEntityRecord b) =>
        string.Equals(a.Layer, b.Layer, StringComparison.OrdinalIgnoreCase) ? "layer " + a.Layer : $"layers {a.Layer} / {b.Layer}";

    /// <summary>Same vertex sequence in either direction (or any rotation for rings), each vertex within the tolerance.</summary>
    internal static bool SameVertices(PlanShape a, PlanShape b, double tolerance)
    {
        if (a.Vertices.Count != b.Vertices.Count || a.Closed != b.Closed) return false;
        if (!a.Bounds.Expand(tolerance).ContainsXY(b.Bounds) || !b.Bounds.Expand(tolerance).ContainsXY(a.Bounds)) return false;
        var n = a.Vertices.Count;
        if (!a.Closed) return Aligned(a, b, i => i, tolerance) || Aligned(a, b, i => n - 1 - i, tolerance);
        for (var offset = 0; offset < n; offset++)
        {
            var o = offset;
            if (Aligned(a, b, i => (i + o) % n, tolerance) || Aligned(a, b, i => ((o - i) % n + n) % n, tolerance)) return true;
        }

        return false;
    }

    private static bool Aligned(PlanShape a, PlanShape b, Func<int, int> map, double tolerance)
    {
        for (var i = 0; i < a.Vertices.Count; i++)
            if (!a.Vertices[i].AlmostEqualsXY(b.Vertices[map(i)], tolerance)) return false;
        return true;
    }

    private static double MaxVertexDistance(PlanShape a, PlanShape b)
    {
        double max = 0;
        foreach (var v in a.Vertices) max = Math.Max(max, b.DistanceToBoundaryXY(v));
        return Math.Round(max, 2);
    }

    private static void Add(List<GeometryIssue> issues, string type, string severity, IReadOnlyList<string> handles, Pt? location, double? value, double? tolerance, string description, string? action) =>
        issues.Add(new GeometryIssue($"GEO-{issues.Count + 1:0000}", type, severity, handles, location?.Rounded(), value is null ? null : Math.Round(value.Value, 2), tolerance, description, action));
}
