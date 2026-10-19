const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var p = doc.GetElement(new ElementId(9907667)) as FamilyInstance; // P1
    var opt = new Options();
    var geom = p.get_Geometry(opt);
    double untrimmedTopZ = double.MinValue;

    if (geom != null) {
      foreach (var obj in geom) {
        if (obj is GeometryInstance gi) {
          var symGeom = gi.GetSymbolGeometry();
          var t = gi.Transform;
          foreach (var so in symGeom) {
            if (so is Solid s && s.Volume > 0.0001) {
              foreach (Face f in s.Faces) {
                if (f is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
                  var worldPt = t.OfPoint(pf.Origin);
                  double fz = worldPt.Z * 304.8;
                  if (fz > untrimmedTopZ) untrimmedTopZ = fz;
                }
              }
            }
          }
        }
      }
    }

    return new { untrimmedTopZ };
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
