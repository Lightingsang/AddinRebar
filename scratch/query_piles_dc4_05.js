const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
      .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
      .Where(p => {
        var loc = p.Location as LocationPoint;
        return loc != null && loc.Point.X * 304.8 >= 6000 && loc.Point.X * 304.8 <= 7700
                           && loc.Point.Y * 304.8 >= 9000 && loc.Point.Y * 304.8 <= 10800;
      })
      .Select(p => {
        var bb = p.get_BoundingBox(null);
        var loc = p.Location as LocationPoint;
        var pOffset = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
        return new {
          id = p.Id.Value,
          mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
          x = loc != null ? loc.Point.X * 304.8 : 0,
          y = loc != null ? loc.Point.Y * 304.8 : 0,
          offsetMm = pOffset != null ? pOffset.AsDouble() * 304.8 : 0,
          topZ = bb != null ? bb.Max.Z * 304.8 : 0,
          botZ = bb != null ? bb.Min.Z * 304.8 : 0
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
        console.log(JSON.stringify(msg.result?.value, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
