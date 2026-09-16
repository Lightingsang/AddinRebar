using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Cad;

/// <summary>Layers the write tools create on demand (markup, tags): one place for the "exists, locked, frozen, off, create" dance.</summary>
public sealed partial class EditContext
{
    /// <summary>The layer an annotation tool writes on: refused when locked or frozen, warned when off, created (with the colour) when missing. Phase 1 — nothing is created here.</summary>
    public ToolError? CheckAnnotationLayer(string layer, string role, out string? warning)
    {
        warning = null;
        if (RefuseSymbolName(layer, "layer") is { } badName) throw new ArgumentException(badName);
        if (FindLayer(layer, out _) is not { } existing) return null;
        if (existing.IsLocked) return new ToolError(ToolErrorCode.LayerLocked, $"{role} layer '{layer}' is locked; unlock it or pass another layer.");
        if (existing.IsFrozen) return new ToolError(ToolErrorCode.LayerFrozen, $"{role} layer '{layer}' is frozen; thaw it or pass another layer.");
        if (existing.IsOff) warning = $"{role} layer '{layer}' is off: the entities exist but are not displayed until it is turned on.";
        return null;
    }

    /// <summary>The layer's id, creating it with the colour when it does not exist. Phase 2 only.</summary>
    public ObjectId EnsureLayer(string name, short colorIndex, out bool created)
    {
        var table = (LayerTable)Tr.GetObject(Db.LayerTableId, OpenMode.ForRead);
        created = false;
        if (table.Has(name)) return table[name];
        table.UpgradeOpen();
        var record = new LayerTableRecord { Name = name, Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex) };
        var id = table.Add(record);
        Tr.AddNewlyCreatedDBObject(record, true);
        created = true;
        return id;
    }
}
