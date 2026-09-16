namespace HPAutoCad.Aec.Geometry;

public enum IntersectionKind
{
    None,
    /// <summary>The segments cross or touch at one point.</summary>
    Point,
    /// <summary>Collinear segments sharing a run longer than the tolerance.</summary>
    Overlap,
}

/// <summary>
///     Plan-view (XY) geometry with explicit tolerances. Every comparison of floating-point geometry in
///     the engine goes through here; nothing else compares coordinates with <c>==</c>.
/// </summary>
public static class GeometryMath
{
    public const double DegreesPerRadian = 180.0 / Math.PI;

    /// <summary>
    ///     Intersection of two segments in the plan. Parallel non-collinear segments never intersect; collinear
    ///     segments intersect when their runs overlap by more than <paramref name="tolerance"/> (<see cref="IntersectionKind.Overlap"/>,
    ///     <paramref name="point"/> = middle of the shared run) or touch end to end (<see cref="IntersectionKind.Point"/>).
    /// </summary>
    public static IntersectionKind IntersectXY(Seg s, Seg t, double tolerance, out Pt point)
    {
        point = default;
        var r = s.B - s.A;
        var q = t.B - t.A;
        var rLength = r.LengthXY;
        var qLength = q.LengthXY;

        // A degenerate segment is a point: on the other segment within the tolerance or nowhere (same answer in both argument orders).
        if (rLength <= tolerance || qLength <= tolerance)
        {
            var (p, other) = rLength <= tolerance ? (s.A, t) : (t.A, s);
            if (other.DistanceXY(p) > tolerance) return IntersectionKind.None;
            point = p;
            return IntersectionKind.Point;
        }

        var denominator = r.CrossXY(q);
        var startDelta = t.A - s.A;

        // Parallel when the angle is so small that the lines never separate by more than the tolerance over the longer segment:
        // |r × q| = |r||q| sin θ, so sin θ ≤ tol / max(|r|, |q|). Nearly coincident lines then take the collinear branch instead of
        // "crossing" somewhere kilometres away.
        if (Math.Abs(denominator) <= rLength * qLength * (tolerance / Math.Max(rLength, qLength)))
        {
            // Parallel. Collinear only when t.A lies on the infinite line of s.
            var offAxis = Math.Abs(startDelta.CrossXY(r)) / rLength;
            if (offAxis > tolerance) return IntersectionKind.None;

            if (!CollinearOverlapXY(s, t, tolerance, out var from, out var to)) return IntersectionKind.None;
            var run = from.DistanceXY(to);
            point = Pt.Mid(from, to);
            return run > tolerance ? IntersectionKind.Overlap : IntersectionKind.Point;
        }

        var u = startDelta.CrossXY(q) / denominator; // along s
        var v = startDelta.CrossXY(r) / denominator; // along t
        var slackS = tolerance / rLength;
        var slackT = tolerance / qLength;
        if (u < -slackS || u > 1 + slackS || v < -slackT || v > 1 + slackT) return IntersectionKind.None;

        point = Pt.Lerp(s.A, s.B, Math.Clamp(u, 0, 1));
        return IntersectionKind.Point;
    }

    /// <summary>True when the crossing point is strictly inside both segments (not at an end within the tolerance).</summary>
    public static bool CrossesProperlyXY(Seg s, Seg t, double tolerance, out Pt point)
    {
        point = default;
        if (IntersectXY(s, t, tolerance, out var p) != IntersectionKind.Point) return false;
        if (IsEndpointXY(s, p, tolerance) || IsEndpointXY(t, p, tolerance)) return false;
        point = p;
        return true;
    }

    public static bool IsEndpointXY(Seg s, Pt p, double tolerance) => s.A.AlmostEqualsXY(p, tolerance) || s.B.AlmostEqualsXY(p, tolerance);

    /// <summary>Shared run of two collinear segments (caller guarantees collinearity); false when they do not touch.</summary>
    public static bool CollinearOverlapXY(Seg s, Seg t, double tolerance, out Pt from, out Pt to)
    {
        from = to = default;
        var direction = s.DirectionXY;
        if (direction.LengthXY <= double.Epsilon) return false;
        double Along(Pt p) => (p - s.A).DotXY(direction);

        var s0 = 0.0;
        var s1 = Along(s.B);
        var (tMin, tMax) = (Along(t.A), Along(t.B));
        if (tMin > tMax) (tMin, tMax) = (tMax, tMin);
        var (sMin, sMax) = s1 >= s0 ? (s0, s1) : (s1, s0);

        var start = Math.Max(sMin, tMin);
        var end = Math.Min(sMax, tMax);
        if (end < start - tolerance) return false;

        from = s.A + direction * start;
        to = s.A + direction * end;
        return true;
    }

    public static bool AreParallelXY(Seg s, Seg t, double angleToleranceDegrees)
    {
        var angle = AngleBetweenXY(s.DirectionXY, t.DirectionXY);
        return angle <= angleToleranceDegrees || Math.Abs(180 - angle) <= angleToleranceDegrees;
    }

    public static bool ArePerpendicularXY(Seg s, Seg t, double angleToleranceDegrees) =>
        Math.Abs(90 - AngleBetweenXY(s.DirectionXY, t.DirectionXY)) <= angleToleranceDegrees;

    /// <summary>Parallel and lying on the same line within the distance tolerance (either segment may be shorter).</summary>
    public static bool AreCollinearXY(Seg s, Seg t, double distanceTolerance, double angleToleranceDegrees)
    {
        if (!AreParallelXY(s, t, angleToleranceDegrees)) return false;
        var line = s.LengthXY >= t.LengthXY ? s : t;
        var other = s.LengthXY >= t.LengthXY ? t : s;
        return DistanceToLineXY(line, other.A) <= distanceTolerance && DistanceToLineXY(line, other.B) <= distanceTolerance;
    }

    /// <summary>Distance from a point to the infinite line through the segment.</summary>
    public static double DistanceToLineXY(Seg line, Pt p)
    {
        var length = line.LengthXY;
        if (length <= double.Epsilon) return line.A.DistanceXY(p);
        return Math.Abs((line.B - line.A).CrossXY(p - line.A)) / length;
    }

    /// <summary>Angle between two directions in degrees, 0..180.</summary>
    public static double AngleBetweenXY(Pt a, Pt b)
    {
        var la = a.LengthXY;
        var lb = b.LengthXY;
        if (la <= double.Epsilon || lb <= double.Epsilon) return 0;
        var cos = Math.Clamp(a.DotXY(b) / (la * lb), -1, 1);
        return Math.Acos(cos) * DegreesPerRadian;
    }

    /// <summary>Direction of a segment as a compass-free angle in degrees, 0 = +X, counter-clockwise, 0..360.</summary>
    public static double DirectionDegreesXY(Seg s)
    {
        var d = s.B - s.A;
        var degrees = Math.Atan2(d.Y, d.X) * DegreesPerRadian;
        return degrees < 0 ? degrees + 360 : degrees;
    }

    /// <summary>Signed shoelace area of a closed ring (positive = counter-clockwise); the last vertex need not repeat the first.</summary>
    public static double SignedAreaXY(IReadOnlyList<Pt> ring)
    {
        if (ring.Count < 3) return 0;
        double sum = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }

        return sum / 2;
    }

    public static double AreaXY(IReadOnlyList<Pt> ring) => Math.Abs(SignedAreaXY(ring));

    /// <summary>Below this area (mm²) a ring is treated as degenerate: the vertex mean is a better centroid than a division by ~0.</summary>
    public const double DegenerateAreaMm2 = 1e-6;

    /// <summary>Area centroid of a closed ring; falls back to the vertex mean for a degenerate (zero-area) ring.</summary>
    public static Pt CentroidXY(IReadOnlyList<Pt> ring)
    {
        if (ring.Count == 0) return Pt.Origin;
        var area = SignedAreaXY(ring);
        if (Math.Abs(area) <= DegenerateAreaMm2)
        {
            var mean = Pt.Origin;
            foreach (var p in ring) mean += p;
            return mean * (1.0 / ring.Count);
        }

        double cx = 0, cy = 0, z = 0;
        for (var i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            var cross = a.X * b.Y - b.X * a.Y;
            cx += (a.X + b.X) * cross;
            cy += (a.Y + b.Y) * cross;
            z += a.Z;
        }

        return new Pt(cx / (6 * area), cy / (6 * area), z / ring.Count);
    }

    /// <summary>Even-odd ray test; points on the boundary (within the tolerance) count as inside.</summary>
    public static bool ContainsPointXY(IReadOnlyList<Pt> ring, Pt p, double tolerance)
    {
        if (ring.Count < 3) return false;
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var a = ring[i];
            var b = ring[j];
            if (new Seg(a, b).DistanceXY(p) <= tolerance) return true;
            var crossesRay = a.Y > p.Y != b.Y > p.Y && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X;
            if (crossesRay) inside = !inside;
        }

        return inside;
    }
}
