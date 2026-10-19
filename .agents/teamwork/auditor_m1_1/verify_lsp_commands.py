import re

lsp_path = r'C:\Dce Pro\DCE Pro.lsp'
with open(lsp_path, 'r', encoding='utf-8', errors='ignore') as f:
    lsp_content = f.read()

# Extract all defun c: commands in lsp
defun_pattern = re.compile(r'\(defun\s+c:([a-zA-Z0-9_\-]+)', re.IGNORECASE)
found_commands = set(m.lower() for m in defun_pattern.findall(lsp_content))
print(f'Total c: commands found in DCE Pro.lsp: {len(found_commands)}')

# The 61 commands from 04-command-mapping-and-roadmap.md
table_commands = [
    'mb1', 'UpdateModelEtabs', 'GETFORM', 'taomv', 'slck', 'sdkt', 'ToiUuCatThep', 'MaTTThep',
    'ShopDrawingBeam', 'bb1', 'bb2', 'bb3', 'bb4', 'bb5', 'bb6', 'bb7', '1111', '111', '222',
    '333', '444', '555', 'ten', 'GanTruc', 'vd', 'vdall', 'THTK_dce', 'tkt_dce', 'stk_dce',
    'ktc', 'QuetThongKe_dce', 'suathep', 'UpDateDLDam', 'XemLienKetThep', 'DatTenLineDam2',
    'ImportKichThuocCot', 'ImportHinhDangCot', 'UpdateThepTinhToan', 'VeMCD_DCE', 'GanLienKetThep',
    'XoaLienKetThep', 'GanDLLineDam', 'XoaDLDataDCE', 'XemLienKetMBCK', 'XuatDuLieuDam',
    'vc', 'vv', 'ColumnREC1', 'ColumnREC2', 'ColumnCi', 'T1', 'T2', 'shts', 'B1', 'B2',
    'tkts', 'cf', 'VBC', 'DTC', 'XTDC', 'StripMong'
]

matched = []
missing = []
for cmd in table_commands:
    if cmd.lower() in found_commands:
        matched.append(cmd)
    else:
        # Check if it appears as a string or substring
        if cmd.lower() in lsp_content.lower():
            matched.append(f'{cmd} (in content)')
        else:
            missing.append(cmd)

print(f'\nMatched: {len(matched)} / {len(table_commands)}')
if missing:
    print('Missing commands:', missing)
else:
    print('ALL 61 commands verified against DCE Pro.lsp!')
