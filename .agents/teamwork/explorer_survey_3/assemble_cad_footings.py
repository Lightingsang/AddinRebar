import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Assemble CAD Footings"):
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
        r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\cad_result_temp.json"
    ]
    p = subprocess.run(cmd, capture_output=True, text=True)
    if p.returncode != 0:
        print("Subprocess error:", p.stderr)
        return None
    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\cad_result_temp.json", "r", encoding="utf-8") as f:
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

        // Find all lines on layer MONG
        var mongLines = new List<Tuple<Point2d, Point2d>>();
        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Line ln && ln.Layer == "MONG")
            {
                mongLines.Add(Tuple.Create(new Point2d(ln.StartPoint.X, ln.StartPoint.Y), new Point2d(ln.EndPoint.X, ln.EndPoint.Y)));
            }
        }

        // Check bounding box of all mongLines
        double minX = mongLines.Min(l => Math.Min(l.Item1.X, l.Item2.X));
        max_x = mongLines.Max(l => Math.Max(l.Item1.X, l.Item2.X));
        double minY = mongLines.Min(l => Math.Min(l.Item1.Y, l.Item2.Y));
        double maxY = mongLines.Max(l => Math.Max(l.Item1.Y, l.Item2.Y));

        return new {
            totalMongLines = mongLines.Count,
            bbox = new { minX, max_x, minY, maxY }
        };
    """
    # Fix variable definition
    code = code.replace("max_x =", "double maxX =")
    code = code.replace("max_x", "maxX")
    res = run_cad_script(code, "Query MONG bbox")
    print(json.dumps(res, indent=2, ensure_ascii=False))
