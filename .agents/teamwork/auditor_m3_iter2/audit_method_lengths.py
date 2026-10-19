import os
import datetime
import re

repo_root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
hpgh_dir = os.path.join(repo_root, "HPGrasshopper")

cutoff_day = datetime.datetime(2026, 10, 12, 0, 0, 0, tzinfo=datetime.timezone.utc)
cutoff_m3 = datetime.datetime(2026, 10, 12, 9, 40, 0, tzinfo=datetime.timezone.utc)
cutoff_iter2 = datetime.datetime(2026, 10, 12, 10, 40, 0, tzinfo=datetime.timezone.utc)

def get_cs_files():
    cs_files = []
    for root, dirs, files in os.walk(hpgh_dir):
        if 'bin' in root or 'obj' in root:
            continue
        for f in files:
            if f.endswith('.cs'):
                fp = os.path.join(root, f)
                cs_files.append(fp)
    return cs_files

def analyze_file(filepath):
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    total = len(lines)
    methods = []
    
    i = 0
    while i < total:
        line = lines[i].strip()
        if (not line or line.startswith('//') or line.startswith('/*') or line.startswith('*') or
            any(re.search(rf"\b{kw}\b", line) for kw in ['class', 'interface', 'struct', 'record', 'enum', 'namespace'])):
            i += 1
            continue
            
        if '(' in line and not any(re.match(rf"^\b{kw}\b", line) for kw in ['if', 'for', 'foreach', 'while', 'switch', 'using', 'catch', 'lock']):
            sig_start = i
            sig_lines = []
            curr = i
            found_body = False
            is_expr = False
            has_semi = False
            while curr < total and curr - sig_start < 25:
                cl = lines[curr].strip()
                if '//' in cl:
                    cl = cl[:cl.index('//')].strip()
                sig_lines.append(cl)
                if '{' in cl:
                    found_body = True
                    break
                if '=>' in cl:
                    found_body = True
                    is_expr = True
                    break
                if ';' in cl:
                    has_semi = True
                    break
                curr += 1
                
            if found_body and not has_semi:
                sig_str = ' '.join(sig_lines)
                if '(' in sig_str and ')' in sig_str:
                    body_start = curr
                    if is_expr:
                        end_line = body_start
                        while end_line < total and ';' not in lines[end_line]:
                            end_line += 1
                        methods.append((sig_start + 1, end_line + 1, end_line - sig_start + 1, sig_str.split('=>')[0].strip()))
                        i = end_line
                    else:
                        brace = 0
                        found_first = False
                        end_line = body_start
                        for scan in range(body_start, total):
                            clean = re.sub(r'//.*$', '', lines[scan])
                            clean = re.sub(r'"(?:\\.|[^"\\])*"', '', clean)
                            for ch in clean:
                                if ch == '{':
                                    brace += 1
                                    found_first = True
                                elif ch == '}':
                                    brace -= 1
                                    if found_first and brace == 0:
                                        end_line = scan
                                        break
                            if found_first and brace == 0:
                                break
                        methods.append((sig_start + 1, end_line + 1, end_line - sig_start + 1, sig_str.split('{')[0].strip()))
                        i = end_line
        i += 1
    return methods

if __name__ == "__main__":
    all_cs = get_cs_files()
    
    # 1. Milestone M3 Iteration 2 touched files:
    m3_iter2_files = [
        os.path.join(hpgh_dir, r"HPGrasshopper.McpBridge\Tools\CanvasBatchValidator.cs"),
        os.path.join(hpgh_dir, r"HPGrasshopper.McpBridge\Tools\CanvasEditor.cs"),
        os.path.join(hpgh_dir, r"HPGrasshopper.McpBridge\Tools\ImageExporter.cs"),
        os.path.join(hpgh_dir, r"HPGrasshopper.McpBridge.Tests\tools\CanvasEditorTests.cs")
    ]
    
    print("=== CHECKING M3 ITERATION 2 TOUCHED FILES ===")
    v_it2 = []
    for fp in m3_iter2_files:
        res = analyze_file(fp)
        for s, e, span, sig in res:
            if span > 50:
                v_it2.append((fp, s, e, span, sig))
            elif span >= 40:
                print(f"  NEAR-LIMIT ({span} lines): {os.path.basename(fp)} lines {s}-{e} -> {sig[:55]}")
    print(f"M3 Iteration 2 Method Violations: {len(v_it2)}")
    for v in v_it2:
        print(f"  VIOLATION: {v[0]} lines {v[1]}-{v[2]} (span: {v[3]} lines): {v[4]}")
        
    print("\n=== CHECKING ALL MILESTONE M3 FILES (MODIFIED AFTER 09:40 UTC) ===")
    m3_all_files = [f for f in all_cs if datetime.datetime.fromtimestamp(os.path.getmtime(f), tz=datetime.timezone.utc) >= cutoff_m3]
    print(f"M3 files count: {len(m3_all_files)}")
    v_m3 = []
    for fp in m3_all_files:
        res = analyze_file(fp)
        for s, e, span, sig in res:
            if span > 50:
                v_m3.append((fp, s, e, span, sig))
    print(f"M3 All Files Method Violations: {len(v_m3)}")
    for v in v_m3:
        print(f"  VIOLATION: {v[0]} lines {v[1]}-{v[2]} (span: {v[3]} lines): {v[4]}")
