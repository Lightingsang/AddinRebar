import re
import json
import math
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Read Blueprint
with open('docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md', 'r', encoding='utf-8') as f:
    bp_text = f.read()

# Read Survey
with open('.agents/teamwork/teamwork_preview_spec_miner_survey_2/survey_dwg_shapes_report.md', 'r', encoding='utf-8') as f:
    sv_text = f.read()

# Read Iso_QHPlus
with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    qh_data = json.load(f)

bp_pattern = re.compile(r'\|\s*\*\*`dang([^`]+)`\*\*\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|')
bp_shapes = {}
for m in bp_pattern.finditer(bp_text):
    c, name, topo, params, form, hooks, revit = [x.strip() for x in m.groups()]
    bp_shapes[c] = {
        'name': name,
        'topology': topo,
        'params': params,
        'formula': form,
        'hooks': hooks,
        'revit': revit
    }

sv_pattern = re.compile(r'\|\s*\*\*`dang([^`]+)`\*\*\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|')
sv_shapes = {}
for m in sv_pattern.finditer(sv_text):
    c, name, topo, params, form, notes = [x.strip() for x in m.groups()]
    sv_shapes[c] = {
        'name': name,
        'topology': topo,
        'params': params,
        'formula': form,
        'notes': notes
    }

print("Loaded Blueprint shapes:", len(bp_shapes))
print("Loaded Survey shapes:", len(sv_shapes))

# Check every shape for:
# 1. Complete formula (is formula non-empty, valid expression?)
# 2. Parameters defined in 'params' column matching the variables in formula
# 3. Hook specifications (90, 135, 180 deg)
# 4. Any ambiguous or incomplete formula (e.g. \sum L_i where i is unspecified, or undefined symbols)
# 5. Check Iso_QHPlus correspondence where available

issues = []

for code, s in sorted(bp_shapes.items()):
    form = s['formula']
    params = s['params']
    
    # Check if formula is missing or placeholder
    if not form or form == '-' or 'TBD' in form:
        issues.append((code, "MISSING_FORMULA", f"Formula is empty or TBD: {form}"))
        
    # Check parameters
    # Common parameters in Kata: L1, L2, L3, L4, L5, L6, goc, hook, Cog, R, D, s, n
    # Let's inspect formulas with \sum L_i
    if r'\sum L_i' in form:
        # Check if params defines the exact L_i range
        # e.g. L_1..L_5 vs \sum L_i
        pass

    # Print summary of each shape
    # print(f"Shape {code:6s} | Params: {params:30s} | Formula: {form}")

# Detailed check of formulas
print("\n=== DETAILED SHAPE AUDIT ===")
for code, s in sorted(bp_shapes.items()):
    form = s['formula']
    params = s['params']
    hooks = s['hooks']
    
    # Look for undefined symbols or potential issues
    # Check hook references
    hook_refs = []
    if 'hook' in form or 'hook135' in form or 'hook180' in form or 'Cog' in form:
        hook_refs.append(form)
    
    # Check trigonometric or sqrt functions
    has_math = any(fn in form for fn in [r'\sin', r'\cos', r'\sqrt', r'\pi', r'\theta'])
    
    if has_math or hook_refs or r'\sum' in form or code in ['00', '08', '08a', '31', '34', '39', '40', '40a', '45', '45a', '67']:
        print(f"[{code:5s}] Formula: {form}")
        print(f"        Params:  {params}")
        print(f"        Hooks:   {hooks}")

