import subprocess
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

dll_path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"

def get_table(tbl):
    cmd = [r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe", dll_path, "--dump-table", tbl, "--json"]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8')
    if res.returncode == 0:
        return json.loads(res.stdout).get("rows", [])
    return []

typedefs = {int(r["Token"], 16): r for r in get_table("TypeDef")}
typerefs = {int(r["Token"], 16): r for r in get_table("TypeRef")}
interface_impls = get_table("InterfaceImpl")

print(f"Total InterfaceImpl rows: {len(interface_impls)}")

commands = []
for impl in interface_impls:
    class_tok = int(impl.get("Class", "0"), 16)
    iface_tok = int(impl.get("Interface", "0"), 16)
    c_type = typedefs.get(class_tok, {})
    c_name = f"{c_type.get('Namespace', '')}.{c_type.get('Name', '')}".strip('.')
    
    # Interface can be TypeRef or TypeDef
    if iface_tok in typerefs:
        i_type = typerefs[iface_tok]
        i_name = f"{i_type.get('TypeNamespace', '')}.{i_type.get('TypeName', '')}".strip('.')
    elif iface_tok in typedefs:
        i_type = typedefs[iface_tok]
        i_name = f"{i_type.get('Namespace', '')}.{i_type.get('Name', '')}".strip('.')
    else:
        i_name = f"Token_{hex(iface_tok)}"

    commands.append((c_name, i_name, c_type.get("Token", "")))

print("\nAll Interface Implementations:")
for c_name, i_name, tok in commands:
    print(f"  [{tok}] {c_name} implements {i_name}")
