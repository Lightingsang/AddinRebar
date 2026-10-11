using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator.ClashTests;

/// <summary>What the document holds for one base set: absent, or present and finding something (null = not checked).</summary>
public sealed record SetState(bool Exists, bool? HasItems);

/// <summary>
///     The parts of an existing clash test that the matrix decides; anything else (results, status, comments) is left alone.
///     <see cref="IgnoreRules" /> (enabled Rules-tab rules by name) and <see cref="MergeComposites" /> are null when not read.
/// </summary>
public sealed record ExistingTest(string Name, string TestType, double ToleranceMm, string? SetA, string? SetB, int Priority,
    IReadOnlyCollection<string>? IgnoreRules = null, bool? MergeComposites = null);

public enum PlanAction
{
    Create,
    Update,
    Unchanged,
    SkipMissingSet,
    SkipEmptySet,
}

/// <summary>
///     One rule's test; <see cref="CurrentName" /> is the stored test it updates (its name differs when the matrix changed
///     the priority) and <see cref="IgnoreRules" /> the Rules-tab rules it enables (<see cref="IgnoreRulePolicy" />).
/// </summary>
public sealed record PlannedTest(string RuleId, string Name, int Priority, double ToleranceMm, string SetA, string SetB, PlanAction Action, string? Reason,
    string? CurrentName = null, IReadOnlyList<string>? IgnoreRules = null)
{
    public IReadOnlyList<string> EnabledIgnoreRules => IgnoreRules ?? Array.Empty<string>();
}

/// <summary>
///     Plan for one LOD: what each matrix rule does, our tests of the same LOD that the matrix no longer lists, and extra
///     tests that encode a rule already matched (both reported, never deleted).
/// </summary>
public sealed record ClashTestPlan(string Lod, double ToleranceMm, IReadOnlyList<PlannedTest> Tests, IReadOnlyList<string> Orphans, IReadOnlyList<string> Duplicates,
    IReadOnlyList<string>? OtherPhases = null)
{
    /// <summary>HP tests of the other design phases (and the earlier LOD350/LOD400 split) — removed only on approval.</summary>
    public IReadOnlyList<string> OtherPhaseTests => OtherPhases ?? Array.Empty<string>();

    /// <summary>What an approved clean-up removes: duplicates of this phase and every HP test of another phase.</summary>
    public IReadOnlyList<string> ToRemove => Duplicates.Concat(OtherPhaseTests).ToList();

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
        IReadOnlyCollection<string>? ruleIds = null,
        Func<string, string>? groupLabel = null)
    {
        lod = ClashMatrix.PhaseOf(lod);
        groupLabel ??= code => BaseSetCatalog.Default.Set(code).DisplayName;
        var toleranceMm = matrix.ToleranceMmFor(lod);
        var rules = matrix.RulesFor(lod, priorities, ruleIds);

        // identity = rule id + phase parsed from the name, not the whole name: a priority or label change renames the
        // stored test (keeping its results) instead of leaving it behind as an orphan. Of two tests of one rule (the
        // earlier LOD350 and LOD400 copies), the one already carrying the planned name is kept, else the first.
        var byRule = new Dictionary<string, ExistingTest>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        var otherPhases = new List<string>();
        var rulesById = matrix.Rules.ToDictionary(r => r.Id, StringComparer.Ordinal);
        foreach (var group in existing
                     .Select(test => (Test: test, Parsed: ClashTestNaming.Parse(test.Name)))
                     .Where(x => x.Parsed is not null)
                     .GroupBy(x => (Rule: x.Parsed!.Value.RuleId, Phase: ClashMatrix.PhaseOf(x.Parsed.Value.Lod))))
        {
            var sameRule = group.Select(x => x.Test).ToList();
            if (group.Key.Phase != lod) { otherPhases.AddRange(sameRule.Select(t => t.Name)); continue; }
            var planned = rulesById.TryGetValue(group.Key.Rule, out var known) ? ClashTestNaming.NameFor(known, lod, groupLabel) : null;
            var keptIndex = Math.Max(0, sameRule.FindIndex(t => t.Name == planned));
            byRule[group.Key.Rule] = sameRule[keptIndex];
            duplicates.AddRange(sameRule.Where((_, index) => index != keptIndex).Select(t => t.Name));
        }

        var tests = rules.Select(rule => PlanOne(rule, lod, toleranceMm, setNameOf, sets, byRule, groupLabel)).ToList();
        var eligible = new HashSet<string>(matrix.RulesFor(lod).Select(r => r.Id), StringComparer.Ordinal);
        var orphans = byRule.Where(pair => !eligible.Contains(pair.Key)).Select(pair => pair.Value.Name).ToList();
        return new ClashTestPlan(lod, toleranceMm, tests, orphans, duplicates, otherPhases);
    }

    private static PlannedTest PlanOne(
        ClashRule rule, string lod, double toleranceMm, Func<string, string> setNameOf,
        IReadOnlyDictionary<string, SetState> sets, IReadOnlyDictionary<string, ExistingTest> byRule, Func<string, string> groupLabel)
    {
        var name = ClashTestNaming.NameFor(rule, lod, groupLabel);
        var setA = setNameOf(rule.Left);
        var setB = setNameOf(rule.Right);
        byRule.TryGetValue(rule.Id, out var current);
        var ignoreRules = IgnoreRulePolicy.For(rule);
        PlannedTest With(PlanAction action, string? reason) => new(rule.Id, name, rule.Priority, toleranceMm, setA, setB, action, reason, current?.Name, ignoreRules);

        var missing = new[] { rule.Left, rule.Right }.Where(code => !sets.TryGetValue(code, out var state) || !state.Exists).Distinct().ToList();
        if (missing.Count > 0) return With(PlanAction.SkipMissingSet, $"search set {string.Join(", ", missing)} not in the document — apply the search sets first");
        var empty = new[] { rule.Left, rule.Right }.Where(code => sets[code].HasItems == false).Distinct().ToList();
        if (empty.Count > 0) return With(PlanAction.SkipEmptySet, $"search set {string.Join(", ", empty)} finds no item in this model");

        if (current is null) return With(PlanAction.Create, null);
        var drift = Drift(current, name, toleranceMm, setA, setB, rule.Priority);
        if (current.IgnoreRules is not null && !IgnoreRulePolicy.SameRules(current.IgnoreRules, ignoreRules))
            drift.Add($"ignore rules [{string.Join(", ", current.IgnoreRules)}] → [{string.Join(", ", ignoreRules)}]");
        if (current.MergeComposites is { } composite && composite != IgnoreRulePolicy.MergeComposites)
            drift.Add($"composite objects {(composite ? "on" : "off")} → {(IgnoreRulePolicy.MergeComposites ? "on" : "off")}");
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
