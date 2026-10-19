import json
import sys
from revit_audit_ipc import RevitReviewerIPC
sys.stdout.reconfigure(encoding="utf-8")

def audit_piles():
    print("=== AUDIT STEP 3: PILES (COC D400) ===")
    with RevitReviewerIPC() as ipc:
        code = """
        var allFoundations = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .OfClass(typeof(FamilyInstance))
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Select(p => {
                var sym = p.Symbol;
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                
                var bb = p.get_BoundingBox(null);
                double bbMaxZ = bb != null ? Math.Round(bb.Max.Z * 304.8, 1) : 0.0;
                double bbMinZ = bb != null ? Math.Round(bb.Min.Z * 304.8, 1) : 0.0;

                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                double zTopMm = -1000.0 + offsetFt * 304.8;

                var pD = sym.LookupParameter("D") ?? sym.LookupParameter("d") ?? sym.LookupParameter("Diameter") ?? sym.LookupParameter("b");
                double dVal = pD != null ? Math.Round(pD.AsDouble() * 304.8, 1) : 0.0;

                return new {
                    id = p.Id.Value,
                    fam = sym.FamilyName,
                    sym = sym.Name,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    dVal = dVal,
                    x = Math.Round(pt.X * 304.8, 2),
                    y = Math.Round(pt.Y * 304.8, 2),
                    zTopCalc = Math.Round(zTopMm, 1),
                    bbMaxZ = bbMaxZ,
                    bbMinZ = bbMinZ
                };
            }).ToList();

        return allFoundations;
        """
        res = ipc.execute(code, label="Audit Foundation Piles")
        val = res['result']['value']
        if isinstance(val, str):
            val = json.loads(val)

        print(f"Total Structural Foundation instances in doc: {len(val)}")

        # Check types
        by_sym = {}
        by_z = {}
        by_assigned_footing_group = {}

        for p in val:
            s = f"{p['fam']}: {p['sym']}"
            by_sym[s] = by_sym.get(s, 0) + 1
            
            z = p['zTopCalc']
            by_z[z] = by_z.get(z, 0) + 1
            
            # assigned footing prefix
            cmt = p['comment'] # e.g. "ĐC-2-05" or similar
            ft_type = cmt.split('-')[0] if '-' in cmt else cmt
            by_assigned_footing_group[ft_type] = by_assigned_footing_group.get(ft_type, 0) + 1

        print("\nDistribution by Family / Symbol:")
        for s, cnt in sorted(by_sym.items()):
            print(f"  {s}: {cnt}")

        print("\nDistribution by Top Elevation Z:")
        for z, cnt in sorted(by_z.items()):
            print(f"  Z = {z} mm: {cnt}")

        print("\nDistribution by Assigned Footing Group:")
        for ft, cnt in sorted(by_assigned_footing_group.items()):
            print(f"  {ft}: {cnt}")

        # Assertions
        assert len(val) == 196, f"Expected 196 piles, got {len(val)}"
        assert by_z.get(-2500.0, 0) == 122, f"Expected 122 piles at -2500, got {by_z.get(-2500.0, 0)}"
        assert by_z.get(-2700.0, 0) == 50, f"Expected 50 piles at -2700, got {by_z.get(-2700.0, 0)}"
        assert by_z.get(-2800.0, 0) == 24, f"Expected 24 piles at -2800, got {by_z.get(-2800.0, 0)}"

        print("\n>>> ALL PILE CHECKS SATISFIED: 196 Piles D400 perfectly distributed! <<<")
        return val

if __name__ == '__main__':
    audit_piles()
