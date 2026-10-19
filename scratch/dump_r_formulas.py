import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

for row in data:
    r = row.get("Row")
    shape = row.get("B_text", "").strip()
    if not shape or r < 4:
        continue
    r_form = row.get("R_formula", "")
    r_text = row.get("R_text", "").strip()
    w_text = row.get("W_text", "").strip()
    
    # Collect columns D..M labels
    dims = []
    for c in "DEFGHIJKLM":
        val = row.get(f"{c}_text", "").strip()
        if val:
            dims.append(f"{c}:{val}")
            
    print(f"Row {r:02d} | Shape: {shape:12s} | Dims: {', '.join(dims):35s} | Map(W): {w_text:15s} | R_Formula: {r_form}")
