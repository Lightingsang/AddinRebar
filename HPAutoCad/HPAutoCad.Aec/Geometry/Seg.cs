namespace HPAutoCad.Aec.Geometry;

/// <summary>
///     A straight segment in millimetres. Curves reach the engine as chains of these (arcs and circles
///     tessellated to a chord tolerance), so every predicate has one primitive to reason about.
/// </summary>
public readonly record struct Seg(Pt A, Pt B)
{
    public double Length => A.Distance(B);

    public double LengthXY => A.DistanceXY(B);

    public Pt Mid => Pt.Mid(A, B);

    /// <summary>Unit direction in the plan; zero for a degenerate segment.</summary>
    public Pt DirectionXY => (B - A).NormalizedXY();

    public Box Bounds => Box.Of(A, B);

    public bool IsDegenerate(double tolerance) => LengthXY <= tolerance;

    /// <summary>Parameter 0..1 of the closest point on the segment to <paramref name="p"/> (plan view).</summary>
    public double ClosestParameterXY(Pt p)
    {
        var d = B - A;
        var lengthSquared = d.X * d.X + d.Y * d.Y;
        if (lengthSquared <= double.Epsilon) return 0;
        var t = ((p.X - A.X) * d.X + (p.Y - A.Y) * d.Y) / lengthSquared;
        return Math.Clamp(t, 0, 1);
    }

    public Pt ClosestPointXY(Pt p) => Pt.Lerp(A, B, ClosestParameterXY(p));

    public double DistanceXY(Pt p) => ClosestPointXY(p).DistanceXY(p);

    /// <summary>Shortest plan distance between two segments; 0 when they cross.</summary>
    public double DistanceXY(Seg other)
    {
        if (GeometryMath.IntersectXY(this, other, 0, out _) != IntersectionKind.None) return 0;
        return Math.Min(
            Math.Min(DistanceXY(other.A), DistanceXY(other.B)),
            Math.Min(other.DistanceXY(A), other.DistanceXY(B)));
    }

    public Seg Reversed() => new(B, A);
}
