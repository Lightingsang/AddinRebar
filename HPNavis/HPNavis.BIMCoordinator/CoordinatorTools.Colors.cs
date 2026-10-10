using Autodesk.Navisworks.Api;
using HPNavis.BIMCoordinator.Colors;
using HPNavis.BIMCoordinator.SearchSets;

namespace HPNavis.BIMCoordinator;

/// <summary>The colour half of the facade: the colour search sets of sheet ColorSearchSet(DSC) and their permanent colours.</summary>
public static partial class CoordinatorTools
{
    /// <summary>
    ///     Preview (default) or apply the colour sets under <c>HP BIMCoordinator/Color</c>, with the same upsert rule as the
    ///     search-set registry (create, unchanged, conflict unless approved for named codes; nothing deleted). Pending sets
    ///     are listed, never built.
    /// </summary>
    public static object SyncColorSets(Document doc, bool apply, bool allowUpdate, IReadOnlyCollection<string>? codes, double mmPerUnit, CancellationToken ct)
    {
        RequirePositive(mmPerUnit);
        SetUpsert.RequireCodesForUpdate(allowUpdate, codes);
        var colors = ColorSetCatalog.Default;
        var plans = colors.Plans(BaseSetCatalog.Default, codes);
        var outcomes = apply ? NavisSearchSetCompiler.Apply(doc, plans, mmPerUnit, allowUpdate, ct) : NavisSearchSetCompiler.Preview(doc, plans, mmPerUnit, ct);
        var conflicts = outcomes.Where(o => o.Action == "conflict").Select(o => o.Name).ToList();
        var pending = codes is { Count: > 0 } ? new List<ColorSetEntry>() : colors.Sets.Where(s => s.Pending is not null).ToList();
        var warnings = new List<string>();
        warnings.AddRange(outcomes.Where(o => o.Items == 0).Select(o => $"{o.Name} finds no element in this model"));
        warnings.AddRange(conflicts.Select(name => $"{name}: the saved set differs from the registry and was left as it is; re-run with allowUpdate=true and codes=[\"{name}\"] once approved"));
        return new
        {
            success = conflicts.Count == 0,
            applied = apply,
            summary = $"{outcomes.Count} colour set(s): " + string.Join(", ", outcomes.GroupBy(o => o.Action).Select(g => $"{g.Count()} {g.Key}")) +
                      (pending.Count > 0 ? $"; {pending.Count} pending (not built)" : ""),
            sets = outcomes.Select(o =>
            {
                var entry = colors.Set(o.Code);
                return new { o.Code, o.Name, o.Items, o.Action, rgb = entry.Rgb, entry.Verified };
            }),
            pending = pending.Select(p => new { p.Code, name = p.DisplayName, reason = p.Pending }),
            warnings,
        };
    }

    /// <summary>
    ///     <c>preview</c> = who would be painted with what (no write); <c>apply</c> = permanent colours in sheet order + read-back;
    ///     <c>verify</c> = read-back only; <c>reset</c> = remove the permanent colours of the painting sets' elements only.
    ///     Fails when no painting set is found or one is missing from the document (the sets were never synced), so a run
    ///     that painted or checked nothing never reports success.
    /// </summary>
    public static object PaintColors(Document doc, string mode, IReadOnlyCollection<string>? codes, CancellationToken ct)
    {
        var outcome = NavisColorPainter.Run(doc, BaseSetCatalog.Default, ColorSetCatalog.Default, mode, codes, ct);
        var mismatched = outcome.Checks.Where(c => c.Mismatched > 0).ToList();
        var verifying = mode is "apply" or "verify";
        var sampled = outcome.Checks.Sum(c => c.Sampled);
        return new
        {
            success = outcome.Usable && mismatched.Count == 0,
            mode,
            summary = $"{outcome.Sets.Count} set(s) read, {outcome.Sets.Count(s => s.Rgb is not null && s.Found > 0)} painting {outcome.Sets.Where(s => s.Rgb is not null).Sum(s => s.Found)} element(s)" +
                      (outcome.Painted > 0 ? $"; painted {outcome.Painted}" : "") +
                      (outcome.Reset > 0 ? $"; reset {outcome.Reset}" : "") +
                      (verifying ? $"; read-back {sampled - outcome.Checks.Sum(c => c.Mismatched)}/{sampled} OK, {outcome.Checks.Sum(c => c.RepaintedByLater)} sampled element(s) taken by a later set" : "") +
                      (outcome.Skipped.Count > 0 ? $"; {outcome.Skipped.Count} skipped" : ""),
            sets = outcome.Sets.Select(s => new { s.Code, s.Name, rgb = s.Rgb, s.Found }),
            skipped = outcome.Skipped,
            mismatched,
        };
    }
}
