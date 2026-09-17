using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Mep;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>The MEP tools: the network graph, its connectivity problems, and every open end with its location.</summary>
public static partial class AecTools
{
    /// <summary>A network description is ~1.5 KB with its capped lists: this many per page stays under the 64 KB cap.</summary>
    public const int MaxNetworkLimit = 30;

    /// <summary>Run / node handles listed per network; the counts are exact whatever the cap (mep_endpoint_check lists every open end).</summary>
    public const int MaxNetworkHandles = 16;

    /// <summary>Open ends located per network in the network page.</summary>
    public const int MaxNetworkOpenEnds = 8;

    /// <summary>An endpoint description is ~250 B with four connections listed: this many per page stays under the 64 KB cap.</summary>
    public const int MaxEndpointLimit = 150;

    /// <summary>Systems listed in the network summary (longest first); <c>systemsTotal</c> is exact.</summary>
    public const int MaxSystemsListed = 20;

    /// <summary>The <c>detection</c> block the MEP tools share: the near-miss reach and the system layer map.</summary>
    public static readonly IReadOnlyList<string> MepDetectionKeys = ["nearMissMm", "systems"];

    public static AnalysisResult<Dictionary<string, object?>> MepDetectNetwork(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs detection, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<Dictionary<string, object?>>();
        (limit, offset) = PageBounds(limit, offset, MaxNetworkLimit, result);
        var read = ReadMep(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, detection, maxCandidates, result, out var nearMissMm, out var systems);
        var networks = read.Network.Networks;
        result.Items = networks.Skip(offset).Take(limit).Select(n => MepService.Describe(n, MaxNetworkHandles, MaxNetworkOpenEnds)).ToArray();
        result.Count = networks.Count;
        result.Offset = offset;
        result.Truncated = networks.Count > offset + result.Items.Count || read.Truncated;
        result.Summary = MepSummary(read, nearMissMm, systems);
        log($"mep_detect_network: {read.Network.Runs} run(s), {read.Network.Nodes} node(s), {networks.Count} network(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<AuditIssue> MepConnectivityCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs detection, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AuditIssue>();
        (limit, offset) = PageBounds(limit, offset, MaxIssueLimit, result);
        var read = ReadMep(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, detection, maxCandidates, result, out var nearMissMm, out var systems);
        var issues = MepChecks.Check(read.Network, ct);
        result.Items = issues.Skip(offset).Take(limit).ToArray();
        result.Count = issues.Count;
        result.Offset = offset;
        result.Truncated = issues.Count > offset + result.Items.Count || read.Truncated;
        result.Summary = new
        {
            examined = read.Examined, runs = read.Network.Runs, nodes = read.Network.Nodes, networks = read.Network.Networks.Count,
            openEnds = read.Network.Endpoints.Count(e => e.IsOpen), orphanNodes = read.Network.OrphanNodes.Count, duplicates = read.Network.Duplicates.Count, crossings = read.Network.Crossings,
            issues = issues.Count, bySeverity = BySeverity(issues), byType = issues.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()), nearMissMm, systems = systems.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(), tolerance = GeometryTolerance.From(toleranceArgs, out _),
        };
        log($"mep_connectivity_check: {read.Network.Runs} run(s), {issues.Count} issue(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<Dictionary<string, object?>> MepEndpointCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs detection, bool includeConnected, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<Dictionary<string, object?>>();
        (limit, offset) = PageBounds(limit, offset, MaxEndpointLimit, result);
        var read = ReadMep(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, detection, maxCandidates, result, out var nearMissMm, out var systems);
        var runs = read.Network.Networks.SelectMany(n => n.Runs).ToDictionary(r => r.Handle, StringComparer.Ordinal);
        var endpoints = read.Network.Endpoints.Where(e => includeConnected || e.IsOpen)
            .OrderBy(e => e.IsOpen ? 0 : 1).ThenBy(e => e.NearestHandle is null ? 1 : 0).ThenBy(e => e.RunHandle.Length).ThenBy(e => e.RunHandle, StringComparer.Ordinal).ThenBy(e => e.End).ToArray();
        result.Items = endpoints.Skip(offset).Take(limit).Select(e => MepService.Describe(e, runs)).ToArray();
        result.Count = endpoints.Length;
        result.Offset = offset;
        result.Truncated = endpoints.Length > offset + result.Items.Count || read.Truncated;
        result.Summary = new
        {
            examined = read.Examined, runs = read.Network.Runs, endpoints = read.Network.Endpoints.Count,
            byState = read.Network.Endpoints.GroupBy(e => e.State).ToDictionary(g => g.Key, g => g.Count()),
            open = read.Network.Endpoints.Count(e => e.IsOpen), nearMisses = read.Network.Endpoints.Count(e => e.NearestHandle is not null),
            bySystem = read.Network.Endpoints.Where(e => e.IsOpen).GroupBy(e => e.System).ToDictionary(g => g.Key, g => g.Count()), nearMissMm, includeConnected,
        };
        log($"mep_endpoint_check: {read.Network.Endpoints.Count} endpoint(s), {result.Count} listed, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    // ---------------------------------------------------------------- helpers

    private static MepService.Outcome ReadMep<T>(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs detection,
        int maxCandidates, AnalysisResult<T> result, out double nearMissMm, out Dictionary<string, IReadOnlyList<string>> systems)
    {
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, toleranceArgs, result);
        foreach (var key in detection.Keys)
            if (!MepDetectionKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"detection.{key} is not an MEP detection setting (known: {string.Join(", ", MepDetectionKeys)}).");
        nearMissMm = PositiveOrDefault(detection.Double("nearMissMm", 0), MepNetworkBuilder.DefaultNearMissMm, "detection.nearMissMm");
        if (nearMissMm <= tol.EndpointConnection) throw new ArgumentException($"detection.nearMissMm ({nearMissMm}) must be above tolerance.endpointConnection ({tol.EndpointConnection}) — equal would report no near miss at all.");
        systems = MepService.ParseSystems(detection.Obj("systems"));
        var read = MepService.Read(db, ed, tr, units, ct, filter, rules, tol, systems, nearMissMm, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling));
        result.Errors.AddRange(read.Classification.Query.Errors);
        result.Warnings.AddRange(read.Classification.Query.Warnings);
        if (read.Network.Runs == 0) result.Warn("No pipe, duct or cable tray runs found: MEP layers are named by the classification rule set (M-PIPE, M-DUCT, E-TRAY, ONG, ONGGIO, MANGCAP…); pass ruleSet or a filter. Nodes are not reported as orphans without runs.");
        if (read.Narrowed) result.Warn("no layers/types in the filter: the scan was narrowed to the rule set's MEP layers, types and fitting block names so maxCandidates counts MEP entities.");
        if (read.Truncated) result.Warn("the scan stopped at maxCandidates: runs beyond it are missing and the open ends reported may be artefacts — raise maxCandidates or filter the layers.");
        if (read.ClosedRunsIgnored > 0) result.Warn($"{read.ClosedRunsIgnored} closed outline(s) on run layers ignored: a run is an open chain (a duct drawn as a closed rectangle is not a run here).");
        if (read.TinyRunsIgnored > 0) result.Warn($"{read.TinyRunsIgnored} run(s) shorter than tolerance.tinySegment ignored.");
        if (read.Network.DoubleLineDucts > 0 && read.Network.DoubleLineDucts >= 0.3 * read.Network.Networks.SelectMany(n => n.Runs).Count(r => r.Kind == MepRunKind.Duct))
            result.Warn($"{read.Network.DoubleLineDucts} duct run(s) have a parallel duct run within {MepNetworkBuilder.DoubleLineDuctMm:0} mm over half their length: this looks like double-line duct drafting — networks, lengths and open ends are per side; the tools read single-line (centreline) runs.");
        return read;
    }

    private static object MepSummary(MepService.Outcome read, double nearMissMm, Dictionary<string, IReadOnlyList<string>> systems)
    {
        var all = read.Network.Networks;
        return new
        {
            examined = read.Examined,
            runs = read.Network.Runs,
            nodes = read.Network.Nodes,
            networks = all.Count,
            totalLengthMm = Math.Round(all.Sum(n => n.LengthMm), 1),
            bySystem = all.SelectMany(n => n.Runs).GroupBy(r => r.System).OrderByDescending(g => g.Sum(r => r.LengthMm)).Take(MaxSystemsListed).ToDictionary(g => g.Key, g => new { runs = g.Count(), lengthMm = Math.Round(g.Sum(r => r.LengthMm), 1) }),
            systemsTotal = all.SelectMany(n => n.Runs).Select(r => r.System).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            byKind = all.SelectMany(n => n.Runs).GroupBy(r => r.Kind).ToDictionary(g => g.Key, g => g.Count()),
            mixedNetworks = all.Count(n => n.Systems.Count > 1),
            openEnds = read.Network.Endpoints.Count(e => e.IsOpen),
            nearMisses = read.Network.Endpoints.Count(e => e.NearestHandle is not null),
            orphanNodes = read.Network.OrphanNodes.Count,
            orphanHandles = read.Network.OrphanNodes.Take(MaxNetworkHandles).Select(n => n.Handle).ToArray(),
            duplicates = read.Network.Duplicates.Count,
            duplicateOverlapMm = Math.Round(read.Network.Duplicates.Sum(d => d.OverlapMm), 1),
            crossings = read.Network.Crossings,
            doubleLineDucts = read.Network.DoubleLineDucts,
            closedRunsIgnored = read.ClosedRunsIgnored,
            tinyRunsIgnored = read.TinyRunsIgnored,
            nearMissMm,
            systems = systems.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            ruleSet = new { name = read.Classification.Rules.Name, source = read.Classification.Rules.Source },
        };
    }
}
