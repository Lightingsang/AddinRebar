import os
import re

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

m2_dirs = [
    r"HPKata/HPKata.Core/Columns",
    r"HPKata/HPKata.Core/Walls",
    r"HPKata/HPKata.UI/ViewModels",
    r"HPKata/HPKata.UI/Views",
    r"HPKata/HPKata.AutoCad/Commands",
    r"HPKata/HPKata.AutoCad/Controllers",
    r"HPKata/HPKata.Revit/Commands",
    r"HPKata/HPKata.Revit/Services",
    r"HPKata/HPKata.Core.Tests/Columns",
    r"HPKata/HPKata.Core.Tests/Walls",
    r"HPKata/HPKata.UI.Tests/ViewModels",
    r"HPKata/HPKata.AutoCad.Tests/Controllers",
    r"HPKata/HPKata.Revit.Tests/Geometry",
    r"HPKata/HPKata.E2E.Tests/Scenarios"
]

# Specifically select M2 files
m2_files = []
for d in m2_dirs:
    full_d = os.path.normpath(os.path.join(base_dir, d))
    if not os.path.exists(full_d):
        continue
    for root, _, filenames in os.walk(full_d):
        for f in filenames:
            if not f.endswith(".cs"):
                continue
            lower = f.lower()
            # Filter specifically to M2 files if directory is shared (like Commands or Scenarios)
            if "beam" in lower or "foundation" in lower or "slab" in lower or "stair" in lower or "qs" in lower:
                continue
            # Keep Column, Wall, EtabsColumnWall, Scenario2, Scenario3, etc.
            rel_path = os.path.relpath(os.path.join(root, f), base_dir)
            m2_files.append(rel_path)

m2_files = sorted(list(set(m2_files)))
print(f"Found {len(m2_files)} M2 files to audit.")

def analyze_methods(rel_path):
    full_path = os.path.normpath(os.path.join(base_dir, rel_path))
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.read().splitlines()

    methods = []
    stack = []
    depth = 0
    in_multi_line_comment = False
    in_string = False
    in_verbatim = False
    in_char = False

    for line_idx, line in enumerate(lines):
        col = 0
        while col < len(line):
            c = line[col]
            if in_multi_line_comment:
                if c == '*' and col + 1 < len(line) and line[col+1] == '/':
                    in_multi_line_comment = False
                    col += 2
                    continue
                col += 1
                continue
            if in_string:
                if c == '\\':
                    col += 2
                    continue
                if c == '"':
                    in_string = False
                col += 1
                continue
            if in_verbatim:
                if c == '"':
                    if col + 1 < len(line) and line[col+1] == '"':
                        col += 2
                        continue
                    in_verbatim = False
                col += 1
                continue
            if in_char:
                if c == '\\':
                    col += 2
                    continue
                if c == "'":
                    in_char = False
                col += 1
                continue

            if c == '/' and col + 1 < len(line):
                if line[col+1] == '/':
                    break
                elif line[col+1] == '*':
                    in_multi_line_comment = True
                    col += 2
                    continue
            if c == '@' and col + 1 < len(line) and line[col+1] == '"':
                in_verbatim = True
                col += 2
                continue
            if c == '$' and col + 1 < len(line) and line[col+1] == '"':
                in_string = True
                col += 2
                continue
            if c == '"':
                in_string = True
                col += 1
                continue
            if c == "'":
                in_char = True
                col += 1
                continue

            if c == '{':
                depth += 1
                sig_lines = []
                for k in range(max(0, line_idx - 6), line_idx + 1):
                    sig_lines.append(lines[k].strip())
                sig_text = ' '.join(sig_lines).split('{')[0]

                is_type = re.search(r'\b(class|struct|interface|enum|namespace|record)\b', sig_text)
                is_flow = re.search(r'\b(if|else|for|foreach|while|do|switch|try|catch|finally|lock|using)\b\s*(\(|{|$)', sig_text)
                is_prop_acc = re.search(r'\b(get|set|init)\b\s*$', sig_text.strip())

                method_match = None
                if not is_type and not is_flow and not is_prop_acc:
                    m = re.search(r'([A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)', sig_text)
                    if m and m.group(2) not in ['if', 'while', 'for', 'foreach', 'switch', 'catch', 'using', 'lock']:
                        method_match = m.group(2)
                    else:
                        m2 = re.search(r'([A-Za-z0-9_]+)\s*\([^\)]*\)(\s*:\s*(base|this)\([^\)]*\))?', sig_text)
                        if m2 and m2.group(1) not in ['if', 'while', 'for', 'foreach', 'switch', 'catch', 'using', 'lock', 'typeof', 'sizeof', 'nameof']:
                            method_match = m2.group(1)

                if method_match:
                    start_l = line_idx + 1
                    for k in range(line_idx, max(-1, line_idx - 6), -1):
                        stripped = lines[k].strip()
                        if any(kw in stripped for kw in ['public', 'private', 'protected', 'internal', 'static', 'async', 'override', 'virtual', '[Fact]', '[Theory]', 'void', 'Task']):
                            start_l = k + 1
                            break
                    stack.append((method_match, start_l, depth))
                else:
                    stack.append((None, line_idx + 1, depth))
            elif c == '}':
                if stack:
                    name, start_l, open_d = stack.pop()
                    if name is not None and open_d == depth:
                        length = (line_idx + 1) - start_l + 1
                        methods.append((name, rel_path, start_l, line_idx + 1, length))
                depth -= 1
            col += 1

    return methods, len(lines)

print("\n--- 1. RULE C2 AUDIT (<= 300 lines) ---")
c2_violations = []
for f in m2_files:
    full_path = os.path.normpath(os.path.join(base_dir, f))
    with open(full_path, "r", encoding="utf-8") as fp:
        lines = fp.readlines()
    if len(lines) > 300:
        c2_violations.append((f, len(lines)))
    # spot check max line
print(f"Total files: {len(m2_files)}")
print(f"C2 violations: {len(c2_violations)}")
for f, l in c2_violations:
    print(f"  FAIL: {f} has {l} lines (> 300)")

print("\n--- 2. RULE M3 AUDIT (<= 50 lines) ---")
all_methods = []
for f in m2_files:
    m_list, _ = analyze_methods(f)
    all_methods.extend(m_list)

m3_violations = [m for m in all_methods if m[4] > 50]
print(f"Total methods analyzed: {len(all_methods)}")
print(f"M3 violations (> 50 lines): {len(m3_violations)}")
for m in sorted(all_methods, key=lambda x: x[4], reverse=True)[:10]:
    print(f"  {m[4]:2d} lines | {m[0]:35} | {os.path.basename(m[1])}:{m[2]}-{m[3]}")
for m in m3_violations:
    print(f"  FAIL: {m[0]} in {m[1]} lines {m[2]}-{m[3]} is {m[4]} lines")

print("\n--- 3. RULE Q-B1 (Dead Code) & Q-B2 (Unannotated Empty Catch) ---")
qb1_violations = []
qb2_violations = []
for f in m2_files:
    full_path = os.path.normpath(os.path.join(base_dir, f))
    with open(full_path, "r", encoding="utf-8") as fp:
        for idx, line in enumerate(fp):
            stripped = line.strip()
            # Check empty catch
            if re.search(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', stripped):
                qb2_violations.append((f, idx + 1, stripped))
            # Check commented out code
            if stripped.startswith('//') and not stripped.startswith('///'):
                content = stripped[2:].strip()
                if re.match(r'^(var\s+[a-zA-Z]|return\s+|if\s*\(|for\s*\(|foreach\s*\(|throw\s+new|new\s+[A-Z])', content):
                    qb1_violations.append((f, idx + 1, stripped))

print(f"Q-B1 (dead commented-out code) violations: {len(qb1_violations)}")
for f, idx, s in qb1_violations:
    print(f"  FAIL: {f}:{idx} -> {s}")

print(f"Q-B2 (empty catch) violations: {len(qb2_violations)}")
for f, idx, s in qb2_violations:
    print(f"  FAIL: {f}:{idx} -> {s}")

print("\n--- 4. ARCHITECTURAL ISOLATION & HOST NEUTRALITY ---")
core_host_violations = []
for f in m2_files:
    if "HPKata.Core" in f:
        full_path = os.path.normpath(os.path.join(base_dir, f))
        with open(full_path, "r", encoding="utf-8") as fp:
            for idx, line in enumerate(fp):
                if "Autodesk.AutoCAD" in line or "Autodesk.Revit" in line:
                    core_host_violations.append((f, idx + 1, line.strip()))

print(f"Core Host Neutrality violations: {len(core_host_violations)}")
for f, idx, s in core_host_violations:
    print(f"  FAIL: {f}:{idx} -> {s}")

print("\n--- 5. RULE R5 (304.8 ISOLATION) ---")
r5_violations = []
for f in m2_files:
    if "RevitUnitConverter.cs" not in f:
        full_path = os.path.normpath(os.path.join(base_dir, f))
        with open(full_path, "r", encoding="utf-8") as fp:
            for idx, line in enumerate(fp):
                if "304.8" in line:
                    r5_violations.append((f, idx + 1, line.strip()))

print(f"Rule R5 violations: {len(r5_violations)}")
for f, idx, s in r5_violations:
    print(f"  FAIL: {f}:{idx} -> {s}")

print("\n--- 6. INTEGRITY / STUBS / NOT IMPLEMENTED ---")
stub_violations = []
for f in m2_files:
    if "Tests" in f:
        continue
    full_path = os.path.normpath(os.path.join(base_dir, f))
    with open(full_path, "r", encoding="utf-8") as fp:
        for idx, line in enumerate(fp):
            if "NotImplementedException" in line or "TODO" in line or "STUB" in line:
                stub_violations.append((f, idx + 1, line.strip()))

print(f"Stub / NotImplemented violations: {len(stub_violations)}")
for f, idx, s in stub_violations:
    print(f"  WARN: {f}:{idx} -> {s}")
