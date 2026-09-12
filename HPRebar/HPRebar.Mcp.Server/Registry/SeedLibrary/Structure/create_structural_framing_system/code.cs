double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
double Mm(double ft) => Math.Round(UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters), 1);
var warnings = new List<string>();

string levelName = args.Require("levelName");
var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
if (level == null)
{
    var m = System.Text.RegularExpressions.Regex.Match(levelName, @"^Level\s+(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    if (!m.Success) throw new ArgumentException($"Level '{levelName}' not found. Create it first (create_level) or use an existing level name.");
    double elevationMm = int.Parse(m.Groups[1].Value) * 4000.0;
    level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(l => Math.Abs(Mm(l.Elevation) - elevationMm) < 1);
    if (level == null)
    {
        level = Level.Create(doc, Ft(elevationMm));
        level.Name = levelName;
        var planType = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == ViewFamily.StructuralPlan)
            ?? new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);
        if (planType != null) ViewPlan.Create(doc, planType.Id, level.Id);
        warnings.Add($"Created level '{levelName}' at {elevationMm} mm.");
    }
    else warnings.Add($"Used existing level '{level.Name}' at {elevationMm} mm instead of creating '{levelName}'.");
}

double xMin = args.RequireDouble("xMin"), xMax = args.RequireDouble("xMax"), yMin = args.RequireDouble("yMin"), yMax = args.RequireDouble("yMax");
if (xMax - xMin < 100 || yMax - yMin < 100) throw new ArgumentException("The rectangle must be at least 100 × 100 mm.");
double z = args.Has("elevation") ? Ft(args.Double("elevation")) : level.Elevation;
var p0 = new XYZ(Ft(xMin), Ft(yMin), z); var p1 = new XYZ(Ft(xMax), Ft(yMin), z); var p2 = new XYZ(Ft(xMax), Ft(yMax), z); var p3 = new XYZ(Ft(xMin), Ft(yMax), z);
var profile = new List<Curve> { Line.CreateBound(p0, p1), Line.CreateBound(p1, p2), Line.CreateBound(p2, p3), Line.CreateBound(p3, p0) };
int directionIndex = (args.Str("directionEdge", "bottom") ?? "bottom").ToLowerInvariant() switch { "right" => 1, "top" => 2, "left" => 3, _ => 0 };

string typeName = args.Str("beamTypeName");
var symbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_StructuralFraming).Cast<FamilySymbol>().ToList();
if (symbols.Count == 0) throw new InvalidOperationException("No structural framing family loaded.");
var beamType = string.IsNullOrWhiteSpace(typeName) ? null
    : symbols.FirstOrDefault(s => string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase))
      ?? symbols.FirstOrDefault(s => s.Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0 || s.FamilyName.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0);
if (beamType == null)
{
    beamType = symbols.FirstOrDefault(s => s.IsActive) ?? symbols[0];
    if (!string.IsNullOrWhiteSpace(typeName)) warnings.Add($"Beam type '{typeName}' not found — used '{beamType.FamilyName}: {beamType.Name}'.");
}
if (!beamType.IsActive) beamType.Activate();

var justify = (args.Str("justify", "center") ?? "center").ToLowerInvariant() switch
{
    "beginning" => BeamSystemJustifyType.Beginning,
    "end" => BeamSystemJustifyType.End,
    "directionline" => BeamSystemJustifyType.DirectionLine,
    _ => BeamSystemJustifyType.Center,
};

var system = BeamSystem.Create(doc, profile, level, directionIndex, args.Bool("is3D", false));
system.BeamType = beamType;
system.LayoutRule = new LayoutRuleFixedDistance(Ft(args.RequireDouble("spacing")), justify);
doc.Regenerate();
var beamIds = system.GetBeamIds().Select(id => id.Value).ToList();

return new
{
    beamSystemId = system.Id.Value,
    level = level.Name,
    beamType = beamType.FamilyName + ": " + beamType.Name,
    beamCount = beamIds.Count,
    beamIds,
    spacing = args.Double("spacing"),
    directionEdge = args.Str("directionEdge", "bottom"),
    justify = args.Str("justify", "center"),
    warnings,
};
