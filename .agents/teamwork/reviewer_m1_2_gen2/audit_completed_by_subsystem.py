import re
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

repo_root = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
matrix_path = repo_root / "docs" / "rea-dce-pro" / "TRUTH_MATRIX.md"
lines = matrix_path.read_text(encoding="utf-8").splitlines()

completed_items = []
current_sub = None
subsystem_re = re.compile(r"^##\s+(\d+)\.\s+Subsystem\s+(\d+):\s+(.*)$")

for i, line in enumerate(lines):
    m = subsystem_re.match(line)
    if m:
        current_sub = int(m.group(2))
    elif line.startswith("## ") and not subsystem_re.match(line):
        current_sub = None
    elif current_sub is not None:
        if line.startswith("|") and not line.startswith("|---") and not line.startswith("| Legacy") and not line.startswith("| Index") and not line.startswith("| Step") and not line.startswith("| Export") and not line.startswith("| Form"):
            cols = [c.strip() for c in line.strip().split("|")[1:-1]]
            if len(cols) >= 4:
                status_col = cols[-1].replace("`", "").strip()
                if "HOÀN THÀNH" in status_col:
                    completed_items.append({
                        "line": i + 1,
                        "subsystem": current_sub,
                        "identifier": cols[0],
                        "inspection": cols[3] if len(cols) > 3 else "",
                        "target_file": cols[4] if len(cols) > 4 else cols[-2],
                        "status": status_col
                    })

print(f"Total HOÀN THÀNH items: {len(completed_items)}\n")

# Group by subsystem
by_sub = {}
for it in completed_items:
    by_sub.setdefault(it["subsystem"], []).append(it)

for sub_id, items in sorted(by_sub.items()):
    print(f"--- Subsystem {sub_id} ({len(items)} items marked HOÀN THÀNH) ---")
    file_map = {}
    for it in items:
        tgt = it["target_file"].replace("`", "").strip().split(":")[0]
        file_map.setdefault(tgt, []).append(it["identifier"])
    
    for f, idents in file_map.items():
        # Check if f exists
        p = repo_root / "HPDcePro" / f
        exists = p.exists()
        print(f"  Target: {f} (Exists: {exists}, count: {len(idents)})")
        if not exists:
            print(f"    MISSING FILE! Identifiers:")
            for id_ in idents[:5]:
                print(f"      - {id_}")
            if len(idents) > 5:
                print(f"      ... and {len(idents)-5} more")
    print()
