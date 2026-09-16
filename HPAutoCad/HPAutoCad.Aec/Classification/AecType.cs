namespace HPAutoCad.Aec.Classification;

/// <summary>The disciplines an AEC object belongs to; rule sets and tool filters use these names.</summary>
public static class Discipline
{
    public const string Structural = "Structural";
    public const string Architecture = "Architecture";
    public const string Mep = "MEP";

    public static readonly IReadOnlyList<string> All = [Structural, Architecture, Mep];

    public static string? Normalize(string? text) => text?.Trim().ToLowerInvariant() switch
    {
        "structural" or "structure" or "s" => Structural,
        "architecture" or "architectural" or "arch" or "a" => Architecture,
        "mep" or "m" or "mechanical" or "electrical" or "plumbing" => Mep,
        _ => null,
    };
}

/// <summary>
///     The AEC object types the classifier can assign. Plain strings (not an enum) so a rule file can name
///     them and a response carries them verbatim; <see cref="DisciplineOf"/> is the fixed mapping.
/// </summary>
public static class AecType
{
    public const string StructuralColumn = "StructuralColumn";
    public const string StructuralBeam = "StructuralBeam";
    public const string StructuralWall = "StructuralWall";
    public const string StructuralSlab = "StructuralSlab";
    public const string StructuralOpening = "StructuralOpening";
    public const string StructuralGrid = "StructuralGrid";

    public const string ArchitecturalWall = "ArchitecturalWall";
    public const string Door = "Door";
    public const string Window = "Window";
    public const string Room = "Room";
    public const string Stair = "Stair";
    public const string Furniture = "Furniture";

    public const string Pipe = "Pipe";
    public const string Duct = "Duct";
    public const string CableTray = "CableTray";
    public const string Equipment = "Equipment";
    public const string Fixture = "Fixture";
    public const string Terminal = "Terminal";
    public const string Fitting = "Fitting";

    public const string Unknown = "Unknown";

    public static readonly IReadOnlyList<string> All =
    [
        StructuralColumn, StructuralBeam, StructuralWall, StructuralSlab, StructuralOpening, StructuralGrid,
        ArchitecturalWall, Door, Window, Room, Stair, Furniture,
        Pipe, Duct, CableTray, Equipment, Fixture, Terminal, Fitting,
    ];

    public static string DisciplineOf(string aecType) => aecType switch
    {
        StructuralColumn or StructuralBeam or StructuralWall or StructuralSlab or StructuralOpening or StructuralGrid => Discipline.Structural,
        ArchitecturalWall or Door or Window or Room or Stair or Furniture => Discipline.Architecture,
        Pipe or Duct or CableTray or Equipment or Fixture or Terminal or Fitting => Discipline.Mep,
        _ => "",
    };

    public static bool IsKnown(string? aecType) => aecType is not null && All.Contains(aecType, StringComparer.Ordinal);
}
