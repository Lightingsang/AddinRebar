import subprocess, json

exe = r"HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe"

csharp = """
dynamic dm = dynamo;
dynamic search = dm?.SearchModel;
if (search == null) return new { error = "SearchModel is null" };

var queries = new [] { "BeamByCurve", "ByStartPointEndPoint", "Point.ByCoordinates", "Level.ByName", "FamilyType.ByName", "SetParameterByName", "ImportJSON", "ReadText" };
var results = new Dictionary<string, List<object>>();

foreach (var q in queries) {
    var matches = new List<object>();
    foreach (dynamic e in search.Entries) {
        string name = e.Name?.ToString() ?? "";
        string fullName = e.FullName?.ToString() ?? "";
        string creationName = e.CreationName?.ToString() ?? "";
        if (name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || fullName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) {
            matches.Add(new { name = name, fullName = fullName, creationName = creationName });
            if (matches.Count >= 3) break;
        }
    }
    results[q] = matches;
}

return results;
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
        "name": "execute_dynamo_code",
        "arguments": {
            "code": csharp,
            "dryRun": False,
            "timeoutSeconds": 30
        }
    }
})

proc.terminate()

raw = res["result"]["content"][0]["text"]
data = json.loads(raw)
print(json.dumps(data, indent=2, ensure_ascii=False))
