import subprocess
import json
from pathlib import Path

node_script = r"C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"

targets = [
    ("DCE Pro.dll", r"C:\Dce Pro\DCE Pro.dll"),
    ("DceSteel.dll", r"C:\Dce Pro\AddinSteel\DceSteel.dll"),
    ("DbimSteelAutocad.dll", r"C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll"),
    ("DCE Excel.dll", r"C:\Dce Pro\V.2020\DCE Excel.dll"),
    ("DBimRevit.dll", r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"),
    ("DceAnalyssiss64.dll", r"C:\Dce Pro\DceAnalyssiss64.dll")
]

results = {}

for name, path in targets:
    print(f"Running rea on {name}...")
    cmd = ["node", node_script, "inspect-managed-artifact", path, "--format", "json"]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    if proc.returncode == 0:
        try:
            data = json.loads(proc.stdout)
            evidence = data.get("evidence", {})
            header = evidence.get("header", {})
            coverage = evidence.get("coverage", {})
            results[name] = {
                "success": True,
                "evidence_id": data.get("evidence_id"),
                "status": header.get("status"),
                "architecture": header.get("architecture"),
                "runtime_version": header.get("runtime_version"),
                "coverage_state": coverage.get("state"),
                "issues": coverage.get("issues", [])
            }
        except Exception as e:
            results[name] = {"success": False, "parse_error": str(e), "raw": proc.stdout[:300]}
    else:
        results[name] = {"success": False, "returncode": proc.returncode, "error": proc.stderr or proc.stdout[:300]}

out_path = Path(".agents/teamwork/explorer_survey_1/rea_inspections.json")
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(results, f, indent=2)

print("\n=== REA Inspection Results ===")
print(json.dumps(results, indent=2))
