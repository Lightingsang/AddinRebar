using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Spatial;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>
///     The one entry point AEC seed scripts call. Each method takes the script globals plus the tool's
///     already-read arguments, resolves nested objects (<c>filter</c>, <c>tolerance</c>) the same way for
///     every tool, runs the service, and logs tool / duration / counts through the script's <c>log</c>.
///     Scripts stay a few lines: read <c>args</c>, call here, return.
/// </summary>
public static partial class AecTools
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 500;

    /// <summary>Detail records carry up to 64 vertices (~2.5 KB each): more than this per page would pass the bridge's 64 KB result cap.</summary>
    public const int MaxDetailLimit = 20;

    public const int MaxCandidatesCeiling = 100_000;

    /// <summary>Layers listed in a query summary; the full breakdown is the query itself.</summary>
    private const int SummaryLayers = 20;

    /// <summary>The detector may find this many times the page size before it stops, so `count` stays meaningful past the page.</summary>
    private const int IssueOverscan = 4;

    public static object DrawingContext(Document doc, Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        bool includeLayouts, bool includeLayers)
    {
        var watch = Stopwatch.StartNew();
        var context = DrawingContextReader.Read(doc, db, ed, tr, units, ct, includeLayouts, includeLayers);
        log($"get_drawing_context: {watch.ElapsedMilliseconds} ms");
        return context;
    }

    public static AnalysisResult<Dictionary<string, object?>> QueryEntities(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, int limit, int offset, string? mode, IReadOnlyList<string> properties, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<Dictionary<string, object?>>();
        var filter = EntityFilter.From(filterArgs, out var unknown);
        foreach (var key in unknown) result.Warn($"filter.{key} is not a known filter key (known: {string.Join(", ", EntityFilter.KnownKeys)}).");
        var detail = ParseMode(mode);
        limit = Math.Clamp(limit, 1, MaxLimit);
        if (detail && limit > MaxDetailLimit)
        {
            result.Warn($"mode=detail returns at most {MaxDetailLimit} entities per page (geometry is large); page with offset.");
            limit = MaxDetailLimit;
        }

        offset = Math.Max(0, offset);
        maxCandidates = Math.Max(Math.Min(maxCandidates, MaxCandidatesCeiling), Math.Min(limit + offset, MaxCandidatesCeiling));

        HashSet<string>? wanted = null;
        if (properties.Count > 0)
        {
            wanted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in properties)
            {
                var name = EntityQueryService.PropertyNames.FirstOrDefault(n => string.Equals(n, p, StringComparison.OrdinalIgnoreCase));
                if (name is null) result.Warn($"properties: '{p}' is not known (known: {string.Join(", ", EntityQueryService.PropertyNames)}).");
                else wanted.Add(name);
            }
        }

        var query = EntityQueryService.Query(db, ed, tr, units, ct, filter, GeometryTolerance.Default, limit, offset, detail || wanted?.Contains("geometry") == true, maxCandidates);
        result.Errors.AddRange(query.Errors);
        result.Warnings.AddRange(query.Warnings);
        result.Items = query.Records.Select(r => EntityQueryService.Project(r, wanted, detail)).ToList();
        result.Count = query.Count;
        result.Offset = offset;
        result.Truncated = query.Truncated || query.Count > offset + query.Records.Count;
        result.Summary = new
        {
            matched = query.Count,
            returned = query.Records.Count,
            byType = query.Records.GroupBy(r => r.Type).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count()),
            byLayer = query.Records.GroupBy(r => r.Layer).OrderByDescending(g => g.Count()).Take(SummaryLayers).ToDictionary(g => g.Key, g => g.Count()),
        };
        result.Success = result.Errors.Count == 0 || query.Records.Count > 0;
        log($"query_entities: {query.Count} matched, {query.Records.Count} returned (offset {offset}, limit {limit}), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<SpatialMatchItem> QuerySpatial(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs sourceArgs, ScriptArgs targetArgs, string? relationText, double? maxDistanceMm, ScriptArgs toleranceArgs, int limit, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        if (!SpatialRelations.TryParse(relationText, out var relation))
            throw new ArgumentException($"relation must be one of {string.Join(", ", SpatialRelations.Names)}.");
        var source = EntityFilter.From(sourceArgs, out var unknownSource);
        var target = EntityFilter.From(targetArgs, out var unknownTarget);
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        limit = Math.Clamp(limit, 1, MaxLimit);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);

        var result = SpatialQueryService.Query(db, ed, tr, units, ct, source, target, relation, maxDistanceMm, tol, limit, maxCandidates);
        foreach (var key in unknownSource) result.Warn($"source.{key} is not a known filter key.");
        foreach (var key in unknownTarget) result.Warn($"target.{key} is not a known filter key.");
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance (known: pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap).");
        log($"query_entities_spatial {SpatialRelations.Name(relation)}: {result.Count} matches, {result.Items.Count} returned, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<object> Measure(Database db, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        string? measure, IReadOnlyList<string> handles, IReadOnlyList<ScriptArgs> points, ScriptArgs toleranceArgs)
    {
        var watch = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(measure)) throw new ArgumentException($"measure is required: one of {string.Join(", ", MeasureService.Measures)}.");
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        var pointsMm = points.Select(p => new Pt(p.Double("x"), p.Double("y"), p.Double("z"))).ToArray();
        if (handles.Count == 0 && pointsMm.Length == 0) throw new ArgumentException("Give handles[] and/or points[] to measure.");

        var result = MeasureService.Measure(db, tr, units, ct, measure!, handles, pointsMm, tol);
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance.");
        log($"measure_geometry {measure}: {result.Items.Count} item(s), {result.Errors.Count} error(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<GeometryIssue> DetectGeometryIssues(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> handles, IReadOnlyList<string> issueTypes, ScriptArgs toleranceArgs, int limit, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<GeometryIssue>();
        var filter = EntityFilter.From(filterArgs, out var unknownFilter);
        if (handles.Count > 0)
        {
            if (!filter.IsEmpty) result.Warn("handles[] given: filter.* was ignored (handles select exactly those entities).");
            filter = new EntityFilter { Handles = handles.Select(h => h.Trim().ToUpperInvariant()).ToArray(), Space = "all" };
        }

        if (filter.IsEmpty) result.Warn("No filter given: every entity in model space is examined (up to maxCandidates). Give layers/types to focus the check.");
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        foreach (var key in unknownFilter) result.Warn($"filter.{key} is not a known filter key.");
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance.");

        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in issueTypes)
        {
            var known = GeometryIssueType.All.FirstOrDefault(k => string.Equals(k, t.Replace('-', '_'), StringComparison.OrdinalIgnoreCase));
            if (known is null) result.Warn($"issueTypes: '{t}' is not known (known: {string.Join(", ", GeometryIssueType.All)}).");
            else types.Add(known);
        }

        if (types.Count == 0) types.UnionWith(GeometryIssueType.All);
        limit = Math.Clamp(limit, 1, MaxLimit);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);

        var query = EntityQueryService.Query(db, ed, tr, units, ct, filter, tol, maxCandidates, 0, false, maxCandidates);
        result.Errors.AddRange(query.Errors);
        result.Warnings.AddRange(query.Warnings);

        var issues = GeometryIssueDetector.Detect(query.Records, types, tol, ct, limit * IssueOverscan);
        result.Items = issues.Take(limit).ToArray();
        result.Count = issues.Count;
        result.Truncated = issues.Count > limit || query.Truncated;
        result.Summary = new
        {
            examined = query.Records.Count,
            withoutGeometry = query.Records.Count(r => r.Shape is null),
            issues = issues.Count,
            bySeverity = issues.GroupBy(i => i.Severity).ToDictionary(g => g.Key, g => g.Count()),
            byType = issues.GroupBy(i => i.Type).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count()),
            checkedTypes = types.OrderBy(t => t).ToArray(),
            tolerance = tol,
        };
        log($"detect_geometry_issues: {query.Records.Count} entities, {issues.Count} issue(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    private static bool ParseMode(string? mode) => (mode ?? "summary").Trim().ToLowerInvariant() switch
    {
        "summary" or "" => false,
        "detail" or "details" or "full" => true,
        _ => throw new ArgumentException("mode must be summary or detail."),
    };
}
