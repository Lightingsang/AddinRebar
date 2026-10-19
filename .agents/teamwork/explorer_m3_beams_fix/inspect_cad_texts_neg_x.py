import json, sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from extract_paired_beams import run_cad_script

def main():
    code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

    double originX = 470383.6;
    double originY = -37486.2;

    var texts = new List<object>();

    foreach (ObjectId id in ms)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent is DBText dt)
        {
            double rx = dt.Position.X - originX;
            double ry = dt.Position.Y - originY;
            if (rx < 2000 && rx > -8000 && ry > -2000 && ry < 30000)
            {
                texts.Add(new {
                    text = dt.TextString,
                    rx = Math.Round(rx, 1),
                    ry = Math.Round(ry, 1),
                    h = Math.Round(dt.Height, 1)
                });
            }
        }
        else if (ent is MText mt)
        {
            double rx = mt.Location.X - originX;
            double ry = mt.Location.Y - originY;
            if (rx < 2000 && rx > -8000 && ry > -2000 && ry < 30000)
            {
                texts.Add(new {
                    text = mt.Contents,
                    rx = Math.Round(rx, 1),
                    ry = Math.Round(ry, 1),
                    h = Math.Round(mt.TextHeight, 1)
                });
            }
        }
    }
    return texts;
    """
    res = run_cad_script(code, "Inspect CAD texts at negative X")
    print(f"Total texts near west / negative X: {len(res['value'])}")
    for t in res['value']:
        print(t)

if __name__ == '__main__':
    main()
