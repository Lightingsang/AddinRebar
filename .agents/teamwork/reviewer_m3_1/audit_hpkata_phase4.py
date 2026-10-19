import os
import re

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
hpkata_dir = os.path.join(root_dir, "HPKata")

# Find all files under HPKata
all_cs_files = []
for root, dirs, files in os.walk(hpkata_dir):
    if any(p in root.split(os.sep) for p in ["bin", "obj", ".vs"]):
        continue
    for f in files:
        if f.endswith(".cs"):
            all_cs_files.append(os.path.join(root, f))

print(f"Total .cs files in HPKata: {len(all_cs_files)}")

# Filter Phase 4 files
phase4_keywords = [
    "Foundation", "Footing", "Pile", "Slab", "Stair", "Lintel", "SectionCut", 
    "GenericSection", "Tier3", "Phase4"
]

phase4_files = []
for p in all_cs_files:
    rel = os.path.relpath(p, hpkata_dir)
    # Check if path contains Phase 4 components
    if any(k.lower() in rel.lower() for k in phase4_keywords):
        phase4_files.append(p)

print(f"Identified Phase 4 files: {len(phase4_files)}")

# 1. Rule C2: File line count <= 300
print("\n=== RULE C2: FILE LINE COUNT (<= 300 LINES) ===")
c2_violations = []
for p in phase4_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = len(lines)
    if count > 300:
        c2_violations.append((rel, count))
        print(f"[FAIL] {rel}: {count} lines (> 300)")

if not c2_violations:
    print(f"PASS: All {len(phase4_files)} Phase 4 files are <= 300 lines.")

# Also check ALL cs files in HPKata for C2
all_c2_violations = []
for p in all_cs_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        count = len(f.readlines())
    if count > 300:
        all_c2_violations.append((rel, count))

if not all_c2_violations:
    print(f"PASS: All {len(all_cs_files)} files in the entire HPKata solution are <= 300 lines!")
else:
    print(f"WARNING: In entire solution, {len(all_c2_violations)} files exceed 300 lines:")
    for rel, c in all_c2_violations:
        print(f"  - {rel}: {c} lines")

# 2. Host Isolation: Check HPKata.Core for Autodesk references
print("\n=== HOST ISOLATION: HPKata.Core ===")
core_dir = os.path.join(hpkata_dir, "HPKata.Core")
autodesk_refs = []
for root, dirs, files in os.walk(core_dir):
    if any(p in root.split(os.sep) for p in ["bin", "obj"]):
        continue
    for f in files:
        if f.endswith(".cs") or f.endswith(".csproj"):
            p = os.path.join(root, f)
            with open(p, "r", encoding="utf-8") as fh:
                for line_idx, line in enumerate(fh, 1):
                    if "Autodesk" in line:
                        autodesk_refs.append((os.path.relpath(p, hpkata_dir), line_idx, line.strip()))

if not autodesk_refs:
    print("PASS: Exactly 0 Autodesk references found in HPKata.Core!")
else:
    print(f"FAIL: Found {len(autodesk_refs)} Autodesk references in HPKata.Core:")
    for rel, l, t in autodesk_refs:
        print(f"  {rel}:{l}: {t}")

# 3. Rule R5: Constant 304.8 occurrences
print("\n=== RULE R5: CONSTANT 304.8 CHECK ===")
revit_dir = os.path.join(hpkata_dir, "HPKata.Revit")
r5_occurrences = []
for root, dirs, files in os.walk(revit_dir):
    if any(p in root.split(os.sep) for p in ["bin", "obj"]):
        continue
    for f in files:
        if f.endswith(".cs"):
            p = os.path.join(root, f)
            with open(p, "r", encoding="utf-8") as fh:
                for line_idx, line in enumerate(fh, 1):
                    if "304.8" in line:
                        rel = os.path.relpath(p, hpkata_dir)
                        r5_occurrences.append((rel, line_idx, line.strip()))

print(f"Occurrences of 304.8 in HPKata.Revit: {len(r5_occurrences)}")
for rel, l, t in r5_occurrences:
    is_valid = "RevitUnitConverter.cs" in rel
    status = "OK" if is_valid else "FAIL"
    print(f"  [{status}] {rel}:{l}: {t}")

# 4. Rule Q-B1: Dead code, TODOs, FIXMEs, HACKs, NotImplementedException
print("\n=== RULE Q-B1: ZERO DEAD CODE & PLACEHOLDERS ===")
qb1_issues = []
placeholder_pattern = re.compile(r"\b(TODO|FIXME|HACK|NotImplementedException)\b")
commented_code_pattern = re.compile(
    r'^\s*//\s*(?:var\s+\w+\s*=|return\s|if\s*\(|while\s*\(|for\s*\(|throw\s+new|Assert\.|using\s*\(\w+|\w+\.\w+\(.*\);)\s*$'
)

for p in phase4_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        for idx, line in enumerate(f, 1):
            sline = line.strip()
            if sline.startswith("///"):
                continue
            m = placeholder_pattern.search(line)
            if m:
                qb1_issues.append((rel, idx, f"Placeholder {m.group(1)}", sline))
            elif commented_code_pattern.match(line):
                qb1_issues.append((rel, idx, "Commented-out code", sline))

if not qb1_issues:
    print(f"PASS: Zero Q-B1 violations across all {len(phase4_files)} Phase 4 files.")
else:
    print(f"FAIL: Found {len(qb1_issues)} Q-B1 issues:")
    for rel, l, kind, text in qb1_issues:
        print(f"  {rel}:{l} [{kind}]: {text}")

# 5. Rule Q-B2: Empty catch blocks
print("\n=== RULE Q-B2: ZERO UNANNOTATED EMPTY CATCH BLOCKS ===")
qb2_issues = []
for p in phase4_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        content = f.read()
    for m in re.finditer(r"catch(?:\s*\([^\)]*\))?\s*\{([^}]*)\}", content):
        body = m.group(1).strip()
        line_num = content[:m.start()].count("\n") + 1
        if not body:
            qb2_issues.append((rel, line_num, "Completely empty catch"))
        elif not any(k in body for k in ["//", "/*", "throw", "return", "Log", "log"]):
            qb2_issues.append((rel, line_num, f"Catch without comment or logging: {body}"))

if not qb2_issues:
    print(f"PASS: Zero Q-B2 violations across all {len(phase4_files)} Phase 4 files.")
else:
    print(f"FAIL: Found {len(qb2_issues)} Q-B2 issues:")
    for rel, l, desc in qb2_issues:
        print(f"  {rel}:{l}: {desc}")

print("\nAudit completed.")
