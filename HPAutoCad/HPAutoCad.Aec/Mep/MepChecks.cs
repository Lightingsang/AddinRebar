using System.Globalization;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;

namespace HPAutoCad.Aec.Mep;

/// <summary>Issue types the MEP connectivity check reports (category <c>mep</c>).</summary>
public static class MepIssueType
{
    public const string OpenEnd = "open_end";
    public const string NearMiss = "near_miss";
    public const string DisconnectedRun = "disconnected_run";
    public const string OrphanNode = "orphan_node";
    public const string DuplicateRun = "duplicate_run";
    public const string MixedSystem = "mixed_system";

    public const string Category = "mep";
    public const string Prefix = "MEP";

    public static readonly IReadOnlyList<string> All = [OpenEnd, NearMiss, DisconnectedRun, OrphanNode, DuplicateRun, MixedSystem];
}

/// <summary>
///     What keeps an MEP drawing from reading as connected systems: a run end that stops just short of what it should meet
///     (<c>near_miss</c>, with the gap), an end connected to nothing (<c>open_end</c>), a run with both ends loose and no node
///     (<c>disconnected_run</c> — one issue, not two open ends), a terminal / fixture / equipment no run reaches (<c>orphan_node</c>;
///     a lone fitting block is info), two runs of one system drawn over each other, a network whose runs belong to several systems. Pure.
/// </summary>
public static class MepChecks
{
    /// <summary>Handles / system names listed on a <c>mixed_system</c> issue.</summary>
    public const int MaxMixedHandles = 4;
    public const int MaxMixedSystems = 5;

    public static IReadOnlyList<AuditIssue> Check(MepOutcome outcome, CancellationToken ct)
    {
        var issues = new List<AuditIssue>();
        if (outcome.Runs == 0) return issues; // nothing to connect: the tool warns, orphans are not the drawing's fault
        var runsById = outcome.Networks.SelectMany(n => n.Runs).ToDictionary(r => r.Handle, StringComparer.Ordinal);
        // two facing open ends are one gap, not two near misses naming each other
        var facing = new HashSet<(string, string)>();
        var openByRun = outcome.Endpoints.Where(e => e.IsOpen).GroupBy(e => e.RunHandle).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        // a run is attached when one of its own ends connects, or when another run's end lands on it (a main whose branches tee in has both its own ends open)
        var attached = new HashSet<string>(outcome.Endpoints.Where(e => !e.IsOpen).Select(e => e.RunHandle).Concat(outcome.Endpoints.SelectMany(e => e.ConnectedTo)), StringComparer.Ordinal);
        foreach (var (handle, ends) in openByRun)
        {
            ct.ThrowIfCancellationRequested();
            var run = runsById[handle];
            if (ends.Count == 2 && !attached.Contains(handle) && ends.All(e => e.NearestHandle is null))
            {
                issues.Add(Issue(MepIssueType.DisconnectedRun, IssueSeverity.Warning, [handle], Pt.Mid(ends[0].PointMm, ends[1].PointMm), run.LengthMm,
                    $"{run.Kind} {handle} ({run.System}, {Mm(run.LengthMm)} mm) connects to nothing at either end.", "Connect it to the system it belongs to, or erase it if it is a leftover."));
                continue;
            }

            foreach (var e in ends)
                if (e.NearestHandle is not null)
                {
                    var partner = openByRun.TryGetValue(e.NearestHandle, out var others) ? others.FirstOrDefault(o => o.NearestHandle == handle && o.PointMm.DistanceXY(e.PointMm) <= e.NearestGapMm + 1) : null;
                    var pair = string.CompareOrdinal(handle, e.NearestHandle) < 0 ? (handle, e.NearestHandle) : (e.NearestHandle, handle);
                    if (partner is not null && !facing.Add(pair)) continue;
                    var at = partner is null ? e.PointMm : Pt.Mid(e.PointMm, partner.PointMm);
                    issues.Add(Issue(MepIssueType.NearMiss, IssueSeverity.Warning, [handle, e.NearestHandle], at, e.NearestGapMm,
                        $"{run.Kind} {handle} ({run.System}) stops {Mm(e.NearestGapMm!.Value)} mm short of {e.NearestHandle} at ({Mm(at.X)}, {Mm(at.Y)}).",
                        "Snap the end onto it (update_entities_batch geometry) or raise tolerance.endpointConnection if the drawing is meant to read that loosely."));
                }
                else
                    issues.Add(Issue(MepIssueType.OpenEnd, IssueSeverity.Warning, [handle], e.PointMm, null,
                        $"{run.Kind} {handle} ({run.System}) ends open at ({Mm(e.PointMm.X)}, {Mm(e.PointMm.Y)}).",
                        "A supply/return end should reach equipment, a terminal or another run; cap it, extend it, or accept it as a stub."));
        }

        foreach (var n in outcome.OrphanNodes)
            issues.Add(Issue(MepIssueType.OrphanNode, MepNodeKind.MustBeServed(n.Kind) ? IssueSeverity.Warning : IssueSeverity.Info, [n.Handle], n.CenterMm, null,
                $"{n.Kind} {n.Handle} on {n.Layer}: no run reaches it.", MepNodeKind.MustBeServed(n.Kind) ? "Draw the run to it, or move the symbol onto the run that serves it." : "A fitting off every run is drafting noise; erase it or place it on the run."));

        foreach (var d in outcome.Duplicates)
            issues.Add(Issue(MepIssueType.DuplicateRun, IssueSeverity.Warning, [d.Handle, d.OtherHandle], Pt.Mid(d.FromMm, d.ToMm), d.OverlapMm,
                $"Runs {d.Handle} and {d.OtherHandle} overlap for {Mm(d.OverlapMm)} mm of the same system.", "Erase the copy; overlapping runs double lengths and hide open ends."));

        foreach (var n in outcome.Networks.Where(n => n.Systems.Count > 1))
        {
            var first = n.Runs[0].Shape;
            var systems = string.Join(", ", n.Systems.Take(MaxMixedSystems)) + (n.Systems.Count > MaxMixedSystems ? ", …" : "");
            issues.Add(Issue(MepIssueType.MixedSystem, IssueSeverity.Info, n.Runs.Take(MaxMixedHandles).Select(r => r.Handle).ToArray(), Pt.Mid(first.Start, first.End), n.LengthMm,
                $"Network {n.Id} joins runs of {n.Systems.Count} systems directly ({systems}).", "Check whether the runs really connect (a supply drawn onto a return) or whether the layers of one system are misnamed (detection.systems)."));
        }

        return AuditIssue.Ordered(issues).Select((i, k) => i with { IssueId = $"{MepIssueType.Prefix}-{k + 1:000}" }).ToArray();
    }

    private static string Mm(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static AuditIssue Issue(string type, string severity, IReadOnlyList<string> handles, Pt at, double? valueMm, string description, string action) =>
        new("", MepIssueType.Category, type, severity, handles, at.Rounded(), valueMm.HasValue ? Math.Round(valueMm.Value, 1) : null, description, action);
}
