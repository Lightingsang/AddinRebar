import subprocess, json

exe = r"HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

ds_code = """
p1 = Point.ByCoordinates(0, 0, 0);
p2 = Point.ByCoordinates(10, 0, 0);
ln = Line.ByStartPointEndPoint(p1, p2);
return = ln;
"""

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
        "name": "execute_designscript",
        "arguments": {
            "script": ds_code
        }
    }
})

proc.terminate()
raw = res["result"]["content"][0]["text"]
print(raw)
