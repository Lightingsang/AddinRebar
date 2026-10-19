import subprocess, json

exe = r"HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

csharp_code = """
var collector = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .WhereElementIsNotElementType()
    .ToElements();

var tang1 = collector
    .Where(e => (e.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsValueString() ?? "") == "Tầng 1")
    .Cast<Autodesk.Revit.DB.FamilyInstance>()
    .ToList();

var symbols = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfClass(typeof(Autodesk.Revit.DB.FamilySymbol))
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .Cast<Autodesk.Revit.DB.FamilySymbol>()
    .Where(s => s.FamilyName.Contains("ConcreteRectangular"))
    .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

if (!symbols.TryGetValue("B_100x300", out var sym100x300))
{
    var baseSym = symbols.Values.First();
    sym100x300 = (Autodesk.Revit.DB.FamilySymbol)baseSym.Duplicate("B_100x300");
    var pB = sym100x300.LookupParameter("b") ?? sym100x300.LookupParameter("Width");
    var pH = sym100x300.LookupParameter("h") ?? sym100x300.LookupParameter("Height");
    if (pB != null) pB.Set(100.0 / 304.8);
    if (pH != null) pH.Set(300.0 / 304.8);
}
if (!sym100x300.IsActive) sym100x300.Activate();

if (!symbols.TryGetValue("B_200x300", out var sym200x300))
{
    var baseSym = symbols.Values.First();
    sym200x300 = (Autodesk.Revit.DB.FamilySymbol)baseSym.Duplicate("B_200x300");
    var pB = sym200x300.LookupParameter("b") ?? sym200x300.LookupParameter("Width");
    var pH = sym200x300.LookupParameter("h") ?? sym200x300.LookupParameter("Height");
    if (pB != null) pB.Set(200.0 / 304.8);
    if (pH != null) pH.Set(300.0 / 304.8);
}
if (!sym200x300.IsActive) sym200x300.Activate();

int countDka = 0;
int countDkap = 0;
int countDkb = 0;

var updatedRecords = new List<object>();

const double COTE_MINUS_1M_OFFSET_FT = -900.0 / 304.8;

foreach (var b in tang1)
{
    var mark = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
    string oldType = b.Symbol.Name;
    string newType = null;
    double targetOffsetFt = 0.0;
    bool isDkap = false;

    if (mark.Contains("DK.a'"))
    {
        newType = "B_100x300";
        targetOffsetFt = COTE_MINUS_1M_OFFSET_FT;
        isDkap = true;
    }
    else if (mark.Contains("DK.a"))
    {
        newType = "B_100x300";
    }
    else if (mark.Contains("DK.b"))
    {
        newType = "B_200x300";
    }

    if (newType != null)
    {
        var targetSymbol = newType == "B_100x300" ? sym100x300 : sym200x300;
        if (b.GetTypeId() != targetSymbol.Id)
        {
            b.ChangeTypeId(targetSymbol.Id);
        }

        if (isDkap)
        {
            var pEnd0 = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION);
            if (pEnd0 != null && !pEnd0.IsReadOnly) pEnd0.Set(targetOffsetFt);

            var pEnd1 = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION);
            if (pEnd1 != null && !pEnd1.IsReadOnly) pEnd1.Set(targetOffsetFt);

            var pZOff = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Z_OFFSET_VALUE);
            if (pZOff != null && !pZOff.IsReadOnly) pZOff.Set(targetOffsetFt);
            
            countDkap++;
        }
        else if (mark.Contains("DK.a"))
        {
            countDka++;
        }
        else if (mark.Contains("DK.b"))
        {
            countDkb++;
        }

        Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(b, 0);
        Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(b, 1);

        updatedRecords.Add(new {
            id = b.Id.Value,
            mark = mark,
            oldType = oldType,
            newType = newType,
            zOffsetMm = Math.Round(targetOffsetFt * 304.8, 1)
        });
    }
}

log($"Da cap nhat {updatedRecords.Count} dam: DK.a={countDka}, DK.a'={countDkap}, DK.b={countDkb}.");

return new {
    status = "SUCCESS",
    totalUpdated = updatedRecords.Count,
    countDka = countDka,
    countDkap = countDkap,
    countDkb = countDkb,
    records = updatedRecords
};
"""

def execute(dry_run):
    proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def send(msg):
        proc.stdin.write((json.dumps(msg) + "\n").encode('utf-8'))
        proc.stdin.flush()
        while True:
            line = proc.stdout.readline()
            if not line: break
            try:
                data = json.loads(line.decode('utf-8'))
                if data.get("id") == msg.get("id"):
                    return data
            except:
                pass

    send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0"}}})
    res = send({
        "jsonrpc": "2.0",
        "id": 2,
        "method": "tools/call",
        "params": {
            "name": "execute_dynamo_code",
            "arguments": {
                "code": csharp_code,
                "dryRun": dry_run,
                "timeoutSeconds": 180
            }
        }
    })
    proc.terminate()
    return res

if __name__ == "__main__":
    print("--- 2. RUNNING LIVE COMMIT (dryRun = False) ---")
    res_live = execute(dry_run=False)
    raw = res_live["result"]["content"][0]["text"]
    data = json.loads(raw)
    print("Live execution result:")
    print("  IsError:", data.get("isError"))
    print("  Message:", data.get("message"))
    print("  Logs:", data.get("logs"))
    if not data.get("isError"):
        val = data.get("value")
        print("  Status:", val.get("status"))
        print("  Total updated:", val.get("totalUpdated"))
        print("  DK.a:", val.get("countDka"))
        print("  DK.a':", val.get("countDkap"))
        print("  DK.b:", val.get("countDkb"))
        with open("scratch/beam_updates_live_result.json", "w", encoding="utf-8") as f:
            json.dump(val, f, indent=2, ensure_ascii=False)
        print("Saved result to scratch/beam_updates_live_result.json")
