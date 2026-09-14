bool countByType = args.Bool("countByType", true);
double Mm(double du) => Math.Round(units.ToMm(du), 1);

var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
var total = 0;
var modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
foreach (ObjectId id in modelSpace)
{
    total++;
    if (!countByType) continue;
    ct.ThrowIfCancellationRequested();
    var dxf = id.ObjectClass.DxfName;
    counts[dxf] = counts.TryGetValue(dxf, out var c) ? c + 1 : 1;
}

var layerCount = 0;
var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
foreach (ObjectId id in layerTable) layerCount++;
var currentLayer = ((LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead)).Name;

return new
{
    fileName = db.Filename,
    isNamedDrawing = doc.IsNamedDrawing,
    insunits = db.Insunits.ToString(),
    measurement = db.Measurement.ToString(),
    unitsLabel = units.Label,
    mmPerUnit = units.MmPerUnit,
    extentsMm = new
    {
        min = new { x = Mm(db.Extmin.X), y = Mm(db.Extmin.Y), z = Mm(db.Extmin.Z) },
        max = new { x = Mm(db.Extmax.X), y = Mm(db.Extmax.Y), z = Mm(db.Extmax.Z) },
    },
    currentLayout = LayoutManager.Current.CurrentLayout,
    currentLayer,
    layerCount,
    modelSpaceEntities = total,
    countsByType = countByType ? counts.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value) : null,
};
