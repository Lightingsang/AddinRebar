const net = require('net');
const fs = require('fs');

const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const req = {
    jsonrpc: '2.0',
    id: 204,
    method: 'autocad.execute',
    params: {
      code: `
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var btr = (BlockTableRecord)tr.GetObject(bt["mm"], OpenMode.ForRead);

        double insX = 49268.5;
        double insY = 4400.0;

        var mongLines = new List<object>();

        foreach (ObjectId id in btr)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is Line l && l.Layer.Equals("MONG", StringComparison.OrdinalIgnoreCase))
            {
                mongLines.Add(new {
                    x1 = Math.Round(insX + l.StartPoint.X, 1),
                    y1 = Math.Round(insY + l.StartPoint.Y, 1),
                    x2 = Math.Round(insX + l.EndPoint.X, 1),
                    y2 = Math.Round(insY + l.EndPoint.Y, 1),
                    len = Math.Round(l.Length, 1)
                });
            }
        }

        return mongLines;
      `
    }
  };
  client.write(JSON.stringify(req) + '\n');
});

let buffer = '';
client.on('data', data => {
  buffer += data.toString();
  const lines = buffer.split('\n');
  buffer = lines.pop();
  for (const line of lines) {
    if (!line.trim()) continue;
    try {
      const msg = JSON.parse(line.trim());
      if (msg.id === 204) {
        fs.writeFileSync('scratch/cad_transformed_mong_lines.json', JSON.stringify(msg.result.value, null, 2));
        console.log('Saved! Total MONG lines:', msg.result.value.length);
        client.end();
      }
    } catch (e) {}
  }
});
