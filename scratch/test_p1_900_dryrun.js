const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var p = doc.GetElement(new ElementId(9907667)) as FamilyInstance; // P1
    var pDai = p.LookupParameter("STR_ChieuCao_DaiCoc");
    pDai.Set(900.0 / 304.8);
    doc.Regenerate();

    var opt = new Options();
    var geom = p.get_Geometry(opt);
    double solidTopZMm = double.MinValue;

    void ProcessGeometry(GeometryElement ge) {
      if (ge == null) return;
      foreach (var obj in ge) {
        if (obj is Solid s && s.Volume > 0.0001) {
          foreach (Face f in s.Faces) {
            if (f is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
              double fz = pf.Origin.Z * 304.8;
              if (fz > solidTopZMm) solidTopZMm = fz;
            }
          }
        } else if (obj is GeometryInstance gi) {
          ProcessGeometry(gi.GetInstanceGeometry());
        }
      }
    }

    ProcessGeometry(geom);

    return new { solidTopZMm };
  `;
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
