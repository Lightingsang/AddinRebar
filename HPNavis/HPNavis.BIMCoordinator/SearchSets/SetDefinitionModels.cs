using System.Text.Json.Serialization;

namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>Internal (language-independent, what searches use) and display names of one Navisworks property.</summary>
public sealed class PropertyKey
{
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("property")] public string Property { get; set; } = "";
    [JsonPropertyName("categoryDisplay")] public string CategoryDisplay { get; set; } = "";
    [JsonPropertyName("propertyDisplay")] public string PropertyDisplay { get; set; } = "";

    public override string ToString() => $"{CategoryDisplay} > {PropertyDisplay}";
}

/// <summary>
///     An extra test ANDed with the category of a set: <c>equals</c> / <c>notEquals</c> (a display string, or a boolean
///     when <c>type</c> is <c>bool</c>; notEquals also matches elements that lack the property), or <c>atLeast</c> on a
///     length given in millimetres (<c>type</c> <c>lengthMm</c>).
/// </summary>
public sealed class ConditionDefinition
{
    public static readonly string[] Operators = { "equals", "notEquals", "atLeast" };
    public static readonly string[] Types = { "string", "bool", "lengthMm" };

    /// <summary>Key into the catalog's <c>properties</c> table.</summary>
    [JsonPropertyName("property")] public string Property { get; set; } = "";
    [JsonPropertyName("op")] public string Op { get; set; } = "equals";
    [JsonPropertyName("type")] public string Type { get; set; } = "string";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
}

/// <summary>
///     One search set as data. Base sets (the 21 matrix groups) take their discipline from the group code; detail and
///     auxiliary sets name it, may narrow it to some role codes, and live in their own folder. <see cref="Categories" />
///     get the <see cref="Conditions" />; <see cref="AlsoCategories" /> join the set without them.
/// </summary>
public sealed class SetDefinition
{
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("categories")] public List<string> Categories { get; set; } = new();
    [JsonPropertyName("conditions")] public List<ConditionDefinition> Conditions { get; set; } = new();
    [JsonPropertyName("alsoCategories")] public List<string> AlsoCategories { get; set; } = new();

    /// <summary>Why the set looks like this: the legacy set, sheet row or live fact it comes from.</summary>
    [JsonPropertyName("evidence")] public string Evidence { get; set; } = "";

    [JsonPropertyName("parent")] public string? Parent { get; set; }

    /// <summary>Folder below the catalog's top folder, '/'-separated (detail and auxiliary sets).</summary>
    [JsonPropertyName("folder")] public string? Folder { get; set; }

    [JsonPropertyName("discipline")] public string? Discipline { get; set; }

    /// <summary>Only these role codes of the discipline (e.g. one model of three); empty = every role of it.</summary>
    [JsonPropertyName("roles")] public List<string> Roles { get; set; } = new();

    /// <summary>Display name of the saved search set, e.g. <c>A1 Furniture</c>; it is the set's identity on re-apply.</summary>
    public string DisplayName => $"{Code} {Name}";

    public IEnumerable<string> AllCategories => Categories.Concat(AlsoCategories);
}
