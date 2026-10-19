import json, subprocess, sys, math

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Extract Beam Centerlines"):
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
            if (ent is Line ln && ln.Layer == "A_Bao_DK_S_0.2" && ln.Length >= 300.0)
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

        // Pair horizontal lines with width between 180 and 350
        var hBeams = new List<object>();
        for (int i = 0; i < hLines.Count; i++)
        {
            for (int j = i + 1; j < hLines.Count; j++)
            {
                double dy = Math.Abs(hLines[i].Item1 - hLines[j].Item1);
                if (dy >= 180.0 && dy <= 350.0)
                {
                    double overlapX1 = Math.Max(hLines[i].Item2, hLines[j].Item2);
                    double overlapX2 = Math.Min(hLines[i].Item3, hLines[j].Item3);
                    if (overlapX2 - overlapX1 >= 400.0)
                    {
                        double centerY = (hLines[i].Item1 + hLines[j].Item1) / 2.0;
                        hBeams.Add(new {
                            dir = "X",
                            y = Math.Round(centerY, 1),
                            x1 = Math.Round(overlapX1, 1),
                            x2 = Math.Round(overlapX2, 1),
                            len = Math.Round(overlapX2 - overlapX1, 1),
                            width = Math.Round(dy, 1)
                        });
                    }
                }
            }
        }

        // Pair vertical lines
        var vBeams = new List<object>();
        for (int i = 0; i < vLines.Count; i++)
        {
            for (int j = i + 1; j < vLines.Count; j++)
            {
                double dx = Math.Abs(vLines[i].Item1 - vLines[j].Item1);
                if (dx >= 180.0 && dx <= 350.0)
                {
                    double overlapY1 = Math.Max(vLines[i].Item2, vLines[j].Item2);
                    double overlapY2 = Math.Min(vLines[i].Item3, vLines[j].Item3);
                    if (overlapY2 - overlapY1 >= 400.0)
                    {
                        double centerX = (vLines[i].Item1 + vLines[j].Item1) / 2.0;
                        vBeams.Add(new {
                            dir = "Y",
                            x = Math.Round(centerX, 1),
                            y1 = Math.Round(overlapY1, 1),
                            y2 = Math.Round(overlapY2, 1),
                            len = Math.Round(overlapY2 - overlapY1, 1),
                            width = Math.Round(dx, 1)
                        });
                    }
                }
            }
        }

        return new {
            totalHBeams = hBeams.Count,
            totalVBeams = vBeams.Count,
            sampleH = hBeams.Take(5).ToList(),
            sampleV = vBeams.Take(5).ToList()
        };
    """
    res = run_cad_script(code, "Extract Beam Centerlines")
    print(json.dumps(res, indent=2, ensure_ascii=False))
