import json, subprocess, os

code = """
var rDoc = (object)revitDoc as Document ?? (object)doc as Document;
var tags = new FilteredElementCollector(rDoc)
    .OfCategory(BuiltInCategory.OST_StructuralFramingTags)
    .WhereElementIsElementType()
    .Cast<FamilySymbol>()
    .ToList();

var sb = new System.Text.StringBuilder();
sb.AppendLine($"Total Structural Framing Tag types: {tags.Count}");
foreach (var t in tags) {
    sb.AppendLine($"  - Tag: '{t.Name}', Family: '{t.FamilyName}', Id: {t.Id}");
}

// Check existing tags in view
var activeView = rDoc.ActiveView;
var existingTags = new FilteredElementCollector(rDoc, activeView.Id)
    .OfCategory(BuiltInCategory.OST_StructuralFramingTags)
    .WhereElementIsNotElementType()
    .GetElementCount();
sb.AppendLine($"Existing Framing Tags in active view '{activeView.Name}': {existingTags}");

return sb.ToString();
"""

with open("HPGrasshopper/test_tags.json", "w", encoding="utf-8") as f:
    json.dump({"code": code}, f)

cmd = ["python", "McpShared/tools/mcp-call.py", "HPGrasshopper/HPGrasshopper.Mcp.Server/bin/Debug/net10.0/HPGrasshopper.Mcp.Server.exe", "tools/call", "execute_grasshopper_code", "@HPGrasshopper/test_tags.json", "--out", "HPGrasshopper/tags_out.json"]
subprocess.run(cmd)

out = json.load(open("HPGrasshopper/tags_out.json", encoding="utf-8"))
import sys
sys.stdout.buffer.write(out["result"]["content"][0]["text"].encode("utf-8"))

if os.path.exists("HPGrasshopper/test_tags.json"): os.remove("HPGrasshopper/test_tags.json")
if os.path.exists("HPGrasshopper/tags_out.json"): os.remove("HPGrasshopper/tags_out.json")
