import subprocess
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

ilspy = r'C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe'
dll = r'C:\Dce Pro\DCE Pro.dll'
cmd = [ilspy, dll, '--dump-table', 'TypeDef', '--json']

proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
stdout, stderr = proc.communicate()

if proc.returncode != 0:
    print("ilspycmd failed:", stderr[:500])
    sys.exit(1)

data = json.loads(stdout)
rows = data.get('rows', [])
print(f"Total TypeDef rows: {len(rows)}")
if rows:
    print("First row keys:", list(rows[0].keys()))
    print("First row:", rows[0])

# Map RID/Token to row
# RID in ECMA-335 TypeDef table starts at 1, token is 0x02000000 | RID
token_map = {}
for idx, r in enumerate(rows):
    rid = idx + 1
    tok_int = 0x02000000 | rid
    tok_hex = f"0x{tok_int:08X}"
    token_map[tok_hex] = r
    # also if row has token or rid
    if 'rid' in r:
        r_rid = r['rid'] if isinstance(r['rid'], int) else int(str(r['rid']), 16)
        token_map[f"0x{0x02000000 | r_rid:08X}"] = r

# 1. Verify Subsystem 7 (11 CAD Jigs: 0x02000E52..0x02000E5C)
jigs_expected = {
    '0x02000E52': 'TagLeader',
    '0x02000E53': 'TagChiaThep',
    '0x02000E54': 'JigSlabRebar',
    '0x02000E55': 'VeMBKC_JIG',
    '0x02000E56': 'ijgRaithepchamtron',
    '0x02000E57': 'ijgVeDaiVong',
    '0x02000E58': 'JigVeDaiC',
    '0x02000E59': 'VePolylineBatKy',
    '0x02000E5A': 'ijgVeThepVachCT',
    '0x02000E5B': 'JigVeVachHam',
    '0x02000E5C': 'DimNhanhMBKC'
}

print("\n--- Verifying Subsystem 7 (11 CAD Jigs) ---")
jigs_verified = 0
for tok, exp_name in jigs_expected.items():
    if tok in token_map:
        row = token_map[tok]
        name = row.get('TypeName') or row.get('name') or row.get('Name')
        ns = row.get('TypeNamespace') or row.get('namespace') or row.get('Namespace')
        base = row.get('Extends') or row.get('extends') or row.get('BaseType')
        print(f"Token {tok}: actual={ns}.{name} (expected {exp_name}) | Extends: {base}")
        if exp_name in str(name):
            jigs_verified += 1
        else:
            print(f"  MISMATCH: expected {exp_name}, got {name}")
    else:
        print(f"Token {tok}: NOT FOUND in TypeDef table!")

print(f"Jigs verified: {jigs_verified} / {len(jigs_expected)}")

# 2. Verify Subsystem 8 (12 Specialized UI Forms & Palettes)
ui_expected = {
    '0x02000DA6': ('DamCong_Frm', 'DceDamMatCatMoi.DamCong_Frm'),
    '0x02000161': ('MongCoc_Frm', 'DceMongCocMoi.MongCoc_Frm'),
    '0x02000163': ('MongCoc_FrmLopThepThem', 'DceMongCocMoi.MongCoc_FrmLopThepThem'),
    '0x02000165': ('MongCoc_FrmChonThepTuDong', 'DceMongCocMoi.MongCoc_FrmChonThepTuDong'),
    '0x02000136': ('ThangMoi_Frm', 'DceThangMoi.ThangMoi_Frm'),
    '0x02000DCA': ('CocBT_Frm', 'DceVeCocBeTong.CocBT_Frm'),
    '0x02000246': ('TKT_frmMain', 'TKT_frmMain'),
    '0x02000B23': ('AutoStrWindow', 'AutoStr.AutoStrWindow'),
    '0x02000E5D': ('Bea_ToiUuCatThepV2', 'Bea_ToiUuCatThepV2'),
    '0x02000E12': ('DcePalette', 'DcePalette'),
    '0x02000E13': ('DcePalette_WallThickness', 'DcePalette_WallThickness'),
    '0x02000E1E': ('DceRibbon', 'DceRibbon')
}

print("\n--- Verifying Subsystem 8 (12 Specialized UI Forms & Palettes) ---")
ui_verified = 0
for tok, (short_name, exp_full) in ui_expected.items():
    if tok in token_map:
        row = token_map[tok]
        name = row.get('TypeName') or row.get('name') or row.get('Name')
        ns = row.get('TypeNamespace') or row.get('namespace') or row.get('Namespace')
        base = row.get('Extends') or row.get('extends') or row.get('BaseType')
        print(f"Token {tok}: actual={ns}.{name} (expected {short_name}) | Extends: {base}")
        if short_name in str(name):
            ui_verified += 1
        else:
            print(f"  MISMATCH: expected {short_name}, got {name}")
    else:
        print(f"Token {tok}: NOT FOUND in TypeDef table!")

print(f"UI forms verified: {ui_verified} / {len(ui_expected)}")
