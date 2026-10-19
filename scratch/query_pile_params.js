const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var sel = doc.GetElement(new ElementId(9907702)) as FamilyInstance;
    var instParams = sel.Parameters.Cast<Parameter>().Where(p => p.HasValue).Select(p => p.Definition.Name + ": " + (p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString())).ToList();
    var sym = sel.Symbol;
    var symParams = sym.Parameters.Cast<Parameter>().Where(p => p.HasValue).Select(p => p.Definition.Name + ": " + (p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString())).ToList();
    return new { instParams, symParams, familyName = sym.Family.Name, symName = sym.Name };
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
