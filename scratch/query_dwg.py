import json
import subprocess
import os

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
SERVER = os.path.join(REPO, "HPAutoCad", "output", "HPAutoCad.Mcp.Server", "HPAutoCad.Mcp.Server.exe")
CALL_PY = os.path.join(REPO, "McpShared", "tools", "mcp-call.py")

csharp_code = """
record TagItem(double X, double Y, string Tag);
var dict = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<TagItem>>();
var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

foreach (ObjectId id in btr)
{
    if (id.ObjectClass.DxfName == "INSERT")
    {
        var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
        string effName = br.IsDynamicBlock 
            ? ((BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead)).Name 
            : ((BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead)).Name;

        if (effName == "kata_block_KHT")
        {
            string sh = "";
            string tag = "";
            foreach (ObjectId aid in br.AttributeCollection)
            {
                var at = (AttributeReference)tr.GetObject(aid, OpenMode.ForRead);
                if (at.Tag == "SH2" && !string.IsNullOrWhiteSpace(at.TextString)) sh = at.TextString;
                if (at.Tag == "DKKC1" && !string.IsNullOrWhiteSpace(at.TextString)) tag = at.TextString;
            }
            if (!string.IsNullOrEmpty(sh))
            {
                if (!dict.ContainsKey(sh)) dict[sh] = new System.Collections.Generic.List<TagItem>();
                dict[sh].Add(new TagItem(Math.Round(br.Position.X, 1), Math.Round(br.Position.Y, 1), tag));
            }
        }
    }
}

var res = dict.Select(kv => new {
    Mark = int.TryParse(kv.Key, out int m) ? m : 999,
    MarkStr = kv.Key,
    Tag = kv.Value.First().Tag,
    Count = kv.Value.Count,
    MinX = kv.Value.Min(i => i.X),
    MaxX = kv.Value.Max(i => i.X),
    MinY = kv.Value.Min(i => i.Y),
    MaxY = kv.Value.Max(i => i.Y)
}).OrderBy(r => r.Mark).ToList();

return res;
"""

payload = json.dumps({"code": csharp_code})
env = os.environ.copy()
env["PYTHONIOENCODING"] = "utf-8"

res = subprocess.run(["python", CALL_PY, SERVER, "tools/call", "execute_autocad_code", payload],
                     capture_output=True, text=True, encoding="utf-8", env=env)

print("Exit code:", res.returncode)
print("STDOUT:", res.stdout[:4000])
print("STDERR:", res.stderr[:4000])
