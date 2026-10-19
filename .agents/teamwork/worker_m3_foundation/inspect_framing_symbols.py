import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var symbols = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsElementType()
            .OfType<FamilySymbol>()
            .Select(s => {
                var pB = s.LookupParameter("b") ?? s.LookupParameter("Width") ?? s.LookupParameter("B");
                var pH = s.LookupParameter("h") ?? s.LookupParameter("Height") ?? s.LookupParameter("H");
                return new {
                    id = s.Id.Value,
                    family = s.FamilyName,
                    name = s.Name,
                    bMm = pB != null ? Math.Round(pB.AsDouble() * 304.8, 1) : 0.0,
                    hMm = pH != null ? Math.Round(pH.AsDouble() * 304.8, 1) : 0.0
                };
            })
            .ToList();
        return new { symbols = symbols };
        """
        res = ipc.execute(code, transaction='none', label='Inspect Framing Symbols')
        for s in res['result']['value']['symbols']:
            print(f"  {s['family']}: {s['name']} (ID: {s['id']}, b={s['bMm']}, h={s['hMm']})")

if __name__ == '__main__':
    main()
