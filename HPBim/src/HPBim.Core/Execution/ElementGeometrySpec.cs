namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PointGeometrySpec), "point")]
[JsonDerivedType(typeof(LineGeometrySpec), "line")]
[JsonDerivedType(typeof(ProfileGeometrySpec), "profile")]
[JsonDerivedType(typeof(OpeningGeometrySpec), "opening")]
[JsonDerivedType(typeof(LevelGeometrySpec), "level")]
public abstract record ElementGeometrySpec
{
    public abstract string GetCanonicalString();
}

public sealed record PointGeometrySpec : ElementGeometrySpec
{
    public Point3D Location { get; init; } = new(0, 0, 0);
    public double RotationDegrees { get; init; }
    public double OffsetZMm { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "PT({0:F2},{1:F2},{2:F2},r:{3:F1},z:{4:F1})",
            Location.X, Location.Y, Location.Z, RotationDegrees, OffsetZMm);
}

public sealed record LineGeometrySpec : ElementGeometrySpec
{
    public Point3D Start { get; init; } = new(0, 0, 0);
    public Point3D End { get; init; } = new(0, 0, 0);
    public double ElevationMm { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "LN(({0:F2},{1:F2},{2:F2})->({3:F2},{4:F2},{5:F2}),el:{6:F1})",
            Start.X, Start.Y, Start.Z, End.X, End.Y, End.Z, ElevationMm);
}

public sealed record ProfileGeometrySpec : ElementGeometrySpec
{
    public IReadOnlyList<Point3D> OuterLoop { get; init; } = Array.Empty<Point3D>();
    public IReadOnlyList<IReadOnlyList<Point3D>> InnerLoops { get; init; } = Array.Empty<IReadOnlyList<Point3D>>();
    public double ThicknessMm { get; init; }
    public double ElevationMm { get; init; }

    public override string GetCanonicalString()
    {
        var sb = new StringBuilder();
        sb.Append("PRF(outer:[");
        if (OuterLoop != null)
        {
            for (int i = 0; i < OuterLoop.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var pt = OuterLoop[i];
                sb.Append(string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})", pt.X, pt.Y, pt.Z));
            }
        }
        sb.Append(']');

        if (InnerLoops != null && InnerLoops.Count > 0)
        {
            sb.Append(",holes:[");
            for (int i = 0; i < InnerLoops.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append('[');
                var loop = InnerLoops[i];
                if (loop != null)
                {
                    for (int j = 0; j < loop.Count; j++)
                    {
                        if (j > 0) sb.Append(',');
                        var pt = loop[j];
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})", pt.X, pt.Y, pt.Z));
                    }
                }
                sb.Append(']');
            }
            sb.Append(']');
        }

        sb.Append(string.Format(CultureInfo.InvariantCulture, ",t:{0:F2},el:{1:F2})", ThicknessMm, ElevationMm));
        return sb.ToString();
    }
}

public sealed record OpeningGeometrySpec : ElementGeometrySpec
{
    public string HostWallSourceKey { get; init; } = string.Empty;
    public double DistanceAlongWallMm { get; init; }
    public double SillHeightMm { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public OpeningKind Kind { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "OPN(h:{0},d:{1:F2},sill:{2:F2},{3:F2}x{4:F2},{5})",
            HostWallSourceKey, DistanceAlongWallMm, SillHeightMm, WidthMm, HeightMm, Kind);
}

public sealed record LevelGeometrySpec : ElementGeometrySpec
{
    public double ElevationM { get; init; }
    public int FloorNumber { get; init; }
    public bool IsBuildingStory { get; init; } = true;

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "LVL(el:{0:F3}m,f:{1},story:{2})",
            ElevationM, FloorNumber, IsBuildingStory);
}
