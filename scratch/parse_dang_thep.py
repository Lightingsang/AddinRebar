import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

print(f"Total rows in export: {len(data)}")

for row in data:
    r_idx = row.get("Row")
    shape = row.get("B_text", "").strip()
    if not shape:
        continue
    # Let's inspect Col T (Col 20)
    t_text = row.get("T_text", "")
    t_form = row.get("T_formula", "")
    
    # Let's collect all non-empty columns from D to O (dimensions)
    dims = []
    for c in "DEFGHIJKLMNOP":
        val = row.get(f"{c}_text", "")
        if val.strip():
            dims.append(f"{c}:{val.strip()}")
            
    print(f"R{r_idx:02d} | Shape: {shape:15s} | DimCols: {', '.join(dims):25s} | T_form: {t_form}")
