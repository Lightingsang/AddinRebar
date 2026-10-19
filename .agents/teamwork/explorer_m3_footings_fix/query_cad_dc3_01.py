import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import subprocess

def run_cad_script(code, label="Query DC-3-01 in CAD"):
    req_args = {
        "code": code,
        "transaction": "none",
        "dryRun": False,
        "timeoutSeconds": 60,
        "label": label
    }
    cmd = [
        sys.executable,
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\tools\mcp-call.py",
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe",
        "tools/call",
        "execute_autocad_code",
        json.dumps(req_args),
        "--out",
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_footings_fix\cad_temp_dc3_01.json"
    ]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode != 0:
        print("Subprocess error:", p.stderr)
        return None
    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_footings_fix\cad_temp_dc3_01.json", "r", encoding="utf-8") as f:
        data = json.load(f)
    if "result" in data and "content" in data["result"]:
        txt = data["result"]["content"][0]["text"]
        try:
            return json.loads(txt)
        except:
            return txt
    return data

if __name__ == "__main__":
    code = """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);

        double shiftX = 49268.55;
        double shiftY = 4399.98;

        // Query lines around DC-3-01: Revit X in [5500, 8200], Y in [-1500, 1500]
        var ents = new List<object>();
        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Line ln && ln.Layer == "MONG")
            {
                double x1 = ln.StartPoint.X + shiftX;
                double y1 = ln.StartPoint.Y + shiftY;
                double x2 = ln.EndPoint.X + shiftX;
                double y2 = ln.EndPoint.Y + shiftY;

                if (Math.Min(x1, x2) >= 5500 && Math.Max(x1, x2) <= 8200 &&
                    Math.Min(y1, y2) >= -1500 && Math.Max(y1, y2) <= 1500)
                {
                    ents.Add(new {
                        type = "Line",
                        p1 = new { x = Math.Round(x1, 1), y = Math.Round(y1, 1) },
                        p2 = new { x = Math.Round(x2, 1), y = Math.Round(y2, 1) },
                        len = Math.Round(ln.Length, 1)
                    });
                }
            }
        }

        return new { count = ents.Count, ents = ents };
    """
    res = run_cad_script(code, "Query MONG lines around DC-3-01")
    print(json.dumps(res, indent=2, ensure_ascii=False))
