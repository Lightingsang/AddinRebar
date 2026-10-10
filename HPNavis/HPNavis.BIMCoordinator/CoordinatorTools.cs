using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using HPNavis.BIMCoordinator.ClashTests;
using HPNavis.BIMCoordinator.Probe;
using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator;

/// <summary>
///     What the BIM-coordination seeds call: each seed reads its <c>args</c> and calls one method here. Answers are
///     plain objects the bridge serialises (camelCase envelopes with <c>success</c>, <c>summary</c>, lists, warnings).
///     Caller mistakes (unknown LOD, LOD 400, unknown rule id) are <see cref="ArgumentException" />s. Nothing here runs a
///     clash test, saves, appends or exports: those heavy calls stay in seed code where the bridge's heavy gate sees them.
/// </summary>
public static partial class CoordinatorTools
{
    public const int MaxProbeItems = 500_000;
    public const int MaxCanaries = 5;
    private const int MaxWarnings = 50;

    public static object ProbeModel(Document doc, int maxItemsPerDiscipline, CancellationToken ct)
    {
        if (maxItemsPerDiscipline is < 1 or > MaxProbeItems) throw new ArgumentException($"maxItemsPerDiscipline must be 1..{MaxProbeItems}");
        var catalog = BaseSetCatalog.Default;
        var files = NavisModelProbe.Files(doc, catalog);
        var properties = NavisModelProbe.Properties(doc, catalog, 2000);
        var disciplines = catalog.DisciplineRoles.Keys.Select(d => NavisModelProbe.Categories(doc, catalog, d, maxItemsPerDiscipline, ct)).ToList();
        var warnings = new List<string>();
        warnings.AddRange(files.Where(f => f.Discipline is null).Select(f => $"file '{f.Name}' has no known ISO role code — its elements are in no base set"));
        warnings.AddRange(properties.Where(p => !p.FoundByInternalName).Select(p => $"property {p.Expected} not found by internal name on sampled elements — search sets would be empty"));
        warnings.AddRange(MissingCategories(catalog, disciplines));
        return new
        {
            success = properties.All(p => p.FoundByInternalName),
            summary = $"{files.Count} file(s), {files.Count(f => f.Discipline is not null)} with a known role; " +
                      string.Join(", ", disciplines.Select(d => $"{d.Discipline} {d.Items} element(s)")),
            files,
            properties,
            disciplines = disciplines.Select(d => new { d.Discipline, d.Items, d.Truncated, categories = d.Categories.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, p => p.Value) }),
            warnings = warnings.Take(MaxWarnings).ToList(),
            warningCount = warnings.Count,
        };
    }

    public static object SyncClashTests(Document doc, string lod, bool apply, double mmPerUnit, IReadOnlyCollection<int>? priorities,
        IReadOnlyCollection<string>? ruleIds, bool listUnchanged, CancellationToken ct)
    {
        RequirePositive(mmPerUnit);
        var matrix = ClashMatrix.Default;
        var catalog = BaseSetCatalog.Default;
        var plan = NavisClashTestCompiler.Plan(doc, matrix, catalog, lod, mmPerUnit, priorities, ruleIds, ct);
        var written = apply ? NavisClashTestCompiler.Apply(doc, catalog, matrix, plan, mmPerUnit, ct) : Array.Empty<string>();
        var mismatched = apply ? NavisClashTestCompiler.ReadBack(doc, plan, mmPerUnit) : Array.Empty<string>();
        return new
        {
            success = mismatched.Count == 0,
            applied = apply,
            lod,
            plan.ToleranceMm,
            summary = $"LOD{lod} @ {plan.ToleranceMm} mm: {plan.Tests.Count} rule(s) — " +
                      string.Join(", ", Enum.GetValues(typeof(PlanAction)).Cast<PlanAction>().Where(a => plan.Count(a) > 0).Select(a => $"{plan.Count(a)} {a}")) +
                      (apply ? $"; written {written.Count}, read back {(mismatched.Count == 0 ? "OK" : $"MISMATCH {mismatched.Count}")}" : ""),
            tests = plan.Tests.Where(t => listUnchanged || t.Action != PlanAction.Unchanged)
                .Select(t => new { t.RuleId, t.Name, t.Priority, action = t.Action.ToString(), t.Reason }),
            orphans = plan.Orphans,
            duplicates = plan.Duplicates,
            mismatched,
        };
    }

    /// <summary>
    ///     Names of the up-to-date tests to run as canaries. The seed runs them itself (heavy), resolving each with
    ///     <see cref="FindTest" /> right before its run — a run disposes the wrappers handed to it.
    /// </summary>
    public static IReadOnlyList<string> CanaryTestNames(Document doc, string lod, double mmPerUnit, int count, IReadOnlyCollection<string>? ruleIds, CancellationToken ct)
    {
        RequirePositive(mmPerUnit);
        // checked again by CanarySelector; here before the plan is built (21 searches) so a bad call costs nothing
        if (count is < 1 or > MaxCanaries || ruleIds is { Count: > MaxCanaries }) throw new ArgumentException($"count and ruleIds are capped at {MaxCanaries}: a clash run cannot be interrupted; Run All belongs in Clash Detective");
        var plan = NavisClashTestCompiler.Plan(doc, ClashMatrix.Default, BaseSetCatalog.Default, lod, mmPerUnit, null, null, ct);
        var names = CanarySelector.Select(ClashMatrix.Default, plan, count, ruleIds).Select(t => t.Name).ToList();
        if (names.Count == 0) throw new ArgumentException($"no up-to-date LOD{lod} test with non-empty sets — apply the search sets and clash tests first");
        return names;
    }

    public static ClashTest FindTest(Document doc, string testName) =>
        NavisClashTestCompiler.AllTests(doc).FirstOrDefault(t => t.DisplayName == testName)
        ?? throw new InvalidOperationException($"clash test '{testName}' is gone");

    /// <summary>Result counts of a test as stored now (status totals and the run state), looked up by name.</summary>
    public static object Summarize(Document doc, string testName, long elapsedMs)
    {
        var test = FindTest(doc, testName);
        var results = Results(test).ToList();
        return new
        {
            name = testName,
            status = test.Status.ToString(),
            elapsedMs,
            resultCount = results.Count,
            byStatus = results.GroupBy(r => r.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
        };
    }

    private static IEnumerable<ClashResult> Results(SavedItem item) =>
        item is ClashResult result ? new[] { result } : item is GroupItem group ? group.Children.SelectMany(Results) : Enumerable.Empty<ClashResult>();

    private static IEnumerable<string> MissingCategories(BaseSetCatalog catalog, IReadOnlyList<DisciplineCategories> disciplines)
    {
        foreach (var group in ClashMatrix.Default.Groups)
        {
            var found = disciplines.First(d => d.Discipline == group.Discipline).Categories;
            if (!catalog.Set(group.Code).Categories.Any(found.ContainsKey))
                yield return $"{group.Code} {group.Name}: none of its categories occur in {group.Discipline} files";
        }
    }

    private static void RequirePositive(double mmPerUnit)
    {
        if (!(mmPerUnit > 0)) throw new ArgumentException("mmPerUnit must be > 0 (pass units.ToMm(1))");
    }
}
