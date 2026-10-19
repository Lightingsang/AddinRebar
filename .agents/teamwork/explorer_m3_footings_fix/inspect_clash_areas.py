import sys
sys.stdout.reconfigure(encoding='utf-8')
import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    footings = json.load(f)['footings']

fts_dict = {f['id']: f for f in footings}

# Inspect DC-6-01 and DC-3'-01
print("=== AREA 1: ĐC-6-01 and ĐC-3'-01 ===")
f6_01 = fts_dict['ĐC-6-01']
f3p_01 = fts_dict['ĐC-3\'-01']
print("ĐC-6-01:", f6_01)
print("ĐC-3'-01:", f3p_01)

# Check all piles near X in [15000, 19000], Y in [2000, 7000]
print("\nPiles near ĐC-6-01 / ĐC-3'-01:")
for p in piles:
    if 15000 <= p['revit_x'] <= 19000 and 2000 <= p['revit_y'] <= 7000:
        print(f"  stt={p['stt']}, handle={p['handle']}, name={p['name']}, revit=({p['revit_x']}, {p['revit_y']})")

print("\n=== AREA 2: ĐC-6-04 and ĐC-3'-02 ===")
f6_04 = fts_dict['ĐC-6-04']
f3p_02 = fts_dict['ĐC-3\'-02']
print("ĐC-6-04:", f6_04)
print("ĐC-3'-02:", f3p_02)

print("\nPiles near ĐC-6-04 / ĐC-3'-02:")
for p in piles:
    if 15000 <= p['revit_x'] <= 19000 and 21000 <= p['revit_y'] <= 26000:
        print(f"  stt={p['stt']}, handle={p['handle']}, name={p['name']}, revit=({p['revit_x']}, {p['revit_y']})")

print("\n=== AREA 3: ĐC-2-09 and ĐC-3-04 ===")
f2_09 = fts_dict['ĐC-2-09']
f3_04 = fts_dict['ĐC-3-04']
print("ĐC-2-09:", f2_09)
print("ĐC-3-04:", f3_04)

print("\nPiles near ĐC-2-09 / ĐC-3-04:")
for p in piles:
    if 28000 <= p['revit_x'] <= 31000 and 1500 <= p['revit_y'] <= 6000:
        print(f"  stt={p['stt']}, handle={p['handle']}, name={p['name']}, revit=({p['revit_x']}, {p['revit_y']})")
