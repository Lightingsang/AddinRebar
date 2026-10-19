import json, subprocess

exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"
dyn_path = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"

with open(dyn_path, "r", encoding="utf-8") as f:
    content = f.read()

args = {
    "filePath": dyn_path,
    "fileContent": content,
    "checkPackages": True
}

with open("scratch/validate_args.json", "w", encoding="utf-8") as f:
    json.dump(args, f)

cmd = ["python", "McpShared/tools/mcp-call.py", exe, "tools/call", "validate_dyn_file", "@scratch/validate_args.json", "--out", "scratch/test_validate.json"]
res = subprocess.run(cmd, capture_output=True, text=True, cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
print("Returncode:", res.returncode)
