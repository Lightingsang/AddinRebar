import csv
import json
import sys
from probe_m3_adversarial import PipeClient, ACAD_PIPE, parse_val

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\Bang_Toa_Do_Tim_Coc_Chuan_Ky_Thuat.csv', 'r', encoding='utf-8') as f:
    r = list(csv.DictReader(f))

# Let's find rows matching coordinates
# P75: Revit X=13950, Y=400 -> CAD X = 13950 + 302352.1 = 316302.1, CAD Y = 400 - 38575.9 = -38175.9
flagged_coords = [
    (13950, 400),
    (17250, 2950),
    (17250, 24950),
    (23650, 24700),
    (23650, 3200),
    (29550, 4450),
    (44600, 4450)
]

for row in r:
    for rx, ry in flagged_coords:
        if abs(float(row['Tọa độ Revit X (mm)']) - rx) < 1.0 and abs(float(row['Tọa độ Revit Y (mm)']) - ry) < 1.0:
            print(f"Pile: {row['Số Hiệu Cọc']} | Handle: {row['CAD Handle']} | CAD X,Y: ({row['Tọa độ CAD X (mm)']}, {row['Tọa độ CAD Y (mm)']}) | Revit X,Y: ({rx}, {ry})")

# Now query CAD around P75: CAD X=316302.1, CAD Y=-38175.9
# Check what polylines exist within 3000mm in CAD!
with PipeClient(ACAD_PIPE, 'autocad') as acad:
    code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
    var ents = new System.Collections.Generic.List<object>();
    double targetX = 316302.1;
    double targetY = -38175.9;
    foreach (ObjectId id in ms)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent == null) continue;
        if (ent is Polyline pl)
        {
            var ext = pl.GeometricExtents;
            if (ext.MinPoint.X >= targetX - 3000 && ext.MaxPoint.X <= targetX + 3000 &&
                ext.MinPoint.Y >= targetY - 3000 && ext.MaxPoint.Y <= targetY + 3000)
            {
                ents.Add(new {
                    handle = pl.Handle.ToString(),
                    layer = pl.Layer,
                    minX = Math.Round(ext.MinPoint.X, 1),
                    maxX = Math.Round(ext.MaxPoint.X, 1),
                    minY = Math.Round(ext.MinPoint.Y, 1),
                    maxY = Math.Round(ext.MaxPoint.Y, 1),
                    revitMinX = Math.Round(ext.MinPoint.X - 302352.1, 1),
                    revitMaxX = Math.Round(ext.MaxPoint.X - 302352.1, 1),
                    revitMinY = Math.Round(ext.MinPoint.Y - (-38575.9), 1),
                    revitMaxY = Math.Round(ext.MaxPoint.Y - (-38575.9), 1)
                });
            }
        }
    }
    return ents;
    """
    res = parse_val(acad.execute(code, label='Query CAD polylines near P75'))
    print("\nPolylines near P75 in CAD:")
    for e in res:
        print(e)
