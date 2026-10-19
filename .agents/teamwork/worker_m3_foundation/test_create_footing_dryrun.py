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

        var fType = doc.GetElement(new ElementId(8247743L)) as FloorType;

        // Sample rectangle: 16100, -850 to 17000, 750 (ĐC-2-01)
        double mmToFt = 1.0 / 304.8;
        var p0 = new XYZ(16100.0 * mmToFt, -850.0 * mmToFt, 0);
        var p1 = new XYZ(17000.0 * mmToFt, -850.0 * mmToFt, 0);
        var p2 = new XYZ(17000.0 * mmToFt,  750.0 * mmToFt, 0);
        var p3 = new XYZ(16100.0 * mmToFt,  750.0 * mmToFt, 0);

        var loop = new CurveLoop();
        loop.Append(Line.CreateBound(p0, p1));
        loop.Append(Line.CreateBound(p1, p2));
        loop.Append(Line.CreateBound(p2, p3));
        loop.Append(Line.CreateBound(p3, p0));

        var floor = Floor.Create(doc, new List<CurveLoop> { loop }, fType.Id, lvl.Id);
        floor.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set("ĐC-2-01");
        doc.Regenerate();

        var bb = floor.get_BoundingBox(null);
        double topZ = bb != null ? bb.Max.Z * 304.8 : 0.0;
        double botZ = bb != null ? bb.Min.Z * 304.8 : 0.0;

        return new {
            id = floor.Id.Value,
            level = lvl.Name,
            levelElevMm = lvl.Elevation * 304.8,
            topZ = Math.Round(topZ, 1),
            botZ = Math.Round(botZ, 1),
            thickMm = Math.Round(topZ - botZ, 1)
        };
        """
        res = ipc.execute(code, transaction='auto', dry_run=True, label='Test Floor.Create Footing (DryRun)')
        print(json.dumps(res, indent=2))

if __name__ == '__main__':
    main()
