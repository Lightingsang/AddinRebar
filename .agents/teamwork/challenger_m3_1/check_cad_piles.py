import json
from probe_m3_adversarial import PipeClient, ACAD_PIPE, parse_val

def main():
    with PipeClient(ACAD_PIPE, 'autocad') as c:
        code = """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        int ptCount = 0;
        int recTtCount = 0;
        var pts = new System.Collections.Generic.List<object>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent == null) continue;
            if (ent is DBPoint pt && ent.Layer == "Point coc")
            {
                ptCount++;
                pts.Add(new { x = Math.Round(pt.Position.X, 2), y = Math.Round(pt.Position.Y, 2) });
            }
            if (ent is BlockReference br && br.Name.Equals("Rec-TT", StringComparison.OrdinalIgnoreCase))
            {
                recTtCount++;
            }
        }
        return new { ptCount, recTtCount, pts };
        """
        res = parse_val(c.execute(code, label='Count CAD Piles'))
        print(f"CAD Piles count: ptCount={res['ptCount']}, recTtCount={res['recTtCount']}")
        with open('cad_piles_raw.json', 'w') as f:
            json.dump(res, f, indent=2)

if __name__ == '__main__':
    main()
