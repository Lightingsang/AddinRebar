import os, re

root_dir = r"HPGrasshopper"
test_files = []
for root, dirs, files in os.walk(root_dir):
    if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
        continue
    for f in files:
        if f.endswith("Tests.cs") or "Tests" in root:
            if f.endswith(".cs"):
                test_files.append(os.path.join(root, f))

print(f"Total test files: {len(test_files)}")

trivial_patterns = [
    re.compile(r"Assert\.(True|False)\((true|false)\)", re.IGNORECASE),
    re.compile(r"Assert\.Equal\(\s*(\d+|true|false|\"[^\"]*\")\s*,\s*\1\s*\)", re.IGNORECASE),
    re.compile(r"Assert\.NotNull\(\s*new\s+", re.IGNORECASE),
    re.compile(r"//\s*TODO", re.IGNORECASE)
]

violations = []
for p in test_files:
    with open(p, "r", encoding="utf-8", errors="ignore") as fp:
        for idx, line in enumerate(fp, 1):
            for pat in trivial_patterns:
                if pat.search(line):
                    violations.append((p, idx, line.strip()))

print(f"Trivial assertion or TODO findings in tests: {len(violations)}")
for p, idx, l in violations:
    print(f"  {p}:{idx}: {l}")
