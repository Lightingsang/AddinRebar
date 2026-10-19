namespace HPBim.Core.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HPBim.Core.Geometry;

public sealed record SlabDef
{
    public string Id { get; init; } = string.Empty;
    public Polygon2D Boundary { get; init; } = Polygon2D.Empty;
    public double ThicknessMm { get; init; } = 100.0;
    public string Level { get; init; } = string.Empty;
    public double ElevationOffsetMm { get; init; }
    public IReadOnlyList<Polygon2D> Voids { get; init; } = Array.Empty<Polygon2D>();

    [JsonIgnore]
    public double GrossAreaM2 => Boundary.Area / 1_000_000.0;

    [JsonIgnore]
    public double NetAreaM2 => (Boundary.Area - (Voids != null ? Voids.Sum(v => v.Area) : 0.0)) / 1_000_000.0;
}
