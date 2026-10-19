import os
import re

production_dirs = [
    r'HPKata\HPKata.Core',
    r'HPKata\HPKata.UI',
    r'HPKata\HPKata.AutoCad',
    r'HPKata\HPKata.Revit'
]

methods = []
for pdir in production_dirs:
    for root, dirs, files in os.walk(pdir):
        if any(x in root for x in ['bin', 'obj', '.git']):
            continue
        for f in files:
            if not f.endswith('.cs'):
                continue
            path = os.path.join(root, f)
            with open(path, 'r', encoding='utf-8', errors='ignore') as fp:
                lines = fp.readlines()
            
            in_method = False
            m_name = ''
            start_line = 0
            brace_depth = 0
            has_opened = False
            depth = 0
            
            for i, line in enumerate(lines, 1):
                clean = re.sub(r'//.*', '', line.strip())
                clean = re.sub(r'"(?:[^"\\]|\\.)*"', '""', clean)
                
                # Check method or constructor signature
                if not in_method and depth >= 1:
                    m = re.search(r'(?:public|private|protected|internal|static|async|override|sealed|virtual|\s)+\s+([\w<>\[\]\?]+)\s+(\w+)\s*\([^;]*\)', clean)
                    c = re.search(r'(?:public|private|protected|internal)\s+(\w+)\s*\([^;]*\)', clean)
                    if (m or c) and not clean.endswith(';'):
                        m_name = m.group(2) if m else c.group(1)
                        start_line = i
                        in_method = True
                        brace_depth = 0
                        has_opened = False
                
                open_c = clean.count('{')
                close_c = clean.count('}')
                
                if in_method:
                    if open_c > 0:
                        has_opened = True
                    brace_depth += (open_c - close_c)
                    if has_opened and brace_depth <= 0:
                        end_line = i
                        length = end_line - start_line + 1
                        methods.append((path, m_name, start_line, end_line, length))
                        in_method = False
                        has_opened = False
                
                depth += (open_c - close_c)

print(f"Total production methods analyzed: {len(methods)}")
long_methods = [m for m in methods if m[4] > 50]
print(f"Production methods > 50 lines: {len(long_methods)}")
for m in sorted(long_methods, key=lambda x: -x[4]):
    print(f"VIOLATION: {m[0]}:{m[2]}-{m[3]} {m[1]} ({m[4]} lines)")

print("\nTop 20 longest production methods:")
for m in sorted(methods, key=lambda x: -x[4])[:20]:
    print(f"  {m[0]}:{m[2]}-{m[3]} {m[1]} ({m[4]} lines)")
