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

def get_methods_line_counts(file_path):
    with open(file_path, "r", encoding="utf-8-sig") as f:
        lines = f.readlines()
    
    methods = []
    # Find methods by looking for method signature patterns
    # We look for lines with access modifiers and parentheses followed by '{'
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        # skip comments
        if line.startswith("//") or line.startswith("/*") or line.startswith("*"):
            i += 1
            continue
        
        # Check if line looks like method signature
        if any(line.startswith(kw) for kw in ["public ", "private ", "protected ", "internal ", "static ", "async "]):
            if any(decl in line for decl in ["class ", "struct ", "interface ", "enum ", "namespace ", "record "]):
                i += 1
                continue
            
            # Could be a property, field, or method
            # Accumulate until ';' or '{'
            sig_start = i + 1
            sig = line
            j = i
            while j < len(lines) and "{" not in sig and ";" not in sig:
                j += 1
                if j < len(lines):
                    sig += " " + lines[j].strip()
            
            if "{" in sig and "(" in sig and not sig.startswith("public event") and not any(k in sig for k in [" get;", " set;", " init;", "=>"]):
                # This is likely a method or constructor
                brace_start = j + 1
                # Find matching '}'
                depth = 0
                k = j
                found_start = False
                end_line = None
                while k < len(lines):
                    l_cleaned = re.sub(r'//.*$', '', lines[k])
                    l_cleaned = re.sub(r'"(\\.|[^"\\])*"', '""', l_cleaned)
                    for ch in l_cleaned:
                        if ch == '{':
                            depth += 1
                            found_start = True
                        elif ch == '}':
                            depth -= 1
                            if found_start and depth == 0:
                                end_line = k + 1
                                break
                    if end_line is not None:
                        break
                    k += 1
                
                if end_line is not None:
                    # extract method name
                    m_name = sig.split("(")[0].strip().split()[-1]
                    total_lines = end_line - sig_start + 1
                    body_lines = end_line - brace_start + 1
                    methods.append((m_name, sig_start, end_line, total_lines, body_lines))
                    i = k
            else:
                i = j
        i += 1
    return methods

all_over_50 = []
for rel, full in phase3_files:
    # Skip test files for method length
    if "tests" in rel.lower():
        continue
    res = get_methods_line_counts(full)
    for m in res:
        name, s, e, tot, body = m
        if tot > 50 or body > 50:
            all_over_50.append((rel, name, s, e, tot, body))

print(f"Scanned {len(phase3_files)} files.")
print(f"Production methods > 50 lines: {len(all_over_50)}")
for rel, name, s, e, tot, body in all_over_50:
    print(f"  {rel} -> {name}() lines {s}-{e} (total: {tot}, body: {body})")
