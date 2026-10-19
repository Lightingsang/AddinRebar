import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.stderr.reconfigure(encoding='utf-8')

repo_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar'
hpdce_dir = os.path.join(repo_root, 'HPDcePro')
src_dir = os.path.join(hpdce_dir, 'src')
assets_dir = os.path.join(hpdce_dir, 'assets')

print("=== CHECK 1: SCANNING FOR 'C:\\Dce Pro' IN HPDcePro/src and HPDcePro/assets ===")
forbidden_occurrences = []

for base_dir in [src_dir, assets_dir]:
    for root, dirs, files in os.walk(base_dir):
        # skip binary files in assets if too big or non-text
        for f in files:
            fpath = os.path.join(root, f)
            rel = os.path.relpath(fpath, repo_root)
            # only scan text/source/config files
            ext = os.path.splitext(f)[1].lower()
            if ext in ['.cs', '.xaml', '.json', '.txt', '.lsp', '.xml', '.props', '.slnx', '.md', '.csv', '.ini', '.config']:
                try:
                    with open(fpath, 'r', encoding='utf-8', errors='ignore') as infile:
                        for lno, line in enumerate(infile):
                            if 'c:\\dce pro' in line.lower() or 'c:/dce pro' in line.lower():
                                forbidden_occurrences.append((rel, lno + 1, line.strip()))
                except Exception as e:
                    pass

print(f"Total occurrences of 'C:\\Dce Pro' found: {len(forbidden_occurrences)}")
for rel, lno, line in forbidden_occurrences:
    print(f"  VIOLATION: {rel}:{lno} -> {line}")

print("\n=== CHECK 2: FILE LENGTH AUDIT (RULE C2 <= 300 LINES) ===")
c2_violations = []
total_cs_files = 0
max_lines = 0
max_file = ""

for root, dirs, files in os.walk(src_dir):
    if 'bin' in dirs: dirs.remove('bin')
    if 'obj' in dirs: dirs.remove('obj')
    for f in files:
        if f.endswith('.cs'):
            total_cs_files += 1
            fpath = os.path.join(root, f)
            rel = os.path.relpath(fpath, repo_root)
            with open(fpath, 'r', encoding='utf-8', errors='ignore') as infile:
                count = sum(1 for _ in infile)
            if count > max_lines:
                max_lines = count
                max_file = rel
            if count > 300:
                c2_violations.append((rel, count))

print(f"Total C# source files scanned: {total_cs_files}")
print(f"Longest file: {max_file} ({max_lines} lines)")
print(f"Rule C2 violations (> 300 lines): {len(c2_violations)}")
for rel, count in c2_violations:
    print(f"  VIOLATION C2: {rel} has {count} lines (> 300)")

print("\n=== CHECK 3: METHOD LENGTH AUDIT (RULE M3 <= 50 LINES) ===")
# Parse methods by brace matching
m3_violations = []

def analyze_methods_in_file(fpath, rel):
    with open(fpath, 'r', encoding='utf-8', errors='ignore') as infile:
        lines = infile.readlines()
    
    # Simple brace depth tracker to find method blocks
    # Look for method signatures
    method_sig_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|async|override|virtual|sealed|\s)+\s+(?:[\w\<\>\[\]\?\,\s]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where[^{]+)?\s*\{?$')
    
    i = 0
    while i < len(lines):
        line = lines[i]
        m = method_sig_pattern.match(line)
        if m and not line.strip().startswith('class ') and not line.strip().startswith('interface ') and not line.strip().startswith('struct '):
            method_name = m.group(1)
            start_line = i + 1
            # Find opening brace
            brace_count = 0
            found_first_brace = False
            j = i
            while j < len(lines):
                for char in lines[j]:
                    if char == '{':
                        brace_count += 1
                        found_first_brace = True
                    elif char == '}':
                        brace_count -= 1
                if found_first_brace and brace_count == 0:
                    break
                j += 1
            if found_first_brace and j < len(lines):
                end_line = j + 1
                length = end_line - start_line + 1
                if length > 50:
                    m3_violations.append((rel, method_name, start_line, end_line, length))
                i = j
        i += 1

for root, dirs, files in os.walk(src_dir):
    if 'bin' in dirs: dirs.remove('bin')
    if 'obj' in dirs: dirs.remove('obj')
    for f in files:
        if f.endswith('.cs'):
            fpath = os.path.join(root, f)
            rel = os.path.relpath(fpath, repo_root)
            analyze_methods_in_file(fpath, rel)

print(f"Rule M3 violations (> 50 lines): {len(m3_violations)}")
for rel, mname, s, e, l in m3_violations:
    print(f"  VIOLATION M3: {rel}:{s}-{e} {mname}() has {l} lines (> 50)")

print("\n=== CHECK 4: DEAD / COMMENTED-OUT CODE AUDIT (RULE Q-B1) ===")
dead_code_pattern = re.compile(r'^\s*//\s*(?:var\s+\w+\s*=|return\s+|if\s*\(|for\s*\(|while\s*\(|\w+\.\w+\([^\)]*\);)\s*$')
dead_code_findings = []

for root, dirs, files in os.walk(src_dir):
    if 'bin' in dirs: dirs.remove('bin')
    if 'obj' in dirs: dirs.remove('obj')
    for f in files:
        if f.endswith('.cs'):
            fpath = os.path.join(root, f)
            rel = os.path.relpath(fpath, repo_root)
            with open(fpath, 'r', encoding='utf-8', errors='ignore') as infile:
                for lno, line in enumerate(infile):
                    if dead_code_pattern.match(line):
                        dead_code_findings.append((rel, lno + 1, line.strip()))

print(f"Dead code findings (Rule Q-B1): {len(dead_code_findings)}")
for rel, lno, line in dead_code_findings:
    print(f"  FINDING Q-B1: {rel}:{lno} -> {line}")

print("\n=== CHECK 5: CATCH BLOCK ANNOTATIONS (RULE Q-B2) ===")
catch_findings = []

for root, dirs, files in os.walk(src_dir):
    if 'bin' in dirs: dirs.remove('bin')
    if 'obj' in dirs: dirs.remove('obj')
    for f in files:
        if f.endswith('.cs'):
            fpath = os.path.join(root, f)
            rel = os.path.relpath(fpath, repo_root)
            with open(fpath, 'r', encoding='utf-8', errors='ignore') as infile:
                lines = infile.readlines()
            for lno, line in enumerate(lines):
                if re.search(r'\bcatch\b', line):
                    # Inspect next 5 lines for Reason:
                    block = "".join(lines[lno:min(len(lines), lno + 6)])
                    if 'Reason:' not in block and 'reason:' not in block and '//' not in block:
                        catch_findings.append((rel, lno + 1, line.strip()))

print(f"Unannotated catch blocks (Rule Q-B2): {len(catch_findings)}")
for rel, lno, line in catch_findings:
    print(f"  VIOLATION Q-B2: {rel}:{lno} -> {line}")
