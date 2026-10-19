import re
import sys
from pathlib import Path

matrix_path = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md")

if not matrix_path.exists():
    print(f"Error: {matrix_path} does not exist")
    sys.exit(1)

content = matrix_path.read_text(encoding="utf-8")
lines = content.splitlines()

print(f"Loaded TRUTH_MATRIX.md: {len(lines)} lines, {len(content)} characters\n")

# Let's inspect sections
section_headers = []
for i, line in enumerate(lines):
    if line.startswith("## "):
        section_headers.append((i, line))

print("Found sections:")
for idx, header in section_headers:
    print(f"  Line {idx+1}: {header}")

print("\n" + "="*60 + "\n")

# Parse Subsystem sections (from Section 2 to Section 10)
# Subsystem 1: Section 2
# Subsystem 2: Section 3
# Subsystem 3: Section 4
# Subsystem 4: Section 5
# Subsystem 5: Section 6
# Subsystem 6: Section 7
# Subsystem 7: Section 8
# Subsystem 8: Section 9
# Subsystem 9: Section 10

subsystems_stats = {}

current_subsystem = None
subsystem_re = re.compile(r"^##\s+(\d+)\.\s+Subsystem\s+(\d+):\s+(.*)$")

for i, line in enumerate(lines):
    m = subsystem_re.match(line)
    if m:
        sec_num = int(m.group(1))
        sub_num = int(m.group(2))
        name = m.group(3)
        current_subsystem = sub_num
        subsystems_stats[sub_num] = {
            "name": name,
            "section_line": i + 1,
            "rows": [],
            "HOÀN THÀNH": 0,
            "ĐANG LÀ MOCK": 0,
            "CHƯA GIẢI MÃ ĐƯỢC": 0,
            "OTHER": 0,
            "other_statuses": set()
        }
    elif line.startswith("## ") and not subsystem_re.match(line):
        current_subsystem = None
    elif current_subsystem is not None:
        # Check if table row
        # Row format: | col1 | col2 | col3 | ... | Status |
        if line.startswith("|") and not line.startswith("|---") and not line.startswith("| Legacy") and not line.startswith("| Index") and not line.startswith("| Step") and not line.startswith("| Export") and not line.startswith("| Form"):
            cols = [c.strip() for c in line.strip().split("|")[1:-1]]
            if len(cols) >= 4:
                status_col = cols[-1]
                # Sometimes status is wrapped in backticks or whitespace
                clean_status = status_col.replace("`", "").strip()
                subsystems_stats[current_subsystem]["rows"].append((i+1, cols[0], clean_status))
                if "HOÀN THÀNH" in clean_status:
                    subsystems_stats[current_subsystem]["HOÀN THÀNH"] += 1
                elif "ĐANG LÀ MOCK" in clean_status:
                    subsystems_stats[current_subsystem]["ĐANG LÀ MOCK"] += 1
                elif "CHƯA GIẢI MÃ ĐƯỢC" in clean_status:
                    subsystems_stats[current_subsystem]["CHƯA GIẢI MÃ ĐƯỢC"] += 1
                else:
                    subsystems_stats[current_subsystem]["OTHER"] += 1
                    subsystems_stats[current_subsystem]["other_statuses"].add(clean_status)

total_endpoints = 0
total_completed = 0
total_mock = 0
total_undecoded = 0
total_other = 0

print(f"{'Subsystem':<35} | {'Count':<6} | {'Complete':<9} | {'Mock':<6} | {'Undecoded':<10} | {'Other':<6}")
print("-" * 85)

for sub_id in sorted(subsystems_stats.keys()):
    st = subsystems_stats[sub_id]
    cnt = len(st["rows"])
    total_endpoints += cnt
    total_completed += st["HOÀN THÀNH"]
    total_mock += st["ĐANG LÀ MOCK"]
    total_undecoded += st["CHƯA GIẢI MÃ ĐƯỢC"]
    total_other += st["OTHER"]
    print(f"Subsystem {sub_id} ({st['name'][:20]}...) | {cnt:<6} | {st['HOÀN THÀNH']:<9} | {st['ĐANG LÀ MOCK']:<6} | {st['CHƯA GIẢI MÃ ĐƯỢC']:<10} | {st['OTHER']:<6}")
    if st["other_statuses"]:
        print(f"   WARNING: Unexpected statuses: {st['other_statuses']}")

print("-" * 85)
print(f"{'GRAND TOTAL':<35} | {total_endpoints:<6} | {total_completed:<9} | {total_mock:<6} | {total_undecoded:<10} | {total_other:<6}")
print("=" * 85)
