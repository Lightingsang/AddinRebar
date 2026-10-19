import csv
import json
from probe_m3_adversarial import PipeClient, ACAD_PIPE, parse_val

def main():
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\Bang_Toa_Do_Tim_Coc_Chuan_Ky_Thuat.csv', 'r', encoding='utf-8') as f:
        rows = list(csv.DictReader(f))

    handles = [r['CAD Handle'] for r in rows]

    with PipeClient(ACAD_PIPE, 'autocad') as c:
        h_str_list = ', '.join([f'"{h}"' for h in handles])
        code = f"""
        var handles = new string[] {{ {h_str_list} }};
        var res = new System.Collections.Generic.List<object>();
        foreach (var hStr in handles)
        {{
            long hVal = System.Convert.ToInt64(hStr, 16);
            var handle = new Handle(hVal);
            if (db.TryGetObjectId(handle, out ObjectId id))
            {{
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent is BlockReference br)
                {{
                    res.Add(new {{ handle = hStr, name = br.Name, x = Math.Round(br.Position.X, 1), y = Math.Round(br.Position.Y, 1) }});
                }}
                else if (ent != null)
                {{
                    res.Add(new {{ handle = hStr, type = ent.GetType().Name }});
                }}
            }}
            else
            {{
                res.Add(new {{ handle = hStr, missing = true }});
            }}
        }}
        return res;
        """
        res = parse_val(c.execute(code, label='Query 196 handles in CAD'))
        print('Total queried handles:', len(res))
        missing = [x for x in res if x.get('missing')]
        print('Missing count:', len(missing))
        xs = [x['x'] for x in res if 'x' in x]
        ys = [x['y'] for x in res if 'y' in x]
        print('CAD X min/max:', min(xs), max(xs), 'CAD Y min/max:', min(ys), max(ys))
        all_rec_tt = all(x.get('name', '').lower() == 'rec-tt' for x in res)
        print('All 196 entities are Rec-TT:', all_rec_tt)

if __name__ == '__main__':
    main()
