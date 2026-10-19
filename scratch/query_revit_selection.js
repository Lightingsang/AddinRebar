const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var selIds = uidoc.Selection.GetElementIds();
    var selList = new List<object>();
    foreach (var id in selIds) {
      var elem = doc.GetElement(id);
      var bb = elem.get_BoundingBox(null);
      var cat = elem.Category?.Name;
      var locPt = (elem.Location as LocationPoint)?.Point;
      var pOffset = elem.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
      var hostLevel = elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsValueString()
                   ?? elem.get_Parameter(BuiltInParameter.LEVEL_PARAM)?.AsValueString();
      selList.Add(new {
        id = id.Value,
        name = elem.Name,
        category = cat,
        mark = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
        x = locPt != null ? locPt.X * 304.8 : 0,
        y = locPt != null ? locPt.Y * 304.8 : 0,
        z = locPt != null ? locPt.Z * 304.8 : 0,
        offsetMm = pOffset != null ? pOffset.AsDouble() * 304.8 : 0,
        bbMinZ = bb != null ? bb.Min.Z * 304.8 : 0,
        bbMaxZ = bb != null ? bb.Max.Z * 304.8 : 0
      });
    }
    return selList;
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
        console.log('Active selection in Revit:');
        console.log(JSON.stringify(msg.result?.value, null, 2));
        client.end();
        break;
      }
    } catch {}
  }
});
