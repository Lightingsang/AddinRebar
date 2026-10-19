const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    var ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
    
    var colEntities = new List<object>();

    foreach (ObjectId id in ms) {
      var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
      if (ent == null) continue;
      if (ent.Layer.StartsWith("CHT-") || ent.Layer == "C-6" || ent.Layer.Contains("Cot")) {
        try {
          var ext = ent.GeometricExtents;
          double width = Math.Abs(ext.MaxPoint.X - ext.MinPoint.X);
          double height = Math.Abs(ext.MaxPoint.Y - ext.MinPoint.Y);
          colEntities.Add(new {
            layer = ent.Layer,
            type = ent.GetType().Name,
            width = Math.Round(width, 1),
            height = Math.Round(height, 1)
          });
        } catch {}
      }
    }

    return new {
      total = colEntities.Count,
      sample = colEntities.Take(15).ToList()
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
        console.log("Result:", JSON.stringify(msg.result, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
