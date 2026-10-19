import os
import re
import sys
import json

sys.stdout.reconfigure(encoding='utf-8')

# 1. Scan all [CommandMethod] in HPDcePro.AutoCad
autocad_src = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.AutoCad'
code_cmds = []

for root, dirs, files in os.walk(autocad_src):
    if 'obj' in root or 'bin' in root: continue
    for f in files:
        if f.endswith('.cs'):
            fpath = os.path.join(root, f)
            with open(fpath, 'r', encoding='utf-8') as csf:
                lines = csf.readlines()
            for idx, line in enumerate(lines):
                m = re.search(r'\[CommandMethod\(\s*"([^"]+)"\s*\)\]', line)
                if m:
                    rel_path = os.path.relpath(fpath, r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro')
                    code_cmds.append({
                        'cmd_name': m.group(1),
                        'file': rel_path.replace('\\', '/'),
                        'line': idx + 1
                    })

print(f"Total [CommandMethod] in HPDcePro.AutoCad: {len(code_cmds)}")

# 2. Parse Subsystem 1 in TRUTH_MATRIX.md
matrix_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md'
with open(matrix_path, 'r', encoding='utf-8') as f:
    matrix_content = f.read()

sec1_match = re.search(r'## 2\. Subsystem 1: AutoCAD <CommandMethod> Endpoints.*?\n(.*?)\n## 3\. Subsystem 2:', matrix_content, re.DOTALL)
sec1_text = sec1_match.group(1)
rows1 = [line.strip() for line in sec1_text.split('\n') if line.strip().startswith('|') and not line.strip().startswith('|---') and not 'Legacy Identifier' in line]

matrix_sub1 = {}
for row in rows1:
    parts = [p.strip() for p in row.split('|')[1:-1]]
    if len(parts) >= 6:
        cmd = parts[0].replace('`', '').strip()
        matrix_sub1[cmd] = {
            'subsystem': parts[1],
            'token': parts[2],
            'inspection': parts[3],
            'target': parts[4].replace('`', '').strip(),
            'status': parts[5].strip()
        }

print(f"Total commands in TRUTH_MATRIX.md Subsystem 1: {len(matrix_sub1)}")

# 3. For each code command, check where it lands in TRUTH_MATRIX.md
print("\n--- Cross-referencing implemented commands with TRUTH_MATRIX.md ---")
matched_mock = 0
case_mismatches = []
not_in_sub1 = []
mislabeled_as_uninspectable = []

for cc in code_cmds:
    cname = cc['cmd_name']
    cfile = cc['file']
    cline = cc['line']
    
    # Exact match
    if cname in matrix_sub1:
        entry = matrix_sub1[cname]
        target = entry['target']
        status = entry['status']
        if status == 'ĐANG LÀ MOCK':
            # Check target and line
            if f"{cfile}:{cline}" == target or cfile == target.split(':')[0]:
                matched_mock += 1
            else:
                print(f"Target mismatch for {cname}: matrix says {target}, code is {cfile}:{cline}")
        else:
            mislabeled_as_uninspectable.append((cname, cfile, cline, status, target))
    else:
        # Case insensitive match
        found_case = False
        for mc, mentry in matrix_sub1.items():
            if mc.lower() == cname.lower():
                found_case = True
                case_mismatches.append((cname, mc, cfile, cline, mentry['status'], mentry['target']))
                break
        if not found_case:
            not_in_sub1.append((cname, cfile, cline))

print(f"Correctly matched ĐANG LÀ MOCK: {matched_mock}")
print(f"Mislabeled as CHƯA GIẢI MÃ ĐƯỢC (exact name): {len(mislabeled_as_uninspectable)}")
for m in mislabeled_as_uninspectable:
    print(f"  {m}")

print(f"Case mismatches in name (and their matrix status): {len(case_mismatches)}")
for cm in case_mismatches:
    print(f"  Code: '{cm[0]}' vs Matrix: '{cm[1]}' in {cm[2]}:{cm[3]} -> Status: {cm[4]}, Target: {cm[5]}")

print(f"Not in Subsystem 1 at all: {len(not_in_sub1)}")
for n in not_in_sub1:
    print(f"  {n}")
