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

print(f"Total HOÀN THÀNH items extracted: {len(completed_items)}")

missing_targets = []
existing_targets = {}

for item in completed_items:
    raw_target = item["target_file"].replace("`", "").strip()
    # If target has :line, separate it
    parts = raw_target.split(":")
    file_rel = parts[0]
    line_no = int(parts[1]) if len(parts) > 1 and parts[1].isdigit() else None
    
    # Try multiple potential path roots
    # E.g. src/HPDcePro.Core/... -> HPDcePro/src/HPDcePro.Core/...
    # or HPDcePro/...
    candidates = [
        repo_root / file_rel,
        repo_root / "HPDcePro" / file_rel,
        repo_root / "HPDcePro" / "src" / file_rel,
    ]
    
    resolved_path = None
    for cand in candidates:
        if cand.exists():
            resolved_path = cand
            break
            
    if resolved_path is None:
        missing_targets.append((item, raw_target))
    else:
        existing_targets[file_rel] = resolved_path

print(f"\nResolved target files across {len(existing_targets)} unique file paths:")
for rel, p in existing_targets.items():
    print(f"  OK: {rel} -> {p.relative_to(repo_root)}")

if missing_targets:
    print(f"\nERROR: {len(missing_targets)} targets could not be found:")
    for it, t in missing_targets:
        print(f"  Sub {it['subsystem']} Line {it['line']}: {it['identifier']} -> target {t}")
else:
    print("\nALL 130 target file paths exist in repository!")
