const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
      .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
      .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250"))
      .ToList();

    var floors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();

    int unjoinedCount = 0;
    foreach (var p in piles) {
      foreach (var f in floors) {
        if (JoinGeometryUtils.AreElementsJoined(doc, p, f)) {
          JoinGeometryUtils.UnjoinGeometry(doc, p, f);
          unjoinedCount++;
        }
      }
    }

    doc.Regenerate();

    var opt = new Options();
    var tops = new Dictionary<double, int>();

    foreach (var p in piles) {
      double solidTopZMm = double.MinValue;
      void Extract(GeometryElement ge) {
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
            Extract(gi.GetInstanceGeometry());
          }
        }
      }
      Extract(p.get_Geometry(opt));
      double roundZ = Math.Round(solidTopZMm, 0);
      if (!tops.ContainsKey(roundZ)) tops[roundZ] = 0;
      tops[roundZ]++;
    }

    return new { unjoinedCount, tops };
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
