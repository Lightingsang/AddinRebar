import json, sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from extract_paired_beams import run_cad_script

def main():
    code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);

    double shiftX = 49268.55;
    double shiftY = 4399.98;

    var negEntities = new List<object>();

    foreach (ObjectId id in btrMm)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent is Line ln)
        {
            double x1 = ln.StartPoint.X + shiftX;
            double y1 = ln.StartPoint.Y + shiftY;
            double x2 = ln.EndPoint.X + shiftX;
            double y2 = ln.EndPoint.Y + shiftY;

            if (x1 < 0 || x2 < 0)
            {
                negEntities.Add(new {
                    type = "Line",
                    layer = ln.Layer,
                    x1 = Math.Round(x1, 1),
                    y1 = Math.Round(y1, 1),
                    x2 = Math.Round(x2, 1),
                    y2 = Math.Round(y2, 1),
                    len = Math.Round(ln.Length, 1)
                });
            }
        }
        else if (ent is Polyline pl)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                var pt = pl.GetPoint2dAt(i);
                double x = pt.X + shiftX;
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
            }
            if (minX < 0)
            {
                negEntities.Add(new {
                    type = "Polyline",
                    layer = pl.Layer,
                    minX = Math.Round(minX, 1),
                    maxX = Math.Round(maxX, 1),
                    numVerts = pl.NumberOfVertices
                });
            }
        }
    }
    return negEntities;
    """
    res = run_cad_script(code, "Inspect CAD entities at negative X")
    print(f"Total negative X entities in block mm: {len(res['value'])}")
    for e in res['value'][:25]:
        print(e)

if __name__ == '__main__':
    main()
