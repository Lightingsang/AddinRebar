using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Coordination;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>The coordination tools: clashes between two sets in plan, and opening requests where MEP routes pass through structure.</summary>
public static partial class AecTools
{
    /// <summary>The MEP types an opening request is made for when the caller names none.</summary>
    public static readonly IReadOnlyList<string> DefaultRouteTypes = [AecType.Pipe, AecType.Duct, AecType.CableTray];

    /// <summary>A clash issue names two entities and their types in ~520 B: this many per page stays under the 64 KB cap.</summary>
    public const int MaxClashLimit = 80;

    /// <summary>The hosts an opening request is made through when the caller names none: members with a thickness, never a slab (a run crossing a slab edge in plan leaves the floor plate).</summary>
    public static readonly IReadOnlyList<string> DefaultHostTypes = [AecType.ArchitecturalWall, AecType.StructuralWall, AecType.StructuralBeam];

    /// <summary>Issues below this severity are counted, not listed, unless asked for: contacts and area overlaps are the normal state of a plan.</summary>
    public const string DefaultClashMinSeverity = IssueSeverity.Warning;

    public static AnalysisResult<AuditIssue> AecClashCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs setA, ScriptArgs setB, string? ruleSet, ScriptArgs toleranceArgs, double clearanceMm, string? minSeverity, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AuditIssue>();
        (limit, offset) = PageBounds(limit, offset, MaxClashLimit, result);
        clearanceMm = clearanceMm < 0 ? throw new ArgumentException("clearanceMm must be >= 0 (0 = hard clashes only).") : clearanceMm;
        var minimum = string.IsNullOrWhiteSpace(minSeverity) ? DefaultClashMinSeverity : minSeverity.Trim().ToLowerInvariant();
        if (minimum is not (IssueSeverity.Critical or IssueSeverity.Warning or IssueSeverity.Info)) throw new ArgumentException("minSeverity must be critical, warning or info.");
        var rules = ClassificationRuleSet.Load(ruleSet);
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance or not positive.");
        var a = ReadSet(db, ed, tr, units, ct, setA, "setA", rules, tol, maxCandidates, result);
        var sameSet = setB.Keys.Count == 0;
        var b = sameSet ? a : ReadSet(db, ed, tr, units, ct, setB, "setB", rules, tol, maxCandidates, result);
        if (a.Subjects.Count == 0) result.Warn($"setA matched no classified entity with a shape ({a.Classification.Query.Records.Count} examined; classify_aec_entities shows what the rule set recognises).");
        if (!sameSet && b.Subjects.Count == 0) result.Warn($"setB matched no classified entity with a shape ({b.Classification.Query.Records.Count} examined).");
        var clashes = ClashDetector.Detect(a.Subjects, b.Subjects, sameSet, clearanceMm, tol, ct);
        if (clashes.Capped) result.Warn($"the narrow phase stopped at its work cap ({ClashDetector.MaxPairs} candidate pairs / {ClashDetector.MaxSegmentPairs} segment pairs): narrow a set (layers, aecTypes) or lower clearanceMm.");
        var listed = clashes.Issues.Where(i => i.AtLeast(minimum)).ToArray();
        result.Items = listed.Skip(offset).Take(limit).ToArray();
        result.Count = listed.Length;
        result.Offset = offset;
        result.Truncated = listed.Length > offset + result.Items.Count || a.Classification.Query.Truncated || b.Classification.Query.Truncated || clashes.Capped;
        result.Summary = new
        {
            setA = Describe(a), setB = sameSet ? null : Describe(b), sameSet, clearanceMm, minSeverity = minimum, pairsChecked = clashes.PairsChecked,
            found = clashes.Issues.Count, listed = listed.Length, belowMinSeverity = clashes.Issues.Count - listed.Length,
            hard = clashes.Hard, clearance = clashes.Clearance, contacts = clashes.Contacts, areaOverlaps = clashes.AreaOverlaps, bySeverity = BySeverity(clashes.Issues),
            byPair = clashes.Issues.Where(i => i.AtLeast(IssueSeverity.Warning)).GroupBy(i => i.Rule ?? "").OrderByDescending(g => g.Count()).Take(20).ToDictionary(g => g.Key, g => g.Count()),
            tolerance = tol, ruleSet = new { name = rules.Name, source = rules.Source },
        };
        log($"aec_clash_check: {a.Subjects.Count} × {b.Subjects.Count} subjects, {clashes.PairsChecked} pair(s), {clashes.Hard} hard / {clashes.Clearance} clearance / {clashes.Contacts} contact(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult AecCreateOpeningRequests(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs routes, ScriptArgs hosts, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs sizes, double maxChordMm, string? layer, double textHeightMm, string? space, bool apply, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var probe = new AnalysisResult<object>();
        var rules = ClassificationRuleSet.Load(ruleSet);
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        foreach (var key in unknownTolerance) probe.Warn($"tolerance.{key} is not a known tolerance or not positive.");
        textHeightMm = PositiveOrDefault(textHeightMm, OpeningWriteService.DefaultTextHeightMm, "textHeightMm");
        maxChordMm = PositiveOrDefault(maxChordMm, OpeningPlanner.DefaultMaxChordMm, "maxChordMm");
        var sizing = Sizes(sizes);
        var r = ReadSet(db, ed, tr, units, ct, routes, "routes", rules, tol, maxCandidates, probe, DefaultRouteTypes);
        var h = ReadSet(db, ed, tr, units, ct, hosts, "hosts", rules, tol, maxCandidates, probe, DefaultHostTypes);
        if (r.Subjects.Count == 0) probe.Warn("routes matched no pipe, duct or cable tray run (classify_aec_entities shows what the rule set recognises).");
        if (h.Subjects.Count == 0) probe.Warn("hosts matched no wall or beam (name aecTypes for other hosts).");
        var plan = OpeningPlanner.Plan(r.Subjects.Select(CoordinationService.ToOpeningSubject).ToArray(), h.Subjects.Select(CoordinationService.ToOpeningSubject).ToArray(), sizing, maxChordMm, tol, ct);
        if (plan.LongChordsSkipped > 0) probe.Warn($"{plan.LongChordsSkipped} pass(es) longer than maxChordMm {maxChordMm:0} skipped: a run drawn inside an outline, not an opening through it (raise maxChordMm to request them).");
        var result = plan.Requests.Count == 0
            ? new EditResult { Items = [], Summary = new { requested = 0, drawn = 0, reason = "no route passes through a host" } }
            : OpeningWriteService.Write(new EditContext(db, tr, units), ct, plan.Requests, layer, textHeightMm, RouteSpace(space, routes, r), dryDecisionsOnly: !apply);
        foreach (var w in probe.Warnings) result.Warn(w);
        result.Summary = new { routes = r.Subjects.Count, hosts = h.Subjects.Count, sizes = sizing, maxChordMm, longChordsSkipped = plan.LongChordsSkipped, plan = result.Summary };
        log($"aec_create_opening_requests: {r.Subjects.Count} route(s) × {h.Subjects.Count} host(s), {plan.Requests.Count} request(s), {plan.LongChordsSkipped} long chord(s), apply={apply}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>
    ///     Where the requests go: the caller's space, else the routes' filter space, else — when the routes were read from every space
    ///     (handles) but all live in one — that space; routes spread over several spaces need the caller to choose.
    /// </summary>
    private static string RouteSpace(string? space, ScriptArgs routes, CoordinationService.SetOutcome read)
    {
        var filter = EntityFilter.From(routes.Obj("filter"), out _);
        if (string.IsNullOrWhiteSpace(space) && filter.Space.Equals("all", StringComparison.OrdinalIgnoreCase) && read.Spaces.Count == 1) return read.Spaces[0];
        return WriteSpace(space, filter);
    }

    private static CoordinationService.SetOutcome ReadSet<T>(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, ScriptArgs set, string name, ClassificationRuleSet rules, GeometryTolerance tol, int maxCandidates,
        AnalysisResult<T> result, IReadOnlyList<string>? defaultTypes = null)
    {
        foreach (var key in set.Keys)
            if (key is not ("filter" or "aecTypes")) throw new ArgumentException($"{name}.{key} is not a set key (known: filter, aecTypes).");
        var filter = EntityFilter.From(set.Obj("filter"), out var unknown);
        foreach (var key in unknown) result.Warn($"{name}.filter.{key} is not a known filter key (known: {string.Join(", ", EntityFilter.KnownKeys)}).");
        var types = CoordinationService.ParseTypes(set.Strings("aecTypes"), name);
        if (types.Count == 0 && defaultTypes is not null) types = defaultTypes;
        var read = CoordinationService.Read(db, ed, tr, units, ct, filter, types, rules, tol, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling));
        result.Errors.AddRange(read.Classification.Query.Errors);
        result.Warnings.AddRange(read.Classification.Query.Warnings);
        if (read.Classification.Query.Truncated) result.Warn($"{name}: the scan stopped at maxCandidates; entities beyond it were not checked.");
        if (read.WithoutShape > 0) result.Warn($"{name}: {read.WithoutShape} matching entit(ies) have no plan shape (or are shorter than tolerance.tinySegment) and were skipped.");
        return read;
    }

    private static object Describe(CoordinationService.SetOutcome set) => new
    {
        examined = set.Classification.Query.Records.Count,
        subjects = set.Subjects.Count,
        aecTypes = set.AecTypes.Count == 0 ? "all" : string.Join(", ", set.AecTypes),
        byType = set.Subjects.GroupBy(s => s.AecType).ToDictionary(g => g.Key, g => g.Count()),
    };

    private static OpeningSizes Sizes(ScriptArgs sizes)
    {
        foreach (var key in sizes.Keys)
            if (key is not ("pipeMm" or "ductMm" or "trayMm" or "marginMm")) throw new ArgumentException($"sizes.{key} is not a size (known: pipeMm, ductMm, trayMm, marginMm).");
        var d = OpeningSizes.Default;
        var margin = sizes.Double("marginMm", -1);
        return new OpeningSizes(PositiveOrDefault(sizes.Double("pipeMm", 0), d.PipeMm, "sizes.pipeMm"), PositiveOrDefault(sizes.Double("ductMm", 0), d.DuctMm, "sizes.ductMm"),
            PositiveOrDefault(sizes.Double("trayMm", 0), d.TrayMm, "sizes.trayMm"), margin < 0 && sizes.Has("marginMm") ? throw new ArgumentException("sizes.marginMm must be >= 0.") : sizes.Has("marginMm") ? margin : d.MarginMm);
    }
}
