using Autodesk.Navisworks.Api;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator.Probe;

/// <summary>One appended file (a child of the model root) and the discipline its ISO role code maps to.</summary>
public sealed record ProbedFile(string Name, string? Role, string? Discipline);

/// <summary>Whether a property the base sets rely on exists on a sample element, and under which names.</summary>
public sealed record ProbedProperty(string Expected, bool FoundByInternalName, bool FoundByDisplayName, string? SampleValue);

/// <summary>Element categories found per discipline, with counts, and the base-set categories nobody matched.</summary>
public sealed record DisciplineCategories(string Discipline, int Items, IReadOnlyDictionary<string, int> Categories, bool Truncated);

/// <summary>
///     Read-only survey the base search sets depend on: which appended files carry which role code, whether the
///     source-file and element-category properties exist under the names the catalog uses, and which Revit
///     categories each discipline really contains. The BIM coordinator reviews it before anything is written.
/// </summary>
public static class NavisModelProbe
{
    public const int MaxFiles = 200;
    private const int MaxFileDepth = 4;
    private const int MaxVisitedNodes = 20_000;
    private static readonly string[] ModelFileExtensions = { ".nwc", ".nwd", ".nwf", ".rvt", ".ifc", ".dwg", ".dgn", ".fbx", ".skp" };

    /// <summary>
    ///     The appended source files. In a federated NWD the file nodes sit under the model root (sometimes under a
    ///     group), while in a single appended model the root's children are levels — so a node counts as a file only when
    ///     its name ends with a model-file extension or carries an ISO role, searched at most four levels down.
    /// </summary>
    public static IReadOnlyList<ProbedFile> Files(Document doc, BaseSetCatalog catalog)
    {
        var files = new List<ProbedFile>();
        var budget = new[] { MaxVisitedNodes };
        foreach (var model in doc.Models)
            Collect(model.RootItem, 0, catalog, files, budget);
        return files;
    }

    private static void Collect(ModelItem item, int depth, BaseSetCatalog catalog, List<ProbedFile> files, int[] budget)
    {
        if (files.Count >= MaxFiles || --budget[0] < 0) return;
        var name = item.DisplayName ?? "";
        var role = IsoFileName.Role(name);
        var isFile = role is not null || ModelFileExtensions.Any(ext => name.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        // a model root with children is the container (the federated .nwd): its files are below it
        var isContainer = depth == 0 && item.Children.Any();
        if (isFile && !isContainer)
        {
            files.Add(new ProbedFile(name, role, role is null ? null : catalog.DisciplineOfRole(role)));
            return;
        }

        if (depth >= MaxFileDepth) return;
        foreach (var child in item.Children)
            Collect(child, depth + 1, catalog, files, budget);
    }
    /// <summary>
    ///     Looks for the two catalog properties on the first items of the model, by internal and by display name. Not only
    ///     geometry: the Revit Element tab sits on the composite element node, above the solids.
    /// </summary>
    public static IReadOnlyList<ProbedProperty> Properties(Document doc, BaseSetCatalog catalog, int sampleSize)
    {
        var sample = doc.Models.RootItems.SelectMany(root => root.Descendants).Take(sampleSize).ToList();
        return new[] { catalog.SourceFile, catalog.ElementCategory }.Select(key =>
        {
            var byName = sample.Select(item => item.PropertyCategories.FindPropertyByName(key.Category, key.Property)).FirstOrDefault(p => p is not null);
            var byDisplay = sample.Select(item => item.PropertyCategories.FindPropertyByDisplayName(key.CategoryDisplay, key.PropertyDisplay)).FirstOrDefault(p => p is not null);
            var value = (byName ?? byDisplay)?.Value;
            return new ProbedProperty(key.ToString(), byName is not null, byDisplay is not null, value is { IsDisplayString: true } ? value.ToDisplayString() : value?.DataType.ToString());
        }).ToList();
    }

    /// <summary>Category histogram of every element of a discipline (role-code search), stopped at <paramref name="maxItems" />.</summary>
    public static DisciplineCategories Categories(Document doc, BaseSetCatalog catalog, string discipline, int maxItems, CancellationToken ct)
    {
        var search = new Search();
        search.Selection.SelectAll();
        search.Locations = SearchLocations.DescendantsAndSelf;
        search.PruneBelowMatch = true;
        foreach (var role in catalog.RolesOf(discipline))
        {
            search.SearchConditions.AddGroup(new[]
            {
                NavisSearchSetCompiler.ToCondition(new ConditionSpec(catalog.SourceFile, ConditionKind.Wildcard, SearchSetPlan.RoleWildcard(role)), 1.0),
                SearchCondition.HasPropertyByName(catalog.ElementCategory.Category, catalog.ElementCategory.Property),
            });
        }

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var seen = 0;
        foreach (var item in search.FindIncremental(doc, false))
        {
            ct.ThrowIfCancellationRequested();
            if (seen++ >= maxItems) return new DisciplineCategories(discipline, seen - 1, counts, true);
            var value = item.PropertyCategories.FindPropertyByName(catalog.ElementCategory.Category, catalog.ElementCategory.Property)?.Value;
            var category = value is { IsDisplayString: true } ? value.ToDisplayString() : "(no category)";
            counts[category] = counts.TryGetValue(category, out var n) ? n + 1 : 1;
        }

        return new DisciplineCategories(discipline, seen, counts, false);
    }
}
