import subprocess
import json

def get_pinvokes(dll_path):
    cmd = [
        r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe",
        "--dump-table", "ImplMap",
        "--json",
        dll_path
    ]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        return []
    data = json.loads(res.stdout)
    return data.get("rows", [])

pinvokes_rvt = get_pinvokes(r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll")
pinvokes_dce = get_pinvokes(r"C:\Dce Pro\DCE Pro.dll")

print(f"ImplMap entries in DBimRevit.dll: {len(pinvokes_rvt)}")
for p in pinvokes_rvt[:20]:
    print(" ", p)

print(f"\nImplMap entries in DCE Pro.dll: {len(pinvokes_dce)}")
for p in pinvokes_dce[:20]:
    print(" ", p)
