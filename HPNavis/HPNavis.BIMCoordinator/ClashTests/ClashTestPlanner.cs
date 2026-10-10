using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.ClashTests;

/// <summary>What the document holds for one base set: absent, or present and finding something (null = not checked).</summary>
public sealed record SetState(bool Exists, bool? HasItems);

/// <summary>The parts of an existing clash test that the matrix decides; anything else (results, status, comments) is left alone.</summary>
public sealed record ExistingTest(string Name, string TestType, double ToleranceMm, string? SetA, string? SetB, int Priority);

public enum PlanAction
{
    Create,
    Update,
    Unchanged,
    SkipMissingSet,
    SkipEmptySet,
}

/// <summary>One rule's test; <see cref="CurrentName" /> is the stored test it updates (its name differs when the matrix changed the priority).</summary>
public sealed record PlannedTest(string RuleId, string Name, int Priority, double ToleranceMm, string SetA, string SetB, PlanAction Action, string? Reason, string? CurrentName = null);

/// <summary>
///     Plan for one LOD: what each matrix rule does, our tests of the same LOD that the matrix no longer lists, and extra
///     tests that encode a rule already matched (both reported, never deleted).
/// </summary>
public sealed record ClashTestPlan(string Lod, double ToleranceMm, IReadOnlyList<PlannedTest> Tests, IReadOnlyList<string> Orphans, IReadOnlyList<string> Duplicates)
{
    public int Count(PlanAction action) => Tests.Count(t => t.Action == action);

    public IEnumerable<PlannedTest> ToWrite => Tests.Where(t => t.Action is PlanAction.Create or PlanAction.Update);
}

/// <summary>
///     Pure diff between the matrix and the document: one Hard test per eligible rule, named by
///     <see cref="ClashTestNaming" />, selections = the two base sets. A test whose set is missing or empty is
///     skipped with a reason — never widened to the whole model. Tolerances compare within 0.01 mm, which
///     absorbs the feet/metre round trip of the document units.
/// </summary>
public static class ClashTestPlanner
{
    public const string TestType = "Hard";
    private const double ToleranceEpsilonMm = 0.01;

    public static ClashTestPlan Plan(
        ClashMatrix matrix,
        string lod,
        Func<string, string> setNameOf,
        IReadOnlyDictionary<string, SetState> sets,
        IReadOnlyCollection<ExistingTest> existing,
        IReadOnlyCollection<int>? priorities = null,
        IReadOnlyCollection<string>? ruleIds = null)
    {
        var toleranceMm = matrix.ToleranceMmFor(lod);
        var rules = matrix.RulesFor(lod, priorities, ruleIds);

        // identity = rule id + LOD parsed from the name, not the whole name: a priority change in the workbook renames
        // the stored test (keeping its results) instead of leaving it behind as an orphan
        var byRule = new Dictionary<string, ExistingTest>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        foreach (var test in existing)
        {
            if (ClashTestNaming.Parse(test.Name) is not { } parsed || parsed.Lod != lod) continue;
            if (byRule.ContainsKey(parsed.RuleId)) duplicates.Add(test.Name);
            else byRule[parsed.RuleId] = test;
        }

        var tests = rules.Select(rule => PlanOne(rule, lod, toleranceMm, setNameOf, sets, byRule)).ToList();
        var eligible = new HashSet<string>(matrix.RulesFor(lod).Select(r => r.Id), StringComparer.Ordinal);
        var orphans = byRule.Where(pair => !eligible.Contains(pair.Key)).Select(pair => pair.Value.Name).ToList();
        return new ClashTestPlan(lod, toleranceMm, tests, orphans, duplicates);
    }

    private static PlannedTest PlanOne(
        ClashRule rule, string lod, double toleranceMm, Func<string, string> setNameOf,
        IReadOnlyDictionary<string, SetState> sets, IReadOnlyDictionary<string, ExistingTest> byRule)
    {
        var name = ClashTestNaming.NameFor(rule, lod);
        var setA = setNameOf(rule.Left);
        var setB = setNameOf(rule.Right);
        byRule.TryGetValue(rule.Id, out var current);
        PlannedTest With(PlanAction action, string? reason) => new(rule.Id, name, rule.Priority, toleranceMm, setA, setB, action, reason, current?.Name);

        var missing = new[] { rule.Left, rule.Right }.Where(code => !sets.TryGetValue(code, out var state) || !state.Exists).Distinct().ToList();
        if (missing.Count > 0) return With(PlanAction.SkipMissingSet, $"search set {string.Join(", ", missing)} not in the document — apply the search sets first");
        var empty = new[] { rule.Left, rule.Right }.Where(code => sets[code].HasItems == false).Distinct().ToList();
        if (empty.Count > 0) return With(PlanAction.SkipEmptySet, $"search set {string.Join(", ", empty)} finds no item in this model");

        if (current is null) return With(PlanAction.Create, null);
        var drift = Drift(current, name, toleranceMm, setA, setB, rule.Priority);
        return drift.Count == 0 ? With(PlanAction.Unchanged, null) : With(PlanAction.Update, string.Join("; ", drift));
    }
    private static List<string> Drift(ExistingTest current, string name, double toleranceMm, string setA, string setB, int priority)
    {
        var drift = new List<string>();
        if (current.Name != name) drift.Add($"name {current.Name} → {name}");
        if (!string.Equals(current.TestType, TestType, StringComparison.OrdinalIgnoreCase)) drift.Add($"type {current.TestType} → {TestType}");
        if (Math.Abs(current.ToleranceMm - toleranceMm) > ToleranceEpsilonMm) drift.Add($"tolerance {current.ToleranceMm:0.##} → {toleranceMm:0.##} mm");
        if (current.SetA != setA || current.SetB != setB) drift.Add($"selections {current.SetA ?? "?"} / {current.SetB ?? "?"} → {setA} / {setB}");
        if (current.Priority != priority) drift.Add($"priority {current.Priority} → {priority}");
        return drift;
    }
}
