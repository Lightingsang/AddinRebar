import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

repo_root = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
matrix_path = repo_root / "docs" / "rea-dce-pro" / "TRUTH_MATRIX.md"
lines = matrix_path.read_text(encoding="utf-8").splitlines()

# Subsystem 5 is lines 527 to 602
sub5_lines = lines[526:602]
missing_targets = []
for idx, l in enumerate(sub5_lines):
    if l.startswith("|") and not l.startswith("|---") and not l.startswith("| Export"):
        cols = [c.strip() for c in l.split("|")[1:-1]]
        status = cols[-1].replace("`", "").strip()
        target = cols[-2].replace("`", "").strip()
        target_clean = target.split(":")[0]
        p = repo_root / "HPDcePro" / target_clean
        if not p.exists():
            missing_targets.append((cols[0], target, status))

print(f"Subsystem 5 missing targets count: {len(missing_targets)}")
for m in missing_targets:
    print(f"  Missing: {m}")
