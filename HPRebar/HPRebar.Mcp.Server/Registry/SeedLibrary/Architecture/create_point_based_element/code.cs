double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
if (levels.Count == 0) throw new InvalidOperationException("The document has no levels.");
Level Nearest(double zFt) => levels.OrderBy(l => Math.Abs(l.Elevation - zFt)).First();
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();

var created = new List<object>();
var warnings = new List<string>();

FamilySymbol Resolve(HPRebar.McpBridge.Core.Scripting.ScriptArgs item, int index)
{
    long typeId = item.Long("typeId", -1);
    if (typeId > 0)
    {
        if (doc.GetElement(new ElementId(typeId)) is FamilySymbol requested) return requested;
        warnings.Add($"data[{index}]: typeId {typeId} is not a family type — falling back to category.");
    }
    string categoryName = item.Str("category");
    if (string.IsNullOrWhiteSpace(categoryName) || !Enum.TryParse<BuiltInCategory>(categoryName, true, out var bic))
        throw new ArgumentException($"data[{index}]: give a valid typeId or a BuiltInCategory in 'category'.");
    var symbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(bic).Cast<FamilySymbol>().ToList();
    return symbols.FirstOrDefault(s => s.IsActive) ?? symbols.FirstOrDefault() ?? throw new InvalidOperationException($"No family type loaded for {categoryName}.");
}

Wall NearestWall(XYZ point, out XYZ onWall)
{
    Wall best = null; double bestDistance = double.MaxValue; onWall = point;
    foreach (var wall in walls)
    {
        if (wall.Location is not LocationCurve lc) continue;
        var flat = new XYZ(point.X, point.Y, lc.Curve.GetEndPoint(0).Z);
        var projected = lc.Curve.Project(flat);
        if (projected == null || projected.Distance >= bestDistance) continue;
        best = wall; bestDistance = projected.Distance; onWall = new XYZ(projected.XYZPoint.X, projected.XYZPoint.Y, point.Z);
    }
    return best != null && bestDistance <= Ft(1500) ? best : null;
}

void TrySet(Element e, string name, double mm)
{
    if (mm <= 0) return;
    var p = e.LookupParameter(name);
    if (p != null && !p.IsReadOnly && p.StorageType == StorageType.Double) p.Set(Ft(mm));
    else warnings.Add($"{e.Id.Value}: no writable instance parameter '{name}' — left at type value.");
}

int index = 0;
foreach (var item in args.List("data"))
{
    ct.ThrowIfCancellationRequested();
    var symbol = Resolve(item, index);
    if (!symbol.IsActive) symbol.Activate();
    var lp = item.Obj("locationPoint");
    var level = Nearest(Ft(item.Double("baseLevel")));
    double offsetFt = Ft(item.Double("baseLevel") + item.Double("baseOffset", 0)) - level.Elevation;
    var point = new XYZ(Ft(lp.Double("x")), Ft(lp.Double("y")), level.Elevation + offsetFt);
    var bic = (BuiltInCategory)symbol.Category.Id.Value;
    bool wallHosted = symbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBasedHosted || bic == BuiltInCategory.OST_Doors || bic == BuiltInCategory.OST_Windows;

    FamilyInstance instance; string hostInfo = null;
    if (wallHosted)
    {
        Wall host = null; var onWall = point;
        long hostId = item.Long("hostWallId", -1);
        if (hostId > 0)
        {
            host = doc.GetElement(new ElementId(hostId)) as Wall;
            if (host == null) warnings.Add($"data[{index}]: hostWallId {hostId} is not a wall — auto-detecting.");
            else if (host.Location is LocationCurve lc) { var pr = lc.Curve.Project(new XYZ(point.X, point.Y, lc.Curve.GetEndPoint(0).Z)); if (pr != null) onWall = new XYZ(pr.XYZPoint.X, pr.XYZPoint.Y, point.Z); }
        }
        host ??= NearestWall(point, out onWall);
        if (host == null) throw new InvalidOperationException($"data[{index}]: no wall within 1500 mm of ({lp.Double("x")}, {lp.Double("y")}) to host '{symbol.FamilyName}'.");
        instance = doc.Create.NewFamilyInstance(onWall, symbol, host, level, StructuralType.NonStructural);
        hostInfo = host.Id.Value.ToString();
        if (item.Bool("facingFlipped", false)) instance.flipFacing();
    }
    else
    {
        var structuralType = bic == BuiltInCategory.OST_StructuralColumns ? StructuralType.Column : StructuralType.NonStructural;
        instance = doc.Create.NewFamilyInstance(point, symbol, level, structuralType);
        // Revit reads the point's Z as an offset for some placement types; pin the offset explicitly
        var offsetParam = instance.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM) ?? instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
        if (offsetParam != null && !offsetParam.IsReadOnly) offsetParam.Set(offsetFt);
    }

    double rotation = item.Double("rotation", 0);
    if (Math.Abs(rotation) > 1e-6)
    {
        var axis = Line.CreateBound(point, point + XYZ.BasisZ);
        ElementTransformUtils.RotateElement(doc, instance.Id, axis, rotation * Math.PI / 180);
    }
    TrySet(instance, "Width", item.Double("width", 0));
    TrySet(instance, "Height", item.Double("height", 0));
    TrySet(instance, "Depth", item.Double("depth", 0));

    created.Add(new { id = instance.Id.Value, name = item.Str("name"), type = symbol.FamilyName + ": " + symbol.Name, category = symbol.Category.Name, level = level.Name, hostWallId = hostInfo, baseOffset = Mm(offsetFt) });
    index++;
}

return new { created = created.Count, elements = created, warnings };
