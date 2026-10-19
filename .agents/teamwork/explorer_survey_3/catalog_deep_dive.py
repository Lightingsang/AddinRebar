import os
import sys
import json
from collections import defaultdict

sys.stdout.reconfigure(encoding='utf-8')

danhba_dir = r"C:\Dce Pro\AiAgent\DanhBa"

# 1. AutoCAD CommandMethod (danhba_lenh.json)
lenh_path = os.path.join(danhba_dir, "danhba_lenh.json")
with open(lenh_path, "r", encoding="utf-8") as f:
    lenh_data = json.load(f)

lenh_items = lenh_data.get("muc", [])
print(f"=== 1. AutoCAD <CommandMethod> ({len(lenh_items)} entries) ===")
by_domain = defaultdict(list)
by_source_group = defaultdict(list)
flags_count = {"can_form": 0, "can_chon": 0, "ghi_ban_ve": 0, "co_the_tu_dong": 0}

for item in lenh_items:
    name = item.get("ten", "")
    domain = item.get("nhom_nghiep_vu", "chua_phan_loai")
    source_grp = item.get("nhom", "khac")
    by_domain[domain].append(name)
    by_source_group[source_grp].append(name)
    for flag in flags_count:
        if item.get(flag):
            flags_count[flag] += 1

print(f"Flags summary: {flags_count}")
print(f"\nGrouped by Business Domain ('nhom_nghiep_vu') [{len(by_domain)} domains]:")
for dom, names in sorted(by_domain.items(), key=lambda x: len(x[1]), reverse=True):
    print(f"  - {dom} ({len(names)} commands): {', '.join(names[:10])}{' ...' if len(names) > 10 else ''}")

print(f"\nGrouped by Module Group ('nhom') [{len(by_source_group)} modules]:")
for grp, names in sorted(by_source_group.items(), key=lambda x: len(x[1]), reverse=True):
    print(f"  - {grp} ({len(names)} commands)")

# 2. Excel Ribbon Commands (danhba_dce_excel.json)
excel_path = os.path.join(danhba_dir, "danhba_dce_excel.json")
with open(excel_path, "r", encoding="utf-8") as f:
    excel_data = json.load(f)

excel_items = excel_data.get("muc", [])
print(f"\n=== 2. Excel Ribbon Commands ({len(excel_items)} entries) ===")
excel_by_grp = defaultdict(list)
excel_flags = {"can_form": 0, "can_chon": 0, "co_the_tu_dong": 0}

for item in excel_items:
    name = item.get("ten", "")
    grp = item.get("nhom", "khac")
    excel_by_grp[grp].append((name, item.get("mo_ta", "")))
    for flag in excel_flags:
        if item.get(flag):
            excel_flags[flag] += 1

print(f"Flags summary: {excel_flags}")
for grp, cmds in sorted(excel_by_grp.items(), key=lambda x: len(x[1]), reverse=True):
    print(f"  - {grp} ({len(cmds)} commands):")
    for name, desc in cmds:
        print(f"      * {name}: {desc[:80]}")

# 3. DceSteel Functions (danhba_dce_thep.json)
thep_path = os.path.join(danhba_dir, "danhba_dce_thep.json")
with open(thep_path, "r", encoding="utf-8") as f:
    thep_data = json.load(f)

thep_items = thep_data.get("muc", [])
print(f"\n=== 3. DceSteel Functions ({len(thep_items)} entries) ===")
thep_by_grp = defaultdict(list)
for item in thep_items:
    name = item.get("ten", "")
    grp = item.get("nhom", "khac")
    thep_by_grp[grp].append((name, item.get("mo_ta", ""), [p.get("ten", "") for p in item.get("tham_so", [])]))

for grp, funcs in sorted(thep_by_grp.items(), key=lambda x: len(x[1]), reverse=True):
    print(f"  - {grp} ({len(funcs)} functions):")
    for name, desc, params in funcs:
        print(f"      * {name}({', '.join(params)}): {desc[:70]}")

# 4. MBKC Functions (danhba_mbkc.json)
mbkc_path = os.path.join(danhba_dir, "danhba_mbkc.json")
with open(mbkc_path, "r", encoding="utf-8") as f:
    mbkc_data = json.load(f)

mbkc_items = mbkc_data.get("muc", [])
print(f"\n=== 4. MBKC Functions ({len(mbkc_items)} entries) ===")
mbkc_by_grp = defaultdict(list)
for item in mbkc_items:
    name = item.get("ten", "")
    grp = item.get("nhom", "khac")
    mbkc_by_grp[grp].append((name, item.get("mo_ta", ""), [p.get("ten", "") for p in item.get("tham_so", [])]))

for grp, funcs in sorted(mbkc_by_grp.items(), key=lambda x: len(x[1]), reverse=True):
    print(f"  - {grp} ({len(funcs)} functions):")
    for name, desc, params in funcs:
        print(f"      * {name}({', '.join(params)}): {desc[:70]}")
