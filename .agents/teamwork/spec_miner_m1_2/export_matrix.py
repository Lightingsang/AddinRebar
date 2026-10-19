import json
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

# 1. Load danhba_lenh.json
danhba_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json'
with open(danhba_path, 'r', encoding='utf-8') as f:
    danhba_data = json.load(f)

commands = danhba_data.get('muc', [])

# 2. Inspect HPDcePro.AutoCad source files
autocad_src = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.AutoCad'
existing_commands = {} # cmd_name_upper -> list of dicts

for root, dirs, files in os.walk(autocad_src):
    if 'bin' in root or 'obj' in root:
        continue
    for file in files:
        if file.endswith('.cs'):
            filepath = os.path.join(root, file)
            with open(filepath, 'r', encoding='utf-8') as cf:
                content = cf.read()
                lines = content.splitlines()
                for idx, line in enumerate(lines):
                    match = re.search(r'\[CommandMethod\s*\(\s*["\']([^"\']+)["\']', line)
                    if match:
                        cname = match.group(1)
                        body_lines = []
                        brace_count = 0
                        started = False
                        for j in range(idx, min(len(lines), idx+150)):
                            cur = lines[j]
                            if '{' in cur:
                                brace_count += cur.count('{')
                                started = True
                            if '}' in cur:
                                brace_count -= cur.count('}')
                            body_lines.append(cur)
                            if started and brace_count == 0:
                                break
                        body = "\n".join(body_lines)
                        rel_path = os.path.relpath(filepath, autocad_src).replace('\\', '/')

                        is_mock = False
                        mock_reasons = []
                        if 'CalculateSample' in body:
                            is_mock = True
                            mock_reasons.append("CalculateSample")
                        if 'GenerateSample' in body:
                            is_mock = True
                            mock_reasons.append("GenerateSample")
                        if 'DceSample' in body:
                            is_mock = True
                            mock_reasons.append("DceSample")
                        if 'axes = { "TRỤC 1", "TRỤC 2" }' in body or 'TRỤC 1' in body:
                            is_mock = True
                            mock_reasons.append("Hardcoded sample axes")
                        if 'ColumnPmmInput("C1"' in body:
                            is_mock = True
                            mock_reasons.append("Sample Column input")

                        existing_commands.setdefault(cname.upper(), []).append({
                            'name': cname,
                            'file': rel_path,
                            'line': idx + 1,
                            'is_mock': is_mock,
                            'reasons': ", ".join(mock_reasons)
                        })

# Mapping rule for target files
def get_target_file(cmd):
    cat = cmd.get('nhom_nghiep_vu', '').strip()
    nhom = cmd.get('nhom', '').strip()

    if 'dam' in nhom.lower() or cat == 'dầm':
        return 'Commands/BeamCommands.cs'
    elif 'cot' in nhom.lower() or cat == 'cột':
        return 'Commands/ColumnCommands.cs'
    elif 'san' in nhom.lower() or cat == 'sàn':
        return 'Commands/SlabCommands.cs'
    elif 'mong' in nhom.lower() or cat == 'móng':
        return 'Commands/FootingCommands.cs'
    elif cat in ['thép', 'thống kê']:
        return 'Commands/RebarCommands.cs'
    elif cat == 'AutoStr' or 'autostr' in nhom.lower():
        return 'Commands/AutoStrCommands.cs'
    elif cat == 'cầu thang' or 'thang' in nhom.lower():
        return 'Commands/StairCommands.cs'
    elif cat == 'vách' or 'vach' in nhom.lower():
        return 'Commands/WallCommands.cs'
    elif cat == 'shop drawing':
        return 'Commands/ShopDrawingCommands.cs'
    elif cat == 'ETABS / SAFE' or 'etabs' in nhom.lower() or 'safe' in nhom.lower():
        return 'Commands/EtabsSyncCommands.cs'
    elif cat == 'trợ lý AI' or 'ai' in nhom.lower():
        return 'Commands/AiAssistantCommands.cs'
    elif cat == 'bản vẽ / in ấn' or 'in' in nhom.lower():
        return 'Commands/DrawingCommands.cs'
    elif cat == 'hệ thống':
        return 'Commands/SystemCadCommands.cs'
    else:
        return 'Commands/GeneralCadCommands.cs'

# Process all 311 commands
processed = []
for idx, cmd in enumerate(commands, 1):
    cname = cmd.get('ten', '').strip()
    cat = cmd.get('nhom_nghiep_vu', 'tiện ích chung').strip()
    mota = cmd.get('mo_ta', '').replace('\n', ' ').replace('|', '/').strip()
    can_chon = "Có" if cmd.get('can_chon') else "Không"
    can_form = "Có" if cmd.get('can_form') else "Không"
    ghi_ban_ve = "Có" if cmd.get('ghi_ban_ve') else "Không"
    chon_gi = cmd.get('chon_gi', '').replace('\n', ' ').replace('|', '/').strip()
    
    # Check status
    upper_name = cname.upper()
    if upper_name in existing_commands:
        matches = existing_commands[upper_name]
        m = matches[0]
        if m['is_mock']:
            status = f"ĐANG LÀ MOCK ({m['file']}:{m['line']})"
            target = m['file']
        else:
            status = f"IMPLEMENTED ({m['file']}:{m['line']})"
            target = m['file']
    else:
        status = "MISSING"
        target = get_target_file(cmd)

    processed.append({
        'index': idx,
        'name': cname,
        'category': cat,
        'description': mota,
        'can_chon': can_chon,
        'can_form': can_form,
        'ghi_ban_ve': ghi_ban_ve,
        'target': target,
        'status': status,
        'chon_gi': chon_gi
    })

# Output stats
status_counts = {}
for p in processed:
    st_prefix = p['status'].split()[0]
    status_counts[st_prefix] = status_counts.get(st_prefix, 0) + 1

print(f"Total commands processed: {len(processed)}")
print(f"Status distribution: {status_counts}")

# Save JSON representation
out_json = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\autocad_311_matrix.json'
with open(out_json, 'w', encoding='utf-8') as f:
    json.dump(processed, f, ensure_ascii=False, indent=2)

print(f"Saved full matrix JSON to: {out_json}")
