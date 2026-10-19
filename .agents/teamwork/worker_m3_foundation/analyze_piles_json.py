import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', encoding='utf-8') as f:
    data = json.load(f)

footings = data['footings']
pile_to_footing = {}
type_pile_count = {}
pile_elevations = {}

for ft in footings:
    ft_id = ft['id']
    ft_type = ft['type']
    pile_top = ft['pileTopElevation']
    stts = ft['pileStts']
    type_pile_count[ft_type] = type_pile_count.get(ft_type, 0) + len(stts)
    for stt in stts:
        pile_to_footing[stt] = ft
        pile_elevations[stt] = pile_top

print("Footings count:", len(footings))
print("Piles mapped:", len(pile_to_footing))
print("Piles by Footing Type:")
for t, cnt in sorted(type_pile_count.items()):
    print(f"  {t}: {cnt} piles")

elev_counts = {}
for stt, el in pile_elevations.items():
    elev_counts[el] = elev_counts.get(el, 0) + 1
print("\nPiles by required pile top elevation in verified_72_footings.json:")
for el, cnt in sorted(elev_counts.items()):
    print(f"  {el}m: {cnt} piles")
