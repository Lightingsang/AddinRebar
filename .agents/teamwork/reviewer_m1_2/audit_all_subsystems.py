import os
import sys
import json
import re
import struct

sys.stdout.reconfigure(encoding='utf-8')

def parse_markdown_table(lines):
    rows = []
    headers = []
    in_table = False
    for line in lines:
        line_s = line.strip()
        if not line_s.startswith('|'):
            if in_table:
                break
            continue
        parts = [p.strip() for p in line_s.split('|')[1:-1]]
        if not headers:
            headers = parts
            in_table = True
            continue
        if all(set(p).issubset({'-', ':', ' '}) for p in parts):
            continue
        row = dict(zip(headers, parts))
        rows.append(row)
    return rows

def verify_subsystem_2(tm_lines):
    print("=== VERIFYING SUBSYSTEM 2: Excel Ribbon Actions ===")
    json_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json'
    with open(json_path, 'r', encoding='utf-8') as f:
        catalog = json.load(f)
    cat_items = catalog.get('muc', [])
    print(f"Catalog JSON count: {len(cat_items)}")

    # Extract lines for Subsystem 2 from TRUTH_MATRIX.md
    sub2_lines = []
    capture = False
    for line in tm_lines:
        if '## 3. Subsystem 2:' in line:
            capture = True
            continue
        if capture and line.startswith('## '):
            break
        if capture:
            sub2_lines.append(line)
    
    rows = parse_markdown_table(sub2_lines)
    print(f"TRUTH_MATRIX Subsystem 2 rows parsed: {len(rows)}")

    cat_map = {item['ten']: item for item in cat_items}
    tm_map = {}
    for r in rows:
        raw_id = r['Legacy Identifier']
        # Extract main command name (strip HTML tags like <br>HostRibbon.Dispatch)
        clean_id = raw_id.split('<br>')[0].strip('` ')
        tm_map[clean_id] = r

    # Compare set of identifiers
    cat_keys = set(cat_map.keys())
    tm_keys = set(tm_map.keys())

    missing_in_tm = cat_keys - tm_keys
    extra_in_tm = tm_keys - cat_keys

    print(f"Missing in TRUTH_MATRIX: {len(missing_in_tm)} {missing_in_tm}")
    print(f"Extra in TRUTH_MATRIX: {len(extra_in_tm)} {extra_in_tm}")

    # Count statuses
    statuses = {}
    for r in rows:
        st = r['Parity Status']
        statuses[st] = statuses.get(st, 0) + 1
    print(f"Subsystem 2 Parity Statuses: {statuses}")

    return {
        'catalog_count': len(cat_items),
        'tm_count': len(rows),
        'missing': list(missing_in_tm),
        'extra': list(extra_in_tm),
        'statuses': statuses,
        'rows': rows
    }

def verify_subsystem_3(tm_lines):
    print("\n=== VERIFYING SUBSYSTEM 3: DceSteel Facade ===")
    json_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_thep.json'
    with open(json_path, 'r', encoding='utf-8') as f:
        catalog = json.load(f)
    cat_items = catalog.get('muc', [])
    print(f"Catalog JSON count: {len(cat_items)}")

    sub3_lines = []
    capture = False
    for line in tm_lines:
        if '## 4. Subsystem 3:' in line:
            capture = True
            continue
        if capture and line.startswith('## '):
            break
        if capture:
            sub3_lines.append(line)
    
    rows = parse_markdown_table(sub3_lines)
    print(f"TRUTH_MATRIX Subsystem 3 rows parsed: {len(rows)}")

    cat_names = set(item['ten'] for item in cat_items)
    tm_names = set(r['Legacy Identifier'].split('(')[0].strip('` ') for r in rows)

    missing_in_tm = cat_names - tm_names
    extra_in_tm = tm_names - cat_names

    print(f"Missing in TRUTH_MATRIX: {len(missing_in_tm)} {missing_in_tm}")
    print(f"Extra in TRUTH_MATRIX: {len(extra_in_tm)} {extra_in_tm}")

    statuses = {}
    for r in rows:
        st = r['Parity Status']
        statuses[st] = statuses.get(st, 0) + 1
    print(f"Subsystem 3 Parity Statuses: {statuses}")

    return {
        'catalog_count': len(cat_items),
        'tm_count': len(rows),
        'missing': list(missing_in_tm),
        'extra': list(extra_in_tm),
        'statuses': statuses
    }

def verify_subsystem_4(tm_lines):
    print("\n=== VERIFYING SUBSYSTEM 4: MBKC Facade ===")
    json_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_mbkc.json'
    with open(json_path, 'r', encoding='utf-8') as f:
        catalog = json.load(f)
    cat_items = catalog.get('muc', [])
    print(f"Catalog JSON count: {len(cat_items)}")

    sub4_lines = []
    capture = False
    for line in tm_lines:
        if '## 5. Subsystem 4:' in line:
            capture = True
            continue
        if capture and line.startswith('## '):
            break
        if capture:
            sub4_lines.append(line)
    
    rows = parse_markdown_table(sub4_lines)
    print(f"TRUTH_MATRIX Subsystem 4 rows parsed: {len(rows)}")

    cat_names = set(item['ten'] for item in cat_items)
    tm_names = set(r['Legacy Identifier'].split('(')[0].strip('` ') for r in rows)

    missing_in_tm = cat_names - tm_names
    extra_in_tm = tm_names - cat_names

    print(f"Missing in TRUTH_MATRIX: {len(missing_in_tm)} {missing_in_tm}")
    print(f"Extra in TRUTH_MATRIX: {len(extra_in_tm)} {extra_in_tm}")

    statuses = {}
    for r in rows:
        st = r['Parity Status']
        statuses[st] = statuses.get(st, 0) + 1
    print(f"Subsystem 4 Parity Statuses: {statuses}")

    return {
        'catalog_count': len(cat_items),
        'tm_count': len(rows),
        'missing': list(missing_in_tm),
        'extra': list(extra_in_tm),
        'statuses': statuses
    }

def verify_subsystem_5(tm_lines):
    print("\n=== VERIFYING SUBSYSTEM 5: Native C++ Exports ===")
    dll_path = r'C:\Dce Pro\DceAnalyssiss64.dll'
    with open(dll_path, 'rb') as f:
        data = f.read()

    e_lfanew = struct.unpack_from('<I', data, 0x3C)[0]
    num_sections = struct.unpack_from('<H', data, e_lfanew + 6)[0]
    opt_hdr_size = struct.unpack_from('<H', data, e_lfanew + 20)[0]
    opt_hdr = e_lfanew + 24
    exp_rva, exp_size = struct.unpack_from('<II', data, opt_hdr + 112)

    sec_hdr_start = opt_hdr + opt_hdr_size
    sections = []
    for i in range(num_sections):
        sec = data[sec_hdr_start + i*40 : sec_hdr_start + (i+1)*40]
        vsize, vrva, rsize, roff = struct.unpack_from('<IIII', sec, 8)
        sections.append((vrva, vsize, roff, rsize))

    def rva_to_off(rva):
        for vrva, vsize, roff, rsize in sections:
            if vrva <= rva < vrva + max(vsize, rsize):
                return roff + (rva - vrva)
        return None

    exp_off = rva_to_off(exp_rva)
    base, num_funcs, num_names, addr_funcs_rva, addr_names_rva, addr_ords_rva = struct.unpack_from('<IIIIII', data, exp_off + 16)
    funcs_off = rva_to_off(addr_funcs_rva)
    names_off = rva_to_off(addr_names_rva)
    ords_off = rva_to_off(addr_ords_rva)

    pe_exports = {}
    for i in range(num_names):
        name_rva = struct.unpack_from('<I', data, names_off + i*4)[0]
        name_off = rva_to_off(name_rva)
        null_idx = data.find(b'\x00', name_off)
        name = data[name_off:null_idx].decode('ascii')
        
        ord_idx = struct.unpack_from('<H', data, ords_off + i*2)[0]
        func_rva = struct.unpack_from('<I', data, funcs_off + ord_idx*4)[0]
        ordinal = base + ord_idx
        pe_exports[name] = {
            'ordinal': ordinal,
            'rva': hex(func_rva)
        }

    print(f"PE exports count from DLL: {len(pe_exports)}")

    sub5_lines = []
    capture = False
    for line in tm_lines:
        if '## 6. Subsystem 5:' in line:
            capture = True
            continue
        if capture and line.startswith('## '):
            break
        if capture:
            sub5_lines.append(line)
    
    rows = parse_markdown_table(sub5_lines)
    print(f"TRUTH_MATRIX Subsystem 5 rows parsed: {len(rows)}")

    mismatches = []
    for r in rows:
        name = r['Legacy Identifier'].strip('` ')
        token_str = r['Token/RVA'] # e.g. "Export RVA: `0x4d40` (Ord: 0)"
        ord_match = re.search(r'Ord:\s*(\d+)', token_str)
        rva_match = re.search(r'RVA:\s*`([^`]+)`', token_str)
        
        if not ord_match or not rva_match:
            mismatches.append(f"{name}: could not parse ordinal/RVA from '{token_str}'")
            continue
        
        tm_ord = int(ord_match.group(1))
        tm_rva = rva_match.group(1).lower()

        if name not in pe_exports:
            mismatches.append(f"{name}: not found in PE exports")
            continue
        
        pe_ord = pe_exports[name]['ordinal']
        pe_rva = pe_exports[name]['rva'].lower()

        # Check both 0-based and 1-based ordinal matching
        # (Since Base=1, PE ordinal is 1-based, Ord index in array is 0-based: ord_idx = pe_ord - 1)
        if tm_ord != pe_ord and tm_ord != (pe_ord - 1):
            mismatches.append(f"{name}: TM(ord={tm_ord}, rva={tm_rva}) != PE(ord={pe_ord}, rva={pe_rva})")
        elif tm_rva != pe_rva:
            mismatches.append(f"{name}: TM RVA {tm_rva} != PE RVA {pe_rva}")

    print(f"Subsystem 5 Mismatches: {len(mismatches)}")
    for m in mismatches[:10]:
        print(" ", m)

    statuses = {}
    for r in rows:
        st = r['Parity Status']
        statuses[st] = statuses.get(st, 0) + 1
    print(f"Subsystem 5 Parity Statuses: {statuses}")

    return {
        'pe_count': len(pe_exports),
        'tm_count': len(rows),
        'mismatches': mismatches,
        'statuses': statuses
    }

def load_json_file(path):
    with open(path, 'rb') as f:
        raw = f.read()
    if raw.startswith(b'\xff\xfe') or raw.startswith(b'\xfe\xff'):
        return json.loads(raw.decode('utf-16'))
    elif raw.startswith(b'\xef\xbb\xbf'):
        return json.loads(raw.decode('utf-8-sig'))
    else:
        try:
            return json.loads(raw.decode('utf-8'))
        except UnicodeDecodeError:
            return json.loads(raw.decode('latin1'))

def verify_subsystem_6(tm_lines):
    print("\n=== VERIFYING SUBSYSTEM 6: Revit Commands ===")
    
    td_data = load_json_file('.agents/teamwork/spec_miner_m1_3/dbimrevit_typedef.json')
    impl_data = load_json_file('.agents/teamwork/spec_miner_m1_3/dbimrevit_interfaceimpl.json')
    tr_data = load_json_file('.agents/teamwork/spec_miner_m1_3/dbimrevit_typeref.json')

    revit_interfaces = {}
    for tr in tr_data['rows']:
        name = tr.get('Name', '')
        if 'IExternalCommand' in name or 'IExternalApplication' in name:
            revit_interfaces[tr.get('Token')] = name

    classes = {td['RID']: td for td in td_data['rows']}
    extracted_revit = {}
    for imp in impl_data['rows']:
        c_val = imp.get('Class')
        c_rid = int(c_val, 16) & 0x00FFFFFF if isinstance(c_val, str) and c_val.startswith('0x') else int(c_val)
        intf = imp.get('Interface')
        if intf in revit_interfaces:
            td = classes.get(c_rid, {})
            full_name = f"{td.get('Namespace', '')}.{td.get('Name', '')}".strip('.')
            extracted_revit[full_name] = {
                'token': td.get('Token'),
                'name': td.get('Name'),
                'namespace': td.get('Namespace'),
                'interface': revit_interfaces[intf]
            }

    print(f"Extracted Revit classes implementing IExternal*: {len(extracted_revit)}")

    sub6_lines = []
    capture = False
    for line in tm_lines:
        if '## 7. Subsystem 6:' in line:
            capture = True
            continue
        if capture and line.startswith('## '):
            break
        if capture:
            sub6_lines.append(line)
    
    rows = parse_markdown_table(sub6_lines)
    print(f"TRUTH_MATRIX Subsystem 6 rows parsed: {len(rows)}")

    mismatches = []
    for r in rows:
        cmd_id = r['Legacy Identifier'].strip('` ')
        token_str = r['Token/RVA'].strip('` ') # e.g. "TypeDef: 0x0200001B"
        token_match = re.search(r'0x[0-9a-fA-F]+', token_str)
        
        # Check against extracted_revit
        found = False
        for full_name, data in extracted_revit.items():
            if data['name'] == cmd_id or full_name == cmd_id:
                found = True
                if token_match:
                    exp_token = token_match.group(0).lower()
                    act_token = data['token'].lower()
                    if exp_token != act_token:
                        mismatches.append(f"{cmd_id}: TM token {exp_token} != actual token {act_token}")
                break
        if not found:
            mismatches.append(f"{cmd_id}: Not found in extracted Revit classes")

    print(f"Subsystem 6 Mismatches: {len(mismatches)}")
    for m in mismatches:
        print(" ", m)

    statuses = {}
    for r in rows:
        st = r['Parity Status']
        statuses[st] = statuses.get(st, 0) + 1
    print(f"Subsystem 6 Parity Statuses: {statuses}")

    return {
        'extracted_count': len(extracted_revit),
        'tm_count': len(rows),
        'mismatches': mismatches,
        'statuses': statuses
    }

def verify_subsystem_9(tm_lines):
    print("\n=== VERIFYING SUBSYSTEM 9: AutoStr Pipeline ===")
    sub9_lines = []
    capture = False
    for line in tm_lines:
        if '## 10. Subsystem 9:' in line:
            capture = True
            continue
        if capture and line.startswith('## '):
            break
        if capture:
            sub9_lines.append(line)
    
    rows = parse_markdown_table(sub9_lines)
    print(f"TRUTH_MATRIX Subsystem 9 rows parsed: {len(rows)}")

    statuses = {}
    for r in rows:
        st = r['Parity Status']
        statuses[st] = statuses.get(st, 0) + 1
    print(f"Subsystem 9 Parity Statuses: {statuses}")

    steps = [r['Legacy Identifier'].strip('` ') for r in rows]
    print(f"Step IDs range: {steps[0]} to {steps[-1]} (Total: {len(steps)})")

    return {
        'tm_count': len(rows),
        'statuses': statuses,
        'steps': steps
    }

def verify_section_11(tm_lines):
    print("\n=== VERIFYING SECTION 11: Static Assets Inventory ===")
    base = r'C:\Dce Pro'

    dwgs = []
    xlsms = []
    cuixs = []
    for root, dirs, files in os.walk(base):
        for f in files:
            fl = f.lower()
            if fl.endswith('.dwg'):
                dwgs.append(os.path.join(root, f))
            elif fl.endswith('.xlsm'):
                xlsms.append(os.path.join(root, f))
            elif fl.endswith('.cuix'):
                cuixs.append(os.path.join(root, f))

    mauhoc_path = os.path.join(base, 'MayHoc', 'mauhoc.db')
    mauhoc_sz = os.path.getsize(mauhoc_path) if os.path.exists(mauhoc_path) else 0

    sys_dir = os.path.join(base, 'System')
    sys_files = []
    for root, dirs, files in os.walk(sys_dir):
        for f in files:
            sys_files.append(os.path.join(root, f))

    lang_dir = os.path.join(base, 'Language')
    lang_files = []
    for root, dirs, files in os.walk(lang_dir):
        for f in files:
            lang_files.append(os.path.join(root, f))

    icon_dir = os.path.join(base, 'Icon')
    icon_files = []
    for root, dirs, files in os.walk(icon_dir):
        for f in files:
            icon_files.append(os.path.join(root, f))

    print(f"Physical DWG count: {len(dwgs)} (Claimed: 17)")
    print(f"Physical XLSM count: {len(xlsms)} (Claimed: 132)")
    print(f"Physical CUIX count: {len(cuixs)} (Claimed: 7)")
    print(f"Physical mauhoc.db: {mauhoc_sz} bytes = {mauhoc_sz / 1000000:.2f} MB (Claimed: 8.98 MB)")
    print(f"Physical System files: {len(sys_files)} (Claimed: 178)")
    print(f"Physical Lang dicts: {len(lang_files)} (Claimed: 8)")
    print(f"Physical Icon files: {len(icon_files)} (Claimed: 133)")

    return {
        'dwgs': len(dwgs),
        'xlsms': len(xlsms),
        'cuixs': len(cuixs),
        'mauhoc_sz': mauhoc_sz,
        'sys_files': len(sys_files),
        'lang_files': len(lang_files),
        'icon_files': len(icon_files)
    }

def main():
    tm_path = r'docs\rea-dce-pro\TRUTH_MATRIX.md'
    with open(tm_path, 'r', encoding='utf-8') as f:
        tm_lines = f.readlines()

    res2 = verify_subsystem_2(tm_lines)
    res3 = verify_subsystem_3(tm_lines)
    res4 = verify_subsystem_4(tm_lines)
    res5 = verify_subsystem_5(tm_lines)
    res6 = verify_subsystem_6(tm_lines)
    res9 = verify_subsystem_9(tm_lines)
    res11 = verify_section_11(tm_lines)

    summary = {
        'Subsystem_2': res2,
        'Subsystem_3': res3,
        'Subsystem_4': res4,
        'Subsystem_5': res5,
        'Subsystem_6': res6,
        'Subsystem_9': res9,
        'Section_11': res11
    }
    with open(r'.agents\teamwork\reviewer_m1_2\audit_results.json', 'w', encoding='utf-8') as f:
        json.dump(summary, f, indent=2, ensure_ascii=False)
    print("\nSaved comprehensive audit results to .agents/teamwork/reviewer_m1_2/audit_results.json")

if __name__ == '__main__':
    main()
