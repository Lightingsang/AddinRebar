import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    print("=== EXECUTING TASK 2: ADJUST 196 PILES D400 EMBEDMENT ===")

    # 1. Load mapping
    with open(r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\exact_196_pile_mapping.json", "r", encoding="utf-8") as f:
        mapping = json.load(f)

    # 2. Query live Revit piles
    with RevitIPC() as ipc:
        query_code = """
        var piles = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                return new {
                    id = p.Id.Value,
                    x = pt.X * 304.8,
                    y = pt.Y * 304.8,
                    z = pt.Z * 304.8
                };
            })
            .ToList();
        return new { piles = piles };
        """
        q_res = ipc.execute(query_code, transaction='none', label='Query Live Piles')
        revit_piles = q_res['result']['value']['piles']
        print(f"Total live piles in Revit: {len(revit_piles)}")

        # Build update list
        csharp_lines = []
        for rp in revit_piles:
            rx, ry = rp['x'], rp['y']
            # Find matching pile in mapping
            best_m = min(mapping, key=lambda m: (rx - m['x'])**2 + (ry - m['y'])**2)
            dist = ((rx - best_m['x'])**2 + (ry - best_m['y'])**2)**0.5
            assert dist < 1.0, f"Pile {rp['id']} at ({rx}, {ry}) not matched (dist={dist})"

            target_z = best_m['pile_top_z'] * 1000.0 # e.g. -2500.0
            # Level 'Tầng Móng' is at -1000.0 mm
            # Offset in mm = target_z - (-1000.0) = target_z + 1000.0
            offset_mm = target_z - (-1000.0)
            offset_ft = offset_mm / 304.8
            ft_id = best_m['footing_id']
            csharp_lines.append(f'            ({rp["id"]}L, {offset_ft:.10f}, "{ft_id}", {best_m["x"]:.1f}, {best_m["y"]:.1f}, {target_z:.1f})')

        code_updates = ",\n".join(csharp_lines)

        update_script = f"""
        var symTron400 = doc.GetElement(new ElementId(9215875L)) as FamilySymbol;
        if (!symTron400.IsActive) symTron400.Activate();

        var updates = new (long id, double offsetFt, string comment, double expX, double expY, double expZ)[] {{
{code_updates}
        }};

        int modifiedCount = 0;
        foreach (var u in updates)
        {{
            var pile = doc.GetElement(new ElementId(u.id)) as FamilyInstance;
            if (pile == null) continue;

            pile.Symbol = symTron400;
            var paramOffset = pile.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
            if (paramOffset != null && !paramOffset.IsReadOnly)
            {{
                paramOffset.Set(u.offsetFt);
            }}

            var paramComment = pile.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            if (paramComment != null && !paramComment.IsReadOnly)
            {{
                paramComment.Set(u.comment);
            }}

            modifiedCount++;
        }}

        doc.Regenerate();

        return new {{ modifiedCount = modifiedCount }};
        """

        print(f"Executing update for {len(csharp_lines)} piles...")
        u_res = ipc.execute(update_script, transaction='auto', label='Update 196 Piles to P400 and Exact Elevations')
        print("Update result:", json.dumps(u_res, indent=2))
        assert u_res['result']['value']['modifiedCount'] == 196

        # Phase 2: Verification Query
        print("Verifying updated piles...")
        verify_script = """
        var piles = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                double zTopMm = -1000.0 + offsetFt * 304.8;
                return new {
                    id = p.Id.Value,
                    sym = p.Symbol.Name,
                    fam = p.Symbol.FamilyName,
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    xMm = Math.Round(pt.X * 304.8, 2),
                    yMm = Math.Round(pt.Y * 304.8, 2),
                    zMm = Math.Round(zTopMm, 1)
                };
            })
            .ToList();

        return new {
            total = piles.Count,
            piles = piles
        };
        """
        v_res = ipc.execute(verify_script, transaction='none', label='Verify Piles After Update')
        v_val = v_res['result']['value']
        assert v_val['total'] == 196, f"Expected 196 piles, got {v_val['total']}"

        verified_piles = v_val['piles']
        z_dist = {}
        sym_dist = {}
        max_xy_err = 0.0
        max_z_err = 0.0

        for vp in verified_piles:
            sym_name = f"{vp['fam']}: {vp['sym']}"
            sym_dist[sym_name] = sym_dist.get(sym_name, 0) + 1
            z_val = vp['zMm']
            z_dist[z_val] = z_dist.get(z_val, 0) + 1

            # Match with mapping
            best_m = min(mapping, key=lambda m: (vp['xMm'] - m['x'])**2 + (vp['yMm'] - m['y'])**2)
            d_xy = ((vp['xMm'] - best_m['x'])**2 + (vp['yMm'] - best_m['y'])**2)**0.5
            d_z = abs(vp['zMm'] - (best_m['pile_top_z'] * 1000.0))
            if d_xy > max_xy_err: max_xy_err = d_xy
            if d_z > max_z_err: max_z_err = d_z

        print("Verification Summary:")
        print(f"Total piles: {len(verified_piles)}")
        print("Symbols:", json.dumps(sym_dist, indent=2))
        print("Z Elevations:", json.dumps(z_dist, indent=2))
        print(f"Max XY center error: {max_xy_err:.3f} mm (Tolerance <= 2.0 mm)")
        print(f"Max Z elevation error: {max_z_err:.3f} mm (Tolerance <= 1.0 mm)")

        assert max_xy_err <= 2.0, f"XY error {max_xy_err} exceeded tolerance"
        assert max_z_err <= 1.0, f"Z error {max_z_err} exceeded tolerance"
        assert z_dist.get(-2500.0, 0) == 122, f"Expected 122 piles at -2500mm, got {z_dist.get(-2500.0, 0)}"
        assert z_dist.get(-2700.0, 0) == 50, f"Expected 50 piles at -2700mm, got {z_dist.get(-2700.0, 0)}"
        assert z_dist.get(-2800.0, 0) == 24, f"Expected 24 piles at -2800mm, got {z_dist.get(-2800.0, 0)}"
        print(">>> TASK 2 VERIFICATION PASSED 100%! <<<")

if __name__ == '__main__':
    main()
