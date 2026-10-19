import json
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

danhba_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_lenh.json'
with open(danhba_path, 'r', encoding='utf-8') as f:
    danhba_data = json.load(f)

commands = danhba_data.get('muc', [])

print(f"Total commands: {len(commands)}")

# Check nulls or missing fields
for i, cmd in enumerate(commands):
    for f in ['ten', 'nhom_nghiep_vu', 'mo_ta', 'can_chon', 'can_form', 'ghi_ban_ve']:
        if f not in cmd:
            print(f"Command {i} missing field {f}")

# Check value distributions
can_chon_count = sum(1 for c in commands if c.get('can_chon'))
can_form_count = sum(1 for c in commands if c.get('can_form'))
ghi_ban_ve_count = sum(1 for c in commands if c.get('ghi_ban_ve'))
co_the_tu_dong_count = sum(1 for c in commands if c.get('co_the_tu_dong'))

print(f"can_chon (requires selection/prompt): {can_chon_count} / {len(commands)}")
print(f"can_form (opens modal dialog/form): {can_form_count} / {len(commands)}")
print(f"ghi_ban_ve (modifies drawing): {ghi_ban_ve_count} / {len(commands)}")
print(f"co_the_tu_dong (can be automated): {co_the_tu_dong_count} / {len(commands)}")

# Both can_form and can_chon
both = [c['ten'] for c in commands if c.get('can_form') and c.get('can_chon')]
print(f"Both form and selection: {len(both)} -> {both[:10]}")

# Neither form nor selection nor drawing
read_only = [c['ten'] for c in commands if not c.get('can_form') and not c.get('can_chon') and not c.get('ghi_ban_ve')]
print(f"Neither form nor selection nor drawing (pure read/utility): {len(read_only)} -> {read_only[:10]}")
