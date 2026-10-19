import sys

with open('docs/rea-dce-pro/TRUTH_MATRIX.md', 'r', encoding='utf-8') as f:
    lines = f.readlines()

counts = {'HOÀN THÀNH': 0, 'ĐANG LÀ MOCK': 0, 'CHƯA GIẢI MÃ ĐƯỢC': 0}
subsystem = 'None'
subsystem_counts = {}

for line in lines[36:]:
    if line.startswith('## '):
        subsystem = line.strip()
        subsystem_counts[subsystem] = {'HOÀN THÀNH': 0, 'ĐANG LÀ MOCK': 0, 'CHƯA GIẢI MÃ ĐƯỢC': 0}
    for k in counts:
        if f'| {k} |' in line:
            counts[k] += 1
            if subsystem in subsystem_counts:
                subsystem_counts[subsystem][k] += 1

print('Total counts:', counts)
for s, sc in subsystem_counts.items():
    print(s, sc)
