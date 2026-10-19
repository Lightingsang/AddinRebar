import subprocess
import json
import time

exe = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"
p = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

def send_and_wait(req_id, method, params):
    req = {"jsonrpc": "2.0", "id": req_id, "method": method, "params": params}
    p.stdin.write(json.dumps(req) + "\n")
    p.stdin.flush()
    while True:
        line = p.stdout.readline()
        if not line:
            return None
        msg = json.loads(line)
        if msg.get("id") == req_id:
            return msg

# 1. Initialize
init_resp = send_and_wait(1, "initialize", {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {"name": "audit", "version": "1.0"}
})
print("Init received. Server info:", init_resp.get("result", {}).get("serverInfo"))

# 2. Initialized notification
p.stdin.write(json.dumps({"jsonrpc": "2.0", "method": "notifications/initialized"}) + "\n")
p.stdin.flush()
time.sleep(0.5)

# 3. tools/list
list_resp = send_and_wait(2, "tools/list", {})
tools = list_resp.get("result", {}).get("tools", [])
print(f"Total tools count: {len(tools)}")
tool_names = sorted([t["name"] for t in tools])
for idx, name in enumerate(tool_names, 1):
    print(f"  {idx:2d}. {name}")

# 4. inspect_type suppression check
call_resp = send_and_wait(3, "tools/call", {"name": "inspect_type", "arguments": {}})
print("inspect_type error code:", call_resp.get("error", {}).get("code"))
print("inspect_type error message:", call_resp.get("error", {}).get("message"))

p.stdin.close()
p.terminate()
