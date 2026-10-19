const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var f = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>()
      .FirstOrDefault(x => x.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == "ĐC-4-05");

    if (f == null) return "Footing ĐC-4-05 not found!";

    var bb = f.get_BoundingBox(null);
    return new {
      id = f.Id.Value,
      mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
      type = f.FloorType.Name,
      minX = bb.Min.X * 304.8,
      maxX = bb.Max.X * 304.8,
      minY = bb.Min.Y * 304.8,
      maxY = bb.Max.Y * 304.8,
      topZ = bb.Max.Z * 304.8,
      botZ = bb.Min.Z * 304.8
    };
  `;
  client.write(JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'revit.execute', params: { code: script, mode: 'none' } }) + '\n');
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
