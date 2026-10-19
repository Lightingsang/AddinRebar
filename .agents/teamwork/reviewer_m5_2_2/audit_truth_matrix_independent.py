import os
import re
import sys

TRUTH_MATRIX_PATH = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md"
REPO_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

with open(TRUTH_MATRIX_PATH, "r", encoding="utf-8") as f:
    lines = f.readlines()

print(f"Total lines in TRUTH_MATRIX.md: {len(lines)}")

# Parse summary table around lines 20-35
summary_rows = {}
for i, line in enumerate(lines[19:35], start=20):
    line_str = line.strip()
    if "|" in line_str and not line_str.startswith("|---") and not "Subsystem Index" in line_str:
        parts = [p.strip() for p in line_str.split("|")[1:-1]]
        if len(parts) >= 6:
            sub_id = parts[0]
            name = parts[1]
            clean_val = lambda s: int(s.replace('*', '').strip())
            total = clean_val(parts[2])
            ht = clean_val(parts[3])
            mock = clean_val(parts[4])
            cgm = clean_val(parts[5])
            summary_rows[sub_id] = {
                "name": name, "total": total, "ht": ht, "mock": mock, "cgm": cgm
            }
            print(f"Summary Table: {sub_id} | Total={total} | HT={ht} | MOCK={mock} | CGM={cgm}")

# Expected breakdown:
expected_subsystems = {
    1: {"total": 311, "ht": 64, "mock": 0, "cgm": 247},
    2: {"total": 73, "ht": 73, "mock": 0, "cgm": 0},
    3: {"total": 49, "ht": 20, "mock": 0, "cgm": 29},
    4: {"total": 29, "ht": 13, "mock": 0, "cgm": 16},
    5: {"total": 69, "ht": 65, "mock": 0, "cgm": 4},
    6: {"total": 24, "ht": 24, "mock": 0, "cgm": 0},
    7: {"total": 11, "ht": 11, "mock": 0, "cgm": 0},
    8: {"total": 12, "ht": 12, "mock": 0, "cgm": 0},
    9: {"total": 24, "ht": 24, "mock": 0, "cgm": 0},
}

# Now parse rows across lines 37 to end
current_sub = None
subsystem_counts = {i: {"total": 0, "ht": 0, "mock": 0, "cgm": 0, "rows": []} for i in range(1, 10)}
ht_target_files = []

sub_header_pattern = re.compile(r"^##\s+(\d+)\.\s+Subsystem\s+(\d+):", re.IGNORECASE)

for idx, line in enumerate(lines):
    line_num = idx + 1
    if line_num > 703:
        break
    if line_num < 37:
        continue
    match = sub_header_pattern.match(line.strip())
    if match:
        current_sub = int(match.group(2))
        continue
    
    if current_sub and current_sub in subsystem_counts:
        stripped = line.strip()
        if stripped.startswith("|") and not stripped.startswith("|---") and not "Legacy Identifier" in stripped and not "Export Symbol" in stripped:
            cols = [c.strip() for c in stripped.split("|")[1:-1]]
            if len(cols) >= 5:
                status = cols[-1]
                # In some tables cols is 6 or 7, status is always the last column
                status_clean = status.strip()
                target_file_col = cols[-2] if len(cols) >= 6 else cols[-1]
                
                subsystem_counts[current_sub]["total"] += 1
                if "HOÀN THÀNH" in status_clean:
                    subsystem_counts[current_sub]["ht"] += 1
                    # Extract target file path
                    # File path might be `src/HPDcePro...` or `src/...:line`
                    file_match = re.search(r'`([^`:]+)(?::\d+)?`', target_file_col)
                    if file_match:
                        raw_path = file_match.group(1).strip()
                    else:
                        raw_path = target_file_col.replace('`', '').strip()
                        if ":" in raw_path:
                            raw_path = raw_path.split(":")[0].strip()
                    ht_target_files.append((line_num, cols[0], raw_path))
                elif "ĐANG LÀ MOCK" in status_clean:
                    subsystem_counts[current_sub]["mock"] += 1
                elif "CHƯA GIẢI MÃ ĐƯỢC" in status_clean:
                    subsystem_counts[current_sub]["cgm"] += 1
                else:
                    print(f"UNKNOWN STATUS at line {line_num}: {status_clean}")

print("\n--- ACTUAL ROW COUNTS AUDIT ---")
total_endpoints = 0
total_ht = 0
total_mock = 0
total_cgm = 0

all_passed = True
for sub_id in range(1, 10):
    c = subsystem_counts[sub_id]
    exp = expected_subsystems[sub_id]
    total_endpoints += c["total"]
    total_ht += c["ht"]
    total_mock += c["mock"]
    total_cgm += c["cgm"]
    
    match_total = c["total"] == exp["total"]
    match_ht = c["ht"] == exp["ht"]
    match_mock = c["mock"] == exp["mock"]
    match_cgm = c["cgm"] == exp["cgm"]
    sub_ok = match_total and match_ht and match_mock and match_cgm
    if not sub_ok:
        all_passed = False
    print(f"Subsystem {sub_id}: Total={c['total']} (exp {exp['total']}) | HT={c['ht']} (exp {exp['ht']}) | MOCK={c['mock']} (exp {exp['mock']}) | CGM={c['cgm']} (exp {exp['cgm']}) -> {'PASS' if sub_ok else 'FAIL'}")

print(f"\nGRAND TOTAL: Total={total_endpoints} (exp 602) | HT={total_ht} (exp 306) | MOCK={total_mock} (exp 0) | CGM={total_cgm} (exp 296)")
arithmetic_ok = (total_ht + total_mock + total_cgm == total_endpoints) and (total_endpoints == 602) and (total_ht == 306) and (total_mock == 0) and (total_cgm == 296)
print(f"Arithmetic Identity Check: 306 + 0 + 296 = 602 -> {'PASS' if arithmetic_ok else 'FAIL'}")

# Verify target files physical presence on disk
print(f"\n--- TARGET FILES DISK PRESENCE AUDIT ({len(ht_target_files)} entries) ---")
missing_files = []
unique_target_paths = set()

for line_num, ident, rel_path in ht_target_files:
    # Resolve relative path: if relative to HPDcePro or repo root
    # e.g. src/HPDcePro... -> G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\...
    # or HPDcePro/src/...
    possible_paths = [
        os.path.join(REPO_ROOT, rel_path),
        os.path.join(REPO_ROOT, "HPDcePro", rel_path),
        os.path.join(REPO_ROOT, "HPDcePro", rel_path.replace("HPDcePro/", "")),
    ]
    found = False
    resolved = None
    for p in possible_paths:
        if os.path.exists(p):
            found = True
            resolved = p
            break
    if not found:
        missing_files.append((line_num, ident, rel_path))
    else:
        unique_target_paths.add(resolved)

print(f"Unique target files resolved on disk: {len(unique_target_paths)}")
print(f"Missing target files for HOÀN THÀNH endpoints: {len(missing_files)}")
if missing_files:
    for mf in missing_files[:10]:
        print(f"  MISSING: line {mf[0]} | {mf[1]} -> {mf[2]}")
else:
    print("[+] ALL 306 HOÀN THÀNH ENDPOINTS HAVE VERIFIED TARGET FILES ON DISK!")

if all_passed and arithmetic_ok and len(missing_files) == 0:
    print("\n>>> INDEPENDENT AUDIT VERDICT: 100% PASS <<<")
else:
    print("\n>>> INDEPENDENT AUDIT VERDICT: FAIL <<<")
