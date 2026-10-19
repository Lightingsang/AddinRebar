namespace HPBim.Core.Models;

using System.Text.Json.Serialization;
using HPBim.Core.Geometry;

public sealed record BeamDef
{
    public string Id { get; init; } = string.Empty;
    public Point2D StartPoint { get; init; }
    public Point2D EndPoint { get; init; }
    public double WidthMm { get; init; } = 200.0;
    public double HeightMm { get; init; } = 400.0;
    public string Level { get; init; } = string.Empty;
    public string BeamType { get; init; } = "D1";
    public bool IsTieBeam { get; init; }

    [JsonIgnore]
    public double Length => LengthMm;

    [JsonIgnore]
    public double LengthMm => StartPoint.DistanceTo(EndPoint);

    [JsonIgnore]
    public Vector2D Direction
    {
        get
        {
            double dx = EndPoint.X - StartPoint.X;
            double dy = EndPoint.Y - StartPoint.Y;
            double len = LengthMm;
            return len > 1e-6 ? new Vector2D(dx / len, dy / len) : new Vector2D(1, 0);
        }
    }
}
