import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import math
from collections import Counter, defaultdict

def load_json(path):
    with open(path, 'r', encoding='utf-8') as f:
        return json.load(f)

# 1. Inspect piles_true_bim_model.json
piles_model = load_json(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json')
piles = piles_model.get('piles', [])
print(f"Total piles in piles_true_bim_model.json: {len(piles)}")

stt_counts = Counter(p['stt'] for p in piles)
dup_stts = {stt: count for stt, count in stt_counts.items() if count > 1}
print(f"Duplicate STTs: {dup_stts}")

for stt, count in dup_stts.items():
    matching = [p for p in piles if p['stt'] == stt]
    print(f"STT {stt}:")
    for m in matching:
        print(f"  handle={m.get('handle')}, name={m.get('name')}, revit=({m.get('revit_x')}, {m.get('revit_y')}), cad=({m.get('cad_x')}, {m.get('cad_y')})")

# 2. Inspect verified_72_footings.json
fts_data = load_json(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json')
footings = fts_data.get('footings', [])
print(f"\nTotal footings in verified_72_footings.json: {len(footings)}")

# Find which footings use duplicate STTs
print("\nFootings using duplicate STTs:")
for ft in footings:
    used_dups = [stt for stt in ft.get('pileStts', []) if stt in dup_stts]
    if used_dups:
        print(f"Footing {ft['id']} ({ft['type']}): pileStts={ft['pileStts']}, center=({ft['centerX']}, {ft['centerY']}), dims=({ft['widthX']}x{ft['widthY']})")

print("\n--- Let's inspect the 8 shifted footings from dispatch ---")
target_8 = ['ĐC-2-04', 'ĐC-2-10', 'ĐC-2-19', 'ĐC-6-01', 'ĐC-6-04', 'ĐC-3-04', 'ĐC-3-03', 'ĐC-3\'-02']
for ft in footings:
    if ft['id'] in target_8:
        print(f"\nFooting {ft['id']} ({ft['type']}):")
        print(f"  pileStts: {ft['pileStts']}")
        print(f"  center: ({ft['centerX']}, {ft['centerY']})")
        print(f"  dims: {ft['widthX']} x {ft['widthY']}")
        print(f"  vertices: {ft['vertices']}")
