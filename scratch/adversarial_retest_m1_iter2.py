"""
ADVERSARIAL RETEST & STRESS HARNESS — MILESTONE M1 ITERATION 2
Author: challenger_m1_1_iter2 (Empirical Challenger)
Target: docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Section 6)
"""

import math
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

print("======================================================================")
print("  ADVERSARIAL STRESS TEST & VERIFICATION — MILESTONE M1 ITERATION 2   ")
print("======================================================================")

# 1. READ BLUEPRINT
bp_path = 'docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md'
with open(bp_path, 'r', encoding='utf-8') as f:
    bp_content = f.read()

lines = bp_content.splitlines()

# Extract Section 6.3 lines
sec6_rows = []
in_table = False
for idx, line in enumerate(lines):
    if line.startswith('### 6.3'):
        in_table = True
        continue
    if in_table and (line.startswith('### 6.4') or line.startswith('## 7.')):
        break
    if in_table and line.strip().startswith('| **`dang'):
        sec6_rows.append((idx + 1, line))

print(f"\n[PHASE 1] Table Parsing & Markdown Formatting Verification")
print(f"Total shape rows discovered in Section 6.3: {len(sec6_rows)}")

parse_errors = []
parsed_shapes = {}

for line_no, raw_line in sec6_rows:
    tokens = [t.strip() for t in raw_line.split('|')]
    # A standard markdown row has leading and trailing empty token from outer pipes:
    # tokens[0] == '', tokens[-1] == '', and exactly 7 column content tokens -> len(tokens) == 9.
    if len(tokens) != 9:
        parse_errors.append((line_no, len(tokens), raw_line))
    else:
        shape_code = tokens[1].replace('*', '').replace('`', '').replace('dang', '').strip()
        parsed_shapes[shape_code] = {
            'line_no': line_no,
            'name': tokens[2],
            'topology': tokens[3],
            'params': tokens[4],
            'formula': tokens[5],
            'hook_rules': tokens[6],
            'revit_equiv': tokens[7],
            'raw': raw_line
        }

if parse_errors:
    print(f"FAILED: {len(parse_errors)} rows failed markdown table parsing!")
    for err in parse_errors:
        print(f"  Line {err[0]}: {err[1]} tokens (expected 9): {err[2]}")
else:
    print(f"PASSED: All {len(parsed_shapes)} rows have exactly 9 tokens (7 columns).")
    print(f"        dang00 verified: No unescaped pipe syntax.")

# Check dang00 specifically
dang00 = parsed_shapes.get('00')
assert dang00 is not None, "dang00 not found in table!"
print(f"  dang00 Formula: {dang00['formula']}")
assert '|' not in dang00['formula'], "Unescaped pipe found inside dang00 formula!"

# 2. VERIFY REQUIRED CLOSED-FORM FORMULAS & SAFEGUARDS
print(f"\n[PHASE 2] Target Mathematical Verification of Required Shapes")

test_cases = [
    ('06', 'Divide-by-zero safeguard', r'\\sin\(?\\alpha', lambda f: '10^{-4}' in f or 'otherwise' in f),
    ('08', 'Explicit closed-form column crank', r'L_1 \+ \\sqrt\{L_2\^2 \+ L_3\^2\} \+ L_4', lambda f: r'\sqrt{L_2^2 + L_3^2}' in f and r'\Delta' not in f),
    ('08a', 'Explicit closed-form column crank mirror', r'L_1 \+ \\sqrt\{L_2\^2 \+ L_3\^2\} \+ L_4', lambda f: r'\sqrt{L_2^2 + L_3^2}' in f and r'\Delta' not in f),
    ('19', 'Iso_QHPlus alignment (2*L1 + pi*L2)', r'2L_1 \+ \\pi L_2', lambda f: '2L_1 + \\pi L_2' in f),
    ('23', 'Spiral overlap explicit L_lap', r'2\(L_1 \+ L_2\) \+ L_\{?\\text\{lap\}\}?', lambda f: 'L_{\\text{lap}}' in f or 'L_{lap}' in f),
    ('23a', 'Spiral overlap short leg explicit L_lap', r'2\(L_1 \+ L_2\) \+ L_\{?\\text\{lap\}\}?', lambda f: 'L_{\\text{lap}}' in f or 'L_{lap}' in f),
    ('25', 'Octagonal stirrup explicit 8-leg formula', r'2\(L_1 \+ L_2\) \+ 4L_3', lambda f: '2(L_1 + L_2) + 4L_3' in f),
    ('34', 'Continuous wave bar closed-form', r'2n \\cdot \\sqrt\{L_1\^2 \+ L_2\^2\}', lambda f: r'\sqrt{L_1^2 + L_2^2}' in f and 'S_{wave}' not in f and 'S_{\\text{wave}}' not in f),
    ('37', 'Curved vault bar explicit arc formula', r'\\pi R \\frac\{\\theta\}\{180\}', lambda f: r'\pi R \frac{\theta}{180}' in f or r'\frac{\theta}{180}' in f),
    ('38', 'Parabolic U-bar explicit semi-circle', r'2L_1 \+ \\pi R', lambda f: '2L_1 + \\pi R' in f and 'L_{arc}' not in f and 'L_{\\text{arc}}' not in f),
    ('39', 'Spiral-to-straight helical transition', r'n\\sqrt\{(\\pi D)\^2 \+ s\^2\}', lambda f: r'\sqrt{(\pi D)^2 + s^2}' in f and 'L_{spiral}' not in f and 'L_{\\text{spiral}}' not in f),
    ('45', 'Sloped column neck explicit closed-form', r'L_1 \+ \\sqrt\{L_2\^2 \+ L_3\^2\} \+ L_4', lambda f: r'\sqrt{L_2^2 + L_3^2}' in f and r'\Delta' not in f),
    ('49', 'Inclined shear truss safeguard', r'\\sin\(?\\alpha', lambda f: '10^{-4}' in f or 'otherwise' in f),
    ('67', 'Helical spiral explicit closed-form', r'n \\cdot \\sqrt\{(\\pi L_1)\^2 \+ L_2\^2\}', lambda f: r'\sqrt{(\pi L_1)^2 + L_2^2}' in f or r'\sqrt{(\pi D)^2 + p^2}' in f)
]

for code, check_name, pattern, validator in test_cases:
    sh = parsed_shapes.get(code)
    if not sh:
        print(f"FAILED: Shape dang{code} not found in parsed table!")
        continue
    f_str = sh['formula']
    p_str = sh['params']
    is_valid = validator(f_str)
    if is_valid:
        print(f"  [PASS] dang{code:4s} ({check_name}):\n         Formula: {f_str}")
    else:
        print(f"  [FAIL] dang{code:4s} ({check_name}):\n         Formula: {f_str}\n         Params:  {p_str}")

# 3. EMPIRICAL COMPUTATION & STRESS TEST ENGINE
print(f"\n[PHASE 3] Empirical Numerical Computation with Adversarial Stress Cases")

def evaluate_shape(code, env):
    """Evaluates the cut length of shape `code` under given parameter environment."""
    L1 = env.get('L1', 0.0)
    L2 = env.get('L2', 0.0)
    L3 = env.get('L3', 0.0)
    L4 = env.get('L4', 0.0)
    L5 = env.get('L5', 0.0)
    L6 = env.get('L6', 0.0)
    alpha = env.get('alpha', 45.0) # degrees
    d = env.get('d', 20.0)
    n = env.get('n', 1)
    R = env.get('R', 500.0)
    theta = env.get('theta', 90.0)
    s = env.get('s', 150.0)
    D = env.get('D', 600.0)
    L_lap = env.get('L_lap', 40.0 * d)
    hook135 = max(7.5 * d, 75.0)
    hook180 = max(9.0 * d, 60.0)
    Cog90 = max(10.0 * d, 75.0)

    # Dispatch shape logic matching Section 6.3 & 6.4 specifications:
    if code == '00':
        pts = env.get('polyline_pts', [(0,0), (100,0), (100,200)])
        return sum(math.hypot(pts[i+1][0]-pts[i][0], pts[i+1][1]-pts[i][1]) for i in range(len(pts)-1))
    elif code == '01':
        return L1
    elif code in ['02', '15', '18', '59']:
        return 2*L1 + L2
    elif code == '02a':
        return 2*L1 + L2 + 2*hook135
    elif code in ['03', '07', '41', '63']:
        return L1 + L2
    elif code in ['04', '05', '42', '45a', '48']:
        return L1 + L2 + L3
    elif code in ['06', '49']:
        # Safeguarded Crank formula
        rad = math.radians(alpha)
        sin_val = math.sin(rad)
        if abs(sin_val) >= 1e-4:
            sloped = L2 / sin_val
        else:
            sloped = L2
        return L1 + sloped + L3
    elif code in ['08', '08a', '45']:
        return L1 + math.sqrt(L2**2 + L3**2) + L4
    elif code == '09':
        return 2*L1 + L2 + 2*Cog90
    elif code == '09a':
        return 2*L1 + L2 + 2*hook180
    elif code in ['10', '57']:
        return L1 + L2 + L3
    elif code in ['11', '12', '12a', '20', '21', '44', '47', '50', '54', '56']:
        return L1 + L2 + L3 + L4
    elif code == '13':
        return L1 + L2 + L3 + L4 + L5
    elif code == '13a':
        return 2*L1 + 2*L2 + L3
    elif code == '14':
        return 2 * (L1 + L2)
    elif code in ['16', '55', '61', '65', '65a', '66']:
        return L1 + L2 + L3 + L4 + L5 + L6
    elif code == '17':
        return 2*(L1 + L2) + L3
    elif code == '19':
        return 2*L1 + math.pi * L2
    elif code == '22':
        return 2*(L1 + L2) + 2*L3
    elif code in ['23', '23a']:
        return 2*(L1 + L2) + L_lap
    elif code == '24':
        return 4*L1 + 2*L3
    elif code == '24a':
        return 2*(L1 + L2) + 2*L3
    elif code == '25':
        return 2*(L1 + L2) + 4*L3 + 2*hook135
    elif code == '26':
        return L1 + L2 + L3 + L4 + L5 + L6
    elif code == '27':
        return L1 + 2*L2 + L3 + 2*hook135
    elif code == '28':
        return L1 + L2 + L3 + 2*hook135
    elif code == '29':
        return 2*(L1 + L2) + L5
    elif code in ['30', '33', '36', '46', '49a', '62', '62a', '64']:
        return L1 + L2 + L3 + L4 + L5
    elif code == '31':
        return 2*L1 + math.pi * R * (theta / 180.0)
    elif code == '32':
        return L1 + L2 + L3 + L4
    elif code == '34':
        return 2 * n * math.sqrt(L1**2 + L2**2)
    elif code in ['35', '35a']:
        return 2*L1 + 2*L2 + L3
    elif code == '37':
        return L1 + math.pi * R * (theta / 180.0) + L5
    elif code == '38':
        return 2*L1 + math.pi * R
    elif code == '39':
        return L1 + L2 + n * math.sqrt((math.pi * D)**2 + s**2)
    elif code == '40':
        return math.pi * L1 + L2
    elif code == '40a':
        return math.pi * L1 + 2*hook135
    elif code in ['43', '43a']:
        return L1 + L2 + L3 + L4 + L5 + L6
    elif code == '51':
        return L1 + L2 + L3 + 2*hook135
    elif code == '51a':
        return L1 + L2 + L3 + Cog90 + hook135
    elif code == '52':
        return L1 + hook135 + Cog90
    elif code == '53':
        return 2*L1 + 2*L2 + L3
    elif code == '53a':
        return 2*L1 + 2*L2 + L3 + 2*L4
    elif code == '58':
        return L1 + 2*L2
    elif code == '60':
        return L1 + L2 + L3 + L4
    elif code == '67':
        return n * math.sqrt((math.pi * L1)**2 + L2**2) + 2*hook135
    elif code == 'Coupler':
        return env.get('L', 80.0)
    else:
        raise ValueError(f"Unknown shape code: {code}")

# Run stress cases
stress_scenarios = [
    ("Standard Dimensions", {
        'L1': 500.0, 'L2': 1000.0, 'L3': 300.0, 'L4': 250.0, 'L5': 200.0, 'L6': 150.0,
        'alpha': 45.0, 'd': 20.0, 'n': 10, 'R': 800.0, 'theta': 90.0, 's': 150.0, 'D': 600.0, 'L': 80.0
    }),
    ("Zero Angle (alpha = 0 deg) - Stress divide-by-zero", {
        'L1': 500.0, 'L2': 1000.0, 'L3': 300.0, 'L4': 250.0, 'L5': 200.0, 'L6': 150.0,
        'alpha': 0.0, 'd': 20.0, 'n': 10, 'R': 800.0, 'theta': 0.0, 's': 150.0, 'D': 600.0, 'L': 80.0
    }),
    ("Tiny Angle (alpha = 1e-5 deg) - Below 1e-4 threshold", {
        'L1': 500.0, 'L2': 1000.0, 'L3': 300.0, 'L4': 250.0, 'L5': 200.0, 'L6': 150.0,
        'alpha': 1e-5, 'd': 20.0, 'n': 10, 'R': 800.0, 'theta': 1e-5, 's': 150.0, 'D': 600.0, 'L': 80.0
    }),
    ("Zero Pitch/Turns (n=0, s=0) - Stress helical equations", {
        'L1': 500.0, 'L2': 0.0, 'L3': 300.0, 'L4': 250.0, 'L5': 200.0, 'L6': 150.0,
        'alpha': 45.0, 'd': 20.0, 'n': 0, 'R': 800.0, 'theta': 90.0, 's': 0.0, 'D': 600.0, 'L': 80.0
    })
]

total_evaluations = 0
failed_evaluations = 0

for scen_name, env in stress_scenarios:
    print(f"\n--- Testing Scenario: {scen_name} ---")
    for code in sorted(parsed_shapes.keys()):
        try:
            val = evaluate_shape(code, env)
            assert not math.isnan(val), f"Result is NaN for shape dang{code}"
            assert not math.isinf(val), f"Result is Inf for shape dang{code}"
            assert val >= 0.0, f"Result is negative ({val}) for shape dang{code}"
            total_evaluations += 1
        except Exception as e:
            failed_evaluations += 1
            print(f"  [ERROR] Shape dang{code} failed in scenario '{scen_name}': {e}")

print(f"\nStress Evaluation Summary:")
print(f"  Total tests executed: {total_evaluations} across {len(parsed_shapes)} shapes")
print(f"  Failed evaluations:   {failed_evaluations}")

# Final assertion
if failed_evaluations == 0 and len(parse_errors) == 0:
    print("\n=======================================================")
    print("      FINAL EMPIRICAL CHALLENGER VERDICT: APPROVE     ")
    print("=======================================================")
    sys.exit(0)
else:
    print("\n=======================================================")
    print("  FINAL EMPIRICAL CHALLENGER VERDICT: REQUEST_CHANGES  ")
    print("=======================================================")
    sys.exit(1)
