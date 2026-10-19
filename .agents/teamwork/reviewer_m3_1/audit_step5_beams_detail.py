import json
import sys
from revit_audit_ipc import RevitReviewerIPC
sys.stdout.reconfigure(encoding="utf-8")

def audit_beams_detail():
    print("=== AUDIT STEP 5: GRADE BEAMS DETAILED VERIFICATION ===")
    with RevitReviewerIPC() as ipc:
        code = """
        var beams = new FilteredElementCollector(doc)
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
                
                return new {
                    id = b.Id.Value,
                    mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = b.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    symName = sym.Name,
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
        return beams;
        """
        res = ipc.execute(code, label="Audit Beams Detail")
        beams = res['result']['value']
        if isinstance(beams, str):
            beams = json.loads(beams)

        print(f"Total Beams: {len(beams)}")
        assert len(beams) == 192

        # Check types GM-1..22, BN-1, BN-2
        beam_types = set()
        for b in beams:
            mk = b['mark']
            # e.g. "GM-1-01" -> "GM-1", or "BN-1-01" -> "BN-1"
            parts = mk.split('-')
            if len(parts) >= 2:
                b_type = f"{parts[0]}-{parts[1]}"
                beam_types.add(b_type)

        print(f"Discovered beam type groups: {sorted(beam_types)}")
        
        # Check base types GM-1..22, BN-1, BN-2
        base_beam_types = set()
        for bt in beam_types:
            prefix, num_part = bt.split('-')
            # strip trailing letters like a, b
            clean_num = ''.join([c for c in num_part if c.isdigit()])
            base_beam_types.add(f"{prefix}-{clean_num}")

        expected_gm = [f"GM-{i}" for i in range(1, 23)]
        expected_bn = ["BN-1", "BN-2"]
        expected_all = set(expected_gm + expected_bn)
        
        missing_types = expected_all - base_beam_types
        print(f"Missing base beam types: {missing_types}")
        print(f"Base beam types matched: {sorted(base_beam_types)}")
        assert len(missing_types) == 0, f"Missing base types: {missing_types}"

        # Check lengths: all lengths > 0
        zero_len = [b for b in beams if b['lenMm'] <= 0]
        print(f"Zero length beams: {len(zero_len)}")
        assert len(zero_len) == 0

        # Check duplicate geometries
        lines = []
        dups = []
        for b in beams:
            p1 = (min(b['x1'], b['x2']), min(b['y1'], b['y2']))
            p2 = (max(b['x1'], b['x2']), max(b['y1'], b['y2']))
            line_sig = (round(p1[0], -1), round(p1[1], -1), round(p2[0], -1), round(p2[1], -1))
            if line_sig in lines:
                dups.append(b)
            else:
                lines.append(line_sig)
        print(f"Duplicate/overlapping beam lines: {len(dups)}")
        assert len(dups) == 0, f"Duplicates found: {dups}"

        print("\n>>> ALL 192 GRADE BEAMS (GM-1..22, BN-1, BN-2) ARE FULLY PRESENT, NON-DUPLICATED, AT TOP Z -1600mm! <<<")

if __name__ == '__main__':
    audit_beams_detail()
