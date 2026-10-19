import json, subprocess

exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

audit_csharp = """
var level = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_Levels)
    .WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.Level>()
    .FirstOrDefault(l => l.Name == "Tầng 1");

if (level == null) throw new InvalidOperationException("Level Tầng 1 not found");

var beams = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .WhereElementIsNotElementType()
    .Where(e => e.LevelId == level.Id || e.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId() == level.Id)
    .Cast<Autodesk.Revit.DB.FamilyInstance>()
    .ToList();

int totalBeams = beams.Count;
int originJustCount = 0;
int topJustCount = 0;
int disallowJoinBothEndsCount = 0;
int elevationMinus100Count = 0;

var typeBreakdown = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

foreach (var b in beams)
{
    var typeName = b.Symbol.Name;
    typeBreakdown[typeName] = typeBreakdown.TryGetValue(typeName, out var c) ? c + 1 : 1;

    var yParam = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Y_JUSTIFICATION);
    if (yParam != null && yParam.AsInteger() == 2) originJustCount++;

    var zParam = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Z_JUSTIFICATION);
    if (zParam != null && zParam.AsInteger() == 0) topJustCount++;

    bool end0 = Autodesk.Revit.DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(b, 0) == false;
    bool end1 = Autodesk.Revit.DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(b, 1) == false;
    if (end0 && end1) disallowJoinBothEndsCount++;

    var loc = b.Location as Autodesk.Revit.DB.LocationCurve;
    if (loc?.Curve is Autodesk.Revit.DB.Line line)
    {
        double zMm = line.GetEndPoint(0).Z * 304.8;
        if (Math.Abs(zMm - (-100.0)) < 1.0) elevationMinus100Count++;
    }
}

log($"Kiem toan: {totalBeams} dam tai {level.Name}. DisallowJoin: {disallowJoinBothEndsCount}/{totalBeams}, OriginJust: {originJustCount}/{totalBeams}, TopJust: {topJustCount}/{totalBeams}, Elev -100mm: {elevationMinus100Count}/{totalBeams}.");

return new
{
    TotalBeams = totalBeams,
    Level = level.Name,
    LevelElevationMm = level.Elevation * 304.8,
    OriginJustCount = originJustCount,
    TopJustCount = topJustCount,
    DisallowJoinBothEndsCount = disallowJoinBothEndsCount,
    ElevationMinus100Count = elevationMinus100Count,
    DisallowJoinPercentage = totalBeams > 0 ? (disallowJoinBothEndsCount * 100.0 / totalBeams) : 0,
    TypeBreakdown = typeBreakdown,
    AllPassed = (totalBeams == 295 && disallowJoinBothEndsCount == 295 && topJustCount == 295 && originJustCount == 295 && elevationMinus100Count == 295)
};
"""

payload = {
    "code": audit_csharp,
    "transaction": "none",
    "dryRun": True,
    "timeoutSeconds": 60
}

with open("scratch/audit_args.json", "w", encoding="utf-8") as f:
    json.dump(payload, f)

cmd = ["python", "McpShared/tools/mcp-call.py", exe, "tools/call", "execute_dynamo_code", "@scratch/audit_args.json", "--out", "scratch/audit_result.json"]
res = subprocess.run(cmd, capture_output=True, text=True, cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
print("Audit run completed. Returncode:", res.returncode)
