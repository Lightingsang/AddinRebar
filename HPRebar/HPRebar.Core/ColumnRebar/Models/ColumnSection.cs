namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>
///     One column stack segment, already reduced to numbers. Every length is millimetres measured from the
///     stack datum (the top face of whatever supports the bottom column), so segments can be compared directly.
/// </summary>
public sealed record ColumnSection
{
    /// <summary>Zero-based position of this segment in the stack, bottom first.</summary>
    public int Index { get; init; }

    public SectionShape Shape { get; init; }

    /// <summary>Width, east-west. Rectangular only.</summary>
    public double B { get; init; }

    /// <summary>Depth, north-south. Rectangular only.</summary>
    public double H { get; init; }

    /// <summary>Diameter. Circular only.</summary>
    public double D { get; init; }

    /// <summary>Height of the column segment itself.</summary>
    public double Hc { get; init; }

    /// <summary>Depth of the deepest beam framing into the top of this segment. Zero when no beam is found.</summary>
    public double Hb { get; init; }

    /// <summary>Distance from the segment top face down to the highest beam face. Zero when no beam is found.</summary>
    public double Zb { get; init; }

    public double TopPosition { get; init; }

    public double BottomPosition { get; init; }

    public double WestPosition { get; init; }

    public double EastPosition { get; init; }

    public double SouthPosition { get; init; }

    public double NorthPosition { get; init; }

    /// <summary>Centre of a circular section. Unused for rectangles.</summary>
    public double CenterX { get; init; }

    /// <summary>Centre of a circular section. Unused for rectangles.</summary>
    public double CenterY { get; init; }

    /// <summary>
    ///     Depth the top bars must start bending below the segment top so they clear the beam.
    ///     Falls back to the largest plan dimension when no beam was found, matching the source tool.
    /// </summary>
    public double BendDepth =>
        Tolerance.AreEqual(Zb + Hb, 0d)
            ? Shape == SectionShape.Rectangle ? System.Math.Max(B, H) : D
            : Zb + Hb;
}
