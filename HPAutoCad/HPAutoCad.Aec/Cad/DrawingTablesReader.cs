using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Standards;

namespace HPAutoCad.Aec.Cad;

/// <summary>Reads the symbol tables a standards check needs into plain <see cref="DrawingTables"/> through the bridge's transaction.</summary>
public static class DrawingTablesReader
{
    public static DrawingTables Read(Database db, Transaction tr, CancellationToken ct)
    {
        var layers = new List<LayerRecord>();
        foreach (var id in (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead))
        {
            ct.ThrowIfCancellationRequested();
            var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
            layers.Add(new LayerRecord(layer.Name, layer.IsLocked, layer.IsFrozen, layer.IsOff, layer.Color.ColorNameForDisplay, LinetypeName(tr, layer.LinetypeObjectId), layer.LineWeight.ToString(), layer.IsDependent, layer.IsHidden));
        }

        var textStyles = new List<string>();
        foreach (var id in (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead))
            textStyles.Add(((TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name);
        var dimStyles = new List<string>();
        foreach (var id in (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead))
            dimStyles.Add(((DimStyleTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name);
        var blocks = new List<BlockDefinitionRecord>();
        var layersInBlocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead))
        {
            ct.ThrowIfCancellationRequested();
            var block = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            blocks.Add(new BlockDefinitionRecord(block.Name, block.IsAnonymous, block.IsLayout, block.IsFromExternalReference, block.IsDependent));
            if (block.IsLayout || block.IsFromExternalReference || block.IsDependent) continue;
            foreach (ObjectId entityId in block)
                if (tr.GetObject(entityId, OpenMode.ForRead) is Entity entity) layersInBlocks.Add(entity.Layer);
        }

        return new DrawingTables { Layers = layers, TextStyles = textStyles, DimStyles = dimStyles, Blocks = blocks, LayersUsedInBlocks = layersInBlocks.ToArray() };
    }

    private static string LinetypeName(Transaction tr, ObjectId id)
    {
        try { return id.IsNull ? "" : ((LinetypeTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name; }
        catch (Autodesk.AutoCAD.Runtime.Exception) { return ""; }
    }
}
