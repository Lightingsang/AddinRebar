import os
import sys
import json
from collections import Counter

sys.stdout.reconfigure(encoding='utf-8')

danhba_dir = r"C:\Dce Pro\AiAgent\DanhBa"

def analyze_json_file(filename):
    filepath = os.path.join(danhba_dir, filename)
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return None
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)
    return data

# 1. danhba_lenh.json (311 AutoCAD <CommandMethod>)
lenh_data = analyze_json_file("danhba_lenh.json")
print("=== 1. DANHBA_LENH.JSON (AutoCAD <CommandMethod>) ===")
print("Metadata:", {k: v for k, v in lenh_data.items() if k != 'muc'})
items = lenh_data.get('muc', [])
print(f"Total <CommandMethod> entries: {len(items)}")

# Group by 'nhom' or 'lop' or 'nhom_nghiep_vu'
groups = Counter(item.get('nhom', 'Unknown') for item in items)
print("Grouping by 'nhom':")
for k, v in groups.most_common():
    print(f"  - {k}: {v}")

classes = Counter(item.get('lop', 'Unknown') for item in items)
print("Top classes ('lop'):")
for k, v in classes.most_common(10):
    print(f"  - {k}: {v}")

# 2. danhba_dce_excel.json (73 Excel Ribbon commands)
excel_data = analyze_json_file("danhba_dce_excel.json")
print("\n=== 2. DANHBA_DCE_EXCEL.JSON (Excel Ribbon Commands) ===")
print("Metadata:", {k: v for k, v in excel_data.items() if k != 'muc'})
excel_items = excel_data.get('muc', [])
print(f"Total Excel commands: {len(excel_items)}")
excel_groups = Counter(item.get('nhom', 'Unknown') for item in excel_items)
print("Grouping by 'nhom':")
for k, v in excel_groups.most_common():
    print(f"  - {k}: {v}")

# 3. danhba_dce_thep.json (49 DceSteel functions)
thep_data = analyze_json_file("danhba_dce_thep.json")
print("\n=== 3. DANHBA_DCE_THEP.JSON (DceSteel Functions) ===")
print("Metadata:", {k: v for k, v in thep_data.items() if k != 'muc'})
thep_items = thep_data.get('muc', [])
print(f"Total DceSteel items: {len(thep_items)}")
thep_groups = Counter(item.get('nhom', 'Unknown') for item in thep_items)
print("Grouping by 'nhom':")
for k, v in thep_groups.most_common():
    print(f"  - {k}: {v}")

# 4. danhba_mbkc.json (29 MBKC functions)
mbkc_data = analyze_json_file("danhba_mbkc.json")
print("\n=== 4. DANHBA_MBKC.JSON (MBKC Functions) ===")
print("Metadata:", {k: v for k, v in mbkc_data.items() if k != 'muc'})
mbkc_items = mbkc_data.get('muc', [])
print(f"Total MBKC items: {len(mbkc_items)}")
mbkc_groups = Counter(item.get('nhom', 'Unknown') for item in mbkc_items)
print("Grouping by 'nhom':")
for k, v in mbkc_groups.most_common():
    print(f"  - {k}: {v}")
