using System;

namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// Immutable snapshot of foundation slab geometry and its local orthonormal coordinate frame.
/// Dimensions and coordinates are strictly in millimetres.
/// </summary>
public sealed record FoundationGeometrySnapshot
{
    public FoundationGeometrySnapshot()
    {
    }

    public FoundationGeometrySnapshot(
        double length,
        double width,
        double thickness,
        double topZ,
        double bottomZ,
        Point3 origin,
        Vector3 localX,
        Vector3 localY,
        Vector3 localZ)
    {
        Length = length;
        Width = width;
        Thickness = thickness;
        TopZ = topZ;
        BottomZ = bottomZ;
        Origin = origin;
        LocalX = localX;
        LocalY = localY;
        LocalZ = localZ;
    }

    /// <summary>Foundation dimension along local X axis in millimetres.</summary>
    public double Length { get; init; }

    /// <summary>Foundation dimension along local Y axis in millimetres.</summary>
    public double Width { get; init; }

    /// <summary>Total vertical thickness/depth of the foundation slab in millimetres.</summary>
    public double Thickness { get; init; }

    /// <summary>Elevation of the top planar face in world coordinates (mm).</summary>
    public double TopZ { get; init; }

    /// <summary>Elevation of the bottom planar face in world coordinates (mm).</summary>
    public double BottomZ { get; init; }

    /// <summary>World origin of the local foundation frame (typically bottom corner) in millimetres.</summary>
    public Point3 Origin { get; init; }

    /// <summary>Unit direction vector along local X axis (horizontal length direction).</summary>
    public Vector3 LocalX { get; init; } = Vector3.UnitX;

    /// <summary>Unit direction vector along local Y axis (horizontal width direction).</summary>
    public Vector3 LocalY { get; init; } = Vector3.UnitY;

    /// <summary>Unit direction vector along local Z axis (upward vertical normal).</summary>
    public Vector3 LocalZ { get; init; } = Vector3.UnitZ;

    // Architectural & naming convenience aliases
    public double LengthMm => Length;
    public double WidthMm => Width;
    public double ThicknessMm => Thickness;
    public double TopElevation => TopZ;
    public double BottomElevation => BottomZ;
    public Vector3 NormalZ => LocalZ;
    public Vector3 DirectionX => LocalX;
    public Vector3 DirectionY => LocalY;
    public Vector3 DirectionZ => LocalZ;

    /// <summary>
    /// Transforms local foundation coordinates (u, v, z relative to bottom face) to world 3D coordinates:
    /// P_world = Origin + u * LocalX + v * LocalY + z * LocalZ.
    /// </summary>
    public Point3 ToWorld(double localX, double localY, double localZ) =>
        Origin + (LocalX * localX) + (LocalY * localY) + (LocalZ * localZ);

    /// <summary>
    /// Transforms a local 3D point to world coordinates.
    /// </summary>
    public Point3 ToWorld(Point3 localPoint) =>
        ToWorld(localPoint.X, localPoint.Y, localPoint.Z);

    /// <summary>
    /// Transforms a world 3D point into the local foundation coordinate frame.
    /// </summary>
    public Point3 ToLocal(Point3 worldPoint)
    {
        Vector3 v = worldPoint - Origin;
        return new Point3(v.Dot(LocalX), v.Dot(LocalY), v.Dot(LocalZ));
    }

    /// <summary>
    /// Factory to construct an axis-aligned snapshot.
    /// </summary>
    public static FoundationGeometrySnapshot CreateAxisAligned(
        Point3 origin,
        double length,
        double width,
        double thickness)
    {
        return new FoundationGeometrySnapshot
        {
            Origin = origin,
            Length = length,
            Width = width,
            Thickness = thickness,
            BottomZ = origin.Z,
            TopZ = origin.Z + thickness,
            LocalX = Vector3.UnitX,
            LocalY = Vector3.UnitY,
            LocalZ = Vector3.UnitZ
        };
    }

    /// <summary>
    /// Factory to construct an oriented snapshot rotated by an angle in the XY plane.
    /// </summary>
    public static FoundationGeometrySnapshot CreateOriented(
        Point3 origin,
        double angleDegrees,
        double length,
        double width,
        double thickness)
    {
        double rad = angleDegrees * (Math.PI / 180.0);
        var ux = new Vector3(Math.Cos(rad), Math.Sin(rad), 0.0).Normalize();
        var uz = Vector3.UnitZ;
        var uy = uz.Cross(ux).Normalize();

        return new FoundationGeometrySnapshot
        {
            Origin = origin,
            Length = length,
            Width = width,
            Thickness = thickness,
            BottomZ = origin.Z,
            TopZ = origin.Z + thickness,
            LocalX = ux,
            LocalY = uy,
            LocalZ = uz
        };
    }
}
