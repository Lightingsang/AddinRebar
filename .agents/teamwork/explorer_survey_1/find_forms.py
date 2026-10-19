import subprocess
import json

form_names = [
    "DamCong_Frm", "MongCoc_Frm", "MongCoc_FrmLopThepThem", "MongCoc_FrmChonThepTuDong",
    "ThangMoi_Frm", "CocBT_Frm", "TKT_frmMain", "AutoStrWindow", "Bea_ToiUuCatThepV2",
    "DcePalette", "DcePalette_WallThickness", "DceRibbon"
]

cmd = [
    r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe",
    "--dump-table", "TypeDef",
    "--json",
    r"C:\Dce Pro\DCE Pro.dll"
]
res = subprocess.run(cmd, capture_output=True, text=True)
data = json.loads(res.stdout)

found_forms = {}
for row in data.get("rows", []):
    name = row.get("Name", "")
    for f in form_names:
        if f.lower() == name.lower():
            found_forms[f] = row

print(f"=== Found {len(found_forms)} / {len(form_names)} Forms in DCE Pro.dll ===")
for f in form_names:
    if f in found_forms:
        row = found_forms[f]
        print(f"FOUND: {f} | Token: {row.get('Token')} | Namespace: '{row.get('Namespace')}'")
    else:
        print(f"MISSING: {f}")
