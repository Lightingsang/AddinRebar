import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

repo_root = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
matrix_path = repo_root / "docs" / "rea-dce-pro" / "TRUTH_MATRIX.md"
lines = matrix_path.read_text(encoding="utf-8").splitlines()

# Subsystem 3 is lines 435 to 490
sub3_lines = lines[434:490]
for idx, l in enumerate(sub3_lines):
    if l.startswith("|") and not l.startswith("|---") and not l.startswith("| Legacy"):
        cols = [c.strip() for c in l.split("|")[1:-1]]
        status = cols[-1].replace("`", "").strip()
        target = cols[-2].replace("`", "").strip()
        print(f"Row {idx}: {cols[0]} | Target: {target} | Status: {status}")
