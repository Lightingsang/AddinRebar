using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     Builds one entity from one item of <c>create_entities_batch</c>: <c>type</c> picks the constructor, geometry
///     arrives in millimetres, common properties (layer, colour, linetype, lineweight) are applied the same way for every
///     type. Nothing is appended here — the caller owns the space and the transaction — but a block reference needs
///     its attributes added after it is in the database, so the outcome carries that step as <see cref="Built.AfterAppend"/>.
/// </summary>
public sealed class EntityFactory(EditContext cx)
{
    public static readonly IReadOnlyList<string> Types = ["line", "polyline", "circle", "arc", "point", "text", "mtext", "blockReference", "dimension"];

    /// <summary>The entity ready to append, or the error that stops this item; warnings are advisory (frozen layer…). <see cref="AfterAppend"/> runs once the entity is in the database and returns its own warnings.</summary>
    public sealed record Built(Entity? Entity, ToolError? Error, IReadOnlyList<string> Warnings, Func<Transaction, IReadOnlyList<string>>? AfterAppend = null);

    public Built Create(ScriptArgs item)
    {
        var warnings = new List<string>();
        var type = (item.Str("type") ?? "").Trim();
        Entity? entity;
        ToolError? error;
        switch (type.ToLowerInvariant())
        {
            case "line": entity = Line(item, out error); break;
            case "polyline": entity = Polyline(item, out error); break;
            case "circle": entity = Circle(item, out error); break;
            case "arc": entity = Arc(item, out error); break;
            case "point": entity = Point(item, out error); break;
            case "text": entity = Text(item, out error); break;
            case "mtext": entity = MText(item, out error); break;
            case "blockreference": return BuildBlockReference(item, warnings);
            case "dimension": entity = Dimension(item, out error); break;
            default:
                return new Built(null, ToolError.Argument($"type '{type}' is not one of {string.Join(", ", Types)}."), warnings);
        }

        if (entity is null) return new Built(null, error, warnings);
        return Finish(entity, item, warnings);
    }

    /// <summary>Layer (create rules) + common properties, validated before anything is set; the entity is disposed when a check fails so nothing leaks.</summary>
    internal Built Finish(Entity entity, ScriptArgs item, List<string> warnings, Func<Transaction, IReadOnlyList<string>>? afterAppend = null)
    {
        var layer = cx.LayerForCreate(item.Str("layer"), out var error, out var warning);
        var errors = new List<ToolError>();
        if (layer is null) errors.Add(error!);
        cx.ValidateProperties(item, errors, warnings, skipLayer: true);
        if (errors.Count > 0)
        {
            entity.Dispose();
            return new Built(null, errors[0], warnings);
        }

        entity.LayerId = layer!.ObjectId;
        if (warning is not null) warnings.Add(warning);
        cx.ApplyProperties(entity, item, skipLayer: true);
        return new Built(entity, null, warnings, afterAppend);
    }

    private Entity? Line(ScriptArgs item, out ToolError? error)
    {
        var a = cx.Point(item, "start", out error);
        var b = a is null ? null : cx.Point(item, "end", out error);
        return a is null || b is null ? null : new Line(a.Value, b.Value);
    }

    private Entity? Polyline(ScriptArgs item, out ToolError? error)
    {
        var points = item.List("points");
        if (points.Count < 2)
        {
            error = ToolError.Argument("polyline needs points: at least 2 entries of {x, y} in millimetres (optional bulge per point).");
            return null;
        }

        var pl = new Polyline(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            var p = cx.Point(points[i], out error, $"points[{i}]");
            if (p is null)
            {
                pl.Dispose();
                return null;
            }

            pl.AddVertexAt(i, new Point2d(p.Value.X, p.Value.Y), points[i].Double("bulge"), 0, 0);
        }

        pl.Closed = item.Bool("closed");
        error = null;
        return pl;
    }

    private Entity? Circle(ScriptArgs item, out ToolError? error)
    {
        var c = cx.Point(item, "center", out error);
        if (c is null) return null;
        var r = item.Double("radiusMm");
        if (r <= 0)
        {
            error = ToolError.Argument("circle needs radiusMm > 0.");
            return null;
        }

        return new Circle(c.Value, Vector3d.ZAxis, cx.ToDrawing(r));
    }

    private Entity? Arc(ScriptArgs item, out ToolError? error)
    {
        var c = cx.Point(item, "center", out error);
        if (c is null) return null;
        var r = item.Double("radiusMm");
        if (r <= 0 || !item.Has("startAngleDeg") || !item.Has("endAngleDeg"))
        {
            error = ToolError.Argument("arc needs center, radiusMm > 0, startAngleDeg and endAngleDeg (counter-clockwise, degrees).");
            return null;
        }

        return new Arc(c.Value, Vector3d.ZAxis, cx.ToDrawing(r), item.Double("startAngleDeg") * Math.PI / 180, item.Double("endAngleDeg") * Math.PI / 180);
    }

    private Entity? Point(ScriptArgs item, out ToolError? error)
    {
        var p = cx.Point(item, "position", out error);
        return p is null ? null : new DBPoint(p.Value);
    }

    private Entity? Text(ScriptArgs item, out ToolError? error)
    {
        var p = cx.Point(item, "position", out error);
        if (p is null) return null;
        var text = item.Str("text");
        var height = item.Double("heightMm");
        if (string.IsNullOrEmpty(text) || height <= 0)
        {
            error = ToolError.Argument("text needs text and heightMm > 0.");
            return null;
        }

        var entity = new DBText { TextString = text, Position = p.Value, Height = cx.ToDrawing(height), Rotation = item.Double("rotationDeg") * Math.PI / 180 };
        return ApplyTextStyle(entity, item, out error) ? entity : null;
    }

    private Entity? MText(ScriptArgs item, out ToolError? error)
    {
        var p = cx.Point(item, "position", out error);
        if (p is null) return null;
        var text = item.Str("text");
        var height = item.Double("heightMm");
        if (string.IsNullOrEmpty(text) || height <= 0)
        {
            error = ToolError.Argument("mtext needs text and heightMm > 0.");
            return null;
        }

        var entity = new MText { Contents = text, Location = p.Value, TextHeight = cx.ToDrawing(height), Rotation = item.Double("rotationDeg") * Math.PI / 180 };
        if (item.Double("widthMm") > 0) entity.Width = cx.ToDrawing(item.Double("widthMm"));
        return ApplyTextStyle(entity, item, out error) ? entity : null;
    }

    private bool ApplyTextStyle(Entity entity, ScriptArgs item, out ToolError? error)
    {
        error = null;
        if (!item.Has("style")) return true;
        var name = item.Str("style") ?? "";
        var styles = (TextStyleTable)cx.Tr.GetObject(cx.Db.TextStyleTableId, OpenMode.ForRead);
        if (!styles.Has(name))
        {
            error = ToolError.Argument($"text style '{name}' does not exist in this drawing.");
            entity.Dispose();
            return false;
        }

        if (entity is DBText t) t.TextStyleId = styles[name];
        else if (entity is MText m) m.TextStyleId = styles[name];
        return true;
    }

    /// <summary>Also used by manage_blocks_attributes insert: the same validation and attribute fill for a single reference.</summary>
    internal Built BuildBlockReference(ScriptArgs item, List<string> warnings)
    {
        var name = item.Str("blockName") ?? "";
        var table = (BlockTable)cx.Tr.GetObject(cx.Db.BlockTableId, OpenMode.ForRead);
        if (string.IsNullOrWhiteSpace(name) || !table.Has(name))
            return new Built(null, ToolError.Argument($"blockReference needs blockName of a defined block ('{name}' is not defined; manage_blocks_attributes listDefinitions shows them)."), warnings);
        var p = cx.Point(item, "position", out var error);
        if (p is null) return new Built(null, error, warnings);
        var scale = item.Double("scale", 1);
        if (scale <= 0) return new Built(null, ToolError.Argument("scale must be greater than 0."), warnings);
        var definitionId = table[name];
        var reference = new BlockReference(p.Value, definitionId) { ScaleFactors = new Scale3d(scale), Rotation = item.Double("rotationDeg") * Math.PI / 180 };
        var attributes = item.Obj("attributes");
        return Finish(reference, item, warnings, tr => BlockService.AppendAttributes(tr, reference, definitionId, attributes));
    }

    private Entity? Dimension(ScriptArgs item, out ToolError? error)
    {
        var p1 = cx.Point(item, "p1", out error);
        var p2 = p1 is null ? null : cx.Point(item, "p2", out error);
        var dimLine = p2 is null ? null : cx.Point(item, "dimLinePoint", out error);
        if (p1 is null || p2 is null || dimLine is null) return null;
        var styleId = AnnotationService.DimStyle(cx, item.Str("dimStyle"), out error);
        if (styleId.IsNull) return null;
        var kind = (item.Str("kind") ?? "linear").Trim().ToLowerInvariant();
        Dimension dimension = kind switch
        {
            "aligned" => new AlignedDimension(p1.Value, p2.Value, dimLine.Value, "", styleId),
            "linear" or "rotated" => new RotatedDimension(item.Double("rotationDeg") * Math.PI / 180, p1.Value, p2.Value, dimLine.Value, "", styleId),
            _ => null!,
        };
        if (dimension is null)
        {
            error = ToolError.Argument("dimension kind must be linear (rotated, default) or aligned here; radial, diameter, angular and mleader are created by manage_annotations.");
            return null;
        }

        if (item.Has("textOverride")) dimension.DimensionText = item.Str("textOverride") ?? "";
        return dimension;
    }
}
