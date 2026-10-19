import re

def check_file_lines(path, max_lines=300):
    with open(path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    count = len(lines)
    print(f"File {path}: {count} lines (Limit: <= {max_lines}) -> {'PASS' if count <= max_lines else 'FAIL'}")
    return count <= max_lines

def check_csharp_methods(path, max_lines=50):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Method inspection
    lines = content.splitlines()
    in_method = False
    method_name = ""
    brace_depth = 0
    start_line = 0
    violations = []
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        # Look for method signatures
        if not in_method and re.match(r'^\s*(public|private|protected|internal|partial)\s+[\w\<\>\?\,\[\]]+\s+(\w+)\s*\(', line):
            # Exclude properties with getters/setters on same line
            if not '{ get' in line and not '=>' in line:
                m = re.search(r'(\w+)\s*\(', line)
                method_name = m.group(1) if m else "unknown"
                in_method = True
                brace_depth = line.count('{') - line.count('}')
                start_line = idx
                continue
        if in_method:
            brace_depth += line.count('{') - line.count('}')
            if brace_depth <= 0 and ('}' in line or idx > start_line):
                length = idx - start_line + 1
                print(f"Method '{method_name}' at lines {start_line}-{idx}: {length} lines (Limit: <= {max_lines}) -> {'PASS' if length <= max_lines else 'FAIL'}")
                if length > max_lines:
                    violations.append((method_name, length))
                in_method = False
                
    # Empty catches
    empty_catches = re.findall(r'catch\s*\([^\)]*\)\s*\{\s*\}', content)
    print(f"Empty catches in {path}: {len(empty_catches)} -> {'PASS' if len(empty_catches) == 0 else 'FAIL'}")
    
    return len(violations) == 0 and len(empty_catches) == 0

xaml_path = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\BeamRebar\View\BeamRebarView.xaml"
cs_path = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar\HPRebar\BeamRebar\ViewModel\BeamRebarViewModel.cs"

p1 = check_file_lines(xaml_path, 300)
p2 = check_file_lines(cs_path, 300)
p3 = check_csharp_methods(cs_path, 50)

print(f"OVERALL CLEAN CODE COMPLIANCE: {'PASS' if p1 and p2 and p3 else 'FAIL'}")
