const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    var ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
    var texts = new List<object>();

    foreach (ObjectId id in ms) {
      var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
      if (ent == null) continue;
      if (ent is DBText dbt && Math.Abs(dbt.Position.X - 740000) < 50000 && Math.Abs(dbt.Position.Y - (-50000)) < 30000) {
        texts.Add(new { text = dbt.TextString, layer = dbt.Layer, x = Math.Round(dbt.Position.X, 1), y = Math.Round(dbt.Position.Y, 1) });
      } else if (ent is MText mt && Math.Abs(mt.Location.X - 740000) < 50000 && Math.Abs(mt.Location.Y - (-50000)) < 30000) {
        texts.Add(new { text = mt.Contents, layer = mt.Layer, x = Math.Round(mt.Location.X, 1), y = Math.Round(mt.Location.Y, 1) });
      }
    }

    return new { count = texts.Count, sample = texts.Take(20).ToList() };
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
        console.log(JSON.stringify(msg.result?.value, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
