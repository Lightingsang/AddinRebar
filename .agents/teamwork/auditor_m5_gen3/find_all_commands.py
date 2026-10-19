import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

src_path = r'HPDcePro/src'
all_cmds = []
for root, dirs, files in os.walk(src_path):
    for f in files:
        if f.endswith('.cs'):
            full = os.path.join(root, f)
            with open(full, 'r', encoding='utf-8') as fp:
                content = fp.read()
            matches = re.findall(r'\[CommandMethod\(\s*"([^"]+)"', content)
            for m in matches:
                all_cmds.append((m, f, full))

print(f"Total [CommandMethod] across entire {src_path}: {len(all_cmds)}")
