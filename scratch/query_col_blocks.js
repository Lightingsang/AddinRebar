const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    var ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
    
    var blocks = new List<object>();

    foreach (ObjectId id in ms) {
      var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
      if (ent == null) continue;
      if (ent is BlockReference br && ent.Layer.Contains("Cot")) {
        var btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
        blocks.Add(new {
          btrName = btr?.Name,
          layer = br.Layer,
          x = Math.Round(br.Position.X, 1),
          y = Math.Round(br.Position.Y, 1)
        });
      }
    }

    return blocks;
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
