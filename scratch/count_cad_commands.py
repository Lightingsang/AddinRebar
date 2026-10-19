import os
import re

count = 0
commands = []
cad_dir = r"HPDcePro/src/HPDcePro.AutoCad"
for root, dirs, files in os.walk(cad_dir):
    for f in files:
        if f.endswith('.cs'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as src:
                for line in src:
                    m = re.search(r'\[CommandMethod\(\s*"([^"]+)"', line)
                    if m:
                        count += 1
                        commands.append((m.group(1), f))

print(f"Total CommandMethod found in HPDcePro.AutoCad: {count}")
cmd_names = set(c[0] for c in commands)
print(f"Unique command names: {len(cmd_names)}")
