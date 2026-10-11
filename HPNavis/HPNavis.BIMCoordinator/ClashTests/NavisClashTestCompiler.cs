using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using Autodesk.Navisworks.Api.Interop;
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
        foreach (var group in matrix.Groups)
        {
            ct.ThrowIfCancellationRequested();
            // the state of the set the tests will select (M4 → M4.9), keyed by the group the rules name
            var plan = SearchSetPlan.For(catalog, matrix, catalog.ClashSetCode(group.Code));
            var saved = NavisSearchSetCompiler.FindSet(doc, plan);
            // empty or not is all the plan needs: FindFirst stops at the first match (21 full searches take ~1 min on a 95 MB model)
            sets[group.Code] = saved is null ? new SetState(false, null)
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
        var plan = SearchSetPlan.For(catalog, matrix, catalog.ClashSetCode(code));
        return $"{plan.Folder}/{plan.DisplayName}";
    }

    public static IReadOnlyList<ExistingTest> ExistingTests(Document doc, double mmPerUnit) =>
        AllTests(doc).Select(test => new ExistingTest(
            test.DisplayName, test.TestType.ToString(), test.Tolerance * mmPerUnit,
            SetName(doc, test.SelectionA), SetName(doc, test.SelectionB), test.Priority,
            test.IgnoreRules.Where(r => r.IsEnabled).Select(r => r.DisplayName).ToList(), test.MergeComposites)).ToList();

    /// <summary>Writes every Create/Update of the plan; returns the names written.</summary>
    public static IReadOnlyList<string> Apply(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, ClashTestPlan plan, double mmPerUnit, CancellationToken ct)
    {
        var clash = doc.GetClash().TestsData;
        var written = new List<string>();
        var ignoreRules = plan.ToWrite.Any() ? IgnoreRuleSource(doc) : new Dictionary<string, Rule>();
        // created in name order, so a new list already reads LOD by LOD, priority group by priority group
        foreach (var planned in plan.ToWrite.OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var rule = matrix.Rules.First(r => r.Id == planned.RuleId);
            var current = planned.CurrentName is null ? null : AllTests(doc).FirstOrDefault(t => t.DisplayName == planned.CurrentName);
            if (current is not null && current.MergeComposites != IgnoreRulePolicy.MergeComposites)
            {
                // an edit that flips MergeComposites keeps nothing else of the copy (ignore rules and selections were
                // dropped, measured live): flip it in an edit of its own, then apply the rest to the re-read test
                var flip = (ClashTest)current.CreateCopyWithoutChildren();
                flip.MergeComposites = IgnoreRulePolicy.MergeComposites;
                clash.TestsEditTestFromCopy(current, flip);
                current = AllTests(doc).First(t => t.DisplayName == planned.CurrentName);
            }

            // an update starts from a copy of the stored test so settings the matrix does not own (ignore rules,
            // primitive types, assignee) survive; a create starts from a blank Hard test
            var test = current is null ? new ClashTest() : (ClashTest)current.CreateCopyWithoutChildren();
            Configure(doc, catalog, matrix, rule, planned, mmPerUnit, test, ignoreRules);
            if (current is null) clash.TestsAddCopy(test);
            else clash.TestsEditTestFromCopy(current, test);
            written.Add(planned.Name);
        }

        return written;
    }

    /// <summary>
    ///     Removes the plan's <see cref="ClashTestPlan.ToRemove" /> tests (duplicates of this phase, HP tests of other
    ///     phases) — only on the caller's explicit approval and only tests whose names parse as HP tests, so a hand-made
    ///     test is never touched. Returns the names removed.
    /// </summary>
    public static IReadOnlyList<string> RemoveOtherPhases(Document doc, ClashTestPlan plan, CancellationToken ct)
    {
        var clash = doc.GetClash().TestsData;
        var removed = new List<string>();
        foreach (var name in plan.ToRemove.Where(n => ClashTestNaming.Parse(n) is not null))
        {
            ct.ThrowIfCancellationRequested();
            var test = AllTests(doc).FirstOrDefault(t => t.DisplayName == name);
            if (test is null) continue;
            clash.TestsRemove(test);
            removed.Add(name);
        }

        return removed;
    }

    /// <summary>Names of planned tests whose stored definition does not match the plan after apply (empty = verified).</summary>
    public static IReadOnlyList<string> ReadBack(Document doc, ClashTestPlan plan, double mmPerUnit)
    {
        // first of each name, like the planner: a duplicate name elsewhere in the document must not abort the run
        var stored = ExistingTests(doc, mmPerUnit).GroupBy(t => t.Name, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        return plan.ToWrite
            .Where(p => !stored.TryGetValue(p.Name, out var t)
                        || Math.Abs(t.ToleranceMm - p.ToleranceMm) > 0.01
                        || !IgnoreRulePolicy.SameRules(t.IgnoreRules ?? Array.Empty<string>(), p.EnabledIgnoreRules)
                        || t.MergeComposites != IgnoreRulePolicy.MergeComposites
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

    /// <summary>Name of the test whose Rules tab supplies the four ignore rules when Navisworks will not hand out its base set.</summary>
    public const string RulesTemplateName = "HP Rules Template";

    /// <summary>
    ///     The four Rules-tab rules by display name. The base rule set comes from Navisworks itself
    ///     (<c>LcClClashRules.LoadBaseRuleSet</c>; a test's <c>IgnoreRules</c> only holds the rules ticked on it); when that
    ///     yields nothing, a test named <see cref="RulesTemplateName" /> with all four ticked is the source.
    /// </summary>
    private static IReadOnlyDictionary<string, Rule> IgnoreRuleSource(Document doc)
    {
        var found = new Dictionary<string, Rule>(StringComparer.Ordinal);
        try
        {
            var baseSet = new RuleCollection();
            LcClClashRules.LoadBaseRuleSet(baseSet, LcOpState.GetActiveInstance());
            foreach (var rule in baseSet) found[rule.DisplayName] = rule.CreateCopy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // the base set is an internal API; the template test below is the documented way round it
            found.Clear();
        }

        if (!IgnoreRulePolicy.Known.All(found.ContainsKey))
        {
            var template = AllTests(doc).FirstOrDefault(t => t.DisplayName == RulesTemplateName);
            if (template is not null)
                foreach (var rule in template.IgnoreRules) found[rule.DisplayName] = rule.CreateCopy();
        }

        var missing = IgnoreRulePolicy.Known.Where(name => !found.ContainsKey(name)).ToList();
        if (missing.Count > 0)
            throw new ArgumentException($"Navisworks did not provide the Rules-tab rule(s) {string.Join(", ", missing.Select(m => $"'{m}'"))}: " +
                                        $"create a clash test named '{RulesTemplateName}', tick all four rules on its Rules tab and run again");
        return found;
    }

    private static void Configure(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, ClashRule rule, PlannedTest planned, double mmPerUnit, ClashTest test,
        IReadOnlyDictionary<string, Rule> ignoreRules)
    {
        test.MergeComposites = IgnoreRulePolicy.MergeComposites;
        test.IgnoreRules.Clear();
        foreach (var name in planned.EnabledIgnoreRules)
        {
            var copy = ignoreRules[name].CreateCopy();
            copy.IsEnabled = true;
            test.IgnoreRules.Add(copy);
        }

        test.DisplayName = planned.Name;
        test.TestType = ClashTestType.Hard;
        test.Tolerance = planned.ToleranceMm / mmPerUnit;
        test.Priority = planned.Priority;
        test.SelectionA.Selection.CopyFrom(SourceFor(doc, catalog, matrix, rule.Left));
        test.SelectionB.Selection.CopyFrom(SourceFor(doc, catalog, matrix, rule.Right));
    }

    private static SelectionSourceCollection SourceFor(Document doc, BaseSetCatalog catalog, ClashMatrix matrix, string code)
    {
        var setCode = catalog.ClashSetCode(code);
        var saved = NavisSearchSetCompiler.FindSet(doc, SearchSetPlan.For(catalog, matrix, setCode))
                    ?? throw new InvalidOperationException($"search set {setCode} disappeared between plan and apply");
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
