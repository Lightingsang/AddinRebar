import os
import re
import sys

def check_file(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        lines = content.splitlines()

    total_lines = len(lines)
    print(f"File: {file_path}")
    print(f"Total lines: {total_lines} (Rule C2 limit: <= 300)")
    c2_pass = total_lines <= 300

    # Check Rule M3: Method length <= 50 lines
    # Using regex to identify method declarations and brace balancing
    method_pattern = re.compile(
        r'^\s*(?:public|private|protected|internal|static|async|override|virtual|sealed|\s)+\s+(?:[A-Za-z0-9_<>?, ]+)\s+([A-Za-z0-9_]+)\s*\([^;]*?\)\s*(?:where[^{]+)?\s*\{',
        re.MULTILINE
    )

    m3_violations = []
    methods_found = []

    for match in method_pattern.finditer(content):
        method_name = match.group(1)
        start_idx = match.start()
        start_line = content[:start_idx].count('\n') + 1

        # Trace matching braces
        brace_count = 0
        end_line = start_line
        in_body = False
        for i, char in enumerate(content[start_idx:]):
            if char == '{':
                brace_count += 1
                in_body = True
            elif char == '}':
                brace_count -= 1
                if in_body and brace_count == 0:
                    end_idx = start_idx + i
                    end_line = content[:end_idx].count('\n') + 1
                    break
        
        method_len = end_line - start_line + 1
        methods_found.append((method_name, start_line, end_line, method_len))
        if method_len > 50:
            m3_violations.append((method_name, start_line, end_line, method_len))

    print(f"Methods analyzed: {len(methods_found)}")
    for name, s, e, l in methods_found:
        print(f"  - {name} (lines {s}-{e}): {l} lines")

    # Check Q-B1: Commented out code
    # Look for commented out C# statements: // ... ; or /* ... ; */
    qb1_violations = []
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//") and not stripped.startswith("///"):
            comment_text = stripped[2:].strip()
            # If comment ends with semicolon or contains common code constructs
            if re.search(r'(?:var|if|for|while|return|Assert|using|new)\s+[A-Za-z0-9_]+.*?;', comment_text) or \
               re.search(r'^[A-Za-z0-9_]+\.[A-Za-z0-9_]+\(.*?\);', comment_text):
                qb1_violations.append((idx, line))

    # Check Q-B2: Unannotated empty catch blocks
    qb2_violations = []
    empty_catch_pattern = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{\s*\}')
    for match in empty_catch_pattern.finditer(content):
        start_idx = match.start()
        line_num = content[:start_idx].count('\n') + 1
        qb2_violations.append((line_num, match.group(0)))

    print(f"Rule C2: {'PASS' if c2_pass else 'FAIL'}")
    print(f"Rule M3: {'PASS' if not m3_violations else 'FAIL'} (violations: {len(m3_violations)})")
    if m3_violations:
        for v in m3_violations:
            print(f"  FAIL: {v[0]} has {v[3]} lines (lines {v[1]}-{v[2]})")

    print(f"Rule Q-B1: {'PASS' if not qb1_violations else 'FAIL'} (violations: {len(qb1_violations)})")
    if qb1_violations:
        for v in qb1_violations:
            print(f"  FAIL line {v[0]}: {v[1]}")

    print(f"Rule Q-B2: {'PASS' if not qb2_violations else 'FAIL'} (violations: {len(qb2_violations)})")
    if qb2_violations:
        for v in qb2_violations:
            print(f"  FAIL line {v[0]}: {v[1]}")

    return c2_pass and not m3_violations and not qb1_violations and not qb2_violations

target = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge.Tests\Host\PublicRouteRegressionTests.cs"
result = check_file(target)
print(f"\nOverall Result for {target}: {'PASS' if result else 'FAIL'}")
