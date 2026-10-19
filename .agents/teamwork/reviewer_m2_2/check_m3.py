import os
import re

core_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core"

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(core_dir):
        if "obj" in root or "bin" in root:
            continue
        for file in files:
            if file.endswith(".cs"):
                cs_files.append(os.path.join(root, file))
    return sorted(cs_files)

def analyze_methods(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
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
                # Check what opened this block
                sig_lines = []
                for k in range(max(0, line_idx - 6), line_idx + 1):
                    sig_lines.append(lines[k].strip())
                sig_text = ' '.join(sig_lines).split('{')[0]

                is_type = re.search(r'\b(class|struct|interface|enum|namespace|record)\b', sig_text)
                is_flow = re.search(r'\b(if|else|for|foreach|while|do|switch|try|catch|finally|lock|using)\b\s*(\(|{|$)', sig_text)
                is_prop_acc = re.search(r'\b(get|set|init)\b\s*$', sig_text.strip())

                is_method = False
                method_name = "unknown"
                if not is_type and not is_flow and not is_prop_acc:
                    # heuristic for method: has parentheses or constructor
                    m = re.search(r'([A-Za-z0-9_<>]+)\s*\([^)]*\)\s*$', sig_text.strip())
                    if m:
                        is_method = True
                        method_name = m.group(1)

                stack.append((depth, line_idx + 1, is_method, method_name, sig_text))
            elif c == '}':
                if stack:
                    start_depth, start_line, is_method, m_name, sig = stack.pop()
                    if is_method:
                        end_line = line_idx + 1
                        length = end_line - start_line + 1
                        methods.append((m_name, start_line, end_line, length, sig))
                depth = max(0, depth - 1)
            col += 1

    return methods

def main():
    files = get_cs_files()
    violations = []
    total_methods = 0
    max_method = (None, 0)
    for f in files:
        rel = os.path.relpath(f, core_dir)
        meths = analyze_methods(f)
        for name, start, end, length, sig in meths:
            total_methods += 1
            if length > max_method[1]:
                max_method = (f"{rel}:{name} (lines {start}-{end})", length)
            if length > 50:
                violations.append((rel, name, start, end, length, sig))

    print(f"Total methods analyzed: {total_methods}")
    print(f"Max method length: {max_method[0]} -> {max_method[1]} lines")
    if violations:
        print(f"\nRule M3 VIOLATIONS (> 50 lines): {len(violations)}")
        for rel, name, start, end, length, sig in violations:
            print(f"- {rel}:{start}-{end} [{name}] -> {length} lines\n  Sig: {sig[:80]}")
    else:
        print("PASS: Rule M3 (All methods <= 50 lines).")

if __name__ == "__main__":
    main()
