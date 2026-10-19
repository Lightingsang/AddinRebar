import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r"C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json", "r", encoding="utf-8") as f:
    thep = json.load(f)

print("=== MatTienThep XDATA / REGAPP ENTRIES ===")
for m in thep.get("muc", []):
    txt = json.dumps(m, ensure_ascii=False)
    if "regapp" in txt.lower() or "xdata" in txt.lower():
        print(f"\n--- {m.get('ten')} ---")
        print("Mo ta:", m.get("mo_ta"))
        print("Tham so:", m.get("tham_so"))
        print("Tra ve:", m.get("tra_ve"))

with open(r"C:\Dce Pro\AiAgent\DanhBa\danhba_cad_api.json", "r", encoding="utf-8") as f:
    cad_api = json.load(f)

print("\n=== CAD API REGAPP / XDATA ENTRIES ===")
for m in cad_api.get("muc", []):
    txt = json.dumps(m, ensure_ascii=False)
    if "regapp" in txt.lower() or "xdata" in txt.lower():
        print(f"\n--- {m.get('ten')} ---")
        print("Mo ta:", m.get("mo_ta"))
        print("Tham so:", m.get("tham_so"))
