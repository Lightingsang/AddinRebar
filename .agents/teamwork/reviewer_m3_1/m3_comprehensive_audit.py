import os
import re
import sys

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper"

# 1. Check file lengths (Rule C2 <= 300)
c2_violations = []
cs_files = []

for root, dirs, files in os.walk(base_dir):
    if any(p in root.split(os.sep) for p in ["bin", "obj", ".git"]):
        continue
    for f in files:
        if f.endswith(".cs"):
            full_path = os.path.join(root, f)
            cs_files.append(full_path)
            with open(full_path, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
                line_count = len(lines)
                if line_count > 300:
                    c2_violations.append((full_path, line_count))

print(f"Total .cs files scanned in HPGrasshopper: {len(cs_files)}")
print(f"Rule C2 (> 300 lines) violations: {len(c2_violations)}")
for path, count in c2_violations:
    rel = os.path.relpath(path, base_dir)
    print(f"  [C2] {rel}: {count} lines")

# 2. Check Q-B1 (commented out code) & Q-B2 (empty catch blocks)
qb1_violations = []
qb2_violations = []

code_patterns = [
    r"^\s*//\s*(?:var|int|string|bool|double|float|return|if|for|foreach|while|new|throw|try|catch|using|public|private|protected|internal|static|async|await)\s+",
    r"^\s*//\s*[A-Za-z0-9_]+\s*=\s*",
    r"^\s*//\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\s*\(",
]

for full_path in cs_files:
    rel = os.path.relpath(full_path, base_dir)
    with open(full_path, "r", encoding="utf-8") as fp:
        lines = fp.readlines()

    for idx, line in enumerate(lines):
        line_num = idx + 1
        # Q-B1 check
        for pat in code_patterns:
            if re.search(pat, line):
                # allow explanatory comments mentioning code
                stripped = line.strip()[2:].strip()
                if not stripped.startswith("e.g.") and not stripped.startswith("i.e.") and not stripped.startswith("TODO") and not stripped.startswith("Example"):
                    # Check if it looks like real commented out code
                    if ";" in stripped or "{" in stripped or "}" in stripped or re.search(r"\b(?:return|var|throw)\b", stripped):
                        qb1_violations.append((rel, line_num, line.strip()))

        # Q-B2 check: catch (...) { }
        # single line empty catch: catch (...) {}
        if re.search(r"catch\s*(?:\([^)]*\))?\s*\{\s*\}", line):
            qb2_violations.append((rel, line_num, line.strip()))

    # multi-line empty catch check
    text = "".join(lines)
    for m in re.finditer(r"catch\s*(?:\([^)]*\))?\s*\{([^{}]*)\}", text):
        body = m.group(1).strip()
        if not body:
            # find line number
            start_pos = m.start()
            line_no = text[:start_pos].count("\n") + 1
            qb2_violations.append((rel, line_no, "empty catch block"))
        elif body and not any(k in body for k in ["//", "/*", "Log.", "log.", "throw", "return"]):
            # catch with whitespace or empty statement
            clean_body = re.sub(r"\s+", "", body)
            if clean_body in ["", ";"]:
                start_pos = m.start()
                line_no = text[:start_pos].count("\n") + 1
                qb2_violations.append((rel, line_no, f"empty catch without reason/logging: {body}"))

print(f"\nRule Q-B1 (commented-out code) potential violations: {len(qb1_violations)}")
for rel, lno, ltext in qb1_violations[:10]:
    print(f"  [Q-B1] {rel}:{lno}: {ltext}")

print(f"\nRule Q-B2 (unannotated empty catch) violations: {len(qb2_violations)}")
for rel, lno, ltext in qb2_violations:
    print(f"  [Q-B2] {rel}:{lno}: {ltext}")

