import os
import re

touched_files = [
    r'HPKata/HPKata.AutoCad/Commands/DimensionUtilityCommands.cs',
    r'HPKata/HPKata.AutoCad/Commands/LayoutCommands.cs',
    r'HPKata/HPKata.AutoCad/Controllers/DimensionUtilityController.cs',
    r'HPKata/HPKata.AutoCad/Controllers/QsTakeoffController.cs',
    r'HPKata/HPKata.Core/QuantitySurveying/Calculators/QsTakeoffCalculator.cs',
    r'HPKata/HPKata.Core/QuantitySurveying/Calculators/QsFormworkCalculator.cs',
    r'HPKata/HPKata.Core/DrawingLayout/Calculators/SheetArrangerCalculator.cs',
    r'HPKata/HPKata.Core.Tests/DrawingLayout/DrawingLayoutStressChallengeTests.cs',
    r'HPKata/HPKata.Core.Tests/QuantitySurveying/QsTakeoffCalculatorTests.cs',
    r'HPKata/HPKata.Core.Tests/QuantitySurveying/QsFormworkCalculatorTests.cs',
    r'HPKata/HPKata.AutoCad.Tests/Controllers/Phase5AutoCadTests.cs'
]

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'

print("=== 1. RULE C2: FILE LINE COUNTS (<= 300) ===")
c2_pass = True
for rel in touched_files:
    full_path = os.path.join(repo_root, rel)
    if not os.path.exists(full_path):
        print(f"MISSING: {rel}")
        c2_pass = False
        continue
    with open(full_path, 'r', encoding='utf-8') as f:
        lines = len(f.readlines())
    status = "PASS" if lines <= 300 else "FAIL"
    if lines > 300:
        c2_pass = False
    print(f"{lines:4d} lines | {status:4s} | {rel}")

print(f"\nRule C2 overall: {'PASS' if c2_pass else 'FAIL'}")

print("\n=== 2. SCANNING ALL HPKATA CS FILES FOR RULE C2 (<= 300) ===")
hpkata_root = os.path.join(repo_root, "HPKata")
c2_all_pass = True
c2_violations = []
for root, dirs, files in os.walk(hpkata_root):
    if "obj" in root or "bin" in root:
        continue
    for file in files:
        if file.endswith(".cs"):
            fpath = os.path.join(root, file)
            with open(fpath, 'r', encoding='utf-8') as f:
                lc = len(f.readlines())
            if lc > 300:
                c2_all_pass = False
                c2_violations.append((lc, os.path.relpath(fpath, repo_root)))

if c2_all_pass:
    print("ALL .cs files in HPKata are <= 300 lines! PASS")
else:
    print(f"Found {len(c2_violations)} files > 300 lines in HPKata:")
    for lc, p in c2_violations:
        print(f"  {lc:4d} lines | {p}")

print("\n=== 3. RULE M3: METHOD LENGTHS (<= 50 lines) IN TOUCHED FILES ===")
# Parse methods roughly by scanning braces or using Roslyn/regex
m3_pass = True
for rel in touched_files:
    full_path = os.path.join(repo_root, rel)
    with open(full_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # We can detect methods using simple line-by-line block matching
    lines = content.splitlines()
    in_method = False
    brace_depth = 0
    method_name = ""
    method_start = 0
    
    # Rough method detector
    for i, line in enumerate(lines):
        trimmed = line.strip()
        # Look for method signatures
        if not in_method and brace_depth == 2: # inside class (namespace=1, class=2)
            # check if line looks like method header
            if any(mod in trimmed for mod in ['public ', 'private ', 'protected ', 'internal ', 'static ']) and '(' in trimmed and not trimmed.startswith('//'):
                # Avoid properties or class declarations
                if not any(kw in trimmed for kw in ['class ', 'interface ', 'struct ', 'enum ', 'namespace ', '=>']):
                    method_name = trimmed.split('(')[0].split()[-1]
                    method_start = i + 1
                    in_method = True
        
        brace_depth += line.count('{') - line.count('}')
        if in_method and brace_depth <= 2:
            method_end = i + 1
            length = method_end - method_start + 1
            status = "PASS" if length <= 50 else "FAIL"
            if length > 50:
                print(f"Method {method_name} in {os.path.basename(rel)}: lines {method_start}-{method_end} ({length} lines) -> {status}")
                m3_pass = False
            in_method = False

print(f"Rule M3 status (rough scan): {'PASS' if m3_pass else 'FAIL'}")

print("\n=== 4. RULES Q-B1, Q-B2, R5 ===")
qb1_violations = []
qb2_violations = []
r5_violations = []

for rel in touched_files:
    full_path = os.path.join(repo_root, rel)
    with open(full_path, 'r', encoding='utf-8') as f:
        flines = f.readlines()
    for idx, l in enumerate(flines):
        # Q-B1: TODO, FIXME, HACK, NotImplementedException
        for bad_kw in ['TODO', 'FIXME', 'HACK', 'NotImplementedException']:
            if bad_kw in l:
                qb1_violations.append((rel, idx+1, bad_kw, l.strip()))
        # Q-B2: empty catch
        if 'catch' in l:
            # check following lines for empty block
            window = "".join(flines[idx:min(len(flines), idx+5)])
            if re.search(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', window):
                qb2_violations.append((rel, idx+1, "empty catch", l.strip()))
        # R5: 304.8 outside RevitUnitConverter.cs
        if '304.8' in l:
            if not rel.endswith('RevitUnitConverter.cs'):
                r5_violations.append((rel, idx+1, l.strip()))

print(f"Q-B1 (Dead code/TODOs): {len(qb1_violations)} violations")
for v in qb1_violations:
    print(f"  {v[0]}:{v[1]} - {v[2]}: {v[3]}")

print(f"Q-B2 (Empty catch): {len(qb2_violations)} violations")
for v in qb2_violations:
    print(f"  {v[0]}:{v[1]}: {v[3]}")

print(f"R5 (304.8 check): {len(r5_violations)} violations")
for v in r5_violations:
    print(f"  {v[0]}:{v[1]}: {v[2]}")

print("\n=== 5. HOST NEUTRALITY: HPKATA.CORE REFERENCES ===")
core_dir = os.path.join(repo_root, "HPKata", "HPKata.Core")
forbidden_refs = []
for root, dirs, files in os.walk(core_dir):
    for file in files:
        if file.endswith((".cs", ".csproj")):
            p = os.path.join(root, file)
            with open(p, 'r', encoding='utf-8') as f:
                c = f.read()
                if "Autodesk.AutoCAD" in c or "Autodesk.Revit" in c:
                    forbidden_refs.append(p)

print(f"HPKata.Core forbidden Autodesk references: {len(forbidden_refs)}")
for fr in forbidden_refs:
    print(f"  {fr}")
