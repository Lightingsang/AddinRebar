namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>Plan position of one main bar, millimetres from the stack datum.</summary>
public sealed record BarPosition
{
    /// <summary>One-based, running clockwise from the south-west corner.</summary>
    public int BarNumber { get; init; }

    public double X0 { get; init; }

    public double Y0 { get; init; }

    /// <summary>Face this bar was laid out on. Corners belong to the face that claimed them first.</summary>
    public BarSide Side { get; init; }
}
