import sys
import re

lsp_path = r"C:\kata_pro\Goi lenh.lsp"
blueprint_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md"

# 1. Read LSP lines 152 to 237 (1-indexed)
with open(lsp_path, "r", encoding="utf-8", errors="replace") as f:
    lsp_lines = f.readlines()

lsp_config_lines = []
for idx in range(152, 238): # 152 to 237 inclusive
    raw_line = lsp_lines[idx - 1].rstrip("\r\n")
    # strip leading ;
    val = raw_line
    if val.startswith(";"):
        val = val[1:]
    lsp_config_lines.append((idx, raw_line, val))

print(f"Read {len(lsp_config_lines)} lines from Goi lenh.lsp (expected 86)")

# 2. Parse Section 3 table from Blueprint
with open(blueprint_path, "r", encoding="utf-8") as f:
    bp_content = f.read()

# Find Section 3 table
table_match = re.search(r"### 3\.1 Exhaustive 86-Parameter Specification Table(.*?)(?=### 3\.2|\Z)", bp_content, re.DOTALL)
if not table_match:
    print("ERROR: Could not find Section 3.1 table in Blueprint!")
    sys.exit(1)

table_text = table_match.group(1)
table_lines = [l.strip() for l in table_text.splitlines() if l.strip().startswith("|") and not l.strip().startswith("| #") and not l.strip().startswith("|--")]

print(f"Found {len(table_lines)} table rows in Section 3.1 (expected 86)")

mismatches = []
parsed_rows = []

for i, row in enumerate(table_lines):
    # Split by |
    cols = [c.strip() for c in row.split("|")[1:-1]]
    if len(cols) < 8:
        print(f"Row {i+1} has fewer than 8 columns: {cols}")
        continue
    item_num_raw = cols[0].replace("*", "")
    line_num_raw = cols[1]
    param_name = cols[2].replace("`", "")
    default_val_raw = cols[3].replace("`", "")
    data_type = cols[4]
    unit = cols[5]
    meaning = cols[6]
    backing = cols[7]

    item_num = int(item_num_raw)
    line_num = int(line_num_raw)

    expected_line_num = 152 + i
    expected_item_num = i + 1
    
    lsp_idx, lsp_raw, lsp_val = lsp_config_lines[i]

    # Check indices
    if item_num != expected_item_num:
        mismatches.append(f"Item #{item_num} != expected #{expected_item_num}")
    if line_num != expected_line_num:
        mismatches.append(f"Line #{line_num} != expected #{expected_line_num}")
    if line_num != lsp_idx:
        mismatches.append(f"Line #{line_num} != LSP line #{lsp_idx}")

    # Check default value
    # Note: in markdown table, "" represents empty string. In lsp_val it might be empty string.
    norm_bp_val = default_val_raw
    if norm_bp_val == '""':
        norm_bp_val = ""
    elif norm_bp_val == '1a, 1b...':
        # Let's check lsp line
        pass

    parsed_rows.append({
        "item": item_num,
        "line": line_num,
        "name": param_name,
        "bp_default": default_val_raw,
        "norm_bp": norm_bp_val,
        "lsp_raw": lsp_raw,
        "lsp_val": lsp_val,
        "type": data_type,
        "unit": unit,
        "meaning": meaning,
        "backing": backing
    })

print("\n--- Detailed Comparison Table ---")
for r in parsed_rows:
    bp_v = r["norm_bp"]
    lsp_v = r["lsp_val"]
    match = (bp_v == lsp_v)
    
    # Special cases: e.g. 1a, 1b... vs 1a, 1b, 1c, 1d ...
    status = "OK" if match else "DIFF"
    if not match:
        print(f"[{status}] Item {r['item']} (Line {r['line']}) {r['name']}: BP='{bp_v}' vs LSP='{lsp_v}'")
    else:
        # print first 5 and last 5 for sanity
        if r['item'] <= 5 or r['item'] >= 82:
            print(f"[{status}] Item {r['item']} (Line {r['line']}) {r['name']}: '{bp_v}' == '{lsp_v}'")

print(f"\nTotal rows checked: {len(parsed_rows)}")
diff_count = sum(1 for r in parsed_rows if r["norm_bp"] != r["lsp_val"])
print(f"Exact string matches: {len(parsed_rows) - diff_count}/{len(parsed_rows)}")
print(f"Differences: {diff_count}")
