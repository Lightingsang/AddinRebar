using System.Text.Json.Serialization;

namespace HPNavis.BIMCoordinator.Rules;

/// <summary>
///     The embedded <c>hp-clash-matrix.json</c> as written by <c>tools/bim-coordinator/generate-clash-matrix.py</c>
///     from sheet <c>RuleClash(HP)</c>. Shapes only; <see cref="ClashMatrix" /> validates and answers questions.
/// </summary>
public sealed class ClashMatrixDocument
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
    [JsonPropertyName("source")] public MatrixSource Source { get; set; } = new();
    [JsonPropertyName("priorityLabels")] public Dictionary<string, string> PriorityLabels { get; set; } = new();

    /// <summary>Allowed tolerance per LOD in millimetres; null = not approved (LOD 400).</summary>
    [JsonPropertyName("toleranceMm")] public Dictionary<string, double?> ToleranceMm { get; set; } = new();

    [JsonPropertyName("groups")] public List<ElementGroup> Groups { get; set; } = new();
    [JsonPropertyName("rules")] public List<ClashRule> Rules { get; set; } = new();
}

public sealed class MatrixSource
{
    [JsonPropertyName("workbook")] public string Workbook { get; set; } = "";
    [JsonPropertyName("sheet")] public string Sheet { get; set; } = "";
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = "";
}

/// <summary>One of the 21 BIM element groups (A1..A9, S1..S5, M1..M7) and the LODs it is checked at.</summary>
public sealed class ElementGroup
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("nameVi")] public string NameVi { get; set; } = "";
    [JsonPropertyName("discipline")] public string Discipline { get; set; } = "";
    [JsonPropertyName("lod")] public Dictionary<string, bool> Lod { get; set; } = new();
}

/// <summary>One matrix pair: the two groups, the priority 1/2/3 and the LODs at which the pair is checked.</summary>
public sealed class ClashRule
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("left")] public string Left { get; set; } = "";
    [JsonPropertyName("right")] public string Right { get; set; } = "";
    [JsonPropertyName("disciplinePair")] public string DisciplinePair { get; set; } = "";
    [JsonPropertyName("priority")] public int Priority { get; set; }

    /// <summary>Workbook cells the pair was read from, kept so a reviewer can trace every test to the sheet.</summary>
    [JsonPropertyName("cells")] public List<string> Cells { get; set; } = new();

    [JsonPropertyName("lod")] public Dictionary<string, bool> Lod { get; set; } = new();

    public bool IsSelfPair => Left == Right;

    public bool IsEligibleAt(string lod) => Lod.TryGetValue(lod, out var eligible) && eligible;
}
