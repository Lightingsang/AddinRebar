import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

autocad_src = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.AutoCad'
all_cmds = []

for root, dirs, files in os.walk(autocad_src):
    for f in files:
        if f.endswith('.cs'):
            fpath = os.path.join(root, f)
            with open(fpath, 'r', encoding='utf-8') as csf:
                lines = csf.readlines()
            for idx, line in enumerate(lines):
                m = re.search(r'\[CommandMethod\(\s*"([^"]+)"\s*\)\]', line)
                if m:
                    rel_path = os.path.relpath(fpath, r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro')
                    all_cmds.append((m.group(1), rel_path.replace('\\', '/'), idx + 1))

print(f'Total [CommandMethod] found in HPDcePro.AutoCad source: {len(all_cmds)}')
cmd_dict = {c[0]: c for c in all_cmds}

# Compare with the 56 ĐANG LÀ MOCK commands in TRUTH_MATRIX.md
matrix_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md'
with open(matrix_path, 'r', encoding='utf-8') as f:
    matrix_content = f.read()

sec1_match = re.search(r'## 2\. Subsystem 1: AutoCAD <CommandMethod> Endpoints.*?\n(.*?)\n## 3\. Subsystem 2:', matrix_content, re.DOTALL)
sec1_text = sec1_match.group(1)
rows1 = [line.strip() for line in sec1_text.split('\n') if line.strip().startswith('|') and not line.strip().startswith('|---') and not 'Legacy Identifier' in line]

mocks_in_matrix = {}
for row in rows1:
    parts = [p.strip() for p in row.split('|')[1:-1]]
    if len(parts) >= 6:
        cmd = parts[0].replace('`', '').strip()
        target = parts[4].replace('`', '').strip()
        status = parts[5].strip()
        if status == 'ĐANG LÀ MOCK':
            mocks_in_matrix[cmd] = target

print(f'Total mocks in TRUTH_MATRIX.md Subsystem 1: {len(mocks_in_matrix)}')

# Are there any commands in HPDcePro.AutoCad that are NOT in mocks_in_matrix?
unaccounted_cmds = [c for c in cmd_dict if c not in mocks_in_matrix]
print(f'Commands in HPDcePro.AutoCad not marked as mock in Subsystem 1: {len(unaccounted_cmds)}')
if unaccounted_cmds:
    print('  Unaccounted commands:', unaccounted_cmds)

# Are there any mocks in TRUTH_MATRIX.md that are NOT in HPDcePro.AutoCad?
missing_in_code = [m for m in mocks_in_matrix if m not in cmd_dict]
print(f'Mocks in TRUTH_MATRIX.md not found in HPDcePro.AutoCad: {len(missing_in_code)}')
if missing_in_code:
    print('  Missing in code:', missing_in_code)

# Check line number accuracy between code and matrix
line_diffs = []
for cmd, target in mocks_in_matrix.items():
    if cmd in cmd_dict:
        code_file = cmd_dict[cmd][1]
        code_line = cmd_dict[cmd][2]
        if ':' in target:
            mat_file, mat_line = target.rsplit(':', 1)
            mat_line = int(mat_line)
            if mat_file != code_file or mat_line != code_line:
                line_diffs.append((cmd, mat_file, mat_line, code_file, code_line))
        else:
            line_diffs.append((cmd, target, 'no line', code_file, code_line))

print(f'Line / file discrepancies: {len(line_diffs)}')
if line_diffs:
    for ld in line_diffs[:10]:
        print('  Diff:', ld)
