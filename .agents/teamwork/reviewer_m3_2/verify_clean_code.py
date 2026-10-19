import os
import re

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def check_all():
    c2_violations = []
    qb1_violations = []
    qb2_violations = []
    r5_violations = []
    host_neutrality_violations = []
    m3_violations = []

    for root, dirs, files in os.walk(HPKATA_DIR):
        if "bin" in root or "obj" in root or ".git" in root:
            continue
        for file in files:
            if not file.endswith(".cs"):
                continue
            path = os.path.join(root, file)
            rel_path = os.path.relpath(path, HPKATA_DIR)
            
            with open(path, "r", encoding="utf-8", errors="replace") as f:
                lines = f.readlines()
            
            # C2: <= 300 lines
            if len(lines) > 300:
                c2_violations.append((rel_path, len(lines)))
            
            # Host neutrality for HPKata.Core
            if rel_path.startswith("HPKata.Core\\") or rel_path.startswith("HPKata.Core/"):
                for idx, line in enumerate(lines, 1):
                    if re.search(r"^\s*using\s+Autodesk\b", line):
                        host_neutrality_violations.append((rel_path, idx, line.strip()))
            
            # R5: 304.8 check
            if "304.8" in "".join(lines):
                if not ("RevitUnitConverter" in rel_path):
                    for idx, line in enumerate(lines, 1):
                        if "304.8" in line:
                            r5_violations.append((rel_path, idx, line.strip()))

            # Q-B1: NotImplementedException, TODO
            for idx, line in enumerate(lines, 1):
                stripped = line.strip()
                if "throw new NotImplementedException" in stripped:
                    qb1_violations.append((rel_path, idx, stripped))
                if re.search(r"//\s*TODO\b", stripped, re.IGNORECASE):
                    qb1_violations.append((rel_path, idx, stripped))
            
            # Q-B2: empty catch block without comment
            content = "".join(lines)
            empty_catches = re.finditer(r"catch\s*\([^\)]*\)\s*\{\s*\}", content)
            for m in empty_catches:
                qb2_violations.append((rel_path, m.group()))

    print(f"C2 Violations (>300 lines): {len(c2_violations)}")
    for v in c2_violations:
        print(f"  {v[0]}: {v[1]} lines")

    print(f"Host Neutrality Violations (HPKata.Core with Autodesk): {len(host_neutrality_violations)}")
    for v in host_neutrality_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

    print(f"R5 Violations (304.8 outside RevitUnitConverter): {len(r5_violations)}")
    for v in r5_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

    print(f"Q-B1 Violations (TODO, NotImplementedException): {len(qb1_violations)}")
    for v in qb1_violations:
        print(f"  {v[0]}:{v[1]}: {v[2]}")

    print(f"Q-B2 Violations (Empty catch without comment): {len(qb2_violations)}")
    for v in qb2_violations:
        print(f"  {v[0]}: {v[1]}")

if __name__ == "__main__":
    check_all()
