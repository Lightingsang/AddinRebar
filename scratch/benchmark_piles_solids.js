const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
      .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
      .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250"))
      .ToList();

    var opt = new Options();
    var results = new List<object>();

    foreach (var p in piles) {
      var loc = (LocationPoint)p.Location;
      var pt = loc != null ? loc.Point : XYZ.Zero;
      var geom = p.get_Geometry(opt);
      double solidTopZMm = double.MinValue;

      if (geom != null) {
        foreach (var obj in geom) {
          if (obj is GeometryInstance gi) {
            foreach (var io in gi.GetInstanceGeometry()) {
              if (io is Solid s && s.Volume > 0.0001) {
                foreach (Face f in s.Faces) {
                  if (f is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
                    double fz = pf.Origin.Z * 304.8;
                    if (fz > solidTopZMm) solidTopZMm = fz;
                  }
                }
              }
            }
          }
        }
      }

      results.Add(new {
        id = p.Id.Value,
        mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
        x = Math.Round(pt.X * 304.8, 1),
        y = Math.Round(pt.Y * 304.8, 1),
        solidTopZ = Math.Round(solidTopZMm, 1)
      });
    }

    sw.Stop();
    return new { count = results.Count, elapsedMs = sw.ElapsedMilliseconds, sample = results.Take(3).ToList() };
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
