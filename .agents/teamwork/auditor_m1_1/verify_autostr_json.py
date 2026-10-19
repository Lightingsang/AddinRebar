import json

autostr_path = r'C:\Dce Pro\AutoStr\DuAnMau.autostr'
with open(autostr_path, 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

print('Top-level keys in DuAnMau.autostr:')
for k in sorted(data.keys()):
    val = data[k]
    if isinstance(val, (int, float, str, bool)):
        print(f'  {k}: {val}')
    elif isinstance(val, list):
        print(f'  {k}: [list of {len(val)} items]')
    elif isinstance(val, dict):
        print(f'  {k}: [dict with {len(val)} keys: {list(val.keys())[:5]}...]')
    else:
        print(f'  {k}: {type(val)}')

if 'Floors' in data and len(data['Floors']) > 0:
    print('\nSample Floor keys:', list(data['Floors'][0].keys()))

if 'Layers' in data:
    print('\nLayers keys:', list(data['Layers'].keys()))
