using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPGeo.Core.Conversion;
using HPGeo.Core.Geometry;
using HPGeo.Core.Model;

namespace HPGeo.AutoCad.Cad;

/// <summary>What a read of the drawing produced: survey points, boundaries, and what was skipped and why.</summary>
internal sealed record DrawingReadResult(
    IReadOnlyList<SurveyPoint> Points,
    IReadOnlyList<BoundaryPolyline> Boundaries,
    IReadOnlyDictionary<string, int> SkippedByType);

/// <summary>
/// Reads survey points (POINT) and boundaries (LWPOLYLINE) from a set of entities, in <b>WCS</b> drawing
/// units with X = Easting and Y = Northing: vertices through <c>GetPoint3dAt</c> and arcs through
/// <c>GetArcSegmentAt</c>, so a mirrored polyline (normal −Z) or an oblique OCS is never read reflected.
/// Arcs are sampled to a chord tolerance (<see cref="BulgeTessellator.ChordCount"/>), never a fixed count.
/// Everything else is counted and skipped — the tool converts what surveyors draw, it does not guess what
/// a circle or a block means. Read-only: the caller owns the transaction and nothing is opened for write.
/// </summary>
internal static class DrawingReader
{
    public static DrawingReadResult Read(Transaction tr, IEnumerable<ObjectId> ids, double chordToleranceDrawingUnits = BulgeTessellator.DefaultToleranceM)
    {
        var points = new List<SurveyPoint>();
        var boundaries = new List<BoundaryPolyline>();
        var skipped = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in ids)
        {
            if (id.IsNull || id.IsErased) continue;
            var entity = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
            switch (entity)
            {
                case DBPoint point:
                    var index = points.Count + 1;
                    points.Add(new SurveyPoint(index, index.ToString(), new PlanePoint(point.Position.X, point.Position.Y), Handle(point)));
                    break;
                case Polyline polyline:
                    boundaries.Add(ReadPolyline(polyline, boundaries.Count + 1, chordToleranceDrawingUnits));
                    break;
                case null:
                    Count(skipped, "non-entity");
                    break;
                default:
                    Count(skipped, entity.GetType().Name);
                    break;
            }
        }
        return new DrawingReadResult(points, boundaries, skipped);
    }

    /// <summary>Every entity of model space (the script command's default scope).</summary>
    public static IEnumerable<ObjectId> ModelSpaceIds(Transaction tr, Database db, Func<Entity, bool>? filter = null)
    {
        var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        foreach (ObjectId id in modelSpace)
        {
            if (filter is null)
            {
                yield return id;
                continue;
            }
            if (tr.GetObject(id, OpenMode.ForRead, false) is Entity entity && filter(entity)) yield return id;
        }
    }

    private static BoundaryPolyline ReadPolyline(Polyline polyline, int ordinal, double tolerance)
    {
        var n = polyline.NumberOfVertices;
        var closed = polyline.Closed;
        // A polyline whose last vertex repeats the first is closed in practice; treat it so and drop the duplicate,
        // otherwise the KML ring would carry the vertex three times.
        var last = n - 1;
        if (!closed && n >= 4 && SamePoint(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(last)))
        {
            last--;
            closed = true;
        }
        var count = last + 1;
        var segmentCount = closed ? count : count - 1;
        var flattened = new List<PlanePoint>();
        for (var i = 0; i < segmentCount; i++)
        {
            var start = polyline.GetPoint3dAt(i);
            flattened.Add(new PlanePoint(start.X, start.Y));
            var next = (i + 1) % count;
            if (Math.Abs(polyline.GetBulgeAt(i)) < 1e-12) continue;
            if (next == 0 && !closed) continue;
            AppendArcInterior(flattened, polyline, i, tolerance);
        }
        if (!closed)
        {
            var end = polyline.GetPoint3dAt(last);
            flattened.Add(new PlanePoint(end.X, end.Y));
        }
        var name = closed ? $"Boundary {ordinal}" : $"Line {ordinal}";
        return new BoundaryPolyline(name, flattened, closed, Handle(polyline));
    }

    /// <summary>Interior chord points of segment <paramref name="i"/> in WCS, sampled to the tolerance.</summary>
    private static void AppendArcInterior(List<PlanePoint> into, Polyline polyline, int i, double tolerance)
    {
        CircularArc3d arc;
        try
        {
            arc = polyline.GetArcSegmentAt(i);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return; // a degenerate segment (zero chord): the straight chord is all there is
        }
        var included = Math.Abs(arc.EndAngle - arc.StartAngle);
        var segments = BulgeTessellator.ChordCount(arc.Radius, included, tolerance);
        var samples = arc.GetSamplePoints(segments + 1); // includes both ends
        for (var k = 1; k < samples.Length - 1; k++)
            into.Add(new PlanePoint(samples[k].Point.X, samples[k].Point.Y));
    }

    private static bool SamePoint(Point3d a, Point3d b) => a.DistanceTo(b) < 1e-6;

    private static string Handle(DBObject o) => o.Handle.ToString();

    private static void Count(Dictionary<string, int> skipped, string key) =>
        skipped[key] = skipped.TryGetValue(key, out var n) ? n + 1 : 1;
}
