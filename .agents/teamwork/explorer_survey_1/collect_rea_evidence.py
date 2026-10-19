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
    ("DceAnalyssiss64.dll", r"C:\Dce Pro\DceAnalyssiss64.dll"),
    ("DceAnalyssiss32.dll", r"C:\Dce Pro\DceAnalyssiss32.dll"),
]

evidence_records = []

for label, path in targets:
    print(f"Inspecting {label}...")
    cmd = ["node", node_script, "inspect-managed-artifact", path, "--format", "json"]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    if proc.returncode != 0:
        print(f"Error on {label}: {proc.stderr[:300]}")
        continue
    
    data = json.loads(proc.stdout)
    norm = data.get("normalized_result", {})
    art = norm.get("artifact", {})
    pe = norm.get("pe", {})
    cls = norm.get("classification", {})
    asm = norm.get("assembly", {}) or {}
    cov = norm.get("coverage", {}) or {}
    refs = norm.get("references", [])
    res = norm.get("resources", [])

    record = {
        "label": label,
        "path": path,
        "evidence_id": data.get("evidence_id"),
        "sha256": art.get("sha256"),
        "byte_length": art.get("byte_length"),
        "format": art.get("format"),
        "pe_architecture": pe.get("architecture"),
        "classification_status": cls.get("status"),
        "runtime_family": cls.get("runtime_family"),
        "managed_architecture": cls.get("managed_architecture"),
        "assembly_name": asm.get("name"),
        "assembly_version": asm.get("version"),
        "assembly_token": asm.get("token"),
        "coverage_state": cov.get("state"),
        "coverage_issues": cov.get("issues", []),
        "references_count": len(refs),
        "key_references": [r.get("name") for r in refs[:10] if isinstance(r, dict)],
        "resources_count": len(res)
    }
    evidence_records.append(record)

out_file = Path(".agents/teamwork/explorer_survey_1/rea_evidence_summary.json")
with open(out_file, "w", encoding="utf-8") as f:
    json.dump(evidence_records, f, indent=2)

print("\n=== Summary Table ===")
for r in evidence_records:
    print(f"{r['label']:<20} | {r['byte_length']:>10} B | {r['classification_status']:<12} | {r['pe_architecture']:<6} | {r['coverage_state']:<8} | {r['evidence_id']}")
