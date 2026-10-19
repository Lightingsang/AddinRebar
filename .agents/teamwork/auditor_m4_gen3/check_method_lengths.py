import os, sys, re
sys.stdout.reconfigure(encoding='utf-8')

src_roots = [
    r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Revit',
    r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Excel'
]

long_methods = []

for root in src_roots:
    for dirpath, dirnames, filenames in os.walk(root):
        if 'bin' in dirpath.split(os.sep) or 'obj' in dirpath.split(os.sep):
            continue
        for f in filenames:
            if f.endswith('.cs'):
                p = os.path.join(dirpath, f)
                with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                    lines = fp.readlines()
                
                # Approximate method boundary detection
                brace_level = 0
                method_start = None
                method_name = ""
                
                for idx, line in enumerate(lines, 1):
                    stripped = line.strip()
                    # method signature candidate
                    if brace_level == 1 and ('public ' in line or 'private ' in line or 'protected ' in line or 'internal ' in line):
                        if '(' in line and not stripped.startswith('//') and not stripped.startswith('public class ') and not stripped.startswith('public sealed class ') and not stripped.startswith('public interface '):
                            method_name = stripped[:60]
                            method_start = idx
                    
                    brace_level += line.count('{') - line.count('}')
                    if brace_level == 1 and method_start is not None:
                        method_len = idx - method_start + 1
                        if method_len > 50:
                            long_methods.append((p, method_start, method_name, method_len))
                        method_start = None

print(f'Methods > 50 lines in Revit & Excel: {len(long_methods)}')
for p, idx, name, count in long_methods:
    print(f'  {p}:{idx} -> {name} ({count} lines)')
