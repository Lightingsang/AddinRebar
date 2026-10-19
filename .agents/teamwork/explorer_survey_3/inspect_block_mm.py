import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Block mm Entities"):
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

        var lineLayers = new Dictionary<string, int>();
        var polyLayers = new Dictionary<string, int>();

        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Line ln)
            {
                if (!lineLayers.ContainsKey(ln.Layer)) lineLayers[ln.Layer] = 0;
                lineLayers[ln.Layer]++;
            }
            else if (ent is Polyline pl)
            {
                if (!polyLayers.ContainsKey(pl.Layer)) polyLayers[pl.Layer] = 0;
                polyLayers[pl.Layer]++;
            }
        }

        // Also check layer MONG in block mm: sample entities
        var mongEntities = new List<object>();
        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent.Layer == "MONG")
            {
                if (ent is Polyline pl)
                {
                    mongEntities.Add(new {
                        type = "Polyline",
                        closed = pl.Closed,
                        verts = pl.NumberOfVertices,
                        area = pl.Area,
                        len = pl.Length
                    });
                }
                else if (ent is Line ln)
                {
                    mongEntities.Add(new {
                        type = "Line",
                        len = ln.Length,
                        p1 = new { x = ln.StartPoint.X, y = ln.StartPoint.Y },
                        p2 = new { x = ln.EndPoint.X, y = ln.EndPoint.Y }
                    });
                }
            }
        }

        return new {
            lineLayers = lineLayers,
            polyLayers = polyLayers,
            mongEntitiesSample = mongEntities.Take(15).ToList()
        };
    """
    res = run_cad_script(code, "Inspect Block mm details")
    print(json.dumps(res, indent=2, ensure_ascii=False))
