namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A 3D Cartesian vector in millimetres or unit space.
/// </summary>
public readonly struct Vector3 : System.IEquatable<Vector3>
{
    public Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    public double Length => System.Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSquared => X * X + Y * Y + Z * Z;

    public Vector3 Normalize()
    {
        double len = Length;
        return len < 1.0e-9 ? Zero : new Vector3(X / len, Y / len, Z / len);
    }

    public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3 Cross(Vector3 other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);
    public static Vector3 operator *(Vector3 v, double scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
    public static Vector3 operator *(double scalar, Vector3 v) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);
    public static Vector3 operator /(Vector3 v, double scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public bool Equals(Vector3 other) =>
        System.Math.Abs(X - other.X) <= 1.0e-9 &&
        System.Math.Abs(Y - other.Y) <= 1.0e-9 &&
        System.Math.Abs(Z - other.Z) <= 1.0e-9;

    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"[{X:0.###}, {Y:0.###}, {Z:0.###}]";

    public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
    public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);
}
