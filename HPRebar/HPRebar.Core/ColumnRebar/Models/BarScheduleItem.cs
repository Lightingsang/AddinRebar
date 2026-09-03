namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>One row of the bar schedule: a group of identical bars. Millimetres.</summary>
public sealed record BarScheduleItem
{
    public string Name { get; init; } = "L1";

    /// <summary>Bars in this row, already multiplied by the number of identical columns.</summary>
    public int Count { get; init; }

    public double Diameter { get; init; }

    public BarShape Shape { get; init; }

    /// <summary>Main leg length.</summary>
    public double L { get; init; }

    /// <summary>Lower bend length.</summary>
    public double La { get; init; }

    /// <summary>Upper bend length.</summary>
    public double Lb { get; init; }

    /// <summary>Extra bottom hook, only set for the DS07A/DS07B transition shapes.</summary>
    public double L1 { get; init; }

    /// <summary>Rise of the transition bend. Zero for every straight shape.</summary>
    public double SlopeX { get; init; }

    /// <summary>Run of the transition bend. Zero for every straight shape.</summary>
    public double SlopeY { get; init; }

    /// <summary>Cut length.</summary>
    public double Length => L + La + Lb;
}
