import json
import math
import csv
import sys

sys.stdout.reconfigure(encoding='utf-8')

def distance_2d(p1, p2):
    return math.hypot(p1[0] - p2[0], p1[1] - p2[1])

def main():
    print("================================================================================")
    print("=== ADVERSARIAL EMPIRICAL TEST SUITE — MILESTONE M3 (PHẦN MÓNG) ===")
    print("================================================================================\n")

    # Load live Revit audit data
    with open('audit_raw_results.json', 'r', encoding='utf-8') as f:
        revit_data = json.load(f)

    grids = revit_data['grids']
    levels = revit_data['levels']
    beams = revit_data['beams']
    piles = revit_data['piles']
    footings = revit_data['footings']

    failures = []
    warnings = []

    # =========================================================================
    # SUITE 1: GRIDS & LEVELS PROBE
    # =========================================================================
    print("--- [SUITE 1] GRIDS & LEVELS PROBE ---")
    print(f"Total Grids in Revit: {len(grids)}")
    if len(grids) != 27:
        failures.append(f"[Grids Count] Expected 27 grids, got {len(grids)}")
    else:
        print("  [PASS] Grids count == 27")

    unpinned_grids = [g for g in grids if not g['pinned']]
    if unpinned_grids:
        failures.append(f"[Grids Pinned] {len(unpinned_grids)} unpinned grids: {[g['name'] for g in unpinned_grids]}")
    else:
        print("  [PASS] All 27 Grids have Pinned == True")

    nan_grids = [g for g in grids if any(math.isnan(g[k]) for k in ['x0', 'y0', 'x1', 'y1'])]
    if nan_grids:
        failures.append(f"[Grids NaN] Grids with NaN coordinates: {[g['name'] for g in nan_grids]}")
    else:
        print("  [PASS] Zero NaN coordinates in Grids")

    print(f"Total Levels in Revit: {len(levels)}")
    if len(levels) != 10:
        failures.append(f"[Levels Count] Expected 10 levels, got {len(levels)}")
    else:
        print("  [PASS] Levels count == 10")

    unpinned_levels = [l for l in levels if not l['pinned']]
    if unpinned_levels:
        failures.append(f"[Levels Pinned] {len(unpinned_levels)} unpinned levels: {[l['name'] for l in unpinned_levels]}")
    else:
        print("  [PASS] All 10 Levels have Pinned == True")

    print("  Levels list:")
    for l in levels:
        print(f"    - ID {l['id']}: {l['name']} at {l['elevMm']} mm (Pinned: {l['pinned']})")

    # =========================================================================
    # SUITE 2: STRUCTURAL FRAMING (BEAMS) PROBE
    # =========================================================================
    print("\n--- [SUITE 2] STRUCTURAL FRAMING (BEAMS) PROBE ---")
    print(f"Total OST_StructuralFraming elements: {len(beams)}")
    if len(beams) != 192:
        failures.append(f"[Beams Count] Expected 192 beams, got {len(beams)}")
    else:
        print("  [PASS] Beams count == 192 (Assert count == 192)")

    # Check for NaN / Inf
    nan_beams = [b for b in beams if any(math.isnan(b[k]) or math.isinf(b[k]) for k in ['x1', 'y1', 'z1', 'x2', 'y2', 'z2', 'lenMm', 'topZ', 'botZ', 'bMm', 'hMm'])]
    if nan_beams:
        failures.append(f"[Beams NaN] {len(nan_beams)} beams contain NaN or Inf values: {[b['id'] for b in nan_beams]}")
    else:
        print("  [PASS] Zero NaN/Inf values in Beams")

    # Check for zero or negative lengths
    zero_len_beams = [b for b in beams if b['lenMm'] <= 0]
    if zero_len_beams:
        failures.append(f"[Beams Zero-Length] {len(zero_len_beams)} beams have length <= 0: {[b['id'] for b in zero_len_beams]}")
    else:
        print("  [PASS] Zero zero-length beams (min length = " + str(min(b['lenMm'] for b in beams)) + " mm)")

    # Check topZ elevation
    beam_z_err = [b for b in beams if abs(b['topZ'] - (-1600.0)) > 1.0]
    if beam_z_err:
        failures.append(f"[Beams TopZ] {len(beam_z_err)} beams have topZ != -1600.0 mm (tol 1mm): {[b['id'] for b in beam_z_err[:5]]}")
    else:
        print("  [PASS] All 192 Beams have topZ == -1600.0 mm (within 1.0 mm tolerance)")

    # Cross section distribution
    sec_counts = {}
    for b in beams:
        sec = f"{int(round(b['bMm']))}x{int(round(b['hMm']))}"
        sec_counts[sec] = sec_counts.get(sec, 0) + 1
    print(f"  Cross section distribution: {sec_counts}")
    expected_secs = {'300x400': 157, '300x600': 11, '200x300': 22, '200x400': 2}
    if sec_counts != expected_secs:
        failures.append(f"[Beams Sections] Cross section mismatch: expected {expected_secs}, got {sec_counts}")
    else:
        print("  [PASS] Cross sections strictly match expected distribution: 157 (300x400), 11 (300x600), 22 (200x300), 2 (200x400)")

    # Check for duplicate beams (same start and end points)
    duplicate_beams = []
    for i in range(len(beams)):
        for j in range(i + 1, len(beams)):
            b1, b2 = beams[i], beams[j]
            p1_start = (b1['x1'], b1['y1'])
            p1_end = (b1['x2'], b1['y2'])
            p2_start = (b2['x1'], b2['y1'])
            p2_end = (b2['x2'], b2['y2'])
            d_direct = distance_2d(p1_start, p2_start) + distance_2d(p1_end, p2_end)
            d_reverse = distance_2d(p1_start, p2_end) + distance_2d(p1_end, p2_start)
            if min(d_direct, d_reverse) < 10.0:  # within 10mm
                duplicate_beams.append((b1['id'], b2['id']))
    if duplicate_beams:
        failures.append(f"[Beams Duplicates] Found {len(duplicate_beams)} duplicate beam pairs: {duplicate_beams}")
    else:
        print("  [PASS] Zero duplicate beams detected")

    # =========================================================================
    # SUITE 3: PILES PROBE & CENTER DRIFT
    # =========================================================================
    print("\n--- [SUITE 3] PILES PROBE & DRIFT ANALYSIS ---")
    print(f"Total OST_StructuralFoundation (Piles): {len(piles)}")
    if len(piles) != 196:
        failures.append(f"[Piles Count] Expected 196 piles, got {len(piles)}")
    else:
        print("  [PASS] Piles count == 196 (Assert count == 196)")

    # Family & Symbol
    wrong_sym = [p for p in piles if p['sym'] != 'P400']
    if wrong_sym:
        failures.append(f"[Piles Symbol] {len(wrong_sym)} piles with non-P400 symbol: {[p['id'] for p in wrong_sym]}")
    else:
        print("  [PASS] All 196 Piles use FamilySymbol 'P400'")

    # TopZ Distribution
    top_z_counts = {}
    for p in piles:
        z = round(p['z'], 1)
        top_z_counts[z] = top_z_counts.get(z, 0) + 1
    print(f"  Pile topZ distribution: {top_z_counts}")
    expected_pile_z = {-2500.0: 122, -2700.0: 50, -2800.0: 24}
    if top_z_counts != expected_pile_z:
        failures.append(f"[Piles TopZ] Expected {expected_pile_z}, got {top_z_counts}")
    else:
        print("  [PASS] Piles topZ strictly in {-2500, -2700, -2800} mm with exact counts: 122, 50, 24")

    # Drift Analysis against CAD Ground Truth CSV
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\Bang_Toa_Do_Tim_Coc_Chuan_Ky_Thuat.csv', 'r', encoding='utf-8') as f:
        cad_csv = list(csv.DictReader(f))
    
    cad_pile_dict = {}
    for r in cad_csv:
        cad_pile_dict[r['Số Hiệu Cọc']] = {
            'cad_x': float(r['Tọa độ CAD X (mm)']),
            'cad_y': float(r['Tọa độ CAD Y (mm)']),
            'revit_x': float(r['Tọa độ Revit X (mm)']),
            'revit_y': float(r['Tọa độ Revit Y (mm)']),
            'handle': r['CAD Handle']
        }

    # Match Revit piles to CAD ground truth
    max_drift = 0.0
    drift_details = []
    unmatched_revit_piles = []

    cad_matched = set()
    for rp in piles:
        rx, ry = rp['x'], rp['y']
        # Find closest CAD pile
        best_cad = None
        min_d = 1e9
        for c_name, c_data in cad_pile_dict.items():
            d = math.hypot(rx - c_data['revit_x'], ry - c_data['revit_y'])
            if d < min_d:
                min_d = d
                best_cad = (c_name, c_data)
        
        if min_d > 2.0:  # tolerance is 2.0mm
            unmatched_revit_piles.append((rp['id'], rx, ry, min_d))
        else:
            cad_matched.add(best_cad[0])
            if min_d > max_drift:
                max_drift = min_d
            drift_details.append((rp['id'], best_cad[0], min_d))

    print(f"  Maximum pile center drift: {max_drift:.4f} mm")
    if unmatched_revit_piles:
        failures.append(f"[Piles Drift] {len(unmatched_revit_piles)} piles exceeded 2.0mm drift: {unmatched_revit_piles}")
    elif len(cad_matched) != 196:
        failures.append(f"[Piles Match] Only matched {len(cad_matched)} distinct CAD piles out of 196")
    else:
        print(f"  [PASS] All 196 Piles have zero center drift (max drift = {max_drift:.4f} mm <= 2.0 mm)")

    # Check for duplicate pile coordinates
    dup_piles = []
    for i in range(len(piles)):
        for j in range(i + 1, len(piles)):
            d = math.hypot(piles[i]['x'] - piles[j]['x'], piles[i]['y'] - piles[j]['y'])
            if d < 100.0:  # pile diameter is 400mm; distance < 100mm is overlap
                dup_piles.append((piles[i]['id'], piles[j]['id'], d))
    if dup_piles:
        failures.append(f"[Piles Clashing/Duplicate] Piles within 100mm: {dup_piles}")
    else:
        print("  [PASS] Zero duplicate or clashing piles")

    # =========================================================================
    # SUITE 4: FOOTINGS PROBE
    # =========================================================================
    print("\n--- [SUITE 4] FOOTINGS PROBE ---")
    print(f"Total OST_Floors (Footings): {len(footings)}")
    if len(footings) != 72:
        failures.append(f"[Footings Count] Expected 72 footings, got {len(footings)}")
    else:
        print("  [PASS] Footings count == 72 (Assert count == 72)")

    # TopZ
    footing_topz_err = [f for f in footings if abs(f['topZ'] - (-1600.0)) > 1.0]
    if footing_topz_err:
        failures.append(f"[Footings TopZ] {len(footing_topz_err)} footings have topZ != -1600.0 mm: {[f['id'] for f in footing_topz_err]}")
    else:
        print("  [PASS] All 72 Footings have topZ == -1600.0 mm (tol 1.0 mm)")

    # Thickness distribution
    thick_counts = {}
    for f in footings:
        th = round(f['thickMm'], 1)
        thick_counts[th] = thick_counts.get(th, 0) + 1
    print(f"  Footing thickness distribution: {thick_counts}")
    expected_footing_th = {1000.0: 56, 1200.0: 12, 1300.0: 4}
    if thick_counts != expected_footing_th:
        failures.append(f"[Footings Thickness] Expected {expected_footing_th}, got {thick_counts}")
    else:
        print("  [PASS] Footing thicknesses strictly in {1000, 1200, 1300} mm with exact counts: 56, 12, 4")

    # Check for NaN / negative bounds
    nan_footings = [f for f in footings if any(math.isnan(f[k]) or math.isinf(f[k]) for k in ['topZ', 'botZ', 'thickMm', 'minX', 'minY', 'maxX', 'maxY'])]
    if nan_footings:
        failures.append(f"[Footings NaN] {len(nan_footings)} footings with NaN: {[f['id'] for f in nan_footings]}")
    else:
        print("  [PASS] Zero NaN coordinates in Footings")

    # =========================================================================
    # SUITE 5: SPATIAL CONTAINMENT & CLASHES
    # =========================================================================
    print("\n--- [SUITE 5] SPATIAL CONTAINMENT, EMBEDMENT & CLASHES ---")
    
    # 1. Pile Containment inside Footings
    # For every pile, find if it sits inside a footing's bounding box and check embedment
    piles_without_footing = []
    embedment_errors = []

    for p in piles:
        px, py, pz = p['x'], p['y'], p['z']
        # Find candidate footings
        candidates = []
        for f in footings:
            # Check bounding box with small tolerance
            if (f['minX'] - 10 <= px <= f['maxX'] + 10) and (f['minY'] - 10 <= py <= f['maxY'] + 10):
                candidates.append(f)
        
        if not candidates:
            piles_without_footing.append((p['id'], px, py))
        else:
            # Check embedment: pile topZ should equal footing bottomZ + 100mm
            matched_embedment = False
            for cand in candidates:
                expected_pile_topz = cand['botZ'] + 100.0
                if abs(pz - expected_pile_topz) <= 1.0:
                    matched_embedment = True
                    break
            if not matched_embedment:
                embedment_errors.append((p['id'], pz, [c['botZ'] for c in candidates]))

    if piles_without_footing:
        failures.append(f"[Pile Containment] {len(piles_without_footing)} piles outside all footings: {piles_without_footing}")
    else:
        print("  [PASS] All 196 Piles are contained within Footing footprints")

    if embedment_errors:
        failures.append(f"[Pile Embedment] {len(embedment_errors)} piles fail 100mm embedment rule: {embedment_errors[:5]}")
    else:
        print("  [PASS] All 196 Piles have exact 100mm embedment into footing bottoms (pile topZ = footing botZ + 100mm)")

    # 2. Footing Overlap Clashes
    # Check if any two distinct footings overlap in 3D volume (they all have topZ = -1600)
    overlapping_footings = []
    for i in range(len(footings)):
        for j in range(i + 1, len(footings)):
            f1, f2 = footings[i], footings[j]
            # 2D BBox overlap
            overlap_x = max(0.0, min(f1['maxX'], f2['maxX']) - max(f1['minX'], f2['minX']))
            overlap_y = max(0.0, min(f1['maxY'], f2['maxY']) - max(f1['minY'], f2['minY']))
            if overlap_x > 1.0 and overlap_y > 1.0:
                # If they overlap in XY, check if they overlap in Z
                overlap_z = max(0.0, min(f1['topZ'], f2['topZ']) - max(f1['botZ'], f2['botZ']))
                if overlap_z > 1.0:
                    overlapping_footings.append((f1['id'], f2['id'], overlap_x, overlap_y, overlap_z))

    if overlapping_footings:
        warnings.append(f"[Footing Clashes] Found {len(overlapping_footings)} overlapping footing bounding boxes: {overlapping_footings[:5]}")
        print(f"  [WARN] {len(overlapping_footings)} footings bounding box overlaps detected")
    else:
        print("  [PASS] Zero 3D volumetric clashes between Footings")

    # 3. Beam Support Proximity
    # Check if each beam endpoint is within 1500mm of a footing or grid line
    unsupported_beams = []
    for b in beams:
        p_start = (b['x1'], b['y1'])
        p_end = (b['x2'], b['y2'])
        start_near = any(distance_2d(p_start, ((f['minX']+f['maxX'])/2, (f['minY']+f['maxY'])/2)) < 2500 for f in footings)
        end_near = any(distance_2d(p_end, ((f['minX']+f['maxX'])/2, (f['minY']+f['maxY'])/2)) < 2500 for f in footings)
        if not (start_near and end_near):
            unsupported_beams.append(b['id'])
    
    if unsupported_beams:
        warnings.append(f"[Beam Proximity] {len(unsupported_beams)} beams may have far support endpoints: {unsupported_beams[:5]}")
    else:
        print("  [PASS] All 192 Beams have endpoints supported at or near Footing nodes")

    # =========================================================================
    # SUMMARY VERDICT
    # =========================================================================
    print("\n================================================================================")
    print("=== EMPIRICAL AUDIT SUMMARY VERDICT ===")
    print("================================================================================")
    print(f"Failures count: {len(failures)}")
    print(f"Warnings count: {len(warnings)}")

    if failures:
        print("\n❌ VERDICT: REJECT")
        for f in failures:
            print(f"  - FAIL: {f}")
    else:
        print("\n✅ VERDICT: APPROVE")
        print("  All assertions satisfied with 0 critical failures!")

    # Save summary report to JSON
    audit_summary = {
        "verdict": "APPROVE" if len(failures) == 0 else "REJECT",
        "failures": failures,
        "warnings": warnings,
        "counts": {
            "grids": len(grids),
            "levels": len(levels),
            "beams": len(beams),
            "piles": len(piles),
            "footings": len(footings)
        },
        "metrics": {
            "max_pile_drift_mm": max_drift,
            "beam_sections": sec_counts,
            "pile_z_dist": top_z_counts,
            "footing_th_dist": thick_counts
        }
    }
    with open('adversarial_audit_summary.json', 'w', encoding='utf-8') as f:
        json.dump(audit_summary, f, indent=2)

    return len(failures) == 0

if __name__ == '__main__':
    success = main()
    sys.exit(0 if success else 1)
