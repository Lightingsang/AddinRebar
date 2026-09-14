bool includeCounts = args.Bool("includeCounts", false);
var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
if (includeCounts)
{
    var modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);
    foreach (ObjectId id in modelSpace)
    {
        ct.ThrowIfCancellationRequested();
        if (tr.GetObject(id, OpenMode.ForRead) is Entity entity)
            counts[entity.Layer] = counts.TryGetValue(entity.Layer, out var c) ? c + 1 : 1;
    }
}

var table = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
var records = new List<LayerTableRecord>();
foreach (ObjectId id in table) records.Add((LayerTableRecord)tr.GetObject(id, OpenMode.ForRead));

var layers = records.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase).Select(l => new
{
    name = l.Name,
    colorIndex = l.Color.ColorIndex,
    isOff = l.IsOff,
    isFrozen = l.IsFrozen,
    isLocked = l.IsLocked,
    isPlottable = l.IsPlottable,
    lineweight = l.LineWeight.ToString(),
    entityCount = includeCounts ? (int?)(counts.TryGetValue(l.Name, out var n) ? n : 0) : null,
}).ToList();
log($"{layers.Count} layers");
return layers;
