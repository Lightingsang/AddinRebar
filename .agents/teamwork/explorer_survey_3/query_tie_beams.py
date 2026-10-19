import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Tie Beams"):
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

        // Collect all texts on Foundation Plan
        double minX = originX - 15000, maxX = originX + 55000;
        double minY = originY - 15000, maxY = originY + 35000;

        var gmTexts = new List<Tuple<string, double, double>>();
        var dimTexts = new List<Tuple<string, double, double>>();

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is DBText dt)
            {
                var pt = dt.Position;
                if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                {
                    string t = dt.TextString.Trim();
                    if (t.StartsWith("GM-") || t.StartsWith("BN-"))
                    {
                        gmTexts.Add(Tuple.Create(t, pt.X, pt.Y));
                    }
                    else if (t.StartsWith("(") && t.Contains("x") && t.EndsWith(")"))
                    {
                        dimTexts.Add(Tuple.Create(t, pt.X, pt.Y));
                    }
                }
            }
        }

        // Match each GM text to the closest (bxh) text within 1500mm
        var beamList = new List<object>();
        foreach (var gm in gmTexts)
        {
            string name = gm.Item1;
            double gx = gm.Item2;
            double gy = gm.Item3;

            string matchedSection = "unknown";
            double minDist = double.MaxValue;

            foreach (var dt in dimTexts)
            {
                double d = Math.Sqrt((dt.Item2 - gx) * (dt.Item2 - gx) + (dt.Item3 - gy) * (dt.Item3 - gy));
                if (d < minDist)
                {
                    minDist = d;
                    matchedSection = dt.Item1;
                }
            }

            beamList.Add(new {
                name = name,
                x = Math.Round(gx - originX, 1),
                y = Math.Round(gy - originY, 1),
                section = minDist <= 2500 ? matchedSection : "unmatched",
                distToSection = Math.Round(minDist, 1)
            });
        }

        return beamList;
    """
    res = run_cad_script(code, "Query GM Beam Names and Sections")
    val = res.get("value", [])
    print(f"Total GM instances found on Plan: {len(val)}")
    
    # Group by name and section
    by_name = {}
    for b in val:
        n = b["name"]
        sec = b["section"]
        if n not in by_name:
            by_name[n] = []
        by_name[n].append(sec)

    print("\nSummary of GM Beams on Plan:")
    for k in sorted(by_name.keys()):
        secs = list(set(by_name[k]))
        count = len(by_name[k])
        print(f"  {k} (count={count}): sections={secs}")

    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\gm_beams_plan_summary.json", "w", encoding="utf-8") as f:
        json.dump(val, f, indent=2, ensure_ascii=False)
