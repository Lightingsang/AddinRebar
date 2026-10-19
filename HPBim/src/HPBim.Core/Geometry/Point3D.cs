namespace HPBim.Core.Geometry;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an immutable 3D point in millimeter Cartesian coordinates.
/// </summary>
public readonly struct Point3D : IEquatable<Point3D>
{
    [JsonConstructor]
    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static Point3D Zero => new(0.0, 0.0, 0.0);

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public double DistanceTo(Point3D other) => Math.Sqrt(DistanceSquaredTo(other));

    public double DistanceSquaredTo(Point3D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    public double Distance2DTo(Point3D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public Point2D ToPoint2D() => new(X, Y);

    public Point3D WithZ(double z) => new(X, Y, z);

    public Point3D Translate(double dx, double dy, double dz) => new(X + dx, Y + dy, Z + dz);

    public bool IsAlmostEqualTo(Point3D other, double tolerance = 1e-6)
    {
        return Math.Abs(X - other.X) <= tolerance &&
               Math.Abs(Y - other.Y) <= tolerance &&
               Math.Abs(Z - other.Z) <= tolerance;
    }

    public static bool operator ==(Point3D left, Point3D right) => left.Equals(right);

    public static bool operator !=(Point3D left, Point3D right) => !left.Equals(right);

    public bool Equals(Point3D other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Z.GetHashCode();
            return hash;
        }
    }

    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
}
