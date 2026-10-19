import json

path = r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json'
with open(path, 'r', encoding='utf-8', errors='ignore') as f:
    data = json.load(f)

items = data['muc']
print(f"Total entries: {len(items)}")

with open(r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_2\actions_catalog.txt', 'w', encoding='utf-8') as out:
    for i, it in enumerate(items):
        ten = it.get('ten', '')
        nhom = it.get('nhom', '')
        lop = it.get('lop', '')
        ham = it.get('ham', '')
        mo_ta = it.get('mo_ta', '').replace('\n', ' ')
        cap = it.get('cap', '')
        bac = it.get('bac', '')
        out.write(f"[{i+1:02d}] {ten}\n")
        out.write(f"     Group: {nhom} | Handler: {lop}.{ham} | Cap: {cap} | Bac: {bac}\n")
        out.write(f"     Description: {mo_ta}\n")
        params = it.get('tham_so', [])
        if params:
            out.write(f"     Params: {params}\n")
        ret = it.get('tra_ve', {})
        if ret:
            out.write(f"     Return: {ret}\n")
        out.write("\n")

print("Wrote catalog to actions_catalog.txt")
