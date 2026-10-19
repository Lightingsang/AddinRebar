import os
import re
import sys

HPBIM_DIR = os.path.abspath(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim")
SRC_DIR = os.path.join(HPBIM_DIR, "src")
TESTS_DIR = os.path.join(HPBIM_DIR, "tests")

FORBIDDEN_MCPS = [
    "HPAutoCad", "HPNavis", "HPEtabs", "HPCivil3d",
    "HPSap2000", "HPPowerBi", "HPExcel", "HPRobot", "HPTekla", "HPRebar"
]

def find_cs_files(base_dir):
    cs_files = []
    for root, dirs, files in os.walk(base_dir):
        if "obj" in dirs:
            dirs.remove("obj")
        if "bin" in dirs:
            dirs.remove("bin")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def find_csproj_files(base_dir):
    csproj_files = []
    for root, dirs, files in os.walk(base_dir):
        if "obj" in dirs:
            dirs.remove("obj")
        if "bin" in dirs:
            dirs.remove("bin")
        for f in files:
            if f.endswith(".csproj"):
                csproj_files.append(os.path.join(root, f))
    return csproj_files

def check_file_length(file_path):
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        return len(f.readlines())

def extract_methods(file_path):
    """Accurately extract C# methods and their line counts, ignoring control flow keywords and raw string literals."""
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()
    
    methods = []
    control_keywords = {"if", "else", "for", "foreach", "while", "do", "switch", "catch", "try", "using", "lock", "fixed"}
    
    # Method pattern: [modifiers] [return_type] MethodName([params])
    # Exclude control structures
    method_pattern = re.compile(
        r'^\s*(?:public|private|protected|internal|static|async|override|virtual|sealed|unsafe)\s+'
        r'(?:(?:public|private|protected|internal|static|async|override|virtual|sealed|unsafe)\s+)*'
        r'[\w\<\>\[\]\,\?\.\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
    )

    in_multiline_string = False
    i = 0
    n = len(lines)
    while i < n:
        raw_line = lines[i]
        
        # Check raw string literal toggle """
        if '"""' in raw_line:
            cnt = raw_line.count('"""')
            if cnt % 2 == 1:
                in_multiline_string = not in_multiline_string

        if in_multiline_string:
            i += 1
            continue

        trimmed = raw_line.strip()
        if not trimmed.startswith("//") and not trimmed.endswith(";"):
            m = method_pattern.match(raw_line)
            if m:
                mname = m.group(1)
                if mname not in control_keywords:
                    # Find opening brace
                    j = i
                    brace_found = False
                    while j < n and j < i + 5:
                        if "{" in lines[j]:
                            brace_found = True
                            break
                        if ";" in lines[j]:
                            break
                        j += 1
                    if brace_found:
                        brace_count = 0
                        k = j
                        start_line = i + 1
                        while k < n:
                            line_k = lines[k]
                            # Count braces only outside raw strings
                            if '"""' in line_k:
                                cnt = line_k.count('"""')
                                if cnt % 2 == 1:
                                    in_multiline_string = not in_multiline_string
                            if not in_multiline_string:
                                brace_count += line_k.count("{") - line_k.count("}")
                                if brace_count == 0:
                                    method_len = k - start_line + 1
                                    methods.append((mname, start_line, k + 1, method_len))
                                    i = k
                                    break
                            k += 1
        i += 1
    return methods

def check_empty_catches(file_path):
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
    
    findings = []
    # Catch block regex: catch (...) { } or catch { }
    catch_pattern = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', re.MULTILINE)
    for m in catch_pattern.finditer(content):
        body = m.group(1).strip()
        if not body:
            line_no = content[:m.start()].count("\n") + 1
            findings.append((line_no, "Empty catch block with NO statements and NO comment"))
        elif not any(c in body for c in ["//", "/*"]):
            statements = [s.strip() for s in body.split(";") if s.strip()]
            if not statements:
                line_no = content[:m.start()].count("\n") + 1
                findings.append((line_no, "Catch block has no statements and no comment"))
    return findings

def check_commented_code(file_path):
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        lines = f.readlines()
    
    findings = []
    code_patterns = [
        re.compile(r'^\s*//\s*(?:var|int|string|bool|double|float)\s+[A-Za-z0-9_]+\s*='),
        re.compile(r'^\s*//\s*(?:if|while|for|foreach)\s*\('),
        re.compile(r'^\s*//\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\s*\('),
        re.compile(r'^\s*//\s*return\s+[^;]+;'),
        re.compile(r'^\s*//\s*[A-Za-z0-9_]+\s*=\s*[^;]+;'),
    ]
    for idx, line in enumerate(lines):
        trimmed = line.strip()
        if trimmed.startswith("//") and not trimmed.startswith("///"):
            for pat in code_patterns:
                if pat.match(line):
                    findings.append((idx + 1, trimmed))
                    break
    return findings

def check_revit_references(file_path):
    with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
    hits = []
    revit_matches = re.finditer(r'\bAutodesk\.Revit\b', content)
    for m in revit_matches:
        line_no = content[:m.start()].count("\n") + 1
        hits.append((line_no, m.group(0)))
    return hits

def check_csproj_dependencies(csproj_path):
    with open(csproj_path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()
    violations = []
    for mcp in FORBIDDEN_MCPS:
        if f"../{mcp}/" in content or f"\\{mcp}\\" in content or f"/{mcp}/" in content:
            violations.append(mcp)
    return violations

def main():
    print("=" * 80)
    print("HPBIM REPOSITORY-WIDE CLEAN CODE AUDIT")
    print("=" * 80)
    
    src_cs = find_cs_files(SRC_DIR)
    test_cs = find_cs_files(TESTS_DIR)
    all_cs = src_cs + test_cs
    print(f"Total C# source files in src/: {len(src_cs)}")
    print(f"Total C# test files in tests/: {len(test_cs)}")
    print(f"Total C# files analyzed: {len(all_cs)}")
    print()

    # 1. Check file lengths
    print("--- 1. FILE LENGTH AUDIT ---")
    long_files_300 = []
    long_files_200 = []
    for f in all_cs:
        rel = os.path.relpath(f, HPBIM_DIR)
        count = check_file_length(f)
        if count > 300:
            long_files_300.append((rel, count))
        elif count > 200:
            long_files_200.append((rel, count))
    
    print(f"Files > 300 lines (HARD LIMIT): {len(long_files_300)}")
    for rel, count in long_files_300:
        print(f"  [FAIL] {rel}: {count} lines")
    print(f"Files > 200 lines (target review trigger): {len(long_files_200)}")
    for rel, count in long_files_200:
        print(f"  [WARN] {rel}: {count} lines")
    if not long_files_300:
        print("  => PASS: 100% of files satisfy hard limit (<= 300 lines)!")
    print()

    # 2. Check method lengths
    print("--- 2. METHOD LENGTH AUDIT (src/) ---")
    long_methods_50 = []
    long_methods_40 = []
    for f in src_cs:
        rel = os.path.relpath(f, HPBIM_DIR)
        methods = extract_methods(f)
        for mname, start, end, mlen in methods:
            if mlen > 50:
                long_methods_50.append((rel, mname, start, end, mlen))
            elif mlen > 40:
                long_methods_40.append((rel, mname, start, end, mlen))
    print(f"Methods > 50 lines (HARD LIMIT): {len(long_methods_50)}")
    for rel, mname, start, end, mlen in long_methods_50:
        print(f"  [FAIL] {rel}:{start}-{end} {mname}: {mlen} lines")
    print(f"Methods > 40 lines (target review trigger): {len(long_methods_40)}")
    for rel, mname, start, end, mlen in long_methods_40:
        print(f"  [WARN] {rel}:{start}-{end} {mname}: {mlen} lines")
    if not long_methods_50:
        print("  => PASS: 100% of methods in src/ satisfy hard limit (<= 50 lines)!")
    print()

    # 3. Check empty catch blocks
    print("--- 3. EMPTY CATCH BLOCK AUDIT ---")
    empty_catches = []
    for f in all_cs:
        rel = os.path.relpath(f, HPBIM_DIR)
        catches = check_empty_catches(f)
        for lno, msg in catches:
            empty_catches.append((rel, lno, msg))
    print(f"Unannotated empty catch blocks: {len(empty_catches)}")
    for rel, lno, msg in empty_catches:
        print(f"  [FAIL] {rel}:{lno} - {msg}")
    if not empty_catches:
        print("  => PASS: 0 unannotated empty catch blocks found!")
    print()

    # 4. Check commented-out code
    print("--- 4. COMMENTED-OUT CODE AUDIT (CM3 / Q-B1) ---")
    commented_code = []
    for f in all_cs:
        rel = os.path.relpath(f, HPBIM_DIR)
        hits = check_commented_code(f)
        for lno, code in hits:
            commented_code.append((rel, lno, code))
    print(f"Commented-out code occurrences: {len(commented_code)}")
    for rel, lno, code in commented_code:
        print(f"  [INFO] {rel}:{lno} -> {code}")
    if not commented_code:
        print("  => PASS: 0 commented-out code detected!")
    print()

    # 5. Check Autodesk.Revit references
    print("--- 5. FORBIDDEN AUTODESK.REVIT.* REFERENCES ---")
    revit_refs = []
    for f in src_cs:
        rel = os.path.relpath(f, HPBIM_DIR)
        # Check HPBim.Core and HPBim.Cli strictly
        parts = rel.split(os.sep)
        if len(parts) > 1 and parts[1] in ("HPBim.Core", "HPBim.Cli"):
            hits = check_revit_references(f)
            for lno, ref in hits:
                revit_refs.append((rel, lno, ref))
    print(f"Autodesk.Revit references in HPBim.Core and HPBim.Cli: {len(revit_refs)}")
    for rel, lno, ref in revit_refs:
        print(f"  [FAIL] {rel}:{lno} -> {ref}")
    if not revit_refs:
        print("  => PASS: 0 Autodesk.Revit references in HPBim.Core and HPBim.Cli!")
    print()

    # 6. Check CSPROJ Cross-MCP references
    print("--- 6. CROSS-MCP PROJECT REFERENCES ---")
    csproj_files = find_csproj_files(HPBIM_DIR)
    cross_refs = []
    for cp in csproj_files:
        rel = os.path.relpath(cp, HPBIM_DIR)
        violations = check_csproj_dependencies(cp)
        for v in violations:
            cross_refs.append((rel, v))
    print(f"Cross-MCP project references: {len(cross_refs)}")
    for rel, v in cross_refs:
        print(f"  [FAIL] {rel} references forbidden MCP: {v}")
    if not cross_refs:
        print("  => PASS: 0 cross-MCP project references! (Only McpShared referenced)")
    print()

    print("=" * 80)
    print("AUDIT SUMMARY COMPLETE")
    print("=" * 80)

if __name__ == "__main__":
    main()
