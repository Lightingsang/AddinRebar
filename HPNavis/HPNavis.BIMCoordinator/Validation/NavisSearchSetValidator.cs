using System.Diagnostics;
using Autodesk.Navisworks.Api;
using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator.Validation;

/// <summary>
///     Read-only acceptance check of the search-set registry against the open model: each set's count from its search
///     and from the saved set (they must agree), empty sets, items of a base set whose category or source-file role is
///     not the set's, overlaps between base sets, detail sets leaving their parent or overlapping inside one folder,
///     parent items no detail covers, and every Revit element no base set takes (the gaps, by discipline and category).
///     Membership uses <see cref="ModelItem" /> value equality (verified live on 8 623 walls).
/// </summary>
public static class NavisSearchSetValidator
{
    /// <summary>
    ///     <paramref name="scope" /> <c>base</c> = the 21 base sets + gaps (~1 min on a 95 MB model), <c>extras</c> = detail and
    ///     auxiliary sets with the parents their checks need; each fits one 120 s run.
    /// </summary>
    public static SearchSetValidation Validate(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, double mmPerUnit, string scope, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        var plans = Scope(catalog, matrix, scope);
        var members = new Dictionary<string, HashSet<ModelItem>>(StringComparer.Ordinal);
        var checks = new List<SetCheck>();
        var issues = new List<ValidationIssue>();
        foreach (var plan in plans)
        {
            ct.ThrowIfCancellationRequested();
            var search = NavisSearchSetCompiler.BuildSearch(plan, mmPerUnit);
            var (check, items) = Resolve(doc, plan, search, issues);
            members[plan.Code] = items;
            checks.Add(check);
            if (items.Count == 0)
                issues.Add(new ValidationIssue(plan.Kind == SetKind.Base ? "warning" : "info", "EMPTY", plan.Code, $"{plan.DisplayName} finds no element in this model", 0));
        }

        var basePlans = plans.Where(p => p.Kind == SetKind.Base).ToList();
        CheckMembership(catalog, basePlans, members, issues, ct);
        CheckOverlaps(basePlans, members, issues);
        CheckDetails(plans, members, issues);
        var allBase = basePlans.Count == matrix.Groups.Count;
        var (gaps, checkedCount) = allBase ? Gaps(doc, catalog, basePlans, members, issues, ct) : (new Dictionary<string, int>(), 0);
        return new SearchSetValidation(checks, issues, gaps, checkedCount, clock.ElapsedMilliseconds);
    }

    private static IReadOnlyList<SearchSetPlan> Scope(BaseSetCatalog catalog, ClashMatrix matrix, string scope)
    {
        switch (scope)
        {
            case "base":
                return SearchSetPlan.All(catalog, matrix);
            case "extras":
                var extras = catalog.Details.Concat(catalog.Auxiliary).ToList();
                var parents = extras.Select(d => d.Parent).Where(p => p is not null).Distinct().Select(p => p!);
                return SearchSetPlan.All(catalog, matrix, parents.Concat(extras.Select(d => d.Code)).ToList());
            default:
                throw new ArgumentException($"scope '{scope}' is not base or extras");
        }
    }

    /// <summary>
    ///     The elements of a set as Navisworks resolves the saved set (what the user gets clicking it). The registry search
    ///     is evaluated as well only when the saved set is missing or its search differs — each evaluation is a full walk.
    /// </summary>
    private static (SetCheck Check, HashSet<ModelItem> Items) Resolve(Document doc, SearchSetPlan plan, Search search, List<ValidationIssue> issues)
    {
        var saved = NavisSearchSetCompiler.FindSet(doc, plan);
        if (saved is null)
        {
            var found = new HashSet<ModelItem>(search.FindAll(doc, false));
            issues.Add(new ValidationIssue("error", "NOT_SAVED", plan.Code, $"{plan.Folder}/{plan.DisplayName} is not in the document", 0));
            return (new SetCheck(plan.Code, plan.Kind.ToString(), plan.DisplayName, plan.Folder, found.Count, null, "missing"), found);
        }

        var resolved = new HashSet<ModelItem>(saved.GetSelectedItems(doc));
        if (saved.HasSearch && saved.Search.ValueEquals(search))
            return (new SetCheck(plan.Code, plan.Kind.ToString(), plan.DisplayName, plan.Folder, resolved.Count, resolved.Count, "same"), resolved);

        var registry = search.FindAll(doc, false).Count;
        issues.Add(new ValidationIssue("error", "SAVED_DIFFERS", plan.Code, $"the saved set is not the registry's search (static set or edited conditions): saved {resolved.Count}, registry {registry}", resolved.Count - registry));
        return (new SetCheck(plan.Code, plan.Kind.ToString(), plan.DisplayName, plan.Folder, registry, resolved.Count, "differs"), resolved);
    }

    private static void CheckMembership(BaseSetCatalog catalog, IReadOnlyList<SearchSetPlan> basePlans, Dictionary<string, HashSet<ModelItem>> members, List<ValidationIssue> issues, CancellationToken ct)
    {
        foreach (var plan in basePlans)
        {
            int wrongCategory = 0, wrongRole = 0;
            foreach (var item in members[plan.Code])
            {
                ct.ThrowIfCancellationRequested();
                if (!plan.Categories.Contains(Text(item, catalog.ElementCategory))) wrongCategory++;
                var role = IsoFileName.Role(Text(item, catalog.SourceFile));
                if (role is null || !plan.Roles.Contains(role)) wrongRole++;
            }

            if (wrongCategory > 0) issues.Add(new ValidationIssue("error", "WRONG_CATEGORY", plan.Code, "elements whose Revit category is not one of the set's", wrongCategory));
            if (wrongRole > 0) issues.Add(new ValidationIssue("error", "WRONG_DISCIPLINE", plan.Code, $"elements whose source file is not a {plan.Discipline} model", wrongRole));
        }
    }

    private static void CheckOverlaps(IReadOnlyList<SearchSetPlan> basePlans, Dictionary<string, HashSet<ModelItem>> members, List<ValidationIssue> issues)
    {
        for (var i = 0; i < basePlans.Count; i++)
        for (var j = i + 1; j < basePlans.Count; j++)
        {
            var shared = members[basePlans[i].Code].Count(members[basePlans[j].Code].Contains);
            if (shared > 0) issues.Add(new ValidationIssue("error", "OVERLAP", $"{basePlans[i].Code}+{basePlans[j].Code}", "elements in two base sets", shared));
        }
    }

    private static void CheckDetails(IReadOnlyList<SearchSetPlan> plans, Dictionary<string, HashSet<ModelItem>> members, List<ValidationIssue> issues)
    {
        foreach (var family in plans.Where(p => p.Kind == SetKind.Detail && p.Parent is not null && members.ContainsKey(p.Parent)).GroupBy(p => (p.Parent!, p.Folder)))
        {
            var parent = members[family.Key.Item1];
            var details = family.ToList();
            foreach (var detail in details)
            {
                var outside = members[detail.Code].Count(item => !parent.Contains(item));
                if (outside > 0) issues.Add(new ValidationIssue("error", "DETAIL_OUTSIDE_PARENT", detail.Code, $"elements not in {family.Key.Item1}", outside));
            }

            for (var i = 0; i < details.Count; i++)
            for (var j = i + 1; j < details.Count; j++)
            {
                var shared = members[details[i].Code].Count(members[details[j].Code].Contains);
                if (shared > 0) issues.Add(new ValidationIssue("warning", "DETAIL_OVERLAP", $"{details[i].Code}+{details[j].Code}", "elements in two detail sets of one folder", shared));
            }

            var covered = new HashSet<ModelItem>(details.SelectMany(d => members[d.Code]));
            var uncovered = parent.Count(item => !covered.Contains(item));
            if (uncovered > 0) issues.Add(new ValidationIssue("info", "NOT_COVERED_BY_DETAILS", family.Key.Item1, $"{family.Key.Item1} elements in no set of {family.Key.Item2}", uncovered));
        }
    }

    /// <summary>Every Revit element (it has an Element category) that no base set takes, counted by discipline and category.</summary>
    private static (IReadOnlyDictionary<string, int> Gaps, int Checked) Gaps(Document doc, BaseSetCatalog catalog, IReadOnlyList<SearchSetPlan> basePlans,
        Dictionary<string, HashSet<ModelItem>> members, List<ValidationIssue> issues, CancellationToken ct)
    {
        var taken = new HashSet<ModelItem>(basePlans.SelectMany(p => members[p.Code]));
        var search = new Search();
        search.Selection.SelectAll();
        search.Locations = SearchLocations.DescendantsAndSelf;
        search.PruneBelowMatch = true;
        search.SearchConditions.Add(SearchCondition.HasPropertyByName(catalog.ElementCategory.Category, catalog.ElementCategory.Property));
        var gaps = new Dictionary<string, int>(StringComparer.Ordinal);
        var count = 0;
        var noRole = 0;
        foreach (var item in search.FindIncremental(doc, false))
        {
            if (++count % 2000 == 0) ct.ThrowIfCancellationRequested();
            if (taken.Contains(item)) continue;
            var role = IsoFileName.Role(Text(item, catalog.SourceFile));
            var discipline = role is null ? null : catalog.DisciplineOfRole(role);
            if (discipline is null) noRole++;
            var key = $"{discipline ?? "?"}|{Text(item, catalog.ElementCategory)}";
            gaps[key] = gaps.TryGetValue(key, out var n) ? n + 1 : 1;
        }

        if (noRole > 0) issues.Add(new ValidationIssue("warning", "NO_ROLE", null, "elements whose source file has no known ISO role code", noRole));
        var total = gaps.Values.Sum();
        if (total > 0) issues.Add(new ValidationIssue("info", "GAP", null, "Revit elements in no base set (see gaps by discipline|category)", total));
        return (gaps.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, p => p.Value), count);
    }

    private static string? Text(ModelItem item, PropertyKey key)
    {
        var value = item.PropertyCategories.FindPropertyByName(key.Category, key.Property)?.Value;
        return value is { IsDisplayString: true } ? value.ToDisplayString() : null;
    }
}
