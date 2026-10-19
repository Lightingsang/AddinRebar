const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    var ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
    
    var colMarks = new List<object>();

    foreach (ObjectId id in ms) {
      var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
      if (ent == null) continue;
      
      Point3d pt = Point3d.Origin;
      string text = "";
      if (ent is DBText dbt) { pt = dbt.Position; text = dbt.TextString; }
      else if (ent is MText mt) { pt = mt.Location; text = mt.Contents; }
      else continue;

      if (pt.X >= 800000 && pt.X <= 900000 && pt.Y >= -65000 && pt.Y <= -35000) {
        if (text.Contains("C") || text.Contains("x") || text.Contains("X")) {
          colMarks.Add(new {
            text = text,
            layer = ent.Layer,
            x = Math.Round(pt.X, 1),
            y = Math.Round(pt.Y, 1)
          });
        }
      }
    }

    return new {
      total = colMarks.Count,
      sample = colMarks.Take(30).ToList()
    };
  `;
  client.write(JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'autocad.execute', params: { code: script } }) + '\n');
});

let buffer = '';
client.on('data', chunk => {
  buffer += chunk.toString();
  const lines = buffer.split('\n');
  for (const line of lines) {
    if (!line.trim()) continue;
    try {
      const msg = JSON.parse(line);
      if (msg.id === 1) {
        console.log("Result:", JSON.stringify(msg.result?.value, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
