import json
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

ipc = None
for _ in range(5):
    try:
        ipc = RevitIPC()
        break
    except Exception:
        time.sleep(0.5)

with ipc:
    q = """
    var beams = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsNotElementType().Cast<FamilyInstance>()
        .Where(b => {
            var crv = (b.Location as LocationCurve)?.Curve;
            if (crv == null) return false;
            return crv.GetEndPoint(0).X * 304.8 < 0 || crv.GetEndPoint(1).X * 304.8 < 0;
        })
        .Select(b => {
            var crv = (b.Location as LocationCurve).Curve;
            return new {
                id = b.Id.Value,
                mark = b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                sym = b.Symbol.Name,
                p0 = new { x = Math.Round(crv.GetEndPoint(0).X * 304.8, 1), y = Math.Round(crv.GetEndPoint(0).Y * 304.8, 1) },
                p1 = new { x = Math.Round(crv.GetEndPoint(1).X * 304.8, 1), y = Math.Round(crv.GetEndPoint(1).Y * 304.8, 1) },
                len = Math.Round(crv.Length * 304.8, 1)
            };
        }).ToList();
    return beams;
    """
    res = ipc.execute(q, transaction='none', label='Query negative X beams')
    for b in res['result']['value']:
        print(b)
