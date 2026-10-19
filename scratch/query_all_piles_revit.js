const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
      .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
      .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250") || p.Symbol.Name.Contains("300"))
      .Select(p => {
        var bb = p.get_BoundingBox(null);
        var loc = p.Location as LocationPoint;
        var pOffset = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
        return new {
          id = p.Id.Value,
          mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
          x = loc != null ? Math.Round(loc.Point.X * 304.8, 1) : 0,
          y = loc != null ? Math.Round(loc.Point.Y * 304.8, 1) : 0,
          offsetMm = pOffset != null ? Math.Round(pOffset.AsDouble() * 304.8, 1) : 0,
          topZ = bb != null ? Math.Round(bb.Max.Z * 304.8, 1) : 0
        };
      }).ToList();
    return piles;
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
        if (msg.result?.isError) {
          console.log('Error:', msg.result.message, msg.result.diagnostics);
        }
        const piles = msg.result?.value || [];
        console.log('Total piles returned:', piles.length);
        const byTopZ = {};
        piles.forEach(p => {
          byTopZ[p.topZ] = (byTopZ[p.topZ] || 0) + 1;
        });
        console.log('Distribution by topZ:', byTopZ);
        const byOffset = {};
        piles.forEach(p => {
          byOffset[p.offsetMm] = (byOffset[p.offsetMm] || 0) + 1;
        });
        console.log('Distribution by offsetMm:', byOffset);
        
        // Find piles with anomalous topZ
        const anomalous = piles.filter(p => p.topZ > -2400 || p.topZ < -2900);
        console.log('Anomalous piles count:', anomalous.length);
        if (anomalous.length > 0) {
          console.log('Sample anomalous:', anomalous.slice(0, 10));
        }

        // Piles near Grid 8 (X around 29050)
        const nearGrid8 = piles.filter(p => Math.abs(p.x - 29050) < 1500);
        console.log('Piles near Grid 8:', nearGrid8.length);
        console.log(nearGrid8.slice(0, 5));

        client.end();
        break;
      }
    } catch {}
  }
});
