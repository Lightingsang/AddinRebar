const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var piles = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFoundation)
      .WhereElementIsNotElementType().Cast<FamilyInstance>()
      .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250") || p.Symbol.Name.Contains("300"))
      .Take(15)
      .Select(p => {
        var bb = p.get_BoundingBox(null);
        var loc = p.Location as LocationPoint;
        var pt = loc != null ? loc.Point : XYZ.Zero;
        var hostLevel = doc.GetElement(p.LevelId) as Level;
        var pOffset = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
        var pElevAtTop = p.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
        var allParams = new List<object>();
        foreach (Parameter param in p.Parameters) {
          if (param.Definition.Name.Contains("Offset") || param.Definition.Name.Contains("Elevation") || param.Definition.Name.Contains("Cao") || param.Definition.Name.Contains("Level") || param.Definition.Name.Contains("Length") || param.Definition.Name.Contains("Dai")) {
            allParams.Add(new { name = param.Definition.Name, val = param.AsValueString() ?? (param.StorageType == StorageType.Double ? (param.AsDouble() * 304.8).ToString() : param.AsString()) });
          }
        }
        return new {
          id = p.Id.Value,
          mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
          family = p.Symbol.Family.Name,
          sym = p.Symbol.Name,
          level = hostLevel?.Name,
          levelElev = hostLevel != null ? hostLevel.Elevation * 304.8 : 0.0,
          offsetMm = pOffset != null ? pOffset.AsDouble() * 304.8 : 0.0,
          ptZ = pt.Z * 304.8,
          bbMinZ = bb != null ? bb.Min.Z * 304.8 : 0.0,
          bbMaxZ = bb != null ? bb.Max.Z * 304.8 : 0.0,
          parameters = allParams
        };
      }).ToList();
    return piles;
  `;

  const req = {
    jsonrpc: '2.0',
    id: 1,
    method: 'revit.execute',
    params: {
      code: script,
      mode: 'none'
    }
  };
  client.write(JSON.stringify(req) + '\n');
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
        console.log('Sample piles from Revit:');
        console.log(JSON.stringify(msg, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
