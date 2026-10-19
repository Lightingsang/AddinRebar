import json
import sys
from probe_m3_adversarial import PipeClient, ACAD_PIPE, parse_val

sys.stdout.reconfigure(encoding='utf-8')

with PipeClient(ACAD_PIPE, 'autocad') as acad:
    code = """
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    var btrMm = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);

    // Look for polylines or entities near cx = 316302.1, cy = -38625.9 inside block mm
    double cx = 316302.1;
    double cy = -38625.9;
    var list = new System.Collections.Generic.List<object>();
    foreach (ObjectId id in btrMm)
    {
        var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent == null) continue;
        Extents3d ext;
        try { ext = ent.GeometricExtents; } catch { continue; }
        if (ext.MinPoint.X >= cx - 2500 && ext.MaxPoint.X <= cx + 2500 &&
            ext.MinPoint.Y >= cy - 2500 && ext.MaxPoint.Y <= cy + 2500)
        {
            string txt = "";
            if (ent is DBText dbt) txt = dbt.TextString;
            else if (ent is MText mt) txt = mt.Contents;

            list.Add(new {
                type = ent.GetType().Name,
                layer = ent.Layer,
                handle = ent.Handle.ToString(),
                text = txt,
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
    return list;
    """
    res = parse_val(acad.execute(code, label='Query inside block mm near ĐC-2-04'))
    print(f"Entities found inside block mm near ĐC-2-04: {len(res)}")
    for e in res:
        print(e)
