import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

danhba_dir = r"C:\Dce Pro\AiAgent\DanhBa"
for fn in ["danhba_dce_thep.json", "danhba_lenh.json", "danhba_dce_excel.json", "danhba_mbkc.json"]:
    fp = os.path.join(danhba_dir, fn)
    print(f"\n=======================================================")
    print(f"CATALOG: {fn}")
    print(f"=======================================================")
    with open(fp, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    items = data.get("muc", []) if isinstance(data, dict) else data
    print(f"Total entries: {len(items)}")
    
    # print all items summary
    for i, m in enumerate(items):
        ten = m.get("ten") or m.get("lenh") or m.get("name")
        mo_ta = m.get("mo_ta") or m.get("description") or m.get("chuc_nang")
        params = m.get("tham_so", [])
        p_str = ", ".join([p.get("ten", "") + ":" + p.get("kieu", "") for p in params]) if isinstance(params, list) else str(params)
        print(f"[{i+1:03d}] {ten} ({p_str}) -> {mo_ta}")
