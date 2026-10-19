import os, re

root_dir = r"HPGrasshopper/HPGrasshopper.E2E.Tests"
cs_files = []
for root, dirs, files in os.walk(root_dir):
    if "obj" in root or "bin" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

cs_files.sort()

# Check Q-B1: commented out code
# Check Q-B2: empty catch blocks without explanatory comments

commented_code_patterns = [
    re.compile(r"^\s*//\s*(?:var|int|string|bool|double|if|for|while|return|throw|Assert\.|new\s+[A-Z]|Console\.|_|[a-zA-Z0-9_]+\s*\=)\b"),
    re.compile(r"^\s*//\s*[a-zA-Z0-9_]+\.[a-zA-Z0-9_]+\s*\("),
]

empty_catch_pattern = re.compile(r"catch\s*(\([^\)]*\))?\s*\{\s*\}")

qb1_violations = []
qb2_violations = []

for p in cs_files:
    rel = os.path.relpath(p, root_dir)
    with open(p, "r", encoding="utf-8") as f:
        content = f.read()
        lines = content.splitlines()

    for i, line in enumerate(lines, 1):
        for pat in commented_code_patterns:
            if pat.search(line):
                # Ignore doc comments or explanation lines like // Multi-version: or // Rule C2: or comments in test data strings
                if not any(k in line for k in ["// Multi-version", "// Rule", "// F0", "// TC_", "// RFC", "// ADR"]):
                    qb1_violations.append((rel, i, line))

    # Search for empty catch blocks
    for m in empty_catch_pattern.finditer(content):
        # Check if inside string literal or actual code
        start = m.start()
        line_num = content[:start].count('\n') + 1
        qb2_violations.append((rel, line_num, m.group(0)))

print(f"Scanned {len(cs_files)} C# files.")
print(f"Rule Q-B1 (Commented-out code) potential violations: {len(qb1_violations)}")
for r, ln, text in qb1_violations:
    print(f"  {r}:{ln}: {text.strip()}")

print(f"Rule Q-B2 (Empty catch without reason) violations: {len(qb2_violations)}")
for r, ln, text in qb2_violations:
    print(f"  {r}:{ln}: {text.strip()}")
