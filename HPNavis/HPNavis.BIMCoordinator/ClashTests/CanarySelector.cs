using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.ClashTests;

/// <summary>
///     Picks the canary tests that prove the generated tests catch real clashes before anyone runs all of them:
///     tests that exist with both sets non-empty, highest priority first, one per discipline pair before a second
///     from the same pair (so ARC-STR, MEP-STR and ARC-MEP are all exercised), in matrix order. Explicit rule ids
///     override the choice but must still be runnable.
/// </summary>
public static class CanarySelector
{
    public static IReadOnlyList<PlannedTest> Select(ClashMatrix matrix, ClashTestPlan plan, int count, IReadOnlyCollection<string>? ruleIds = null)
    {
        if (count is < 1 or > CoordinatorTools.MaxCanaries) throw new ArgumentException($"count must be 1..{CoordinatorTools.MaxCanaries}");
        if (ruleIds is { Count: > CoordinatorTools.MaxCanaries }) throw new ArgumentException($"at most {CoordinatorTools.MaxCanaries} ruleIds per call; Run All belongs in Clash Detective");
        var runnable = plan.Tests.Where(t => t.Action == PlanAction.Unchanged).ToList();
        if (ruleIds is { Count: > 0 })
        {
            var notReady = ruleIds.Where(id => runnable.All(t => t.RuleId != id)).ToList();
            if (notReady.Count > 0)
                throw new ArgumentException($"rule(s) {string.Join(", ", notReady)} have no up-to-date test with non-empty sets at LOD{plan.Lod} — apply the clash tests first");
            return runnable.Where(t => ruleIds.Contains(t.RuleId)).ToList();
        }

        var pairOf = matrix.Rules.ToDictionary(r => r.Id, r => r.DisciplinePair, StringComparer.Ordinal);
        var ordered = runnable.OrderBy(t => t.Priority).ToList(); // stable: matrix order inside a priority
        var firstOfEachPair = ordered.GroupBy(t => pairOf[t.RuleId]).Select(g => g.First()).OrderBy(t => t.Priority).ThenBy(t => ordered.IndexOf(t));
        var chosen = firstOfEachPair.Take(count).ToList();
        chosen.AddRange(ordered.Where(t => !chosen.Contains(t)).Take(count - chosen.Count));
        return chosen;
    }
}
