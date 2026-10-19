import os
import re

target_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.E2E.Tests"
cs_files = []
for r, d, files in os.walk(target_dir):
    if "bin" in r or "obj" in r:
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(r, f))

commented_code_violations = []

code_keywords = (
    "var ", "return ", "if ", "for ", "foreach ", "while ", "await ",
    "using ", "throw ", "public ", "private ", "internal ", "protected ",
    "class ", "interface ", "record ", "struct ", "new "
)

allowed_prefixes = (
    "TODO", "NOTE", "Multi-version:", "Multi-version", "Ref:", "See", "e.g.",
    "Rule", "PCC", "ADR", "Phase", "Step", "Tier", "Case", "Example", "Doc"
)

for fp in cs_files:
    rel = os.path.relpath(fp, target_dir)
    with open(fp, "r", encoding="utf-8") as f:
        lines = f.readlines()
    for idx, line in enumerate(lines, 1):
        s = line.strip()
        if s.startswith("//"):
            c = s[2:].strip()
            # If it starts with any code statement
            if any(c.startswith(kw) for kw in code_keywords) or (c.endswith(";") and not any(c.startswith(ap) for ap in allowed_prefixes)):
                # Exclude purely explanatory sentences
                if not any(word in c.lower() for word in ["example", "sample", "note", "todo", "multi-version", "reason", "ref:", "url", "rule", "tested", "simulat"]):
                    commented_code_violations.append((rel, idx, s))

print(f"Total C# files scanned: {len(cs_files)}")
print(f"Commented-out code violations: {len(commented_code_violations)}")
if commented_code_violations:
    for v in commented_code_violations:
        print("  VIOLATION:", v)
else:
    print("PASS: Exactly zero commented-out code violations found.")
