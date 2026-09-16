using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Standards;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     One query, several audit sections: <c>geometry</c> (the phase-A detector) and <c>standards</c> (the rule-driven checker),
///     merged into <see cref="AuditIssue"/>s in the stable severity order so a page is the same on every run. A section that
///     cannot be trusted on a subset (unused layers) is skipped with a warning rather than reported wrongly.
/// </summary>
public static class AuditService
{
    /// <summary>The geometry detector stops after this many findings per run — a page is 100, and the count stays meaningful well past it.</summary>
    public const int MaxGeometryIssues = 2000;

    public sealed record Outcome(IReadOnlyList<AuditIssue> Issues, EntityQueryResult Query, IReadOnlyList<string> SectionsRun, IReadOnlyList<string> ChecksRun, bool WholeDrawing, List<string> Warnings, bool GeometryCapped = false);

    public static Outcome Run(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct,
        EntityFilter filter, IReadOnlySet<string> sections, GeometryTolerance tol, CadStandardsRuleSet? rules, IReadOnlySet<string>? standardsChecks, int maxCandidates)
    {
        var warnings = new List<string>();
        var query = EntityQueryService.Query(db, ed, tr, units, ct, filter, tol, maxCandidates, 0, false, maxCandidates);
        var wholeDrawing = filter.IsEmpty && filter.Space.Equals("all", StringComparison.OrdinalIgnoreCase) && !query.Truncated;
        var issues = new List<AuditIssue>();
        var sectionsRun = new List<string>();
        var checksRun = new List<string>();

        var geometryCapped = false;
        if (sections.Contains(AuditCategory.Geometry))
        {
            // The detector checks every space on its own (a title block repeated on two layouts is not a duplicate).
            var found = GeometryIssueDetector.Detect(query.Records, new HashSet<string>(GeometryIssueType.All), tol, ct, MaxGeometryIssues);
            geometryCapped = found.Count >= MaxGeometryIssues;
            issues.AddRange(found.Select(AuditIssue.From));
            if (geometryCapped) warnings.Add($"geometry findings stopped at {MaxGeometryIssues}; narrow the filter to see the rest.");
            sectionsRun.Add(AuditCategory.Geometry);
        }

        if (sections.Contains(AuditCategory.Standards) && rules is not null)
        {
            var enabled = new HashSet<string>(rules.EnabledChecks(), StringComparer.OrdinalIgnoreCase);
            if (standardsChecks is not null) enabled.IntersectWith(standardsChecks);
            if (enabled.Contains(StandardsIssueType.UnusedLayer) && !wholeDrawing)
            {
                enabled.Remove(StandardsIssueType.UnusedLayer);
                warnings.Add("unused_layer skipped: it needs the whole drawing (no filter, space: all, not truncated).");
            }

            var tables = DrawingTablesReader.Read(db, tr, ct);
            issues.AddRange(CadStandardsChecker.Check(query.Records, tables, rules, enabled, wholeDrawing, ct));
            sectionsRun.Add(AuditCategory.Standards);
            checksRun.AddRange(enabled.OrderBy(c => c));
        }

        return new Outcome(AuditIssue.Ordered(issues), query, sectionsRun, checksRun, wholeDrawing, warnings, geometryCapped);
    }
}
