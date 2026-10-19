import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var symTron400 = doc.GetElement(new ElementId(9215875L)) as FamilySymbol;
        if (!symTron400.IsActive) symTron400.Activate();

        var pile = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilyInstance))
            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
            .Cast<FamilyInstance>()
            .FirstOrDefault();

        var locBefore = ((LocationPoint)pile.Location).Point;
        double zTopBefore = pile.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() * 304.8 - 1000.0;

        pile.Symbol = symTron400;
        doc.Regenerate();

        var locAfter = ((LocationPoint)pile.Location).Point;
        double zTopAfter = pile.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() * 304.8 - 1000.0;

        return new {
            before = new { x = locBefore.X * 304.8, y = locBefore.Y * 304.8, zTop = zTopBefore, sym = "P_300x300" },
            after = new { x = locAfter.X * 304.8, y = locAfter.Y * 304.8, zTop = zTopAfter, sym = pile.Symbol.Name }
        };
        """
        res = ipc.execute(code, transaction='auto', dry_run=True, label='Test Assigning P400 Symbol (DryRun)')
        print(json.dumps(res, indent=2))

if __name__ == '__main__':
    main()
