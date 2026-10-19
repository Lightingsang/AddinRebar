import re
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

matrix_path = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md")
content = matrix_path.read_text(encoding="utf-8")
lines = content.splitlines()

subsystem_ranges = {}
subsystem_re = re.compile(r"^##\s+(\d+)\.\s+Subsystem\s+(\d+):\s+(.*)$")

current_sub = None

for i, line in enumerate(lines):
    m = subsystem_re.match(line)
    if m:
        if current_sub is not None:
            subsystem_ranges[current_sub]["end"] = i - 1
        current_sub = int(m.group(2))
        subsystem_ranges[current_sub] = {
            "name": m.group(3),
            "start": i,
            "end": len(lines) - 1
        }
    elif line.startswith("## ") and current_sub is not None:
        subsystem_ranges[current_sub]["end"] = i - 1
        current_sub = None

for sub_id, info in subsystem_ranges.items():
    print(f"Subsystem {sub_id}: {info['name']} (Lines {info['start']+1} to {info['end']+1})")
    sub_lines = lines[info['start']:info['end']+1]
    
    header_found = False
    headers = []
    rows = []
    for l in sub_lines:
        if l.startswith("|") and not l.startswith("|---"):
            cols = [c.strip() for c in l.split("|")[1:-1]]
            if not header_found:
                headers = cols
                header_found = True
            else:
                rows.append(cols)
    print(f"  Headers: {headers}")
    print(f"  Total Rows: {len(rows)}")
    
    completed = [r for r in rows if "HOÀN THÀNH" in r[-1]]
    mocked = [r for r in rows if "ĐANG LÀ MOCK" in r[-1]]
    undecoded = [r for r in rows if "CHƯA GIẢI MÃ ĐƯỢC" in r[-1]]
    print(f"  HOÀN THÀNH: {len(completed)}, ĐANG LÀ MOCK: {len(mocked)}, CHƯA GIẢI MÃ ĐƯỢC: {len(undecoded)}")
    
    if sub_id in (2, 3, 4, 5, 6, 9):
        print(f"  Sample HOÀN THÀNH in Subsystem {sub_id}:")
        for r in completed[:3]:
            print(f"    - {r[0]}: {r[-2] if len(r) >= 2 else ''} -> {r[-1]}")
        print(f"  Sample ĐANG LÀ MOCK in Subsystem {sub_id}:")
        for r in mocked[:3]:
            print(f"    - {r[0]}: {r[-2] if len(r) >= 2 else ''} -> {r[-1]}")
        print(f"  Sample CHƯA GIẢI MÃ ĐƯỢC in Subsystem {sub_id}:")
        for r in undecoded[:3]:
            print(f"    - {r[0]}: {r[-2] if len(r) >= 2 else ''} -> {r[-1]}")
    print()
