import sys
sys.stdout.reconfigure(encoding='utf-8')
import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    footings = json.load(f)['footings']

f3_01 = [f for f in footings if f['id'] == 'ĐC-3-01'][0]
print("ĐC-3-01:", json.dumps(f3_01, indent=2, ensure_ascii=False))

f3_piles = [p for p in piles if p['stt'] in f3_01['pileStts']]
print("\nAssigned piles:")
for p in f3_piles:
    print(f"  P{p['stt']} at ({p['revit_x']}, {p['revit_y']})")
