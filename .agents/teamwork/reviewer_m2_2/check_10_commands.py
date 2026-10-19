import os
import re

base_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

lsp_path = os.path.join(base_dir, r"HPKata/HPKata.AutoCad/GoiLenh_HPKata.lsp")
col_cmd_path = os.path.join(base_dir, r"HPKata/HPKata.AutoCad/Commands/ColumnCommands.cs")
wall_cmd_path = os.path.join(base_dir, r"HPKata/HPKata.AutoCad/Commands/WallCommands.cs")

with open(lsp_path, "r", encoding="utf-8") as f:
    lsp_content = f.read()

with open(col_cmd_path, "r", encoding="utf-8") as f:
    col_content = f.read()

with open(wall_cmd_path, "r", encoding="utf-8") as f:
    wall_content = f.read()

expected_commands = [
    ("vc", "ve_cot"),
    ("vcs", "ve_cot_shop"),
    ("raicot", "raicot"),
    ("dimcot", "dimcot"),
    ("tkcot", "tkcot"),
    ("loadcolumn", "loadcolumn"),
    ("vv", "ve_vach"),
    ("vvs", "ve_vach_shop"),
    ("vlv", "ve_loi_vach"),
    ("loadwall", "loadwall")
]

print("=== VERIFYING 10 PHASE 3 AUTOCAD COMMANDS ===")
for alias, target in expected_commands:
    # Check LSP
    lsp_pattern = rf'\(defun\s+c:{alias}\s*\(\s*\)\s*\(command\s+"{target}"\)\s*\)'
    lsp_match = bool(re.search(lsp_pattern, lsp_content))
    
    # Check C#
    target_match = False
    alias_match = False
    combined_content = col_content + "\n" + wall_content
    if f'[CommandMethod("{target}")]' in combined_content:
        target_match = True
    if f'[CommandMethod("{alias}")]' in combined_content:
        alias_match = True
        
    print(f"Command '{alias}' -> '{target}': LSP Defun = {lsp_match}, C# Target = {target_match}, C# Alias = {alias_match}")
