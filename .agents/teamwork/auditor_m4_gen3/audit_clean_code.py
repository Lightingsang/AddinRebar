import os, sys, re
sys.stdout.reconfigure(encoding='utf-8')

src_roots = [
    r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Revit',
    r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Excel'
]

long_files = []
unannotated_catches = []
commented_out_code = []

for root in src_roots:
    for dirpath, dirnames, filenames in os.walk(root):
        if 'bin' in dirpath.split(os.sep) or 'obj' in dirpath.split(os.sep):
            continue
        for f in filenames:
            if f.endswith('.cs'):
                p = os.path.join(dirpath, f)
                with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                    lines = fp.readlines()
                
                # Check line count
                if len(lines) > 300:
                    long_files.append((p, len(lines)))
                
                # Check catches
                for idx, line in enumerate(lines, 1):
                    if re.search(r'\bcatch\b', line):
                        # check if line or next 2 lines have '// Reason:'
                        context = ''.join(lines[idx-1:min(len(lines), idx+3)])
                        if '// Reason:' not in context and '// reason:' not in context:
                            unannotated_catches.append((p, idx, line.strip()))

print(f'Files > 300 lines in Revit & Excel: {len(long_files)}')
for p, count in long_files:
    print(f'  {p}: {count} lines')

print(f'Unannotated catches in Revit & Excel: {len(unannotated_catches)}')
for p, idx, line in unannotated_catches:
    print(f'  {p}:{idx} -> {line}')
