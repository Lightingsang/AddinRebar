double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
string[] defaultModel = { "OST_Walls", "OST_Doors", "OST_Windows", "OST_Furniture", "OST_Columns", "OST_StructuralColumns", "OST_Floors", "OST_Roofs", "OST_Stairs", "OST_StructuralFraming", "OST_Ceilings", "OST_MEPSpaces", "OST_Rooms" };
string[] defaultAnnotation = { "OST_Dimensions", "OST_TextNotes", "OST_GenericAnnotation", "OST_WallTags", "OST_DoorTags", "OST_WindowTags", "OST_RoomTags", "OST_AreaTags", "OST_SpaceTags", "OST_ViewportLabels", "OST_TitleBlocks" };

var view = doc.ActiveView;
var wanted = new List<string>();
if (!args.Has("modelCategoryList") && !args.Has("annotationCategoryList")) { wanted.AddRange(defaultModel); wanted.AddRange(defaultAnnotation); }
else { wanted.AddRange(args.Strings("modelCategoryList")); wanted.AddRange(args.Strings("annotationCategoryList")); }

var categories = wanted.Select(n => Enum.TryParse<BuiltInCategory>(n, true, out var bic) ? bic : (BuiltInCategory?)null).Where(b => b != null).Select(b => b.Value).ToList();
var collector = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType();
if (categories.Count > 0) collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

bool includeHidden = args.Bool("includeHidden");
int limit = args.Int("limit", 100);
var elements = collector.ToElements().Where(e => includeHidden || !e.IsHidden(view)).ToList();
int filteredCount = elements.Count;
if (limit > 0 && elements.Count > limit) elements = elements.Take(limit).ToList();

object Describe(Element e)
{
    var props = new Dictionary<string, object>();
    if (e.Location is LocationPoint lp) { props["location"] = new { x = Mm(lp.Point.X), y = Mm(lp.Point.Y), z = Mm(lp.Point.Z) }; }
    else if (e.Location is LocationCurve lc)
    {
        var a = lc.Curve.GetEndPoint(0); var b = lc.Curve.GetEndPoint(1);
        props["start"] = new { x = Mm(a.X), y = Mm(a.Y), z = Mm(a.Z) };
        props["end"] = new { x = Mm(b.X), y = Mm(b.Y), z = Mm(b.Z) };
        props["length"] = Mm(lc.Curve.Length);
    }
    foreach (var pname in new[] { "Comments", "Mark" })
    {
        var p = e.LookupParameter(pname);
        if (p != null && p.StorageType == StorageType.String && !string.IsNullOrEmpty(p.AsString())) props[pname.ToLowerInvariant()] = p.AsString();
    }
    var typeElement = doc.GetElement(e.GetTypeId()) as ElementType;
    return new
    {
        id = e.Id.Value,
        uniqueId = e.UniqueId,
        name = e.Name,
        category = e.Category?.Name ?? "unknown",
        typeName = typeElement?.Name,
        familyName = typeElement?.FamilyName,
        level = (doc.GetElement(e.LevelId) as Level)?.Name,
        properties = props,
    };
}

return new
{
    viewId = view.Id.Value,
    viewName = view.Name,
    totalElementsInView = new FilteredElementCollector(doc, view.Id).WhereElementIsNotElementType().GetElementCount(),
    filteredElementCount = filteredCount,
    returned = elements.Count,
    elements = elements.Select(Describe).ToList(),
};
