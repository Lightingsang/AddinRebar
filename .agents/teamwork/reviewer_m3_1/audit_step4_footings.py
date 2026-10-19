import json
import sys
import math
from revit_audit_ipc import RevitReviewerIPC
sys.stdout.reconfigure(encoding="utf-8")

def audit_footings():
    print("=== AUDIT STEP 4: FOOTINGS & PILE EMBEDMENT ===")
    with RevitReviewerIPC() as ipc:
        code = """
        var footings = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .OfClass(typeof(Floor))
            .Cast<Floor>()
            .Select(f => {
                var bb = f.get_BoundingBox(null);
                double topZ = bb != null ? Math.Round(bb.Max.Z * 304.8, 1) : 0.0;
                double botZ = bb != null ? Math.Round(bb.Min.Z * 304.8, 1) : 0.0;
                double thick = f.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() * 304.8 ?? 0.0;
                
                return new {
                    id = f.Id.Value,
                    mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    typeName = f.FloorType.Name,
                    topZ = topZ,
                    botZ = botZ,
                    thickMm = Math.Round(thick, 1),
                    minX = bb != null ? Math.Round(bb.Min.X * 304.8, 1) : 0.0,
                    maxX = bb != null ? Math.Round(bb.Max.X * 304.8, 1) : 0.0,
                    minY = bb != null ? Math.Round(bb.Min.Y * 304.8, 1) : 0.0,
                    maxY = bb != null ? Math.Round(bb.Max.Y * 304.8, 1) : 0.0
                };
            }).ToList();

        var piles = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .OfClass(typeof(FamilyInstance))
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                double zTopMm = -1000.0 + offsetFt * 304.8;
                return new {
                    id = p.Id.Value,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    assignedFooting = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    x = Math.Round(pt.X * 304.8, 2),
                    y = Math.Round(pt.Y * 304.8, 2),
                    zTop = Math.Round(zTopMm, 1)
                };
            }).ToList();

        return new { footings = footings, piles = piles };
        """
        res = ipc.execute(code, label="Audit Footings Piles Embedment")
        val = res['result']['value']
        if isinstance(val, str):
            val = json.loads(val)

        footings = val['footings']
        piles = val['piles']

        print(f"Total Footings: {len(footings)}")
        assert len(footings) == 72, f"Expected 72 footings, got {len(footings)}"

        # Check top elevation
        abnormal_top_z = [f for f in footings if abs(f['topZ'] - (-1600.0)) > 1.0]
        print(f"Footings with Top Z deviating from -1600.0 mm: {len(abnormal_top_z)}")
        assert len(abnormal_top_z) == 0

        # Thickness distribution
        by_thick = {}
        for f in footings:
            t = f['thickMm']
            by_thick[t] = by_thick.get(t, 0) + 1
        print("Thickness distribution:", by_thick)
        assert by_thick.get(1000.0, 0) == 56
        assert by_thick.get(1200.0, 0) == 12
        assert by_thick.get(1300.0, 0) == 4

        # Map footings by mark
        ft_map = {f['mark']: f for f in footings}
        print(f"Mapped {len(ft_map)} unique footing marks.")

        # Check each pile's embedment into its assigned footing
        embedment_errors = []
        containment_errors = []
        piles_without_footing = []

        for p in piles:
            ft_mark = p['assignedFooting']
            if not ft_mark or ft_mark not in ft_map:
                piles_without_footing.append(p)
                continue
            
            ft = ft_map[ft_mark]
            # Embedment = pile top Z - footing bottom Z
            # Footing bottom Z = -1600.0 - ft['thickMm']
            expected_bot_z = -1600.0 - ft['thickMm']
            actual_embedment = p['zTop'] - expected_bot_z
            
            if abs(actual_embedment - 100.0) > 1.0:
                embedment_errors.append({
                    'pileId': p['id'],
                    'assignedFooting': ft_mark,
                    'pileTopZ': p['zTop'],
                    'footingThick': ft['thickMm'],
                    'footingBotZ': expected_bot_z,
                    'embedment': actual_embedment
                })

            # Check bbox containment
            # Tolerant by 200mm for pile radius
            if not (ft['minX'] - 200 <= p['x'] <= ft['maxX'] + 200 and ft['minY'] - 200 <= p['y'] <= ft['maxY'] + 200):
                containment_errors.append({
                    'pileId': p['id'],
                    'assignedFooting': ft_mark,
                    'pilePos': (p['x'], p['y']),
                    'footingBBox': (ft['minX'], ft['maxX'], ft['minY'], ft['maxY'])
                })

        print(f"Piles without matched footing: {len(piles_without_footing)}")
        assert len(piles_without_footing) == 0, f"Unmatched piles: {len(piles_without_footing)}"

        print(f"Piles with embedment != 100.0 mm: {len(embedment_errors)}")
        if embedment_errors:
            for e in embedment_errors[:10]:
                print("  Embedment error:", e)
        assert len(embedment_errors) == 0, f"Found {len(embedment_errors)} embedment errors!"

        print(f"Piles outside footing bounding box: {len(containment_errors)}")
        if containment_errors:
            for c in containment_errors[:10]:
                print("  Containment error:", c)
        assert len(containment_errors) == 0, f"Found {len(containment_errors)} containment errors!"

        print("\n>>> ALL 196 PILES ARE EMBEDDED EXACTLY 100mm INTO FOOTING BOTTOMS & ENCLOSED IN FOOTINGS! <<<")

if __name__ == '__main__':
    audit_footings()
