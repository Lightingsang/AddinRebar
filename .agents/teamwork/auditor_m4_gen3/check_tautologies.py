import os, sys, re
sys.stdout.reconfigure(encoding='utf-8')

test_root = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests'
suspicious = []

tautology_patterns = [
    r'Assert\.True\(\s*true\s*\)',
    r'Assert\.False\(\s*false\s*\)',
    r'Assert\.Equal\(\s*([0-9a-zA-Z_]+)\s*,\s*\1\s*\)',
    r'Assert\.Same\(\s*([0-9a-zA-Z_]+)\s*,\s*\1\s*\)'
]

for dirpath, dirnames, filenames in os.walk(test_root):
    if 'bin' in dirpath.split(os.sep) or 'obj' in dirpath.split(os.sep):
        continue
    for f in filenames:
        if f.endswith('.cs'):
            p = os.path.join(dirpath, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                for line_no, line in enumerate(fp, 1):
                    for pat in tautology_patterns:
                        if re.search(pat, line, re.I):
                            suspicious.append((p, line_no, line.strip()))

print(f'Total suspicious tautological assertions found: {len(suspicious)}')
for p, line_no, line in suspicious:
    print(f'{p}:{line_no} -> {line}')
