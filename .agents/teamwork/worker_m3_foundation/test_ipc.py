import sys
sys.path.append(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation')
from revit_ipc import RevitIPC

def main():
    with RevitIPC() as ipc:
        res = ipc.execute('return $"Connected: {doc.Title}";', transaction='none', label='Test Connection')
        print("Result:", res)

if __name__ == '__main__':
    main()
