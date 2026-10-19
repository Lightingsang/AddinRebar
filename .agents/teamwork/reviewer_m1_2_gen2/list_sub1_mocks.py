import sys
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

repo_root = Path(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
matrix_path = repo_root / "docs" / "rea-dce-pro" / "TRUTH_MATRIX.md"
lines = matrix_path.read_text(encoding="utf-8").splitlines()

# Subsystem 1 is lines 37 to 354
sub1_lines = lines[36:354]
mock_cmds = []
for idx, l in enumerate(sub1_lines):
    if l.startswith("|") and not l.startswith("|---") and not l.startswith("| Legacy"):
        cols = [c.strip() for c in l.split("|")[1:-1]]
        status = cols[-1].replace("`", "").strip()
        target = cols[-2].replace("`", "").strip()
        if "ĐANG LÀ MOCK" in status:
            mock_cmds.append((cols[0], target))

print(f"Subsystem 1 ĐANG LÀ MOCK count: {len(mock_cmds)}")
for idx, (cmd, tgt) in enumerate(mock_cmds):
    print(f"  {idx+1:2d}. {cmd:<30} -> {tgt}")
