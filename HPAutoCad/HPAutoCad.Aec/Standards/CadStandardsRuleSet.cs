using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Standards;

/// <summary>
///     The CAD standards <c>cad_standards_check</c> enforces, loaded from JSON — the embedded default (AIA/NCS-shaped, permissive)
///     or a project file <c>rules\cad-standards.json</c> beside the MCP server's registry. Every section is optional: a missing or
///     disabled section is simply not checked. Regexes and severities are validated once at load.
/// </summary>
public sealed class CadStandardsRuleSet
{
    public const string EmbeddedResource = "HPAutoCad.Aec.Rules.cad-standards.default.json";
    public const string UserFileName = "cad-standards.json";
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    public string Name { get; init; } = RuleFileLocator.DefaultName;

    public string Source { get; init; } = "embedded";

    public int Version { get; set; } = 1;

    public LayerNamingRule? LayerNaming { get; set; }

    public List<EntityLayerRule> EntityLayer { get; set; } = [];

    public LayerZeroRule? LayerZero { get; set; }

    public OverrideRule? Overrides { get; set; }

    public AllowedNamesRule? TextStyles { get; set; }

    public TextHeightRule? TextHeights { get; set; }

    public AllowedNamesRule? DimStyles { get; set; }

    public LayerNamingRule? BlockNaming { get; set; }

    public UnusedLayerRule? UnusedLayers { get; set; }

    /// <summary>Which checks this set switches on — what the summary reports as <c>checkedTypes</c>.</summary>
    public IReadOnlyList<string> EnabledChecks()
    {
        var checks = new List<string>();
        if (LayerNaming?.Pattern is not null) checks.Add(StandardsIssueType.LayerNaming);
        if (EntityLayer.Count > 0) checks.Add(StandardsIssueType.EntityLayer);
        if (LayerZero is not null) checks.Add(StandardsIssueType.LayerZero);
        if (Overrides?.Color == true) checks.Add(StandardsIssueType.ColorOverride);
        if (Overrides?.Linetype == true) checks.Add(StandardsIssueType.LinetypeOverride);
        if (Overrides?.Lineweight == true) checks.Add(StandardsIssueType.LineweightOverride);
        if (TextStyles?.Allowed.Count > 0) checks.Add(StandardsIssueType.TextStyle);
        if (TextHeights?.AllowedMm.Count > 0) checks.Add(StandardsIssueType.TextHeight);
        if (DimStyles?.Allowed.Count > 0) checks.Add(StandardsIssueType.DimStyle);
        if (BlockNaming?.Pattern is not null) checks.Add(StandardsIssueType.BlockNaming);
        if (UnusedLayers is not null) checks.Add(StandardsIssueType.UnusedLayer);
        return checks;
    }

    public static CadStandardsRuleSet Load(string? name)
    {
        var resolved = RuleFileLocator.Resolve(name, UserFileName);
        return resolved.Json is null ? LoadEmbedded() : Parse(resolved.Json, resolved.Name, resolved.Source);
    }

    public static CadStandardsRuleSet LoadEmbedded() => Parse(RuleFileLocator.ReadEmbedded(typeof(CadStandardsRuleSet), EmbeddedResource), RuleFileLocator.DefaultName, "embedded");

    public static CadStandardsRuleSet Parse(string json, string name, string source)
    {
        CadStandardsRuleSet? set;
        try
        {
            set = JsonSerializer.Deserialize<CadStandardsRuleSet>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"rule set '{name}' ({source}) is not valid JSON: {exception.Message}");
        }

        if (set is null) throw new ArgumentException($"rule set '{name}' ({source}) is empty.");
        set = new CadStandardsRuleSet
        {
            Name = name, Source = source, Version = set.Version, LayerNaming = set.LayerNaming, EntityLayer = set.EntityLayer, LayerZero = set.LayerZero, Overrides = set.Overrides,
            TextStyles = set.TextStyles, TextHeights = set.TextHeights, DimStyles = set.DimStyles, BlockNaming = set.BlockNaming, UnusedLayers = set.UnusedLayers,
        };
        Validate(set, name);
        return set;
    }

    private static void Validate(CadStandardsRuleSet set, string name)
    {
        foreach (var (label, regex) in new[] { ("layerNaming.pattern", set.LayerNaming?.Pattern), ("blockNaming.pattern", set.BlockNaming?.Pattern) })
        {
            if (regex is null) continue;
            try { _ = new Regex(regex, RegexOptions.CultureInvariant, RegexTimeout); }
            catch (ArgumentException exception) { throw new ArgumentException($"rule set '{name}': {label} is not a valid regex — {exception.Message}"); }
        }

        foreach (var severity in new[] { set.LayerNaming?.Severity, set.LayerZero?.Severity, set.Overrides?.Severity, set.TextStyles?.Severity, set.TextHeights?.Severity, set.DimStyles?.Severity, set.BlockNaming?.Severity, set.UnusedLayers?.Severity }.Concat(set.EntityLayer.Select(r => r.Severity)))
            if (severity is not null && severity is not (IssueSeverity.Critical or IssueSeverity.Warning or IssueSeverity.Info))
                throw new ArgumentException($"rule set '{name}': severity '{severity}' must be critical, warning or info.");
        for (var i = 0; i < set.EntityLayer.Count; i++)
        {
            var rule = set.EntityLayer[i];
            if (rule.Types.Count == 0 || rule.Layers.Count == 0) throw new ArgumentException($"rule set '{name}': entityLayer[{i}] needs types[] and layers[].");
            if (string.IsNullOrWhiteSpace(rule.Id)) rule.Id = $"entity-layer-{i + 1}";
        }

        if (set.TextHeights is { } th && (th.ToleranceMm < 0 || th.AllowedMm.Any(h => h <= 0))) throw new ArgumentException($"rule set '{name}': textHeights.allowedMm must be > 0 and toleranceMm >= 0.");
        if (set.TextHeights is { } ths && ths.Space.Trim().ToLowerInvariant() is not ("all" or "paper" or "model")) throw new ArgumentException($"rule set '{name}': textHeights.space must be all, paper or model.");
    }
}

/// <summary>A name pattern (regex, case-sensitive) with wildcard exemptions — for layer and block names.</summary>
public sealed class LayerNamingRule
{
    private List<string> _exempt = [];
    private Regex? _regex;

    public string? Pattern { get; set; }

    public List<string> Exempt { get => _exempt; set => _exempt = value ?? []; }

    public string Severity { get; set; } = IssueSeverity.Warning;

    public string? Description { get; set; }

    [JsonIgnore]
    public Regex? Regex => Pattern is null ? null : _regex ??= new Regex(Pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
}

/// <summary>Entities of these DXF types belong on layers matching one of these wildcards.</summary>
public sealed class EntityLayerRule
{
    private List<string> _types = [];
    private List<string> _layers = [];

    public string? Id { get; set; }

    public List<string> Types { get => _types; set => _types = value ?? []; }

    public List<string> Layers { get => _layers; set => _layers = value ?? []; }

    public string Severity { get; set; } = IssueSeverity.Warning;

    public string? Message { get; set; }
}

/// <summary>Layer 0 carries block geometry only: any other entity type on it is reported.</summary>
public sealed class LayerZeroRule
{
    private List<string> _allowedTypes = ["INSERT"];

    public List<string> AllowedTypes { get => _allowedTypes; set => _allowedTypes = value ?? []; }

    public string Severity { get; set; } = IssueSeverity.Warning;
}

/// <summary>Entity-level colour / linetype / lineweight overrides (anything but ByLayer) are reported.</summary>
public sealed class OverrideRule
{
    private List<string> _exemptTypes = [];

    public bool Color { get; set; } = true;

    public bool Linetype { get; set; } = true;

    public bool Lineweight { get; set; } = true;

    private List<string> _exemptLayers = [];

    /// <summary>DXF types allowed to carry their own colour/linetype/lineweight (a hatch or a block reference often does by design).</summary>
    public List<string> ExemptTypes { get => _exemptTypes; set => _exemptTypes = value ?? []; }

    /// <summary>Layers whose entities may carry overrides (markup layers colour by severity on purpose).</summary>
    public List<string> ExemptLayers { get => _exemptLayers; set => _exemptLayers = value ?? []; }

    public string Severity { get; set; } = IssueSeverity.Info;
}

/// <summary>Allowed text / dimension style names (wildcards); empty = not checked.</summary>
public sealed class AllowedNamesRule
{
    private List<string> _allowed = [];

    public List<string> Allowed { get => _allowed; set => _allowed = value ?? []; }

    public string Severity { get; set; } = IssueSeverity.Warning;
}

/// <summary>Allowed text heights in mm as drawn (the check is per space so model-space annotation at scale can be exempted).</summary>
public sealed class TextHeightRule
{
    private List<double> _allowedMm = [];

    public List<double> AllowedMm { get => _allowedMm; set => _allowedMm = value ?? []; }

    public double ToleranceMm { get; set; } = 0.05;

    /// <summary><c>all</c>, <c>paper</c> or <c>model</c>.</summary>
    public string Space { get; set; } = "paper";

    public string Severity { get; set; } = IssueSeverity.Info;
}

/// <summary>Layers with no entity in the examined set (only when the whole drawing was examined).</summary>
public sealed class UnusedLayerRule
{
    private List<string> _exempt = ["0", "Defpoints"];

    public List<string> Exempt { get => _exempt; set => _exempt = value ?? []; }

    public string Severity { get; set; } = IssueSeverity.Info;
}
