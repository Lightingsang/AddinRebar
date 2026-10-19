using System;

namespace HPKata.Core.CadUtilities.Models
{
    /// <summary>
    /// Pure C# 2D point representation in millimeter space.
    /// </summary>
    public readonly struct Point2D : IEquatable<Point2D>
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Point2D Add(Vector2D vec) => new Point2D(X + vec.X, Y + vec.Y);
        public Vector2D Subtract(Point2D other) => new Vector2D(X - other.X, Y - other.Y);

        public bool Equals(Point2D other) =>
            Math.Abs(X - other.X) < 1e-6 && Math.Abs(Y - other.Y) < 1e-6;

        public override bool Equals(object? obj) => obj is Point2D other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Math.Round(X, 3), Math.Round(Y, 3));
        public override string ToString() => $"({X:F2}, {Y:F2})";

        public static Point2D operator +(Point2D p, Vector2D v) => p.Add(v);
        public static Vector2D operator -(Point2D p1, Point2D p2) => p1.Subtract(p2);
        public static bool operator ==(Point2D left, Point2D right) => left.Equals(right);
        public static bool operator !=(Point2D left, Point2D right) => !left.Equals(right);
    }
}
