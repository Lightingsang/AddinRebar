using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Structural;

/// <summary>One grid line: its axis in mm, the label read from a bubble at either end (or a text on the line), and its direction.</summary>
public sealed record GridLine(string Handle, string? Label, string Direction, Pt StartMm, Pt EndMm, double LengthMm, double DirectionDeg, string? BubbleHandle, int MergedSegments = 1)
{
    /// <summary>Lines along X are called <c>horizontal</c>, along Y <c>vertical</c>; anything else keeps its angle.</summary>
    public const string Horizontal = "horizontal";
    public const string Vertical = "vertical";
    public const string Skewed = "skewed";
}

/// <summary>Where two grid lines of different directions meet (extended by the reach so lines that stop short still intersect).</summary>
public sealed record GridIntersection(string A, string B, Pt PointMm, string HandleA, string HandleB);

/// <summary>The grid topology <c>structural_detect_grids</c> reports; <see cref="UnboundedCount"/> = XLINE/RAY grid objects the reader has no extent for.</summary>
public sealed record GridSystem(IReadOnlyList<GridLine> Lines, IReadOnlyList<GridIntersection> Intersections, IReadOnlyDictionary<string, IReadOnlyList<double>> SpacingMm, int UnboundedCount = 0);

/// <summary>
///     Grid topology from classified <c>StructuralGrid</c> objects: open runs are grid lines (collinear pieces — a line and the
///     stub that carries its bubble — merge into one), closed circles / block references are bubbles, a TEXT inside a bubble or
///     a block bubble's attribute is the label. A line is labelled by the bubble sitting on its axis nearest either end within
///     <see cref="BubbleReachMm"/>; intersections come from lines of different directions extended by the reach; spacing is the
///     sorted gaps between parallel lines. Pure.
/// </summary>
public static class GridDetector
{
    /// <summary>How far past a line's end a bubble (or the crossing line) may sit and still belong to it.</summary>
    public const double BubbleReachMm = 2500;

    /// <summary>A text this close to a line's midpoint (and no bubble) is the line's label written on it.</summary>
    public const double LabelOnLineReachMm = 50;

    /// <summary>A bubble whose extent is smaller than this still looks this far for its text.</summary>
    public const double MinBubbleLabelRadiusMm = 25;

    /// <summary>Labels longer than this are notes, not grid marks.</summary>
    public const int MaxLabelChars = 6;

    private sealed record Bubble(AecObject O, Pt Center, double Radius, string? Label);

    public static GridSystem Detect(IReadOnlyList<AecObject> gridObjects, AecClassifier.TextIndex texts, GeometryTolerance tol, double reachMm = BubbleReachMm)
    {
        var runs = new List<(AecObject O, Seg Axis)>();
        var bubbles = new List<Bubble>();
        var unbounded = 0;
        foreach (var o in gridObjects)
        {
            if (o.Shape is null) { if (o.Type.ToUpperInvariant() is "XLINE" or "RAY") unbounded++; continue; }
            if (o.Shape.IsPoint) continue;
            var isBlock = o.Type.Equals("INSERT", StringComparison.OrdinalIgnoreCase);
            if (!o.Shape.Closed && !isBlock && LongestSegment(o.Shape) is { } axis && axis.LengthXY > tol.TinySegment) runs.Add((o, axis));
            else if (o.Shape.Closed || isBlock)
            {
                var centre = o.Shape.Bounds.Center;
                var radius = o.Shape.Bounds.LongSideXY / 2;
                bubbles.Add(new Bubble(o, centre, radius, AttributeLabel(o) ?? LabelNear(texts, centre, Math.Max(radius, MinBubbleLabelRadiusMm))));
            }
        }

        var gridLines = new List<GridLine>();
        foreach (var (o, axis, pieces) in MergeCollinear(runs, tol))
        {
            var angle = GeometryMath.DirectionDegreesXY(axis) % 180;
            var direction = Math.Abs(angle) <= tol.ParallelAngle || Math.Abs(angle - 180) <= tol.ParallelAngle ? GridLine.Horizontal : Math.Abs(angle - 90) <= tol.ParallelAngle ? GridLine.Vertical : GridLine.Skewed;
            // The bubble must sit on the line's axis (within its own radius) and within reach of an end — a corner bubble of the crossing grid is not ours.
            var bubble = bubbles.Where(b => b.Label is not null && GeometryMath.DistanceToLineXY(axis, b.Center) <= Math.Max(b.Radius, tol.RoomGap))
                .Select(b => (b, d: Math.Min(b.Center.DistanceXY(axis.A), b.Center.DistanceXY(axis.B))))
                .Where(x => x.d <= reachMm).OrderBy(x => x.d).Select(x => x.b).FirstOrDefault();
            var label = bubble?.Label ?? LabelNear(texts, axis.Mid, LabelOnLineReachMm);
            gridLines.Add(new GridLine(o.Handle, label, direction, axis.A.Rounded(), axis.B.Rounded(), Math.Round(axis.LengthXY, 1), Math.Round(angle, 3), bubble?.O.Handle, pieces));
        }

        gridLines = gridLines.OrderBy(l => l.Direction, StringComparer.Ordinal).ThenBy(l => l.Direction == GridLine.Horizontal ? l.StartMm.Y : l.StartMm.X).ThenBy(l => l.Handle, StringComparer.Ordinal).ToList();

        var intersections = new List<GridIntersection>();
        for (var i = 0; i < gridLines.Count; i++)
            for (var j = i + 1; j < gridLines.Count; j++)
            {
                var a = gridLines[i];
                var b = gridLines[j];
                if (a.Direction == b.Direction && a.Direction != GridLine.Skewed) continue;
                var sa = Extend(new Seg(a.StartMm, a.EndMm), reachMm);
                var sb = Extend(new Seg(b.StartMm, b.EndMm), reachMm);
                if (GeometryMath.IntersectXY(sa, sb, tol.PointEquality, out var point) == IntersectionKind.Point && !intersections.Any(x => x.PointMm.AlmostEqualsXY(point, tol.PointEquality)))
                    intersections.Add(new GridIntersection(a.Label ?? a.Handle, b.Label ?? b.Handle, point.Rounded(), a.Handle, b.Handle));
            }

        var spacing = new Dictionary<string, IReadOnlyList<double>>();
        foreach (var group in gridLines.Where(l => l.Direction != GridLine.Skewed).GroupBy(l => l.Direction))
        {
            var positions = group.Select(l => l.Direction == GridLine.Horizontal ? l.StartMm.Y : l.StartMm.X).OrderBy(v => v).ToArray();
            spacing[group.Key] = positions.Zip(positions.Skip(1), (p, q) => Math.Round(q - p, 1)).ToArray();
        }

        return new GridSystem(gridLines, intersections, spacing, unbounded);
    }

    /// <summary>A jogged or L-shaped grid polyline follows its longest segment; a plain line is its only segment.</summary>
    private static Seg? LongestSegment(PlanShape shape)
    {
        if (shape.Vertices.Count < 2) return null;
        if (shape.Vertices.Count == 2) return new Seg(shape.Start, shape.End);
        return shape.Segments.OrderByDescending(s => s.LengthXY).First();
    }

    /// <summary>Pieces on one line (a grid line and the stub that carries its bubble) become one grid line spanning them all; the longest piece keeps the handle.</summary>
    private static IEnumerable<(AecObject O, Seg Axis, int Pieces)> MergeCollinear(List<(AecObject O, Seg Axis)> runs, GeometryTolerance tol)
    {
        var merged = new List<(AecObject O, Seg Axis, int Pieces)>();
        foreach (var run in runs.OrderByDescending(r => r.Axis.LengthXY).ThenBy(r => r.O.Handle, StringComparer.Ordinal))
        {
            var at = merged.FindIndex(m => GeometryMath.AreCollinearXY(m.Axis, run.Axis, tol.Collinearity, tol.ParallelAngle));
            if (at < 0) { merged.Add((run.O, run.Axis, 1)); continue; }
            var host = merged[at];
            merged[at] = (host.O, Span(host.Axis, run.Axis), host.Pieces + 1);
        }

        return merged;
    }

    /// <summary>The segment covering both collinear pieces, along the host's direction.</summary>
    private static Seg Span(Seg host, Seg piece)
    {
        var d = host.DirectionXY;
        var origin = host.A;
        Pt[] ends = [host.A, host.B, piece.A, piece.B];
        var lo = ends.Min(p => (p - origin).DotXY(d));
        var hi = ends.Max(p => (p - origin).DotXY(d));
        return new Seg(origin + d * lo, origin + d * hi);
    }

    /// <summary>A block bubble's label: its first short attribute value (the template's own tag name is unknown).</summary>
    private static string? AttributeLabel(AecObject o)
    {
        if (!o.Properties.TryGetValue("attributes", out var raw) || raw is not IDictionary<string, string> attributes) return null;
        return attributes.Values.Select(v => v.Trim()).FirstOrDefault(v => v.Length is > 0 and <= MaxLabelChars);
    }

    /// <summary>The short text nearest a point within the radius — a grid mark, not a note.</summary>
    private static string? LabelNear(AecClassifier.TextIndex texts, Pt at, double radiusMm)
    {
        var area = Box.Of(new Pt(at.X - radiusMm, at.Y - radiusMm), new Pt(at.X + radiusMm, at.Y + radiusMm));
        return texts.Near(area, 0)
            .Where(t => t.Text is { Length: > 0 and <= MaxLabelChars } && (t.PositionMm ?? t.BoundsMm?.Center) is { } p && p.DistanceXY(at) <= radiusMm)
            .OrderBy(t => (t.PositionMm ?? t.BoundsMm!.Value.Center).DistanceXY(at))
            .Select(t => t.Text!.Trim())
            .FirstOrDefault();
    }

    private static Seg Extend(Seg s, double byMm)
    {
        var d = s.DirectionXY;
        return new Seg(s.A - d * byMm, s.B + d * byMm);
    }
}
