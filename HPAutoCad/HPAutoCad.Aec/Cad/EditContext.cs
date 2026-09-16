using System.Globalization;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Model;
using HPRebar.McpBridge.Core.Scripting;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     What every write service needs: the bridge's transaction, the unit converter, and the checks a write must pass
///     before it touches the drawing — the target layer exists and is not locked/frozen, the space exists, points are
///     {x, y} in millimetres. Every check answers with a <see cref="ToolError"/> instead of throwing, so a batch can
///     report per item.
/// </summary>
public sealed class EditContext(Database db, Transaction tr, ScriptUnits units)
{
    public Database Db { get; } = db;

    public Transaction Tr { get; } = tr;

    public ScriptUnits Units { get; } = units;

    public double ToDrawing(double mm) => Units.ToDrawing(mm);

    public double ToMm(double drawing) => Units.ToMm(drawing);

    /// <summary>{x, y[, z]} in millimetres → a drawing point; null with an error when the object is not a point.</summary>
    public Point3d? Point(ScriptArgs parent, string key, out ToolError? error)
    {
        var p = parent.Obj(key);
        if (p.IsEmpty || !p.Has("x") || !p.Has("y"))
        {
            error = ToolError.Argument($"{key} must be an object {{x, y}} in millimetres.");
            return null;
        }

        error = null;
        return new Point3d(ToDrawing(p.Double("x")), ToDrawing(p.Double("y")), ToDrawing(p.Double("z")));
    }

    public Point3d? Point(ScriptArgs point, out ToolError? error, string label = "point")
    {
        if (point.IsEmpty || !point.Has("x") || !point.Has("y"))
        {
            error = ToolError.Argument($"{label} must be an object {{x, y}} in millimetres.");
            return null;
        }

        error = null;
        return new Point3d(ToDrawing(point.Double("x")), ToDrawing(point.Double("y")), ToDrawing(point.Double("z")));
    }

    /// <summary>The layer a new entity may go on: it must exist and not be locked; a frozen layer is allowed but noted (the entity is invisible).</summary>
    public LayerTableRecord? LayerForCreate(string? name, out ToolError? error, out string? warning)
    {
        warning = null;
        var record = FindLayer(name, out error);
        if (record is null) return null;
        if (record.IsLocked)
        {
            error = new ToolError(ToolErrorCode.LayerLocked, $"Layer '{record.Name}' is locked; unlock it or choose another layer.");
            return null;
        }

        if (record.IsFrozen) warning = $"layer '{record.Name}' is frozen: the entity is created but not visible until the layer is thawed.";
        return record;
    }

    /// <summary>An existing entity may be modified only when its layer is neither locked nor frozen.</summary>
    public bool LayerAllowsEdit(Entity entity, out ToolError? error)
    {
        var handle = entity.Handle.ToString();
        var layer = (LayerTableRecord)Tr.GetObject(entity.LayerId, OpenMode.ForRead);
        if (layer.IsLocked)
        {
            error = ToolError.ForHandle(ToolErrorCode.LayerLocked, handle, $"Entity {handle} is on locked layer '{layer.Name}'.");
            return false;
        }

        if (layer.IsFrozen)
        {
            error = ToolError.ForHandle(ToolErrorCode.LayerFrozen, handle, $"Entity {handle} is on frozen layer '{layer.Name}'.");
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    ///     Phase 1 of every edit: resolves the handle and opens the entity for read after its layer passes and it lives in a layout
    ///     (model/paper space) — an entity inside a block definition is refused, since editing it would change every reference.
    ///     Nothing is upgraded here, so a refused batch leaves the bridge's change counter at zero.
    /// </summary>
    public Entity? OpenForEdit(string? handle, out ToolError? error)
    {
        var entity = HandleResolver.OpenEntity(Db, Tr, handle, out error);
        if (entity is null || !LayerAllowsEdit(entity, out error)) return null;
        var owner = Tr.GetObject(entity.OwnerId, OpenMode.ForRead) as BlockTableRecord;
        if (owner is not null && !owner.IsLayout)
        {
            error = ToolError.ForHandle(ToolErrorCode.UnsupportedEntity, entity.Handle.ToString(), $"Entity {entity.Handle} lives inside block definition '{owner.Name}'; editing it would change every reference — edit the definition explicitly.");
            return null;
        }

        return entity;
    }

    /// <summary>Phase 2: upgrades a read-open entity to write; never throws.</summary>
    public bool Upgrade(Entity entity, out ToolError? error)
    {
        error = null;
        try
        {
            if (!entity.IsWriteEnabled) entity.UpgradeOpen();
            return true;
        }
        catch (AcadException exception)
        {
            error = ToolError.ForHandle(ToolErrorCode.Internal, entity.Handle.ToString(), $"Entity {entity.Handle} cannot be opened for write: {exception.ErrorStatus}.");
            return false;
        }
    }

    /// <summary>Opens the entity for write in one step (single-item ops that validated everything else already); never throws.</summary>
    public Entity? OpenForWrite(string? handle, out ToolError? error)
    {
        var entity = OpenForEdit(handle, out error);
        return entity is not null && Upgrade(entity, out error) ? entity : null;
    }

    /// <summary>Normalised, de-duplicated handles in input order; duplicates are reported once so a batch never opens the same entity twice.</summary>
    public static IReadOnlyList<string> DistinctHandles(IEnumerable<string?> handles, List<string> warnings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var h in handles)
        {
            var key = HandleResolver.Normalize(h) ?? h ?? "";
            if (seen.Add(key)) result.Add(h ?? "");
            else warnings.Add($"handle {h} listed more than once; the first occurrence is used.");
        }

        return result;
    }

    /// <summary>The block table record new entities are appended to: <c>current</c> (default), <c>model</c>, or a layout name.</summary>
    public BlockTableRecord? Space(string? space, out ToolError? error)
    {
        error = null;
        var name = (space ?? "current").Trim();
        ObjectId id;
        switch (name.ToLowerInvariant())
        {
            case "":
            case "current":
                id = Db.CurrentSpaceId;
                break;
            case "model":
                id = SymbolUtilityServices.GetBlockModelSpaceId(Db);
                break;
            default:
                var layouts = (DBDictionary)Tr.GetObject(Db.LayoutDictionaryId, OpenMode.ForRead);
                id = ObjectId.Null;
                foreach (var entry in layouts)
                {
                    if (!string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)) continue;
                    id = ((Layout)Tr.GetObject(entry.Value, OpenMode.ForRead)).BlockTableRecordId;
                    break;
                }

                if (id.IsNull)
                {
                    error = ToolError.Argument($"space '{space}' is not a layout of this drawing (use current, model, or a layout name).");
                    return null;
                }

                break;
        }

        return (BlockTableRecord)Tr.GetObject(id, OpenMode.ForWrite);
    }

    /// <summary>
    ///     Validates the common entity properties an item may carry without touching the entity — <c>layer</c> (updates only: a create
    ///     resolves its layer first, see <see cref="LayerForCreate"/>), <c>colorIndex</c> (0 ByBlock, 1–255 ACI, 256 ByLayer), <c>color</c>
    ///     ("#RRGGBB", "ByLayer", "ByBlock"), <c>linetype</c> (must be loaded), <c>lineweight</c> (hundredths of mm as AutoCAD lists them: 0, 5,
    ///     9, 13, 15, 18, 20, 25, 30, 35, 40, 50, 53, 60, 70, 80, 90, 100, 106, 120, 140, 158, 200, 211; −1 ByLayer, −2 ByBlock), <c>visible</c>.
    ///     Every failure is an error, never a throw.
    /// </summary>
    public void ValidateProperties(ScriptArgs item, List<ToolError> errors, List<string> warnings, bool skipLayer = false)
    {
        if (item.Has("layer") && !skipLayer)
        {
            var layer = FindLayer(item.Str("layer"), out var error);
            if (layer is null) errors.Add(error!);
            else if (layer.IsLocked) warnings.Add($"layer '{layer.Name}' is locked: an entity moved there cannot be edited until the layer is unlocked.");
            else if (layer.IsFrozen) warnings.Add($"layer '{layer.Name}' is frozen: an entity moved there is not visible until the layer is thawed.");
        }

        if ((item.Has("colorIndex") || item.Has("color")) && ParseColor(item, out var colorError) is null) errors.Add(colorError!);
        if (item.Has("linetype") && !((LinetypeTable)Tr.GetObject(Db.LinetypeTableId, OpenMode.ForRead)).Has(item.Str("linetype") ?? ""))
            errors.Add(ToolError.Argument($"linetype '{item.Str("linetype")}' is not loaded in this drawing."));
        if (item.Has("lineweight") && !Enum.IsDefined(typeof(LineWeight), item.Int("lineweight", int.MinValue)))
            errors.Add(ToolError.Argument($"lineweight {item.Str("lineweight")} is not an AutoCAD lineweight (hundredths of mm from the standard list, −1 ByLayer, −2 ByBlock)."));
    }

    /// <summary>Sets the common properties <see cref="ValidateProperties"/> accepted; returns what was applied. Call after validation only.</summary>
    public IReadOnlyList<string> ApplyProperties(Entity entity, ScriptArgs item, bool skipLayer = false)
    {
        var applied = new List<string>();
        if (item.Has("layer") && !skipLayer && FindLayer(item.Str("layer"), out _) is { } layer)
        {
            entity.LayerId = layer.ObjectId;
            applied.Add("layer");
        }

        if ((item.Has("colorIndex") || item.Has("color")) && ParseColor(item, out _) is { } color)
        {
            entity.Color = color;
            applied.Add("color");
        }

        if (item.Has("linetype"))
        {
            entity.LinetypeId = ((LinetypeTable)Tr.GetObject(Db.LinetypeTableId, OpenMode.ForRead))[item.Str("linetype") ?? ""];
            applied.Add("linetype");
        }

        if (item.Has("lineweight"))
        {
            entity.LineWeight = (LineWeight)item.Int("lineweight");
            applied.Add("lineweight");
        }

        if (item.Has("visible"))
        {
            entity.Visible = item.Bool("visible", true);
            applied.Add("visible");
        }

        return applied;
    }

    /// <summary>The layer named, or the current layer when the name is empty; an error when it does not exist.</summary>
    public LayerTableRecord? FindLayer(string? name, out ToolError? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(name)) return (LayerTableRecord)Tr.GetObject(Db.Clayer, OpenMode.ForRead);
        var table = (LayerTable)Tr.GetObject(Db.LayerTableId, OpenMode.ForRead);
        if (!table.Has(name))
        {
            error = ToolError.Argument($"Layer '{name}' does not exist in this drawing; run create_layer first or choose an existing layer.");
            return null;
        }

        return (LayerTableRecord)Tr.GetObject(table[name], OpenMode.ForRead);
    }

    public static Color? ParseColor(ScriptArgs item, out ToolError? error)
    {
        error = null;
        if (item.Has("colorIndex"))
        {
            var index = item.Int("colorIndex", -1);
            if (index is < 0 or > 256)
            {
                error = ToolError.Argument("colorIndex must be 0 (ByBlock), 1–255 (ACI) or 256 (ByLayer).");
                return null;
            }

            return Color.FromColorIndex(ColorMethod.ByAci, (short)index);
        }

        var text = (item.Str("color") ?? "").Trim();
        switch (text.ToLowerInvariant())
        {
            case "bylayer": return Color.FromColorIndex(ColorMethod.ByLayer, 256);
            case "byblock": return Color.FromColorIndex(ColorMethod.ByBlock, 0);
        }

        if (text.Length == 7 && text[0] == '#' && int.TryParse(text[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return Color.FromRgb((byte)(rgb >> 16), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
        error = ToolError.Argument($"color '{text}' is not '#RRGGBB', 'ByLayer' or 'ByBlock' (or use colorIndex).");
        return null;
    }
}
