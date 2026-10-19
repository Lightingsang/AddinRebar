const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hprebar-mcp-r2026', () => {
  const script = `
    var p = doc.GetElement(new ElementId(9907702)) as FamilyInstance;
    var levelDinhDai = new FilteredElementCollector(doc).OfClass(typeof(Level))
      .Cast<Level>().FirstOrDefault(l => l.Name == "Đỉnh Đài Móng");
    
    // Check which level parameter is writable
    var pLevel = p.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
    var pLevel2 = p.get_Parameter(BuiltInParameter.LEVEL_PARAM);
    bool canSet1 = pLevel != null && !pLevel.IsReadOnly;
    bool canSet2 = pLevel2 != null && !pLevel2.IsReadOnly;

    if (canSet1 && levelDinhDai != null) pLevel.Set(levelDinhDai.Id);
    else if (canSet2 && levelDinhDai != null) pLevel2.Set(levelDinhDai.Id);

    var pOffset = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
    pOffset?.Set(0.0);

    var pDai = p.LookupParameter("STR_ChieuCao_DaiCoc");
    pDai?.Set(1200.0 / 304.8); // Footing ĐC-4-05 is 1200mm thick!

    doc.Regenerate();

    var opt = new Options();
    var geom = p.get_Geometry(opt);
    double solidTopZ = 0;
    foreach (var obj in geom) {
      if (obj is GeometryInstance gi) {
        foreach (var io in gi.GetInstanceGeometry()) {
          if (io is Solid s && s.Volume > 0) {
            foreach (Face f in s.Faces) {
              if (f is PlanarFace pf && pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) {
                solidTopZ = pf.Origin.Z * 304.8;
              }
            }
          }
        }
      }
    }

    return new {
      canSet1,
      canSet2,
      levelName = (p.Host as Level)?.Name ?? p.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsValueString(),
      offsetMm = pOffset.AsDouble() * 304.8,
      chieuCaoDaiMm = pDai.AsDouble() * 304.8,
      solidTopZ = solidTopZ
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
