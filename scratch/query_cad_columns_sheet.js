const net = require('net');
const client = net.createConnection('\\\\.\\pipe\\hpautocad-mcp-2026', () => {
  const script = `
    // Search texts or block references around x: 739659, y: -47064
    var db = HostApplicationServices.WorkingDatabase;
    var doc = Application.DocumentManager.MdiActiveDocument;
    var ed = doc.Editor;

    var filter = new SelectionFilter(new TypedValue[] {
      new TypedValue((int)DxfCode.Start, "*")
    });

    // Let's query texts around (700000..800000, -70000..-30000)
    var pMin = new Point3d(700000, -70000, 0);
    var pMax = new Point3d(850000, -20000, 0);

    var res = ed.SelectCrossingWindow(pMin, pMax);
    if (res.Status != PromptStatus.OK) return new { status = res.Status.ToString() };

    var layers = new HashSet<string>();
    var blockNames = new HashSet<string>();
    int textCount = 0;
    int insertCount = 0;
    int polyCount = 0;

    using (var tr = db.TransactionManager.StartTransaction()) {
      foreach (SelectedObject so in res.Value) {
        var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
        if (ent == null) continue;
        layers.Add(ent.Layer);
        if (ent is BlockReference br) {
          insertCount++;
          var btr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
          if (btr != null) blockNames.Add(btr.Name);
        } else if (ent is DBText || ent is MText) {
          textCount++;
        } else if (ent is Polyline) {
          polyCount++;
        }
      }
      tr.Commit();
    }

    return new {
      totalSelected = res.Value.Count,
      textCount,
      insertCount,
      polyCount,
      layers = layers.ToList(),
      blockNames = blockNames.ToList()
    };
  `;
  client.write(JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'autocad.execute', params: { code: script } }) + '\n');
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
client.on('error', err => {
  console.error("CAD pipe error:", err.message);
});
