import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r"C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json", "r", encoding="utf-8") as f:
    thep = json.load(f)

for m in thep.get("muc", []):
    txt = json.dumps(m, ensure_ascii=False)
    if "regapp" in txt.lower() or "xdata" in txt.lower():
        print(f"=== {m.get('ten')} ===")
        print("Mo ta:", m.get("mo_ta"))
        print("Tham so:", m.get("tham_so"))
        print("Tra ve:", m.get("tra_ve"))
        print()
