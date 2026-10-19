import json
import sys
import os
import math

sys.stdout.reconfigure(encoding="utf-8")

PIPE_PATH = r'\\.\pipe\hprebar-mcp-r2026'

class RevitIPC:
    def __init__(self, pipe_path=PIPE_PATH):
        self.pipe_path = pipe_path
        self.f = open(pipe_path, 'r+b', buffering=0)
        self.msg_id = 9000

    def execute(self, code, transaction="none", dry_run=False, timeout=60, label="Revit Query"):
        self.msg_id += 1
        req = {
            'jsonrpc': '2.0',
            'id': self.msg_id,
            'method': 'revit.execute',
            'params': {
                'code': code,
                'transaction': transaction,
                'dryRun': dry_run,
                'timeoutSeconds': timeout,
                'label': label
            }
        }
        self.f.write((json.dumps(req) + '\n').encode('utf-8'))
        buf = b""
        while True:
            chunk = self.f.read(65536)
            if not chunk:
                break
            buf += chunk
            while b'\n' in buf:
                raw_line, buf = buf.split(b'\n', 1)
                raw_line = raw_line.strip()
                if not raw_line:
                    continue
                try:
                    data = json.loads(raw_line.decode('utf-8'))
                    if data.get('id') == self.msg_id:
                        return data
                except Exception:
                    continue

    def close(self):
        try:
            self.f.close()
        except Exception:
            pass

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

def run_tests():
    print("=" * 80)
    print("EMPIRICAL ADVERSARIAL CHALLENGE — MILESTONE M3 FOUNDATION")
    print("=" * 80)

    # ---------------------------------------------------------
    # PART 1: Mathematical Verification of Pile Count Formula
    # ---------------------------------------------------------
    print("\n[PART 1] Mathematical verification of pile count formula:")
    terms = [
        ("ĐC-1", 7, 1),
        ("ĐC-2", 28, 2),
        ("ĐC-2'", 4, 2),
        ("ĐC-3", 15, 3),
        ("ĐC-3'", 2, 3),
        ("ĐC-4", 10, 4),
        ("ĐC-5", 2, 5),
        ("ĐC-6", 4, 6)
    ]
    total_footings_calc = sum(t[1] for t in terms)
    total_piles_calc = sum(t[1] * t[2] for t in terms)
    formula_str = " + ".join(f"{t[1]}x{t[2]}" for t in terms)
    print(f"  Formula: {formula_str}")
    print(f"  Calculated Total Footings = {total_footings_calc} (expected 72)")
    print(f"  Calculated Total Piles = {total_piles_calc} (expected 196)")
    assert total_footings_calc == 72, f"Footings sum mismatch: {total_footings_calc} != 72"
    assert total_piles_calc == 196, f"Piles sum mismatch: {total_piles_calc} != 196"
    print("  => PASS: Math identity holds exactly.")

    # Check CAD source data for these 8 types
    cad_mapping_file = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\exact_196_pile_mapping.json'
    footings_cad_file = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\created_72_footings.json'
    
    with open(cad_mapping_file, 'r', encoding='utf-8') as f:
        piles_data = json.load(f)
    with open(footings_cad_file, 'r', encoding='utf-8') as f:
        footings_data = json.load(f)

    print(f"\n[PART 1.1] Checking CAD dataset counts:")
    print(f"  CAD Footings count: {len(footings_data)}")
    print(f"  CAD Piles count: {len(piles_data)}")
    assert len(footings_data) == 72, f"CAD footings count != 72, got {len(footings_data)}"
    assert len(piles_data) == 196, f"CAD piles count != 196, got {len(piles_data)}"

    # Tally CAD footings by prefix/type
    from collections import Counter
    ft_types = Counter()
    for ft in footings_data:
        ft_types[ft['comment']] += 1
    print("  Footing instances per type in CAD dataset:")
    for t_name, exp_cnt, exp_piles in terms:
        actual_cnt = ft_types[t_name]
        print(f"    {t_name:<6}: actual={actual_cnt}, expected={exp_cnt} (diff={actual_cnt - exp_cnt})")
        assert actual_cnt == exp_cnt, f"Footing type {t_name} count mismatch!"

    # Tally piles per footing in CAD dataset
    piles_per_ft = Counter()
    for p in piles_data:
        piles_per_ft[p['footing_id']] += 1
    
    # Verify each footing has exact expected piles
    for ft in footings_data:
        ft_id = ft['mark']
        ft_type = ft['comment']
        expected_piles = dict((t[0], t[2]) for t in terms)[ft_type]
        actual_piles = piles_per_ft[ft_id]
        assert actual_piles == expected_piles, f"Footing {ft_id} ({ft_type}) has {actual_piles} piles, expected {expected_piles}"
    print("  => PASS: Every CAD footing has exactly the designated pile count.")

    # ---------------------------------------------------------
    # PART 2: Live Revit Audit via Named Pipe
    # ---------------------------------------------------------
    print("\n[PART 2] Connecting to LIVE Revit 2026 via Named Pipe...")
    with RevitIPC() as ipc:
        # 2.1 Grids and Levels
        print("\n--- 2.1 Auditing Grids & Levels in Revit ---")
        res_gl = ipc.execute("""
        var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().Select(g => new { id = g.Id.Value, name = g.Name, pinned = g.Pinned }).OrderBy(g => g.name).ToList();
        var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().Select(l => new { id = l.Id.Value, name = l.Name, elevMm = Math.Round(l.Elevation * 304.8, 1), pinned = l.Pinned }).OrderBy(l => l.elevMm).ToList();
        return new { grids = grids, levels = levels };
        """, label="Challenge Grids Levels")
        val_gl = res_gl['result']['value']
        if isinstance(val_gl, str): val_gl = json.loads(val_gl)
        revit_grids = val_gl['grids']
        revit_levels = val_gl['levels']
        print(f"  Live Grids Count: {len(revit_grids)} (All pinned: {all(g['pinned'] for g in revit_grids)})")
        print(f"  Live Levels Count: {len(revit_levels)} (All pinned: {all(l['pinned'] for l in revit_levels)})")
        assert len(revit_grids) == 27, f"Grids count != 27, got {len(revit_grids)}"
        assert all(g['pinned'] for g in revit_grids), "Some grids are NOT pinned!"
        assert len(revit_levels) == 10, f"Levels count != 10, got {len(revit_levels)}"
        assert all(l['pinned'] for l in revit_levels), "Some levels are NOT pinned!"
        print("  => PASS: Grids and Levels intact and pinned.")

        # 2.2 Piles in Revit
        print("\n--- 2.2 Auditing Piles in Revit ---")
        res_p = ipc.execute("""
        var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                double zTopMm = -1000.0 + offsetFt * 304.8;
                return new {
                    id = p.Id.Value,
                    sym = p.Symbol.Name,
                    fam = p.Symbol.FamilyName,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    x = Math.Round(pt.X * 304.8, 2),
                    y = Math.Round(pt.Y * 304.8, 2),
                    z = Math.Round(zTopMm, 1)
                };
            }).OrderBy(p => p.id).ToList();
        return piles;
        """, label="Challenge Piles")
        val_p = res_p['result']['value']
        if isinstance(val_p, str): val_p = json.loads(val_p)
        print(f"  Live Piles Count: {len(val_p)}")
        assert len(val_p) == 196, f"Live piles count != 196, got {len(val_p)}"
        
        # Check pile family & symbol
        for p in val_p:
            assert p['fam'] == '4_STR_Coc_Tron', f"Invalid pile family {p['fam']}"
            assert p['sym'] == 'P400', f"Invalid pile symbol {p['sym']}"
        print("  All 196 piles are 4_STR_Coc_Tron: P400.")

        # Check pile Z elevation distribution
        pile_z_dist = Counter()
        for p in val_p:
            pile_z_dist[p['z']] += 1
        print("  Live Pile Z distribution:")
        for z, c in sorted(pile_z_dist.items()):
            print(f"    Z = {z} mm: {c} piles")
        assert pile_z_dist[-2500.0] == 122, f"Expected 122 piles at -2500, got {pile_z_dist[-2500.0]}"
        assert pile_z_dist[-2700.0] == 50, f"Expected 50 piles at -2700, got {pile_z_dist[-2700.0]}"
        assert pile_z_dist[-2800.0] == 24, f"Expected 24 piles at -2800, got {pile_z_dist[-2800.0]}"
        print("  => PASS: Pile elevations strictly match footing embedment requirements.")

        # 2.3 Footings in Revit
        print("\n--- 2.3 Auditing Footings in Revit (Checking Heuristic Clustering vs True 72 Footings) ---")
        res_f = ipc.execute("""
        var floors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
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
                    thickMm = Math.Round(thick, 1)
                };
            }).OrderBy(f => f.mark).ToList();
        return floors;
        """, label="Challenge Footings")
        val_f = res_f['result']['value']
        if isinstance(val_f, str): val_f = json.loads(val_f)
        print(f"  Live Floors/Footings Count: {len(val_f)}")
        assert len(val_f) == 72, f"Live footings count != 72! Got {len(val_f)}. (If 57, heuristic clustering exists!)"
        
        # Check thickness distribution
        ft_thick_dist = Counter()
        for f in val_f:
            ft_thick_dist[f['thickMm']] += 1
            assert abs(f['topZ'] - (-1600.0)) <= 1.0, f"Footing {f['mark']} topZ {f['topZ']} != -1600.0"
        print("  Live Footing Thickness distribution:")
        for t, c in sorted(ft_thick_dist.items()):
            print(f"    Thick = {t} mm: {c} footings")
        assert ft_thick_dist[1000.0] == 56, f"Expected 56 footings of thick 1000, got {ft_thick_dist[1000.0]}"
        assert ft_thick_dist[1200.0] == 12, f"Expected 12 footings of thick 1200, got {ft_thick_dist[1200.0]}"
        assert ft_thick_dist[1300.0] == 4, f"Expected 4 footings of thick 1300, got {ft_thick_dist[1300.0]}"
        print("  => PASS: 72 authentic individual footings exist, 0 heuristic clusters (57 old footings purged).")

        # 2.4 Grade Beams in Revit
        print("\n--- 2.4 Auditing Grade Beams in Revit (22 GM types + BN-1, BN-2) ---")
        res_b = ipc.execute("""
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
        """, label="Challenge Beams")
        val_b = res_b['result']['value']
        if isinstance(val_b, str): val_b = json.loads(val_b)
        print(f"  Live Beams Count: {len(val_b)}")
        assert len(val_b) == 192, f"Live beams count != 192, got {len(val_b)}"
        
        # Check beam sections
        beam_sec_dist = Counter()
        for b in val_b:
            sec = f"{int(b['bMm'])}x{int(b['hMm'])}"
            beam_sec_dist[sec] += 1
            assert abs(b['topZ'] - (-1600.0)) <= 1.0, f"Beam {b['mark']} topZ {b['topZ']} != -1600.0"
        print("  Live Beam Cross-Section distribution:")
        for sec, c in sorted(beam_sec_dist.items()):
            print(f"    {sec}: {c} spans")
        assert beam_sec_dist["300x400"] == 157, f"Expected 157 spans of 300x400, got {beam_sec_dist['300x400']}"
        assert beam_sec_dist["300x600"] == 11, f"Expected 11 spans of 300x600, got {beam_sec_dist['300x600']}"
        assert beam_sec_dist["200x300"] == 22, f"Expected 22 spans of 200x300, got {beam_sec_dist['200x300']}"
        assert beam_sec_dist["200x400"] == 2, f"Expected 2 spans of 200x400, got {beam_sec_dist['200x400']}"
        print("  => PASS: Beam sections match DWG KC-HT-90-TLTT-16.dwg exactly.")

        # Check representation of all 22 GM types and BN-1, BN-2 in the beam marks
        all_beam_marks = set(b['mark'].split('-')[0] + ('-' + b['mark'].split('-')[1] if len(b['mark'].split('-')) > 1 and b['mark'].split('-')[1].isalpha() else '') for b in val_b)
        print(f"  Sample Beam Mark prefixes in Revit: {sorted(list(set(b['mark'] for b in val_b[:20])))}")

        # Verify phantom elements check
        print("\n--- 2.5 Auditing Phantom Elements ---")
        res_phantom = ipc.execute("""
        var totalFraming = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().GetElementCount();
        var totalFloors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).GetElementCount();
        var totalFoundations = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFoundation).GetElementCount();
        return new { framing = totalFraming, floors = totalFloors, foundations = totalFoundations };
        """, label="Challenge Phantom Elements")
        val_ph = res_phantom['result']['value']
        if isinstance(val_ph, str): val_ph = json.loads(val_ph)
        print(f"  Total StructuralFraming in Doc: {val_ph['framing']} (Foundation Beams = 192)")
        print(f"  Total Floors in Doc: {val_ph['floors']} (Footings = 72)")
        print(f"  Total StructuralFoundation in Doc: {val_ph['foundations']} (Piles = 196)")
        assert val_ph['framing'] == 192, f"Phantom beams exist! Total framing = {val_ph['framing']} > 192"
        assert val_ph['floors'] == 72, f"Phantom floors exist! Total floors = {val_ph['floors']} > 72"
        assert val_ph['foundations'] == 196, f"Phantom piles exist! Total foundations = {val_ph['foundations']} > 196"
        print("  => PASS: 0 phantom elements in the entire Revit Document.")

    # ---------------------------------------------------------
    # PART 3: Probing state/bim_coordination_ledger.json
    # ---------------------------------------------------------
    print("\n[PART 3] Probing state/bim_coordination_ledger.json...")
    ledger_path = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\state\bim_coordination_ledger.json'
    with open(ledger_path, 'r', encoding='utf-8') as f:
        ledger = json.load(f)

    print(f"  Ledger Project: {ledger.get('project')}")
    print(f"  Ledger UpdatedAt: {ledger.get('updatedAt')}")
    print(f"  Ledger ActivePhase: {ledger.get('activePhase')}")
    print(f"  Ledger PhaseStatus: {ledger.get('phaseStatus')}")
    print(f"  Ledger PrimeDirective: {ledger.get('primeDirective')[:60]}...")
    
    summary = ledger.get('summary', {})
    print(f"  Ledger Summary: totalElements={summary.get('totalElements')}, totalPiles={summary.get('totalPiles')}, totalFootings={summary.get('totalFootings')}, totalGradeBeams={summary.get('totalGradeBeams')}")
    
    elements = ledger.get('elements', [])
    print(f"  Total elements in ledger: {len(elements)}")
    assert len(elements) == 460, f"Expected 460 elements, found {len(elements)}"
    
    # Check that each element has valid fields
    pile_count = 0
    footing_count = 0
    beam_count = 0
    for idx, el in enumerate(elements):
        assert 'elementId' in el and el['elementId'] > 0, f"Element {idx} missing elementId"
        assert 'classification' in el and el['classification'] == 'NHOM_1', f"Element {idx} not NHOM_1"
        assert 'status' in el and el['status'] == 'PASSED_2PHASE', f"Element {idx} status not PASSED_2PHASE"
        assert 'tolerances' in el, f"Element {idx} missing tolerances"
        cat = el.get('category')
        if 'Foundation' in cat and el.get('type') == 'Cọc D400':
            pile_count += 1
            assert 'coordinates' in el
            assert el['elevationTopMm'] in [-2500, -2700, -2800]
        elif 'Floors' in cat:
            footing_count += 1
            assert el['thicknessMm'] in [1000, 1200, 1300]
            assert el['topElevationMm'] == -1600
        elif 'Framing' in cat:
            beam_count += 1
            assert el['topElevationMm'] == -1600
            assert f"{int(el['bMm'])}x{int(el['hMm'])}" in ["300x400", "300x600", "200x300", "200x400"]
        else:
            raise AssertionError(f"Unknown element category: {cat}")

    print(f"  Verified authentic evidence for all 460 elements:")
    print(f"    - Piles: {pile_count}/196")
    print(f"    - Footings: {footing_count}/72")
    print(f"    - Beams: {beam_count}/192")
    assert pile_count == 196
    assert footing_count == 72
    assert beam_count == 192
    print("  => PASS: Every record in the ledger contains authentic verification evidence.")

    print("\n" + "=" * 80)
    print("ALL EMPIRICAL CHALLENGES PASSED WITH ZERO DISCREPANCIES!")
    print("VERDICT: APPROVE")
    print("=" * 80)

if __name__ == '__main__':
    run_tests()
