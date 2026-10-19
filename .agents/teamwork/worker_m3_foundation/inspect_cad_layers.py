import json, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8")

def run_cad_script(code, label="Query Beams"):
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
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        // Find layers in ModelSpace and in Block mm
        var msLayers = new Dictionary<string, int>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent != null)
            {
                msLayers[ent.Layer] = msLayers.GetValueOrDefault(ent.Layer) + 1;
            }
        }

        var btrMmLayers = new Dictionary<string, int>();
        if (bt.Has("mm"))
        {
            var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);
            foreach (ObjectId id in btrMm)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent != null)
                {
                    btrMmLayers[ent.Layer] = btrMmLayers.GetValueOrDefault(ent.Layer) + 1;
                }
            }
        }

        return new {
            msLayers = msLayers.OrderByDescending(k => k.Value).Take(20).ToDictionary(k => k.Key, k => k.Value),
            mmLayers = btrMmLayers
        };
    """
    res = run_cad_script(code, "Inspect CAD Layers")
    print(json.dumps(res, indent=2, ensure_ascii=False))
