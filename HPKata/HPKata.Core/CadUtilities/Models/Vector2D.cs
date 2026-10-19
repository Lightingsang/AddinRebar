using System;

namespace HPKata.Core.CadUtilities.Models
{
    /// <summary>
    /// Pure C# 2D vector representation for direction, projection, and geometry operations.
    /// </summary>
    public readonly struct Vector2D : IEquatable<Vector2D>
    {
        public double X { get; }
        public double Y { get; }

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Length => Math.Sqrt(X * X + Y * Y);
        public double LengthSquared => X * X + Y * Y;

        public Vector2D Normalize()
        {
            double len = Length;
            return len > 1e-9 ? new Vector2D(X / len, Y / len) : new Vector2D(0, 0);
        }

        public double Dot(Vector2D other) => X * other.X + Y * other.Y;
        public double Cross(Vector2D other) => X * other.Y - Y * other.X;

        public Vector2D Normal() => new Vector2D(-Y, X); // 90 degrees CCW normal

        public Vector2D Scale(double factor) => new Vector2D(X * factor, Y * factor);

        public double AngleRad => Math.Atan2(Y, X);

        public bool Equals(Vector2D other) =>
            Math.Abs(X - other.X) < 1e-6 && Math.Abs(Y - other.Y) < 1e-6;

        public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Math.Round(X, 3), Math.Round(Y, 3));
        public override string ToString() => $"Vector2D({X:F2}, {Y:F2})";

        public static Vector2D operator *(Vector2D v, double s) => v.Scale(s);
        public static Vector2D operator *(double s, Vector2D v) => v.Scale(s);
        public static Vector2D operator +(Vector2D v1, Vector2D v2) => new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
        public static Vector2D operator -(Vector2D v1, Vector2D v2) => new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
        public static bool operator ==(Vector2D left, Vector2D right) => left.Equals(right);
        public static bool operator !=(Vector2D left, Vector2D right) => !left.Equals(right);
    }
}
