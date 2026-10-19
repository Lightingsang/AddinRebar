import sys
import json

sys.stdout.reconfigure(encoding='utf-8')
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_acad_client

c = get_acad_client()
code = """
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
var mmList = new List<object>();

foreach (ObjectId id in btr)
{
    var br = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
    if (br == null) continue;
    if (br.Name.Equals("mm", StringComparison.OrdinalIgnoreCase))
    {
        var ext = br.GeometricExtents;
        mmList.Add(new {
            handle = br.Handle.ToString(),
            layer = br.Layer,
            pos = new { x = Math.Round(br.Position.X, 1), y = Math.Round(br.Position.Y, 1) },
            minX = Math.Round(ext.MinPoint.X, 1), maxX = Math.Round(ext.MaxPoint.X, 1),
            minY = Math.Round(ext.MinPoint.Y, 1), maxY = Math.Round(ext.MaxPoint.Y, 1)
        });
    }
}
return new { count = mmList.Count, items = mmList };
"""
res = c.execute(code, transaction='auto', label='Count mm blocks')
c.close()
val = res['result']['value']
if isinstance(val, str):
    val = json.loads(val)
print('Total mm blocks in CAD:', val['count'])

# Find the mm block closest to (316302.1, -38625.9)
cx, cy = 316302.1, -38625.9
import math
nearest = None
min_d = 999999.0
for it in val['items']:
    mx = (it['minX'] + it['maxX']) / 2.0
    my = (it['minY'] + it['maxY']) / 2.0
    d = math.hypot(mx - cx, my - cy)
    if d < min_d:
        min_d = d
        nearest = (it, d, mx, my)

print("Nearest mm block to P75/P76 CAD pos:")
print(nearest)
