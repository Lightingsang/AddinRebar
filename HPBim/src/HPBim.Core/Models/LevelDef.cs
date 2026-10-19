namespace HPBim.Core.Models;

using System;
using System.Text.Json.Serialization;

public sealed record LevelDef
{
    public string Name { get; init; } = string.Empty;
    public double ElevationM { get; init; }

    [JsonIgnore]
    public double ElevationMm => ElevationM * 1000.0;
    public double HeightMm { get; init; }
    public int FloorNumber { get; init; }
    public bool IsStructural { get; init; } = true;

    [JsonIgnore]
    public double TopElevationM => ElevationM + (HeightMm / 1000.0);

    [JsonIgnore]
    public double TopElevationMm => ElevationMm + HeightMm;

    public LevelDef() { }

    public LevelDef(string name, double elevationM, double heightMm, int floorNumber, bool isStructural = true)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ElevationM = elevationM;
        HeightMm = heightMm;
        FloorNumber = floorNumber;
        IsStructural = isStructural;
    }
}
