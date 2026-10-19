#!/usr/bin/env python3
import os
import re
import sys

TARGET_DIR = r"HPGrasshopper/HPGrasshopper.McpBridge"

print("=" * 80)
print(f"INDEPENDENT FORENSIC INTEGRITY AUDIT: {TARGET_DIR}")
print("=" * 80)

cs_files = []
for root, _, files in os.walk(TARGET_DIR):
    if "bin" in root or "obj" in root:
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

cs_files.sort()
print(f"Total C# files located: {len(cs_files)}")

# 1. Check Rule C2: File line lengths
c2_violations = []
file_lengths = {}
for p in cs_files:
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
    count = len(lines)
    file_lengths[p] = count
    if count > 300:
        c2_violations.append((p, count))

print("\n--- CHECK 1: RULE C2 FILE LENGTH (<= 300 lines) ---")
if c2_violations:
    print(f"FAILED C2: {len(c2_violations)} files exceed 300 lines!")
    for p, c in c2_violations:
        print(f"  {p}: {c} lines")
else:
    max_f = max(file_lengths.items(), key=lambda x: x[1])
    print(f"PASS: All {len(cs_files)} files <= 300 lines. (Max: {max_f[0]} with {max_f[1]} lines)")

# 2. Check Rule M3: Method lengths
# Regex and bracket depth parser for methods
def parse_methods_in_file(path):
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
    lines = text.splitlines()
    
    # We strip string literals and comments to do exact token/bracket counting
    # But preserve line positions
    methods = []
    
    # Line-by-line method signature search
    # Matches common C# method / ctor / property patterns
    method_sig_pattern = re.compile(
        r'^\s*(?:(?:public|private|protected|internal|static|async|virtual|override|sealed|unsafe|partial)\s+)+([A-Za-z0-9_<>?,\[\]\s]+?)\s+([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(([^;]*?)\)\s*(?:where\s+.*)?$'
    )
    
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        
        # Check if line or multi-line looks like a method signature
        if any(stripped.startswith(mod) for mod in ['public ', 'private ', 'protected ', 'internal ', 'static ', 'async ']):
            # Accumulate signature lines until '(' and ')'
            sig_start = i
            sig_lines = [stripped]
            j = i
            while j < len(lines) and ')' not in lines[j] and ';' not in lines[j] and '{' not in lines[j] and (j - i) < 15:
                j += 1
                if j < len(lines):
                    sig_lines.append(lines[j].strip())
            
            sig_full = " ".join(sig_lines)
            
            # Check if this is a method (contains '(' and ')' and not class/interface/delegate/record/event)
            if '(' in sig_full and ')' in sig_full and not any(kw in sig_full for kw in ['class ', 'interface ', 'delegate ', 'record ', 'event ']):
                # Now find body '{'
                brace_line = -1
                k = j
                while k < len(lines) and (k - j) < 10:
                    if '{' in lines[k]:
                        brace_line = k
                        break
                    if ';' in lines[k] and '{' not in lines[k]:
                        break
                    k += 1
                
                if brace_line != -1:
                    # Find closing brace '}' matching this '{'
                    depth = 0
                    end_line = -1
                    in_block_comment = False
                    for m in range(brace_line, len(lines)):
                        l_text = lines[m]
                        # crude string / comment removal for brace matching
                        # remove strings
                        clean_l = re.sub(r'"(\\.|[^"\\])*"', '""', l_text)
                        clean_l = re.sub(r'//.*$', '', clean_l)
                        for ch in clean_l:
                            if ch == '{':
                                depth += 1
                            elif ch == '}':
                                depth -= 1
                                if depth == 0:
                                    end_line = m
                                    break
                        if depth == 0:
                            break
                    
                    if end_line != -1:
                        total_span = end_line - sig_start + 1
                        body_span = end_line - brace_line + 1
                        name_match = re.search(r'([A-Za-z0-9_]+)\s*(?:<[^>]+>)?\s*\(', sig_full)
                        m_name = name_match.group(1) if name_match else "unknown"
                        methods.append({
                            "name": m_name,
                            "sig_start": sig_start + 1,
                            "brace_start": brace_line + 1,
                            "end_line": end_line + 1,
                            "total_span": total_span,
                            "body_span": body_span,
                            "signature": sig_full[:60]
                        })
                        i = end_line
        i += 1
    return methods

all_methods = []
m3_violations = []
for p in cs_files:
    m_list = parse_methods_in_file(p)
    for m in m_list:
        m["file"] = p
        all_methods.append(m)
        if m["total_span"] > 50 or m["body_span"] > 50:
            m3_violations.append(m)

print(f"\n--- CHECK 2: RULE M3 METHOD LENGTH (<= 50 lines) ---")
print(f"Total methods analyzed across project: {len(all_methods)}")
if m3_violations:
    print(f"FAILED M3: {len(m3_violations)} methods exceed 50 lines!")
    for m in m3_violations:
        print(f"  {m['file']}:{m['sig_start']}-{m['end_line']} -> {m['name']} (Total: {m['total_span']}, Body: {m['body_span']})")
else:
    print("PASS: Zero methods violate Rule M3 (<= 50 lines).")
    top_methods = sorted(all_methods, key=lambda x: max(x["total_span"], x["body_span"]), reverse=True)[:5]
    print("Longest methods found:")
    for m in top_methods:
        print(f"  {m['file']}:{m['sig_start']}-{m['end_line']} -> {m['name']} (Total: {m['total_span']}, Body: {m['body_span']})")

# 3. Check Rule Q-B1: Commented-out code
print("\n--- CHECK 3: RULE Q-B1 ZERO DEAD / COMMENTED CODE ---")
code_comment_patterns = [
    re.compile(r'^\s*//\s*(?:var|int|string|bool|if|for|foreach|while|return|throw|try|catch|using|new)\s+[A-Za-z0-9_]'),
    re.compile(r'^\s*//\s*[A-Za-z0-9_]+\s*=\s*[^;]+;'),
    re.compile(r'^\s*//\s*[A-Za-z0-9_.]+\([^)]*\);'),
]
qb1_violations = []
for p in cs_files:
    with open(p, "r", encoding="utf-8") as f:
        for idx, line in enumerate(f, 1):
            for pat in code_comment_patterns:
                if pat.search(line):
                    # Filter out documentation examples in XML comments (/// <example> etc)
                    if not line.strip().startswith("///"):
                        qb1_violations.append((p, idx, line.strip()))

if qb1_violations:
    print(f"FAILED Q-B1: {len(qb1_violations)} commented code lines found!")
    for p, idx, text in qb1_violations:
        print(f"  {p}:{idx} -> {text}")
else:
    print("PASS: 0 lines of commented-out code detected.")

# 4. Check Rule Q-B2: Unannotated empty catch blocks
print("\n--- CHECK 4: RULE Q-B2 ZERO UNANNOTATED EMPTY CATCH BLOCKS ---")
empty_catch_pattern = re.compile(r'catch(?:\s*\([^)]*\))?\s*\{\s*\}')
qb2_violations = []
for p in cs_files:
    with open(p, "r", encoding="utf-8") as f:
        content = f.read()
    matches = list(empty_catch_pattern.finditer(content))
    if matches:
        for m in matches:
            line_no = content[:m.start()].count("\n") + 1
            qb2_violations.append((p, line_no, m.group(0)))

if qb2_violations:
    print(f"FAILED Q-B2: {len(qb2_violations)} unannotated empty catch blocks found!")
    for p, idx, text in qb2_violations:
        print(f"  {p}:{idx} -> {text}")
else:
    print("PASS: 0 unannotated empty catch blocks detected.")

# 5. Check for cheats / dummy facades / NotImplementedException
print("\n--- CHECK 5: CHEATS, FACADES, HARDCODED RESULTS ---")
cheat_patterns = [
    re.compile(r'throw\s+new\s+NotImplementedException'),
    re.compile(r'return\s+true;\s*//\s*bypass', re.IGNORECASE),
    re.compile(r'//\s*TODO\s*implement', re.IGNORECASE),
]
cheat_violations = []
for p in cs_files:
    with open(p, "r", encoding="utf-8") as f:
        for idx, line in enumerate(f, 1):
            for pat in cheat_patterns:
                if pat.search(line):
                    cheat_violations.append((p, idx, line.strip()))

if cheat_violations:
    print(f"FAILED CHEATS: {len(cheat_violations)} suspicious lines found!")
    for p, idx, text in cheat_violations:
        print(f"  {p}:{idx} -> {text}")
else:
    print("PASS: 0 fake facades or bypasses detected.")

print("\n" + "=" * 80)
total_fails = len(c2_violations) + len(m3_violations) + len(qb1_violations) + len(qb2_violations) + len(cheat_violations)
print(f"STATIC AUDIT OVERALL RESULT: {'CLEAN (0 failures)' if total_fails == 0 else f'INTEGRITY VIOLATION ({total_fails} failures)'}")
print("=" * 80)
