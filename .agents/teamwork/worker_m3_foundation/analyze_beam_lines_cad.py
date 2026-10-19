import json, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Analyze Beam Lines in Block mm"):
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

        var hLines = new List<Tuple<double, double, double>>(); // y, x1, x2
        var vLines = new List<Tuple<double, double, double>>(); // x, y1, y2

        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Line ln && ln.Layer == "A_Bao_DK_S_0.2" && ln.Length >= 400.0)
            {
                double x1 = ln.StartPoint.X + shiftX;
                double y1 = ln.StartPoint.Y + shiftY;
                double x2 = ln.EndPoint.X + shiftX;
                double y2 = ln.EndPoint.Y + shiftY;

                if (Math.Abs(y1 - y2) < 5.0)
                {
                    double y = (y1 + y2) / 2.0;
                    hLines.Add(Tuple.Create(y, Math.Min(x1, x2), Math.Max(x1, x2)));
                }
                else if (Math.Abs(x1 - x2) < 5.0)
                {
                    double x = (x1 + x2) / 2.0;
                    vLines.Add(Tuple.Create(x, Math.Min(y1, y2), Math.Max(y1, y2)));
                }
            }
        }

        return new {
            totalH = hLines.Count,
            totalV = vLines.Count,
            distinctY = hLines.Select(h => Math.Round(h.Item1, 0)).Distinct().OrderBy(y => y).ToList(),
            distinctX = vLines.Select(v => Math.Round(v.Item1, 0)).Distinct().OrderBy(x => x).ToList()
        };
    """
    res = run_cad_script(code, "Inspect Beam Lines Alignment")
    print(json.dumps(res, indent=2, ensure_ascii=False))
