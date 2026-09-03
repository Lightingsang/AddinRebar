namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>How many main bars go on the section and how thick everything is. Millimetres.</summary>
public sealed record BarLayoutSpec
{
    /// <summary>Bars along the south and north faces, corners included. Rectangular only; at least 2.</summary>
    public int Nx { get; init; } = 2;

    /// <summary>Bars along the west and east faces, corners included. Rectangular only; at least 2.</summary>
    public int Ny { get; init; } = 2;

    /// <summary>Bars around a circular section. Must be a positive multiple of four.</summary>
    public int Nd { get; init; } = 4;

    public double BarDiameter { get; init; }

    public double StirrupDiameter { get; init; }

    public double Cover { get; init; }

    /// <summary>Percentage of bars spliced at the same level. 50 staggers alternate bars to a double lap.</summary>
    public double SplitOverlap { get; init; } = 50;

    /// <summary>Lap length as a multiple of the bar diameter.</summary>
    public double OverlapFactor { get; init; } = 35;

    /// <summary>Total main bars this spec produces.</summary>
    public int BarCount =>
        Nd > 0 && Nx == 0 && Ny == 0 ? Nd : 2 * Nx + 2 * (Ny - 2);
}
