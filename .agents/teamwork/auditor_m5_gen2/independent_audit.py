import os
import re
import sys

HPBIM_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim"

def get_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        # ignore bin and obj
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_file_lengths(files):
    violations = []
    over_200_prod = []
    for f in files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        line_count = len(lines)
        if line_count > 300:
            violations.append((f, line_count))
        elif line_count > 200 and "\\src\\" in f:
            over_200_prod.append((f, line_count))
    return violations, over_200_prod

def check_empty_catches(files):
    violations = []
    # Q-B2: empty catch block without comment
    # Look for catch (...) { } or catch { }
    pattern = re.compile(r'catch\s*(\([^\)]*\))?\s*\{\s*\}', re.MULTILINE)
    for f in files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            content = fp.read()
        for match in pattern.finditer(content):
            violations.append((f, match.group(0)))
    return violations

def check_commented_code(files):
    violations = []
    # Look for single-line comments that look like commented-out code
    code_comment_patterns = [
        re.compile(r'^\s*//\s*(var|int|string|bool|double|float|return|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|switch\s*\(|public|private|protected|internal|class|struct|interface|void)\b.*[;{}]\s*$'),
        re.compile(r'^\s*//\s*[a-zA-Z0-9_]+\.[a-zA-Z0-9_]+\(.*\);\s*$'),
        re.compile(r'^\s*//\s*throw\s+new\b.*;\s*$')
    ]
    for f in files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            for i, line in enumerate(fp, start=1):
                # ignore documentation comments ///
                if line.strip().startswith("///"):
                    continue
                # ignore comments explaining rules or todo
                for p in code_comment_patterns:
                    if p.match(line):
                        # check if it's an example in a comment
                        if "e.g." in line or "example" in line.lower() or "note" in line.lower():
                            continue
                        violations.append((f, i, line.strip()))
    return violations

def check_method_lengths(src_files):
    # Heuristic method boundary finder based on braces
    violations = []
    warnings = []
    for f in src_files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()

        i = 0
        while i < len(lines):
            line = lines[i].strip()
            # Look for method declaration
            # typically: access_modifier [static/async/virtual/override] return_type MethodName(...)
            # ignore class, struct, record, interface, enum declarations
            is_method_header = False
            if re.match(r'^(public|private|protected|internal)\s+(static\s+|async\s+|virtual\s+|override\s+|sealed\s+)*[a-zA-Z0-9_<>, ?\[\]]+\s+[A-Za-z0-9_]+\s*\(', line):
                if not re.search(r'\b(class|struct|record|interface|enum|delegate)\b', line):
                    is_method_header = True

            if is_method_header:
                start_line = i + 1
                method_name = line
                # find opening brace
                brace_depth = 0
                opened = False
                j = i
                while j < len(lines):
                    for char in lines[j]:
                        if char == '{':
                            brace_depth += 1
                            opened = True
                        elif char == '}':
                            brace_depth -= 1
                    if opened and brace_depth == 0:
                        break
                    j += 1
                if opened and brace_depth == 0:
                    method_len = (j - start_line) + 1
                    if method_len > 50:
                        violations.append((f, start_line, method_len, method_name[:60]))
                    elif method_len > 40:
                        warnings.append((f, start_line, method_len, method_name[:60]))
                    i = j
            i += 1
    return violations, warnings

def check_facades_and_dummies(files):
    # Facade detection: methods with only "return <constant>" or "throw new NotImplementedException"
    facades = []
    pattern_not_impl = re.compile(r'throw\s+new\s+NotImplementedException\s*\(', re.MULTILINE)
    for f in files:
        if "\\tests\\" in f:
            continue
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            content = fp.read()
        if pattern_not_impl.search(content):
            facades.append((f, "NotImplementedException"))
    return facades

if __name__ == "__main__":
    files = get_cs_files(HPBIM_DIR)
    print(f"Total C# files found in HPBim: {len(files)}")
    src_files = [f for f in files if "\\src\\" in f]
    test_files = [f for f in files if "\\tests\\" in f]
    print(f"  src files: {len(src_files)}")
    print(f"  test files: {len(test_files)}")

    len_violations, over_200_prod = check_file_lengths(files)
    print(f"\n1. File length > 300 lines violations: {len(len_violations)}")
    for v in len_violations:
        print(f"  VIOLATION: {v[0]} ({v[1]} lines)")
    print(f"Production classes > 200 lines (triggers): {len(over_200_prod)}")
    for v in over_200_prod:
        print(f"  TRIGGER: {v[0]} ({v[1]} lines)")

    catches = check_empty_catches(files)
    print(f"\n2. Empty catches violations (Q-B2): {len(catches)}")
    for c in catches:
        print(f"  VIOLATION: {c[0]} -> {c[1]}")

    commented = check_commented_code(files)
    print(f"\n3. Commented-out code violations (Q-B1): {len(commented)}")
    for c in commented:
        print(f"  VIOLATION: {c[0]}:{c[1]} -> {c[2]}")

    method_violations, method_warnings = check_method_lengths(src_files)
    print(f"\n4. Method length > 50 lines in src/ violations (Q-B3): {len(method_violations)}")
    for m in method_violations:
        print(f"  VIOLATION: {m[0]}:{m[1]} ({m[2]} lines) -> {m[3]}")
    print(f"Method length 41-50 lines in src/ (triggers): {len(method_warnings)}")
    for m in method_warnings:
        print(f"  TRIGGER: {m[0]}:{m[1]} ({m[2]} lines) -> {m[3]}")

    facades = check_facades_and_dummies(files)
    print(f"\n5. Facades / NotImplementedException in src/: {len(facades)}")
    for f in facades:
        print(f"  VIOLATION: {f[0]} -> {f[1]}")
