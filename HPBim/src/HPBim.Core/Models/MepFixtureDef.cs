namespace HPBim.Core.Models;

using HPBim.Core.Geometry;

public sealed record MepFixtureDef
{
    public string Id { get; init; } = string.Empty;
    public MepCategory Category { get; init; } = MepCategory.Sanitary;
    public string FamilyName { get; init; } = string.Empty;
    public Point3D Location { get; init; }
    public double RotationDeg { get; init; }
    public string Level { get; init; } = string.Empty;
    public double WidthMm { get; init; }
    public double DepthMm { get; init; }
    public double HeightMm { get; init; }
    public string MountingType { get; init; } = "Floor";
}
