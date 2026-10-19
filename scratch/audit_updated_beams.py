import subprocess, json

exe = r"HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

csharp = """
var collector = new Autodesk.Revit.DB.FilteredElementCollector(doc)
    .OfCategory(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming)
    .WhereElementIsNotElementType()
    .ToElements();

var tang1 = collector
    .Where(e => (e.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsValueString() ?? "") == "Tầng 1")
    .Cast<Autodesk.Revit.DB.FamilyInstance>()
    .ToList();

int totalTang1 = tang1.Count;

var typeBreakdown = tang1
    .GroupBy(e => e.Symbol.Name)
    .ToDictionary(g => g.Key, g => g.Count());

int dkaCount = 0;
int dkaCorrectType = 0;

int dkapCount = 0;
int dkapCorrectType = 0;
int dkapCorrectOffset = 0;

int dkbCount = 0;
int dkbCorrectType = 0;

int disallowJoinBothEndsCount = 0;

foreach (var b in tang1)
{
    var mark = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
    string tName = b.Symbol.Name;

    bool end0 = !Autodesk.Revit.DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(b, 0);
    bool end1 = !Autodesk.Revit.DB.Structure.StructuralFramingUtils.IsJoinAllowedAtEnd(b, 1);
    if (end0 && end1) disallowJoinBothEndsCount++;

    if (mark.Contains("DK.a'"))
    {
        dkapCount++;
        if (tName == "B_100x300") dkapCorrectType++;
        var pZOff = b.get_Parameter(Autodesk.Revit.DB.BuiltInParameter.Z_OFFSET_VALUE);
        double zMm = pZOff != null ? Math.Round(pZOff.AsDouble() * 304.8, 1) : 0.0;
        if (Math.Abs(zMm - (-900.0)) < 1.0) dkapCorrectOffset++;
    }
    else if (mark.Contains("DK.a"))
    {
        dkaCount++;
        if (tName == "B_100x300") dkaCorrectType++;
    }
    else if (mark.Contains("DK.b"))
    {
        dkbCount++;
        if (tName == "B_200x300") dkbCorrectType++;
    }
}

return new {
    totalTang1 = totalTang1,
    disallowJoinCount = disallowJoinBothEndsCount,
    dka = new { total = dkaCount, correct = dkaCorrectType },
    dkap = new { total = dkapCount, correct = dkapCorrectType, correctOffset = dkapCorrectOffset },
    dkb = new { total = dkbCount, correct = dkbCorrectType },
    typeBreakdown = typeBreakdown
};
"""

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
            "code": csharp,
            "dryRun": True,
            "timeoutSeconds": 30
        }
    }
})
proc.terminate()

raw = res["result"]["content"][0]["text"]
data = json.loads(raw)
val = data.get("value")
print("=== AUDIT RESULTS ===")
print(json.dumps(val, indent=2))
with open("scratch/final_audit_results.json", "w", encoding="utf-8") as f:
    json.dump(val, f, indent=2)
