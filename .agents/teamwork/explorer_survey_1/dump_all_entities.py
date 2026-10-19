import subprocess
import os

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"
out_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1"

targets = {
    "DCE_Pro": r"C:\Dce Pro\DCE Pro.dll",
    "DceSteel": r"C:\Dce Pro\AddinSteel\DceSteel.dll",
    "DbimSteelAutocad": r"C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll",
    "DCE_Excel": r"C:\Dce Pro\V.2020\DCE Excel.dll",
    "ZwDCE_Pro": r"C:\Dce Pro\ZwDCE Pro.dll",
    "IcDCE_Pro": r"C:\Dce Pro\IcDCE Pro.dll"
}

for name, path in targets.items():
    print(f"Extracting types for {name} ({path})...")
    out_file = os.path.join(out_dir, f"{name}_entities.txt")
    cmd = [ilspy, path, "-l", "cisde"]
    res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, errors="replace")
    if res.returncode == 0:
        with open(out_file, "w", encoding="utf-8") as f:
            f.write(res.stdout)
        lines = [line.strip() for line in res.stdout.splitlines() if line.strip()]
        print(f"  -> Success: {len(lines)} entities written to {out_file}")
    else:
        print(f"  -> Error: {res.stderr}")
