import os
import re

bridge_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.McpBridge"

cs_files = []
for root, dirs, files in os.walk(bridge_dir):
    if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
        continue
    for f in files:
        if f.endswith(".cs"):
            cs_files.append(os.path.join(root, f))

def analyze_methods(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()

    n = len(lines)
    methods = []
    
    # Simple state machine for method detection
    for i in range(n):
        line = lines[i].strip()
        # Skip comments, attributes, using, namespace, types
        if not line or line.startswith("//") or line.startswith("/*") or line.startswith("*"):
            continue
        if any(kw in line for kw in [" class ", " struct ", " interface ", " record ", " enum "]):
            continue
        if line.startswith("[") and line.endswith("]"):
            continue
            
        # A method usually has '(' and ')' and returns something or is constructor
        # Must not be control flow
        if "(" in line:
            # Check control flow
            if any(re.match(rf"^\b{kw}\b", line) for kw in ["if", "for", "foreach", "while", "switch", "using", "catch", "lock", "fixed"]):
                continue
            # Look ahead for opening brace
            curr = i
            sig_lines = []
            has_brace = False
            has_semicolon = False
            while curr < n:
                l = lines[curr].strip()
                sig_lines.append(l)
                if "{" in l:
                    has_brace = True
                    break
                if ";" in l:
                    has_semicolon = True
                    break
                curr += 1
                
            if has_brace and not has_semicolon:
                brace_start_line = curr
                # Count braces
                brace_depth = 0
                method_end_line = -1
                for idx in range(brace_start_line, n):
                    l = lines[idx]
                    # strip comments and quotes
                    clean_l = re.sub(r'//.*$', '', l)
                    clean_l = re.sub(r'"(?:\\.|[^"\\])*"', '', clean_l)
                    clean_l = re.sub(r"'(?:\\.|[^'\\])*'", '', clean_l)
                    brace_depth += clean_l.count("{") - clean_l.count("}")
                    if brace_depth == 0:
                        method_end_line = idx
                        break
                        
                if method_end_line != -1:
                    total_lines = method_end_line - i + 1
                    body_lines = method_end_line - brace_start_line + 1
                    sig_text = " ".join(sig_lines).split("{")[0].strip()
                    # Only consider if it looks like a method / constructor
                    methods.append((i + 1, method_end_line + 1, total_lines, body_lines, sig_text))
    return methods

all_methods = []
violations = []
for p in cs_files:
    rel = os.path.relpath(p, bridge_dir)
    res = analyze_methods(p)
    for start_l, end_l, total_l, body_l, sig in res:
        all_methods.append((rel, start_l, end_l, total_l, body_l, sig))
        if total_l > 50:
            violations.append((rel, start_l, end_l, total_l, body_l, sig))

all_methods.sort(key=lambda x: -x[2])
print(f"Total methods/constructors identified: {len(all_methods)}")
print("\n--- Longest Methods in HPGrasshopper.McpBridge ---")
for rel, start_l, end_l, total_l, body_l, sig in all_methods[:15]:
    print(f"{total_l:3d} lines [{start_l}:{end_l}] {rel} -> {sig[:65]}")

print(f"\nRule M3 Violations (> 50 lines): {len(violations)}")
for rel, start_l, end_l, total_l, body_l, sig in violations:
    print(f"  VIOLATION: {rel}:{start_l}-{end_l} ({total_l} lines) -> {sig}")
