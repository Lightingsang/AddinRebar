double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();
if (levels.Count == 0) throw new InvalidOperationException("The document has no levels.");
Level Nearest(double zFt) => levels.OrderBy(l => Math.Abs(l.Elevation - zFt)).First();

var created = new List<object>();
var warnings = new List<string>();

CurveLoop LoopOf(HPRebar.McpBridge.Core.Scripting.ScriptArgs boundary, double zFt, int index)
{
    var segments = boundary.List("outerLoop");
    if (segments.Count < 3) throw new ArgumentException($"data[{index}]: outerLoop needs at least 3 segments.");
    var points = segments.Select(s => s.Obj("p0")).Select(p => new XYZ(Ft(p.Double("x")), Ft(p.Double("y")), zFt)).ToList();
    var curves = new List<Curve>();
    for (int i = 0; i < points.Count; i++)
    {
        var a = points[i]; var b = points[(i + 1) % points.Count];
        if (a.DistanceTo(b) < Ft(1)) continue;
        curves.Add(Line.CreateBound(a, b));
    }
    return CurveLoop.Create(curves);
}

T TypeByThickness<T>(long typeId, double thicknessMm, string label) where T : HostObjAttributes
{
    if (typeId > 0 && doc.GetElement(new ElementId(typeId)) is T requested) return requested;
    var types = new FilteredElementCollector(doc).OfClass(typeof(T)).Cast<T>().Where(t => t.GetCompoundStructure() != null).ToList();
    if (types.Count == 0) throw new InvalidOperationException($"No {label} type with a compound structure in the document.");
    if (thicknessMm <= 0) return types[0];
    var match = types.FirstOrDefault(t => Math.Abs(Mm(t.GetCompoundStructure().GetWidth()) - thicknessMm) < 0.5);
    if (match != null) return match;
    var source = types.FirstOrDefault(t => t.GetCompoundStructure().LayerCount == 1) ?? types[0];
    var name = $"{label} {thicknessMm:0}mm";
    var copy = (T)source.Duplicate(name);
    var cs = copy.GetCompoundStructure();
    int core = cs.GetFirstCoreLayerIndex(); if (core < 0) core = 0;
    for (int i = 0; i < cs.LayerCount; i++) if (i != core) cs.SetLayerWidth(i, 0);
    cs.SetLayerWidth(core, Ft(thicknessMm));
    copy.SetCompoundStructure(cs);
    warnings.Add($"Created {label} type '{name}' from '{source.Name}'.");
    return copy;
}

int index = 0;
foreach (var item in args.List("data"))
{
    ct.ThrowIfCancellationRequested();
    string categoryName = item.Str("category", "OST_Floors");
    var level = Nearest(Ft(item.Double("baseLevel")));
    double offsetFt = Ft(item.Double("baseLevel") + item.Double("baseOffset", 0)) - level.Elevation;
    long typeId = item.Long("typeId", -1);
    double thickness = item.Double("thickness", 0);
    var loop = LoopOf(item.Obj("boundary"), level.Elevation, index);
    Element element; string typeName;

    switch (categoryName.ToUpperInvariant())
    {
        case "OST_CEILINGS":
        {
            var type = TypeByThickness<CeilingType>(typeId, thickness, "Ceiling");
            var ceiling = Ceiling.Create(doc, new List<CurveLoop> { loop }, type.Id, level.Id);
            ceiling.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM)?.Set(offsetFt);
            element = ceiling; typeName = type.Name;
            break;
        }
        case "OST_ROOFS":
        {
            var type = typeId > 0 ? doc.GetElement(new ElementId(typeId)) as RoofType : null;
            type ??= new FilteredElementCollector(doc).OfClass(typeof(RoofType)).Cast<RoofType>().FirstOrDefault() ?? throw new InvalidOperationException("No roof type in the document.");
            var profile = new CurveArray();
            foreach (var c in loop) profile.Append(c);
            var roof = doc.Create.NewFootPrintRoof(profile, level, type, out var _);
            roof.get_Parameter(BuiltInParameter.ROOF_LEVEL_OFFSET_PARAM)?.Set(offsetFt);
            element = roof; typeName = type.Name;
            break;
        }
        default:
        {
            var type = TypeByThickness<FloorType>(typeId, thickness, "Floor");
            var floor = Floor.Create(doc, new List<CurveLoop> { loop }, type.Id, level.Id, item.Bool("structural", true), null, 0);
            floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Set(offsetFt);
            element = floor; typeName = type.Name;
            categoryName = "OST_Floors";
            break;
        }
    }

    created.Add(new { id = element.Id.Value, name = item.Str("name"), category = categoryName, type = typeName, level = level.Name, offset = Mm(offsetFt), area = Math.Round(UnitUtils.ConvertFromInternalUnits(element.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0, UnitTypeId.SquareMeters), 3) });
    index++;
}

return new { created = created.Count, elements = created, warnings };
