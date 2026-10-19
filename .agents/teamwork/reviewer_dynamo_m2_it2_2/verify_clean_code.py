import os
import re

def check_file(path):
    print(f"=== Checking {path} ===")
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    total_lines = len(lines)
    print(f"Total lines: {total_lines} (Limit <= 300: {'PASS' if total_lines <= 300 else 'FAIL'})")
    
    # Check commented-out code
    commented_code = 0
    for idx, l in enumerate(lines, 1):
        s = l.strip()
        if s.startswith("//") and any(k in s for k in [";", "{", "}", "public ", "private ", "var ", "return "]):
            # Check if not just explanation
            if not s.startswith("// Port evaluation") and not s.startswith("// Multi-version"):
                print(f"Suspicious comment at L{idx}: {s}")
                commented_code += 1
    print(f"Commented code lines: {commented_code} (Rule Q-B1: {'PASS' if commented_code == 0 else 'WARN'})")
    
    # Check methods by tracking braces
    # Simple regex for C# methods
    method_pattern = re.compile(r'^\s*(?:public|private|internal|protected)\s+(?:static\s+|async\s+|sealed\s+|override\s+)*(?:[\w\<\>\?,\.\[\]]+)\s+([A-Za-z0-9_]+)\s*\(')
    
    methods = []
    i = 0
    while i < len(lines):
        m = method_pattern.match(lines[i])
        if m:
            m_name = m.group(1)
            sig_start = i + 1
            # find opening brace
            while i < len(lines) and "{" not in lines[i]:
                # check if expression bodied method e.g. =>
                if "=>" in lines[i]:
                    break
                i += 1
            
            if i < len(lines) and "=>" in lines[i] and "{" not in lines[i]:
                # Expression-bodied
                end_line = i + 1
                length = end_line - sig_start + 1
                methods.append((m_name, sig_start, end_line, length, 1))
                i += 1
                continue
            
            body_start = i + 1
            brace_count = lines[i].count("{") - lines[i].count("}")
            i += 1
            while i < len(lines) and brace_count > 0:
                brace_count += lines[i].count("{") - lines[i].count("}")
                i += 1
            body_end = i
            sig_to_end = body_end - sig_start + 1
            body_only = body_end - body_start + 1
            methods.append((m_name, sig_start, body_end, sig_to_end, body_only))
            continue
        i += 1
        
    print(f"Found {len(methods)} methods:")
    for m_name, start, end, total, body in methods:
        status = "PASS" if total <= 50 else "FAIL"
        print(f"  {m_name} (L{start}-L{end}): total={total}, body={body} [{status}]")

check_file(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Execution\CanvasGraphRunner.cs")
check_file(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Coordinator\DynamoBridgeCoordinator.cs")
