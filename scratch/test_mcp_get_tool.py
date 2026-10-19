import subprocess
import json

server_exe = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

for tool in ["batch_edit_graph", "search_nodes_library", "get_active_graph_info"]:
    cmd = [
        "python",
        r"McpShared/tools/mcp-call.py",
        server_exe,
        "tools/call",
        "get_tool",
        json.dumps({"name": tool})
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    res = json.loads(proc.stdout)
    text = res["result"]["content"][0]["text"]
    parsed = json.loads(text)
    print(f"Tool '{tool}': status={parsed.get('status')}, isPublished={parsed.get('isPublished')}, isRunnable={parsed.get('isRunnable')}")
