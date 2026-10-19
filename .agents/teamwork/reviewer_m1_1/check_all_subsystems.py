import os
import re
import sys
import json

sys.stdout.reconfigure(encoding='utf-8')

matrix_path = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md'
with open(matrix_path, 'r', encoding='utf-8') as f:
    matrix_content = f.read()

# Check all subsystems in TRUTH_MATRIX.md
# Find all tables
table_sections = re.findall(r'## \d+\. (Subsystem \d+[^:\n]*):?([^\n]*)\n(.*?)(?=\n## \d+|\n## 11|\n## 12|\Z)', matrix_content, re.DOTALL)

print(f"Found {len(table_sections)} subsystem sections in TRUTH_MATRIX.md:")
subsystems_data = {}
for s_title, s_sub, s_body in table_sections:
    s_name = (s_title + s_sub).strip()
    rows = [line.strip() for line in s_body.split('\n') if line.strip().startswith('|') and not line.strip().startswith('|---') and not 'Legacy Identifier' in line]
    subsystems_data[s_name] = []
    for r in rows:
        parts = [p.strip() for p in r.split('|')[1:-1]]
        if len(parts) >= 6:
            ident = parts[0].replace('`', '').strip()
            subsys = parts[1]
            token = parts[2]
            insp = parts[3]
            target = parts[4].replace('`', '').strip()
            status = parts[5].strip()
            subsystems_data[s_name].append({
                'identifier': ident, 'subsys': subsys, 'token': token, 'insp': insp, 'target': target, 'status': status
            })
    print(f"  {s_name}: {len(subsystems_data[s_name])} entries")

# Let's inspect Subsystem 3: DceSteel Facade
steel_cmds = [e['identifier'] for e in subsystems_data.get('Subsystem 3: DceSteel Facade (AddinSteelCad)', [])]
print(f"Subsystem 3 entries ({len(steel_cmds)}):", steel_cmds[:10])

# Check the unaccounted commands against all subsystems
from scan_autocad_commands import unaccounted_cmds
print("\nChecking the 24 unaccounted commands against all subsystems in TRUTH_MATRIX.md:")
for uc in unaccounted_cmds:
    found = False
    for s_name, entries in subsystems_data.items():
        for e in entries:
            if e['identifier'].lower() == uc.lower():
                print(f"  '{uc}' matches '{e['identifier']}' in '{s_name}' (Status: {e['status']}, Target: {e['target']})")
                found = True
                break
        if found:
            break
    if not found:
        print(f"  '{uc}' NOT FOUND anywhere in TRUTH_MATRIX.md!")
