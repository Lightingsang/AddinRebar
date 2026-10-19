namespace HPBim.Core.Models;

using System.Text.Json.Serialization;
using HPBim.Core.Geometry;

public sealed record RoomDef
{
    public string Name { get; init; } = string.Empty;
    public Point2D TagLocation { get; init; }
    public double? TheoreticalAreaM2 { get; init; }
    public string Level { get; init; } = string.Empty;
    public Polygon2D? BoundaryPolygon { get; init; }
    public double TagRotationDeg { get; init; }

    [JsonIgnore]
    public double ComputedAreaM2 => (BoundaryPolygon?.Area ?? 0.0) / 1_000_000.0;
}
