const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var sel = doc.GetElement(new ElementId(9907702));
    var bb = sel.get_BoundingBox(null);
    var loc = (sel.Location as LocationPoint)?.Point;
    var pars = sel.Parameters.Cast<Parameter>().Where(p => p.HasValue).Select(p => new {
      name = p.Definition.Name,
      val = p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString()
    }).ToList();

    var center = loc ?? XYZ.Zero;
    var nearby = new FilteredElementCollector(doc)
      .WherePasses(new BoundingBoxIntersectsFilter(new Outline(center - new XYZ(10,10,10), center + new XYZ(10,10,10))))
      .WhereElementIsNotElementType()
      .Select(e => {
        var ebb = e.get_BoundingBox(null);
        return new {
          id = e.Id.Value,
          cat = e.Category?.Name,
          name = e.Name,
          mark = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
          minX = ebb != null ? ebb.Min.X * 304.8 : 0,
          maxX = ebb != null ? ebb.Max.X * 304.8 : 0,
          minY = ebb != null ? ebb.Min.Y * 304.8 : 0,
          maxY = ebb != null ? ebb.Max.Y * 304.8 : 0,
          minZ = ebb != null ? ebb.Min.Z * 304.8 : 0,
          maxZ = ebb != null ? ebb.Max.Z * 304.8 : 0
        };
      }).ToList();

    return new { pileParams = pars, nearby = nearby };
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
