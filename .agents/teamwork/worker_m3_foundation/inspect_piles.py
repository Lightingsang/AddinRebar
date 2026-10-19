import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var symbols = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .WhereElementIsElementType()
            .OfType<FamilySymbol>()
            .Select(s => new {
                id = s.Id.Value,
                familyName = s.FamilyName,
                name = s.Name
            })
            .ToList();

        var piles = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .Select(p => {
                var loc = (LocationPoint)p.Location;
                var pt = loc != null ? loc.Point : XYZ.Zero;
                double offsetFt = p.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM)?.AsDouble() ?? 0.0;
                return new {
                    id = p.Id.Value,
                    familyName = p.Symbol.FamilyName,
                    symbolName = p.Symbol.Name,
                    mark = p.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "",
                    comment = p.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? "",
                    level = doc.GetElement(p.LevelId)?.Name ?? "",
                    x = Math.Round(pt.X * 304.8, 2),
                    y = Math.Round(pt.Y * 304.8, 2),
                    z = Math.Round(pt.Z * 304.8, 2),
                    offset = Math.Round(offsetFt * 304.8, 2)
                };
            })
            .ToList();

        return new {
            symbols = symbols,
            pileCount = piles.Count,
            pilesSample = piles.Take(5).ToList(),
            zSummary = piles.GroupBy(p => p.z).Select(g => new { z = g.Key, count = g.Count() }).ToList()
        };
        """
        res = ipc.execute(code, transaction='none', label='Inspect Foundation Symbols & Piles')
        if 'result' not in res or 'value' not in res['result']:
            print("Error in res:", json.dumps(res, indent=2))
            return
        val = res['result']['value']
        print("Foundation Symbols:")
        for s in val['symbols']:
            print(f"  {s['familyName']}: {s['name']} (ID: {s['id']})")
        print(f"\nTotal Piles in Revit: {val['pileCount']}")
        print("Pile Z distribution in Revit:", json.dumps(val['zSummary'], indent=2))
        print("Piles Sample:", json.dumps(val['pilesSample'], indent=2))

if __name__ == '__main__':
    main()
