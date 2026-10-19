import json, sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from extract_paired_beams import run_cad_script

def main():
    code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);

    var layers = new Dictionary<string, int>();
    var entTypes = new Dictionary<string, int>();
    foreach (ObjectId id in btrMm)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent != null)
        {
            layers[ent.Layer] = layers.GetValueOrDefault(ent.Layer, 0) + 1;
            entTypes[ent.GetType().Name] = entTypes.GetValueOrDefault(ent.GetType().Name, 0) + 1;
        }
    }
    return new { layers = layers, entTypes = entTypes };
    """
    res = run_cad_script(code, "Check Block mm Entities")
    print(json.dumps(res['value'], indent=2, ensure_ascii=False))

if __name__ == '__main__':
    main()
