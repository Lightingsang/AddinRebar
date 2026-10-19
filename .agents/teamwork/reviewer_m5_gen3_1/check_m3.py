import os, re

root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src'
m3_violations = []

def check_file(filepath):
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    in_method = False
    start_line = 0
    brace_depth = 0
    method_name = ""
    
    # Simple regex for method signature
    sig_pattern = re.compile(r'^\s*(?:public|private|protected|internal|static|async|override|virtual|sealed)\s+[\w\<\>\[\],\s\?]+\s+([A-Za-z0-9_]+)\s*\(')

    for idx, line in enumerate(lines):
        line_stripped = line.strip()
        if not in_method:
            m = sig_pattern.match(line)
            if m and not line_stripped.endswith(';'):
                # Potential method start
                method_name = m.group(1)
                start_line = idx + 1
                brace_depth = line.count('{') - line.count('}')
                if '{' in line:
                    in_method = True
        else:
            brace_depth += line.count('{') - line.count('}')
            if brace_depth <= 0:
                end_line = idx + 1
                method_length = end_line - start_line + 1
                if method_length > 50:
                    m3_violations.append((filepath, start_line, end_line, method_length, method_name))
                in_method = False

for dirpath, _, filenames in os.walk(root):
    for f in filenames:
        if f.endswith('.cs'):
            check_file(os.path.join(dirpath, f))

print(f"M3 violations (>50 lines): {len(m3_violations)}")
for p, s, e, length, name in m3_violations[:25]:
    rel = os.path.relpath(p, root)
    print(f"  {rel}:{s}-{e} ({length} lines) in {name}")
