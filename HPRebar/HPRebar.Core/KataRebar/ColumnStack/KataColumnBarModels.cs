using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>A point of a bar in the column's own frame (X along b, Y along h, Z elevation), in mm.</summary>
public readonly record struct KataPoint3(double X, double Y, double Z);

/// <summary>
/// One storey of the real column: elevations and the section's extents in the column frame, in mm. The rebar plan
/// places bars on these faces; the sheet only gives counts, diameters and lap positions.
/// </summary>
public sealed record KataColumnStoreyGeometry(double BaseMm, double TopMm, double MinXMm, double MaxXMm, double MinYMm, double MaxYMm);

public enum KataColumnBarGroup
{
    /// <summary>First half of the bars (staggered laps): laps start at the storey base + J.</summary>
    A,

    /// <summary>Second half: laps start 40 bar diameters above group A's.</summary>
    B
}

public enum KataColumnBarKind
{
    /// <summary>In the pile cap, foot at the cap bottom, up to the lap in the first storey.</summary>
    Starter,

    /// <summary>From its lap in one storey to the lap end in the storey above.</summary>
    Main,

    /// <summary>Main bar of the top storey ending under the roof with an anchorage leg.</summary>
    Top
}

/// <summary>One bar of the plan, as a polyline of its centre line.</summary>
/// <param name="Storey">Index of the storey whose section holds the bar (0 for starters).</param>
public sealed record KataColumnBarRun(
    KataColumnBarKind Kind,
    int Storey,
    KataColumnBarGroup Group,
    int Layer,
    double DiameterMm,
    IReadOnlyList<KataPoint3> Points);

/// <summary>The whole longitudinal plan plus what had to be decided (steps cranked or cut, J missing).</summary>
public sealed record KataColumnBarPlan(IReadOnlyList<KataColumnBarRun> Bars, IReadOnlyList<string> Notes, IReadOnlyList<string> Errors);
