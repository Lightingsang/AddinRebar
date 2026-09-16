using System.Text.Json.Serialization;

namespace HPAutoCad.Aec.Geometry;

/// <summary>
///     A point or vector in millimetres. Plans are analysed in XY; Z is carried so elevations survive a
///     round trip but the predicates are 2-D unless they say otherwise. Serialises as <c>{x, y, z}</c>.
/// </summary>
public readonly record struct Pt(double X, double Y, double Z = 0)
{
    public static readonly Pt Origin = new(0, 0, 0);

    public static Pt operator +(Pt a, Pt b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Pt operator -(Pt a, Pt b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Pt operator *(Pt a, double k) => new(a.X * k, a.Y * k, a.Z * k);

    [JsonIgnore]
    public double LengthXY => Math.Sqrt(X * X + Y * Y);

    [JsonIgnore]
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public double DistanceXY(Pt other) => (this - other).LengthXY;

    public double Distance(Pt other) => (this - other).Length;

    /// <summary>2-D cross product (signed area of the parallelogram) — the sign tells left/right of a direction.</summary>
    public double CrossXY(Pt other) => X * other.Y - Y * other.X;

    public double DotXY(Pt other) => X * other.X + Y * other.Y;

    /// <summary>Unit vector in XY; the zero vector stays zero rather than becoming NaN.</summary>
    public Pt NormalizedXY()
    {
        var length = LengthXY;
        return length <= double.Epsilon ? new Pt(0, 0, 0) : new Pt(X / length, Y / length, 0);
    }

    public static Pt Mid(Pt a, Pt b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);

    public static Pt Lerp(Pt a, Pt b, double t) => a + (b - a) * t;

    /// <summary>Same point in the plan within the tolerance (XY only — a column base and top share a plan position).</summary>
    public bool AlmostEqualsXY(Pt other, double tolerance) => DistanceXY(other) <= tolerance;

    public Pt Rounded(int decimals = 1) => new(Math.Round(X, decimals), Math.Round(Y, decimals), Math.Round(Z, decimals));

    public override string ToString() => FormattableString.Invariant($"({X:0.###}, {Y:0.###}, {Z:0.###})");
}
