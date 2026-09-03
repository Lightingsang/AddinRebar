using System.Collections.Generic;

namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>The centre-line of one main bar as an ordered point list, bottom end first. Millimetres.</summary>
public sealed record BarPolyline
{
    public int BarNumber { get; init; }

    public double Diameter { get; init; }

    public IReadOnlyList<Point3> Points { get; init; } = new List<Point3>();

    public SpliceSpec Splice { get; init; } = new();

    /// <summary>Bar-type name, used only to keep unlike bars out of the same schedule row.</summary>
    public string BarTypeName { get; init; } = string.Empty;
}
