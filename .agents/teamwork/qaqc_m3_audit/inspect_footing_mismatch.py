import json
import sys
sys.stdout.reconfigure(encoding="utf-8")
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_revit_client

def main():
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        v_fts = json.load(f)['footings']

    v_map = {f['id']: f for f in v_fts}

    with get_revit_client() as revit:
        res = revit.execute('''
        var footings = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
            .Select(f => {
                var bb = f.get_BoundingBox(null);
                var opt = new Options();
                var geom = f.get_Geometry(opt);
                var pts = new List<object>();
                foreach (var obj in geom) {
                    if (obj is Solid solid && solid.Volume > 0) {
                        foreach (Face face in solid.Faces) {
                            if (face is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
                                foreach (EdgeArray loop in pf.EdgeLoops) {
                                    foreach (Edge edge in loop) {
                                        var p = edge.AsCurve().GetEndPoint(0);
                                        pts.Add(new { x = Math.Round(p.X * 304.8, 1), y = Math.Round(p.Y * 304.8, 1) });
                                    }
                                }
                            }
                        }
                    }
                }
                return new {
                    id = f.Id.Value,
                    mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
                    minX = Math.Round(bb.Min.X * 304.8, 1),
                    minY = Math.Round(bb.Min.Y * 304.8, 1),
                    maxX = Math.Round(bb.Max.X * 304.8, 1),
                    maxY = Math.Round(bb.Max.Y * 304.8, 1),
                    pts = pts
                };
            }).ToList();
        return footings;
        ''')
        r_fts = res['result']['value']

    for rf in r_fts[:10]:
        mark = rf['mark']
        vf = v_map.get(mark)
        print(f"\nFooting {mark} (ID: {rf['id']}):")
        print(f"  Revit BBox: X=[{rf['minX']}, {rf['maxX']}], Y=[{rf['minY']}, {rf['maxY']}]")
        r_cx = (rf['minX'] + rf['maxX']) / 2.0
        r_cy = (rf['minY'] + rf['maxY']) / 2.0
        print(f"  Revit Center: ({r_cx:.1f}, {r_cy:.1f})")
        if vf:
            print(f"  CAD Vertices Center: ({vf['centerX']:.1f}, {vf['centerY']:.1f})")
            print(f"  CAD Vertices: {vf['vertices']}")
            print(f"  Delta center: dx={r_cx - vf['centerX']:.1f}, dy={r_cy - vf['centerY']:.1f}")

if __name__ == '__main__':
    main()
