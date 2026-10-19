import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

danhba_dir = r"C:\Dce Pro\AiAgent\DanhBa"

# 1. Inspect danhba_dce_thep.json
with open(os.path.join(danhba_dir, "danhba_dce_thep.json"), "r", encoding="utf-8") as f:
    thep = json.load(f)

print("==================================================================")
print("DANH BA DCE THEP (MatTienThep)")
print("==================================================================")
for m in thep.get("muc", []):
    ten = m.get("ten")
    mo_ta = m.get("mo_ta")
    nhom = m.get("nhom")
    params = m.get("tham_so", [])
    p_str = ", ".join([f"{p.get('ten')}:{p.get('kieu')}" for p in params])
    print(f"[{nhom}] {ten}({p_str})")
    print(f"   -> {mo_ta}")

# 2. Inspect danhba_lenh.json
with open(os.path.join(danhba_dir, "danhba_lenh.json"), "r", encoding="utf-8") as f:
    lenh = json.load(f)

print("\n==================================================================")
print("DANH BA LENH (AutoCAD Commands & Aliases)")
print("==================================================================")
items = lenh.get("muc", []) if isinstance(lenh, dict) else lenh
for m in items:
    ten = m.get("lenh") or m.get("ten")
    viet_tat = m.get("viet_tat") or m.get("alias") or m.get("lenh_tat")
    mo_ta = m.get("mo_ta") or m.get("chuc_nang")
    nhom = m.get("nhom") or m.get("phan_loai")
    print(f"[{nhom}] {ten} (alias: {viet_tat}) -> {mo_ta}")
