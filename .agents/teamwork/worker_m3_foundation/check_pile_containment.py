import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
    footings_data = json.load(f)['footings']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

print(f"Total footings: {len(footings_data)}")
print(f"Total piles: {len(piles)}")

pile_assignment = {}
for p in piles:
    px = p['revit_x']
    py = p['revit_y']
    matching_fts = []
    for ft in footings_data:
        # Check if inside bounding box
        vx = [v['x'] for v in ft['vertices']]
        vy = [v['y'] for v in ft['vertices']]
        min_x, max_x = min(vx), max(vx)
        min_y, max_y = min(vy), max(vy)
        if min_x - 1.0 <= px <= max_x + 1.0 and min_y - 1.0 <= py <= max_y + 1.0:
            matching_fts.append(ft)
    if len(matching_fts) == 1:
        pile_assignment[p['name']] = matching_fts[0]
    elif len(matching_fts) == 0:
        print(f"Warning: Pile {p['name']} at ({px}, {py}) has NO containing footing!")
    else:
        print(f"Warning: Pile {p['name']} at ({px}, {py}) has MULTIPLE containing footings: {[f['id'] for f in matching_fts]}")

print(f"Piles uniquely assigned by containment: {len(pile_assignment)} / {len(piles)}")

# Check pile elevations distribution from assigned footings
elev_groups = {}
type_groups = {}
for pname, ft in pile_assignment.items():
    ptop = ft['pileTopElevation']
    ftype = ft['type']
    elev_groups[ptop] = elev_groups.get(ptop, 0) + 1
    type_groups[ftype] = type_groups.get(ftype, 0) + 1

print("\nPile counts by assigned Footing Type:")
for t, cnt in sorted(type_groups.items()):
    print(f"  {t}: {cnt} piles")

print("\nPile counts by required Pile Top Elevation:")
for el, cnt in sorted(elev_groups.items()):
    print(f"  {el}m: {cnt} piles")
