using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Architecture;
using HPAutoCad.Aec.Cad;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec;

/// <summary>The architecture tools: rooms from walls, their boundary problems, tags, the area schedule and rule-driven dimensions.</summary>
public static partial class AecTools
{
    /// <summary>A room with its outline is ~1.5 KB at <see cref="MaxOutlineVertices"/>: this many per page stays under the 64 KB cap.</summary>
    public const int MaxRoomLimit = 30;

    /// <summary>Outline vertices listed per room; <c>outlineTruncated</c> says when a room has more.</summary>
    public const int MaxOutlineVertices = 32;

    /// <summary>Rooms tagged per call — each is echoed twice in the answer.</summary>
    public const int MaxRoomTags = 120;

    /// <summary>Area rows per page: a row lists up to 20 room labels, so 50 rows of long Vietnamese names stay under the cap.</summary>
    public const int MaxAreaRowLimit = 50;

    public static readonly IReadOnlyList<string> DimensionSubjects = ["rooms", "entities"];

    public static AnalysisResult<Dictionary<string, object?>> ArchDetectRooms(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs labels, ScriptArgs detection, bool includeOutline, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<Dictionary<string, object?>>();
        (limit, offset) = PageBounds(limit, offset, MaxRoomLimit, result);
        var read = ReadRooms(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, labels, detection, maxCandidates, result, out var settings);
        result.Items = read.Rooms.Skip(offset).Take(limit).Select(r => r.Describe(includeOutline ? MaxOutlineVertices : 0)).ToArray();
        result.Count = read.Rooms.Count;
        result.Offset = offset;
        result.Truncated = read.Rooms.Count > offset + result.Items.Count || read.Classification.Query.Truncated;
        if (read.Walls == 0 && read.Outlines == 0) result.Warn("No walls or room outlines found: wall and room layers are named by the classification rule set (A-WALL, WALL, TUONG…, A-ROOM, PHONG…); pass ruleSet or a filter.");
        result.Summary = RoomSummary(read, settings);
        log($"arch_detect_rooms: {read.Walls} wall(s), {read.Loops.Segments} segment(s), {read.Rooms.Count} room(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<AuditIssue> ArchRoomBoundaryCheck(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs labels, ScriptArgs detection, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AuditIssue>();
        (limit, offset) = PageBounds(limit, offset, MaxIssueLimit, result);
        var read = ReadRooms(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, labels, detection, maxCandidates, result, out var settings);
        var tol = GeometryTolerance.From(toleranceArgs, out _);
        var issues = RoomBoundaryChecks.Check(read.Loops, read.Rooms, tol, settings.MaxGapMm, ct);
        result.Items = issues.Skip(offset).Take(limit).ToArray();
        result.Count = issues.Count;
        result.Offset = offset;
        result.Truncated = issues.Count > offset + result.Items.Count || read.Classification.Query.Truncated;
        result.Summary = new
        {
            examined = read.Classification.Query.Records.Count, walls = read.Walls, outlines = read.Outlines, rooms = read.Rooms.Count,
            openEnds = read.Loops.OpenEnds.Count, closedGaps = read.Loops.ClosedGaps.Count, openings = read.Loops.Openings.Count, overshoots = read.Loops.Overshoots, issues = issues.Count, bySeverity = BySeverity(issues),
            byType = issues.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()), detection = settings, tolerance = tol,
        };
        log($"arch_room_boundary_check: {read.Rooms.Count} room(s), {issues.Count} issue(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult ArchCreateRoomTags(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs labels, ScriptArgs detection, IReadOnlyList<string> roomIds, bool onlyUnlabelled, string? format, string? layer, double textHeightMm, string? blockName, ScriptArgs attributes, string? space, bool apply, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var probe = new AnalysisResult<object>();
        textHeightMm = PositiveOrDefault(textHeightMm, RoomWriteService.DefaultTagHeightMm, "textHeightMm");
        var read = ReadRooms(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, labels, detection, maxCandidates, probe, out _);
        var rooms = RoomService.Select(read.Rooms, roomIds);
        if (onlyUnlabelled) rooms = rooms.Where(r => r.Name is null && r.Number is null).ToArray();
        if (rooms.Count == 0) throw new ArgumentException(read.Rooms.Count == 0 ? "no room found (arch_detect_rooms shows what the walls enclose)." : "every room is labelled already (onlyUnlabelled) — nothing to tag.");
        if (rooms.Count > MaxRoomTags) throw new ArgumentException($"{rooms.Count} rooms match; the maximum per call is {MaxRoomTags} — pass roomIds or a filter.");
        var filter = EntityFilter.From(filterArgs, out _);
        var result = RoomWriteService.WriteTags(new EditContext(db, tr, units), ct, rooms, format, layer, textHeightMm, WriteSpace(space, filter), blockName, attributes, dryDecisionsOnly: !apply);
        foreach (var w in probe.Warnings) result.Warn(w);
        log($"arch_create_room_tags: {rooms.Count} room(s), apply={apply} → {result.CreatedCount} tag(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static AnalysisResult<AreaRow> ArchGenerateAreaSchedule(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs labels, ScriptArgs detection, IReadOnlyList<string> roomIds, string? groupBy, int limit, int offset, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var result = new AnalysisResult<AreaRow>();
        (limit, offset) = PageBounds(limit, offset, MaxAreaRowLimit, result);
        var read = ReadRooms(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, labels, detection, maxCandidates, result, out _);
        var rooms = RoomService.Select(read.Rooms, roomIds);
        var rows = AreaSchedule.Build(rooms, groupBy ?? "room");
        result.Items = rows.Skip(offset).Take(limit).ToArray();
        result.Count = rows.Count;
        result.Offset = offset;
        result.Truncated = rows.Count > offset + result.Items.Count || read.Classification.Query.Truncated;
        result.Summary = new
        {
            rooms = rooms.Count, groups = rows.Count, groupBy = (groupBy ?? "room").ToLowerInvariant(), totalAreaMm2 = Math.Round(rooms.Sum(r => r.AreaMm2), 1), totalAreaM2 = Math.Round(rooms.Sum(r => r.AreaMm2) / 1_000_000, 2),
            unlabelled = rooms.Count(r => r.Name is null && r.Number is null), byDepartment = rooms.Count(r => r.Department is not null), zones = read.Zones, ruleSet = new { name = read.Classification.Rules.Name, source = read.Classification.Rules.Source },
        };
        log($"arch_generate_area_schedule: {rooms.Count} room(s), {rows.Count} row(s), {watch.ElapsedMilliseconds} ms");
        return result;
    }

    public static EditResult ArchAutoDimensionPlan(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, Action<string> log,
        ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs detection, string? subject, IReadOnlyList<string> roomIds, IReadOnlyList<ScriptArgs> rules, string? layer, string? dimStyle, string? space, bool apply, int maxCandidates)
    {
        var watch = Stopwatch.StartNew();
        var probe = new AnalysisResult<object>();
        var which = (subject ?? "rooms").Trim().ToLowerInvariant();
        if (!DimensionSubjects.Contains(which)) throw new ArgumentException($"subject must be one of {string.Join(", ", DimensionSubjects)}.");
        var filter = EntityFilter.From(filterArgs, out _);
        var tol = GeometryTolerance.From(toleranceArgs, out _);
        List<DimensionSubject> subjects;
        if (which == "rooms")
        {
            var read = ReadRooms(db, ed, tr, units, ct, filterArgs, ruleSet, toleranceArgs, ScriptArgs.Empty, detection, maxCandidates, probe, out _);
            subjects = RoomService.Select(read.Rooms, roomIds).Select(r => new DimensionSubject(r.Id, r.Outline, r.Handles)).ToList();
            if (subjects.Count == 0) throw new ArgumentException("no room found to dimension (arch_detect_rooms shows what the walls enclose).");
        }
        else
        {
            if (filter.IsEmpty) throw new ArgumentException("subject entities needs a filter (handles, layers, types…) naming the closed outlines to dimension.");
            var query = EntityQueryService.Query(db, ed, tr, units, ct, filter, tol, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling), 0, false, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling));
            if (query.Errors.Count > 0) throw new ArgumentException($"filter: {string.Join("; ", query.Errors.Select(e => e.Message))}");
            subjects = query.Records.Where(r => r.Shape is { Closed: true }).Select(r => new DimensionSubject(r.Handle, r.Shape!, [r.Handle])).ToList();
            if (subjects.Count == 0) throw new ArgumentException($"no closed outline among the {query.Records.Count} entities the filter matched.");
        }

        var plans = AutoDimensionRules.Plan(rules, subjects, tol, ct);
        var result = RoomWriteService.WriteDimensions(new EditContext(db, tr, units), ct, plans, layer, dimStyle, WriteSpace(space, filter), dryDecisionsOnly: !apply);
        foreach (var w in probe.Warnings) result.Warn(w);
        log($"arch_auto_dimension_plan: {subjects.Count} subject(s), {plans.Count} dimension(s), apply={apply}, {watch.ElapsedMilliseconds} ms");
        return result;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>The <c>detection</c> block every architecture tool shares, so the room ids one tool lists are the ids the next one selects: sizes of gaps, openings, faces.</summary>
    public static readonly IReadOnlyList<string> DetectionKeys = ["maxGapMm", "minOpeningMm", "maxOpeningMm", "minAreaMm2", "minWidthMm", "maxWallThicknessMm"];

    private static LoopSettings Settings(ScriptArgs detection, GeometryTolerance tol)
    {
        foreach (var key in detection.Keys)
            if (!DetectionKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException($"detection.{key} is not a detection setting (known: {string.Join(", ", DetectionKeys)}).");
        var d = LoopSettings.Default(tol);
        var settings = d with
        {
            MaxGapMm = PositiveOrDefault(detection.Double("maxGapMm", 0), d.MaxGapMm, "detection.maxGapMm"),
            MinOpeningMm = PositiveOrDefault(detection.Double("minOpeningMm", 0), d.MinOpeningMm, "detection.minOpeningMm"),
            MaxOpeningMm = PositiveOrDefault(detection.Double("maxOpeningMm", 0), d.MaxOpeningMm, "detection.maxOpeningMm"),
            MinAreaMm2 = PositiveOrDefault(detection.Double("minAreaMm2", 0), d.MinAreaMm2, "detection.minAreaMm2"),
            MinWidthMm = PositiveOrDefault(detection.Double("minWidthMm", 0), d.MinWidthMm, "detection.minWidthMm"),
            MaxWallThicknessMm = PositiveOrDefault(detection.Double("maxWallThicknessMm", 0), d.MaxWallThicknessMm, "detection.maxWallThicknessMm"),
        };
        if (settings.MinOpeningMm > settings.MaxOpeningMm) throw new ArgumentException("detection.minOpeningMm must not exceed maxOpeningMm.");
        if (settings.MaxGapMm >= settings.MinOpeningMm) throw new ArgumentException("detection.maxGapMm must be below minOpeningMm (a facing pair farther than a gap is a doorway).");
        return settings;
    }

    private static RoomService.Outcome ReadRooms<T>(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, ScriptArgs filterArgs, string? ruleSet, ScriptArgs toleranceArgs, ScriptArgs labels,
        ScriptArgs detection, int maxCandidates, AnalysisResult<T> result, out LoopSettings settings)
    {
        var (filter, rules, tol) = StructuralInputs(filterArgs, ruleSet, toleranceArgs, result);
        settings = Settings(detection, tol);
        var read = RoomService.Read(db, ed, tr, units, ct, filter, rules, tol, RoomLabelRules.From(labels), settings, Math.Clamp(maxCandidates, 1, MaxCandidatesCeiling));
        result.Errors.AddRange(read.Classification.Query.Errors);
        result.Warnings.AddRange(read.Classification.Query.Warnings);
        if (read.TextsTruncated) result.Warn($"only the first {ClassificationService.MaxTextIndex} texts of the space were indexed for labels; rooms beyond them read as unlabelled.");
        if (read.Narrowed) result.Warn("no layers/types in the filter: the scan was narrowed to the rule set's wall and room layers so maxCandidates counts walls, not furniture.");
        if (read.Classification.Query.Truncated) result.Warn("the scan stopped at maxCandidates: walls beyond it are missing and the open ends reported may be artefacts — raise maxCandidates or filter the layers.");
        return read;
    }

    private static object RoomSummary(RoomService.Outcome read, LoopSettings settings) => new
    {
        examined = read.Classification.Query.Records.Count,
        walls = read.Walls,
        wallSegments = read.Loops.Segments,
        outlines = read.Outlines,
        zones = read.Zones,
        rooms = read.Rooms.Count,
        fromWalls = read.Rooms.Count(r => r.Source == Room.FromWalls),
        fromOutlines = read.Rooms.Count(r => r.Source == Room.FromOutline),
        loopsReplacedByOutlines = read.LoopsReplacedByOutlines,
        totalAreaM2 = Math.Round(read.Rooms.Sum(r => r.AreaMm2) / 1_000_000, 2),
        labelled = read.Rooms.Count(r => r.Name is not null || r.Number is not null),
        openEnds = read.Loops.OpenEnds.Count,
        closedGaps = read.Loops.ClosedGaps.Count,
        openings = read.Loops.Openings.Count,
        overshoots = read.Loops.Overshoots,
        cavities = read.Loops.Cavities,
        tinyFaces = read.Loops.TinyFaces,
        nested = read.Loops.Nested,
        detection = settings,
        ruleSet = new { name = read.Classification.Rules.Name, source = read.Classification.Rules.Source },
    };
}
