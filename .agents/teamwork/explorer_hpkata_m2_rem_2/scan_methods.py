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
                    print(f"{filepath}:{start_line}: {method_name} is {length} lines (lines {start_line}-{idx})")
                in_method = False
                started_braces = False
        else:
            class_brace_depth += open_b - close_b

for root_dir in ['HPKata/HPKata.Revit', 'HPKata/HPKata.Revit.Tests']:
    for root, dirs, files in os.walk(root_dir):
        if 'bin' in dirs: dirs.remove('bin')
        if 'obj' in dirs: dirs.remove('obj')
        for file in files:
            if file.endswith('.cs'):
                check_file(os.path.join(root, file))
print("Method scan complete.")


print("\n--- Scanning proposed files in explorer folder ---")
proposed_dir = '.agents/teamwork/explorer_hpkata_m2_rem_2'
for file in sorted(os.listdir(proposed_dir)):
    if file.startswith('proposed_') and file.endswith('.cs'):
        path = os.path.join(proposed_dir, file)
        with open(path, 'r', encoding='utf-8') as f:
            total_lines = len(f.readlines())
        print(f"\n{file}: {total_lines} total lines")
        check_file(path)
