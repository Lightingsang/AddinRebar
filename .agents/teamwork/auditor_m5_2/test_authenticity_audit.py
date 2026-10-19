import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'
test_dir = os.path.join(repo_root, 'HPDcePro', 'tests')

print("=== SCANNING ALL TEST FILES IN HPDcePro/tests ===")
test_files = []
for root, dirs, files in os.walk(test_dir):
    if 'bin' in dirs: dirs.remove('bin')
    if 'obj' in dirs: dirs.remove('obj')
    for f in files:
        if f.endswith('.cs'):
            test_files.append(os.path.join(root, f))

print(f"Total test files found: {len(test_files)}")

total_facts = 0
total_theories = 0
total_asserts = 0
tautology_findings = []
empty_tests = []
test_inventory = []

tautology_patterns = [
    re.compile(r'Assert\.True\s*\(\s*true\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.False\s*\(\s*false\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.Equal\s*\(\s*(\w+)\s*,\s*\1\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.NotNull\s*\(\s*"[^"]*"\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.True\s*\(\s*1\s*==\s*1\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.Equal\s*\(\s*0\s*,\s*0\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.Equal\s*\(\s*1\s*,\s*1\s*\)', re.IGNORECASE),
]

for tf in test_files:
    rel = os.path.relpath(tf, repo_root)
    with open(tf, 'r', encoding='utf-8', errors='ignore') as infile:
        content = infile.read()
        lines = content.splitlines()

    file_facts = len(re.findall(r'\[Fact\]', content))
    file_theories = len(re.findall(r'\[Theory\]', content))
    file_asserts = len(re.findall(r'Assert\.\w+', content))
    
    total_facts += file_facts
    total_theories += file_theories
    total_asserts += file_asserts
    
    test_inventory.append((rel, file_facts + file_theories, file_asserts))

    # check for tautologies line by line
    for lno, line in enumerate(lines):
        for tp in tautology_patterns:
            if tp.search(line):
                tautology_findings.append((rel, lno + 1, line.strip()))

    # check for empty test bodies
    # look for [Fact] followed by void Method() { }
    method_matches = re.finditer(r'\[(?:Fact|Theory)[^\]]*\]\s*(?:\[[^\]]*\]\s*)*public\s+(?:async\s+Task|void)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*\{([^}]*)\}', content, re.DOTALL)
    for mm in method_matches:
        mname = mm.group(1)
        mbody = mm.group(2).strip()
        if not mbody or mbody == '// pass' or len(mbody) < 5:
            empty_tests.append((rel, mname))

print(f"\nTotal Test Cases Identified ([Fact] + [Theory]): {total_facts + total_theories} (Facts: {total_facts}, Theories: {total_theories})")
print(f"Total Assert Statements: {total_asserts}")
print(f"Average Assertions per Test: {total_asserts / max(1, total_facts + total_theories):.2f}")

print(f"\nTautological Assertions Detected: {len(tautology_findings)}")
for rel, lno, line in tautology_findings:
    print(f"  VIOLATION: {rel}:{lno} -> {line}")

print(f"Empty Test Methods Detected: {len(empty_tests)}")
for rel, mname in empty_tests:
    print(f"  VIOLATION: {rel} :: {mname}() is empty!")

print("\n=== TOP 20 TEST FILES BY ASSERTION DENSITY ===")
test_inventory.sort(key=lambda x: x[1], reverse=True)
for rel, tests, asserts in test_inventory[:20]:
    print(f"  {rel}: {tests} tests, {asserts} asserts")
