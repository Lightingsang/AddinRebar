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

def find_files(base_dir, ext):
    matched = []
    for root, dirs, files in os.walk(base_dir):
        if "obj" in dirs:
            dirs.remove("obj")
        if "bin" in dirs:
            dirs.remove("bin")
        for f in files:
            if f.endswith(ext):
                matched.append(os.path.join(root, f))
    return matched

print("Scanning C# and csproj files...")
cs_files = find_files(HPBIM_DIR, ".cs")
csproj_files = find_files(HPBIM_DIR, ".csproj")

print(f"Total C# files: {len(cs_files)}")
print(f"Total csproj files: {len(csproj_files)}")

# 1. File line counts
files_over_300 = []
for f in cs_files:
    with open(f, "r", encoding="utf-8", errors="ignore") as fp:
        lines = fp.readlines()
        if len(lines) > 300:
            files_over_300.append((os.path.relpath(f, HPBIM_DIR), len(lines)))

print(f"\n1. Files > 300 lines: {len(files_over_300)}")
for rel, count in files_over_300:
    print(f"  VIOLATION: {rel} ({count} lines)")

# 2. Autodesk.Revit in HPBim.Core and HPBim.Cli
revit_refs = []
for f in cs_files:
    rel = os.path.relpath(f, HPBIM_DIR)
    parts = rel.split(os.sep)
    if len(parts) > 1 and parts[1] in ("HPBim.Core", "HPBim.Cli"):
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            for idx, line in enumerate(fp, 1):
                if "Autodesk.Revit" in line:
                    revit_refs.append((rel, idx, line.strip()))

print(f"\n2. Autodesk.Revit.* in HPBim.Core / HPBim.Cli: {len(revit_refs)}")
for rel, idx, line in revit_refs:
    print(f"  VIOLATION: {rel}:{idx} -> {line}")

# 3. Cross MCP references in csproj
cross_refs = []
for cp in csproj_files:
    rel = os.path.relpath(cp, HPBIM_DIR)
    with open(cp, "r", encoding="utf-8", errors="ignore") as fp:
        content = fp.read()
        for mcp in FORBIDDEN_MCPS:
            # Check for path reference e.g. ../HPAutoCad/ or ..\HPAutoCad\ or ..\HPRebar\ (not McpShared\HPRebar)
            if f"../{mcp}/" in content or f"..\\{mcp}\\" in content or f"/{mcp}/" in content or f"\\{mcp}\\" in content:
                # Disregard McpShared\HPRebar.*
                if mcp == "HPRebar" and ("McpShared/HPRebar" in content or "McpShared\\HPRebar" in content):
                    # check if there's any reference that is NOT McpShared
                    for line in content.splitlines():
                        if "HPRebar" in line and "McpShared" not in line:
                            cross_refs.append((rel, line.strip()))
                else:
                    cross_refs.append((rel, mcp))

print(f"\n3. Cross-MCP references in csproj: {len(cross_refs)}")
for rel, mcp in cross_refs:
    print(f"  VIOLATION: {rel} -> {mcp}")

# 4. Method line count in src/
method_pattern = re.compile(
    r'^\s*(?:public|private|protected|internal|static|async|override|virtual|sealed|unsafe)\s+'
    r'(?:(?:public|private|protected|internal|static|async|override|virtual|sealed|unsafe)\s+)*'
    r'[\w\<\>\[\]\,\?\.\s]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*)?$'
)
control_keywords = {"if", "else", "for", "foreach", "while", "do", "switch", "catch", "try", "using", "lock", "fixed"}

methods_over_50 = []
for f in cs_files:
    rel = os.path.relpath(f, HPBIM_DIR)
    if not rel.startswith("src"):
        continue
    with open(f, "r", encoding="utf-8", errors="ignore") as fp:
        lines = fp.readlines()
    
    in_multiline_string = False
    i = 0
    n = len(lines)
    while i < n:
        raw_line = lines[i]
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
                            if '"""' in line_k:
                                cnt = line_k.count('"""')
                                if cnt % 2 == 1:
                                    in_multiline_string = not in_multiline_string
                            if not in_multiline_string:
                                brace_count += line_k.count("{") - line_k.count("}")
                                if brace_count == 0:
                                    method_len = k - start_line + 1
                                    if method_len > 50:
                                        methods_over_50.append((rel, mname, start_line, k + 1, method_len))
                                    i = k
                                    break
                            k += 1
        i += 1

print(f"\n4. Methods > 50 lines in src/: {len(methods_over_50)}")
for rel, mname, s, e, mlen in methods_over_50:
    print(f"  VIOLATION: {rel}:{s}-{e} {mname} ({mlen} lines)")

# 5. Empty catch blocks without comments
empty_catches = []
catch_pattern = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', re.MULTILINE)
for f in cs_files:
    rel = os.path.relpath(f, HPBIM_DIR)
    with open(f, "r", encoding="utf-8", errors="ignore") as fp:
        content = fp.read()
    for m in catch_pattern.finditer(content):
        body = m.group(1).strip()
        if not body:
            lno = content[:m.start()].count("\n") + 1
            empty_catches.append((rel, lno, "Empty catch without comment"))
        elif not any(c in body for c in ["//", "/*"]):
            statements = [s.strip() for s in body.split(";") if s.strip()]
            if not statements:
                lno = content[:m.start()].count("\n") + 1
                empty_catches.append((rel, lno, "No statements and no comment"))

print(f"\n5. Empty catch blocks: {len(empty_catches)}")
for rel, lno, msg in empty_catches:
    print(f"  VIOLATION: {rel}:{lno} {msg}")

# 6. Commented-out code
code_patterns = [
    re.compile(r'^\s*//\s*(?:var|int|string|bool|double|float)\s+[A-Za-z0-9_]+\s*='),
    re.compile(r'^\s*//\s*(?:if|while|for|foreach)\s*\('),
    re.compile(r'^\s*//\s*[A-Za-z0-9_]+\.[A-Za-z0-9_]+\s*\('),
    re.compile(r'^\s*//\s*return\s+[^;]+;'),
    re.compile(r'^\s*//\s*[A-Za-z0-9_]+\s*=\s*[^;]+;'),
]
commented_lines = []
for f in cs_files:
    rel = os.path.relpath(f, HPBIM_DIR)
    with open(f, "r", encoding="utf-8", errors="ignore") as fp:
        for idx, line in enumerate(fp, 1):
            trimmed = line.strip()
            if trimmed.startswith("//") and not trimmed.startswith("///"):
                for pat in code_patterns:
                    if pat.match(line):
                        commented_lines.append((rel, idx, trimmed))
                        break

print(f"\n6. Commented-out code lines: {len(commented_lines)}")
for rel, idx, code in commented_lines:
    print(f"  VIOLATION: {rel}:{idx} -> {code}")
