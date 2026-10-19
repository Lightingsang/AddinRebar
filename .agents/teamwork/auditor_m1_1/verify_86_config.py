import os
import re

LSP_PATH = r"C:\kata_pro\Goi lenh.lsp"
BLUEPRINT_PATH = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md"

with open(LSP_PATH, 'r', encoding='utf-8', errors='replace') as f:
    lsp_lines = f.readlines()

# Extract lines 152 to 237 (1-indexed)
# In 0-indexed: index 151 to 236 inclusive (total 86 lines)
raw_config_lines = lsp_lines[151:237]
print(f"Total extracted lines from Goi lenh.lsp: {len(raw_config_lines)}")

with open(BLUEPRINT_PATH, 'r', encoding='utf-8') as f:
    bp_content = f.read()

# Parse the 86 table rows in Section 3 of blueprint
# Table format: | **1** | 152 | `LayerDim` | `kata_dim` | `string` | CAD Layer Name | Dimension lines layer | ...
row_pattern = re.compile(r'\|\s*\*\*(\d+)\*\*\s*\|\s*(\d+)\s*\|\s*`([^`]+)`\s*\|\s*`([^`]*)`\s*\|')
matches = row_pattern.findall(bp_content)
print(f"Total parsed config table rows from blueprint: {len(matches)}")

mismatches = []
for idx, (param_no, line_no, param_name, default_val) in enumerate(matches):
    p_num = int(param_no)
    l_num = int(line_no)
    lsp_idx = l_num - 1
    raw_lsp = lsp_lines[lsp_idx].strip()
    # strip leading semicolon
    expected_val = raw_lsp.lstrip(';').strip()
    
    # check default_val vs expected_val
    # note: some strings like " mép" or empty string ""
    norm_def = default_val.strip('"')
    norm_exp = expected_val.strip('"')
    
    match_status = "OK"
    if norm_def != norm_exp and default_val != expected_val:
        # Check if equivalent (e.g. 1/25 vs 1/25, or empty string)
        if norm_def == "" and norm_exp == "":
            pass
        elif default_val == '""' and norm_exp == "":
            pass
        elif norm_def.lower() == norm_exp.lower():
            pass
        else:
            match_status = f"DIFF: bp='{default_val}' vs lsp='{expected_val}'"
            mismatches.append((p_num, l_num, param_name, default_val, expected_val))
    
    # print first 10 and last 5
    if idx < 5 or idx >= 80:
        print(f"Item {p_num:02d} (Line {l_num}): name={param_name}, bp_val='{default_val}', lsp='{raw_lsp}' -> {match_status}")

print(f"\nTotal value mismatches: {len(mismatches)}")
for m in mismatches:
    print(f"  Item {m[0]} Line {m[1]} {m[2]}: bp='{m[3]}' vs lsp='{m[4]}'")
