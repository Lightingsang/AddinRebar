import os
import json
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Load the legacy catalog if it exists in C:\Dce Pro\AiAgent\DanhBa\danhba_dce_pro_commands.json
catalog_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_pro_commands.json'
if os.path.exists(catalog_path):
    with open(catalog_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    print(f"Loaded legacy catalog: {len(data)} items")
    cmd_names = [item.get('name') or item.get('command') or item.get('Command') for item in data]
    print(f"Sample command names: {cmd_names[:10]}")
else:
    print(f"Catalog not found at {catalog_path}")

# Check how many of these command names appear anywhere in HPDcePro/src
found_in_src = {}
for root, dirs, files in os.walk(r'HPDcePro/src'):
    for f in files:
        if f.endswith('.cs'):
            full = os.path.join(root, f)
            with open(full, 'r', encoding='utf-8', errors='ignore') as fp:
                c = fp.read()
            for cmd in cmd_names:
                if cmd and cmd in c:
                    if cmd not in found_in_src:
                        found_in_src[cmd] = []
                    found_in_src[cmd].append(f)

print(f"Commands from catalog found anywhere in HPDcePro/src (.cs text): {len(found_in_src)} / {len(cmd_names)}")

# Also check HPDcePro.AutoCad specifically
found_in_cad = {k: v for k, v in found_in_src.items() if any('AutoCad' in path for path in v)}
print(f"Commands from catalog found in HPDcePro.AutoCad: {len(found_in_cad)}")
