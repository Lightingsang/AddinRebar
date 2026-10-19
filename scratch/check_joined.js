const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var p = doc.GetElement(new ElementId(9907667)) as FamilyInstance; // P1
    var floors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();
    var joinedFloors = new List<string>();
    foreach (var f in floors) {
      if (JoinGeometryUtils.AreElementsJoined(doc, p, f)) {
        joinedFloors.Add(f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? f.Name);
      }
    }
    return new { joinedFloors };
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
