import subprocess
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

dll_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"
cmd = [r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe", dll_path, "--dump-table", "TypeDef", "--json"]
res = subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8')
data = json.loads(res.stdout)

rows = data.get("rows", [])
print(f"Total TypeDef rows: {len(rows)}")

revit_types = []
for r in rows:
    name = r.get("Name", "")
    ns = r.get("Namespace", "")
    token = r.get("Token", "")
    full = f"{ns}.{name}" if ns else name
    revit_types.append((token, full))

print("\n--- All Types in DBimRevit.dll ---")
for tok, name in revit_types:
    print(f"  [{tok}] {name}")
