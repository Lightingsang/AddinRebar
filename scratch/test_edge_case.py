import subprocess
import json

server_exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

# Non-existent tool
cmd = ["python", "McpShared/tools/mcp-call.py", server_exe, "tools/call", "get_tool", '{"name": "nonexistent_tool_123"}']
res = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
print("Nonexistent tool response:")
print(res.stdout)

# Non-existent runner tool
cmd2 = ["python", "McpShared/tools/mcp-call.py", server_exe, "tools/call", "nonexistent_direct_tool", '{}']
res2 = subprocess.run(cmd2, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
print("Nonexistent direct tool response:")
print(res2.stdout)
