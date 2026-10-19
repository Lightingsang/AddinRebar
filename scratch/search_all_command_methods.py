import os
import re

count = 0
commands = []
dce_dir = r"HPDcePro"
for root, dirs, files in os.walk(dce_dir):
    for f in files:
        if f.endswith('.cs'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as src:
                for line in src:
                    m = re.search(r'\[CommandMethod\(\s*"([^"]+)"', line)
                    if m:
                        count += 1
                        commands.append((m.group(1), f, p))

print(f"Total CommandMethod found in HPDcePro: {count}")
unique_cmds = set(c[0] for c in commands)
print(f"Unique command names: {len(unique_cmds)}")
for f in set(c[1] for c in commands):
    sub = [c[0] for c in commands if c[1] == f]
    print(f"  {f}: {len(sub)} commands")
