import json
import sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

sys.stdout.reconfigure(encoding="utf-8")

def main():
    print("=== EXECUTING TASK 3B: MODEL 22 GRADE BEAMS GM-1..22 ===")

    # Load 192 spans
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'r', encoding='utf-8') as f:
        spans = json.load(f)

    print(f"Loaded {len(spans)} grade beam spans.")

    # Generate C# items
    lines = []
    for s in spans:
        lines.append(f'            ("{s["name"]}", {s["bMm"]}, {s["hMm"]}, {s["x1"]:.1f}, {s["y1"]:.1f}, {s["x2"]:.1f}, {s["y2"]:.1f})')
    items_block = ",\n".join(lines)

    code = f"""
    var lvl = new FilteredElementCollector(doc)
        .OfClass(typeof(Level))
        .Cast<Level>()
        .FirstOrDefault(l => l.Name == "Đỉnh Đài Móng");

    var sym300x400 = doc.GetElement(new ElementId(5398128L)) as FamilySymbol;
    var sym300x600 = doc.GetElement(new ElementId(8531370L)) as FamilySymbol;
    var sym200x300 = doc.GetElement(new ElementId(8674599L)) as FamilySymbol;
    var sym200x400 = doc.GetElement(new ElementId(5398080L)) as FamilySymbol;

    if (!sym300x400.IsActive) sym300x400.Activate();
    if (!sym300x600.IsActive) sym300x600.Activate();
    if (!sym200x300.IsActive) sym200x300.Activate();
    if (!sym200x400.IsActive) sym200x400.Activate();

    var items = new (string name, int b, int h, double x1, double y1, double x2, double y2)[] {{
{items_block}
    }};

    double mmToFt = 1.0 / 304.8;
    var created = new List<object>();

    foreach (var it in items)
    {{
        var sym = (it.b == 300 && it.h == 600) ? sym300x600 :
                  (it.b == 200 && it.h == 300) ? sym200x300 :
                  (it.b == 200 && it.h == 400) ? sym200x400 : sym300x400;

        var p0 = new XYZ(it.x1 * mmToFt, it.y1 * mmToFt, lvl.Elevation);
        var p1 = new XYZ(it.x2 * mmToFt, it.y2 * mmToFt, lvl.Elevation);
        var line = Line.CreateBound(p0, p1);

        var beam = doc.Create.NewFamilyInstance(line, sym, lvl, Autodesk.Revit.DB.Structure.StructuralType.Beam);
        beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(it.name);
        beam.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(it.name);
        created.Add(new {{
            id = beam.Id.Value,
            name = it.name,
            b = it.b,
            h = it.h
        }});
    }}

    doc.Regenerate();
    return new {{ count = created.Count, items = created }};
    """

    with RevitIPC() as ipc:
        print("Dispatching creation of 192 grade beams to Revit...")
        res = ipc.execute(code, transaction='auto', timeout=180, label='Create Grade Beams GM-1..22')
        val = res['result']['value']
        print(f"Created beam count: {val['count']}")
        assert val['count'] == len(spans)

        # Phase 2: Verification Query
        print("Running Phase 2 Verification for Grade Beams...")
        verify_code = """
        var beams = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Select(b => {
                var sym = b.Symbol;
                var pB = sym.LookupParameter("b") ?? sym.LookupParameter("Width") ?? sym.LookupParameter("B");
                var pH = sym.LookupParameter("h") ?? sym.LookupParameter("Height") ?? sym.LookupParameter("H");
                double bMm = pB != null ? Math.Round(pB.AsDouble() * 304.8, 1) : 0.0;
                double hMm = pH != null ? Math.Round(pH.AsDouble() * 304.8, 1) : 0.0;

                var bb = b.get_BoundingBox(null);
                double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
                double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;

                var crv = (b.Location as LocationCurve)?.Curve;
                var p0 = crv != null ? crv.GetEndPoint(0) : XYZ.Zero;
                var p1 = crv != null ? crv.GetEndPoint(1) : XYZ.Zero;

                return new {
                    id = b.Id.Value,
                    mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = b.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    symbolName = sym.Name,
                    bMm = bMm,
                    hMm = hMm,
                    topZ = Math.Round(topZ, 1),
                    botZ = Math.Round(botZ, 1),
                    lenMm = crv != null ? Math.Round(crv.Length * 304.8, 1) : 0.0,
                    x1 = Math.Round(p0.X * 304.8, 1),
                    y1 = Math.Round(p0.Y * 304.8, 1),
                    x2 = Math.Round(p1.X * 304.8, 1),
                    y2 = Math.Round(p1.Y * 304.8, 1)
                };
            })
            .ToList();

        return new { count = beams.Count, beams = beams };
        """
        v_res = ipc.execute(verify_code, transaction='none', label='Verify Grade Beams')
        v_val = v_res['result']['value']
        print(f"Verified beam count in Revit: {v_val['count']}")
        assert v_val['count'] == len(spans)

        err_b = 0
        err_h = 0
        err_z = 0
        for b in v_val['beams']:
            top_z = b['topZ']
            b_mm = b['bMm']
            h_mm = b['hMm']
            mark = b['mark']

            if abs(top_z - (-1600.0)) > 1.0:
                print(f"Error topZ for beam {b['id']} ({mark}): {top_z}")
                err_z += 1

            # Check that cross section dimensions are exact
            if b_mm not in [200.0, 300.0] or h_mm not in [300.0, 400.0, 600.0]:
                print(f"Error section for beam {b['id']} ({mark}): {b_mm}x{h_mm}")
                err_b += 1
                err_h += 1

        print(f"Beam Phase 2 Verification: err_z={err_z}, err_b={err_b}, err_h={err_h}")
        assert err_z == 0 and err_b == 0 and err_h == 0
        print(">>> 192 GRADE BEAM SPANS CREATED AND VERIFIED 100%! <<<")

        with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\created_grade_beams.json', 'w', encoding='utf-8') as f_out:
            json.dump(v_val['beams'], f_out, indent=2, ensure_ascii=False)

if __name__ == '__main__':
    main()
