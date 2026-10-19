import os
import glob
import re

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Cli"
cs_files = [f for f in glob.glob(os.path.join(base_dir, "**", "*.cs"), recursive=True) if "obj" not in f]

print("=== FILE LINE COUNTS (Constraint: <= 200 lines) ===")
max_file_len = 0
for f in sorted(cs_files):
    with open(f, "r", encoding="utf-8") as fp:
        lines = fp.readlines()
    count = len(lines)
    rel = os.path.relpath(f, base_dir)
    print(f"{count:4d} lines : {rel}")
    if count > max_file_len:
        max_file_len = count

print(f"\nMax file length: {max_file_len} (Pass: {max_file_len <= 200})")

print("\n=== REFS TO AUTODESK.REVIT.* (Constraint: 0) ===")
revit_refs = []
for f in cs_files:
    with open(f, "r", encoding="utf-8") as fp:
        for idx, line in enumerate(fp, 1):
            if "Autodesk.Revit" in line:
                revit_refs.append((f, idx, line.strip()))
print(f"Total Autodesk.Revit references: {len(revit_refs)}")
for r in revit_refs:
    print(r)

print("\n=== METHOD LENGTH AUDIT (Constraint: <= 40 lines) ===")
# Simple bracket matching method length estimator
for f in sorted(cs_files):
    rel = os.path.relpath(f, base_dir)
    with open(f, "r", encoding="utf-8") as fp:
        content = fp.read()
    lines = content.splitlines()
    
    # Check method signatures and count lines between opening and closing braces
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # Look for method signatures
        if re.search(r'(public|private|protected|internal|static|async)\s+[\w\<\>\[\]\,\?\s]+\s+\w+\s*\(', line) and not line.endswith(';'):
            start_line = i + 1
            # find opening brace
            brace_count = 0
            found_first = False
            j = i
            while j < len(lines):
                for ch in lines[j]:
                    if ch == '{':
                        brace_count += 1
                        found_first = True
                    elif ch == '}':
                        brace_count -= 1
                if found_first and brace_count == 0:
                    method_len = j - i + 1
                    method_name = line
                    if method_len > 40:
                        print(f"WARN: Method > 40 lines ({method_len} lines) at {rel}:{start_line} -> {method_name}")
                    else:
                        # print(f"OK: {method_len:2d} lines at {rel}:{start_line} -> {method_name[:40]}")
                        pass
                    i = j
                    break
                j += 1
        i += 1
print("Method length audit completed.")
