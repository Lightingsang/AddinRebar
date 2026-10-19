import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var symVuong = doc.GetElement(new ElementId(9215809L)) as FamilySymbol;
        var symTron400 = doc.GetElement(new ElementId(9215875L)) as FamilySymbol;

        var vuongParams = symVuong.Parameters.Cast<Parameter>()
            .Where(p => p.HasValue)
            .Select(p => new { name = p.Definition.Name, val = p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString() })
            .ToList();

        var tronParams = symTron400.Parameters.Cast<Parameter>()
            .Where(p => p.HasValue)
            .Select(p => new { name = p.Definition.Name, val = p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString() })
            .ToList();

        // Sample instance parameters
        var samplePile = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .FirstOrDefault();

        var instParams = samplePile != null ? samplePile.Parameters.Cast<Parameter>()
            .Where(p => p.HasValue)
            .Select(p => new { name = p.Definition.Name, readOnly = p.IsReadOnly, val = p.AsValueString() ?? p.AsString() ?? p.AsDouble().ToString() })
            .ToList() : null;

        return new {
            vuong = new { name = symVuong.Name, family = symVuong.FamilyName, isLoaded = symVuong.IsActive, parameters = vuongParams },
            tron400 = new { name = symTron400.Name, family = symTron400.FamilyName, isLoaded = symTron400.IsActive, parameters = tronParams },
            sampleInstance = instParams
        };
        """
        res = ipc.execute(code, transaction='none', label='Inspect Pile Family Details')
        print(json.dumps(res['result']['value'], indent=2))

if __name__ == '__main__':
    main()
