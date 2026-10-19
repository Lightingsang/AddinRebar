namespace HPBim.Core.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HPBim.Core.Geometry;

public sealed record WallDef
{
    public string Id { get; init; } = string.Empty;
    public Point2D StartPoint { get; init; }
    public Point2D EndPoint { get; init; }
    public double ThicknessMm { get; init; } = 220.0;
    public string BaseLevel { get; init; } = string.Empty;
    public string TopLevel { get; init; } = string.Empty;
    public double BaseOffsetMm { get; init; }
    public double TopOffsetMm { get; init; }
    public IReadOnlyList<OpeningDef> Openings { get; init; } = Array.Empty<OpeningDef>();

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

    [JsonIgnore]
    public Vector2D Normal => new Vector2D(-Direction.Y, Direction.X);
}
