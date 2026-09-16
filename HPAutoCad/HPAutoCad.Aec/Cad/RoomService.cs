using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPAutoCad.Aec.Architecture;
using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The read half of the architecture tools: classify the architectural discipline once (the query narrowed to the rule set's
///     wall and room layers when the caller gave none, so the candidate cap counts walls, not furniture), turn every wall into
///     segments and every explicit closed outline on a room layer into a room, run the loop finder over the walls, and label each
///     room from the texts inside it. An explicit outline wins over the wall loop it duplicates; an outline that holds other
///     outlines is a zone, not a room.
/// </summary>
public static class RoomService
{
    public sealed record Outcome(IReadOnlyList<Room> Rooms, LoopOutcome Loops, ClassificationService.Outcome Classification, int Walls, int Outlines, int Zones, int LoopsReplacedByOutlines, bool TextsTruncated, bool Narrowed);

    /// <summary>A wall loop whose inside point lies in an explicit outline and whose area is at least this share of it is that outline drawn as walls (a zone outline is far larger).</summary>
    public const double OutlineAreaMatch = 0.60;

    public static Outcome Read(Database db, Editor ed, Transaction tr, ScriptUnits units, CancellationToken ct, EntityFilter filter, ClassificationRuleSet rules, GeometryTolerance tol,
        RoomLabelRules labelRules, LoopSettings settings, int maxCandidates)
    {
        if (filter.Space.Equals("all", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("rooms are found per space: pass filter.space model or a layout name (handles pick every space).");
        var narrowed = Narrow(filter, rules);
        var texts = ClassificationService.BuildTextIndex(db, ed, tr, units, ct, tol, filter.Space);
        var classification = ClassificationService.Classify(db, ed, tr, units, ct, narrowed ?? filter, rules, tol, new HashSet<string> { Discipline.Architecture }, 0, includeUnknown: false, maxCandidates, texts);
        var walls = new List<WallSegment>();
        var outlines = new List<AecObject>();
        var wallCount = 0;
        foreach (var o in classification.Objects)
        {
            ct.ThrowIfCancellationRequested();
            if (o.Shape is null) continue;
            if (o.AecType == AecType.ArchitecturalWall) { wallCount++; walls.AddRange(o.Shape.Segments.Select(s => new WallSegment(s, o.Handle))); }
            else if (o.AecType == AecType.Room && o.Shape.Closed && o.Shape.Vertices.Count >= 3 && Math.Abs(o.Shape.AreaMm2) >= settings.MinAreaMm2) outlines.Add(o);
        }

        var loops = RoomLoopFinder.Find(walls, tol, settings, ct);
        var rooms = new List<Room>();
        var zones = 0;
        var candidates = outlines.OrderBy(o => o.Handle.Length).ThenBy(o => o.Handle, StringComparer.Ordinal).Select(o => (o, inside: Room.InsidePoint(o.Shape!, tol))).ToList();
        foreach (var (o, _) in candidates)
        {
            var shape = o.Shape!;
            // a zone outline holds other outlines' inside points: scheduled as its parts, not as a room of its own
            if (candidates.Any(c => !ReferenceEquals(c.o, o) && shape.ContainsPointXY(c.inside, tol.PointEquality) && shape.DistanceToBoundaryXY(c.inside) > tol.PointEquality && Math.Abs(c.o.Shape!.AreaMm2) < Math.Abs(shape.AreaMm2))) { zones++; continue; }
            rooms.Add(new Room($"R-{rooms.Count + 1:000}", Room.FromOutline, [o.Handle], shape, Math.Abs(shape.AreaMm2), shape.LengthMm, shape.Centroid, Room.InsidePoint(shape, tol)));
        }

        var replaced = 0;
        foreach (var loop in loops.Loops)
        {
            var outline = new PlanShape(loop.Ring, true, loop.PerimeterMm, loop.AreaMm2);
            var inside = Room.InsidePoint(outline, tol);
            if (rooms.Any(r => r.Source == Room.FromOutline && r.Outline.ContainsPointXY(inside, tol.PointEquality) && loop.AreaMm2 >= OutlineAreaMatch * r.AreaMm2)) { replaced++; continue; }
            rooms.Add(new Room($"R-{rooms.Count + 1:000}", Room.FromWalls, loop.Handles, outline, loop.AreaMm2, loop.PerimeterMm, outline.Centroid, inside));
        }

        var labelled = rooms.Select(r => RoomLabels.Apply(r, LabelsInside(texts.Index, r, tol), labelRules)).ToArray();
        return new Outcome(labelled, loops, classification, wallCount, outlines.Count, zones, replaced, texts.Truncated, narrowed is not null);
    }

    /// <summary>With no layer/type/block filter given, the query is limited to the rule set's wall and room layers and types so the candidate cap is spent on walls.</summary>
    private static EntityFilter? Narrow(EntityFilter filter, ClassificationRuleSet rules)
    {
        if (filter.Layers.Count > 0 || filter.Types.Count > 0 || filter.BlockNames.Count > 0 || filter.Handles.Count > 0) return null;
        var relevant = rules.Rules.Where(r => r.AecType is AecType.ArchitecturalWall or AecType.Room).ToList();
        var layers = relevant.SelectMany(r => r.Layers).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var types = relevant.SelectMany(r => r.Types).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return layers.Length == 0 ? null : new EntityFilter { Layers = layers, Types = types, Colors = filter.Colors, Linetypes = filter.Linetypes, TextContains = filter.TextContains, VisibleOnly = filter.VisibleOnly, Space = filter.Space };
    }

    private static IReadOnlyList<RoomLabels.Label> LabelsInside(AecClassifier.TextIndex texts, Room room, GeometryTolerance tol) => texts.Near(room.Outline.Bounds, 0)
        .Where(t => t.Text is { Length: > 0 } && (t.PositionMm ?? t.BoundsMm?.Center) is { } p && room.Outline.ContainsPointXY(p, tol.PointEquality) && room.Outline.DistanceToBoundaryXY(p) > tol.PointEquality)
        .Select(t => new RoomLabels.Label(t.Handle, t.Text!, t.PositionMm ?? t.BoundsMm!.Value.Center))
        .OrderBy(l => l.Handle.Length).ThenBy(l => l.Handle, StringComparer.Ordinal).ToArray();

    /// <summary>The rooms a tool argument names (ids, or wall/outline handles — a party wall selects both rooms); every id must resolve.</summary>
    public static IReadOnlyList<Room> Select(IReadOnlyList<Room> rooms, IReadOnlyList<string> roomIds)
    {
        if (roomIds.Count == 0) return rooms;
        var wanted = new List<Room>();
        foreach (var id in roomIds)
        {
            var key = id.Trim();
            var matches = rooms.Where(r => r.Id.Equals(key, StringComparison.OrdinalIgnoreCase) || r.Handles.Contains(key, StringComparer.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0) throw new ArgumentException($"roomIds: '{id}' is not a detected room (arch_detect_rooms lists ids R-001… and their handles).");
            foreach (var room in matches) if (!wanted.Contains(room)) wanted.Add(room);
        }

        return wanted;
    }
}
