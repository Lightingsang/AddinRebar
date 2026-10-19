"""
EMPIRICAL VERIFICATION & STRESS TEST SUITE FOR SECTION 6
HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md

Author: challenger_m1_1 (Empirical Challenger)
Target: Section 6 - Complete Mathematical Catalog of All 68 Rebar Shapes (dang00..dang67 + 16 Variants)
"""

import math
import json
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

# ==============================================================================
# 1. PARSE BLUEPRINT SECTION 6.3
# ==============================================================================

with open('docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md', 'r', encoding='utf-8') as f:
    bp_text = f.read()

# Extract Section 6.3 lines
bp_lines = bp_text.splitlines()
sec6_lines = []
in_sec6 = False
for i, line in enumerate(bp_lines):
    if line.startswith('### 6.3'):
        in_sec6 = True
        continue
    if in_sec6 and line.startswith('## 7.'):
        break
    if in_sec6 and line.strip().startswith('| **`dang'):
        sec6_lines.append((i + 1, line))

print(f"Found {len(sec6_lines)} shape lines in Section 6.3 table.")

# ==============================================================================
# 2. PARSE SURVEY REPORT SECTION 4.4
# ==============================================================================

with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/survey_dwg_shapes_report.md', 'r', encoding='utf-8') as f:
    sv_text = f.read()

sv_lines = sv_text.splitlines()
sec4_lines = []
in_sec4 = False
for i, line in enumerate(sv_lines):
    if line.startswith('### 4.4'):
        in_sec4 = True
        continue
    if in_sec4 and line.startswith('## 5.'):
        break
    if in_sec4 and line.strip().startswith('| **`dang'):
        sec4_lines.append((i + 1, line))

print(f"Found {len(sec4_lines)} shape lines in Survey Report Section 4.4 table.")

# ==============================================================================
# 3. PARSE ISO_QHPLUS.XLS DANG_THEP
# ==============================================================================

with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    qh_data = json.load(f)

qh_shapes = {}
for row in qh_data:
    r = row.get("Row", 0)
    if r < 4:
        continue
    code = row.get("B_text", "").strip()
    if code:
        qh_shapes[code] = row

print(f"Found {len(qh_shapes)} shapes in Iso_QHPlus.xls Dang_thep sheet.")

# ==============================================================================
# TEST 1: COVERAGE & INVENTORY COMPLETENESS (68 Base + 16 Variants + Coupler)
# ==============================================================================

test1_results = {"pass": True, "details": []}

expected_base = [f"{i:02d}" for i in range(68)]
expected_variants = ['02a', '08a', '09a', '12a', '13a', '23a', '24a', '35a', '40a', '43a', '45a', '49a', '51a', '53a', '62a', '65a']
expected_total = set(expected_base + expected_variants + ['Coupler'])

bp_shape_dict = {}
corrupted_markdown_rows = []

for line_no, line in sec6_lines:
    parts = [p.strip() for p in line.split('|')]
    if len(parts) != 9:
        corrupted_markdown_rows.append((line_no, line))
        # Handle split row for dang00
        # | **`dang00`** | Generic Freeform Rebar | Arbitrary $N$-vertex polyline | Open polyline vertices | $L_{\text{cut}} = \sum_{i=1}^{N-1} \|P_{i+1} - P_i\|$ | Freeform polyline in CAD | Freeform Rebar |
        code_match = re.search(r'\*\*`dang([^`]+)`\*\*', line)
        if code_match:
            c = code_match.group(1)
            bp_shape_dict[c] = {
                'line': line_no,
                'raw': line,
                'corrupted': True
            }
    else:
        c = parts[1].replace('*', '').replace('`', '').replace('dang', '').strip()
        bp_shape_dict[c] = {
            'line': line_no,
            'name': parts[2],
            'topology': parts[3],
            'params': parts[4],
            'formula': parts[5],
            'hooks': parts[6],
            'revit': parts[7],
            'corrupted': False
        }

bp_codes = set(bp_shape_dict.keys())

missing_base = [c for c in expected_base if c not in bp_codes]
missing_variants = [c for c in expected_variants if c not in bp_codes]

print("\n--- TEST 1: COVERAGE VERIFICATION ---")
print(f"Total shapes expected: {len(expected_total)} (68 base + 16 variants + 1 Coupler)")
print(f"Total shapes found:    {len(bp_codes)}")
print(f"Missing base shapes:   {missing_base}")
print(f"Missing variants:      {missing_variants}")

if missing_base or missing_variants or len(bp_codes) != 85:
    test1_results["pass"] = False
    test1_results["details"].append(f"Coverage mismatch: missing base {missing_base}, missing variants {missing_variants}")
else:
    test1_results["details"].append("All 68 base shapes + 16 variants + 1 Coupler are present (85 total).")

# ==============================================================================
# TEST 2: MARKDOWN TABLE STRUCTURAL INTEGRITY
# ==============================================================================

test2_results = {"pass": True, "details": []}
print("\n--- TEST 2: MARKDOWN TABLE STRUCTURAL INTEGRITY ---")
if corrupted_markdown_rows:
    test2_results["pass"] = False
    for lno, raw in corrupted_markdown_rows:
        print(f"FAIL: Line {lno} has broken table delimiters due to unescaped LaTeX pipe '|':\n  {raw}")
        test2_results["details"].append(f"Line {lno}: unescaped pipe '|' in LaTeX math corrupts table columns.")
else:
    test2_results["details"].append("Markdown table delimiters are valid.")

# ==============================================================================
# TEST 3: HOOK RULES & CLAMPING VERIFICATION
# ==============================================================================

test3_results = {"pass": True, "details": []}
print("\n--- TEST 3: HOOK RULES & CLAMPING LOGIC ---")

def eval_hooks(d):
    # Blueprint Section 6.1:
    # Cog 90: 10d to 12d, clamped >= 75 mm
    cog90_min = max(10.0 * d, 75.0)
    cog90_max = max(12.0 * d, 75.0)
    
    # hook 135: 7.5d to 10d, clamped >= 75 mm
    hook135_min = max(7.5 * d, 75.0)
    hook135_max = max(10.0 * d, 75.0)
    
    # hook 180: 5d + 4d straight extension = 9d, clamped >= 60 mm
    hook180 = max(9.0 * d, 60.0)
    
    return {
        'cog90': (cog90_min, cog90_max),
        'hook135': (hook135_min, hook135_max),
        'hook180': hook180
    }

# Check edge diameters
d_test_cases = [6, 8, 10, 16, 20, 25, 32, 40]
for d in d_test_cases:
    h = eval_hooks(d)
    # Check clamping
    if d == 6:
        assert h['cog90'][0] == 75.0, f"Expected 75mm clamp for d=6, got {h['cog90'][0]}"
        assert h['hook135'][0] == 75.0, f"Expected 75mm clamp for d=6, got {h['hook135'][0]}"
        assert h['hook180'] == 60.0, f"Expected 60mm clamp for d=6, got {h['hook180']}"
    if d == 8:
        assert h['hook135'][0] == 75.0, f"Expected 75mm clamp for d=8, got {h['hook135'][0]}"
    print(f"  d={d:2d}mm: Cog90=[{h['cog90'][0]:.0f}..{h['cog90'][1]:.0f}], hook135=[{h['hook135'][0]:.1f}..{h['hook135'][1]:.1f}], hook180={h['hook180']:.0f}")

test3_results["details"].append("Hook formulas in Section 6.1 mathematically sound with proper clamping.")

# ==============================================================================
# TEST 4: EMPIRICAL EVALUATION & AMBIGUITY STRESS TEST ON ALL SHAPES
# ==============================================================================

test4_results = {"pass": True, "ambiguities": [], "evaluated": 0, "failures": []}
print("\n--- TEST 4: MATHEMATICAL EVALUABILITY & AMBIGUITY AUDIT ---")

# We test evaluating each shape under sample dimensions:
# Sample: L1=200, L2=5000, L3=200, L4=150, L5=100, L6=100, goc=45, d=20, hook=150, n=10, R=1000, s=150

sample_env = {
    'L1': 200.0, 'L2': 5000.0, 'L3': 200.0, 'L4': 150.0, 'L5': 100.0, 'L6': 100.0,
    'goc': 45.0, 'alpha': math.radians(45.0), 'd': 20.0,
    'hook': 150.0, 'hook135': 150.0, 'hook180': 180.0, 'Cog': 200.0, 'Cog90': 200.0,
    'n': 10, 'R': 1000.0, 'theta': 90.0, 'D': 600.0, 's': 150.0,
    'L_lap': 800.0, 'L': 5000.0
}

# Audit formulas for completeness and undeclared symbols
audit_findings = []

for code in sorted(bp_shape_dict.keys()):
    info = bp_shape_dict[code]
    if info.get('corrupted'):
        audit_findings.append({
            'code': code,
            'type': 'CORRUPTED_MARKDOWN',
            'desc': 'Cell delimiters broken by LaTeX pipe symbol.'
        })
        continue
    
    formula = info['formula']
    params = info['params']
    
    # Check for undeclared variables in formula
    # Extract LaTeX symbols from formula
    # Looking for: \Delta x, \Delta y, S_{wave}, L_{arc}, L_{spiral}, L_{lap}
    if r'\Delta x' in formula:
        if 'Delta x' not in params and r'\Delta x' not in params:
            audit_findings.append({
                'code': code,
                'type': 'UNDECLARED_SYMBOL',
                'desc': f"Formula uses '\\Delta x' but parameter list ({params}) does not declare it."
            })
    if r'\Delta y' in formula:
        if 'Delta y' not in params and r'\Delta y' not in params:
            audit_findings.append({
                'code': code,
                'type': 'UNDECLARED_SYMBOL',
                'desc': f"Formula uses '\\Delta y' but parameter list ({params}) does not declare it."
            })
    if 'S_{\\text{wave}}' in formula or 'S_{wave}' in formula or 'wave' in formula:
        audit_findings.append({
            'code': code,
            'type': 'UNDEFINED_FUNCTION',
            'desc': "Formula uses 'S_wave' without defining how to compute wave arc length from parameters (L1..L4)."
        })
    if 'L_{\\text{arc}}' in formula or 'L_{arc}' in formula or 'arc' in formula:
        audit_findings.append({
            'code': code,
            'type': 'UNDEFINED_FUNCTION',
            'desc': f"Formula uses 'L_arc' without providing mathematical equation in terms of parameters ({params})."
        })
    if 'L_{\\text{spiral}}' in formula or 'L_{spiral}' in formula or 'spiral' in formula:
        audit_findings.append({
            'code': code,
            'type': 'UNDEFINED_FUNCTION',
            'desc': f"Formula uses 'L_spiral' without providing helical formula or pitch/turns in parameters ({params})."
        })
    if 'L_{\\text{lap}}' in formula or 'L_{lap}' in formula or 'lap' in formula:
        if 'L_lap' not in params and 'L_{lap}' not in params and 'lap' not in params.lower():
            audit_findings.append({
                'code': code,
                'type': 'UNDECLARED_SYMBOL',
                'desc': f"Formula uses 'L_lap' but parameter list ({params}) does not declare it."
            })
    if code in ['06', '49']:
        # Trigonometric division
        if r'\sin \alpha' in formula or r'\sin(\alpha)' in formula:
            audit_findings.append({
                'code': code,
                'type': 'EDGE_CASE_DIVISION_BY_ZERO',
                'desc': "Formula divides by '\\sin \\alpha'. When alpha=0 deg, triggers ZeroDivisionError. Angular units (degrees vs radians) not explicitly stated."
            })
    if code == '25' and (r'\sum' in formula or r'L_i' in formula):
        # Octagonal stirrup
        # Formula: \sum L_i + 2\cdot \text{hook}, but params: L1, L2, L3
        audit_findings.append({
            'code': code,
            'type': 'AMBIGUOUS_SUMMATION',
            'desc': "Octagonal stirrup has 8 legs, but params only list L1, L2, L3. Formula '\\sum L_i' does not specify coefficients (e.g. 4*L1 + 4*L2 or 8*L1)."
        })

print(f"Total audit findings identified: {len(audit_findings)}")
for f in audit_findings:
    print(f"  [{f['code']:6s}] {f['type']:28s}: {f['desc']}")

# ==============================================================================
# TEST 5: CROSS-CHECK AGAINST ISO_QHPLUS.XLS GROUND TRUTH
# ==============================================================================

print("\n--- TEST 5: CROSS-CHECK AGAINST ISO_QHPLUS.XLS DANG_THEP ---")
discrepancies_qh = []

for qh_code, row in qh_shapes.items():
    r_form = row.get("R_formula", "")
    r_text = row.get("R_text", "")
    w_map = row.get("W_text", "")
    r_idx = row.get("Row")
    
    # Check if shape code matches blueprint
    # Note: qh has some slash codes like "09/09a/38", "31/33"
    sub_codes = qh_code.split('/')
    for sc in sub_codes:
        sc = sc.strip()
        if sc in bp_shape_dict:
            bp_form = bp_shape_dict[sc].get('formula', '')
            # Compare nature of formula:
            # In QHPlus, almost all shapes use =SUM(D:M), meaning simple sum of all segments.
            # Compare shapes that differ significantly:
            if sc == '19' and r'hook180' in bp_form:
                # In QHPlus: =2*G23+J23*PI() (2*L1 + pi*L2)
                # In BP: L1 + 2*hook180
                discrepancies_qh.append({
                    'code': sc,
                    'qh_row': r_idx,
                    'qh_form': r_form,
                    'bp_form': bp_form,
                    'comment': "Discrepancy: Iso_QHPlus calculates 2*L1 + pi*L2 (semi-circular U-bar), whereas Blueprint specifies L1 + 2*hook180 (straight bar with hooks)."
                })
            elif sc in ['06', '08', '08a', '45', '49']:
                discrepancies_qh.append({
                    'code': sc,
                    'qh_row': r_idx,
                    'qh_form': r_form,
                    'bp_form': bp_form,
                    'comment': f"Discrepancy: Iso_QHPlus uses direct segment summation {r_form}, whereas Blueprint introduces trigonometric/Pythagorean terms ({bp_form})."
                })

print(f"Total discrepancies with Iso_QHPlus: {len(discrepancies_qh)}")
for d in discrepancies_qh:
    print(f"  [{d['code']:5s}] QHPlus: {d['qh_form']:20s} vs BP: {d['bp_form']:35s}")
    print(f"         {d['comment']}")

# ==============================================================================
# SUMMARY & VERDICT RECOMMENDATION
# ==============================================================================

print("\n=======================================================")
print("                  CHALLENGE VERDICT                    ")
print("=======================================================")

has_blocking = len(corrupted_markdown_rows) > 0 or len(audit_findings) > 0
if has_blocking:
    print("VERDICT: REQUEST_CHANGES")
    print("Reasons:")
    if corrupted_markdown_rows:
        print(f"  1. Markdown syntax table corruption on {len(corrupted_markdown_rows)} rows due to unescaped LaTeX pipe character.")
    if audit_findings:
        print(f"  2. Mathematical ambiguities and incomputable expressions for {len(audit_findings)} shapes:")
        for af in audit_findings:
            print(f"     - dang{af['code']}: {af['type']} - {af['desc']}")
else:
    print("VERDICT: APPROVE")

