import os
import re
import sys

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def find_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        # Skip bin and obj directories
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_file_c2(file_path):
    with open(file_path, "r", encoding="utf-8-sig", errors="replace") as f:
        lines = f.readlines()
    return len(lines), lines

def check_todo_fixme_hack(lines, rel_path):
    violations = []
    for idx, line in enumerate(lines, 1):
        # Match TODO, FIXME, HACK not as part of words like HACKER or KATA_HACK
        match = re.search(r"\b(TODO|FIXME|HACK)\b", line, re.IGNORECASE)
        if match:
            violations.append((idx, line.strip()))
    return violations

def check_commented_code(lines, rel_path):
    violations = []
    # Patterns that look like commented-out code
    code_patterns = [
        re.compile(r"^\s*//\s*(var|int|string|bool|double|float|return|if|for|foreach|while|try|catch|throw|using|new\s+[A-Z]|class|public|private|protected|internal)\s+"),
        re.compile(r"^\s*//\s*[a-zA-Z0-9_.]+\s*\([^)]*\)\s*;"),
        re.compile(r"^\s*//\s*[a-zA-Z0-9_.]+\s*=\s*[^;]+;"),
    ]
    for idx, line in enumerate(lines, 1):
        # Skip explanatory comments
        if any(marker in line for marker in ["Multi-version:", "Example:", "e.g.", "Rule:", "XML", "Note:"]):
            continue
        for pat in code_patterns:
            if pat.search(line):
                violations.append((idx, line.strip()))
                break
    return violations

def check_empty_catches(content, rel_path):
    violations = []
    # Match catch (...) { } where block has no statements and no comment
    empty_catch_re = re.compile(r"catch\s*(?:\([^)]*\))?\s*\{([^{}]*)\}", re.MULTILINE)
    for m in empty_catch_re.finditer(content):
        inner = m.group(1).strip()
        if not inner or (not inner.startswith("//") and not inner.startswith("/*")):
            # Find line number
            line_no = content[:m.start()].count("\n") + 1
            violations.append((line_no, m.group(0).strip()))
    return violations

def parse_methods_m3(lines, rel_path):
    # Method detection heuristic
    violations = []
    brace_depth = 0
    in_method = False
    method_name = ""
    method_start_line = 0
    method_brace_start_depth = 0
    
    method_sig_pattern = re.compile(
        r"^\s*(?:public|private|protected|internal|static|virtual|override|async|sealed|\s)+\s+"
        r"(?:(?:[a-zA-Z0-9_<>,\[\]\?]+)\s+)+([a-zA-Z0-9_]+)\s*\([^;]*\)\s*(?:where\s+.*)?$"
    )
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        
        if not in_method and brace_depth >= 1:
            match = method_sig_pattern.match(stripped)
            if match and not stripped.endswith(";"):
                potential_name = match.group(1)
                if potential_name not in ("class", "interface", "record", "struct", "enum", "namespace"):
                    method_name = potential_name
                    method_start_line = idx
        
        in_str = False
        in_char = False
        in_line_comment = False
        for c_idx, c in enumerate(line):
            if in_line_comment:
                break
            if c == '/' and c_idx + 1 < len(line) and line[c_idx+1] == '/':
                in_line_comment = True
                break
            if c == '"' and not in_char:
                if c_idx > 0 and line[c_idx-1] == '\\':
                    pass
                else:
                    in_str = not in_str
            elif c == "'" and not in_str:
                if c_idx > 0 and line[c_idx-1] == '\\':
                    pass
                else:
                    in_char = not in_char
            elif not in_str and not in_char:
                if c == '{':
                    brace_depth += 1
                    if method_name and not in_method:
                        in_method = True
                        method_brace_start_depth = brace_depth
                elif c == '}':
                    if in_method and brace_depth == method_brace_start_depth:
                        method_len = idx - method_start_line + 1
                        if method_len > 50:
                            violations.append((method_name, method_start_line, idx, method_len))
                        in_method = False
                        method_name = ""
                    brace_depth -= 1
    return violations

print("Scanning HPKata...")
cs_files = find_cs_files(HPKATA_DIR)
print(f"Total C# files found in HPKata: {len(cs_files)}")

c2_violations = []
qb1_todo_violations = []
qb1_code_violations = []
qb2_catch_violations = []
m3_violations = []

phase4_keywords = [
    "foundation", "slab", "stair", "lintel", "sectioncut",
    "vesan", "vethang", "vmd", "vmc", "vmbm", "pile",
    "padfooting", "footing", "pilecap"
]

phase4_files = []
for f in cs_files:
    rel = os.path.relpath(f, HPKATA_DIR).replace("\\", "/")
    rel_lower = rel.lower()
    if any(k in rel_lower for k in phase4_keywords):
        phase4_files.append(f)

print(f"Total Phase 4 related C# files found: {len(phase4_files)}")

for f in cs_files:
    rel = os.path.relpath(f, HPKATA_DIR).replace("\\", "/")
    total_lines, lines = check_file_c2(f)
    content = "".join(lines)
    
    # C2 Check
    if total_lines > 300:
        c2_violations.append((rel, total_lines))
        
    # Q-B1: TODO/FIXME/HACK
    todos = check_todo_fixme_hack(lines, rel)
    if todos:
        qb1_todo_violations.append((rel, todos))
        
    # Q-B1: Commented code
    c_code = check_commented_code(lines, rel)
    if c_code:
        qb1_code_violations.append((rel, c_code))
        
    # Q-B2: Empty catches
    catches = check_empty_catches(content, rel)
    if catches:
        qb2_catch_violations.append((rel, catches))
        
    # M3: Method length > 50 (specifically check all Phase 4 files and report on any)
    m3s = parse_methods_m3(lines, rel)
    if m3s:
        m3_violations.append((rel, m3s))

print(f"\n--- Rule C2 (<= 300 lines) Violations: {len(c2_violations)} ---")
for rel, cnt in c2_violations:
    is_p4 = any(k in rel.lower() for k in phase4_keywords)
    print(f"  {'[PHASE 4] ' if is_p4 else ''}{rel}: {cnt} lines")

print(f"\n--- Rule Q-B1 (TODO/FIXME/HACK) Violations: {len(qb1_todo_violations)} ---")
for rel, todos in qb1_todo_violations:
    print(f"  {rel}: {todos}")

print(f"\n--- Rule Q-B1 (Commented Code) Violations: {len(qb1_code_violations)} ---")
for rel, code in qb1_code_violations:
    print(f"  {rel}: {code}")

print(f"\n--- Rule Q-B2 (Unannotated Empty Catch) Violations: {len(qb2_catch_violations)} ---")
for rel, catches in qb2_catch_violations:
    print(f"  {rel}: {catches}")

print(f"\n--- Rule M3 (> 50 lines method) Violations in Phase 4 files: ---")
p4_m3 = []
for rel, m3s in m3_violations:
    if any(k in rel.lower() for k in phase4_keywords):
        p4_m3.append((rel, m3s))
        print(f"  [PHASE 4 VIOLATION] {rel}: {m3s}")

if not p4_m3:
    print("  Zero Phase 4 method violations found!")

# Host Neutrality check: HPKata.Core must not reference Autodesk
print("\n--- Host Neutrality Check (HPKata.Core) ---")
core_violations = []
for f in cs_files:
    rel = os.path.relpath(f, HPKATA_DIR).replace("\\", "/")
    if rel.startswith("HPKata.Core/"):
        _, lines = check_file_c2(f)
        for idx, line in enumerate(lines, 1):
            if re.search(r"\bAutodesk\b", line):
                core_violations.append((rel, idx, line.strip()))

if core_violations:
    print(f"VIOLATIONS FOUND in HPKata.Core ({len(core_violations)}):")
    for r, idx, l in core_violations:
        print(f"  {r}:{idx} -> {l}")
else:
    print("PASS: HPKata.Core has ZERO Autodesk references.")

# Rule R5 Check: 304.8 in HPKata
print("\n--- Rule R5 Check (304.8 constant) ---")
r5_occurrences = []
for f in cs_files:
    rel = os.path.relpath(f, HPKATA_DIR).replace("\\", "/")
    _, lines = check_file_c2(f)
    for idx, line in enumerate(lines, 1):
        if "304.8" in line:
            r5_occurrences.append((rel, idx, line.strip()))

print(f"Occurrences of 304.8 in HPKata: {len(r5_occurrences)}")
for r, idx, l in r5_occurrences:
    print(f"  {r}:{idx} -> {l}")
