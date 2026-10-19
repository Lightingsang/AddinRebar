import json, subprocess, sys

exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"
code = """
try {
    Autodesk.Revit.DB.Structure.StructuralFramingUtils.DisallowJoinAtEnd(null, 0);
} catch (Exception ex) {
    log("DisallowJoinAtEnd exists: " + ex.Message);
}
return "Compiled successfully";
"""

args = {
    "code": code,
    "dryRun": True,
    "transaction": "none"
}

cmd = ["python", "McpShared/tools/mcp-call.py", exe, "tools/call", "execute_dynamo_code", json.dumps(args), "--out", "scratch/test_out.json"]
res = subprocess.run(cmd, capture_output=True, text=True, cwd=r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar")
print("Done. Returncode:", res.returncode)
