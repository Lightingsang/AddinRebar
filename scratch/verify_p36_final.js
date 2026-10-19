const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var p = doc.GetElement(new ElementId(9907702)) as FamilyInstance; // P36
    var pLevel = p.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
    var pOffset = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
    var pDai = p.LookupParameter("STR_ChieuCao_DaiCoc");

    var opt = new Options();
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

    return new {
      id = p.Id.Value,
      mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString(),
      level = pLevel?.AsValueString(),
      offsetMm = pOffset.AsDouble() * 304.8,
      daiMm = pDai.AsDouble() * 304.8,
      solidTopZMm
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
