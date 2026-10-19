import subprocess
import json

node_script = r"C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"
target = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"

cmd = ["node", node_script, "inspect-managed-artifact", target, "--format", "json"]
proc = subprocess.run(cmd, capture_output=True, text=True)
data = json.loads(proc.stdout)

print("Top keys:", list(data.keys()))
evidence = data.get("evidence", {})
print("Evidence keys:", list(evidence.keys()))
for k, v in evidence.items():
    if isinstance(v, (str, int, float, bool, type(None))):
        print(f"  {k}: {v}")
    elif isinstance(v, dict):
        print(f"  {k} (dict): {list(v.keys())}")
    elif isinstance(v, list):
        print(f"  {k} (list): len={len(v)}")
