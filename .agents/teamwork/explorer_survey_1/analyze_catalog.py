import json
from pathlib import Path

catalog_path = Path(".agents/teamwork/explorer_survey_1/catalog_dlls.json")
with open(catalog_path, "r", encoding="utf-8") as f:
    dlls = json.load(f)

print(f"Total DLLs: {len(dlls)}")

managed = [d for d in dlls if d["pe"].get("is_managed")]
native = [d for d in dlls if d["pe"].get("is_pe") and not d["pe"].get("is_managed")]

print(f"Managed DLLs: {len(managed)}")
print(f"Native DLLs: {len(native)}")

print("\n--- Key Core Managed Assemblies in C:\\Dce Pro ---")
core_names = [
    "DCE Pro.dll", "DceSteel.dll", "DbimSteelAutocad.dll", "DCE Excel.dll", 
    "DBimRevit.dll", "ZwDCE Pro.dll", "GcDCE Pro.dll", "IcDCE Pro.dll",
    "DbimTaiKhoanApi.dll"
]

for d in dlls:
    rel = d["relative_path"]
    name = Path(rel).name
    if name in core_names or "AddinDbimDcepro" in rel:
        print(f"[{d['scope']}] {rel}")
        print(f"  Size: {d['size_bytes']} bytes ({d['size_mb']} MB)")
        print(f"  Arch: {d['pe'].get('machine')}, Managed: {d['pe'].get('is_managed')}, CLR: {d['pe'].get('clr_version')}")
        print(f"  SHA-256: {d['sha256']}")

print("\n--- Native Assemblies in C:\\Dce Pro and Unpacked ---")
for d in native:
    rel = d["relative_path"]
    name = Path(rel).name
    if any(k in name.lower() for k in ["dceanalyssiss", "sqlite", "sni", "vosk"]):
        print(f"[{d['scope']}] {rel}")
        print(f"  Size: {d['size_bytes']} bytes ({d['size_mb']} MB)")
        print(f"  Arch: {d['pe'].get('machine')}, HasExports: {d['pe'].get('has_exports')}")
        print(f"  SHA-256: {d['sha256']}")
