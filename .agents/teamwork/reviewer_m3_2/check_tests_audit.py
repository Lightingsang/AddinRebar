import os
import re

TEST_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests"

print(f"Scanning test directory: {TEST_DIR}")

cs_files = []
for root, dirs, files in os.walk(TEST_DIR):
    if "bin" in dirs:
        dirs.remove("bin")
    if "obj" in dirs:
        dirs.remove("obj")
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

print(f"Found {len(cs_files)} test files.\n")

total_assertions = 0
total_tests = 0
findings = []

for file_path in sorted(cs_files):
    rel_path = os.path.relpath(file_path, TEST_DIR)
    with open(file_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    line_count = len(lines)
    if line_count > 300:
        findings.append(f"CRITICAL: Rule C2 violation in {rel_path}: {line_count} lines (> 300)")

    # Check autodesk references
    for idx, line in enumerate(lines, 1):
        if "Autodesk" in line:
            findings.append(f"CRITICAL: Host reference found in {rel_path}:{idx}: {line.strip()}")

    # Check for empty catch
    for idx, line in enumerate(lines, 1):
        if re.search(r"catch\s*\([^\)]*\)\s*\{\s*\}", line):
            findings.append(f"CRITICAL: Rule Q-B2 violation (empty catch) in {rel_path}:{idx}")

    # Check for commented-out code (Q-B1)
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//") and not stripped.startswith("///"):
            comment = stripped[2:].strip()
            # If it looks like code e.g. contains semicolon, var, Assert, new
            if re.search(r"(var\s+\w+\s*=|Assert\.\w+\(|;\s*$|new\s+\w+\()", comment):
                # Check if it's an explanation comment vs commented out code
                # e.g., // 2 legs of 500, base of 1000 is an explanation
                # // Assert.Equal(x, y); is commented-out code
                if re.match(r"(Assert\.\w+|var\s+\w+\s*=|return\s+|if\s*\(|new\s+\w+.*;)", comment):
                    findings.append(f"WARNING: Potential dead code in {rel_path}:{idx}: {stripped}")

    # Check methods length (Rule M3)
    method_regex = re.compile(r"^\s*(public|private|protected|internal)?\s*(static\s+)?(void|async\s+Task|[A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*$")
    
    current_method = None
    method_start = 0
    brace_depth = 0
    in_method = False

    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if re.match(r"\[(Fact|Theory)\]", stripped):
            total_tests += 1

        # Check vacuous assertions
        if "Assert.True(true" in stripped:
            findings.append(f"CRITICAL: Vacuous assertion Assert.True(true) in {rel_path}:{idx}")
        if "Assert.False(false" in stripped:
            findings.append(f"CRITICAL: Vacuous assertion Assert.False(false) in {rel_path}:{idx}")
        if re.search(r"Assert\.Equal\(\s*(\w+)\s*,\s*\1\s*\)", stripped):
            findings.append(f"CRITICAL: Tautological assertion Assert.Equal(x, x) in {rel_path}:{idx}: {stripped}")

        if "Assert." in stripped:
            total_assertions += 1

        # Simple method tracking
        match = method_regex.match(line)
        if match and not in_method:
            current_method = match.group(4)
            method_start = idx
            in_method = True
            brace_depth = 0

        if in_method:
            brace_depth += line.count("{") - line.count("}")
            if brace_depth == 0 and "}" in line:
                method_len = idx - method_start + 1
                if method_len > 50:
                    findings.append(f"WARNING: Rule M3 method length in {rel_path}:{method_start} ({current_method}): {method_len} lines (> 50)")
                in_method = False
                current_method = None

    print(f"{rel_path:45} | Lines: {line_count:3} | Tests in file: {len(re.findall(r'\[(Fact|Theory)\]', ''.join(lines))):2}")

print(f"\nTotal test methods declared: {total_tests}")
print(f"Total assertions: {total_assertions}")
print(f"Total findings: {len(findings)}")
for f in findings:
    print(f"  {f}")
