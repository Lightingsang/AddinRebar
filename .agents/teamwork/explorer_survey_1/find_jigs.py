import subprocess
import json

jig_names = [
    "VeMBKC_JIG", "JigSlabRebar", "TagLeader", "TagChiaThep", 
    "ijgRaithepchamtron", "ijgVeDaiVong", "JigVeDaiC", "VePolylineBatKy", 
    "ijgVeThepVachCT", "JigVeVachHam", "DimNhanhMBKC"
]

cmd = [
    r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe",
    "--dump-table", "TypeDef",
    "--json",
    r"C:\Dce Pro\DCE Pro.dll"
]
res = subprocess.run(cmd, capture_output=True, text=True)
data = json.loads(res.stdout)

found_jigs = {}
for row in data.get("rows", []):
    name = row.get("Name", "")
    for j in jig_names:
        if j.lower() == name.lower():
            found_jigs[j] = row

print(f"=== Found {len(found_jigs)} / {len(jig_names)} Jigs in DCE Pro.dll ===")
for j in jig_names:
    if j in found_jigs:
        row = found_jigs[j]
        print(f"FOUND: {j} | Token: {row.get('Token')} | Namespace: '{row.get('Namespace')}' | BaseType: {row.get('BaseType')}")
    else:
        print(f"MISSING: {j}")
