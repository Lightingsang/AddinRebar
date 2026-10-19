import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

# Inspect row 2 (headers) and row 4
for r in [2, 3, 4]:
    row = data[r - 1]
    print(f"--- ROW {r} ---")
    for k in sorted(row.keys()):
        print(f"  {k}: {row[k]}")
