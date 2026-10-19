import subprocess, os

test_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests"
for root, dirs, files in os.walk(test_dir):
    if "bin" in dirs: dirs.remove("bin")
    if "obj" in dirs: dirs.remove("obj")
    for f in sorted(files):
        if f.endswith(".cs"):
            p = os.path.join(root, f)
            with open(p, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
            non_empty = [l for l in lines if l.strip()]
            ps_cmd = ["powershell", "-NoProfile", "-Command", f"(Get-Content '{p}' | Measure-Object -Line).Lines"]
            res = subprocess.run(ps_cmd, capture_output=True, text=True)
            ps_lines = res.stdout.strip()
            print(f"{f:42} | Raw: {len(lines):3d} | Non-empty: {len(non_empty):3d} | PS: {ps_lines}")
