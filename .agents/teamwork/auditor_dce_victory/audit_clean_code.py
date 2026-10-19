import os
import re

HPDCEPRO_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro"

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(HPDCEPRO_DIR):
        if any(ignored in root.split(os.sep) for ignored in ["bin", "obj", ".vs", ".git"]):
            continue
        for f in files:
            if f.endswith(".cs") and not f.endswith(".g.cs") and not f.endswith(".AssemblyAttributes.cs"):
                cs_files.append(os.path.join(root, f))
    return sorted(cs_files)

def check_file_lengths(cs_files):
    violations = []
    for path in cs_files:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
        count = len(lines)
        if count > 300:
            rel = os.path.relpath(path, HPDCEPRO_DIR)
            violations.append((rel, count))
    return violations

def check_method_lengths(cs_files):
    # Method heuristic: tracks braces inside methods
    # We look for method signatures, then count lines between open brace and closing brace
    method_sig_pattern = re.compile(
        r'^\s*(?:public|private|protected|internal|static|async|virtual|override|sealed|new|\s)+'
        r'(?:[\w\<\>\[\],\s\?\.]+\s+)'
        r'([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
    )
    violations = []
    for path in cs_files:
        rel = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
        
        in_method = False
        method_name = ""
        method_start = 0
        brace_depth = 0
        method_brace_level = 0
        
        for idx, line in enumerate(lines):
            line_str = line.strip()
            # Check if this line looks like a method signature
            if not in_method and ("(" in line_str and ")" in line_str and not line_str.startswith("//") and not line_str.startswith("*") and not line_str.endswith(";")):
                if any(k in line_str for k in ["class ", "interface ", "struct ", "record ", "namespace ", "enum ", "delegate "]):
                    pass
                else:
                    m = method_sig_pattern.match(line.rstrip())
                    if m:
                        method_name = m.group(1)
                        method_start = idx + 1
                        in_method = True
                        method_brace_level = brace_depth
            
            # Count braces (ignoring simple string literals or comments)
            # Remove string literals and char literals for brace counting
            sanitized = re.sub(r'"(\\.|[^"\\])*"', '', line_str)
            sanitized = re.sub(r"'(\\.|[^'\\])*'", '', sanitized)
            sanitized = re.sub(r'//.*$', '', sanitized)
            
            open_count = sanitized.count('{')
            close_count = sanitized.count('}')
            
            if in_method:
                brace_depth += open_count - close_count
                if brace_depth <= method_brace_level and ('}' in sanitized):
                    length = (idx + 1) - method_start + 1
                    if length > 50:
                        violations.append((rel, method_name, method_start, idx + 1, length))
                    in_method = False
            else:
                brace_depth += open_count - close_count
    return violations

def check_commented_code(cs_files):
    # Detect commented-out code (lines starting with // that parse as C# statements like var, return, if, for, ;, etc.)
    code_indicators = [
        re.compile(r'^\s*//\s*(?:var|int|string|double|bool|if\s*\(|for\s*\(|foreach\s*\(|while\s*\(|return\s+|throw\s+new|public\s+|private\s+|protected\s+).*;\s*$'),
        re.compile(r'^\s*//\s*[A-Za-z0-9_]+\s*=\s*.*;\s*$'),
        re.compile(r'^\s*//\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\(.*\);\s*$'),
        re.compile(r'^\s*//\s*(?:using\s+[A-Za-z0-9_\.]+;)\s*$')
    ]
    violations = []
    for path in cs_files:
        rel = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            for idx, line in enumerate(f, start=1):
                raw = line.strip()
                if raw.startswith("//"):
                    # Check if it's documentation or comment explaining something
                    # If it matches code indicators
                    for pattern in code_indicators:
                        if pattern.match(line):
                            # Ensure it's not a doc explanation or formula example
                            violations.append((rel, idx, raw))
                            break
    return violations

def check_catch_blocks(cs_files):
    # Detect catch blocks that don't have // Reason:
    violations = []
    for path in cs_files:
        rel = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
        
        # Find all catch blocks
        # e.g. catch (...) { ... } or catch { ... }
        catch_pattern = re.compile(r'catch\s*(?:\([^\)]*\))?\s*\{([^}]*)\}', re.DOTALL)
        for m in catch_pattern.finditer(content):
            body = m.group(1)
            # Check if body has "// Reason:"
            if "// Reason:" not in body:
                # Find line number
                start_pos = m.start()
                line_no = content[:start_pos].count('\n') + 1
                violations.append((rel, line_no, m.group(0).split('\n')[0]))
    return violations

if __name__ == "__main__":
    files = get_cs_files()
    print(f"Total C# files scanned: {len(files)}")
    
    c2 = check_file_lengths(files)
    print(f"Rule C2 (>300 lines) violations: {len(c2)}")
    for r, count in c2:
        print(f"  {r}: {count} lines")
        
    m3 = check_method_lengths(files)
    print(f"Rule M3 (>50 lines) violations: {len(m3)}")
    for r, name, s, e, l in m3:
        print(f"  {r}:{s} Method {name} has {l} lines")
        
    qb1 = check_commented_code(files)
    print(f"Rule Q-B1 (commented code) violations: {len(qb1)}")
    for r, line, text in qb1:
        print(f"  {r}:{line} -> {text}")
        
    qb2 = check_catch_blocks(files)
    print(f"Rule Q-B2 (unannotated catch) violations: {len(qb2)}")
    for r, line, text in qb2:
        print(f"  {r}:{line} -> {text}")
