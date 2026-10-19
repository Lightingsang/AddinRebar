import os
import re

all_cs_files = []
for r, d, fs in os.walk("HPGrasshopper"):
    if "bin" in r or "obj" in r:
        continue
    for f in fs:
        if f.endswith(".cs"):
            all_cs_files.append(os.path.join(r, f))

print(f"Total C# source files: {len(all_cs_files)}")

skipped_tests = []
tautology_asserts = []
empty_catches = []

for f in all_cs_files:

    with open(f, "r", encoding="utf-8") as fh:
        lines = fh.readlines()
    for idx, l in enumerate(lines, 1):
        stripped = l.strip()
        if "Skip =" in l or "Skip=" in l:
            # Check if it is a Skip attribute on a test Fact/Theory
            if "[Fact(" in l or "[Theory(" in l or "Skip" in l:
                skipped_tests.append((f, idx, stripped))
        if re.search(r"Assert\.True\s*\(\s*true\s*\)", l):
            tautology_asserts.append((f, idx, stripped))
        if re.search(r"Assert\.False\s*\(\s*false\s*\)", l):
            tautology_asserts.append((f, idx, stripped))
        if re.search(r"Assert\.Equal\s*\(\s*([^,]+)\s*,\s*\1\s*\)", l):
            tautology_asserts.append((f, idx, stripped))
        if re.search(r"catch\s*(\([^\)]*\))?\s*\{\s*\}", l):
            empty_catches.append((f, idx, stripped))

print(f"Skipped tests found: {len(skipped_tests)}")
for s in skipped_tests:
    print("  SKIP:", s)

print(f"Tautology asserts found: {len(tautology_asserts)}")
for t in tautology_asserts:
    print("  TAUTOLOGY:", t)

print(f"Empty catch blocks found: {len(empty_catches)}")
for c in empty_catches:
    print("  EMPTY CATCH:", c)

import json
seed_dir = os.path.join("HPGrasshopper", "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary")
seeds = []
for root, dirs, files in os.walk(seed_dir):
    if "tool.json" in files and "code.cs" in files:
        tool_json_path = os.path.join(root, "tool.json")
        code_cs_path = os.path.join(root, "code.cs")
        with open(tool_json_path, "r", encoding="utf-8") as f:
            meta = json.load(f)
        with open(code_cs_path, "r", encoding="utf-8") as f:
            code_lines = len(f.readlines())
        rel = os.path.relpath(root, seed_dir).replace("\\", "/")
        seeds.append((rel, meta.get("name"), code_lines))

print(f"\nFound {len(seeds)} embedded seeds:")
for s in sorted(seeds):
    print(f"  {s[0]:<35} name={s[1]:<35} lines={s[2]}")

print("\nChecking file lines across all C# files:")
raw_over_300 = []
split_over_300 = []
for cs_file in all_cs_files:
    with open(cs_file, "r", encoding="utf-8") as fh:
        raw_lines = fh.readlines()
        split_lines = "".join(raw_lines).splitlines()
        if len(raw_lines) > 300:
            raw_over_300.append((cs_file, len(raw_lines)))
        if len(split_lines) > 300:
            split_over_300.append((cs_file, len(split_lines)))


print(f"Files with raw line count > 300: {len(raw_over_300)}")
for item in raw_over_300:
    print("  RAW:", item)

print(f"Files with splitlines count > 300: {len(split_over_300)}")
for item in split_over_300:
    print("  SPLIT:", item)

print("\nChecking Rule Q-B1 dead code patterns in comments:")
qb1_candidates = []
code_keywords = [
    r"//\s*(var|int|string|bool|double|float|long|void)\s+\w+\s*=",
    r"//\s*return\s+[^;]+;",
    r"//\s*if\s*\([^)]+\)\s*\{?",
    r"//\s*for\s*\([^)]+\)\s*\{?",
    r"//\s*while\s*\([^)]+\)\s*\{?",
    r"//\s*throw\s+new\s+\w+",
    r"//\s*await\s+\w+"
]
for cs_file in all_cs_files:
    with open(cs_file, "r", encoding="utf-8") as fh:
        for idx, line in enumerate(fh, 1):
            for pat in code_keywords:
                if re.search(pat, line):
                    # verify it's not inside a string literal
                    if not re.search(r'["\'].*//.*["\']', line):
                        qb1_candidates.append((cs_file, idx, line.strip()))

print(f"Potential Rule Q-B1 commented-out code: {len(qb1_candidates)}")
for c in qb1_candidates:
    print("  QB1:", c)



