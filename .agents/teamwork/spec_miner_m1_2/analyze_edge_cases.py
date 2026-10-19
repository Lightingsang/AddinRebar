import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('autocad_311_matrix.json', 'r', encoding='utf-8') as f:
    commands = json.load(f)

# Categories
cats = {}
for c in commands:
    cats.setdefault(c['category'], []).append(c)

print("=== CATEGORY BREAKDOWN ===")
for cat, items in sorted(cats.items(), key=lambda x: -len(x[1])):
    impl = sum(1 for x in items if 'IMPLEMENTED' in x['status'])
    mock = sum(1 for x in items if 'MOCK' in x['status'])
    miss = sum(1 for x in items if 'MISSING' in x['status'])
    print(f"| {cat} | {len(items)} | {impl} | {mock} | {miss} |")

print("\n=== EDGE CASES ===")
# 1. No prompt, no form, no drawing (pure state/debug)
e1 = [c for c in commands if c['can_chon'] == 'Không' and c['can_form'] == 'Không' and c['ghi_ban_ve'] == 'Không']
print(f"Edge case 1 - Pure State/Utility ({len(e1)}): {[c['name'] for c in e1]}")

# 2. Automated commands (co_the_tu_dong = True)
# We can load original json to check co_the_tu_dong
with open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json', 'r', encoding='utf-8') as f:
    orig_data = json.load(f)
orig_map = {c['ten'].strip(): c for c in orig_data['muc']}

auto_cmds = [c for c in commands if orig_map.get(c['name'], {}).get('co_the_tu_dong')]
print(f"\nEdge case 2 - Co The Tu Dong ({len(auto_cmds)}): {[c['name'] for c in auto_cmds]}")

# 3. Both form and multi-step selection
complex_interactive = [c for c in commands if c['can_chon'] == 'Có' and c['can_form'] == 'Có' and '→' in c['chon_gi']]
print(f"\nEdge case 3 - Complex Multi-stage Prompt + Form ({len(complex_interactive)}): {[c['name'] for c in complex_interactive[:10]]}")

# 4. Layout/Viewport commands
vp_cmds = [c for c in commands if 'layout' in c['description'].lower() or 'model' in c['description'].lower() or 'viewport' in c['description'].lower()]
print(f"\nEdge case 4 - Viewport/Layout Manipulation ({len(vp_cmds)}): {[c['name'] for c in vp_cmds]}")

# 5. XData commands
xdata_cmds = [c for c in commands if 'xdata' in c['description'].lower() or 'liên kết' in c['description'].lower() or 'gán' in c['description'].lower() or 'liên kết' in c['name'].lower()]
print(f"\nEdge case 5 - XData Registry & Binding ({len(xdata_cmds)}): {[c['name'] for c in xdata_cmds[:10]]}")
