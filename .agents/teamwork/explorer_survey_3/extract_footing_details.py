import json, os, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Detailed Foundation Data"):
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
    # Query exact footing detail titles, dimensions and cote blocks in the detail area
    code = """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        // Scan around Detail Footing area: X: 540000..630000, Y: -65000..-10000
        double minX = 535000, maxX = 640000;
        double minY = -67000, maxY = -8000;

        var texts = new List<object>();
        var dims = new List<object>();
        var cotes = new List<object>();

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null) continue;

            if (ent is DBText dt)
            {
                var pt = dt.Position;
                if (pt.X >= minX && pt.X <= maxX && pt.Y >= minY && pt.Y <= maxY)
                {
            string t = dt.TextString.Trim();
            if (t.Length > 0)
            {
                texts.Add(new {
                    text = t,
                    layer = dt.Layer,
                    x = Math.Round(pt.X, 1),
                    y = Math.Round(pt.Y, 1)
                });
            }
        }
    }

    return new {
        totalTexts = texts.Count,
        totalDims = dims.Count,
        totalCotes = cotes.Count,
        cotes = cotes,
        allTexts = texts
    };
    """
    res = run_cad_script(code, "Query Footing Detail Region")
    print(f"Result summary: totalTexts={res.get('value', {}).get('totalTexts')}, totalDims={res.get('value', {}).get('totalDims')}, totalCotes={res.get('value', {}).get('totalCotes')}")
    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\footing_details_raw.json", "w", encoding="utf-8") as f:
        json.dump(res.get("value", {}), f, indent=2, ensure_ascii=False)
    print("Wrote footing_details_raw.json")
