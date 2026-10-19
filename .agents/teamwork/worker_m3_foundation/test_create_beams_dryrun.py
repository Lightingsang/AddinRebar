import json
import sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

sys.stdout.reconfigure(encoding="utf-8")

def main():
    print("=== TESTING GRADE BEAMS CREATION (DRY RUN) ===")
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation\cad_extracted_beam_spans.json', 'r', encoding='utf-8') as f:
        spans = json.load(f)

    # Build C# array
    lines = []
    for s in spans[:5]:
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
    int count = 0;
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
        count++;
    }}

    doc.Regenerate();
    return new {{ count = count }};
    """

    with RevitIPC() as ipc:
        res = ipc.execute(code, transaction='auto', dry_run=True, label='Test Beams DryRun')
        print(json.dumps(res, indent=2))

if __name__ == '__main__':
    main()
