import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    print("=== EXECUTING TASK 1: CLEAN REVIT MODEL ===")
    with RevitIPC() as ipc:
        cleanup_code = """
        // 1. Defensively pin all Grids and Levels
        var grids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToList();
        int pinnedGrids = 0;
        foreach (var g in grids)
        {
            g.Pinned = true;
            pinnedGrids++;
        }

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToList();
        int pinnedLevels = 0;
        foreach (var l in levels)
        {
            l.Pinned = true;
            pinnedLevels++;
        }

        // 2. Delete all ghost framing elements
        var framingIds = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .ToElementIds();
        int deletedFraming = framingIds.Count;
        if (framingIds.Count > 0)
        {
            doc.Delete(framingIds);
        }

        // 3. Purge legacy 57 clustered floor slabs
        var floorIds = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .ToElementIds();
        int deletedFloors = floorIds.Count;
        if (floorIds.Count > 0)
        {
            doc.Delete(floorIds);
        }

        return new {
            pinnedGrids = pinnedGrids,
            pinnedLevels = pinnedLevels,
            deletedFraming = deletedFraming,
            deletedFloors = deletedFloors
        };
        """
        res = ipc.execute(cleanup_code, transaction='auto', label='Task 1 Cleanup & Pinning')
        print("Task 1 Cleanup Execution Result:", json.dumps(res, indent=2))

        # Verification step
        verify_code = """
        var framingCount = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralFraming)
            .WhereElementIsNotElementType()
            .GetElementCount();

        var grids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToList();
        int gridCount = grids.Count;
        int unpinnedGrids = grids.Count(g => !g.Pinned);

        var levels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToList();
        int levelCount = levels.Count;
        int unpinnedLevels = levels.Count(l => !l.Pinned);

        var floorCount = new FilteredElementCollector(doc)
            .OfClass(typeof(Floor))
            .GetElementCount();

        return new {
            framingCount = framingCount,
            gridCount = gridCount,
            unpinnedGrids = unpinnedGrids,
            levelCount = levelCount,
            unpinnedLevels = unpinnedLevels,
            floorCount = floorCount
        };
        """
        v_res = ipc.execute(verify_code, transaction='none', label='Verify Task 1')
        print("Verification Result:", json.dumps(v_res, indent=2))
        v_val = v_res['result']['value']
        assert v_val['framingCount'] == 0, f"Expected 0 framing, got {v_val['framingCount']}"
        assert v_val['gridCount'] == 27, f"Expected 27 grids, got {v_val['gridCount']}"
        assert v_val['unpinnedGrids'] == 0, f"Expected 0 unpinned grids, got {v_val['unpinnedGrids']}"
        assert v_val['levelCount'] == 10, f"Expected 10 levels, got {v_val['levelCount']}"
        assert v_val['unpinnedLevels'] == 0, f"Expected 0 unpinned levels, got {v_val['unpinnedLevels']}"
        assert v_val['floorCount'] == 0, f"Expected 0 floors, got {v_val['floorCount']}"
        print(">>> TASK 1 VERIFICATION PASSED 100%! <<<")

if __name__ == '__main__':
    main()
