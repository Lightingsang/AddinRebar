using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>One property test inside a search, in catalog terms (mm, display strings), independent of Navisworks.</summary>
public sealed record ConditionSpec(PropertyKey Property, ConditionKind Kind, string Value, string ValueType = "string")
{
    public override string ToString() => Kind switch
    {
        ConditionKind.Wildcard => $"{Property} like '{Value}'",
        ConditionKind.NotEquals => $"{Property} <> {Value} (or absent)",
        ConditionKind.AtLeast => $"{Property} >= {Value} mm",
        _ => $"{Property} = {Value}",
    };
}

public enum ConditionKind
{
    Equals,
    NotEquals,
    Wildcard,
    AtLeast,
}

public enum SetKind
{
    Base,
    Detail,
    Auxiliary,
    Color,
}

/// <summary>
///     A search set as data: an OR of AND-groups (disjunctive normal form), because a Navisworks search ORs its
///     condition groups and ANDs the conditions inside one group (verified live). Each group is "source file has role R"
///     AND "element category is C" AND the set's extra conditions (category tested first) — roles × categories groups in all.
/// </summary>
public sealed record SearchSetPlan(
    string Code, SetKind Kind, string Discipline, string? Parent, IReadOnlyList<string> FolderPath, string DisplayName,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Categories, IReadOnlyList<IReadOnlyList<ConditionSpec>> Groups, string Evidence)
{
    public string Folder => string.Join("/", FolderPath);

    /// <summary>
    ///     Wildcard on an element's source file: the role, then the four-character number, then anything — Revit
    ///     elements report the local model they were exported from, e.g. <c>THCSLT-HPC-TT-ZZ-M3-AA-0001_sangtq6ANLE.rvt</c>
    ///     (a user suffix after the number). A volume code equal to a role code would also match; ISO volume codes here
    ///     (TT, NCR, LH_HB_HC…) never are.
    /// </summary>
    public static string RoleWildcard(string role) => $"*-{role}-????*";

    public static SearchSetPlan For(BaseSetCatalog catalog, ClashMatrix matrix, string code)
    {
        var definition = catalog.AllDefinitions.FirstOrDefault(d => d.Code == code) ?? throw new ArgumentException($"no search set '{code}' in the registry");
        if (catalog.Sets.Contains(definition))
        {
            var discipline = matrix.Group(code).Discipline;
            return Build(catalog, definition, SetKind.Base, discipline, new[] { catalog.Folder, catalog.DisciplineFolders[discipline] });
        }

        var kind = catalog.Details.Contains(definition) ? SetKind.Detail : SetKind.Auxiliary;
        var path = new[] { catalog.Folder }.Concat(definition.Folder!.Split('/').Where(p => p.Length > 0)).ToArray();
        return Build(catalog, definition, kind, definition.Discipline!, path);
    }

    /// <summary>Base sets by default; detail and auxiliary sets on request; or exactly the given codes.</summary>
    public static IReadOnlyList<SearchSetPlan> All(BaseSetCatalog catalog, ClashMatrix matrix, IReadOnlyCollection<string>? codes = null, bool includeExtras = false)
    {
        IEnumerable<string> wanted = codes is { Count: > 0 } ? codes
            : includeExtras ? catalog.AllDefinitions.Select(d => d.Code) : matrix.Groups.Select(g => g.Code);
        return wanted.Select(code => For(catalog, matrix, code)).ToList();
    }

    /// <summary>A plan for a definition that lives outside the base registry (the colour sets), in the given folder.</summary>
    public static SearchSetPlan ForDefinition(BaseSetCatalog catalog, SetDefinition definition, SetKind kind, string discipline, IReadOnlyList<string> path) =>
        Build(catalog, definition, kind, discipline, path);

    private static SearchSetPlan Build(BaseSetCatalog catalog, SetDefinition definition, SetKind kind, string discipline, IReadOnlyList<string> path)
    {
        var roles = definition.Roles.Count > 0 ? definition.Roles : catalog.RolesOf(discipline).ToList();
        var extras = Alternatives(catalog, definition.Conditions);
        var groups = new List<IReadOnlyList<ConditionSpec>>();
        foreach (var role in roles)
        {
            var source = new ConditionSpec(catalog.SourceFile, ConditionKind.Wildcard, RoleWildcard(role));
            foreach (var category in definition.Categories)
                groups.AddRange(extras.Select(extra => Group(source, catalog, category, extra)));
            groups.AddRange(definition.AlsoCategories.Select(category => Group(source, catalog, category, Array.Empty<ConditionSpec>())));
        }

        return new SearchSetPlan(definition.Code, kind, discipline, definition.Parent, path, definition.DisplayName,
            roles, definition.AllCategories.ToList(), groups, definition.Evidence);
    }

    /// <summary>Every combination of the conditions' alternatives (one extra-condition list per group); a single empty list without conditions.</summary>
    private static IReadOnlyList<IReadOnlyList<ConditionSpec>> Alternatives(BaseSetCatalog catalog, IReadOnlyList<ConditionDefinition> conditions)
    {
        IEnumerable<IReadOnlyList<ConditionSpec>> combinations = new[] { (IReadOnlyList<ConditionSpec>)Array.Empty<ConditionSpec>() };
        foreach (var condition in conditions)
        {
            var specs = condition.Alternatives.Select(value => ToSpec(catalog, condition, value)).ToList();
            combinations = combinations.SelectMany(prefix => specs.Select(spec => (IReadOnlyList<ConditionSpec>)prefix.Concat(new[] { spec }).ToList())).ToList();
        }

        return combinations.ToList();
    }

    /// <summary>Category <c>*</c> = any element with a Revit category (a discipline-wide set such as <c>HP_A_All</c>).</summary>
    public const string AnyCategory = "*";

    private static IReadOnlyList<ConditionSpec> Group(ConditionSpec source, BaseSetCatalog catalog, string category, IReadOnlyList<ConditionSpec> extra) =>
        // category first: Navisworks stops a group at its first failing condition, and the category test is far more
        // selective than the source-file wildcard (live: M5 4.7 s instead of 20.9 s, same 1 089 elements)
        new[]
        {
            category == AnyCategory
                ? new ConditionSpec(catalog.ElementCategory, ConditionKind.Wildcard, AnyCategory)
                : new ConditionSpec(catalog.ElementCategory, ConditionKind.Equals, category),
            source,
        }.Concat(extra).ToList();

    private static ConditionSpec ToSpec(BaseSetCatalog catalog, ConditionDefinition condition, string value) =>
        new(catalog.Property(condition.Property),
            condition.Op switch { "notEquals" => ConditionKind.NotEquals, "atLeast" => ConditionKind.AtLeast, "like" => ConditionKind.Wildcard, _ => ConditionKind.Equals },
            value, condition.Type);
}
