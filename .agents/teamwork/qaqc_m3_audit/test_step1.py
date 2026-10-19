import sys, json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_revit_client, get_acad_client

sys.stdout.reconfigure(encoding="utf-8")

def check_step1():
    print("=== CHECK STEP 1: GRIDS & LEVELS ===")
    with get_revit_client() as revit:
        res = revit.execute('''
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
            .Select(g => {
                var crv = g.Curve;
                var p0 = crv.GetEndPoint(0);
                var p1 = crv.GetEndPoint(1);
                return new {
                    name = g.Name,
                    pinned = g.Pinned,
                    p0x = Math.Round(p0.X * 304.8, 1),
                    p0y = Math.Round(p0.Y * 304.8, 1),
                    p1x = Math.Round(p1.X * 304.8, 1),
                    p1y = Math.Round(p1.Y * 304.8, 1)
                };
            }).ToList();
        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
            .Select(l => new {
                name = l.Name,
                pinned = l.Pinned,
                elevMm = Math.Round(l.Elevation * 304.8, 1)
            }).OrderBy(l => l.elevMm).ToList();
        return new { grids = grids, levels = levels };
        ''')
        val = res['result']['value']
        if isinstance(val, str):
            val = json.loads(val)
        
        grids = val['grids']
        levels = val['levels']

        print(f"Total Grids: {len(grids)}")
        all_grids_pinned = all(g['pinned'] for g in grids)
        print(f"All Grids Pinned: {all_grids_pinned}")

        # Separate X (vertical grid lines: 1, 2, 3...) and Y (horizontal grid lines: A, B, C...)
        print("\nGrids list:")
        for g in sorted(grids, key=lambda x: x['name']):
            print(f"  Grid {g['name']}: Pinned={g['pinned']}, Start=({g['p0x']}, {g['p0y']}), End=({g['p1x']}, {g['p1y']})")

        print(f"\nTotal Levels: {len(levels)}")
        all_levels_pinned = all(l['pinned'] for l in levels)
        print(f"All Levels Pinned: {all_levels_pinned}")
        print("\nLevels list:")
        for l in levels:
            print(f"  Level '{l['name']}': Elev={l['elevMm']} mm, Pinned={l['pinned']}")

if __name__ == '__main__':
    check_step1()
