using System.Text.Json.Serialization;

namespace HPAutoCad.Aec.Geometry;

/// <summary>
///     Axis-aligned bounding box in millimetres. The broad phase of every spatial operation: cheap to
///     compare, indexable, and what AutoCAD's <c>GeometricExtents</c> gives for free. <see cref="Empty"/>
///     is the identity of <see cref="Union"/>; it contains nothing and intersects nothing.
/// </summary>
public readonly record struct Box(Pt Min, Pt Max)
{
    public static readonly Box Empty = new(new Pt(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity),
        new Pt(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity));

    [JsonIgnore]
    public bool IsEmpty => Min.X > Max.X || Min.Y > Max.Y;

    [JsonIgnore]
    public double Width => IsEmpty ? 0 : Max.X - Min.X;

    [JsonIgnore]
    public double Height => IsEmpty ? 0 : Max.Y - Min.Y;

    [JsonIgnore]
    public double Depth => IsEmpty ? 0 : Max.Z - Min.Z;

    [JsonIgnore]
    public double AreaXY => Width * Height;

    [JsonIgnore]
    public Pt Center => IsEmpty ? Pt.Origin : Pt.Mid(Min, Max);

    /// <summary>The longer plan side — what "size" means for a column outline or a block footprint.</summary>
    [JsonIgnore]
    public double LongSideXY => Math.Max(Width, Height);

    [JsonIgnore]
    public double ShortSideXY => Math.Min(Width, Height);

    /// <summary>Half the plan diagonal — the radius of the smallest circle around the box's centre that covers it.</summary>
    [JsonIgnore]
    public double HalfDiagonalXY => Math.Sqrt(Width * Width + Height * Height) / 2;

    public static Box Of(Pt a, Pt b) => new(
        new Pt(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z)),
        new Pt(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z)));

    public static Box Of(IEnumerable<Pt> points)
    {
        var box = Empty;
        foreach (var p in points) box = box.Include(p);
        return box;
    }

    public Box Include(Pt p) => new(
        new Pt(Math.Min(Min.X, p.X), Math.Min(Min.Y, p.Y), Math.Min(Min.Z, p.Z)),
        new Pt(Math.Max(Max.X, p.X), Math.Max(Max.Y, p.Y), Math.Max(Max.Z, p.Z)));

    public Box Union(Box other) => other.IsEmpty ? this : IsEmpty ? other : Include(other.Min).Include(other.Max);

    /// <summary>Grown (or shrunk with a negative value) by the same amount on every side in the plan.</summary>
    public Box Expand(double byMm) => IsEmpty ? this : new Box(new Pt(Min.X - byMm, Min.Y - byMm, Min.Z), new Pt(Max.X + byMm, Max.Y + byMm, Max.Z));

    /// <summary>True when the boxes are at least <paramref name="distance"/> apart along x or along y — a cheap, conservative "farther than" test (no square root).</summary>
    public bool SeparatedByXY(Box other, double distance) =>
        other.Min.X - Max.X >= distance || Min.X - other.Max.X >= distance || other.Min.Y - Max.Y >= distance || Min.Y - other.Max.Y >= distance;

    /// <summary>Plan-view overlap test; touching edges count as intersecting when the tolerance is ≥ 0.</summary>
    public bool IntersectsXY(Box other, double tolerance = 0) =>
        !IsEmpty && !other.IsEmpty
        && Min.X <= other.Max.X + tolerance && other.Min.X <= Max.X + tolerance
        && Min.Y <= other.Max.Y + tolerance && other.Min.Y <= Max.Y + tolerance;

    public bool ContainsXY(Pt p, double tolerance = 0) =>
        !IsEmpty && p.X >= Min.X - tolerance && p.X <= Max.X + tolerance && p.Y >= Min.Y - tolerance && p.Y <= Max.Y + tolerance;

    public bool ContainsXY(Box other, double tolerance = 0) => !other.IsEmpty && ContainsXY(other.Min, tolerance) && ContainsXY(other.Max, tolerance);

    /// <summary>Shortest plan distance between the two boxes; 0 when they overlap.</summary>
    public double DistanceXY(Box other)
    {
        if (IsEmpty || other.IsEmpty) return double.PositiveInfinity;
        var dx = Math.Max(0, Math.Max(other.Min.X - Max.X, Min.X - other.Max.X));
        var dy = Math.Max(0, Math.Max(other.Min.Y - Max.Y, Min.Y - other.Max.Y));
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public Box Rounded(int decimals = 1) => IsEmpty ? this : new Box(Min.Rounded(decimals), Max.Rounded(decimals));
}
