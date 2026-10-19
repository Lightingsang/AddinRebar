import subprocess
import json

server_exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

cmd = [
    "python",
    r"McpShared/tools/mcp-call.py",
    server_exe,
    "tools/call",
    "search_tools",
    json.dumps({"query": ""})
]
proc = subprocess.run(cmd, capture_output=True, text=True)
res = json.loads(proc.stdout)
text = res["result"]["content"][0]["text"]
parsed = json.loads(text)
print("Search tools count:", len(parsed))
for item in parsed:
    print(f"  {item.get('name')}: status={item.get('status')}")
