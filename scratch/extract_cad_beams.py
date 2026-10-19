import subprocess, json

exe = r"HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe"

# C# script to find all beam marks, notes, and their locations in the drawing
code = """
var results = new List<object>();
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

foreach (ObjectId id in ms)
{
    var ent = tr.GetObject(id, OpenMode.ForRead);
    string txt = null;
    Point3d pos = Point3d.Origin;
    double rot = 0;
    string layer = (ent as Entity)?.Layer ?? "";

    if (ent is DBText dbt)
    {
        txt = dbt.TextString;
        pos = dbt.Position;
        rot = dbt.Rotation;
    }
    else if (ent is MText mt)
    {
        txt = mt.Text;
        pos = mt.Location;
        rot = mt.Rotation;
    }

    if (!string.IsNullOrWhiteSpace(txt))
    {
        string t = txt.Trim();
        if (t.Contains("DK.a") || t.Contains("DK.b") || t.Contains("DK.a'") || 
            t.StartsWith("DK") || t.Contains("100x300") || t.Contains("200x300") ||
            t.Contains("300x300") || t.Contains("200x350") || t.Contains("COTE -1.000") ||
            t.Contains("TIE") || t.Contains("Tie"))
        {
            results.Add(new {
                text = t,
                x = Math.Round(pos.X, 1),
                y = Math.Round(pos.Y, 1),
                rot = Math.Round(rot, 2),
                layer = layer
            });
        }
    }
}

return System.Text.Json.JsonSerializer.Serialize(results);
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
res = send({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {"name": "execute_autocad_code", "arguments": {"code": code}}})

raw_json = res["result"]["content"][0]["text"]
data = json.loads(raw_json)
if data.get("isError"):
    print("Error:", data.get("message"))
    for d in data.get("diagnostics", []):
        print(d)
else:
    val = json.loads(data["value"])
    print(f"Total found entities: {len(val)}")
    with open("scratch/cad_beam_texts.json", "w", encoding="utf-8") as f:
        json.dump(val, f, indent=2, ensure_ascii=False)
    print("Saved to scratch/cad_beam_texts.json")

proc.terminate()
