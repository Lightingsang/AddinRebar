import re
import json
import math
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Read Blueprint Section 6
with open('docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md', 'r', encoding='utf-8') as f:
    bp_content = f.read()

lines = bp_content.splitlines()

# Dynamically locate Section 6.3 table rows
in_table = False
bp_lines = []
for i, line in enumerate(lines):
    if line.startswith('### 6.3'):
        in_table = True
        continue
    if in_table and (line.startswith('### 6.4') or line.startswith('## 7.') or line.startswith('---')):
        break
    if in_table and line.strip().startswith('| **`dang'):
        bp_lines.append((i + 1, line))

print(f"Total rows in Section 6.3 table: {len(bp_lines)}")

parsed_table = []
corrupted_rows = []

for line_num, line in bp_lines:
    parts = [p.strip() for p in line.split('|')]
    # If standard markdown table row, parts[0] is empty, parts[-1] is empty, so len(parts) should be 9 for 7 columns
    # cols: 0:empty, 1:Shape Code, 2:Name, 3:Topology, 4:Params, 5:Formula, 6:Hooks, 7:Revit, 8:empty
    if len(parts) != 9:
        corrupted_rows.append((line_num, len(parts), line))
    else:
        parsed_table.append({
            'line_num': line_num,
            'code': parts[1].replace('*', '').replace('`', '').replace('dang', '').strip(),
            'name': parts[2],
            'topology': parts[3],
            'params': parts[4],
            'formula': parts[5],
            'hooks': parts[6],
            'revit': parts[7]
        })

print(f"Properly parsed rows: {len(parsed_table)}")
print(f"Corrupted / split rows: {len(corrupted_rows)}")
for r in corrupted_rows:
    print(f"  Line {r[0]}: parts={r[1]}")
    print(f"    Content: {r[2]}")
