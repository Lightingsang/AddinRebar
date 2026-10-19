import json
import glob
import re

seed_files = sorted(glob.glob('HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/**/tool.json', recursive=True))

manifests = {}
for sf in seed_files:
    with open(sf, 'r', encoding='utf-8') as f:
        data = json.load(f)
        manifests[data['name']] = data

print(f"Total seed manifests: {len(manifests)}")

with open('.agents/skills/hp-mcp-grasshopper/SKILL.md', 'r', encoding='utf-8') as f:
    skill_content = f.read()

# Verify each seed
for name, m in manifests.items():
    print(f"\n--- Checking seed: {name} ---")
    tx = m.get('transaction')
    req = m.get('inputSchema', {}).get('required', [])
    props = m.get('inputSchema', {}).get('properties', {})
    
    # Check if tool is in SKILL.md
    if name not in skill_content:
        print(f"ERROR: {name} not found in SKILL.md")
        continue
    
    # Check properties
    for p_name, p_def in props.items():
        if p_name not in skill_content:
            print(f"ERROR: property {p_name} of {name} NOT found in SKILL.md")
        else:
            p_type = p_def.get('type')
            is_req = p_name in req
            default_val = p_def.get('default')
            # Check how it's documented
            print(f"  OK: {p_name} (type: {p_type}, required: {is_req}, default: {default_val})")
