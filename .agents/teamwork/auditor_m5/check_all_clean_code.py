import os
import re
import sys

root_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper"

cs_files = []
for r, d, files in os.walk(root_dir):
    if "\\bin\\" in r or "\\obj\\" in r:
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(r, f))

print(f"Total C# files found in HPGrasshopper: {len(cs_files)}")

# 1. Rule C2: File line count <= 300
c2_violations = []
for f in cs_files:
    with open(f, "r", encoding="utf-8", errors="replace") as fp:
        lines = fp.readlines()
    if len(lines) > 300:
        c2_violations.append((f, len(lines)))

print(f"\n--- RULE C2 (<= 300 lines per file) ---")
if c2_violations:
    print(f"FAILED C2: {len(c2_violations)} files violate Rule C2:")
    for f, count in c2_violations:
        rel = os.path.relpath(f, root_dir)
        print(f"  {rel}: {count} lines")
else:
    print("PASS: 100% of C# files are <= 300 lines.")

# 2. Rule M3: Method length <= 50 lines
m3_violations = []
for f in cs_files:
    with open(f, "r", encoding="utf-8", errors="replace") as fp:
        lines = fp.readlines()
    
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # Look for method signatures
        match = re.match(r'^(?:(?:public|private|protected|internal|static|async|virtual|override|sealed)\s+)+[\w\<\>\[\]\?\s,]+\s+(\w+)\s*\([^\)]*\)', line)
        if match and not line.endswith(";") and not line.startswith("//"):
            method_name = match.group(1)
            # Find body
            start_line = i
            brace_count = 0
            found_open = False
            curr = i
            while curr < len(lines):
                # Count braces outside of strings/comments roughly
                curr_line = lines[curr]
                # Strip string literals and comments
                clean_line = re.sub(r'"(?:\\.|[^"\\])*"', '', curr_line)
                clean_line = re.sub(r'//.*', '', clean_line)
                brace_count += clean_line.count('{')
                brace_count -= clean_line.count('}')
                if '{' in clean_line:
                    found_open = True
                if found_open and brace_count == 0:
                    method_len = curr - start_line + 1
                    if method_len > 50:
                        m3_violations.append((f, method_name, start_line + 1, method_len))
                    break
                curr += 1
            i = curr
        i += 1

print(f"\n--- RULE M3 (<= 50 lines per method) ---")
if m3_violations:
    print(f"FAILED M3: {len(m3_violations)} methods violate Rule M3 (> 50 lines):")
    for f, name, line_num, length in m3_violations:
        rel = os.path.relpath(f, root_dir)
        print(f"  {rel}:{line_num} {name}() -> {length} lines")
else:
    print("PASS: 100% of parsed methods are <= 50 lines.")

# 3. Rule Q-B1: Commented-out code
qb1_violations = []
comment_code_pattern = re.compile(r'^\s*//\s*(?:var|if\s*\(|for\s*\(|while\s*\(|return\s+|public\s+|private\s+|protected\s+|class\s+|throw\s+new|new\s+\w+\(|using\s*\()\b')
for f in cs_files:
    with open(f, "r", encoding="utf-8", errors="replace") as fp:
        lines = fp.readlines()
    for idx, l in enumerate(lines):
        if comment_code_pattern.match(l):
            qb1_violations.append((f, idx + 1, l.strip()))

print(f"\n--- RULE Q-B1 (0 dead / commented-out code blocks) ---")
if qb1_violations:
    print(f"FAILED Q-B1: {len(qb1_violations)} commented code lines found:")
    for f, line_num, text in qb1_violations:
        rel = os.path.relpath(f, root_dir)
        print(f"  {rel}:{line_num} -> {text}")
else:
    print("PASS: 0 commented-out code lines found.")

# 4. Rule Q-B2: Unannotated empty catch blocks
qb2_violations = []
catch_pattern = re.compile(r'catch(?:\s*\([^\)]*\))?\s*\{([^}]*)\}', re.MULTILINE | re.DOTALL)
for f in cs_files:
    with open(f, "r", encoding="utf-8", errors="replace") as fp:
        content = fp.read()
    for m in catch_pattern.finditer(content):
        body = m.group(1).strip()
        # Empty or no comment explaining why
        if not body or ('//' not in body and '/*' not in body and 'throw' not in body and 'Log' not in body):
            qb2_violations.append((f, m.start(), m.group(0)))

print(f"\n--- RULE Q-B2 (0 unannotated catch blocks) ---")
if qb2_violations:
    print(f"FAILED Q-B2: {len(qb2_violations)} unannotated catch blocks found:")
    for f, pos, text in qb2_violations:
        rel = os.path.relpath(f, root_dir)
        print(f"  {rel} -> {text[:60]}...")
else:
    print("PASS: 0 unannotated catch blocks found.")
