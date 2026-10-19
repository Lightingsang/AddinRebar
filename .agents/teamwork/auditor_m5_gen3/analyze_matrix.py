import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r'docs/rea-dce-pro/TRUTH_MATRIX.md', 'r', encoding='utf-8') as f:
    text = f.read()

# Let's inspect sections in TRUTH_MATRIX.md
sections = re.split(r'\n##\s+', text)
print(f"Total ## sections: {len(sections)}")

for sec in sections:
    first_line = sec.splitlines()[0]
    # count rows and statuses
    rows = [l for l in sec.splitlines() if '|' in l and not l.strip().startswith('|---')]
    statuses = {}
    for r in rows:
        parts = [p.strip() for p in r.split('|') if p.strip()]
        if len(parts) >= 2:
            st = parts[-1]
            statuses[st] = statuses.get(st, 0) + 1
    print(f"\nSection: {first_line}")
    print(f"  Total rows: {len(rows)}")
    for k, v in statuses.items():
        if k in ['HOÀN THÀNH', 'ĐANG LÀ MOCK', 'CHƯA GIẢI MÃ ĐƯỢC']:
            print(f"    {k}: {v}")
