import json

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\revit_live_survey_summary.json', 'r', encoding='utf-8') as f:
    revit_data = json.load(f)
revit_piles = revit_data['piles']['items']

with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\piles_true_bim_model.json', 'r', encoding='utf-8') as f:
    true_piles = json.load(f)['piles']

print(f"Revit piles count: {len(revit_piles)}")
print(f"True piles count: {len(true_piles)}")

# Match by coordinates
max_diff = 0
unmatched = 0
matched = []
for rp in revit_piles:
    rx = rp['xMm']
    ry = rp['yMm']
    best_dist = 1e9
    best_tp = None
    for tp in true_piles:
        tx = tp['revit_x']
        ty = tp['revit_y']
        d = ((rx - tx)**2 + (ry - ty)**2)**0.5
        if d < best_dist:
            best_dist = d
            best_tp = tp
    if best_dist > max_diff:
        max_diff = best_dist
    if best_dist > 5.0:
        unmatched += 1
    matched.append((rp, best_tp, best_dist))

print(f"Max distance between Revit pile and True pile: {max_diff:.3f} mm")
print(f"Piles with distance > 5mm: {unmatched}")
