import os
import re

CORE_DIR = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core"

def audit_all_methods():
    all_methods = []
    
    for root, dirs, files in os.walk(CORE_DIR):
        if "obj" in root or "bin" in root:
            continue
        for f in files:
            if not f.endswith(".cs"):
                continue
            fpath = os.path.join(root, f)
            rel = os.path.relpath(fpath, CORE_DIR)
            with open(fpath, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
            
            i = 0
            while i < len(lines):
                line = lines[i].strip()
                # Check method definition
                m_decl = re.search(r'\b(public|private|protected|internal|static|async|virtual|override|sealed)\b.*?\b([A-Za-z0-9_]+)\s*\(', line)
                if m_decl and not any(kw in line for kw in ["class ", "interface ", "struct ", "enum ", "record ", "namespace ", "delegate "]):
                    name = m_decl.group(2)
                    decl_start = i + 1
                    
                    if "=>" in line and ";" in line:
                        all_methods.append({
                            "file": rel, "name": name, "decl_start": decl_start, "decl_end": decl_start,
                            "body_start": decl_start, "body_end": decl_start,
                            "total_span": 1, "body_span": 1, "is_expr": True, "is_switch": False
                        })
                        i += 1
                        continue
                    
                    # Search for body {
                    brace_depth = 0
                    brace_found = False
                    body_start = None
                    body_end = None
                    is_switch = False
                    
                    k = i
                    while k < len(lines):
                        cur_line = lines[k]
                        if "switch" in cur_line:
                            is_switch = True
                        cleaned = re.sub(r'".*?"', '', re.sub(r'//.*', '', cur_line))
                        for ch in cleaned:
                            if ch == '{':
                                brace_depth += 1
                                if not brace_found:
                                    brace_found = True
                                    body_start = k + 1
                            elif ch == '}':
                                brace_depth -= 1
                                if brace_found and brace_depth == 0:
                                    body_end = k + 1
                                    decl_end = k + 1
                                    total_span = decl_end - decl_start + 1
                                    body_span = body_end - body_start + 1
                                    all_methods.append({
                                        "file": rel, "name": name,
                                        "decl_start": decl_start, "decl_end": decl_end,
                                        "body_start": body_start, "body_end": body_end,
                                        "total_span": total_span, "body_span": body_span,
                                        "is_expr": False, "is_switch": is_switch
                                    })
                                    i = k
                                    break
                        if brace_found and brace_depth == 0:
                            break
                        if not brace_found and ";" in cleaned:
                            break
                        k += 1
                i += 1
    return all_methods

if __name__ == "__main__":
    methods = audit_all_methods()
    print(f"Total methods analyzed: {len(methods)}")
    
    print("\nMethods with Total Span > 40:")
    for m in sorted(methods, key=lambda x: -x["total_span"]):
        if m["total_span"] > 40:
            print(f"- {m['file']} -> {m['name']}(): TotalSpan={m['total_span']}, BodySpan={m['body_span']}, BodyLines={m['body_start']}-{m['body_end']}, IsSwitch={m['is_switch']}")
            
    print("\nMethods with Body Span > 50:")
    body_over_50 = [m for m in methods if m["body_span"] > 50]
    print(f"Count: {len(body_over_50)}")
    for m in body_over_50:
        print(f"- {m['file']} -> {m['name']}(): BodySpan={m['body_span']}")
