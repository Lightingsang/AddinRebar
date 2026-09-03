namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>One evenly spaced group of ties. Millimetres, offset measured from the base of the tie run.</summary>
public sealed record StirrupRun
{
    public int Count { get; init; }

    public double Spacing { get; init; }

    public double StartOffset { get; init; }
}
