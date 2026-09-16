using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Standards;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>The QA/QC tools: CAD standards, the aggregate audit, and issue markup.</summary>
public static partial class AecTools
{
    /// <summary>An audit issue with a long layer name and two handles is ~500 B: 100 per page (+ summary) measures under the 64 KB result cap.</summary>
    public const int MaxIssueLimit = 100;

    public static AnalysisResult<AuditIssue> CadStandardsCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> handles, string? ruleSet, IReadOnlyList<string> checks, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AuditIssue>();
        var filter = ResolveFilter(filterArgs, handles, result);
        if (filter.IsEmpty && !filterArgs.Has("space")) filter = new EntityFilter { Space = "all" }; // the whole drawing: layers and blocks are checked as tables, entities everywhere
        var rules = CadStandardsRuleSet.Load(ruleSet);
        var wanted = ParseChecks(checks, StandardsIssueType.All, "checks", result);
        foreach (var disabled in wanted.Except(rules.EnabledChecks(), StringComparer.OrdinalIgnoreCase).Where(_ => checks.Count > 0))
            result.Warn($"checks: '{disabled}' is not enabled by rule set '{rules.Name}' (its section is missing or empty).");
        (limit, offset) = PageBounds(limit, offset, MaxIssueLimit, result);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);

        var outcome = AuditService.Run(db, ed, tr, units, ct, filter, new HashSet<string> { AuditCategory.Standards }, GeometryTolerance.Default, rules, wanted, maxCandidates);
        Fill(result, outcome, limit, offset);
        result.Summary = new
        {
            examined = outcome.Query.Records.Count,
            wholeDrawing = outcome.WholeDrawing,
            issues = outcome.Issues.Count,
            bySeverity = BySeverity(outcome.Issues),
            byType = outcome.Issues.GroupBy(i => i.Type).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count()),
            checkedTypes = outcome.ChecksRun,
            allowedTextStyles = rules.TextStyles?.Allowed is { Count: > 0 } ts ? ts : null,
            allowedTextHeightsMm = rules.TextHeights?.AllowedMm is { Count: > 0 } th ? th : null,
            allowedDimStyles = rules.DimStyles?.Allowed is { Count: > 0 } ds ? ds : null,
            entityLayerRules = rules.EntityLayer.Select(r => new { r.Id, r.Types, r.Layers }).ToArray(),
            ruleSet = new { name = rules.Name, source = rules.Source, version = rules.Version },
        };
        log($"cad_standards_check: {outcome.Query.Records.Count} entities, {outcome.Issues.Count} issue(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<AuditIssue> AuditAecDrawing(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> sections, ScriptArgs toleranceArgs, string? ruleSet, string? minSeverity, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AuditIssue>();
        var filter = EntityFilter.From(filterArgs, out var unknownFilter);
        foreach (var key in unknownFilter) result.Warn($"filter.{key} is not a known filter key (known: {string.Join(", ", EntityFilter.KnownKeys)}).");
        if (filter.IsEmpty && !filterArgs.Has("space")) filter = new EntityFilter { Space = "all" };
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance or not positive.");
        var wanted = ParseChecks(sections, AuditCategory.All, "sections", result);
        var minimum = (minSeverity ?? IssueSeverity.Info).Trim().ToLowerInvariant();
        if (minimum is not (IssueSeverity.Critical or IssueSeverity.Warning or IssueSeverity.Info)) throw new ArgumentException("minSeverity must be critical, warning or info.");
        var rules = wanted.Contains(AuditCategory.Standards) ? CadStandardsRuleSet.Load(ruleSet) : null;
        (limit, offset) = PageBounds(limit, offset, MaxIssueLimit, result);
        maxCandidates = Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling);

        var outcome = AuditService.Run(db, ed, tr, units, ct, filter, wanted, tol, rules, null, maxCandidates);
        var issues = outcome.Issues.Where(i => i.AtLeast(minimum)).ToArray();
        Fill(result, outcome with { Issues = issues }, limit, offset);
        result.Summary = new
        {
            sections = outcome.SectionsRun,
            examined = outcome.Query.Records.Count,
            wholeDrawing = outcome.WholeDrawing,
            issues = issues.Length,
            belowMinSeverity = outcome.Issues.Count - issues.Length,
            bySeverity = BySeverity(issues),
            byCategory = issues.GroupBy(i => i.Category).ToDictionary(g => g.Key, g => g.Count()),
            byType = issues.GroupBy(i => i.Type).OrderByDescending(g => g.Count()).Take(20).ToDictionary(g => g.Key, g => g.Count()),
            checkedStandards = outcome.ChecksRun,
            ruleSet = rules is null ? null : new { name = rules.Name, source = rules.Source, version = rules.Version },
            tolerance = tol,
        };
        log($"audit_aec_drawing [{string.Join(",", outcome.SectionsRun)}]: {outcome.Query.Records.Count} entities, {issues.Length} issue(s) ≥ {minimum}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult CreateIssueMarkup(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        IReadOnlyList<ScriptArgs> issues, string? style, string? layer, double radiusMm, double textHeightMm, bool withLeader, bool colorBySeverity, string? space, bool atomic)
    {
        var watch = Stopwatch.StartNew();
        var result = IssueMarkupService.Create(new EditContext(db, tr, units), ct, issues, style, layer, radiusMm, textHeightMm, withLeader, colorBySeverity, space, atomic);
        log($"create_issue_markup: {issues.Count} issue(s) → {result.CreatedCount} entities, {result.Errors.Count} error(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    // ---------------------------------------------------------------- helpers

    private static void Fill(AnalysisResult<AuditIssue> result, AuditService.Outcome outcome, int limit, int offset)
    {
        result.Errors.AddRange(outcome.Query.Errors);
        result.Warnings.AddRange(outcome.Query.Warnings);
        result.Warnings.AddRange(outcome.Warnings);
        result.Items = outcome.Issues.Skip(offset).Take(limit).ToArray();
        result.Count = outcome.Issues.Count;
        result.Offset = offset;
        result.Truncated = outcome.Query.Truncated || outcome.GeometryCapped || outcome.Issues.Count > offset + result.Items.Count;
    }

    private static Dictionary<string, int> BySeverity(IReadOnlyList<AuditIssue> issues) => new()
    {
        [IssueSeverity.Critical] = issues.Count(i => i.Severity == IssueSeverity.Critical),
        [IssueSeverity.Warning] = issues.Count(i => i.Severity == IssueSeverity.Warning),
        [IssueSeverity.Info] = issues.Count(i => i.Severity == IssueSeverity.Info),
    };

    private static HashSet<string> ParseChecks<T>(IReadOnlyList<string> given, IReadOnlyList<string> known, string argName, AnalysisResult<T> result)
    {
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in given)
        {
            var match = known.FirstOrDefault(k => string.Equals(k, g.Trim().Replace('-', '_'), StringComparison.OrdinalIgnoreCase));
            if (match is null) result.Warn($"{argName}: '{g}' is not known (known: {string.Join(", ", known)}).");
            else wanted.Add(match);
        }

        if (wanted.Count == 0) wanted.UnionWith(known);
        return wanted;
    }

    private static (int Limit, int Offset) PageBounds<T>(int limit, int offset, int cap, AnalysisResult<T> result)
    {
        if (limit > cap) result.Warn($"limit is capped at {cap} per page; page with offset.");
        return (Math.Clamp(limit, 1, cap), Math.Max(0, offset));
    }
}
