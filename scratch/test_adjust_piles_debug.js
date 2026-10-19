const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var floors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();
    var floorPolys = new List<(string Name, double BotZMm, List<XYZ> Loop)>();
    foreach (var f in floors) {
      var bb = f.get_BoundingBox(null);
      double botZ = bb != null ? bb.Min.Z * 304.8 : -2600.0;
      string mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
      var opt = new Options();
      var geom = f.get_Geometry(opt);
      var loop = new List<XYZ>();
      if (geom != null) {
        foreach (var obj in geom) {
          if (obj is Solid solid && solid.Volume > 0) {
            foreach (Face face in solid.Faces) {
              if (face is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
                foreach (EdgeArray ea in pf.EdgeLoops) {
                  foreach (Edge e in ea) loop.Add(e.AsCurve().GetEndPoint(0));
                }
              }
            }
          }
        }
      }
      if (loop.Count >= 3) floorPolys.Add((mark, botZ, loop));
    }

    bool PointInPolygon(XYZ p, List<XYZ> poly) {
      bool inside = false;
      for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) {
        if (((poly[i].Y > p.Y) != (poly[j].Y > p.Y)) &&
            (p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)) {
          inside = !inside;
        }
      }
      return inside;
    }

    var piles = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
      .OfCategory(BuiltInCategory.OST_StructuralFoundation).Cast<FamilyInstance>()
      .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250") || p.Symbol.Name.Contains("300"))
      .ToList();

    int matchedCount = 0;
    var sampleMatches = new List<object>();

    foreach (var p in piles) {
      var loc = (LocationPoint)p.Location;
      if (loc == null) continue;
      var pt = loc.Point;
      bool matched = false;
      string matchedFooting = "";
      double botZMm = 0;

      foreach (var fp in floorPolys) {
        if (PointInPolygon(pt, fp.Loop)) {
          matched = true;
          matchedFooting = fp.Name;
          botZMm = fp.BotZMm;
          break;
        }
      }

      if (matched) matchedCount++;
      if (sampleMatches.Count < 10) {
        sampleMatches.Add(new {
          mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
          x = pt.X * 304.8,
          y = pt.Y * 304.8,
          matched = matched,
          matchedFooting = matchedFooting,
          botZMm = botZMm,
          targetTopZ = botZMm + 100.0
        });
      }
    }

    return new {
      totalPiles = piles.Count,
      matchedCount = matchedCount,
      samples = sampleMatches
    };
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
