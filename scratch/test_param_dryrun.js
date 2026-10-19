const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var sel = doc.GetElement(new ElementId(9907702)) as FamilyInstance;
    var pDai = sel.LookupParameter("STR_ChieuCao_DaiCoc");
    double oldVal = pDai.AsDouble();
    
    // Set to 100mm (0.1m = 100 / 304.8 ft)
    pDai.Set(100.0 / 304.8);
    doc.Regenerate();

    var opt = new Options();
    var geom = sel.get_Geometry(opt);
    double newMaxZ = 0;
    foreach (var obj in geom) {
      if (obj is GeometryInstance gi) {
        foreach (var io in gi.GetInstanceGeometry()) {
          if (io is Solid s && s.Volume > 0) {
            foreach (Face f in s.Faces) {
              if (f is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
                newMaxZ = pf.Origin.Z * 304.8;
              }
            }
          }
        }
      }
    }

    return new {
      oldValMm = oldVal * 304.8,
      newMaxZ = newMaxZ
    };
  `;
  // Test with dryRun = true so nothing is committed yet
  client.write(JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'revit.execute', params: { code: script, mode: 'auto', dryRun: true } }) + '\n');
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
