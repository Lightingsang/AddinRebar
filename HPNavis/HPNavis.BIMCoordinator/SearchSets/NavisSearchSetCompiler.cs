using Autodesk.Navisworks.Api;

namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>Outcome of one set: its live item count and what apply did (or would do).</summary>
public sealed record SearchSetOutcome(string Code, string Kind, string Discipline, string Folder, string Name, int Items, string Action, int Groups);

/// <summary>
///     Turns <see cref="SearchSetPlan" />s into dynamic Navisworks search sets under the plan's folder path. Upsert by
///     display name inside that folder: identical search → <c>unchanged</c>, absent → <c>created</c>, different →
///     <c>conflict</c> (left as it is) unless the caller allows updates, then <c>updated</c> (replaced in place). Sets
///     outside the plan folders are never written and nothing is ever removed. Navisworks main thread only.
/// </summary>
public static class NavisSearchSetCompiler
{
    public static Search BuildSearch(SearchSetPlan plan, double mmPerUnit)
    {
        var search = new Search();
        search.Selection.SelectAll();
        search.Locations = SearchLocations.DescendantsAndSelf;
        search.PruneBelowMatch = true;
        foreach (var group in plan.Groups) search.SearchConditions.AddGroup(group.Select(spec => ToCondition(spec, mmPerUnit)).ToList());
        return search;
    }

    public static SearchCondition ToCondition(ConditionSpec spec, double mmPerUnit)
    {
        var condition = SearchCondition.HasPropertyByName(spec.Property.Category, spec.Property.Property);
        return spec.Kind switch
        {
            ConditionKind.Wildcard => condition.DisplayStringWildcard(spec.Value).IgnoreStringValueCase(),
            ConditionKind.NotEquals => condition.EqualValue(Value(spec)).Negate(),
            ConditionKind.AtLeast => condition.CompareWith(SearchConditionComparison.NumericGreaterThanOrEqual,
                VariantData.FromDoubleLength(double.Parse(spec.Value, System.Globalization.CultureInfo.InvariantCulture) / mmPerUnit)),
            _ => condition.EqualValue(Value(spec)),
        };
    }

    private static VariantData Value(ConditionSpec spec) =>
        spec.ValueType == "bool" ? VariantData.FromBoolean(bool.Parse(spec.Value)) : VariantData.FromDisplayString(spec.Value);

    /// <summary>Counts what each plan finds now and what apply would do, without writing anything.</summary>
    public static IReadOnlyList<SearchSetOutcome> Preview(Document doc, IReadOnlyList<SearchSetPlan> plans, double mmPerUnit, CancellationToken ct) =>
        Run(doc, plans, mmPerUnit, write: false, allowUpdate: false, ct);

    /// <summary>Writes missing sets (and differing ones when <paramref name="allowUpdate" />); returns what was done.</summary>
    public static IReadOnlyList<SearchSetOutcome> Apply(Document doc, IReadOnlyList<SearchSetPlan> plans, double mmPerUnit, bool allowUpdate, CancellationToken ct) =>
        Run(doc, plans, mmPerUnit, write: true, allowUpdate, ct);

    private static IReadOnlyList<SearchSetOutcome> Run(Document doc, IReadOnlyList<SearchSetPlan> plans, double mmPerUnit, bool write, bool allowUpdate, CancellationToken ct)
    {
        var outcomes = new List<SearchSetOutcome>();
        foreach (var plan in plans)
        {
            ct.ThrowIfCancellationRequested();
            var search = BuildSearch(plan, mmPerUnit);
            var count = search.FindAll(doc, false).Count;
            var existing = FindSet(doc, plan);
            var decision = SetUpsert.Decide(existing is not null, existing is { HasSearch: true } && existing.Search.ValueEquals(search), allowUpdate);
            var action = decision switch
            {
                UpsertAction.Create => write ? Create(doc, plan, search) : "create",
                UpsertAction.Update => write ? Replace(doc, plan, search) : "update",
                UpsertAction.Unchanged => "unchanged",
                _ => "conflict",
            };
            outcomes.Add(new SearchSetOutcome(plan.Code, plan.Kind.ToString(), plan.Discipline, plan.Folder, plan.DisplayName, count, action, plan.Groups.Count));
        }

        return outcomes;
    }

    private static string Create(Document doc, SearchSetPlan plan, Search search)
    {
        doc.SelectionSets.AddCopy(EnsureFolder(doc, plan.FolderPath), new SelectionSet(search) { DisplayName = plan.DisplayName });
        return "created";
    }

    private static string Replace(Document doc, SearchSetPlan plan, Search search)
    {
        var parent = FindFolder(doc, plan.FolderPath)!;
        doc.SelectionSets.ReplaceWithCopy(parent, IndexOfSet(parent, plan.DisplayName), new SelectionSet(search) { DisplayName = plan.DisplayName });
        return "updated";
    }

    /// <summary>The saved set a plan owns, or null; looked up fresh because every edit replaces the document's copies.</summary>
    public static SelectionSet? FindSet(Document doc, SearchSetPlan plan) =>
        FindFolder(doc, plan.FolderPath)?.Children.OfType<SelectionSet>().FirstOrDefault(s => s.DisplayName == plan.DisplayName);

    /// <summary>Folder path + name of a saved item, e.g. <c>HP BIMCoordinator/MEP/M4 Pipes and Pipe Accessories</c>.</summary>
    public static string PathOf(Document doc, SavedItem item)
    {
        var names = new List<string>();
        GroupItem current = doc.SelectionSets.RootItem;
        foreach (var index in doc.SelectionSets.CreateIndexPath(item))
        {
            var child = current.Children[index];
            names.Add(child.DisplayName);
            if (child is GroupItem group) current = group;
        }

        return string.Join("/", names);
    }

    /// <summary>Paths of every saved set below a folder (recursively), for the orphan report.</summary>
    public static IReadOnlyList<string> SetPathsUnder(Document doc, IReadOnlyList<string> folder)
    {
        var paths = new List<string>();
        void Walk(GroupItem group, string prefix)
        {
            foreach (var child in group.Children)
            {
                if (child is SelectionSet) paths.Add($"{prefix}/{child.DisplayName}");
                else if (child is GroupItem sub) Walk(sub, $"{prefix}/{child.DisplayName}");
            }
        }

        if (FindFolder(doc, folder) is { } start) Walk(start, string.Join("/", folder));
        return paths;
    }

    public static GroupItem? FindFolder(Document doc, IReadOnlyList<string> path)
    {
        GroupItem current = doc.SelectionSets.RootItem;
        foreach (var name in path)
        {
            var next = current.Children.OfType<FolderItem>().FirstOrDefault(f => f.DisplayName == name);
            if (next is null) return null;
            current = next;
        }

        return current;
    }

    private static int IndexOfSet(GroupItem parent, string displayName)
    {
        for (var i = 0; i < parent.Children.Count; i++)
            if (parent.Children[i] is SelectionSet set && set.DisplayName == displayName) return i;
        return -1;
    }

    /// <summary>Creates the missing folders of the path one level at a time, re-reading the tree after each add.</summary>
    private static GroupItem EnsureFolder(Document doc, IReadOnlyList<string> path)
    {
        for (var depth = 1; depth <= path.Count; depth++)
        {
            if (FindFolder(doc, path.Take(depth).ToList()) is not null) continue;
            var parent = FindFolder(doc, path.Take(depth - 1).ToList())!;
            var folder = new FolderItem { DisplayName = path[depth - 1] };
            if (depth == 1) doc.SelectionSets.AddCopy(folder);
            else doc.SelectionSets.AddCopy(parent, folder);
        }

        return FindFolder(doc, path) ?? throw new InvalidOperationException($"folder {string.Join("/", path)} was not created");
    }
}
