import json, subprocess

exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

csharp_code = """
var level = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_Levels)
    .WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.Level>()
    .FirstOrDefault(l => l.Name == "Tầng 1");

if (level == null) throw new InvalidOperationException("Level Tầng 1 not found");

// Find all 295 beams currently at ±0.000 or Tầng 1
var beams = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.FamilyInstance>()
    .Where(b => b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsValueString() != "Đỉnh Đài Móng")
    .ToList();

int updatedCount = 0;
const double TARGET_Z_FT = -100.0 / 304.8;

foreach (var b in beams)
{
    // 1. Set Reference Level
    var refParam = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
    if (refParam != null && !refParam.IsReadOnly && refParam.AsElementId() != level.Id)
    {
        refParam.Set(level.Id);
    }

    // 2. Set Curve Z to -100mm
    var loc = b.Location as Autodesk.Revit.DB.LocationCurve;
    if (loc?.Curve is Autodesk.Revit.DB.Line line)
    {
        var p0 = line.GetEndPoint(0);
        var p1 = line.GetEndPoint(1);
        if (Math.Abs(p0.Z - TARGET_Z_FT) > 0.001 || Math.Abs(p1.Z - TARGET_Z_FT) > 0.001)
        {
            var newLine = Autodesk.Revit.DB.Line.CreateBound(
                new Autodesk.Revit.DB.XYZ(p0.X, p0.Y, TARGET_Z_FT),
                new Autodesk.Revit.DB.XYZ(p1.X, p1.Y, TARGET_Z_FT));
            loc.Curve = newLine;
        }
    }

    // 3. Justifications
    var yJust = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Y_JUSTIFICATION);
    if (yJust != null && !yJust.IsReadOnly) yJust.Set(2); // Origin

    var zJust = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Z_JUSTIFICATION);
    if (zJust != null && !zJust.IsReadOnly) zJust.Set(0); // Top

    // 4. Disallow Join
    Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(b, 0);
    Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(b, 1);

    updatedCount++;
}

log($"Cap nhat thanh cong {updatedCount} dam sang Level 'Tầng 1' voi cao do dinh -100mm.");

return new
{
    Status = "SUCCESS",
    UpdatedCount = updatedCount,
    Level = level.Name,
    TargetElevationMm = -100.0
};
"""

payload = {
    "code": csharp_code,
    "transaction": "auto",
    "dryRun": False,
    "timeoutSeconds": 300
}

with open("scratch/update_beams_args.json", "w", encoding="utf-8") as f:
    json.dump(payload, f)

cmd = ["python", "McpShared/tools/mcp-call.py", exe, "tools/call", "execute_dynamo_code", "@scratch/update_beams_args.json", "--out", "scratch/update_beams_out.json"]
res = subprocess.run(cmd, capture_output=True, text=True, cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
print("Update run completed. Returncode:", res.returncode)
