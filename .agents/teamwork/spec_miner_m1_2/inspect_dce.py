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
danhba_names = {c['ten'].strip().upper(): c for c in commands}

# 2. Inspect HPDcePro.AutoCad source files to find existing commands
autocad_src = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.AutoCad'
existing_commands = {} # cmd_name_upper -> (file, line, code_snippet, is_mock)

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
                        cname = match.group(1).upper()
                        surrounding = "\n".join(lines[idx:min(len(lines), idx+30)])
                        is_mock = False
                        if any(k in surrounding for k in ['Sample', 'CalculateSample', 'GenerateSample', 'Mock', 'DceSample', 'dummy', 'fake']):
                            is_mock = True
                        rel_path = os.path.relpath(filepath, autocad_src)
                        existing_commands[cname] = (rel_path, idx+1, is_mock)

print(f"Total commands in danhba_lenh.json: {len(commands)}")
print(f"Total CommandMethods in HPDcePro.AutoCad: {len(existing_commands)}")

# Extra commands in HPDcePro.AutoCad
extras = []
for cname, (relpath, line_no, is_mock) in sorted(existing_commands.items()):
    if cname not in danhba_names:
        extras.append((cname, relpath, line_no))

print(f"\nExtra commands in HPDcePro.AutoCad not in danhba_lenh.json ({len(extras)}):")
for cname, relpath, line_no in extras:
    print(f"  {cname} ({relpath}:{line_no})")

