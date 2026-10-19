import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        var updates = new (long id, double offsetFt, string comment)[] {
            (9907667L, -1500.0 / 304.8, "ĐC-2-01"),
            (9907668L, -1500.0 / 304.8, "ĐC-2-01")
        };
        int updated = 0;
        foreach (var u in updates)
        {
            var p = doc.GetElement(new ElementId(u.id)) as FamilyInstance;
            if (p != null) updated++;
        }
        return new { count = updated };
        """
        res = ipc.execute(code, transaction='none', label='Test Typed Array')
        print(res)

if __name__ == '__main__':
    main()
