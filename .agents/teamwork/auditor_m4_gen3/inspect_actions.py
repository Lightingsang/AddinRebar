import os, sys, re, json
sys.stdout.reconfigure(encoding='utf-8')

handler_dir = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\src\HPDcePro.Excel\ActionHandlers'
all_actions = set()

for f in sorted(os.listdir(handler_dir)):
    if f.endswith('.cs'):
        path = os.path.join(handler_dir, f)
        with open(path, 'r', encoding='utf-8') as fp:
            content = fp.read()
        match = re.search(r'private static readonly string\[\] s_actions\s*=\s*\{([^}]+)\};', content, re.S)
        if match:
            raw = match.group(1)
            actions = re.findall(r'"([^"]+)"', raw)
            for a in actions:
                all_actions.add(a)

danhba_path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json'
with open(danhba_path, 'r', encoding='utf-8-sig') as fp:
    legacy_data = json.load(fp)

items = legacy_data.get('muc', [])
legacy_tens = set(it['ten'] for it in items if 'ten' in it)

print(f'Legacy tens count: {len(legacy_tens)}')
print(f'Implemented actions count: {len(all_actions)}')

missing = legacy_tens - all_actions
extra = all_actions - legacy_tens

print(f'Missing in implementation: {len(missing)}')
if missing:
    for m in sorted(missing):
        print(f'  Missing: {m}')

print(f'Extra in implementation: {len(extra)}')
if extra:
    for e in sorted(extra):
        print(f'  Extra: {e}')
