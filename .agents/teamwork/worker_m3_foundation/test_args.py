import sys
import json
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        code = """
        int count = args.GetProperty("items").GetArrayLength();
        return new { count = count };
        """
        sample_args = {"items": [1, 2, 3, 4, 5]}
        res = ipc.execute(code, transaction='none', args=sample_args, label='Test Args')
        print(res)

if __name__ == '__main__':
    main()
