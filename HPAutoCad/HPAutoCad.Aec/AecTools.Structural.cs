using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Structural;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>The structural tools: grids, members, the three geometry checks, tagging and the schedule. No capacity claims anywhere.</summary>
public static partial class AecTools
{
    /// <summary>Grid intersections listed in a summary (~100 B each); the count is exact whatever the cap.</summary>
    public const int MaxIntersectionsListed = 200;

    /// <summary>A grid line is ~200 B and the summary carries the intersections: this many lines per page stays under the 64 KB cap.</summary>
    public const int MaxGridLimit = 100;

    /// <summary>A member description is ~470 B: this many per page stays under the 64 KB cap with room for the summary.</summary>
    public const int MaxMemberLimit = 100;

    /// <summary>A tagged member is echoed three times (~250 B); more than this per call and the answer would not fit — tag by kind or filter.</summary>
    public const int MaxTagMembers = 120;

    public static AnalysisResult<GridLine> StructuralDetectGrids(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, double reachMm, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<GridLine>();
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, toleranceArgs, result);
        if (reachMm < 0) throw new ArgumentException("reachMm must be >= 0 (0 = the default).");
        if (reachMm == 0) reachMm = GridDetector.BubbleReachMm;
        (limit, offset) = PageBounds(limit, offset, MaxGridLimit, result);
        var read = StructuralService.Read(db, ed, tr, units, ct, filter, rules, tol, new HashSet<string>(), 0, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling), reachMm);
        result.Errors.AddRange(read.Classification.Query.Errors);
        result.Warnings.AddRange(read.Classification.Query.Warnings);
        var grids = read.Grids;
        result.Items = grids.Lines.Skip(offset).Take(limit).ToArray();
        result.Count = grids.Lines.Count;
        result.Offset = offset;
        result.Truncated = grids.Lines.Count > offset + result.Items.Count || read.Classification.Query.Truncated;
        if (grids.UnboundedCount > 0) result.Warn($"{grids.UnboundedCount} grid object(s) are XLINE/RAY (no extent): not reported as lines; draw grids as LINEs to have them detected.");
        if (grids.Lines.Count == 0) result.Warn("No grid lines found: grid layers are named by the classification rule set (S-GRID, A-GRID, GRID, TRUC, AXIS…); pass ruleSet or a filter.");
        result.Summary = new
        {
            lines = grids.Lines.Count,
            labeled = grids.Lines.Count(l => l.Label is not null),
            merged = grids.Lines.Sum(l => l.MergedSegments - 1),
            unbounded = grids.UnboundedCount,
            byDirection = grids.Lines.GroupBy(l => l.Direction).ToDictionary(g => g.Key, g => g.Count()),
            labels = grids.Lines.Where(l => l.Label is not null).Select(l => l.Label!).OrderBy(x => x.Length).ThenBy(x => x, StringComparer.Ordinal).ToArray(),
            spacingMm = grids.SpacingMm,
            intersectionCount = grids.Intersections.Count,
            intersections = grids.Intersections.Take(MaxIntersectionsListed).ToArray(),
            intersectionsTruncated = grids.Intersections.Count > MaxIntersectionsListed,
            bubbleReachMm = reachMm,
        };
        log($"structural_detect_grids: {grids.Lines.Count} line(s), {grids.Intersections.Count} intersection(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<Dictionary<string, object?>> StructuralDetectMembers(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> kinds, string? ruleSet, ScriptArgs prefixes, double minConfidence, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<Dictionary<string, object?>>();
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, ScriptArgs.Empty, result);
        var wanted = StructuralService.ParseKinds(kinds, result);
        var prefixMap = StructuralService.ParsePrefixes(prefixes);
        (limit, offset) = PageBounds(limit, offset, MaxMemberLimit, result);
        var read = StructuralService.Read(db, ed, tr, units, ct, filter, rules, tol, wanted, Math.Clamp(minConfidence, 0, 1), Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling), prefixes: prefixMap);
        result.Errors.AddRange(read.Classification.Query.Errors);
        result.Warnings.AddRange(read.Classification.Query.Warnings);
        var members = read.Members.OrderBy(m => MemberKind.Rank(m.Kind)).ThenBy(m => m.Handle.Length).ThenBy(m => m.Handle, StringComparer.Ordinal).ToArray();
        result.Items = members.Skip(offset).Take(limit).Select(StructuralService.Describe).ToArray();
        result.Count = members.Length;
        result.Offset = offset;
        result.Truncated = members.Length > offset + result.Items.Count || read.Classification.Query.Truncated;
        result.Summary = new
        {
            examined = read.Classification.Query.Records.Count,
            members = members.Length,
            byKind = members.GroupBy(m => m.Kind).OrderBy(g => MemberKind.Rank(g.Key)).ToDictionary(g => g.Key, g => g.Count()),
            sections = members.GroupBy(m => (m.Kind, m.Section)).OrderByDescending(g => g.Count()).Take(20).Select(g => new { kind = g.Key.Kind, section = g.Key.Section, count = g.Count() }).ToArray(),
            marked = members.Count(m => m.Mark is not null),
            duplicateExisting = MemberTagging.DuplicateExisting(members),
            gridLines = read.Grids.Lines.Count,
            ruleSet = new { name = rules.Name, source = rules.Source },
        };
        log($"structural_detect_members: {read.Classification.Query.Records.Count} entities, {members.Length} member(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<AuditIssue> StructuralMemberConnectivityCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, int limit, int offset, int maxCandidates) =>
        StructuralCheck("structural_member_connectivity_check", db, ed, tr, units, ct, log, filterArgs, ruleSet, toleranceArgs, limit, offset, maxCandidates,
            (read, tol, _) => StructuralChecks.Connectivity(read.Members, tol, ct),
            read => new { beams = read.Members.Count(m => m.Kind == MemberKind.Beam), supports = read.Members.Count(m => MemberKind.Supports.Contains(m.Kind)) });

    public static AnalysisResult<AuditIssue> StructuralColumnAlignmentCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, double alignmentToleranceMm, double searchRadiusMm, int limit, int offset, int maxCandidates)
    {
        alignmentToleranceMm = PositiveOrDefault(alignmentToleranceMm, StructuralChecks.DefaultAlignmentToleranceMm, "alignmentToleranceMm");
        searchRadiusMm = PositiveOrDefault(searchRadiusMm, StructuralChecks.GridSearchRadiusMm, "searchRadiusMm");
        return StructuralCheck("structural_column_alignment_check", db, ed, tr, units, ct, log, filterArgs, ruleSet, toleranceArgs, limit, offset, maxCandidates,
            (read, _, result) =>
            {
                if (read.Grids.Intersections.Count == 0) result.Warn($"no grid intersections found ({read.Grids.Lines.Count} grid line(s)); nothing to align columns to — check the grid layer / rule set.");
                return StructuralChecks.ColumnAlignment(read.Members, read.Grids, alignmentToleranceMm, searchRadiusMm, ct);
            },
            read => new { columns = read.Members.Count(m => m.Kind == MemberKind.Column), gridLines = read.Grids.Lines.Count, intersections = read.Grids.Intersections.Count, alignmentToleranceMm, searchRadiusMm });
    }

    public static AnalysisResult<AuditIssue> StructuralOpeningConflictCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, double clearanceMm, int limit, int offset, int maxCandidates)
    {
        clearanceMm = PositiveOrDefault(clearanceMm, StructuralChecks.DefaultOpeningClearanceMm, "clearanceMm");
        return StructuralCheck("structural_opening_conflict_check", db, ed, tr, units, ct, log, filterArgs, ruleSet, toleranceArgs, limit, offset, maxCandidates,
            (read, tol, _) => StructuralChecks.OpeningConflicts(read.Members, tol, clearanceMm, ct),
            read => new { openings = read.Members.Count(m => m.Kind == MemberKind.Opening), columns = read.Members.Count(m => m.Kind == MemberKind.Column), hosts = read.Members.Count(m => m.Kind is MemberKind.Slab or MemberKind.Wall), clearanceMm });
    }

    public static EditResult StructuralTagMembers(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> kinds, string? ruleSet, ScriptArgs prefixes, int start, int digits, string? sortBy, bool overwrite, string? layer, double textHeightMm, string? space, bool apply)
    {
        var watch = Stopwatch.StartNew();
        var probe = new AnalysisResult<object>();
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, ScriptArgs.Empty, probe);
        var wanted = StructuralService.ParseKinds(kinds, probe);
        var prefixMap = StructuralService.ParsePrefixes(prefixes);
        if (start < 0 || digits is < 1 or > 6) throw new ArgumentException("start must be >= 0 and digits 1..6.");
        textHeightMm = PositiveOrDefault(textHeightMm, StructuralWriteService.DefaultTagHeightMm, "textHeightMm");
        var read = StructuralService.Read(db, ed, tr, units, ct, filter, rules, tol, wanted, 0, MaxCandidatesCeiling, prefixes: prefixMap);
        if (read.Members.Count == 0) throw new ArgumentException("no structural members matched the filter/kinds (structural_detect_members shows what is classified).");
        if (read.Members.Count > MaxTagMembers) throw new ArgumentException($"{read.Members.Count} members match; the maximum per call is {MaxTagMembers} — tag one kind or a filter at a time (start continues the numbering).");
        if (read.Classification.Query.Truncated) probe.Warn("the scan was truncated at maxCandidates: members beyond it are not tagged.");
        var ordered = MemberTagging.Order(read.Members, sortBy ?? "row");
        var tags = MemberTagging.Assign(ordered, prefixMap, start, digits, overwrite);
        var result = StructuralWriteService.WriteTags(new EditContext(db, tr, units), ct, tags, layer, textHeightMm, WriteSpace(space, filter), dryDecisionsOnly: !apply, MemberTagging.DuplicateExisting(read.Members));
        foreach (var w in probe.Warnings) result.Warn(w);
        log($"structural_tag_members: {tags.Count} member(s), apply={apply} → {result.CreatedCount} text(s), {result.ModifiedCount} attribute/text edit(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static object StructuralGenerateMemberSchedule(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, IReadOnlyList<string> kinds, string? ruleSet, ScriptArgs prefixes, bool writeTable, ScriptArgs insertPoint, string? title, string? layer, double rowHeightMm, double columnWidthMm, double textHeightMm, string? space)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<ScheduleRow>();
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, ScriptArgs.Empty, result);
        var wanted = StructuralService.ParseKinds(kinds, result);
        var prefixMap = StructuralService.ParsePrefixes(prefixes);
        var read = StructuralService.Read(db, ed, tr, units, ct, filter, rules, tol, wanted, 0, MaxCandidatesCeiling, prefixes: prefixMap);
        if (read.Classification.Query.Truncated) result.Warn("the scan was truncated at maxCandidates: the schedule misses the members beyond it.");
        var rows = MemberTagging.Schedule(read.Members);
        result.Items = rows;
        result.Count = rows.Count;
        result.Summary = new
        {
            members = read.Members.Count,
            byKind = read.Members.GroupBy(m => m.Kind).OrderBy(g => MemberKind.Rank(g.Key)).ToDictionary(g => g.Key, g => g.Count()),
            totalLengthMm = Math.Round(rows.Sum(r => r.TotalLengthMm), 1),
            unmarked = read.Members.Count(m => m.Mark is null),
            duplicateExisting = MemberTagging.DuplicateExisting(read.Members),
            ruleSet = new { name = rules.Name, source = rules.Source },
        };
        if (!writeTable)
        {
            log($"structural_generate_member_schedule: {read.Members.Count} member(s), {rows.Count} row(s), {watch.ElapsedMilliseconds} ms");
            return result;
        }

        var cx = new EditContext(db, tr, units);
        var at = cx.Point(insertPoint, out var pointError, "insertPoint") ?? throw new ArgumentException(pointError!.Message + " (writeTable needs insertPoint {x, y} in mm).");
        var edit = StructuralWriteService.WriteTable(cx, rows, at, string.IsNullOrWhiteSpace(title) ? "MEMBER SCHEDULE" : title!, layer,
            PositiveOrDefault(rowHeightMm, StructuralWriteService.DefaultTableRowHeightMm, "rowHeightMm"), PositiveOrDefault(columnWidthMm, StructuralWriteService.DefaultTableColumnWidthMm, "columnWidthMm"),
            PositiveOrDefault(textHeightMm, StructuralWriteService.DefaultTagHeightMm, "textHeightMm"), WriteSpace(space, filter));
        foreach (var w in result.Warnings) edit.Warn(w);
        edit.Summary = new { schedule = result.Summary, rows, table = edit.Summary };
        log($"structural_generate_member_schedule: {read.Members.Count} member(s), {rows.Count} row(s), table {edit.AffectedHandles.FirstOrDefault()}, {watch.ElapsedMilliseconds} ms");
        return edit;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Marks and tables go where the members were read from unless the caller names a space; members read from every space need one.</summary>
    private static string WriteSpace(string? space, EntityFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(space)) return space.Trim();
        if (filter.Space.Equals("all", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("the entities were read from every space (filter.space all / handles): pass space (model or a layout name) to say where the new entities go.");
        return filter.Space;
    }

    /// <summary>0 / absent = the default; negative is the caller's mistake, not a silent default.</summary>
    private static double PositiveOrDefault(double value, double fallback, string name) =>
        value < 0 ? throw new ArgumentException($"{name} must be > 0 (0 = the default {fallback}).") : value == 0 ? fallback : value;

    private static (EntityFilter Filter, ClassificationRuleSet Rules, GeometryTolerance Tol) StructuralInputs<T>(ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, AnalysisResult<T> result)
    {
        var filter = EntityFilter.From(filterArgs, out var unknownFilter);
        foreach (var key in unknownFilter) result.Warn($"filter.{key} is not a known filter key (known: {string.Join(", ", EntityFilter.KnownKeys)}).");
        var tol = GeometryTolerance.From(toleranceArgs, out var unknownTolerance);
        foreach (var key in unknownTolerance) result.Warn($"tolerance.{key} is not a known tolerance or not positive.");
        return (filter, ClassificationRuleSet.Load(ruleSet), tol);
    }

    private static AnalysisResult<AuditIssue> StructuralCheck(string tool, Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, int limit, int offset, int maxCandidates,
        Func<StructuralService.Outcome, GeometryTolerance, AnalysisResult<AuditIssue>, IReadOnlyList<AuditIssue>> check, Func<StructuralService.Outcome, object> describe)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AuditIssue>();
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, toleranceArgs, result);
        (limit, offset) = PageBounds(limit, offset, MaxIssueLimit, result);
        var read = StructuralService.Read(db, ed, tr, units, ct, filter, rules, tol, null, 0, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling));
        result.Errors.AddRange(read.Classification.Query.Errors);
        result.Warnings.AddRange(read.Classification.Query.Warnings);
        var issues = check(read, tol, result);
        result.Items = issues.Skip(offset).Take(limit).ToArray();
        result.Count = issues.Count;
        result.Offset = offset;
        result.Truncated = issues.Count > offset + result.Items.Count || read.Classification.Query.Truncated;
        result.Summary = new { examined = read.Classification.Query.Records.Count, members = read.Members.Count, scope = describe(read), issues = issues.Count, bySeverity = BySeverity(issues), byType = issues.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()), tolerance = tol };
        log($"{tool}: {read.Members.Count} member(s), {issues.Count} issue(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }
}
