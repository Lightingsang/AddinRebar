import sys, os

sys.stdout.reconfigure(encoding='utf-8')

truth_matrix_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md'

with open(truth_matrix_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

total_ht = 0
total_mock = 0
total_cgm = 0
row_count = 0
subsystems = {}
current_sub = 'Unknown'

first_row_line = None
last_row_line = None

for idx, line in enumerate(lines, 1):
    if line.startswith('## ') and not 'Executive Summary' in line:
        current_sub = line.strip()
    if '|' in line and not line.strip().startswith('|---') and not 'Legacy Identifier' in line and not 'Subsystem Index' in line and not 'Export Symbol' in line and not 'Form / Dialog / Palette' in line and not 'Step Index' in line:
        parts = [p.strip() for p in line.split('|')[1:-1]]
        if len(parts) >= 4:
            status_col = parts[-1]
            if status_col in ['HOÀN THÀNH', 'ĐANG LÀ MOCK', 'CHƯA GIẢI MÃ ĐƯỢC']:
                if first_row_line is None:
                    first_row_line = idx
                last_row_line = idx
                row_count += 1
                if current_sub not in subsystems:
                    subsystems[current_sub] = {'HT': 0, 'MOCK': 0, 'CGM': 0, 'total': 0}
                subsystems[current_sub]['total'] += 1
                if status_col == 'HOÀN THÀNH':
                    total_ht += 1
                    subsystems[current_sub]['HT'] += 1
                elif status_col == 'ĐANG LÀ MOCK':
                    total_mock += 1
                    subsystems[current_sub]['MOCK'] += 1
                elif status_col == 'CHƯA GIẢI MÃ ĐƯỢC':
                    total_cgm += 1
                    subsystems[current_sub]['CGM'] += 1

print(f"First row line: {first_row_line}, Last row line: {last_row_line}")
print(f"Total counted rows: {row_count}")
print(f"Total HOAN THANH: {total_ht}")
print(f"Total DANG LA MOCK: {total_mock}")
print(f"Total CHUA GIAI MA DUOC: {total_cgm}")
print(f"Sum check: {total_ht + total_mock + total_cgm} == {row_count}")

for sub, stats in subsystems.items():
    print(f"  {sub}: Total={stats['total']}, HT={stats['HT']}, MOCK={stats['MOCK']}, CGM={stats['CGM']}")
