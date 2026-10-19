import json, subprocess

code = """
var server = Grasshopper.Instances.ComponentServer;
var proxies = server.ObjectProxies;

var sb = new System.Text.StringBuilder();
sb.AppendLine($"Total ObjectProxies in ComponentServer: {proxies.Count}");

var revitObjects = new System.Collections.Generic.List<string>();
foreach (var p in proxies) {
    if (p.Category == "Revit" || (p.SubCategory != null && p.SubCategory.Contains("Revit")) || p.Desc.Name.Contains("Revit") || p.Desc.Name.Contains("Element")) {
        revitObjects.Add($"{p.Category} > {p.SubCategory} > {p.Desc.Name} (Guid: {p.Guid})");
    }
}

sb.AppendLine($"Found {revitObjects.Count} Revit/Element related components. Top 50:");
int count = 0;
foreach (var item in revitObjects) {
    if (item.Contains("Query") || item.Contains("Parameter") || item.Contains("Filter") || item.Contains("View") || item.Contains("Framing") || item.Contains("Beam")) {
        sb.AppendLine($"  * {item}");
        count++;
        if (count >= 50) break;
    }
}

return sb.ToString();
"""

payload = {"code": code}
with open("HPGrasshopper/test_query_proxies.json", "w", encoding="utf-8") as f:
    json.dump(payload, f)

cmd = ["python", "McpShared/tools/mcp-call.py", "HPGrasshopper/HPGrasshopper.Mcp.Server/bin/Debug/net10.0/HPGrasshopper.Mcp.Server.exe", "tools/call", "execute_grasshopper_code", "@HPGrasshopper/test_query_proxies.json", "--out", "HPGrasshopper/query_proxies_out.json"]
subprocess.run(cmd)

out = json.load(open("HPGrasshopper/query_proxies_out.json", encoding="utf-8"))
import sys
sys.stdout.buffer.write(out["result"]["content"][0]["text"].encode("utf-8"))
