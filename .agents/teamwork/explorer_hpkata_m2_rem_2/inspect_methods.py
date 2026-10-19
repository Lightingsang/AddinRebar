import os
import re

def inspect_methods(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    print(f"\nFile: {filepath} ({len(lines)} lines)")
    
    in_method = False
    method_name = ''
    start_line = 0
    brace_depth = 0
    class_brace_depth = 0
    started_braces = False
    
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
                print(f"  Method '{method_name}': {length} lines (L{start_line}-L{idx})")
                in_method = False
                started_braces = False
        else:
            class_brace_depth += open_b - close_b

folder = '.agents/teamwork/explorer_hpkata_m2_rem_2'
for f in sorted(os.listdir(folder)):
    if f.startswith('proposed_') and f.endswith('.cs'):
        inspect_methods(os.path.join(folder, f))
