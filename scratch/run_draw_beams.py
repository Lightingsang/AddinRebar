import json, subprocess

exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

# 1. Load beams from model JSON
with open("scratch/story1_grade_beams_model.json", "r", encoding="utf-8") as f:
    model_data = json.load(f)

beams_input = []
for b in model_data["beams"]:
    beams_input.append({
        "mark": b.get("Mark", ""),
        "type": b.get("TypeName", "B_200x400"),
        "sx": float(b.get("StartX", 0)),
        "sy": float(b.get("StartY", 0)),
        "ex": float(b.get("EndX", 0)),
        "ey": float(b.get("EndY", 0))
    })

print(f"Prepared {len(beams_input)} beams for execution.")

csharp_code = """
var levelName = args.Str("levelName", "Tầng 1");
var familyName = args.Str("familyName", "1_STR_Framing_ConcreteRectangular");
var beamList = args.List("beams");

var level = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_Levels)
    .WhereElementIsNotElementType()
    .Cast<Autodesk.Revit.DB.Level>()
    .FirstOrDefault(l => l.Name == levelName);

if (level == null)
{
    throw new InvalidOperationException($"Level '{levelName}' not found in document.");
}

var symbols = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfClass(typeof(Autodesk.Revit.DB.FamilySymbol))
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .Cast<Autodesk.Revit.DB.FamilySymbol>()
    .Where(s => s.FamilyName.Contains("ConcreteRectangular"))
    .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

var oldBeams = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .WhereElementIsNotElementType()
    .Where(e => e.LevelId == level.Id || e.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId() == level.Id)
    .Select(e => e.Id)
    .ToList();

int deletedOld = 0;
foreach (var oid in oldBeams)
{
    doc.Delete(oid);
    deletedOld++;
}

int createdCount = 0;
var createdIds = new List<long>();
var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

const double MM_TO_FT = 1.0 / 304.8;

foreach (var b in beamList)
{
    var mark = b.Str("mark", "");
    var type = b.Str("type", "B_200x400");
    var sx = b.Double("sx") * MM_TO_FT;
    var sy = b.Double("sy") * MM_TO_FT;
    var ex = b.Double("ex") * MM_TO_FT;
    var ey = b.Double("ey") * MM_TO_FT;

    if (!symbols.TryGetValue(type, out var symbol))
    {
        log($"Warning: Symbol '{type}' not found for mark {mark}");
        continue;
    }

    if (!symbol.IsActive)
    {
        symbol.Activate();
    }

    var p1 = new Autodesk.Revit.DB.XYZ(sx, sy, 0.0);
    var p2 = new Autodesk.Revit.DB.XYZ(ex, ey, 0.0);
    var line = Autodesk.Revit.DB.Line.CreateBound(p1, p2);

    var inst = doc.Create.NewFamilyInstance(line, symbol, level, Autodesk.Revit.DB.Structure.StructuralType.Beam);

    var yJust = inst.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Y_JUSTIFICATION);
    if (yJust != null && !yJust.IsReadOnly) yJust.Set(2);

    var zJust = inst.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Z_JUSTIFICATION);
    if (zJust != null && !zJust.IsReadOnly) zJust.Set(0);

    var mParam = inst.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.ALL_MODEL_MARK);
    if (mParam != null && !mParam.IsReadOnly) mParam.Set(mark);

    Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(inst, 0);
    Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(inst, 1);

    long idVal = inst.Id.Value;
    createdIds.Add(idVal);
    createdCount++;

    typeCounts[type] = typeCounts.TryGetValue(type, out var c) ? c + 1 : 1;
}

log($"Dung thanh cong {createdCount}/{beamList.Count} dam da kieng tai {levelName}. Da xoa {deletedOld} dam cu.");

return new
{
    status = "SUCCESS",
    createdCount,
    totalInput = beamList.Count,
    deletedOld,
    level = levelName,
    topElevationMm = -100.0,
    typeCounts,
    firstIds = createdIds.Take(10).ToList()
};
"""

def run_script(dry_run, beam_slice=None):
    beams_to_run = beams_input if beam_slice is None else beams_input[:beam_slice]
    payload = {
        "code": csharp_code,
        "timeoutSeconds": 300,
        "args": {
            "levelName": "Tầng 1",
            "familyName": "1_STR_Framing_ConcreteRectangular",
            "beams": beams_to_run
        },
        "dryRun": dry_run,
        "transaction": "auto"
    }
    
    args_file = "scratch/draw_beams_args.json"
    out_file = f"scratch/draw_beams_out_{'dryrun' if dry_run else 'live'}.json"
    
    with open(args_file, "w", encoding="utf-8") as f:
        json.dump(payload, f)
        
    cmd = ["python", "McpShared/tools/mcp-call.py", exe, "tools/call", "execute_dynamo_code", f"@{args_file}", "--out", out_file]
    res = subprocess.run(cmd, capture_output=True, text=True, cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
    print(f"Executed dryRun={dry_run} ({len(beams_to_run)} beams). Returncode: {res.returncode}")
    return out_file

if __name__ == "__main__":
    print("--- RUNNING LIVE CREATION FOR ALL 295 BEAMS ---")
    out_file = run_script(dry_run=False, beam_slice=None)
    with open(out_file, "r", encoding="utf-8") as f:
        data = json.load(f)
    print("Execution finished. Output saved to", out_file)
