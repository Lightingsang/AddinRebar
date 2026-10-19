import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var floorTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(FloorType))
            .Cast<FloorType>()
            .Select(ft => new {
                id = ft.Id.Value,
                name = ft.Name,
                thickMm = ft.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() * 304.8 ?? 0.0
            })
            .ToList();

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Select(l => new {
                id = l.Id.Value,
                name = l.Name,
                elevMm = l.Elevation * 304.8
            })
            .ToList();

        return new { floorTypes = floorTypes, levels = levels };
        """
        res = ipc.execute(code, transaction='none', label='Inspect FloorTypes & Levels')
        val = res['result']['value']
        print("Floor Types:")
        for ft in val['floorTypes']:
            print(f"  {ft['name']} (ID: {ft['id']}, Thickness: {ft['thickMm']:.1f} mm)")
        print("\nLevels:")
        for l in val['levels']:
            print(f"  {l['name']} (ID: {l['id']}, Elev: {l['elevMm']:.1f} mm)")

if __name__ == '__main__':
    main()
