import os
import re

def check_file(filepath):
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    in_method = False
    method_name = ''
    start_line = 0
    brace_depth = 0
    class_brace_depth = 0
    started_braces = False
    violations = []
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        clean = re.sub(r'//.*', '', stripped)
        clean = re.sub(r'"[^"]*"', '', clean)
        
        open_b = clean.count('{')
        close_b = clean.count('}')
        
        if class_brace_depth >= 1 and not in_method:
            m = re.match(r'(?:public|private|protected|internal|static|async|override|sealed|virtual|\s)+\s+[\w<>\[\]\?]+\s+(\w+)\s*\([^;]*\)', stripped)
            if m and not stripped.endswith(';'):
                method_name = m.group(1)
                start_line = idx
                in_method = True
                brace_depth = 0
                started_braces = False
        
        if in_method:
            if open_b > 0:
                started_braces = True
            brace_depth += open_b - close_b
            if started_braces and brace_depth <= 0:
                length = idx - start_line + 1
                if length > 50:
                    violations.append((filepath, start_line, idx, method_name, length))
                in_method = False
                started_braces = False
        else:
            class_brace_depth += open_b - close_b
    return violations

all_violations = []
for root, dirs, files in os.walk('HPKata'):
    for b in ['bin', 'obj', '.git']:
        if b in dirs: dirs.remove(b)
    for file in files:
        if file.endswith('.cs'):
            all_violations.extend(check_file(os.path.join(root, file)))

print(f"Total Rule M3 violations (>50 lines): {len(all_violations)}")
for v in all_violations:
    print(f"  {v[0]}:{v[1]}-{v[2]} {v[3]} ({v[4]} lines)")
