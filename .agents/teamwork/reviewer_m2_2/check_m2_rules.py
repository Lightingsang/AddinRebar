import os
import re

files = [
    r"HPGrasshopper/HPGrasshopper.McpBridge/Execution/ICanvasUndoScope.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasRollbackResult.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge/Execution/RemovedComponentSnapshot.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs",
    r"HPGrasshopper/HPGrasshopper.McpBridge.Tests/Execution/Milestone2CommitOrderingAndRollbackLedgerTests.cs"
]

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def analyze_methods(filepath):
    full_path = os.path.normpath(os.path.join(base_dir, filepath))
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
                        methods.append((name, filepath, start_l, line_idx + 1, length))
                depth -= 1
            col += 1

    return methods, len(lines)

def check_empty_catch_and_dead_code(filepath):
    full_path = os.path.normpath(os.path.join(base_dir, filepath))
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    empty_catches = []
    dead_code = []

    for idx, line in enumerate(lines):
        # Empty catch check
        if re.search(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', line):
            empty_catches.append((idx + 1, line.strip()))
        
        # Dead commented code heuristics
        stripped = line.strip()
        if stripped.startswith('//') and not stripped.startswith('///'):
            content = stripped[2:].strip()
            # If line starts with C# keywords or statements
            if re.match(r'^(var|return|if\s*\(|for\s*\(|foreach\s*\(|public|private|throw|new\s+[A-Z]|await|Console\.)', content):
                dead_code.append((idx + 1, stripped))

    return empty_catches, dead_code

print("=== RULE C2 LINE COUNT AUDIT (Max <= 300) ===")
for f in files:
    full_path = os.path.normpath(os.path.join(base_dir, f))
    with open(full_path, "r", encoding="utf-8") as fp:
        count = len(fp.readlines())
    status = "PASS" if count <= 300 else "FAIL"
    print(f"[{status}] {count:3d} lines | {f}")

print("\n=== RULE M3 METHOD LENGTH AUDIT (Max <= 50) ===")
all_methods = []
for f in files:
    m_list, _ = analyze_methods(f)
    all_methods.extend(m_list)

m3_violations = [m for m in all_methods if m[4] > 50]
print(f"Total methods analyzed: {len(all_methods)}")
print(f"Methods exceeding 50 lines: {len(m3_violations)}")
for m in sorted(all_methods, key=lambda x: x[4], reverse=True)[:10]:
    print(f"  {m[4]:2d} lines | {m[0]:35} | {os.path.basename(m[1])}:{m[2]}-{m[3]}")

if m3_violations:
    print("M3 VIOLATIONS:")
    for m in m3_violations:
        print(f"  FAIL: {m[0]} in {m[1]} lines {m[2]}-{m[3]} is {m[4]} lines")
else:
    print("PASS: Zero methods exceed 50 lines.")

print("\n=== Q-B1 (DEAD CODE) & Q-B2 (EMPTY CATCH) AUDIT ===")
total_ec = 0
total_dc = 0
for f in files:
    ec, dc = check_empty_catch_and_dead_code(f)
    if ec:
        print(f"Empty catch in {f}: {ec}")
        total_ec += len(ec)
    if dc:
        print(f"Commented code in {f}: {dc}")
        total_dc += len(dc)

print(f"Total empty unannotated catches (Q-B2): {total_ec}")
print(f"Total dead commented-out code (Q-B1): {total_dc}")
