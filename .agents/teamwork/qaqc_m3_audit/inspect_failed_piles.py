import json
import sys
sys.stdout.reconfigure(encoding="utf-8")
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_revit_client

def inspect_failed_piles():
    failed_piles_stts = [75, 81, 83, 99, 100, 106, 118, 126, 187]

    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\exact_196_pile_mapping.json', 'r', encoding='utf-8') as f:
        cad_piles = json.load(f)

    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        v_fts = {f['id']: f for f in json.load(f)['footings']}

    with get_revit_client() as revit:
        res = revit.execute('''
        var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                return new {
                    id = p.Id.Value,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    x = Math.Round(pt.X * 304.8, 1),
                    y = Math.Round(pt.Y * 304.8, 1),
                    z = Math.Round(pt.Z * 304.8, 1)
                };
            }).ToList();
        var footings = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
            .Select(f => {
                var bb = f.get_BoundingBox(null);
                return new {
                    id = f.Id.Value,
                    mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    minX = Math.Round(bb.Min.X * 304.8, 1),
                    maxX = Math.Round(bb.Max.X * 304.8, 1),
                    minY = Math.Round(bb.Min.Y * 304.8, 1),
                    maxY = Math.Round(bb.Max.Y * 304.8, 1)
                };
            }).ToList();
        return new { piles = piles, footings = footings };
        ''')
        val = res['result']['value']
        piles = val['piles']
        footings = {f['mark']: f for f in val['footings']}

    for cp in cad_piles:
        stt = cp.get('pile_idx', 0) + 1 # or stt
        # match revit pile by coord
        rp = next((p for p in piles if math.hypot(p['x'] - cp['x'], p['y'] - cp['y']) < 1.0), None)
        if rp and (stt in failed_piles_stts or int(rp['mark'].replace('P','',1) or 0) in failed_piles_stts):
            ft_mark = rp['comment']
            rf = footings.get(ft_mark)
            vf = v_fts.get(ft_mark)
            print(f"\nPile {rp['mark']} (ID {rp['id']}) at ({rp['x']}, {rp['y']}):")
            print(f"  Assigned footing comment: {ft_mark}")
            if rf:
                print(f"  Revit Footing BBox: X=[{rf['minX']}, {rf['maxX']}], Y=[{rf['minY']}, {rf['maxY']}]")
                in_x = rf['minX'] <= rp['x'] <= rf['maxX']
                in_y = rf['minY'] <= rp['y'] <= rf['maxY']
                print(f"  Inside BBox? X: {in_x}, Y: {in_y}")
            if vf:
                print(f"  CAD Vertices: {vf['vertices']}")

if __name__ == '__main__':
    import math
    inspect_failed_piles()
