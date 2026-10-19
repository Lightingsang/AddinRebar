import subprocess
import json

commands = [
    "DBimRevit.CommandLogin",
    "DBimRevit.CommandLogout",
    "DBimRevit.CommandAddBeam",
    "DBimRevit.CommandAddColumn",
    "DBimRevit.CommandAddGird",
    "DBimRevit.CommandAddSlab",
    "DBimRevit.CommandAddWall",
    "DBimRevit.CommandAutoCut",
    "DBimRevit.CommandAutoJoin",
    "DBimRevit.CommandBatchUnjoin",
    "DBimRevit.CommandBeamDisallowJoin",
    "DBimRevit.CommandCreateCope",
    "DBimRevit.CommandJoinByOrder",
    "DBimRevit.DceBeamBreaks",
    "DBimRevit.CommandDatTenDam",
    "DBimRevit.CommandDceSetup",
    "DBimRevit.CommandExportBeamToExcel",
    "DBimRevit.CommandExportToAutocad",
    "DBimRevit.CommandCreateAnalytical",
    "DBimRevit.CommandEtabsToRevit",
    "DBimRevit.CommandExportToEtabs",
    "DBimRevit.CommandXuatKhoiLuong",
    "DBimRevit.CommandAddRebarBeam",
    "DBimRevit.AdskApplication"
]

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"
dll = r"scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"

results = {}
for cmd in commands:
    res = subprocess.run([ilspy, dll, "-t", cmd], capture_output=True, text=True)
    code = res.stdout
    # extract class line, base class, inner types
    lines = [l.strip() for l in code.splitlines() if l.strip()]
    class_decl = [l for l in lines if ("public class" in l or "public sealed class" in l)]
    attributes = [l for l in lines if l.startswith("[") and l.endswith("]") and "Compiler" not in l and "Debugger" not in l]
    results[cmd] = {
        "decl": class_decl[0] if class_decl else "",
        "attributes": attributes,
        "raw": code
    }
    print(f"=== {cmd} ===")
    print("  Decl:", class_decl[0] if class_decl else "")
    print("  Attrs:", attributes)

with open('.agents/teamwork/spec_miner_m1_3/revit_decompiled_summary.json', 'w', encoding='utf-8') as f:
    json.dump(results, f, indent=2, ensure_ascii=False)
