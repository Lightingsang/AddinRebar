import json
import sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

sys.stdout.reconfigure(encoding="utf-8")

def main():
    print("=== EXECUTING TASK 3A: MODEL 72 FOOTINGS ===")

    # Load 72 footings
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        footings_data = json.load(f)['footings']

    print(f"Loaded {len(footings_data)} footings from verified_72_footings.json")

    # Group into batches or single script
    # Let's generate C# code to create all 72 footings in one clean transaction
    # Types:
    # 1000mm: 8247743L
    # 1200mm: 8247807L
    # 1300mm: 9414540L

    type_map = {
        1000: 8247743,
        1200: 8247807,
        1300: 9414540
    }

    # Generate C# data
    csharp_items = []
    for ft in footings_data:
        ft_id = ft['id']
        ft_type = ft['type']
        thick = ft['thicknessMm']
        type_id = type_map[thick]
        verts = ft['vertices']
        # 4 vertices
        v_str = ", ".join([f"({v['x']:.1f}, {v['y']:.1f})" for v in verts])
        csharp_items.append(f'            ("{ft_id}", "{ft_type}", {type_id}L, {thick}, new (double x, double y)[] {{ {v_str} }})')

    items_block = ",\n".join(csharp_items)

    create_script = f"""
    var lvl = new FilteredElementCollector(doc)
        .OfClass(typeof(Level))
        .Cast<Level>()
        .FirstOrDefault(l => l.Name == "Đỉnh Đài Móng");

    var items = new (string id, string type, long typeId, int thick, (double x, double y)[] pts)[] {{
{items_block}
    }};

    double mmToFt = 1.0 / 304.8;
    var created = new List<object>();

    foreach (var it in items)
    {{
        var loop = new CurveLoop();
        for (int i = 0; i < it.pts.Length; i++)
        {{
            var pCur = new XYZ(it.pts[i].x * mmToFt, it.pts[i].y * mmToFt, 0);
            var pNext = new XYZ(it.pts[(i + 1) % it.pts.Length].x * mmToFt, it.pts[(i + 1) % it.pts.Length].y * mmToFt, 0);
            loop.Append(Line.CreateBound(pCur, pNext));
        }}

        var floor = Floor.Create(doc, new List<CurveLoop> {{ loop }}, new ElementId(it.typeId), lvl.Id);
        floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(it.id);
        floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(it.type);

        created.Add(new {{
            id = floor.Id.Value,
            mark = it.id,
            type = it.type,
            thick = it.thick
        }});
    }}

    doc.Regenerate();

    return new {{ count = created.Count, items = created }};
    """

    with RevitIPC() as ipc:
        print("Dispatching creation of 72 footings to Revit...")
        res = ipc.execute(create_script, transaction='auto', timeout=120, label='Create 72 Footings')
        val = res['result']['value']
        print(f"Created footings count: {val['count']}")
        assert val['count'] == 72, f"Expected 72 footings, got {val['count']}"

        # Phase 2: Verification Query
        print("Running Phase 2 Verification for 72 Footings...")
        verify_script = """
        var floors = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .Cast<Floor>()
            .Select(f => {
                var bb = f.get_BoundingBox(null);
                double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;
                double thick = f.FloorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() * 304.8 ?? 0.0;
                return new {
                    id = f.Id.Value,
                    mark = f.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    typeName = f.FloorType.Name,
                    topZ = Math.Round(topZ, 1),
                    botZ = Math.Round(botZ, 1),
                    thickMm = Math.Round(thick, 1)
                };
            })
            .ToList();

        return new { count = floors.Count, floors = floors };
        """
        v_res = ipc.execute(verify_script, transaction='none', label='Verify 72 Footings')
        v_val = v_res['result']['value']
        print(f"Verified floor count in Revit: {v_val['count']}")
        assert v_val['count'] == 72

        err_z = 0
        err_h = 0
        for f in v_val['floors']:
            mark = f['mark']
            top_z = f['topZ']
            thick = f['thickMm']
            bot_z = f['botZ']
            # Target topZ is -1600.0 mm
            if abs(top_z - (-1600.0)) > 1.0:
                print(f"Error topZ for {mark}: {top_z}")
                err_z += 1
            # Check thickness matches 1000, 1200, 1300
            expected_thick = 1000 if ("ĐC-1" in mark or "ĐC-2" in mark or "ĐC-3" in mark) and "ĐC-4" not in mark and "ĐC-5" not in mark and "ĐC-6" not in mark else (1200 if ("ĐC-4" in mark or "ĐC-5" in mark) else 1300)
            if abs(thick - expected_thick) > 0.0:
                print(f"Error thick for {mark}: {thick} != {expected_thick}")
                err_h += 1

        print(f"Footing Phase 2 Verification: err_z={err_z}, err_h={err_h}")
        assert err_z == 0 and err_h == 0
        print(">>> 72 FOOTINGS CREATED AND VERIFIED 100%! <<<")

        with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\created_72_footings.json', 'w', encoding='utf-8') as f_out:
            json.dump(v_val['floors'], f_out, indent=2, ensure_ascii=False)

if __name__ == '__main__':
    main()
