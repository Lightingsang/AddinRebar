import json

def load_json(path):
    with open(path, 'rb') as f:
        raw = f.read()
    if raw.startswith(b'\xff\xfe') or raw.startswith(b'\xfe\xff'):
        return json.loads(raw.decode('utf-16'))
    elif raw.startswith(b'\xef\xbb\xbf'):
        return json.loads(raw.decode('utf-8-sig'))
    else:
        try:
            return json.loads(raw.decode('utf-8'))
        except UnicodeDecodeError:
            return json.loads(raw.decode('latin1'))

typedefs = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_typedef.json')
impls = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_interfaceimpl.json')
typerefs = load_json('.agents/teamwork/spec_miner_m1_3/dbimrevit_typeref.json')

print('TypeDef keys:', typedefs.keys() if isinstance(typedefs, dict) else len(typedefs))
print('InterfaceImpl keys:', impls.keys() if isinstance(impls, dict) else len(impls))
print('TypeRef keys:', typerefs.keys() if isinstance(typerefs, dict) else len(typerefs))

# If there is a 'rows' key or table name key:
for k, v in typedefs.items():
    if isinstance(v, list):
        print(f"typedefs['{k}'] has {len(v)} items")
for k, v in typerefs.items():
    if isinstance(v, list):
        print(f"typerefs['{k}'] has {len(v)} items")
for k, v in impls.items():
    if isinstance(v, list):
        print(f"impls['{k}'] has {len(v)} items")

td_rows = typedefs['rows']
tr_rows = typerefs['rows']
impl_rows = impls['rows']

revit_interfaces = {}
for tr in tr_rows:
    name = tr.get('Name', '')
    if 'IExternalCommand' in name or 'IExternalApplication' in name:
        token = tr.get('Token')
        print('Found interface TypeRef:', name, token, tr.get('Namespace'))
        revit_interfaces[token] = name

classes = {td['RID']: td for td in td_rows}

commands = []
apps = []

for imp in impl_rows:
    class_val = imp.get('Class')
    if isinstance(class_val, str) and class_val.startswith('0x'):
        class_rid = int(class_val, 16) & 0x00FFFFFF
    elif isinstance(class_val, str):
        class_rid = int(class_val)
    else:
        class_rid = class_val
    
    intf = imp.get('Interface')
    if intf in revit_interfaces:
        td = classes.get(class_rid, {})
        full_name = f"{td.get('Namespace', '')}.{td.get('Name', '')}".strip('.')
        intf_name = revit_interfaces[intf]
        print(f"Token: {td.get('Token')} | RID: {class_rid:3d} | {intf_name:20s} | {full_name}")
        if 'IExternalCommand' in intf_name:
            commands.append((td.get('Token'), td.get('Name'), td.get('Namespace'), full_name))
        elif 'IExternalApplication' in intf_name:
            apps.append((td.get('Token'), td.get('Name'), td.get('Namespace'), full_name))

print(f"\nTotal IExternalCommand implementations: {len(commands)}")
for c in commands:
    print(f"  {c[0]} | {c[3]}")

print(f"\nTotal IExternalApplication implementations: {len(apps)}")
for a in apps:
    print(f"  {a[0]} | {a[3]}")
