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

def audit_file_lengths(cs_files):
    violations = []
    max_len = 0
    max_file = ""
    for path in cs_files:
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
        count = len(lines)
        if count > max_len:
            max_len = count
            max_file = path
        if count > 300:
            rel = os.path.relpath(path, HPDCEPRO_DIR)
            violations.append((rel, count))
    return violations, max_file, max_len

def audit_method_lengths(cs_files):
    # Match method declaration headers
    # e.g., public void Foo(...)
    method_sig_pattern = re.compile(
        r'^\s*(?:public|private|protected|internal|static|async|virtual|override|sealed|new|\s)+'
        r'(?:[\w\<\>\[\],\s\?\.]+\s+)'
        r'([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
    )
    violations = []
    max_len = 0
    max_method = ""
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
            
            sanitized = re.sub(r'"(\\.|[^"\\])*"', '', line_str)
            sanitized = re.sub(r"'(\\.|[^'\\])*'", '', sanitized)
            sanitized = re.sub(r'//.*$', '', sanitized)
            
            open_count = sanitized.count('{')
            close_count = sanitized.count('}')
            
            if in_method:
                brace_depth += open_count - close_count
                if brace_depth <= method_brace_level and ('}' in sanitized):
                    length = (idx + 1) - method_start + 1
                    if length > max_len:
                        max_len = length
                        max_method = f"{rel}:{method_name}"
                    if length > 50:
                        violations.append((rel, method_name, method_start, idx + 1, length))
                    in_method = False
            else:
                brace_depth += open_count - close_count
    return violations, max_method, max_len

def audit_commented_code(cs_files):
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
                    for pattern in code_indicators:
                        if pattern.match(line):
                            violations.append((rel, idx, raw))
                            break
    return violations

def audit_catch_blocks(cs_files):
    violations = []
    for path in cs_files:
        rel = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
        
        catch_pattern = re.compile(r'catch\s*(?:\([^\)]*\))?\s*\{([^}]*)\}', re.DOTALL)
        for m in catch_pattern.finditer(content):
            body = m.group(1)
            # Rule Q-B2: Empty catch block requires reason comment explaining why
            has_statement = any(stmt in body for stmt in [';', 'throw', 'return', 'Log', 'Console', 'Debug'])
            has_comment = '//' in body or '/*' in body
            if not has_statement and not has_comment:
                start_pos = m.start()
                line_no = content[:start_pos].count('\n') + 1
                violations.append((rel, line_no, m.group(0).split('\n')[0]))
            elif not has_comment and "// Reason:" not in body:
                # Also check strict rule if catch swallows without explanation
                pass
    return violations

def audit_drm_telemetry(cs_files):
    forbidden = [
        'LicenseMoi', 'LicenseCheckAPI', 'DceSerinumber', 'DbimTaiKhoanApi',
        'DceUpdate', 'DceGuard', 'DAnalysisss'
    ]
    violations = []
    for path in cs_files:
        rel = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            content = f.read()
            for token in forbidden:
                if token.lower() in content.lower():
                    violations.append((rel, token))
    return violations

def audit_dummy_mocks(cs_files):
    # Check for dummy mocks: NotImplementedException, return null/default/constant in non-trivial interfaces
    violations = []
    for path in cs_files:
        # We only check src files, not test files
        if "tests" in path.split(os.sep):
            continue
        rel = os.path.relpath(path, HPDCEPRO_DIR)
        with open(path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
        for idx, line in enumerate(lines, start=1):
            if "throw new NotImplementedException" in line:
                violations.append((rel, idx, "throw new NotImplementedException"))
    return violations

if __name__ == "__main__":
    files = get_cs_files()
    print(f"Total C# files scanned: {len(files)}")
    
    c2, max_f, max_flen = audit_file_lengths(files)
    print(f"Rule C2 (lines <= 300) violations: {len(c2)} (Max file: {os.path.basename(max_f)} with {max_flen} lines)")
    for r, count in c2:
        print(f"  VIOLATION: {r}: {count} lines")
        
    m3, max_m, max_mlen = audit_method_lengths(files)
    print(f"Rule M3 (method <= 50 lines) violations: {len(m3)} (Max method: {max_m} with {max_mlen} lines)")
    for r, name, s, e, l in m3:
        print(f"  VIOLATION: {r}:{s} Method {name} has {l} lines")
        
    qb1 = audit_commented_code(files)
    print(f"Rule Q-B1 (commented code) violations: {len(qb1)}")
    for r, line, text in qb1:
        print(f"  VIOLATION: {r}:{line} -> {text}")
        
    qb2 = audit_catch_blocks(files)
    print(f"Rule Q-B2 (unannotated catch) violations: {len(qb2)}")
    for r, line, text in qb2:
        print(f"  VIOLATION: {r}:{line} -> {text}")

    drm = audit_drm_telemetry(files)
    print(f"Legacy DRM / Telemetry violations: {len(drm)}")
    for r, token in drm:
        print(f"  VIOLATION: {r} contains {token}")

    mocks = audit_dummy_mocks(files)
    print(f"Dummy mock / NotImplementedException in src: {len(mocks)}")
    for r, line, text in mocks:
        print(f"  VIOLATION: {r}:{line} -> {text}")
