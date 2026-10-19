import os, glob, re

def check_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    in_method = False
    method_name = ""
    start_line = 0
    brace_count = 0
    
    # Simple regex for method signature
    method_regex = re.compile(r'^\s*(public|private|protected|internal)\s+(static\s+|async\s+|sealed\s+|override\s+|virtual\s+)*[\w\<\>\[\]\,\?\s]+\s+(\w+)\s*\([^\)]*\)')
    
    for i, line in enumerate(lines):
        line_num = i + 1
        trimmed = line.strip()
        
        if not in_method:
            m = method_regex.search(line)
            if m:
                # check if not interface or abstract
                if ';' in line and '{' not in line and '=>' not in line:
                    continue
                method_name = m.group(3)
                start_line = line_num
                in_method = True
                brace_count = line.count('{') - line.count('}')
                if brace_count == 0 and '{' in line:
                    # single line block
                    in_method = False
                elif '=>' in line and ';' in line:
                    # expression bodied
                    in_method = False
        else:
            brace_count += line.count('{') - line.count('}')
            if brace_count <= 0:
                length = line_num - start_line + 1
                if length > 50:
                    print(f"{os.path.basename(file_path)}: {method_name} at line {start_line}-{line_num} ({length} lines)")
                in_method = False

base = "g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPBim"
for root, dirs, files in os.walk(base):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for file in files:
        if file.endswith(".cs") and not file.endswith(".g.cs") and not file.endswith("AssemblyInfo.cs"):
            check_file(os.path.join(root, file))
