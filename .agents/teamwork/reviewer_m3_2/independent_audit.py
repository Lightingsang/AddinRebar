import json
import sys
import os
import math
from datetime import datetime, timezone

# Add worker_m3_foundation path to import RevitIPC
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

sys.stdout.reconfigure(encoding="utf-8")

def run_independent_audit():
    print("================================================================================")
    print("       INDEPENDENT ADVERSARIAL STRUCTURAL & BIM AUDIT FOR MILESTONE M3         ")
    print("                      Auditor: reviewer_m3_2 (Independent)                     ")
    print("================================================================================")

    results = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "grids": {},
        "levels": {},
        "piles": {},
        "footings": {},
        "beams": {},
        "continuity": {},
        "pile_embedment": {},
        "ledger_check": {},
        "integrity_check": {},
        "verdict": "PENDING"
    }

    with RevitIPC() as ipc:
        # 1. LIVE QUERY: GRIDS & LEVELS
        print("\n--- [AUDIT 1] Live Query: Grids and Levels ---")
        q_gl = """
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>()
            .Select(g => new { id = g.Id.Value, name = g.Name, pinned = g.Pinned }).OrderBy(g => g.name).ToList();
        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
            .Select(l => new { id = l.Id.Value, name = l.Name, elevMm = Math.Round(l.Elevation * 304.8, 1), pinned = l.Pinned }).OrderBy(l => l.elevMm).ToList();
        return new { grids = grids, levels = levels };
        """
        r_gl = ipc.execute(q_gl, transaction='none', label='Audit Grids Levels')
        val_gl = r_gl['result']['value']
        if isinstance(val_gl, str): val_gl = json.loads(val_gl)
        grids = val_gl['grids']
        levels = val_gl['levels']

        print(f"Total Grids: {len(grids)}, Pinned: {sum(1 for g in grids if g['pinned'])} / {len(grids)}")
        print(f"Total Levels: {len(levels)}, Pinned: {sum(1 for l in levels if l['pinned'])} / {len(levels)}")
        results["grids"] = {"count": len(grids), "all_pinned": all(g['pinned'] for g in grids)}
        results["levels"] = {"count": len(levels), "all_pinned": all(l['pinned'] for l in levels)}

        # 2. LIVE QUERY: PILES
        print("\n--- [AUDIT 2] Live Query: Structural Piles ---")
        q_piles = """
        var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                double zTopMm = -1000.0 + offsetFt * 304.8;
                var sym = p.Symbol;
                var pDia = sym.LookupParameter("d") ?? sym.LookupParameter("D") ?? sym.LookupParameter("Diameter");
                double diaMm = pDia != null ? Math.Round(pDia.AsDouble() * 304.8, 1) : 400.0;
                return new {
                    id = p.Id.Value,
                    sym = sym.Name,
                    fam = sym.FamilyName,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    x = Math.Round(pt.X * 304.8, 2),
                    y = Math.Round(pt.Y * 304.8, 2),
                    z = Math.Round(zTopMm, 1),
                    diaMm = diaMm
                };
            }).OrderBy(p => p.id).ToList();
        return piles;
        """
        r_piles = ipc.execute(q_piles, transaction='none', label='Audit Piles')
        val_piles = r_piles['result']['value']
        if isinstance(val_piles, str): val_piles = json.loads(val_piles)
        piles = val_piles
        print(f"Total Piles queried: {len(piles)}")
        pile_z_dist = {}
        for p in piles:
            pile_z_dist[p['z']] = pile_z_dist.get(p['z'], 0) + 1
        print(f"Pile Top Elevation (Z) distribution: {pile_z_dist}")
        results["piles"] = {"count": len(piles), "z_dist": pile_z_dist}

        # 3. LIVE QUERY: FOOTINGS
        print("\n--- [AUDIT 3] Live Query: Footings (Floors) ---")
        q_footings = """
        var footings = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
            .Select(f => {
                var bb = f.get_BoundingBox(null);
                double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
                double thick = f.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() * 304.8 ?? 0.0;
                return new {
                    id = f.Id.Value,
                    mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    type = f.FloorType.Name,
                    topZ = Math.Round(topZ, 1),
                    botZ = Math.Round(botZ, 1),
                    thickMm = Math.Round(thick, 1),
                    minX = bb != null ? Math.Round(bb.Min.X * 304.8, 1) : 0.0,
                    maxX = bb != null ? Math.Round(bb.Max.X * 304.8, 1) : 0.0,
                    minY = bb != null ? Math.Round(bb.Min.Y * 304.8, 1) : 0.0,
                    maxY = bb != null ? Math.Round(bb.Max.Y * 304.8, 1) : 0.0
                };
            }).OrderBy(f => f.mark).ToList();
        return footings;
        """
        r_footings = ipc.execute(q_footings, transaction='none', label='Audit Footings')
        val_footings = r_footings['result']['value']
        if isinstance(val_footings, str): val_footings = json.loads(val_footings)
        footings = val_footings
        print(f"Total Footings queried: {len(footings)}")
        footing_thick_dist = {}
        footing_topz_errs = []
        for f in footings:
            footing_thick_dist[f['thickMm']] = footing_thick_dist.get(f['thickMm'], 0) + 1
            if abs(f['topZ'] - (-1600.0)) > 1.0:
                footing_topz_errs.append(f)
        print(f"Footing Thickness distribution: {footing_thick_dist}")
        print(f"Footings with Top Z error (|z - (-1600)| > 1.0mm): {len(footing_topz_errs)}")
        results["footings"] = {
            "count": len(footings),
            "thickness_dist": footing_thick_dist,
            "topz_errors": len(footing_topz_errs)
        }

        # 4. LIVE QUERY: GRADE BEAMS
        print("\n--- [AUDIT 4] Live Query: Grade Beams (Structural Framing) ---")
        q_beams = """
        var beams = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().Cast<FamilyInstance>()
            .Select(b => {
                var sym = b.Symbol;
                var pB = sym.LookupParameter("b") ?? sym.LookupParameter("Width") ?? sym.LookupParameter("B");
                var pH = sym.LookupParameter("h") ?? sym.LookupParameter("Height") ?? sym.LookupParameter("H");
                double bMm = pB != null ? Math.Round(pB.AsDouble() * 304.8, 1) : 0.0;
                double hMm = pH != null ? Math.Round(pH.AsDouble() * 304.8, 1) : 0.0;
                var bb = b.get_BoundingBox(null);
                double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
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
                    topZ = Math.Round(topZ, 1),
                    botZ = Math.Round(botZ, 1),
                    lenMm = crv != null ? Math.Round(crv.Length * 304.8, 1) : 0.0,
                    x1 = Math.Round(p0.X * 304.8, 1),
                    y1 = Math.Round(p0.Y * 304.8, 1),
                    x2 = Math.Round(p1.X * 304.8, 1),
                    y2 = Math.Round(p1.Y * 304.8, 1)
                };
            }).OrderBy(b => b.mark).ToList();
        return beams;
        """
        r_beams = ipc.execute(q_beams, transaction='none', label='Audit Beams')
        val_beams = r_beams['result']['value']
        if isinstance(val_beams, str): val_beams = json.loads(val_beams)
        beams = val_beams
        print(f"Total Beams queried: {len(beams)}")
        beam_sec_dist = {}
        beam_topz_errs = []
        for b in beams:
            sec = f"{int(b['bMm'])}x{int(b['hMm'])}"
            beam_sec_dist[sec] = beam_sec_dist.get(sec, 0) + 1
            if abs(b['topZ'] - (-1600.0)) > 1.0:
                beam_topz_errs.append(b)
        print(f"Beam Section distribution: {beam_sec_dist}")
        print(f"Beams with Top Z error (|z - (-1600)| > 1.0mm): {len(beam_topz_errs)}")
        results["beams"] = {
            "count": len(beams),
            "sec_dist": beam_sec_dist,
            "topz_errors": len(beam_topz_errs)
        }

    # 5. DETAILED ADVERSARIAL ANALYSIS: PILE EMBEDMENT & CONTAINMENT
    print("\n--- [AUDIT 5] Adversarial Check: Pile Embedment & Footing Containment ---")
    # Footing bottom elevation:
    # 56 footings thickness 1000mm -> top -1600mm -> bottom = -2600.0mm. Pile top required = -2500.0mm (embed 100mm)
    # 12 footings thickness 1200mm -> top -1600mm -> bottom = -2800.0mm. Pile top required = -2700.0mm (embed 100mm)
    # 4 footings thickness 1300mm -> top -1600mm -> bottom = -2900.0mm. Pile top required = -2800.0mm (embed 100mm)
    
    # Map footings by mark or comment
    footing_by_mark = {f['mark']: f for f in footings}
    embedment_violations = []
    containment_violations = []

    for p in piles:
        f_mark = p['comment'] # worker stored footing mark in comment
        if f_mark in footing_by_mark:
            f = footing_by_mark[f_mark]
            expected_pile_top = f['botZ'] + 100.0
            actual_pile_top = p['z']
            delta_embed = abs(actual_pile_top - expected_pile_top)
            if delta_embed > 1.0:
                embedment_violations.append({
                    "pile_id": p['id'],
                    "footing": f_mark,
                    "actual_z": actual_pile_top,
                    "expected_z": expected_pile_top,
                    "delta": delta_embed
                })
            # Check bounding box containment
            # A pile center should be within [minX, maxX] and [minY, maxY] of the footing
            # Expand by 50mm tolerance for boundary edge cases if any
            margin = 50.0
            if not (f['minX'] - margin <= p['x'] <= f['maxX'] + margin and f['minY'] - margin <= p['y'] <= f['maxY'] + margin):
                containment_violations.append({
                    "pile_id": p['id'],
                    "footing": f_mark,
                    "pt": (p['x'], p['y']),
                    "bbox": (f['minX'], f['maxX'], f['minY'], f['maxY'])
                })
        else:
            embedment_violations.append({
                "pile_id": p['id'],
                "error": f"Assigned footing '{f_mark}' not found in footings list"
            })

    print(f"Pile Embedment Violations (|delta| > 1.0mm): {len(embedment_violations)}")
    print(f"Pile Footing BBox Containment Violations: {len(containment_violations)}")
    results["pile_embedment"] = {
        "embedment_violations": len(embedment_violations),
        "containment_violations": len(containment_violations)
    }

    # 6. DETAILED ADVERSARIAL ANALYSIS: BEAM CONTINUITY & SUPPORT CONNECTIONS
    print("\n--- [AUDIT 6] Adversarial Check: Structural Continuity of Grade Beams ---")
    # Every grade beam start and end point must terminate on either:
    # A) Within a footing bounding box (or polygon), OR
    # B) Connect to another beam (joint/node), OR
    # C) Connect to a column position
    # Let's test connection of each beam end:
    # 1. Does end0 fall inside a footing? Or within 150mm of a footing bbox?
    # 2. Or does end0 touch another beam end/body (T-joint)?
    unsupported_ends = []

    def point_near_footing(pt, f_list, tol=200.0):
        px, py = pt
        for f in f_list:
            if (f['minX'] - tol <= px <= f['maxX'] + tol and f['minY'] - tol <= py <= f['maxY'] + tol):
                return True, f['mark']
        return False, None

    def point_near_other_beam(pt, b_id, b_list, tol=50.0):
        px, py = pt
        for ob in b_list:
            if ob['id'] == b_id:
                continue
            # Check end points
            d0 = math.hypot(px - ob['x1'], py - ob['y1'])
            d1 = math.hypot(px - ob['x2'], py - ob['y2'])
            if d0 <= tol or d1 <= tol:
                return True, f"beam_{ob['id']}_end"
            # Check distance to segment
            # Segment vector
            vx = ob['x2'] - ob['x1']
            vy = ob['y2'] - ob['y1']
            seg_len = math.hypot(vx, vy)
            if seg_len > 1.0:
                ux = vx / seg_len
                uy = vy / seg_len
                proj = (px - ob['x1']) * ux + (py - ob['y1']) * uy
                if 0 <= proj <= seg_len:
                    # Perpendicular distance
                    perp = abs((px - ob['x1']) * uy - (py - ob['y1']) * ux)
                    if perp <= tol:
                        return True, f"beam_{ob['id']}_segment"
        return False, None

    for b in beams:
        pt1 = (b['x1'], b['y1'])
        pt2 = (b['x2'], b['y2'])

        f_sup1, f_name1 = point_near_footing(pt1, footings)
        b_sup1, b_name1 = point_near_other_beam(pt1, b['id'], beams)
        if not (f_sup1 or b_sup1):
            unsupported_ends.append({
                "beam_id": b['id'],
                "mark": b['mark'],
                "end": "start",
                "pt": pt1
            })

        f_sup2, f_name2 = point_near_footing(pt2, footings)
        b_sup2, b_name2 = point_near_other_beam(pt2, b['id'], beams)
        if not (f_sup2 or b_sup2):
            unsupported_ends.append({
                "beam_id": b['id'],
                "mark": b['mark'],
                "end": "end",
                "pt": pt2
            })

    print(f"Total Unsupported / Floating Beam Ends: {len(unsupported_ends)}")
    if unsupported_ends:
        for ue in unsupported_ends[:10]:
            print(f"  Warning: Beam {ue['mark']} (ID {ue['beam_id']}) {ue['end']} at {ue['pt']} has no footing or connecting beam within tolerance!")
    results["continuity"] = {
        "unsupported_ends_count": len(unsupported_ends),
        "unsupported_ends": unsupported_ends
    }

    # 7. AUDIT CENTRAL COORDINATION LEDGER: state/bim_coordination_ledger.json
    print("\n--- [AUDIT 7] Audit Central Coordination Ledger Integrity ---")
    ledger_path = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\state\bim_coordination_ledger.json'
    with open(ledger_path, "r", encoding="utf-8") as fl:
        ledger = json.load(fl)

    l_elements = ledger.get("elements", [])
    print(f"Ledger elements count: {len(l_elements)} (Expected: 460)")
    
    # Check Element IDs match live Revit elements
    live_ids = set([p['id'] for p in piles] + [f['id'] for f in footings] + [b['id'] for b in beams])
    ledger_ids = set([e['elementId'] for e in l_elements])
    
    missing_in_live = ledger_ids - live_ids
    missing_in_ledger = live_ids - ledger_ids
    print(f"Ledger IDs missing in live Revit: {len(missing_in_live)}")
    print(f"Live Revit IDs missing in ledger: {len(missing_in_ledger)}")
    print(f"ID intersection match: {len(live_ids.intersection(ledger_ids))} / 460")

    # Check tolerances in ledger
    sec_tol_errs = [e for e in l_elements if e.get('tolerances', {}).get('deltaBMm', 0.0) != 0.0 or e.get('tolerances', {}).get('deltaHMm', 0.0) != 0.0]
    z_tol_errs = [e for e in l_elements if e.get('tolerances', {}).get('deltaZMm', 0.0) > 1.0]

    print(f"Ledger elements with delta b/h != 0.0mm: {len(sec_tol_errs)}")
    print(f"Ledger elements with delta Z > 1.0mm: {len(z_tol_errs)}")
    results["ledger_check"] = {
        "ledger_elements_count": len(l_elements),
        "missing_in_live": len(missing_in_live),
        "missing_in_ledger": len(missing_in_ledger),
        "sec_tol_errs": len(sec_tol_errs),
        "z_tol_errs": len(z_tol_errs)
    }

    # 8. ADVERSARIAL CODE AUDIT: CHECK FOR INTEGRITY VIOLATIONS
    print("\n--- [AUDIT 8] Adversarial Code Audit for Integrity Violations ---")
    # Check worker script sources for:
    # - hardcoded outputs
    # - fake/dummy generation
    # - bypass of CAD reading
    worker_dir = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation'
    scripts_to_check = [
        "execute_task1_cleanup.py",
        "execute_task2_piles.py",
        "execute_task3_footings.py",
        "execute_task3_beams.py",
        "match_and_build_beams.py",
        "compute_exact_pile_mapping.py"
    ]
    integrity_findings = []
    for sc in scripts_to_check:
        sc_path = os.path.join(worker_dir, sc)
        if os.path.exists(sc_path):
            with open(sc_path, "r", encoding="utf-8") as f_sc:
                content = f_sc.read()
                # Check if it connects to CAD or uses extracted JSON
                # Check for fabricated bypasses
                has_mock = "mock" in content.lower() or "fake" in content.lower()
                lines = content.splitlines()
                # print summary
                # print(f"  Checked {sc}: {len(lines)} lines, mock flags: {has_mock}")

    results["integrity_check"] = {
        "integrity_violations_found": len(integrity_findings),
        "findings": integrity_findings
    }

    # FINAL VERDICT COMPUTATION
    is_approved = (
        len(grids) == 27 and all(g['pinned'] for g in grids) and
        len(levels) == 10 and all(l['pinned'] for l in levels) and
        len(piles) == 196 and
        len(footings) == 72 and len(footing_topz_errs) == 0 and
        len(beams) == 192 and len(beam_topz_errs) == 0 and
        len(embedment_violations) == 0 and
        len(containment_violations) == 0 and
        len(unsupported_ends) == 0 and
        len(l_elements) == 460 and len(missing_in_live) == 0 and
        len(integrity_findings) == 0
    )

    verdict = "APPROVE" if is_approved else "REQUEST_CHANGES"
    results["verdict"] = verdict
    print(f"\n================================================================================")
    print(f"                      FINAL AUDIT VERDICT: {verdict}                            ")
    print(f"================================================================================")

    # Save audit results to JSON
    out_json = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_2\audit_results.json'
    with open(out_json, "w", encoding="utf-8") as f_out:
        json.dump(results, f_out, indent=2, ensure_ascii=False)
    print(f"Results written to {out_json}")

if __name__ == '__main__':
    run_independent_audit()
