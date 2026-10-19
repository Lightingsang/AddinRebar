import os
import re

target = r"HPGrasshopper/HPGrasshopper.McpBridge"

def analyze_methods(p):
    with open(p, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    # We will find every block {...} that belongs to a method / function / constructor / accessor
    # Token-based state machine
    methods = []
    
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        
        # Check if line looks like method / ctor / property header
        # Ignore comments and attributes
        if stripped.startswith("//") or stripped.startswith("/*") or stripped.startswith("*"):
            i += 1
            continue
        
        # Look for keywords indicating member declaration
        is_candidate = any(stripped.startswith(kw) for kw in [
            "public ", "private ", "protected ", "internal ", "static ", "async ", "virtual ", "override ", "sealed "
        ])
        
        if is_candidate:
            decl_start = i
            header_lines = [stripped]
            j = i
            while j < len(lines) and "{" not in lines[j] and ";" not in lines[j] and (j - i) < 20:
                j += 1
                if j < len(lines):
                    header_lines.append(lines[j].strip())
            
            header = " ".join(header_lines)
            
            # Check if this is a method or property with body
            if "(" in header and ")" in header and not any(k in header for k in ["class ", "interface ", "delegate ", "record "]):
                # Find the opening brace
                brace_line = -1
                for k in range(decl_start, min(j + 5, len(lines))):
                    if "{" in lines[k]:
                        brace_line = k
                        break
                
                if brace_line != -1:
                    # Match braces to find method end
                    depth = 0
                    end_line = -1
                    for k in range(brace_line, len(lines)):
                        l = lines[k]
                        # remove string literals and line comments
                        clean = re.sub(r'"(\\.|[^"\\])*"', '""', l)
                        clean = re.sub(r'//.*$', '', clean)
                        for ch in clean:
                            if ch == '{': depth += 1
                            elif ch == '}':
                                depth -= 1
                                if depth == 0:
                                    end_line = k
                                    break
                        if depth == 0:
                            break
                    
                    if end_line != -1:
                        total_span = end_line - decl_start + 1
                        body_span = end_line - brace_line + 1
                        m_name = header.split("(")[0].strip().split()[-1]
                        methods.append({
                            "name": m_name,
                            "file": os.path.relpath(p, target),
                            "start": decl_start + 1,
                            "brace": brace_line + 1,
                            "end": end_line + 1,
                            "total_span": total_span,
                            "body_span": body_span,
                            "header": header[:70]
                        })
                        i = end_line
        i += 1
    return methods

all_m = []
for r, _, fs in os.walk(target):
    if "bin" in r or "obj" in r: continue
    for f in fs:
        if f.endswith(".cs"):
            all_m.extend(analyze_methods(os.path.join(r, f)))

all_m.sort(key=lambda x: max(x["total_span"], x["body_span"]), reverse=True)

print(f"Total methods analyzed: {len(all_m)}")
violations = [m for m in all_m if m["total_span"] > 50 or m["body_span"] > 50]
print(f"Violations (> 50 lines): {len(violations)}")
for v in violations:
    print(f"FAIL: {v['file']}:{v['start']}-{v['end']} -> {v['name']} (Total: {v['total_span']}, Body: {v['body_span']})")

print("\nMethods with span >= 35 lines:")
for m in all_m:
    if max(m["total_span"], m["body_span"]) >= 35:
        print(f"  {m['file']:<40} lines {m['start']:3d}-{m['end']:3d} | Total: {m['total_span']:2d}, Body: {m['body_span']:2d} -> {m['name']}()")
