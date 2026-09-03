namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>Tie spacing settings for one column segment. Millimetres.</summary>
public sealed record StirrupSpec
{
    /// <summary>
    ///     0 spaces ties evenly at <see cref="S"/> over the whole run. 1, 2 and 3 split the run into a
    ///     dense/sparse/dense pattern of growing middle length.
    /// </summary>
    public int TypeDis { get; init; }

    /// <summary>Spacing for the even layout.</summary>
    public double S { get; init; }

    /// <summary>Spacing in the dense end zones.</summary>
    public double S1 { get; init; }

    /// <summary>Spacing in the sparse middle zone.</summary>
    public double S2 { get; init; }

    /// <summary>True carries the ties up through the beam depth instead of stopping under it.</summary>
    public bool IsTiesUp { get; init; }
}
