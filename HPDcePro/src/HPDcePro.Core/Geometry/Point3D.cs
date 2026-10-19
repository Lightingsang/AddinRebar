using System;
using System.Globalization;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents an immutable 3D point with double-precision coordinates.
    /// </summary>
    public readonly struct Point3D : IEquatable<Point3D>
    {
        public static readonly Point3D Zero = new Point3D(0.0, 0.0, 0.0);

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public Point3D MidPoint(Point3D other)
        {
            return new Point3D((X + other.X) * 0.5, (Y + other.Y) * 0.5, (Z + other.Z) * 0.5);
        }

        public Point3D Offset(double dx, double dy, double dz)
        {
            return new Point3D(X + dx, Y + dy, Z + dz);
        }

        public static Point3D operator +(Point3D p, Vector3D v) => new Point3D(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
        public static Point3D operator -(Point3D p, Vector3D v) => new Point3D(p.X - v.X, p.Y - v.Y, p.Z - v.Z);
        public static Vector3D operator -(Point3D a, Point3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static bool operator ==(Point3D a, Point3D b) => a.Equals(b);
        public static bool operator !=(Point3D a, Point3D b) => !a.Equals(b);

        public bool Equals(Point3D other)
        {
            return Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9 && Math.Abs(Z - other.Z) < 1e-9;
        }

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

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F4}, {1:F4}, {2:F4})", X, Y, Z);
        }
    }
}
