const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    return "AutoCAD pipe connected!";
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
        console.log(JSON.stringify(msg, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
client.on('error', err => {
  console.error("CAD pipe error:", err.message);
});
