import sys
sys.stdout.reconfigure(encoding='utf-8')
import json
import math

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    piles = json.load(f)['piles']

print(f"Total piles: {len(piles)}")

# For each pile, find nearest neighbors
distances = []
for i, p1 in enumerate(piles):
    dists = []
    for j, p2 in enumerate(piles):
        if i == j: continue
        d = math.hypot(p1['revit_x'] - p2['revit_x'], p1['revit_y'] - p2['revit_y'])
        dists.append((d, j))
    dists.sort()
    distances.append((i, dists[0][0], dists[:5]))

# Sort piles by distance to nearest neighbor
distances.sort(key=lambda x: x[1])

print("\nSmallest nearest-neighbor distances:")
for i, min_d, neighbors in distances[:10]:
    p = piles[i]
    print(f"Pile {p['name']} (handle {p['handle']}) at ({p['revit_x']}, {p['revit_y']}): nearest={min_d:.1f}mm")

print("\nLargest nearest-neighbor distances (isolated piles):")
for i, min_d, neighbors in distances[-15:]:
    p = piles[i]
    print(f"Pile {p['name']} (handle {p['handle']}) at ({p['revit_x']}, {p['revit_y']}): nearest={min_d:.1f}mm")
