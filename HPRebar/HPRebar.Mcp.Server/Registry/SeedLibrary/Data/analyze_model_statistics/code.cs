double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
bool detailed = args.Bool("includeDetailedTypes", true);
int top = args.Int("topCategories", 40);

var instances = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();
int totalTypes = new FilteredElementCollector(doc).WhereElementIsElementType().GetElementCount();
int totalViews = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().Count(v => !v.IsTemplate);
int totalSheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();
int totalFamilies = new FilteredElementCollector(doc).OfClass(typeof(Family)).GetElementCount();

var byCategory = new Dictionary<string, (int count, Dictionary<string, int> types)>();
foreach (var e in instances)
{
    ct.ThrowIfCancellationRequested();
    var cat = e.Category?.Name;
    if (cat == null) continue;
    if (!byCategory.TryGetValue(cat, out var entry)) entry = (0, new Dictionary<string, int>());
    entry.count++;
    if (detailed)
    {
        var t = doc.GetElement(e.GetTypeId()) as ElementType;
        if (t != null)
        {
            var key = (t.FamilyName ?? "") + " : " + t.Name;
            entry.types[key] = entry.types.TryGetValue(key, out var n) ? n + 1 : 1;
        }
    }
    byCategory[cat] = entry;
}

var categories = byCategory.OrderByDescending(kv => kv.Value.count).Select(kv => new
{
    categoryName = kv.Key,
    elementCount = kv.Value.count,
    typeCount = kv.Value.types.Count,
    types = detailed ? kv.Value.types.OrderByDescending(t => t.Value).Take(30).Select(t => new { type = t.Key, instanceCount = t.Value }).ToList() : null,
});
if (top > 0) categories = categories.Take(top);

var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation)
    .Select(l => new { levelName = l.Name, elevation = Mm(l.Elevation), elementCount = instances.Count(e => e.LevelId == l.Id) }).ToList();

return new
{
    projectName = doc.ProjectInformation?.Name ?? doc.Title,
    title = doc.Title,
    totalElements = instances.Count,
    totalTypes,
    totalFamilies,
    totalViews,
    totalSheets,
    categoryCount = byCategory.Count,
    categories = categories.ToList(),
    levels,
};
