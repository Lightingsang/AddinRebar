import sys, json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_acad_client

sys.stdout.reconfigure(encoding="utf-8")

def inspect_blocks():
    print("=== INSPECTING BLOCK REFERENCES IN MODELSPACE ===")
    with get_acad_client() as acad:
        res = acad.execute('''
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        var gridBubbles = new List<object>();
        var mmBlocks = new List<object>();

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead);
            if (ent is BlockReference br)
            {
                var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                string bname = btr.Name;
                if (bname.Equals("Trucdv-2018-01", StringComparison.OrdinalIgnoreCase))
                {
                    // read attributes
                    string txt = "";
                    foreach (ObjectId attId in br.AttributeCollection)
                    {
                        var att = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                        txt += att.TextString + ";";
                    }
                    gridBubbles.Add(new {
                        x = Math.Round(units.ToMm(br.Position.X), 1),
                        y = Math.Round(units.ToMm(br.Position.Y), 1),
                        tag = txt
                    });
                }
                else if (bname.Equals("mm", StringComparison.OrdinalIgnoreCase))
                {
                    mmBlocks.Add(new {
                        x = Math.Round(units.ToMm(br.Position.X), 1),
                        y = Math.Round(units.ToMm(br.Position.Y), 1)
                    });
                }
            }
        }
        return new { gridBubbles = gridBubbles, mmCount = mmBlocks.Count };
        ''')
        val = res['result']['value']
        print(f"Grid bubbles count: {len(val['gridBubbles'])}")
        print(f"mm (Footing) blocks count: {val['mmCount']}")
        for gb in val['gridBubbles'][:20]:
            print("  Grid bubble:", gb)

if __name__ == '__main__':
    inspect_blocks()
