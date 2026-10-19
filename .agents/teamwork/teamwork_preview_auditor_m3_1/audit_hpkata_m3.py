import os
import re
import sys
import subprocess

hpkata_revit_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit"
hpkata_revit_tests_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit.Tests"

print("=" * 60)
print("FORENSIC INTEGRITY AUDIT — MILESTONE M3")
print("=" * 60)

# 1. Rule C2: Line Count <= 300
print("\n[CHECK 1] Rule C2 Line Count Audit (Limit <= 300 lines)")
c2_violations = []
total_files = 0

for target_dir in [hpkata_revit_dir, hpkata_revit_tests_dir]:
    for root, dirs, files in os.walk(target_dir):
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                total_files += 1
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")
                with open(full_path, "r", encoding="utf-8") as fp:
                    lines = fp.readlines()
                    cnt = len(lines)
                    status = "PASS" if cnt <= 300 else "FAIL (>300)"
                    if cnt > 300:
                        c2_violations.append((rel_path, cnt))
                    print(f"  {cnt:4d} lines | {status:10s} | {rel_path}")

print(f"\nRule C2 Result: {len(c2_violations)} violations out of {total_files} files.")

# 2. Rule M3: Method Line Span <= 50
print("\n[CHECK 2] Rule M3 Method Length Audit (Limit <= 50 lines)")
m3_violations = []

for target_dir in [hpkata_revit_dir, hpkata_revit_tests_dir]:
    for root, dirs, files in os.walk(target_dir):
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")
                with open(full_path, "r", encoding="utf-8") as fp:
                    lines = fp.readlines()

                brace_stack = []
                for idx, line in enumerate(lines, 1):
                    for char in line:
                        if char == '{':
                            brace_stack.append(idx)
                        elif char == '}':
                            if brace_stack:
                                open_line = brace_stack.pop()
                                # Depth 1: inside class body, directly enclosing a member
                                if len(brace_stack) == 1:
                                    span = idx - open_line + 1
                                    if span > 50:
                                        m3_violations.append((rel_path, open_line, idx, span))
                                        print(f"  FAIL: {rel_path}:{open_line}-{idx} ({span} lines > 50)")

if not m3_violations:
    print("  PASS: All class members/methods are <= 50 lines.")
else:
    print(f"  FAIL: {len(m3_violations)} method length violations found.")

# 3. Rule R5: 304.8 Quarantine
print("\n[CHECK 3] Rule R5 Quarantine Audit (304.8 literal only in RevitUnitConverter.cs)")
r5_violations = []
allowed_file = os.path.normpath(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Revit\Units\RevitUnitConverter.cs")

for root, dirs, files in os.walk(hpkata_revit_dir):
    if "bin" in root or "obj" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            full_path = os.path.normpath(os.path.join(root, f))
            rel_path = os.path.relpath(full_path, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")
            with open(full_path, "r", encoding="utf-8") as fp:
                for line_idx, line in enumerate(fp, 1):
                    if "304.8" in line:
                        if full_path != allowed_file:
                            r5_violations.append((rel_path, line_idx, line.strip()))
                            print(f"  VIOLATION: {rel_path}:{line_idx}: {line.strip()}")
                        else:
                            print(f"  ALLOWED: {rel_path}:{line_idx}: {line.strip()}")

if not r5_violations:
    print("  PASS: 304.8 appears strictly inside RevitUnitConverter.cs and nowhere else in HPKata.Revit.")
else:
    print(f"  FAIL: {len(r5_violations)} R5 violations found.")

# 4. Clean Code Rules Q-B1 (Dead/Commented Code) & Q-B2 (Unannotated Catches)
print("\n[CHECK 4] Clean Code Rules Q-B1 & Q-B2 Audit")
catch_pattern = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}", re.MULTILINE)
code_like_patterns = [
    re.compile(r"^\s*//\s*(?:var|if|for|foreach|while|return|throw|new|public|private|protected|internal|class|interface)\s+"),
    re.compile(r"^\s*//\s*[A-Za-z0-9_]+\s*=\s*"),
    re.compile(r"^\s*//\s*[A-Za-z0-9_.]+\s*\([^)]*\)\s*;"),
]

qb1_violations = []
qb2_violations = []

for target_dir in [hpkata_revit_dir, hpkata_revit_tests_dir]:
    for root, dirs, files in os.walk(target_dir):
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata")
                with open(full_path, "r", encoding="utf-8") as fp:
                    content = fp.read()
                    lines = content.splitlines()

                # Q-B2: Empty catch
                for match in catch_pattern.finditer(content):
                    body = match.group(1).strip()
                    if not body or not any(c.isalnum() for c in body):
                        qb2_violations.append((rel_path, match.group(0)))
                        print(f"  FAIL Q-B2 (empty catch): {rel_path}: {match.group(0)}")

                # Q-B1: Commented-out code
                for line_idx, line in enumerate(lines, 1):
                    stripped = line.strip()
                    if "///" in stripped or "e.g." in stripped.lower():
                        continue
                    for pat in code_like_patterns:
                        if pat.search(line):
                            qb1_violations.append((rel_path, line_idx, stripped))
                            print(f"  FAIL Q-B1 (commented code): {rel_path}:{line_idx}: {stripped}")

if not qb1_violations:
    print("  PASS Q-B1: 0 commented-out or dead code patterns found.")
if not qb2_violations:
    print("  PASS Q-B2: 0 unannotated empty catch blocks found.")

print("\n" + "=" * 60)
print(f"AUDIT SUMMARY: C2={len(c2_violations)}, M3={len(m3_violations)}, R5={len(r5_violations)}, QB1={len(qb1_violations)}, QB2={len(qb2_violations)}")
print("=" * 60)
