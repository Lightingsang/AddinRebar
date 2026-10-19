using System;

namespace HPKata.Core.Models
{
    /// <summary>
    /// Represents a 3D vector in host-neutral metric coordinates.
    /// </summary>
    public readonly record struct KataVector3D(double X, double Y, double Z)
    {
        public static KataVector3D Zero => new(0.0, 0.0, 0.0);
        public static KataVector3D UnitX => new(1.0, 0.0, 0.0);
        public static KataVector3D UnitY => new(0.0, 1.0, 0.0);
        public static KataVector3D UnitZ => new(0.0, 0.0, 1.0);

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public KataVector3D Normalize()
        {
            double len = Length;
            return len > 1e-9 ? new KataVector3D(X / len, Y / len, Z / len) : Zero;
        }

        public double DotProduct(KataVector3D other) =>
            X * other.X + Y * other.Y + Z * other.Z;

        public KataVector3D CrossProduct(KataVector3D other) =>
            new(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X);

        public KataVector3D Multiply(double scalar) =>
            new(X * scalar, Y * scalar, Z * scalar);

        public override string ToString() => $"<{X:F3}, {Y:F3}, {Z:F3}>";
    }
}
