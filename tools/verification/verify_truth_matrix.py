#!/usr/bin/env python3
"""
Adversarial Verification Suite for Master Truth Matrix (docs/rea-dce-pro/TRUTH_MATRIX.md)
Validates markdown table formatting, cross-verifies all 9 subsystems against authoritative ground-truth catalogs,
extracts PE exports from C:\\Dce Pro\\DceAnalyssiss64.dll, and checks token integrity and status counts.
"""

import os
import sys
import json
import re
import struct

# Force UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

TRUTH_MATRIX_PATH = r"docs/rea-dce-pro/TRUTH_MATRIX.md"
DANHBA_LENH_PATH = r"C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json"
DANHBA_EXCEL_PATH = r"C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json"
DANHBA_THEP_PATH = r"C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json"
DANHBA_MBKC_PATH = r"C:\Dce Pro\AiAgent\DanhBa\danhba_mbkc.json"
NATIVE_DLL_PATH = r"C:\Dce Pro\DceAnalyssiss64.dll"

def parse_pe_exports(filepath):
    """Extract PE32+ export directory: (ordinal, name, rva_hex)"""
    with open(filepath, 'rb') as f:
        data = f.read()
    
    e_lfanew = struct.unpack_from('<I', data, 0x3C)[0]
    pe_sig = data[e_lfanew:e_lfanew+4]
    if pe_sig != b'PE\0\0':
        raise ValueError("Invalid PE signature")
    
    coff = e_lfanew + 4
    num_sections = struct.unpack_from('<H', data, coff + 2)[0]
    opt_size = struct.unpack_from('<H', data, coff + 16)[0]
    
    opt = coff + 20
    magic = struct.unpack_from('<H', data, opt)[0]
    is_64 = (magic == 0x20b)
    
    data_dirs_offset = opt + (112 if is_64 else 96)
    export_rva, export_size = struct.unpack_from('<II', data, data_dirs_offset)
    if export_rva == 0:
        return []
        
    sec_header = opt + opt_size
    sections = []
    for i in range(num_sections):
        s_data = data[sec_header + i*40 : sec_header + (i+1)*40]
        s_name = s_data[:8].rstrip(b'\0').decode('latin1', errors='replace')
        vsize, vrva, raw_size, raw_ptr = struct.unpack_from('<IIII', s_data, 8)
        sections.append((s_name, vrva, vsize, raw_ptr, raw_size))
        
    def rva_to_offset(rva):
        for name, vrva, vsize, raw_ptr, raw_size in sections:
            if vrva <= rva < vrva + max(vsize, raw_size):
                return raw_ptr + (rva - vrva)
        return None

    exp_off = rva_to_offset(export_rva)
    if not exp_off:
        return []
    
    base = struct.unpack_from('<I', data, exp_off + 16)[0]
    num_funcs = struct.unpack_from('<I', data, exp_off + 20)[0]
    num_names = struct.unpack_from('<I', data, exp_off + 24)[0]
    addr_funcs = rva_to_offset(struct.unpack_from('<I', data, exp_off + 28)[0])
    addr_names = rva_to_offset(struct.unpack_from('<I', data, exp_off + 32)[0])
    addr_ords = rva_to_offset(struct.unpack_from('<I', data, exp_off + 36)[0])
    
    func_rvas = [struct.unpack_from('<I', data, addr_funcs + i*4)[0] for i in range(num_funcs)]
    
    exports = []
    for i in range(num_names):
        name_rva = struct.unpack_from('<I', data, addr_names + i*4)[0]
        name_off = rva_to_offset(name_rva)
        name_end = data.find(b'\0', name_off)
        name = data[name_off:name_end].decode('ascii')
        
        ord_idx = struct.unpack_from('<H', data, addr_ords + i*2)[0]
        rva = func_rvas[ord_idx]
        ordinal = base + ord_idx
        exports.append({
            'ordinal': ordinal,
            'name': name,
            'rva': f"0x{rva:X}",
            'rva_int': rva
        })
    return exports

def clean_cell(cell):
    text = cell.strip()
    return text

def parse_identifier(raw_cell):
    """
    Extract base identifier from cell.
    Handles:
      `2DXCLIP`
      `APIAUTOCAD`<br>`HostRibbon.Dispatch` -> primary: APIAUTOCAD
      `MatTienThep.PhienBan`
      `VeMBKC_JIG`
      `AutoStrStep01`
    """
    # extract all backticked code tokens
    backtick_tokens = re.findall(r'`([^`]+)`', raw_cell)
    if backtick_tokens:
        primary = backtick_tokens[0].strip()
        all_tokens = [t.strip() for t in backtick_tokens]
        return primary, all_tokens
    # fallback if no backticks
    clean = raw_cell.strip()
    return clean, [clean]

def parse_truth_matrix(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
        
    subsystems = {}
    current_subsystem = None
    subsystem_headers = {}
    summary_stats = []
    formatting_defects = []

    subsystem_re = re.compile(r'^##\s+(\d+)\.\s+Subsystem\s+(\d+):\s+(.*?)\s+\((\d+)\s+.*?\)', re.IGNORECASE)
    summary_re = re.compile(r'^###\s+Summary Statistics Table', re.IGNORECASE)
    
    in_summary_table = False
    in_subsystem_table = False
    expected_cols = 0
    header_col_names = []

    for line_idx, line in enumerate(lines):
        line_num = line_idx + 1
        stripped = line.strip()

        # Check section headers
        sub_match = subsystem_re.match(stripped)
        if sub_match:
            sec_num = int(sub_match.group(1))
            sub_id = int(sub_match.group(2))
            sub_name = sub_match.group(3).strip()
            claimed_count = int(sub_match.group(4))
            current_subsystem = sub_id
            subsystems[sub_id] = {
                'id': sub_id,
                'name': sub_name,
                'claimed_count': claimed_count,
                'rows': [],
                'header_line': line_num
            }
            in_subsystem_table = False
            in_summary_table = False
            continue

        if summary_re.match(stripped):
            in_summary_table = True
            current_subsystem = None
            in_subsystem_table = False
            continue

        if stripped.startswith('## ') and not sub_match:
            # Another major section
            current_subsystem = None
            in_subsystem_table = False
            in_summary_table = False
            continue

        # Check table lines
        if stripped.startswith('|') and stripped.endswith('|'):
            # Table row
            # Check separator
            if re.match(r'^\|(?:\s*:?-+:?\s*\|)+$', stripped):
                # Separator line
                continue
            
            cells = [c.strip() for c in stripped.split('|')[1:-1]]
            
            if in_summary_table:
                if 'Subsystem Index' in cells[0]:
                    # Header
                    continue
                # If separator or blank reached earlier, summary table continues until non-table line
                summary_stats.append({
                    'line': line_num,
                    'cells': cells
                })
                continue

            if current_subsystem is not None:
                if 'Legacy Identifier' in cells[0]:
                    # Table header
                    in_subsystem_table = True
                    expected_cols = len(cells)
                    header_col_names = cells
                    subsystem_headers[current_subsystem] = header_col_names
                    continue
                
                if in_subsystem_table:
                    # Check column count
                    if len(cells) != expected_cols:
                        formatting_defects.append({
                            'line': line_num,
                            'subsystem': current_subsystem,
                            'defect': f"Column count mismatch: expected {expected_cols}, got {len(cells)}",
                            'raw': stripped
                        })
                    
                    # Unpack expected 6 columns
                    # | Legacy Identifier | Subsystem | Token/RVA | Inspection Result | HPDcePro Target File | Parity Status |
                    raw_ident = cells[0]
                    ident_primary, ident_all = parse_identifier(raw_ident)
                    sub_col = clean_cell(cells[1]) if len(cells) > 1 else ''
                    token_rva = clean_cell(cells[2]) if len(cells) > 2 else ''
                    inspection = clean_cell(cells[3]) if len(cells) > 3 else ''
                    target_file = clean_cell(cells[4]) if len(cells) > 4 else ''
                    status = clean_cell(cells[5]) if len(cells) > 5 else ''

                    # Validate status value
                    valid_statuses = {'HOÀN THÀNH', 'ĐANG LÀ MOCK', 'CHƯA GIẢI MÃ ĐƯỢC'}
                    # Handle bold or markdown markers in status
                    status_clean = status.replace('**', '').replace('`', '').strip()
                    if status_clean not in valid_statuses:
                        formatting_defects.append({
                            'line': line_num,
                            'subsystem': current_subsystem,
                            'defect': f"Invalid status: '{status}' (expected one of {valid_statuses})",
                            'raw': stripped
                        })

                    subsystems[current_subsystem]['rows'].append({
                        'line': line_num,
                        'identifier': ident_primary,
                        'all_identifiers': ident_all,
                        'raw_identifier': raw_ident,
                        'subsystem_col': sub_col,
                        'token_rva': token_rva,
                        'inspection': inspection,
                        'target_file': target_file,
                        'status': status_clean,
                        'raw_status': status,
                        'cells': cells
                    })
        else:
            # Not a table line
            if in_summary_table and stripped == '---':
                in_summary_table = False

    return subsystems, summary_stats, formatting_defects

def run_adversarial_verification():
    print("=" * 80)
    print("ADVERSARIAL VERIFICATION: docs/rea-dce-pro/TRUTH_MATRIX.md")
    print("=" * 80)

    # 1. Parse TRUTH_MATRIX.md
    subsystems, summary_stats, formatting_defects = parse_truth_matrix(TRUTH_MATRIX_PATH)
    print(f"\n[+] Parsed {len(subsystems)} subsystems from TRUTH_MATRIX.md")
    print(f"[+] Total markdown formatting defects detected during initial scan: {len(formatting_defects)}")
    for d in formatting_defects:
        print(f"    - Line {d['line']} (Subsystem {d.get('subsystem')}): {d['defect']}")

    # 2. Check Summary Statistics Table consistency
    print("\n--- CHECKING SUMMARY STATISTICS TABLE (SECTION 1) ---")
    summary_errors = []
    total_endpoints_counted = 0
    total_hoan_thanh = 0
    total_mock = 0
    total_chua_giai_ma = 0

    for sub_id in range(1, 10):
        if sub_id not in subsystems:
            summary_errors.append(f"Subsystem {sub_id} missing completely from document!")
            continue
        
        sub = subsystems[sub_id]
        actual_rows = len(sub['rows'])
        claimed_count = sub['claimed_count']
        
        # Count actual statuses
        ht_count = sum(1 for r in sub['rows'] if r['status'] == 'HOÀN THÀNH')
        mock_count = sum(1 for r in sub['rows'] if r['status'] == 'ĐANG LÀ MOCK')
        cgm_count = sum(1 for r in sub['rows'] if r['status'] == 'CHƯA GIẢI MÃ ĐƯỢC')
        
        total_endpoints_counted += actual_rows
        total_hoan_thanh += ht_count
        total_mock += mock_count
        total_chua_giai_ma += cgm_count

        if actual_rows != claimed_count:
            summary_errors.append(f"Subsystem {sub_id} ({sub['name']}): Header claims {claimed_count} entries, but table has {actual_rows} rows!")

        print(f"Subsystem {sub_id:2d}: {sub['name']:<35} | Claimed: {claimed_count:3d} | Actual: {actual_rows:3d} | HT: {ht_count:3d} | MOCK: {mock_count:3d} | CGM: {cgm_count:3d}")

    # Cross-check Section 1 Summary Statistics Table cells against counted rows
    for srow in summary_stats:
        cells = [c.replace('**', '').strip() for c in srow['cells']]
        if len(cells) < 6:
            continue
        first_cell = cells[0]
        sub_m = re.search(r'Subsystem\s+(\d+)', first_cell)
        if sub_m:
            sid = int(sub_m.group(1))
            if sid in subsystems:
                s_obj = subsystems[sid]
                actual_cnt = len(s_obj['rows'])
                ht_cnt = sum(1 for r in s_obj['rows'] if r['status'] == 'HOÀN THÀNH')
                mock_cnt = sum(1 for r in s_obj['rows'] if r['status'] == 'ĐANG LÀ MOCK')
                cgm_cnt = sum(1 for r in s_obj['rows'] if r['status'] == 'CHƯA GIẢI MÃ ĐƯỢC')

                c_total = int(cells[2])
                c_ht = int(cells[3])
                c_mock = int(cells[4])
                c_cgm = int(cells[5])

                if c_total != actual_cnt:
                    summary_errors.append(f"Summary Table Subsystem {sid}: Total mismatch (table claims {c_total} != counted {actual_cnt})")
                if c_ht != ht_cnt:
                    summary_errors.append(f"Summary Table Subsystem {sid}: HOÀN THÀNH mismatch (table claims {c_ht} != counted {ht_cnt})")
                if c_mock != mock_cnt:
                    summary_errors.append(f"Summary Table Subsystem {sid}: ĐANG LÀ MOCK mismatch (table claims {c_mock} != counted {mock_cnt})")
                if c_cgm != cgm_cnt:
                    summary_errors.append(f"Summary Table Subsystem {sid}: CHƯA GIẢI MÃ ĐƯỢC mismatch (table claims {c_cgm} != counted {cgm_cnt})")
        elif 'Grand Total' in first_cell:
            c_total = int(cells[2])
            c_ht = int(cells[3])
            c_mock = int(cells[4])
            c_cgm = int(cells[5])

            if c_total != total_endpoints_counted:
                summary_errors.append(f"Grand Total mismatch (table claims {c_total} != counted {total_endpoints_counted})")
            if c_ht != total_hoan_thanh:
                summary_errors.append(f"Grand Total HOÀN THÀNH mismatch (table claims {c_ht} != counted {total_hoan_thanh})")
            if c_mock != total_mock:
                summary_errors.append(f"Grand Total ĐANG LÀ MOCK mismatch (table claims {c_mock} != counted {total_mock})")
            if c_cgm != total_chua_giai_ma:
                summary_errors.append(f"Grand Total CHƯA GIẢI MÃ ĐƯỢC mismatch (table claims {c_cgm} != counted {total_chua_giai_ma})")

    print(f"\nTOTAL ALL SUBSYSTEMS: Actual Rows={total_endpoints_counted} | HT={total_hoan_thanh} | MOCK={total_mock} | CGM={total_chua_giai_ma}")
    if summary_errors:
        print("[!] SUMMARY DISCREPANCIES FOUND:")
        for err in summary_errors:
            print(f"    - {err}")
    else:
        print("[+] Summary table and header counts match actual row counts 100%.")

    # 3. Check for duplicates in each subsystem
    print("\n--- CHECKING FOR DUPLICATE ENTRIES WITHIN SUBSYSTEMS ---")
    duplicate_errors = []
    for sub_id, sub in subsystems.items():
        seen = {}
        for row in sub['rows']:
            ident = row['identifier']
            if ident in seen:
                duplicate_errors.append({
                    'subsystem': sub_id,
                    'identifier': ident,
                    'line1': seen[ident]['line'],
                    'line2': row['line']
                })
            else:
                seen[ident] = row
    
    if duplicate_errors:
        print(f"[!] FOUND {len(duplicate_errors)} DUPLICATE IDENTIFIERS:")
        for dup in duplicate_errors:
            print(f"    - Subsystem {dup['subsystem']}: Identifier '{dup['identifier']}' on Line {dup['line1']} and Line {dup['line2']}")
    else:
        print("[+] ZERO duplicate identifiers found across all 9 subsystems.")

    # 4. Cross-Verification: Subsystem 1 vs danhba_lenh.json
    print("\n--- SUBSYSTEM 1 CROSS-VERIFICATION: AutoCAD Commands (311 entries) ---")
    with open(DANHBA_LENH_PATH, 'r', encoding='utf-8') as f:
        lenh_data = json.load(f)
    catalog_lenh = [m['ten'] for m in lenh_data['muc']]
    catalog_lenh_set = set(catalog_lenh)
    
    matrix_sub1 = [r['identifier'] for r in subsystems[1]['rows']]
    matrix_sub1_set = set(matrix_sub1)

    missing_in_matrix_sub1 = catalog_lenh_set - matrix_sub1_set
    extra_in_matrix_sub1 = matrix_sub1_set - catalog_lenh_set

    print(f"Catalog entries count: {len(catalog_lenh)} (Unique: {len(catalog_lenh_set)})")
    print(f"Matrix rows count:    {len(matrix_sub1)} (Unique: {len(matrix_sub1_set)})")
    if missing_in_matrix_sub1:
        print(f"[!] Missing in TRUTH_MATRIX ({len(missing_in_matrix_sub1)}): {missing_in_matrix_sub1}")
    if extra_in_matrix_sub1:
        print(f"[!] Extra in TRUTH_MATRIX ({len(extra_in_matrix_sub1)}): {extra_in_matrix_sub1}")
    if not missing_in_matrix_sub1 and not extra_in_matrix_sub1:
        print("[+] 100% Bijection: All 311 commands in danhba_lenh.json match Subsystem 1 exactly.")

    # 5. Cross-Verification: Subsystem 2 vs danhba_dce_excel.json
    print("\n--- SUBSYSTEM 2 CROSS-VERIFICATION: Excel Ribbon Actions (73 entries) ---")
    with open(DANHBA_EXCEL_PATH, 'r', encoding='utf-8') as f:
        excel_data = json.load(f)
    catalog_excel = [m['ten'] for m in excel_data['muc']]
    catalog_excel_set = set(catalog_excel)

    matrix_sub2 = [r['identifier'] for r in subsystems[2]['rows']]
    matrix_sub2_set = set(matrix_sub2)

    missing_in_matrix_sub2 = catalog_excel_set - matrix_sub2_set
    extra_in_matrix_sub2 = matrix_sub2_set - catalog_excel_set

    print(f"Catalog entries count: {len(catalog_excel)} (Unique: {len(catalog_excel_set)})")
    print(f"Matrix rows count:    {len(matrix_sub2)} (Unique: {len(matrix_sub2_set)})")
    if missing_in_matrix_sub2:
        print(f"[!] Missing in TRUTH_MATRIX ({len(missing_in_matrix_sub2)}): {missing_in_matrix_sub2}")
    if extra_in_matrix_sub2:
        print(f"[!] Extra in TRUTH_MATRIX ({len(extra_in_matrix_sub2)}): {extra_in_matrix_sub2}")
    if not missing_in_matrix_sub2 and not extra_in_matrix_sub2:
        print("[+] 100% Bijection: All 73 commands in danhba_dce_excel.json match Subsystem 2 exactly.")

    # 6. Cross-Verification: Subsystem 3 vs danhba_dce_thep.json
    print("\n--- SUBSYSTEM 3 CROSS-VERIFICATION: DceSteel Detailing (49 entries) ---")
    with open(DANHBA_THEP_PATH, 'r', encoding='utf-8') as f:
        thep_data = json.load(f)
    catalog_thep = [m['ten'] for m in thep_data['muc']]
    catalog_thep_set = set(catalog_thep)

    matrix_sub3 = [r['identifier'] for r in subsystems[3]['rows']]
    matrix_sub3_set = set(matrix_sub3)

    missing_in_matrix_sub3 = catalog_thep_set - matrix_sub3_set
    extra_in_matrix_sub3 = matrix_sub3_set - catalog_thep_set

    print(f"Catalog entries count: {len(catalog_thep)} (Unique: {len(catalog_thep_set)})")
    print(f"Matrix rows count:    {len(matrix_sub3)} (Unique: {len(matrix_sub3_set)})")
    if missing_in_matrix_sub3:
        print(f"[!] Missing in TRUTH_MATRIX ({len(missing_in_matrix_sub3)}): {missing_in_matrix_sub3}")
    if extra_in_matrix_sub3:
        print(f"[!] Extra in TRUTH_MATRIX ({len(extra_in_matrix_sub3)}): {extra_in_matrix_sub3}")
    if not missing_in_matrix_sub3 and not extra_in_matrix_sub3:
        print("[+] 100% Bijection: All 49 functions in danhba_dce_thep.json match Subsystem 3 exactly.")

    # 7. Cross-Verification: Subsystem 4 vs danhba_mbkc.json
    print("\n--- SUBSYSTEM 4 CROSS-VERIFICATION: MBKC Framing (29 entries) ---")
    with open(DANHBA_MBKC_PATH, 'r', encoding='utf-8') as f:
        mbkc_data = json.load(f)
    catalog_mbkc = [m['ten'] for m in mbkc_data['muc']]
    catalog_mbkc_set = set(catalog_mbkc)

    matrix_sub4 = [r['identifier'] for r in subsystems[4]['rows']]
    matrix_sub4_set = set(matrix_sub4)

    missing_in_matrix_sub4 = catalog_mbkc_set - matrix_sub4_set
    extra_in_matrix_sub4 = matrix_sub4_set - catalog_mbkc_set

    print(f"Catalog entries count: {len(catalog_mbkc)} (Unique: {len(catalog_mbkc_set)})")
    print(f"Matrix rows count:    {len(matrix_sub4)} (Unique: {len(matrix_sub4_set)})")
    if missing_in_matrix_sub4:
        print(f"[!] Missing in TRUTH_MATRIX ({len(missing_in_matrix_sub4)}): {missing_in_matrix_sub4}")
    if extra_in_matrix_sub4:
        print(f"[!] Extra in TRUTH_MATRIX ({len(extra_in_matrix_sub4)}): {extra_in_matrix_sub4}")
    if not missing_in_matrix_sub4 and not extra_in_matrix_sub4:
        print("[+] 100% Bijection: All 29 functions in danhba_mbkc.json match Subsystem 4 exactly.")

    # 8. Cross-Verification: Subsystem 5 vs DceAnalyssiss64.dll Exports
    print("\n--- SUBSYSTEM 5 CROSS-VERIFICATION: Native C++ Core (69 exports) ---")
    pe_exports = parse_pe_exports(NATIVE_DLL_PATH)
    pe_exports_dict = {e['name']: e for e in pe_exports}
    pe_ordinals_dict = {e['ordinal']: e for e in pe_exports}

    matrix_sub5 = subsystems[5]['rows']
    matrix_sub5_dict = {r['identifier']: r for r in matrix_sub5}

    print(f"Binary PE Exports: {len(pe_exports)} exports found")
    print(f"Matrix Table Rows: {len(matrix_sub5)} rows found")

    missing_in_matrix_sub5 = set(pe_exports_dict.keys()) - set(matrix_sub5_dict.keys())
    extra_in_matrix_sub5 = set(matrix_sub5_dict.keys()) - set(pe_exports_dict.keys())

    if missing_in_matrix_sub5:
        print(f"[!] Missing PE exports in TRUTH_MATRIX ({len(missing_in_matrix_sub5)}): {missing_in_matrix_sub5}")
    if extra_in_matrix_sub5:
        print(f"[!] Extra exports in TRUTH_MATRIX ({len(extra_in_matrix_sub5)}): {extra_in_matrix_sub5}")

    # Check ordinals and RVAs in Subsystem 5 table
    rva_mismatches = []
    ordinal_mismatches = []
    for row in matrix_sub5:
        func_name = row['identifier']
        if func_name in pe_exports_dict:
            pe_exp = pe_exports_dict[func_name]
            token_field = row['token_rva']
            # Expected format: Ordinal #X / RVA 0xXXXX (or Ord: X / RVA: `0xXXXX`)
            ord_match = re.search(r'Ord(?:inal)?[:\s]+#?(\d+)', token_field)
            rva_match = re.search(r'RVA[:\s]+`?(0x[0-9a-fA-F]+)`?', token_field)
            
            if ord_match:
                table_ord = int(ord_match.group(1))
                if table_ord != pe_exp['ordinal']:
                    ordinal_mismatches.append({
                        'func': func_name,
                        'table_ord': table_ord,
                        'pe_ord': pe_exp['ordinal'],
                        'line': row['line']
                    })
            else:
                ordinal_mismatches.append({
                    'func': func_name,
                    'table_ord': None,
                    'pe_ord': pe_exp['ordinal'],
                    'line': row['line'],
                    'issue': f"Ordinal pattern missing in '{token_field}'"
                })

            if rva_match:
                table_rva = rva_match.group(1).upper()
                pe_rva = pe_exp['rva'].upper()
                if int(table_rva, 16) != int(pe_rva, 16):
                    rva_mismatches.append({
                        'func': func_name,
                        'table_rva': table_rva,
                        'pe_rva': pe_rva,
                        'line': row['line']
                    })
            else:
                rva_mismatches.append({
                    'func': func_name,
                    'table_rva': None,
                    'pe_rva': pe_exp['rva'],
                    'line': row['line'],
                    'issue': f"RVA pattern missing in '{token_field}'"
                })

    if ordinal_mismatches:
        print(f"[!] ORDINAL MISMATCHES in Subsystem 5 ({len(ordinal_mismatches)}):")
        for m in ordinal_mismatches[:10]:
            print(f"    - {m}")
    else:
        print("[+] 100% Ordinal Match: All 69 ordinals in table match PE binary exactly.")

    if rva_mismatches:
        print(f"[!] RVA MISMATCHES in Subsystem 5 ({len(rva_mismatches)}):")
        for m in rva_mismatches[:10]:
            print(f"    - {m}")
    else:
        print("[+] 100% RVA Match: All 69 RVAs in table match PE binary export RVAs exactly.")

    # 9. Verification of Subsystems 6..9 (Revit, Jigs, Forms, AutoStr)
    print("\n--- SUBSYSTEMS 6..9 VERIFICATION ---")
    for sub_id in [6, 7, 8, 9]:
        sub = subsystems[sub_id]
        print(f"\nSubsystem {sub_id} ({sub['name']}):")
        print(f"  Rows count: {len(sub['rows'])}")
        # Check token formatting
        token_issues = []
        for r in sub['rows']:
            tok = r['token_rva']
            if not tok or tok == '-':
                token_issues.append((r['line'], r['identifier'], "Empty token"))
        print(f"  Status distribution: HOÀN THÀNH={sum(1 for r in sub['rows'] if r['status']=='HOÀN THÀNH')}, MOCK={sum(1 for r in sub['rows'] if r['status']=='ĐANG LÀ MOCK')}, CGM={sum(1 for r in sub['rows'] if r['status']=='CHƯA GIẢI MÃ ĐƯỢC')}")
        if token_issues:
            print(f"  [!] Token issues ({len(token_issues)}): {token_issues[:5]}")
        else:
            print("  [+] All entries have non-empty Token/RVA/TypeDef metadata.")

    # 10. Summary Results Object for Report
    report = {
        'total_endpoints': total_endpoints_counted,
        'status_counts': {
            'HOAN_THANH': total_hoan_thanh,
            'DANG_LA_MOCK': total_mock,
            'CHUA_GIAI_MA_DUOC': total_chua_giai_ma
        },
        'formatting_defects_count': len(formatting_defects),
        'formatting_defects': formatting_defects,
        'summary_errors': summary_errors,
        'duplicate_errors': duplicate_errors,
        'sub1_bijection': len(missing_in_matrix_sub1) == 0 and len(extra_in_matrix_sub1) == 0,
        'sub2_bijection': len(missing_in_matrix_sub2) == 0 and len(extra_in_matrix_sub2) == 0,
        'sub3_bijection': len(missing_in_matrix_sub3) == 0 and len(extra_in_matrix_sub3) == 0,
        'sub4_bijection': len(missing_in_matrix_sub4) == 0 and len(extra_in_matrix_sub4) == 0,
        'sub5_bijection': len(missing_in_matrix_sub5) == 0 and len(extra_in_matrix_sub5) == 0,
        'sub5_ordinal_match': len(ordinal_mismatches) == 0,
        'sub5_rva_match': len(rva_mismatches) == 0,
        'subsystems_detail': {
            sub_id: {
                'name': sub['name'],
                'claimed': sub['claimed_count'],
                'actual': len(sub['rows']),
                'ht': sum(1 for r in sub['rows'] if r['status']=='HOÀN THÀNH'),
                'mock': sum(1 for r in sub['rows'] if r['status']=='ĐANG LÀ MOCK'),
                'cgm': sum(1 for r in sub['rows'] if r['status']=='CHƯA GIẢI MÃ ĐƯỢC')
            } for sub_id, sub in subsystems.items()
        }
    }

    report_out_path = r"tools/verification/parity_verification_results.json"
    with open(report_out_path, 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    print(f"\n[+] Detailed verification report written to: {report_out_path}")

    return report

if __name__ == '__main__':
    run_adversarial_verification()
