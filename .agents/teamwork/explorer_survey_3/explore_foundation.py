import json, os, subprocess, sys

def run_cad_script(code, label="Query Foundation"):
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
        
        var summary = new List<string>();
        int totalPl = 0;
        var layerMap = new Dictionary<string, int>();

        foreach (ObjectId id in btrMm)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Polyline pl)
            {
                totalPl++;
                string l = pl.Layer;
                if (!layerMap.ContainsKey(l)) layerMap[l] = 0;
                layerMap[l]++;
            }
        }

        // Also find all BlockReferences of block "mm" in ModelSpace
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        var mmRefs = new List<object>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is BlockReference br)
            {
                var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                if (btr.Name == "mm")
                {
                    mmRefs.Add(new {
                        handle = br.Handle.ToString(),
                        x = br.Position.X,
                        y = br.Position.Y,
                        z = br.Position.Z,
                        scaleX = br.ScaleFactors.X,
                        scaleY = br.ScaleFactors.Y,
                        rotation = br.Rotation,
                        layer = br.Layer
                    });
                }
            }
        }

        return new {
            totalPlInBlock = totalPl,
            layerMap = layerMap,
            mmReferencesInModelSpace = mmRefs
        };
    """
    res = run_cad_script(code, "Inspect Block mm details")
    print(json.dumps(res, indent=2, ensure_ascii=False))
