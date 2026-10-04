using System.Text.Json;
using System.Text.RegularExpressions;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;

namespace HPRebar.Mcp.Server.Registry;

/// <summary>What the validator found; a tool is accepted only with zero <see cref="Errors"/>.</summary>
public sealed record ValidationReport(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
///     Mechanical checks on a proposed tool: name, category, schema shape, args ↔ schema agreement,
///     guard/compile verdict from the bridge, examples, transaction mode, hard-coded values. It decides
///     nothing about usefulness — that is what the human review and the run statistics are for.
/// </summary>
public static partial class ToolValidator
{
    /// <summary>The engine's own tools, reserved in every host; the host's core tools come from its profile.</summary>
    public static readonly string[] RegistryToolNames = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"];

    /// <summary>The line a report shows when the bridge that analysed the code predates the quality check.</summary>
    public const string QualityNotAnalysedWarning = "code quality not analysed: the bridge predates the quality check — redeploy it; publishing stays allowed.";

    private static readonly HashSet<string> SchemaTypes = new(StringComparer.Ordinal) { "string", "number", "integer", "boolean", "array", "object" };

    [GeneratedRegex("^[a-z][a-z0-9_]{2,63}$")]
    private static partial Regex NamePattern();

    /// <summary>The Revit server's behaviour before profiles existed.</summary>
    public static ValidationReport Validate(ToolRecord candidate, AnalyzeResult? analysis, IReadOnlyCollection<ToolRecord> existing, bool newVersion) =>
        Validate(candidate, analysis, existing, newVersion, HostProfile.Revit);

    /// <param name="profile">Decides the categories, the reserved names and the host a record must declare.</param>
    public static ValidationReport Validate(ToolRecord candidate, AnalyzeResult? analysis, IReadOnlyCollection<ToolRecord> existing, bool newVersion, IHostProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // ---- identity ----
        if (!NamePattern().IsMatch(candidate.Name)) errors.Add("name must be snake_case: lowercase letters, digits, underscores, 3–64 chars, starting with a letter.");
        if (IsReserved(candidate.Name, profile)) errors.Add($"name '{candidate.Name}' is reserved by the server.");
        if (candidate.Host is not null && !string.Equals(candidate.Host, profile.HostId, StringComparison.OrdinalIgnoreCase))
            errors.Add($"host must be '{profile.HostId}' (this server serves {profile.DisplayName}); got '{candidate.Host}'.");
        var clash = existing.FirstOrDefault(t => string.Equals(t.Name, candidate.Name, StringComparison.OrdinalIgnoreCase));
        if (clash is not null && !newVersion) errors.Add($"tool '{candidate.Name}' already exists (v{clash.Version}, {ToolRegistryDb.StatusText(clash.Status)}); pass newVersion=true to propose version {clash.Version + 1}, or pick another name.");
        if (clash is null && newVersion) warnings.Add("newVersion=true but no tool with that name exists; creating version 1.");

        if (string.IsNullOrWhiteSpace(candidate.Description) || candidate.Description.Trim().Length < 20) errors.Add("description must say what the tool does (≥ 20 characters).");
        if (!profile.Categories.Contains(candidate.Category, StringComparer.OrdinalIgnoreCase)) errors.Add($"category must be one of {string.Join(", ", profile.Categories)}.");
        if (TransactionModes.Normalize(candidate.Transaction) is null) errors.Add("transaction must be auto, manual or none.");
        if (candidate.TimeoutSeconds < ExecuteCodeService.MinTimeoutSeconds || candidate.TimeoutSeconds > profile.MaxTimeoutSeconds)
            errors.Add($"timeoutSeconds must be between {ExecuteCodeService.MinTimeoutSeconds} and {profile.MaxTimeoutSeconds}.");
        if (string.IsNullOrWhiteSpace(candidate.Code)) errors.Add("code is empty.");
        if (System.Text.Encoding.UTF8.GetByteCount(candidate.Code) > 32 * 1024) errors.Add("code exceeds 32 KB.");

        // ---- schema ----
        var properties = ValidateSchema(candidate.InputSchema, errors);

        // ---- args ↔ schema ----
        if (analysis is not null)
        {
            var read = analysis.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var key in read.Where(k => !properties.Contains(k))) errors.Add($"code reads args.{key} but inputSchema.properties does not declare it.");
            foreach (var prop in properties.Where(p => !read.Contains(p))) warnings.Add($"inputSchema declares '{prop}' but the code never reads args.*(\"{prop}\").");

            foreach (var violation in analysis.GuardViolations) errors.Add($"guard {violation.Line}:{violation.Column} {violation.Message}");
            foreach (var diagnostic in analysis.Diagnostics) errors.Add($"compile {diagnostic.Line}:{diagnostic.Column} {diagnostic.Id} {diagnostic.Message}");
            if (analysis.GuardViolations.Count == 0 && analysis.Diagnostics.Count == 0 && !analysis.Compiles) errors.Add("code did not compile (no diagnostics returned).");

            var mode = TransactionModes.Normalize(candidate.Transaction);
            if (analysis.UsesTransaction && mode != TransactionModes.Manual) errors.Add("code opens its own Transaction/TransactionGroup: set transaction=\"manual\".");
            if (!analysis.UsesTransaction && mode == TransactionModes.Manual) warnings.Add("transaction=\"manual\" but the code opens no Transaction; \"auto\" is usually what you want.");

            foreach (var literal in analysis.Literals.Where(IsSuspiciousLiteral))
                warnings.Add($"hard-coded {literal.Kind} {Quote(literal)} at line {literal.Line}{(literal.BoundTo is null ? "" : $" ({literal.BoundTo})")} — consider an args parameter with a default.");
            AddQualityFindings(analysis, errors, warnings);
        }
        else
        {
            warnings.Add("code was not analysed (bridge offline): guard, compile and args checks skipped — run test_tool before publishing.");
        }

        // ---- examples ----
        if (candidate.Examples.Count == 0) errors.Add("give at least one example {title, args}.");
        else if (candidate.Examples.Count < 2) warnings.Add("two or more examples with different args make test_tool meaningful.");
        var required = RequiredNames(candidate.InputSchema);
        for (var i = 0; i < candidate.Examples.Count; i++)
        {
            var example = candidate.Examples[i];
            if (string.IsNullOrWhiteSpace(example.Title)) errors.Add($"examples[{i}].title is empty.");
            if (example.Args.ValueKind != JsonValueKind.Object) { errors.Add($"examples[{i}].args must be an object."); continue; }
            foreach (var key in example.Args.EnumerateObject().Select(p => p.Name).Where(k => !properties.Contains(k)))
                errors.Add($"examples[{i}].args.{key} is not in inputSchema.properties.");
            foreach (var key in required.Where(r => !example.Args.TryGetProperty(r, out _)))
                errors.Add($"examples[{i}].args is missing required '{key}'.");
        }

        if (candidate.Examples.Count >= 2)
        {
            var distinct = candidate.Examples.Select(e => RegistryJson.Canonical(e.Args)).Distinct().Count();
            if (distinct < 2) warnings.Add("all examples have identical args; vary at least one value.");
        }

        return new ValidationReport(errors, warnings);
    }

    public static bool IsReserved(string name, IHostProfile profile) =>
        RegistryToolNames.Contains(name, StringComparer.OrdinalIgnoreCase) || profile.CoreToolNames.Contains(name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Quality findings: `error` blocks the draft, anything else is a warning; an old bridge gives one warning.</summary>
    private static void AddQualityFindings(AnalyzeResult analysis, List<string> errors, List<string> warnings)
    {
        if (!analysis.QualityAnalysed)
        {
            warnings.Add(QualityNotAnalysedWarning);
            return;
        }

        foreach (var finding in analysis.QualityFindings)
        {
            var line = $"quality {finding.RuleId} {finding.Line}:{finding.Column} {finding.Message}";
            if (finding.Severity == QualityFinding.Error) errors.Add(line);
            else warnings.Add(line);
        }
    }

    /// <summary>Accepts the JSON Schema subset the dynamic tool layer can express; returns the top-level property names.</summary>
    private static HashSet<string> ValidateSchema(JsonElement schema, List<string> errors)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (schema.ValueKind != JsonValueKind.Object) { errors.Add("inputSchema must be a JSON Schema object."); return names; }
        if (schema.TryGetProperty("type", out var type) && type.GetString() != "object") errors.Add("inputSchema.type must be \"object\".");
        if (!schema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object) { errors.Add("inputSchema.properties (object) is required, even if empty."); return names; }

        foreach (var property in properties.EnumerateObject())
        {
            if (!NamePropertyOk(property.Name)) errors.Add($"inputSchema.properties.{property.Name}: use a camelCase identifier.");
            if (string.Equals(property.Name, "dryRun", StringComparison.OrdinalIgnoreCase)) errors.Add("inputSchema.properties.dryRun is added automatically; remove it.");
            names.Add(property.Name);
            CheckPropertySchema($"inputSchema.properties.{property.Name}", property.Value, errors, depth: 0);
        }

        foreach (var name in RequiredNames(schema).Where(r => !names.Contains(r))) errors.Add($"inputSchema.required contains '{name}' which is not a property.");
        return names;
    }

    private static void CheckPropertySchema(string path, JsonElement schema, List<string> errors, int depth)
    {
        if (schema.ValueKind != JsonValueKind.Object) { errors.Add($"{path} must be a schema object."); return; }
        if (depth > 3) { errors.Add($"{path}: nesting deeper than 3 levels is not supported."); return; }

        if (!schema.TryGetProperty("type", out var typeElement)) { errors.Add($"{path}.type is required."); return; }
        var typeName = typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() : null;
        if (typeName is null || !SchemaTypes.Contains(typeName)) { errors.Add($"{path}.type must be one of string, number, integer, boolean, array, object."); return; }

        if (typeName == "array")
        {
            if (schema.TryGetProperty("items", out var items)) CheckPropertySchema(path + ".items", items, errors, depth + 1);
            else errors.Add($"{path}.items is required for arrays.");
        }
        else if (typeName == "object" && schema.TryGetProperty("properties", out var nested) && nested.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in nested.EnumerateObject()) CheckPropertySchema($"{path}.{property.Name}", property.Value, errors, depth + 1);
        }
    }

    private static IReadOnlyList<string> RequiredNames(JsonElement schema) =>
        schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array
            ? required.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToList()
            : [];

    private static bool NamePropertyOk(string name) => name.Length > 0 && char.IsLetter(name[0]) && name.All(c => char.IsLetterOrDigit(c) || c == '_');

    /// <summary>Numbers with ≥ 3 significant digits (8000, 0.025) or names longer than 3 chars smell like inputs.</summary>
    private static bool IsSuspiciousLiteral(CodeLiteral literal)
    {
        if (literal.Kind == "number")
        {
            var digits = literal.Value.Count(char.IsDigit);
            return digits >= 3 && !literal.Context.Contains("const ", StringComparison.Ordinal);
        }

        return literal.Value.Length >= 4 && literal.Value.Any(char.IsLetter) && !literal.Context.Contains("log(", StringComparison.Ordinal)
               && !literal.Context.Contains("throw ", StringComparison.Ordinal) && !literal.Context.Contains("Exception(", StringComparison.Ordinal);
    }

    private static string Quote(CodeLiteral literal) => literal.Kind == "string" ? $"\"{literal.Value}\"" : literal.Value;
}
