const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var p = doc.GetElement(new ElementId(9907667)) as FamilyInstance; // P1
    var f = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Cast<Floor>().First(fl => fl.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() == "ĐC-2-05");
    
    bool wasJoined = JoinGeometryUtils.AreElementsJoined(doc, p, f);
    if (wasJoined) {
      JoinGeometryUtils.UnjoinGeometry(doc, p, f);
      doc.Regenerate();
    }

    double solidTopZMm = double.MinValue;
    void ProcessGeom(GeometryElement ge) {
      if (ge == null) return;
      foreach (var obj in ge) {
        if (obj is Solid s && s.Volume > 0.0001) {
          foreach (Face face in s.Faces) {
            if (face is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
              double fz = pf.Origin.Z * 304.8;
              if (fz > solidTopZMm) solidTopZMm = fz;
            }
          }
        } else if (obj is GeometryInstance gi) {
          ProcessGeom(gi.GetInstanceGeometry());
        }
      }
    }

    ProcessGeom(p.get_Geometry(new Options()));
    var bb = p.get_BoundingBox(null);

    return new {
      wasJoined,
      solidTopZMm,
      bbMaxZ = bb.Max.Z * 304.8,
      bbMinZ = bb.Min.Z * 304.8
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
