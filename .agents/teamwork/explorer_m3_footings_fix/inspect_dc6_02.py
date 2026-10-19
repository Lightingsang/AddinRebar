import sys
sys.stdout.reconfigure(encoding='utf-8')
import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    footings = json.load(f)['footings']

fts_dict = {f['id']: f for f in footings}

print("=== ĐC-6-02 ===")
f6_02 = fts_dict['ĐC-6-02']
print("ĐC-6-02:", f6_02)

print("\nPiles near ĐC-6-02 (X in [8000, 11000], Y in [8500, 11500]):")
for p in piles:
    if 8000 <= p['revit_x'] <= 11000 and 8500 <= p['revit_y'] <= 11500:
        print(f"  stt={p['stt']}, handle={p['handle']}, name={p['name']}, revit=({p['revit_x']}, {p['revit_y']})")
