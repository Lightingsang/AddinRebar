using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     One <c>set</c> object of <c>update_entities_batch</c> in two phases: <see cref="Validate"/> lists every refusal the set
///     would cause on this entity without touching it (unknown keys, keys that do not apply to the type, missing layers/styles,
///     bad numbers), so an atomic batch can refuse before the first write; <see cref="Apply"/> then sets a validated <c>set</c>
///     and records each applied key in <c>changed</c> as it goes, so a failure midway is reported honestly.
/// </summary>
public sealed partial class EntityUpdater(EditContext cx)
{
    public static readonly IReadOnlyList<string> Keys =
        ["layer", "colorIndex", "color", "linetype", "lineweight", "visible", "text", "heightMm", "rotationDeg", "scale", "attributes", "textOverride", "style", "dimStyle", "geometry", "move", "rotate", "scaleBy"];

    public List<ToolError> Validate(Entity entity, ScriptArgs set, List<string> warnings)
    {
        var errors = new List<ToolError>();
        var h = entity.Handle.ToString();
        foreach (var key in set.Keys)
            if (!Keys.Contains(key, StringComparer.OrdinalIgnoreCase)) errors.Add(ToolError.Argument($"set.{key} is not a known key (known: {string.Join(", ", Keys)})."));
        cx.ValidateProperties(set, errors, warnings);

        if (set.Has("text") && entity is not (DBText or MText or Dimension)) errors.Add(Unsupported(entity, "text", "TEXT, MTEXT and dimensions"));
        if (set.Has("heightMm"))
        {
            if (entity is not (DBText or MText)) errors.Add(Unsupported(entity, "heightMm", "TEXT and MTEXT"));
            else if (set.Double("heightMm") <= 0) errors.Add(ToolError.Argument("heightMm must be > 0."));
        }

        if (set.Has("rotationDeg") && entity is not (DBText or MText or BlockReference)) errors.Add(Unsupported(entity, "rotationDeg", "TEXT, MTEXT and block references (use rotate {angleDeg, about} for anything else)"));
        if (set.Has("scale"))
        {
            if (entity is not BlockReference) errors.Add(Unsupported(entity, "scale", "block references (use scaleBy for anything else)"));
            else if (set.Double("scale") <= 0) errors.Add(ToolError.Argument("scale must be > 0."));
        }

        if (set.Has("textOverride") && entity is not Dimension) errors.Add(Unsupported(entity, "textOverride", "dimensions"));
        if (set.Has("style"))
        {
            if (entity is not (DBText or MText)) errors.Add(Unsupported(entity, "style", "TEXT and MTEXT"));
            else if (!((TextStyleTable)cx.Tr.GetObject(cx.Db.TextStyleTableId, OpenMode.ForRead)).Has(set.Str("style") ?? "")) errors.Add(ToolError.Argument($"text style '{set.Str("style")}' does not exist in this drawing."));
        }

        if (set.Has("dimStyle"))
        {
            if (entity is not Dimension) errors.Add(Unsupported(entity, "dimStyle", "dimensions"));
            else if (AnnotationService.DimStyle(cx, set.Str("dimStyle"), out var styleError).IsNull) errors.Add(styleError!);
        }

        if (set.Has("attributes"))
        {
            if (entity is BlockReference block) BlockService.ValidateAttributes(cx, block, set.Obj("attributes"), errors, warnings);
            else errors.Add(Unsupported(entity, "attributes", "block references"));
        }

        if (set.Has("geometry")) ValidateGeometry(entity, set.Obj("geometry"), errors);
        if (set.Has("move") && !set.Obj("move").Has("dx") && !set.Obj("move").Has("dy") && !set.Obj("move").Has("dz")) errors.Add(ToolError.Argument("move needs dx and/or dy (mm)."));
        if (set.Has("rotate"))
        {
            if (!set.Obj("rotate").Has("angleDeg")) errors.Add(ToolError.Argument("rotate needs angleDeg (and optionally about {x, y})."));
            ValidatePivot(entity, set.Obj("rotate"), "rotate", errors);
        }

        if (set.Has("scaleBy"))
        {
            if (set.Obj("scaleBy").Double("factor") <= 0) errors.Add(ToolError.Argument("scaleBy needs factor > 0 (and optionally about {x, y})."));
            ValidatePivot(entity, set.Obj("scaleBy"), "scaleBy", errors);
        }

        return errors.Select(e => e.Handle is null ? e with { Handle = h } : e).ToList();
    }

    /// <summary>Sets a validated <c>set</c>; an AutoCAD exception propagates, with <paramref name="changed"/> holding what was applied before it.</summary>
    public void Apply(Entity entity, ScriptArgs set, List<string> changed)
    {
        changed.AddRange(cx.ApplyProperties(entity, set));
        if (set.Has("text"))
        {
            switch (entity)
            {
                case DBText t: t.TextString = set.Str("text") ?? ""; break;
                case MText m: m.Contents = set.Str("text") ?? ""; break;
                case Dimension d: d.DimensionText = set.Str("text") ?? ""; break;
            }

            changed.Add("text");
        }

        if (set.Has("heightMm"))
        {
            if (entity is DBText t) t.Height = cx.ToDrawing(set.Double("heightMm"));
            else if (entity is MText m) m.TextHeight = cx.ToDrawing(set.Double("heightMm"));
            changed.Add("heightMm");
        }

        if (set.Has("rotationDeg"))
        {
            var radians = set.Double("rotationDeg") * Math.PI / 180;
            switch (entity)
            {
                case DBText t: t.Rotation = radians; break;
                case MText m: m.Rotation = radians; break;
                case BlockReference b: b.Rotation = radians; break;
            }

            changed.Add("rotationDeg");
        }

        if (set.Has("scale") && entity is BlockReference scaled)
        {
            scaled.ScaleFactors = new Scale3d(set.Double("scale"));
            changed.Add("scale");
        }

        if (set.Has("textOverride") && entity is Dimension dim)
        {
            dim.DimensionText = set.Str("textOverride") ?? ""; // "" restores the measured value, "<>" keeps it inside custom text
            changed.Add("textOverride");
        }

        if (set.Has("style"))
        {
            var styles = (TextStyleTable)cx.Tr.GetObject(cx.Db.TextStyleTableId, OpenMode.ForRead);
            if (entity is DBText t) t.TextStyleId = styles[set.Str("style") ?? ""];
            else if (entity is MText m) m.TextStyleId = styles[set.Str("style") ?? ""];
            changed.Add("style");
        }

        if (set.Has("dimStyle") && entity is Dimension styled)
        {
            styled.DimensionStyle = AnnotationService.DimStyle(cx, set.Str("dimStyle"), out _);
            changed.Add("dimStyle");
        }

        if (set.Has("attributes") && entity is BlockReference block)
            changed.AddRange(BlockService.WriteAttributes(cx, block, set.Obj("attributes")).Select(tag => "attributes." + tag));
        if (set.Has("geometry")) ApplyGeometry(entity, set.Obj("geometry"), changed);
        if (set.Has("move"))
        {
            var move = set.Obj("move");
            entity.TransformBy(Matrix3d.Displacement(new Vector3d(cx.ToDrawing(move.Double("dx")), cx.ToDrawing(move.Double("dy")), cx.ToDrawing(move.Double("dz")))));
            changed.Add("move");
        }

        if (set.Has("rotate"))
        {
            entity.TransformBy(Matrix3d.Rotation(set.Obj("rotate").Double("angleDeg") * Math.PI / 180, Vector3d.ZAxis, PivotOf(entity, set.Obj("rotate"))));
            changed.Add("rotate");
        }

        if (set.Has("scaleBy"))
        {
            entity.TransformBy(Matrix3d.Scaling(set.Obj("scaleBy").Double("factor"), PivotOf(entity, set.Obj("scaleBy"))));
            changed.Add("scaleBy");
        }
    }

    private static string Dxf(Entity e) => e.ObjectId.ObjectClass.DxfName ?? e.GetType().Name;

    private static ToolError Unsupported(Entity entity, string key, string appliesTo) =>
        ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, entity.Handle.ToString(), $"{key} applies to {appliesTo}; {entity.Handle} is a {Dxf(entity)}.");
}
