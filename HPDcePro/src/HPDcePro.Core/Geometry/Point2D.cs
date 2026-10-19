using System;
using System.Globalization;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents an immutable 2D point with double-precision coordinates.
    /// Metric millimeters is the canonical internal unit.
    /// </summary>
    public readonly struct Point2D : IEquatable<Point2D>
    {
        public static readonly Point2D Zero = new Point2D(0.0, 0.0);

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

        public double DistanceToSquared(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        public Point2D MidPoint(Point2D other)
        {
            return new Point2D((X + other.X) * 0.5, (Y + other.Y) * 0.5);
        }

        public Point2D Offset(double dx, double dy)
        {
            return new Point2D(X + dx, Y + dy);
        }

        public Point2D Offset(Vector2D vector)
        {
            return new Point2D(X + vector.X, Y + vector.Y);
        }

        public Point2D OffsetX(double dx)
        {
            return new Point2D(X + dx, Y);
        }

        public Point2D OffsetY(double dy)
        {
            return new Point2D(X, Y + dy);
        }

        public Point2D Polar(double distance, double angleRadians)
        {
            return new Point2D(
                X + distance * Math.Cos(angleRadians),
                Y + distance * Math.Sin(angleRadians));
        }

        public static Point2D operator +(Point2D p, Vector2D v) => new Point2D(p.X + v.X, p.Y + v.Y);
        public static Point2D operator -(Point2D p, Vector2D v) => new Point2D(p.X - v.X, p.Y - v.Y);
        public static Vector2D operator -(Point2D a, Point2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);

        public static bool operator ==(Point2D a, Point2D b) => a.Equals(b);
        public static bool operator !=(Point2D a, Point2D b) => !a.Equals(b);

        public bool Equals(Point2D other)
        {
            return Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9;
        }

        public override bool Equals(object? obj) => obj is Point2D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F4}, {1:F4})", X, Y);
        }
    }
}
