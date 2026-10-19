import glob
import os
import re

ui_dir = os.path.abspath("HPKata/HPKata.UI")
tests_dir = os.path.abspath("HPKata/HPKata.UI.Tests")

cs_files = []
for base in [ui_dir, tests_dir]:
    for root, dirs, files in os.walk(base):
        if "bin" in root or "obj" in root:
            continue
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))

print(f"Total C# files found: {len(cs_files)}")
print(f"{'Lines':<6} {'File'}")
print("-" * 80)

c2_violations = []
autodesk_violations = []
empty_catches = []
commented_code = []

for path in sorted(cs_files):
    rel_path = os.path.relpath(path, os.path.abspath("."))
    with open(path, "r", encoding="utf-8") as fp:
        lines = fp.readlines()
    count = len(lines)
    print(f"{count:<6} {rel_path}")
    if count > 300:
        c2_violations.append((rel_path, count))
    
    # Check Autodesk references
    for i, line in enumerate(lines, 1):
        if "Autodesk" in line:
            autodesk_violations.append((rel_path, i, line.strip()))
        if re.search(r'catch\s*\([^\)]*\)\s*\{\s*\}', line) or (re.search(r'catch\s*\{', line) and '}' in line):
            empty_catches.append((rel_path, i, line.strip()))

print("\n--- Summary ---")
print(f"Rule C2 (>300 lines) Violations: {len(c2_violations)}")
for f, c in c2_violations:
    print(f"  {f}: {c} lines")

print(f"Host Neutrality (Autodesk.*) Violations: {len(autodesk_violations)}")
for f, i, l in autodesk_violations:
    print(f"  {f}:{i}: {l}")

print(f"Empty catch block Violations: {len(empty_catches)}")
for f, i, l in empty_catches:
    print(f"  {f}:{i}: {l}")
