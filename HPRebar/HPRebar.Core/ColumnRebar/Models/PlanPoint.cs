namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>A plan-only position in the column's local millimetre frame.</summary>
public readonly struct PlanPoint
{
    public PlanPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }

    public double Y { get; }

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
