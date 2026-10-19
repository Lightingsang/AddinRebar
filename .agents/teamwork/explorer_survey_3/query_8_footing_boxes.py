import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Footing Detail Boxes"):
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
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        var centers = new List<Tuple<string, double, double>>
        {
            Tuple.Create("DC-1", 548584.0, -31504.5),
            Tuple.Create("DC-2", 548826.0, -62478.5),
            Tuple.Create("DC-2'", 566988.4, -32292.5),
            Tuple.Create("DC-3", 567254.5, -62622.2),
            Tuple.Create("DC-3'", 604184.9, -62992.7),
            Tuple.Create("DC-4", 585772.0, -33586.2),
            Tuple.Create("DC-5", 586515.7, -63012.6),
            Tuple.Create("DC-6", 604257.8, -33377.2)
        };

        var results = new List<object>();

        foreach (var c in centers)
        {
            string name = c.Item1;
            double cx = c.Item2;
            double cy = c.Item3;

            var dims = new List<double>();
            var texts = new List<string>();
            var polys = new List<object>();
            int circleCount = 0; // count piles (circles D=400)

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null) continue;

                if (ent is Dimension dim)
                {
                    var p = dim.TextPosition;
                    if (Math.Abs(p.X - cx) <= 5000 && Math.Abs(p.Y - cy) <= 5000)
                    {
                        dims.Add(Math.Round(dim.Measurement, 1));
                    }
                }
                else if (ent is DBText dt)
                {
                    var p = dt.Position;
                    if (Math.Abs(p.X - cx) <= 5000 && Math.Abs(p.Y - cy) <= 5000)
                    {
                        string s = dt.TextString.Trim();
                        if (s.Length > 0) texts.Add(s);
                    }
                }
                else if (ent is Circle cir)
                {
                    var p = cir.Center;
                    if (Math.Abs(p.X - cx) <= 5000 && Math.Abs(p.Y - cy) <= 5000)
                    {
                        if (Math.Abs(cir.Radius * 2 - 400) <= 20 || Math.Abs(cir.Radius * 2 - 350) <= 20)
                        {
                            circleCount++;
                        }
                    }
                }
                else if (ent is Polyline pl && pl.Closed)
                {
                    if (Math.Abs(pl.StartPoint.X - cx) <= 5000 && Math.Abs(pl.StartPoint.Y - cy) <= 5000)
                    {
                        if (pl.Area >= 0.5e6 && pl.Area <= 25e6)
                        {
                            polys.Add(new {
                                layer = pl.Layer,
                                areaM2 = Math.Round(pl.Area / 1e6, 3),
                                verts = pl.NumberOfVertices
                            });
                        }
                    }
                }
            }

            results.Add(new {
                name = name,
                centerX = cx,
                centerY = cy,
                circleCount = circleCount,
                dims = dims.Distinct().OrderBy(d => d).ToList(),
                polys = polys,
                textsSample = texts.Take(10).ToList()
            });
        }

        return results;
    """
    res = run_cad_script(code, "Query 8 Footing Detail Boxes")
    print(json.dumps(res, indent=2, ensure_ascii=False))
