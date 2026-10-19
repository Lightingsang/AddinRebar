using System;
using System.Globalization;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents an immutable 3D vector with double-precision coordinates.
    /// </summary>
    public readonly struct Vector3D : IEquatable<Vector3D>
    {
        public static readonly Vector3D Zero = new Vector3D(0.0, 0.0, 0.0);
        public static readonly Vector3D UnitX = new Vector3D(1.0, 0.0, 0.0);
        public static readonly Vector3D UnitY = new Vector3D(0.0, 1.0, 0.0);
        public static readonly Vector3D UnitZ = new Vector3D(0.0, 0.0, 1.0);

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double LengthSquared => X * X + Y * Y + Z * Z;

        public Vector3D Normalize()
        {
            double len = Length;
            if (len < 1e-12) return Zero;
            return new Vector3D(X / len, Y / len, Z / len);
        }

        public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

        public Vector3D Cross(Vector3D other)
        {
            return new Vector3D(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X);
        }

        public double AngleBetween(Vector3D other)
        {
            double l1 = Length;
            double l2 = other.Length;
            if (l1 < 1e-12 || l2 < 1e-12) return 0.0;
            double cos = Dot(other) / (l1 * l2);
            if (cos > 1.0) cos = 1.0;
            if (cos < -1.0) cos = -1.0;
            return Math.Acos(cos);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator -(Vector3D v) => new Vector3D(-v.X, -v.Y, -v.Z);
        public static Vector3D operator *(Vector3D v, double s) => new Vector3D(v.X * s, v.Y * s, v.Z * s);
        public static Vector3D operator *(double s, Vector3D v) => new Vector3D(v.X * s, v.Y * s, v.Z * s);
        public static Vector3D operator /(Vector3D v, double s)
        {
            if (Math.Abs(s) < 1e-12) throw new DivideByZeroException("Cannot divide vector by zero.");
            return new Vector3D(v.X / s, v.Y / s, v.Z / s);
        }

        public static bool operator ==(Vector3D a, Vector3D b) => a.Equals(b);
        public static bool operator !=(Vector3D a, Vector3D b) => !a.Equals(b);

        public bool Equals(Vector3D other)
        {
            return Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9 && Math.Abs(Z - other.Z) < 1e-9;
        }

        public override bool Equals(object? obj) => obj is Vector3D other && Equals(other);

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

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "<{0:F4}, {1:F4}, {2:F4}>", X, Y, Z);
        }
    }
}
