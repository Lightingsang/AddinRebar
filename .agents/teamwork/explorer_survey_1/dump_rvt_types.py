import subprocess
import json

dll_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"

cmd = [
    r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe",
    "--dump-table", "TypeDef",
    "--json",
    dll_path
]
res = subprocess.run(cmd, capture_output=True, text=True)
data = json.loads(res.stdout)

print(f"=== All Types in DBimRevit.dll ===")
for t in data["rows"]:
    token = t.get("Token")
    name = t.get("Name")
    ns = t.get("Namespace")
    print(f"[{token}] {ns}.{name}")
