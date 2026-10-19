import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('scratch/iso_qhplus_dang_thep.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

for row in data:
    r = row.get("Row")
    if r < 44:
        continue
    b_text = row.get("B_text", "").strip()
    r_form = row.get("R_formula", "")
    r_text = row.get("R_text", "").strip()
    w_text = row.get("W_text", "").strip()
    print(f"Row {r:02d} | B: {b_text!r} | R_text: {r_text!r} | R_form: {r_form!r} | W: {w_text!r}")
