import os
import re

BASE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata"

phase3_files = []
for root, dirs, files in os.walk(BASE_DIR):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in files:
        if not f.endswith(".cs"): continue
        full_path = os.path.join(root, f)
        rel_path = os.path.relpath(full_path, BASE_DIR)
        rel_lower = rel_path.lower()
        if "column" in rel_lower or "wall" in rel_lower:
            phase3_files.append((rel_path, full_path))

print(f"Scanning {len(phase3_files)} Phase 3 files for method lengths...")

def find_methods(code):
    # Regex to find method signatures
    # Look for access modifier followed by return type, method name, parameter list, and opening brace
    lines = code.splitlines()
    methods = []
    
    # We can scan line by line
    # Look for '{' that opens a method body
    # We track depth: namespace (depth 0/1) -> class (depth 1/2) -> method (depth 2/3)
    depth = 0
    in_class = False
    method_start = None
    method_name = None
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        # Remove comments and strings
        cleaned = re.sub(r'//.*$', '', stripped)
        cleaned = re.sub(r'"(\\.|[^"\\])*"', '""', cleaned)
        cleaned = re.sub(r"'(\\.|[^'\\])*'", "''", cleaned)
        
        # Check for start of method: if depth is at class level (usually 2 or 1 if file-scoped namespace)
        # Look backwards for method signature if cleaned has '{'
        for ch in cleaned:
            if ch == '{':
                depth += 1
                # If we were searching for a method start, or depth transitioned
                # Check if this brace belongs to a method
                # Look at previous lines (up to 5 lines back) for method header
                header = " ".join([lines[k].strip() for k in range(max(0, idx - 6), idx)])
                # Check if header has method pattern and not class/struct/namespace/property
                if not any(kw in header for kw in ["class ", "struct ", "interface ", "enum ", "namespace ", "get;", "set;", "=>", "init;"]):
                    m = re.search(r'(?:public|private|protected|internal|static|async|override|virtual|sealed)\s+[\w<>\[\],\?]+\s+([A-Za-z0-9_]+)\s*\([^;{}]*\)\s*$', header)
                    if m:
                        m_name = m.group(1)
                        if m_name not in ["if", "while", "for", "foreach", "switch", "catch", "lock"]:
                            # Find exact starting line of the method header
                            sig_start = idx
                            for k in range(idx - 1, max(0, idx - 7), -1):
                                if any(acc in lines[k] for acc in ["public", "private", "protected", "internal"]):
                                    sig_start = k + 1
                                    break
                            methods.append({
                                "name": m_name,
                                "sig_start": sig_start,
                                "brace_start": idx,
                                "depth": depth,
                                "end_line": None
                            })
            elif ch == '}':
                # Check if closing any open method
                for m in reversed(methods):
                    if m["depth"] == depth and m["end_line"] is None:
                        m["end_line"] = idx
                        break
                depth -= 1

    return methods

over_50 = []
for rel, full in phase3_files:
    # Skip test files for method length check (tests often have arrange/act/assert that can be ~50-60 lines or theory data)
    # But let's check both production and test
    with open(full, "r", encoding="utf-8-sig") as fp:
        code = fp.read()
    methods = find_methods(code)
    for m in methods:
        if m["end_line"] is not None:
            # Measure: signature start to end brace
            total = m["end_line"] - m["sig_start"] + 1
            body_only = m["end_line"] - m["brace_start"] + 1
            if total > 50 or body_only > 50:
                over_50.append((rel, m["name"], m["sig_start"], m["end_line"], total, body_only))

print(f"\nMethods exceeding 50 lines:")
for rel, name, start, end, total, body in over_50:
    print(f"  {rel} -> {name}() lines {start}-{end} (total: {total}, body: {body})")
