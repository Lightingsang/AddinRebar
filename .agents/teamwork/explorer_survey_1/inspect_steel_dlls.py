import subprocess

ilspy = r"C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe"

for dll in [r"C:\Dce Pro\AddinSteel\DceSteel.dll", r"C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll"]:
    print(f"=== Inspecting {dll} ===")
    cmd = [ilspy, dll, "-l", "c"]
    res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, errors="replace")
    lines = [l.strip() for l in res.stdout.splitlines() if l.strip()]
    print(f"Total classes: {len(lines)}")
    for l in lines[:25]:
        print(f"  {l}")
