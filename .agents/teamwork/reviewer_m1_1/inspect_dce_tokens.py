import subprocess
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

ilspy = r'C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe'
dll = r'C:\Dce Pro\DCE Pro.dll'
cmd = [ilspy, dll, '--dump-table', 'TypeDef', '--json']

proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
stdout, stderr = proc.communicate()

if proc.returncode != 0:
    print("ilspycmd failed:", stderr[:500])
    sys.exit(1)

data = json.loads(stdout)
print("Type of data:", type(data))
if isinstance(data, dict):
    print("Keys of data:", list(data.keys()))
    # inspect first value or table
    for k in list(data.keys())[:3]:
        val = data[k]
        print(f"Key '{k}': type={type(val)}, len={len(val) if hasattr(val, '__len__') else 'N/A'}")
        if isinstance(val, list) and len(val) > 0:
            print("  First element in val:", val[0])
elif isinstance(data, list):
    print("First element:", data[0])
