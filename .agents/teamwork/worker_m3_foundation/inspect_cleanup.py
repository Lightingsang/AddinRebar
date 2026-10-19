import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var floors = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .Cast<Floor>()
            .Select(f => new {
                id = f.Id.Value,
                type = f.FloorType.Name,
                comment = f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString() ?? ""
            })
            .ToList();
            
        var framings = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .ToElementIds()
            .Select(id => id.Value)
            .ToList();
            
        var grids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .Select(g => new { id = g.Id.Value, name = g.Name, pinned = g.Pinned })
            .ToList();
            
        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Select(l => new { id = l.Id.Value, name = l.Name, pinned = l.Pinned })
            .ToList();

        return new {
            floorCount = floors.Count,
            floors = floors,
            framingCount = framings.Count,
            gridCount = grids.Count,
            grids = grids,
            levelCount = levels.Count,
            levels = levels
        };
        """
        res = ipc.execute(code, transaction='none', label='Inspect Elements Before Task 1')
        val = res['result']['value']
        print(f"Floor count: {val['floorCount']}")
        print(f"Framing count: {val['framingCount']}")
        print(f"Grid count: {val['gridCount']}")
        print(f"Level count: {val['levelCount']}")
        print("Sample floors:", json.dumps(val['floors'][:5], indent=2))

if __name__ == '__main__':
    main()
