import glob
import os
import re

files = [
    'HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs',
    'HPKata/HPKata.UI.Tests/Views/StaticResourceParityScannerTests.cs'
]

print("=== Clean Code Audit for M4 Files ===")
for f in files:
    lines = open(f, encoding='utf-8').readlines()
    line_count = len(lines)
    print(f"File: {f} -> {line_count} lines (Rule C2 <= 300: {'PASS' if line_count <= 300 else 'FAIL'})")
    
    # Check method line count
    methods = []
    current_method = None
    brace_depth = 0
    method_start = 0
    for idx, l in enumerate(lines, 1):
        # simple heuristic for method declarations
        if re.search(r'(public|private|protected|internal)\s+(static\s+)?(async\s+)?[\w<>[\]?]+\s+\w+\s*\(', l) and ';' not in l:
            current_method = l.strip()
            method_start = idx
        if '{' in l:
            brace_depth += l.count('{')
        if '}' in l:
            brace_depth -= l.count('}')
            if brace_depth == 1 and current_method: # closed method in class
                m_len = idx - method_start + 1
                methods.append((current_method, m_len))
                current_method = None
                
    for m_name, m_len in methods:
        status = 'PASS' if m_len <= 50 else 'FAIL'
        if status == 'FAIL':
            print(f"  Method {m_name}: {m_len} lines ({status})")
    print(f"  Total methods checked: {len(methods)}, all <= 50 lines: {all(m[1] <= 50 for m in methods)}")

    # Check for empty catch without comment
    catches = re.findall(r'catch\s*\([^\)]*\)\s*\{\s*\}', open(f, encoding='utf-8').read())
    print(f"  Empty catches without comments (Rule Q-B2): {len(catches)}")
    
    # Check for dead/commented out code
    commented_code = re.findall(r'//\s*(var|if|for|while|return|throw|Assert|window)\s', open(f, encoding='utf-8').read())
    print(f"  Commented-out code (Rule Q-B1): {len(commented_code)}")
