using System;

namespace HPDcePro.Core.Geometry
{
    /// <summary>
    /// Represents a 3D plane with equation Ax + By + Cz + D = 0, where (A, B, C) is the unit normal vector.
    /// </summary>
    public readonly struct Plane3D : IEquatable<Plane3D>
    {
        public Vector3D Normal { get; }
        public double D { get; }

        public double A => Normal.X;
        public double B => Normal.Y;
        public double C => Normal.Z;

        public Plane3D(Vector3D normal, double d)
        {
            Normal = normal.Normalize();
            D = d;
        }

        public static Plane3D From3Points(Point3D p1, Point3D p2, Point3D p3)
        {
            Vector3D v1 = p2 - p1;
            Vector3D v2 = p3 - p1;
            Vector3D normal = v1.Cross(v2).Normalize();
            if (normal.Length < 1e-12)
            {
                throw new ArgumentException("The three points are collinear and cannot define a plane.");
            }
            double d = -(normal.X * p1.X + normal.Y * p1.Y + normal.Z * p1.Z);
            return new Plane3D(normal, d);
        }

        public double SignedDistance(Point3D pt)
        {
            return Normal.X * pt.X + Normal.Y * pt.Y + Normal.Z * pt.Z + D;
        }

        public double AbsoluteDistance(Point3D pt)
        {
            return Math.Abs(SignedDistance(pt));
        }

        public double AngleBetween(Plane3D other)
        {
            return Normal.AngleBetween(other.Normal);
        }

        public bool IntersectLine(Point3D linePt, Vector3D lineDir, out Point3D intersection, double tol = 1e-9)
        {
            intersection = Point3D.Zero;
            double denominator = Normal.Dot(lineDir);
            if (Math.Abs(denominator) < tol)
            {
                return false;
            }

            double t = -(Normal.X * linePt.X + Normal.Y * linePt.Y + Normal.Z * linePt.Z + D) / denominator;
            intersection = linePt + lineDir * t;
            return true;
        }

        public bool Equals(Plane3D other)
        {
            return Normal == other.Normal && Math.Abs(D - other.D) < 1e-9;
        }

        public override bool Equals(object? obj) => obj is Plane3D other && Equals(other);
        public override int GetHashCode() => (Normal.GetHashCode() * 397) ^ D.GetHashCode();
        public static bool operator ==(Plane3D a, Plane3D b) => a.Equals(b);
        public static bool operator !=(Plane3D a, Plane3D b) => !a.Equals(b);

        public override string ToString() => $"Plane3D[{A:F4}x + {B:F4}y + {C:F4}z + {D:F4} = 0]";
    }
}
