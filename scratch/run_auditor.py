import os
import subprocess
import json

# Run RoslynSyntaxAuditor and capture full details
res = subprocess.run(
    ["dotnet", "run", "--project", "scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj"],
    cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar",
    capture_output=True,
    text=True
)
print("Roslyn Output:")
print(res.stdout)
if res.stderr:
    print("Roslyn Stderr:")
    print(res.stderr)
