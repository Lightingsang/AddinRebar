import sys
import os
import re
import json

sys.stdout.reconfigure(encoding='utf-8')

matrix_path = r'docs/rea-dce-pro/TRUTH_MATRIX.md'
with open(matrix_path, 'r', encoding='utf-8') as f:
    matrix_content = f.read()

# Subsystem 1 rows
sec1_match = re.search(r'## 2\. Subsystem 1: AutoCAD <CommandMethod> Endpoints.*?\n(.*?)\n## 3\. Subsystem 2:', matrix_content, re.DOTALL)
sec1_text = sec1_match.group(1)
rows1 = [line.strip() for line in sec1_text.split('\n') if line.strip().startswith('|') and not line.strip().startswith('|---') and not 'Legacy Identifier' in line]

mocks_sub1 = []
uninspected_sub1 = []

for row in rows1:
    parts = [p.strip() for p in row.split('|')[1:-1]]
    if len(parts) >= 6:
        cmd = parts[0].replace('`', '').strip()
        subsys = parts[1]
        token = parts[2]
        insp = parts[3]
        target = parts[4].replace('`', '').strip()
        status = parts[5].strip()
        
        entry = {
            'cmd': cmd, 'token': token, 'insp': insp, 'target': target, 'status': status
        }
        if status == 'ĐANG LÀ MOCK':
            mocks_sub1.append(entry)
        elif status == 'CHƯA GIẢI MÃ ĐƯỢC':
            uninspected_sub1.append(entry)
        else:
            print(f"Unknown status: {status} for {cmd}")

print(f"Subsystem 1 - Total ĐANG LÀ MOCK: {len(mocks_sub1)}")
print(f"Subsystem 1 - Total CHƯA GIẢI MÃ ĐƯỢC: {len(uninspected_sub1)}")

# 1. Verify file paths and line numbers of mocks_sub1
missing_files = []
line_mismatches = []
valid_mocks = []

for m in mocks_sub1:
    target = m['target']
    # Target may be path:line or path
    if ':' in target:
        path, line_str = target.rsplit(':', 1)
        try:
            line_num = int(line_str)
        except ValueError:
            path = target
            line_num = None
    else:
        path = target
        line_num = None
        
    full_path = os.path.join(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro', path)
    if not os.path.exists(full_path):
        # try without HPDcePro prefix if path already has it
        full_path2 = os.path.join(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar', path)
        if os.path.exists(full_path2):
            full_path = full_path2
        else:
            missing_files.append((m['cmd'], target, full_path))
            continue
            
    # Read file lines and check near line_num
    with open(full_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    if line_num is not None:
        if line_num <= len(lines):
            # Inspect snippet around line_num (e.g. line_num - 2 to line_num + 5)
            snippet = "".join(lines[max(0, line_num-3):min(len(lines), line_num+5)])
            valid_mocks.append((m['cmd'], path, line_num, snippet))
        else:
            line_mismatches.append((m['cmd'], path, line_num, len(lines)))
    else:
        line_mismatches.append((m['cmd'], path, 'No line number', len(lines)))

print(f"Mock files verified: {len(valid_mocks)}")
print(f"Missing target files: {len(missing_files)}")
if missing_files:
    for mf in missing_files[:5]:
        print("  Missing:", mf)
print(f"Line mismatches / no line number: {len(line_mismatches)}")
if line_mismatches:
    for lm in line_mismatches:
        print("  Line issue:", lm)
