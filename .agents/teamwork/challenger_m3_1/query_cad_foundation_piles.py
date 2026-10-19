import json
from probe_m3_adversarial import PipeClient, ACAD_PIPE, parse_val

def main():
    with PipeClient(ACAD_PIPE, 'autocad') as c:
        code = """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
        var piles = new System.Collections.Generic.List<object>();
        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            if (ent is BlockReference br && br.Name.Equals("Rec-TT", StringComparison.OrdinalIgnoreCase))
            {
                double cx = br.Position.X;
                double cy = br.Position.Y;
                // Check if within foundation plan region
                if (cx >= 300000 && cx <= 350000 && cy >= -45000 && cy <= -5000)
                {
                    piles.Add(new {
                        handle = br.Handle.ToString(),
                        cadX = Math.Round(cx, 1),
                        cadY = Math.Round(cy, 1),
                        revitX = Math.Round(cx - 302352.1, 1),
                        revitY = Math.Round(cy - (-38575.9), 1)
                    });
                }
            }
        }
        return piles;
        """
        res = parse_val(c.execute(code, label='Query Foundation Plan Piles'))
        print(f"Total Rec-TT in Foundation Plan region: {len(res)}")
        with open('cad_plan_piles.json', 'w') as f:
            json.dump(res, f, indent=2)

if __name__ == '__main__':
    main()
