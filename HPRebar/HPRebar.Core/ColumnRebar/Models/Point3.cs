namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>A point in the column's local millimetre frame: X east, Y north, Z up from the datum face.</summary>
public readonly struct Point3
{
    public Point3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Millimetres east of the datum.</summary>
    public double X { get; }

    /// <summary>Millimetres north of the datum.</summary>
    public double Y { get; }

    /// <summary>Millimetres above the datum.</summary>
    public double Z { get; }

    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
}
