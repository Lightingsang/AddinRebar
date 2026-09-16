using HPAutoCad.Aec.Geometry;

namespace HPAutoCad.Aec.Issues;

/// <summary>Kinds <c>cad_standards_check</c> reports; the tool argument uses these snake_case names.</summary>
public static class StandardsIssueType
{
    public const string LayerNaming = "layer_naming";
    public const string EntityLayer = "entity_layer";
    public const string LayerZero = "layer_zero";
    public const string ColorOverride = "color_override";
    public const string LinetypeOverride = "linetype_override";
    public const string LineweightOverride = "lineweight_override";
    public const string TextStyle = "text_style";
    public const string TextHeight = "text_height";
    public const string DimStyle = "dim_style";
    public const string BlockNaming = "block_naming";
    public const string UnusedLayer = "unused_layer";

    public static readonly IReadOnlyList<string> All =
        [LayerNaming, EntityLayer, LayerZero, ColorOverride, LinetypeOverride, LineweightOverride, TextStyle, TextHeight, DimStyle, BlockNaming, UnusedLayer];
}

/// <summary>The sections <c>audit_aec_drawing</c> can run; each maps to one detector and one id prefix.</summary>
public static class AuditCategory
{
    public const string Geometry = "geometry";
    public const string Standards = "standards";

    public static readonly IReadOnlyList<string> All = [Geometry, Standards];
}

/// <summary>
///     One finding of any audit section, in the shape every QA tool shares: id (<c>GEO-nnnn</c>, <c>STD-nnnn</c>), category,
///     type, severity, the entities involved, a location to zoom to, an optional measured value, a sentence, and what to do.
///     <c>Rule</c> names the standards rule that fired. Never re-derive geometry to explain an issue: it is all here.
/// </summary>
public sealed record AuditIssue(
    string IssueId,
    string Category,
    string Type,
    string Severity,
    IReadOnlyList<string> Handles,
    Pt? LocationMm,
    double? ValueMm,
    string Description,
    string? SuggestedAction = null,
    string? Layer = null,
    string? Rule = null)
{
    public static AuditIssue From(GeometryIssue g) =>
        new(g.IssueId, AuditCategory.Geometry, g.Type, g.Severity, g.Handles, g.LocationMm, g.ValueMm, g.Description, g.SuggestedAction);

    /// <summary>critical → warning → info, then category, type, first handle (ordinal), layer, description: the same order on every run and page.</summary>
    public static IReadOnlyList<AuditIssue> Ordered(IEnumerable<AuditIssue> issues) => issues
        .OrderBy(i => SeverityRank(i.Severity))
        .ThenBy(i => i.Category, StringComparer.Ordinal)
        .ThenBy(i => i.Type, StringComparer.Ordinal)
        .ThenBy(i => i.Handles.Count == 0 ? "" : i.Handles[0], StringComparer.Ordinal)
        .ThenBy(i => i.Layer ?? "", StringComparer.Ordinal)
        .ThenBy(i => i.Description, StringComparer.Ordinal)
        .ToArray();

    public static int SeverityRank(string severity) => severity switch
    {
        IssueSeverity.Critical => 0,
        IssueSeverity.Warning => 1,
        _ => 2,
    };

    /// <summary>true when the issue is at least as severe as <paramref name="minimum"/> (critical ≥ warning ≥ info).</summary>
    public bool AtLeast(string minimum) => SeverityRank(Severity) <= SeverityRank(minimum);
}
