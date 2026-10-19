import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Footing Geometry"):
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

        double originX = 470383.6;
        double originY = -37486.2;

        // 1. Get all footing texts
        var footings = new List<Tuple<string, double, double, string>>(); // name, x, y, handle
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is DBText dt)
            {
                string t = dt.TextString.Trim();
                if (t.StartsWith("ĐC-") || t.StartsWith("DC-") || t.StartsWith("ÑC-"))
                {
                    footings.Add(Tuple.Create(t, dt.Position.X, dt.Position.Y, dt.Handle.ToString()));
                }
            }
        }

        // 2. Also examine block mm: find its insertion point in ModelSpace
        Point3d mmPos = Point3d.Origin;
        bool hasMm = false;
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is BlockReference br)
            {
                var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                if (btr.Name == "mm")
                {
                    mmPos = br.Position;
                    hasMm = true;
                    break;
                }
            }
        }

        // 3. Collect all closed polylines from block mm transformed by mmPos
        var mmPolys = new List<Tuple<string, double, double, double, int, List<Point2d>>>(); // layer, cx, cy, area, verts, pts
        if (hasMm && bt.Has("mm"))
        {
            var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);
            foreach (ObjectId id in btrMm)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent is Polyline pl && pl.Closed)
                {
                    double cx = 0, cy = 0;
                    var pts = new List<Point2d>();
                    for (int i = 0; i < pl.NumberOfVertices; i++)
                    {
                        var p = pl.GetPoint2dAt(i);
                        double wx = p.X + mmPos.X;
                        double wy = p.Y + mmPos.Y;
                        pts.Add(new Point2d(wx, wy));
                        cx += wx;
                        cy += wy;
                    }
                    cx /= pl.NumberOfVertices;
                    cy /= pl.NumberOfVertices;
                    mmPolys.Add(Tuple.Create(pl.Layer, cx, cy, pl.Area, pl.NumberOfVertices, pts));
                }
            }
        }

        // 4. Also collect closed polylines directly in ModelSpace
        var msPolys = new List<Tuple<string, double, double, double, int, List<Point2d>>>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Polyline pl && pl.Closed)
            {
                double cx = 0, cy = 0;
                var pts = new List<Point2d>();
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    var p = pl.GetPoint2dAt(i);
                    pts.Add(p);
                    cx += p.X;
                    cy += p.Y;
                }
                cx /= pl.NumberOfVertices;
                cy /= pl.NumberOfVertices;
                msPolys.Add(Tuple.Create(pl.Layer, cx, cy, pl.Area, pl.NumberOfVertices, pts));
            }
        }

        // 5. For each footing text, find the nearest polygon from mmPolys or msPolys within 3000mm
        var matched = new List<object>();
        foreach (var ft in footings)
        {
            string name = ft.Item1;
            double fx = ft.Item2;
            double fy = ft.Item3;

            // search mmPolys
            double bestDist = double.MaxValue;
            string bestSource = "none";
            string bestLayer = "";
            double bestArea = 0;
            int bestVerts = 0;
            double bestCx = 0, bestCy = 0;

            foreach (var p in mmPolys)
            {
                double d = Math.Sqrt((p.Item2 - fx) * (p.Item2 - fx) + (p.Item3 - fy) * (p.Item3 - fy));
                if (d < bestDist)
                {
                    bestDist = d;
                    bestSource = "block_mm";
                    bestLayer = p.Item1;
                    bestArea = p.Item4;
                    bestVerts = p.Item5;
                    bestCx = p.Item2;
                    bestCy = p.Item3;
                }
            }

            foreach (var p in msPolys)
            {
                double d = Math.Sqrt((p.Item2 - fx) * (p.Item2 - fx) + (p.Item3 - fy) * (p.Item3 - fy));
                if (d < bestDist)
                {
                    bestDist = d;
                    bestSource = "modelspace";
                    bestLayer = p.Item1;
                    bestArea = p.Item4;
                    bestVerts = p.Item5;
                    bestCx = p.Item2;
                    bestCy = p.Item3;
                }
            }

            matched.Add(new {
                name = name,
                textX = Math.Round(fx - originX, 1),
                textY = Math.Round(fy - originY, 1),
                distToPoly = Math.Round(bestDist, 1),
                source = bestSource,
                polyLayer = bestLayer,
                areaM2 = Math.Round(bestArea / 1e6, 3),
                verts = bestVerts,
                polyCx = Math.Round(bestCx - originX, 1),
                polyCy = Math.Round(bestCy - originY, 1)
            });
        }

        return new {
            totalFootingTexts = footings.Count,
            totalMmClosedPolys = mmPolys.Count,
            totalMsClosedPolys = msPolys.Count,
            matchedSample = matched.Take(25).ToList()
        };
    """
    res = run_cad_script(code, "Match Footings with Polylines")
    print(json.dumps(res, indent=2, ensure_ascii=False))
