import subprocess
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

jig_types = [
    ("0x02000E52", "TagLeader"),
    ("0x02000E53", "TagChiaThep"),
    ("0x02000E54", "JigSlabRebar"),
    ("0x02000E55", "VeMBKC_JIG"),
    ("0x02000E56", "ijgRaithepchamtron"),
    ("0x02000E57", "ijgVeDaiVong"),
    ("0x02000E58", "JigVeDaiC"),
    ("0x02000E59", "VePolylineBatKy"),
    ("0x02000E5A", "ijgVeThepVachCT"),
    ("0x02000E5B", "JigVeVachHam"),
    ("0x02000E5C", "DimNhanhMBKC")
]

for token, name in jig_types:
    cmd = [r'C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe', r'C:\Dce Pro\DCE Pro.dll', '-t', f'ClassLibrary1.{name}']
    proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
    out = proc.stdout
    print(f"=== {name} ({token}) ===")
    lines = out.splitlines()
    print(f"Total lines: {len(lines)}")
    # Print signature, fields, and method names
    for line in lines[:30]:
        if any(kw in line for kw in ['class ', 'public ', 'protected ', 'private ']):
            print(f"  {line.strip()}")
    print()
