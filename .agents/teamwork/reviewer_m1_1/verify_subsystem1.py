import sys
import os
import re
import json

sys.stdout.reconfigure(encoding='utf-8')

# Read danhba_lenh.json
danhba_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json'
with open(danhba_path, 'r', encoding='utf-8') as f:
    danhba_data = json.load(f)

danhba_items = danhba_data.get('muc', [])
danhba_cmds = [item['ten'] for item in danhba_items]
danhba_dict = {item['ten']: item for item in danhba_items}

print(f"Total commands in danhba_lenh.json: {len(danhba_cmds)}")

# Read TRUTH_MATRIX.md
matrix_path = r'docs/rea-dce-pro/TRUTH_MATRIX.md'
with open(matrix_path, 'r', encoding='utf-8') as f:
    matrix_content = f.read()

# Extract Subsystem 1: AutoCAD <CommandMethod> Endpoints
sec1_match = re.search(r'## 2\. Subsystem 1: AutoCAD <CommandMethod> Endpoints.*?\n(.*?)\n## 3\. Subsystem 2:', matrix_content, re.DOTALL)
if not sec1_match:
    print("ERROR: Could not find Subsystem 1 section!")
    sys.exit(1)

sec1_text = sec1_match.group(1)
rows = [line.strip() for line in sec1_text.split('\n') if line.strip().startswith('|') and not line.strip().startswith('|---') and not 'Legacy Identifier' in line]

matrix_cmds = []
matrix_details = {}
for row in rows:
    parts = [p.strip() for p in row.split('|')[1:-1]]
    if len(parts) >= 6:
        cmd = parts[0].replace('`', '').strip()
        matrix_cmds.append(cmd)
        matrix_details[cmd] = {
            'subsystem': parts[1],
            'token_rva': parts[2],
            'inspection': parts[3],
            'target_file': parts[4],
            'status': parts[5]
        }

print(f"Total commands in TRUTH_MATRIX.md Subsystem 1: {len(matrix_cmds)}")

# Set comparison
danhba_set = set(danhba_cmds)
matrix_set = set(matrix_cmds)

missing_in_matrix = [c for c in danhba_cmds if c not in matrix_set]
extra_in_matrix = [c for c in matrix_cmds if c not in danhba_set]

print(f"Missing in matrix: {len(missing_in_matrix)}")
if missing_in_matrix:
    print("  Missing items:", missing_in_matrix)

print(f"Extra in matrix: {len(extra_in_matrix)}")
if extra_in_matrix:
    print("  Extra items:", extra_in_matrix)

# Status distribution
status_counts = {}
for c, d in matrix_details.items():
    st = d['status']
    status_counts[st] = status_counts.get(st, 0) + 1
print("Status counts in Subsystem 1:", status_counts)

# Verify order and exact match
exact_match = (danhba_cmds == matrix_cmds)
print(f"Exact 1:1 list equality (including order): {exact_match}")
if not exact_match and not missing_in_matrix and not extra_in_matrix:
    print("Sets are identical, but order differs.")
