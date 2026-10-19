import os
import json
import re

from verify_m4_clean_code import FILES, analyze_csharp_methods, root_dir

with open(r'scratch\all_members_m4.json', 'r', encoding='utf-8') as f:
    roslyn_members = json.load(f)

print(f"{'File':<55} | {'Python Max':<15} | {'Roslyn Max':<15} | {'Discrepancy / Root Cause'}")
print("-" * 125)

for rel in FILES:
    full = os.path.join(root_dir, rel)
    with open(full, 'r', encoding='utf-8') as f:
        content = f.read()
    py_methods = analyze_csharp_methods(content)
    py_max = max([m[3] for m in py_methods]) if py_methods else 0
    py_top = [m for m in py_methods if m[3] == py_max][0] if py_methods else None
    
    file_roslyn = [m for m in roslyn_members if m['File'] == rel]
    ros_max = max([m['Lines'] for m in file_roslyn]) if file_roslyn else 0
    ros_top = [m for m in file_roslyn if m['Lines'] == ros_max][0] if file_roslyn else None
    
    short_rel = os.path.basename(rel)
    py_info = f"{py_max} ({py_top[0]})" if py_top else "0 (N/A)"
    ros_info = f"{ros_max} ({ros_top['Member']})" if ros_top else "0 (N/A)"
    
    diff = ""
    if py_max != ros_max:
        diff = f"MISMATCH: Py={py_max} vs Ros={ros_max} ({ros_top['Member'] if ros_top else 'N/A'})"
    else:
        diff = "MATCH"
    print(f"{short_rel:<55} | {py_info:<15} | {ros_info:<15} | {diff}")
