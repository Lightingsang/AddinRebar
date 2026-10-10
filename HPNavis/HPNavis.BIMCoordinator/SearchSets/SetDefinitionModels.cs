using System.Text.Json.Serialization;

namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>Internal (language-independent, what searches use) and display names of one Navisworks property.</summary>
public sealed class PropertyKey
{
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("property")] public string Property { get; set; } = "";
    [JsonPropertyName("categoryDisplay")] public string CategoryDisplay { get; set; } = "";
    [JsonPropertyName("propertyDisplay")] public string PropertyDisplay { get; set; } = "";

    /// <summary>
    ///     Search by the display names. Only for a tab whose internal name is shared by many tabs: the Revit
    ///     <c>System Type</c> tab is one of many <c>LcRevitData_Parameter</c> tabs, so its <c>Name</c> is only unique by
    ///     display name (English UI). The element's own System Type property is an element reference, not text.
    /// </summary>
    [JsonPropertyName("byDisplayName")] public bool ByDisplayName { get; set; }

    public override string ToString() => $"{CategoryDisplay} > {PropertyDisplay}";
}

/// <summary>
///     An extra test ANDed with the category of a set: <c>equals</c> / <c>notEquals</c> (a display string, or a boolean
///     when <c>type</c> is <c>bool</c>; notEquals also matches elements that lack the property), <c>like</c> (a display
///     string wildcard, <c>*</c> and <c>?</c>), or <c>atLeast</c> on a length given in millimetres (<c>type</c>
///     <c>lengthMm</c>). <see cref="Values" /> lists alternatives (OR): the set gets one condition group per value.
/// </summary>
public sealed class ConditionDefinition
{
    public static readonly string[] Operators = { "equals", "notEquals", "like", "atLeast" };
    public static readonly string[] Types = { "string", "bool", "lengthMm" };

    /// <summary>Key into the catalog's <c>properties</c> table.</summary>
    [JsonPropertyName("property")] public string Property { get; set; } = "";
    [JsonPropertyName("op")] public string Op { get; set; } = "equals";
    [JsonPropertyName("type")] public string Type { get; set; } = "string";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("values")] public List<string> Values { get; set; } = new();

    /// <summary>The alternatives this condition stands for: <see cref="Values" /> when given, else the single <see cref="Value" />.</summary>
    public IReadOnlyList<string> Alternatives => Values.Count > 0 ? Values : new[] { Value };
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

    /// <summary>Exact name of the saved set when the source dictates it (the colour sheet's <c>HP_P_Drainage_RainWater</c>).</summary>
    [JsonPropertyName("displayName")] public string? DisplayNameOverride { get; set; }

    /// <summary>Display name of the saved search set, e.g. <c>A1 Furniture</c>; it is the set's identity on re-apply.</summary>
    public string DisplayName => DisplayNameOverride ?? $"{Code} {Name}";

    public IEnumerable<string> AllCategories => Categories.Concat(AlsoCategories);
}
