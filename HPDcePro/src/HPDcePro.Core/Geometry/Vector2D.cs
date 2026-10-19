using System;
using System.Globalization;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents an immutable 2D vector with double-precision coordinates.
    /// </summary>
    public readonly struct Vector2D : IEquatable<Vector2D>
    {
        public static readonly Vector2D Zero = new Vector2D(0.0, 0.0);
        public static readonly Vector2D UnitX = new Vector2D(1.0, 0.0);
        public static readonly Vector2D UnitY = new Vector2D(0.0, 1.0);

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
            if (len < 1e-12)
            {
                return Zero;
            }
            return new Vector2D(X / len, Y / len);
        }

        public double Dot(Vector2D other) => X * other.X + Y * other.Y;

        public double Cross(Vector2D other) => X * other.Y - Y * other.X;

        public double AngleToOX
        {
            get
            {
                double angle = Math.Atan2(Y, X);
                if (angle < 0.0)
                {
                    angle += 2.0 * Math.PI;
                }
                return angle;
            }
        }

        public double AngleBetween(Vector2D other)
        {
            double l1 = Length;
            double l2 = other.Length;
            if (l1 < 1e-12 || l2 < 1e-12)
            {
                return 0.0;
            }
            double cos = Dot(other) / (l1 * l2);
            if (cos > 1.0) cos = 1.0;
            if (cos < -1.0) cos = -1.0;
            return Math.Acos(cos);
        }

        public Vector2D Perpendicular() => new Vector2D(-Y, X);

        public Vector2D Rotate(double radians)
        {
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return new Vector2D(X * cos - Y * sin, X * sin + Y * cos);
        }

        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
        public static Vector2D operator -(Vector2D v) => new Vector2D(-v.X, -v.Y);
        public static Vector2D operator *(Vector2D v, double scalar) => new Vector2D(v.X * scalar, v.Y * scalar);
        public static Vector2D operator *(double scalar, Vector2D v) => new Vector2D(v.X * scalar, v.Y * scalar);
        public static Vector2D operator /(Vector2D v, double scalar)
        {
            if (Math.Abs(scalar) < 1e-12)
            {
                throw new DivideByZeroException("Cannot divide vector by zero scalar.");
            }
            return new Vector2D(v.X / scalar, v.Y / scalar);
        }

        public static bool operator ==(Vector2D a, Vector2D b) => a.Equals(b);
        public static bool operator !=(Vector2D a, Vector2D b) => !a.Equals(b);

        public bool Equals(Vector2D other)
        {
            return Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9;
        }

        public override bool Equals(object? obj) => obj is Vector2D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "<{0:F4}, {1:F4}>", X, Y);
        }
    }
}
