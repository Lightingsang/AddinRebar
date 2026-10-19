import os
import re

files = [
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/IsolatedFixtureDirectory.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fixtures/FixtureDefinitionTests.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/F1_F7_CoreSafetyRegressionTests.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/F4_F10_F13_CanvasToolRegressionTests.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/F8_F9_F11_SeedAndDataTreeRegressionTests.cs"
]

print("=== RULE C2 CHECK (<= 300 lines) ===")
c2_pass = True
for f in files:
    with open(f, "r", encoding="utf-8") as fp:
        lines = fp.readlines()
    passed = len(lines) <= 300
    if not passed: c2_pass = False
    print(f"{f}: {len(lines)} lines -> {'PASS' if passed else 'FAIL'}")
print("Rule C2 Overall:", "PASS" if c2_pass else "FAIL")

print("\n=== RULE M3 CHECK (Methods <= 50 lines) ===")
# Method parsing
method_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w\<\>\[\]\?]+\s+(\w+)\s*\([^\)]*\)\s*(?:where[^{]+)?\{', re.MULTILINE)

m3_pass = True
for f in files:
    with open(f, "r", encoding="utf-8") as fp:
        content = fp.read()
    lines = content.splitlines()
    # Simple brace matching to measure method lengths
    i = 0
    max_len = 0
    max_method = ""
    while i < len(lines):
        line = lines[i]
        match = re.match(r'^\s*(?:public|private|protected|internal|static|\s)+[\w\<\>\[\]\?]+\s+(\w+)\s*\([^\)]*\)', line)
        if match and not ";" in line:
            method_name = match.group(1)
            # find opening brace
            brace_count = 0
            start_line = i
            found_open = False
            curr = i
            while curr < len(lines):
                brace_count += lines[curr].count('{')
                brace_count -= lines[curr].count('}')
                if '{' in lines[curr]:
                    found_open = True
                if found_open and brace_count == 0:
                    method_len = curr - start_line + 1
                    if method_len > max_len:
                        max_len = method_len
                        max_method = method_name
                    if method_len > 50:
                        print(f"FAIL M3: {f} -> {method_name} has {method_len} lines > 50")
                        m3_pass = False
                    break
                curr += 1
            i = curr
        i += 1
    print(f"{f}: Max method '{max_method}' = {max_len} lines (<= 50)")
print("Rule M3 Overall:", "PASS" if m3_pass else "FAIL")

print("\n=== RULE Q-B1 CHECK (No commented-out code) ===")
comment_code_pattern = re.compile(r'^\s*//\s*(var|if|for|while|return|public|private|protected|class|throw|new|using)\b', re.MULTILINE)
qb1_pass = True
for f in files:
    with open(f, "r", encoding="utf-8") as fp:
        content = fp.read()
    matches = comment_code_pattern.findall(content)
    passed = len(matches) == 0
    if not passed: qb1_pass = False
    print(f"{f}: {len(matches)} commented code lines -> {'PASS' if passed else 'FAIL'}")
print("Rule Q-B1 Overall:", "PASS" if qb1_pass else "FAIL")

print("\n=== RULE Q-B2 CHECK (No unannotated empty catches) ===")
catch_pattern = re.compile(r'catch\s*\([^\)]*\)\s*\{([^}]*)\}', re.MULTILINE | re.DOTALL)
qb2_pass = True
for f in files:
    with open(f, "r", encoding="utf-8") as fp:
        content = fp.read()
    matches = catch_pattern.findall(content)
    unannotated = 0
    for m in matches:
        body = m.strip()
        if not body or (not '//' in body and not '/*' in body):
            unannotated += 1
    passed = unannotated == 0
    if not passed: qb2_pass = False
    print(f"{f}: {unannotated} unannotated catches -> {'PASS' if passed else 'FAIL'}")
print("Rule Q-B2 Overall:", "PASS" if qb2_pass else "FAIL")
