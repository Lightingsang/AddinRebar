import os
import re
import sys

HPBIM_DIR = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim"

def find_cs_files(root):
    files = []
    for dp, _, fns in os.walk(root):
        if any(x in dp for x in ["\\bin\\", "/bin/", "\\obj\\", "/obj/"]):
            continue
        for fn in fns:
            if fn.endswith(".cs"):
                files.append(os.path.join(dp, fn))
    return sorted(files)

def check_file_lengths(files):
    long_files = []
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
            n = len(fp.readlines())
        if n > 300:
            long_files.append((f, n))
    return long_files

def check_commented_code(files):
    # Rule Q-B1: zero commented-out code
    # We look for lines starting with // that look like code statements
    code_patterns = [
        re.compile(r'^\s*//\s*(var|int|string|bool|double|float|long|byte|void)\s+[A-Za-z0-9_]+\s*='),
        re.compile(r'^\s*//\s*(return|throw\s+new|await)\s+'),
        re.compile(r'^\s*//\s*(if|for|foreach|while|switch)\s*\('),
        re.compile(r'^\s*//\s*[A-Za-z0-9_.]+\s*\([^)]*\)\s*;'),
        re.compile(r'^\s*//\s*[A-Za-z0-9_.]+\s*=\s*[^;]+;'),
    ]
    violations = []
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
            lines = fp.readlines()
        for idx, line in enumerate(lines, 1):
            s = line.strip()
            # Exclude lines that are clearly doc comments or explanations
            if s.startswith("///"):
                continue
            for pat in code_patterns:
                if pat.search(line):
                    # Filter out natural language explanations that happen to contain keywords
                    if ";" in s or "return " in s or "throw " in s or "=>" in s:
                        violations.append((f, idx, s))
                        break
    return violations

def check_catch_blocks(files):
    # Rule Q-B2: zero unannotated catch blocks
    violations = []
    for f in files:
        with open(f, "r", encoding="utf-8-sig", errors="replace") as fp:
            text = fp.read()
        lines = text.splitlines()
        for idx, line in enumerate(lines):
            m = re.search(r'\bcatch\b(?:\s*\([^)]*\))?\s*\{?', line)
            if m:
                # Find the catch block opening and body
                k = idx
                found_open = False
                block_lines = []
                brace_depth = 0
                while k < len(lines):
                    l = lines[k]
                    for char in l:
                        if char == '{':
                            found_open = True
                            brace_depth += 1
                        elif char == '}':
                            brace_depth -= 1
                    if found_open:
                        block_lines.append(l)
                        if brace_depth == 0:
                            break
                    k += 1
                
                # Check block_lines
                block_content = "\n".join(block_lines)
                has_comment = "//" in block_content or "/*" in block_content
                # If block is empty or just rethrows or swallows without comment
                # Clean code rule Q-B2: every catch block MUST have a comment explaining why
                if not has_comment:
                    violations.append((f, idx + 1, line.strip()))
    return violations

def check_core_purity():
    core_path = os.path.join(HPBIM_DIR, "src", "HPBim.Core")
    violations = []
    for dp, _, fns in os.walk(core_path):
        if any(x in dp for x in ["\\bin\\", "/bin/", "\\obj\\", "/obj/"]):
            continue
        for fn in fns:
            p = os.path.join(dp, fn)
            with open(p, "r", encoding="utf-8-sig", errors="replace") as fp:
                txt = fp.read()
            if "Autodesk.Revit" in txt or "RevitAPI" in txt:
                violations.append(p)
    return violations

def main():
    files = find_cs_files(HPBIM_DIR)
    print(f"Total C# files found in HPBim: {len(files)}")
    
    long_files = check_file_lengths(files)
    print(f"Files > 300 lines: {len(long_files)}")
    for f, n in long_files:
        print(f"  VIOLATION C2: {f} ({n} lines)")
        
    commented = check_commented_code(files)
    print(f"Potential commented code lines: {len(commented)}")
    for f, idx, line in commented:
        print(f"  VIOLATION Q-B1: {f}:{idx} -> {line}")
        
    unannotated_catches = check_catch_blocks(files)
    print(f"Unannotated catch blocks: {len(unannotated_catches)}")
    for f, idx, line in unannotated_catches:
        print(f"  VIOLATION Q-B2: {f}:{idx} -> {line}")
        
    core_viols = check_core_purity()
    print(f"HPBim.Core purity violations: {len(core_viols)}")
    for v in core_viols:
        print(f"  VIOLATION PURITY: {v}")

if __name__ == "__main__":
    main()
