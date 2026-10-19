import json, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query DK Entities"):
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
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_temp.json"
    ]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode != 0:
        print("Subprocess error:", p.stderr)
        return None
    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_temp.json", "r", encoding="utf-8") as f:
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

        var dkLines = new List<object>();
        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Line ln && ent.Layer == "A_Bao_DK_S_0.2")
            {
                dkLines.Add(new {
                    x1 = Math.Round(ln.StartPoint.X + shiftX, 1),
                    y1 = Math.Round(ln.StartPoint.Y + shiftY, 1),
                    x2 = Math.Round(ln.EndPoint.X + shiftX, 1),
                    y2 = Math.Round(ln.EndPoint.Y + shiftY, 1),
                    len = Math.Round(ln.Length, 1)
                });
            }
        }

        return new {
            totalDkLinesInMm = dkLines.Count,
            sample = dkLines.Take(10).ToList()
        };
    """
    res = run_cad_script(code, "Inspect DK Lines in Block mm")
    print(json.dumps(res, indent=2, ensure_ascii=False))
