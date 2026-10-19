import subprocess
import json

server_exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

tool = "batch_edit_graph"
cmd = [
    "python",
    r"McpShared/tools/mcp-call.py",
    server_exe,
    "tools/call",
    json.dumps({"name": "get_tool", "arguments": {"name": tool}})
]
proc = subprocess.run(cmd, capture_output=True, text=True)
print("STDOUT:")
print(proc.stdout)
print("STDERR:")
print(proc.stderr)
