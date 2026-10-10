using System.Text.Json;
using System.Text.Json.Serialization;
using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>
///     The search-set registry: the 21 base sets (one per matrix group), detail sets (MEP systems, models, clash scope)
///     and auxiliary sets (elements no matrix group covers, so nothing is silently dropped). Discipline membership comes
///     from the ISO 19650 role code of the element's source file, so ARC and STR walls/floors never mix. Loaded from the
///     embedded <c>hp-base-sets.json</c> and checked on load: every group covered, every property and operator known.
/// </summary>
public sealed class BaseSetCatalog
{
    public const string ResourceName = "HPNavis.BIMCoordinator.hp-base-sets.json";
    private static readonly Lazy<BaseSetCatalog> Embedded = new(() => Parse(ClashMatrix.ReadResource(ResourceName), ClashMatrix.Default));

    public static BaseSetCatalog Default => Embedded.Value;

    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }

    /// <summary>Top-level selection-set folder that owns every set this engine writes; nothing outside it is touched.</summary>
    [JsonPropertyName("folder")] public string Folder { get; set; } = "";

    [JsonPropertyName("disciplineFolders")] public Dictionary<string, string> DisciplineFolders { get; set; } = new();
    [JsonPropertyName("properties")] public Dictionary<string, PropertyKey> Properties { get; set; } = new();
    [JsonPropertyName("disciplineRoles")] public Dictionary<string, List<string>> DisciplineRoles { get; set; } = new();
    [JsonPropertyName("sets")] public List<SetDefinition> Sets { get; set; } = new();
    [JsonPropertyName("details")] public List<SetDefinition> Details { get; set; } = new();
    [JsonPropertyName("auxiliary")] public List<SetDefinition> Auxiliary { get; set; } = new();

    public PropertyKey SourceFile => Property("sourceFile");

    public PropertyKey ElementCategory => Property("category");

    public PropertyKey Property(string key) =>
        Properties.TryGetValue(key, out var property) ? property : throw new InvalidOperationException($"property '{key}' is not in the catalog");

    public SetDefinition Set(string code) =>
        Sets.FirstOrDefault(s => s.Code == code) ?? throw new ArgumentException($"no base search set for group '{code}'");

    public IEnumerable<SetDefinition> AllDefinitions => Sets.Concat(Details).Concat(Auxiliary);

    public IReadOnlyList<string> RolesOf(string discipline) =>
        DisciplineRoles.TryGetValue(discipline, out var roles) ? roles : throw new InvalidOperationException($"no role codes for discipline {discipline}");

    /// <summary>The discipline whose role list contains <paramref name="roleCode" />, or null for a code nobody claims.</summary>
    public string? DisciplineOfRole(string roleCode) =>
        DisciplineRoles.FirstOrDefault(pair => pair.Value.Contains(roleCode, StringComparer.OrdinalIgnoreCase)).Key;

    public static BaseSetCatalog Parse(string json, ClashMatrix matrix)
    {
        var catalog = JsonSerializer.Deserialize<BaseSetCatalog>(json) ?? throw new InvalidOperationException("search-set registry JSON is empty");
        catalog.Validate(matrix);
        return catalog;
    }

    /// <summary>
    ///     Checks one definition against this catalog's property table (shared with the colour registry): categories,
    ///     known properties / operators / value types, and values that parse — a bad value must fail the load, not an
    ///     apply halfway through the registry.
    /// </summary>
    public void ValidateDefinition(SetDefinition definition, string registry = "search-set registry")
    {
        void Fail(string message) => throw new InvalidOperationException($"{registry}: {definition.Code}: {message}");

        if (definition.Categories.Count == 0) Fail("needs at least one category");
        if (definition.AllCategories.Contains(SearchSetPlan.AnyCategory) && definition.AllCategories.Count() > 1)
            Fail($"category '{SearchSetPlan.AnyCategory}' already takes every element and must stand alone");
        if (definition.AllCategories.Distinct(StringComparer.Ordinal).Count() != definition.AllCategories.Count()) Fail("a category is listed twice");
        foreach (var condition in definition.Conditions)
        {
            if (!Properties.ContainsKey(condition.Property)) Fail($"unknown property '{condition.Property}'");
            if (!ConditionDefinition.Operators.Contains(condition.Op)) Fail($"unknown operator '{condition.Op}'");
            if (!ConditionDefinition.Types.Contains(condition.Type)) Fail($"unknown value type '{condition.Type}'");
            if (condition.Op == "atLeast" && condition.Type != "lengthMm") Fail("atLeast needs type lengthMm");
            if (condition.Op == "like" && condition.Type != "string") Fail("like needs type string");
            if (condition.Values.Count > 0 && condition.Value.Length > 0) Fail("give either value or values, not both");
            // alternatives are ORed groups: (≠a) OR (≠b), or true OR false, would take every element
            if (condition.Values.Count > 1 && (condition.Op is "notEquals" or "atLeast" || condition.Type == "bool"))
                Fail($"values (alternatives) only work with equals/like on text, not {condition.Op} {condition.Type}");
            if (condition.Values.Distinct(StringComparer.Ordinal).Count() != condition.Values.Count) Fail("a value is listed twice");
            foreach (var value in condition.Alternatives)
            {
                if (string.IsNullOrWhiteSpace(value)) Fail("a condition value is empty");
                if (condition.Type == "bool" && !bool.TryParse(value, out _)) Fail($"'{value}' is not true/false");
                if (condition.Type == "lengthMm" && !(double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var mm) && mm > 0))
                    Fail($"'{value}' is not a positive length in mm");
            }
        }

        var groupsPerRole = definition.Categories.Count * definition.Conditions.Aggregate(1, (product, c) => product * c.Alternatives.Count) + definition.AlsoCategories.Count;
        if (groupsPerRole > MaxGroupsPerRole) Fail($"{groupsPerRole} condition groups per role code (categories × alternatives) exceed {MaxGroupsPerRole}; split the set");
    }

    /// <summary>One search is one OR of groups; past this the cartesian product of alternatives makes a search Navisworks crawls through.</summary>
    public const int MaxGroupsPerRole = 50;

    private void Validate(ClashMatrix matrix)
    {
        void Fail(string message) => throw new InvalidOperationException("search-set registry: " + message);

        if (string.IsNullOrWhiteSpace(Folder)) Fail("a top folder is required");
        var missing = matrix.Groups.Select(g => g.Code).Except(Sets.Select(s => s.Code)).ToList();
        if (missing.Count > 0) Fail($"base sets missing for group(s) {string.Join(", ", missing)}");
        if (Sets.Count != matrix.Groups.Count) Fail($"{Sets.Count} base sets for {matrix.Groups.Count} groups");
        foreach (var discipline in matrix.Groups.Select(g => g.Discipline).Distinct())
            if (!DisciplineFolders.ContainsKey(discipline) || !DisciplineRoles.ContainsKey(discipline)) Fail($"discipline {discipline} needs a folder and role codes");
        var claimed = DisciplineRoles.SelectMany(p => p.Value.Select(r => r.ToUpperInvariant())).ToList();
        if (claimed.Count != claimed.Distinct().Count()) Fail("a role code is claimed by two disciplines");
        foreach (var key in new[] { "sourceFile", "category" })
            if (!Properties.ContainsKey(key)) Fail($"property '{key}' is required");

        var codes = AllDefinitions.Select(d => d.Code).ToList();
        if (codes.Count != codes.Distinct().Count()) Fail("two sets share a code");
        foreach (var definition in AllDefinitions)
            ValidateDefinition(definition);

        // base sets take discipline, folder and roles from the matrix group, so the registry may not override them
        foreach (var set in Sets.Where(s => s.Roles.Count > 0 || s.Parent is not null || s.Folder is not null || s.Discipline is not null))
            Fail($"{set.Code}: a base set takes roles, folder and discipline from its group");

        foreach (var extra in Details.Concat(Auxiliary))
        {
            if (string.IsNullOrWhiteSpace(extra.Folder)) Fail($"{extra.Code} needs a folder");
            if (extra.Discipline is null || !DisciplineRoles.ContainsKey(extra.Discipline)) Fail($"{extra.Code} needs a known discipline");
            if (extra.Roles.Any(r => !RolesOf(extra.Discipline!).Contains(r))) Fail($"{extra.Code}: a role is not one of {extra.Discipline}");
        }

        foreach (var detail in Details)
        {
            var parent = Sets.FirstOrDefault(s => s.Code == detail.Parent);
            if (parent is null) { Fail($"{detail.Code}: a detail set needs a known parent"); continue; }
            if (matrix.Group(parent.Code).Discipline != detail.Discipline) Fail($"{detail.Code}: discipline differs from {parent.Code}");
            var outside = detail.AllCategories.Except(parent.Categories).ToList();
            if (outside.Count > 0) Fail($"{detail.Code}: categories {string.Join(", ", outside)} are not in {parent.Code}");
        }

        foreach (var auxiliary in Auxiliary.Where(a => a.Parent is not null))
            Fail($"{auxiliary.Code}: auxiliary sets have no parent");
    }
}
