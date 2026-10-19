import subprocess
import json

cmd = [r'C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe', r'C:\Dce Pro\DCE Pro.dll', '--dump-table', 'TypeDef', '--json']
proc = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
data = json.loads(proc.stdout)
rows = data.get('rows', [])

# Map TypeRef tokens
cmd_tr = [r'C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe', r'C:\Dce Pro\DCE Pro.dll', '--dump-table', 'TypeRef', '--json']
proc_tr = subprocess.run(cmd_tr, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8')
data_tr = json.loads(proc_tr.stdout)
typerefs = {r.get('Token'): f"{r.get('Namespace')}.{r.get('Name')}" for r in data_tr.get('rows', [])}

print("=== 11 CAD JIGS ===")
for r in rows:
    token_str = r.get('Token', '')
    try:
        val = int(token_str, 16)
        if 0x02000E52 <= val <= 0x02000E5C:
            bt = typerefs.get(r.get('BaseType'), r.get('BaseType'))
            print(f"{token_str} (RID {r.get('RID')}): {r.get('Namespace')}.{r.get('Name')} | Base: {bt}")
    except:
        pass

print("\n=== 12 SPECIALIZED UI FORMS / PALETTES / RIBBON ===")
form_rids = [310, 353, 355, 357, 582, 2851, 3494, 3530, 3602, 3603, 3614, 3677]
for r in rows:
    if r.get('RID') in form_rids:
        bt = typerefs.get(r.get('BaseType'), r.get('BaseType'))
        print(f"{r.get('Token')} (RID {r.get('RID')}): {r.get('Namespace')}.{r.get('Name')} | Base: {bt}")
