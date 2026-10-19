import os
import re

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
hpkata_dir = os.path.join(root_dir, "HPKata")

# Find all cs files
all_cs_files = []
for root, dirs, files in os.walk(hpkata_dir):
    if any(p in root.split(os.sep) for p in ["bin", "obj", ".vs"]):
        continue
    for f in files:
        if f.endswith(".cs") or f.endswith(".xaml"):
            all_cs_files.append(os.path.join(root, f))

phase4_keywords = [
    "foundation", "footing", "pile", "slab", "stair", "lintel", "sectioncut", 
    "genericsection", "tier3", "phase4"
]

phase4_files = [
    p for p in all_cs_files 
    if any(k in os.path.relpath(p, hpkata_dir).lower() for k in phase4_keywords)
]

print(f"Total Phase 4 files identified: {len(phase4_files)}")

# 1. Rule C2
c2_violations = []
for p in phase4_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
    if len(lines) > 300:
        c2_violations.append((rel, len(lines)))

print(f"\nRule C2 Violations (files > 300 lines): {len(c2_violations)}")
for rel, count in c2_violations:
    print(f"  FAIL: {rel} ({count} lines)")

# 2. Host Neutrality in HPKata.Core
core_autodesk = []
core_files = [p for p in all_cs_files if "HPKata.Core" in p]
for p in core_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        for idx, line in enumerate(f, 1):
            if "Autodesk" in line:
                core_autodesk.append((rel, idx, line.strip()))

print(f"\nHost Neutrality in HPKata.Core (Autodesk references): {len(core_autodesk)}")
for rel, idx, line in core_autodesk:
    print(f"  FAIL: {rel}:{idx}: {line}")

# 3. Rule R5 in HPKata.Revit
r5_violations = []
revit_files = [p for p in all_cs_files if "HPKata.Revit" in p]
for p in revit_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        for idx, line in enumerate(f, 1):
            if "304.8" in line:
                if "RevitUnitConverter.cs" not in rel:
                    r5_violations.append((rel, idx, line.strip()))

print(f"\nRule R5 Violations (304.8 outside RevitUnitConverter.cs): {len(r5_violations)}")
for rel, idx, line in r5_violations:
    print(f"  FAIL: {rel}:{idx}: {line}")

# 4. Rule Q-B1 (Placeholders, commented code)
qb1_violations = []
placeholder_re = re.compile(r"\b(TODO|FIXME|HACK|NotImplementedException)\b")
for p in phase4_files:
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        for idx, line in enumerate(f, 1):
            if line.strip().startswith("///"):
                continue
            m = placeholder_re.search(line)
            if m:
                qb1_violations.append((rel, idx, m.group(1), line.strip()))

print(f"\nRule Q-B1 Violations (TODO/FIXME/HACK/NotImplementedException): {len(qb1_violations)}")
for rel, idx, kind, line in qb1_violations:
    print(f"  FAIL: {rel}:{idx} [{kind}]: {line}")

# 5. Rule Q-B2 (Empty catch blocks)
qb2_violations = []
for p in phase4_files:
    if not p.endswith(".cs"):
        continue
    rel = os.path.relpath(p, hpkata_dir)
    with open(p, "r", encoding="utf-8") as f:
        content = f.read()
    for m in re.finditer(r"catch(?:\s*\([^\)]*\))?\s*\{([^}]*)\}", content):
        body = m.group(1).strip()
        line_num = content[:m.start()].count("\n") + 1
        if not body:
            qb2_violations.append((rel, line_num, "Completely empty catch"))
        elif not any(k in body for k in ["//", "/*", "throw", "return", "Log", "log"]):
            qb2_violations.append((rel, line_num, f"Catch without comment or logging: {body}"))

print(f"\nRule Q-B2 Violations (Unannotated empty catches): {len(qb2_violations)}")
for rel, idx, desc in qb2_violations:
    print(f"  FAIL: {rel}:{idx}: {desc}")

print("\nAudit complete.")
