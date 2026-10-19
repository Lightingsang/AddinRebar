import os
import re
import sys

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def find_all_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        if "bin" in dirs: dirs.remove("bin")
        if "obj" in dirs: dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_file_length(cs_files):
    violations = []
    for path in cs_files:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
            if len(lines) > 300:
                rel = os.path.relpath(path, base_dir)
                violations.append((rel, len(lines)))
    return violations

def check_methods(cs_files):
    long_methods = []
    # Pattern to match method declarations (including constructors and properties with get/set blocks)
    method_sig_pattern = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|async|override|sealed|virtual|new|unsafe|\s)+'
        r'[\w\<\>\[\],\?\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+[^{]+)?$'
    )
    ctor_pattern = re.compile(
        r'^\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|\s)+'
        r'([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?::\s*(?:base|this)\s*\([^\)]*\))?\s*$'
    )

    for path in cs_files:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
        rel = os.path.relpath(path, base_dir)

        i = 0
        while i < len(lines):
            line_str = lines[i].strip()
            m = method_sig_pattern.match(lines[i]) or ctor_pattern.match(lines[i])
            if m and not line_str.endswith(';') and not line_str.startswith('//'):
                method_name = m.group(1)
                start_line = i + 1
                brace_count = 0
                found_open = False
                j = i
                while j < len(lines):
                    for char in lines[j]:
                        if char == '{':
                            brace_count += 1
                            found_open = True
                        elif char == '}':
                            brace_count -= 1
                    if found_open and brace_count == 0:
                        end_line = j + 1
                        method_len = end_line - start_line + 1
                        if method_len > 50:
                            long_methods.append((rel, method_name, start_line, end_line, method_len))
                        i = j
                        break
                    j += 1
            i += 1
    return long_methods

def check_dead_code(cs_files):
    dead_code = []
    commented_code_pattern = re.compile(
        r'^\s*//\s*(?:var\s+\w+\s*=|return\s|if\s*\(|while\s*\(|for\s*\(|throw\s+new|Assert\.|throw\s|using\s*\(\w+|\w+\.\w+\(.*\);)\s*$'
    )
    for path in cs_files:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
        rel = os.path.relpath(path, base_dir)
        for idx, line in enumerate(lines):
            stripped = line.strip()
            if stripped.startswith('///'):
                continue
            # Allow explanatory comments or disabled test markers if annotated
            if commented_code_pattern.match(line):
                dead_code.append((rel, idx + 1, stripped))
    return dead_code

def check_unannotated_catch(cs_files):
    unannotated = []
    for path in cs_files:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
        rel = os.path.relpath(path, base_dir)
        for match in re.finditer(r'catch(?:\s*\([^\)]*\))?\s*\{', content):
            start_idx = match.start()
            brace_count = 1
            pos = match.end()
            while pos < len(content) and brace_count > 0:
                if content[pos] == '{':
                    brace_count += 1
                elif content[pos] == '}':
                    brace_count -= 1
                pos += 1
            catch_body = content[match.end():pos-1].strip()
            line_num = content[:start_idx].count('\n') + 1
            if len(catch_body) == 0:
                unannotated.append((rel, line_num, "EMPTY CATCH WITH NO BODY OR REASON COMMENT"))
            elif len(catch_body.splitlines()) == 1 and not any(k in catch_body for k in ['throw', 'return', 'Log', 'log', '//', '/*']):
                unannotated.append((rel, line_num, f"POTENTIAL SILENT CATCH: {catch_body}"))
    return unannotated

def main():
    cs_files = find_all_cs_files()
    print(f"Total C# files found in HPKata: {len(cs_files)}")

    print("\n--- CHECK 1: Rule C2 (File Length <= 300) ---")
    c2_violations = check_file_length(cs_files)
    print(f"Violations: {len(c2_violations)}")
    for rel, count in c2_violations:
        print(f"  {rel}: {count} lines")

    print("\n--- CHECK 2: Rule M3 (Method Length <= 50) ---")
    m3_violations = check_methods(cs_files)
    print(f"Violations: {len(m3_violations)}")
    for rel, name, s, e, l in m3_violations:
        print(f"  {rel} -> {name}: lines {s}-{e} ({l} lines)")

    print("\n--- CHECK 3: Rule Q-B1 (Commented-out code) ---")
    qb1_violations = check_dead_code(cs_files)
    print(f"Violations: {len(qb1_violations)}")
    for rel, l, text in qb1_violations:
        print(f"  {rel}:{l}: {text}")

    print("\n--- CHECK 4: Rule Q-B2 (Unannotated Catch Blocks) ---")
    qb2_violations = check_unannotated_catch(cs_files)
    print(f"Violations: {len(qb2_violations)}")
    for rel, l, text in qb2_violations:
        print(f"  {rel}:{l}: {text}")

if __name__ == "__main__":
    main()
