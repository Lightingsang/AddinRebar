import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var lvl = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault(l => l.Name == "Đỉnh Đài Móng");

        var sym300x400 = doc.GetElement(new ElementId(5398128L)) as FamilySymbol;
        if (!sym300x400.IsActive) sym300x400.Activate();

        double mmToFt = 1.0 / 304.8;
        // Test beam: from (0, 0) to (5000, 0) at Level 'Đỉnh Đài Móng'
        var p0 = new XYZ(0, 0, lvl.Elevation);
        var p1 = new XYZ(5000.0 * mmToFt, 0, lvl.Elevation);
        var line = Line.CreateBound(p0, p1);

        var beam = doc.Create.NewFamilyInstance(line, sym300x400, lvl, Autodesk.Revit.DB.Structure.StructuralType.Beam);
        beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set("GM-1");
        doc.Regenerate();

        var bb = beam.get_BoundingBox(null);
        double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
        double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;

        return new {
            id = beam.Id.Value,
            level = lvl.Name,
            levelElevMm = lvl.Elevation * 304.8,
            topZ = Math.Round(topZ, 1),
            botZ = Math.Round(botZ, 1),
            heightMm = Math.Round(topZ - botZ, 1)
        };
        """
        res = ipc.execute(code, transaction='auto', dry_run=True, label='Test Create Beam (DryRun)')
        print(json.dumps(res, indent=2))

if __name__ == '__main__':
    main()
