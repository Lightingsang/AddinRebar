import os, re

hpdynamo_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo"

# Let's inspect methods across production files
long_methods = []

for root, dirs, files in os.walk(hpdynamo_dir):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    if "Tests" in root: continue
    for f in files:
        if f.endswith(".cs"):
            p = os.path.join(root, f)
            with open(p, "r", encoding="utf-8", errors="ignore") as fl:
                lines = fl.read().splitlines()
            
            # Simple heuristic for method definitions:
            # find { and matching }
            # Track block lines
            i = 0
            while i < len(lines):
                line = lines[i].strip()
                # Check method-like signature (ends with { or followed by {)
                if any(k in line for k in ["public ", "private ", "protected ", "internal "]) and "(" in line and not line.endswith(";"):
                    start = i
                    brace_count = 0
                    started = False
                    for j in range(i, len(lines)):
                        for char in lines[j]:
                            if char == '{':
                                brace_count += 1
                                started = True
                            elif char == '}':
                                brace_count -= 1
                        if started and brace_count == 0:
                            length = j - start + 1
                            if length > 50:
                                long_methods.append((os.path.relpath(p, hpdynamo_dir), line, length))
                            i = j
                            break
                i += 1

print(f"Production methods > 50 lines: {len(long_methods)}")
for f, m, l in long_methods:
    print(f"  {f}: {m[:40]}... ({l} lines)")
