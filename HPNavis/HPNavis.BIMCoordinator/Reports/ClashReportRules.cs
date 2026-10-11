using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.Reports;

/// <summary>One HP test of the model as the run planner sees it: its name and whether it ever ran.</summary>
public sealed record PhaseTestState(string Name, bool HasRun);

/// <summary>The HP tests of one design phase in run order (ordinal name order: priority group, then pair).</summary>
public sealed record PhaseTestList(string Phase, IReadOnlyList<string> Names, int SkippedAlreadyRun);

/// <summary>
///     The pure rules of the clash report with pictures: which tests a phase run covers and where a batch resumes,
///     which results the Navisworks report carries, and where its HTML goes beside the snapshot.
/// </summary>
public static class ClashReportRules
{
    /// <summary>The report tab's own name for the HTML table with one picture per clash.</summary>
    public const string HtmlTabularFormatter = "HTML (Tabular)";

    /// <summary>All tests' results in one file (the report tab's "All Tests (combined)").</summary>
    public const string CombinedDriver = "Clash_ReportDriver_AllCombined";

    /// <summary>The report field that carries the picture; its absence means the field names are not the English ones.</summary>
    public const string PictureField = "Image";

    /// <summary>The page the report writes inside its folder; Navisworks puts the pictures in a sibling folder.</summary>
    public const string HtmlFileName = "clash-report.html";

    /// <summary>Upper bound of pictures in one report: Navisworks renders each one on its main thread inside one 600 s call.</summary>
    public const int MaxPictures = 2000;

    /// <summary>Open results, the ones a coordination meeting acts on; they get pictures before resolved ones.</summary>
    public static readonly IReadOnlyCollection<string> OpenStatuses = new[] { "New", "Active", "Reviewed" };

    /// <summary>
    ///     Results the coordination sheet lists: open ones to act on and Resolved ones to close.
    ///     Approved clashes were accepted as they are and are left out.
    /// </summary>
    public static readonly IReadOnlyCollection<string> ReportedStatuses = OpenStatuses.Concat(new[] { "Resolved" }).ToArray();

    public static bool IsOpen(string status) => OpenStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    public static bool IsReported(string status) => ReportedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    /// <summary>The report fields that list an item's every property (one column per property of the whole model).</summary>
    public static bool IsPropertyListField(string fieldName) => fieldName.IndexOf("Propert", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    ///     The phase's HP tests in ordinal name order, each name once. <paramref name="lod" /> "auto" (or empty) means the
    ///     model's single phase; <paramref name="onlyNotRun" /> keeps the tests that never ran.
    /// </summary>
    public static PhaseTestList PhaseTests(IReadOnlyCollection<PhaseTestState> tests, string? lod, bool onlyNotRun)
    {
        var phase = ClashSnapshotRules.ResolvePhase(lod, tests.Select(t => t.Name));
        var ofPhase = tests
            .Where(t => ClashTestNaming.Parse(t.Name) is { } parsed && ClashMatrix.PhaseOf(parsed.Lod) == phase)
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .Select(g => new PhaseTestState(g.Key, g.All(t => t.HasRun)))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
        if (ofPhase.Count == 0) throw new ArgumentException($"the model holds no HP clash test of phase {phase}");
        var chosen = onlyNotRun ? ofPhase.Where(t => !t.HasRun).ToList() : ofPhase;
        return new PhaseTestList(phase, chosen.Select(t => t.Name).ToList(), ofPhase.Count - chosen.Count);
    }

    /// <summary>
    ///     The names still to run after <paramref name="after" /> (the last test the previous batch attempted; empty = from
    ///     the start). A name cursor, not an index: a test added, removed or finished between two calls cannot shift it.
    /// </summary>
    public static IReadOnlyList<string> RemainingAfter(IReadOnlyList<string> names, string? after) =>
        string.IsNullOrEmpty(after) ? names : names.Where(n => string.CompareOrdinal(n, after) > 0).ToList();

    /// <summary>
    ///     Whether a batch may start one more test: the slowest test so far must still fit in the budget, because a run
    ///     cannot be interrupted and a call that overruns its timeout is failed by the bridge.
    /// </summary>
    public static bool MayStartAnother(double elapsedSeconds, double slowestSeconds, int budgetSeconds) =>
        elapsedSeconds + slowestSeconds < budgetSeconds;

    /// <summary>
    ///     The folder of the HTML report that belongs to a snapshot: the snapshot's own name without <c>.json</c>, beside it,
    ///     so the workbook builder finds the pictures of exactly that export.
    /// </summary>
    public static string HtmlFolderFor(string snapshotPath) =>
        Path.Combine(Path.GetDirectoryName(snapshotPath)!, Path.GetFileNameWithoutExtension(snapshotPath));

    /// <summary>
    ///     Index of the first name equal to <paramref name="wanted" /> (case-insensitive), or the caller's error listing
    ///     what this Navisworks offers — a renamed formatter must say so instead of writing another format.
    /// </summary>
    public static int IndexOf(IReadOnlyList<string> names, string wanted, string what)
    {
        for (var i = 0; i < names.Count; i++)
            if (string.Equals(names[i], wanted, StringComparison.OrdinalIgnoreCase)) return i;
        throw new InvalidOperationException($"this Navisworks has no report {what} '{wanted}' (it offers: {string.Join(", ", names)})");
    }
}
