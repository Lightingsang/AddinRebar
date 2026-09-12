double M2(double sqft) => UnitUtils.ConvertFromInternalUnits(sqft, UnitTypeId.SquareMeters);
double M3(double cuft) => UnitUtils.ConvertFromInternalUnits(cuft, UnitTypeId.CubicMeters);
int maxIds = args.Int("maxElementIdsPerMaterial", 50);

ICollection<Element> elements;
if (args.Bool("selectedElementsOnly"))
    elements = uidoc.Selection.GetElementIds().Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
else
{
    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    var cats = args.Strings("categoryFilters").Select(n => Enum.TryParse<BuiltInCategory>(n, true, out var b) ? b : (BuiltInCategory?)null).Where(b => b != null).Select(b => b.Value).ToList();
    if (cats.Count > 0) collector = collector.WherePasses(new ElementMulticategoryFilter(cats));
    elements = collector.ToElements();
}

var data = new Dictionary<long, (string name, string cls, double area, double volume, HashSet<long> ids)>();
int i = 0;
foreach (var e in elements)
{
    if (++i % 200 == 0) { ct.ThrowIfCancellationRequested(); progress(i, elements.Count, "materials"); }
    foreach (var matId in e.GetMaterialIds(false))
    {
        var m = doc.GetElement(matId) as Material;
        if (m == null) continue;
        if (!data.TryGetValue(matId.Value, out var entry)) entry = (m.Name, m.MaterialClass, 0, 0, new HashSet<long>());
        entry.area += e.GetMaterialArea(matId, false);
        entry.volume += e.GetMaterialVolume(matId);
        entry.ids.Add(e.Id.Value);
        data[matId.Value] = entry;
    }
}

var materials = data.OrderByDescending(kv => kv.Value.volume).Select(kv => new
{
    materialId = kv.Key,
    materialName = kv.Value.name,
    materialClass = kv.Value.cls,
    area = Math.Round(M2(kv.Value.area), 3),
    volume = Math.Round(M3(kv.Value.volume), 3),
    elementCount = kv.Value.ids.Count,
    elementIds = kv.Value.ids.Take(maxIds).ToList(),
}).ToList();

return new
{
    elementsScanned = elements.Count,
    totalMaterials = materials.Count,
    totalArea = Math.Round(materials.Sum(m => m.area), 3),
    totalVolume = Math.Round(materials.Sum(m => m.volume), 3),
    materials,
};
