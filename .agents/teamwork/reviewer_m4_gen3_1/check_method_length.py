import os
import re

revit_src = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Revit"

methods_over_50 = []

for root, dirs, files in os.walk(revit_src):
    if "obj" in root or "bin" in root:
        continue
    for f in files:
        if not f.endswith(".cs"):
            continue
        path = os.path.join(root, f)
        rel_path = os.path.relpath(path, revit_src)
        with open(path, "r", encoding="utf-8") as file:
            lines = file.readlines()
        
        # Simple method bracket counter
        in_method = False
        method_name = ""
        method_start = 0
        brace_count = 0
        
        for idx, line in enumerate(lines):
            stripped = line.strip()
            # Heuristic for method declaration: public/private/protected/internal [static] ... (...)
            if not in_method:
                if re.search(r"^\s*(public|private|protected|internal)\s+(static\s+|virtual\s+|override\s+|sealed\s+|async\s+)*[\w\<\>\[\]\?\,\s]+\s+\w+\s*\(", line):
                    if not stripped.endswith(";"): # not interface/abstract
                        in_method = True
                        method_name = stripped
                        method_start = idx + 1
                        brace_count = line.count("{") - line.count("}")
            else:
                brace_count += line.count("{") - line.count("}")
                if brace_count == 0:
                    length = (idx + 1) - method_start + 1
                    if length > 50:
                        methods_over_50.append((rel_path, method_start, method_name, length))
                    in_method = False

print(f"Methods > 50 lines (Rule M3): {len(methods_over_50)}")
for f, ln, name, l in methods_over_50:
    print(f"  - {f}:{ln} '{name}' -> {l} lines")
