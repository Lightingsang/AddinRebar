const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var levelDinhDai = new FilteredElementCollector(doc).OfClass(typeof(Level))
      .Cast<Level>().FirstOrDefault(l => l.Name == "Đỉnh Đài Móng")
      ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();

    var floors = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().ToList();
    var floorPolys = new List<(string Mark, string Type, double ThickMm, double BotZMm, List<XYZ> Loop)>();

    foreach (var f in floors) {
      var bb = f.get_BoundingBox(null);
      double topZ = bb != null ? bb.Max.Z * 304.8 : -1600.0;
      double botZ = bb != null ? bb.Min.Z * 304.8 : -2600.0;
      double thickMm = Math.Round(topZ - botZ);
      string mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "";
      string typeName = f.FloorType.Name;

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
      if (loop.Count >= 3) floorPolys.Add((mark, typeName, thickMm, botZ, loop));
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
      .Where(p => p.Symbol.Family.Name.Contains("Coc") || p.Symbol.Name.Contains("P400") || p.Symbol.Name.Contains("250"))
      .ToList();

    int matched = 0;
    int unmatched = 0;
    var thicknessCount = new Dictionary<double, int>();

    foreach (var p in piles) {
      var loc = (LocationPoint)p.Location;
      if (loc == null) continue;
      var pt = loc.Point;

      double targetThickMm = 1000.0;
      bool found = false;
      foreach (var fp in floorPolys) {
        if (PointInPolygon(pt, fp.Loop)) {
          targetThickMm = fp.ThickMm;
          found = true;
          break;
        }
      }

      if (found) matched++; else unmatched++;
      if (!thicknessCount.ContainsKey(targetThickMm)) thicknessCount[targetThickMm] = 0;
      thicknessCount[targetThickMm]++;

      var pLevel = p.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
      if (pLevel != null && !pLevel.IsReadOnly) pLevel.Set(levelDinhDai.Id);

      var pOffset = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
      pOffset?.Set(0.0);

      var pDai = p.LookupParameter("STR_ChieuCao_DaiCoc");
      pDai?.Set(targetThickMm / 304.8);
    }

    doc.Regenerate();

    // Verify solid tops of all piles
    var optP = new Options();
    int correctEmbedmentCount = 0;
    var errors = new List<string>();

    foreach (var p in piles) {
      var loc = (LocationPoint)p.Location;
      var pt = loc.Point;
      double solidTopZMm = double.MinValue;
      var geom = p.get_Geometry(optP);
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

      // Check matching footing
      double expectedTopZ = -2500.0;
      foreach (var fp in floorPolys) {
        if (PointInPolygon(pt, fp.Loop)) {
          expectedTopZ = fp.BotZMm + 100.0;
          break;
        }
      }

      if (Math.Abs(solidTopZMm - expectedTopZ) <= 1.0) {
        correctEmbedmentCount++;
      } else {
        errors.Add($"Pile {p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString()} topZ={solidTopZMm:F1} != exp={expectedTopZ:F1}");
      }
    }

    return new {
      totalPiles = piles.Count,
      matched,
      unmatched,
      thicknessCount,
      correctEmbedmentCount,
      errorsCount = errors.Count,
      sampleErrors = errors.Take(5).ToList()
    };
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
