import subprocess, json

exe = r"HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"
target_file = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\VeHeDamTang1_DaKieng.dyn"

with open(target_file, "r", encoding="utf-8") as f:
    content = f.read()

proc = subprocess.Popen([exe], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

def send(msg):
    proc.stdin.write((json.dumps(msg) + "\n").encode("utf-8"))
    proc.stdin.flush()
    while True:
        line = proc.stdout.readline()
        if not line: break
        try:
            data = json.loads(line.decode("utf-8"))
            if data.get("id") == msg.get("id"):
                return data
        except:
            pass

send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0"}}})

res = send({
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
        "name": "validate_dyn_file",
        "arguments": {
            "filePath": target_file,
            "fileContent": content,
            "checkPackages": False
        }
    }
})

proc.terminate()

raw = res["result"]["content"][0]["text"]
data = json.loads(raw)
print(json.dumps(data, indent=2, ensure_ascii=False))
