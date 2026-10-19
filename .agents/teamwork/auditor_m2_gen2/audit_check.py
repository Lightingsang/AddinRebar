import os
import re
import sys

HPBIM_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim"

def get_cs_files(root_dir):
    cs_files = []
    for root, dirs, files in os.walk(root_dir):
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_file_lines(cs_files):
    violations = []
    file_stats = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8-sig") as fp:
            lines = fp.readlines()
        line_count = len(lines)
        rel_path = os.path.relpath(f, HPBIM_DIR)
        file_stats.append((rel_path, line_count))
        if line_count > 300:
            violations.append((rel_path, line_count))
    return file_stats, violations

def check_method_lengths(cs_files):
    # Rough C# method parser tracking braces
    violations = []
    for f in cs_files:
        rel_path = os.path.relpath(f, HPBIM_DIR)
        with open(f, "r", encoding="utf-8-sig") as fp:
            lines = fp.readlines()
        
        # Simple brace depth tracker
        brace_depth = 0
        current_method = None
        method_start_line = 0
        method_brace_depth = 0
        
        # We look for method signatures: access modifiers, return types, name(args)
        # Note: class level is typically brace_depth 1 or 2 (namespace, class)
        for i, line in enumerate(lines, 1):
            stripped = line.strip()
            
            # Check for method definition when at class level (brace_depth == 1 or 2)
            if not current_method and brace_depth in (1, 2):
                if re.search(r'\b(public|private|protected|internal|static|async|override|virtual)\b.*\([^\)]*\)\s*(\{)?$', stripped):
                    if not stripped.startswith("//") and not stripped.startswith("*"):
                        current_method = stripped
                        method_start_line = i
                        method_brace_depth = brace_depth
            
            open_braces = line.count('{')
            close_braces = line.count('}')
            brace_depth += open_braces - close_braces
            
            if current_method and brace_depth <= method_brace_depth:
                method_len = i - method_start_line + 1
                if method_len > 50:
                    violations.append((rel_path, method_start_line, method_len, current_method))
                current_method = None
                
    return violations

def check_empty_catches(cs_files):
    empty_catches = []
    for f in cs_files:
        rel_path = os.path.relpath(f, HPBIM_DIR)
        with open(f, "r", encoding="utf-8-sig") as fp:
            content = fp.read()
        
        # match catch (...) { } with optional whitespace or no comments
        matches = re.finditer(r'catch\s*(\([^\)]*\))?\s*\{([^\}]*)\}', content)
        for m in matches:
            body = m.group(2).strip()
            if not body or (not any(c.isalpha() for c in body) and '//' not in body and '/*' not in body):
                line_no = content[:m.start()].count('\n') + 1
                empty_catches.append((rel_path, line_no, m.group(0)))
    return empty_catches

def check_commented_code(cs_files):
    commented_code = []
    code_patterns = [
        re.compile(r'^\s*//\s*(var|int|string|double|bool|if|for|foreach|while|return|new|Assert)\s+'),
        re.compile(r'^\s*//\s*[A-Z][a-zA-Z0-9_]*\.[A-Z][a-zA-Z0-9_]*\('),
        re.compile(r'^\s*//\s*[a-z][a-zA-Z0-9_]*\s*=\s*'),
    ]
    for f in cs_files:
        rel_path = os.path.relpath(f, HPBIM_DIR)
        with open(f, "r", encoding="utf-8-sig") as fp:
            for i, line in enumerate(fp, 1):
                # ignore doc comments or normal explanatory comments
                if line.strip().startswith("///"):
                    continue
                for pat in code_patterns:
                    if pat.search(line):
                        commented_code.append((rel_path, i, line.strip()))
                        break
    return commented_code

def check_revit_and_mcp_refs():
    violations = []
    for root, dirs, files in os.walk(HPBIM_DIR):
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            ext = os.path.splitext(f)[1]
            if ext in (".cs", ".csproj"):
                path = os.path.join(root, f)
                with open(path, "r", encoding="utf-8-sig") as fp:
                    for i, line in enumerate(fp, 1):
                        if "Autodesk.Revit" in line or "Autodesk" in line:
                            # ignore comments explaining absence of Autodesk
                            if "//" in line or "<!--" in line:
                                continue
                            violations.append((os.path.relpath(path, HPBIM_DIR), i, line.strip()))
                        if re.search(r'\b(HPAutoCad|HPCivil3d|HPEtabs|HPExcel|HPNavis|HPPowerBi|HPRobot|HPSap2000|HPTekla)\b', line):
                            violations.append((os.path.relpath(path, HPBIM_DIR), i, line.strip()))
    return violations

def check_test_assertions(cs_files):
    test_files = [f for f in cs_files if "tests" in f.lower()]
    facts = 0
    theories = 0
    assertions = []
    trivial_assertions = []
    
    for f in test_files:
        rel_path = os.path.relpath(f, HPBIM_DIR)
        with open(f, "r", encoding="utf-8-sig") as fp:
            lines = fp.readlines()
        for i, line in enumerate(lines, 1):
            if "[Fact]" in line:
                facts += 1
            if "[Theory]" in line:
                theories += 1
            if "Assert." in line:
                assertions.append((rel_path, i, line.strip()))
                if re.search(r'Assert\.True\(\s*true\s*\)', line) or re.search(r'Assert\.False\(\s*false\s*\)', line):
                    trivial_assertions.append((rel_path, i, line.strip()))
                if re.search(r'Assert\.Equal\(\s*([^,]+)\s*,\s*\1\s*\)', line):
                    trivial_assertions.append((rel_path, i, line.strip()))
    return facts, theories, assertions, trivial_assertions

if __name__ == "__main__":
    cs_files = get_cs_files(HPBIM_DIR)
    print(f"Total C# files found in HPBim: {len(cs_files)}")
    
    file_stats, file_violations = check_file_lines(cs_files)
    print(f"\n--- File Line Count Audit (Limit <= 300) ---")
    file_stats.sort(key=lambda x: x[1], reverse=True)
    for path, cnt in file_stats:
        status = "FAIL (>300)" if cnt > 300 else "PASS"
        print(f"[{status}] {cnt:3d} lines: {path}")
    print(f"File line limit violations: {len(file_violations)}")
    
    method_violations = check_method_lengths(cs_files)
    print(f"\n--- Method Length Audit (Limit <= 50) ---")
    if method_violations:
        for path, line, length, name in method_violations:
            print(f"[FAIL] {path}:{line} ({length} lines) - {name[:60]}")
    else:
        print("PASS: 0 methods exceed 50 lines.")
    print(f"Method line limit violations: {len(method_violations)}")
    
    empty_catches = check_empty_catches(cs_files)
    print(f"\n--- Empty Catch Audit (Q-B2) ---")
    if empty_catches:
        for path, line, text in empty_catches:
            print(f"[FAIL] {path}:{line} - {text}")
    else:
        print("PASS: 0 empty catches.")
    print(f"Empty catch violations: {len(empty_catches)}")
    
    commented_code = check_commented_code(cs_files)
    print(f"\n--- Commented-Out Code Audit (Q-B1) ---")
    if commented_code:
        for path, line, text in commented_code:
            print(f"[FLAG] {path}:{line} - {text}")
    else:
        print("PASS: 0 commented-out code lines detected.")
    print(f"Commented code lines: {len(commented_code)}")
    
    revit_refs = check_revit_and_mcp_refs()
    print(f"\n--- Autodesk/Revit & Cross-MCP References ---")
    if revit_refs:
        for path, line, text in revit_refs:
            print(f"[FAIL] {path}:{line} - {text}")
    else:
        print("PASS: 0 Autodesk/Revit references and 0 cross-MCP references.")
    print(f"Reference violations: {len(revit_refs)}")
    
    facts, theories, assertions, trivial = check_test_assertions(cs_files)
    print(f"\n--- Test Assertions Audit ---")
    print(f"Total [Fact] attributes: {facts}")
    print(f"Total [Theory] attributes: {theories}")
    print(f"Total assertions: {len(assertions)}")
    print(f"Trivial assertions (e.g. Assert.True(true)): {len(trivial)}")
    if trivial:
        for path, line, text in trivial:
            print(f"[FAIL] Trivial assertion at {path}:{line}: {text}")
