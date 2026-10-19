import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\revit_live_survey_summary.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

grids = data['grids']
print(f"Total Grids: {len(grids)}")
x_grids = []
y_grids = []
for g in sorted(grids, key=lambda x: x['name']):
    # check horizontal or vertical
    x1, y1 = g['x1'], g['y1']
    x2, y2 = g['x2'], g['y2']
    if abs(y1 - y2) < 1.0:
        y_grids.append((g['name'], y1))
    elif abs(x1 - x2) < 1.0:
        x_grids.append((g['name'], x1))
    else:
        print(f"Inclined grid: {g['name']}")

print("\nX Grids (Vertical):")
for name, x in sorted(x_grids, key=lambda x: x[1]):
    print(f"  Grid {name}: X = {x:.1f} mm")

print("\nY Grids (Horizontal):")
for name, y in sorted(y_grids, key=lambda x: x[1]):
    print(f"  Grid {name}: Y = {y:.1f} mm")
