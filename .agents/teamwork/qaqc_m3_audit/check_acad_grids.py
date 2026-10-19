import sys, json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_acad_client

sys.stdout.reconfigure(encoding="utf-8")

def check_acad_grids():
    print("=== QUERYING AUTOCAD GRIDS & DIMENSIONS ===")
    with get_acad_client() as acad:
        # Check layers in DWG
        res = acad.execute('''
        var layers = new List<string>();
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                layers.Add(ltr.Name);
            }
        }
        return layers;
        ''')
        print("Raw AutoCAD response:", res)
        if 'result' in res and 'value' in res['result']:
            layers = res['result']['value']
        else:
            print("Error or unexpected structure in response:", res)
            return
        print(f"Total layers: {len(layers)}")
        grid_layers = [l for l in layers if any(k in l.upper() for k in ['TRUC', 'GRID', 'DIM', 'TIEU', 'COT'])]
        print("Relevant layers:", grid_layers)

        # Query texts on grid layers or text matching 1..13, A..N
        res_txt = acad.execute('''
        var texts = new List<object>();
        using (var tr = doc.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead);
                if (ent is DBText dbt)
                {
                    string txt = dbt.TextString.Trim();
                    if ((txt.Length <= 2 && ("ABCDEFGHIJKLMN12345678910111213".Contains(txt))) || dbt.Layer.ToUpper().Contains("TRUC"))
                    {
                        texts.Add(new { layer = dbt.Layer, text = txt, x = Math.Round(dbt.Position.X, 1), y = Math.Round(dbt.Position.Y, 1) });
                    }
                }
                else if (ent is MText mt)
                {
                    string txt = mt.Text.Trim();
                    if ((txt.Length <= 2 && ("ABCDEFGHIJKLMN12345678910111213".Contains(txt))) || mt.Layer.ToUpper().Contains("TRUC"))
                    {
                        texts.Add(new { layer = mt.Layer, text = txt, x = Math.Round(mt.Location.X, 1), y = Math.Round(mt.Location.Y, 1) });
                    }
                }
            }
        }
        return texts;
        ''')
        texts = res_txt['result']['value']
        if isinstance(texts, str): texts = json.loads(texts)
        print(f"Found {len(texts)} potential grid texts in ModelSpace")
        # filter to those with layer containing TRUC
        truc_texts = [t for t in texts if 'TRUC' in t['layer'].upper()]
        print(f"TRUC texts: {len(truc_texts)}")
        for t in truc_texts[:30]:
            print(" ", t)

if __name__ == '__main__':
    check_acad_grids()
