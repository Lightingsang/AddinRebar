import json

with open(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m3\tools-list.json', encoding='utf-8') as f:
    data = json.load(f)

seeds = [
    'get_model_info', 'select_objects', 'get_part_properties', 'modify_user_properties',
    'create_beam', 'create_column', 'create_contour_plate', 'create_rebar_group',
    'create_single_rebar', 'get_reinforcement_info', 'list_drawings', 'export_ifc'
]

tools_by_name = {t['name']: t for t in data['result']['tools']}

print(f"{'Tool Name':<25} | {'Parameters':<35} | {'Required'}")
print("-" * 80)
for name in sorted(seeds):
    if name not in tools_by_name:
        print(f"MISSING: {name}")
        continue
    t = tools_by_name[name]
    props = list(t.get('inputSchema', {}).get('properties', {}).keys())
    req = t.get('inputSchema', {}).get('required', [])
    print(f"{name:<25} | {', '.join(props):<35} | {', '.join(req) if req else 'none'}")
