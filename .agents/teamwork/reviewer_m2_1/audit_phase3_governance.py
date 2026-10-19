import os
import re

hpkata_root = os.path.abspath(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")

print(f"Scanning HPKata root: {hpkata_root}")

# Collect all cs, xaml files
all_cs_files = []
all_xaml_files = []

for root, dirs, files in os.walk(hpkata_root):
    # skip bin, obj, .vs
    if any(part in root.split(os.sep) for part in ["bin", "obj", ".vs"]):
        continue
    for f in files:
        if f.endswith(".cs"):
            all_cs_files.append(os.path.join(root, f))
        elif f.endswith(".xaml"):
            all_xaml_files.append(os.path.join(root, f))

print(f"Total C# files found: {len(all_cs_files)}")
print(f"Total XAML files found: {len(all_xaml_files)}")

# 1. LAYER ISOLATION CHECK:
# HPKata.Core must have ZERO references to Autodesk.AutoCAD.* or Autodesk.Revit.*
core_violations = []
for cs_path in all_cs_files:
    if "HPKata.Core" in cs_path and not "HPKata.Core.Tests" in cs_path:
        with open(cs_path, "r", encoding="utf-8") as f:
            content = f.read()
            if "Autodesk.AutoCAD" in content:
                core_violations.append((cs_path, "Contains Autodesk.AutoCAD"))
            if "Autodesk.Revit" in content:
                core_violations.append((cs_path, "Contains Autodesk.Revit"))

print("\n=== 1. HPKata.Core Layer Isolation ===")
if core_violations:
    print(f"FAILED: Found {len(core_violations)} violations in HPKata.Core:")
    for path, reason in core_violations:
        print(f"  {path}: {reason}")
else:
    print("PASSED: HPKata.Core has ZERO references to Autodesk.AutoCAD.* or Autodesk.Revit.*")

# 2. RULE R5: Constant 304.8 must appear strictly and exclusively in RevitUnitConverter.cs
r5_violations = []
r5_hits = []
for cs_path in all_cs_files:
    with open(cs_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        for idx, line in enumerate(lines, 1):
            if "304.8" in line:
                # check if comment
                clean_line = line.strip()
                if not (clean_line.startswith("//") or clean_line.startswith("/*")):
                    if os.path.basename(cs_path) == "RevitUnitConverter.cs":
                        r5_hits.append((cs_path, idx, clean_line))
                    else:
                        r5_violations.append((cs_path, idx, clean_line))

print("\n=== 2. Rule R5 (304.8 Isolation) ===")
print(f"Legitimate occurrences in RevitUnitConverter.cs: {len(r5_hits)}")
for path, idx, line in r5_hits:
    print(f"  {os.path.basename(path)}:{idx} -> {line}")
if r5_violations:
    print(f"FAILED: Found {len(r5_violations)} forbidden occurrences of 304.8:")
    for path, idx, line in r5_violations:
        print(f"  {path}:{idx} -> {line}")
else:
    print("PASSED: Constant 304.8 appears strictly and exclusively in RevitUnitConverter.cs")

# 3. RULE C2: File line count <= 300
c2_violations = []
phase3_files = []

keywords = ["Column", "Wall", "Cot", "Vach", "Tier4_Scenario2", "Tier4_Scenario3"]

for cs_path in all_cs_files:
    rel = os.path.relpath(cs_path, hpkata_root)
    with open(cs_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        count = len(lines)
    is_p3 = any(kw.lower() in rel.lower() for kw in keywords)
    if is_p3:
        phase3_files.append((rel, count))
    if count > 300:
        c2_violations.append((rel, count, is_p3))

print(f"\n=== 3. Rule C2 (File Line Count <= 300) ===")
print(f"Identified {len(phase3_files)} Phase 3 C# files.")
if c2_violations:
    print(f"FAILED: Found {len(c2_violations)} files > 300 lines:")
    for rel, count, is_p3 in c2_violations:
        tag = "[PHASE 3]" if is_p3 else "[OTHER]"
        print(f"  {tag} {rel}: {count} lines")
else:
    print(f"PASSED: 100% of C# files in HPKata (all {len(all_cs_files)} files) are <= 300 lines!")

# 4. RULE Q-B1: Zero dead/commented-out code or unaddressed TODOs/FIXMEs/HACKs
qb1_violations = []
todo_pattern = re.compile(r"\b(TODO|FIXME|HACK|XXX)\b", re.IGNORECASE)
# Commented out code pattern: e.g. // var x = ..., // if (...), // return ...
code_comment_pattern = re.compile(r"^\s*//\s*(if\s*\(|for\s*\(|while\s*\(|var\s+\w+\s*=|return\s+|throw\s+new\s+|Console\.|Debug\.)")

for cs_path in all_cs_files:
    rel = os.path.relpath(cs_path, hpkata_root)
    with open(cs_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    for idx, line in enumerate(lines, 1):
        if todo_pattern.search(line):
            qb1_violations.append((rel, idx, "TODO/FIXME/HACK", line.strip()))
        if code_comment_pattern.search(line):
            qb1_violations.append((rel, idx, "Commented-out code", line.strip()))

print(f"\n=== 4. Rule Q-B1 (Dead Code / Unaddressed TODOs) ===")
if qb1_violations:
    print(f"Found {len(qb1_violations)} possible Q-B1 violations:")
    for rel, idx, kind, snippet in qb1_violations:
        print(f"  {rel}:{idx} [{kind}] -> {snippet}")
else:
    print("PASSED: Zero dead code, commented-out code, or unaddressed TODOs/FIXMEs/HACKs.")

# 5. RULE Q-B2: Zero unannotated empty catch blocks
qb2_violations = []
catch_empty_pattern = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}")

for cs_path in all_cs_files:
    rel = os.path.relpath(cs_path, hpkata_root)
    with open(cs_path, "r", encoding="utf-8") as f:
        content = f.read()
    if catch_empty_pattern.search(content):
        qb2_violations.append((rel, "Empty catch block"))

print(f"\n=== 5. Rule Q-B2 (Empty Catch Blocks) ===")
if qb2_violations:
    print(f"FAILED: Found {len(qb2_violations)} empty catch violations:")
    for rel, issue in qb2_violations:
        print(f"  {rel}: {issue}")
else:
    print("PASSED: Zero unannotated empty catch blocks.")
