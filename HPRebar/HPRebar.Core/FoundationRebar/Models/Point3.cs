using System;

namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// A 3D Cartesian point in millimetres representing coordinates in foundation space.
/// </summary>
public readonly struct Point3 : IEquatable<Point3>
{
    public Point3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static readonly Point3 Zero = new(0, 0, 0);

    public static Point3 operator +(Point3 p, Vector3 v) => new(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
    public static Point3 operator -(Point3 p, Vector3 v) => new(p.X - v.X, p.Y - v.Y, p.Z - v.Z);
    public static Vector3 operator -(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Point3 operator *(Point3 p, double scalar) => new(p.X * scalar, p.Y * scalar, p.Z * scalar);
    public static Point3 operator *(double scalar, Point3 p) => new(p.X * scalar, p.Y * scalar, p.Z * scalar);
    public static Point3 operator /(Point3 p, double scalar) => new(p.X / scalar, p.Y / scalar, p.Z / scalar);

    public double DistanceTo(Point3 other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool IsAlmostEqualTo(Point3 other, double tolerance = 1.0e-6) =>
        Math.Abs(X - other.X) <= tolerance &&
        Math.Abs(Y - other.Y) <= tolerance &&
        Math.Abs(Z - other.Z) <= tolerance;

    public void Deconstruct(out double x, out double y, out double z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public bool Equals(Point3 other) => IsAlmostEqualTo(other);
    public override bool Equals(object? obj) => obj is Point3 other && Equals(other);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";

    public static bool operator ==(Point3 left, Point3 right) => left.Equals(right);
    public static bool operator !=(Point3 left, Point3 right) => !left.Equals(right);
}
