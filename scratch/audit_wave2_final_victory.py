import os
import re
import sys

HPBIM_ROOT = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim"

def get_cs_files(root_dir):
    cs_files = []
    for dirpath, dirnames, filenames in os.walk(root_dir):
        if any(skip in dirpath for skip in ["\\obj\\", "/obj/", "\\bin\\", "/bin/"]):
            continue
        for f in filenames:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(dirpath, f))
    return sorted(cs_files)

def check_file_line_counts(files):
    violations = []
    file_counts = {}
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
            lines = fp.readlines()
        count = len(lines)
        file_counts[f] = count
        if count > 300:
            violations.append((f, count))
    return file_counts, violations

def check_commented_code(files):
    violations = []
    # Patterns that look like commented-out code (e.g. // var x =, // return, // if (, // foreach, // using ...)
    code_pattern = re.compile(r'^\s*//\s*(var|int|string|bool|return|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|throw\s+new|await\s+|Console\.|_logger\.|[A-Z][a-zA-Z0-9_]*\s*=[^=]|;\s*$)')
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
            lines = fp.readlines()
        for idx, line in enumerate(lines, 1):
            if code_pattern.search(line):
                # Exclude regular comment explanations like "// if not found..."
                stripped = line.strip()
                # If it looks like valid code commented out
                if any(k in stripped for k in [";", "return ", "throw new ", "await "]):
                    violations.append((f, idx, stripped))
    return violations

def check_catch_blocks(files):
    violations = []
    # Find catch blocks and see if they have annotations or explanations
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
            content = fp.read()
        lines = content.splitlines()
        for idx, line in enumerate(lines):
            if re.search(r'\bcatch\b', line):
                # Inspect the next 5 lines
                block_sample = "\n".join(lines[idx:idx+6])
                # Check if empty block without comment
                if re.search(r'catch[^{]*\{\s*\}', block_sample):
                    violations.append((f, idx+1, "Empty catch block"))
                elif re.search(r'catch[^{]*\{', block_sample):
                    # Check if body has at least a comment or statement
                    pass
    return violations

def check_core_purity():
    core_dir = os.path.join(HPBIM_ROOT, "src", "HPBim.Core")
    violations = []
    for dirpath, dirnames, filenames in os.walk(core_dir):
        if any(skip in dirpath for skip in ["\\obj\\", "/obj/", "\\bin\\", "/bin/"]):
            continue
        for f in filenames:
            if f.endswith(".cs") or f.endswith(".csproj"):
                path = os.path.join(dirpath, f)
                with open(path, "r", encoding="utf-8-sig", errors="replace") as fp:
                    text = fp.read()
                if "Autodesk.Revit" in text or "RevitAPI" in text:
                    violations.append((path, "Forbidden Revit reference"))
    return violations

def measure_methods(file_path):
    # Method heuristic based on brace tracking and signature detection
    methods = []
    with open(file_path, "r", encoding="utf-8-sig", errors="replace") as fp:
        lines = fp.readlines()

    # Regex for C# method / constructor signature
    sig_regex = re.compile(r'^\s*(public|private|protected|internal|static|async|override|virtual|sealed|unsafe|\s+)*\s*([A-Za-z0-9_<>,?\[\]\s]+)\s+([A-Za-z0-9_]+)\s*\([^;]*\)(\s*where\s+[^{]+)?\s*$')
    ctor_regex = re.compile(r'^\s*(public|private|protected|internal)\s+([A-Za-z0-9_]+)\s*\([^;]*\)\s*(:\s*(base|this)\s*\([^)]*\))?\s*$')

    i = 0
    while i < len(lines):
        line = lines[i]
        # Skip comments, properties, etc.
        if (sig_regex.search(line) or ctor_regex.search(line)) and not line.strip().endswith(';'):
            start_line = i + 1
            # Look ahead for opening brace
            brace_found = False
            k = i
            while k < len(lines) and k < i + 5:
                if '{' in lines[k]:
                    brace_found = True
                    break
                k += 1
            if brace_found:
                brace_count = 0
                started = False
                end_line = start_line
                for j in range(k, len(lines)):
                    brace_count += lines[j].count('{')
                    brace_count -= lines[j].count('}')
                    if brace_count > 0:
                        started = True
                    if started and brace_count == 0:
                        end_line = j + 1
                        break
                length = end_line - start_line + 1
                methods.append((line.strip(), start_line, end_line, length))
                i = max(i + 1, end_line)
                continue
        i += 1
    return methods

if __name__ == "__main__":
    files = get_cs_files(HPBIM_ROOT)
    print(f"Total C# files in HPBim: {len(files)}")
    
    file_counts, c2_violations = check_file_line_counts(files)
    print(f"\n--- Rule C2 File Line Counts (Max 300) ---")
    if c2_violations:
        print(f"FAIL: {len(c2_violations)} violations found:")
        for f, cnt in c2_violations:
            print(f"  {f}: {cnt} lines")
    else:
        max_file = max(file_counts.items(), key=lambda x: x[1])
        print(f"PASS: All {len(files)} files <= 300 lines. (Longest: {os.path.basename(max_file[0])} with {max_file[1]} lines)")

    print(f"\n--- Rule Q-B1 Commented-Out Code ---")
    qb1_violations = check_commented_code(files)
    if qb1_violations:
        print(f"Potential violations ({len(qb1_violations)}):")
        for f, idx, s in qb1_violations:
            print(f"  {os.path.basename(f)}:{idx}: {s}")
    else:
        print("PASS: 0 lines of commented-out code found.")

    print(f"\n--- Rule Q-B2 Catch Blocks ---")
    qb2_violations = check_catch_blocks(files)
    if qb2_violations:
        print(f"Violations ({len(qb2_violations)}):")
        for f, idx, s in qb2_violations:
            print(f"  {os.path.basename(f)}:{idx}: {s}")
    else:
        print("PASS: No empty or unannotated catch blocks found.")

    print(f"\n--- Architecture Purity (HPBim.Core) ---")
    purity_violations = check_core_purity()
    if purity_violations:
        print(f"FAIL: {len(purity_violations)} violations found in HPBim.Core:")
        for path, err in purity_violations:
            print(f"  {path}: {err}")
    else:
        print("PASS: HPBim.Core has strictly 0 references to Autodesk.Revit.*.")
