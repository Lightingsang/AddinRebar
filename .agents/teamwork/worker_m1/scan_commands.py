import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

cmd_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.AutoCad\Commands"
files = [os.path.join(cmd_dir, f) for f in os.listdir(cmd_dir) if f.endswith('.cs')]

found = {}
for f in files:
    rel_path = os.path.relpath(f, r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar").replace('\\', '/')
    with open(f, encoding='utf-8') as fp:
        for idx, line in enumerate(fp):
            m = re.search(r'\[CommandMethod\("([^"]+)"\)\]', line)
            if m:
                cmd = m.group(1)
                found[cmd] = (rel_path, idx + 1)

print(f"Total command methods found in C# source: {len(found)}")
for cmd, (p, ln) in sorted(found.items()):
    print(f"{cmd} -> {p}:{ln}")
