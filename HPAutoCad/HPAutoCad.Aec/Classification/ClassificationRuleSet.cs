using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPAutoCad.Aec.Classification;

/// <summary>
///     The rules the classifier runs, loaded from JSON: the default set embedded in this assembly, a user
///     set beside the MCP server's registry (<c>%AppData%\HPAutoCad\McpServer\rules\&lt;name&gt;.json</c>),
///     never an arbitrary path. Every rule is validated once at load so a typo in a rule file surfaces as a
///     tool error, not as a silently useless rule.
/// </summary>
public sealed class ClassificationRuleSet
{
    public const string DefaultName = "default";
    public const string UserName = "user";
    private const string EmbeddedResource = "HPAutoCad.Aec.Rules.aec-classification.default.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // A misspelt key ("layer" for "layers") would otherwise silently drop a criterion and widen the rule to everything.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    public string Name { get; init; } = DefaultName;

    public string Source { get; init; } = "embedded";

    public int Version { get; init; } = 1;

    public IReadOnlyList<ClassificationRule> Rules { get; init; } = [];

    /// <summary>Where user rule sets live: one JSON file per name, next to the AutoCAD MCP server's registry.</summary>
    public static string UserRulesDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HPAutoCad", "McpServer", "rules");

    /// <summary>
    ///     <c>default</c> → the embedded set; <c>user</c> → <c>rules\aec-classification.json</c>; any other plain name → <c>rules\&lt;name&gt;.json</c>;
    ///     null/empty → the user set when the file exists, else the default. Names with path separators are refused.
    /// </summary>
    public static ClassificationRuleSet Load(string? name)
    {
        name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        if (name is not null && (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('/') || name.Contains('\\') || name.Contains("..")))
            throw new ArgumentException($"ruleSet must be 'default', 'user' or a plain file name inside {UserRulesDirectory}.");

        if (string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase)) return LoadEmbedded();

        var fileName = name is null || string.Equals(name, UserName, StringComparison.OrdinalIgnoreCase) ? "aec-classification.json" : name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? name : name + ".json";
        var path = Path.Combine(UserRulesDirectory, fileName);
        if (File.Exists(path)) return Parse(File.ReadAllText(path), name ?? UserName, "rules\\" + fileName); // the folder is fixed; never echo the profile path
        if (name is null) return LoadEmbedded();
        throw new ArgumentException($"ruleSet '{name}' not found: expected {path}.");
    }

    public static ClassificationRuleSet LoadEmbedded()
    {
        using var stream = typeof(ClassificationRuleSet).Assembly.GetManifestResourceStream(EmbeddedResource)
                           ?? throw new InvalidOperationException($"embedded rule set {EmbeddedResource} is missing from {typeof(ClassificationRuleSet).Assembly.GetName().Name}");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd(), DefaultName, "embedded");
    }

    public static ClassificationRuleSet Parse(string json, string name, string source)
    {
        RuleFile? file;
        try
        {
            file = JsonSerializer.Deserialize<RuleFile>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException($"rule set '{name}' ({source}) is not valid JSON: {exception.Message}");
        }

        if (file is null || file.Rules.Count == 0) throw new ArgumentException($"rule set '{name}' ({source}) has no rules.");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in file.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Id)) throw new ArgumentException($"rule set '{name}': every rule needs an id.");
            if (!ids.Add(rule.Id)) throw new ArgumentException($"rule set '{name}': duplicate rule id '{rule.Id}'.");
            if (!AecType.IsKnown(rule.AecType)) throw new ArgumentException($"rule '{rule.Id}': aecType '{rule.AecType}' is not one of {string.Join(", ", AecType.All)}.");
            if (rule.Confidence is <= 0 or > 1) throw new ArgumentException($"rule '{rule.Id}': confidence must be in (0, 1].");
            foreach (var (label, value) in new[] { ("minSizeMm", rule.MinSizeMm), ("maxSizeMm", rule.MaxSizeMm), ("minShortSideMm", rule.MinShortSideMm), ("maxShortSideMm", rule.MaxShortSideMm), ("minAspectRatio", rule.MinAspectRatio), ("maxAspectRatio", rule.MaxAspectRatio) })
                if (value is { } v && !(v >= 0 && double.IsFinite(v))) throw new ArgumentException($"rule '{rule.Id}': {label} must be a non-negative number.");
            if (!(rule.NearbyRadiusMm > 0) || !(rule.NearbyTextBoost >= 0 && rule.NearbyTextBoost <= 0.5)) throw new ArgumentException($"rule '{rule.Id}': nearbyRadiusMm must be > 0 and nearbyTextBoost in [0, 0.5].");
            if (rule.Layers.Concat(rule.Types).Concat(rule.BlockNames).Any(string.IsNullOrWhiteSpace)) throw new ArgumentException($"rule '{rule.Id}': empty pattern in layers/types/blockNames.");
            try
            {
                _ = rule.TextRegex;
                _ = rule.NearbyTextRegex;
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException($"rule '{rule.Id}': invalid regex — {exception.Message}");
            }
        }

        return new ClassificationRuleSet { Name = name, Source = source, Version = file.Version, Rules = file.Rules };
    }

    public bool UsesNearbyText => Rules.Any(r => r.NearbyTextPattern is not null);

    private sealed class RuleFile
    {
        public int Version { get; set; } = 1;

        private List<ClassificationRule> _rules = [];

        public List<ClassificationRule> Rules { get => _rules; set => _rules = value ?? []; }
    }
}
