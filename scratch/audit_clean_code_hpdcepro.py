import os
import re
from pathlib import Path

def get_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        # Ignore bin, obj, .git
        if any(part in ('bin', 'obj', '.git', '.vs') for part in Path(root).parts):
            continue
        for f in files:
            if f.endswith('.cs') and not f.endswith('.g.cs') and not f.endswith('.Designer.cs') and not f.endswith('AssemblyInfo.cs'):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_file_lengths(cs_files):
    print("=" * 60)
    print("CHECK 3.1: FILE LENGTH <= 300 LINES")
    print("=" * 60)
    violations = []
    for f in cs_files:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            lines = fp.readlines()
            count = len(lines)
            if count > 300:
                violations.append((f, count))
    
    if violations:
        print(f"[FAIL] {len(violations)} files exceed 300 lines:")
        for f, c in violations:
            print(f"  - {f}: {c} lines")
        return False, violations
    else:
        print(f"[PASS] All {len(cs_files)} C# files <= 300 lines.")
        return True, []

def check_method_lengths(cs_files):
    print("=" * 60)
    print("CHECK 3.2: METHOD LENGTH <= 50 LINES")
    print("=" * 60)
    violations = []
    
    # Simple brace-depth parser to detect method bodies
    # A method starts after a signature line and open brace
    for f in cs_files:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            lines = fp.readlines()
            
        in_method = False
        method_name = ""
        method_start = 0
        brace_level = 0
        method_brace_level = 0
        
        # We can track blocks inside classes (brace_level >= 2 usually)
        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            
            # Simple heuristic for method declaration if not in method
            # Check for patterns like 'public ... (...)', 'private ... (...)', 'internal ... (...)', 'protected ... (...)'
            # But not class/struct/enum/interface/record
            if not in_method and brace_level >= 1:
                # ignore property get/set, attributes, control flow
                if any(stripped.startswith(mod) for mod in ['public ', 'private ', 'internal ', 'protected ', 'static ', 'async ']):
                    if '(' in stripped and not any(kw in stripped for kw in ['class ', 'struct ', 'interface ', 'record ', 'enum ', 'delegate ']):
                        # likely method or constructor
                        method_name = stripped
                        method_start = idx
            
            # Count braces
            # Remove string literals and char literals for brace counting
            cleaned_line = re.sub(r'"(\\.|[^"\\])*"', '""', line)
            cleaned_line = re.sub(r"'(\\.|[^'\\])*'", "''", cleaned_line)
            cleaned_line = re.sub(r'//.*', '', cleaned_line)
            
            open_count = cleaned_line.count('{')
            close_count = cleaned_line.count('}')
            
            if open_count > 0 or close_count > 0:
                if not in_method and method_name and open_count > 0:
                    in_method = True
                    method_brace_level = brace_level + 1
                
                brace_level += open_count - close_count
                
                if in_method and brace_level < method_brace_level:
                    # method finished
                    length = idx - method_start + 1
                    if length > 50:
                        violations.append((f, method_name[:40], method_start, idx, length))
                    in_method = False
                    method_name = ""
                    
    if violations:
        print(f"[FAIL] Found {len(violations)} methods exceeding 50 lines:")
        for f, m, s, e, l in violations:
            print(f"  - {Path(f).name}:{s}-{e} ({l} lines): {m}")
        return False, violations
    else:
        print("[PASS] All detected methods <= 50 lines.")
        return True, []

def check_commented_code(cs_files):
    print("=" * 60)
    print("CHECK 3.3: COMMENTED-OUT CODE BLOCKS")
    print("=" * 60)
    violations = []
    
    # Patterns indicating commented-out C# statements
    code_pattern = re.compile(r'^\s*//\s*(var|int|string|double|bool|if|for|foreach|while|return|throw|using|new|Assert|Console)\s+.*[;{}]', re.IGNORECASE)
    multi_line_comment_code = re.compile(r'/\*.*?(var|int|double|return|Assert).*?\*/', re.DOTALL)
    
    for f in cs_files:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            content = fp.read()
            lines = content.splitlines()
            
        for idx, line in enumerate(lines, 1):
            if code_pattern.search(line):
                # Exclude ordinary comments that happen to mention words
                stripped = line.strip()
                if not stripped.startswith('///') and not 'e.g.' in stripped and not 'example' in stripped.lower():
                    violations.append((f, idx, stripped))
                    
    if violations:
        print(f"[FAIL] Found {len(violations)} potential commented-out code lines:")
        for f, l, s in violations[:10]:
            print(f"  - {Path(f).name}:{l}: {s}")
        if len(violations) > 10:
            print(f"  ... and {len(violations) - 10} more.")
        return False, violations
    else:
        print("[PASS] Zero commented-out code lines found.")
        return True, []

def check_catch_blocks(cs_files):
    print("=" * 60)
    print("CHECK 3.4: CATCH BLOCK REASON COMMENTS")
    print("=" * 60)
    violations = []
    
    catch_pattern = re.compile(r'catch\s*(\([^\)]*\))?\s*\{', re.MULTILINE)
    
    for f in cs_files:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            content = fp.read()
            lines = content.splitlines()
            
        for idx, line in enumerate(lines, 1):
            if 'catch' in line:
                match = re.search(r'catch\s*(\([^\)]*\))?\s*\{?', line)
                if match:
                    # Check next 5 lines for a comment with "reason" or "//"
                    # The requirement: "Check that EVERY catch block contains a `// Reason: ...` comment explaining why the exception is handled."
                    chunk = "\n".join(lines[idx-1 : min(len(lines), idx+10)])
                    # Look inside catch block
                    if not re.search(r'//\s*(Reason|reason)', chunk):
                        violations.append((f, idx, line.strip()))
                        
    if violations:
        print(f"[FAIL] Found {len(violations)} catch blocks missing '// Reason: ...' comment:")
        for f, l, s in violations:
            print(f"  - {Path(f).name}:{l}: {s}")
        return False, violations
    else:
        print("[PASS] All catch blocks have '// Reason: ...' comments.")
        return True, []

def main():
    base_dir = "HPDcePro"
    cs_files = get_cs_files(base_dir)
    print(f"Found {len(cs_files)} C# files in {base_dir}")
    
    ok1, v1 = check_file_lengths(cs_files)
    ok2, v2 = check_method_lengths(cs_files)
    ok3, v3 = check_commented_code(cs_files)
    ok4, v4 = check_catch_blocks(cs_files)
    
    print("\nSummary Clean Code:")
    print(f"Files <= 300 lines: {'PASS' if ok1 else 'FAIL'}")
    print(f"Methods <= 50 lines: {'PASS' if ok2 else 'FAIL'}")
    print(f"No commented code: {'PASS' if ok3 else 'FAIL'}")
    print(f"Catch block reason comments: {'PASS' if ok4 else 'FAIL'}")

if __name__ == '__main__':
    main()
