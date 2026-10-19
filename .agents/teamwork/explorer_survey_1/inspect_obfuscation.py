import subprocess
import json
import re

def analyze_assembly(dll_path):
    print(f"\n==========================================")
    print(f"Analyzing: {dll_path}")
    print(f"==========================================")
    cmd = [
        r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe",
        "--dump-table", "TypeDef",
        "--json",
        dll_path
    ]
    res = subprocess.run(cmd, capture_output=True, text=True)
    if res.returncode != 0:
        print(f"Error dumping TypeDef: {res.stderr[:300]}")
        return

    data = json.loads(res.stdout)
    types = data.get("rows", [])
    print(f"Total TypeDefs: {len(types)}")

    # Check how many have readable non-obfuscated names
    # Obfuscated typically look like base64-like random letters/numbers of len > 12 without vowels/spaces or typical patterns
    readable_types = []
    obfuscated_types = []
    
    for t in types:
        name = t.get("Name", "")
        ns = t.get("Namespace", "")
        fullname = f"{ns}.{name}" if ns else name
        
        # heuristic for obfuscation: long random string with mixed case and digits, or empty namespace with random name
        if re.match(r'^[a-zA-Z0-9]{15,}$', name) or re.match(r'^[A-Z0-9]{15,}$', ns):
            obfuscated_types.append(fullname)
        elif name.startswith("<") or name.startswith("Costura"):
            # compiler generated or embedded
            pass
        else:
            readable_types.append(fullname)

    print(f"Readable / Semantic Types: {len(readable_types)}")
    print(f"Obfuscated Types: {len(obfuscated_types)}")
    print("Sample readable types (up to 20):")
    for r in readable_types[:20]:
        print(f"  - {r}")

targets = [
    r"C:\Dce Pro\DCE Pro.dll",
    r"C:\Dce Pro\AddinSteel\DceSteel.dll",
    r"C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll",
    r"C:\Dce Pro\V.2020\DCE Excel.dll",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll"
]

for t in targets:
    analyze_assembly(t)
