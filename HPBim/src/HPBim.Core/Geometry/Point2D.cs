namespace HPBim.Core.Geometry;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an immutable 2D point in millimeter Cartesian coordinates.
/// </summary>
public readonly struct Point2D : IEquatable<Point2D>
{
    [JsonConstructor]
    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public static Point2D Zero => new(0.0, 0.0);

    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }

    public double DistanceTo(Point2D other) => Math.Sqrt(DistanceSquaredTo(other));

    public double DistanceSquaredTo(Point2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return dx * dx + dy * dy;
    }

    public Point2D Translate(double dx, double dy) => new(X + dx, Y + dy);

    public Point2D Translate(Vector2D offset) => new(X + offset.X, Y + offset.Y);

    public Point3D ToPoint3D(double z = 0.0) => new(X, Y, z);

    public bool IsAlmostEqualTo(Point2D other, double tolerance = 1e-6)
    {
        return Math.Abs(X - other.X) <= tolerance && Math.Abs(Y - other.Y) <= tolerance;
    }

    public static Point2D operator +(Point2D point, Vector2D vector) => new(point.X + vector.X, point.Y + vector.Y);

    public static Point2D operator -(Point2D point, Vector2D vector) => new(point.X - vector.X, point.Y - vector.Y);

    public static Vector2D operator -(Point2D end, Point2D start) => new(end.X - start.X, end.Y - start.Y);

    public static bool operator ==(Point2D left, Point2D right) => left.Equals(right);

    public static bool operator !=(Point2D left, Point2D right) => !left.Equals(right);

    public bool Equals(Point2D other) => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) => obj is Point2D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
