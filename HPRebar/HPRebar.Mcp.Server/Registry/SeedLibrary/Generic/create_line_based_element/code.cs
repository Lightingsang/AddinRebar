double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
if (levels.Count == 0) throw new InvalidOperationException("The document has no levels.");
Level Nearest(double zFt) => levels.OrderBy(l => Math.Abs(l.Elevation - zFt)).First();
XYZ Point(HPRebar.McpBridge.Core.Scripting.ScriptArgs p) => new XYZ(Ft(p.Double("x")), Ft(p.Double("y")), Ft(p.Double("z")));

var created = new List<object>();
var warnings = new List<string>();

WallType WallTypeFor(long typeId, double thicknessMm)
{
    if (typeId > 0 && doc.GetElement(new ElementId(typeId)) is WallType requested) return requested;
    var types = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(t => t.Kind == WallKind.Basic).ToList();
    if (types.Count == 0) throw new InvalidOperationException("No basic wall type in the document.");
    if (thicknessMm <= 0) return types[0];
    var match = types.FirstOrDefault(t => Math.Abs(Mm(t.Width) - thicknessMm) < 0.5);
    if (match != null) return match;
    var source = types.FirstOrDefault(t => t.GetCompoundStructure()?.LayerCount == 1) ?? types[0];
    var name = $"Wall {thicknessMm:0}mm";
    var copy = source.Duplicate(name) as WallType;
    var cs = copy.GetCompoundStructure();
    int core = cs.GetFirstCoreLayerIndex(); if (core < 0) core = 0;
    for (int i = 0; i < cs.LayerCount; i++) if (i != core) cs.SetLayerWidth(i, 0);
    cs.SetLayerWidth(core, Ft(thicknessMm));
    copy.SetCompoundStructure(cs);
    warnings.Add($"Created wall type '{name}' from '{source.Name}'.");
    return copy;
}

FamilySymbol SymbolFor(long typeId, BuiltInCategory bic, string label)
{
    if (typeId > 0 && doc.GetElement(new ElementId(typeId)) is FamilySymbol requested) return requested;
    var symbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(bic).Cast<FamilySymbol>().ToList();
    var symbol = symbols.FirstOrDefault(s => s.IsActive) ?? symbols.FirstOrDefault();
    if (symbol == null) throw new InvalidOperationException($"No family type loaded for {label}. Load a family or pass typeId.");
    if (typeId > 0) warnings.Add($"typeId {typeId} is not a {label} type — used '{symbol.FamilyName}: {symbol.Name}' ({symbol.Id.Value}).");
    if (!symbol.IsActive) symbol.Activate();
    return symbol;
}

void TrySet(Element e, BuiltInParameter p, double valueFt) { var param = e.get_Parameter(p); if (param != null && !param.IsReadOnly) param.Set(valueFt); }

int index = 0;
foreach (var item in args.List("data"))
{
    ct.ThrowIfCancellationRequested();
    index++;
    string categoryName = item.Require("category");
    if (!Enum.TryParse<BuiltInCategory>(categoryName, true, out var bic)) throw new ArgumentException($"data[{index - 1}].category '{categoryName}' is not a BuiltInCategory.");
    var line = item.Obj("locationLine");
    var p0 = Point(line.Obj("p0")); var p1 = Point(line.Obj("p1"));
    if (p0.DistanceTo(p1) < Ft(10)) throw new ArgumentException($"data[{index - 1}]: line is shorter than 10 mm.");
    double baseElevationMm = item.RequireDouble("baseLevel") + item.Double("baseOffset", 0);
    var level = Nearest(Ft(item.Double("baseLevel")));
    double offsetFt = Ft(baseElevationMm) - level.Elevation;
    long typeId = item.Long("typeId", -1);
    double thickness = item.Double("thickness", 0), height = item.Double("height", 0);
    Element element; string typeName;

    switch (bic)
    {
        case BuiltInCategory.OST_Walls:
        {
            var wallType = WallTypeFor(typeId, thickness);
            var flat = Line.CreateBound(new XYZ(p0.X, p0.Y, level.Elevation), new XYZ(p1.X, p1.Y, level.Elevation));
            var wall = Wall.Create(doc, flat, wallType.Id, level.Id, Ft(height > 0 ? height : 3000), offsetFt, false, item.Bool("structural", false));
            element = wall; typeName = wallType.Name;
            break;
        }
        case BuiltInCategory.OST_StructuralFraming:
        {
            var symbol = SymbolFor(typeId, bic, "structural framing");
            var beam = doc.Create.NewFamilyInstance(Line.CreateBound(p0, p1), symbol, level, StructuralType.Beam);
            element = beam; typeName = symbol.FamilyName + ": " + symbol.Name;
            break;
        }
        case BuiltInCategory.OST_DuctCurves:
        {
            var ductType = typeId > 0 ? doc.GetElement(new ElementId(typeId)) as Autodesk.Revit.DB.Mechanical.DuctType : null;
            ductType ??= new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Mechanical.DuctType)).Cast<Autodesk.Revit.DB.Mechanical.DuctType>().FirstOrDefault()
                ?? throw new InvalidOperationException("No duct type in the document.");
            var system = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Mechanical.MechanicalSystemType)).Cast<MEPSystemType>().FirstOrDefault(s => s.SystemClassification == MEPSystemClassification.SupplyAir)
                ?? new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Mechanical.MechanicalSystemType)).Cast<MEPSystemType>().FirstOrDefault()
                ?? throw new InvalidOperationException("No mechanical system type in the document.");
            var duct = Autodesk.Revit.DB.Mechanical.Duct.Create(doc, system.Id, ductType.Id, level.Id, p0, p1);
            if (thickness > 0) TrySet(duct, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, Ft(thickness));
            if (height > 0) TrySet(duct, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, Ft(height));
            element = duct; typeName = ductType.Name;
            break;
        }
        case BuiltInCategory.OST_PipeCurves:
        {
            var pipeType = typeId > 0 ? doc.GetElement(new ElementId(typeId)) as Autodesk.Revit.DB.Plumbing.PipeType : null;
            pipeType ??= new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipeType)).Cast<Autodesk.Revit.DB.Plumbing.PipeType>().FirstOrDefault()
                ?? throw new InvalidOperationException("No pipe type in the document.");
            var system = new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType)).Cast<MEPSystemType>().FirstOrDefault()
                ?? throw new InvalidOperationException("No piping system type in the document.");
            var pipe = Autodesk.Revit.DB.Plumbing.Pipe.Create(doc, system.Id, pipeType.Id, level.Id, p0, p1);
            if (thickness > 0) TrySet(pipe, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, Ft(thickness));
            element = pipe; typeName = pipeType.Name;
            break;
        }
        case BuiltInCategory.OST_Conduit:
        {
            var conduitType = typeId > 0 ? doc.GetElement(new ElementId(typeId)) as Autodesk.Revit.DB.Electrical.ConduitType : null;
            conduitType ??= new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Electrical.ConduitType)).Cast<Autodesk.Revit.DB.Electrical.ConduitType>().FirstOrDefault()
                ?? throw new InvalidOperationException("No conduit type in the document.");
            var conduit = Autodesk.Revit.DB.Electrical.Conduit.Create(doc, conduitType.Id, p0, p1, level.Id);
            if (thickness > 0) TrySet(conduit, BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM, Ft(thickness));
            element = conduit; typeName = conduitType.Name;
            break;
        }
        case BuiltInCategory.OST_CableTray:
        {
            var trayType = typeId > 0 ? doc.GetElement(new ElementId(typeId)) as Autodesk.Revit.DB.Electrical.CableTrayType : null;
            trayType ??= new FilteredElementCollector(doc).OfClass(typeof(Autodesk.Revit.DB.Electrical.CableTrayType)).Cast<Autodesk.Revit.DB.Electrical.CableTrayType>().FirstOrDefault()
                ?? throw new InvalidOperationException("No cable tray type in the document.");
            var tray = Autodesk.Revit.DB.Electrical.CableTray.Create(doc, trayType.Id, p0, p1, level.Id);
            if (thickness > 0) TrySet(tray, BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM, Ft(thickness));
            if (height > 0) TrySet(tray, BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM, Ft(height));
            element = tray; typeName = trayType.Name;
            break;
        }
        default:
        {
            var symbol = SymbolFor(typeId, bic, categoryName);
            var instance = doc.Create.NewFamilyInstance(Line.CreateBound(p0, p1), symbol, level, StructuralType.NonStructural);
            element = instance; typeName = symbol.FamilyName + ": " + symbol.Name;
            break;
        }
    }

    created.Add(new { id = element.Id.Value, category = categoryName, type = typeName, level = level.Name, baseOffset = Mm(offsetFt), length = Mm(p0.DistanceTo(p1)) });
}

return new { created = created.Count, elements = created, warnings };
