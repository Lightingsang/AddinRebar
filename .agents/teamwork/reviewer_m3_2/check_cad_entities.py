import sys
import json

sys.stdout.reconfigure(encoding='utf-8')
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_acad_client

c = get_acad_client()
code = """
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
var entities = new List<object>();
double cx = 316302.1;
double cy = -38625.9;
double r = 3000.0;

foreach (ObjectId id in btr)
{
    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
    if (ent == null) continue;
    try
    {
        var ext = ent.GeometricExtents;
        double mx = (ext.MinPoint.X + ext.MaxPoint.X)/2.0;
        double my = (ext.MinPoint.Y + ext.MaxPoint.Y)/2.0;
        if (Math.Abs(mx - cx) < r && Math.Abs(my - cy) < r)
        {
            if (ent is BlockReference br)
            {
                entities.Add(new {
                    handle = ent.Handle.ToString(),
                    type = "BlockReference",
                    name = br.Name,
                    layer = ent.Layer,
                    minX = Math.Round(ext.MinPoint.X, 1),
                    maxX = Math.Round(ext.MaxPoint.X, 1),
                    minY = Math.Round(ext.MinPoint.Y, 1),
                    maxY = Math.Round(ext.MaxPoint.Y, 1)
                });
            }
            else if (ent is Polyline pl)
            {
                entities.Add(new {
                    handle = ent.Handle.ToString(),
                    type = "Polyline",
                    name = "",
                    layer = ent.Layer,
                    minX = Math.Round(ext.MinPoint.X, 1),
                    maxX = Math.Round(ext.MaxPoint.X, 1),
                    minY = Math.Round(ext.MinPoint.Y, 1),
                    maxY = Math.Round(ext.MaxPoint.Y, 1)
                });
            }
            else if (ent is DBText dbt)
            {
                entities.Add(new {
                    handle = ent.Handle.ToString(),
                    type = "DBText",
                    name = dbt.TextString,
                    layer = ent.Layer,
                    minX = Math.Round(ext.MinPoint.X, 1),
                    maxX = Math.Round(ext.MaxPoint.X, 1),
                    minY = Math.Round(ext.MinPoint.Y, 1),
                    maxY = Math.Round(ext.MaxPoint.Y, 1)
                });
            }
        }
    }
    catch {}
}
return entities;
"""
res = c.execute(code, transaction='auto', label='Query Blocks and Polylines')
c.close()

val = res['result']['value']
if isinstance(val, str):
    val = json.loads(val)

print(f"Total entities found: {len(val)}")
for e in val:
    print(e)
