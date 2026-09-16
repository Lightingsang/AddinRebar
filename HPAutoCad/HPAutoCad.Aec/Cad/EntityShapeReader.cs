using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Turns an open entity into an <see cref="AecEntityRecord"/> in millimetres: identity and style
///     verbatim, plan geometry as a <see cref="Shape"/> (lines and polylines exact, curves tessellated to
///     the chord tolerance, blocks/text/dimensions as their bounding rectangle), exact length/area from
///     AutoCAD where the entity is a curve. Never throws for one bad entity: the record carries a note.
/// </summary>
public sealed partial class EntityShapeReader
{
    private readonly Database _db;
    private readonly Transaction _tr;
    private readonly ScriptUnits _units;
    private readonly GeometryTolerance _tol;
    private readonly ObjectId _modelSpaceId;
    private readonly Dictionary<ObjectId, string> _spaceNames = new();
    private readonly Dictionary<string, (bool Off, bool Frozen)> _layerState = new(StringComparer.OrdinalIgnoreCase);

    public EntityShapeReader(Database db, Transaction tr, ScriptUnits units, GeometryTolerance tolerance)
    {
        _db = db;
        _tr = tr;
        _units = units;
        _tol = tolerance;
        _modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
    }

    public double Mm(double drawingUnits) => _units.ToMm(drawingUnits);

    public Pt ToPt(Point3d p) => new(Mm(p.X), Mm(p.Y), Mm(p.Z));

    public Pt ToPt(Point2d p, double z = 0) => new(Mm(p.X), Mm(p.Y), Mm(z));

    /// <summary>Which space the entity sits in: "Model" or the layout's name.</summary>
    public string SpaceOf(Entity entity)
    {
        var owner = entity.OwnerId;
        if (owner == _modelSpaceId) return "Model";
        if (_spaceNames.TryGetValue(owner, out var name)) return name;
        try
        {
            if (_tr.GetObject(owner, OpenMode.ForRead) is BlockTableRecord btr)
            {
                name = btr.IsLayout && _tr.GetObject(btr.LayoutId, OpenMode.ForRead) is Layout layout ? layout.LayoutName : btr.Name;
            }
        }
        catch (AcadException)
        {
            name = null;
        }

        return _spaceNames[owner] = name ?? "Model";
    }

    /// <summary>Visible = the entity's own flag and its layer neither off nor frozen.</summary>
    public bool IsVisible(Entity entity)
    {
        if (!entity.Visible) return false;
        if (!_layerState.TryGetValue(entity.Layer, out var state))
        {
            try
            {
                var record = (LayerTableRecord)_tr.GetObject(entity.LayerId, OpenMode.ForRead);
                state = (record.IsOff, record.IsFrozen);
            }
            catch (AcadException)
            {
                state = (false, false);
            }

            _layerState[entity.Layer] = state;
        }

        return !state.Off && !state.Frozen;
    }

    public AecEntityRecord Read(Entity entity, bool detail)
    {
        var id = entity.ObjectId;
        var type = id.ObjectClass.DxfName ?? id.ObjectClass.Name;
        Box? bounds = null;
        string? note = null;
        try
        {
            var extents = entity.GeometricExtents;
            bounds = Box.Of(ToPt(extents.MinPoint), ToPt(extents.MaxPoint));
        }
        catch (AcadException exception)
        {
            note = $"no finite extents ({exception.ErrorStatus})";
        }

        PlanShape? shape = null;
        string? text = null;
        string? blockName = null;
        Dictionary<string, string>? attributes = null;
        Pt? position = null;
        double? length = null;
        double? area = null;
        string? style = null;
        double? textHeight = null;

        try
        {
            switch (entity)
            {
                case Line line:
                    shape = PlanShape.Segment(ToPt(line.StartPoint), ToPt(line.EndPoint), Mm(line.Length));
                    break;
                case Polyline pl:
                    shape = ReadPolyline(pl);
                    break;
                case Circle circle:
                    // A circle is its own mirror image, so the OCS normal does not matter here.
                    shape = new PlanShape(PlanShape.ArcPoints(ToPt(circle.Center), Mm(circle.Radius), 0, 360, _tol.ChordError, Mm(circle.Center.Z)), true, Mm(circle.Circumference), Mm(circle.Radius) * Mm(circle.Radius) * Math.PI, approximate: true);
                    position = ToPt(circle.Center);
                    break;
                case Arc arc:
                    // Angles are in the arc's OCS: a mirrored arc (normal -Z, every mirrored door swing) would come out reflected if the
                    // start/end angles were used as if the normal were +Z. AutoCAD's own sample points are in WCS whatever the normal.
                    shape = SampleArc(arc);
                    position = ToPt(arc.Center);
                    break;
                case Solid solid:
                    shape = new PlanShape([ToPt(solid.GetPointAt(0)), ToPt(solid.GetPointAt(1)), ToPt(solid.GetPointAt(3)), ToPt(solid.GetPointAt(2))], true);
                    break;
                case Hatch hatch:
                    shape = ReadHatch(hatch, out area);
                    break;
                case BlockReference block:
                    blockName = BlockName(block);
                    attributes = ReadAttributes(block);
                    position = ToPt(block.Position);
                    // The extents of a reference include its attributes; the stand-in footprint is the symbol alone when there are any.
                    var symbol = attributes is null ? bounds : SymbolBounds(block) ?? bounds;
                    shape = symbol is { } bb ? PlanShape.Rectangle(bb) : null;
                    break;
                case DBText dbText:
                    text = dbText.TextString;
                    position = ToPt(dbText.Position);
                    shape = bounds is { } tb ? PlanShape.Rectangle(tb) : null;
                    style = StyleName(dbText.TextStyleId);
                    textHeight = Mm(dbText.Height);
                    break;
                case MText mText:
                    text = mText.Text;
                    position = ToPt(mText.Location);
                    shape = bounds is { } mb ? PlanShape.Rectangle(mb) : null;
                    style = StyleName(mText.TextStyleId);
                    textHeight = Mm(mText.TextHeight);
                    break;
                case Dimension dimension:
                    text = string.IsNullOrEmpty(dimension.DimensionText) ? Mm(dimension.Measurement).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : dimension.DimensionText;
                    position = ToPt(dimension.TextPosition);
                    shape = bounds is { } db2 ? PlanShape.Rectangle(db2) : null;
                    style = StyleName(dimension.DimensionStyle);
                    break;
                case Xline or Ray:
                    note = "infinite construction line — no bounded geometry";
                    break;
                case Curve curve:
                    shape = SampleCurve(curve);
                    break;
                default:
                    shape = bounds is { } ob ? PlanShape.Rectangle(ob) : null;
                    if (shape is null && note is null) note = $"{type} has no readable plan geometry";
                    break;
            }
        }
        catch (AcadException exception)
        {
            note = $"geometry could not be read ({exception.ErrorStatus})";
            shape = null;
        }

        if (shape is not null)
        {
            length ??= shape.ExactLengthMm ?? (shape.Vertices.Count > 1 ? shape.LengthMm : null);
            if (shape.Closed) area ??= shape.AreaMm2;
        }

        var record = new AecEntityRecord
        {
            Handle = entity.Handle.ToString(),
            Type = type,
            Layer = entity.Layer,
            Color = ColorOf(entity),
            Linetype = entity.Linetype,
            Lineweight = entity.LineWeight.ToString(),
            Space = SpaceOf(entity),
            Visible = IsVisible(entity),
            BoundsMm = bounds?.Rounded(),
            LengthMm = length is null ? null : Math.Round(length.Value, 1),
            AreaMm2 = area is null ? null : Math.Round(area.Value, 1),
            Text = text,
            Style = style,
            TextHeightMm = textHeight is null ? null : Math.Round(textHeight.Value, 2),
            BlockName = blockName,
            Attributes = attributes,
            PositionMm = position?.Rounded(),
            Shape = shape,
            GeometryNote = shape is null ? note ?? "no plan geometry" : null,
        };
        if (detail) record.Geometry = record.ToGeometry();
        return record;
    }

    private PlanShape ReadPolyline(Polyline pl)
    {
        var points = new List<Pt>(pl.NumberOfVertices + 8);
        var segments = pl.Closed ? pl.NumberOfVertices : pl.NumberOfVertices - 1;
        var hasArcs = false;
        for (var i = 0; i < pl.NumberOfVertices; i++)
        {
            var p = pl.GetPoint3dAt(i);
            if (i < segments && Math.Abs(pl.GetBulgeAt(i)) > 1e-9)
            {
                // Arc segment: sample the exact CircularArc3d AutoCAD builds from the bulge (WCS points, so the OCS normal is
                // irrelevant); the segment's end vertex is added by the next iteration or by the ring closure.
                hasArcs = true;
                var arc = pl.GetArcSegmentAt(i);
                var sweep = Math.Abs(arc.EndAngle - arc.StartAngle);
                var samples = arc.GetSamplePoints(ArcSteps(Mm(arc.Radius), sweep) + 1);
                for (var k = 0; k < samples.Length - 1; k++) points.Add(ToPt(samples[k].Point));
            }
            else
            {
                points.Add(ToPt(p));
            }
        }

        // A ring never repeats its first vertex: exported polylines often carry both Closed = true and a duplicate closing vertex.
        var closed = pl.Closed || (pl.NumberOfVertices >= 3 && points[0].AlmostEqualsXY(points[^1], _tol.PointEquality));
        if (closed && points.Count >= 4 && points[0].AlmostEqualsXY(points[^1], _tol.PointEquality)) points.RemoveAt(points.Count - 1);
        double? area = null;
        if (closed)
        {
            try { area = Mm(pl.Area) * _units.MmPerUnit; } catch (AcadException) { area = null; }
        }

        return new PlanShape(points, closed, Mm(pl.Length), area, approximate: hasArcs);
    }

    private PlanShape? ReadHatch(Hatch hatch, out double? area)
    {
        area = null;
        try { area = Mm(hatch.Area) * _units.MmPerUnit; } catch (AcadException) { area = null; }

        // The footprint is the outermost polyline loop (the first one when AutoCAD did not flag any); it is rebuilt as an in-memory
        // Polyline so bulges get the same arc sampling as a drawn polyline. Curve loops (circles, splines) fall back to the extents.
        HatchLoop? footprint = null;
        for (var i = 0; i < hatch.NumberOfLoops; i++)
        {
            var loop = hatch.GetLoopAt(i);
            if (!loop.IsPolyline || loop.Polyline.Count < 3) continue;
            if (footprint is null || (loop.LoopType & HatchLoopTypes.Outermost) != 0) footprint = loop;
            if ((loop.LoopType & HatchLoopTypes.Outermost) != 0) break;
        }

        if (footprint is not null)
        {
            using var pl = new Polyline(footprint.Polyline.Count);
            for (var i = 0; i < footprint.Polyline.Count; i++)
            {
                var vertex = footprint.Polyline[i];
                pl.AddVertexAt(i, vertex.Vertex, vertex.Bulge, 0, 0);
            }

            pl.Closed = true;
            pl.Elevation = hatch.Elevation;
            var shape = ReadPolyline(pl);
            return new PlanShape(shape.Vertices, true, null, area, shape.Approximate);
        }

        Box? bounds = null;
        try
        {
            var e = hatch.GeometricExtents;
            bounds = Box.Of(ToPt(e.MinPoint), ToPt(e.MaxPoint));
        }
        catch (AcadException)
        {
            return null;
        }

        return PlanShape.Rectangle(bounds.Value);
    }
}
