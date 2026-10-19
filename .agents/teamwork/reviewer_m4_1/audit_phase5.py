import os
import re

root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

phase5_patterns = [
    r"QuantitySurveying",
    r"DrawingLayout",
    r"CadUtilities",
    r"QsTakeoff",
    r"DrawingUtility",
    r"QsCommands",
    r"LayoutCommands",
    r"DimensionUtility",
    r"TextAnnotation",
    r"LayerUtility",
    r"KataClean",
    r"RevitQs",
    r"RevitLayout",
    r"Phase5"
]

all_cs = []
for dirpath, dirnames, filenames in os.walk(root):
    # skip bin, obj
    if "\\bin\\" in dirpath or "\\obj\\" in dirpath:
        continue
    for f in filenames:
        if f.endswith(".cs"):
            full_path = os.path.join(dirpath, f)
            all_cs.append(full_path)

print(f"Total .cs files in HPKata: {len(all_cs)}")

# Filter Phase 5 files
phase5_files = []
for p in all_cs:
    for pat in phase5_patterns:
        if pat.lower() in p.lower():
            phase5_files.append(p)
            break

print(f"Total Phase 5 .cs files identified: {len(phase5_files)}")

# 1. Check Rule C2: <= 300 physical lines
c2_violations = []
for p in phase5_files:
    with open(p, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()
        if len(lines) > 300:
            c2_violations.append((p, len(lines)))

print(f"\n--- Rule C2 (File length <= 300) ---")
if not c2_violations:
    print("PASS: All Phase 5 files are <= 300 lines.")
else:
    print(f"FAIL: {len(c2_violations)} files violate C2:")
    for p, count in c2_violations:
        print(f"  {p}: {count} lines")

# Also check max line count among phase 5 files
max_file = max(phase5_files, key=lambda p: sum(1 for _ in open(p, "r", encoding="utf-8", errors="ignore")))
with open(max_file, "r", encoding="utf-8", errors="ignore") as f:
    max_lines = len(f.readlines())
print(f"Longest Phase 5 file: {os.path.basename(max_file)} ({max_lines} lines)")

# 2. Check Rule M3: <= 50 lines per method in Phase 5 files
# Simple method parser based on braces
print(f"\n--- Rule M3 (Method length <= 50) ---")
m3_violations = []
method_header_re = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w\<\>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$')

for p in phase5_files:
    with open(p, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()
    
    in_method = False
    brace_depth = 0
    method_name = ""
    start_line = 0
    
    for i, line in enumerate(lines):
        trimmed = line.strip()
        if not in_method:
            m = method_header_re.match(line)
            if m and not trimmed.endswith(";") and not "class " in trimmed and not "interface " in trimmed:
                method_name = m.group(1)
                start_line = i + 1
        
        # Track braces
        open_b = line.count('{')
        close_b = line.count('}')
        if method_name and open_b > 0 and not in_method:
            in_method = True
            brace_depth = open_b - close_b
            if brace_depth == 0:
                # single line method
                length = (i + 1) - start_line + 1
                if length > 50:
                    m3_violations.append((p, method_name, length, start_line))
                in_method = False
                method_name = ""
        elif in_method:
            brace_depth += open_b - close_b
            if brace_depth <= 0:
                end_line = i + 1
                length = end_line - start_line + 1
                if length > 50:
                    m3_violations.append((p, method_name, length, start_line))
                in_method = False
                method_name = ""
                brace_depth = 0

if not m3_violations:
    print("PASS: All methods in Phase 5 files are <= 50 lines.")
else:
    print(f"FAIL/WARNING: {len(m3_violations)} methods > 50 lines:")
    for p, m_name, length, s_line in m3_violations:
        print(f"  {os.path.basename(p)}:{s_line} {m_name}() = {length} lines")

# 3. Check Rule Q-B1: dead/commented-out code, TODO, FIXME, HACK, NotImplementedException
print(f"\n--- Rule Q-B1 (Dead code, TODO/FIXME/HACK/NotImplementedException) ---")
qb1_issues = []
commented_code_pattern = re.compile(r'^\s*//\s*(?:var |int |double |string |if\s*\(|for\s*\(|foreach\s*\(|return |throw |new |\w+\.\w+\()')
todo_pattern = re.compile(r'\b(?:TODO|FIXME|HACK|NotImplementedException)\b')

for p in phase5_files:
    with open(p, "r", encoding="utf-8", errors="ignore") as f:
        for i, line in enumerate(f):
            if todo_pattern.search(line):
                qb1_issues.append((p, i + 1, "TODO/FIXME/NotImplemented", line.strip()))
            if commented_code_pattern.search(line):
                # check if not just a comment explaining code
                qb1_issues.append((p, i + 1, "Commented-out code", line.strip()))

if not qb1_issues:
    print("PASS: Zero TODO/FIXME/NotImplementedException or commented-out code in Phase 5.")
else:
    print(f"FAIL: {len(qb1_issues)} Q-B1 issues:")
    for p, lnum, kind, text in qb1_issues:
        print(f"  {os.path.basename(p)}:{lnum} [{kind}] {text}")

# 4. Check Rule Q-B2: Empty catch blocks
print(f"\n--- Rule Q-B2 (Catch blocks without justification) ---")
qb2_issues = []
catch_re = re.compile(r'catch\s*(?:\([^\)]*\))?\s*\{([^}]*)\}')

for p in phase5_files:
    with open(p, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
    # match single or multi line catch blocks
    for m in re.finditer(r'catch\s*(?:\([^\)]*\))?\s*\{([\s\S]*?)\}', content):
        body = m.group(1).strip()
        if not body:
            qb2_issues.append((p, "Empty catch block"))
        elif "//" not in body and "/*" not in body and len(body) < 15:
            # maybe unannotated?
            qb2_issues.append((p, f"Catch block without comment: {body}"))

if not qb2_issues:
    print("PASS: Zero unannotated empty catch blocks.")
else:
    print(f"WARNING/FAIL: {len(qb2_issues)} catch issues:")
    for p, desc in qb2_issues:
        print(f"  {os.path.basename(p)}: {desc}")

# 5. Check Rule R5: 304.8 literal across entire HPKata solution
print(f"\n--- Rule R5 (304.8 isolation) ---")
r5_occurrences = []
for p in all_cs:
    with open(p, "r", encoding="utf-8", errors="ignore") as f:
        for i, line in enumerate(f):
            if "304.8" in line:
                r5_occurrences.append((p, i + 1, line.strip()))

print(f"Total 304.8 occurrences in entire solution: {len(r5_occurrences)}")
for p, lnum, line in r5_occurrences:
    rel = os.path.relpath(p, root)
    print(f"  {rel}:{lnum} -> {line}")
    if "RevitUnitConverter.cs" not in rel:
        print(f"  VIOLATION: 304.8 found outside RevitUnitConverter.cs!")

# 6. Check Core host isolation (Autodesk.*)
print(f"\n--- Host Isolation: HPKata.Core Autodesk references ---")
core_autodesk = []
for p in all_cs:
    if "\\HPKata.Core\\" in p:
        with open(p, "r", encoding="utf-8", errors="ignore") as f:
            for i, line in enumerate(f):
                if "Autodesk" in line:
                    core_autodesk.append((p, i + 1, line.strip()))

if not core_autodesk:
    print("PASS: 0 references to Autodesk in HPKata.Core")
else:
    print(f"Matches for 'Autodesk' in HPKata.Core: {len(core_autodesk)}")
    for p, lnum, line in core_autodesk:
        rel = os.path.relpath(p, root)
        print(f"  {rel}:{lnum} -> {line}")
