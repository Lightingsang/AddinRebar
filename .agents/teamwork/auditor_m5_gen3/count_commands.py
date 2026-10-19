import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

autocad_path = r'HPDcePro/src/HPDcePro.AutoCad'
commands = []
for root, dirs, files in os.walk(autocad_path):
    for f in files:
        if f.endswith('.cs'):
            full = os.path.join(root, f)
            with open(full, 'r', encoding='utf-8') as fp:
                content = fp.read()
            matches = re.findall(r'\[CommandMethod\(\s*"([^"]+)"', content)
            for m in matches:
                commands.append((m, f, full))

print(f"Total [CommandMethod] declared in {autocad_path}: {len(commands)}")
unique_cmds = set(c[0] for c in commands)
print(f"Unique command names: {len(unique_cmds)}")
for c, f, _ in commands:
    print(f"  {c} ({f})")
