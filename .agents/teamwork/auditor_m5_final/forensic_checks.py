#!/usr/bin/env python3
"""
Independent Forensic Integrity Verification Script
By: auditor_m5_final
"""

import os
import re
import sys
import json
import hashlib

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
HPGRASSHOPPER_DIR = os.path.join(BASE_DIR, "HPGrasshopper")
MCPSHARED_DIR = os.path.join(BASE_DIR, "McpShared")

print("=" * 80)
print("FORENSIC INTEGRITY AUDIT — Milestone M5 Final Pass")
print(f"Target directory: {HPGRASSHOPPER_DIR}")
print("=" * 80)

# 1. Collect all C# files in HPGrasshopper (excluding bin/obj)
cs_files = []
for root, dirs, files in os.walk(HPGRASSHOPPER_DIR):
    if any(b in root.split(os.sep) for b in ['bin', 'obj', '.vs']):
        continue
    for f in files:
        if f.endswith('.cs'):
            cs_files.append(os.path.join(root, f))

cs_files.sort()
print(f"Total C# files found in HPGrasshopper: {len(cs_files)}")

# Check 1: Rule C2 (File Length <= 300)
c2_violations = []
file_lengths = {}
for p in cs_files:
    rel = os.path.relpath(p, BASE_DIR).replace('\\', '/')
    with open(p, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    count = len(lines)
    file_lengths[rel] = count
    if count > 300:
        c2_violations.append((rel, count))

max_file = max(file_lengths.items(), key=lambda x: x[1])
print(f"\n[Check 1: Rule C2 File Length <= 300]")
print(f"Max file length: {max_file[0]} ({max_file[1]} lines)")
if c2_violations:
    print(f"FAIL: {len(c2_violations)} files violate Rule C2:")
    for v in c2_violations:
        print(f"  - {v[0]}: {v[1]} lines")
else:
    print("PASS: 0 files exceed 300 lines.")

# Check 2: Rule Q-B1 (Commented-out dead code)
# Detect comments that look like commented-out statements
print(f"\n[Check 2: Rule Q-B1 Commented-out Dead Code]")
qb1_violations = []
statement_patterns = [
    re.compile(r'^\s*//\s*(var|int|string|bool|double|float|return|throw|new|if\s*\(|for\s*\(|foreach\s*\(|while\s*\()\s+.*[;{]\s*$'),
    re.compile(r'^\s*//\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\(.*\);\s*$'),
    re.compile(r'^\s*//\s*await\s+[A-Za-z0-9_]+\(.*\);\s*$'),
]
# Ignore harmless documentation comments, links, or multi-line comment headers
for p in cs_files:
    rel = os.path.relpath(p, BASE_DIR).replace('\\', '/')
    with open(p, 'r', encoding='utf-8', errors='replace') as f:
        for idx, line in enumerate(f, start=1):
            stripped = line.strip()
            # ignore xml doc comments
            if stripped.startswith('///'):
                continue
            for pat in statement_patterns:
                if pat.match(line):
                    # Check if it's an example in comments or explanation
                    if any(kw in stripped.lower() for kw in ['example:', 'e.g.', 'todo', 'note:', 'schema:', 'multi-version:', 'http']):
                        continue
                    qb1_violations.append((rel, idx, stripped))

if qb1_violations:
    print(f"FLAGGED: {len(qb1_violations)} potential commented-out code lines found:")
    for rel, line_num, text in qb1_violations[:10]:
        print(f"  - {rel}:{line_num} -> {text}")
else:
    print("PASS: 0 commented-out dead code lines detected.")

# Check 3: Rule Q-B2 (Unannotated empty catch blocks)
print(f"\n[Check 3: Rule Q-B2 Unannotated Empty Catch Blocks]")
qb2_violations = []

def tokenize_for_catch(text):
    tokens = []
    i = 0
    n = len(text)
    line = 1
    while i < n:
        ch = text[i]
        if ch == '\n':
            line += 1
            i += 1
            continue
        if ch.isspace():
            i += 1
            continue
        if ch == '/' and i + 1 < n and text[i+1] == '/':
            # line comment
            i += 2
            comment = ""
            while i < n and text[i] != '\n':
                comment += text[i]
                i += 1
            tokens.append(('COMMENT', comment, line))
            continue
        if ch == '/' and i + 1 < n and text[i+1] == '*':
            # block comment
            i += 2
            comment = ""
            while i + 1 < n and not (text[i] == '*' and text[i+1] == '/'):
                if text[i] == '\n': line += 1
                comment += text[i]
                i += 1
            i += 2
            tokens.append(('COMMENT', comment, line))
            continue
        if ch == '"':
            # string literal
            i += 1
            escaped = False
            while i < n:
                if escaped:
                    escaped = False
                elif text[i] == '\\':
                    escaped = True
                elif text[i] == '"':
                    i += 1
                    break
                elif text[i] == '\n':
                    line += 1
                i += 1
            tokens.append(('STRING', "", line))
            continue
        if ch in '{}();':
            tokens.append(('PUNCT', ch, line))
            i += 1
            continue
        if ch.isalpha() or ch == '_':
            ident = ""
            while i < n and (text[i].isalnum() or text[i] == '_'):
                ident += text[i]
                i += 1
            tokens.append(('IDENT', ident, line))
            continue
        i += 1
    return tokens

for p in cs_files:
    rel = os.path.relpath(p, BASE_DIR).replace('\\', '/')
    with open(p, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()
    toks = tokenize_for_catch(content)
    # Search for pattern: IDENT('catch') -> optional params in () -> PUNCT('{') -> PUNCT('}') without comment
    for idx, tok in enumerate(toks):
        if tok[0] == 'IDENT' and tok[1] == 'catch':
            # look ahead to find '{'
            j = idx + 1
            has_paren = False
            if j < len(toks) and toks[j][0] == 'PUNCT' and toks[j][1] == '(':
                has_paren = True
                p_depth = 1
                j += 1
                while j < len(toks) and p_depth > 0:
                    if toks[j][0] == 'PUNCT':
                        if toks[j][1] == '(': p_depth += 1
                        elif toks[j][1] == ')': p_depth -= 1
                    j += 1
            if j < len(toks) and toks[j][0] == 'PUNCT' and toks[j][1] == '{':
                # Now check inside braces
                k = j + 1
                brace_content = []
                while k < len(toks) and not (toks[k][0] == 'PUNCT' and toks[k][1] == '}'):
                    brace_content.append(toks[k])
                    k += 1
                if k < len(toks):
                    # Check if body is empty or only has comments
                    code_in_body = [t for t in brace_content if t[0] != 'COMMENT']
                    comments_in_body = [t for t in brace_content if t[0] == 'COMMENT']
                    if len(code_in_body) == 0:
                        # Empty catch block! Does it have an explanatory comment?
                        if len(comments_in_body) == 0:
                            qb2_violations.append((rel, tok[2], "Unannotated empty catch block (no statements, no comments)"))
                        else:
                            # It has comments explaining why it's empty (allowed by Q-B2 if explained)
                            pass

if qb2_violations:
    print(f"FAIL: {len(qb2_violations)} unannotated empty catch blocks found:")
    for rel, line_num, reason in qb2_violations:
        print(f"  - {rel}:{line_num} -> {reason}")
else:
    print("PASS: 0 unannotated empty catch blocks detected.")

# Check 4: ADR-0007 Embedded Seeds Quality & Line Counts
print(f"\n[Check 4: ADR-0007 Embedded Seeds Quality]")
seed_dir = os.path.join(HPGRASSHOPPER_DIR, "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary")
seed_tools = []
for root, dirs, files in os.walk(seed_dir):
    if "tool.json" in files:
        tool_json_path = os.path.join(root, "tool.json")
        code_cs_path = os.path.join(root, "code.cs")
        seed_tools.append((root, tool_json_path, code_cs_path))

print(f"Found {len(seed_tools)} embedded seed tools in SeedLibrary (expected 12).")
adr0007_issues = []
for sroot, tj_path, cs_path in seed_tools:
    rel_seed = os.path.relpath(sroot, seed_dir).replace('\\', '/')
    with open(tj_path, 'r', encoding='utf-8') as f:
        tj = json.load(f)
    tool_name = tj.get("name", "UNKNOWN")
    if not os.path.exists(cs_path):
        adr0007_issues.append(f"{rel_seed}: Missing code.cs")
        continue
    with open(cs_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    count = len(lines)
    code_text = "".join(lines)
    if count > 300:
        adr0007_issues.append(f"{tool_name} ({rel_seed}): {count} lines exceeds 300")
    if "/*" in code_text or ("//" in code_text and any(pat.match(l) for l in lines for pat in statement_patterns)):
        pass
    print(f"  - Seed: {tool_name:<32} Lines: {count:>3} | Path: {rel_seed}")

if len(seed_tools) != 12:
    adr0007_issues.append(f"Expected exactly 12 seeds, but found {len(seed_tools)}")

if adr0007_issues:
    print(f"FAIL: ADR-0007 seed issues found:")
    for issue in adr0007_issues:
        print(f"  - {issue}")
else:
    print("PASS: All 12 seeds meet ADR-0007 requirements (0 > 300 lines, all valid).")

# Check 5: Forensic Prohibited Patterns (Hardcoded test results / facade returns)
print(f"\n[Check 5: Forensic Prohibited Patterns]")
prohibited_findings = []
# Look for fake test assertions or return constant mocks in production code
# (excluding test projects)
prod_cs_files = [f for f in cs_files if 'Tests' not in f]
print(f"Scanning {len(prod_cs_files)} production C# files for mock facades / hardcoded bypasses...")

facade_patterns = [
    (re.compile(r'public\s+bool\s+[A-Za-z0-9_]+\s*\([^)]*\)\s*=>\s*true\s*;'), "Constant => true method"),
    (re.compile(r'public\s+string\s+[A-Za-z0-9_]+\s*\([^)]*\)\s*=>\s*"[^"]+"\s*;'), "Constant => string method"),
    (re.compile(r'return\s+new\s+ExecuteResult\s*\{\s*Success\s*=\s*true\s*\}\s*;'), "Hardcoded ExecuteResult success without computation"),
    (re.compile(r'return\s+new\s+CallToolResult\s*\{\s*IsError\s*=\s*false\s*\}\s*;'), "Hardcoded CallToolResult success"),
]

for p in prod_cs_files:
    rel = os.path.relpath(p, BASE_DIR).replace('\\', '/')
    with open(p, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()
    for pat, desc in facade_patterns:
        matches = list(pat.finditer(content))
        for m in matches:
            # Check context
            snip = content[max(0, m.start()-50):min(len(content), m.end()+50)].replace('\n', ' ')
            prohibited_findings.append((rel, desc, snip))

if prohibited_findings:
    print(f"FLAGGED: {len(prohibited_findings)} potential facade patterns found in production files:")
    for rel, desc, snip in prohibited_findings:
        print(f"  - {rel}: {desc} -> {snip}")
else:
    print("PASS: 0 facade implementations or hardcoded bypasses found.")

print("\n" + "=" * 80)
print("AUDIT SCRIPT SUMMARY:")
print(f"C2 Violations: {len(c2_violations)}")
print(f"Q-B1 Violations: {len(qb1_violations)}")
print(f"Q-B2 Violations: {len(qb2_violations)}")
print(f"ADR-0007 Issues: {len(adr0007_issues)}")
print(f"Prohibited Pattern Findings: {len(prohibited_findings)}")
print("=" * 80)
