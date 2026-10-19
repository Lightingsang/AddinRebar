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
existing_commands = {} # cmd_name_upper -> list of dicts: {'file': relpath, 'line': idx+1, 'is_mock': bool, 'reasons': str}

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
                        # Extract method body roughly
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
                        rel_path = os.path.relpath(filepath, autocad_src)

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

                        rel_norm = rel_path.replace('\\', '/')
                        existing_commands.setdefault(cname.upper(), []).append({
                            'name': cname,
                            'file': rel_norm,
                            'line': idx + 1,
                            'is_mock': is_mock,
                            'reasons': ", ".join(mock_reasons)
                        })

# Target mapping rule for missing commands
def get_target_file(cmd):
    cat = cmd.get('nhom_nghiep_vu', '').strip()
    nhom = cmd.get('nhom', '').strip()
    cname = cmd.get('ten', '').strip()

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

print("Ready to process commands.")
