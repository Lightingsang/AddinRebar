import json
import sys
import math
sys.stdout.reconfigure(encoding="utf-8")
from revit_audit_ipc import RevitReviewerIPC

def audit_grids_and_levels():
    print("=== AUDIT STEP 1: GRIDS & LEVELS ===")
    with RevitReviewerIPC() as ipc:
        code = """
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
            .Select(g => {
                var crv = g.Curve;
                var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
                var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;
                return new {
                    id = g.Id.Value,
                    name = g.Name,
                    pinned = g.Pinned,
                    x0 = Math.Round(p0.X * 304.8, 1),
                    y0 = Math.Round(p0.Y * 304.8, 1),
                    x1 = Math.Round(p1.X * 304.8, 1),
                    y1 = Math.Round(p1.Y * 304.8, 1)
                };
            }).OrderBy(g => g.name).ToList();

        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
            .Select(l => new {
                id = l.Id.Value,
                name = l.Name,
                elevFt = l.Elevation,
                elevMm = Math.Round(l.Elevation * 304.8, 1),
                pinned = l.Pinned
            }).OrderBy(l => l.elevMm).ToList();

        return new { grids = grids, levels = levels };
        """
        res = ipc.execute(code, label="Audit Grids Levels")
        val = res['result']['value']
        if isinstance(val, str):
            val = json.loads(val)
        
        grids = val['grids']
        levels = val['levels']
        
        print(f"Total Grids: {len(grids)}")
        unpinned_grids = [g for g in grids if not g['pinned']]
        print(f"Unpinned Grids count: {len(unpinned_grids)}")
        for g in grids:
            print(f"  Grid '{g['name']}': ID={g['id']}, Pinned={g['pinned']}, ({g['x0']},{g['y0']}) -> ({g['x1']},{g['y1']})")
            
        print(f"\nTotal Levels: {len(levels)}")
        unpinned_levels = [l for l in levels if not l['pinned']]
        print(f"Unpinned Levels count: {len(unpinned_levels)}")
        for l in levels:
            print(f"  Level '{l['name']}': ID={l['id']}, Pinned={l['pinned']}, Elev={l['elevMm']} mm")
            
        return grids, levels

if __name__ == '__main__':
    audit_grids_and_levels()
