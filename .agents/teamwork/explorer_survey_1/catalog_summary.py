import json
from pathlib import Path

catalog_path = Path(".agents/teamwork/explorer_survey_1/catalog_dlls.json")
with open(catalog_path, "r", encoding="utf-8") as f:
    dlls = json.load(f)

# Group DLLs
dce_core = []
cad_ports = []
unpacked_rvt = []
native_dlls = []
third_party = []

for d in dlls:
    scope = d["scope"]
    rel = d["relative_path"]
    name = Path(rel).name
    is_managed = d["pe"].get("is_managed", False)

    if scope == "Revit Unpacked":
        unpacked_rvt.append(d)
    elif not is_managed:
        native_dlls.append(d)
    elif name in ["ZwDCE Pro.dll", "GcDCE Pro.dll", "IcDCE Pro.dll"]:
        cad_ports.append(d)
    elif any(core in rel for core in ["DCE Pro.dll", "DceSteel.dll", "DbimSteelAutocad.dll", "DCE Excel.dll", "DbimTaiKhoanApi.dll"]):
        dce_core.append(d)
    else:
        third_party.append(d)

print(f"DCE Core Managed: {len(dce_core)}")
print(f"CAD Ports (Excluded): {len(cad_ports)}")
print(f"Native DLLs: {len(native_dlls)}")
print(f"Revit 2026 Unpacked: {len(unpacked_rvt)}")
print(f"Third-Party / Dependencies: {len(third_party)}")

# Print markdown tables for report
def print_table(title, items):
    print(f"\n### {title} ({len(items)} files)")
    print("| Relative Path | Size (Bytes) | Size (MB) | Arch | Managed | CLR | SHA-256 |")
    print("|---|---|---|---|---|---|---|")
    for item in sorted(items, key=lambda x: x["relative_path"]):
        p = item["pe"]
        arch = p.get("machine", "Unknown")
        mng = "Yes" if p.get("is_managed") else "No"
        clr = p.get("clr_version", "-") or "-"
        sha = item["sha256"][:16] + "..."
        print(f"| `{item['relative_path']}` | {item['size_bytes']:,} | {item['size_mb']} | {arch} | {mng} | {clr} | `{item['sha256']}` |")

print_table("Core Dce Pro Managed Assemblies", dce_core)
print_table("CAD Vertical Ports (ZWCAD / GstarCAD / IntelliCAD)", cad_ports)
print_table("Native PE DLLs (C++ / C)", native_dlls)
print_table("Extracted Revit 2026 Add-in Binaries (`scratch/dce_rvt_unpacked`)", unpacked_rvt)
