namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>Tie spacing settings for one column segment. Millimetres.</summary>
public sealed record StirrupSpec
{
    /// <summary>
    /// Even spacing at <see cref="S"/>, or dense ends at <see cref="S1"/> around a sparse middle at
    /// <see cref="S2"/>.
    /// </summary>
    public TieLayout Layout { get; init; }

    /// <summary>Spacing for the even layout.</summary>
    public double S { get; init; }

    /// <summary>Spacing in the dense end zones.</summary>
    public double S1 { get; init; }

    /// <summary>Spacing in the sparse middle zone.</summary>
    public double S2 { get; init; }

    /// <summary>True carries the ties up through the beam depth instead of stopping under it.</summary>
    public bool IsTiesUp { get; init; }
}
