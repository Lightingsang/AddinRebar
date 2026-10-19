using System;

namespace HPKata.Revit.Abstractions
{
    /// <summary>
    /// Lightweight 3D point structure in Revit decimal feet for headless geometry calculations.
    /// Free of C++/CLI runtime dependencies, enabling fast execution in unit test harnesses.
    /// </summary>
    public readonly record struct RevitPoint3D(double X, double Y, double Z)
    {
        public static RevitPoint3D Zero => new(0.0, 0.0, 0.0);
        public static RevitPoint3D BasisZ => new(0.0, 0.0, 1.0);

        public static RevitPoint3D operator +(RevitPoint3D a, RevitPoint3D b) =>
            new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static RevitPoint3D operator -(RevitPoint3D a, RevitPoint3D b) =>
            new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static RevitPoint3D operator *(RevitPoint3D a, double scalar) =>
            new(a.X * scalar, a.Y * scalar, a.Z * scalar);

        public double DistanceTo(RevitPoint3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
