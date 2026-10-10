using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator.ClashTests;

/// <summary>
///     Reads the document into the planner's inputs and writes a <see cref="ClashTestPlan" />: a new Hard test per
///     <see cref="PlanAction.Create" />, <c>TestsEditTestFromCopy</c> per <see cref="PlanAction.Update" /> (results,
///     statuses and comments stay; Navisworks marks the test Old until it runs again). Never removes a test and
///     never runs one — running is heavy and stays in the seed's own code where the bridge's heavy gate sees it.
///     Tolerance is converted with <paramref name="mmPerUnit" /> (millimetres per document unit) and read back.
/// </summary>
public static class NavisClashTestCompiler
{
    public static ClashTestPlan Plan(Document doc, ClashMatrix matrix, BaseSetCatalog catalog, string lod, double mmPerUnit,
        IReadOnlyCollection<int>? priorities, IReadOnlyCollection<string>? ruleIds, CancellationToken ct)
    {
        matrix.RulesFor(lod, priorities, ruleIds); // caller mistakes fail before the ~1 min scan of the sets below
        var sets = new Dictionary<string, SetState>(StringComparer.Ordinal);
        foreach (var plan in SearchSetPlan.All(catalog, matrix))
        {
            ct.ThrowIfCancellationRequested();
            var saved = NavisSearchSetCompiler.FindSet(doc, plan);
            // empty or not is all the plan needs: FindFirst stops at the first match (21 full searches take ~1 min on a 95 MB model)
            sets[plan.Code] = saved is null ? new SetState(false, null)
                : new SetState(true, saved.HasSearch ? saved.Search.FindFirst(doc, false) is not null : saved.GetSelectedItems(doc).Count > 0);
        }

        return ClashTestPlanner.Plan(matrix, lod, code => SetPath(catalog, matrix, code), sets, ExistingTests(doc, mmPerUnit), priorities, ruleIds);
    }

    /// <summary>
    ///     A test's selection is identified by the set's full path, not its name: a test still bound to a set of the same
    ///     name in an older folder (e.g. before the registry moved to Architecture/Structure/MEP) must be re-pointed.
    /// </summary>
    public static string SetPath(BaseSetCatalog catalog, ClashMatrix matrix, string code)
    {
        var plan = SearchSetPlan.For(catalog, matrix, code);
        return $"{plan.Folder}/{plan.DisplayName}";
    }

    public static IReadOnlyList<ExistingTest> ExistingTests(Document doc, double mmPerUnit) =>
        AllTests(doc).Select(test => new ExistingTest(
            test.DisplayName, test.TestType.ToString(), test.Tolerance * mmPerUnit,
            SetName(doc, test.SelectionA), SetName(doc, test.SelectionB), test.Priority)).ToList();

    /// <summary>Writes every Create/Update of the plan; returns the names written.</summary>
    public static IReadOnlyList<string> Apply(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, ClashTestPlan plan, double mmPerUnit, CancellationToken ct)
    {
        var clash = doc.GetClash().TestsData;
        var written = new List<string>();
        foreach (var planned in plan.ToWrite)
        {
            ct.ThrowIfCancellationRequested();
            var rule = matrix.Rules.First(r => r.Id == planned.RuleId);
            var current = planned.CurrentName is null ? null : AllTests(doc).FirstOrDefault(t => t.DisplayName == planned.CurrentName);
            // an update starts from a copy of the stored test so settings the matrix does not own (ignore rules,
            // primitive types, assignee) survive; a create starts from a blank Hard test
            var test = current is null ? new ClashTest() : (ClashTest)current.CreateCopyWithoutChildren();
            Configure(doc, catalog, matrix, rule, planned, mmPerUnit, test);
            if (current is null) clash.TestsAddCopy(test);
            else clash.TestsEditTestFromCopy(current, test);
            written.Add(planned.Name);
        }

        return written;
    }

    /// <summary>Names of planned tests whose stored definition does not match the plan after apply (empty = verified).</summary>
    public static IReadOnlyList<string> ReadBack(Document doc, ClashTestPlan plan, double mmPerUnit)
    {
        // first of each name, like the planner: a duplicate name elsewhere in the document must not abort the run
        var stored = ExistingTests(doc, mmPerUnit).GroupBy(t => t.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        return plan.ToWrite
            .Where(p => !stored.TryGetValue(p.Name, out var t)
                        || Math.Abs(t.ToleranceMm - p.ToleranceMm) > 0.01
                        || t.SetA != p.SetA || t.SetB != p.SetB
                        || !string.Equals(t.TestType, ClashTestPlanner.TestType, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();
    }

    /// <summary>Every clash test in the document, including tests filed in Clash Detective folders.</summary>
    public static IEnumerable<ClashTest> AllTests(Document doc) => Flatten(doc.GetClash().TestsData.Tests);

    private static IEnumerable<ClashTest> Flatten(IEnumerable<SavedItem> items)
    {
        foreach (var item in items)
        {
            if (item is ClashTest test) yield return test;
            else if (item is GroupItem group)
                foreach (var nested in Flatten(group.Children)) yield return nested;
        }
    }

    private static void Configure(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, ClashRule rule, PlannedTest planned, double mmPerUnit, ClashTest test)
    {
        test.DisplayName = planned.Name;
        test.TestType = ClashTestType.Hard;
        test.Tolerance = planned.ToleranceMm / mmPerUnit;
        test.Priority = planned.Priority;
        test.SelectionA.Selection.CopyFrom(SourceFor(doc, catalog, matrix, rule.Left));
        test.SelectionB.Selection.CopyFrom(SourceFor(doc, catalog, matrix, rule.Right));
    }

    private static SelectionSourceCollection SourceFor(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, string code)
    {
        var saved = NavisSearchSetCompiler.FindSet(doc, SearchSetPlan.For(catalog, matrix, code))
                    ?? throw new InvalidOperationException($"search set {code} disappeared between plan and apply");
        return new SelectionSourceCollection { doc.SelectionSets.CreateSelectionSource(saved) };
    }

    private static string? SetName(Document doc, ClashSelection selection)
    {
        var sources = selection.Selection.SelectionSources;
        if (sources.Count != 1) return null;
        var saved = doc.SelectionSets.ResolveSelectionSource(sources[0]);
        return saved is null ? null : NavisSearchSetCompiler.PathOf(doc, saved);
    }
}
