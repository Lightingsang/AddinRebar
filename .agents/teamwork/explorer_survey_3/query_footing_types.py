import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Footing Types"):
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
    # Query details of each footing type from the detail drawing area (X: 540000..610000, Y: -65000..-15000)
    # Specifically looking for titles: "MAËT BAÈNG ÑAØI COÏC ÑC-1", "ÑC-2", etc.
    code = """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        var titles = new List<Tuple<string, double, double>>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is DBText dt)
            {
                string t = dt.TextString.Trim();
                if (t.Contains("ÑAØI COÏC") || t.Contains("ĐÀI CỌC") || t.Contains("MAËT CAÉT"))
                {
                    titles.Add(Tuple.Create(t, dt.Position.X, dt.Position.Y));
                }
            }
        }

        // For each title, collect dimensions and text within dx in [-10000, 10000], dy in [-8000, 8000]
        var details = new List<object>();
        foreach (var t in titles)
        {
            string title = t.Item1;
            double tx = t.Item2;
            double ty = t.Item3;

            var localDims = new List<double>();
            var localTexts = new List<string>();

            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent is Dimension dim)
                {
                    var p = dim.TextPosition;
                    if (Math.Abs(p.X - tx) <= 8000 && Math.Abs(p.Y - ty) <= 8000)
                    {
                        localDims.Add(Math.Round(dim.Measurement, 1));
                    }
                }
                else if (ent is DBText dt)
                {
                    var p = dt.Position;
                    if (Math.Abs(p.X - tx) <= 8000 && Math.Abs(p.Y - ty) <= 8000)
                    {
                        string s = dt.TextString.Trim();
                        if (s.Contains("%%C") || s.Contains("d=") || s.Contains("D=") || s.Contains("COTE") || s.Contains("L="))
                        {
                            localTexts.Add(s);
                        }
                    }
                }
            }

            details.Add(new {
                title = title,
                x = Math.Round(tx, 1),
                y = Math.Round(ty, 1),
                dims = localDims.Distinct().OrderBy(d => d).ToList(),
                annotations = localTexts.Distinct().ToList()
            });
        }

        return details;
    """
    res = run_cad_script(code, "Query Footing Type Details")
    print(json.dumps(res, indent=2, ensure_ascii=False))
