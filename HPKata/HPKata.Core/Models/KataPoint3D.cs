using System;

namespace HPKata.Core.Models
{
    /// <summary>
    /// Represents a 3D point in host-neutral metric millimeters (mm).
    /// </summary>
    public readonly record struct KataPoint3D(double X, double Y, double Z)
    {
        public static KataPoint3D Zero => new(0.0, 0.0, 0.0);

        public double DistanceTo(KataPoint3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public KataPoint3D Add(KataVector3D vector) =>
            new(X + vector.X, Y + vector.Y, Z + vector.Z);

        public KataVector3D Subtract(KataPoint3D other) =>
            new(X - other.X, Y - other.Y, Z - other.Z);

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    }
}
