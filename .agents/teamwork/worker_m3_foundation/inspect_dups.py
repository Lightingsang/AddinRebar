import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

for stt in [52, 75, 106, 118, 126, 187]:
    matches = [p for p in piles if p['stt'] == stt]
    print(f"STT {stt}: {[m['name'] for m in matches]} {[ (m['revit_x'], m['revit_y']) for m in matches ]}")
