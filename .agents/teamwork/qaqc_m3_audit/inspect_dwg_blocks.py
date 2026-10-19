import sys, json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit')
from audit_toolkit import get_acad_client

sys.stdout.reconfigure(encoding="utf-8")

def inspect_dwg():
    print("=== INSPECTING AUTOCAD DWG CONTENT ===")
    with get_acad_client() as acad:
        res = acad.execute('''
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var blockNames = new List<string>();
        foreach (ObjectId id in bt)
        {
            var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
            if (!btr.IsLayout && !btr.IsAnonymous)
                blockNames.Add(btr.Name);
        }
        return blockNames;
        ''')
        block_names = res['result']['value']
        print(f"Total Block definitions: {len(block_names)}")
        print("Sample block names:", [b for b in block_names if any(k in b.lower() for k in ['mm', 'coc', 'mong', 'dam', 'truc', 'grid', 'col', 'cot'])][:30])

if __name__ == '__main__':
    inspect_dwg()
