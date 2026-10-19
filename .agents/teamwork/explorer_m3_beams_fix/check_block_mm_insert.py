import json, sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from extract_paired_beams import run_cad_script

def main():
    code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

    var inserts = new List<object>();
    foreach (ObjectId id in ms)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent is BlockReference br && br.Name == "mm")
        {
            inserts.Add(new {
                pos = new { x = br.Position.X, y = br.Position.Y, z = br.Position.Z },
                rot = br.Rotation,
                scale = new { x = br.ScaleFactors.X, y = br.ScaleFactors.Y, z = br.ScaleFactors.Z }
            });
        }
    }
    return inserts;
    """
    res = run_cad_script(code, "Find Block mm Inserts")
    print(json.dumps(res['value'], indent=2, ensure_ascii=False))

if __name__ == '__main__':
    main()
