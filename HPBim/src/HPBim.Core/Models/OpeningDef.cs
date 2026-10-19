namespace HPBim.Core.Models;

using System.Text.Json.Serialization;

public sealed record OpeningDef
{
    public string Id { get; init; } = string.Empty;
    public string HostWallId { get; init; } = string.Empty;
    public double DistanceAlongWallMm { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public double SillHeightMm { get; init; }
    public OpeningKind Kind { get; init; } = OpeningKind.Door;
    public OpeningSwing Swing { get; init; } = OpeningSwing.Left;
    public string? Description { get; init; }

    [JsonIgnore]
    public double CenterDistanceMm => DistanceAlongWallMm + (WidthMm / 2.0);

    [JsonIgnore]
    public double EndDistanceMm => DistanceAlongWallMm + WidthMm;

    [JsonIgnore]
    public double HeadHeightMm => SillHeightMm + HeightMm;
}
