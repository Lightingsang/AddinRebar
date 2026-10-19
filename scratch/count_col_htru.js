const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    if (!bt.Has("COL-HTRU")) return new { found = false };
    var btr = tr.GetObject(bt["COL-HTRU"], OpenMode.ForRead) as BlockTableRecord;
    int count = 0;
    foreach (ObjectId id in btr) count++;
    return new { found = true, count };
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
        console.log("Full Result:", JSON.stringify(msg, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
