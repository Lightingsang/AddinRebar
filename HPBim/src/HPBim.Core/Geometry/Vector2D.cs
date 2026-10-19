namespace HPBim.Core.Geometry;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an immutable 2D vector for directional and displacement math.
/// </summary>
public readonly struct Vector2D : IEquatable<Vector2D>
{
    [JsonConstructor]
    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }

    public static Vector2D Zero => new(0.0, 0.0);
    public static Vector2D UnitX => new(1.0, 0.0);
    public static Vector2D UnitY => new(0.0, 1.0);

    [JsonIgnore]
    public double Length => Math.Sqrt(LengthSquared);

    [JsonIgnore]
    public double LengthSquared => X * X + Y * Y;

    [JsonIgnore]
    public double SquaredLength => LengthSquared;

    public static Vector2D FromPoints(Point2D start, Point2D end) => new(end.X - start.X, end.Y - start.Y);

    public bool IsZero(double tolerance = 1e-9) => LengthSquared <= tolerance * tolerance;

    public Vector2D Normalize()
    {
        double len = Length;
        if (len < 1e-9)
        {
            throw new InvalidOperationException("Cannot normalize a zero-length vector.");
        }
        return new Vector2D(X / len, Y / len);
    }

    public bool TryNormalize(out Vector2D unit, double tolerance = 1e-9)
    {
        double len = Length;
        if (len <= tolerance)
        {
            unit = Zero;
            return false;
        }
        unit = new Vector2D(X / len, Y / len);
        return true;
    }

    /// <summary>
    /// Gets the left-hand perpendicular normal vector (-Y, X) rotating 90 degrees counter-clockwise.
    /// Used for wall offset direction (left normal).
    /// </summary>
    [JsonIgnore]
    public Vector2D LeftNormal => new(-Y, X);

    /// <summary>
    /// Gets the right-hand perpendicular normal vector (Y, -X) rotating 90 degrees clockwise.
    /// </summary>
    [JsonIgnore]
    public Vector2D RightNormal => new(Y, -X);

    public double Dot(Vector2D other) => X * other.X + Y * other.Y;

    public double Cross(Vector2D other) => X * other.Y - Y * other.X;

    [JsonIgnore]
    public double AngleRad => Math.Atan2(Y, X);

    [JsonIgnore]
    public double AngleDeg => AngleRad * (180.0 / Math.PI);

    public double AngleBetween(Vector2D other)
    {
        double cross = Cross(other);
        double dot = Dot(other);
        return Math.Atan2(cross, dot);
    }

    public bool IsAlmostEqualTo(Vector2D other, double tolerance = 1e-6)
    {
        return Math.Abs(X - other.X) <= tolerance && Math.Abs(Y - other.Y) <= tolerance;
    }

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator *(Vector2D v, double scalar) => new(v.X * scalar, v.Y * scalar);
    public static Vector2D operator *(double scalar, Vector2D v) => new(v.X * scalar, v.Y * scalar);
    public static Vector2D operator /(Vector2D v, double scalar)
    {
        if (Math.Abs(scalar) < 1e-12)
        {
            throw new DivideByZeroException("Vector scalar divisor cannot be zero.");
        }
        return new Vector2D(v.X / scalar, v.Y / scalar);
    }
    public static Vector2D operator -(Vector2D v) => new(-v.X, -v.Y);

    public static bool operator ==(Vector2D left, Vector2D right) => left.Equals(right);
    public static bool operator !=(Vector2D left, Vector2D right) => !left.Equals(right);

    public bool Equals(Vector2D other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (X.GetHashCode() * 397) ^ Y.GetHashCode();
        }
    }

    public override string ToString() => $"Vector2D({X:0.###}, {Y:0.###})";
}
