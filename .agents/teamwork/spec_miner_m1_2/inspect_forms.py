import subprocess
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

forms = [
    ("0x02000136", "ClassLibrary1.DceThangMoi.ThangMoi_Frm", "ThangMoi_Frm"),
    ("0x02000161", "ClassLibrary1.DceMongCocMoi.MongCoc_Frm", "MongCoc_Frm"),
    ("0x02000163", "ClassLibrary1.DceMongCocMoi.MongCoc_FrmLopThepThem", "MongCoc_FrmLopThepThem"),
    ("0x02000165", "ClassLibrary1.DceMongCocMoi.MongCoc_FrmChonThepTuDong", "MongCoc_FrmChonThepTuDong"),
    ("0x02000246", "ClassLibrary1.TKT_frmMain", "TKT_frmMain"),
    ("0x02000B23", "ClassLibrary1.AutoStr.AutoStrWindow", "AutoStrWindow"),
    ("0x02000DA6", "ClassLibrary1.DceDamMatCatMoi.DamCong_Frm", "DamCong_Frm"),
    ("0x02000DCA", "ClassLibrary1.DceVeCocBeTong.CocBT_Frm", "CocBT_Frm"),
    ("0x02000E12", "ClassLibrary1.DcePalette", "DcePalette"),
    ("0x02000E13", "ClassLibrary1.DcePalette_WallThickness", "DcePalette_WallThickness"),
    ("0x02000E1E", "ClassLibrary1.DceRibbon", "DceRibbon"),
    ("0x02000E5D", "ClassLibrary1.Bea_ToiUuCatThepV2", "Bea_ToiUuCatThepV2")
]

for token, full_name, short_name in forms:
    cmd = [r'C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe', r'C:\Dce Pro\DCE Pro.dll', '-t', full_name]
    proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
    out = proc.stdout
    print(f"=== {short_name} ({token}) ===")
    lines = out.splitlines()
    print(f"Total lines: {len(lines)}")
    # Find base class and declaration
    for line in lines[:25]:
        if any(kw in line for kw in ['class ', 'public partial', 'public class', 'inherits']):
            print(f"  Declaration: {line.strip()}")
    print()
