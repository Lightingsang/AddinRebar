import os
import re
import sys

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

# Phase 3 files inventory
# Phase 3: Column & Shear Wall Shop Drawing
# Includes HPKata.Core (Columns, Walls), HPKata.UI (Column, Wall ViewModels/Views),
# HPKata.AutoCad (ColumnCommands, WallCommands), HPKata.Revit (Column, Wall commands/services),
# and Phase 3 tests.

phase3_files = []

for root, dirs, files in os.walk(BASE_DIR):
    # Skip bin and obj directories
    if "bin" in dirs:
        dirs.remove("bin")
    if "obj" in dirs:
        dirs.remove("obj")
    for f in files:
        if not f.endswith(".cs"):
            continue
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, BASE_DIR)
        
        # Check if file belongs to Phase 3
        # Match Columns or Walls or specific Phase 3 commands/views
        rel_lower = rel_path.lower()
        is_phase3 = False
        if "column" in rel_lower or "wall" in rel_lower:
            is_phase3 = True
        
        if is_phase3:
            phase3_files.append((rel_path, full_path))

print(f"Total Phase 3 C# files found: {len(phase3_files)}")

# 1. Rule C2: Measure every .cs file touched by Phase 3 <= 300 lines
c2_violations = []
for rel, full in phase3_files:
    with open(full, "r", encoding="utf-8-sig") as fp:
        lines = fp.readlines()
    count = len(lines)
    if count > 300:
        c2_violations.append((rel, count))

print(f"\n--- Rule C2 (File <= 300 lines) ---")
if c2_violations:
    print(f"FAILED: {len(c2_violations)} files exceed 300 lines:")
    for rel, count in c2_violations:
        print(f"  {rel}: {count} lines")
else:
    print("PASSED: All Phase 3 files <= 300 lines.")

# 2. Rule M3: Measure methods in Phase 3 code <= 50 lines
# Let's parse C# methods using bracket counting
m3_violations = []

def analyze_methods(rel, full):
    with open(full, "r", encoding="utf-8-sig") as fp:
        code_lines = fp.readlines()

    # Simple state machine to detect method declarations and brace counts
    in_method = False
    method_name = ""
    start_line = 0
    brace_depth = 0
    class_brace_depth = 0
    
    # regex for method signatures
    method_sig_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w<>\[\],\?]+\s+([A-Za-z0-9_]+)\s*\([^;]*$')
    # property accessor or lambda or field check
    # Avoid getters/setters/properties

    for idx, raw_line in enumerate(code_lines, 1):
        line = raw_line.strip()
        # count braces
        # Strip string literals and char literals to avoid false braces
        cleaned_line = re.sub(r'"(\\.|[^"\\])*"', '""', raw_line)
        cleaned_line = re.sub(r"'(\\.|[^'\\])*'", "''", cleaned_line)
        # Strip single line comments
        cleaned_line = re.sub(r'//.*$', '', cleaned_line)
        
        open_braces = cleaned_line.count('{')
        close_braces = cleaned_line.count('}')

        if not in_method:
            # Check if this line looks like a method signature
            # Usually inside a class (brace_depth >= 1)
            # Not a class/struct/namespace/interface/enum
            if any(kw in cleaned_line for kw in ["class ", "struct ", "interface ", "enum ", "namespace "]):
                pass
            elif ("(" in cleaned_line and ")" in cleaned_line and not cleaned_line.endswith(";")) or ("(" in cleaned_line and not ")" in cleaned_line and not cleaned_line.endswith(";")):
                # potential method header
                m = re.search(r'(?:public|private|protected|internal|static|async|override|virtual|sealed)\s+.*?\s+([A-Za-z0-9_]+)\s*\([^;]*$', cleaned_line)
                if m and not any(kw in cleaned_line for kw in ["class ", "record ", "if ", "while ", "for ", "catch ", "switch "]):
                    method_name = m.group(1)
                    start_line = idx
                    # If this line also has '{', in_method starts
                    if open_braces > 0:
                        in_method = True
                        brace_depth = open_braces - close_braces
                        continue

            if open_braces > 0 and start_line == idx - 1:
                in_method = True
                brace_depth = open_braces - close_braces
        else:
            brace_depth += (open_braces - close_braces)
            if brace_depth <= 0:
                end_line = idx
                total_lines = end_line - start_line + 1
                if total_lines > 50:
                    m3_violations.append((rel, method_name, start_line, end_line, total_lines))
                in_method = False
                method_name = ""
                start_line = 0
                brace_depth = 0

for rel, full in phase3_files:
    analyze_methods(rel, full)

print(f"\n--- Rule M3 (Methods <= 50 lines) ---")
if m3_violations:
    print(f"FAILED/TRIGGERED: {len(m3_violations)} methods > 50 lines:")
    for rel, mname, s, e, cnt in m3_violations:
        print(f"  {rel} -> {mname}() [lines {s}-{e}]: {cnt} lines")
else:
    print("PASSED: All Phase 3 methods <= 50 lines.")

# 3. Rule Q-B1: Zero commented-out code, zero TODOs/FIXMEs/HACKs
qb1_violations = []
for rel, full in phase3_files:
    with open(full, "r", encoding="utf-8-sig") as fp:
        lines = fp.readlines()
    for idx, raw_line in enumerate(lines, 1):
        line = raw_line.strip()
        # check TODO, FIXME, HACK
        if any(tag in line for tag in ["TODO", "FIXME", "HACK", "UNDONE"]):
            qb1_violations.append((rel, idx, line, "TODO/FIXME/HACK"))
        # check commented out code: // followed by C# syntax like var, int, double, if, return, for, public, etc.
        # e.g. // var x = ...; or // int i = ...; or // return ...;
        if re.match(r'^\s*//\s*(?:var|int|double|string|bool|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|return\s+|new\s+|throw\s+|public\s+|private\s+|protected\s+)[^;{}]*[;{}]', raw_line):
            qb1_violations.append((rel, idx, line, "Commented-out code"))

print(f"\n--- Rule Q-B1 (Zero commented-out code, TODO, FIXME, HACK) ---")
if qb1_violations:
    print(f"FAILED: {len(qb1_violations)} instances found:")
    for rel, idx, line, reason in qb1_violations:
        print(f"  {rel}:{idx} [{reason}] {line}")
else:
    print("PASSED: Zero commented-out code, zero TODOs/FIXMEs/HACKs.")

# 4. Rule Q-B2: Zero unannotated empty catch blocks
qb2_violations = []
for rel, full in phase3_files:
    with open(full, "r", encoding="utf-8-sig") as fp:
        content = fp.read()
    # Find catch blocks: catch (...) { }
    # Look for catch\s*(\([^)]*\))?\s*\{\s*\}
    empty_catch_matches = re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', content)
    for m in empty_catch_matches:
        body = m.group(1).strip()
        if not body: # Completely empty
            qb2_violations.append((rel, m.start(), "Completely empty catch"))
        elif not any(c in body for c in ["//", "/*", "throw", "return", "Log", "Console", "Debug"]):
            # Has statements but no comments explaining why if empty
            if len(body) == 0:
                qb2_violations.append((rel, m.start(), "Empty catch without comment"))

print(f"\n--- Rule Q-B2 (Zero unannotated empty catch blocks) ---")
if qb2_violations:
    print(f"FAILED: {len(qb2_violations)} empty catch blocks:")
    for rel, pos, reason in qb2_violations:
        print(f"  {rel} [{reason}]")
else:
    print("PASSED: Zero unannotated empty catch blocks.")

# 5. Rule R5: Constant 304.8 strictly in RevitUnitConverter.cs
r5_violations = []
for root, dirs, files in os.walk(BASE_DIR):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in files:
        if not f.endswith(".cs"): continue
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, BASE_DIR)
        with open(full_path, "r", encoding="utf-8-sig") as fp:
            content = fp.read()
        if "304.8" in content:
            if not rel_path.endswith("RevitUnitConverter.cs"):
                r5_violations.append(rel_path)

print(f"\n--- Rule R5 (Constant 304.8 strictly in RevitUnitConverter.cs) ---")
if r5_violations:
    print(f"FAILED: 304.8 found outside RevitUnitConverter.cs in:")
    for v in r5_violations:
        print(f"  {v}")
else:
    print("PASSED: 304.8 appears strictly and exclusively in RevitUnitConverter.cs.")

# 6. Host Neutrality: HPKata.Core has ZERO references to Autodesk.*
core_violations = []
core_dir = os.path.join(BASE_DIR, "HPKata.Core")
for root, dirs, files in os.walk(core_dir):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in files:
        if not f.endswith(".cs"): continue
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, BASE_DIR)
        with open(full_path, "r", encoding="utf-8-sig") as fp:
            for idx, line in enumerate(fp, 1):
                if "Autodesk." in line:
                    core_violations.append((rel_path, idx, line.strip()))

print(f"\n--- Host Neutrality (HPKata.Core has 0 references to Autodesk.*) ---")
if core_violations:
    print(f"FAILED: Autodesk references in HPKata.Core:")
    for r, idx, l in core_violations:
        print(f"  {r}:{idx} {l}")
else:
    print("PASSED: HPKata.Core is 100% host-neutral (0 Autodesk references).")
