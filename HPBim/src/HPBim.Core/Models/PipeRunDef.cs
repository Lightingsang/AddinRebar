namespace HPBim.Core.Models;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HPBim.Core.Geometry;

public sealed record PipeRunDef
{
    public string Id { get; init; } = string.Empty;
    public PipeSystemType SystemType { get; init; } = PipeSystemType.WaterSupply;
    public double DiameterMm { get; init; } = 25.0;
    public IReadOnlyList<Point3D> Points { get; init; } = Array.Empty<Point3D>();
    public double Slope { get; init; }
    public string Material { get; init; } = "PPR";
    public string Level { get; init; } = string.Empty;

    [JsonIgnore]
    public double TotalLengthMm
    {
        get
        {
            if (Points == null || Points.Count < 2) return 0.0;
            double len = 0.0;
            for (int i = 0; i < Points.Count - 1; i++)
            {
                len += Points[i].DistanceTo(Points[i + 1]);
            }
            return len;
        }
    }
}
