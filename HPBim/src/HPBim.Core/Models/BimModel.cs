namespace HPBim.Core.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

public sealed record BimModel
{
    public string SchemaVersion { get; init; } = "1.0";
    public string ArtifactType { get; init; } = "HPBim.Model.v1";
    public string ProjectName { get; init; } = string.Empty;
    public IReadOnlyList<LevelDef> Levels { get; init; } = Array.Empty<LevelDef>();
    public IReadOnlyList<AxisGrid> Grids { get; init; } = Array.Empty<AxisGrid>();
    public IReadOnlyList<ColumnDef> Columns { get; init; } = Array.Empty<ColumnDef>();
    public IReadOnlyList<WallDef> Walls { get; init; } = Array.Empty<WallDef>();
    public IReadOnlyList<BeamDef> Beams { get; init; } = Array.Empty<BeamDef>();
    public IReadOnlyList<SlabDef> Slabs { get; init; } = Array.Empty<SlabDef>();
    public IReadOnlyList<RoomDef> Rooms { get; init; } = Array.Empty<RoomDef>();
    public IReadOnlyList<MepFixtureDef> Fixtures { get; init; } = Array.Empty<MepFixtureDef>();
    public IReadOnlyList<PipeRunDef> Pipes { get; init; } = Array.Empty<PipeRunDef>();
    public IReadOnlyList<RebarDef> Rebars { get; init; } = Array.Empty<RebarDef>();

    [JsonIgnore]
    public double TotalRebarWeightKg => Rebars != null ? Rebars.Sum(r => r.TotalWeightKg) : 0.0;

    public LevelDef? GetLevel(string name) =>
        Levels?.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<WallDef> GetWallsByLevel(string levelName) =>
        Walls != null
            ? Walls.Where(w => string.Equals(w.BaseLevel, levelName, StringComparison.OrdinalIgnoreCase))
            : Enumerable.Empty<WallDef>();

    public IEnumerable<ColumnDef> GetColumnsByLevel(string levelName) =>
        Columns != null
            ? Columns.Where(c => string.Equals(c.BaseLevel, levelName, StringComparison.OrdinalIgnoreCase))
            : Enumerable.Empty<ColumnDef>();
}
