import re
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

# 1. Parse Blueprint Section 6.3 table
with open('docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md', 'r', encoding='utf-8') as f:
    bp_text = f.read()

# Extract section 6.3 table
bp_shapes = {}
bp_pattern = re.compile(r'\|\s*\*\*`dang([^`]+)`\*\*\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|')
for match in bp_pattern.finditer(bp_text):
    code, name, topo, params, formula, hooks, revit = [m.strip() for m in match.groups()]
    bp_shapes[code] = {
        'code': code,
        'name': name,
        'topology': topo,
        'params': params,
        'formula': formula,
        'hooks': hooks,
        'revit': revit
    }

print(f"Total shapes in Blueprint Section 6.3: {len(bp_shapes)}")

# 2. Parse survey_dwg_shapes_report.md Section 4.4 table
with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/survey_dwg_shapes_report.md', 'r', encoding='utf-8') as f:
    survey_text = f.read()

survey_shapes = {}
survey_pattern = re.compile(r'\|\s*\*\*`dang([^`]+)`\*\*\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|')
for match in survey_pattern.finditer(survey_text):
    code, name, topo, params, formula, notes = [m.strip() for m in match.groups()]
    survey_shapes[code] = {
        'code': code,
        'name': name,
        'topology': topo,
        'params': params,
        'formula': formula,
        'notes': notes
    }

print(f"Total shapes in Survey Report Section 4.4: {len(survey_shapes)}")

# 3. Parse Iso_QHPlus.xls Dang_thep sheet
with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    qh_data = json.load(f)

qh_shapes = {}
for row in qh_data:
    r = row.get("Row", 0)
    if r < 4:
        continue
    shape_code = row.get("B_text", "").strip()
    if not shape_code:
        continue
    r_formula = row.get("R_formula", "").strip()
    w_map = row.get("W_text", "").strip()
    qh_shapes[shape_code] = {
        'row': r,
        'code': shape_code,
        'formula': r_formula,
        'map': w_map,
        'row_data': row
    }

print(f"Total shape rows in Iso_QHPlus Dang_thep: {len(qh_shapes)}")

# Compare shape sets
print("\n=== BLUEPRINT vs SURVEY REPORT SHAPES ===")
bp_codes = set(bp_shapes.keys())
survey_codes = set(survey_shapes.keys())

only_bp = bp_codes - survey_codes
only_survey = survey_codes - bp_codes
common = bp_codes & survey_codes

print(f"Common: {len(common)}")
print(f"Only in Blueprint: {sorted(only_bp)}")
print(f"Only in Survey: {sorted(only_survey)}")

# Check 68 base shapes (00 to 67)
base_68 = [f"{i:02d}" for i in range(68)]
missing_base_bp = [c for c in base_68 if c not in bp_codes]
print(f"\nMissing base shapes (00..67) in Blueprint: {missing_base_bp}")

# Check 16 variants
variants_in_bp = [c for c in bp_codes if c.endswith('a') or not c.isdigit()]
print(f"\nVariants in Blueprint: {sorted(variants_in_bp)} (count: {len(variants_in_bp)})")

# Check formulas between Blueprint and Survey Report
print("\n=== FORMULA COMPARISON: BLUEPRINT vs SURVEY REPORT ===")
diff_formulas = []
for code in sorted(common):
    bp_f = bp_shapes[code]['formula']
    sv_f = survey_shapes[code]['formula']
    if bp_f != sv_f:
        diff_formulas.append((code, bp_f, sv_f))

print(f"Formulas with different text representations: {len(diff_formulas)}")
for code, bf, sf in diff_formulas:
    print(f"Shape {code:6s} | BP: {bf}  <===>  SV: {sf}")
