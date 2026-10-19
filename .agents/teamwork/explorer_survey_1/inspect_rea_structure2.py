import subprocess
import json

node_script = r"C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"
target = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"

cmd = ["node", node_script, "inspect-managed-artifact", target, "--format", "json"]
proc = subprocess.run(cmd, capture_output=True, text=True)
data = json.loads(proc.stdout)

norm = data.get("normalized_result", {})
print("normalized_result keys:", list(norm.keys()))
for k, v in norm.items():
    if isinstance(v, (str, int, float, bool, type(None))):
        print(f"  {k}: {v}")
    elif isinstance(v, dict):
        print(f"  {k} (dict): {list(v.keys())}")
    elif isinstance(v, list):
        print(f"  {k} (list): len={len(v)}")

if "classification" in norm:
    print("classification:", json.dumps(norm["classification"], indent=2))

if "assembly" in norm:
    print("assembly:", json.dumps(norm["assembly"], indent=2))

if "coverage" in norm:
    print("coverage:", json.dumps(norm["coverage"], indent=2))
