import json
import sys
from collections import Counter

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

c = Counter(p['stt'] for p in piles)
dups = {k: v for k, v in c.items() if v > 1}
print('Duplicate STTs in piles_true_bim_model.json:', dups)

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    fts = json.load(f)['footings']

for ft in fts:
    used_dups = [stt for stt in ft['pileStts'] if stt in dups]
    if used_dups:
        print(f"Footing {ft['id']}: pileStts={ft['pileStts']}, center=({ft['centerX']}, {ft['centerY']})")
