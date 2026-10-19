import sys
sys.stdout.reconfigure(encoding='utf-8')
import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    footings = json.load(f)['footings']

fts_dict = {f['id']: f for f in footings}

print("=== ĐC-3-03 ===")
f3_03 = fts_dict['ĐC-3-03']
print("ĐC-3-03:", f3_03)

print("\nPiles near ĐC-3-03 (X in [43500, 46500], Y in [3500, 6500]):")
for p in piles:
    if 43500 <= p['revit_x'] <= 46500 and 3500 <= p['revit_y'] <= 6500:
        print(f"  stt={p['stt']}, handle={p['handle']}, name={p['name']}, revit=({p['revit_x']}, {p['revit_y']})")
