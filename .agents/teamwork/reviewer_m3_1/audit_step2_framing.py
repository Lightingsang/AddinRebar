import json
import sys
from revit_audit_ipc import RevitReviewerIPC
sys.stdout.reconfigure(encoding="utf-8")

def audit_structural_framing():
    print("=== AUDIT STEP 2: STRUCTURAL FRAMING & GHOST BEAMS ===")
    with RevitReviewerIPC() as ipc:
        code = """
        var allFraming = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Select(b => {
                var sym = b.Symbol;
                var pB = sym.LookupParameter("b") ?? sym.LookupParameter("Width") ?? sym.LookupParameter("B");
                var pH = sym.LookupParameter("h") ?? sym.LookupParameter("Height") ?? sym.LookupParameter("H");
                double bMm = pB != null ? Math.Round(pB.AsDouble() * 304.8, 1) : 0.0;
                double hMm = pH != null ? Math.Round(pH.AsDouble() * 304.8, 1) : 0.0;
                
                var bb = b.get_BoundingBox(null);
                double topZ = bb != null ? Math.Round(bb.Max.Z * 304.8, 1) : 0.0;
                double botZ = bb != null ? Math.Round(bb.Min.Z * 304.8, 1) : 0.0;
                
                var crv = (b.Location as LocationCurve)?.Curve;
                var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
                var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;
                
                var lvl = doc.GetElement(b.LevelId) as Level;
                
                return new {
                    id = b.Id.Value,
                    mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = b.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    fam = sym.FamilyName,
                    sym = sym.Name,
                    levelName = lvl != null ? lvl.Name : "",
                    bMm = bMm,
                    hMm = hMm,
                    topZ = topZ,
                    botZ = botZ,
                    lenMm = crv != null ? Math.Round(crv.Length * 304.8, 1) : 0.0,
                    x1 = Math.Round(p0.X * 304.8, 1),
                    y1 = Math.Round(p0.Y * 304.8, 1),
                    x2 = Math.Round(p1.X * 304.8, 1),
                    y2 = Math.Round(p1.Y * 304.8, 1)
                };
            }).ToList();

        return allFraming;
        """
        res = ipc.execute(code, label="Audit Framing Beams")
        val = res['result']['value']
        if isinstance(val, str):
            val = json.loads(val)

        print(f"Total Structural Framing elements in doc: {len(val)}")

        # Group by level and category
        by_level = {}
        by_sym = {}
        by_mark_prefix = {}
        top_z_dist = {}
        
        non_foundation_beams = []
        abnormal_z_beams = []

        for b in val:
            lvl = b['levelName']
            by_level[lvl] = by_level.get(lvl, 0) + 1
            
            s = f"{b['fam']}: {b['sym']} ({int(b['bMm'])}x{int(b['hMm'])})"
            by_sym[s] = by_sym.get(s, 0) + 1
            
            mk = b['mark']
            prefix = mk.split('-')[0] if '-' in mk else mk
            by_mark_prefix[prefix] = by_mark_prefix.get(prefix, 0) + 1
            
            tz = b['topZ']
            top_z_dist[tz] = top_z_dist.get(tz, 0) + 1
            
            if abs(tz - (-1600.0)) > 1.0:
                abnormal_z_beams.append(b)

        print("\nDistribution by Reference Level:")
        for lvl, cnt in sorted(by_level.items()):
            print(f"  {lvl}: {cnt}")

        print("\nDistribution by Cross-Section / Symbol:")
        for s, cnt in sorted(by_sym.items()):
            print(f"  {s}: {cnt}")

        print("\nDistribution by Mark Prefix:")
        for pfx, cnt in sorted(by_mark_prefix.items()):
            print(f"  {pfx}: {cnt}")

        print("\nDistribution by Top Elevation (Z):")
        for tz, cnt in sorted(top_z_dist.items()):
            print(f"  Z = {tz} mm: {cnt}")

        print(f"\nBeams with Top Z deviating from -1600.0 mm (> 1.0 mm tolerance): {len(abnormal_z_beams)}")
        if abnormal_z_beams:
            for b in abnormal_z_beams[:10]:
                print(f"  FAIL BEAM: ID={b['id']}, Mark={b['mark']}, TopZ={b['topZ']}")

        return val

if __name__ == '__main__':
    audit_structural_framing()
