import subprocess

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"
out_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1"

for typ in ["ClassLibrary1.DceRebarXDataTKT", "ClassLibrary1.Links_XDataObject"]:
    out_file = f"{typ.split('.')[-1]}_decompiled.cs"
    print(f"Decompiling {typ}...")
    cmd = [ilspy, r"C:\Dce Pro\DCE Pro.dll", "-t", typ]
    res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, errors="replace")
    if res.returncode == 0:
        with open(f"{out_dir}\\{out_file}", "w", encoding="utf-8") as f:
            f.write(res.stdout)
        print(f"  -> Wrote {out_file} ({len(res.stdout.splitlines())} lines)")
    else:
        print(f"  -> Error: {res.stderr}")
