double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

string categoryName = args.Str("filterCategory");
string className = args.Str("filterElementType");
long symbolId = args.Long("filterFamilySymbolId", -1);
bool includeTypes = args.Bool("includeTypes", false);
bool includeInstances = args.Bool("includeInstances", true);
bool visibleOnly = args.Bool("filterVisibleInCurrentView", false);
int max = Math.Max(1, args.Int("maxElements", 50));

if (string.IsNullOrWhiteSpace(categoryName) && string.IsNullOrWhiteSpace(className) && symbolId <= 0)
    throw new ArgumentException("Give at least one of filterCategory, filterElementType or filterFamilySymbolId.");
if (!includeTypes && !includeInstances) throw new ArgumentException("includeTypes and includeInstances cannot both be false.");

FilteredElementCollector NewCollector() => visibleOnly && !includeTypes ? new FilteredElementCollector(doc, doc.ActiveView.Id) : new FilteredElementCollector(doc);

ElementFilter categoryFilter = null;
if (!string.IsNullOrWhiteSpace(categoryName))
{
    if (Enum.TryParse<BuiltInCategory>(categoryName, true, out var bic)) categoryFilter = new ElementCategoryFilter(bic);
    else
    {
        var cat = doc.Settings.Categories.Cast<Category>().FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));
        if (cat == null) throw new ArgumentException($"Unknown category '{categoryName}'. Use a BuiltInCategory name like OST_Walls.");
        categoryFilter = new ElementCategoryFilter(cat.Id);
    }
}

ElementFilter boxFilter = null;
if (args.Has("boundingBoxMin") && args.Has("boundingBoxMax"))
{
    var mn = args.Obj("boundingBoxMin"); var mx = args.Obj("boundingBoxMax");
    var outline = new Outline(new XYZ(Ft(mn.Double("x")), Ft(mn.Double("y")), Ft(mn.Double("z"))), new XYZ(Ft(mx.Double("x")), Ft(mx.Double("y")), Ft(mx.Double("z"))));
    boxFilter = new BoundingBoxIntersectsFilter(outline);
}

var results = new List<Element>();
void Collect(bool types)
{
    var collector = NewCollector();
    collector = types ? collector.WhereElementIsElementType() : collector.WhereElementIsNotElementType();
    if (categoryFilter != null) collector = collector.WherePasses(categoryFilter);
    if (boxFilter != null && !types) collector = collector.WherePasses(boxFilter);
    IEnumerable<Element> items = collector;
    if (!string.IsNullOrWhiteSpace(className))
        items = items.Where(e => string.Equals(e.GetType().Name, className, StringComparison.OrdinalIgnoreCase) || string.Equals(e.GetType().FullName, className, StringComparison.OrdinalIgnoreCase));
    if (symbolId > 0 && !types) items = items.Where(e => e is FamilyInstance fi && fi.Symbol.Id.Value == symbolId);
    foreach (var e in items) { ct.ThrowIfCancellationRequested(); results.Add(e); }
}
if (includeInstances) Collect(false);
if (includeTypes) Collect(true);

int total = results.Count;
var page = results.Take(max).ToList();

object Info(Element e)
{
    var typeElement = e is ElementType et ? et : doc.GetElement(e.GetTypeId()) as ElementType;
    object location = null;
    if (e.Location is LocationPoint lp) location = new { kind = "point", x = Mm(lp.Point.X), y = Mm(lp.Point.Y), z = Mm(lp.Point.Z), rotationDeg = Math.Round(lp.Rotation * 180 / Math.PI, 2) };
    else if (e.Location is LocationCurve lc)
    {
        var a = lc.Curve.GetEndPoint(0); var b = lc.Curve.GetEndPoint(1);
        location = new { kind = "curve", start = new { x = Mm(a.X), y = Mm(a.Y), z = Mm(a.Z) }, end = new { x = Mm(b.X), y = Mm(b.Y), z = Mm(b.Z) }, length = Mm(lc.Curve.Length) };
    }
    object box = null;
    var bb = e.get_BoundingBox(null);
    if (bb != null) box = new { min = new { x = Mm(bb.Min.X), y = Mm(bb.Min.Y), z = Mm(bb.Min.Z) }, max = new { x = Mm(bb.Max.X), y = Mm(bb.Max.Y), z = Mm(bb.Max.Z) }, height = Mm(bb.Max.Z - bb.Min.Z) };
    var parameters = new Dictionary<string, object>();
    foreach (var name in new[] { "Comments", "Mark", "Length", "Area", "Volume", "Height", "Width", "Unconnected Height", "Base Offset", "Top Offset", "Thickness" })
    {
        var p = e.LookupParameter(name);
        if (p == null || !p.HasValue) continue;
        parameters[name] = p.StorageType switch
        {
            StorageType.String => (object)p.AsString(),
            StorageType.Integer => p.AsInteger(),
            StorageType.Double => p.AsValueString(),
            StorageType.ElementId => p.AsValueString(),
            _ => null,
        };
    }
    return new
    {
        id = e.Id.Value,
        uniqueId = e.UniqueId,
        name = e.Name,
        category = e.Category?.Name,
        elementClass = e.GetType().Name,
        isType = e is ElementType,
        typeId = typeElement?.Id.Value,
        typeName = typeElement?.Name,
        familyName = typeElement?.FamilyName,
        level = (doc.GetElement(e.LevelId) as Level)?.Name,
        location,
        boundingBox = box,
        parameters,
    };
}

return new { total, returned = page.Count, truncated = total > page.Count, elements = page.Select(Info).ToList() };
