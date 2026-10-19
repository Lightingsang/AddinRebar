import os
import re
import sys

HPKATA_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(HPKATA_DIR):
        if "bin" in dirs:
            dirs.remove("bin")
        if "obj" in dirs:
            dirs.remove("obj")
        for f in files:
            if f.endswith(".cs"):
                cs_files.append(os.path.join(root, f))
    return cs_files

def check_c2(cs_files):
    violations = []
    for f in cs_files:
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        if len(lines) > 300:
            rel = os.path.relpath(f, HPKATA_DIR)
            violations.append((rel, len(lines)))
    return violations

def check_m3(cs_files):
    # Check method length <= 50 lines using brace matching
    violations = []
    method_sig_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|\s)+[\w<>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where[^{]+)?$')
    
    for f in cs_files:
        rel = os.path.relpath(f, HPKATA_DIR)
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            lines = fp.readlines()
        
        i = 0
        n = len(lines)
        while i < n:
            line = lines[i].strip()
            # check if line looks like method declaration
            if not line.startswith("//") and not line.startswith("/*") and not line.startswith("["):
                m = method_sig_pattern.match(lines[i])
                if m and not line.endswith(";"): # not abstract/interface
                    method_name = m.group(1)
                    # skip class/struct/enum/interface declarations
                    if method_name not in ["class", "struct", "interface", "enum", "record", "namespace"]:
                        # find opening brace
                        start_line = i + 1
                        brace_count = 0
                        found_open = False
                        j = i
                        while j < n:
                            for ch in lines[j]:
                                if ch == '{':
                                    brace_count += 1
                                    found_open = True
                                elif ch == '}':
                                    brace_count -= 1
                            if found_open and brace_count == 0:
                                end_line = j + 1
                                length = end_line - start_line + 1
                                if length > 50:
                                    violations.append((rel, method_name, start_line, end_line, length))
                                i = j
                                break
                            j += 1
            i += 1
    return violations

def check_qb1(cs_files):
    violations = []
    # check for TODO, FIXME, HACK, NotImplementedException, and commented out code (e.g. // var x = ...; or // if (...) )
    todo_pat = re.compile(r'\b(TODO|FIXME|HACK|NotImplementedException)\b', re.IGNORECASE)
    comment_code_pat = re.compile(r'^\s*//\s*(?:var|if|for|while|return|tr\.|doc\.|ed\.|new\s+[A-Z]|Console\.|[A-Za-z_][A-Za-z0-9_]*\s*=[^=]|.+;)\s*$')
    
    for f in cs_files:
        rel = os.path.relpath(f, HPKATA_DIR)
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            for idx, line in enumerate(fp, 1):
                if todo_pat.search(line):
                    # ignore comments discussing NotImplementedException or docs
                    if "NotImplementedException" in line and ("throw new NotImplementedException" in line or "TODO" in line):
                        violations.append((rel, idx, line.strip()))
                    elif any(k in line for k in ["TODO", "FIXME", "HACK"]):
                        violations.append((rel, idx, line.strip()))
                if comment_code_pat.match(line):
                    # verify it's not just a markdown doc or text comment with semicolon
                    stripped = line.strip()[2:].strip()
                    if not stripped.startswith("http") and not stripped.startswith("Multi-version:"):
                        if stripped.endswith(";") or stripped.startswith("var ") or stripped.startswith("return "):
                            violations.append((rel, idx, line.strip()))
    return violations

def check_qb2(cs_files):
    violations = []
    # Empty catch block without comments
    empty_catch = re.compile(r'catch\s*(?:\([^\)]*\))?\s*\{\s*\}')
    for f in cs_files:
        rel = os.path.relpath(f, HPKATA_DIR)
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            content = fp.read()
        for m in empty_catch.finditer(content):
            violations.append((rel, m.group(0)))
    return violations

def check_r5(cs_files):
    violations = []
    # Constant 304.8 strictly and exclusively in RevitUnitConverter.cs
    for f in cs_files:
        rel = os.path.relpath(f, HPKATA_DIR)
        with open(f, "r", encoding="utf-8", errors="ignore") as fp:
            for idx, line in enumerate(fp, 1):
                if "304.8" in line:
                    if not rel.endswith("RevitUnitConverter.cs"):
                        violations.append((rel, idx, line.strip()))
    return violations

def check_host_neutrality(cs_files):
    violations = []
    # HPKata.Core has zero references to Autodesk.*
    for f in cs_files:
        rel = os.path.relpath(f, HPKATA_DIR)
        if "HPKata.Core" in rel and "HPKata.Core.Tests" not in rel:
            with open(f, "r", encoding="utf-8", errors="ignore") as fp:
                for idx, line in enumerate(fp, 1):
                    if "Autodesk." in line:
                        violations.append((rel, idx, line.strip()))
    return violations

if __name__ == "__main__":
    files = get_cs_files()
    print(f"Total C# files found in HPKata: {len(files)}")
    
    c2 = check_c2(files)
    print(f"Rule C2 (>300 lines) violations: {len(c2)}")
    for v in c2:
        print(f"  {v[0]}: {v[1]} lines")
        
    m3 = check_m3(files)
    print(f"Rule M3 (>50 lines) violations: {len(m3)}")
    for v in m3:
        print(f"  {v[0]} :: {v[1]} (lines {v[2]}-{v[3]}, count {v[4]})")
        
    qb1 = check_qb1(files)
    print(f"Rule Q-B1 (TODO/dead code) violations: {len(qb1)}")
    for v in qb1:
        print(f"  {v[0]}:{v[1]}: {v[2]}")
        
    qb2 = check_qb2(files)
    print(f"Rule Q-B2 (empty unannotated catch) violations: {len(qb2)}")
    for v in qb2:
        print(f"  {v[0]}: {v[1]}")
        
    r5 = check_r5(files)
    print(f"Rule R5 (304.8 outside RevitUnitConverter) violations: {len(r5)}")
    for v in r5:
        print(f"  {v[0]}:{v[1]}: {v[2]}")
        
    hn = check_host_neutrality(files)
    print(f"Host neutrality (Autodesk in HPKata.Core) violations: {len(hn)}")
    for v in hn:
        print(f"  {v[0]}:{v[1]}: {v[2]}")
