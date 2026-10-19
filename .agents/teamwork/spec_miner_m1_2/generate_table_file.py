import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open('autocad_311_matrix.json', 'r', encoding='utf-8') as f:
    commands = json.load(f)

# Sort commands by index
commands.sort(key=lambda x: x['index'])

lines = []
lines.append("# Master Truth Matrix: 311 AutoCAD Commands\n")
lines.append("| # | Lệnh CAD | Nhóm nghiệp vụ | Mô tả tiếng Việt | Cần chọn (`can_chon`) | Mở Form (`can_form`) | Ghi bản vẽ (`ghi_ban_ve`) | Target File trong `HPDcePro.AutoCad` | Trạng thái hiện tại |")
lines.append("|---|----------|----------------|------------------|----------------------|----------------------|---------------------------|---------------------------------------|---------------------|")

for c in commands:
    idx = c['index']
    name = c['name']
    cat = c['category']
    mota = c['description'].replace('|', '/')
    can_chon = c['can_chon']
    can_form = c['can_form']
    ghi_ban_ve = c['ghi_ban_ve']
    target = c['target']
    status = c['status']
    lines.append(f"| {idx} | `{name}` | {cat} | {mota} | {can_chon} | {can_form} | {ghi_ban_ve} | `{target}` | **{status}** |")

output_path = 'commands_311_table.md'
with open(output_path, 'w', encoding='utf-8') as f:
    f.write("\n".join(lines))

print(f"Generated commands table with {len(commands)} rows: {output_path}")
